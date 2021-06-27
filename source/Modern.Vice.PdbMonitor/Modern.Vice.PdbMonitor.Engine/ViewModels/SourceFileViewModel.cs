using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class SourceFileViewModel : ScopedViewModel
    {
        readonly BreakpointsViewModel breakpoints;
        readonly AcmeFile file;
        readonly IViceBridge viceBridge;
        readonly TaskFactory uiFactory;
        public string Path => file.RelativePath;
        public ImmutableArray<LineViewModel> Lines { get; }
        public int CursorColumn { get; set; }
        public int CursorRow { get; set; }
        public int? ExecutionRow { get; set; }
        public RelayCommandAsync<LineViewModel> AddOrRemoveBreakpointCommand { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lines"></param>
        /// <remarks>
        /// Constructor arguments are passed by <see cref="ServiceProviderExtension.CreateSourceFileViewModel"/>.
        /// It is mandatory that they are in sync.
        /// </remarks>
        public SourceFileViewModel(IViceBridge viceBridge, AcmeFile file, ImmutableArray<LineViewModel> lines, BreakpointsViewModel breakpoints)
        {
            this.viceBridge = viceBridge;
            this.breakpoints = breakpoints;
            this.file = file;
            uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
            Lines = lines;
            viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;
            AddOrRemoveBreakpointCommand = new RelayCommandAsync<LineViewModel>(AddOrRemoveBreakpointAsync,
               canExecute: l => l?.Address is not null && viceBridge.IsConnected);
            var fileBreakpoints = breakpoints.Breakpoints.Where(b => b.File == file).ToImmutableArray();
            AddBreakpointsToLine(fileBreakpoints);
            breakpoints.Breakpoints.CollectionChanged += Breakpoints_CollectionChanged;
        }

        void ViceBridge_ConnectedChanged(object? sender, ConnectedChangedEventArgs e)
        {
            uiFactory.StartNew(() => AddOrRemoveBreakpointCommand.RaiseCanExecuteChanged());
        }

        void Breakpoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var newBreakpoints = e.NewItems!.Cast<BreakpointViewModel>().ToImmutableArray();
                        AddBreakpointsToLine(newBreakpoints);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        var newBreakpoints = e.OldItems!.Cast<BreakpointViewModel>().ToImmutableArray();
                        foreach (var newBreakpoint in newBreakpoints)
                        {
                            if (newBreakpoint.File == file)
                            {
                                var targetLine = Lines.Single(l => l.SourceLine == newBreakpoint.Line);
                                targetLine.Breakpoint = null;
                            }
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var line in Lines)
                    {
                        line.Breakpoint = null;
                    }
                    break;
            }
        }

        void AddBreakpointsToLine(ImmutableArray<BreakpointViewModel> newBreakpoints)
        {
            foreach (var newBreakpoint in newBreakpoints)
            {
                if (newBreakpoint.File == file)
                {
                    var targetLine = Lines.Single(l => l.SourceLine == newBreakpoint.Line);
                    targetLine.Breakpoint = newBreakpoint;
                }
            }
        }

        public void ClearExecutionRow()
        {
            foreach (var line in Lines)
            {
                line.IsExecution = false;
            }
        }
        internal async Task AddOrRemoveBreakpointAsync(LineViewModel? line)
        {
            if (line!.Breakpoint is null)
            {
                int lineNumber = Lines.IndexOf(line);
                await breakpoints.AddBreakpointAsync(file, line!.SourceLine, lineNumber, label: null, condition: null);
            }
            else
            {
                await breakpoints.RemoveBreakpointAsync(line!.Breakpoint, forceRemove: false);
            }
        }
        public void SetExecutionRow(int rowIndex)
        {
            Lines[rowIndex].IsExecution = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                breakpoints.Breakpoints.CollectionChanged -= Breakpoints_CollectionChanged;
                viceBridge.ConnectedChanged -= ViceBridge_ConnectedChanged;
            }
            base.Dispose(disposing);
        }
    }

    public class LineViewModel : NotifiableObject
    {
        public AcmeLine SourceLine { get; }
        public bool IsExecution { get; set; }
        public BreakpointViewModel? Breakpoint { get; set; }
        public bool HasBreakpoint => Breakpoint is not null;
        public int Row { get; }
        public ushort? Address { get; }
        public string Content { get; }
        public LineViewModel(AcmeLine sourceLine, int row, ushort? address, string content)
        {
            SourceLine = sourceLine;
            Row = row;
            Address = address;
            Content = content;
        }
    }
}
