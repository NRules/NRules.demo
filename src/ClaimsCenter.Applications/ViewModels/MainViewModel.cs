using System;
using System.ComponentModel;
using System.Globalization;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using System.Windows;
using System.Windows.Input;
using NRules.Samples.ClaimsCenter.Applications.Properties;
using NRules.Samples.ClaimsCenter.Applications.Views;

namespace NRules.Samples.ClaimsCenter.Applications.ViewModels
{
    public class MainViewModel : ViewModel<IMainView>
    {
        private readonly IMessageService _messageService;
        private readonly AppSettings _settings;
        private readonly Lazy<ClaimListViewModel> _claimListViewModel;
        private readonly Lazy<ClaimViewModel> _claimViewModel;
        private readonly ICommand _aboutCommand;

        public MainViewModel(IMainView view, IMessageService messageService, ISettingsService settingsService,
            Lazy<ClaimListViewModel> claimListViewModel, Lazy<ClaimViewModel> claimViewModel)
            : base(view)
        {
            _messageService = messageService;
            _settings = settingsService.Get<AppSettings>();
            _claimListViewModel = claimListViewModel;
            _claimViewModel = claimViewModel;
            view.Closing += ViewClosing;
            view.Closed += ViewClosed;

            if (_settings.Left >= 0 && _settings.Top >= 0 && _settings.Width > 0 && _settings.Height > 0
                && _settings.Left + _settings.Width <= SystemParameters.VirtualScreenWidth
                && _settings.Top + _settings.Height <= SystemParameters.VirtualScreenHeight)
            {
                ViewCore.Left = _settings.Left;
                ViewCore.Top = _settings.Top;
                ViewCore.Height = _settings.Height;
                ViewCore.Width = _settings.Width;
            }
            ViewCore.IsMaximized = _settings.IsMaximized;

            _aboutCommand = new DelegateCommand(ShowAboutMessage);
        }

        public ICommand AboutCommand => _aboutCommand;
        public ICommand RefreshCommand { get; set; }

        public string Title => ApplicationInfo.ProductName;

        public object ClaimListView => _claimListViewModel.Value.View;
        public object ClaimView => _claimViewModel.Value.View;

        public event CancelEventHandler Closing;

        public void Show()
        {
            ViewCore.Show();
        }

        public void Close()
        {
            ViewCore.Close();
        }

        protected virtual void OnClosing(CancelEventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        private void ViewClosing(object sender, CancelEventArgs e)
        {
            OnClosing(e);
        }

        private void ViewClosed(object sender, EventArgs e)
        {
            _settings.Left = ViewCore.Left;
            _settings.Top = ViewCore.Top;
            _settings.Height = ViewCore.Height;
            _settings.Width = ViewCore.Width;
            _settings.IsMaximized = ViewCore.IsMaximized;
        }

        private void ShowAboutMessage()
        {
            _messageService.ShowMessage(View, string.Format(CultureInfo.CurrentCulture, Resources.AboutText,
                ApplicationInfo.ProductName, ApplicationInfo.Version));
        }
    }
}
