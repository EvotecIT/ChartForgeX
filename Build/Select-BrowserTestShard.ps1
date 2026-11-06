function Get-BrowserTestCase {
    <#
    .SYNOPSIS
    Lists the browser test cases a filter selects from the built test assembly, sorted ordinally.
    .DESCRIPTION
    Discovery lists a theory whose rows are only known at run time as one method case.
    #>
    param(
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $Configuration,
        [Parameter(Mandatory)] [string] $Filter
    )
    $listing = & dotnet test $Project -c $Configuration --no-build --no-restore --nologo --list-tests --filter $Filter 2>&1
    if ($LASTEXITCODE) {
        $listing | Write-Host
        throw "Browser test discovery failed with exit code $LASTEXITCODE."
    }
    $started = $false
    $cases = [Collections.Generic.List[string]]::new()
    foreach ($line in $listing) {
        $text = "$line"
        if ($text -match '^The following Tests are available:') { $started = $true; continue }
        if ($started -and $text -match '^\s{4}(\S.*)$') { $cases.Add($Matches[1].TrimEnd()) }
    }
    if ($cases.Count -eq 0) { throw "No browser tests matched '$Filter'." }
    $cases.Sort([StringComparer]::Ordinal)
    , $cases.ToArray()
}

function Get-BrowserTestClass {
    <# Returns Namespace.Class for a listed display name: Namespace.Class.Method plus optional theory arguments. #>
    param([Parameter(Mandatory)] [string] $Case)
    $method = $Case.Split('(')[0]
    $method.Substring(0, $method.LastIndexOf('.'))
}

function Select-BrowserTestShard {
    <#
    .SYNOPSIS
    Selects one deterministic test-class shard of the browser contracts matched by a filter.
    .DESCRIPTION
    Lists the matching cases from the built test assembly and balances whole classes across
    shards by their number of listed cases, largest first onto the lightest shard (the same split
    for every browser suite). Every class belongs to exactly one shard and keeps its xUnit
    collection on one runner. Writes shard.json beside the results so Test-BrowserShardCoverage.ps1 can
    prove the shards of one suite cover the listed classes exactly once.
    #>
    param(
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $Configuration,
        [Parameter(Mandatory)] [string] $Filter,
        [Parameter(Mandatory)] [ValidateRange(1, 16)] [int] $ShardIndex,
        [Parameter(Mandatory)] [ValidateRange(1, 16)] [int] $ShardCount,
        [Parameter(Mandatory)] [string] $Suite,
        [Parameter(Mandatory)] [string] $ResultsDirectory
    )
    if ($ShardIndex -gt $ShardCount) { throw 'ShardIndex must not exceed ShardCount.' }

    $cases = Get-BrowserTestCase -Project $Project -Configuration $Configuration -Filter $Filter

    $classOf = { param($case) Get-BrowserTestClass -Case $case }
    $ordinal = { param($values) $sorted = [string[]] @($values); [Array]::Sort($sorted, [StringComparer]::Ordinal); , $sorted }
    $classes = @($cases | Group-Object { & $classOf $_ } | Sort-Object @{ Expression = 'Count'; Descending = $true }, Name)
    $loads = [int[]]::new($ShardCount)
    $owned = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($class in $classes) {
        $lightest = 0
        for ($index = 1; $index -lt $ShardCount; $index++) { if ($loads[$index] -lt $loads[$lightest]) { $lightest = $index } }
        $loads[$lightest] += $class.Count
        if ($lightest -eq $ShardIndex - 1) { [void] $owned.Add($class.Name) }
    }
    $selected = @($cases | Where-Object { $owned.Contains((& $classOf $_)) })
    # The trailing dot keeps one class from matching another class whose name extends it.
    $ownedClasses = & $ordinal $owned
    $shardFilter = if ($owned.Count) {
        "($Filter)&(" + (($ownedClasses | ForEach-Object { "FullyQualifiedName~$_." }) -join '|') + ')'
    } else { $null }

    $digest = { param($values) [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($values -join "`n")))) }
    New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
    [ordered]@{
        suite = $Suite
        shard = $ShardIndex
        shards = $ShardCount
        filter = $Filter
        listedClasses = $classes.Count
        listingSha256 = & $digest (& $ordinal ($classes | ForEach-Object Name))
        classes = $ownedClasses
        cases = $selected
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $ResultsDirectory 'shard.json') -Encoding utf8
    Write-Host "Browser shard $ShardIndex/$ShardCount runs $($selected.Count) of $($cases.Count) cases in $($owned.Count) of $($classes.Count) classes."
    [pscustomobject]@{ Filter = $shardFilter; Classes = $owned; Cases = $selected; AllCases = $cases }
}

function Assert-BrowserShardExecution {
    <# Proves the TRX executed every listed case the shard owns, and nothing else. #>
    param(
        [Parameter(Mandatory)] $Selection,
        [Parameter(Mandatory)] [xml] $Report
    )
    $owned = [Collections.Generic.HashSet[string]]::new([string[]] @($Selection.Cases), [StringComparer]::Ordinal)
    $matched = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $extra = [Collections.Generic.List[string]]::new()
    foreach ($name in @($Report.TestRun.Results.UnitTestResult | ForEach-Object testName)) {
        # A theory listed as its method runs one row per data item.
        $case = if ($owned.Contains($name)) { $name } else { $name.Split('(')[0] }
        if ($owned.Contains($case)) { [void] $matched.Add($case) } else { $extra.Add($name) }
    }
    $missing = @($Selection.Cases | Where-Object { -not $matched.Contains($_) })
    if ($missing.Count -or $extra.Count) {
        throw "Shard did not run exactly its listed cases. Missing: $(($missing | Select-Object -First 5) -join '; '). Unexpected: $(($extra | Select-Object -First 5) -join '; ')."
    }
}
