using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Checks actual browser glyph bounds alongside native pixels for the public grouped-stack recipe.</summary>
public sealed class CartesianLabelLaneBrowserTests {
    [BrowserTheory]
    [InlineData(360, VisualThemeMode.Light)]
    [InlineData(360, VisualThemeMode.Dark)]
    [InlineData(640, VisualThemeMode.Light)]
    [InlineData(640, VisualThemeMode.Dark)]
    public async Task NormalizedReversedStackTotalsClearRenderedCategoryGlyphs(int width, VisualThemeMode mode) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = BarRecipe.Create("compact-options", mode).WithSize(width, 360)
            .WithTitle("Completed orders").WithSubtitle("Normalized grouped stacks")
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
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
}
