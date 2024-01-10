using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace Modern.Vice.PdbMonitor.Core.Common;

/// <summary>
/// Contains all debugging information
/// </summary>
public record Pdb(ImmutableDictionary<PdbPath, PdbFile> Files, 
        ImmutableDictionary<string, PdbLabel> Labels,
        ImmutableDictionary<PdbLine, PdbFile> LinesToFilesMap,
        ImmutableDictionary<string, PdbVariable> GlobalVariablesMap,
        ImmutableDictionary<int, PdbType> Types,
        ImmutableArray<PdbLine> LinesWithAddress,
        ImmutableDictionary<PdbLine, LineSymbolReferences> SymbolReferences,
        PdbAddressToLineMap AddressToLineMap)
{
    public static Pdb Empty { get; } = new Pdb(ImmutableDictionary<PdbPath, PdbFile>.Empty,
        ImmutableDictionary<string, PdbLabel>.Empty,
        ImmutableDictionary<PdbLine, PdbFile>.Empty,
        ImmutableDictionary<string, PdbVariable>.Empty,
        ImmutableDictionary<int, PdbType>.Empty,
        ImmutableArray<PdbLine>.Empty,
        ImmutableDictionary<PdbLine, LineSymbolReferences>.Empty,
        PdbAddressToLineMap.Empty);
    public PdbFile? GetFileOfLine(PdbLine line) => LinesToFilesMap[line];
    public ImmutableHashSet<PdbVariable> GlobalVariables { get; } = GlobalVariablesMap.Values.ToImmutableHashSet();
}
/// <summary>
/// Provides fast search for lines given address.
/// </summary>
public class PdbAddressToLineMap
{
    public static readonly PdbAddressToLineMap Empty = new PdbAddressToLineMap(ImmutableArray<Segment?>.Empty);
    public record SegmentItem(PdbLine Line, PdbAssemblyLine? AssemblyLine);
    /// <summary>
    /// Contains HiWord segments array.
    /// </summary>
    public ImmutableArray<Segment?> Segments { get; }
    public PdbAddressToLineMap(ImmutableArray<Segment?> segments)
    {
        Segments = segments;
    }
    public class Segment
    {
        public ImmutableArray<SegmentItem?> Addresses { get; }
        public Segment(ImmutableArray<SegmentItem?> addresses)
        {
            Addresses = addresses;
        }
    }
    public SegmentItem? FindLineAtAddress(ushort address)
    {
        byte hi = (byte)(address >> 8);
        var segment = Segments[hi];
        if (segment is null)
        {
            return null;
        }
        byte lo  = (byte)(address & 0xFF);
        return segment.Addresses[lo];
    }
}
public sealed record PdbFile(PdbPath Path, ImmutableDictionary<string, PdbFunction> Functions, ImmutableArray<PdbLine> Lines)
{
    public static readonly PdbFile Empty = 
        new PdbFile(PdbPath.Empty, ImmutableDictionary<string, PdbFunction>.Empty, ImmutableArray<PdbLine>.Empty);
    /// <summary>
    /// Creates an empty PdbFile with just <see cref="PdbFile.Path"/> set.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static PdbFile CreateWithNoContent(PdbPath path) => 
        new PdbFile(path, ImmutableDictionary<string, PdbFunction>.Empty, ImmutableArray<PdbLine>.Empty);
    public static ImmutableDictionary<PdbLine, PdbFile>.Builder CreateLineToFileMapBuilder()
        => ImmutableDictionary.CreateBuilder<PdbLine, PdbFile>(ReferenceEqualityComparer.Instance);
    public static PdbFile CreateFromRelativePath(string relativePath) =>
        new PdbFile(new PdbPath(relativePath, IsRelative: true), ImmutableDictionary<string, PdbFunction>.Empty, ImmutableArray<PdbLine>.Empty);
    public static PdbFile CreateFromAbsolutePath(string absolutePath) =>
        new PdbFile(new PdbPath(absolutePath, IsRelative: false), ImmutableDictionary<string, PdbFunction>.Empty, ImmutableArray<PdbLine>.Empty);
    // TODO improve string creation each time this line is called
    public string Content => string.Join(Environment.NewLine, Lines.Select(l => l.Text));
}
public sealed class PdbFileByPathEqualityComparer : IEqualityComparer<PdbFile>
{
    public static readonly PdbFileByPathEqualityComparer Instance = new PdbFileByPathEqualityComparer();
    public bool Equals(PdbFile? x, PdbFile? y)
    {
        return x?.Path == y?.Path;
    }

    public int GetHashCode(PdbFile obj) => obj.Path.GetHashCode();
}
/// <summary>
/// DataLength might be longer than data where there are more than 8 bytes (ACME report omits next bytes)
/// </summary>
public record PdbLine(PdbPath path, int LineNumber, string Text)
{
    public ImmutableArray<AddressRange> Addresses { get; init; } = ImmutableArray<AddressRange>.Empty;
    public ImmutableDictionary<string, PdbVariable> Variables { get; init; } = ImmutableDictionary<string, PdbVariable>.Empty;
    public ImmutableArray<PdbAssemblyLine> AssemblyLines { get; init; } = ImmutableArray<PdbAssemblyLine>.Empty;
    /// <summary>
    /// Owner function of this line.
    /// </summary>
    public PdbFunction? Function { get; set; }
    public static PdbLine Create(PdbPath path, int lineNumber, string text, AddressRange addressRange, 
        ImmutableDictionary<string, PdbVariable> variables)
    {
        return new PdbLine(path, lineNumber, text)
        {
            Addresses = ImmutableArray<AddressRange>.Empty.Add(addressRange),
            Variables = variables,
        };
    }
    public static PdbLine Create(PdbPath path, int lineNumber, ushort startAddress,
        ImmutableArray<byte> data, ushort dataLength, bool hasMoreData, string text)
    {
        return new PdbLine(path, lineNumber, text)
        {
            Addresses = ImmutableArray<AddressRange>.Empty.Add(new AddressRange(startAddress, dataLength, data, hasMoreData)),
        };
    }
    public bool IsAddressWithinLine(ushort address)
    {
        foreach (var range in Addresses)
        {
            if (range.IsAddressInRange(address))
            {
                return true;
            }
        }
        
        return false;
    }

    public PdbAssemblyLine? GetAssemblyLineAtAddress(ushort address)
    {
        int i = 0;
        while (i < AssemblyLines.Length)
        {
            var line = AssemblyLines[i];
            if (line.IsAddressInRange(address))
            {
                return line;
            }
            if (line.Address > address)
            {
                break;
            }
        }
        return null;
    }
}

public sealed record PdbAssemblyLine(ushort Address, string Text, ImmutableArray<byte> Data)
{
    public bool IsAddressInRange(ushort address) => address >= Address && address < (Address + Data.Length);
}

public  record AddressRange(ushort StartAddress, ushort Length, ImmutableArray<byte>? Data = null, bool HasMoreData = false)
{
    public static AddressRange FromRange(ushort startAddress, ushort endAddress)
        => new AddressRange(startAddress, (ushort)(endAddress - startAddress + 1));
    public ushort EndAddress => (ushort)(StartAddress + Length - 1);
    public bool IsAddressInRange(ushort address) => address >= StartAddress && address <= EndAddress;
}
public sealed record PdbLabel(ushort Address, string Name);

public sealed record PdbVariable(string Name, int Start, int End, ushort? Base, PdbDefinedType Type,
    SymbolDeclarationSource? Definition): IWithDefinition;

public sealed record SymbolDeclarationSource(PdbPath Path, int LineNumber, int ColumnNumber);

/* PdbType stuff */
public abstract class PdbType
{
    public int Id { get; init; }
    public abstract string ValueType { get; }
}

public class PdbVoidType: PdbType {
    public override string ValueType => "void";
}
public abstract class PdbDefinedType: PdbType
{
    public string Name { get; init; }
    public int Size { get; init; }
    public PdbDefinedType(int id, string name, int size)
    {
        Id = id;
        Name = name;
        Size = size;
    }
}

public class PdbValueType: PdbDefinedType
{
    public PdbVariableType VariableType { get; init; }
    public override string ValueType => VariableType.ToString();
    public PdbValueType(int id, string name, int size, PdbVariableType variableType) : base(id, name, size)
    {
        VariableType = variableType;
    }
}
public class PdbEnumType: PdbValueType
{
    public ImmutableDictionary<object, string> ByKey { get; }
    public ImmutableDictionary<string, object> ByValue { get; }
    public PdbEnumType(int id, string name, int size, PdbVariableType variableType, 
        IEnumerable<KeyValuePair<object, string>> values)
        : base(id, name, size, variableType)
    {
        var grouped = values.GroupBy(p => p.Key, g => g.Value, (key, g) => new { Key = key, Values = g.ToImmutableArray() })
            .ToImmutableArray();
        ByKey = grouped.ToImmutableDictionary(v => v.Key, v => string.Join(", ", v.Values));
        ByValue = grouped.ToImmutableDictionary(v => string.Join(", ", v.Values), v => v.Key);
    }

}
public class PdbArrayType: PdbDefinedType
{
    public PdbDefinedType? ReferencedOfType { get; set; }
    public override string ValueType => $"{ReferencedOfType?.ValueType}[]";
    public PdbArrayType(int id, string name, int size) : base(id, name, size)
    {
    }
    public int? ItemsCount => ReferencedOfType?.Size > 0 ? Size / ReferencedOfType.Size : null;
    public override string? ToString() => $"{ReferencedOfType}[{ItemsCount}]";
}
public class PdbPtrType : PdbDefinedType
{
    public PdbDefinedType? ReferencedOfType { get; set; }
    public override string ValueType => $"{ReferencedOfType?.ValueType}*";
    public PdbPtrType(int id, string name, int size) : base(id, name, size)
    {
    }
    public int? Items => ReferencedOfType?.Size > 0 ? Size / ReferencedOfType.Size : null;
    public override string? ToString() => $"Ptr to {ReferencedOfType}";
}

public class PdbStructType: PdbDefinedType
{
    public override string ValueType => "struct";
    public ImmutableArray<PdbTypeMember> Members { get; set; } = ImmutableArray<PdbTypeMember>.Empty;
    public PdbStructType(int id, string name, int size) : base(id, name, size)
    {
    }
    public override string? ToString() => $"Struct {Name} of {Size}B";
}


public record PdbTypeMember(string Name, int Offset, PdbType Type);

public enum PdbVariableType
{
    Void,
    Bool,
    Byte,
    UByte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    /// <summary>
    /// https://en.wikipedia.org/wiki/IEEE_754
    /// </summary>
    Float,
}

public interface IWithDefinition
{
    public string Name { get; }
    public SymbolDeclarationSource? Definition { get; }
}

public record PdbFunction(string Name, string XName, PdbPath DefinitionFile, int Start, int End, int LineNumber,
    SymbolDeclarationSource? Definition): IWithDefinition;

public record PdbParseResult<T>(T ParsedData, ImmutableArray<PdbParseError> Errors);
public static class PdbParseResultBuilder
{
    public static PdbParseResult<T> Create<T>(T parsedData, ImmutableArray<PdbParseError> errors) => new PdbParseResult<T>(parsedData, errors);
}
public record PdbParseError(int LineNumber, string Line, string ErrorText);
/// <summary>
/// Defines both relative and absolute paths.
/// </summary>
/// <param name="Path"></param>
/// <param name="IsRelative"></param>
/// <remarks>On Windows path casing shouldn't matter.</remarks>
public sealed record PdbPath(string Path, bool IsRelative)
{
    public static readonly PdbPath Empty = new PdbPath(string.Empty, IsRelative: true);
    public static PdbPath CreateRelative(string relativePath) => new PdbPath(relativePath, true);
    public static PdbPath CreateAbsolute(string absolutePath) => new PdbPath(absolutePath, false);
    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public string FileName => System.IO.Path.GetFileName(Path);
    public static PdbPath Create(string directory, string path)
    {
        StringComparison comparison = IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var normalizedDirectory = System.IO.Path.GetFullPath(directory);
        var normalizedPath = System.IO.Path.GetFullPath(path);
        bool isRelative = normalizedPath.StartsWith(normalizedDirectory, comparison);
        if (isRelative)
        {
            // take care of last character, otherwise relative path might start with path separator
            int prefixLength = normalizedDirectory.EndsWith(System.IO.Path.PathSeparator)
                ? directory.Length : directory.Length + 1;
            return CreateRelative(normalizedPath[prefixLength..]);
        }
        else
        {
            return CreateAbsolute(path);
        }
    }
    public bool Equals(PdbPath? other)
    {
        if (IsWindows)
        {
            if (other is null)
            {
                return false;
            }
            return other.IsRelative == IsRelative && string.Equals(other?.Path, Path, StringComparison.OrdinalIgnoreCase);
        }
        return base.Equals(other);
    }
    public override int GetHashCode()
    {
        if (IsWindows)
        {
            return HashCode.Combine(IsRelative, Path.ToLowerInvariant());
        }
        return base.GetHashCode();
    }
    public override string ToString()
    {
        string prefix = IsRelative ? "RELATIVE": "ABSOLUTE";
        return $"{prefix}:{Path}";
    }
}

public sealed class PdbLineReferenceEqualityComparer : IEqualityComparer<PdbLine>
{
    public bool Equals(PdbLine? x, PdbLine? y)
    {
        throw new NotImplementedException();
    }

    public int GetHashCode([DisallowNull] PdbLine obj)
    {
        throw new NotImplementedException();
    }
}
/// <summary>
/// Provides link from line to symbol reference.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Column"></param>
/// <param name="Symbol"></param>
public sealed record LineSymbolReference<T>(int Column, int Length, T Symbol);
public sealed record LineSymbolReferences
{
    public static LineSymbolReferences Empty { get; } = new LineSymbolReferences();
    /// <summary>
    /// Provides references to all global variables.
    /// </summary>
    public ImmutableArray<LineSymbolReference<PdbVariable>> GlobalVariables { get; }
    /// <summary>
    /// Provides references to all local variables.
    /// </summary>
    public ImmutableArray<LineSymbolReference<PdbVariable>> LocalVariables { get; }
    /// <summary>
    /// Provides references to all function call occurrences. 
    /// </summary>
    public ImmutableArray<LineSymbolReference<PdbFunction>> Functions { get; }
    public LineSymbolReferences()
    {
        GlobalVariables = ImmutableArray<LineSymbolReference<PdbVariable>>.Empty;
        LocalVariables = ImmutableArray<LineSymbolReference<PdbVariable>>.Empty;
        Functions = ImmutableArray<LineSymbolReference<PdbFunction>>.Empty;
    }
    public LineSymbolReferences(IEnumerable<LineSymbolReference<PdbVariable>> globalVariables,
        IEnumerable<LineSymbolReference<PdbVariable>> localVariables,
        IEnumerable<LineSymbolReference<PdbFunction>> functions)
    {
        GlobalVariables = globalVariables.OrderBy(r => r.Column).ToImmutableArray();
        LocalVariables = localVariables.OrderBy(r => r.Column).ToImmutableArray();
        Functions = functions.OrderBy(r => r.Column).ToImmutableArray();
    }
}

public static class LineSymbolReferencesExtension
{
    /// <summary>
    /// Searches for a symbol that matches the column.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public static T? GetAtColumn<T>(this ImmutableArray<LineSymbolReference<T>> source,
        int column)
        where T: class
    {
        foreach (var reference in source)
        {
            if (reference.Column > column)
            {
                return null;
            }
            if (reference.Column <= column && reference.Column + reference.Length >= column)
            {
                return reference.Symbol;
            }
        }
        return null;
    }
}
