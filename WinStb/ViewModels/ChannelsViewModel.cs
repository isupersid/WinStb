using System.Collections.ObjectModel;
using WinStb.Models;

namespace WinStb.ViewModels
{
    public class ChannelsViewModel : BaseViewModel
    {
        private ObservableCollection<Genre> _genres;
        private ObservableCollection<Channel> _channels;
        private ObservableCollection<VodItem> _vodItems;
        private Genre _selectedGenre;
        private Channel _selectedChannel;
        private VodItem _selectedVodItem;
        private bool _isLoading;
        private string _selectedContentType;

        public ObservableCollection<Genre> Genres
        {
            get => _genres;
            set => SetProperty(ref _genres, value);
        }

        public ObservableCollection<Channel> Channels
        {
            get => _channels;
            set => SetProperty(ref _channels, value);
        }

        public ObservableCollection<VodItem> VodItems
        {
            get => _vodItems;
            set => SetProperty(ref _vodItems, value);
        }

        public Genre SelectedGenre
        {
            get => _selectedGenre;
            set => SetProperty(ref _selectedGenre, value);
        }

        public Channel SelectedChannel
        {
            get => _selectedChannel;
            set => SetProperty(ref _selectedChannel, value);
        }

        public VodItem SelectedVodItem
        {
            get => _selectedVodItem;
            set => SetProperty(ref _selectedVodItem, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string SelectedContentType
        {
            get => _selectedContentType;
            set => SetProperty(ref _selectedContentType, value);
        }

        public ChannelsViewModel()
        {
            Genres = new ObservableCollection<Genre>();
            Channels = new ObservableCollection<Channel>();
            VodItems = new ObservableCollection<VodItem>();
            SelectedContentType = "LiveTV";
        }
    }
}
