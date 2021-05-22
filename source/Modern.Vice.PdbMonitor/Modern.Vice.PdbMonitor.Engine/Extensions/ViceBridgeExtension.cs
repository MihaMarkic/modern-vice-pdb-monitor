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
        public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
        public static async Task<TResponse?> AwaitWithLogAndTimeoutAsync<TCommand, TResponse>(this Task<CommandResponse<TResponse>> task, IDispatcher dispatcher, ILogger logger, 
            TCommand command, Func<TResponse, Task> action, 
            TimeSpan? timeout = default, CancellationToken ct = default)
            where TCommand: ViceCommand<TResponse>
            where TResponse : ViceResponse
        {
            TResponse response;
            try
            {
                var cr = await command.Response.AwaitWithTimeoutAsync(timeout ?? DefaultTimeout, ct);
                if (LogResponseErrors<TCommand, TResponse>(dispatcher, logger, cr))
                {
                    response = cr.Response!;
                    {
                        await action(response);
                    }
                    return response;
                }
                return default;
            }
            catch (TimeoutException)
            {
                LogTimeout<TCommand>(dispatcher, logger);
                return default;
            }
        }
        static bool LogResponseErrors<TCommand, TResponse>(IDispatcher dispatcher, ILogger logger, CommandResponse<TResponse> cr)
            where TCommand: IViceCommand
            where TResponse : ViceResponse
        {
            if (!cr.IsSuccess)
            {
                logger.LogError("Response for command {Command} returned error code {ErrorCode}", typeof(TCommand).Name, cr.ErrorCode);
                dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Communication", 
                    $"Response for command {typeof(TCommand).Name} returned error code {cr.ErrorCode}"));
                return false;
            }
            return true;
        }
        public static async Task<TResponse?> AwaitWithLogAndTimeoutAsync<TCommand, TResponse>(this Task<CommandResponse<TResponse>> task, IDispatcher dispatcher, ILogger logger, TCommand command, 
            Action<TResponse> action,
            TimeSpan? timeout = default, CancellationToken ct = default)
            where TCommand : ViceCommand<TResponse>
            where TResponse : ViceResponse
        {
            TResponse response;
            try
            {
                var cr = await command.Response.AwaitWithTimeoutAsync(timeout ?? DefaultTimeout, ct);
                if (LogResponseErrors<TCommand, TResponse>(dispatcher, logger, cr))
                {
                    response = cr.Response!;
                    {
                        action(response);
                    }
                    return response;
                }
                return default;
            }
            catch (TimeoutException)
            {
                LogTimeout<TCommand>(dispatcher, logger);
                return default;
            }
        }

        public static async Task<TResponse?> AwaitWithLogAndTimeoutAsync<TCommand, TResponse>(this Task<CommandResponse<TResponse>> task, IDispatcher dispatcher, ILogger logger, TCommand command,
            TimeSpan? timeout = default, CancellationToken ct = default)
            where TCommand: ViceCommand<TResponse>
            where TResponse: ViceResponse
        {
            try
            {
                var cr = await task.AwaitWithTimeoutAsync(timeout ?? DefaultTimeout, ct);
                if (LogResponseErrors<TCommand, TResponse>(dispatcher, logger, cr))
                {
                    return cr.Response!;
                }
                return default;
            }
            catch (TimeoutException)
            {
                LogTimeout<TCommand>(dispatcher, logger);
                return default;
            }
        }

        internal static void LogTimeout<TCommand>(IDispatcher dispatcher, ILogger logger)
        {
            logger.LogError("Timeout occurred while executing {Command}", typeof(TCommand).Name);
            dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Communication", $"Timeout occurred while executing {typeof(TCommand).Name}"));
        }
    }
}
