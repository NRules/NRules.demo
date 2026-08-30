using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using NRules.Samples.ClaimsExpert.Contract;
using NRules.Samples.ClaimsExpert.Domain;
using NRules.Samples.ClaimsExpert.Service.Infrastructure;

namespace NRules.Samples.ClaimsExpert.Service.Services;

public class ClaimServiceImpl : ClaimService.ClaimServiceBase
{
    private readonly IClaimMapper _mapper;
    private readonly IClaimRepository _claimRepository;

    public ClaimServiceImpl(IClaimMapper mapper, IClaimRepository claimRepository)
    {
        _mapper = mapper;
        _claimRepository = claimRepository;
    }

    public override async Task GetAll(GetAllClaimsRequest request, IServerStreamWriter<ClaimDto> responseStream, ServerCallContext context)
    {
        var claims = _claimRepository.GetAll().ToArray();
        var claimDtos = claims.Select(_mapper.Map).ToArray();
        foreach (var claimDto in claimDtos)
        {
            await responseStream.WriteAsync(claimDto);
        }
    }

    public override Task<ClaimDto> FindByClaimId(FindByClaimIdRequest request, ServerCallContext context)
    {
        var claim = _claimRepository.GetById(request.ClaimId);
        return Task.FromResult(_mapper.Map(claim));
    }
}
