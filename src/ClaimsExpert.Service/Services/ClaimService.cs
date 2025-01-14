using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Grpc.Core;
using Grpc.Core.Utils;
using NRules.Samples.ClaimsExpert.Contract;
using NRules.Samples.ClaimsExpert.Domain;

namespace NRules.Samples.ClaimsExpert.Service.Services;

public class ClaimServiceImpl : ClaimService.ClaimServiceBase
{
    private readonly IMapper _mapper;
    private readonly IClaimRepository _claimRepository;

    public ClaimServiceImpl(IMapper mapper, IClaimRepository claimRepository)
    {
        _mapper = mapper;
        _claimRepository = claimRepository;
    }

    public override async Task GetAll(GetAllClaimsRequest request, IServerStreamWriter<ClaimDto> responseStream, ServerCallContext context)
    {
        var claims = _claimRepository.GetAll().ToArray();
        var claimDtos = claims.Select(_mapper.Map<ClaimDto>).ToArray();
        await responseStream.WriteAllAsync(claimDtos);
    }

    public override Task<ClaimDto> FindByClaimId(FindByClaimIdRequest request, ServerCallContext context)
    {
        var claim = _claimRepository.GetById(request.ClaimId);
        return Task.FromResult(_mapper.Map<ClaimDto>(claim));
    }
}