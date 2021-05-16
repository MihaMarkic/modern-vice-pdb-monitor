using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modern.Vice.PdbMonitor.Engine.Messages;
using Righthand.MessageBus;
using Righthand.ViceMonitor.Bridge.Commands;
using Righthand.ViceMonitor.Bridge.Responses;

namespace Righthand.ViceMonitor.Bridge.Services.Abstract
{
    public static class ViceBridgeExtension
    {
        public static async Task<TResponse?> ExecuteCommandAsync<TCommand, TResponse>(this IViceBridge viceBridge, IDispatcher dispatcher, ILogger logger, 
            TCommand command, Func<TResponse, Task> action, 
            TimeSpan timeout = default, CancellationToken ct = default)
            where TCommand: ViceCommand<TResponse>
            where TResponse : ViceResponse
        {
            viceBridge.EnqueueCommand(command);
            TResponse response;
            try
            {
                response = await command.Response.AwaitWithTimeoutAsync(TimeSpan.FromSeconds(5));
                await action(response);
                return response;
            }
            catch (TimeoutException)
            {
                logger.LogError("Timeout occurred while executing {Command}", command.GetType().Name);
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Communication", $"Timeout occurred while executing {command.GetType().Name}"));
                return default;
            }
        }
        public static async Task<TResponse?> ExecuteCommandAsync<TCommand, TResponse>(this IViceBridge viceBridge, IDispatcher dispatcher, ILogger logger, TCommand command, Action<TResponse> action,
            TimeSpan timeout = default, CancellationToken ct = default)
            where TCommand : ViceCommand<TResponse>
            where TResponse : ViceResponse
        {
            viceBridge.EnqueueCommand(command);
            TResponse response;
            try
            {
                response = await command.Response.AwaitWithTimeoutAsync(TimeSpan.FromSeconds(5));
                action(response);
                return response;
            }
            catch (TimeoutException)
            {
                logger.LogError("Timeout occurred while executing {Command}", command.GetType().Name);
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Communication", $"Timeout occurred while executing {command.GetType().Name
                return default;
            }
        }
    }
}
