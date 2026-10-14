# Share sizes, as a storage report or a Get-ChildItem sum gives.
$shares = @'
Share,Department,SizeGB
\\fs01\eng-builds,Engineering,640
\\fs01\eng-source,Engineering,412
\\fs02\eng-media,Engineering,188
\\fs01\sales,Sales,655
\\fs02\finance,Finance,412
\\fs02\marketing,Marketing,318
\\fs02\hr,HR,96
'@ | ConvertFrom-Csv

$departments = $shares | Group-Object Department | ForEach-Object {
    [pscustomobject]@{
        Department = $_.Name
        Shares     = $_.Count
        SizeGB     = [int] ($_.Group |
            Measure-Object SizeGB -Sum).Sum
    }
} | Sort-Object SizeGB -Descending

New-ImageChart {
    foreach ($d in $departments) {
        New-ImageChartTreemap -Name $d.Department -Value $d.SizeGB
    }
} -Options (New-ImageChartOptions -ShowDataLabels -NoLegend) `
    -FilePath storage-treemap.png -Width 640 -Height 320

$departments | Format-Table -AutoSize
