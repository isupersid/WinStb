using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinStb.ViewModels;

namespace WinStb.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }

        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            // Navigate to Profiles page by default
            ContentFrame.Navigate(typeof(ProfilesPage), ViewModel);
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                string tag = item.Tag?.ToString();

                switch (tag)
                {
                    case "Profiles":
                        ContentFrame.Navigate(typeof(ProfilesPage), ViewModel);
                        break;
                    case "LiveTV":
                    case "VOD":
                        if (!ViewModel.IsConnected)
                        {
                            ShowDialog("Please select and connect to a profile first");
                            NavView.SelectedItem = NavView.MenuItems[0];
                            return;
                        }
                        ContentFrame.Navigate(typeof(ChannelsPage), new object[] { ViewModel, tag });
                        break;
                }
            }
        }

        private async void ShowDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Notice",
                Content = message,
                CloseButtonText = "OK"
            };

            await dialog.ShowAsync();
        }
    }
}
