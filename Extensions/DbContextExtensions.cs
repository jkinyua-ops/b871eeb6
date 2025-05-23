using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nostra.DataLoad.Infrastructure;
using System;

namespace Nostra.DataLoad.Extensions
{
    public static class DbContextExtensions
    {
        public static IServiceCollection AddDatabaseWithMonitoring<TContext>(
            this IServiceCollection services,
            string connectionString,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped) 
            where TContext : DbContext
        {
            services.AddDbContext<TContext>((provider, options) =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var performanceLogger = new DatabasePerformanceLogging(
                    loggerFactory.CreateLogger<DatabasePerformanceLogging>());

                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                });

                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);
                
                // In development, we might want to enable these
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging(true);
                    options.EnableDetailedErrors(true);
                }
                
                options.AddInterceptors(performanceLogger);
            }, contextLifetime);

            return services;
        }
    }
}