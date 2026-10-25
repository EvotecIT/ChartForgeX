using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRadialBandBeforeControls {
    [Theory] [InlineData("pie", true)] [InlineData("donut", false)] [InlineData("mixed", true)]
    public void ExistingOneEditApiUsesTheChangedBandAndScopedObservation(string family, bool automatic) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(family, automatic));
        var to = renderer.PrepareSection(Model(family, automatic, outer: 55)); var middle = renderer.PrepareSection(Model(family, automatic, outer: 50));
        var name = "radial-band-before-value-" + family + "-" + automatic;
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-middle");
        string? payload = null; Exception? error = null; try { payload = renderer.PrepareValueTransitionJson(from, to, 3); } catch (Exception failure) { error = failure; }
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.Message }));
        Assert.Null(error); using var json = JsonDocument.Parse(payload!); Assert.Equal("outer", json.RootElement.GetProperty("seriesKey").GetString());
        Assert.Equal("primary", json.RootElement.GetProperty("observationId").GetString()); Assert.Equal(45, json.RootElement.GetProperty("fromValue").GetDouble());
    }
    [Theory] [InlineData("pie", false)] [InlineData("donut", true)] [InlineData("mixed", false)]
    public void CurrentReflowApiRetainsTheFullBandCatalogWhenOneRingIsHidden(string family, bool automatic) {
        var renderer = new HtmlInteractiveChartRenderer(); var model = Model(family, automatic); Exception? error = null; HtmlChartSection? from = null, to = null;
        try { from = renderer.PrepareVisibilitySection(model, View()); to = renderer.PrepareVisibilitySection(model, View(ChartVisibilityTarget.Series(new ChartSeriesId("outer")))); } catch (Exception failure) { error = failure; }
        var name = "radial-band-before-reflow-" + family + "-" + automatic;
        if (from != null) NativeRadialMotionBeforeControls.Capture(from, name + "-from"); if (to != null) NativeRadialMotionBeforeControls.Capture(to, name + "-to");
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { error = error?.Message })); Assert.Null(error); Assert.NotNull(to);
        Assert.Equal(2, to!.PreparedVisual.Scene.VisibilityCapture!.SeriesKeys.Count);
    }
    [Theory] [InlineData("pie", true)] [InlineData("donut", false)] [InlineData("mixed", true)]
    public void OrdinaryNativeBandsAndSingleSeriesDefaultsKeepRepeatableSvgPngAndHtml(string family, bool automatic) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(family, automatic); var first = renderer.PrepareSection(chart); var repeat = renderer.PrepareSection(chart);
        NativeRadialMotionBeforeControls.Capture(first, "radial-band-before-ordinary-" + family + "-" + automatic);
        Assert.Equal(first.Html, repeat.Html); Assert.Equal(first.PreparedVisual.ToSvg(), repeat.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), repeat.PreparedVisual.ToPng());
        var single = renderer.PrepareSection(NativeRadialMotionBeforeControls.Model(family == "donut", false));
        NativeRadialMotionBeforeControls.Capture(single, "radial-band-before-single-" + family + "-" + automatic);
        var singleRepeat = renderer.PrepareSection(NativeRadialMotionBeforeControls.Model(family == "donut", false)); Assert.Equal(single.Html, singleRepeat.Html); Assert.Equal(single.PreparedVisual.ToPng(), singleRepeat.PreparedVisual.ToPng());
    }
    internal static Chart Model(string family = "mixed", bool automatic = true, bool other = false, double inner = 60, double outer = 45, int width = 760, bool dark = false, string labels = "inside") {
        var chart = NativeRadialMotionBeforeControls.Model(family == "donut", other, labels, inner, width, dark);
        var values = other ? new[] { outer, 20d, 9d, 3d, 1d } : new[] { outer, 20d, 5d };
        var outside = new ChartSeries(new ChartSeriesId("outer"), "Outer", family == "pie" ? ChartSeriesKind.Pie : ChartSeriesKind.Donut,
            values.Select((value, index) => new ChartPoint(index + 1, value)))
            .WithObservationIds(values.Select((_, index) => new ChartObservationId(index == 0 ? "primary" : "part-" + index)));
        if (automatic) chart.WithAutomaticRadialBands(new ChartRadialBandAllocation(.12, .06));
        else { chart.Series[0].WithRadialBand(new ChartRadialBand(.12, .49)); outside.WithRadialBand(new ChartRadialBand(.55, 1)); }
        chart.AddSeries(outside); return chart;
    }
    internal static ChartVisibilityView View(params ChartVisibilityTarget[] targets) => new(targets, ChartVisibilityLayout.Reflow);
}
