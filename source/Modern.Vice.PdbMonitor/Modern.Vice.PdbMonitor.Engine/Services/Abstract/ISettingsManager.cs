using Modern.Vice.PdbMonitor.Engine.Models.Configuration;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract;

public interface ISettingsManager
{
    Settings LoadSettings();
    T? Load<T>(string path)
        where T: class;
    void Save(Settings settings);
    void Save<T>(T settings, string path, bool createDirectory);
}
