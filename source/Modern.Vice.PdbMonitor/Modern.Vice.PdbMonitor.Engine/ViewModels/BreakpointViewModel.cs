using System;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class BreakpointViewModel : NotifiableObject, ICloneable
    {
        public uint CheckpointNumber { get; }
        public bool IsCurrentlyHit { get; set; }
        public bool StopWhenHit { get; set; }
        public bool IsEnabled { get; set; }
        public uint HitCount { get; set; }
        public uint IgnoreCount { get; set; }
        public BreakpointMode Mode { get; set; }
        public AcmeLine? Line { get; set; }
        public AcmeFile? File { get; set; }
        public AcmeLabel? Label { get; set; }
        public ushort StartAddress { get; set; }
        public ushort EndAddress { get; set; }
        public string? Condition { get; set; }
        public string? FileName => File?.RelativePath;
        public int? LineNumber { get; set; }
        public BreakpointViewModel()
        {
            // checkpoint gets assigned upon save, mark it as unset
            CheckpointNumber = uint.MaxValue;
        }
        public BreakpointViewModel(uint checkpointNumber, bool stopWhenHit, bool isEnabled, BreakpointMode mode,
            AcmeLine? line, int? lineNumber, AcmeFile? file, AcmeLabel? label, ushort startAddress, ushort endAddress, string? condition)
        {
            CheckpointNumber = checkpointNumber;
            StopWhenHit = stopWhenHit;
            IsEnabled = isEnabled;
            Mode = mode;
            Line = line;
            LineNumber = lineNumber;
            File = file;
            Label = label;
            StartAddress = startAddress;
            EndAddress = endAddress;
            Condition = condition;
        }
        object ICloneable.Clone() => Clone();
        public BreakpointViewModel Clone()
        {
            return (BreakpointViewModel)MemberwiseClone();
        }
        /// <summary>
        /// Used to compare detail editing changes.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsChangedFrom(BreakpointViewModel other)
        {
            return !(CheckpointNumber == other.CheckpointNumber && StopWhenHit == other.StopWhenHit && IsEnabled == other.IsEnabled
                && Mode == other.Mode && Line == other.Line && File == other.File && Label == other.Label && StartAddress == other.StartAddress
                && EndAddress == other.EndAddress && string.Equals(Condition , other.Condition, StringComparison.Ordinal));
        }
    }
}
