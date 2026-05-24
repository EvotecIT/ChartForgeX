# Visual Canvas

`VisualCanvas` is a fixed-size layered composition surface for visuals that are not grids: desktop wallpapers, social preview images, report covers, kiosk screens, and product hero graphics.

Use it when the output needs explicit placement, layered backgrounds, side rails, central hero typography, badges, or host-provided image slots. `VisualGrid` remains the right surface for rows and columns of charts or visual blocks.

The first canvas primitives are intentionally generic:

- vertical background color treatment
- optional technology horizon backdrop
- reusable `VisualCanvasTheme` colors for accents, tile text, glass fills, badge fills, feature strips, placeholders, and backdrop highlights
- absolute text layers
- multi-color hero title layers
- information tiles for side rails, with glass or outline surfaces and text or built-in icons
- hero badges for logos, terminal prompts, or product marks
- image layers using SVG hrefs and host-provided RGBA pixels for PNG output
- feature strips for compact bottom rows
- SVG and PNG export

Example:

```csharp
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;

var canvas = VisualCanvas.CreateSocialPreview()
    .WithTitle("PowerBGInfo social preview")
    .WithTheme(new VisualCanvasTheme {
        Accent = ChartColor.FromHex("#2F80FF"),
        HeroTitleAccentColor = ChartColor.FromHex("#2F80FF"),
        TileValueColor = ChartColor.FromHex("#F8FAFC")
    })
    .WithBackground(ChartColor.FromHex("#020713"), ChartColor.FromHex("#071A35"))
    .WithBackdrop(VisualCanvasBackdropStyle.TechHorizon)
    .AddInfoTile(58, 92, 250, 82, "PC", "HOSTNAME", "DEV-Workstation", accent: ChartColor.FromHex("#2F80FF"), iconKind: VisualCanvasInfoTileIconKind.Computer)
    .AddInfoTile(892, 70, 250, 96, "CPU", "CPU", "Intel Core i7-12700K", "23%", ChartColor.FromHex("#60A5FA"), 0.23, VisualCanvasInfoTileSurfaceStyle.Outline, VisualCanvasInfoTileIconKind.Cpu)
    .AddHeroBadge(538, 157, 124, 88, ">_", ChartColor.FromHex("#22A7FF"))
    .AddHeroTitle(312, 296, 576, 82, new[] {
        new VisualCanvasTextRun("Power", ChartColor.FromHex("#F8FAFC")),
        new VisualCanvasTextRun("BGInfo", ChartColor.FromHex("#2F80FF"))
    })
    .AddText(330, 402, 540, "Desktop background insights for Windows and PowerShell", 24, ChartColor.FromHex("#C6D3EA"), VisualCanvasTextAlignment.Center);

canvas.SaveSvg("powerbginfo-social-preview.svg");
canvas.SavePng("powerbginfo-social-preview.png");
```

PowerBGInfo-style desktop generation should stay thin: resolve Windows facts in PowerBGInfo, then pass those strings into `VisualCanvas` templates or layers. Keep reusable layout, typography, image, and tile behavior in ChartForgeX so other hosts can reuse the same engine for OpenGraph images, generated documentation, and report covers.
