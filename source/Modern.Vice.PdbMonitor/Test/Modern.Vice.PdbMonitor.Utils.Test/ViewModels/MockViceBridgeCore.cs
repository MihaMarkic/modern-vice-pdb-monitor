using System;
using System.Threading;
using System.Threading.Tasks;
using Righthand.ViceMonitor.Bridge;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;
using Righthand.ViceMonitor.Bridge.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Engine.Test.ViewModels;

public abstract class MockViceBridgeCore : IViceBridge
{
    public Task RunnerTask => throw new NotImplementedException();

    public bool IsRunning => throw new NotImplementedException();

    public bool IsStarted => throw new NotImplementedException();

    public bool IsConnected => throw new NotImplementedException();

    public IPerformanceProfiler PerformanceProfiler => throw new NotImplementedException();

    public event EventHandler<ViceResponseEventArgs> ViceResponse;
    public event EventHandler<ConnectedChangedEventArgs> ConnectedChanged;

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    protected abstract ViceResponse ProcessCommand<T>(T command)
        where T : IViceCommand;

    public T EnqueueCommand<T>(T command, bool resumeOnStopped = false)
        where T : IViceCommand
    {
        var response = ProcessCommand(command);
        command.SetResult(response);
        return command;
    }

    public void Start(int port = 6502)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(bool waitForQueueToProcess)
    {
        throw new NotImplementedException();
    }

    public Task<bool> WaitForConnectionStatusChangeAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
