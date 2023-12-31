using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Compiler.Oscar64.Models;
using Compiler.Oscar64.Services.Implementation;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;

namespace Compiler.Oscar64;

public class Oscar64CompilerServices : ICompilerServices
{
    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    readonly Oscar64DbjParser parser;
    readonly IFileService fileService;
    readonly ILogger<Oscar64CompilerServices> logger;
    public Oscar64CompilerServices(Oscar64DbjParser parser, IFileService fileService, ILogger<Oscar64CompilerServices> logger)
    {
        this.parser = parser;
        this.fileService = fileService;
        this.logger = logger;
    }
    public async Task<(Pdb? Pdb, string? ErrorMessage)> ParseDebugSymbolsAsync(string projectDirectory, string prgPath, 
        CancellationToken ct)
    {
        string metadataFilePath = GetDebugFilePath(projectDirectory, prgPath);
        DebugFile? debugFile = null;
        try
        {
            debugFile = await parser.LoadFileAsync(metadataFilePath, ct);
            if (debugFile is null)
            {
                return (null, $"Debug data file {metadataFilePath} does not exist of failed to deserialize");
            }
            return ParseDebugFile(projectDirectory, debugFile);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    (Pdb? Pdb, string? ErrorMessage) ParseDebugFile(string projectDirectory, DebugFile debugFile)
    {
        var uniqueFileNamesBuilder = ImmutableHashSet.CreateBuilder<string>();
        foreach (var f in debugFile.Functions)
        {
            uniqueFileNamesBuilder.Add(f.Source);
            foreach (var l in f.Lines)
            {
                uniqueFileNamesBuilder.Add(l.Source);
            }
        }
        var uniqueFileNames = uniqueFileNamesBuilder.ToImmutable();
        var paths = uniqueFileNames.Select(n => new { Original = n, Path = PdbPath.Create(projectDirectory, n) })
            .ToImmutableDictionary(i => i.Original, i => i.Path);
        var emptyPdbFiles = uniqueFileNames.Select(n => PdbFile.CreateWithNoContent(paths[n]))
            .ToImmutableDictionary(f => f.Path, f => f);

        var pdbTypes = CreatePdbTypes(debugFile.Types);
        var globalVariables = CreateVariables(projectDirectory, pdbTypes, debugFile.Variables);

        var (pdbLinesAndFunctions, localVariablesMap) = CreatePdbLines(projectDirectory, debugFile.Functions, emptyPdbFiles, paths, pdbTypes);
        var pdbFiles = pdbLinesAndFunctions.Select(
            p => p.Key with { Lines = p.Value.Lines, Functions = p.Value.Functions })
            .ToImmutableArray();
        var lines = pdbFiles.SelectMany(f => f.Lines).ToImmutableArray();
        var finalLineToFileMap = (from f in pdbFiles
                                  from l in f.Lines
                                  select new { File = f, Line = l })
                          .ToImmutableDictionary(p => p.Line, p => p.File, 
                                RhReferenceEqualityComparer<PdbLine>.Instance);
        var pdbFilesMap = pdbFiles.ToImmutableDictionary(f => f.Path, f => f);
        var symbolReferencesMap = CreateSymbolReferences(projectDirectory, debugFile, pdbFilesMap, globalVariables, localVariablesMap);

        var pdb = new Pdb(
            pdbFilesMap,
            ImmutableDictionary<string, PdbLabel>.Empty,
            finalLineToFileMap,
            globalVariables,
            pdbTypes,
            lines.Where(l => !l.Addresses.IsEmpty).ToImmutableArray(),
            symbolReferencesMap);

        return (pdb, null);
    }

    internal record VariableRange(int? Start, int? End)
    {
        public static VariableRange All = new VariableRange(null, null);
        /// <summary>
        /// Returns span between end and start. When All, it returns <see cref="int.MaxValue"/>, otherwise
        /// difference.
        /// </summary>
        /// <remarks>Either range is All or both <see cref="Start"/> and <see cref="End"/> are not nul.</remarks>
        public int Span {
            get => this == All ? 
                int.MaxValue
                : (End ?? throw new Exception("End does not have a value")) 
                    - (Start ?? throw new Exception("End does not have a value"));
        }
        public bool Contains(int Value)
        {
            return this == All ? true: Value >= Start && Value <= End;
        }

    }
    /// <summary>
    /// Stores range of the variable, mapped <see cref="PdbVariable"/> and source <see cref="Variable"/>.
    /// </summary>
    /// <param name="Range"></param>
    /// <param name="Variable"></param>
    /// <param name="Source"></param>
    internal record VariableWithRange(VariableRange Range, PdbVariable Variable, Variable Source);
    /// <summary>
    /// Sorts and arranges possibly nested variables.
    /// </summary>
    /// <param name="types"></param>
    /// <param name="variables"></param>
    /// <returns></returns>
    /// <remarks>Nested range never overlaps outside parent.</remarks>
    internal static ImmutableDictionary<string, ImmutableArray<VariableWithRange>> CreateLineVariables
        (string projectDirectory, ImmutableDictionary<int, PdbType> types, ImmutableArray<Variable> variables)
    {
        if (variables.IsDefaultOrEmpty)
        {
            return ImmutableDictionary<string, ImmutableArray<VariableWithRange>>.Empty;
        }

        var query = from v in variables
                    let pdbVariable = ConvertVariableToPdb(projectDirectory, v, types)
                    where pdbVariable is not null
                    let p = new VariableWithRange(
                        v.Enter is not null && v.Leave is not null
                            ? new VariableRange(v.Enter.Value, v.Leave.Value) : VariableRange.All,
                        pdbVariable, v)
                    group p by p.Variable.Name;
        var map = query.ToImmutableDictionary(
            g => g.Key,
            g => g.OrderBy(r => r.Range.Start ?? -1).ToImmutableArray());

        return map;
    }

    static PdbVariable? ConvertVariableToPdb(string projectDirectory, Variable v, ImmutableDictionary<int, PdbType> types)
    {
        var type = types[v.TypeId] as PdbDefinedType;
        if (type is not null)
        {
            var declarationReference = v.References.FirstOrDefault();
            SymbolDeclarationSource? declarationSource = declarationReference is not null ?
                new SymbolDeclarationSource(PdbPath.Create(projectDirectory, declarationReference.Source), 
                    declarationReference.Line, declarationReference.Column): null;
            return new PdbVariable(v.Name, v.Start, v.End, v.Base, type, declarationSource);
        }
        return null;
    }

    internal static ImmutableDictionary<string, PdbVariable> CreateVariables(string projectDirectory,
        ImmutableDictionary<int, PdbType> types, ImmutableArray<Variable> variables)
    {
        if (variables.IsDefaultOrEmpty)
        {
            return ImmutableDictionary<string, PdbVariable>.Empty;
        }
        var query = from v in variables
                    let pdb = ConvertVariableToPdb(projectDirectory, v, types)
                    where pdb is not null
                    select pdb;

        return query.ToImmutableDictionary(v => v.Name, v => v);
    }

    internal static ImmutableDictionary<int, PdbType> CreatePdbTypes(ImmutableArray<Oscar64Type> types)
    {
        // first creates a map between typeId and Oscar64Type, PdbType
        // at this point members and array types are missing in PdbType
        var map = types
            .Select(t =>
                (
                    Source: t,
                    Type: ParseVariableType(t)
                )
            )
            .ToDictionary(t => t.Type.Id, t => t);
        // fill PdbType.Members using map
        foreach (var p in map.Values.Where(v => v.Type is PdbStructType))
        {
            var source = p.Source as Oscar64StructType;
            var type = p.Type as PdbStructType;
            if (type is not null && source?.Members.IsDefaultOrEmpty == false)
            {
                type.Members = source.Members
                    .Select(m => new PdbTypeMember(m.Name, m.Offset, map[m.TypeId].Type))
                    .ToImmutableArray();
            }
        }

        // fills nesting of types for arrays
        foreach (var p in map.Values.Where(t => t.Source.ElementTypeId is not null))
        {
            var nested = map[p.Source.ElementTypeId!.Value];
            var nestedType = nested.Type as PdbDefinedType;
            if (nestedType is not null)
            {
                switch (p.Type)
                {
                    case PdbArrayType arrayType:
                        arrayType.ReferencedOfType = nestedType;
                        break;
                    case PdbPtrType ptrType:
                        ptrType.ReferencedOfType = nestedType;
                        break;
                }
            }
        }
        return map.ToImmutableDictionary(m => m.Key, m => m.Value.Type);
    }

    internal static PdbType ParseVariableType(Oscar64Type source)
    {
        return source switch
        {
            Oscar64VoidType vt => new PdbVoidType { Id = source.TypeId },
            Oscar64IntType it => it.Size switch
            {
                1 => new PdbValueType(it.TypeId, it.Name, it.Size, PdbVariableType.Byte),
                2 => new PdbValueType(it.TypeId, it.Name, it.Size, PdbVariableType.Int16),
                4 => new PdbValueType(it.TypeId, it.Name, it.Size, PdbVariableType.Int32),
                _ => throw new ArgumentException($"Invalid variable type input parameters combination int and {it.Size}")
            },
            Oscar64UIntType it => it.Size switch
            {
                1 => new PdbValueType(it.TypeId, it.Name, it.Size, PdbVariableType.UByte),
                2 => new PdbValueType(it.TypeId, it.Name, it.Size, PdbVariableType.UInt16),
                4 => new PdbValueType(it.TypeId, it.Name, it.Size, PdbVariableType.UInt32),
                _ => throw new ArgumentException($"Invalid variable type input parameters combination uint and {it.Size}")
            },
            Oscar64BoolType bt => bt.Size switch
            {
                1 => new PdbValueType(bt.TypeId, bt.Name, bt.Size, PdbVariableType.Bool),
                _ => throw new ArgumentException($"Invalid variable type input parameters combination bool and {bt.Size}")
            },
            Oscar64FloatType ft => new PdbValueType(ft.TypeId, ft.Name, ft.Size, PdbVariableType.Float),
            Oscar64PtrType pt => new PdbPtrType(pt.TypeId, pt.Name, pt.Size),
            Oscar64StructType st => new PdbStructType(st.TypeId, st.Name, st.Size),
            Oscar64ArrayType at => new PdbArrayType(at.TypeId, at.Name, at.Size),
            Oscar64EnumType et => CreateEnumType(et),
            _ => throw new ArgumentException($"Invalid variable type parameter {source.GetType().Name}"),
        };
    }
    internal static PdbEnumType CreateEnumType(Oscar64EnumType source)
    {
        var variableType = source.Size switch
        {
            1 => PdbVariableType.UByte,
            2 => PdbVariableType.UInt16,
            4 => PdbVariableType.UInt32,
            _ => throw new Exception($"Size {source.Size} is invalid for enum type member"),
        };
        var members = source.Members.Select(m => new KeyValuePair<object, string>(m.Value, m.Name));
        return new PdbEnumType(source.TypeId, source.Name, source.Size, variableType, members);
    }

    internal (
        ImmutableDictionary<PdbFile, (ImmutableArray<PdbLine> Lines, ImmutableDictionary<string, PdbFunction> Functions)> FilesMap,
        ImmutableDictionary<Variable, PdbVariable> VariablesMap
        )
        CreatePdbLines(
            string projectDirectory,
            ImmutableArray<Function> functions, 
            ImmutableDictionary<PdbPath, PdbFile> files, 
            ImmutableDictionary<string, PdbPath> paths,
            ImmutableDictionary<int, PdbType> types)
    {
        // sorts all text on files
        var filesWithAllText = files.Select(f =>
        {
            var pdbPath = f.Key;
            string absolutePath = pdbPath.IsRelative ? Path.Combine(projectDirectory, pdbPath.Path) : pdbPath.Path;
            var textLines = fileService.ReadAllLines(absolutePath);
            return new { File = f.Value, Text = textLines };
        }).ToImmutableDictionary(r => r.File, r => r.Text);

        var variablesMapBuilder = ImmutableDictionary.CreateBuilder<Variable, PdbVariable>();

        // prepares slots for resulting PdbLines
        // Dictionary of file -> pdb lines array per each source line
        var linesMap = filesWithAllText.ToDictionary(f => f.Key, f => new PdbLine[f.Value.Length]);
        // Dictionary of file -> pdb functions list
        var functionsMap = files.Values.ToDictionary(f => f, f => new List<PdbFunction>());

        // contains files where function is implemented
        var filesWithFunctionCode = new HashSet<PdbFile>();
        // populate all lines from metadata first
        // ignore empty functions
        foreach (var f in functions.Where(f => f.Lines.Length > 0))
        {
            filesWithFunctionCode.Clear();
            // keep track of generated lines
            var linesBuilder = new List<PdbLine>(f.Lines.Length);
            // first creates nested map of variables
            var lineVariablesMap = CreateLineVariables(projectDirectory, types, f.Variables);
            // adds generated variables to global list of mappings between variables and pdb variables
            variablesMapBuilder.AddRange(lineVariablesMap.Values
                .SelectMany(v => v)
                .ToDictionary(r => r.Source, r => r.Variable));
            // then checks if it has any nested variable
            bool hasNestedVariables = lineVariablesMap.Values
                .Any(l => l.Any(v => v.Range != VariableRange.All));
            // when no nesting, variables are the same for each line
            ImmutableDictionary<string, PdbVariable> allLinesVariables;
            if (!hasNestedVariables)
            {
                allLinesVariables = lineVariablesMap
                    .SelectMany(lv => lv.Value)
                    .ToImmutableDictionary(v => v.Variable.Name, v => v.Variable);
            }
            else
            {
                allLinesVariables = ImmutableDictionary<string, PdbVariable>.Empty;
            }
            foreach (var line in f.Lines)
            {
                // functions are placed in implementation files, their definition file is ignored, at least for now
                var path = paths[line.Source];
                var file = files[path];
                filesWithFunctionCode.Add(file);

                var slots = linesMap[file];

                var fileWithText = filesWithAllText[file];
                int lineNumber = line.LineNumber - 1;
                PdbLine pdbLine;
                if (slots[lineNumber] is null)
                {
                    // when pdb line is created, also assign it variables
                    pdbLine = new PdbLine(path, lineNumber, fileWithText[lineNumber])
                    {
                        Variables = hasNestedVariables ? GetLineVariables(lineNumber, lineVariablesMap)
                                        : allLinesVariables,
                    };
                }
                else
                {
                    pdbLine = slots[lineNumber];
                }
                pdbLine = pdbLine with
                {
                    Addresses = pdbLine.Addresses.Add(new AddressRange(line.Start, (ushort)(line.End - line.Start))),
                };
                slots[lineNumber] = pdbLine;
                linesBuilder.Add(pdbLine);
            }
            var definitionPath = paths[f.Source];
            // creates 
            var declarationReference = f.References.FirstOrDefault();
            var declarationSource = declarationReference is not null ?
                new SymbolDeclarationSource(PdbPath.Create(projectDirectory, declarationReference.Source),
                    declarationReference.Line, declarationReference.Column): null;
            var pdbFunction = new PdbFunction(f.Name, f.XName, definitionPath, f.Start, f.End, f.LineNumber, declarationSource);
            // stores all function to all files where it has code
            foreach (var fileWithFunctionCode in filesWithFunctionCode)
            {
                functionsMap[fileWithFunctionCode].Add(pdbFunction);
            }
            // assign function to all generated PdbLines
            foreach (var l in linesBuilder)
            {
                l.Function = pdbFunction;
            }
        }
        // populate text lines that are not in metadata
        foreach (var file in linesMap.Keys)
        {
            var textLines = filesWithAllText[file];
            var slots = linesMap[file];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] is null)
                {
                    slots[i] = new PdbLine(file.Path, i, textLines[i]);
                }
            }
        }
        var functionsResult = functionsMap.ToImmutableDictionary(
            f => f.Key,
            f => f.Value.ToImmutableDictionary(d => d.XName, d => d));
        var linesResult = linesMap.ToImmutableDictionary(f => f.Key, f => f.Value.ToImmutableArray());
        return (
            files.Values.ToImmutableDictionary(f => f, f => (linesResult[f], functionsResult[f])),
            variablesMapBuilder.ToImmutable()
            );
    }
    internal enum PopulateSymbolReferenceTarget
    {
        GlobalVariables,
        LocalVariables,
        Functions
    }

    internal class LineSymbolReferencesBuilder
    {
        public List<LineSymbolReference<PdbVariable>> GlobalVariables { get; } = new();
        public List<LineSymbolReference<PdbVariable>> LocalVariables { get; } = new();
        public List<LineSymbolReference<PdbFunction>> Functions { get; } = new();
        public LineSymbolReferences ToImmutable() => new LineSymbolReferences(GlobalVariables, LocalVariables, Functions);
    }
    /// <summary>
    /// Creates symbol references for each line.
    /// </summary>
    /// <param name="projectDirectory"></param>
    /// <param name="debugFile"></param>
    /// <param name="filesMap"></param>
    /// <param name="globalVariables"></param>
    /// <param name="localVariablesMap"></param>
    /// <returns></returns>
    internal ImmutableDictionary<PdbLine, LineSymbolReferences> CreateSymbolReferences(
        string projectDirectory, DebugFile debugFile, ImmutableDictionary<PdbPath, PdbFile> filesMap,
        ImmutableDictionary<string, PdbVariable> globalVariables, 
        ImmutableDictionary<Variable, PdbVariable> localVariablesMap)
    {
        var builder = new Dictionary<PdbLine, LineSymbolReferencesBuilder>();
        foreach (var globalVariable in debugFile.Variables)
        {
            AddReferences(globalVariable, PopulateSymbolReferenceTarget.GlobalVariables);
        }
        foreach (var function in debugFile.Functions)
        {
            AddReferences(function, PopulateSymbolReferenceTarget.Functions);
            foreach (var variable in function.Variables)
            {
                AddReferences(variable, PopulateSymbolReferenceTarget.LocalVariables);
            }
        }
        void AddReferences(IWithReferences withReferences, PopulateSymbolReferenceTarget target)
        {
            if (!withReferences.References.IsDefaultOrEmpty)
            {
                foreach (var sr in withReferences.References)
                {
                    var pdbPath = PdbPath.Create(projectDirectory, sr.Source);
                    if (filesMap.TryGetValue(pdbPath, out var file) && file.Lines.Length >= sr.Line - 1)
                    {
                        var line = file.Lines[sr.Line - 1];
                        if (!builder.TryGetValue(line, out var lineSymbolReferencesBuilder))
                        {
                            lineSymbolReferencesBuilder = new LineSymbolReferencesBuilder();
                            builder[line] = lineSymbolReferencesBuilder;
                        }
                        switch (target)
                        {
                            case PopulateSymbolReferenceTarget.GlobalVariables:
                                {
                                    var source = (Variable)withReferences;
                                    if (globalVariables.TryGetValue(source.Name, out var globalVariable))
                                    {
                                        lineSymbolReferencesBuilder.GlobalVariables.Add(
                                                new LineSymbolReference<PdbVariable>(sr.Column, source.Name.Length, globalVariable));
                                    }
                                    else
                                    {
                                        logger.LogError("Failed to find global variable {GlobalVariable}", source.Name);
                                    }
                                }
                                break;
                            case PopulateSymbolReferenceTarget.LocalVariables:
                                {
                                    var source = (Variable)withReferences;
                                    if (localVariablesMap.TryGetValue(source, out var variable))
                                    {
                                        lineSymbolReferencesBuilder.LocalVariables.Add(
                                                new LineSymbolReference<PdbVariable>(sr.Column, source.Name.Length, variable));
                                    }
                                    else
                                    {
                                        logger.LogError("Failed to find local variable {LocalVariable} in function {Function}", 
                                            source.Name, line.Function?.Name);
                                    }
                                }
                                break;
                            case PopulateSymbolReferenceTarget.Functions:
                                {
                                    var source = (Function)withReferences;
                                    var functionPdbPath = PdbPath.Create(projectDirectory, source.Source);
                                    if (filesMap.TryGetValue(functionPdbPath, out var functionFile)
                                        && functionFile.Functions.TryGetValue(source.XName, out var function))
                                    {
                                        lineSymbolReferencesBuilder.Functions.Add(
                                                new LineSymbolReference<PdbFunction>(sr.Column, function.Name.Length, function));
                                    }
                                    else
                                    {
                                        logger.LogError("Failed to find function {Function}", source.XName);
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        logger.LogError("Failed to find source file {Path}", pdbPath);
                    }
                }
            }
        }
        return builder
            .ToImmutableDictionary(
                p => p.Key,
                p => p.Value.ToImmutable());
    }
    internal ImmutableDictionary<string, PdbVariable> GetLineVariables(int lineNumber,
        ImmutableDictionary<string, ImmutableArray<VariableWithRange>> variablesMap)
    {
        var query = from vm in variablesMap
                    let variable = GetLineVariable(lineNumber, vm.Value)
                    where variable is not null
                    select (Name: vm.Key, Variable: variable);
        return query.ToImmutableDictionary(g => g.Name, g => g.Variable);
    }

    /// <summary>
    /// Returns inner most range variable determined by minimum range's span
    /// </summary>
    /// <param name="lineNumber"></param>
    /// <param name="ranges"></param>
    /// <returns></returns>
    internal static PdbVariable? GetLineVariable(int lineNumber, ImmutableArray<VariableWithRange> ranges)
    {
        return ranges
            .Where(r => r.Range.Contains(lineNumber))
            .OrderBy(r => r.Range.Span).FirstOrDefault()?.Variable;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projectDirectory">Globals.ProjectDirectory!</param>
    /// <param name="prgPath"></param>
    /// <returns></returns>
    internal string GetDebugFilePath(string projectDirectory, string prgPath)
    {
        string directory = Path.Combine(projectDirectory, Path.GetDirectoryName(prgPath) ?? "");
        string project = Path.GetFileNameWithoutExtension(prgPath);
        return Path.Combine(directory, $"{project}.dbj");
    }
}
