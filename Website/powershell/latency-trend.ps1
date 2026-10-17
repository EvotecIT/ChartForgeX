# Fourteen days of p95 latency, oldest first.
$p95 = 182, 175, 190, 188, 201, 240, 262, 310, 298, 214, 205, 198,
    190, 186
$target = 250

$chart = @{
    Title    = 'Orders API latency'
    Subtitle = 'p95 against the 250 ms target'
    XLabel   = 1..$p95.Count | ForEach-Object { "D$_" }
    YTitle   = 'p95 latency (ms)'
    Width    = 760
    Height   = 380
    ShowGrid = $true
    Options  = New-ImageChartOptions -ShowLegend -TickCount 5
}
$series = {
    New-ImageChartLine -Name 'Orders API' -Value $p95 -Smooth -Marker Circle
    New-ImageChartLine -Name 'Target' -Value (@($target) * $p95.Count) -Color '#c78404'
}
$events = {
    New-ImageChartAnnotation -X 6 -Y 0 -Text 'Release 4.2'
    New-ImageChartAnnotation -X 10 -Y 0 -Text 'Index fix'
}

# One definition, two formats: the extension picks the renderer.
foreach ($file in 'latency-trend.png', 'latency-trend.svg') {
    New-ImageChart $series -FilePath $file @chart -AnnotationsDefinition $events
}

$above = 1..$p95.Count | Where-Object { $p95[$_ - 1] -gt $target }
"Days above target: $($above -join ', ')"
"Peak: $(($p95 | Measure-Object -Maximum).Maximum) ms"
