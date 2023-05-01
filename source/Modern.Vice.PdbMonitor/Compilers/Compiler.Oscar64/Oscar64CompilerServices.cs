using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Compiler.Oscar64.Models;
using Compiler.Oscar64.Services.Implementation;
using Microsoft.Extensions.FileProviders;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;

namespace Compiler.Oscar64;

public class Oscar64CompilerServices : ICompilerServices
{
    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    readonly Oscar64DbjParser parser;
    public Oscar64CompilerServices(Oscar64DbjParser parser)
    {
        this.parser = parser;
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

    private static (Pdb? Pdb, string? ErrorMessage) ParseDebugFile(string projectDirectory, DebugFile debugFile)
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

    internal static ImmutableDictionary<string, PdbVariable> CreateVariables(ImmutableDictionary<int, PdbType> types,
        ImmutableArray<Variable> variables)
    {
        if (variables.IsDefaultOrEmpty)
        {
            return ImmutableDictionary<string, PdbVariable>.Empty;
        }
        var query = from v in variables
                    let t = types[v.TypeId] as PdbDefinedType
                    where t is not null
                    select new PdbVariable(v.Name, v.Start, v.End, v.Base, t);

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
                    Type: ParseVariableType(t.TypeId, t.Name, t.TypeName, t.Size)
                )
            )
            .ToDictionary(t => t.Type.Id, t => t);
        // fill PdbType.Members using map
        foreach (var p in map.Values)
        {
            var source = p.Source;
            var type = p.Type;
            if (!source.Members.IsDefaultOrEmpty && type is PdbStructType structType)
            {
                structType.Members = source.Members 
                    .Select(m => new PdbTypeMember(m.Name, m.Offset, map[m.TypeId].Type))
                    .ToImmutableArray();
            }
        }

        // fills nesting of types for arrays
        foreach (var p in map.Values.Where(t => t.Source.ElementTypeId is not null))
        {
            var nested = map[p.Source.ElementTypeId!.Value];
            var nestedType = nested.Type as  PdbDefinedType;
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

    internal static PdbType ParseVariableType(int typeId, string name, string? typeName, int size)
    {
        return typeName switch
        {
            null when size == 0 => new PdbVoidType { Id = typeId },
            "uint" when size == 1 => new PdbValueType(typeId, name, size, PdbVariableType.UByte),
            "int" when size == 1 => new PdbValueType(typeId, name, size, PdbVariableType.Byte),
            "uint" when size == 2 => new PdbValueType(typeId, name, size, PdbVariableType.UInt16),
            "int" when size == 2 => new PdbValueType(typeId, name, size, PdbVariableType.Int16),
            "uint" when size == 4 => new PdbValueType(typeId, name, size, PdbVariableType.UInt32),
            "int" when size == 4 => new PdbValueType(typeId, name, size, PdbVariableType.Int32),
            "bool" when size == 1 => new PdbValueType(typeId, name, size, PdbVariableType.Bool),
            "float" => new PdbValueType(typeId, name, size, PdbVariableType.Float),
            "ptr" => new PdbPtrType(typeId, name, size),
            "struct" => new PdbStructType(typeId, name, size),
            "array" => new PdbArrayType(typeId, name, size),
            _ => throw new ArgumentException($"Invalid variable type input parameters combination {typeName} and {size}")
        };
    }

    internal static 
        ImmutableDictionary<PdbFile, (ImmutableArray<PdbLine> Lines, ImmutableDictionary<string, PdbFunction> Functions)>
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
            string[] textLines = File.ReadAllLines(absolutePath);
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
            foreach (var line in f.Lines)
            {
                var slots = linesMap[file];
                var fileWithText = filesWithAllText[file];
                int lineNumber = line.LineNumber - 1;
                PdbLine pdbLine;
                if (slots[lineNumber] is null)
                {
                    //var pdbLine = new PdbLine(lineNumber, line.Start, null, (ushort)(line.End - line.Start), null,
                    //    fileWithText[lineNumber]);
                    pdbLine = new PdbLine(lineNumber, fileWithText[lineNumber]);
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
            var variables = CreateVariables(types, f.Variables);
            var definitionPath = paths[f.Source];
            var pdbFunction = new PdbFunction(f.Name, definitionPath, f.Start, f.End, f.LineNumber, variables);
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
