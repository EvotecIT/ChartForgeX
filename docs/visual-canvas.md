# Visual Canvas

`VisualCanvas` is a fixed-size layered composition surface for visuals that are not grids: desktop wallpapers, social preview images, report covers, kiosk screens, and product hero graphics.

Install `ChartForgeX.Visuals` for the canvas API. Add `ChartForgeX.Stories` when encoding its completed pixels as GIF.

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
- SVG, HTML, PNG, JPEG, BMP, PPM, and TIFF export, plus GIF encoding through Stories

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
canvas.ToRgbaImage().SaveGif("powerbginfo-social-preview.gif"); // Requires ChartForgeX.Stories.
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

All raster decoders bound encoded input and pixel counts before allocating image buffers. The defaults are 64 MiB of input and 67,108,864 pixels (256 MiB of RGBA output). Codec working buffers use additional memory. Set smaller limits for uploads or other untrusted input; the same options work with files, byte arrays, and non-seekable streams:

```csharp
var limits = new RasterDecodeOptions {
    MaximumEncodedBytes = 8 * 1024 * 1024,
    MaximumPixels = 4_000_000
};
var uploadedImage = RasterImageDecoder.Read(uploadStream, limits);
```

`Read` leaves the supplied stream open. Oversized input, invalid dimensions, and PNG data that expands beyond its declared scanlines throw `InvalidDataException`; `TryRead` and `TryDecode` return `false`. Applications that intentionally decode larger trusted images can increase the limits explicitly.

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

Use `SvgRasterizer.Rasterize(svg)` when the host needs to see rendering losses as well as pixels:

```csharp
var result = SvgRasterizer.Rasterize(backdropSvg);
foreach (var warning in result.Diagnostics)
    Console.WriteLine($"{warning.Code}: {warning.ElementName}#{warning.ElementId}: {warning.Message}");
var composition = ImageComposition.FromImage(result.Image);
```

Diagnostics identify unsupported visible elements (`SFR001`, including `foreignObject` and `textPath`), ignored filters (`SFR002`, including CSS filters), undecodable embedded images (`SFR003`), and unresolved or depth-limited `use` references (`SFR004`). Nested SVG images retain these diagnostics. At most 256 entries are returned; `SFR999` reports truncation. `Rasterize(svg, strict: true)` throws `NotSupportedException` when diagnostics are present. `ToImage` and `ToPng` retain their best-effort behavior. An empty diagnostic list does not guarantee complete browser SVG fidelity; shaping, stroke paints, and other subset limitations still apply.

Stroked SVG circles, ellipses, arcs, rounded rectangles, and curved paths are flattened to the output resolution and outlined as one shape, so they are as smooth as fills at any stroke width. `stroke-linecap` (`butt`, `round`, `square`), `stroke-linejoin` (`miter`, `round`, `bevel`), `stroke-miterlimit`, and `stroke-dasharray` are honoured. A gradient used as a stroke paint is drawn in its first stop color.

### Fonts for composed text

Text drawn from a `FontSpec` (`ImageComposition.DrawText` and `TextLayoutEngine`) picks its face in this order:

1. `FontSpec.FromFile(path)` — that font file, always. This is the only choice that renders identically on every host, so ship the font with the application when the output must not vary.
2. `FontSpec.FromFamily("Inter, Segoe UI, sans-serif")` — the first family of the stack that is registered with `FontRegistry` (below) or installed, at the face closest to `Weight` and `Italic` (Segoe UI at 700 is Segoe UI Bold). Installed families are read from the operating system's font folders the first time a named family is requested. TrueType and CFF outlines are both read: `.ttf`, `.otf`, and `.ttc`/`.otc` collections, including CID-keyed CJK fonts such as Noto Sans CJK and CFF2 variable fonts.
3. The generic fallback for the stack — a serif, monospace, or sans-serif face known to ship with Windows, macOS, or common Linux distributions — with its bold or italic sibling when one is installed.

When no real bold or italic face exists, bold is synthesized by emboldening and italic by shearing the regular face. An explicit supported `wght` axis uses the font's weight outline instead of synthetic bold; `ital` or `slnt` similarly replaces synthetic italic. Without explicit axes, a variable font uses its default instance.

Set `FontSpec.Variations` to immutable `FontVariationSettings`, or build a selection with `FontSpec.FromFile(path).WithVariation("wght", 650).WithVariation("wdth", 85)`. Chart text-role overrides expose the same `WithVariation` method. Null on an override inherits; `FontVariationSettings.Default` resets to the font's default instance. Tags are case-sensitive four-character ASCII letters or digits. Values must be finite; each face clamps recognized values to its `fvar` range and ignores axes it does not provide. A fallback face receives the same requested axes and applies its own ranges.

The canonical font reader applies `avar` version 1 mapping, TrueType `gvar` contour/composite deltas, phantom metrics, `HVAR`/`MVAR`, CFF2 blends, variation feature substitutions and GDEF-backed GPOS `VariationIndex` adjustments. Measurement, direct drawing, fitted and rotated buffers use the same instance. SVG and HTML write `font-variation-settings`; SVG rasterization inherits the CSS value through groups and spans, including `normal` resets. Native SVG relies on the browser's font engine and available font files. Selection does not automatically infer weight or optical size from `FontSpec.Weight` or `TextStyle.FontSize`.

`avar` version 2 mapping and non-default COLR v1 paint variation are not applied. TrueType instruction programs are not executed. Small-text hinting aligns vertical landmarks; the opt-in full mode also fits narrow straight stems horizontally and limits their darkening.

SVG `<text>` rasterized by `SvgRasterizer` picks its face the same way from its computed `font-family` stack, `font-weight` (numbers from 1 to 1000, `normal`, `bold`, and `bolder`/`lighter` relative to the parent), and `font-style` (`italic` or `oblique`), whether they come from attributes, `style="..."`, a stylesheet, an ancestor `<g>`, or a `<tspan>`. Text without a `font-family` uses the sans-serif fallback. Each run is placed so its face's own baseline sits on the `y` coordinate, as a browser places it.

`VisualCanvas` uses `VisualCanvasTheme.TextMeasurementMode = TextMeasurementMode.InstalledFonts` by default. Its fitting and line metrics use the same font resolution and shaping as raster text. Register the application's font files when layout must remain identical across generating hosts. Set `TextMeasurementMode.PortableEstimate` explicitly for legacy host-independent estimated layout; unresolved fonts also retain the portable fallback.

`VisualCanvasTheme.FontFamily` (set directly or through `VisualDesignTokens`), a key/value block's `FontFamilyName`, and `MonospaceFontFamily` for hero badge symbols are resolved at the weights the SVG output writes: 500 for plain text and values, 800 for emphasized text, tile icons, and feature icons, 850 for hero titles and badges, and 700, 650, and 500 for tile labels, values, and details. PNG draws the matching registered or installed face. Only a family without a bold face is emboldened. Call `block.MeasureHeight(canvas.Theme)` to measure a key/value block with the canvas's measurement mode.

#### Characters the face does not have, right-to-left text, and Arabic

A face rarely covers every script a server name, user name, or vendor title can contain. Raster text is therefore drawn per character cluster (a base character with its combining marks, joiners, and variation selectors), each by the first face that has all of it:

1. the face chosen above;
2. the families that follow it in the requested stack (`"Inter, Noto Sans JP, sans-serif"` draws Japanese in Noto Sans JP);
3. every family registered with `FontRegistry`;
4. the platform's broad-coverage fonts, where installed: on Windows Segoe UI, Segoe UI Symbol, Segoe UI Emoji, Microsoft YaHei, Microsoft JhengHei, Yu Gothic, Malgun Gothic, Nirmala UI, Leelawadee UI, Ebrima, Gadugi, and Arial Unicode MS; on Linux the Noto families and DejaVu Sans; on macOS PingFang, Hiragino, Apple SD Gothic Neo, Geeza Pro, and similar.

Each fallback family is matched to the run's weight and slant, and when that face lacks the character its other faces are tried, so bold Arabic in a Segoe UI Black headline comes from Segoe UI Bold or Regular. A regular fallback face in a run requesting bold weight receives synthetic ink without changing its advance, even when the primary face is already bold. Existing bold fallback faces, explicitly selected variable weight axes and colour glyphs retain their authored ink. A character no face has takes the chosen face's `.notdef` advance and draws nothing. Coverage is answered from each file's `cmap` alone and cached per code point, so a fallback face is read only when it draws something, and text that the chosen face covers entirely never consults the chain. Measuring, wrapping, fitting, and drawing all use the same clusters, so fitted text with fallback characters stays inside its box. A combining sequence such as `e` + U+0301 is drawn as the precomposed `é` when the face has it.

Lines are reordered for display with the Unicode Bidirectional Algorithm (UAX #9): explicit embeddings, overrides, and isolates, weak and neutral types, paired brackets, whitespace reset, and mirrored brackets at right-to-left levels. The paragraph direction is left-to-right, as SVG and CSS default to. Arabic letters take their initial, medial, final, or isolated forms from their neighbours' Unicode joining types. Fonts supply these glyphs and required ligatures through GSUB; faces without joining features use covered Arabic Presentation Forms. Canonically equivalent Hebrew presentation glyphs are used when a face supplies them instead of substitution lookups.

The shared glyph layout reads the font's selected language system for each script. It applies composition, localization, required and standard ligature features, contextual substitutions, and the Arabic joining features. GPOS supplies pair and class kerning, mark-to-base, mark-to-ligature, mark-to-mark, and cursive attachment. Measurement and painting use the same positioned glyphs, including fallback-face metrics. SVG text buffers reserve the ink bounds of stacked marks rather than clipping them to the ordinary line height. Different SVG paint spans retain their own glyphs and colours; ligatures do not combine characters from different paint owners. Optional malformed layout lookups are bounded and skipped while the face's usable outlines remain available.

`TextStyle.OpenTypeLanguageTag` selects a case-sensitive font language tag, such as `SRB` or `TRK`, without changing the text's casing or direction. Null selects the default language system; an absent tag in a font also falls back to its default. Tags are one to four ASCII letters or digits, padded to four characters, and are not BCP 47 culture names. Selection applies independently to each script and fallback face, and cached default runs remain separate from localized runs. Chart and grid role styles expose the same setting through `TextStyleOverride.WithOpenTypeLanguage`; an override of `normal` restores the default. SVG uses CSS `font-language-override`, inherited by nested spans; native display requires a browser implementing that property and a font providing those forms.

GPOS device tables supply size-specific kerning, placement and anchor corrections. The renderer selects the nearest whole logical font size (half sizes round up), converts the font's signed pixel deltas to design units, and uses the corrected run for measurement and painting. Fractional sizes scale that run normally. PNG output scaling and dependency-free SVG rasterization magnify the same layout rather than selecting another correction size, so increasing export resolution preserves fitting and wrapping. Mixed-size SVG spans and fallback faces retain their own sizes and font metrics. Invalid optional correction data is ignored without losing usable design-unit positioning. GDEF-backed variation adjustments follow the selected axis coordinates. Native SVG display uses the browser's font engine and size policy. Contour-point anchor hinting remains separate work.

Emoji use the selected face's colour data when available. COLR v0 layers use the selected CPAL palette, with the foreground palette index taking the text colour. COLR v1 adds linear, radial and sweep gradients, affine transforms, clipping and composite modes; gradient interpolation and composition use linear-light colour values. Detailed vector glyphs reduce internal antialias sampling when necessary to retain colour within bounded rendering work. CBDT/CBLC and sbix faces use the nearest available bitmap strike at the output pixel size, preferring the larger strike for ties. PNG, JPEG and uncompressed RGB TIFF bitmap data use the existing image codecs. Fitted and rotated buffers retain colour ink bounds and output pixel density. Colour glyphs receive text opacity once after composition and do not receive synthetic bold copies.

A cluster with U+FE0F, or a pictograph that defaults to emoji presentation, tries emoji fallback faces before ordinary text faces. An explicitly selected colour face that covers the cluster takes precedence. Register colour-only fonts through the same `FontRegistry` methods as text fonts; font files remain application assets and are not bundled with ChartForgeX. Fonts without usable colour data retain their monochrome outlines in the text colour. Unsupported, cyclic or excessive optional paint and bitmap data fall back to usable outlines. `FontSpec.ColorPaletteIndex` selects a zero-based CPAL palette for COLR layers and gradients without changing advances. Zero selects the default; an absent or unusable alternate palette also uses zero. `FontSpec.WithColorPalette(index)` and `TextStyleOverride.WithColorPalette(index)` expose the same selection. A null role override inherits and zero explicitly resets; clones, fallback clusters, fitted text and rotated text retain the selection. Bitmap strikes and the special foreground entry are unaffected. Native SVG and HTML emit CSS `font-palette` and matching `@font-palette-values` rules for named families in the requested stack; those fonts must be available to the browser. Imported SVG supports named base-palette rules and inherited normal resets, but not CSS palette overrides, interpolation, or automatic light/dark selection. COLR v1 paint variation retains base-instance values. Native browser colour interpolation and bitmap rounding can differ from the deterministic raster output.

Script-specific shaping groups virama-connected consonants into one fallback syllable. Devanagari, Bengali, Gurmukhi, Gujarati, Odia, Tamil, Telugu, Kannada and Malayalam use font-selected consonant forms, ordered Indic feature stages and vowel/reph reordering. Modern Indic tags are preferred when the font declares them; older tags use their consonant-plus-halant convention. Thai and Lao AM vowels decompose and reorder their ring before tone marks. Khmer coeng forms, split vowels and register shifters use their own feature stages. The selected font supplies the substitutions and attachment anchors; a font without those forms cannot produce the same result.

Sinhala decomposes split vowels, retains explicit joiners during normalisation and places repaya after the font's consonant forms. Myanmar fonts declaring the modern `mym2` script model reorder kinzi, medial ra, pre-base vowels and anusvara, with font-driven stacked consonants and positioned marks. These scripts zero font-classified mark advances before required distance adjustments, so measurement and drawing retain the same width. Legacy Myanmar `mymr` fonts retain the generic layout path; legacy encodings such as Zawgyi are not converted. The gallery's `sinhala-myanmar-showcase` uses a font stack covering both scripts.

GSUB can combine emoji ZWJ sequences when the selected face provides the ligature. Device-size GPOS corrections and variable positioning follow the font size and selected axes. Correctly shaped syllables can have different widths from nominal character sequences, so fitted labels and collision reservations use the shaped advances.

#### Small text

Text drawn at 12 output pixels or smaller is lightly hinted, independently of the font: the run's baseline moves to a whole pixel and each glyph is stretched vertically so its face's x-height and cap height also land on whole pixels. Nothing moves horizontally, so advances, widths, wrapping, and fitting are exactly those of unhinted text. Larger text is drawn as designed. Set `TextStyle.Hinting = TextHinting.None` for composed text, or `ChartOptions.PngTextHinting = TextHinting.None` for chart PNGs, where exact outline geometry matters more than crisp rows, such as frames of an animation that moves text by fractions of a pixel. SVG `<text>` is hinted in its own glyph buffer, which is then placed on a whole pixel row when it is only translated. `TextHinting.Auto` retains fractional horizontal positions and does not darken stems. `TextHinting.Full` adds bounded horizontal fitting of narrow, straight stems and at most 0.35 output pixels of stem-width adjustment. It can move ink by less than one output pixel while advances, wrapping, and fitted widths retain the same layout. Counter spaces remain open; ambiguous or very complex outlines retain vertical fitting, and synthetic slants, italic faces, explicitly selected `ital`/`slnt` axes and colour glyphs keep their existing policy. Set `TextStyle.Hinting = TextHinting.Full` for composition or `chart.Options.PngTextHinting = TextHinting.Full` for chart PNGs. The decision uses final output pixel size, so a 10px label receives fitting at 1x and retains its designed outline at 2x. Fitted and rotated `Full` buffers retain the final output density, including logical 5px text rendered at 2x. Native SVG and HTML keep the browser font engine; imported SVG rasterization retains the default vertical fitting.

`TextHinting.Full` also uses authored horizontal EBDT/EBLC monochrome or grayscale strikes when their size exactly matches the final output pixel size. Coverage uses the text colour and opacity, while advances remain those of the shaped outline font. Byte-aligned and packed image formats 1, 2, 5, 6 and 7 support 1-, 2-, 4- and 8-bit coverage. A 6px label rendered at 2x can use a 12px strike; antialias sampling does not change strike selection. Fitted and rotated buffers retain bitmap overhangs. `Auto` and `None` retain their outline policy. Fractional or absent strikes, synthetic bold or italic, non-default variable instances, component bitmap records and EBSC scaling retain outline rendering. Browser font engines may use different strikes and advances.

On a host with no fonts at all, such as a bare `mcr.microsoft.com/dotnet/aspnet` container, nothing throws: text is drawn with a small built-in bitmap font. That is legible but not presentable, so containers should either install a font package or ship `.ttf` files with the application and register them.

`FontRegistry` registers font faces once, process-wide and thread-safely, and every raster path then finds them by family name: chart, grid, topology, and visual block themes, VisualCanvas themes and design tokens, `FontSpec.FromFamily`, and SVG `font-family`:

```csharp
FontRegistry.Register("Inter", Path.Combine(fonts, "Inter-Regular.ttf"));
FontRegistry.Register("Inter", Path.Combine(fonts, "Inter-Bold.ttf"), weight: 700);
FontRegistry.RegisterDirectory(Path.Combine(fonts, "brand")); // each file under its own family, weight, and slant
FontRegistry.Register("sans-serif", Path.Combine(fonts, "Inter-Regular.ttf")); // fallback for stacks ending in sans-serif, and for hosts with no fonts

var chart = Chart.Create().WithTheme(ChartTheme.ReportDark().WithFontFamily("Inter, sans-serif"));
```

Hosts without filesystem font assets can register the same faces from bytes or embedded resource streams:

```csharp
FontRegistry.Register("Inter", fontBytes);
using var stream = typeof(Program).Assembly.GetManifestResourceStream("MyApp.Inter-Bold.ttf");
FontRegistry.Register("Inter", stream!, weight: 700);
```

The byte overload retains a copy, so later edits to `fontBytes` do not change the face. The stream overload reads from the current position to the end, supports non-seekable streams, and leaves the stream open. Both accept the same weight, italic flag and collection index as file registration. Memory faces take part in measurement, drawing and fallback without temporary font files; their resolved file path is null. Browser SVG still needs the matching font through CSS or `FontFace` for the browser to draw its text.

A registered family is matched before an installed family of the same name, with the same weight and slant rules, and only its registered faces are considered, so register each weight you use; a missing bold is synthesized. `RegisterFile` and `RegisterDirectory` read the family, weight, and italic flag from each file's own tables. Missing or invalid font files can be retried after they become available. The file cache detects changes to file size or modification time and retains at most 128 files and 64 MiB of font payloads. `FontRegistry.Clear()` removes every registration and clears the file cache; use it before re-registering a replacement that retains its original size and timestamp.

Chart, grid, topology, and visual block PNG renderers resolve their theme font stack (and a text style's `FontFamily`) the same way, at regular weight, and draw emphasized text such as titles, legends, and data labels with that family's real bold face, measuring it with the same face, so `ChartFontStacks.SystemSans` draws Segoe UI on Windows as the SVG does in a browser. An explicit `PngFontPath` keeps its synthesized emphasis. `chart.GetPngFontInfo()` reports the resolved face. `TextMeasurementMode.InstalledFonts` measures with the same regular and bold faces, and `TextMeasurementMode.PortableEstimate` still never inspects host fonts.

For user-supplied files, use `RasterImageDecoder.TryRead(...)`, `RasterImageDecoder.TryDecode(...)`, `ImageComposition.TryFromFile(...)`, or `ImageComposition.TryFromBytes(...)` when unsupported or corrupt images should be handled as a normal validation result instead of an exception.

`VisualCanvasPlacement` resolves layer coordinates from a named anchor. For `TopLeft`, offsets move right and down from the top-left edge. For `BottomRight`, positive offsets are insets from the right and bottom edges, so `VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 20, 20)` places a layer 20 pixels from the bottom-right corner. Center anchors use offsets as signed nudges from the centered position.

The same placement object can resolve against another region:

```csharp
var tile = new VisualCanvasInfoTileLayer(20, 20, 240, 90, "PC", "HOST", "WK01");
var badgeBounds = VisualCanvasPlacement.At(VisualCanvasAnchor.TopRight, 8, 8).Resolve(tile.Bounds, 42, 24);
```

PowerBGInfo-style desktop generation should stay thin: resolve Windows facts in PowerBGInfo, then pass those strings into `VisualCanvas` templates or layers. Keep reusable layout, typography, image, and tile behavior in ChartForgeX so other hosts can reuse the same engine for OpenGraph images, generated documentation, and report covers.
