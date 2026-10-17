---
title: PowerBGInfo
description: Windows wallpapers drawn by ChartForgeX. Twelve finished examples, one built step by step, your own data sources and the routes to production.
slug: powerbginfo
layout: powerbginfo
meta.prism: true
meta.seo_title: PowerBGInfo wallpapers drawn by ChartForgeX
---

## Install the module

```powershell
Install-Module PowerBGInfo
```

PowerBGInfo runs on Windows only. It is built for .NET Framework 4.7.2 and .NET 8 on Windows, so it works in Windows PowerShell 5.1 and in PowerShell 7.4 or later. It needs no separate helper modules; run the same command again to update.

## Render a preview to a file

```powershell
New-BGInfo -Theme Aurora -ThemeMode Dark -Target File {
    New-BGInfoValue -BuiltinValue HostName -Name 'Computer'
    New-BGInfoValue -BuiltinValue FullUserName -Name 'User'
    New-BGInfoValue -BuiltinValue OSName -Name 'Windows'
    New-BGInfoValue -BuiltinValue IPv4Address -Name 'IP address'
    New-BGInfoValue -BuiltinValue Uptime -Name 'Up for'
    New-BGInfoChart -Region Right -Kind Gauge -Metric MemoryPercent `
        -Title 'Memory used' -Maximum 100 -ValueSuffix ' %'
} -ConfigurationDirectory C:\Temp\PowerBGInfo -OutputFileName preview.png
```

`-Target File` writes `preview.png` and leaves the desktop alone. The built-in values read the machine the script runs on, and `-Metric MemoryPercent` reads the memory in use at that moment, so every machine gets its own wallpaper from the same script.

## Set it as the wallpaper

```powershell
New-BGInfo -Theme Aurora {
    New-BGInfoValue -BuiltinValue HostName -Name 'Computer'
    New-BGInfoValue -BuiltinValue OSName -Name 'Windows'
} -ConfigurationDirectory C:\ProgramData\PowerBGInfo -WallpaperFit Fill
```

Without `-Target`, `New-BGInfo` sets the wallpaper for the account that runs it. `-AllUsers` stages it for every profile, and `-Target LogonScreen` or `-Target Both` sets the lock screen too; those need an elevated session. One run applies one image; run it again to refresh the values.
