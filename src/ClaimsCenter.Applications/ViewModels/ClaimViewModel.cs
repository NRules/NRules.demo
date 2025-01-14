using System.Linq;
using System.Waf.Applications;
using System.Windows.Input;
using NRules.Samples.ClaimsCenter.Applications.Views;
using NRules.Samples.ClaimsExpert.Contract;

namespace NRules.Samples.ClaimsCenter.Applications.ViewModels;

public class ClaimViewModel : ViewModel<IClaimView>
{
    private ClaimDto? _claim;
    private ICommand? _adjudicateCommand;

    public ClaimViewModel(IClaimView view)
        : base(view)
    {
    }

    public ClaimDto? Claim
    {
        get => _claim;
        set
        {
            if (SetProperty(ref _claim, value))
            {
                RaisePropertyChanged(nameof(IsEnabled));
                RaisePropertyChanged(nameof(HasAlerts));
            }
        }
    }

    public ICommand? AdjudicateCommand
    {
        get => _adjudicateCommand;
        set => SetProperty(ref _adjudicateCommand, value);
    }

    public bool IsEnabled => Claim != null;
    public bool HasAlerts => Claim?.Alerts?.Any() == true;
}