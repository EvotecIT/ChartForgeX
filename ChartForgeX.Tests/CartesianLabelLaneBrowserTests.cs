using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Checks actual browser glyph bounds alongside native pixels for the public grouped-stack recipe.</summary>
public sealed class CartesianLabelLaneBrowserTests {
    [Theory]
    [InlineData(360, VisualThemeMode.Light)]
    [InlineData(360, VisualThemeMode.Dark)]
    [InlineData(640, VisualThemeMode.Light)]
    [InlineData(640, VisualThemeMode.Dark)]
    public void NativeNormalizedReversedStackTotalsKeepPaintedCategoryLanesClear(int width, VisualThemeMode mode) {
        var chart = Recipe(width, mode);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var rasterOptions = new VisualRenderOptions();
        var image = PngReader.Decode(prepared.ToPng(rasterOptions));
        var totals = CaptionInk(prepared, image, rasterOptions, "stack-total-label", 8);
        var categories = CaptionInk(prepared, image, rasterOptions, "axis-x-label", 4);
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;

        Assert.All(totals, ink => Assert.True(ink.Top >= plot.Bottom + 2,
            "The reversed stack's complete native total ink must paint outside the plot, below its value end."));
        Assert.True(totals.Max(ink => ink.Bottom) + 2 <= categories.Min(ink => ink.Top),
            "Native total glyphs need a clear pixel gap before the category glyphs.");
        foreach (var lane in new[] { totals, categories }) {
            for (var index = 1; index < lane.Length; index++)
                Assert.True(lane[index - 1].Right + 1 <= lane[index].Left,
                    "Distinct native captions must retain separate painted bounds.");
        }
        Assert.All(totals.Concat(categories), ink => Assert.True(ink.Left >= 0 && ink.Right <= image.Width
            && ink.Top >= 0 && ink.Bottom <= image.Height, "Native caption ink must remain inside the PNG."));
    }

    [BrowserTheory]
    [InlineData(360, VisualThemeMode.Light)]
    [InlineData(360, VisualThemeMode.Dark)]
    [InlineData(640, VisualThemeMode.Light)]
    [InlineData(640, VisualThemeMode.Dark)]
    public async Task NormalizedReversedStackTotalsClearRenderedCategoryGlyphs(int width, VisualThemeMode mode) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Recipe(width, mode);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(svg + "<img style='display:block' src='data:image/png;base64,"
            + Convert.ToBase64String(png) + "'>", width, 720);
        await session.Page.EvaluateAsync("async () => { await document.fonts.ready; await document.querySelector('img').decode(); }");
        var defects = await session.Page.EvaluateAsync<string[]>("""
            () => {
                const svg = document.querySelector('svg'), defects = [];
                const totals = [...svg.querySelectorAll('[data-cfx-role="stack-total-label"]')];
                const categories = [...svg.querySelectorAll('[data-cfx-role="axis-x-label"]')];
                const axes = [...svg.querySelectorAll('[data-cfx-role^="axis-"]')].filter(node => node.querySelector('text'));
                const overlaps = (a, b) => a.left < b.right && a.right > b.left && a.top < b.bottom && a.bottom > b.top;
                if (totals.length !== 8 || totals.some(node => node.textContent !== '100')) defects.push('Complete totals were lost');
                if (categories.map(node => node.textContent).join(',') !== 'North,South,East,West') defects.push('Category names changed');
                for (const total of totals) {
                    const box = total.getBoundingClientRect();
                    if (axes.some(axis => overlaps(box, axis.getBoundingClientRect()))) defects.push('Total overlaps axis glyphs');
                    if (totals.some(other => other !== total && overlaps(box, other.getBoundingClientRect()))) defects.push('Totals overlap');
                }
                const image = document.querySelector('img');
                if (image.naturalWidth !== Number(svg.getAttribute('width')) || image.naturalHeight !== Number(svg.getAttribute('height')))
                    defects.push('Native PNG dimensions differ');
                return defects;
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var name = "cartesian-total-lanes-" + width + "-" + mode.ToString().ToLowerInvariant();
            await session.Page.ScreenshotAsync(new() { Path = Path.Combine(output, name + ".png"), FullPage = true });
            await File.WriteAllBytesAsync(Path.Combine(output, name + "-native.png"), png);
            await File.WriteAllTextAsync(Path.Combine(output, name + ".svg"), svg);
        }
        Assert.Empty(defects);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static Chart Recipe(int width, VisualThemeMode mode) => BarRecipe.Create("compact-options", mode).WithSize(width, 360)
        .WithTitle("Completed orders").WithSubtitle("Normalized grouped stacks")
        .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);

    private static ChartRect[] CaptionInk(PreparedVisual prepared, RgbaImage image, VisualRenderOptions options, string role, int count) {
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == role)
            .OrderBy(CaptionCenter).ToArray();
        Assert.Equal(count, labels.Length);
        var centers = labels.Select(CaptionCenter).ToArray();
        var scene = prepared.Scene;
        // Isolate only this caption family's paint, leaving bar fills, grid lines and other text intact.
        // The decoded public PNG remains the observed image; the control identifies its antialiased glyph pixels.
        var without = VisualSceneRasterRenderer.Render(new VisualScene(scene.Size,
            scene.Nodes.Where(node => node is not VisualSceneText text || text.Role != role).ToArray(), scene.Diagnostics, scene.Regions),
            options.Scale, options.Supersampling, options.PixelBudget, options.TextHinting);
        var bounds = new ChartRect[labels.Length];
        for (var index = 0; index < labels.Length; index++) {
            var label = labels[index];
            var left = index == 0 ? 0 : (int)Math.Floor((centers[index - 1] + centers[index]) / 2);
            var right = index == labels.Length - 1 ? image.Width : (int)Math.Floor((centers[index] + centers[index + 1]) / 2);
            var inkLeft = image.Width; var inkRight = -1; var inkTop = image.Height; var inkBottom = -1; var pixels = 0;
            // Partition by horizontal caption centers, independently of Left/Center/Right text anchors.
            // Every image row is inspected, so a native baseline/vertical-offset regression cannot escape
            // by painting beyond the expected scene text rectangle.
            for (var y = 0; y < image.Height; y++) for (var x = left; x < right; x++) {
                var offset = (y * image.Width + x) * 4;
                if (!IsCaptionInk(image.Pixels, without.Pixels, offset, label.Color)) continue;
                inkLeft = Math.Min(inkLeft, x); inkRight = Math.Max(inkRight, x);
                inkTop = Math.Min(inkTop, y); inkBottom = Math.Max(inkBottom, y); pixels++;
            }
            Assert.True(pixels >= 12 && inkRight - inkLeft >= 7 && inkBottom - inkTop >= 4,
                role + " slot " + index + " must paint a recognizable native caption; observed " + pixels + " pixels at ["
                + inkLeft + "," + inkTop + ".." + inkRight + "," + inkBottom + "].");
            bounds[index] = new ChartRect(inkLeft, inkTop, inkRight - inkLeft + 1, inkBottom - inkTop + 1);
            var expectedLeft = label.Text.Lines.Min(label.LineLeft);
            var expectedRight = label.Text.Lines.Max(line => label.LineLeft(line) + line.Width);
            Assert.True(inkLeft >= expectedLeft - 2 && inkRight < expectedRight + 2,
                role + " slot " + index + " native ink [" + inkLeft + "," + inkTop + ".." + inkRight + "," + inkBottom
                + "] exceeds its horizontal caption [" + expectedLeft + ".." + expectedRight + "] plus two-pixel edge allowance.");
        }
        return bounds;
    }

    private static double CaptionCenter(VisualSceneText text) => (text.Text.Lines.Min(text.LineLeft)
        + text.Text.Lines.Max(line => text.LineLeft(line) + line.Width)) / 2;

    private static bool IsCaptionInk(byte[] image, byte[] without, int offset, ChartColor color) {
        if (image[offset + 3] < 240) return false;
        var red = color.R - without[offset]; var green = color.G - without[offset + 1]; var blue = color.B - without[offset + 2];
        var length = red * red + green * green + blue * blue;
        if (length < 1) return false;
        var changed = Math.Abs(image[offset] - without[offset]) + Math.Abs(image[offset + 1] - without[offset + 1])
            + Math.Abs(image[offset + 2] - without[offset + 2]);
        if (changed < 18) return false;
        var coverage = ((image[offset] - without[offset]) * red + (image[offset + 1] - without[offset + 1]) * green
            + (image[offset + 2] - without[offset + 2]) * blue) / (double)length;
        // Accept partially covered edge pixels along the text-to-background blend, without counting unrelated chart paint.
        return coverage >= .12 && coverage <= 1.05
            && Math.Abs(image[offset] - without[offset] - coverage * red) <= 16
            && Math.Abs(image[offset + 1] - without[offset + 1] - coverage * green) <= 16
            && Math.Abs(image[offset + 2] - without[offset + 2] - coverage * blue) <= 16;
    }
}
