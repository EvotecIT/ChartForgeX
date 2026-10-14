# One row per deployment ring, as a WSUS or Intune export gives it.
$rings = @'
Ring,Machines,UpToDate
Servers,48,46
Workstations,412,367
Laptops,236,175
'@ | ConvertFrom-Csv | Select-Object Ring, Machines, UpToDate,
    @{ Name = 'Compliant'; Expression = {
        [int] (100 * $_.UpToDate / $_.Machines) } }

$options = New-ImageChartOptions -ProgressMaximum 100 `
    -ShowDataLabels -NoProgressHandles -NoLegend -NoPlotBackground
New-ImageChart {
    foreach ($ring in $rings) {
        New-ImageChartProgress $ring.Ring $ring.Compliant
    }
} -Options $options -FilePath patch-compliance.png `
    -Width 560 -Height 240

$rings | Format-Table -AutoSize
