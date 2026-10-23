using System.Collections.Generic;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Authored axes cannot retain a request from a detached preparation view.</summary>
public sealed class ChartViewportPresentationIsolationTests {
    [Theory]
    [InlineData("x", false)]
    [InlineData("x", true)]
    [InlineData("y", false)]
    [InlineData("y", true)]
    [InlineData("secondaryX", false)]
    [InlineData("secondaryX", true)]
    [InlineData("secondaryY", false)]
    [InlineData("secondaryY", true)]
    public void NumericFullEndpointWindowsCannotLeakIntoLaterFullPreparation(string slot, bool reversed) {
        var horizontal = slot is "y" or "secondaryX";
        var chart = Numeric(horizontal);
        Axis(chart, slot).Reversed = reversed;
        Axis(chart, slot).Labels.Add(new ChartAxisLabel(slot is "x" or "y" ? 2 : 20, "Authored caption"));
        AssertFullResetIsolation(chart, slot);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LogarithmicFullEndpointWindowsCannotLeakIntoLaterFullPreparation(bool reversed) {
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false).WithDataLabels(false)
            .AddLine("Logarithmic", new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(4, 30) })
            .WithXAxisBounds(1, 4);
        chart.Options.XAxis.WithLogarithmicScale(2).WithReversal(reversed);
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(2, "Two"));
        AssertFullResetIsolation(chart, "x");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IncludedSessionFullEndpointWindowsCannotLeakIntoLaterFullPreparation(bool reversed) {
        var friday = new DateTime(2026, 1, 30, 9, 0, 0, DateTimeKind.Utc);
        var monday = friday.AddDays(3);
        var domain = new ChartTimeSessionDomain(new[] {
            new ChartTimeSession(friday, friday.AddHours(8)),
            new ChartTimeSession(monday, monday.AddHours(8)) });
        var dates = new[] { friday, friday.AddHours(4), monday, monday.AddHours(8) };
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false).WithDataLabels(false)
            .AddLine("Included time", dates.Select((date, index) => new ChartPoint(ChartDateTime.ToOADate(date), index + 1)))
            .WithXAxisBounds(ChartDateTime.ToOADate(friday), ChartDateTime.ToOADate(monday.AddHours(8)));
        chart.Options.XAxis.WithTimeSessions(domain).WithReversal(reversed);
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(friday, "Open"));
        AssertFullResetIsolation(chart, "x");
        Assert.Same(domain, chart.Options.XAxis.TimeSessions);
    }

    private static void AssertFullResetIsolation(Chart chart, string slot) {
        var axes = Axes(chart);
        var labels = axes.Select(axis => axis.Labels).ToArray();
        var labelValues = labels.Select(values => values.ToArray()).ToArray();
        var authoredBounds = axes.Select(axis => (axis.Minimum, axis.Maximum)).ToArray();
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)));
        var renderer = new HtmlInteractiveChartRenderer();
        var full = chart.Prepare(context);
        var htmlFull = renderer.PrepareSection(chart, null);
        var fullSvg = full.ToSvg(); var fullPng = full.ToPng();
        var htmlFullSvg = htmlFull.PreparedVisual.ToSvg();
        var htmlFullPng = htmlFull.PreparedVisual.ToPng();
        var fullProjection = Projection(full, slot);
        var htmlFullProjection = Projection(htmlFull.PreparedVisual, slot);
        Assert.True(fullProjection.MaximumInclusive);
        Assert.True(htmlFullProjection.MaximumInclusive);
        // Exact resolved full ends are essential: an interior maximum cannot expose the stale-policy match.
        var requested = new ChartAxisWindow(fullProjection.FullMinimum, fullProjection.FullMaximum, maximumInclusive: false);
        var exclusive = chart.Prepare(context, Viewport(slot, requested));
        var exclusiveSvg = exclusive.ToSvg(); var exclusivePng = exclusive.ToPng();
        var reset = chart.Prepare(context, null);
        var htmlRequested = new ChartAxisWindow(htmlFullProjection.FullMinimum, htmlFullProjection.FullMaximum, maximumInclusive: false);
        var htmlExclusive = renderer.PrepareSection(chart, Viewport(slot, htmlRequested));
        var htmlExclusiveSvg = htmlExclusive.PreparedVisual.ToSvg();
        var htmlExclusivePng = htmlExclusive.PreparedVisual.ToPng();
        var htmlReset = renderer.PrepareSection(chart, null);

        // Materialize both supported paths before checking: a core failure must not hide the HTML reset oracle.
        AssertSourceUnchanged(chart, axes, labels, labelValues, authoredBounds);
        Assert.True(Projection(reset, slot).MaximumInclusive);
        Assert.True(Projection(htmlReset.PreparedVisual, slot).MaximumInclusive);
        using var resetMetadata = Metadata(htmlReset.Html);
        using var exclusiveMetadata = Metadata(htmlExclusive.Html);
        AssertInclusiveMetadata(resetMetadata.RootElement.GetProperty(slot + "Projection"));
        Assert.False(exclusiveMetadata.RootElement.GetProperty(slot + "Projection").GetProperty("maximumInclusive").GetBoolean());
        Assert.Equal(fullSvg, reset.ToSvg()); Assert.Equal(fullPng, reset.ToPng());
        Assert.Equal(htmlFullSvg, htmlReset.PreparedVisual.ToSvg()); Assert.Equal(htmlFullPng, htmlReset.PreparedVisual.ToPng());
        Assert.Null(Projection(reset, slot).ConstrainViewport(new ChartAxisWindow(fullProjection.FullMinimum, fullProjection.FullMaximum)));
        Assert.True(fullProjection.MaximumInclusive); Assert.True(htmlFullProjection.MaximumInclusive);
        Assert.False(Projection(exclusive, slot).MaximumInclusive);
        Assert.False(Projection(htmlExclusive.PreparedVisual, slot).MaximumInclusive);
        Assert.Equal(fullSvg, full.ToSvg()); Assert.Equal(fullPng, full.ToPng());
        Assert.Equal(exclusiveSvg, exclusive.ToSvg()); Assert.Equal(exclusivePng, exclusive.ToPng());
        Assert.Equal(htmlExclusiveSvg, htmlExclusive.PreparedVisual.ToSvg()); Assert.Equal(htmlExclusivePng, htmlExclusive.PreparedVisual.ToPng());
    }

    private static void AssertSourceUnchanged(Chart chart, ChartAxis[] axes, List<ChartAxisLabel>[] labels,
        ChartAxisLabel[][] values, (double? Minimum, double? Maximum)[] bounds) {
        var current = Axes(chart);
        for (var index = 0; index < axes.Length; index++) {
            Assert.Same(axes[index], current[index]); Assert.Same(labels[index], current[index].Labels);
            Assert.Equal(values[index], current[index].Labels.ToArray());
            Assert.Equal(bounds[index].Minimum, current[index].Minimum); Assert.Equal(bounds[index].Maximum, current[index].Maximum);
        }
    }
    private static Chart Numeric(bool horizontal) {
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 100) };
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false).WithDataLabels(false)
            .AddLine("Primary", points).AddLine("Secondary", points);
        chart.Series[1].UseSecondaryValueAxis();
        if (horizontal) chart.Options.LineOrientation = ChartOrientation.Horizontal;
        return chart;
    }
    private static ChartAxis[] Axes(Chart chart) => new[] {
        chart.Options.XAxis, chart.Options.YAxis, chart.Options.SecondaryXAxis, chart.Options.SecondaryYAxis };
    private static ChartAxis Axis(Chart chart, string slot) => slot switch {
        "x" => chart.Options.XAxis, "y" => chart.Options.YAxis,
        "secondaryX" => chart.Options.SecondaryXAxis, _ => chart.Options.SecondaryYAxis };
    private static ChartAxisProjection Projection(PreparedVisual prepared, string slot) => slot switch {
        "x" => prepared.XProjection!, "y" => prepared.YProjection!,
        "secondaryX" => prepared.SecondaryXProjection!, _ => prepared.SecondaryYProjection! };
    private static ChartCartesianViewport Viewport(string slot, ChartAxisWindow window) => new(
        slot == "x" ? window : null, slot == "y" ? window : null,
        slot == "secondaryX" ? window : null, slot == "secondaryY" ? window : null);
    private static JsonDocument Metadata(string html) => JsonDocument.Parse(System.Net.WebUtility.HtmlDecode(
        System.Text.RegularExpressions.Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]*)\"").Groups[1].Value));
    private static void AssertInclusiveMetadata(JsonElement projection) {
        if (projection.TryGetProperty("maximumInclusive", out var value)) Assert.True(value.GetBoolean());
    }
}
