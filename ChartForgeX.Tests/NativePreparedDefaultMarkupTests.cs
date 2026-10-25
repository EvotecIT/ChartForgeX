using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Existing public producers only; retain these exact asset-free outputs before and after common-shell factoring.</summary>
public sealed class NativePreparedDefaultMarkupTests {
    [Fact]
    public void OrdinaryChartAdaptiveSectionRetainsDeterministicAssetFreeMarkup() {
        var series = ChartSeries.CreateLine(new ChartSeriesId("default2-line"), "Observed", new[] {
            new ChartPoint(0, 4), new ChartPoint(1, 6), new ChartPoint(2, 5)
        });
        var chart = Chart.Create().WithTitle("Ordinary chart control").WithSize(640, 400).WithLegend(false).WithDataLabels(false).AddSeries(series);
        chart.Options.Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family); chart.Options.ThemeMode = VisualThemeMode.Light;
        var renderer = new HtmlInteractiveChartRenderer();
        void Configure(HtmlChartInteractionOptions options) {
            options.IdScope = "default2-chart"; options.Interaction.ChartId = "default2-chart";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 420;
        }
        var first = renderer.PrepareSection(chart, configure: Configure); var second = renderer.PrepareSection(chart, configure: Configure);
        Assert.Equal(first.Html, second.Html); Assert.NotNull(first.NarrowPreparedVisual);
        Assert.Contains("data-cfx-chart-id=\"default2-chart\"", first.Html); Assert.Contains("data-cfx-adaptive-narrow", first.Html);
        Assert.DoesNotContain("<script", first.Html, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("<style", first.Html, StringComparison.OrdinalIgnoreCase);
        Save("ordinary-chart-section", first.Html, new { producer = nameof(HtmlInteractiveChartRenderer) + ".PrepareSection", seriesId = series.Id.Value,
            width = 640, height = 400, narrowWidth = 360, narrowHeight = 420, font = BrowserProofFonts.Family, theme = "Graphite Light", assets = false },
            first.PreparedVisual.ToSvg("default2-native-chart"), first.PreparedVisual.ToPng(),
            first.NarrowPreparedVisual!.ToSvg("default2-native-chart-narrow"), first.NarrowPreparedVisual.ToPng());
    }

    [Fact]
    public void OrdinaryTopologyFragmentRetainsDeterministicExistingNativeMarkup() {
        var chart = TopologyChart.Create().WithId("default2-topology").WithViewport(640, 400).WithLegend(null).AddHierarchy(new[] {
            new TopologyHierarchyItem("lead", "Example lead"), new TopologyHierarchyItem("member", "Example member", "lead")
        }, new TopologyHierarchyOptions { NodeDisplayMode = TopologyNodeDisplayMode.Card });
        chart.Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family); chart.ThemeMode = VisualThemeMode.Light;
        var options = new TopologyRenderOptions { IdScope = "default2-topology-svg", IncludeLegend = false };
        var renderer = new HtmlInteractiveTopologyRenderer();
        var first = renderer.RenderFragmentWithoutAssets(chart, options); var second = renderer.RenderFragmentWithoutAssets(chart, options);
        Assert.Equal(first, second); Assert.Contains("data-node-id=\"lead\"", first); Assert.Contains("data-node-id=\"member\"", first);
        Assert.DoesNotContain("<script", first, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("<style", first, StringComparison.OrdinalIgnoreCase);
        var native = chart.Prepare(options);
        Save("ordinary-topology-fragment", first, new { producer = nameof(HtmlInteractiveTopologyRenderer) + ".RenderFragmentWithoutAssets",
            id = "default2-topology", idScope = "default2-topology-svg", nodes = new[] { "lead", "member" }, width = 640, height = 400,
            font = BrowserProofFonts.Family, theme = "Graphite Light", assets = false }, native.ToSvg(), native.ToPng());
    }

    private static void Save(string name, string html, object source, string svg, byte[] png, string? narrowSvg = null, byte[]? narrowPng = null) {
        var configured = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(configured)) return;
        if (!Path.IsPathRooted(configured)) throw new InvalidOperationException("Default markup controls require the task's absolute capture root.");
        var directory = Path.Combine(configured, "native-prepared-default-controls"); Directory.CreateDirectory(directory);
        var prefix = Path.Combine(directory, name); File.WriteAllText(prefix + ".html", html, new UTF8Encoding(false));
        File.WriteAllText(prefix + ".native.svg", svg, new UTF8Encoding(false)); File.WriteAllBytes(prefix + ".native.png", png);
        if (narrowSvg != null) File.WriteAllText(prefix + ".narrow.native.svg", narrowSvg, new UTF8Encoding(false));
        if (narrowPng != null) File.WriteAllBytes(prefix + ".narrow.native.png", narrowPng);
        string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        File.WriteAllText(prefix + ".source.json", JsonSerializer.Serialize(new { source, htmlSha256 = Digest(Encoding.UTF8.GetBytes(html)),
            svgSha256 = Digest(Encoding.UTF8.GetBytes(svg)), pngSha256 = Digest(png),
            narrowSvgSha256 = narrowSvg == null ? null : Digest(Encoding.UTF8.GetBytes(narrowSvg)), narrowPngSha256 = narrowPng == null ? null : Digest(narrowPng) },
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
