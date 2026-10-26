using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeInitialFadeTests {
    [Theory]
    [InlineData(ChartSeriesKind.Line)] [InlineData(ChartSeriesKind.Pie)] [InlineData(ChartSeriesKind.Gauge)]
    [InlineData(ChartSeriesKind.ProgressRing)] [InlineData(ChartSeriesKind.RadialBar)] [InlineData(ChartSeriesKind.Radar)]
    [InlineData(ChartSeriesKind.Circle)] [InlineData(ChartSeriesKind.Heatmap)] [InlineData(ChartSeriesKind.Gantt)]
    [InlineData(ChartSeriesKind.RegionMap)] [InlineData(ChartSeriesKind.Sunburst)] [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Chord)] [InlineData(ChartSeriesKind.WordCloud)]
    public void CapturedNativeFamiliesFadeWithoutInventingAnyValueOrGeometryBaseline(ChartSeriesKind kind) {
        var chart = Assert.IsType<Chart>(ExampleRecipes.ForKind(kind).Create()); var renderer = new HtmlInteractiveChartRenderer();
        var final = renderer.PrepareSection(chart); var frames = VisualValueCapture.InitialFadeFrames(final.PreparedVisual, 3);
        var payload = renderer.PrepareNativeTransitionJson(final, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, 3));
        Capture(final, frames, payload, "initial-fade-" + kind);
        Assert.Same(final.PreparedVisual, frames[2]); Assert.Equal(final.PreparedVisual.ToSvg(), frames[2].ToSvg()); Assert.Equal(final.PreparedVisual.ToPng(), frames[2].ToPng());
        Assert.Equal(0, Assert.IsType<VisualSceneGroup>(frames[0].Scene.Nodes[0]).Opacity);
        Assert.Equal(.5, Assert.IsType<VisualSceneGroup>(frames[1].Scene.Nodes[0]).Opacity);
        Assert.Equal(final.PreparedVisual.Scene.Nodes, frames[1].Scene.Nodes.Skip(1).Take(final.PreparedVisual.Scene.Nodes.Count));
        Assert.Empty(frames[0].Regions); Assert.Equal(final.PreparedVisual.Regions, frames[1].Regions);
        using var json = JsonDocument.Parse(payload); Assert.Equal("initial-fade", json.RootElement.GetProperty("effect").GetString());
        Assert.Equal("native-visual-opacity", json.RootElement.GetProperty("sourceKind").GetString()); Assert.Empty(json.RootElement.GetProperty("readings").EnumerateArray());
        HtmlFontRootObservation.AssertDestination(final.Html, json.RootElement.GetProperty("destination").GetString()!);
        var native = json.RootElement.GetProperty("drawings").GetProperty("wide").GetProperty("frames");
        Assert.Equal(new[] { "0", "0.5", "1" }, native.EnumerateArray().Select(frame => frame.GetProperty("nativeOpacity").GetString()));
    }
    [Fact]
    public void WholeVisualOverlapUsesOneNativeAlphaAndDoesNotReinvokeCustomFormattingOrFontResolution() {
        var builder = new VisualSceneBuilder(new VisualSize(32, 24), FontSpec.SystemSans()); var red = ChartColor.FromHex("#CC2030");
        builder.Rect(new ChartRect(2, 2, 16, 16), red); builder.Rect(new ChartRect(8, 4, 16, 16), red);
        var final = new PreparedVisual(builder.Build()); var frame = VisualValueCapture.OpacityFrame(final, .5); var rgba = frame.ToRgba(new VisualRenderOptions(supersampling: 1));
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "initial-fade-overlap.svg"), frame.ToSvg()); File.WriteAllBytes(Path.Combine(output, "initial-fade-overlap.png"), frame.ToPng());
        }
        Assert.Equal(128, rgba.Pixels[(10 * rgba.Width + 10) * 4 + 3]); Assert.NotEqual(192, rgba.Pixels[(10 * rgba.Width + 10) * 4 + 3]);
        var calls = 0; var chart = CircleRecipe.Create().WithValueFormat(ChartValueFormat.Custom(value => { calls++; return value + " units"; }));
        var renderer = new HtmlInteractiveChartRenderer(); var section = renderer.PrepareSection(chart); var count = calls;
        var captured = VisualValueCapture.InitialFadeFrames(section.PreparedVisual, 3); var svg = captured.Select(item => item.ToSvg()).ToArray(); var png = captured.Select(item => item.ToPng()).ToArray();
        chart.WithValueFormat(ChartValueFormat.Custom(_ => "Later"));
        try {
            FontRegistry.Clear(); var repeated = VisualValueCapture.InitialFadeFrames(section.PreparedVisual, 3);
            Assert.Equal(svg, repeated.Select(item => item.ToSvg())); for (var index = 0; index < repeated.Count; index++) Assert.Equal(png[index], repeated[index].ToPng());
            _ = renderer.PrepareNativeTransitionJson(section, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, 3)); Assert.Equal(count, calls);
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Fact]
    public void FinalOnlyAdmissionCaptureLimitsAndCancellationPreserveTheAcceptedOrdinaryScene() {
        var renderer = new HtmlInteractiveChartRenderer(); var final = renderer.PrepareSection(CircleRecipe.Create()); var svg = final.Html;
        Assert.Throws<ArgumentException>(() => renderer.PrepareNativeTransitionJson(final, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)));
        Assert.Throws<ArgumentException>(() => renderer.PrepareNativeTransitionJson(final, final, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade, 3)));
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualValueCapture.InitialFadeFrames(final.PreparedVisual, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => VisualValueCapture.InitialFadeFrames(final.PreparedVisual, 17));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => renderer.PrepareNativeTransitionJson(final, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialFade), cancelled.Token));
        Assert.Same(final.PreparedVisual, VisualValueCapture.OpacityFrame(final.PreparedVisual, 1)); Assert.Equal(svg, final.Html);
    }
    private static void Capture(HtmlChartSection final, IReadOnlyList<PreparedVisual> frames, string payload, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name); File.WriteAllText(path + ".final.html", final.Html); File.WriteAllText(path + ".transition.json", payload);
        for (var index = 0; index < frames.Count; index++) { File.WriteAllText(path + ".frame-" + index + ".svg", frames[index].ToSvg()); File.WriteAllBytes(path + ".frame-" + index + ".png", frames[index].ToPng()); }
    }
}
