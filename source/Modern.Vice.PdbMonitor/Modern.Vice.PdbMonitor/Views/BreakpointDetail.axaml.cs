using System.Xml;
using Avalonia.Controls;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace Modern.Vice.PdbMonitor.Views;

partial class BreakpointDetail : UserControl
{
    public BreakpointDetail()
    {
        InitializeComponent();
        InitConditionsEditors();
    }
    void InitConditionsEditors()
    {
        var assembly = typeof(BreakpointDetail).Assembly;
        using (Stream s = assembly.GetManifestResourceStream("Modern.Vice.PdbMonitor.Resources.breakpoint-condition.xshd")!)
        {
            using (XmlTextReader reader = new XmlTextReader(s))
            {
                Conditions.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
    }
}
