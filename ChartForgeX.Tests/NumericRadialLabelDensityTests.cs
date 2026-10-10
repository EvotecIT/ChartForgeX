using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects deliberate caption density without weakening viewport, mark or source-data contracts.</summary>
public sealed class NumericRadialLabelDensityTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrowdedCaptionsUseTheRequestedDensityAndRetainTheirCompleteDescriptions(bool bars) {
        var chart = CrowdedChart(bars);
        var counts = new Dictionary<ChartLabelDensity, int>();
        foreach (var density in new[] { ChartLabelDensity.Dense, ChartLabelDensity.Auto, ChartLabelDensity.Relaxed, ChartLabelDensity.All }) {
            chart.Options.XAxis.LabelDensity = density;
            var scene = NumericRadialAxisPolicyTests.Prepare(chart).Scene;
            var captions = NumericRadialAxisTextTests.Footprints(scene).Where(item => item.Text.Role == "radial-category-label").ToArray();
            counts.Add(density, captions.Length);
            Assert.Equal(20, scene.Regions.Count(region => region.Role == "radial-category-label"));
            Assert.Contains(scene.Regions, region => region.Role == "radial-category-label" && region.Label == "Region 20 request volume");
            Assert.All(captions, caption => Assert.True(LabelPlacementService.Contains(new ChartRect(0, 0, 360, 360), caption.Bounds)));
            var marks = NumericRadialSeriesTests.Marks(scene).Select(mark => new LabelMarkShape(VisualSceneGeometry.Flatten(mark, 8), true, 0)).ToArray();
            Assert.All(captions, caption => Assert.All(marks, mark => Assert.False(mark.Intersects(caption.Bounds))));
            if (density != ChartLabelDensity.All) AssertNoOverlaps(captions.Select(item => item.Bounds).ToArray());
            else Assert.Contains(true, captions.SelectMany((first, index) => captions.Skip(index + 1).Select(second => Intersects(first.Bounds, second.Bounds))));
        }
        Assert.True(counts[ChartLabelDensity.Dense] >= counts[ChartLabelDensity.Auto]);
        Assert.True(counts[ChartLabelDensity.Auto] > counts[ChartLabelDensity.Relaxed]);
        Assert.True(counts[ChartLabelDensity.All] > counts[ChartLabelDensity.Dense]);
    }

    [Fact]
    public void AllPreservesFullTurnTickEligibilityOnIndependentAxes() {
        var chart = NumericRadialAxisPolicyTests.RelativeChart(true).WithRadialGeometry(new(-90, 269, .2, .25, .1));
        chart.Options.XAxis.Visible = false;
        var partial = NumericRadialAxisPolicyTests.Prepare(chart).Scene;
        chart.WithRadialGeometry(new(-90, 270, .2, .25, .1));
        var scene = NumericRadialAxisPolicyTests.Prepare(chart).Scene;
        foreach (var side in new[] { "Primary", "Secondary" }) {
            var maximumCaption = side == "Primary" ? "P2" : "S2";
            Assert.Contains(partial.Regions, region => region.Role == "radial-value-label" && region.Label == maximumCaption);
            Assert.Equal(partial.Regions.Count(region => region.Role == "radial-value-label" && region.Id!.Contains(side, StringComparison.Ordinal)) - 1,
                scene.Regions.Count(region => region.Role == "radial-value-label" && region.Id!.Contains(side, StringComparison.Ordinal)));
            Assert.DoesNotContain(scene.Regions, region => region.Role == "radial-value-label" && region.Label == maximumCaption);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AllAxisCaptionsRemainObstaclesWhenPointAndTotalLabelsAreEnabled(bool bars) {
        var chart = CrowdedChart(bars).WithSize(800, 440);
        chart.Options.XAxis.LabelDensity = ChartLabelDensity.All;
        var axesOnly = NumericRadialAxisPolicyTests.Prepare(chart).Scene;
        chart.WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Left);
        chart.Options.ShowStackTotals = true;
        chart.Series[0].WithStackGroup("requests");
        var scene = NumericRadialAxisPolicyTests.Prepare(chart).Scene;
        var captions = NumericRadialAxisTextTests.Footprints(scene);
        var axes = captions.Where(item => item.Text.Role == "radial-category-label").ToArray();
        Assert.Equal(NumericRadialAxisTextTests.Footprints(axesOnly).Where(item => item.Text.Role == "radial-category-label").Select(item => item.Bounds), axes.Select(item => item.Bounds));
        Assert.All(captions.Where(item => item.Text.Role is "radial-data-label" or "stack-total"), label =>
            Assert.All(axes, axis => Assert.False(Intersects(label.Bounds, axis.Bounds))));
        Assert.Equal(20, scene.Regions.Count(region => region.Role == "point"));
        Assert.Equal(20, scene.Regions.Count(region => region.Role == "radial-data-label"));
        Assert.Equal(20, scene.Regions.Count(region => region.Role == "stack-total"));
    }

    internal static Chart CrowdedChart(bool bars) {
        var chart = V2GalleryModels.Create(bars ? ChartSeriesKind.RadialBar : ChartSeriesKind.RadialColumn, "compact-all-labels")
            .WithSize(360, 360).WithHeader(false).WithLegend(false).WithFontFamily("Carlito");
        chart.Options.PngFontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        return chart;
    }

    private static void AssertNoOverlaps(ChartRect[] bounds) {
        for (var first = 0; first < bounds.Length; first++) for (var second = first + 1; second < bounds.Length; second++)
            Assert.False(Intersects(bounds[first], bounds[second]));
    }
    private static bool Intersects(ChartRect first, ChartRect second) => first.Left < second.Right - .001 && second.Left < first.Right - .001
        && first.Top < second.Bottom - .001 && second.Top < first.Bottom - .001;
}
