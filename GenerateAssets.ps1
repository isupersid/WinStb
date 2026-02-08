# PowerShell script to generate placeholder assets for WinStb UWP app
# Run this script to quickly create all required image assets

$assetsFolder = Join-Path $PSScriptRoot "WinStb\Assets"

# Create Assets folder if it doesn't exist
if (-not (Test-Path $assetsFolder)) {
    New-Item -Path $assetsFolder -ItemType Directory -Force | Out-Null
    Write-Host "Created Assets folder: $assetsFolder" -ForegroundColor Green
}

# Load System.Drawing assembly
Add-Type -AssemblyName System.Drawing

function Create-PlaceholderImage {
    param(
        [int]$Width,
        [int]$Height,
        [string]$Filename
    )

    $filepath = Join-Path $assetsFolder $Filename

    # Create bitmap
    $bitmap = New-Object System.Drawing.Bitmap $Width, $Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    # Set high quality rendering
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Fill with Windows blue gradient
    $color1 = [System.Drawing.Color]::FromArgb(0, 120, 212)  # Windows blue
    $color2 = [System.Drawing.Color]::FromArgb(0, 90, 158)   # Darker blue

    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point 0, 0),
        (New-Object System.Drawing.Point $Width, $Height),
        $color1,
        $color2
    )

    $graphics.FillRectangle($brush, 0, 0, $Width, $Height)

    # Draw a simple TV/Play icon in the center
    $centerX = $Width / 2
    $centerY = $Height / 2
    $iconSize = [Math]::Min($Width, $Height) * 0.4

    # Draw TV screen rectangle
    $rectX = $centerX - ($iconSize / 2)
    $rectY = $centerY - ($iconSize / 2)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [Math]::Max(2, $iconSize / 20))
    $graphics.DrawRectangle($pen, $rectX, $rectY, $iconSize, $iconSize * 0.7)

    # Draw play triangle
    $triangleSize = $iconSize * 0.3
    $triangle = @(
        (New-Object System.Drawing.Point ($centerX - $triangleSize/3), ($centerY - $triangleSize/2)),
        (New-Object System.Drawing.Point ($centerX - $triangleSize/3), ($centerY + $triangleSize/2)),
        (New-Object System.Drawing.Point ($centerX + $triangleSize*2/3), $centerY)
    )
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $graphics.FillPolygon($whiteBrush, $triangle)

    # Save the image
    $bitmap.Save($filepath, [System.Drawing.Imaging.ImageFormat]::Png)

    # Cleanup
    $bitmap.Dispose()
    $graphics.Dispose()
    $brush.Dispose()
    $pen.Dispose()
    $whiteBrush.Dispose()

    Write-Host "Created: $Filename ($Width x $Height)" -ForegroundColor Cyan
}

# Generate all required assets
Write-Host "`nGenerating WinStb assets...`n" -ForegroundColor Yellow

Create-PlaceholderImage -Width 400 -Height 400 -Filename "LockScreenLogo.scale-200.png"
Create-PlaceholderImage -Width 1240 -Height 600 -Filename "SplashScreen.scale-200.png"
Create-PlaceholderImage -Width 300 -Height 300 -Filename "Square150x150Logo.scale-200.png"
Create-PlaceholderImage -Width 88 -Height 88 -Filename "Square44x44Logo.scale-200.png"
Create-PlaceholderImage -Width 24 -Height 24 -Filename "Square44x44Logo.targetsize-24_altform-unplated.png"
Create-PlaceholderImage -Width 50 -Height 50 -Filename "StoreLogo.png"
Create-PlaceholderImage -Width 620 -Height 300 -Filename "Wide310x150Logo.scale-200.png"

Write-Host "`n✓ All assets generated successfully!" -ForegroundColor Green
Write-Host "Assets location: $assetsFolder`n" -ForegroundColor Gray
