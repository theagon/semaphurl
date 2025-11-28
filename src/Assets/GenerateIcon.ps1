# PowerShell script to generate LinkRouter icon
# Run this script to create icon.ico and icon.png

Add-Type -AssemblyName System.Drawing

$size = 256
$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Enable anti-aliasing
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Background - Gradient circle
$rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddEllipse(16, 16, $size - 32, $size - 32)

# Purple gradient background
$gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 139, 92, 246),  # Purple
    [System.Drawing.Color]::FromArgb(255, 79, 70, 229),   # Indigo
    [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal
)
$graphics.FillPath($gradientBrush, $path)

# Draw arrow symbol (router icon)
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 16)
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

# Main horizontal line
$graphics.DrawLine($pen, 70, 128, 186, 128)

# Arrow head
$arrowPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 14)
$arrowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$arrowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawLine($arrowPen, 156, 88, 186, 128)
$graphics.DrawLine($arrowPen, 156, 168, 186, 128)

# Branch lines (showing routing)
$branchPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(200, 255, 255, 255), 8)
$branchPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$branchPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

# Top branch
$graphics.DrawLine($branchPen, 100, 128, 100, 80)
$graphics.DrawLine($branchPen, 100, 80, 130, 65)

# Bottom branch  
$graphics.DrawLine($branchPen, 100, 128, 100, 176)
$graphics.DrawLine($branchPen, 100, 176, 130, 191)

# Cleanup
$pen.Dispose()
$arrowPen.Dispose()
$branchPen.Dispose()
$gradientBrush.Dispose()
$graphics.Dispose()

# Save PNG
$pngPath = Join-Path $PSScriptRoot "icon.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Created: $pngPath"

# Create ICO file (multi-resolution)
function Save-Icon {
    param($Bitmap, $Path)
    
    $sizes = @(16, 32, 48, 256)
    $iconStream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($iconStream)
    
    # ICO Header
    $writer.Write([Int16]0)        # Reserved
    $writer.Write([Int16]1)        # Type (1 = ICO)
    $writer.Write([Int16]$sizes.Count)  # Number of images
    
    $imageOffset = 6 + ($sizes.Count * 16)  # Header + directory entries
    $imageData = @()
    
    foreach ($s in $sizes) {
        $resized = New-Object System.Drawing.Bitmap($s, $s)
        $g = [System.Drawing.Graphics]::FromImage($resized)
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($Bitmap, 0, 0, $s, $s)
        $g.Dispose()
        
        $ms = New-Object System.IO.MemoryStream
        $resized.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $data = $ms.ToArray()
        $ms.Dispose()
        $resized.Dispose()
        
        # Directory entry
        $writer.Write([Byte]$(if ($s -eq 256) { 0 } else { $s }))  # Width
        $writer.Write([Byte]$(if ($s -eq 256) { 0 } else { $s }))  # Height
        $writer.Write([Byte]0)     # Color palette
        $writer.Write([Byte]0)     # Reserved
        $writer.Write([Int16]1)    # Color planes
        $writer.Write([Int16]32)   # Bits per pixel
        $writer.Write([Int32]$data.Length)   # Image size
        $writer.Write([Int32]$imageOffset)   # Image offset
        
        $imageOffset += $data.Length
        $imageData += ,($data)
    }
    
    # Write image data
    foreach ($data in $imageData) {
        $writer.Write($data)
    }
    
    $writer.Flush()
    [System.IO.File]::WriteAllBytes($Path, $iconStream.ToArray())
    
    $writer.Dispose()
    $iconStream.Dispose()
}

$icoPath = Join-Path $PSScriptRoot "icon.ico"
Save-Icon -Bitmap $bitmap -Path $icoPath
Write-Host "Created: $icoPath"

$bitmap.Dispose()
Write-Host "Icon generation complete!"

