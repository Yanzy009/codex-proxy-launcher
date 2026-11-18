Add-Type -AssemblyName System.Drawing

$size = 256
$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::FromArgb(31, 41, 79))

$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(64, 217, 199), 22)
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawLines($pen, @(
    (New-Object System.Drawing.Point(48, 82)),
    (New-Object System.Drawing.Point(105, 139)),
    (New-Object System.Drawing.Point(158, 119))
))
$graphics.FillEllipse([System.Drawing.Brushes]::White, 156, 151, 50, 50)

New-Item -ItemType Directory -Force -Path (Split-Path $args[0]) | Out-Null
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$stream = [System.IO.File]::Create($args[0])
$icon.Save($stream)
$stream.Dispose()
$icon.Dispose()
$pen.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
