using System.Collections.Immutable;
using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class SourceFileViewModel : ScopedViewModel
    {
        public string Path { get; }
        public ImmutableArray<LineViewModel> Lines { get; }
        public int CursorColumn { get; set; }
        public int CursorRow { get; set; }
        public int? ExecutionRow { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lines"></param>
        /// <remarks>
        /// Constructor arguments are passed by <see cref="ServiceProviderExtension.CreateSourceFileViewModel"/>.
        /// It is mandatory that they are in sync.
        /// </remarks>
        public SourceFileViewModel(string path, ImmutableArray<LineViewModel> lines)
        {
            Path = path;
            Lines = lines;
        }
        public void ClearExecutionRow()
        {
            foreach (var line in Lines)
            {
                line.IsExecution = false;
            }
        }
        public void SetExecutionRow(int rowIndex)
        {
            Lines[rowIndex].IsExecution = true;
        }
    }

    public class LineViewModel : NotifiableObject
    {
        public bool IsExecution { get; set; }
        public int Row { get; }
        public ushort? Address { get; }
        public string Content { get; }
        public LineViewModel(int row, ushort? address, string content)
        {
            Row = row;
            Address = address;
            Content = content;
        }
    }
}
