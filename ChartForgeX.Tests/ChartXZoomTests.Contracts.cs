using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ChartXZoomTests {
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    public void BankRetainsAuthoredIdentityAndHostOptionsWithoutOffWindowNavigation(bool step, int level) {
        var points = new[] {
            new ChartPoint(0, 2).WithMarkOptions(new ChartMarkOptions(href: "#outside", selected: true)),
            new ChartPoint(5, 5).WithMarkOptions(new ChartMarkOptions(href: "#inside", tooltip: "Reading detail",
                accessibleLabel: "Current reading", cssClass: "host-point", selected: true, current: true,
                dataAttributes: new Dictionary<string, string> { ["data-record"] = "middle" })),
            new ChartPoint(10, 8)
        };
        var series = new ChartSeries(new ChartSeriesId("observed"), "Observed", step ? ChartSeriesKind.StepLine : ChartSeriesKind.Line, points)
            .WithObservationIds(new[] { new ChartObservationId("zero"), new ChartObservationId("five"), new ChartObservationId("ten") })
            .WithMarkOptions(new ChartMarkOptions(href: "#series", cssClass: "series-hooks"));
        if (step) series.WithInterpolation(ChartInterpolation.Step);
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).AddSeries(series);
        chart.Options.XAxis.Reversed = step;
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2));
        chart.ClearSeries();
        var view = bank.At(level, .5);
        var groups = view.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(new[] { "zero", "five", "ten" }, groups.Select(group => group.Metadata["data-cfx-observation-id"]));
        Assert.All(groups, group => Assert.Equal("observed", group.Metadata["data-cfx-series-key"]));
        var middle = groups[1];
        Assert.Equal("#inside", middle.Href); Assert.Equal("Reading detail", middle.Tooltip);
        Assert.Equal("Current reading", middle.Metadata["aria-label"]);
        Assert.Equal("host-point", middle.Metadata["class"]); Assert.Equal("middle", middle.Metadata["data-record"]);
        Assert.Equal("true", middle.Metadata["data-cfx-selected"]); Assert.Equal("true", middle.Metadata["data-cfx-current"]);
        Assert.Equal("Current reading", view.Regions.Single(region => region.Id == middle.Id).Label);
        Assert.Contains(view.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "series" && group.Metadata["class"] == "series-hooks");
        Assert.Equal(level == 0 ? 2 : 1, view.Scene.Nodes.Count(node => node.Role == "mark-selected-outline"));
        if (level == 1) {
            Assert.Null(groups[0].Href); Assert.Null(groups[2].Href);
            Assert.Equal("false", groups[0].Metadata["data-cfx-visible"]);
            Assert.Equal("false", groups[2].Metadata["data-cfx-visible"]);
        }
    }

    [Theory]
    [InlineData("x")]
    [InlineData("y")]
    [InlineData("secondary-y")]
    public void BankRejectsInteriorRuleCrossingsRatherThanFreezingTheirMotion(string slot) {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Primary", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) })
            .AddLine("Secondary", new[] { new ChartPoint(0, 4), new ChartPoint(10, 6) });
        chart.Series[1].UseSecondaryValueAxis();
        var axis = slot == "x" ? chart.Options.XAxis : slot == "y" ? chart.Options.YAxis : chart.Options.SecondaryYAxis;
        axis.RuleCrossAt = 5;
        Assert.Throws<NotSupportedException>(() => chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void MagnifiedTrackRetainsIntegerTickPositions(double factor) {
        var chart = Bare().WithXAxisBounds(0, 1).WithXAxisIntegerTicks()
            .AddLine("Counts", new[] { new ChartPoint(0, 2), new ChartPoint(1, 8) });
        var bank = chart.PrepareXZoom(Context(), factor == 1 ? new ChartXZoomLevels(1) : new ChartXZoomLevels(1, factor));
        Assert.All(bank.Bank[^1].Ticks, tick => Assert.Equal(Math.Truncate(tick), tick));
        Assert.Equal(new[] { 0d, 1d }, bank.Bank[^1].Ticks);
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(.5, "Authored half"));
        Assert.Equal(new[] { .5 }, chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2)).Bank[1].Ticks);
    }
    [Theory]
    [InlineData("linear", true)]
    [InlineData("radial", true)]
    [InlineData("image", true)]
    [InlineData("pattern", true)]
    [InlineData("linear", false)]
    [InlineData("radial", false)]
    [InlineData("image", false)]
    [InlineData("pattern", false)]
    public void AuthoredFilledMarkersContributeOnlyVisibleInkToMovingClearance(string kind, bool visible) {
        var color = Blue.WithAlpha(visible ? (byte)255 : (byte)0);
        var image = new ChartForgeX.Raster.RgbaImage(1, 1, new byte[] { color.R, color.G, color.B, color.A });
        var stops = new[] { new ChartGradientStop(0, color), new ChartGradientStop(1, color) };
        var fill = kind switch {
            "linear" => ChartFill.LinearGradient(new ChartPoint(0, 0), new ChartPoint(1, 0), stops),
            "radial" => ChartFill.RadialGradient(new ChartPoint(.5, .5), .5, stops),
            "image" => ChartFill.Image(image),
            _ => ChartFill.Pattern(image, 2, 2)
        };
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Observed", new[] { new ChartPoint(5, 5) }, Blue).AddHorizontalLine(5, "Fixed reference");
        chart.Series[0].Color = ChartColor.Transparent;
        chart.Series[0].WithFill(fill).ConfigureMarkers(marker => { marker.Enabled = true; marker.Radius = 24; marker.StrokeWidth = 0; });
        var bank = chart.PrepareXZoom(Context(), new ChartXZoomLevels(1, 2));
        var level = bank.Bank[1];
        var caption = level.Layers.Select(layer => layer.Caption).Single(item => item?.Id == "annotation-0-label")!;
        var intervals = caption.Hidden.Where(item => item.Reason.StartsWith("moving-mark-clearance:", StringComparison.Ordinal)).ToArray();
        if (!visible) { Assert.Empty(intervals); return; }
        Assert.NotEmpty(intervals);
        var interval = intervals[0]; var pan = -(interval.Minimum + interval.Maximum) / 2 / level.Travel;
        var blocked = bank.At(1, pan);
        Assert.DoesNotContain(blocked.Scene.Nodes, node => node.Role == "annotation-label");
        Assert.Contains(blocked.Diagnostics, diagnostic => diagnostic.Message.Contains("moving-mark-clearance:", StringComparison.Ordinal));
        Assert.Single(blocked.Regions, region => region.Role == "annotation");
    }

}
