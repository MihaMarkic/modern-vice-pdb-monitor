using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.Messages
{
    public record OpenSourceFileMessage(AcmePdbFile File, int? Line = default);
}
