using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class SourceFileViewerViewModel : NotifiableObject
{
    readonly ILogger<SourceFileViewerViewModel> logger;
    readonly ISubscription openSourceLineNumberFileSubscription;
    readonly ISubscription openSourceLineFileSubscription;
    readonly ISubscription openAddressSubscription;
    readonly Globals globals;
    readonly IServiceProvider serviceProvider;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly ISubscription debugDataChangedSubscription;
    public ObservableCollection<IViewableContent> Files { get; }
    public IViewableContent? Selected { get; set; }
    public RelayCommand<IViewableContent> CloseSourceFileCommand { get; }
    public SourceFileViewerViewModel(IDispatcher dispatcher, ILogger<SourceFileViewerViewModel> logger, Globals globals, IServiceProvider serviceProvider,
        ExecutionStatusViewModel executionStatusViewModel)
    {
        this.logger = logger;
        this.globals = globals;
        this.executionStatusViewModel = executionStatusViewModel;
        this.serviceProvider = serviceProvider;
        openSourceLineNumberFileSubscription = dispatcher.Subscribe<OpenSourceLineNumberFileMessage>(OpenSourceLineNumberFile);
        openSourceLineFileSubscription = dispatcher.Subscribe<OpenSourceLineFileMessage>(OpenSourceLineFile);
        openAddressSubscription = dispatcher.Subscribe<OpenAddressMessage>(OpenAddressDisassembly);
        CloseSourceFileCommand = new(CloseSourceFile);
        Files = new();
        globals.PropertyChanged += Globals_PropertyChanged;
        executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
        debugDataChangedSubscription = dispatcher.Subscribe<DebugDataChangedMessage>(SubscriptionDebugDataChanged);
    }
    protected override void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        base.OnPropertyChanged(name!);
    }
    void Globals_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Globals.Project):
                // closes all files when project changes or is closed
                while (Files.Count > 0)
                {
                    CloseSourceFile(Files[0]);
                }
                break;
        }
    }
    Task SubscriptionDebugDataChanged(DebugDataChangedMessage message, CancellationToken ct)
    {
        RefreshSourceFiles();
        return Task.CompletedTask;
    }
    void ExecutionStatusViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ExecutionStatusViewModel.IsDebugging) when !executionStatusViewModel.IsDebugging:
            case nameof(ExecutionStatusViewModel.IsDebuggingPaused) when !executionStatusViewModel.IsDebuggingPaused:
                ClearExecutionRow();
                break;
        }
    }
    internal void ClearExecutionRow()
    {
        foreach (var file in Files)
        {
            file.ClearExecutionRow();
        }
    }
    /// <summary>
    /// This has to be called when PDB changes
    /// It should reload all open files
    /// </summary>
    /// <remarks>App shouldn't be debugging when it happens.</remarks>
    internal void RefreshSourceFiles()
    {
        var oldFiles = Files.ToImmutableArray();
        var filesToDelete = new List<int>();
        var debugSymbols = (globals.Project?.DebugSymbols).ValueOrThrow();
        for (int i = 0; i < oldFiles.Length; i++)
        {
            var oldFile = Files[i];
            if (oldFile is SourceFileViewModel oldSourceFile)
            {
                var newFile = debugSymbols.Files[oldSourceFile.Path];
                if (newFile is null)
                {
                    filesToDelete.Add(i);
                }
                else
                {
                    var item = CreateSourceFileViewModel(newFile, debugSymbols.SymbolReferences);
                    Files[i] = item;
                }
            }
            else if (oldFile is DisassemblyViewModel)
            {
                filesToDelete.Add(i);
            }
            oldFile.Scope!.Dispose();
        }
        filesToDelete.Reverse();
        foreach (int fileIndexToDelete in filesToDelete)
        {
            Files.RemoveAt(fileIndexToDelete);
        }
    }
    internal SourceFileViewModel CreateSourceFileViewModel(PdbFile file, 
        ImmutableDictionary<PdbLine, LineSymbolReferences> symbolReferences)
    {
        var content = file.Lines
            .Select((l, i) => { 
                if (!symbolReferences.TryGetValue(l, out var sr))
                {
                    sr = LineSymbolReferences.Empty;
                }
                return new LineViewModel(l, l.LineNumber, sr); 
            })
            .ToImmutableArray();
        var item = serviceProvider.CreateScopedSourceFileViewModel(file, content);
        return item;
    }
    internal SourceFileViewModel? GetSourceFile(PdbFile file)
    {
        var item = Files.OfType<SourceFileViewModel>().FirstOrDefault(f => f.Path == file.Path);
        var debugSymbols = (globals.Project?.DebugSymbols).ValueOrThrow();
        if (item is null)
        {
            item = CreateSourceFileViewModel(file, debugSymbols.SymbolReferences);
            Files.Add(item);
        }
        return item;
    }
    internal void OpenSourceLineFile(OpenSourceLineFileMessage? message)
    {
        if (message is not null)
        {
            var pdbFile = message.File;
            var item = GetSourceFile(pdbFile);
            if (item is not null)
            {
                Selected = item;
                if (message.IsExecution)
                {
                    ClearExecutionRow();
                    if (executionStatusViewModel.IsDebugging)
                    {
                        item.SetExecutionRow(message.Line, message.AssemblyLine);
                    }
                }
                int row = item.GetEditorRowByLineNumber(message.Line.LineNumber-1)+1;
                item.SetCursorRow(row);
                if (message.Column.HasValue)
                {
                    item.SetCursorColumn(message.Column.Value);
                }
                if (message.MoveCaret && message.Column.HasValue)
                {
                    item.SetMoveCaret(row, message.Column.Value);
                }
            }
        }
    }
    internal void OpenSourceLineNumberFile(OpenSourceLineNumberFileMessage? message)
    {
        if (message is not null)
        {
            var pdbFile = message.File;
            var item = GetSourceFile(pdbFile);
            if (item is not null)
            {
                Selected = item;
                int? cursorRow = message.Line;
                int row = item.GetEditorRowByLineNumber(cursorRow.Value - 1) + 1;
                item.SetCursorRow(row);
                if (message.Column.HasValue)
                {
                    item.SetCursorColumn(message.Column.Value);
                }
                if (message.MoveCaret && cursorRow.HasValue && message.Column.HasValue)
                {
                    int caretRow = item.GetEditorRowByLineNumber(cursorRow.Value - 1) + 1;
                    item.SetMoveCaret(caretRow, message.Column.Value);
                }
            }
        }
    }
    internal void OpenAddressDisassembly(OpenAddressMessage? message)
    {
        if (message is not null)
        {
            var item = serviceProvider.CreateScopedDisassemblyViewModel(message.Address);
            Files.Add(item);
            Selected = item;
        }
    }
        void CloseSourceFile(IViewableContent? sourceFile)
    {
        if (sourceFile is not null)
        {
            Files.Remove(sourceFile);
        }
    }
    public void CloseAll()
    {
        Files.Clear();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            executionStatusViewModel.PropertyChanged += ExecutionStatusViewModel_PropertyChanged;
            globals.PropertyChanged -= Globals_PropertyChanged;
            foreach (var file in Files)
            {
                file.Scope!.Dispose();
            }
            openSourceLineNumberFileSubscription.Dispose();
            openSourceLineFileSubscription.Dispose();
            openAddressSubscription.Dispose();
            debugDataChangedSubscription.Dispose();
        }
        base.Dispose(disposing);
    }
}
