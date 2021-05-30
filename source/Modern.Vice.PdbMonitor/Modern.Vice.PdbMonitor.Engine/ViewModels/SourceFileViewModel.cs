using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class SourceFileViewModel : ScopedViewModel
    {
        readonly BreakpointsViewModel breakpoints;
        readonly AcmeFile file;
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
        public SourceFileViewModel(AcmeFile file, ImmutableArray<LineViewModel> lines, BreakpointsViewModel breakpoints)
        {
            this.breakpoints = breakpoints;
            this.file = file;
            Lines = lines;
            AddOrRemoveBreakpointCommand = new RelayCommandAsync<LineViewModel>(AddOrRemoveBreakpointAsync, l => l?.Address is not null);
            breakpoints.Breakpoints.CollectionChanged += Breakpoints_CollectionChanged;
        }

        void Breakpoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var newBreakpoints = e.NewItems!.Cast<BreakpointViewModel>().ToImmutableArray();
                        foreach (var newBreakpoint in newBreakpoints)
                        {
                            if (newBreakpoint.File == file)
                            {
                                var targetLine = Lines.Single(l => l.SourceLine == newBreakpoint.Line);
                                targetLine.Breakpoint = newBreakpoint;
                            }
                        }
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
                breakpoints.AddBreakpointAsync(file, line!.SourceLine, condition: null);
            }
            else
            {
                breakpoints.RemoveBreakpointAsync(line!.Breakpoint);
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
