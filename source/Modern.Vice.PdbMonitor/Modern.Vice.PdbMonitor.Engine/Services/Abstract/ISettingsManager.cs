using Modern.Vice.PdbMonitor.Engine.Models.Configuration;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;

public interface ISettingsManager
{
    Settings LoadSettings();
    BreakpointsSettings LoadBreakpointsSettings(string filePath);
    T? Load<T>(string path)
        where T: class;
    void Save(Settings settings);
    void Save(BreakpointsSettings breakpointsSettings, string filePath);
    void Save<T>(T settings, string path, bool createDirectory);
}
