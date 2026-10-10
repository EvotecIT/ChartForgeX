using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WaterfallOpeningBalanceTests {
    [Fact]
    public void OpeningBalanceSeedsTheRunningBalanceAndSubtotalAnchorWithItsOwnSourceOrdinal() {
        var chart = Chart.Create().AddWaterfall("Movement", new[] {
            ChartWaterfallItem.OpeningBalance(40, 100), ChartWaterfallItem.Subtotal(10), ChartWaterfallItem.Delta(30, 30),
            ChartWaterfallItem.Subtotal(20), ChartWaterfallItem.Delta(50, -160), ChartWaterfallItem.Total(60),
            ChartWaterfallItem.Total(70), ChartWaterfallItem.Delta(80, 10), ChartWaterfallItem.Subtotal(90)
        });
        var steps = ChartWaterfallSteps.Create(chart.Series[0]);
        Assert.Equal(4, chart.Series[0].SourcePointCount);
        Assert.Empty(chart.Series[0].Points);
        Assert.Equal(new[] { 40d, 10, 30, 20, 50, 60, 70, 80, 90 }, steps.Select(step => step.X));
        Assert.Equal(new[] { 0d, 100, 100, 100, 130, 0, 0, -30, -30 }, steps.Select(step => step.Start));
        Assert.Equal(new[] { 100d, 100, 130, 130, -30, -30, -30, -20, -20 }, steps.Select(step => step.End));
        Assert.Equal(new[] { 100d, 0, 30, 30, -160, -30, -30, 10, 10 }, steps.Select(step => step.Value));
        Assert.False(steps[0].IsCheckpoint);
        Assert.Equal(0, steps[0].SourceIndex); Assert.Equal(new[] { 0 }, steps[0].SourceIndices);
        Assert.Empty(steps[1].SourceIndices);
        Assert.Equal(new[] { 1 }, steps[3].SourceIndices);
        Assert.Equal(new[] { 0, 1, 2 }, steps[5].SourceIndices);
        Assert.Equal(steps[5].SourceIndices, steps[6].SourceIndices);
        Assert.Equal(3, steps[7].SourceIndex); Assert.Equal(new[] { 3 }, steps[8].SourceIndices);
        Assert.DoesNotContain(steps, step => step.IsAppendedTotal);
        var range = ChartRange.FromChart(chart);
        Assert.True(range.MinY <= -30); Assert.True(range.MaxY >= 130);
    }

    [Theory]
    [InlineData(-100d)]
    [InlineData(0d)]
    [InlineData(100d)]
    public void AnOpeningBalanceAloneIsDataWithNoInventedChangeOrTotal(double value) {
        var chart = Chart.Create().WithDataLabels().AddWaterfall("Opening", new[] { ChartWaterfallItem.OpeningBalance(1, value) });
        Assert.Equal(1, chart.Series[0].SourcePointCount);
        var step = Assert.Single(ChartWaterfallSteps.Create(chart.Series[0]));
        Assert.Equal(0, step.Start); Assert.Equal(value, step.End); Assert.Equal(value, step.Value);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.no-data");
        var opening = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "point");
        Assert.Equal("opening-balance", opening.Metadata["data-cfx-waterfall-kind"]);
        Assert.Equal("0", opening.Metadata["data-cfx-source-point"]);
        Assert.False(opening.Metadata.ContainsKey("data-cfx-delta")); Assert.False(opening.Metadata.ContainsKey("data-cfx-derived"));
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "waterfall-total");
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label == "Opening balance (1)");
    }

    [Fact]
    public void OpeningFactoriesAndSnapshotsRetainAuthoredValuesAndRejectInvalidShapeAtomically() {
        var date = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var opening = ChartWaterfallItem.OpeningBalance(date, -25);
        Assert.Equal(new ChartPoint(date, -25).X, opening.X); Assert.Equal(-25, opening.Value);
        var items = new[] { opening, ChartWaterfallItem.Subtotal(date.AddDays(1)) };
        var chart = Chart.Create().AddWaterfall("Opening", items);
        items[0] = ChartWaterfallItem.OpeningBalance(date, 999);
        Assert.Same(opening, chart.Series[0].WaterfallItems[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartWaterfallItem>)chart.Series[0].WaterfallItems)[0] = items[0]);
        var json = JsonSerializer.SerializeToElement(chart.Series[0].WaterfallItems);
        Assert.Equal(-25, json[0].GetProperty("Value").GetDouble());
        Assert.Equal(JsonValueKind.Null, json[1].GetProperty("Value").ValueKind);
        Assert.Equal(3, json[0].GetProperty("Kind").GetInt32());
        Assert.False(json[0].TryGetProperty("DeltaValue", out _));
        Assert.Throws<ArgumentException>(() => chart.AddWaterfall("Late", new[] { ChartWaterfallItem.Delta(1, 2), ChartWaterfallItem.OpeningBalance(2, 3) }));
        Assert.Throws<ArgumentException>(() => chart.AddWaterfall("Repeated", new[] { ChartWaterfallItem.OpeningBalance(1, 2), ChartWaterfallItem.OpeningBalance(2, 3) }));
        Assert.Throws<ArgumentException>(() => chart.AddWaterfall("After checkpoint", new[] { ChartWaterfallItem.Total(1), ChartWaterfallItem.OpeningBalance(2, 3) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartWaterfallItem.OpeningBalance(1, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartWaterfallItem.OpeningBalance(1, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartWaterfallItem.OpeningBalance(double.NegativeInfinity, 1));
        Assert.Single(chart.Series); Assert.Same(opening, chart.Series[0].WaterfallItems[0]);
        var overflow = Chart.Create().AddWaterfall("Overflow", new[] {
            ChartWaterfallItem.OpeningBalance(1, double.MaxValue), ChartWaterfallItem.Delta(2, double.MaxValue)
        });
        Assert.Throws<InvalidOperationException>(() => overflow.Prepare(new VisualRenderContext()));
    }

    [Theory]
    [InlineData(false, 100d)]
    [InlineData(true, -100d)]
    public void OpeningMarksLegendsAndDetachedArtifactsShareLocalizedSourceFactsAndPointPaint(bool secondary, double value) {
        var chart = Chart.Create().WithSize(700, 420).WithPointLegend().WithDataLabels().WithBarStyle(ChartBarStyle.Flat)
            .AddWaterfall("Movement", new[] { ChartWaterfallItem.OpeningBalance(1, value), ChartWaterfallItem.Delta(2, -value / 2), ChartWaterfallItem.Total(3) });
        chart.ConfigureLabels(labels => labels.OpeningBalance = "Initial balance");
        if (secondary) chart.Series[0].UseSecondaryYAxis();
        (secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis).WithReversal(); chart.Options.XAxis.WithReversal();
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context);
        var groups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Metadata.ContainsKey("data-cfx-waterfall-kind")
            && group.Role is "point" or "waterfall-total").ToArray();
        Assert.Equal("Initial balance", groups[0].Metadata["data-cfx-label-waterfall-value"]);
        Assert.Equal("1", groups[0].Metadata["data-cfx-source-count"]);
        Assert.Equal("0", groups[0].Metadata["data-cfx-source-point"]);
        Assert.Equal("1", groups[1].Metadata["data-cfx-source-point"]);
        Assert.Equal("0,1", groups[2].Metadata["data-cfx-source-points"]);
        Assert.Equal(value.ToString(System.Globalization.CultureInfo.InvariantCulture), groups[0].Metadata["data-cfx-label"]);
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label == "Initial balance (1)");
        var colors = context.Theme.Resolve(context.ThemeMode);
        var legends = VisualCartesianCompiler.LegendEntries(chart, colors);
        Assert.Equal("Initial balance", legends[0].Label); Assert.Equal(colors.Status.Medium.Fill, legends[0].Color);
        Assert.Equal(groups[0].Metadata["data-cfx-source-point"], legends[0].Metadata!["data-cfx-source-point"]);
        Assert.Equal(colors.Status.Medium.Fill, prepared.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "waterfall-bar").Fill);
        var accent = ChartColor.FromHex("#A5358A");
        chart.Series[0].WithPointColor(0, accent).WithPointFillPattern(0, ChartFillPattern.Crosshatch).WithPointLabel(0, "Opening caption")
            .ConfigurePointDataLabelStyle(0, style => style.WithColor("#FFFFFF"));
        var overridden = chart.Prepare(context);
        Assert.Equal(accent, overridden.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "waterfall-bar").Fill);
        Assert.Contains(overridden.Scene.Nodes, node => node.Role == "waterfall-bar-pattern");
        Assert.Contains(overridden.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "point" && group.Metadata["data-cfx-label"] == "Opening caption");
        var calls = 0;
        chart.Options.XAxis.WithLabelFormatter(x => { calls++; return "Position " + x; });
        var detached = chart.Prepare(context); var callbackCalls = calls;
        Assert.True(callbackCalls > 0);
        var svg = detached.ToSvg(); var png = detached.ToPng();
        var artifact = detached.ToArtifact("opening", VisualArtifactKind.Chart);
        var artifactSvg = artifact.ToSvg(); var artifactPng = artifact.ToPng(); var interchange = artifact.ToInterchangeJson();
        chart.Series.Clear(); chart.ConfigureLabels(labels => labels.OpeningBalance = "Changed");
        Assert.Equal(svg, detached.ToSvg()); Assert.Equal(png, detached.ToPng()); Assert.Equal(callbackCalls, calls);
        Assert.Equal(artifactSvg, artifact.ToSvg()); Assert.Equal(artifactPng, artifact.ToPng()); Assert.Equal(interchange, artifact.ToInterchangeJson());
    }
}
