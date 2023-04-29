using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using PropertyChanged;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class SourceFileViewModel : ScopedViewModel
{
    readonly Globals globals;
    readonly BreakpointsViewModel breakpointsViewModel;
    readonly PdbFile file;
    readonly IViceBridge viceBridge;
    readonly IServiceProvider serviceProvider;
    readonly TaskFactory uiFactory;
    public event EventHandler? ShowCursorRow;
    /// <summary>
    /// Raised when any of lines in <see cref="Lines"/> has a breakpoint added or removed.
    /// </summary>
    public event EventHandler? BreakpointsChanged;
    public event EventHandler? ExecutionRowChanged;
    public PdbPath Path => file.Path;
    public ImmutableArray<LineViewModel> Lines { get; }
    public int CursorColumn { get; set; }
    public int CursorRow { get; protected set; }
    public RelayCommandAsync<LineViewModel> AddOrRemoveBreakpointCommand { get; }
    public ImmutableDictionary<int, ImmutableArray<SyntaxElement>> Elements { get; private set; }
    public SourceLanguage? SourceLanguage => globals.Project?.SourceLanguage;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <param name="lines"></param>
    /// <remarks>
    /// Constructor arguments are passed by <see cref="ServiceProviderExtension.CreateSourceFileViewModel"/>.
    /// It is mandatory that they are in sync.
    /// </remarks>
    public SourceFileViewModel(Globals globals, IViceBridge viceBridge, IServiceProvider serviceProvider,
        PdbFile file, ImmutableArray<LineViewModel> lines, BreakpointsViewModel breakpoints)
    {
        this.globals =  globals;
        this.viceBridge = viceBridge;
        this.serviceProvider = serviceProvider;
        this.breakpointsViewModel = breakpoints;
        this.file = file;
        Elements = ImmutableDictionary<int, ImmutableArray<SyntaxElement>>.Empty;
        uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        Lines = lines;
        viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;
        AddOrRemoveBreakpointCommand = new RelayCommandAsync<LineViewModel>(AddOrRemoveBreakpointAsync,
           canExecute: l => l?.Address is not null && viceBridge.IsConnected);
        var fileBreakpoints = breakpoints.Breakpoints.Where(b => b.File == file).ToImmutableArray();
        AddBreakpointsToLine(fileBreakpoints);
        breakpoints.Breakpoints.CollectionChanged += Breakpoints_CollectionChanged;
        _ = ParseFileAsync();
    }
    async Task ParseFileAsync()
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var projectFactory = scope.ServiceProvider.GetRequiredService<IProjectFactory>();
            var compiler = projectFactory.GetCompiler(globals.Project!.CompilerType);
            Elements = await Task.Run(() => compiler.GetSyntaxElements(file.Content));
        }
    }
    [SuppressPropertyChangedWarnings]
    void OnShowCursorRow(EventArgs e) => ShowCursorRow?.Invoke(this, e);
    [SuppressPropertyChangedWarnings]
    void OnExecutionRowChanged(EventArgs e) => ExecutionRowChanged?.Invoke(this, e);
    [SuppressPropertyChangedWarnings]
    void OnBreakpointsChanged(EventArgs e) => BreakpointsChanged?.Invoke(this, e);
    public void SetCursorRow(int value)
    {
        CursorRow = value;
        OnShowCursorRow(EventArgs.Empty);
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
                    OnBreakpointsChanged(EventArgs.Empty);
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
                    OnBreakpointsChanged(EventArgs.Empty);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                foreach (var line in Lines)
                {
                    line.Breakpoint = null;
                }
                OnBreakpointsChanged(EventArgs.Empty);
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
    public int? ExecutionRow
    {
        get
        {
            for (int i=0; i<Lines.Length; i++)
            {
                if (Lines[i].IsExecution)
                {
                    return i;
                }
            }
            return null;
        }
    }
    public void ClearExecutionRow()
    {
        foreach (var line in Lines)
        {
            line.IsExecution = false;
        }
        OnExecutionRowChanged(EventArgs.Empty);
    }
    internal async Task AddOrRemoveBreakpointAsync(LineViewModel? line)
    {
        if (line!.Breakpoint is null)
        {
            int lineNumber = Lines.IndexOf(line);
            await breakpointsViewModel.AddBreakpointAsync(file, line!.SourceLine, lineNumber, label: null, condition: null);
        }
        else
        {
            await breakpointsViewModel.RemoveBreakpointAsync(line!.Breakpoint, forceRemove: false);
        }
    }
    public void SetExecutionRow(int rowIndex)
    {
        Lines[rowIndex].IsExecution = true;
        OnExecutionRowChanged(EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            breakpointsViewModel.Breakpoints.CollectionChanged -= Breakpoints_CollectionChanged;
            viceBridge.ConnectedChanged -= ViceBridge_ConnectedChanged;
        }
        base.Dispose(disposing);
    }
}

public class LineViewModel : NotifiableObject
{
    public PdbLine SourceLine { get; }
    public bool IsExecution { get; set; }
    public BreakpointViewModel? Breakpoint { get; set; }
    public bool HasBreakpoint => Breakpoint is not null;
    public int Row { get; }
    public ushort? Address { get; }
    public string Content { get; }
    public LineViewModel(PdbLine sourceLine, int row, ushort? address, string content)
    {
        SourceLine = sourceLine;
        Row = row;
        Address = address;
        Content = content;
    }
}
