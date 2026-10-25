using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeCompleteCartesianValueBeforeControls {
    [Theory] [InlineData("line-labels")] [InlineData("bar-labels")] [InlineData("area")] [InlineData("stacked-area")]
    public void ExistingNativeValueEntryCapturesActualEnabledCaptionAreaAndStackGeometry(string variant) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(variant, 30)); var to = renderer.PrepareSection(Model(variant, 40));
        var expected = renderer.PrepareSection(Model(variant, ChartForgeX.Rendering.ChartMath.InterpolateRange(30, 40, .5)));
        string? payload = null; var error = Record.Exception(() => payload = renderer.PrepareValueTransitionJson(from, to, 3));
        NativeRadialMotionBeforeControls.Capture(from, "complete-before-" + variant + "-from"); NativeRadialMotionBeforeControls.Capture(to, "complete-before-" + variant + "-to");
        NativeRadialMotionBeforeControls.Capture(expected, "complete-before-" + variant + "-independent");
        NativeRadialMotionBeforeControls.Save("complete-before-" + variant + ".facts.json", JsonSerializer.Serialize(new { error = error?.ToString(), payload }));
        Assert.Null(error); Assert.NotNull(payload);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OrdinaryEnabledLabelRenderingKeepsItsExistingNativeBytes(bool bar) {
        var renderer = new HtmlInteractiveChartRenderer(); var variant = bar ? "bar-labels" : "line-labels";
        var first = renderer.PrepareSection(Model(variant, 30)); var second = renderer.PrepareSection(Model(variant, 30));
        NativeRadialMotionBeforeControls.Capture(first, "complete-before-ordinary-" + bar);
        Assert.Equal(first.Html, second.Html); Assert.Equal(first.PreparedVisual.ToSvg(), second.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), second.PreparedVisual.ToPng());
    }
    private static Chart Model(string variant, double value) {
        var kind = variant == "bar-labels" ? ChartSeriesKind.Bar : variant == "area" ? ChartSeriesKind.Area : variant == "stacked-area" ? ChartSeriesKind.StackedArea : ChartSeriesKind.Line;
        var series = new ChartSeries(new ChartSeriesId("delivery"), "Delivery", kind, new[] { new ChartPoint(0, 20), new ChartPoint(1, value), new ChartPoint(2, 40) })
            .WithObservationIds(new[] { new ChartObservationId("earlier"), new ChartObservationId("current"), new ChartObservationId("later") });
        var chart = Chart.Create().WithSize(760, 420).WithFontFamily(BrowserProofFonts.Family).WithDataLabels(true).WithLegend(false).AddSeries(series);
        chart.ConfigureXAxis(axis => { axis.Minimum = -.5; axis.Maximum = 2.5; }); chart.ConfigureYAxis(axis => { axis.Minimum = 0; axis.Maximum = 160; });
        if (kind == ChartSeriesKind.StackedArea) chart.AddSeries(new ChartSeries(new ChartSeriesId("comparison"), "Comparison", kind, new[] { new ChartPoint(0, 10), new ChartPoint(1, 15), new ChartPoint(2, 20) })
            .WithObservationIds(new[] { new ChartObservationId("comparison-a"), new ChartObservationId("comparison-b"), new ChartObservationId("comparison-c") }));
        return chart;
    }
}
