using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Modern.Vice.PdbMonitor.Views;

public partial class ExecutionAddressInfo : UserControl
{
    public static readonly DirectProperty<ExecutionAddressInfo, ushort?> ExecutionAddressProperty =
        AvaloniaProperty.RegisterDirect<ExecutionAddressInfo, ushort?>(nameof(ExecutionAddress),
            o => o.ExecutionAddress, (o, v) => o.ExecutionAddress = v, 
            defaultBindingMode: BindingMode.OneWay);
    public static readonly DirectProperty<ExecutionAddressInfo, bool> ExecutionAddressVisibleProperty =
        AvaloniaProperty.RegisterDirect<ExecutionAddressInfo, bool>(nameof(ExecutionAddressVisible),
        o => o.ExecutionAddressVisible, (o, v) => o.ExecutionAddressVisible = v, 
        defaultBindingMode: BindingMode.OneWay);
    public static readonly DirectProperty<ExecutionAddressInfo, bool> EffectiveVisibilityProperty =
        AvaloniaProperty.RegisterDirect<ExecutionAddressInfo, bool>(nameof(EffectiveVisibility),
    o => o.EffectiveVisibility, (o, v) => o.EffectiveVisibility = v,
        defaultBindingMode: BindingMode.OneWay);
    ushort? executionAddress;
    bool executionAddressVisible;
    bool effectiveVisibility;
    public ExecutionAddressInfo()
    {
        InitializeComponent();
    }
    public ushort? ExecutionAddress
    {
        get => executionAddress;
        set => SetAndRaise(ExecutionAddressProperty, ref executionAddress, value);
    }
    public bool EffectiveVisibility
    {
        get => effectiveVisibility;
        set => SetAndRaise(EffectiveVisibilityProperty, ref effectiveVisibility, value);
    }
    public bool ExecutionAddressVisible
    {
        get => executionAddressVisible;
        set
        {
            SetAndRaise(ExecutionAddressVisibleProperty, ref executionAddressVisible, value);
            _ = DelayShowVisibilityAsync(value);
        }
    }

    CancellationTokenSource? delayedVisibilityCts;
    /// <summary>
    /// Delays show to avoid flickering.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <remarks>Not done in most beautiful way.</remarks>
    async Task DelayShowVisibilityAsync(bool value)
    {
        delayedVisibilityCts?.Cancel();
        if (value)
        {
            delayedVisibilityCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(50, delayedVisibilityCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
        EffectiveVisibility = value;
    }
}
