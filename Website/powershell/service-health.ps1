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

# -PassThru returns the ChartForgeX chart, so the dashboard can embed it.
$latency = New-ImageChart {
    New-ImageChartLine -Name "$($slowest.Name) p95 (ms)" -Value $week
    New-ImageChartLine -Name 'Target' -Value (@(250) * $week.Count) -Color '#eeb640'
} -FilePath latency.png -Width 932 -Height 320 -Theme $theme -ShowGrid -PassThru `
    -Options (New-ImageChartOptions -ShowLegend -LegendPosition Bottom -TickCount 4)
# It is a ChartForgeX object, so its .NET API is one dot away.
[void] $latency.WithTitle("$($slowest.Name) latency").WithSubtitle('Last seven days')
[void] $latency.WithXLabels('Wed', 'Thu', 'Fri', 'Sat', 'Sun', 'Mon', 'Tue')

$table = New-ImageTableBlock -Title Services -Column Name, Status, Requests, P95 `
    -StatusColumn Status -Theme $theme -Row ($services | ForEach-Object {
        , @($_.Name, $_.Status, ([int] $_.Requests).ToString('N0'), "$($_.P95) ms")
    })
$checks = New-ImageListBlock -Title Checks -Theme $theme `
    -Item 'TLS expiry', 'Backups', 'Queue depth' -Value '41 days', 'OK', '1,204' `
    -Status Positive, Positive, Warning
[void] $table.WithSize(616, 270)
[void] $checks.WithSize(300, 270)

$card = @{ Width = 300; Height = 170; Theme = $theme }
$items = @(
    New-ImageMetricCard -Label Requests -Value $total -Format N0 -Status Positive `
        -Trend '+8% vs last week' -MiniValues 98, 101, 104, 110, 116 @card
    New-ImageMetricCard -Label 'Slowest p95' -Value ([int] $slowest.P95) -Unit ms `
        -Caption $slowest.Name -Status Warning -MiniValues $week @card
    New-ImageMetricCard -Label Healthy -Value "$healthy of $($services.Count)" `
        -Caption services -Status Positive @card
) | ForEach-Object { New-ImageVisualGridItem -Block $_ }
$items += New-ImageVisualGridItem -Block $table -ColumnSpan 2
$items += New-ImageVisualGridItem -Block $checks
$items += New-ImageVisualGridItem -Chart $latency -ColumnSpan 3

$grid = New-ImageVisualGrid -Title 'Service health' -Subtitle 'Week 41' -Columns 3 `
    -Gap 16 -Padding 24 -AdaptiveRowHeights -Theme $theme -Content $items
$grid | New-ImageVisualStory -FilePath service-health.png
$grid | New-ImageVisualStory -FilePath service-health.html

$services | Sort-Object { [int] $_.P95 } -Descending | Format-Table -AutoSize
