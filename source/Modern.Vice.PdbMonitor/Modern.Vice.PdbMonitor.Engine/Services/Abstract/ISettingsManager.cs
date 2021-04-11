using Modern.Vice.PdbMonitor.Engine.Models.Configuration;
using System;

namespace Modern.Vice.PdbMonitor.Engine.Services.Abstract
{
    public interface ISettingsManager
    {
        Settings Load();
        void Save(Settings settings);
    }
}
