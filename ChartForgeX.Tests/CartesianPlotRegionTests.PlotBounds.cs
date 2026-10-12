using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class CartesianPlotRegionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlotRegionMatchesFinalAxesAfterSecondaryAxisOrHorizontalTotalLayout(bool horizontal) {
        var chart = Chart.Create().WithSize(520, 340).WithTitle("Mapping extent");
        var values = new[] { new ChartPoint(1, 3), new ChartPoint(2, 7) };
        if (horizontal) chart.WithStackedBars().WithStackTotals().WithXAxisBounds(0, 10)
            .AddHorizontalBar("First", values).AddHorizontalBar("Second", values.AsEnumerable().Reverse());
        else {
            chart.AddLine("Primary", values).AddLine("Secondary", new[] { new ChartPoint(1, 30), new ChartPoint(2, 70) });
            chart.Series[1].UseSecondaryYAxis();
            chart.WithSecondaryYAxisBounds(0, 100);
        }
        var prepared = Prepare(chart);
        var region = Assert.Single(prepared.Regions, item => item.Role == "cartesian-plot");
        Assert.Equal("cartesian-plot", region.Id);
        var plot = region.Bounds;
        var axes = prepared.Scene.Nodes.OfType<VisualSceneLine>().ToArray();
        var x = Assert.Single(axes, item => item.Role == "axis-x");
        var y = Assert.Single(axes, item => item.Role == "axis-y");
        Assert.Equal(plot.Left, x.Start.X); Assert.Equal(plot.Right, x.End.X);
        Assert.Equal(plot.Bottom, x.Start.Y); Assert.Equal(plot.Bottom, x.End.Y);
        Assert.Equal(plot.Left, y.Start.X); Assert.Equal(plot.Top, y.Start.Y); Assert.Equal(plot.Bottom, y.End.Y);
        Assert.True(plot.Width > 0 && plot.Height > 0);
        if (horizontal) {
            chart.Options.ShowStackTotals = false;
            Assert.True(Assert.Single(Prepare(chart).Regions, item => item.Role == "cartesian-plot").Bounds.Right > plot.Right);
        } else Assert.Equal(plot.Right, Assert.Single(axes, item => item.Role == "axis-secondary-y").Start.X);

        chart.Options.ShowStackTotals = horizontal;
        chart.Options.ClipMarksToPlot = false;
        Assert.Equal(plot, Assert.Single(Prepare(chart).Regions, item => item.Role == "cartesian-plot").Bounds);
        chart.WithSize(800, 500).WithTitle("Changed after preparation");
        Assert.Equal(plot, region.Bounds);
        Assert.NotEqual(plot, Assert.Single(Prepare(chart).Regions, item => item.Role == "cartesian-plot").Bounds);
    }

    [Fact]
    public void PlotMetadataDoesNotChangeStaticPaintOrItsIdentityAndTranslatesWithTheScene() {
        var prepared = Prepare(Chart.Create().WithSize(360, 240).WithTitle("Metadata is not paint")
            .AddLine("Reading", new[] { new ChartPoint(1, 3), new ChartPoint(2, 5) }));
        var plot = Assert.Single(prepared.Regions, item => item.Role == "cartesian-plot");
        var without = new VisualScene(prepared.Size, prepared.Scene.Nodes, prepared.Diagnostics,
            prepared.Regions.Where(item => item.Role != "cartesian-plot").ToArray());
        var withMetadata = new PreparedVisual(prepared.Scene, prepared.Accessibility);
        var withoutMetadata = new PreparedVisual(without, prepared.Accessibility);
        Assert.Equal(withoutMetadata.ToSvg(), withMetadata.ToSvg());
        Assert.Equal(withoutMetadata.ToPng(), withMetadata.ToPng());

        var builder = new VisualSceneBuilder(new VisualSize(600, 400), new VisualRenderContext().Font);
        builder.Append(prepared.Scene, 20, 30, "panel-0");
        var translated = Assert.Single(builder.Build().Regions, item => item.Role == "cartesian-plot");
        Assert.Equal("panel-0/cartesian-plot", translated.Id);
        Assert.Equal(new ChartRect(plot.Bounds.Left + 20, plot.Bounds.Top + 30, plot.Bounds.Width, plot.Bounds.Height), translated.Bounds);
    }

    [Fact]
    public void EmptyNonCartesianAndExhaustedLayoutsHaveNoCartesianPlotContract() {
        var empty = Prepare(Chart.Create().AddLine("Empty", Array.Empty<ChartPoint>()));
        var radial = Prepare(Chart.Create().AddDonut("Share", new[] { new ChartPoint(1, 3), new ChartPoint(2, 5) }));
        // A frame may exhaust the content rectangle before the Cartesian owner measures its axes.
        var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        VisualCartesianCompiler.BuildInViewport(Chart.Create().AddLine("Reading", new[] { new ChartPoint(1, 3) }),
            context, builder, new ChartRect(0, 0, 0, 0));
        var tiny = new PreparedVisual(builder.Build());
        Assert.Contains(empty.Diagnostics, item => item.Code == "cartesian.no-data");
        Assert.Contains(tiny.Diagnostics, item => item.Code == "cartesian.insufficient-space");
        Assert.All(new[] { empty, radial, tiny }, visual => Assert.DoesNotContain(visual.Regions, item => item.Role == "cartesian-plot"));
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
