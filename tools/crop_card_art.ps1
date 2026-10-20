# 把 AI 生成的原始图裁成《杀戮尖塔 2》的标准尺寸。
#
# 用法：在仓库根目录执行
#   pwsh -File tools/crop_card_art.ps1 -SourceDir "C:\path\to\assets\hu_tao_cards"
#
# 输入：SourceDir 下的 card_map.json（[{File,Id,Title}]）+ 对应 jpg
# 输出：
#   HuTaoMod/images/card_portraits/<id>.jpg       250x190   （小卡图）
#   HuTaoMod/images/card_portraits/big/<id>.jpg   1000x760  （大卡图）
#
# 说明：用 JPEG(q92) 而不是 PNG —— 83 张 1000x760 PNG 会让 .pck 涨到 85MB，
#       JPEG 版本只有约 21MB，导入时再走 Godot 的 lossy 压缩。

param(
    [Parameter(Mandatory = $true)][string]$SourceDir
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$big = Join-Path $PSScriptRoot "..\HuTaoMod\images\card_portraits\big"
$small = Join-Path $PSScriptRoot "..\HuTaoMod\images\card_portraits"
New-Item -ItemType Directory -Force $big | Out-Null

$map = Get-Content (Join-Path $SourceDir "card_map.json") -Raw | ConvertFrom-Json
$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$ep = New-Object System.Drawing.Imaging.EncoderParameters(1)
$ep.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter([System.Drawing.Imaging.Encoder]::Quality, [long]92)

$count = 0
foreach ($item in $map) {
    $file = Join-Path $SourceDir $item.File
    if (-not (Test-Path $file)) { Write-Warning "missing: $($item.File)"; continue }

    $img = [System.Drawing.Image]::FromFile($file)
    $w = $img.Width; $h = $img.Height

    # 目标比例 1000:760，居中裁切
    $targetRatio = 1000.0 / 760.0
    if (($w / $h) -gt $targetRatio) { $cw = [int]($h * $targetRatio); $ch = $h }
    else { $cw = $w; $ch = [int]($w / $targetRatio) }
    $sx = [int](($w - $cw) / 2); $sy = [int](($h - $ch) / 2)

    $bmp = New-Object System.Drawing.Bitmap(1000, 760, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($img,
        (New-Object System.Drawing.Rectangle(0, 0, 1000, 760)),
        (New-Object System.Drawing.Rectangle($sx, $sy, $cw, $ch)),
        [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    $bmp.Save((Join-Path $big "$($item.Id).jpg"), $codec, $ep)

    $thumb = New-Object System.Drawing.Bitmap(250, 190, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g2 = [System.Drawing.Graphics]::FromImage($thumb)
    $g2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g2.DrawImage($bmp, 0, 0, 250, 190)
    $g2.Dispose()
    $thumb.Save((Join-Path $small "$($item.Id).jpg"), $codec, $ep)

    $thumb.Dispose(); $bmp.Dispose(); $img.Dispose()
    $count++
}

Write-Host "cropped $count cards -> $big / $small"
