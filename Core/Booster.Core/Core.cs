using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Booster.Core;

public class BoosterOnConfig
{
    private readonly ServiceContainer container;

    public BoosterOnConfig(WebApplicationBuilder builder)
    {
        var collection = new ServiceContainerBuilder();

        collection.AddService<WebApplicationBuilder>(builder);
        collection.AddService<ConfigurationManager>(builder.Configuration);
        collection.AddService<IWebHostEnvironment>(builder.Environment);
        collection.AddService<ConfigureHostBuilder>(builder.Host);
        collection.AddService<ILoggingBuilder>(builder.Logging);
        collection.AddService<IMetricsBuilder>(builder.Metrics);
        collection.AddService<IServiceCollection>(builder.Services);
        collection.AddService<ConfigureWebHostBuilder>(builder.WebHost);

        container = new ServiceContainer(collection);
    }

    public void Run(params Delegate[] delegates)
    {
        foreach (var @delegate in delegates) container.CreateDelegate<Action>(@delegate)();
    }
}

public class BoosterOnStartup
{
    private readonly ServiceContainer container;

    public BoosterOnStartup(IServiceCollection services, WebApplication app)
    {
        var singletonContainer = new SingletonServiceFactoryContainer(services, app.Services);
        var transientContainer = new TransientServiceFactoryContainer(services, app.Services);

        var collection = new ServiceContainerBuilder();
        collection.AddService<WebApplication>(app);

        container = new ServiceContainer(singletonContainer, transientContainer, new ServiceContainer(collection));
    }

    public void Run(params Delegate[] delegates)
    {
        foreach (var @delegate in delegates) container.CreateDelegate<Action>(@delegate)();
    }
}