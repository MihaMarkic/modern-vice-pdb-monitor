using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Righthand.MessageBus;

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
            var lines = globals.Pdb?.LinesWithAddress;
            ushort? address = Registers.Current.PC;
            if (lines.HasValue && address.HasValue)
            {
                var matchingLine = BinarySearch(lines.Value, address.Value);
                if (matchingLine is not null)
                {
                    var file = globals.Pdb!.Files[matchingLine.FileRelativePath];
                    int matchingLineNumber = file.Lines.IndexOf(matchingLine);
                    dispatcher.Dispatch(
                        new OpenSourceFileMessage(file, ExecutingLine: matchingLineNumber)
                    );
                    return;
                }
            }
            SourceFileViewerViewModel.ClearExecutionRow();
        }
        /// <summary>
        /// Applies binary search for line with given <paramref name="address"/>.
        /// </summary>
        /// <param name="lines">Code lines containing data.</param>
        /// <param name="address">Address to search for.</param>
        /// <returns></returns>
        internal AcmeLine? BinarySearch(ImmutableArray<AcmeLine> lines, ushort address)
        {
            int from = 0;
            int to = lines.Length - 1;
            AcmeLine? line = null;
            if (lines.Length == 0)
            {
                return null;
            }
            // if there is no next line, then address has to fall between StartAddress and bytes length even though it might not be correct,
            // or before the StartAddress of the next line
            while (from < to)
            {
                int middle = (from + to) / 2;
                line = lines[middle];

                // address has to be in an earlier line
                if (address < line.StartAddress)
                {
                    to = Math.Max(middle - 1, from);
                }
                
                else if (line.IsAddressWithinLine(address))
                {
                    return line;
                }
                else
                {
                    from = Math.Min(middle + 1, to);
                }
            }
            line = lines[from];
            if (line.IsAddressWithinLine(address))
            {
                return line;
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
