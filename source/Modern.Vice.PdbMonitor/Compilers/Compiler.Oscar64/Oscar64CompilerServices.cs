using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Compiler.Oscar64.Models;
using Compiler.Oscar64.Services.Implementation;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;

namespace Compiler.Oscar64;

public class Oscar64CompilerServices : ICompilerServices
{
    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    readonly Oscar64DbjParser parser;
    readonly IFileService fileService;
    public Oscar64CompilerServices(Oscar64DbjParser parser, IFileService fileService)
    {
        this.parser = parser;
        this.fileService = fileService;
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
        var allLines = debugFile.Functions.SelectMany(f => f.Lines).ToImmutableArray();
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
        var globalVariables = CreateVariables(pdbTypes, debugFile.Variables);

        var pdbLinesAndFunctions = CreatePdbLines(projectDirectory, debugFile.Functions, 
            emptyPdbFiles, paths, pdbTypes);
        var pdbFiles = pdbLinesAndFunctions.Select(p => p.Key with { Lines = p.Value.Lines, Functions = p.Value.Functions })
            .ToImmutableArray();
        var lines = pdbFiles.SelectMany(f => f.Lines).ToImmutableArray();
        var finalLineToFileMap = (from f in pdbFiles
                                  from l in f.Lines
                                  select new { File = f, Line = l })
                          .ToImmutableDictionary(p => p.Line, p => p.File, RhReferenceEqualityComparer<PdbLine>.Instance);
        var pdb = new Pdb(
            pdbFiles.ToImmutableDictionary(f => f.Path, f => f),
            ImmutableDictionary<string, PdbLabel>.Empty,
            finalLineToFileMap,
            globalVariables,
            pdbTypes,
            lines.Where(l => !l.Addresses.IsEmpty).ToImmutableArray());

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
    internal record VariableWithRange(VariableRange Range, PdbVariable Variable);
    /// <summary>
    /// Sorts and arranges possibly nested variables.
    /// </summary>
    /// <param name="types"></param>
    /// <param name="variables"></param>
    /// <returns></returns>
    /// <remarks>Nested range never overlaps outside parent.</remarks>
    internal static ImmutableDictionary<string, ImmutableArray<VariableWithRange>> CreateLineVariables
        (ImmutableDictionary<int, PdbType> types, ImmutableArray<Variable> variables)
    {
        if (variables.IsDefaultOrEmpty)
        {
            return ImmutableDictionary<string, ImmutableArray<VariableWithRange>>.Empty;
        }

        var query = from v in variables
                    let pdb = ConvertVariableToPdb(v, types)
                    where pdb is not null
                    let p = new VariableWithRange(
                        v.Enter is not null && v.Leave is not null
                            ? new VariableRange(v.Enter.Value, v.Leave.Value) : VariableRange.All,
                        pdb)
                    group p by p.Variable.Name;
        var map = query.ToImmutableDictionary(
            g => g.Key,
            g => g.OrderBy(r => r.Range.Start ?? -1).ToImmutableArray());

        return map;
    }

    static PdbVariable? ConvertVariableToPdb(Variable v, ImmutableDictionary<int, PdbType> types)
    {
        var type = types[v.TypeId] as PdbDefinedType;
        return type is not null ? new PdbVariable(v.Name, v.Start, v.End, v.Base, type) : null;
    }

    internal static ImmutableDictionary<string, PdbVariable> CreateVariables(ImmutableDictionary<int, PdbType> types,
        ImmutableArray<Variable> variables)
    {
        if (variables.IsDefaultOrEmpty)
        {
            return ImmutableDictionary<string, PdbVariable>.Empty;
        }
        var query = from v in variables
                    let pdb = ConvertVariableToPdb(v, types)
                    where pdb is not null
                    select pdb;

        return query.ToImmutableDictionary(v => v.Name, v => v);
    }

    internal static ImmutableDictionary<int, PdbType> CreatePdbTypes(ImmutableArray<Oscar64Type> types)
    {
        // first creates a map between typeid and Oscar64Type, PdbType
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

    internal ImmutableDictionary<PdbFile, (ImmutableArray<PdbLine> Lines, ImmutableDictionary<string, PdbFunction> Functions)>
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

        // prepares slots for resulting PdbLines
        var linesMap = filesWithAllText.ToDictionary(f => f.Key, f => new PdbLine[f.Value.Length]);
        var functionsMap = files.Values.ToDictionary(f => f, f => new List<PdbFunction>());

        // populate all lines from metadata first
        // ignore empty functions
        foreach (var f in functions.Where(f => f.Lines.Length > 0))
        {
            // functions are placed in implementation files, their definition file is ignored, at least for now
            var firstLineSourcePath = f.Lines.First().Source;
            var path = paths[firstLineSourcePath];
            var file = files[path];
            // keep track of generated lines
            var linesBuilder = new List<PdbLine>(f.Lines.Length);
            var slots = linesMap[file];
            // first creates nested map of variables
            var lineVariablesMap = CreateLineVariables(types, f.Variables);
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
                var fileWithText = filesWithAllText[file];
                int lineNumber = line.LineNumber - 1;
                PdbLine pdbLine;
                if (slots[lineNumber] is null)
                {
                    // when pdb line is created, also assign it variables
                    pdbLine = new PdbLine(lineNumber, fileWithText[lineNumber])
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
            var pdbFunction = new PdbFunction(f.Name, definitionPath, f.Start, f.End, f.LineNumber);
            functionsMap[file].Add(pdbFunction);
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
                    slots[i] = new PdbLine(i, textLines[i]);
                }
            }
        }
        var functionsResult = functionsMap.ToImmutableDictionary(
            f => f.Key,
            f => f.Value.ToImmutableDictionary(d => d.Name, d => d));
        //var pdbLine = new PdbLine(l.Line, l.Start, null, (ushort)(l.End - l.Start), null, "xxx");
        var linesResult = linesMap.ToImmutableDictionary(f => f.Key, f => f.Value.ToImmutableArray());
        return files.Values.ToImmutableDictionary(f => f, f => (linesResult[f], functionsResult[f]));
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
