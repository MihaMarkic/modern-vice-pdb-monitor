using System;
using System.IO;
using System.Runtime.CompilerServices;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class ProjectViewModel : OverlayContentViewModel
    {
        readonly Globals globals;
        readonly ISettingsManager settingsManager;
        public Project Project => globals.Project!;
        public string ProjectFile => globals.ProjectFile!;
        public string? PrgPath
        {
            get => Project.PrgPath;
            set => Project.PrgPath = value;
        }
        public ProjectViewModel(Globals globals, ISettingsManager settingsManager, IDispatcher dispatcher) : base(dispatcher)
        {
            this.globals = globals;
            this.settingsManager = settingsManager;
        }
        public void AssignPrgFulPath(string value)
        {
            try
            {
                var projectDirectory = new Uri(Path.Combine($"{globals.ProjectDirectory}{Path.DirectorySeparatorChar}"));
                var prgPath = new Uri(value);
                PrgPath = projectDirectory.MakeRelativeUri(prgPath).ToString();
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Assigning PRG", ex.Message));
            }
        }
        public bool IsPrgPathValid
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PrgPath))
                {
                    string fullPath = Path.Combine(globals.ProjectDirectory!, PrgPath);
                    return File.Exists(fullPath);
                }
                return false;
            }
        }
        protected override void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            switch (name)
            {
                case nameof(PrgPath):
                    try
                    {
                        settingsManager.Save(Project, globals.Settings.RecentProjects[0], createDirectory: false);
                    }
                    catch (Exception ex)
                    {
                        dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Save project", ex.Message));
                    }
                    break;
            }
            base.OnPropertyChanged(name);
        }
    }
}
