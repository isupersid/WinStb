using WinStb.Models;

namespace WinStb.ViewModels
{
    public class PlayerViewModel : BaseViewModel
    {
        private string _streamUrl = "";
        private string _title = "";
        private bool _isPlaying;
        private bool _isLoading;

        public string StreamUrl
        {
            get => _streamUrl;
            set => SetProperty(ref _streamUrl, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
    }
}
