using System;
using System.Collections.Generic;
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
        var started = false;

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

            // Each of these activates only under its own service manager and is a no-op otherwise,
            // so the same binary runs as a systemd unit, a Windows service, or a console program.
            builder.Host.UseSystemd();
            builder.Host.UseWindowsService();
            builder.Host.UseSerilog();
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(ConfigureContainer);

            var port = ResolveListenPort(builder.Configuration);
            var addresses = RemoveUnbindableAddresses(ResolveListenAddresses(builder.Configuration), port);
            builder.WebHost.ConfigureKestrel(options => ConfigureEndpoint(options, addresses, port));
            builder.Services.AddGrpc();

            var app = builder.Build();
            app.MapGrpcService<ClaimServiceImpl>();
            app.MapGrpcService<AdjudicationServiceImpl>();

            lifetime = app.Lifetime;
            lifetime.ApplicationStarted.Register(() =>
            {
                started = true;
                Log.Information("Claims expert service started");
            });
            lifetime.ApplicationStopped.Register(() => Log.Information("Claims expert service stopped"));

            app.Run();
            return 0;
        }
        catch (OperationCanceledException) when (!started && lifetime?.ApplicationStopping.IsCancellationRequested == true)
        {
            // Shutdown was requested before the host finished starting. That is an orderly stop,
            // not a failure, so it must not be reported as one. Cancellation from any other source
            // is a genuine startup failure and falls through to the handler below.
            //
            // The check that the host never started is what keeps this narrow. Shutting down also
            // cancels, so without it a hosted service that overran the shutdown timeout would be
            // reported as a clean exit, and the host records that only at debug level.
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
    /// Drops addresses that cannot currently be bound, so that one unusable interface does not stop
    /// the service. A host name on a multi-homed machine routinely resolves to addresses that come
    /// and go, such as virtual switch, container and VPN adapters, or registrations left behind in
    /// DNS. The endpoint this replaced tolerated that, because it reported one bound port for the
    /// whole host name and could not represent a partial failure; Kestrel binds each address
    /// separately and abandons startup on the first that fails, so they are probed here instead.
    ///
    /// A single address is never probed. There is nothing to degrade to, so the configuration is
    /// either usable or not, and Kestrel's own error names the endpoint and the reason.
    /// </summary>
    private static IPAddress[] RemoveUnbindableAddresses(IPAddress[] addresses, int port)
    {
        if (addresses.Length < 2)
        {
            return addresses;
        }

        var bindable = new List<IPAddress>();
        var failures = new List<string>();
        foreach (var address in addresses)
        {
            var failure = DescribeBindFailure(address, port);
            if (failure == null)
            {
                bindable.Add(address);
            }
            else
            {
                failures.Add($"{address}: {failure}");
            }
        }

        if (bindable.Count == 0)
        {
            throw new InvalidOperationException(
                $"None of the addresses configured by '{HostnameSetting}' could be bound on port {port} " +
                $"({String.Join("; ", failures)}).");
        }

        foreach (var failure in failures)
        {
            Log.Warning("Not listening on an address that could not be bound. {Failure}", failure);
        }

        return bindable.ToArray();
    }

    /// <summary>
    /// Returns why an address cannot be bound on the port, or null when it can be. The address is
    /// bound and released rather than inferred from the interface list, because that asks the same
    /// question Kestrel asks moments later. Another process can still take the port in between, in
    /// which case Kestrel fails exactly as it would have without the probe.
    /// </summary>
    private static string? DescribeBindFailure(IPAddress address, int port)
    {
        try
        {
            using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(address, port));
            return null;
        }
        catch (SocketException ex)
        {
            return ex.Message;
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
