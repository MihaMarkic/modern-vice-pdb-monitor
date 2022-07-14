using System.IO;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Common;
using Modern.Vice.PdbMonitor.Engine.ViewModels;

namespace Modern.Vice.PdbMonitor.Engine.Models;

public class Project : NotifiableObject
{
    public const string StopAtLabelNone = "[None]";
    public string? File { get; set; }
    public string? Directory => Path.GetDirectoryName(File);
    public string? FullPrgPath => IsPrgSet ? Path.Combine(Directory!, PrgPath!) : null;
    public bool IsPrgSet => !string.IsNullOrWhiteSpace(PrgPath);
    public Pdb? DebugSymbols { get; set; }
    public string Caption => $"{Globals.AppName} - {File}";
    public string? PrgPath { get; set; }
    /// <summary>
    /// When enabled, the application will be started from first available address
    /// </summary>
    public DebugAutoStartMode AutoStartMode { get; set; }
    public string StopAtLabel { get; set; } = StopAtLabelNone;
    public CompilerType CompilerType { get; init; }
    public ProjectConfiguration ToConfiguration() => new ProjectConfiguration
    {
        PrgPath = PrgPath,
        AutoStartMode = AutoStartMode,
        StopAtLabel = StopAtLabel == StopAtLabelNone ? null: StopAtLabel,
        CompilerType = CompilerType,
    };
    public static Project FromConfiguration(ProjectConfiguration configuration) => new Project
    {
        PrgPath = configuration.PrgPath,
        AutoStartMode = configuration.AutoStartMode,
        StopAtLabel = configuration.StopAtLabel ?? StopAtLabelNone,
        CompilerType = configuration.CompilerType
    };
}

public record ProjectConfiguration
{
    public string? PrgPath { get; init; }
    public CompilerType CompilerType { get; init;  }
    public DebugAutoStartMode AutoStartMode { get; init; }
    public string? StopAtLabel { get; init; }
}
