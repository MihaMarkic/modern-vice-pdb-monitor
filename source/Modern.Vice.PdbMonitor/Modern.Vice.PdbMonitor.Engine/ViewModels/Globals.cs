using System;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Models.Configuration;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class Globals : NotifiableObject
    {
        readonly ILogger<Globals> logger;
        readonly ISettingsManager settingsManager;
        public Settings Settings { get; set; } = default!;
        public Project? Project { get; set; }
        public string? ProjectDirectory { get; set; }
        public AcmePdb? Pdb { get; set; }
        public Globals(ILogger<Globals> logger, ISettingsManager settingsManager)
        {
            this.logger = logger;
            this.settingsManager = settingsManager;
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
    }
}
