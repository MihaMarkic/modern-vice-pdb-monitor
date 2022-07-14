using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Modern.Vice.PdbMonitor.Engine.Messages;

namespace Modern.Vice.PdbMonitor.Views;

public partial class ModalDialogWindow : Window
{
    ShowModalDialogMessageCore? message;
    public ModalDialogWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (message is not null)
        {
            message.Close -= Message_Close;
        }
        message = DataContext as ShowModalDialogMessageCore;
        if (message is not null)
        {
            message.Close += Message_Close;
        }
        base.OnDataContextChanged(e);
    }

    private void Message_Close(object? sender, EventArgs e)
    {
        Close();
    }
}
