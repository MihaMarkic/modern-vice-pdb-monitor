using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Models.Configuration;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class Globals : NotifiableObject
{
    public const string AppName = "Modern VICE PDB Monitor";
    public const string ProjectDirectory = nameof(ProjectDirectory);
    public const string ProjectDebugSymbols = nameof(ProjectDebugSymbols);

    readonly ILogger<Globals> logger;
    readonly ISettingsManager settingsManager;
    Project? project;
    public Settings Settings { get; set; } = default!;
    public string? ProjectCaption => Project?.Caption ?? Globals.AppName;
    public Globals(ILogger<Globals> logger, ISettingsManager settingsManager)
    {
        this.logger = logger;
        this.settingsManager = settingsManager;
    }
    public Project? Project 
    { 
        get => project;
        set
        {
            if (Project != value)
            {
                if (project is not null)
                {
                    project.PropertyChanged -= Project_PropertyChanged;
                }
                project = value;
                if (project is not null)
                {
                    project.PropertyChanged += Project_PropertyChanged;
                }
            }
        }
    }

    void Project_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Project.Directory):
                OnPropertyChanged(ProjectDirectory);
                break;
            case nameof(Project.DebugSymbols):
                OnPropertyChanged(ProjectDebugSymbols);
                break;
        }
    }

    protected override void OnPropertyChanged([CallerMemberName] string name = null!)
    {
        base.OnPropertyChanged(name);
        switch (name)
        {
            case nameof(Project):
                OnPropertyChanged(nameof(ProjectCaption));
                break;
        }
    }

    public void Load()
    {
        Settings = settingsManager.LoadSettings();
        logger.LogDebug("Loaded settings");
    }
    public void Save()
    {
        try
        {
            settingsManager.Save(Settings);
            logger.LogDebug("Saved settings");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed saving settings");
        }
    }
    public void SaveBreakpoints(IList<BreakpointViewModel> breakpoints)
    {
        if (Project?.BreakpointsSettingsPath is not null)
        {
            var items = breakpoints.Select(b =>
                new BreakpointInfo(b.StopWhenHit, b.IsEnabled, b.Mode, b.StartAddress, b.EndAddress, b.Condition,
                    b.FileName, b.LineNumber, b.Line?.Text, b.Label)
                ).ToImmutableArray();
            var settings = new BreakpointsSettings(items);
            try
            {
                settingsManager.Save(settings, Project.BreakpointsSettingsPath);
                logger.LogDebug("Saved breakpoints settings");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed saving breakpoints settings");
            }
        }
    }
    public BreakpointsSettings LoadBreakpoints()
    {
        if (Project?.BreakpointsSettingsPath is not null)
        {
            return settingsManager.LoadBreakpointsSettings(Project.BreakpointsSettingsPath);
        }
        return BreakpointsSettings.Empty;
    }
    protected override void Dispose(bool disposing)
    {
        if (Project is not null)
        {
            Project.PropertyChanged -= Project_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
