using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeLayeredValueTransitionTests {
    [Theory]
    [InlineData(1, 20, 80, 50)]
    [InlineData(2, 0, 100, 50)]
    [InlineData(2, -20, 120, 50)]
    public void TypedLayerFramesMatchNativePreparedIntermediateWithClampsCapsSeparatorsAndCompleteCenterFacts(int index, double start, double end, double middle) {
        var family = BrowserProofFonts.Family; var renderer = new HtmlInteractiveChartRenderer();
        var chart = Model(390, true, index, start, family);
        var from = renderer.PrepareSection(chart); var to = renderer.PrepareSection(Model(390, true, index, end, family));
        var expected = renderer.PrepareSection(Model(390, true, index, middle, family)).PreparedVisual;
        var frames = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3);
        Assert.Same(from.PreparedVisual, frames[0]); Assert.Same(to.PreparedVisual, frames[2]);
        Assert.Equal(expected.ToSvg("radial-middle"), frames[1].ToSvg("radial-middle")); Assert.Equal(expected.ToPng(), frames[1].ToPng());
        NativeValueIdentityProof.AssertEqual(expected, frames[1], "layered-existing-" + index + "-" + start + "-" + end);
        Assert.Equal(8, frames[1].Scene.Nodes.Count(node => node.Role == "layered-radial-separator"));
        Assert.Equal(4, frames[1].Scene.Nodes.Count(node => node.Role == "layered-radial-layer-cap"));
        Assert.Contains(frames[1].Regions, region => region.Id == "series-0-center-value" && region.Label == (index == 2 ? "50" : "42"));
        var nativeSvg = frames.Select(frame => frame.ToSvg("detached-radial")).ToArray(); var nativePng = frames.Select(frame => frame.ToPng()).ToArray();
        chart.Series[0].RadialLayers[index].WithValue(1).WithAngles(30, 180).WithGeometry(.5, .25).WithColor(ChartColor.FromHex("#FF0000"));
        chart.WithFontFamily("Later missing font").WithValueFormat(ChartValueFormat.Custom(_ => "Changed"));
        try {
            FontRegistry.Clear(); var retained = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3);
            Assert.Equal(nativeSvg, retained.Select(frame => frame.ToSvg("detached-radial")));
            for (var frame = 0; frame < retained.Count; frame++) Assert.Equal(nativePng[frame], retained[frame].ToPng());
        } finally { _ = BrowserProofFonts.Family; }
        using var json = JsonDocument.Parse(renderer.PrepareValueTransitionJson(from, to, 3));
        Assert.Equal("layered-radial-value", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal("completion", json.RootElement.GetProperty("seriesKey").GetString());
        Assert.Equal(index == 1 ? "verified" : "complete", json.RootElement.GetProperty("observationId").GetString());
        Assert.Equal(start, json.RootElement.GetProperty("fromValue").GetDouble()); Assert.Equal(end, json.RootElement.GetProperty("toValue").GetDouble());
        Capture(frames[1], "native-layered-midpoint-" + index + "-" + start + "-" + end);
    }

    [Fact]
    public void PolicyChangesUnsupportedFamiliesAndCancelledCaptureLeaveOrdinaryNativeScenesAvailable() {
        var family = BrowserProofFonts.Family; var renderer = new HtmlInteractiveChartRenderer();
        var before = renderer.PrepareSection(Model(760, false, 2, 20, family)); var after = renderer.PrepareSection(Model(760, false, 2, 80, family));
        var unchanged = before.PreparedVisual.ToSvg();
        var changedPolicy = Model(760, false, 2, 80, family); changedPolicy.Series[0].RadialLayers[1].SeparatorCount = 4;
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(before, renderer.PrepareSection(changedPolicy), 3));
        changedPolicy = Model(760, false, 2, 80, family); changedPolicy.Series[0].RadialLayers[1].WithColor(ChartColor.FromHex("#FF0000"));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(before, renderer.PrepareSection(changedPolicy), 3));
        var custom = renderer.PrepareSection(Model(760, false, 2, 80, family).WithValueFormat(ChartValueFormat.Custom(value => value + " units")));
        Assert.NotEmpty(custom.PreparedVisual.ToPng()); Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(before, custom, 3));
        var circle = renderer.PrepareSection(CircleRecipe.Create().WithFontFamily(family));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(circle, circle, 3));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(before, renderer.PrepareSection(ProgressRingRecipe.Create().WithFontFamily(family)), 3));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => renderer.PrepareValueTransitionJson(before, after, 3, cancelled.Token));
        Assert.Equal(unchanged, before.PreparedVisual.ToSvg());
    }

    [Fact]
    public void EqualZeroAndFullEndpointInkStillRejectsChangedCapPolicyThatWouldAlterIntermediatePaint() {
        var family = BrowserProofFonts.Family; var renderer = new HtmlInteractiveChartRenderer();
        var zero = renderer.PrepareSection(Model(760, false, 2, 0, family));
        var chart = Model(760, false, 2, 100, family); chart.Series[0].RadialLayers[2].LineCap = ChartRadialLayerCap.Butt;
        var full = renderer.PrepareSection(chart); var capture = zero.PreparedVisual.Scene.ValueCapture!;
        var readings = full.PreparedVisual.Scene.ValueCapture!.Plan.Readings.Select(row => row.Value).ToArray();
        // Zero has no value arc, and a completed circle has no endpoint caps. Native
        // endpoint equality alone therefore cannot establish matching cap policy.
        Assert.Equal(full.PreparedVisual.ToSvg("same-endpoint"), capture.Frame(zero.PreparedVisual, readings).ToSvg("same-endpoint"));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(zero, full, 3));
    }

    internal static Chart Model(int width, bool dark, int index, double value, string family) {
        var chart = LayeredRadialRecipe.Create().WithSize(width, 400).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithFontFamily(family).WithLegend(false);
        var series = chart.Series[0];
        for (var row = 0; row < 3; row++) series.WithObservationId(row, new ChartObservationId(new[] { "reviewed", "verified", "complete" }[row]));
        series.RadialLayers[0].WithLineCap(ChartRadialLayerCap.Butt).WithOpacity(.6);
        series.ConfigureDataLabelStyle(style => style.WithUnderline(TextDecorationStyle.Dashed));
        var layer = series.RadialLayers[index]; layer.WithValue(value);
        series.UpdateRadialLayer(index, layer); return chart;
    }
    internal static void Configure(HtmlChartInteractionOptions options) {
        options.Interaction.ChartId = "native-layered-value"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 400;
    }
    private static void Capture(PreparedVisual frame, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), frame.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".png"), frame.ToPng());
    }
}
