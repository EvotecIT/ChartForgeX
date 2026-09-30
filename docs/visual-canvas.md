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
- image layers using SVG hrefs and host-provided RGBA pixels for raster output, with `Stretch`, `Contain`, `Cover`, `Center`, and `Tile` fit modes
- dependency-free raster image input for baseline/progressive JPEG, PNG, GIF first frames, BMP, PPM, and uncompressed RGB TIFF files or byte arrays
- rendered ChartForgeX layers for charts, chart grids, visual blocks, visual grids, and topology diagrams
- anchor-based placement for all built-in canvas layers and rendered ChartForgeX layers
- feature strips for compact bottom rows
- SVG, HTML, PNG, GIF, JPEG, BMP, PPM, and TIFF export

Example:

```csharp
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using ChartForgeX.Raster;
using System.IO;
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
    .AddText(330, 402, 540, "Desktop background insights for Windows and PowerShell", 24, ChartColor.FromHex("#C6D3EA"), TextAlignment.Center)
    .AddChart(VisualCanvasPlacement.At(VisualCanvasAnchor.BottomCenter, -160, 50), 220, 120, cpuChart, VisualCanvasImageFit.Contain)
    .AddVisualBlock(VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 106, 74), 180, 104, ramCard, VisualCanvasImageFit.Center);

canvas.SaveSvg("powerbginfo-social-preview.svg");
canvas.SavePng("powerbginfo-social-preview.png");
canvas.Save("powerbginfo-social-preview.gif");
canvas.Save("powerbginfo-social-preview.jpg", new RasterImageOptions { JpegQuality = 92, PngCompressionLevel = 9 });
canvas.SaveBmp("powerbginfo-social-preview.bmp");
```

## Responsive output

Keep layer coordinates in one design space while producing multiple target sizes with `WithResponsiveLayout(...)` and `WithOutputSize(...)`:

```csharp
canvas
    .WithResponsiveLayout(1200, 630, VisualCanvasImageFit.Cover)
    .WithOutputSize(1920, 1080)
    .SavePng("powerbginfo-wallpaper.png");
```

`Contain`, `Cover`, and `Stretch` use the same design-space transform in SVG and PNG. This lets a host reuse one wallpaper, report-cover, or social-preview composition without recalculating every layer position.

The generated example gallery includes a native 1920x1080 endpoint wallpaper and a 1200x630 social preview. Both are rendered as SVG, static HTML, and 2x-density PNG and participate in the shared visual baseline, so layout and renderer parity are checked at the dimensions real hosts consume.

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

`AddImageFile(...)`, `AddImageBytes(...)`, and `AddHeroBadgeImageFile(...)` are available when an image should be placed into an existing canvas region or inside the central hero badge. The dependency-free decoder supports baseline/progressive JPEG, PNG, BMP, PPM, and uncompressed RGB TIFF. Hosts that need unsupported image variants can decode them before handing RGBA pixels to `AddRasterImage(...)` or `AddHeroBadge(...)`.

```csharp
var brandedCanvas = VisualCanvas.CreateSocialPreview()
    .AddHeroBadgeImageFile(
        538,
        157,
        124,
        88,
        "logo.png",
        fit: VisualCanvasImageFit.Contain,
        padding: 12);
```

For lower-level wallpaper and report generation where the host wants to work directly with RGBA pixels, use `ImageComposition`. It keeps the same anchor and fit model as `VisualCanvas`, but focuses on image-engine operations: load or create a background, alpha-blend overlays, draw rectangle outlines, draw text, place ChartForgeX layers, and save by extension without `System.Drawing`.

Charts, chart grids, visual blocks, visual grids, visual canvases, and topology charts expose `ToRgbaImage(...)`. Use that direct path when the next operation is composition: encoding to PNG and decoding it immediately adds work without changing the pixels. Keep `ToPng()` and the generic raster exporters at file, stream, HTTP, clipboard, and other encoded-image boundaries.

```csharp
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

var wallpaper = ImageComposition
    .FromFile("wallpaper.jpg")
    .DrawImageFile("logo.png", VisualCanvasPlacement.At(VisualCanvasAnchor.TopRight, 32, 32), 220, 90, VisualCanvasImageFit.Contain, opacity: 0.92)
    .StrokeRectangle(28, 26, 530, 58, ChartColors.White, thickness: 2)
    .DrawCallout(558, 55, 590, 28, 180, 42, "HTML capture", 16, ChartColors.Yellow, ChartColor.FromRgba(0, 0, 0, 196), ChartColors.White)
    .DrawText(32, 32, 520, "DEV-WKS-01", 34, ChartColors.White, emphasized: true);

wallpaper.Save("wallpaper-output.jpg", new RasterImageOptions {
    Background = ChartColors.Black,
    JpegQuality = 92,
    PngCompressionLevel = 9
});

using var output = File.Create("wallpaper-output.png");
wallpaper.Write(output, RasterImageFormat.Png, new RasterImageOptions { PngCompressionLevel = 9 });
```

### Shapes, gradients, and SVG backdrops

`ImageComposition` draws antialiased circles, ellipses, arcs, and gradient rectangles directly, with the same coverage model the SVG rasterizer uses for fills and strokes:

```csharp
var card = ImageComposition.Create(1200, 630, ChartColor.FromHex("#0A0D12"))
    .FillRectangleRadialGradient(0, 0, 1200, 630, 1200, 0, 700, ChartColor.FromRgba(44, 95, 240, 51), ChartColor.FromRgba(44, 95, 240, 0))
    .FillRectangleLinearGradient(84, 538, 260, 42, ChartColor.FromHex("#3BCBB5"), ChartColor.FromHex("#7399FF"), angle: 0, radius: 21)
    .FillCircle(120, 90, 26, ChartColor.FromHex("#2C5FF0"))
    .StrokeCircle(120, 90, 34, ChartColors.White, thickness: 1.5)
    .DrawProgressRing(965, 270, 96, 18, fraction: 0.72, trackColor: ChartColor.FromHex("#222A38"), color: ChartColor.FromHex("#FF6D7A"))
    .DrawArc(965, 270, 120, startAngle: -90, sweepAngle: 90, ChartColors.White, thickness: 2, ImageLineCap.Butt);
```

Angles are in degrees, clockwise on screen from three o'clock, so `-90` starts at twelve o'clock. `DrawArc` takes `ImageLineCap.Butt`, `Round` (the default), or `Square`. `DrawProgressRing` paints a full track and a round-capped arc for `fraction` of it; a fraction of one or more fills the ring. Linear gradient angles follow the same convention: `0` runs left to right and `90` top to bottom. A radial gradient's center is in composition coordinates and may sit outside the rectangle, which is how a corner glow is drawn; fade to the same color with zero alpha to avoid a gray fringe.

When the backdrop is easier to describe as SVG, rasterize it straight to pixels and keep composing. `SvgRasterizer.ToImage(svg)` returns the `RgbaImage` that `SvgRasterizer.ToPng(svg)` would encode, so nothing is encoded and decoded on the way:

```csharp
var composition = ImageComposition.FromImage(SvgRasterizer.ToImage(backdropSvg));
composition.DrawText(84, 196, 650, title, titleStyle, TextWrapMode.Word, maximumLines: 3);
```

Stroked SVG circles, ellipses, arcs, rounded rectangles, and curved paths are flattened to the output resolution and outlined as one shape, so they are as smooth as fills at any stroke width. `stroke-linecap` (`butt`, `round`, `square`), `stroke-linejoin` (`miter`, `round`, `bevel`), `stroke-miterlimit`, and `stroke-dasharray` are honoured. A gradient used as a stroke paint is drawn in its first stop color.

### Fonts for composed text

Text drawn from a `FontSpec` (`ImageComposition.DrawText` and `TextLayoutEngine`) picks its face in this order:

1. `FontSpec.FromFile(path)` — that TrueType file, always. This is the only choice that renders identically on every host, so ship the font with the application when the output must not vary.
2. `FontSpec.FromFamily("Inter, Segoe UI, sans-serif")` — the first family of the stack that is registered with `FontRegistry` (below) or installed, at the face closest to `Weight` and `Italic` (Segoe UI at 700 is Segoe UI Bold). Installed families are read from the operating system's font folders the first time a named family is requested; only TrueType outlines (`.ttf`, `.ttc`) are considered.
3. The generic fallback for the stack — a serif, monospace, or sans-serif face known to ship with Windows, macOS, or common Linux distributions — with its bold or italic sibling when one is installed.

When no real bold or italic face exists, bold is synthesized by emboldening and italic by shearing the regular face. A variable font renders its default instance; its axes are not applied.

SVG `<text>` rasterized by `SvgRasterizer` picks its face the same way from its computed `font-family` stack, `font-weight` (numbers from 1 to 1000, `normal`, `bold`, and `bolder`/`lighter` relative to the parent), and `font-style` (`italic` or `oblique`), whether they come from attributes, `style="..."`, a stylesheet, an ancestor `<g>`, or a `<tspan>`. Text without a `font-family` uses the sans-serif fallback. Each run is placed so its face's own baseline sits on the `y` coordinate, as a browser places it.

`VisualCanvas` text follows the same rules in both outputs. `VisualCanvasTheme.FontFamily` (set directly or through `VisualDesignTokens`), a key/value block's `FontFamilyName`, and `MonospaceFontFamily` for hero badge symbols are resolved at the weights the SVG output writes: 500 for plain text and values, 800 for emphasized text, tile icons, and feature icons, 850 for hero titles and badges, and 700, 650, and 500 for tile labels, values, and details. PNG output draws the matching installed face (Segoe UI 850 is Segoe UI Black), and fitting, wrapping, ellipsis, key/value column widths, row heights, and tile text fitting are measured with that face in both outputs, so fitted PNG text does not overflow its box and the SVG output breaks lines where the PNG does. Only a family without a bold face is emboldened. Call `block.MeasureHeight(canvas.Theme)` to measure a key/value block with the family it will draw with.

On a host with no fonts at all, such as a bare `mcr.microsoft.com/dotnet/aspnet` container, nothing throws: text is drawn with a small built-in bitmap font. That is legible but not presentable, so containers should either install a font package or ship `.ttf` files with the application and register them.

`FontRegistry` registers font files once, process-wide and thread-safely, and every raster path then finds them by family name: chart, grid, topology, and visual block themes, VisualCanvas themes and design tokens, `FontSpec.FromFamily`, and SVG `font-family`:

```csharp
FontRegistry.Register("Inter", Path.Combine(fonts, "Inter-Regular.ttf"));
FontRegistry.Register("Inter", Path.Combine(fonts, "Inter-Bold.ttf"), weight: 700);
FontRegistry.RegisterDirectory(Path.Combine(fonts, "brand")); // each file under its own family, weight, and slant
FontRegistry.Register("sans-serif", Path.Combine(fonts, "Inter-Regular.ttf")); // fallback for stacks ending in sans-serif, and for hosts with no fonts

var chart = Chart.Create().WithTheme(ChartTheme.ReportDark().WithFontFamily("Inter, sans-serif"));
```

A registered family is matched before an installed family of the same name, with the same weight and slant rules, and only its registered faces are considered, so register each weight you use; a missing bold is synthesized. `RegisterFile` and `RegisterDirectory` read the family, weight, and italic flag from each file's own tables. `FontRegistry.Clear()` removes every registration.

Chart, grid, topology, and visual block PNG renderers resolve their theme font stack (and a text style's `FontFamily`) the same way, at regular weight, and draw emphasized text such as titles, legends, and data labels with that family's real bold face, measuring it with the same face, so `ChartFontStacks.SystemSans` draws Segoe UI on Windows as the SVG does in a browser. An explicit `PngFontPath` keeps its synthesized emphasis. `chart.GetPngFontInfo()` reports the resolved face. `TextMeasurementMode.InstalledFonts` measures with the same regular and bold faces, and `TextMeasurementMode.PortableEstimate` still never inspects host fonts.

For user-supplied files, use `RasterImageDecoder.TryRead(...)`, `RasterImageDecoder.TryDecode(...)`, `ImageComposition.TryFromFile(...)`, or `ImageComposition.TryFromBytes(...)` when unsupported or corrupt images should be handled as a normal validation result instead of an exception.

`VisualCanvasPlacement` resolves layer coordinates from a named anchor. For `TopLeft`, offsets move right and down from the top-left edge. For `BottomRight`, positive offsets are insets from the right and bottom edges, so `VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 20, 20)` places a layer 20 pixels from the bottom-right corner. Center anchors use offsets as signed nudges from the centered position.

The same placement object can resolve against another region:

```csharp
var tile = new VisualCanvasInfoTileLayer(20, 20, 240, 90, "PC", "HOST", "WK01");
var badgeBounds = VisualCanvasPlacement.At(VisualCanvasAnchor.TopRight, 8, 8).Resolve(tile.Bounds, 42, 24);
```

PowerBGInfo-style desktop generation should stay thin: resolve Windows facts in PowerBGInfo, then pass those strings into `VisualCanvas` templates or layers. Keep reusable layout, typography, image, and tile behavior in ChartForgeX so other hosts can reuse the same engine for OpenGraph images, generated documentation, and report covers.
