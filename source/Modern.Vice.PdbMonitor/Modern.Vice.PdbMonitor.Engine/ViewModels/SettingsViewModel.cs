using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models.Configuration;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public sealed class SettingsViewModel: OverlayContentViewModel
{
    readonly ILogger<SettingsViewModel> logger;
    readonly Globals globals;
    public Settings Settings => globals.Settings;
    public bool IsVicePathGood { get; private set; }
    public RelayCommand VerifyValuesCommand { get; }
    public SettingsViewModel(ILogger<SettingsViewModel> logger, Globals globals, IDispatcher dispatcher): base(dispatcher)
    {
        this.logger = logger;
        this.globals = globals;
        globals.Settings.PropertyChanged += Settings_PropertyChanged;
        VerifyValues();
        VerifyValuesCommand = new RelayCommand(VerifyValues);
    }

    void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch(e.PropertyName)
        {
            case nameof(Settings.VicePath):
                VerifyVicePath();
                break;
        }
    }
    public void VerifyValues()
    {
        VerifyVicePath();
    }
    public void VerifyVicePath()
    {
        if (string.IsNullOrWhiteSpace(Settings.VicePath))
        {
            IsVicePathGood = false;
            return;
        }
        string path = Path.Combine(Settings.VicePath, "bin"); 
        if (!Directory.Exists(path))
        {
            IsVicePathGood = false;
            return;
        }
        try
        {
            IsVicePathGood = Directory.GetFiles(path, "x64dtv.exe").Any();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed checking VICE directory validity");
            IsVicePathGood = false;
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            globals.Settings.PropertyChanged -= Settings_PropertyChanged;
        }
        base.Dispose(disposing);
    }
}
