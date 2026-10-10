using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WaterfallCheckpointTests {
    [Fact]
    public void TypedInputKeepsImmutableItemsWithoutInventingCheckpointSourceValues() {
        var items = Items();
        var chart = Chart.Create().AddWaterfall("Movement", items);
        var series = chart.Series[0];
        items[0] = ChartWaterfallItem.Delta(40, 999);
        Assert.Empty(series.Points);
        Assert.Equal(4, series.SourcePointCount);
        Assert.Equal(100, series.WaterfallItems[0].Value);
        Assert.Null(series.WaterfallItems[1].Value);
        Assert.Equal(ChartWaterfallItemKind.Subtotal, series.WaterfallItems[1].Kind);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartWaterfallItem>)series.WaterfallItems)[0] = items[0]);
        series.Points.Add(new ChartPoint(90, 10));
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(new VisualRenderContext()));
    }

    [Fact]
    public void CheckpointsPreserveRunningBalanceAuthoredOrderAndOnlyTheirContributingDeltas() {
        var chart = Chart.Create().AddWaterfall("Movement", Items());
        var steps = ChartWaterfallSteps.Create(chart.Series[0]);
        Assert.Equal(new[] { 40d, 10, 30, 20, 50, 60, 70, 80 }, steps.Select(step => step.X));
        Assert.Equal(new[] { 0d, 0, 100, 100, -40, -40, 0, -15 }, steps.Select(step => step.Start));
        Assert.Equal(new[] { 100d, 100, -40, -40, -40, -15, -15, -5 }, steps.Select(step => step.End));
        Assert.Equal(new[] { 100d, 100, -140, -140, 0, 25, -15, 10 }, steps.Select(step => step.Value));
        Assert.Equal(new[] { 0 }, steps[1].SourceIndices);
        Assert.Equal(new[] { 1 }, steps[3].SourceIndices);
        Assert.Empty(steps[4].SourceIndices);
        Assert.Equal(new[] { 0, 1, 2 }, steps[6].SourceIndices);
        Assert.Equal(3, steps[7].SourceIndex);
        Assert.DoesNotContain(steps, step => step.IsAppendedTotal);
        var range = ChartRange.FromChart(chart);
        Assert.True(range.MinY <= -40); Assert.True(range.MaxY >= 100);

        var continuation = Chart.Create().AddWaterfall("Continuation", new[] {
            ChartWaterfallItem.Delta(1, 100), ChartWaterfallItem.Total(2), ChartWaterfallItem.Total(3),
            ChartWaterfallItem.Delta(4, -20), ChartWaterfallItem.Subtotal(5), ChartWaterfallItem.Delta(6, 5)
        });
        var continued = ChartWaterfallSteps.Create(continuation.Series[0]);
        Assert.Equal(100, continued[2].Value);
        Assert.Equal(-20, continued[4].Value); Assert.Equal(100, continued[4].Start); Assert.Equal(80, continued[4].End);
        Assert.Equal(new[] { 1 }, continued[4].SourceIndices);
        Assert.Equal(80, continued[5].Start); Assert.Equal(85, continued[5].End);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedMarksLegendsAndLabelsShareTypedFactsAndExistingPointOverrides(bool secondary) {
        var chart = Chart.Create().WithSize(900, 500).WithPointLegend().WithDataLabels().WithBarStyle(ChartBarStyle.Flat)
            .AddWaterfall("Movement", Items());
        chart.Options.Labels.Subtotal = "Stage sum"; chart.Options.Labels.Total = "Balance"; chart.Options.Labels.Change = "Movement";
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(20, "Costs"));
        var series = chart.Series[0];
        var accent = ChartColor.FromHex("#A5358A");
        series.WithPointColor(3, accent).WithPointFillPattern(3, ChartFillPattern.Crosshatch).WithPointLabel(3, "Cost checkpoint")
            .ConfigurePointDataLabelStyle(3, style => style.WithColor("#FFFFFF"));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.WithPointColor(8, accent));
        if (secondary) series.UseSecondaryYAxis();
        var axis = secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
        axis.WithReversal(); chart.Options.XAxis.WithReversal();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.no-data");
        var groups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Metadata.ContainsKey("data-cfx-waterfall-kind")
            && group.Role is "point" or "waterfall-subtotal" or "waterfall-total").ToArray();
        Assert.Equal(8, groups.Length);
        Assert.Equal("Cost checkpoint", groups[3].Metadata["data-cfx-label"]);
        Assert.Equal("subtotal", groups[3].Metadata["data-cfx-waterfall-kind"]);
        Assert.Equal("-140", groups[3].Metadata["data-cfx-value"]);
        Assert.Equal("1", groups[3].Metadata["data-cfx-source-points"]);
        Assert.False(groups[3].Metadata.ContainsKey("data-cfx-source-point"));
        Assert.False(groups[3].Metadata.ContainsKey("data-cfx-delta"));
        Assert.Equal("3", groups[7].Metadata["data-cfx-source-point"]);
        Assert.Equal("10", groups[7].Metadata["data-cfx-delta"]);
        var sourceGroup = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "series");
        Assert.Equal("4", sourceGroup.Metadata["data-cfx-source-points"]); Assert.Equal("8", sourceGroup.Metadata["data-cfx-rendered-points"]);
        var legends = VisualCartesianCompiler.LegendEntries(chart, new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light));
        Assert.Equal(8, legends.Count); Assert.Equal("Costs", legends[3].Label); Assert.Equal(accent, legends[3].Color);
        Assert.Equal(ChartFillPattern.Crosshatch, legends[3].Pattern);
        Assert.Equal(groups[3].Metadata["data-cfx-derived-identity"], legends[3].Metadata!["data-cfx-derived-identity"]);
        Assert.Equal("3", legends[7].Metadata!["data-cfx-source-point"]);
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-cfx-label-for") == "series-0-point-3");
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label?.StartsWith("Stage sum (", StringComparison.Ordinal) == true);
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label?.StartsWith("Balance (", StringComparison.Ordinal) == true);
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label?.StartsWith("Costs (", StringComparison.Ordinal) == true);
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label == "80 (80)");
        Assert.DoesNotContain(prepared.Regions, region => region.Role == "axis-x-label" && region.Label?.StartsWith("Total (", StringComparison.Ordinal) == true);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "waterfall-bar-pattern");
    }

    [Fact]
    public void TypedAndLegacyRoutesShareMathAndPreparedArtifactsDetachFromLaterChanges() {
        var raw = new[] { new ChartPoint(10, 100), new ChartPoint(20, -40), new ChartPoint(40, 80) };
        var legacy = Chart.Create().AddWaterfall("Movement", raw);
        var typed = Chart.Create().AddWaterfall("Movement", new[] {
            ChartWaterfallItem.Delta(10, 100), ChartWaterfallItem.Delta(20, -40), ChartWaterfallItem.Delta(40, 80), ChartWaterfallItem.Total(50)
        });
        var legacySteps = ChartWaterfallSteps.Create(legacy.Series[0]); var typedSteps = ChartWaterfallSteps.Create(typed.Series[0]);
        Assert.Equal(legacySteps.Select(step => (step.X, step.Value, step.Start, step.End)), typedSteps.Select(step => (step.X, step.Value, step.Start, step.End)));
        Assert.True(legacySteps[3].IsAppendedTotal); Assert.False(typedSteps[3].IsAppendedTotal);
        Assert.Equal(raw, legacy.Series[0].Points); Assert.Empty(typed.Series[0].Points);
        var prepared = typed.Prepare(VisualExportRequest.ForChart(typed).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var artifact = prepared.ToArtifact("waterfall", VisualArtifactKind.Chart);
        var artifactSvg = artifact.ToSvg(); var artifactPng = artifact.ToPng(); var interchange = artifact.ToInterchangeJson();
        typed.Series.Clear(); typed.Options.Labels.Total = "Changed";
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(artifactSvg, artifact.ToSvg()); Assert.Equal(artifactPng, artifact.ToPng()); Assert.Equal(interchange, artifact.ToInterchangeJson());
        Assert.Contains("data-cfx-waterfall-kind=\"total\"", svg);
        Assert.Contains("data-cfx-source-points=\"0,1,2\"", svg);
        Assert.Contains("data-cfx-waterfall-kind=\"total\"", artifactSvg);
        Assert.Contains("data-cfx-source-points=\"0,1,2\"", artifactSvg);
    }

    [Fact]
    public void ExplicitZeroCheckpointsAreDataAndInvalidInputsRejectWithoutAddingASeries() {
        var chart = Chart.Create();
        Assert.Throws<ArgumentException>(() => chart.AddWaterfall("Empty", Array.Empty<ChartWaterfallItem>()));
        Assert.Throws<ArgumentException>(() => chart.AddWaterfall("Duplicate", new[] { ChartWaterfallItem.Delta(1, 2), ChartWaterfallItem.Total(1) }));
        Assert.Throws<ArgumentException>(() => chart.AddWaterfall("Null", new ChartWaterfallItem[] { null! }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartWaterfallItem.Delta(1, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartWaterfallItem.Total(double.PositiveInfinity));
        Assert.Empty(chart.Series);
        chart.AddWaterfall("Zero", new[] { ChartWaterfallItem.Subtotal(1), ChartWaterfallItem.Total(2) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(0, chart.Series[0].SourcePointCount);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.no-data");
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role is "waterfall-subtotal" or "waterfall-total"));
        var overflow = Chart.Create().AddWaterfall("Overflow", new[] { ChartWaterfallItem.Delta(1, double.MaxValue), ChartWaterfallItem.Delta(2, double.MaxValue) });
        Assert.Throws<InvalidOperationException>(() => overflow.Prepare(new VisualRenderContext()));
        var subtotalOverflow = Chart.Create().AddWaterfall("Subtotal overflow", new[] {
            ChartWaterfallItem.Delta(1, 1e308), ChartWaterfallItem.Total(2), ChartWaterfallItem.Delta(3, -1e308),
            ChartWaterfallItem.Delta(4, -1e308), ChartWaterfallItem.Subtotal(5)
        });
        Assert.Throws<InvalidOperationException>(() => ChartWaterfallSteps.Create(subtotalOverflow.Series[0]));
    }

    private static ChartWaterfallItem[] Items() => new[] {
        ChartWaterfallItem.Delta(40, 100), ChartWaterfallItem.Subtotal(10), ChartWaterfallItem.Delta(30, -140),
        ChartWaterfallItem.Subtotal(20), ChartWaterfallItem.Subtotal(50), ChartWaterfallItem.Delta(60, 25),
        ChartWaterfallItem.Total(70), ChartWaterfallItem.Delta(80, 10)
    };
}
