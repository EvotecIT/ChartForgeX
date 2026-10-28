function Invoke-RequiredTestContract {
    <# Runs an explicitly routed rendering contract without losing diagnostics or accepting skipped discovery. #>
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $Configuration,
        [Parameter(Mandatory)] [string] $Filter,
        [Parameter(Mandatory)] [string] $RequiredTestName,
        [Parameter(Mandatory)] [int] $RequiredCaseCount,
        [Parameter(Mandatory)] [string] $ReportRoot,
        [Parameter(Mandatory)] [int] $TimeoutSeconds,
        [string] $DiagnosticsSettingsPath
    )

    if (-not [IO.Path]::IsPathRooted($ReportRoot)) {
        throw 'Required test-contract reports need an absolute output root.'
    }
    $output = Join-Path $ReportRoot ([Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $reportName = "$Name-contracts.trx"
    $arguments = @('test', $Project, '-c', $Configuration, '--no-build', '--no-restore',
        '--filter', $Filter, '--logger', "trx;LogFileName=$reportName", '--results-directory', $output)
    if (-not [string]::IsNullOrWhiteSpace($DiagnosticsSettingsPath)) {
        $arguments += @('--settings', $DiagnosticsSettingsPath, '--logger', 'console;verbosity=detailed',
            '--diag', (Join-Path $output "$Name.vstest.log"))
    }
    Invoke-DotNetCommand -Arguments $arguments -Description "$Name contracts" -TimeoutSeconds $TimeoutSeconds
    [xml] $report = Get-Content -LiteralPath (Join-Path $output $reportName) -Raw
    $counters = $report.TestRun.ResultSummary.Counters
    if ([int]$counters.total -eq 0 -or [int]$counters.executed -ne [int]$counters.total -or
        [int]$counters.passed -ne [int]$counters.total -or [int]$counters.notExecuted -ne 0) {
        throw "$Name contracts must execute and pass without skips. See $output."
    }
    $required = @($report.TestRun.Results.UnitTestResult | Where-Object {
        $_.outcome -eq 'Passed' -and ($_.testName -eq $RequiredTestName -or
            $_.testName.StartsWith($RequiredTestName + '(', [StringComparison]::Ordinal))
    })
    if ($required.Count -ne $RequiredCaseCount) {
        throw "$Name validation requires $RequiredCaseCount passed case(s) of $RequiredTestName. See $output."
    }
}
