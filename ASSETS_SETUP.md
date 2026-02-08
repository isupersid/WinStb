# Assets Setup Guide

This file explains how to create the required image assets for the WinStb UWP application.

## Required Assets

The following image files are required in the `WinStb\Assets` folder:

| File Name | Size | Description |
|-----------|------|-------------|
| LockScreenLogo.scale-200.png | 400x400 px | Lock screen logo |
| SplashScreen.scale-200.png | 1240x600 px | Splash screen shown on app launch |
| Square150x150Logo.scale-200.png | 300x300 px | Medium tile logo |
| Square44x44Logo.scale-200.png | 88x88 px | Small tile and app list logo |
| Square44x44Logo.targetsize-24_altform-unplated.png | 24x24 px | Small icon |
| StoreLogo.png | 50x50 px | Store listing logo |
| Wide310x150Logo.scale-200.png | 620x300 px | Wide tile logo |

## Option 1: Using Visual Studio Asset Generator (Recommended)

1. Open the solution in Visual Studio
2. Right-click on the `WinStb` project in Solution Explorer
3. Select `Store` → `Create App Packages`
4. Choose "I want to create packages for sideloading"
5. In the dialog, click on "Asset Generator"
6. Provide a source image (ideally 400x400 px or larger)
7. Visual Studio will automatically generate all required sizes
8. Click "Generate" and the assets will be created in the Assets folder

## Option 2: Manual Creation

You can create these assets manually using any image editor (Photoshop, GIMP, Paint.NET, etc.).

### Design Guidelines

- **Background Color**: Use transparent background or a solid color (e.g., #0078D4 - Windows blue)
- **Icon Content**: Simple, recognizable symbol related to TV/streaming
- **Padding**: Leave 20% padding around the main icon content
- **Format**: PNG with transparency

### Simple Icon Ideas

Since this is a Stalker Portal/IPTV player app, consider these icon themes:
- TV screen with play button
- Satellite dish
- TV antenna
- Streaming symbol (circles with waves)
- Play button with TV elements

## Option 3: Quick Placeholder Assets

For quick testing, you can create solid-color placeholder images:

### Using PowerShell (Windows)

```powershell
# Navigate to the Assets folder
cd "C:\Users\singhsidd\code\WinStb\WinStb\Assets"

# Create placeholder images using PowerShell and .NET
Add-Type -AssemblyName System.Drawing

function Create-PlaceholderImage($width, $height, $filename) {
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    # Fill with Windows blue color
    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(0, 120, 212))
    $graphics.FillRectangle($brush, 0, 0, $width, $height)

    $bitmap.Save($filename, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    $graphics.Dispose()
}

# Create all required assets
Create-PlaceholderImage 400 400 "LockScreenLogo.scale-200.png"
Create-PlaceholderImage 1240 600 "SplashScreen.scale-200.png"
Create-PlaceholderImage 300 300 "Square150x150Logo.scale-200.png"
Create-PlaceholderImage 88 88 "Square44x44Logo.scale-200.png"
Create-PlaceholderImage 24 24 "Square44x44Logo.targetsize-24_altform-unplated.png"
Create-PlaceholderImage 50 50 "StoreLogo.png"
Create-PlaceholderImage 620 300 "Wide310x150Logo.scale-200.png"
```

### Using Python (if installed)

```python
from PIL import Image

def create_placeholder(width, height, filename):
    img = Image.new('RGB', (width, height), color='#0078D4')
    img.save(filename)

# Create all required assets
create_placeholder(400, 400, "LockScreenLogo.scale-200.png")
create_placeholder(1240, 600, "SplashScreen.scale-200.png")
create_placeholder(300, 300, "Square150x150Logo.scale-200.png")
create_placeholder(88, 88, "Square44x44Logo.scale-200.png")
create_placeholder(24, 24, "Square44x44Logo.targetsize-24_altform-unplated.png")
create_placeholder(50, 50, "StoreLogo.png")
create_placeholder(620, 300, "Wide310x150Logo.scale-200.png")
```

## Option 4: Download Free Icons

You can download free icon templates from:

- [Windows App Studio Assets Generator](https://appstudio.windows.com/en-us/home)
- [IconArchive](http://www.iconarchive.com/)
- [Flaticon](https://www.flaticon.com/)
- [Icons8](https://icons8.com/)

Remember to check the license before using any downloaded icons commercially.

## After Creating Assets

1. Place all generated PNG files in the `WinStb\Assets` folder
2. Rebuild the project
3. The assets will be automatically included in the app package

## Verification

To verify your assets are correct:

1. Open `Package.appxmanifest` in Visual Studio
2. Go to the "Visual Assets" tab
3. You should see all asset images displayed correctly
4. Any missing or incorrect assets will show a warning icon

If you see warnings, regenerate the specific assets that are missing or incorrect.
