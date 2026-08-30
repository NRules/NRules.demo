using System;
using System.Linq;
using NRules.Samples.ClaimsExpert.Contract;
using NRules.Samples.ClaimsExpert.Domain;

namespace NRules.Samples.ClaimsExpert.Service.Infrastructure;

public interface IClaimMapper
{
    ClaimDto Map(Claim claim);
}

/// <summary>
/// Projects domain claims onto the gRPC contract. Protobuf string fields reject null, so every
/// optional value is coalesced to an empty string.
/// </summary>
public class ClaimMapper : IClaimMapper
{
    public ClaimDto Map(Claim claim)
    {
        var name = claim.Patient?.Name ?? Name.Empty;
        var address = claim.Patient?.Address ?? Address.Empty;

        var claimDto = new ClaimDto
        {
            Id = claim.Id,
            ClaimType = claim.ClaimType.ToString(),
            Status = MapStatus(claim.Status),
            PatientFirstName = name.FirstName ?? String.Empty,
            PatientMiddleName = name.MiddleName ?? String.Empty,
            PatientLastName = name.LastName ?? String.Empty,
            PatientAddressLine1 = address.Line1 ?? String.Empty,
            PatientAddressLine2 = address.Line2 ?? String.Empty,
            PatientAddressCity = address.City ?? String.Empty,
            PatientAddressState = address.State ?? String.Empty,
            PatientAddressZip = address.Zip ?? String.Empty,
        };

        if (claim.Alerts != null)
        {
            claimDto.Alerts.AddRange(claim.Alerts.Select(Map));
        }

        return claimDto;
    }

    private static ClaimAlertDto Map(ClaimAlert alert)
    {
        return new ClaimAlertDto
        {
            Message = alert.Message ?? String.Empty,
            RuleName = alert.RuleName ?? String.Empty,
        };
    }

    private static AdjudicationStatus MapStatus(ClaimStatus status)
    {
        return Enum.TryParse(status.ToString(), ignoreCase: true, out AdjudicationStatus result)
            ? result
            : AdjudicationStatus.Open;
    }
}
