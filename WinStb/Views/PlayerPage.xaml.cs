using System;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WinStb.ViewModels;

namespace WinStb.Views
{
    public sealed partial class PlayerPage : Page
    {
        public PlayerViewModel LocalViewModel { get; }
        public MainViewModel MainViewModel { get; private set; }
        private DispatcherTimer _watchdogTimer;

        public PlayerPage()
        {
            this.InitializeComponent();
            LocalViewModel = new PlayerViewModel();

            // Setup watchdog timer (send keepalive every 60 seconds)
            _watchdogTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            _watchdogTimer.Tick += WatchdogTimer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is object[] parameters && parameters.Length == 3)
            {
                MainViewModel = parameters[0] as MainViewModel;
                LocalViewModel.StreamUrl = parameters[1] as string;
                LocalViewModel.Title = parameters[2] as string;

                PlayStream();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Stop watchdog timer
            _watchdogTimer?.Stop();

            // Stop and cleanup media player
            if (MediaPlayer.MediaPlayer != null)
            {
                MediaPlayer.MediaPlayer.Pause();
                MediaPlayer.MediaPlayer.Source = null;
            }
        }

        private async void PlayStream()
        {
            LoadingPanel.Visibility = Visibility.Visible;

            try
            {
                var mediaPlayer = new MediaPlayer();

                // Create media source from URL
                var mediaSource = MediaSource.CreateFromUri(new Uri(LocalViewModel.StreamUrl));
                mediaPlayer.Source = mediaSource;

                // Set media player
                MediaPlayer.SetMediaPlayer(mediaPlayer);

                // Start playback
                mediaPlayer.Play();
                LocalViewModel.IsPlaying = true;

                // Start watchdog timer
                _watchdogTimer.Start();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Playback Error",
                    Content = $"Could not play the stream: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void WatchdogTimer_Tick(object sender, object e)
        {
            // Send watchdog keepalive to the portal
            try
            {
                await MainViewModel.PortalClient.SendWatchdogAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Watchdog error: {ex.Message}");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
