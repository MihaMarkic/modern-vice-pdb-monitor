using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Engine.Messages;

public record OpenSourceFileMessage(PdbFile File, int? Line = default, int? ExecutingLine = default,
    int? Column = default, bool MoveCaret = false);
