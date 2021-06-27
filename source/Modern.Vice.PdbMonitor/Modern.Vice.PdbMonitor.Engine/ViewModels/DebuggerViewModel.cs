using System.ComponentModel;
using System.IO;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class DebuggerViewModel : ScopedViewModel
    {
        readonly ILogger<DebuggerViewModel> logger;
        readonly Globals globals;
        readonly IDispatcher dispatcher;
        readonly IAcmePdbManager acmePdbManager;
        readonly ExecutionStatusViewModel executionStatusViewModel;
        public RegistersViewModel Registers {get;}
        public string? ProjectName => Path.GetFileName(globals.Project?.PrgPath);
        public Project? Project => globals.Project;
        public bool IsOpenProject => Project is not null;
        public ProjectExplorerViewModel ProjectExplorer { get; }
        public SourceFileViewerViewModel SourceFileViewerViewModel { get; }
        public DebuggerViewModel(ILogger<DebuggerViewModel> logger, Globals globals, ProjectExplorerViewModel projectExplorerViewModel,
            SourceFileViewerViewModel sourceFileViewerViewModel, RegistersViewModel registers, IDispatcher dispatcher, IAcmePdbManager acmePdbManager,
            ExecutionStatusViewModel executionStatusViewModel)
        {
            this.logger = logger;
            this.globals = globals;
            this.dispatcher = dispatcher;
            this.acmePdbManager = acmePdbManager;
            this.executionStatusViewModel = executionStatusViewModel;
            executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
            ProjectExplorer = projectExplorerViewModel;
            SourceFileViewerViewModel = sourceFileViewerViewModel;
            Registers = registers;
            Registers.PropertyChanged += Registers_PropertyChanged;
            globals.PropertyChanged += Globals_PropertyChanged;
        }

        void ExecutionStatusViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ExecutionStatusViewModel.IsDebugging):
                    // clears execution rows
                    if (!executionStatusViewModel.IsDebugging)
                    {
                        foreach (var fileViewer in SourceFileViewerViewModel.Files)
                        {
                            fileViewer.ClearExecutionRow();
                        }
                    }
                    break;
            }
        }

        void Registers_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ushort? address = Registers.Current.PC;
            if (address.HasValue)
            {
                var matchingLine = acmePdbManager.FindLineUsingAddress(address.Value);
                if (matchingLine is not null)
                {
                    var file = acmePdbManager.FindFileOfLine(matchingLine)!;
                    int matchingLineNumber = file.Lines.IndexOf(matchingLine);
                    dispatcher.Dispatch(
                        new OpenSourceFileMessage(file, ExecutingLine: matchingLineNumber)
                    );
                    return;
                }
            }
            SourceFileViewerViewModel.ClearExecutionRow();
        }
        
        void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Globals.Project):
                    OnPropertyChanged(nameof(Project));
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                executionStatusViewModel.PropertyChanged -= ExecutionStatusViewModel_PropertyChanged;
                Registers.PropertyChanged -= Registers_PropertyChanged;
                globals.PropertyChanged -= Globals_PropertyChanged;
            }
            base.Dispose(disposing);
        }
    }
}
