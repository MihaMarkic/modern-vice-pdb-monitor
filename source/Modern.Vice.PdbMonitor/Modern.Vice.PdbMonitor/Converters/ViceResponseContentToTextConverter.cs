using Avalonia.Layout;
using Avalonia;
using Righthand.ViceMonitor.Bridge.Responses;

namespace Modern.Vice.PdbMonitor.Converters;
public class ViceResponseContentToTextConverter : ParameterlessValueConverter<ViceResponse?, string>
{
    public static readonly DirectProperty<ViceResponseContentToTextConverter, byte?> PCRegisterIdProperty
    = AvaloniaProperty.RegisterDirect<ViceResponseContentToTextConverter, byte?>(
       nameof(PCRegisterId),
       o => o.PCRegisterId,
       (o, v) => o.PCRegisterId = v
    );
    public byte? PCRegisterId { get; set; }
    public override string? Convert(ViceResponse? value, Type targetType, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }
        return value switch
        {
            StoppedResponse stopped => $"{stopped.ErrorCode}",
            CheckpointInfoResponse checkpointInfo =>
                $"{checkpointInfo.ErrorCode} " +
                $"{checkpointInfo.StartAddress:x4}-{checkpointInfo.EndAddress:x4} " +
                $"{(checkpointInfo.Enabled ? "enabled" : "disabled")} {(checkpointInfo.StopWhenHit ? "stop" : "cont")}",
            RegistersResponse registers => $"{registers.ErrorCode} PC:{GetPCFromRegisterResponse(registers):x4}",
            _ => null
        };
    }

    ushort? GetPCFromRegisterResponse(RegistersResponse registers)
    {
        if (PCRegisterId.HasValue)
        {
            return registers.Items.SingleOrDefault(i => i.RegisterId == PCRegisterId)?.RegisterValue;
        }
        return null;
    }

    public override ViceResponse? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
