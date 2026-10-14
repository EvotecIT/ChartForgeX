# Fourteen days of p95 latency, oldest first.
$p95 = 182, 175, 190, 188, 201, 240, 262, 310, 298, 214, 205, 198,
    190, 186
$target = 250

$series = @(
    New-ImageChartLine -Name 'Orders API' -Value $p95 -Smooth
    New-ImageChartLine -Name 'Target' -Color '#c78404' `
        -Value (@($target) * $p95.Count)
)
$events = {
    New-ImageChartAnnotation -X 6 -Y 0 -Text 'Release 4.2'
    New-ImageChartAnnotation -X 10 -Y 0 -Text 'Index fix'
}
$chart = @{
    Width    = 760
    Height   = 380
    XTitle   = 'Day'
    YTitle   = 'p95 latency (ms)'
    ShowGrid = $true
    Options  = New-ImageChartOptions -ShowLegend -TickCount 5
}

# One definition, two formats: the extension picks the renderer.
foreach ($file in 'latency-trend.png', 'latency-trend.svg') {
    New-ImageChart -Definition $series -FilePath $file @chart `
        -AnnotationsDefinition $events
}

$above = 1..$p95.Count | Where-Object { $p95[$_ - 1] -gt $target }
"Days above target: $($above -join ', ')"
"Peak: $(($p95 | Measure-Object -Maximum).Maximum) ms"
