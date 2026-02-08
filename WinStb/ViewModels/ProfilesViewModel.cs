using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WinStb.Models;

namespace WinStb.ViewModels
{
    public class ProfilesViewModel : BaseViewModel
    {
        private ObservableCollection<Profile> _profiles;
        private Profile _selectedProfile;
        private bool _isLoading;

        public ObservableCollection<Profile> Profiles
        {
            get => _profiles;
            set => SetProperty(ref _profiles, value);
        }

        public Profile SelectedProfile
        {
            get => _selectedProfile;
            set => SetProperty(ref _selectedProfile, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ProfilesViewModel()
        {
            Profiles = new ObservableCollection<Profile>();
        }
    }
}
