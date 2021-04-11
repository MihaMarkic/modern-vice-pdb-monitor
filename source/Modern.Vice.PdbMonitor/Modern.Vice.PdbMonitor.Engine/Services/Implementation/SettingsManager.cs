using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Models.Configuration;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Newtonsoft.Json;

namespace Modern.Vice.PdbMonitor.Engine.Services.Implementation
{
    public class SettingsManager : ISettingsManager
    {
        readonly ILogger<SettingsManager> logger;
        readonly string directory;
        readonly string settingsPath;
        public SettingsManager(ILogger<SettingsManager> logger)
        {
            this.logger = logger;
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ModernVicePdbMonitor");
            settingsPath = Path.Combine(directory, "settings.json");
        }
        public Settings Load()
        {
            Settings? result = null;
            if (File.Exists(settingsPath))
            {
                try
                {
                    string content = File.ReadAllText(settingsPath);
                    result = JsonConvert.DeserializeObject<Settings>(content);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to load settings, will fallback to default");
                }
            }
            return result ?? new Settings();
        }
        public void Save(Settings settings)
        {
            var data = JsonConvert.SerializeObject(settings);
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(settingsPath, data);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed saving settings:{ex.Message}");
                throw new Exception($"Failed saving settings:{ex.Message}", ex);
            }
        }
    }
}
