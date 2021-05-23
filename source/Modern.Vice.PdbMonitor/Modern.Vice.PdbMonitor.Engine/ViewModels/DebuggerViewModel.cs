using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Models;
using System.ComponentModel;
using System.Collections.Immutable;
using Righthand.MessageBus;
using Modern.Vice.PdbMonitor.Engine.Messages;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class DebuggerViewModel : ScopedViewModel
    {
        readonly ILogger<DebuggerViewModel> logger;
        readonly Globals globals;
        readonly IDispatcher dispatcher;
        readonly ExecutionStatusViewModel executionStatusViewModel;
        public RegistersViewModel Registers {get;}
        public string? ProjectName => Path.GetFileName(globals.Project?.PrgPath);
        public Project? Project => globals.Project;
        public bool IsOpenProject => Project is not null;
        public ProjectExplorerViewModel ProjectExplorer { get; }
        public SourceFileViewerViewModel SourceFileViewerViewModel { get; }
        public DebuggerViewModel(ILogger<DebuggerViewModel> logger, Globals globals, ProjectExplorerViewModel projectExplorerViewModel,
            SourceFileViewerViewModel sourceFileViewerViewModel, RegistersViewModel registers, IDispatcher dispatcher,
            ExecutionStatusViewModel executionStatusViewModel)
        {
            this.logger = logger;
            this.globals = globals;
            this.dispatcher = dispatcher;
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
            var lines = globals.Pdb?.Lines;
            ushort? address = Registers.Current.PC;
            if (lines.HasValue && address.HasValue)
            {
                var matchingLine = GetAcmeLineMatchingAddress(lines.Value, address.Value);
                if (matchingLine is not null)
                {
                    var file = globals.Pdb!.Files[matchingLine.FileRelativePath];
                    int matchingLineNumber = file.Lines.IndexOf(matchingLine);
                    dispatcher.Dispatch(
                        new OpenSourceFileMessage(file, ExecutingLine: matchingLineNumber));
                }
            }
        }
        AcmeLine? GetAcmeLineMatchingAddress(ImmutableArray<AcmeLine> lines, ushort address)
        {
            AcmeLine? lastAddressLine = null;
            foreach (var line in lines.Take(lines.Length - 1))
            {
                if (line.StartAddress.HasValue)
                {
                    if (lastAddressLine is null)
                    {
                        lastAddressLine = line;
                    }
                    else
                    {
                        if (address >= lastAddressLine.StartAddress!.Value && address < line.StartAddress.Value)
                        {
                            return lastAddressLine;
                        }
                        else
                        {
                            lastAddressLine = line;
                        }
                    }
                }
            }
            return null;
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
