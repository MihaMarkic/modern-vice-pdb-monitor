using System.Collections.Immutable;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class SourceFileViewModel : ScopedViewModel
    {
        public string Path { get; }
        public ImmutableArray<Line> Lines { get; }
        public int CursorColumn { get; set; }
        public int CursorRow { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lines"></param>
        /// <remarks>
        /// Constructor arguments are passed by <see cref="ServiceProviderExtension.CreateSourceFileViewModel"/>.
        /// It is mandatory that they are in sync.
        /// </remarks>
        public SourceFileViewModel(string path, ImmutableArray<Line> lines)
        {
            Path = path;
            Lines = lines;
        }
    }

    public record Line(int Row, string Content);
}
