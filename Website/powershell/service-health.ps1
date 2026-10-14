# Service data from a monitoring API, a CSV export or Get-Counter.
$services = @'
Name,Status,Requests,P95
Gateway,Healthy,48210,84
Identity,Healthy,21980,112
Orders,Warning,15402,318
Search,Healthy,30117,146
'@ | ConvertFrom-Csv
$week = 182, 190, 201, 262, 310, 298, 214   # Orders p95, last seven days
$theme = 'Dark'

$total = ($services | Measure-Object Requests -Sum).Sum
$slowest = $services | Sort-Object { [int] $_.P95 } | Select-Object -Last 1
$healthy = @($services | Where-Object Status -eq 'Healthy').Count

# -PassThru returns the chart so the dashboard can embed it.
$latency = New-ImageChart -Title "$($slowest.Name) latency" -Subtitle 'Last seven days' `
    -XLabel Wed, Thu, Fri, Sat, Sun, Mon, Tue -FilePath latency.png `
    -Width 932 -Height 320 -Theme $theme -ShowGrid -PassThru `
    -Options (New-ImageChartOptions -ShowLegend -LegendPosition Bottom -TickCount 4) {
    New-ImageChartLine -Name "$($slowest.Name) p95 (ms)" -Value $week -Marker Circle
    New-ImageChartLine -Name 'Target' -Value (@(250) * $week.Count) -Color '#eeb640'
}

# Pipe the rows in and pick columns the way Format-Table does.
$table = $services | New-ImageTableBlock -Title Services -StatusColumn Status `
    -Theme $theme -Width 616 -Height 270 -Property Name, Status,
    @{ Name = 'Requests'; Expression = { [int] $_.Requests }; FormatString = 'N0'; Alignment = 'Right' },
    @{ Name = 'P95'; Expression = { "$($_.P95) ms" }; Alignment = 'Right' }

$checks = @{
    Title  = 'Checks'
    Item   = 'TLS expiry', 'Backups', 'Queue depth'
    Value  = '41 days', 'OK', '1,204'
    Status = 'Positive', 'Positive', 'Warning'
    Theme  = $theme; Width = 300; Height = 270
}

$card = @{ Width = 300; Height = 170; Theme = $theme }
$grid = @{
    Title = 'Service health'; Subtitle = 'Week 41'; Columns = 3
    Gap = 16; Padding = 24; AdaptiveRowHeights = $true; Theme = $theme
}
$dashboard = New-ImageVisualGrid @grid -ContentDefinition {
    New-ImageMetricCard -Label Requests -Value $total -Format N0 -Status Positive `
        -Trend '+8% vs last week' -MiniValues 98, 101, 104, 110, 116 @card
    New-ImageMetricCard -Label 'Slowest p95' -Value ([int] $slowest.P95) -Unit ms `
        -Caption $slowest.Name -Status Warning -MiniValues $week @card
    New-ImageMetricCard -Label Healthy -Value "$healthy of $($services.Count)" `
        -Caption services -Status Positive @card
    New-ImageVisualGridItem -Block $table -ColumnSpan 2
    New-ImageListBlock @checks
    New-ImageVisualGridItem -Chart $latency -ColumnSpan 3
}

# One dashboard, two outputs: PNG for chat and email, HTML for a browser.
foreach ($file in 'service-health.png', 'service-health.html') {
    $dashboard | New-ImageVisualStory -FilePath $file
}

$services | Sort-Object { [int] $_.P95 } -Descending | Format-Table -AutoSize
