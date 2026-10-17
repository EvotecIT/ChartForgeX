$rings = @'
Ring,Compliant,Missing
Servers,96,2
Workstations,89,45
Laptops,74,61
'@ | ConvertFrom-Csv

# SocialPreview is 1200 x 630, the size link previews expect.
New-ImageCanvas -Preset SocialPreview -Title 'Patch compliance' `
    -LayerDefinition {
    $text = @{ X = 72; Width = 1056 }
    New-ImageCanvasText @text -Y 72 -Text 'Patch compliance' `
        -FontSize 58 -Emphasized
    New-ImageCanvasText @text -Y 154 -FontSize 26 `
        -Text 'October cumulative update, three rings'
    $x = 72
    foreach ($ring in $rings) {
        $tile = @{
            X = $x; Y = 260; Width = 336; Height = 176
            Icon = $ring.Ring.Substring(0, 2).ToUpper()
            Label = $ring.Ring; Value = "$($ring.Compliant)%"
            Detail = "$($ring.Missing) machines need updates"
            Progress = $ring.Compliant / 100
            Accent = if ([int] $ring.Compliant -ge 90) { '#37c9b8' }
                     else { '#eeb640' }
        }
        New-ImageCanvasInfoTile @tile
        $x += 360
    }
} -FilePath social-card.png

$rings | Format-Table -AutoSize
