using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.ViewModels
{
    public class RegistersViewModel: NotifiableObject
    {
        readonly ILogger<RegistersViewModel> logger;
        readonly IViceBridge viceBridge;
        readonly RegistersMapping mapping;
        readonly CommandsManager commandsManager;
        public Registers6510 Current { get; private set; } = Registers6510.Empty;
        public Registers6510 Previous { get; private set; } = Registers6510.Empty;
        public bool IsLoadingMappings { get; private set; }
        public bool IsLoadingRegisters { get; private set; }
        public RelayCommandAsync UpdateCommand { get; }
        public RegistersViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge, RegistersMapping mapping)
        {
            this.logger = logger;
            this.viceBridge = viceBridge;
            this.mapping = mapping;
            commandsManager = new CommandsManager(this, new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext()));
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
            UpdateCommand = commandsManager.CreateRelayCommandAsync(Update, () => !IsLoadingMappings && IsLoadingRegisters);
        }
        public async Task InitAsync()
        {
            var command = viceBridge.EnqueueCommand(new RegistersAvailableCommand(MemSpace.MainMemory));
            var response = await command.Response;
            mapping.Init(response);

        }
        async Task Update()
        {
            var command = viceBridge.EnqueueCommand(new RegistersGetCommand(MemSpace.MainMemory));
            var response = await command.Response;
            await UpdateRegistersFromResponse(response);
        }

        void ViceBridge_ViceResponse(object? sender, ViceResponseEventArgs e)
        {
            switch (e.Response)
            {
                case RegistersResponse registerResponse:
                    if (!IsLoadingMappings)
                    {
                        _ = UpdateRegistersFromResponse(registerResponse);
                    }
                    break;
            }
        }
        async Task UpdateRegistersFromResponse(RegistersResponse response)
        {
            if (mapping.IsMappingAvailable)
            {
                Previous = Current;
                Current = mapping.MapValues(response);
            }
            else if (!IsLoadingMappings)
            {
                IsLoadingMappings = true;
                try
                {
                    var command = viceBridge.EnqueueCommand(new RegistersAvailableCommand(MemSpace.MainMemory));
                    var availableResponse = await command.Response;
                    mapping.Init(availableResponse);
                }
                finally
                {
                    IsLoadingMappings = false;
                }
                await UpdateRegistersFromResponse(response);
            }
        }
        public void Reset()
        {
            mapping.Clear();
            Current = Registers6510.Empty;
            Previous = Registers6510.Empty;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                viceBridge.ViceResponse -= ViceBridge_ViceResponse;
            }
            base.Dispose(disposing);
        }
    }
}
