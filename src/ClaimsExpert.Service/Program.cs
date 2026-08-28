using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using Serilog.Events;

namespace NRules.Samples.ClaimsExpert.Service;

public class Program
{
    private const string HostnameSetting = "grpcEndpointHostname";
    private const string PortSetting = "grpcEndpointPort";

    private static int Main(string[] args)
    {
        IHostApplicationLifetime? lifetime = null;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
            .MinimumLevel.Override("Grpc", LogEventLevel.Information)
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                // Args must be passed explicitly: the WebApplicationOptions overload does not pick up
                // the command line otherwise, which would silently drop --setting=value overrides.
                Args = args,
                // Anchor the content root to the binary rather than the current directory, so that a
                // service started by systemd (which defaults to a working directory of /) loads the
                // same appsettings.json as dotnet run. Note that this also takes precedence over the
                // --contentRoot switch and the ASPNETCORE_CONTENTROOT environment variable.
                ContentRootPath = AppContext.BaseDirectory,
            });

            builder.Host.UseSystemd();
            builder.Host.UseSerilog();
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(ConfigureContainer);

            var addresses = ResolveListenAddresses(builder.Configuration);
            var port = ResolveListenPort(builder.Configuration);
            builder.WebHost.ConfigureKestrel(options => ConfigureEndpoint(options, addresses, port));
            builder.Services.AddGrpc();

            var app = builder.Build();
            app.MapGrpcService<ClaimServiceImpl>();
            app.MapGrpcService<AdjudicationServiceImpl>();

            lifetime = app.Lifetime;
            lifetime.ApplicationStarted.Register(() => Log.Information("Claims expert service started"));
            lifetime.ApplicationStopped.Register(() => Log.Information("Claims expert service stopped"));

            app.Run();
            return 0;
        }
        catch (OperationCanceledException) when (lifetime?.ApplicationStopping.IsCancellationRequested == true)
        {
            // Shutdown was requested before the host finished starting. That is an orderly stop,
            // not a failure, so it must not be reported as one. Cancellation from any other source
            // is a genuine startup failure and falls through to the handler below.
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Claims expert service terminated unexpectedly");
            return 1;
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

    private static void ConfigureEndpoint(KestrelServerOptions options, IPAddress[] addresses, int port)
    {
        foreach (var address in addresses)
        {
            options.Listen(address, port, listen => listen.Protocols = HttpProtocols.Http2);
        }
    }

    /// <summary>
    /// Resolves the configured hostname to the addresses to listen on. The service listens on IPv4
    /// only, and on every address the hostname resolves to, matching the endpoint it replaced.
    /// Resolving here rather than inside the Kestrel options callback keeps a configuration mistake
    /// at the top of the startup stack instead of nested inside a server activation failure.
    /// </summary>
    private static IPAddress[] ResolveListenAddresses(IConfiguration config)
    {
        var hostname = config[HostnameSetting] ?? "localhost";

        if (String.Equals(hostname, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { IPAddress.Loopback };
        }

        if (hostname is "*" or "+" or "0.0.0.0")
        {
            return new[] { IPAddress.Any };
        }

        if (String.IsNullOrWhiteSpace(hostname))
        {
            throw new InvalidOperationException(
                $"Configuration setting '{HostnameSetting}' is empty. Specify an IPv4 address, a host name, " +
                "or '*' to listen on all IPv4 interfaces.");
        }

        if (IPAddress.TryParse(hostname, out var literal))
        {
            return new[] { RequireIPv4(literal, hostname) };
        }

        IPAddress[] resolved;
        try
        {
            resolved = Dns.GetHostAddresses(hostname);
        }
        catch (SocketException ex)
        {
            throw new InvalidOperationException(
                $"Configuration setting '{HostnameSetting}' is '{hostname}', which could not be resolved: {ex.Message}", ex);
        }

        // Filter to IPv4 here rather than with the AddressFamily overload of GetHostAddresses: that
        // overload reports an IPv6-only name as SocketException(HostNotFound), which is
        // indistinguishable from a misspelled one, and it ignores the filter for the local hostname.
        // Duplicates are removed because a resolver may return the same address more than once, and
        // binding it twice would fail as an address conflict against ourselves.
        var addresses = resolved
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .Distinct()
            .ToArray();

        if (addresses.Length == 0)
        {
            throw new InvalidOperationException(
                $"Configuration setting '{HostnameSetting}' is '{hostname}', which resolved only to non-IPv4 " +
                $"addresses ({String.Join(", ", resolved.Select(address => address.ToString()))}). " +
                "The service listens on IPv4 only.");
        }

        return addresses;
    }

    private static IPAddress RequireIPv4(IPAddress address, string hostname)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return address;
        }

        // An IPv4-mapped IPv6 literal such as ::ffff:127.0.0.1 denotes an IPv4 address, so bind it as one.
        if (address.IsIPv4MappedToIPv6)
        {
            return address.MapToIPv4();
        }

        throw new InvalidOperationException(
            $"Configuration setting '{HostnameSetting}' is '{hostname}', which is not an IPv4 address. " +
            "The service listens on IPv4 only.");
    }

    /// <summary>
    /// Reads and validates the configured listen port.
    /// </summary>
    private static int ResolveListenPort(IConfiguration config)
    {
        var port = config.GetValue<int?>(PortSetting)
            ?? throw new InvalidOperationException($"Configuration setting '{PortSetting}' is not set.");

        if (port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                $"Configuration setting '{PortSetting}' is '{port}'. Specify a port between 1 and 65535; " +
                "binding to an operating system assigned port is not supported.");
        }

        return port;
    }
}
