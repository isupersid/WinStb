# WinStb - Windows Stalker Portal Player

A Windows Store (UWP) application for connecting to Stalker Portal middleware and playing IPTV content.

## Features

- **Profile Management**: Create and manage multiple portal profiles with MAC address authentication
- **Live TV**: Browse and play live TV channels organized by genres
- **VOD**: Browse and play Video On Demand content (movies and series)
- **Fluid UI**: Modern Windows 10/11 interface with navigation and media controls
- **Stalker Protocol**: Full implementation of Stalker Portal API including:
  - MAC-based authentication
  - Token management
  - Channel/VOD retrieval
  - Stream URL generation
  - Keepalive/watchdog

## Project Structure

```
WinStb/
├── Models/              # Data models
│   ├── Profile.cs       # User profile with portal credentials
│   ├── Channel.cs       # Live TV channel
│   ├── Genre.cs         # Category/Genre
│   └── VodItem.cs       # VOD content item
├── Services/            # Business logic
│   ├── StalkerPortalClient.cs  # Stalker Portal API client
│   └── ProfileService.cs       # Profile management
├── ViewModels/          # MVVM ViewModels
│   ├── BaseViewModel.cs
│   ├── MainViewModel.cs
│   ├── ProfilesViewModel.cs
│   ├── ChannelsViewModel.cs
│   └── PlayerViewModel.cs
└── Views/               # XAML UI pages
    ├── MainPage.xaml
    ├── ProfilesPage.xaml
    ├── ChannelsPage.xaml
    └── PlayerPage.xaml
```

## Getting Started

### Prerequisites

- Visual Studio 2019 or later
- Windows 10 SDK (10.0.17763.0 or later)
- .NET Framework 4.7.2 or later

### Building the App

1. Open `WinStb.sln` in Visual Studio
2. Restore NuGet packages (should happen automatically)
3. **Create app assets** (see Assets section below)
4. Build the solution (Ctrl+Shift+B)
5. Run in Debug mode (F5)

### Assets Required

Before building, you need to create the following image assets in the `Assets` folder:

- `LockScreenLogo.scale-200.png` (400x400 px)
- `SplashScreen.scale-200.png` (1240x600 px)
- `Square150x150Logo.scale-200.png` (300x300 px)
- `Square44x44Logo.scale-200.png` (88x88 px)
- `Square44x44Logo.targetsize-24_altform-unplated.png` (24x24 px)
- `StoreLogo.png` (50x50 px)
- `Wide310x150Logo.scale-200.png` (620x300 px)

You can use any image editor or Visual Studio's built-in asset generator to create these.

**Quick way to create placeholder assets:**
1. Right-click on the `WinStb` project in Visual Studio
2. Select `Store` → `Create App Packages`
3. Visual Studio will help you generate all required assets

## How to Use

### 1. Create a Profile

1. Launch the app
2. Click "Add Profile"
3. Enter:
   - **Profile Name**: A friendly name for your service
   - **Portal URL**: Your Stalker Portal URL (e.g., `http://example.com/stalker_portal`)
   - **MAC Address**: Your device MAC address (format: `00:1A:79:XX:XX:XX`)
   - **Optional fields**: Serial Number, Device ID, STB Type (defaults to MAG254)
4. Click "Add"

### 2. Connect to Portal

1. In the Profiles page, click "Connect" next to your profile
2. Wait for authentication to complete
3. If successful, you'll see "Connected to [Profile Name]"

### 3. Watch Content

#### Live TV:
1. Click "Live TV" in the navigation menu
2. Select a category from the left sidebar (or "All Channels")
3. Click on any channel to start playing

#### VOD:
1. Click "VOD" in the navigation menu
2. Select a category from the left sidebar
3. Click on any movie/series to start playing

## Stalker Portal Protocol

The app implements the Stalker Portal API with the following features:

### Authentication Flow
1. **Handshake**: Initial request to get authentication token
2. **Token Storage**: Token is stored and used in all subsequent requests
3. **Profile Retrieval**: Get device profile information

### API Endpoints Implemented
- `handshake` - Get authentication token
- `get_profile` - Retrieve device profile
- `get_genres` - Get Live TV categories
- `get_ordered_list` (itv) - Get channels
- `get_categories` (vod) - Get VOD categories
- `get_ordered_list` (vod) - Get VOD items
- `create_link` - Get streaming URL for playback
- `watchdog` - Send keepalive during playback

### Authentication Headers
The client emulates a MAG set-top box with proper headers:
- `User-Agent`: MAG200 browser identification
- `X-User-Agent`: Device model and connection type
- `Cookie`: MAC address and timezone
- `Authorization`: Bearer token (after handshake)

## Technical Details

### Dependencies
- **Microsoft.NETCore.UniversalWindowsPlatform** (6.2.14): UWP runtime
- **Newtonsoft.Json** (13.0.3): JSON serialization

### Data Storage
- Profiles are stored in `ApplicationData.Current.LocalFolder`
- Files: `profiles.json` and `current_profile.json`

### Media Playback
- Uses `MediaPlayerElement` with `MediaPlayer` for streaming
- Supports HLS and direct stream URLs
- Automatic keepalive every 60 seconds during playback

## Customization

### MAC Address Format
The app generates MAC addresses in the format `00:1A:79:XX:XX:XX` which is recognized by Stalker middleware as MAG devices. You can customize this in `Profile.cs`.

### Device Emulation
Default STB type is **MAG254**. You can change this per-profile or modify the default in `Profile.cs`.

### Timeout Settings
HTTP timeout is set to 30 seconds in `StalkerPortalClient.cs`. Adjust if needed for slower connections.

## Troubleshooting

### Connection Failed
- Verify the portal URL is correct and includes `/stalker_portal` path
- Check if MAC address is whitelisted by your service provider
- Ensure your network allows IPTV traffic

### No Channels/VOD Loading
- Ensure you're connected to a profile first
- Check your subscription status with the provider
- Try refreshing the content list

### Playback Issues
- Verify the stream URL is accessible
- Check if your firewall allows media streaming
- Some streams may require specific codecs

## Publishing to Windows Store

To publish this app to the Microsoft Store:

1. Create a developer account at [partner.microsoft.com](https://partner.microsoft.com)
2. Reserve an app name
3. Update `Package.appxmanifest` with your publisher details
4. Create app packages: `Project` → `Store` → `Create App Packages`
5. Upload to Partner Center

## License

This project is provided as-is for educational purposes. Ensure you have proper authorization to access any IPTV service you connect to.

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests.

## Disclaimer

This application is designed for legitimate use with authorized Stalker Portal services. Users are responsible for ensuring they have proper rights and subscriptions for any content they access.
