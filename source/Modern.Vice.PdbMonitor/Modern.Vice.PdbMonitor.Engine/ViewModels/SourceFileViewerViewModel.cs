using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class SourceFileViewerViewModel : NotifiableObject
{
    readonly ILogger<SourceFileViewerViewModel> logger;
    readonly ISubscription openSourceFileSubscription;
    readonly Globals globals;
    readonly IServiceProvider serviceProvider;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly ISubscription debugDataChangedSubscription;
    public ObservableCollection<SourceFileViewModel> Files { get; }
    public SourceFileViewModel? Selected { get; set; }
    public RelayCommand<SourceFileViewModel> CloseSourceFileCommand { get; }
    public SourceFileViewerViewModel(IDispatcher dispatcher, ILogger<SourceFileViewerViewModel> logger, Globals globals, IServiceProvider serviceProvider,
        ExecutionStatusViewModel executionStatusViewModel)
    {
        this.logger = logger;
        this.globals = globals;
        this.executionStatusViewModel = executionStatusViewModel;
        this.serviceProvider = serviceProvider;
        openSourceFileSubscription = dispatcher.Subscribe<OpenSourceFileMessage>(OpenSourceFile);
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
        for (int i = 0; i < oldFiles.Length; i++)
        {
            var oldFile = Files[i];
            var newFile = globals.Project?.DebugSymbols?.Files[oldFile.Path];
            if (newFile is null)
            {
                filesToDelete.Add(i);
            }
            else
            {
                var item = CreateSourceFileViewModel(newFile);
                Files[i] = item;
            }
            oldFile.Scope!.Dispose();
        }
        filesToDelete.Reverse();
        foreach (int fileIndexToDelete in filesToDelete)
        {
            Files.RemoveAt(fileIndexToDelete);
        }
    }
    internal SourceFileViewModel CreateSourceFileViewModel(PdbFile file)
    {
        var content = file.Lines
            .Select((l, i) => new LineViewModel(l, l.LineNumber, l.Text))
            .ToImmutableArray();
        var item = serviceProvider.CreateScopedSourceFileViewModel(file, content);
        return item;
    }
    internal void OpenSourceFile(OpenSourceFileMessage? message)
    {
        var pdbFile = message!.File;
        var item = Files.FirstOrDefault(f => f.Path == pdbFile.Path);
        if (item is null)
        {
            item = CreateSourceFileViewModel(pdbFile);
            Files.Add(item);
        }
        if (item is not null)
        {
            Selected = item;
            int? cursorRow = null;
            if (message.Line.HasValue)
            {
                cursorRow = message.Line.Value;
            }
            ClearExecutionRow();
            if (message.ExecutingLine.HasValue && executionStatusViewModel.IsDebugging)
            {
                item.SetExecutionRow(message.ExecutingLine.Value);
                if (!message.Line.HasValue)
                {
                    cursorRow = message.ExecutingLine.Value;
                }
            }
            if (cursorRow.HasValue)
            {
                item.SetCursorRow(cursorRow.Value);
            }
        }
    }
    void CloseSourceFile(SourceFileViewModel? sourceFile)
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
            openSourceFileSubscription.Dispose();
            debugDataChangedSubscription.Dispose();
        }
        base.Dispose(disposing);
    }
}
