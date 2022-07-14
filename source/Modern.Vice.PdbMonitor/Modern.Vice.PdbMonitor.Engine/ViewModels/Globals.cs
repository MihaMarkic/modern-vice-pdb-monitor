using System;
using System.ComponentModel;
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
    protected override void Dispose(bool disposing)
    {
        if (Project is not null)
        {
            Project.PropertyChanged -= Project_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
