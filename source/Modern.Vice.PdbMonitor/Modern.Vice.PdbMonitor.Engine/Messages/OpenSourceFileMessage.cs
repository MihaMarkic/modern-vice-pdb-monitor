using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Engine.Messages;

public abstract record OpenFileMessage(PdbFile File, int? Column = default, bool MoveCaret = false);
public record OpenSourceLineNumberFileMessage(PdbFile File, int Line, int? Column = default, bool MoveCaret = false)
    : OpenFileMessage(File, Column, MoveCaret);
public record OpenSourceLineFileMessage(PdbFile File, PdbLine Line, PdbAssemblyLine? AssemblyLine, bool IsExecution,
    int? Column = default, bool MoveCaret = false)
    : OpenFileMessage(File, Column, MoveCaret);
public record OpenAddressMessage(ushort Address);
