Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

$r = 56
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddArc(8, 8, $r, $r, 180, 90)
$path.AddArc($size - 8 - $r, 8, $r, $r, 270, 90)
$path.AddArc($size - 8 - $r, $size - 8 - $r, $r, $r, 0, 90)
$path.AddArc(8, $size - 8 - $r, $r, $r, 90, 90)
$path.CloseFigure()

$rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 29, 78, 216),
    [System.Drawing.Color]::FromArgb(255, 56, 189, 248),
    45)
$g.FillPath($brush, $path)

$font = New-Object System.Drawing.Font('Microsoft YaHei UI', 84,
    [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = [System.Drawing.StringAlignment]::Center
$fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
$fmt.FormatFlags = [System.Drawing.StringFormatFlags]::NoWrap
$text = [string][char]0x6DF1 + [string][char]0x84DD
$g.DrawString($text, $font, [System.Drawing.Brushes]::White,
    (New-Object System.Drawing.RectangleF(0, 0, $size, $size)), $fmt)
$g.Dispose()

$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $ms.ToArray()
$bmp.Dispose()

$icoMs = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($icoMs)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]1)
$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([uint16]1)
$bw.Write([uint16]32)
$bw.Write([uint32]$png.Length)
$bw.Write([uint32]22)
$bw.Write($png)
$bw.Flush()

$outDir = Split-Path -Parent $MyInvocation.MyCommand.Path
[System.IO.File]::WriteAllBytes((Join-Path $outDir '..\assets\app.ico'), $icoMs.ToArray())
Write-Output ('app.ico written: ' + $png.Length + ' png bytes')
