$builds = @'
Day,Passed,Failed
Mon,41,3
Tue,38,6
Wed,45,2
Thu,52,4
Fri,47,1
'@ | ConvertFrom-Csv

# Titles, category labels and the bar layout are all parameters.
$chart = @{
    Title    = 'Builds by result'
    Subtitle = 'Week 41, main branch'
    XLabel   = $builds.Day
    Width    = 640
    Height   = 360
    ShowGrid = $true
    FilePath = 'builds.png'
    Options  = New-ImageChartOptions -BarLayout Stacked -ShowLegend `
        -LegendPosition Bottom -Palette '#11776f', '#d64545'
}
New-ImageChart @chart {
    New-ImageChartBar -Name Passed -Value $builds.Passed
    New-ImageChartBar -Name Failed -Value $builds.Failed
}

$builds | Format-Table -AutoSize
