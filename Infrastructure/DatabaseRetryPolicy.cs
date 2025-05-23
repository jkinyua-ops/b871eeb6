using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Nostra.DataLoad.Infrastructure
{
    public static class DatabaseRetryPolicy
    {
        private static readonly Random Jitter = new Random();

        public static IAsyncPolicy CreateRetryPolicy(ILogger logger)
        {
            return Policy
                .Handle<SqlException>(ex => IsTransientError(ex.Number))
                .Or<TimeoutException>()
                .OrInner<SqlException>(ex => IsTransientError(ex.Number))
                .Or<DbUpdateException>(ex => ex.InnerException is SqlException sqlEx && IsTransientError(sqlEx.Number))
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt =>
                    {
                        // Exponential backoff with jitter
                        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        var jitterSeconds = Jitter.Next(0, 3);
                        return baseDelay + TimeSpan.FromSeconds(jitterSeconds);
                    },
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogWarning(
                            exception,
                            "Database operation failed (Attempt {RetryCount} of 5). Retrying in {RetryTimeSpan}...",
                            retryCount,
                            timeSpan);
                    }
                );
        }

        private static bool IsTransientError(int errorNumber)
        {
            // SQL Server transient error numbers
            int[] transientErrors =
            {
                -2, // Timeout
                2, // Connection broken
                53, // Server not found
                121, // Connection broken during login
                258, // Timeout in transaction
                1205, // Deadlock victim
                10053, // Connection aborted by server
                10054, // Connection reset by peer
                10060, // Connection timeout
                40197, // Error processing request
                40501, // Service is busy
                40613, // Database unavailable
                49918, // Not enough resources
                49919, // Not enough resources
                49920, // Service is busy
            };

            return Array.IndexOf(transientErrors, errorNumber) >= 0;
        }
    }
}