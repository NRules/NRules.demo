using Autofac;
using Microsoft.Extensions.Configuration;
using NRules.Samples.ClaimsExpert.Domain.Modules;
using NRules.Samples.ClaimsExpert.Service.Modules;
using Serilog;
using Topshelf;

namespace NRules.Samples.ClaimsExpert.Service;

public class Program
{
    private static IContainer? Container { get; set; }

    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
            
        HostFactory.Run(x =>
        {
            x.Service<IServiceController>(s =>
            {
                s.ConstructUsing(name => BuildServiceController());
                s.WhenStarted(sc => sc.Start());
                s.WhenStopped(sc => Container?.Dispose());
                s.AfterStartingService(() => Log.Information("Claims expert service started"));
                s.AfterStoppingService(() => Log.Information("Claims expert service stopped"));
            });
            x.RunAsLocalSystem();

            x.SetDescription("Claims expert service");
            x.SetDisplayName("Claims Expert");
            x.SetServiceName("ClaimsExpert");
        });

        Log.CloseAndFlush();
    }

    private static IServiceController BuildServiceController()
    {
        var config = BuildConfiguration();
        Container = BuildContainer(config);
        return Container.Resolve<IServiceController>();
    }

    private static IContainer BuildContainer(IConfiguration config)
    {
        var builder = new ContainerBuilder();
        builder.Register(c => config).As<IConfiguration>();
        builder.RegisterAssemblyModules(typeof(ServiceModule).Assembly);
        builder.RegisterAssemblyModules(typeof(DomainModule).Assembly);
        var container = builder.Build();
        return container;
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        return config;
    }
}