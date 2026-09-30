# Xem nhanh một thư mục sprite: contact sheet (biết ảnh nào là gì) + bbox vùng đục (map ảnh vào node Figma).
#   .\inspect.ps1 -Dir "Assets\_Game\...\Sprites" -Sheet "$env:TEMP\sheet.png"
#   .\inspect.ps1 -Dir "..." -Bbox UI_Title,UI_Box        # bỏ -Bbox = đo hết
param(
  [Parameter(Mandatory = $true)][string]$Dir,
  [string]$Sheet,
  [string[]]$Bbox
)
Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem "$Dir\*.png" | Sort-Object Name

if ($Sheet) {
  $cols = 6; $tile = 210; $lab = 22
  $rows = [math]::Ceiling($files.Count / $cols)
  $bmp = New-Object System.Drawing.Bitmap(($cols * $tile), ($rows * ($tile + $lab)))
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.Clear([System.Drawing.Color]::FromArgb(255, 120, 120, 120))   # nền xám: thấy được cả art sáng lẫn tối
  $font = New-Object System.Drawing.Font("Arial", 9)
  for ($i = 0; $i -lt $files.Count; $i++) {
    $im = [System.Drawing.Image]::FromFile($files[$i].FullName)
    $c = $i % $cols; $r = [math]::Floor($i / $cols)
    $sc = [math]::Min(($tile - 8) / $im.Width, ($tile - 8) / $im.Height)
    $w = [int]($im.Width * $sc); $h = [int]($im.Height * $sc)
    $g.DrawImage($im, ($c * $tile + [int](($tile - $w) / 2)), ($r * ($tile + $lab) + [int](($tile - $h) / 2)), $w, $h)
    $g.DrawString($files[$i].BaseName, $font, [System.Drawing.Brushes]::White, ($c * $tile + 2), ($r * ($tile + $lab) + $tile))
    $im.Dispose()
  }
  $bmp.Save($Sheet, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
  "sheet -> $Sheet"
}

foreach ($f in $files) {
  if ($Bbox -and $Bbox -notcontains $f.BaseName) { continue }
  $b = New-Object System.Drawing.Bitmap($f.FullName)
  $minx = $b.Width; $miny = $b.Height; $maxx = -1; $maxy = -1
  for ($y = 0; $y -lt $b.Height; $y++) { for ($x = 0; $x -lt $b.Width; $x++) {
    if ($b.GetPixel($x, $y).A -gt 8) {
      if ($x -lt $minx) { $minx = $x }; if ($x -gt $maxx) { $maxx = $x }
      if ($y -lt $miny) { $miny = $y }; if ($y -gt $maxy) { $maxy = $y }
    } } }
  "{0}: img {1}x{2} | opaque {3},{4} {5}x{6}" -f $f.BaseName, $b.Width, $b.Height, $minx, $miny, ($maxx - $minx + 1), ($maxy - $miny + 1)
  $b.Dispose()
}
