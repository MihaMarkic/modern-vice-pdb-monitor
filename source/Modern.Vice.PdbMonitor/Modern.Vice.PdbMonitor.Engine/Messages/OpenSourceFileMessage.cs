using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.Messages
{
    public record OpenSourceFileMessage(AcmeFile File, int? Line = default, int? ExecutingLine = default);
}
