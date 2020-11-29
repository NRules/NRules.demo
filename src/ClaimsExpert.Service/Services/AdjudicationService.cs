using System.Threading.Tasks;
using Grpc.Core;
using NRules.Samples.ClaimsExpert.Contract;
using NRules.Samples.ClaimsExpert.Domain;
using Serilog;

namespace NRules.Samples.ClaimsExpert.Service.Services
{
    public class AdjudicationServiceImpl : AdjudicationService.AdjudicationServiceBase
    {
        private readonly ISessionFactory _sessionFactory;
        private readonly IClaimRepository _claimRepository;

        public AdjudicationServiceImpl(ISessionFactory sessionFactory, IClaimRepository claimRepository)
        {
            _sessionFactory = sessionFactory;
            _claimRepository = claimRepository;
        }

        public override Task<AdjudicationResponse> Adjudicate(AdjudicationRequest request, ServerCallContext context)
        {
            var claim = _claimRepository.GetById(request.ClaimId);
            Log.Information("Adjudicating claim. ClaimId={0}", request.ClaimId);

            claim.Alerts.Clear();
            claim.Status = ClaimStatus.Open;

            ISession session = _sessionFactory.CreateSession();
            session.Insert(claim);
            session.Insert(claim.Patient);
            session.Insert(claim.Insured);
            session.Fire();

            var alerts = session.Query<ClaimAlert>();

            foreach (var alert in alerts)
            {
                claim.Alerts.Add(alert);
            }
            _claimRepository.Save(claim);

            return Task.FromResult(new AdjudicationResponse());
        }
    }
}