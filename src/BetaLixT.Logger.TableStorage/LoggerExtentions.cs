using BetaLixT.Logger.TableStorage.Repositories;
using BetaLixT.Logger;
using BetaLixT.Logger.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetaLixT.Logger.TableStorage
{
    public static class LoggerExtentions
    {
        public static ILoggingBuilder AddTableStorageLogger(this ILoggingBuilder builder, IConfiguration configuration)
        {
            /*builder.AddConfiguration();*/
            var tableStorageOptions = new TableStorageLoggerOptions();
            configuration.Bind(TableStorageLoggerOptions.OptionsKey, tableStorageOptions);
            EnsureTablesCreated(tableStorageOptions);
            
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, LoggerProvider>());
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILogRepository, LogRepository>());
            builder.Services.AddOptions();
            builder.Services.Configure<TableStorageLoggerOptions>(configuration.GetSection(TableStorageLoggerOptions.OptionsKey));

            return builder;
        }

        private static void EnsureTablesCreated(TableStorageLoggerOptions options)
        {
            var repository = new LogRepository(Options.Create(options));
            repository.Table.CreateIfNotExists();
        }
    }
}
