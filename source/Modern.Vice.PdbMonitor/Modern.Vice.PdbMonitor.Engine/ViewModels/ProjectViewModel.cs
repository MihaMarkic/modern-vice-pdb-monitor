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
        readonly IProjectPrgFileWatcher projectPrgFileWatcher;
        readonly ExecutionStatusViewModel executionStatusViewModel;
        public Project Project => globals.Project!;
        public string ProjectFile => globals.ProjectFile!;
        public string? PrgPath
        {
            get => Project.PrgPath;
            set => Project.PrgPath = value;
        }
        public bool AutoStart
        {
            get => Project.AutoStart;
            set => Project.AutoStart = value;
        }
        public bool IsStartingDebugging => executionStatusViewModel.IsDebugging;
        public bool IsDebugging => executionStatusViewModel.IsDebugging;
        public bool IsDebuggingPaused => executionStatusViewModel.IsDebuggingPaused;
        public bool IsEditable => !IsStartingDebugging && !IsDebugging;
        public ProjectViewModel(Globals globals, ISettingsManager settingsManager, IDispatcher dispatcher,
            IProjectPrgFileWatcher projectPrgFileWatcher, ExecutionStatusViewModel executionStatusViewModel) : base(dispatcher)
        {
            this.globals = globals;
            this.settingsManager = settingsManager;
            this.projectPrgFileWatcher = projectPrgFileWatcher;
            this.executionStatusViewModel = executionStatusViewModel;
            executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        }

        void ExecutionStatusViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ExecutionStatusViewModel.IsStartingDebugging):
                    OnPropertyChanged(nameof(IsStartingDebugging));
                    break;
                case nameof(ExecutionStatusViewModel.IsDebugging):
                    OnPropertyChanged(nameof(IsDebugging));
                    break;
                case nameof(ExecutionStatusViewModel.IsDebuggingPaused):
                    OnPropertyChanged(nameof(IsDebuggingPaused));
                    break;
            }
        }

        public void AssignPrgFullPath(string value)
        {
            try
            {
                var prgPath = new Uri(value);
                var projectDirectory = new Uri(Path.Combine($"{globals.ProjectDirectory}{Path.DirectorySeparatorChar}"));
                PrgPath = projectDirectory.MakeRelativeUri(prgPath).ToString();
                dispatcher.Dispatch(new PrgFilePathChangedMessage());
                projectPrgFileWatcher.Start(globals.ProjectDirectory!, value);
            }
            catch (Exception ex)
            {
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Assigning PRG (.o)", ex.Message));
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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
            }
            base.Dispose(disposing);
        }
    }
}
