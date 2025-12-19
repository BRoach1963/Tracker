# Create WiX installer images with proper dimensions
# Banner: 493x58 (top banner - shows during install)
# Dialog: 493x312 (left sidebar on welcome/finish dialogs)

Add-Type -AssemblyName System.Drawing

# Colors - Prickly Cactus green theme
$darkGreen = [System.Drawing.Color]::FromArgb(27, 94, 32)
$mediumGreen = [System.Drawing.Color]::FromArgb(46, 125, 50)
$lightGreen = [System.Drawing.Color]::FromArgb(129, 199, 132)
$white = [System.Drawing.Color]::White

# ============================================
# BANNER: 493x58 - Top banner during install
# Keep it simple - just a small accent on the left side
# ============================================
$banner = New-Object System.Drawing.Bitmap(493, 58)
$g = [System.Drawing.Graphics]::FromImage($banner)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

# White background (Windows default)
$g.Clear($white)

# Green accent strip on left (narrow)
$accentBrush = New-Object System.Drawing.SolidBrush($mediumGreen)
$g.FillRectangle($accentBrush, 0, 0, 8, 58)

# Small cactus icon area
$iconBrush = New-Object System.Drawing.SolidBrush($lightGreen)
$g.FillEllipse($iconBrush, 15, 15, 28, 28)
$g.FillEllipse($accentBrush, 18, 18, 22, 22)

# Company name
$font = New-Object System.Drawing.Font("Segoe UI Semibold", 11)
$textBrush = New-Object System.Drawing.SolidBrush($darkGreen)
$g.DrawString("Prickly Cactus Software", $font, $textBrush, 50, 19)

$banner.Save("$PSScriptRoot\Banner.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$g.Dispose()
$banner.Dispose()
Write-Host "Banner.bmp created (493x58)"

# ============================================
# DIALOG: 493x312 - Left sidebar on welcome/finish
# Gradient with branding
# ============================================
$dialog = New-Object System.Drawing.Bitmap(493, 312)
$g2 = [System.Drawing.Graphics]::FromImage($dialog)
$g2.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g2.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

# Gradient background (vertical)
$gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point(0, 312)),
    $darkGreen,
    $mediumGreen
)
$g2.FillRectangle($gradientBrush, 0, 0, 493, 312)

# Decorative lighter overlay at bottom
$overlayBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(30, 255, 255, 255))
$g2.FillRectangle($overlayBrush, 0, 250, 493, 62)

# Simple cactus silhouettes at bottom
$cactusBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(80, 0, 0, 0))

# Cactus 1 - tall
$g2.FillRectangle($cactusBrush, 50, 220, 20, 92)
$g2.FillEllipse($cactusBrush, 45, 210, 30, 30)
$g2.FillRectangle($cactusBrush, 30, 240, 15, 40)
$g2.FillEllipse($cactusBrush, 27, 235, 20, 20)
$g2.FillRectangle($cactusBrush, 70, 250, 15, 30)
$g2.FillEllipse($cactusBrush, 67, 245, 20, 20)

# Cactus 2 - short round
$g2.FillEllipse($cactusBrush, 110, 260, 35, 52)

# Cactus 3 - medium
$g2.FillRectangle($cactusBrush, 160, 240, 18, 72)
$g2.FillEllipse($cactusBrush, 155, 232, 28, 28)

# Title text
$titleFont = New-Object System.Drawing.Font("Segoe UI", 24, [System.Drawing.FontStyle]::Bold)
$subFont = New-Object System.Drawing.Font("Segoe UI", 12)
$smallFont = New-Object System.Drawing.Font("Segoe UI", 9)
$whiteBrush = [System.Drawing.Brushes]::White
$lightBrush = New-Object System.Drawing.SolidBrush($lightGreen)

$g2.DrawString("Tracker", $titleFont, $whiteBrush, 30, 40)
$g2.DrawString("Team Management", $subFont, $lightBrush, 32, 80)
$g2.DrawString("Application", $subFont, $lightBrush, 32, 100)

# Version/company at bottom
$g2.DrawString("Prickly Cactus Software", $smallFont, $whiteBrush, 30, 285)

$dialog.Save("$PSScriptRoot\Dialog.bmp", [System.Drawing.Imaging.ImageFormat]::Bmp)
$g2.Dispose()
$dialog.Dispose()
Write-Host "Dialog.bmp created (493x312)"

Write-Host "`nImages created successfully!" -ForegroundColor Green
