﻿using System;
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
            directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModernVicePdbMonitor");
            settingsPath = Path.Combine(directory, "settings.json");
        }
        public Settings LoadSettings()
        {
            Settings? result;
            try
            {
                result = Load<Settings>(settingsPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to load settings, will fallback to default");
                result = null;
            }
            return result ?? new Settings();
        }
        public T? Load<T>(string path)
            where T: class
        {
            T? result = null;
            if (File.Exists(path))
            {
                try
                {
                    string content = File.ReadAllText(path);
                    result = JsonConvert.DeserializeObject<T>(content);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to load {typeof(T).Name}");
                    throw;
                }
            }
            return result;
        }
        public void Save(Settings settings) => Save(settings, settingsPath, true);
        public void Save<T>(T settings, string path, bool createDirectory)
        {
            var data = JsonConvert.SerializeObject(settings);
            try
            {
                if (createDirectory)
                {
                    string? directory = Path.GetDirectoryName(path);
                    if (directory is not null)
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                File.WriteAllText(path, data);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed saving settings:{ex.Message}");
                throw new Exception($"Failed saving settings:{ex.Message}", ex);
            }
        }
    }
}
