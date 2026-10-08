using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GalleryHtmlHealthTests {
    [Fact]
    public void NativeGridAndHostOwnedFlatSurfacesQualifyWhileClippingAndMissingPagePoliciesFail() {
        var chart = Chart.Create().WithSize(320, 220).WithTitle("Counts").WithTheme(ChartTheme.GraphiteLight())
            .AddBar("Counts", new[] { new ChartPoint(1, 20), new ChartPoint(2, 50) });
        var grid = ChartGrid.Create().WithTitle("Comparison").WithColumns(1).Add(chart);
        var gridHtml = grid.ToHtmlPage();
        Assert.DoesNotContain("overflow:visible", gridHtml);
        Assert.Contains("data-cfx-role=\"panel\"", gridHtml);
        var host = chart.WithHostFrame().ToHtmlPage();
        Assert.DoesNotContain("data-cfx-role=\"frame-card\"", host);
        var table = ChartTable.Create().WithTitle("Release gates").WithTheme(ChartTheme.GraphiteLight())
            .WithColumns("Gate", "Status").AddRow("Build", "Passed");
        var tableHtml = table.ToHtmlPage();
        var artifactHtml = TableArtifact.Create("packed-table").AddColumn("gate", "Gate")
            .AddRow("build", "Passed").ToVisualArtifact().ToHtmlPage();
        Assert.DoesNotContain("data-cfx-role=\"frame-card\"", tableHtml);
        var variants = new Dictionary<string, string> {
            ["grid"] = gridHtml,
            ["host"] = host,
            ["table"] = tableHtml,
            ["static-artifact"] = artifactHtml,
            ["scrolling-artifact"] = artifactHtml.Replace(".chartforgex-visual-artifact{max-width:100%;height:auto}", ".chartforgex-visual-artifact{max-width:100%;height:auto;overflow:auto}", StringComparison.Ordinal),
            ["unbounded-artifact"] = artifactHtml.Replace("viewBox=", "data-missing-viewbox=", StringComparison.Ordinal),
            ["clipped-grid"] = gridHtml.Replace(".chartforgex-grid{margin:0 auto}", ".chartforgex-grid{margin:0 auto;overflow:hidden}", StringComparison.Ordinal),
            ["missing-flat-surface"] = host.Replace("background:#FFFFFF", "background:inherit", StringComparison.Ordinal),
            ["missing-print"] = host.Replace("@media print", "@media screen", StringComparison.Ordinal),
            ["missing-text-polish"] = host.Replace("-webkit-font-smoothing:antialiased", "-webkit-font-smoothing:auto", StringComparison.Ordinal)
        };
        Assert.NotEqual(gridHtml, variants["clipped-grid"]);
        Assert.NotEqual(host, variants["missing-flat-surface"]);
        Assert.NotEqual(artifactHtml, variants["scrolling-artifact"]);
        var directory = Path.Combine(Path.GetTempPath(), "ChartForgeX-html-health-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            var svg = chart.ToSvg(); var png = chart.ToPng();
            foreach (var variant in variants) {
                File.WriteAllText(Path.Combine(directory, variant.Key + ".html"), variant.Value);
                File.WriteAllText(Path.Combine(directory, variant.Key + ".svg"), svg);
                File.WriteAllBytes(Path.Combine(directory, variant.Key + ".png"), png);
            }
            GalleryWriter.Write(directory);
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "svg-png-comparison.json")));
            var health = manifest.RootElement.GetProperty("charts").EnumerateArray()
                .ToDictionary(item => item.GetProperty("name").GetString()!, item => item.GetProperty("html").Clone());
            foreach (var name in new[] { "grid", "host", "table", "static-artifact" })
                Assert.True(health[name].GetProperty("healthy").GetBoolean(), health[name].GetRawText());
            Assert.False(health["clipped-grid"].GetProperty("hasExpectedOverflow").GetBoolean());
            Assert.False(health["scrolling-artifact"].GetProperty("hasExpectedOverflow").GetBoolean());
            Assert.False(health["unbounded-artifact"].GetProperty("hasExpectedOverflow").GetBoolean());
            Assert.False(health["missing-flat-surface"].GetProperty("hasFlatSurface").GetBoolean());
            Assert.False(health["missing-print"].GetProperty("hasPrintCss").GetBoolean());
            Assert.False(health["missing-text-polish"].GetProperty("hasTextPolish").GetBoolean());
            foreach (var name in new[] { "clipped-grid", "scrolling-artifact", "unbounded-artifact", "missing-flat-surface", "missing-print", "missing-text-polish" })
                Assert.False(health[name].GetProperty("healthy").GetBoolean(), health[name].GetRawText());
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }
}
