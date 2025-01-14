using NRules.Samples.ClaimsExpert.Domain;
using NRules.Samples.ClaimsExpert.Rules;
using Serilog;

namespace NRules.Samples.ClaimsExpert.Service.Services;

public class NotificationService : INotificationService
{
    public void ClaimDenied(Claim claim)
    {
        Log.Warning("Notification, claim denied. ClaimId={0}", claim.Id);
    }
}