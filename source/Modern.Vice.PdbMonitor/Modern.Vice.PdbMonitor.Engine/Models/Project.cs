namespace Modern.Vice.PdbMonitor.Engine.Models
{
    public class Project
    {
        public string? PrgPath { get; set; }
        /// <summary>
        /// When enabled, the application will be started from first available address
        /// </summary>
        public DebugAutoStartMode AutoStartMode { get; set; }
        public string? StopAtLabel { get; set; }
    }
}
