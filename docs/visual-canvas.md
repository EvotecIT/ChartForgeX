# Visual Canvas

`VisualCanvas` is a fixed-size layered composition surface for visuals that are not grids: desktop wallpapers, social preview images, report covers, kiosk screens, and product hero graphics.

Use it when the output needs explicit placement, layered backgrounds, side rails, central hero typography, badges, or host-provided image slots. `VisualGrid` remains the right surface for rows and columns of charts or visual blocks.

The first canvas primitives are intentionally generic:

- vertical background color treatment
- optional technology horizon backdrop
- reusable `VisualCanvasTheme` colors for accents, tile text, glass fills, badge fills, feature strips, placeholders, and backdrop highlights
- absolute text layers
- multi-color hero title layers
- key/value text blocks with measured label columns, wrapped value text, per-row color overrides, and anchor-based placement
- information tiles for side rails, with glass, outline, or raised surfaces, text or built-in icons, progress rails, and compact right-side mini charts
- hero badges for logos, terminal prompts, or product marks
- image layers using SVG hrefs and host-provided RGBA pixels for raster output, with `Stretch`, `Contain`, `Cover`, and `Center` fit modes
- dependency-free raster image input for baseline JPEG, PNG, BMP, PPM, and uncompressed RGB TIFF files or byte arrays
- rendered ChartForgeX layers for charts, chart grids, visual blocks, visual grids, and topology diagrams
- anchor-based placement for all built-in canvas layers and rendered ChartForgeX layers
- feature strips for compact bottom rows
- SVG, HTML, PNG, BMP, PPM, and TIFF export

Example:

```csharp
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

var cpuChart = Chart.Create()
    .WithSize(220, 120)
    .WithTransparentBackground()
    .WithHeader(false)
    .WithCard(false)
    .AddLine("CPU", new[] {
        new ChartPoint(1, 18),
        new ChartPoint(2, 31),
        new ChartPoint(3, 24),
        new ChartPoint(4, 45),
        new ChartPoint(5, 39)
    });

var ramCard = MetricCard.Create()
    .WithSize(180, 104)
    .WithTransparentBackground()
    .WithCard(false)
    .WithMetric("RAM", "41%")
    .WithMiniSparkline(new[] { 32d, 36d, 41d, 38d, 43d });

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
    .AddInfoTile(892, 70, 250, 96, "CPU", "CPU", "Intel Core i7-12700K", "23%", ChartColor.FromHex("#60A5FA"), 0.23, VisualCanvasInfoTileSurfaceStyle.Raised, VisualCanvasInfoTileIconKind.Cpu, VisualCanvasInfoTileMiniChartKind.Area, new[] { 18d, 26d, 22d, 37d, 48d, 43d }, 100)
    .AddKeyValueBlock(
        VisualCanvasPlacement.At(VisualCanvasAnchor.BottomLeft, 66, 50),
        300,
        new[] {
            VisualCanvasKeyValueItem.LabelRow("System"),
            VisualCanvasKeyValueItem.Pair("Host", "DEV-WKS-01"),
            VisualCanvasKeyValueItem.Pair("IPv4", "10.0.0.42 192.168.1.42"),
            VisualCanvasKeyValueItem.Pair("Domain", "corp.example.test")
        },
        valueWrapWidth: 170,
        labelColor: ChartColor.FromHex("#C4D4EC"),
        valueColor: ChartColor.FromHex("#F8FAFC"))
    .AddHeroBadge(538, 157, 124, 88, ">_", ChartColor.FromHex("#22A7FF"))
    .AddHeroTitle(312, 296, 576, 82, new[] {
        new VisualCanvasTextRun("Power", ChartColor.FromHex("#F8FAFC")),
        new VisualCanvasTextRun("BGInfo", ChartColor.FromHex("#2F80FF"))
    })
    .AddText(330, 402, 540, "Desktop background insights for Windows and PowerShell", 24, ChartColor.FromHex("#C6D3EA"), VisualCanvasTextAlignment.Center)
    .AddChart(VisualCanvasPlacement.At(VisualCanvasAnchor.BottomCenter, -160, 50), 220, 120, cpuChart, VisualCanvasImageFit.Contain)
    .AddVisualBlock(VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 106, 74), 180, 104, ramCard, VisualCanvasImageFit.Center);

canvas.SaveSvg("powerbginfo-social-preview.svg");
canvas.SavePng("powerbginfo-social-preview.png");
canvas.SaveBmp("powerbginfo-social-preview.bmp");
```

To start from an existing background image without `System.Drawing` or platform-specific graphics APIs, decode it through the reusable raster input path and place it as the first canvas layer:

```csharp
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Raster;

var background = RasterImageDecoder.Read("wallpaper.png");

var canvas = background
    .ToVisualCanvas()
    .AddKeyValueBlock(
        VisualCanvasPlacement.At(VisualCanvasAnchor.TopLeft, 20, 20),
        360,
        new[] {
            VisualCanvasKeyValueItem.Pair("Host", "DEV-WKS-01"),
            VisualCanvasKeyValueItem.Pair("IPv4", "10.0.0.42 192.168.1.42")
        });

canvas.SavePng("wallpaper-with-info.png");
```

`AddImageFile(...)` and `AddImageBytes(...)` are available when an image should be placed into an existing canvas region. The dependency-free decoder supports baseline JPEG, PNG, BMP, PPM, and uncompressed RGB TIFF. Progressive JPEG remains outside the built-in decoder for now; hosts that need unsupported image variants can decode them before handing RGBA pixels to `AddRasterImage(...)`.

For user-supplied files, use `RasterImageDecoder.TryRead(...)` or `TryDecode(...)` when unsupported or corrupt images should be handled as a normal validation result instead of an exception.

`VisualCanvasPlacement` resolves layer coordinates from a named anchor. For `TopLeft`, offsets move right and down from the top-left edge. For `BottomRight`, positive offsets are insets from the right and bottom edges, so `VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 20, 20)` places a layer 20 pixels from the bottom-right corner. Center anchors use offsets as signed nudges from the centered position.

The same placement object can resolve against another region:

```csharp
var tile = new VisualCanvasInfoTileLayer(20, 20, 240, 90, "PC", "HOST", "WK01");
var badgeBounds = VisualCanvasPlacement.At(VisualCanvasAnchor.TopRight, 8, 8).Resolve(tile.Bounds, 42, 24);
```

PowerBGInfo-style desktop generation should stay thin: resolve Windows facts in PowerBGInfo, then pass those strings into `VisualCanvas` templates or layers. Keep reusable layout, typography, image, and tile behavior in ChartForgeX so other hosts can reuse the same engine for OpenGraph images, generated documentation, and report covers.
