using System;
using System.Collections.Immutable;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Core;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Modern.Vice.PdbMonitor.Engine.Models;
using Modern.Vice.PdbMonitor.Engine.Services.Implementation;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;
using Righthand.ViceMonitor.Bridge.Shared;

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
            var command = viceBridge.EnqueueCommand( new RegistersAvailableCommand(MemSpace.MainMemory));
            await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, mapping.Init);
        }
        async Task Update()
        {
            var command = viceBridge.EnqueueCommand(new RegistersGetCommand(MemSpace.MainMemory));
            await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, UpdateRegistersFromResponseAsync);
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
                    var command = viceBridge.EnqueueCommand(new RegistersAvailableCommand(MemSpace.MainMemory));
                    await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, mapping.Init);
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
        internal async Task<bool> SetRegisters((Register6510 RegisterCode, ushort Value) item, params (Register6510 RegisterCode, ushort Value)[] others)
        {
            var items = ImmutableArray<(Register6510 RegisterCode, ushort Value)>.Empty.Add(item).AddRange(others);
            var builder = ImmutableArray.CreateBuilder<RegisterItem>(items.Length);
            foreach (var i in items)
            {
                var registerId = mapping.GetRegisterId(i.RegisterCode);
                if (!registerId.HasValue)
                {
                    logger.LogError("Failed to get {RegisterCode} register id", i.RegisterCode);
                    dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Setting start address", $"Failed to get {i.RegisterCode} register id"));
                    return false;
                }
                builder.Add(new RegisterItem(registerId.Value, i.Value));
            }
            var registers = builder.ToImmutable();
            var command = viceBridge.EnqueueCommand(new RegistersSetCommand(MemSpace.MainMemory, registers));
            var response = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
            bool success = response is not null;
            if (success)
            {
                await UpdateRegistersFromResponseAsync(response!);
            }
            return success;
        }
        internal async Task<bool> SetStartAddressAsync(CancellationToken ct) => await SetRegisters(new(Register6510.PC, 0xC000));
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
