namespace Modern.Vice.PdbMonitor.Engine.Messages;

public record ErrorMessage
{
    public ErrorMessageLevel Level { get; }
    public string Title { get; }
    public string Text { get; }
    public ErrorMessage(ErrorMessageLevel level, string title, string text)
    {
        Level = level;
        Title = title;
        Text = text;
    }
}

public enum ErrorMessageLevel
{
    Info,
    Warning,
    Error
}
