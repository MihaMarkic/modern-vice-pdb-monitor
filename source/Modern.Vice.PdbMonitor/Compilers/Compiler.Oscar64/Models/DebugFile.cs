using System.Collections.Immutable;
using JsonSubTypes;
using Newtonsoft.Json;

namespace Compiler.Oscar64.Models;
public record DebugFile(
    ImmutableArray<MemoryBlock> Memory,
    ImmutableArray<Variable> Variables,
    ImmutableArray<Function> Functions,
    ImmutableArray<Oscar64Type> Types);

public enum MemoryBlockType
{
    NativeCode,
    Data,
    Start,
    End
}
public record MemoryBlock(string Name, string XName, ushort Start, ushort End, string Type, string Source, [JsonProperty("line")] int LineNumber);
public record Variable(string Name, ushort Start, ushort End, ushort? Base, int? Enter, int? Leave, int TypeId);
public  record Function(string Name, string XName, ushort Start, ushort End, int TypeId, string Source, [JsonProperty("line")] int LineNumber, 
    ImmutableArray<FunctionLine> Lines, ImmutableArray<Variable> Variables);
public record FunctionLine(ushort Start, ushort End, string Source, [JsonProperty("Line")]int LineNumber);

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
