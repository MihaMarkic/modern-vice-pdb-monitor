using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Righthand.MessageBus;
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
        readonly IDispatcher dispatcher;
        public Registers6510 Current { get; private set; } = Registers6510.Empty;
        public Registers6510 Previous { get; private set; } = Registers6510.Empty;
        public bool IsLoadingMappings { get; private set; }
        public bool IsLoadingRegisters { get; private set; }
        public RelayCommandAsync UpdateCommand { get; }
        public RegistersViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge, RegistersMapping mapping, IDispatcher dispatcher)
        {
            this.logger = logger;
            this.viceBridge = viceBridge;
            this.mapping = mapping;
            this.dispatcher = dispatcher;
            commandsManager = new CommandsManager(this, new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext()));
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
            UpdateCommand = commandsManager.CreateRelayCommandAsync(Update, () => !IsLoadingMappings && IsLoadingRegisters);
        }
        public async Task InitAsync()
        {
            await viceBridge.ExecuteCommandAsync(dispatcher, logger, new RegistersAvailableCommand(MemSpace.MainMemory),
                (Action<RegistersAvailableResponse>)mapping.Init);
        }
        async Task Update()
        {
            await viceBridge.ExecuteCommandAsync(dispatcher, logger, new RegistersGetCommand(MemSpace.MainMemory), 
                (Func<RegistersResponse, Task>)UpdateRegistersFromResponseAsync);
        }

        void ViceBridge_ViceResponse(object? sender, ViceResponseEventArgs e)
        {
            switch (e.Response)
            {
                case RegistersResponse registerResponse:
                    if (!IsLoadingMappings)
                    {
                        _ = UpdateRegistersFromResponseAsync(registerResponse);
                    }
                    break;
            }
        }
        async Task UpdateRegistersFromResponseAsync(RegistersResponse response)
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
                    await viceBridge.ExecuteCommandAsync(dispatcher, logger, new RegistersAvailableCommand(MemSpace.MainMemory),
                        (Action<RegistersAvailableResponse>)mapping.Init);
                }
                finally
                {
                    IsLoadingMappings = false;
                }
                await UpdateRegistersFromResponseAsync(response);
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
