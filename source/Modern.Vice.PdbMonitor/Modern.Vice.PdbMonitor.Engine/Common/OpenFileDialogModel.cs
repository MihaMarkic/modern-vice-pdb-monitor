namespace Modern.Vice.PdbMonitor.Engine.Common;

public readonly record struct OpenFileDialogModel(string? InitialDirectory, string Name, string Extension);
public readonly record struct DebugFileOpenDialogModel(string? InitialDirectory, string Title, string Name, string Extension);
