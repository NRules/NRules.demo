using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using NRules.Samples.ClaimsExpert.Contract;

namespace NRules.Samples.ClaimsCenter.Cli;

/// <summary>
/// Wraps the generated gRPC clients so the commands do not each have to know how the channel is
/// created or how the streaming call is drained.
/// </summary>
internal sealed class ClaimsClient : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly ClaimService.ClaimServiceClient _claimService;
    private readonly AdjudicationService.AdjudicationServiceClient _adjudicationService;

    public ClaimsClient(string address)
    {
        Address = address;
        _channel = GrpcChannel.ForAddress(address);
        _claimService = new ClaimService.ClaimServiceClient(_channel);
        _adjudicationService = new AdjudicationService.AdjudicationServiceClient(_channel);
    }

    public string Address { get; }

    public async Task<IReadOnlyList<ClaimDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var claims = new List<ClaimDto>();
        using var call = _claimService.GetAll(new GetAllClaimsRequest(), cancellationToken: cancellationToken);
        await foreach (var claim in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            claims.Add(claim);
        }

        return claims;
    }

    public async Task<ClaimDto> FindByClaimIdAsync(long claimId, CancellationToken cancellationToken)
    {
        using var call = _claimService.FindByClaimIdAsync(
            new FindByClaimIdRequest { ClaimId = claimId }, cancellationToken: cancellationToken);
        return await call;
    }

    public async Task AdjudicateAsync(long claimId, CancellationToken cancellationToken)
    {
        using var call = _adjudicationService.AdjudicateAsync(
            new AdjudicationRequest { ClaimId = claimId }, cancellationToken: cancellationToken);
        await call;
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}
