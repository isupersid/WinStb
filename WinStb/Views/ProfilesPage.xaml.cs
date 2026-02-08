using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WinStb.Models;
using WinStb.ViewModels;

namespace WinStb.Views
{
    public sealed partial class ProfilesPage : Page
    {
        public ProfilesViewModel LocalViewModel { get; }
        public MainViewModel MainViewModel { get; private set; }

        public ProfilesPage()
        {
            this.InitializeComponent();
            LocalViewModel = new ProfilesViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is MainViewModel mainViewModel)
            {
                MainViewModel = mainViewModel;
            }

            LocalViewModel.IsLoading = true;
            var profiles = await MainViewModel.ProfileService.GetProfilesAsync();
            LocalViewModel.Profiles.Clear();
            foreach (var profile in profiles)
            {
                LocalViewModel.Profiles.Add(profile);
            }
            LocalViewModel.IsLoading = false;
        }

        private async void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Add New Profile",
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var panel = new StackPanel { Spacing = 10 };

            var nameBox = new TextBox { Header = "Profile Name", PlaceholderText = "My IPTV Service" };
            var urlBox = new TextBox { Header = "Portal URL", PlaceholderText = "http://example.com/stalker_portal" };
            var macBox = new TextBox { Header = "MAC Address", PlaceholderText = "00:1A:79:XX:XX:XX" };
            var serialBox = new TextBox { Header = "Serial Number (Optional)" };
            var deviceIdBox = new TextBox { Header = "Device ID (Optional)" };
            var stbTypeBox = new TextBox { Header = "STB Type", Text = "MAG254" };

            panel.Children.Add(nameBox);
            panel.Children.Add(urlBox);
            panel.Children.Add(macBox);
            panel.Children.Add(serialBox);
            panel.Children.Add(deviceIdBox);
            panel.Children.Add(stbTypeBox);

            dialog.Content = panel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var profile = new Profile
                {
                    Name = nameBox.Text,
                    PortalUrl = urlBox.Text,
                    MacAddress = string.IsNullOrWhiteSpace(macBox.Text) ? null : macBox.Text,
                    SerialNumber = serialBox.Text,
                    DeviceId = deviceIdBox.Text,
                    StbType = stbTypeBox.Text
                };

                // If MAC address is empty, generate a default one
                if (string.IsNullOrWhiteSpace(profile.MacAddress))
                {
                    var random = new Random();
                    profile.MacAddress = $"00:1A:79:{random.Next(0, 256):X2}:{random.Next(0, 256):X2}:{random.Next(0, 256):X2}";
                }

                await MainViewModel.ProfileService.AddProfileAsync(profile);
                LocalViewModel.Profiles.Add(profile);
            }
        }

        private async void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            var profileId = (sender as Button)?.Tag?.ToString();
            var profile = LocalViewModel.Profiles.FirstOrDefault(p => p.Id == profileId);

            if (profile == null)
                return;

            var dialog = new ContentDialog
            {
                Title = "Edit Profile",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var panel = new StackPanel { Spacing = 10 };

            var nameBox = new TextBox { Header = "Profile Name", Text = profile.Name };
            var urlBox = new TextBox { Header = "Portal URL", Text = profile.PortalUrl };
            var macBox = new TextBox { Header = "MAC Address", Text = profile.MacAddress };
            var serialBox = new TextBox { Header = "Serial Number (Optional)", Text = profile.SerialNumber };
            var deviceIdBox = new TextBox { Header = "Device ID (Optional)", Text = profile.DeviceId };
            var stbTypeBox = new TextBox { Header = "STB Type", Text = profile.StbType };

            panel.Children.Add(nameBox);
            panel.Children.Add(urlBox);
            panel.Children.Add(macBox);
            panel.Children.Add(serialBox);
            panel.Children.Add(deviceIdBox);
            panel.Children.Add(stbTypeBox);

            dialog.Content = panel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                profile.Name = nameBox.Text;
                profile.PortalUrl = urlBox.Text;
                profile.MacAddress = macBox.Text;
                profile.SerialNumber = serialBox.Text;
                profile.DeviceId = deviceIdBox.Text;
                profile.StbType = stbTypeBox.Text;

                await MainViewModel.ProfileService.UpdateProfileAsync(profile);

                // Refresh the list
                var index = LocalViewModel.Profiles.IndexOf(profile);
                LocalViewModel.Profiles.RemoveAt(index);
                LocalViewModel.Profiles.Insert(index, profile);
            }
        }

        private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            var profileId = (sender as Button)?.Tag?.ToString();
            var profile = LocalViewModel.Profiles.FirstOrDefault(p => p.Id == profileId);

            if (profile == null)
                return;

            var dialog = new ContentDialog
            {
                Title = "Delete Profile",
                Content = $"Are you sure you want to delete '{profile.Name}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await MainViewModel.ProfileService.DeleteProfileAsync(profileId);
                LocalViewModel.Profiles.Remove(profile);
            }
        }

        private async void ConnectProfile_Click(object sender, RoutedEventArgs e)
        {
            var profileId = (sender as Button)?.Tag?.ToString();
            var profile = LocalViewModel.Profiles.FirstOrDefault(p => p.Id == profileId);

            if (profile == null)
                return;

            LocalViewModel.IsLoading = true;
            MainViewModel.StatusMessage = "Connecting...";

            try
            {
                var success = await MainViewModel.PortalClient.AuthenticateAsync(profile);

                if (success)
                {
                    MainViewModel.CurrentProfile = profile;
                    MainViewModel.IsConnected = true;
                    MainViewModel.StatusMessage = $"Connected to {profile.Name}";
                    await MainViewModel.ProfileService.SetCurrentProfileAsync(profile);

                    var successDialog = new ContentDialog
                    {
                        Title = "Success",
                        Content = $"Connected to {profile.Name}",
                        CloseButtonText = "OK"
                    };
                    await successDialog.ShowAsync();
                }
                else
                {
                    MainViewModel.StatusMessage = "Connection failed";

                    var errorMessage = MainViewModel.PortalClient.LastError ??
                        "Could not connect to the portal. Please check your URL and MAC address.";

                    var errorDialog = new ContentDialog
                    {
                        Title = "Connection Failed",
                        Content = errorMessage,
                        CloseButtonText = "OK"
                    };
                    await errorDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                MainViewModel.StatusMessage = "Connection error";

                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Error connecting: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await errorDialog.ShowAsync();
            }
            finally
            {
                LocalViewModel.IsLoading = false;
            }
        }
    }
}
