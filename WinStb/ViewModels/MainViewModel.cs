using System.Collections.ObjectModel;
using WinStb.Models;
using WinStb.Services;

namespace WinStb.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private Profile _currentProfile;
        private bool _isConnected;
        private string _statusMessage;

        public Profile CurrentProfile
        {
            get => _currentProfile;
            set => SetProperty(ref _currentProfile, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public StalkerPortalClient PortalClient { get; }
        public ProfileService ProfileService { get; }

        public MainViewModel()
        {
            PortalClient = new StalkerPortalClient();
            ProfileService = new ProfileService();
            StatusMessage = "No profile selected";
        }
    }
}
