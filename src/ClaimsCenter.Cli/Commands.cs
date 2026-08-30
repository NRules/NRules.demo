using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using NRules.Samples.ClaimsCenter.Cli.Output;
using NRules.Samples.ClaimsExpert.Contract;

namespace NRules.Samples.ClaimsCenter.Cli;

/// <summary>
/// Command handlers. Every handler runs through <see cref="ExecuteAsync"/>, which owns the
/// channel lifetime and turns gRPC failures into a message on stderr and exit code
/// <see cref="ExitCodes.RpcFailure"/>, or an unresolvable/invalid service address into a
/// message on stderr and exit code <see cref="ExitCodes.UsageError"/>.
/// </summary>
internal static class Commands
{
    public static Task<int> ListAsync(string? endpointOption, bool json, CancellationToken cancellationToken)
    {
        return ExecuteAsync(endpointOption, async client =>
        {
            var claims = await client.GetAllAsync(cancellationToken);

            if (json)
            {
                JsonOutput.WriteClaims(Console.Out, claims);
            }
            else
            {
                ClaimFormatter.WriteList(Console.Out, claims);
            }

            return ExitCodes.Success;
        });
    }

    public static Task<int> ShowAsync(string? endpointOption, long claimId, bool json, CancellationToken cancellationToken)
    {
        return ExecuteAsync(endpointOption, async client =>
        {
            var claim = await client.FindByClaimIdAsync(claimId, cancellationToken);
            WriteClaim(claim, json);
            return ExitCodes.Success;
        });
    }

    public static Task<int> AdjudicateAsync(string? endpointOption, long claimId, bool json, CancellationToken cancellationToken)
    {
        return ExecuteAsync(endpointOption, async client =>
        {
            await client.AdjudicateAsync(claimId, cancellationToken);

            // Re-fetch so the caller sees the status and alerts the rules just produced, which is
            // what the WPF client does through ClaimController.RefreshSelected.
            var claim = await client.FindByClaimIdAsync(claimId, cancellationToken);
            WriteClaim(claim, json);
            return ExitCodes.Success;
        });
    }

    private static void WriteClaim(ClaimDto claim, bool json)
    {
        if (json)
        {
            JsonOutput.WriteClaim(Console.Out, claim);
        }
        else
        {
            ClaimFormatter.WriteDetail(Console.Out, claim);
        }
    }

    private static async Task<int> ExecuteAsync(string? endpointOption, Func<ClaimsClient, Task<int>> action)
    {
        // Named before resolution succeeds so a failure in Resolve itself still has something to
        // report: the precedence is --endpoint, then the environment variable, then appsettings.json,
        // so the option (when given) is the best candidate for "the address that was resolved".
        var address = endpointOption;
        ClaimsClient client;

        try
        {
            address = EndpointResolver.Resolve(endpointOption);
            client = new ClaimsClient(address);
        }
        catch (Exception exception) when (exception is UriFormatException or ArgumentException or InvalidOperationException)
        {
            // GrpcChannel.ForAddress validates the address eagerly, so a malformed or unsupported
            // address (no scheme, an unknown scheme, garbage text) throws here rather than surfacing
            // as an RpcException. This is a usage error, not an RPC failure: it is never retryable.
            // Scoped tightly to resolution and construction so a bug inside a command handler is
            // never misreported as a bad address.
            Console.Error.WriteLine($"Invalid service address '{address}'");
            return ExitCodes.UsageError;
        }

        // The channel must be disposed however action(client) exits, including when it throws
        // something other than RpcException, so disposal is a using block around the call rather
        // than folded into the catches below.
        using (client)
        {
            try
            {
                return await action(client);
            }
            catch (RpcException exception) when (exception.StatusCode == StatusCode.Unavailable)
            {
                Console.Error.WriteLine($"Cannot reach claims expert service at {client.Address}");
                return ExitCodes.RpcFailure;
            }
            catch (RpcException exception)
            {
                Console.Error.WriteLine($"Request failed: {exception.StatusCode}. {exception.Status.Detail}");
                return ExitCodes.RpcFailure;
            }
        }
    }
}
