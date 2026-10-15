---
title: PowerBGInfo
description: Windows wallpapers with system details, charts and topology, drawn by ChartForgeX. Four PowerShell scripts shown with the wallpapers they rendered.
slug: powerbginfo
layout: powerbginfo
meta.prism: true
meta.seo_title: PowerBGInfo wallpapers drawn by ChartForgeX
---

## Install the module

```powershell
Install-Module PowerBGInfo
```

PowerBGInfo runs on Windows only. It is built for .NET Framework 4.7.2 and .NET 8 on Windows, so it works in Windows PowerShell 5.1 and in PowerShell 7.4 or later. It no longer needs separate helper modules; run the same command again to update.

## Render a preview to a file

```powershell
New-BGInfo -Target File {
    New-BGInfoBackground -Width 1920 -Height 1080 `
        -Gradient '#0B3B46', '#050A14' -Pattern Grid
    New-BGInfoValue -BuiltinValue HostName -Name 'Computer'
    New-BGInfoValue -BuiltinValue FullUserName -Name 'User'
    New-BGInfoValue -BuiltinValue OSName -Name 'Windows'
    New-BGInfoValue -BuiltinValue IPv4Address -Name 'IP address'
    New-BGInfoChart -Kind Gauge -Metric MemoryPercent `
        -Title 'Memory used' -Maximum 100 -ValueSuffix ' %' `
        -LineColor '#2DD4BF' -ThemeMode Dark `
        -BackgroundColor '#0B1222C2' -CornerRadius 18 `
        -Width 320 -Height 240 -Anchor BottomRight `
        -OffsetX 64 -OffsetY 56
} -Color '#94A3B8' -ValueColor White `
    -FontSize 20 -ValueFontSize 20 -SpaceX 64 -SpaceY 56 `
    -ConfigurationDirectory C:\Temp\PowerBGInfo `
    -OutputFileName preview.png
```

`-Target File` writes `preview.png` and leaves the desktop alone. The built-in values read the machine the script runs on, and `-Metric MemoryPercent` reads the memory in use at that moment, so every machine gets its own wallpaper from the same script.

## Set it as the wallpaper

```powershell
New-BGInfo {
    New-BGInfoValue -BuiltinValue HostName -Name 'Computer'
    New-BGInfoValue -BuiltinValue OSName -Name 'Windows'
} -ConfigurationDirectory C:\ProgramData\PowerBGInfo -WallpaperFit Fill
```

Without `-Target`, `New-BGInfo` sets the wallpaper for the signed-in user. `-AllUsers` stages it for every profile, and `-Target LogonScreen` or `-Target Both` sets the lock screen too; those need an elevated session. Run the script at sign-in or from a scheduled task to keep the values current.
