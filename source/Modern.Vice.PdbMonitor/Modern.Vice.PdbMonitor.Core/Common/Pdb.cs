using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        ImmutableDictionary<string, PdbVariable> GlobalVariables,
        ImmutableDictionary<int, PdbType> Types,
        ImmutableArray<PdbLine> LinesWithAddress)
{
    public static Pdb Empty { get; } = new Pdb(ImmutableDictionary<PdbPath, PdbFile>.Empty,
        ImmutableDictionary<string, PdbLabel>.Empty,
        ImmutableDictionary<PdbLine, PdbFile>.Empty,
        ImmutableDictionary<string, PdbVariable>.Empty,
        ImmutableDictionary<int, PdbType>.Empty,
        ImmutableArray<PdbLine>.Empty);
    public PdbFile? GetFileOfLine(PdbLine line) => LinesToFilesMap[line];
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
}
public  record AddressRange(ushort StartAddress, ushort Length, ImmutableArray<byte>? Data = null, bool HasMoreData = false)
{
    public static AddressRange FromRange(ushort startAddress, ushort endAddress)
        => new AddressRange(startAddress, (ushort)(endAddress - startAddress + 1));
    public ushort EndAddress => (ushort)(StartAddress + Length - 1);
    public bool IsAddressInRange(ushort address) => address >= StartAddress && address <= EndAddress;
}
public sealed record PdbLabel(ushort Address, string Name);

public sealed record PdbVariable(string Name, int Start, int End, ushort? Base, PdbDefinedType Type);


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

public record PdbFunction(string Name, string XName, PdbPath DefinitionFile, int Start, int End, int LineNumber);

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
