using System.IO;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace Modern.Vice.PdbMonitor.Views;

public partial class BreakpointDetail : UserControl
{
    readonly TextEditor conditions;
    public BreakpointDetail()
    {
        InitializeComponent();
        conditions = this.FindControl<TextEditor>("Conditions");
        InitConditionsEditors();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    void InitConditionsEditors()
    {
        var assembly = typeof(BreakpointDetail).Assembly;
        using (Stream s = assembly.GetManifestResourceStream("Modern.Vice.PdbMonitor.Resources.breakpoint-condition.xshd")!)
        {
            using (XmlTextReader reader = new XmlTextReader(s))
            {
                conditions.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
    }
}
