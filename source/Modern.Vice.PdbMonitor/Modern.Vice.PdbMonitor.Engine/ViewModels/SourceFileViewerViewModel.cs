using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Righthand.MessageBus;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class SourceFileViewerViewModel : NotifiableObject
    {
        readonly ILogger<SourceFileViewerViewModel> logger;
        readonly Subscription openSourceFileSubscription;
        readonly Globals globals;
        readonly IServiceProvider serviceProvider;
        public ObservableCollection<SourceFileViewModel> Files { get; }
        public SourceFileViewModel? Selected { get; set; }
        public RelayCommand<SourceFileViewModel> CloseSourceFileCommand { get; }
        public SourceFileViewerViewModel(IDispatcher dispatcher, ILogger<SourceFileViewerViewModel> logger, Globals globals, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.globals = globals;
            this.serviceProvider = serviceProvider;
            openSourceFileSubscription = dispatcher.Subscribe<OpenSourceFileMessage>(OpenSourceFile);
            CloseSourceFileCommand = new(CloseSourceFile);
            Files = new();
        }
        internal void OpenSourceFile(object sender, OpenSourceFileMessage? message)
        {
            var pdbFile = message!.File;
            var item = Files.FirstOrDefault(f => string.Equals(f.Path, pdbFile.Path, StringComparison.Ordinal));
            if (item is null)
            {
                string path = Path.Combine(globals.ProjectDirectory!, pdbFile.Path);
                if (File.Exists(path))
                {
                    var content = File.ReadAllLines(path)
                        .Select((l, i) => new Line(i + 1, l))
                        .ToImmutableArray();
                    item = serviceProvider.CreateScopedSourceFileViewModel(pdbFile.Path, content);
                    Files.Add(item);
                }
            }
            if (item is not null)
            {
                if (message.Line.HasValue)
                {
                    item.CursorRow = message.Line.Value;
                }
                Selected = item;
            }
        }
        void CloseSourceFile(SourceFileViewModel? sourceFile)
        {
            if (sourceFile is not null)
            {
                Files.Remove(sourceFile);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach(var file in Files)
                {
                    file.Scope!.Dispose();
                }
                openSourceFileSubscription.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
