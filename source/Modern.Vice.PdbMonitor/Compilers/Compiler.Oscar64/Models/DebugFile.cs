using System.Collections.Immutable;
using JsonSubTypes;
using Newtonsoft.Json;

namespace Compiler.Oscar64.Models;
public class DebugFile
{
    public ImmutableArray<MemoryBlock> Memory { get; init; } = ImmutableArray<MemoryBlock>.Empty;
    public ImmutableArray<Variable> Variables { get; init; } = ImmutableArray<Variable>.Empty;
    public ImmutableArray<Function> Functions { get; init; } = ImmutableArray<Function>.Empty;
    public ImmutableArray<Oscar64Type> Types { get; init; } = ImmutableArray<Oscar64Type>.Empty;
}

public enum MemoryBlockType
{
    NativeCode,
    Data,
    Start,
    End
}
public interface IWithReferences
{
    ImmutableArray<SymbolReference> References { get; }
}
public record MemoryBlock(string Name, string XName, ushort Start, ushort End, string Type, string Source, [JsonProperty("line")] int LineNumber);
public record Variable(string Name, ushort Start, ushort End, ushort? Base, int? Enter, int? Leave, int TypeId) : IWithReferences
{
    public ImmutableArray<SymbolReference> References { get; init; } = ImmutableArray<SymbolReference>.Empty;
}
public record Function(string Name, string XName, ushort Start, ushort End, int TypeId, string Source, [JsonProperty("line")] int LineNumber)
    : IWithReferences
{
    public ImmutableArray<FunctionLine> Lines { get; init; } = ImmutableArray<FunctionLine>.Empty;
    public ImmutableArray<Variable> Variables { get; init; } = ImmutableArray<Variable>.Empty;
    public ImmutableArray<SymbolReference> References { get; init; } = ImmutableArray<SymbolReference>.Empty;
}
public record SymbolReference(string Source, int Line, int Column);
public record FunctionLine(ushort Start, ushort End, string Source, [JsonProperty("Line")] int LineNumber);

[JsonConverter(typeof(JsonSubtypes), "type")]
[JsonSubtypes.KnownSubType(typeof(Oscar64UIntType), "uint")]
[JsonSubtypes.KnownSubType(typeof(Oscar64IntType), "int")]
[JsonSubtypes.KnownSubType(typeof(Oscar64BoolType), "bool")]
[JsonSubtypes.KnownSubType(typeof(Oscar64FloatType), "float")]
[JsonSubtypes.KnownSubType(typeof(Oscar64StructType), "struct")]
[JsonSubtypes.KnownSubType(typeof(Oscar64ArrayType), "array")]
[JsonSubtypes.KnownSubType(typeof(Oscar64PtrType), "ptr")]
[JsonSubtypes.KnownSubType(typeof(Oscar64EnumType), "enum")]
// JsonSubtypes does not support null discriminator value
[JsonSubtypes.FallBackSubType(typeof(Oscar64VoidType))]
public abstract record Oscar64Type(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId);
public record Oscar64VoidType(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId)
    : Oscar64Type(Name, TypeId, Size, ElementTypeId);
public record Oscar64UIntType(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId)
    : Oscar64Type(Name, TypeId, Size, ElementTypeId);
public record Oscar64IntType(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId)
    : Oscar64Type(Name, TypeId, Size, ElementTypeId);
public record Oscar64BoolType(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId)
    : Oscar64Type(Name, TypeId, Size, ElementTypeId);
public record Oscar64FloatType(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId)
    : Oscar64Type(Name, TypeId, Size, ElementTypeId);
public record Oscar64StructType(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId,
    ImmutableArray<Oscar64StructMember> Members)
    : Oscar64Type(Name, TypeId, Size, ElementTypeId);
public record Oscar64ArrayType(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId)
    : Oscar64Type(Name, TypeId, Size, ElementTypeId);
public record Oscar64PtrType(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId)
    : Oscar64Type(Name, TypeId, Size, ElementTypeId);
public record Oscar64EnumType(string Name, int TypeId, int Size, [JsonProperty("eid")] int? ElementTypeId,
    ImmutableArray<Oscar64EnumMember> Members)
    : Oscar64Type(Name, TypeId, Size, ElementTypeId);

public record Oscar64StructMember(string Name, int Offset, int TypeId);
public record Oscar64EnumMember(string Name, uint Value);

public sealed record AssemblyFunction(string Name, string FullName, ImmutableArray<AssemblySourceLine> SourceLines);
public sealed record AssemblySourceLine(int LineNumber, string FilePath, ImmutableArray<AssemblyExecutionLine> ExecutionLines);
public sealed record AssemblyExecutionLine(ushort Address, string Text, ImmutableArray<byte> Data);
