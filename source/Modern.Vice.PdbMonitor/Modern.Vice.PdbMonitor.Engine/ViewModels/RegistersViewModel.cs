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

namespace Modern.Vice.PdbMonitor.Engine.ViewModels;

public class RegistersViewModel: NotifiableObject
{
    readonly ILogger<RegistersViewModel> logger;
    readonly IViceBridge viceBridge;
    readonly RegistersMapping mapping;
    readonly ExecutionStatusViewModel executionStatusViewModel;
    readonly CommandsManager commandsManager;
    readonly IDispatcher dispatcher;
    readonly IProfiler profiler;
    readonly TaskFactory uiFactory;
    public event EventHandler? RegistersUpdated;
    public Registers6510 Current { get; private set; } = Registers6510.Empty;
    public Registers6510 Previous { get; private set; } = Registers6510.Empty;
    public bool IsLoadingMappings { get; private set; }
    public bool IsLoadingRegisters { get; private set; }
    public byte? PCRegisterId { get; private set; }
    public RelayCommandAsync UpdateCommand { get; }
    public RegistersViewModel(ILogger<RegistersViewModel> logger, IViceBridge viceBridge, RegistersMapping mapping,
        ExecutionStatusViewModel executionStatusViewModel, IDispatcher dispatcher, IProfiler profiler)
    {
        this.logger = logger;
        this.viceBridge = viceBridge;
        this.mapping = mapping;
        this.executionStatusViewModel = executionStatusViewModel;
        this.dispatcher = dispatcher;
        this.profiler = profiler;
        uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        commandsManager = new CommandsManager(this, new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext()));
        if (!profiler.IsActive)
        {
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
        }
        profiler.IsActiveChanged += Profiler_IsActiveChanged;
        UpdateCommand = commandsManager.CreateRelayCommandAsync(Update, () => !IsLoadingMappings && IsLoadingRegisters);
    }

    private void Profiler_IsActiveChanged(object? sender, EventArgs e)
    {
        if (profiler.IsActive)
        {
            viceBridge.ViceResponse -= ViceBridge_ViceResponse;
        }
        else
        {
            viceBridge.ViceResponse += ViceBridge_ViceResponse;
        }
    }

    protected void OnRegistersUpdated(EventArgs e) => RegistersUpdated?.Invoke(this, e);
    public async Task InitAsync()
    {
        await mapping.Initialized;
        PCRegisterId = mapping.GetRegisterId(Register6510.PC);
    }
    async Task Update()
    {
        var command = viceBridge.EnqueueCommand(new RegistersGetCommand(MemSpace.MainMemory),
            resumeOnStopped: true);
        await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command, UpdateRegistersFromResponseAsync);
    }

    void ViceBridge_ViceResponse(object? sender, ViceResponseEventArgs e)
    {
        switch (e.Response)
        {
            case RegistersResponse registerResponse:
                if (!IsLoadingMappings)
                {
                    _ = uiFactory.StartNew(() => UpdateRegistersFromResponseAsync(registerResponse));
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
            OnRegistersUpdated(EventArgs.Empty);
        }
        else if (!IsLoadingMappings)
        {
            IsLoadingMappings = true;
            try
            {
                var command = viceBridge.EnqueueCommand(new RegistersAvailableCommand(MemSpace.MainMemory),
                    resumeOnStopped: true);
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
        var command = viceBridge.EnqueueCommand(new RegistersSetCommand(MemSpace.MainMemory, registers),
            resumeOnStopped: true);
        var response = await command.Response.AwaitWithLogAndTimeoutAsync(dispatcher, logger, command);
        bool success = response is not null;
        if (success)
        {
            await UpdateRegistersFromResponseAsync(response!);
        }
        return success;
    }
    internal async Task<bool> SetStartAddressAsync(ushort address, CancellationToken ct) => await SetRegisters(new(Register6510.PC, address));
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            viceBridge.ViceResponse -= ViceBridge_ViceResponse;
            profiler.IsActiveChanged -= Profiler_IsActiveChanged;
        }
        base.Dispose(disposing);
    }
}
