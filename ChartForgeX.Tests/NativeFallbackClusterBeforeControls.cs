using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Run after radial51 and before captured-cluster product. Uses only that prerequisite API.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeFallbackClusterBeforeControls : IDisposable {
    internal const string Primary = "CFX Recorded Cluster Primary";
    internal const string Fallback = "CFX Recorded Cluster Fallback";
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ActualFallbackCaptionCanUseTheExistingCompleteNativeValueApi(bool donut) => Observe(donut, true);
    [Theory] [InlineData(false)] [InlineData(true)]
    public void CapturedPrimaryOnlyCaptionRemainsAnExactCompleteNativeControl(bool donut) => Observe(donut, false);
    private static void Observe(bool donut, bool fallback) {
        Register(); var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(donut, fallback, 60));
        var to = renderer.PrepareSection(Model(donut, fallback, 80)); var middle = renderer.PrepareSection(Model(donut, fallback, 70));
        var name = "fallback-before-" + donut + "-" + fallback;
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-native-middle");
        string? payload = null; Exception? failure = null; try { payload = renderer.PrepareValueTransitionJson(from, to, 3); } catch (Exception exception) { failure = exception; }
        var faces = from.PreparedVisual.Scene.Nodes.OfType<VisualSceneText>().SelectMany(row => row.Text.Lines).SelectMany(row => row.Glyphs).Select(row => row.Face.Root).Distinct().Count();
        NativeRadialMotionBeforeControls.Save(name + ".capture.json", JsonSerializer.Serialize(new { payload, nativeFaces = faces, error = failure?.GetType().Name, message = failure?.Message }));
        Assert.Equal(fallback ? 2 : 1, faces); Assert.Null(failure); Assert.NotNull(payload);
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
    }
    internal static void Register() {
        FontRegistry.Register(Primary, PrimaryBytes());
        FontRegistry.Register(Fallback, FallbackBytes());
    }
    // The incumbent common frame measures Mg even for omitted headings; keep that reached sample primary-covered.
    internal static byte[] PrimaryBytes() => OpenTypeTestFonts.NameKeyed(includePrivateUse: false, extraGlyphs: new Dictionary<int, int> { ['M'] = OpenTypeTestFonts.H });
    internal static byte[] FallbackBytes() => OpenTypeTestFonts.NameKeyed();
    internal static Chart Model(bool donut, bool fallback, double value) {
        var series = new ChartSeries(new ChartSeriesId("parts"), "H", donut ? ChartSeriesKind.Donut : ChartSeriesKind.Pie,
            new[] { new ChartPoint(1, value), new ChartPoint(2, 25), new ChartPoint(3, 10) })
            .WithObservationIds(new[] { new ChartObservationId("primary"), new ChartObservationId("second"), new ChartObservationId("third") });
        var chart = Chart.Create().WithSize(760, 420).WithFontFamily(Primary + ", " + Fallback).WithLegend(false).WithDataLabels(true)
            .WithDonutCenterLabel(false).WithPieSliceLabelContent(ChartPieSliceLabelContent.Label).WithDataLabelPlacement(ChartDataLabelPlacement.Inside)
            .WithXLabels(fallback ? "H" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter) : "HH", "OH", "HO").AddSeries(series);
        chart.Options.MaximumPieSlices = 8; return chart;
    }
    public void Dispose() { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
}
