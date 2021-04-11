using System;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Engine.Models.Configuration;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class Globals : NotifiableObject
    {
        readonly ILogger<Globals> logger;
        readonly ISettingsManager settingsManager;
        public Settings Settings { get; set; } = default!;
        public Globals(ILogger<Globals> logger, ISettingsManager settingsManager)
        {
            this.logger = logger;
            this.settingsManager = settingsManager;
        }
        public void Load()
        {
            Settings = settingsManager.Load();
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
