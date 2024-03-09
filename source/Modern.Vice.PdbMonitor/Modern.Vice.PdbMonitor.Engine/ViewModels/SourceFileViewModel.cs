using System.Collections.Frozen;
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

public class SourceFileViewModel : ScopedViewModel, IViewableContent
{
    readonly ILogger<SourceFileViewModel> logger;
    readonly Globals globals;
    readonly BreakpointsViewModel breakpointsViewModel;
    readonly WatchedVariablesViewModel watchedVariablesViewModel;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly PdbFile file;
    readonly IDispatcher dispatcher;
    readonly IViceBridge viceBridge;
    readonly IServiceProvider serviceProvider;
    readonly TaskFactory uiFactory;
    public CallStackViewModel CallStack { get; }
    public event EventHandler? ShowCursorRow;
    public event EventHandler? ShowCursorColumn;
    public event EventHandler<MoveCaretEventArgs>? MoveCaret;
    /// <summary>
    /// Raised when any of lines in <see cref="Lines"/> has a breakpoint added or removed.
    /// </summary>
    public event EventHandler? BreakpointsChanged;
    public event EventHandler? ExecutionRowChanged;
    /// <summary>
    /// Signals need to update the content. Happens when <see cref="ShowAssemblyLines"/> changes.
    /// </summary>
    public event EventHandler? ContentChanged;
    public string Caption => Path.FileName;
    public PdbPath Path => file.Path;
    public ImmutableArray<LineViewModel> Lines { get; }
    public ImmutableArray<EditorLineViewModel> EditorLines { get; private set; }
    public int CursorColumn { get; set; }
    public int CursorRow { get; protected set; }
    public RelayCommandAsync<LineViewModel> AddOrRemoveBreakpointCommand { get; }
    public RelayCommand<PdbFunction> GoToImplementationCommand { get; }
    public RelayCommand<IWithDefinition> GoToDefinitionCommand { get; }
    public RelayCommand<PdbVariable> AddVariableToWatchCommand { get; }
    public RelayCommandAsync<PdbVariable> AddStoreBreakpointCommand { get; }
    public RelayCommandAsync<PdbVariable> AddLoadBreakpointCommand { get; }
    public object? ContextSymbolReference { get; set; }
    public PdbFunction? ContextFunctionReference => ContextSymbolReference as PdbFunction;
    public PdbVariable? ContextVariableReference => ContextSymbolReference as PdbVariable;
    public IWithDefinition? ContextWithDefinitionReference => ContextSymbolReference as IWithDefinition;
    public ImmutableDictionary<int, ImmutableArray<SyntaxElement>> Elements { get; private set; }
    public SourceLanguage? SourceLanguage => globals.Project?.SourceLanguage;
    public bool ShowAssemblyLines { get; set; }
    public bool ShowProfilingData { get; set; }
    public ImmutableDictionary<int, int>? EditorRowToLinesMap { get; private set; }
    public ImmutableArray<int>? LineToEditorRowMap { get; private set; }
    public int? ExecutionRow { get; set; }
    FrozenDictionary<PdbLine, LineViewModel> lineToViewModelMap;
    FrozenDictionary<PdbAssemblyLine, AssemblyLineViewModel> assemblyLineToViewModelMap = FrozenDictionary<PdbAssemblyLine, AssemblyLineViewModel>.Empty;
    PdbLine? executionLine;
    PdbAssemblyLine? executionAssemblyLine;
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
        PdbFile file, ImmutableArray<LineViewModel> lines, 
        BreakpointsViewModel breakpointsViewModel, WatchedVariablesViewModel watchedVariablesViewModel,
        ExecutionStatusViewModel executionStatusViewModel, CallStackViewModel callStackViewModel)
    {
        this.logger = logger;
        this.globals =  globals;
        this.dispatcher = dispatcher;
        this.viceBridge = viceBridge;
        this.serviceProvider = serviceProvider;
        this.breakpointsViewModel = breakpointsViewModel;
        this.watchedVariablesViewModel = watchedVariablesViewModel;
        this.executionStatusViewModel = executionStatusViewModel;
        this.CallStack = callStackViewModel;
        this.file = file;
        Elements = ImmutableDictionary<int, ImmutableArray<SyntaxElement>>.Empty;
        uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        Lines = lines;
        viceBridge.ConnectedChanged += ViceBridge_ConnectedChanged;

        lineToViewModelMap = Lines.ToFrozenDictionary(l => l.SourceLine, l => l);

        AddOrRemoveBreakpointCommand = new RelayCommandAsync<LineViewModel>(AddOrRemoveBreakpointAsync,
           canExecute: l => l?.Address is not null);
        var fileBreakpoints = breakpointsViewModel.Breakpoints
            .Where(b => b.Bind is BreakpointLineBind lineBind && lineBind.File == file)
            .ToImmutableArray();
        AddBreakpointsToLine(fileBreakpoints);
        breakpointsViewModel.Breakpoints.CollectionChanged += Breakpoints_CollectionChanged;
        _ = ParseFileAsync();
        GoToImplementationCommand = new RelayCommand<PdbFunction>(GoToImplementation, f => f is not null);
        GoToDefinitionCommand = new RelayCommand<IWithDefinition>(GoToDefinition, d => d is not null);
        AddVariableToWatchCommand = new RelayCommand<PdbVariable>(AddVariableToWatch, v => v is not null);
        AddStoreBreakpointCommand = new RelayCommandAsync<PdbVariable>(AddStoreBreakpoint, v => v is not null);
        AddLoadBreakpointCommand = new RelayCommandAsync<PdbVariable>(AddLoadBreakpoint, v => v is not null);
        (EditorLines, EditorRowToLinesMap, LineToEditorRowMap) = CreateEditorLines();
    }

    internal (
        ImmutableArray<EditorLineViewModel> EditorLines, 
        ImmutableDictionary<int, int>? EditorRowToLinesMap,
        ImmutableArray<int>? LineToEditorRowMap) 
        CreateEditorLines()
    {
        ImmutableArray<EditorLineViewModel>.Builder builder;
        ImmutableDictionary<int, int>? editorRowToLinesMap;
        ImmutableArray<int>? lineToEditorRowMap;
        if (ShowAssemblyLines)
        {
            int lines = Lines.Sum(l => 1 + l.AssemblyLines.Length);
            builder = ImmutableArray.CreateBuilder<EditorLineViewModel>(lines);
            var editorRowToLinesMapBuilder = ImmutableDictionary.CreateBuilder<int, int>();
            var lineToEditorRowMapBuilder = ImmutableArray.CreateBuilder<int>(Lines.Length);
            for (int i =0; i<Lines.Length; i++)
            {
                var line = Lines[i];
                editorRowToLinesMapBuilder.Add(builder.Count, i);
                lineToEditorRowMapBuilder.Add(builder.Count);
                builder.Add(line);
                builder.AddRange(line.AssemblyLines);
            }
            editorRowToLinesMap = editorRowToLinesMapBuilder.ToImmutable();
            lineToEditorRowMap = lineToEditorRowMapBuilder.ToImmutable();
            assemblyLineToViewModelMap = builder.OfType<AssemblyLineViewModel>()
                .ToFrozenDictionary(al => al.SourceLine, al => al);
        }
        else
        {
            builder = ImmutableArray.CreateBuilder<EditorLineViewModel>(Lines.Length);
            builder.AddRange(Lines);
            editorRowToLinesMap = null;
            lineToEditorRowMap = null;
            assemblyLineToViewModelMap = FrozenDictionary<PdbAssemblyLine, AssemblyLineViewModel>.Empty;
        }
        return (builder.ToImmutable(), editorRowToLinesMap, lineToEditorRowMap);
    }

    public string Text => string.Join(Environment.NewLine, EditorLines.Select(l => l.Content));

    public LineViewModel GetByLineNumber(int lineNumber)
    {
        int mapped = GetLineIndex(lineNumber);
        return Lines[lineNumber];
    }

    public int GetLineIndex(int lineNumber)
    {
        if (ShowAssemblyLines)
        {
            return EditorRowToLinesMap![lineNumber];
        }
        return lineNumber;
    }
    public  int GetEditorRowByLineNumber(int lineNumber)
    {
        if (ShowAssemblyLines)
        {
            return LineToEditorRowMap!.Value[lineNumber];
        }
        return lineNumber;
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
    [SuppressPropertyChangedWarnings]
    void OnContentChanged(EventArgs e) => ContentChanged?.Invoke(this, e);
    /// <summary>
    /// Editor row number
    /// </summary>
    /// <param name="row"></param>
    public void SetCursorRow(int row)
    {
        CursorRow = row;
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
    /// <param name="row">Editor row number</param>
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
                    new OpenSourceLineNumberFileMessage(implementationFile, definition.LineNumber, 
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
                    new OpenSourceLineNumberFileMessage(definitionFile, definition.LineNumber, 
                        Column: definition.ColumnNumber, MoveCaret: true));
            }
            else
            {
                logger.LogError("Couldn't find symbol {VariableName} definition file {DefinitionFile}",
                    source!.Name, definition.Path);
            }
        }
    }

    void AddVariableToWatch(PdbVariable? source)
    {
        if (source is not null)
        {
            watchedVariablesViewModel.AddVariable(source);
        }
    }
    async Task AddStoreBreakpoint(PdbVariable? variable)
    {
        await AddVariableBreakpoint(variable, BreakpointMode.Store);
    }
    async Task AddLoadBreakpoint(PdbVariable? variable)
    {
        await AddVariableBreakpoint(variable, BreakpointMode.Load);
    }
    async Task AddVariableBreakpoint(PdbVariable? variable, BreakpointMode mode)
    {
        if (variable is not null)
        {
            // TODO check if DebugSymbols are static for this view model and simplify if yes
            var globalVariables = globals.Project?.DebugSymbols?.GlobalVariables ?? ImmutableHashSet<PdbVariable>.Empty;
            bool isGlobal = globalVariables.Contains(variable);

            BreakpointBind bind = isGlobal 
                ? new BreakpointGlobalVariableBind(variable.Name):
                new BreakpointNoBind((ushort)variable.Start, (ushort)variable.End);
            var breakpoint = new BreakpointViewModel(
                stopWhenHit: true,
                isEnabled: true,
                mode: mode,
                bind: bind,
                addressRanges: ImmutableHashSet<BreakpointAddressRange>.Empty
                .Add(new BreakpointAddressRange((ushort)variable.Start, (ushort)variable.End)),
                condition: null);
            await breakpointsViewModel.AddBreakpointAsync(breakpoint, CancellationToken.None);
        }
        else
        {
            logger.LogError("Failed to add variable breakpoint for null variable");
        }
    }
    public VariableInfo? GetContextSymbolReferenceInfo(object symbolReference)
    {
        if (symbolReference is PdbVariable variable)
        {
            // TODO check if DebugSymbols are static for this view model and simplify if yes
            var globalVariables = globals.Project?.DebugSymbols?.GlobalVariables ?? ImmutableHashSet<PdbVariable>.Empty;
            bool isGlobal = globalVariables.Contains(variable);
            var slot = watchedVariablesViewModel.GetVariableSlot(variable, isGlobal);
            return new VariableInfo(isGlobal, variable.Name, slot);
        }
        return null;
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
    public void ClearExecutionRow()
    {
        foreach (var line in EditorLines)
        {
            line.IsExecution = false;
        }
        ExecutionRow = null;
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
    public void SetExecutionRow(PdbLine line, PdbAssemblyLine? assemblyLine)
    {
        executionLine = line;
        executionAssemblyLine = assemblyLine;
        UpdateExecutionLineVisibility();
    }

    bool IsLive => executionStatusViewModel.IsDebugging && executionStatusViewModel.IsDebuggingPaused;

    internal void UpdateExecutionLineVisibility()
    {
        ClearExecutionRow();
        if (ShowAssemblyLines)
        {
            if (executionAssemblyLine is not null)
            {
                var viewModel = assemblyLineToViewModelMap[executionAssemblyLine];
                viewModel.IsExecution = true;
                if (IsLive)
                {
                    ExecutionRow = EditorLines.IndexOf(viewModel);
                }
            }
        }
        else
        {
            if (executionLine is not null)
            {
                var viewModel = lineToViewModelMap[executionLine];
                viewModel.IsExecution = true;
                if (IsLive)
                {
                    ExecutionRow = EditorLines.IndexOf(viewModel);
                }
            }
        }
        if (IsLive)
        {
            OnExecutionRowChanged(EventArgs.Empty);
        }
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
            case nameof(ShowAssemblyLines):
                (EditorLines, EditorRowToLinesMap, LineToEditorRowMap) = CreateEditorLines();
                OnContentChanged(EventArgs.Empty);
                UpdateExecutionLineVisibility();
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
public record VariableInfo(bool IsGlobal, string VariableName, VariableSlot? Slot)
{
    public bool HasValue => Slot is not null;
}

public abstract class EditorLineViewModel: NotifiableObject
{
    public string Content { get; }
    public ushort? Address { get; }
    public bool IsExecution { get; set; }
    public EditorLineViewModel(string content, ushort? address)
    {
        Content = content;
        Address = address;
    }
}

public class AssemblyLineViewModel : EditorLineViewModel
{
    public PdbAssemblyLine SourceLine { get; }
    public AssemblyLineViewModel(PdbAssemblyLine sourceLine, string spacePrefix)
        : base($"{spacePrefix}{sourceLine.Text}", sourceLine.Address)
    {
        SourceLine = sourceLine;
    }
}
public class LineViewModel : EditorLineViewModel
{
    readonly Lazy<ImmutableArray<AssemblyLineViewModel>> assemblyLines;
    readonly string spacePrefix;
    public PdbLine SourceLine { get; }
    public ImmutableArray<BreakpointViewModel> Breakpoints { get; private set; }
    public bool HasBreakpoint => !Breakpoints.IsEmpty;
    public int Row { get; }
    
    public LineSymbolReferences SymbolReferences { get; }
    public LineViewModel(PdbLine sourceLine, int row, LineSymbolReferences symbolReferences) 
        : base(sourceLine.Text, sourceLine.Addresses.FirstOrDefault()?.StartAddress)
    {
        Breakpoints = ImmutableArray<BreakpointViewModel>.Empty;
        SourceLine = sourceLine;
        Row = row;
        SymbolReferences = symbolReferences;
        assemblyLines = new Lazy<ImmutableArray<AssemblyLineViewModel>>(InitAssemblyLines);
        spacePrefix = CreateSpacePrefix(Content);
    }
    internal static string CreateSpacePrefix(string content)
    {
        if (content.Length == 0)
        {
            return "";
        }
        int index = 0;
        while (index < content.Length && (content[index] == ' ' || content[index] == '\t'))
        {
            index++;
        }
        return content.Substring(0, index);
    }
    public ImmutableArray<AssemblyLineViewModel> AssemblyLines => assemblyLines.Value;
    ImmutableArray<AssemblyLineViewModel> InitAssemblyLines()
    {
        return SourceLine.AssemblyLines
            .Select(l => new AssemblyLineViewModel(l, spacePrefix))
            .ToImmutableArray();
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
