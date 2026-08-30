using System;
using Microsoft.Extensions.Configuration;

namespace NRules.Samples.ClaimsCenter.Cli;

/// <summary>
/// Resolves the service address from, in order of precedence, the --endpoint option, the
/// grpcEndpointAddress environment variable, and appsettings.json next to the binary.
/// </summary>
internal static class EndpointResolver
{
    private const string SettingName = "grpcEndpointAddress";
    private const string DefaultAddress = "http://127.0.0.1:8888";

    public static string Resolve(string? endpointOption)
    {
        if (!string.IsNullOrWhiteSpace(endpointOption))
        {
            return endpointOption;
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(SettingName);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        // The base path is the binary's directory rather than the current directory, so that the
        // CLI behaves the same however it is started. This mirrors the service, which anchors its
        // content root to AppContext.BaseDirectory for the same reason.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var fromSettings = configuration[SettingName];
        return string.IsNullOrWhiteSpace(fromSettings) ? DefaultAddress : fromSettings;
    }
}
