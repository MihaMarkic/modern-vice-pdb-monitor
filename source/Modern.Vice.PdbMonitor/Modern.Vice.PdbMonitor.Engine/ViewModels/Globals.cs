using System;
using System.IO;
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
        public string? ProjectFile => Settings.RecentProjects.Count > 0 ? Settings.RecentProjects[0] : null;
        public string? ProjectDirectory => Settings.RecentProjects.Count > 0 ? Path.GetDirectoryName(Settings.RecentProjects[0]) : null;
        public string? FullPrgPath => IsPrgSet ? Path.Combine(ProjectDirectory!, Project!.PrgPath!) : null;
        public bool IsPrgSet => !string.IsNullOrWhiteSpace(Project?.PrgPath);
        public AcmePdb? Pdb { get; set; }
        public Globals(ILogger<Globals> logger, ISettingsManager settingsManager)
        {
            this.logger = logger;
            this.settingsManager = settingsManager;
        }
        /// <summary>
        /// Report file has name with extension .rep
        /// </summary>
        /// <param name="prgPath"></param>
        /// <returns></returns>
        internal string GetReportFileName(string prgPath) => GetRelatedFileName(prgPath,"rep");
        /// <summary>
        /// Labels file has name with extension .lbl
        /// </summary>
        /// <param name="prgPath"></param>
        /// <returns></returns>
        internal string GetLabelsFileName(string prgPath) => GetRelatedFileName(prgPath, "lbl");
        internal string GetRelatedFileName(string prgPath, string extension) => $"{Path.GetFileNameWithoutExtension(prgPath)}.{extension}";
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
