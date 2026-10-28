function Invoke-BrowserTestHost {
    <#
    .SYNOPSIS
    Runs browser assertions on the existing test-only loopback asset server.
    .DESCRIPTION
    Owns readiness, logs and teardown for one Node server. HtmlTinkerX continues
    to own browser sessions. The action receives the actual loopback gallery URI.
    .PARAMETER Root
    Absolute directory of task-owned static test assets.
    .PARAMETER OutputRoot
    Absolute task-owned directory for readiness, process and server log evidence.
    .PARAMETER Action
    Product assertions to execute after the server publishes its real address.
    #>
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string] $OutputRoot,
        [Parameter(Mandatory)] [scriptblock] $Action
    )
    if (-not [IO.Path]::IsPathRooted($Root) -or -not [IO.Path]::IsPathRooted($OutputRoot)) {
        throw 'Browser test roots must be absolute task-owned paths.'
    }
    $addressFile = Join-Path $OutputRoot ('gallery-address-' + [Guid]::NewGuid().ToString('N') + '.txt')
    $serverLog = Join-Path $OutputRoot 'gallery-server.log'
    $start = [Diagnostics.ProcessStartInfo]::new('node')
    $start.CreateNoWindow = $true
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.ArgumentList.Add((Join-Path $PSScriptRoot 'Serve-BrowserGallery.cjs'))
    $start.ArgumentList.Add($Root)
    $start.ArgumentList.Add($addressFile)
    $server = $null
    try {
        $server = [Diagnostics.Process]::Start($start)
        $standardOutput = $server.StandardOutput.ReadToEndAsync()
        $standardError = $server.StandardError.ReadToEndAsync()
        [IO.File]::WriteAllText((Join-Path $OutputRoot 'gallery-server-process.json'),
            (@{ pid = $server.Id; startedUtc = [DateTime]::UtcNow.ToString('o'); root = $Root } | ConvertTo-Json))
        $timer = [Diagnostics.Stopwatch]::StartNew()
        while (-not [IO.File]::Exists($addressFile)) {
            if ($server.HasExited) { throw "Gallery server exited with code $($server.ExitCode) before readiness. See $serverLog." }
            if ($timer.Elapsed.TotalSeconds -ge 60) { throw "Gallery server PID $($server.Id) did not become ready within 60 seconds. See $serverLog." }
            Start-Sleep -Milliseconds 100
        }
        $address = [IO.File]::ReadAllText($addressFile)
        $galleryUri = $null
        if (-not [Uri]::TryCreate($address, [UriKind]::Absolute, [ref] $galleryUri) -or
            $galleryUri.Scheme -ne 'http' -or -not $galleryUri.IsLoopback -or $galleryUri.AbsolutePath -ne '/catalog.html') {
            throw "Gallery server published an invalid readiness address. See $serverLog."
        }
        & $Action $galleryUri
    } finally {
        if ($server) {
            try {
                if (-not $server.HasExited) { $server.Kill(); [void] $server.WaitForExit(10000) }
                if (-not $server.HasExited) { throw "Owned gallery server PID $($server.Id) did not exit after termination." }
                $capturedOutput = if ($standardOutput.Wait(5000)) { $standardOutput.Result } else { '[stdout capture did not finish]' }
                $capturedError = if ($standardError.Wait(5000)) { $standardError.Result } else { '[stderr capture did not finish]' }
                [IO.File]::WriteAllText($serverLog, $capturedOutput + $capturedError)
                if ([IO.File]::Exists($addressFile)) { [IO.File]::Delete($addressFile) }
            } finally { $server.Dispose() }
        }
    }
}
