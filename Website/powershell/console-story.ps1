# Rows from your own command. The story replays them; it never
# runs the commands it shows.
$health = @'
Name,Status,P95
Gateway,Running,84 ms
Orders,Degraded,318 ms
Search,Running,146 ms
'@ | ConvertFrom-Csv

$terminal = @{
    Title = 'pwsh'; WorkingDirectory = 'C:\Ops'; Theme = 'Dark'
    WindowStyle = 'WindowsTerminal'; Width = 640
}
$story = New-ImageConsoleStory @terminal -PassThru -Content {
    New-ImageConsoleStoryCommand 'Get-ServiceHealth -Env Prod'
    $health | New-ImageConsoleStoryTable -Property Name, Status, P95
    New-ImageConsoleStoryBlankLine
    New-ImageConsoleStoryCommand 'Restart-OrdersPool -WhatIf'
    New-ImageConsoleStoryOutput 'What if: restart 3 Orders nodes' `
        -Style Warning
    New-ImageConsoleStoryOutput 'Health check passed' -Style Success
}

# SVG and HTML replay in a browser; PNG keeps the final frame.
# .gif and .apng write the same animation for chat tools.
foreach ($ext in 'svg', 'html', 'png') {
    $story | Export-ImageConsoleStory -Path "console-story.$ext"
}

Get-Item console-story.* | Format-Table Name,
    @{ Name = 'KB'; Expression = { [int] ($_.Length / 1KB) } }
