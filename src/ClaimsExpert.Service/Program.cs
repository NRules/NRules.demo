using System;
using System.Linq;
using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NRules.Samples.ClaimsExpert.Domain.Modules;
using NRules.Samples.ClaimsExpert.Service.Modules;
using NRules.Samples.ClaimsExpert.Service.Services;
using Serilog;

namespace NRules.Samples.ClaimsExpert.Service;

public class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSystemd();
            builder.Host.UseSerilog();
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(ConfigureContainer);

            builder.WebHost.ConfigureKestrel(options => ConfigureEndpoint(options, builder.Configuration));
            builder.Services.AddGrpc();

            var app = builder.Build();
            app.MapGrpcService<ClaimServiceImpl>();
            app.MapGrpcService<AdjudicationServiceImpl>();

            Log.Information("Claims expert service started");
            app.Run();
            Log.Information("Claims expert service stopped");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureContainer(ContainerBuilder builder)
    {
        builder.RegisterAssemblyModules(typeof(ServiceModule).Assembly);
        builder.RegisterAssemblyModules(typeof(DomainModule).Assembly);
    }

    private static void ConfigureEndpoint(KestrelServerOptions options, IConfiguration config)
    {
        var hostname = config["grpcEndpointHostname"] ?? "localhost";
        var port = Int32.Parse(config["grpcEndpointPort"]!);

        static void Insecure(ListenOptions listen) => listen.Protocols = HttpProtocols.Http2;

        if (String.Equals(hostname, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            options.ListenLocalhost(port, Insecure);
        }
        else if (hostname is "*" or "+" or "0.0.0.0")
        {
            options.ListenAnyIP(port, Insecure);
        }
        else
        {
            var address = IPAddress.TryParse(hostname, out var parsed)
                ? parsed
                : Dns.GetHostAddresses(hostname).First();
            options.Listen(address, port, Insecure);
        }
    }
}
