# Last month's bill, then each change that moved it.
$steps = [ordered]@{
    September  = 4200
    Compute    = 380
    Reserved   = -250
    Storage    = 610
    Licences   = -140
}

New-ImageChart {
    New-ImageChartWaterfall -Name 'Cloud cost (EUR)' `
        -Value @($steps.Values) -Labels @($steps.Keys)
} -ShowGrid -FilePath cost-bridge.png -Width 640 -Height 340 `
    -Options (New-ImageChartOptions -ShowDataLabels -NoLegend)

$steps.GetEnumerator() | Format-Table -AutoSize `
    @{ Name = 'Step'; Expression = 'Key' },
    @{ Name = 'EUR'; Expression = 'Value' }
"October: {0:N0} EUR" -f ($steps.Values | Measure-Object -Sum).Sum
