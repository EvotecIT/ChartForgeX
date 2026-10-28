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
public sealed class NativeCartesianValueTransitionTests {
    [Theory]
    [InlineData(false, "linear")] [InlineData(true, "linear")] [InlineData(false, "log-positive")] [InlineData(true, "log-positive")]
    [InlineData(false, "log-negative")] [InlineData(true, "log-negative")] [InlineData(false, "elapsed-time")] [InlineData(true, "elapsed-time")]
    public void FixedNativeAxesUseTheActualIndependentNativeMidpoint(bool bar, string scale) {
        var first = scale == "log-negative" ? -20d : 20d; var last = scale == "log-negative" ? -80d : 80d;
        Prove(NativeCartesianValueBeforeControls.Model(bar, scale, first), NativeCartesianValueBeforeControls.Model(bar, scale, last),
            NativeCartesianValueBeforeControls.Model(bar, scale, (first + last) / 2), "fixed-" + bar + "-" + scale);
    }
    [Theory]
    [InlineData(false, "calendar-bands")] [InlineData(true, "calendar-bands")] [InlineData(false, "sessions")] [InlineData(true, "sessions")]
    [InlineData(false, "symmetric-log")] [InlineData(true, "symmetric-log")] [InlineData(false, "reversed")] [InlineData(true, "reversed")]
    public void CalendarSessionAndNonlinearAxisFactsStayWithTheCapturedNativeTransform(bool bar, string policy) {
        Chart Model(double value) {
            var time = policy is "calendar-bands" or "sessions";
            var chart = NativeCartesianValueBeforeControls.Model(bar, time ? "elapsed-time" : "linear", value);
            if (policy == "calendar-bands") chart.ConfigureXAxis(axis => { axis.Minimum = null; axis.Maximum = null; axis.TimeSpacing = ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Day)); });
            if (policy == "sessions") { var first = new DateTime(2026, 1, 1).ToOADate(); chart.ConfigureXAxis(axis => axis.TimeSessions = new ChartTimeSessionDomain(new[] {
                new ChartTimeSession(first, first + 1), new ChartTimeSession(first + 2, first + 3) })); }
            if (policy == "symmetric-log") chart.ConfigureYAxis(axis => { axis.Scale = ChartScaleKind.SymmetricLogarithmic; axis.Minimum = -100; axis.SymmetricLogarithmThreshold = 2; });
            if (policy == "reversed") chart.ConfigureYAxis(axis => axis.Reversed = true);
            return chart;
        }
        if (bar && policy == "sessions") {
            var renderer = new HtmlInteractiveChartRenderer(); var ordinary = renderer.PrepareSection(NativeCartesianValueBeforeControls.Model(true, "elapsed-time", 20));
            NativeRadialMotionBeforeControls.Capture(ordinary, "cartesian-bar-time-without-sessions"); Exception? nativeInvalid = null;
            try { _ = renderer.PrepareSection(Model(20)); } catch (Exception exception) { nativeInvalid = exception; }
            NativeRadialMotionBeforeControls.Save("cartesian-bar-sessions.native-domain.json", JsonSerializer.Serialize(new { error = nativeInvalid?.GetType().Name, message = nativeInvalid?.Message }));
            Assert.IsType<NotSupportedException>(nativeInvalid); Assert.Contains("Time session compression", nativeInvalid!.Message, StringComparison.Ordinal);
            Assert.NotEmpty(ordinary.PreparedVisual.ToSvg()); return;
        }
        Prove(Model(20), Model(80), Model(50), "native-transform-" + bar + "-" + policy);
    }
    [Theory]
    [InlineData("line-default-last")] [InlineData("line-pattern-markers")] [InlineData("line-step")] [InlineData("line-smooth-broken")]
    [InlineData("bar-flat-zero")] [InlineData("bar-gradient")] [InlineData("bar-capsule-negative")] [InlineData("bar-capsule-subpixel")]
    public void NativeLineLayersMarkersAndBarSurfacesRepaintThroughTheirOriginalOwners(string policy) {
        var first = policy == "bar-flat-zero" ? -40d : policy == "bar-capsule-negative" ? -80d : policy == "bar-capsule-subpixel" ? .01 : 20;
        var last = policy == "bar-flat-zero" ? 40d : policy == "bar-capsule-negative" ? -20d : policy == "bar-capsule-subpixel" ? .03 : 80;
        var midpoint = ChartMath.InterpolateRange(first, last, .5);
        NativeRadialMotionBeforeControls.Save("cartesian-native-reading-" + policy + ".facts.json", JsonSerializer.Serialize(new { first, last, arithmeticMean = (first + last) / 2, nativeMidpoint = midpoint }));
        Prove(Styled(policy, first), Styled(policy, last), Styled(policy, midpoint), policy);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void CapturedBuiltinReadoutsPreserveAuthoredLabelsExplicitAxisLabelsAndNamedCulture(bool authored) {
        Chart Model(double value) {
            var chart = Styled("line-default-last", value).WithValueFormat(ChartValueFormat.Number("0.0 'units'", CultureInfo.GetCultureInfo("pl-PL")));
            chart.ConfigureYAxis(axis => axis.Labels.Add(new ChartAxisLabel(50, "Native midpoint")));
            if (authored) chart.Series[0].WithPointLabel(1, "Authored caption"); return chart;
        }
        Prove(Model(20), Model(80), Model(50), "readout-" + authored);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void MultipleReadingsRetainOrderedSeriesObservationPairsAndIndependentSecondaryValueAxes(bool bar) {
        Chart Model(double value, double secondary) {
            var chart = NativeCartesianValueBeforeControls.Model(bar, "linear", value);
            var points = new[] { new ChartPoint(0, 600), new ChartPoint(1, secondary), new ChartPoint(2, 1200) };
            var extra = bar ? ChartSeries.CreateBar(new ChartSeriesId("alternate"), "Alternate", points) : ChartSeries.CreateLine(new ChartSeriesId("alternate"), "Alternate", points);
            extra.UseSecondaryValueAxis().WithObservationId(0, new ChartObservationId("earlier")).WithObservationId(1, new ChartObservationId("current")).WithObservationId(2, new ChartObservationId("later"));
            return chart.AddSeries(extra).ConfigureSecondaryYAxis(axis => { axis.Minimum = 0; axis.Maximum = 2000; });
        }
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(20, 1000)); var to = renderer.PrepareSection(Model(80, 1800)); var expected = renderer.PrepareSection(Model(50, 1400));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3));
        Capture(from, to, expected, frames, payload, "secondary-" + bar);
        NativeValueIdentityProof.AssertEqual(expected.PreparedVisual, frames[1], "cartesian-secondary-" + bar); Assert.Equal(expected.PreparedVisual.ToPng(), frames[1].ToPng());
        using var json = JsonDocument.Parse(payload); var rows = json.RootElement.GetProperty("readings").EnumerateArray().ToArray(); Assert.Equal(6, rows.Length);
        Assert.Equal(new[] { "delivery", "alternate" }, rows.Where(row => row.GetProperty("observationId").GetString() == "current").Select(row => row.GetProperty("seriesKey").GetString()));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, to, 3));
    }
    [Fact]
    public void NativeValuePolicyAndPreservedVisibilityStayDetachedFromLaterCallerAndFontMutation() {
        var renderer = new HtmlInteractiveChartRenderer(); var source = Styled("line-pattern-markers", 20); var destination = Styled("line-pattern-markers", 80);
        var view = new ChartVisibilityView(labelLayering: ChartVisibilityLabelLayering.GroupWithSeries);
        var from = renderer.PrepareVisibilitySection(source, view); var to = renderer.PrepareVisibilitySection(destination, view);
        var expected = renderer.PrepareVisibilitySection(Styled("line-pattern-markers", 50), view);
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var svg = frames.Select(frame => frame.ToSvg()).ToArray(); var png = frames.Select(frame => frame.ToPng()).ToArray();
        Capture(from, to, expected, frames, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)), "detached-view");
        source.Series[0].Points[1] = new ChartPoint(999, -999); source.Series[0].PointColors.Clear(); source.Series[0].ConfigureMarkers(markers => { markers.Shape = ChartMarkerShape.Diamond; markers.Fill = ChartColor.Black; });
        source.WithFontFamily("Changed later").WithValueFormat(ChartValueFormat.Custom(_ => "Changed later"));
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
            Assert.Equal(svg, repeated.Select(frame => frame.ToSvg())); for (var index = 0; index < repeated.Count; index++) Assert.Equal(png[index], repeated[index].ToPng());
            NativeValueIdentityProof.AssertEqual(expected.PreparedVisual, repeated[1], "cartesian-detached-view");
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Fact]
    public void ChangedTopologyDomainsPaintCallbacksAndUnrepresentedLayersRejectWithoutBlockingOrdinaryRendering() {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Styled("line-default-last", 20));
        var changes = new Action<Chart>[] {
            chart => chart.Series[0].Points[1] = new ChartPoint(1.5, 80), chart => chart.ConfigureYAxis(axis => axis.Maximum = 200),
            chart => chart.Series[0].WithColor(ChartColor.Black), chart => chart.WithDataLabels(true),
            chart => chart.WithValueFormat(ChartValueFormat.Custom(value => value.ToString(CultureInfo.InvariantCulture))),
            chart => chart.Series[0].WithObservationId(1, new ChartObservationId("other")), chart => chart.Series[0].WithInterpolation(ChartInterpolation.Step),
            chart => chart.ConfigureSecondaryXAxis(axis => axis.Reversed = true)
        };
        var failures = new List<object>(); var errors = new List<Exception?>(); var number = 0;
        foreach (var change in changes) { var chart = Styled("line-default-last", 80); change(chart); var to = renderer.PrepareSection(chart); Exception? failure = null;
            try { _ = renderer.PrepareValueTransitionJson(from, to, 3); } catch (Exception error) { failure = error; }
            failures.Add(new { svg = to.PreparedVisual.ToSvg(), error = failure?.Message }); errors.Add(failure);
            if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } directory) {
                Directory.CreateDirectory(directory); var prefix = Path.Combine(directory, "cartesian-rejection-" + number++);
                File.WriteAllText(prefix + ".svg", to.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".png", to.PreparedVisual.ToPng());
            }
        }
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "cartesian-rejections.json"), JsonSerializer.Serialize(failures)); }
        Assert.All(errors, error => Assert.IsType<NotSupportedException>(error));
        var bar = NativeCartesianValueBeforeControls.Model(true, "linear", 80).WithBarMode(ChartBarMode.Stacked); var stacked = renderer.PrepareSection(bar);
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, stacked, 3));
        var toValue = renderer.PrepareSection(Styled("line-default-last", 80));
        Assert.NotEmpty(renderer.PrepareNativeTransitionJson(from, toValue, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 3, ChartColor.White)));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel(); Assert.Throws<OperationCanceledException>(() => renderer.PrepareValueTransitionJson(from, toValue, 3, cancelled.Token));
    }
    internal static Chart Styled(string policy, double value) {
        var bar = policy.StartsWith("bar", StringComparison.Ordinal); var chart = NativeCartesianValueBeforeControls.Model(bar, "linear", value);
        chart.ConfigureYAxis(axis => { axis.Minimum = -100; axis.Maximum = 100; });
        if (!bar) { chart.Options.LineMarkerMode = null;
            if (policy == "line-pattern-markers") { chart.WithLineMarkers(ChartLineMarkerMode.All); chart.Series[0].FillPattern = ChartFillPattern.DiagonalForward; chart.Series[0].ConfigureMarkers(marker => { marker.Shape = ChartMarkerShape.Square; marker.Stroke = ChartColor.Black; marker.StrokeWidth = 2; }); }
            if (policy == "line-step") chart.Series[0].WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
            if (policy == "line-smooth-broken") { chart.Series[0].WithInterpolation(ChartInterpolation.Smooth); chart.Series[0].Points[2] = new ChartPoint(2, 60, true); chart.WithLuminousLineStyle(); }
        } else { chart.WithBarVisualStyle(policy.Contains("capsule", StringComparison.Ordinal) ? ChartBarVisualStyle.DashboardCapsule()
                : policy == "bar-gradient" ? ChartBarVisualStyle.Solid() : ChartBarVisualStyle.Flat());
            if (policy == "bar-gradient") chart.Series[0].FillPattern = ChartFillPattern.DiagonalForward;
        } return chart;
    }
    private static void Prove(Chart source, Chart destination, Chart middle, string name) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(source); var to = renderer.PrepareSection(destination); var expected = renderer.PrepareSection(middle);
        var frames = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3); var payload = renderer.PrepareValueTransitionJson(from, to, 3);
        Capture(from, to, expected, frames, payload, name); NativeValueIdentityProof.AssertEqual(expected.PreparedVisual, frames[1], "cartesian-" + name);
        Assert.Equal(expected.PreparedVisual.ToSvg("native-cartesian-middle"), frames[1].ToSvg("native-cartesian-middle")); Assert.Equal(expected.PreparedVisual.ToPng(), frames[1].ToPng());
        Assert.Equal(expected.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label)), frames[1].Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label)));
    }
    private static void Capture(HtmlChartSection from, HtmlChartSection to, HtmlChartSection expected, IReadOnlyList<PreparedVisual> frames, string payload, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var prefix = Path.Combine(output, "native-cartesian-" + name); File.WriteAllText(prefix + ".from.html", from.Html); File.WriteAllText(prefix + ".to.html", to.Html);
        File.WriteAllText(prefix + ".expected.svg", expected.PreparedVisual.ToSvg()); File.WriteAllBytes(prefix + ".expected.png", expected.PreparedVisual.ToPng()); File.WriteAllText(prefix + ".transition.json", payload);
        for (var index = 0; index < frames.Count; index++) { File.WriteAllText(prefix + ".frame-" + index + ".svg", frames[index].ToSvg()); File.WriteAllBytes(prefix + ".frame-" + index + ".png", frames[index].ToPng()); }
    }
}
