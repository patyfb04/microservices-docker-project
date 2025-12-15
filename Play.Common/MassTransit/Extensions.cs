using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Play.Common.Settings;
using System;
using System.Reflection;
namespace Play.Common.MassTransit
{
    public static class Extensions
    {
        public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection serviceCollection,
            Action<IRetryConfigurator> configureRetries = null)
        {

            serviceCollection.AddMassTransit(configure =>
            {
                configure.AddConsumers(Assembly.GetEntryAssembly());
                UsingRabbitMq((ServiceCollectionBusConfigurator)configure, configureRetries);

            });

            return serviceCollection;
        }

        public static void UsingRabbitMq(this ServiceCollectionBusConfigurator configure, 
            Action<IRetryConfigurator> configureRetries) {

            configure.UsingRabbitMq((context, configurator) => {

                var configuration = context.GetRequiredService<IConfiguration>();
                var serviceSettings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
                var rabbitMQSettings = configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
                var massTransitSettings = configuration.GetSection(nameof(MassTransitSettings)).Get<MassTransitSettings>();

                configurator.Host(rabbitMQSettings.Host);
                configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(serviceSettings.ServiceName, false));

                if (configureRetries is null)
                {
                    configureRetries = (retryConfigurator) => retryConfigurator.Interval(massTransitSettings.Retries, TimeSpan.FromSeconds(massTransitSettings.TimeIntervalInSeconds));
                }

                configurator.UseMessageRetry(configureRetries);
            });
        }
    }
}
