using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeCircleValueTransitionTests {
    [Theory]
    [InlineData(40, 90, 65, "warning", "Warning")]
    [InlineData(50, 70, 60, "warning", "Warning")]
    [InlineData(70, 90, 80, "positive", "Success")]
    [InlineData(-20, 120, 50, "negative", "Danger")]
    public void TypedCircleFramesMatchCompleteNativeEndpointsAndIntermediateStatusPaintAndRawClamping(double start, double end, double middle, string status, string state) {
        var renderer = new HtmlInteractiveChartRenderer(); var family = BrowserProofFonts.Family;
        var from = renderer.PrepareSection(Model(390, true, start, family)); var to = renderer.PrepareSection(Model(390, true, end, family));
        var expected = renderer.PrepareSection(Model(390, true, middle, family)).PreparedVisual;
        var frames = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3);
        Assert.Same(from.PreparedVisual, frames[0]); Assert.Same(to.PreparedVisual, frames[2]);
        Assert.Equal(expected.ToSvg("circle-middle"), frames[1].ToSvg("circle-middle")); Assert.Equal(expected.ToPng(), frames[1].ToPng());
        NativeValueIdentityProof.AssertEqual(expected, frames[1], "circle-existing-" + start + "-" + end);
        var group = Assert.Single(frames[1].Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "circle-chart");
        Assert.Equal(status, group.Metadata["data-cfx-status"]); Assert.Equal(state, group.Metadata["data-cfx-state"]);
        Assert.Equal(middle.ToString("R", CultureInfo.InvariantCulture), group.Metadata["data-cfx-value"]);
        Assert.Equal(Math.Max(0, Math.Min(1, middle / 100)).ToString("R", CultureInfo.InvariantCulture), group.Metadata["data-cfx-percent"]);
        Assert.Equal(2, frames[1].Scene.Nodes.Count(node => node.Role == "circle-value-cap"));
        Assert.Contains(frames[1].Regions, region => region.Role == "circle-label" && region.Label == middle.ToString(CultureInfo.InvariantCulture));
        using var json = JsonDocument.Parse(renderer.PrepareValueTransitionJson(from, to, 3));
        Assert.Equal("circle-value", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal("completed", json.RootElement.GetProperty("seriesKey").GetString());
        Assert.Equal("current", json.RootElement.GetProperty("observationId").GetString());
        Assert.Equal(start, json.RootElement.GetProperty("fromValue").GetDouble()); Assert.Equal(end, json.RootElement.GetProperty("toValue").GetDouble());
        Capture(frames[1], "native-circle-midpoint-" + start + "-" + end);
    }

    [Theory]
    [InlineData("series")]
    [InlineData("point")]
    [InlineData("state")]
    public void AuthoredColorPrecedenceStateOverrideAndPointTypographyRemainNativeAtEveryReading(string policy) {
        var renderer = new HtmlInteractiveChartRenderer(); var family = BrowserProofFonts.Family;
        Chart Create(double value) {
            var chart = Model(760, false, value, family); var series = chart.Series[0];
            series.StateRole = ChartSeriesState.Info;
            if (policy is "series" or "point") series.WithColor(ChartColor.FromHex("#2468AC"));
            if (policy == "point") series.WithPointColor(0, ChartColor.FromHex("#7B3294"));
            series.WithPointLabel(0, "Accepted assessment");
            series.ConfigurePointDataLabelStyle(0, style => style.WithUnderline(TextDecorationStyle.Dashed).WithFontSize(13));
            chart.Options.CircleRadiusScale = 1.2; chart.Options.CircleStrokeScale = 1.6; return chart;
        }
        var from = renderer.PrepareSection(Create(40)); var to = renderer.PrepareSection(Create(90));
        var middle = AssertNativeMiddle(renderer, from, to, Create(65));
        var colors = VisualTheme.Graphite().Resolve(VisualThemeMode.Light);
        var expectedColor = policy == "point" ? ChartColor.FromHex("#7B3294") : policy == "series" ? ChartColor.FromHex("#2468AC") : colors.Status.Info.Fill;
        Assert.Equal(expectedColor, Assert.Single(middle.Scene.Nodes.OfType<VisualSceneSlice>(), node => node.Role == "circle-value").Fill);
        Assert.Equal(colors.Status.Info.Fill, Assert.Single(middle.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "circle-status-marker").Fill);
        var group = Assert.Single(middle.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "circle-chart");
        Assert.Equal("warning", group.Metadata["data-cfx-status"]); Assert.Equal("Info", group.Metadata["data-cfx-state"]);
        Assert.Contains(middle.Regions, region => region.Role == "circle-label" && region.Label == "Accepted assessment");
    }

    [Theory]
    [InlineData(false, true)] [InlineData(true, false)]
    public void NativeLabelAndStatusVisibilityPoliciesKeepTheirOwnLayoutAndRemainDetached(bool labels, bool status) {
        var family = BrowserProofFonts.Family; var renderer = new HtmlInteractiveChartRenderer();
        Chart Create(double value) {
            var chart = Model(390, true, value, family); chart.Series[0].ShowDataLabels = labels; chart.Options.ShowCircleStatusLabel = status; return chart;
        }
        var chart = Create(40); var from = renderer.PrepareSection(chart); var to = renderer.PrepareSection(Create(90));
        var middle = AssertNativeMiddle(renderer, from, to, Create(65));
        Assert.DoesNotContain(middle.Scene.Nodes, node => node.Role == "circle-status-marker");
        if (!labels) Assert.DoesNotContain(middle.Scene.Nodes, node => node.Role is "circle-label" or "circle-title");
        var svg = middle.ToSvg("retained-circle"); var png = middle.ToPng();
        chart.Series[0].UpdateGaugeValue(0, new ChartGaugeValue(5, -50, 50));
        chart.Series[0].WithPointLabel(0, "Later changed caption").WithPointColor(0, ChartColor.FromHex("#FF0000"));
        chart.Options.CircleRadiusScale = .65; chart.WithFontFamily("Later missing face").WithValueFormat(ChartValueFormat.Custom(_ => "Changed"));
        try {
            FontRegistry.Clear(); var retained = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3)[1];
            Assert.Equal(svg, retained.ToSvg("retained-circle")); Assert.Equal(png, retained.ToPng());
        } finally { _ = BrowserProofFonts.Family; }
    }

    [Fact]
    public void ChangedIntermediatePaletteIsRejectedEvenWhenBothCompleteNativeEndpointPaintsMatch() {
        var renderer = new HtmlInteractiveChartRenderer(); var family = BrowserProofFonts.Family;
        var from = renderer.PrepareSection(Model(760, false, 40, family));
        var changed = Model(760, false, 100, family);
        changed.WithTheme(VisualTheme.Graphite().Configure(builder => builder.Light.Status.Medium =
            new VisualTokenColor(ChartColor.FromHex("#7B3294"), builder.Light.Status.Medium.Ink)), VisualThemeMode.Light).WithFontFamily(family);
        var to = renderer.PrepareSection(changed); var capture = from.PreparedVisual.Scene.ValueCapture!;
        Assert.Equal(to.PreparedVisual.ToSvg("same-circle-endpoint"), capture.Frame(from.PreparedVisual, new[] { 100d }).ToSvg("same-circle-endpoint"));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, to, 3));
    }

    [Fact]
    public void ChangedNativePolicyCustomFormattingUnidentifiedReadingsOuterLegendChangesAndCancellationCannotPublishFrames() {
        var renderer = new HtmlInteractiveChartRenderer(); var family = BrowserProofFonts.Family;
        var from = renderer.PrepareSection(Model(760, false, 40, family)); var after = Model(760, false, 90, family);
        var original = from.PreparedVisual.ToSvg();
        after.Options.CircleStrokeScale = 1.7;
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, renderer.PrepareSection(after), 3));
        after = Model(760, false, 90, family); after.Series[0].StateRole = ChartSeriesState.Info;
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, renderer.PrepareSection(after), 3));
        after = Model(760, false, 90, family); after.Series[0].UpdateGaugeValue(0, new ChartGaugeValue(90, -10, 100));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, renderer.PrepareSection(after), 3));
        var custom = renderer.PrepareSection(Model(760, false, 90, family).WithValueFormat(ChartValueFormat.Custom(value => value + " units")));
        Assert.NotEmpty(custom.PreparedVisual.ToPng()); Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, custom, 3));
        var anonymous = renderer.PrepareSection(CircleRecipe.Create().WithFontFamily(family));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(anonymous, anonymous, 3));
        var legendFrom = renderer.PrepareSection(Model(760, false, 40, family).WithLegend(true));
        var legendTo = renderer.PrepareSection(Model(760, false, 90, family).WithLegend(true));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(legendFrom, legendTo, 3));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => renderer.PrepareValueTransitionJson(from, renderer.PrepareSection(Model(760, false, 90, family)), 3, cancelled.Token));
        Assert.Equal(original, from.PreparedVisual.ToSvg());
    }

    internal static Chart Model(int width, bool dark, double value, string family) {
        var chart = CircleRecipe.Create().WithSize(width, 400).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithFontFamily(family).WithLegend(false);
        var series = chart.Series[0]; series.WithObservationId(0, new ChartObservationId("current"));
        var reading = series.GaugeValues[0]; series.UpdateGaugeValue(0, new ChartGaugeValue(value, reading.Minimum, reading.Maximum));
        return chart;
    }
    internal static void Configure(HtmlChartInteractionOptions options) {
        options.Interaction.ChartId = "native-circle-value"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 400;
    }
    private static PreparedVisual AssertNativeMiddle(HtmlInteractiveChartRenderer renderer, HtmlChartSection from, HtmlChartSection to, Chart expected) {
        var middle = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3)[1]; var native = renderer.PrepareSection(expected).PreparedVisual;
        Assert.Equal(native.ToSvg("circle-middle"), middle.ToSvg("circle-middle")); Assert.Equal(native.ToPng(), middle.ToPng()); return middle;
    }
    private static void Capture(PreparedVisual frame, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), frame.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".png"), frame.ToPng());
    }
}
