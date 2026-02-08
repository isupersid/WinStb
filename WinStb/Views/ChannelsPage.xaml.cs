using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WinStb.Models;
using WinStb.ViewModels;

namespace WinStb.Views
{
    public sealed partial class ChannelsPage : Page
    {
        public ChannelsViewModel LocalViewModel { get; }
        public MainViewModel MainViewModel { get; private set; }

        public ChannelsPage()
        {
            this.InitializeComponent();
            LocalViewModel = new ChannelsViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is object[] parameters && parameters.Length == 2)
            {
                MainViewModel = parameters[0] as MainViewModel;
                var contentType = parameters[1] as string;
                LocalViewModel.SelectedContentType = contentType;

                // Set visibility based on content type
                if (contentType == "LiveTV")
                {
                    LiveTVGrid.Visibility = Visibility.Visible;
                    VODGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LiveTVGrid.Visibility = Visibility.Collapsed;
                    VODGrid.Visibility = Visibility.Visible;
                }
            }

            await LoadDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDataAsync(bool forceRefresh = false)
        {
            LocalViewModel.IsLoading = true;

            try
            {
                if (LocalViewModel.SelectedContentType == "LiveTV")
                {
                    // Load genres
                    var genres = await MainViewModel.PortalClient.GetGenresAsync();
                    LocalViewModel.Genres.Clear();

                    // Add "All" option
                    LocalViewModel.Genres.Add(new Genre { Id = "*", Title = "All Channels" });

                    foreach (var genre in genres)
                    {
                        LocalViewModel.Genres.Add(genre);
                    }

                    // Select "All" by default
                    if (LocalViewModel.Genres.Count > 0)
                    {
                        LocalViewModel.SelectedGenre = LocalViewModel.Genres[0];
                    }

                    // Load all channels
                    await LoadChannelsAsync(forceRefresh);
                }
                else if (LocalViewModel.SelectedContentType == "VOD")
                {
                    // Load VOD categories
                    var categories = await MainViewModel.PortalClient.GetVodCategoriesAsync();
                    LocalViewModel.Genres.Clear();

                    // Add "All" option
                    LocalViewModel.Genres.Add(new Genre { Id = "*", Title = "All Movies/Series" });

                    foreach (var category in categories)
                    {
                        LocalViewModel.Genres.Add(category);
                    }

                    // Select "All" by default
                    if (LocalViewModel.Genres.Count > 0)
                    {
                        LocalViewModel.SelectedGenre = LocalViewModel.Genres[0];
                    }

                    // Load VOD items
                    await LoadVodItemsAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Error loading content: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
            finally
            {
                LocalViewModel.IsLoading = false;
            }
        }

        private bool _isLoadingChannels = false;

        private async System.Threading.Tasks.Task LoadChannelsAsync(bool forceRefresh = false)
        {
            // Prevent concurrent calls
            if (_isLoadingChannels)
            {
                System.Diagnostics.Debug.WriteLine("LoadChannelsAsync already in progress, skipping duplicate call");
                return;
            }

            _isLoadingChannels = true;
            LocalViewModel.IsLoading = true;

            try
            {
                var genreId = LocalViewModel.SelectedGenre?.Id;
                var channels = await MainViewModel.PortalClient.GetAllChannelsAsync(forceRefresh);

                System.Diagnostics.Debug.WriteLine($"Loaded {channels.Count} channels total");

                // Filter by genre if not "All"
                if (!string.IsNullOrEmpty(genreId) && genreId != "*")
                {
                    channels = channels.Where(c => c.GenreTitle == LocalViewModel.SelectedGenre.Title).ToList();
                    System.Diagnostics.Debug.WriteLine($"Filtered to {channels.Count} channels for genre {LocalViewModel.SelectedGenre.Title}");
                }

                // Update UI collection on the UI thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    LocalViewModel.Channels.Clear();

                    foreach (var channel in channels)
                    {
                        LocalViewModel.Channels.Add(channel);
                    }

                    System.Diagnostics.Debug.WriteLine($"Added {LocalViewModel.Channels.Count} channels to UI");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadChannelsAsync error: {ex.GetType().Name} - {ex.Message}");
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Error loading channels: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
            finally
            {
                LocalViewModel.IsLoading = false;
                _isLoadingChannels = false;
            }
        }

        private bool _isLoadingVodItems = false;

        private async System.Threading.Tasks.Task LoadVodItemsAsync()
        {
            // Prevent concurrent calls
            if (_isLoadingVodItems)
            {
                System.Diagnostics.Debug.WriteLine("LoadVodItemsAsync already in progress, skipping duplicate call");
                return;
            }

            _isLoadingVodItems = true;
            LocalViewModel.IsLoading = true;

            try
            {
                var categoryId = LocalViewModel.SelectedGenre?.Id;

                // Load all pages
                var allItems = new System.Collections.Generic.List<VodItem>();
                var page = 0;
                var hasMorePages = true;

                while (hasMorePages && page < 10) // Limit to 10 pages for now
                {
                    var items = await MainViewModel.PortalClient.GetVodItemsAsync(categoryId, page);

                    if (items.Count == 0)
                    {
                        hasMorePages = false;
                    }
                    else
                    {
                        allItems.AddRange(items);
                        page++;

                        if (items.Count < 14) // Less than full page
                        {
                            hasMorePages = false;
                        }
                    }
                }

                // Update UI collection on the UI thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    LocalViewModel.VodItems.Clear();
                    foreach (var item in allItems)
                    {
                        LocalViewModel.VodItems.Add(item);
                    }

                    System.Diagnostics.Debug.WriteLine($"Loaded {LocalViewModel.VodItems.Count} VOD items");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadVodItemsAsync error: {ex.GetType().Name} - {ex.Message}");
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Error loading VOD items: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
            finally
            {
                LocalViewModel.IsLoading = false;
                _isLoadingVodItems = false;
            }
        }

        private async void Genre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LocalViewModel.SelectedGenre == null)
                return;

            if (LocalViewModel.SelectedContentType == "LiveTV")
            {
                await LoadChannelsAsync();
            }
            else if (LocalViewModel.SelectedContentType == "VOD")
            {
                await LoadVodItemsAsync();
            }
        }

        private async void Channel_Click(object sender, ItemClickEventArgs e)
        {
            var channel = e.ClickedItem as Channel;
            if (channel == null)
                return;

            try
            {
                var streamUrl = await MainViewModel.PortalClient.CreateLinkAsync(channel.Cmd, false);

                if (!string.IsNullOrEmpty(streamUrl))
                {
                    this.Frame.Navigate(typeof(PlayerPage), new object[] { MainViewModel, streamUrl, channel.Name });
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = "Could not get stream URL",
                        CloseButtonText = "OK"
                    };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Error playing channel: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }

        private async void VodItem_Click(object sender, ItemClickEventArgs e)
        {
            var vodItem = e.ClickedItem as VodItem;
            if (vodItem == null)
                return;

            try
            {
                var streamUrl = await MainViewModel.PortalClient.CreateLinkAsync(vodItem.Cmd, true);

                if (!string.IsNullOrEmpty(streamUrl))
                {
                    this.Frame.Navigate(typeof(PlayerPage), new object[] { MainViewModel, streamUrl, vodItem.Name });
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = "Could not get stream URL",
                        CloseButtonText = "OK"
                    };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Error playing content: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Refresh button clicked - forcing cache refresh");
            await LoadDataAsync(forceRefresh: true);
        }
    }
}
