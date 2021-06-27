using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class BreakpointViewModel : NotifiableObject
    {
        public uint CheckpointNumber { get; }
        public bool IsCurrentlyHit { get; set; }
        public bool StopWhenHit { get; set; }
        public bool IsEnabled { get; set; }
        public uint HitCount { get; set; }
        public uint IgnoreCount { get; set; }
        public AcmeLine? Line { get; set; }
        public AcmeFile? File { get; set; }
        public AcmeLabel? Label { get; set; }
        public ushort StartAddress { get; }
        public ushort EndAddress { get; }
        public string? Condition { get; set; }
        public string? FileName => File?.RelativePath;
        public int? LineNumber { get; }
        public BreakpointViewModel(uint checkpointNumber, bool stopWhenHit, bool isEnabled,
            AcmeLine? line, int? lineNumber, AcmeFile? file, AcmeLabel? label, ushort startAddress, ushort endAddress, string? condition)
        {
            CheckpointNumber = checkpointNumber;
            StopWhenHit = stopWhenHit;
            IsEnabled = isEnabled;
            Line = line;
            LineNumber = lineNumber;
            File = file;
            Label = label;
            StartAddress = startAddress;
            EndAddress = endAddress;
            Condition = condition;
        }
    }
}
