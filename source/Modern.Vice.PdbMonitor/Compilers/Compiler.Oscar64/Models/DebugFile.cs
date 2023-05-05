using System.Collections.Immutable;
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
public record MemoryBlock(string Name, ushort Start, ushort End, string Type, string Source, [JsonProperty("line")] int LineNumber);
public record Variable(string Name, ushort Start, ushort End, ushort? Base, int TypeId);
public  record Function(string Name, ushort Start, ushort End, int TypeId, string Source, [JsonProperty("line")] int LineNumber, 
    ImmutableArray<FunctionLine> Lines, ImmutableArray<Variable> Variables);
public record FunctionLine(ushort Start, ushort End, string Source, [JsonProperty("Line")]int LineNumber);
public record Oscar64Type(string Name, int TypeId, int Size, [JsonProperty("type")]string? TypeName, 
    [JsonProperty("eid")] int? ElementTypeId, ImmutableArray<Oscar64TypeMember> Members);
public record Oscar64TypeMember(string Name, int Offset, int TypeId);
