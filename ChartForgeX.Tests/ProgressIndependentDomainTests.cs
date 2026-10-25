using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ProgressIndependentDomainTests {
    [Theory]
    [InlineData(390, false)]
    [InlineData(760, true)]
    public void RowDomainsKeepRawMeasurementsAndIndependentClampedGeometry(int width, bool dark) {
        var chart = IndependentProgressRecipe.Create(width == 390 ? "compact" : "wide", dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.Series[0].UpdateProgressItem(1, new ChartProgressItem("Verified jobs", 2500, 0, 2000));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var rows = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "progress-row").ToArray();
        Assert.Equal(new[] { "0.72", "1", "0.3" }, rows.Select(row => row.Metadata["data-cfx-ratio"]));
        Assert.Equal("2500", rows[1].Metadata["data-cfx-value"]); Assert.Equal("-20", rows[2].Metadata["data-cfx-min"]);
        Assert.Equal(new[] { "reviewed", "verified", "temperature" }, rows.Select(row => row.Metadata["data-cfx-observation-id"]));
        var svg = prepared.ToSvg(); chart.Series[0].ReplaceProgressItems(Array.Empty<ChartProgressItem>());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Contains(chart.Prepare(new VisualRenderContext()).Diagnostics, diagnostic => diagnostic.Code == "progress.no-data");
        ScalarDepthCapture.Write(prepared, "independent-progress-" + width + "-" + dark);
    }

    [Fact]
    public void TypedLifecycleRetainsIdentityOnUpdateAndReplacesSourceWithoutWritablePointAliases() {
        var items = new[] { new ChartProgressItem("Temperature", -8, -20, 20) };
        var series = ChartSeries.CreateProgressBars(new ChartSeriesId("source"), "Source", items).WithObservationId(0, new ChartObservationId("one"));
        items[0] = new ChartProgressItem("Changed", 1);
        Assert.Equal("Temperature", series.ProgressItems[0].Label); Assert.Empty(series.Points); Assert.Equal(1, series.SourcePointCount);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartProgressItem>)series.ProgressItems)[0] = new ChartProgressItem("Mutated", 1));
        series.UpdateProgressItem(0, new ChartProgressItem("Temperature", 3, -20, 20)); Assert.Equal("one", series.ObservationIds[0].Value);
        Assert.Throws<ArgumentNullException>(() => series.ReplaceProgressItems(new[] { default(ChartProgressItem) }));
        Assert.Equal(3, series.ProgressItems[0].Value); Assert.Equal("one", series.ObservationIds[0].Value);
        var chart = Chart.Create().AddSeries(series); var envelope = chart.ToVisualArtifact("progress").ToInterchangeEnvelope();
        var facts = envelope.Extensions["chart.series.0.scalar.sourceFacts"]; Assert.Contains("minimum", facts, StringComparison.Ordinal);
        series.ReplaceProgressItems(Array.Empty<ChartProgressItem>()); Assert.Empty(series.ObservationIds); Assert.Equal(facts, envelope.Extensions["chart.series.0.scalar.sourceFacts"]);
    }

    [Fact]
    public void SharedFactoryPreservesTheNumericSharedDomainDrawing() {
        var typed = Chart.Create().WithLegend(false).AddProgressBars("Rows", new[] { new ChartProgressItem("First", 40), new ChartProgressItem("Second", 120) });
        var numeric = Chart.Create().WithLegend(false).WithXLabels("First", "Second")
            .AddSeries(new ChartSeries("Rows", ChartSeriesKind.ProgressBar, new[] { new ChartPoint(1, 40), new ChartPoint(2, 120) }));
        var context = new VisualRenderContext(); var first = typed.Prepare(context); var second = numeric.Prepare(context);
        Assert.Equal(first.ToSvg("same"), second.ToSvg("same"));
        Assert.Equal(first.ToPng(new VisualRenderOptions(supersampling: 1)), second.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(2, typed.Series[0].ObservationCount); Assert.Empty(typed.Series[0].Points); Assert.Equal(2, numeric.Series[0].Points.Count);
    }
}
