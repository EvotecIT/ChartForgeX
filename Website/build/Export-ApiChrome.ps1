# Lifts the site header and footer out of the pages built at the API reference routes, so the API docs step wraps
# its pages in exactly the markup every other page uses. Run after the site build and before the API docs steps.
param(
    [string]$Site = './_site',
    [string]$Out = './_temp/chrome'
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $Out | Out-Null
foreach ($route in @(@{ Path = 'api'; Name = 'api' }, @{ Path = 'powershell/commands'; Name = 'powershell-commands' })) {
    $page = Join-Path $Site (Join-Path $route.Path 'index.html')
    $html = [IO.File]::ReadAllText((Resolve-Path $page))
    $header = [regex]::Match($html, '(?s)<a class="cfx-skip".*?</header>')
    $footer = [regex]::Match($html, '(?s)<footer class="cfx-footer">.*?</footer>')
    if (-not $header.Success -or -not $footer.Success) { throw "No site header or footer in $page." }
    # The API docs step requires a navigation placeholder in custom fragments. The visible header is already complete,
    # so the placeholder sits in an inert template that browsers never render.
    $marker = "<template data-cfx-api-nav>{{NAV_ACTIONS}}</template>"
    [IO.File]::WriteAllText((Join-Path $Out "$($route.Name)-header.html"), $header.Value + "`n" + $marker + "`n")
    [IO.File]::WriteAllText((Join-Path $Out "$($route.Name)-footer.html"), $footer.Value + "`n")
}
Write-Output "API chrome written to $Out"
