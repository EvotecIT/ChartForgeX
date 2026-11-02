using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Reachable marker families retain the actual asymmetric custom surface in source semantics and native exports.</summary>
public sealed class CustomMarkerFamilyBoundsTests {
    [Theory]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Dumbbell)]
    public void ConfiguredPlotRadiiRetainFittedLegendGlyphs(ChartSeriesKind kind) {
        var chart = Family(kind);
        chart.Series[0].ConfigureMarkers(options => { options.Radius = 32; options.Enabled = true; });
        if (kind == ChartSeriesKind.Dumbbell)
            chart.Series[0].ConfigureDumbbell(options => { options.StartMarker.Radius = 24; options.EndMarker.Radius = 40; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plotMarkers = prepared.Scene.Nodes.OfType<VisualSceneEllipse>()
            .Where(mark => mark.Role is "marker" or "dumbbell-start" or "dumbbell-end").ToArray();
        Assert.NotEmpty(plotMarkers);
        if (kind == ChartSeriesKind.Scatter) Assert.All(plotMarkers, mark => Assert.Equal(32, mark.Rx));
        else {
            Assert.Equal(24, Assert.Single(plotMarkers, mark => mark.Role == "dumbbell-start").Rx);
            Assert.Equal(40, Assert.Single(plotMarkers, mark => mark.Role == "dumbbell-end").Rx);
        }
        var legends = prepared.Scene.Nodes.OfType<VisualSceneEllipse>()
            .Where(mark => mark.Role is "legend-swatch" or "legend-dumbbell-start" or "legend-dumbbell-end").ToArray();
        Assert.Equal(kind == ChartSeriesKind.Dumbbell ? 2 : 1, legends.Length);
        foreach (var mark in legends) {
            var half = mark.Stroke.HasValue ? mark.StrokeWidth / 2 : 0;
            var painted = new ChartRect(mark.Cx - mark.Rx - half, mark.Cy - mark.Ry - half,
                2 * (mark.Rx + half), 2 * (mark.Ry + half));
            Assert.Contains(prepared.Regions, region => region.Role == "legend" && LabelPlacementService.Contains(region.Bounds, painted));
            Assert.True(mark.Rx < 24, "Legend fitting must retain the glyph budget instead of the plot's authored radius.");
        }
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line)] [InlineData(ChartSeriesKind.Scatter)] [InlineData(ChartSeriesKind.Bubble)]
    [InlineData(ChartSeriesKind.ErrorBar)] [InlineData(ChartSeriesKind.Dumbbell)] [InlineData(ChartSeriesKind.Lollipop)]
    [InlineData(ChartSeriesKind.Slope)] [InlineData(ChartSeriesKind.RangeBand)] [InlineData(ChartSeriesKind.RangeArea)]
    [InlineData(ChartSeriesKind.DifferenceArea)] [InlineData(ChartSeriesKind.Radar)] [InlineData(ChartSeriesKind.Polar)]
    public void AsymmetricMarkersStayWithinTheirOwnSourceFootprintsAndKeepNativeLegendGeometry(ChartSeriesKind kind) {
        var chart = Family(kind); var series = chart.Series[0];
        var shape = ChartMarkerPath.Start(.2, -.25).LineTo(.6, -.25).LineTo(.6, .5).LineTo(.2, .5);
        series.ConfigureMarkers(options => { options.CustomPath = shape; if (kind != ChartSeriesKind.Bubble) options.Radius = 16; options.StrokeWidth = 2; options.Enabled = true; });
        if (kind == ChartSeriesKind.Dumbbell) series.ConfigureDumbbell(options => { options.StartMarker.Radius = 8; options.EndMarker.Radius = 20; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        ScatterBubbleColorRenderingTests.Capture(prepared, "custom-marker-family-bounds-" + kind);
        var roles = new[] { "marker", "bubble", "error-marker", "dumbbell-start", "dumbbell-end", "lollipop-marker", "slope-marker", "range-marker", "difference-marker", "radar-point", "polar-point" };
        var marks = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => roles.Contains(node.Role)).ToArray(); Assert.NotEmpty(marks);
        var sources = prepared.Regions.Where(region => region.Role is "point" or "radar-point" or "polar-point").ToArray(); Assert.NotEmpty(sources);
        foreach (var mark in marks) {
            var bound = new ChartPath(mark.Commands).Bounds(); var half = mark.Stroke.HasValue ? mark.StrokeWidth / 2 : 0;
            var painted = new ChartRect(bound.X - half, bound.Y - half, bound.Width + half * 2, bound.Height + half * 2);
            Assert.Contains(sources, region => LabelPlacementService.Contains(region.Bounds, painted));
        }
        var legends = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role is "legend-swatch" or "legend-dumbbell-start" or "legend-dumbbell-end").ToArray();
        Assert.NotEmpty(legends); Assert.All(legends, legend => Assert.Equal(shape.Commands.Select(command => command.Kind), legend.Commands.Select(command => command.Kind)));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Dumbbell)]
    [InlineData(ChartSeriesKind.Slope)]
    [InlineData(ChartSeriesKind.RangeArea)]
    [InlineData(ChartSeriesKind.ErrorBar)]
    public void SparseCallbackSquaresFitAutomaticEndpointsAndTheirSourceRegions(ChartSeriesKind kind) {
        var chart = Family(kind).WithLegend(false);
        var series = chart.Series[0];
        series.ConfigureMarkers(options => options.Enabled = false);
        var style = new ChartItemStyle(markers: new ChartMarkerOptions {
            Enabled = true, Radius = 32, Shape = ChartMarkerShape.Square, StrokeWidth = 2
        });
        if (kind == ChartSeriesKind.Dumbbell) {
            series.ReplaceDumbbells(new[] { new ChartDumbbell(1, 10, 20), new ChartDumbbell(3, 25, 40) });
            series.ConfigureDumbbell(options => { options.StartMarker.Radius = 8; options.EndMarker.Radius = 12; });
            series.WithItemStyler<ChartDumbbell>(item => item.Index == 1 ? style : null);
        } else if (kind == ChartSeriesKind.ErrorBar) {
            series.ReplaceErrorBars(new[] {
                new ChartErrorBar(1, 10, yBounds: new ChartErrorBounds(10, 10)),
                new ChartErrorBar(3, 40, yBounds: new ChartErrorBounds(40, 40))
            });
            series.WithItemStyler<ChartErrorBar>(item => item.Index == 1 ? style : null);
        } else if (kind == ChartSeriesKind.RangeArea)
            series.WithItemStyler<ChartRangeBand>(item => item.Index == 1 ? style : null);
        else series.WithItemStyler<ChartPoint>(item => item.Index == 1 ? style : null);

        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = prepared.XProjection!.PlotBounds;
        var fittedPlot = new ChartRect(plot.X - 1e-6, plot.Y - 1e-6, plot.Width + 2e-6, plot.Height + 2e-6);
        var roles = new[] { "dumbbell-start", "dumbbell-end", "slope-marker", "range-marker", "error-marker" };
        var marks = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(mark => roles.Contains(mark.Role)).ToArray();
        Assert.Equal(kind is ChartSeriesKind.Dumbbell or ChartSeriesKind.RangeArea ? 2 : 1, marks.Length);
        foreach (var mark in marks) {
            Assert.Equal(4, mark.Commands.Count);
            var bounds = new ChartPath(mark.Commands).Bounds();
            Assert.Equal(64, bounds.Width, 6); Assert.Equal(64, bounds.Height, 6);
            var painted = new ChartRect(bounds.X - 1, bounds.Y - 1, bounds.Width + 2, bounds.Height + 2);
            Assert.True(LabelPlacementService.Contains(fittedPlot, painted), "The callback square must fit the automatic plot gutter.");
            Assert.Contains(prepared.Regions, region => region.Role == "point" && LabelPlacementService.Contains(region.Bounds, painted));
        }
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), mark => roles.Contains(mark.Role));
    }

    private static Chart Family(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(400, 360).WithHeader(false).WithLegend(true).WithAxes(false).WithGrid(false).WithDataLabels(false);
        var points = new[] { new ChartPoint(1, 20), new ChartPoint(3, 30), new ChartPoint(5, 40) };
        return kind switch {
            ChartSeriesKind.Line => chart.AddLine("Samples", points), ChartSeriesKind.Scatter => chart.AddScatter("Samples", points),
            ChartSeriesKind.Bubble => chart.AddBubble("Samples", new[] { new ChartBubble(1, 20, 2), new ChartBubble(3, 30, 10) }),
            ChartSeriesKind.ErrorBar => chart.AddErrorBar("Samples", new[] { new ChartErrorBar(1, 20, yBounds: new ChartErrorBounds(10, 30)) }),
            ChartSeriesKind.Dumbbell => chart.AddDumbbell("Samples", new[] { new ChartDumbbell(1, 10, 30) }),
            ChartSeriesKind.Lollipop => chart.AddLollipop("Samples", points), ChartSeriesKind.Slope => chart.AddSlope("Samples", 20, 40),
            ChartSeriesKind.RangeBand => chart.AddRangeBand("Samples", new[] { new ChartRangeBand(1, 10, 30), new ChartRangeBand(3, 20, 40) }),
            ChartSeriesKind.RangeArea => chart.AddRangeArea("Samples", new[] { new ChartRangeBand(1, 10, 30), new ChartRangeBand(3, 20, 40) }, smooth: false),
            ChartSeriesKind.DifferenceArea => chart.AddDifferenceArea("Samples", new[] { new ChartDifferencePoint(1, 10, 30), new ChartDifferencePoint(3, 20, 40) }),
            ChartSeriesKind.Radar => chart.AddRadar("Samples", points), ChartSeriesKind.Polar => chart.AddPolar("Samples", points),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
