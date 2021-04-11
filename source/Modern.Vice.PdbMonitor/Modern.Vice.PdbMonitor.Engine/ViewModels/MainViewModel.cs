﻿using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class MainViewModel: NotifiableObject
    {
        readonly ILogger<MainViewModel> logger;
        readonly IAcmePdbParser acmePdbParser;
        readonly Globals globals;
        public Project? Project { get; private set; }
        public SettingsViewModel SettingsViewModel { get; }
        public RelayCommand ShowSettingsCommand { get; }
        public RelayCommand TestCommand { get; }
        public bool IsShowingSettings { get; private set; }
        public MainViewModel(ILogger<MainViewModel> logger, IAcmePdbParser acmePdbParser, SettingsViewModel settingsViewModel, Globals globals)
        {
            this.logger = logger;
            this.acmePdbParser = acmePdbParser;
            SettingsViewModel = settingsViewModel;
            this.globals = globals;
            ShowSettingsCommand = new RelayCommand(() => IsShowingSettings = !IsShowingSettings);
            TestCommand = new RelayCommand(Test);
        }

        void Test()
        {
            if (!string.IsNullOrWhiteSpace(globals.Settings.VicePath))
            {
                string path = Path.Combine(globals.Settings.VicePath, "bin", "x64dtv.exe");
                var proc = Process.Start(path, "-binarymonitor");
            }
        }

        public void CreateProject(string prgPath)
        {
            if (File.Exists(prgPath))
            {
                Project = new Project(prgPath);
            }
        }
    }
}
