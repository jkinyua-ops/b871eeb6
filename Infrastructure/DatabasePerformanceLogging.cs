using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nostra.DataLoad.Infrastructure
{
    public class DatabasePerformanceLogging : DbCommandInterceptor
    {
        private readonly ILogger<DatabasePerformanceLogging> _logger;
        private static readonly TimeSpan _slowQueryThreshold = TimeSpan.FromSeconds(3);

        public DatabasePerformanceLogging(ILogger<DatabasePerformanceLogging> logger)
        {
            _logger = logger;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Duration > _slowQueryThreshold)
            {
                _logger.LogWarning(
                    "Slow query detected ({Duration}ms): {CommandText}",
                    eventData.Duration.TotalMilliseconds,
                    command.CommandText);
            }

            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override int NonQueryExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result)
        {
            if (eventData.Duration > _slowQueryThreshold)
            {
                _logger.LogWarning(
                    "Slow non-query operation detected ({Duration}ms): {CommandText}",
                    eventData.Duration.TotalMilliseconds,
                    command.CommandText);
            }

            return base.NonQueryExecuted(command, eventData, result);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            return base.ScalarExecuting(command, eventData, result);
        }

        public override object ScalarExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            object result)
        {
            if (eventData.Duration > _slowQueryThreshold)
            {
                _logger.LogWarning(
                    "Slow scalar operation detected ({Duration}ms): {CommandText}",
                    eventData.Duration.TotalMilliseconds,
                    command.CommandText);
            }

            return base.ScalarExecuted(command, eventData, result);
        }
    }
}