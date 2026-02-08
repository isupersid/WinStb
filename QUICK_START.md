# Quick Start Guide for WinStb

This guide will help you get the WinStb app up and running quickly.

## Prerequisites

✅ Visual Studio 2019 or later
✅ Windows 10 SDK (10.0.17763.0 or later)
✅ UWP development workload installed

## Step-by-Step Setup

### 1. Open the Solution

```
1. Navigate to: C:\Users\singhsidd\code\WinStb
2. Double-click WinStb.sln to open in Visual Studio
```

### 2. Restore NuGet Packages

Visual Studio should automatically restore packages. If not:
```
Right-click on the solution → Restore NuGet Packages
```

### 3. Select Build Configuration

In Visual Studio toolbar:
- Configuration: **Debug**
- Platform: **x64** (or x86 if you prefer)

### 4. Build the Solution

```
Build → Build Solution (Ctrl+Shift+B)
```

### 5. Run the App

```
Debug → Start Debugging (F5)
```

The app will launch in a few seconds!

## First Time Usage

### Creating Your First Profile

1. The app opens on the **Profiles** page
2. Click **"Add Profile"** button
3. Fill in the details:
   ```
   Profile Name: My IPTV Service
   Portal URL: http://your-portal.com/stalker_portal
   MAC Address: 00:1A:79:XX:XX:XX (auto-generated if left empty)
   STB Type: MAG254 (default)
   ```
4. Click **"Add"**

### Connecting to Portal

1. Click **"Connect"** next to your profile
2. Wait a few seconds for authentication
3. You should see "Connected to My IPTV Service"

### Watching Content

**Live TV:**
1. Click "Live TV" in the left navigation
2. Select a category (or "All Channels")
3. Click on any channel to start playing

**VOD:**
1. Click "VOD" in the left navigation
2. Browse movies/series by category
3. Click on any title to start playing

## Troubleshooting

### Build Errors

**Problem:** Missing references or NuGet errors
**Solution:**
```
Tools → NuGet Package Manager → Package Manager Console
Run: Update-Package -Reinstall
```

**Problem:** SDK version not found
**Solution:**
1. Right-click project → Properties
2. Application → Target version: Select your installed SDK version

### Connection Issues

**Problem:** "Connection Failed" when connecting to profile
**Solutions:**
- Verify the portal URL is correct
- Check if MAC address is whitelisted
- Try with HTTPS if HTTP fails
- Check firewall settings

**Problem:** No channels/VOD loading
**Solutions:**
- Ensure profile is connected (green status)
- Check your subscription is active
- Click "Refresh" to reload content

### Playback Issues

**Problem:** "Could not play the stream"
**Solutions:**
- Check internet connection
- Verify stream URL is accessible
- Try a different channel/content
- Check Windows Media Player codecs

**Problem:** Black screen or loading forever
**Solutions:**
- Stop and restart playback
- Reconnect to the profile
- Check if the content source is live

## Development Tips

### Debugging

To see detailed logs while running:
```
View → Output Window (Ctrl+Alt+O)
Select: Debug from the dropdown
```

### Testing with Different Portals

Create multiple profiles to test different Stalker Portal servers:
```
Profiles → Add Profile → Fill details → Connect
```

### Modifying the UI

XAML files are in: `WinStb\Views\*.xaml`
Code-behind files: `WinStb\Views\*.xaml.cs`

Live XAML preview:
```
View → Other Windows → XAML Designer
```

### Changing API Behavior

Stalker Portal client: `WinStb\Services\StalkerPortalClient.cs`

## Common Customizations

### Change Default STB Type

Edit: `WinStb\Models\Profile.cs`
```csharp
StbType = "MAG322"; // Instead of MAG254
```

### Adjust HTTP Timeout

Edit: `WinStb\Services\StalkerPortalClient.cs`
```csharp
_httpClient.Timeout = TimeSpan.FromSeconds(60); // Instead of 30
```

### Modify Keepalive Interval

Edit: `WinStb\Views\PlayerPage.xaml.cs`
```csharp
Interval = TimeSpan.FromSeconds(30) // Instead of 60
```

## Building for Release

### Create Release Build

1. Configuration: **Release**
2. Platform: **x64**, **x86**, **ARM** (as needed)
3. Build → Build Solution

### Create App Package

1. Project → Store → Create App Packages
2. Select "Sideloading" or "Microsoft Store"
3. Select architectures (x86, x64, ARM)
4. Click "Create"

Package will be in: `WinStb\AppPackages\`

### Install on Another PC

1. Copy the AppPackages folder to target PC
2. Right-click the `.ps1` file → Run with PowerShell
3. Or manually install the `.appx` file

## Getting Help

### Resources

- **Project README**: See README.md for detailed documentation
- **Assets Guide**: See ASSETS_SETUP.md for customizing app icons
- **Stalker Protocol**: Research from public implementations

### Common Files

| File | Purpose |
|------|---------|
| `StalkerPortalClient.cs` | API communication |
| `ProfileService.cs` | Profile storage |
| `MainPage.xaml` | Main navigation |
| `ProfilesPage.xaml` | Profile management |
| `ChannelsPage.xaml` | Content browsing |
| `PlayerPage.xaml` | Video playback |

## Next Steps

1. ✅ Build and run the app
2. ✅ Create a test profile
3. ✅ Connect and browse content
4. 🎨 Customize the UI to your liking
5. 📦 Build release version
6. 🚀 Publish to Microsoft Store (optional)

---

**Need more help?** Check out the full README.md or the official UWP documentation at https://docs.microsoft.com/windows/uwp/
