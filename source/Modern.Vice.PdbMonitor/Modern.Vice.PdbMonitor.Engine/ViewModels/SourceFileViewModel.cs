using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;
using PropertyChanged;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class SourceFileViewModel : ScopedViewModel
{
    readonly ILogger<SourceFileViewModel> logger;
    readonly Globals globals;
    readonly BreakpointsViewModel breakpointsViewModel;
    readonly PdbFile file;
    readonly IDispatcher dispatcher;
    readonly IViceBridge viceBridge;
    readonly IServiceProvider serviceProvider;
    readonly TaskFactory uiFactory;
    public event EventHandler? ShowCursorRow;
    public event EventHandler? ShowCursorColumn;
    public event EventHandler<MoveCaretEventArgs>? MoveCaret;
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
    public RelayCommand<PdbFunction> GoToImplementationCommand { get; }
    public RelayCommand<IWithDefinition> GoToDefinitionCommand { get; }
    public object? ContextSymbolReference { get; set; }
    public PdbFunction? ContextFunctionReference => ContextSymbolReference as PdbFunction;
    public PdbVariable? ContextVariableReference => ContextSymbolReference as PdbVariable;
    public IWithDefinition? ContextWithDefinitionReference => ContextSymbolReference as IWithDefinition;
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
    public SourceFileViewModel(ILogger<SourceFileViewModel> logger,
        Globals globals, IViceBridge viceBridge, IDispatcher dispatcher, IServiceProvider serviceProvider,
        PdbFile file, ImmutableArray<LineViewModel> lines, BreakpointsViewModel breakpoints)
    {
        this.logger = logger;
        this.globals =  globals;
        this.dispatcher = dispatcher;
        this.viceBridge = viceBridge;
        this.serviceProvider = serviceProvider;
        this.breakpointsViewModel = breakpoints;
        this.file = file;
        Elements = ImmutableDictionary<int, ImmutableArray<SyntaxElement>>.Empty;
        uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        Lines = lines;
        viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;
        AddOrRemoveBreakpointCommand = new RelayCommandAsync<LineViewModel>(AddOrRemoveBreakpointAsync,
           canExecute: l => l?.Address is not null);
        var fileBreakpoints = breakpoints.Breakpoints
            .Where(b => b.Bind is BreakpointLineBind lineBind && lineBind.File == file)
            .ToImmutableArray();
        AddBreakpointsToLine(fileBreakpoints);
        breakpoints.Breakpoints.CollectionChanged += Breakpoints_CollectionChanged;
        _ = ParseFileAsync();
        GoToImplementationCommand = new RelayCommand<PdbFunction>(GoToImplementation, f => f is not null);
        GoToDefinitionCommand = new RelayCommand<IWithDefinition>(GoToDefinition, d => d is not null);
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
    void OnShowCursorColumn(EventArgs e) => ShowCursorColumn?.Invoke(this, e);
    [SuppressPropertyChangedWarnings]
    void OnMoveCaret(MoveCaretEventArgs e) => MoveCaret?.Invoke(this, e);
    [SuppressPropertyChangedWarnings]
    void OnExecutionRowChanged(EventArgs e) => ExecutionRowChanged?.Invoke(this, e);
    [SuppressPropertyChangedWarnings]
    void OnBreakpointsChanged(EventArgs e) => BreakpointsChanged?.Invoke(this, e);
    public void SetCursorRow(int value)
    {
        CursorRow = value;
        OnShowCursorRow(EventArgs.Empty);
    }
    public void SetCursorColumn(int value)
    {
        CursorColumn = value;
        OnShowCursorColumn(EventArgs.Empty);
    }
    /// <summary>
    /// Request client to position caret.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="column"></param>
    public void SetMoveCaret(int row, int column)
    {
        OnMoveCaret(new MoveCaretEventArgs(row, column));
    }
    void ViceBridge_ConnectedChanged(object? sender, ConnectedChangedEventArgs e)
    {
        uiFactory.StartNew(() => AddOrRemoveBreakpointCommand.RaiseCanExecuteChanged());
    }
    private void GoToImplementation(PdbFunction? function)
    {
        var definition = function?.Definition;
        if (definition is not null)
        {
            var implementationFile = globals.Project?.DebugSymbols?.Files[function!.DefinitionFile];
            if (implementationFile is not null)
            {
                dispatcher.Dispatch(
                    new OpenSourceFileMessage(implementationFile, definition.LineNumber, 
                        Column: definition.ColumnNumber, MoveCaret: true));
            }
            else
            {
                logger.LogError("Couldn't find function {FunctionName} definition file {DefinitionFile}",
                    function!.Name, function.DefinitionFile);
            }
        }
    }
    void GoToDefinition(IWithDefinition? source)
    {
        var definition = source?.Definition;
        if (definition is not null)
        {
            var definitionFile = globals.Project?.DebugSymbols?.Files[definition.Path];
            if (definitionFile is not null)
            {
                dispatcher.Dispatch(
                    new OpenSourceFileMessage(definitionFile, definition.LineNumber, 
                        Column: definition.ColumnNumber, MoveCaret: true));
            }
            else
            {
                logger.LogError("Couldn't find symbol {VariableName} definition file {DefinitionFile}",
                    source!.Name, definition.Path);
            }
        }
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
                        if (newBreakpoint.Bind is BreakpointLineBind lineBind && lineBind.File == file)
                        {
                            var targetLine = Lines.Single(l => SourceLinesMatch(l.SourceLine, lineBind.Line));
                            targetLine.RemoveBreakpoint(newBreakpoint);
                        }
                    }
                    OnBreakpointsChanged(EventArgs.Empty);
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                foreach (var line in Lines)
                {
                    line.ClearBreakpoints();
                }
                OnBreakpointsChanged(EventArgs.Empty);
                break;
        }
    }
    void AddBreakpointsToLine(ImmutableArray<BreakpointViewModel> newBreakpoints)
    {
        foreach (var newBreakpoint in newBreakpoints)
        {
            if (newBreakpoint.Bind is BreakpointLineBind lineBind && lineBind.File == file)
            {
                var targetLine = Lines.Single(l => SourceLinesMatch(l.SourceLine, lineBind.Line));
                targetLine.AddBreakpoint(newBreakpoint);
            }
        }
    }
    /// <summary>
    /// Simplifies lines matching to avoid unnecessary comparisons
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    internal bool SourceLinesMatch(PdbLine a, PdbLine? b)
    {
        if (b is null)
        {
            return false;
        }
        return a.LineNumber == b.LineNumber
            && a.Function?.Name == b.Function?.Name
            && a.Function?.DefinitionFile == b.Function?.DefinitionFile;
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
        if (line!.Breakpoints.IsEmpty)
        {
            int lineNumber = Lines.IndexOf(line);
            await breakpointsViewModel.AddLineBreakpointAsync(file, line!.SourceLine, lineNumber, condition: null);
        }
        else
        {
            foreach (var breakpoint in line!.Breakpoints)
            {
                await breakpointsViewModel.RemoveBreakpointAsync(breakpoint, forceRemove: false);
            }
        }
    }
    public void SetExecutionRow(int rowIndex)
    {
        Lines[rowIndex].IsExecution = true;
        OnExecutionRowChanged(EventArgs.Empty);
    }

    protected override void OnPropertyChanged([CallerMemberName] string name = default!)
    {
        base.OnPropertyChanged(name);
        switch (name)
        {
            case nameof(ContextSymbolReference):
                GoToImplementationCommand.RaiseCanExecuteChanged();
                GoToDefinitionCommand.RaiseCanExecuteChanged();
                break;
        }
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
    public ImmutableArray<BreakpointViewModel> Breakpoints { get; private set; }
    public bool HasBreakpoint => !Breakpoints.IsEmpty;
    public int Row { get; }
    public ushort? Address { get; }
    public string Content { get; }
    public LineSymbolReferences SymbolReferences { get; }
    public LineViewModel(PdbLine sourceLine, int row, string content, LineSymbolReferences symbolReferences)
    {
        Breakpoints = ImmutableArray<BreakpointViewModel>.Empty;
        SourceLine = sourceLine;
        Row = row;
        Content = content;
        Address = sourceLine.Addresses.FirstOrDefault()?.StartAddress;
        SymbolReferences = symbolReferences;
    }
    public void ClearBreakpoints()
    {
        Breakpoints = ImmutableArray<BreakpointViewModel>.Empty;
    }
    public void AddBreakpoint(BreakpointViewModel breakpoint)
    {
        Breakpoints = Breakpoints.Add(breakpoint);
    }
    public void RemoveBreakpoint(BreakpointViewModel breakpoint)
    {
        Breakpoints = Breakpoints.Remove(breakpoint);
    }
}

public sealed class MoveCaretEventArgs: EventArgs
{
    public int Line { get; }
    public int Column { get; }
    public MoveCaretEventArgs(int row, int column)
    {
        Line = row;
        Column = column;
    }
}
