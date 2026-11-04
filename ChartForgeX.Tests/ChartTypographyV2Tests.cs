using System.Globalization;
using System.Xml.Linq;
using System.Text.Json.Nodes;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class ChartTypographyV2Tests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeRadialTransitionsRejectChangedCentreCaptionGap(bool layered) {
        var family = BrowserProofFonts.Family;
        var theme = VisualTheme.Graphite().WithFontFamily(family);
        var json = JsonNode.Parse(theme.ToJson())!;
        json["typography"]!["chart"]!["centerValue"]!["captionGap"] = "8px";
        Chart Model(double value) => layered ? NativeLayeredValueTransitionTests.Model(760, false, 2, value, family)
            : NativeRemainingValueTransitionTests.Model("rings", 760, false, value, family);
        var renderer = new HtmlInteractiveChartRenderer();
        var first = renderer.PrepareSection(Model(20).WithTheme(theme));
        var ordinary = renderer.PrepareSection(Model(80).WithTheme(VisualTheme.FromJson(theme.ToJson())));
        Assert.NotEmpty(renderer.PrepareValueTransitionJson(first, ordinary, 3));
        var last = renderer.PrepareSection(Model(80).WithTheme(VisualTheme.FromJson(json.ToJsonString())));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(first, last, 3));
        Assert.NotEmpty(first.PreparedVisual.ToPng());
        Assert.NotEmpty(last.PreparedVisual.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Donut)]
    [InlineData(ChartSeriesKind.ProgressRing)]
    [InlineData(ChartSeriesKind.LayeredRadial)]
    public void RadialCentresUseTheSharedRadiusScaleAndKeepAuthoredTypography(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(600, 600).WithHeader(false).WithLegend(false).WithDataLabels();
        if (kind == ChartSeriesKind.Donut) chart.AddDonut("Done", new[] { new ChartPoint(1, 25), new ChartPoint(2, 75) });
        else if (kind == ChartSeriesKind.ProgressRing) chart.AddProgressRing("Done", new[] { new ChartPoint(1, 75) });
        else chart.AddLayeredRadial("Done", new[] { ChartRadialLayer.Create("Progress", 75).WithGeometry(1, .12) });
        var role = kind == ChartSeriesKind.Donut ? "donut-total-label" : kind == ChartSeriesKind.ProgressRing ? "progress-ring-value" : "layered-radial-value";
        var captionRole = kind == ChartSeriesKind.Donut ? "donut-title" : kind == ChartSeriesKind.ProgressRing ? "progress-ring-title" : "layered-radial-title";
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var radius = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Max(slice => slice.Outer);
        var value = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == role);
        var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == captionRole);
        Assert.Equal(Math.Max(20, Math.Min(40, .16 * radius)), value.Text.Size, 6);
        Assert.Equal(12, caption.Text.Size);
        Assert.Equal(4, caption.Baseline - caption.Text.Ascent - (value.Baseline - value.Text.Ascent + value.Text.Metrics.Height), 6);
        chart.WithTheme(VisualTheme.Graphite().WithTypography(new VisualTypography(centerValueSize: 28, dataLabelSize: 13.5)));
        var authored = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(28, Assert.Single(authored.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == role).Text.Size);
        Assert.Equal(13.5, Assert.Single(authored.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == captionRole).Text.Size);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void PublicChartPaintsSemanticWeightsAndKeepsAuthoredWeights(VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(800, 520).WithTheme(VisualTheme.Graphite(), mode).WithTitle("Typography")
            .WithXLabels("First", "Second").AddPie("Allocation", new[] { new ChartPoint(1, 40), new ChartPoint(2, 60) });
        var document = XDocument.Parse(chart.ToSvg());
        var text = document.Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.Equal("600", text.Single(element => element.Value == "Typography").Attribute("font-weight")!.Value);
        Assert.Equal(2, text.Count(element => (string?)element.Parent?.Attribute("data-cfx-role") == "legend-value"));
        Assert.Equal(2, text.Count(element => (string?)element.Parent?.Attribute("data-cfx-role") == "legend-percentage"));
        Assert.All(text.Where(element => (string?)element.Parent?.Attribute("data-cfx-role") == "legend-value"), element => Assert.Equal("500", element.Attribute("font-weight")!.Value));
        Assert.All(text.Where(element => (string?)element.Parent?.Attribute("data-cfx-role") == "legend-percentage"), element => Assert.Equal("400", element.Attribute("font-weight")!.Value));
        chart.ConfigureTitleStyle(style => style.FontWeight = "800").ConfigureLegendStyle(style => style.FontWeight = "600");
        var authored = XDocument.Parse(chart.ToSvg()).Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.Equal("800", authored.Single(element => element.Value == "Typography").Attribute("font-weight")!.Value);
        Assert.Equal(2, authored.Count(element => (string?)element.Parent?.Attribute("data-cfx-role") == "legend-value"));
        Assert.All(authored.Where(element => (string?)element.Parent?.Attribute("data-cfx-role") == "legend-value"), element => Assert.Equal("600", element.Attribute("font-weight")!.Value));
    }

    [Theory]
    [InlineData(479, 15, 12, 11, 12)]
    [InlineData(480, 17, 13, 12, 12.5)]
    [InlineData(960, 17, 13, 12, 12.5)]
    public void ChartRoleSizesResolveBeforeLayoutAndSurviveFamilyAndWidthChanges(int width, double title, double subtitle, double tick, double legend) {
        var theme = VisualTheme.Graphite().WithFontFamily("Arial, sans-serif");
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 360)), theme);
        Assert.Equal(title, context.Theme.Typography.TitleSize);
        Assert.Equal(subtitle, context.Theme.Typography.SubtitleSize);
        Assert.Equal(tick, context.Theme.Typography.AxisSize);
        Assert.Equal(legend, context.Theme.Typography.LegendSize);
        var restored = VisualTheme.FromJson(context.Theme.ToJson());
        Assert.Equal(context.Theme.Typography.TitleSize, restored.Typography.TitleSize);
        Assert.Equal(VisualTheme.Graphite().Resolve(VisualThemeMode.Light).Other, restored.Resolve(VisualThemeMode.Light).Other);
        var wide = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(960, 360)), restored.WithFontFamily(ChartFontStacks.ProductSans));
        Assert.Equal(17, wide.Theme.Typography.TitleSize);
        Assert.Equal(ChartFontStacks.ProductSans, wide.Theme.Typography.Family);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void SubMinimumAuthoredChartTextIsHiddenButItsSemanticTitleSurvives(VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(390, 320).WithTheme(VisualTheme.Graphite(), mode)
            .WithTitle("Revenue forecast").ConfigureTitleStyle(style => style.FontSize = 9)
            .AddLine("Revenue", new[] { new ChartPoint(1, 12), new ChartPoint(2, 18) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var document = XDocument.Parse(prepared.ToSvg());
        var text = document.Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.NotEmpty(text);
        Assert.DoesNotContain(text, element => element.Value == "Revenue forecast");
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "title" && element.Value.Contains("Revenue forecast", StringComparison.Ordinal));
        Assert.All(text, element => Assert.True(double.Parse(element.Attribute("font-size")!.Value, CultureInfo.InvariantCulture) >= 10));
        Assert.NotEmpty(prepared.ToPng());
    }
}
