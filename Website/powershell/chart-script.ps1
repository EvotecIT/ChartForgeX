$builds = @'
Day,Passed,Failed
Mon,41,3
Tue,38,6
Wed,45,2
Thu,52,4
Fri,47,1
'@ | ConvertFrom-Csv

# -ChartScript hands you the ChartForgeX chart itself, so every
# method in the .NET API is available, not only the cmdlets.
New-ImageChart -ChartScript {
    param($chart)
    # ChartForgeX loads in the module's own context: ask the chart
    # for its types instead of writing [ChartForgeX.Primitives...].
    $point = $chart.GetType().Assembly.GetType(
        'ChartForgeX.Primitives.ChartPoint')
    function ConvertTo-Point([double[]] $values) {
        $points = [Array]::CreateInstance($point, $values.Count)
        for ($i = 0; $i -lt $values.Count; $i++) {
            $points[$i] = $point::new($i + 1, $values[$i])
        }
        , $points
    }
    [void] $chart.WithTitle('Builds by result')
    [void] $chart.WithSubtitle('Week 41, main branch')
    [void] $chart.WithXLabels([string[]] $builds.Day)
    [void] $chart.AddBar('Passed', (ConvertTo-Point $builds.Passed))
    [void] $chart.AddBar('Failed', (ConvertTo-Point $builds.Failed))
    [void] $chart.WithStackedBars()
} -ShowGrid -FilePath chart-script.png -Width 640 -Height 360 `
    -Options (New-ImageChartOptions -ShowLegend `
        -LegendPosition Bottom -Palette '#11776f', '#d64545')

$builds | Format-Table -AutoSize
