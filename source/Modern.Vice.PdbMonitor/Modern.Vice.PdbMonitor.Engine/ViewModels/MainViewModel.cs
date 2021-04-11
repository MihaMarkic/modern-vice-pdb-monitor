using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class MainViewModel: NotifiableObject
    {
        readonly ILogger<MainViewModel> logger;
        readonly IAcmePdbParser acmePdbParser;
        readonly Globals globals;
        public SettingsViewModel SettingsViewModel { get; }
        public RelayCommand ShowSettingsCommand { get; }
        public bool IsShowingSettings { get; private set; }
        public MainViewModel(ILogger<MainViewModel> logger, IAcmePdbParser acmePdbParser, SettingsViewModel settingsViewModel, Globals globals)
        {
            this.logger = logger;
            this.acmePdbParser = acmePdbParser;
            SettingsViewModel = settingsViewModel;
            this.globals = globals;
            ShowSettingsCommand = new RelayCommand(() => IsShowingSettings = !IsShowingSettings);
        }
    }
}
