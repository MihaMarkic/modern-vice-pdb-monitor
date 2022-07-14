namespace Modern.Vice.PdbMonitor.Engine.Models;

public record FlagModel(string Name, bool Value)
{
    public override string ToString() => $"{(Value ? "+": "-")}{Name}";
}
