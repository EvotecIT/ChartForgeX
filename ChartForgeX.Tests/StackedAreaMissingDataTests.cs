using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Missing-X alignment changes contours without inventing observations, source values or totals.</summary>
public sealed class StackedAreaMissingDataTests {
    [Theory]
    [InlineData(ChartStackedAreaMissingDataPolicy.Independent)]
    [InlineData(ChartStackedAreaMissingDataPolicy.Gap)]
    [InlineData(ChartStackedAreaMissingDataPolicy.Zero)]
    public void GeometryUsesDeclaredPolicyWhileRawIdentityAndNormalizedTotalsStayAuthored(ChartStackedAreaMissingDataPolicy policy) {
        var chart = Model(policy);
        foreach (var series in chart.Series) series.WithNormalization(100);
        chart.Series[0].WithObservationId(1, new ChartObservationId("observed-zero"));
        var layout = ChartStackLayout.Create(chart, ChartBarCoordinateMap.Create(chart));
        var aligned = layout.AreaVertices(0);
        if (policy == ChartStackedAreaMissingDataPolicy.Independent) Assert.Null(aligned);
        else {
            Assert.NotNull(aligned);
            Assert.Equal(policy == ChartStackedAreaMissingDataPolicy.Zero ? 4 : 3, aligned!.Count);
            if (policy == ChartStackedAreaMissingDataPolicy.Zero) {
                Assert.Null(aligned[1].SourcePoint); Assert.Equal(aligned[1].Base, aligned[1].End);
                Assert.Equal(1, aligned[1].X);
            } else Assert.True(aligned[1].BreakBefore);
        }
        Assert.Equal(4, layout.Totals.Count);
        Assert.Equal(3, layout.Totals.Single(total => total.Coordinate == 1).SourceValue);
        Assert.Equal(100, layout.Totals.Single(total => total.Coordinate == 1).Value);
        var prepared = Prepare(chart);
        var document = XDocument.Parse(prepared.ToSvg());
        var points = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(7, points.Length);
        Assert.DoesNotContain(points, point => (string?)point.Attribute("data-cfx-series") == "0" && (string?)point.Attribute("data-cfx-x") == "1");
        var zero = points.Single(point => (string?)point.Attribute("data-cfx-observation-id") == "observed-zero");
        Assert.Equal("0", (string?)zero.Attribute("data-cfx-y"));
        Assert.Equal("1", (string?)zero.Attribute("data-cfx-source-point"));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[0].Points.Clear(); chart.Series[1].WithStackedAreaMissingData(ChartStackedAreaMissingDataPolicy.Independent);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GapCutsTheWholeStackAndZeroRendersAtMissingXInNativeAndSvg(bool reversed) {
        var gap = Model(ChartStackedAreaMissingDataPolicy.Gap); var zero = Model(ChartStackedAreaMissingDataPolicy.Zero);
        foreach (var chart in new[] { gap, zero }) {
            chart.Options.XAxis.WithBounds(0, 3).WithReversal(reversed); chart.Options.YAxis.WithBounds(0, 8).WithReversal(reversed);
        }
        var empty = Prepare(gap); var filled = Prepare(zero);
        var lines = filled.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "line").ToArray();
        var top = lines[1].Commands[1];
        var area = filled.Scene.Nodes.OfType<VisualScenePath>().First(path => path.Role == "area");
        var baseline = reversed ? area.Commands.Min(command => command.Y) : area.Commands.Max(command => command.Y);
        var image = filled.ToRgba(new VisualRenderOptions(supersampling: 1));
        var emptyImage = empty.ToRgba(new VisualRenderOptions(supersampling: 1));
        var offset = ((int)Math.Round((top.Y + baseline) / 2) * image.Width + (int)Math.Round(top.X)) * 4;
        Assert.Equal(0, emptyImage.Pixels[offset + 3]); Assert.True(image.Pixels[offset + 3] > 0);
        Assert.All(empty.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "line"),
            path => Assert.Equal(2, path.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo)));
        Assert.Equal(7, XDocument.Parse(filled.ToSvg()).Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "point"));
    }

    [Fact]
    public void GroupPoliciesRejectAmbiguityButDifferentAxesAndNamedStacksRemainIndependent() {
        var chart = Model(ChartStackedAreaMissingDataPolicy.Zero);
        chart.Series[1].StackedAreaMissingData = ChartStackedAreaMissingDataPolicy.Gap;
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        chart.Series[1].UseSecondaryValueAxis(); Prepare(chart);
        chart.Series[1].UsePrimaryValueAxis().WithStackGroup("Other"); Prepare(chart);
        chart.Series[0].Points.Add(new ChartPoint(3, 4));
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Series[0].StackedAreaMissingData = (ChartStackedAreaMissingDataPolicy)99);
        Assert.Throws<InvalidOperationException>(() => ChartSeries.CreateLine(new ChartSeriesId("line"), "Line", Array.Empty<ChartPoint>())
            .WithStackedAreaMissingData(ChartStackedAreaMissingDataPolicy.Zero));
    }

    [Fact]
    public void AlignedBreaksCutEveryMemberWhileSignedStacksAndAnEmptyMemberKeepTheirMeaning() {
        var chart = Chart.Create()
            .AddSeries(new ChartSeries(new ChartSeriesId("positive"), "Positive", ChartSeriesKind.StackedArea,
                new[] { new ChartPoint(0, 2), new ChartPoint(2, 4, true) }))
            .AddSeries(new ChartSeries(new ChartSeriesId("negative"), "Negative", ChartSeriesKind.StackedArea,
                new[] { new ChartPoint(0, -3), new ChartPoint(1, -5), new ChartPoint(2, -6) }))
            .AddSeries(new ChartSeries(new ChartSeriesId("absent"), "Absent", ChartSeriesKind.StackedArea, Array.Empty<ChartPoint>()));
        foreach (var series in chart.Series) series.WithStackedAreaMissingData(ChartStackedAreaMissingDataPolicy.Zero).WithNormalization(100);
        var layout = ChartStackLayout.Create(chart, ChartBarCoordinateMap.Create(chart));
        var negative = layout.AreaVertices(1)!; var absent = layout.AreaVertices(2)!;
        Assert.All(negative, vertex => { Assert.Equal(0, vertex.Base); Assert.Equal(-100, vertex.End); });
        Assert.Equal(new[] { 100d, 0, 100 }, absent.Select(vertex => vertex.End));
        Assert.All(absent, vertex => { Assert.Null(vertex.SourcePoint); Assert.Equal(vertex.Base, vertex.End); });
        for (var series = 0; series < 3; series++) Assert.True(layout.AreaVertices(series)![2].BreakBefore);
        var prepared = Prepare(chart);
        Assert.Equal(5, prepared.Regions.Count(region => region.Role == "point"));
        Assert.NotEmpty(prepared.ToPng());
    }

    private static Chart Model(ChartStackedAreaMissingDataPolicy policy) {
        var chart = Chart.Create().WithSize(600, 400).WithHeader(false).WithAxes(false).WithGrid(false).WithLegend(false).WithDataLabels(false)
            .WithLineVisualStyle(ChartLineVisualStyle.Plain());
        chart.AddSeries(new ChartSeries(new ChartSeriesId("lower"), "Lower", ChartSeriesKind.StackedArea,
            new[] { new ChartPoint(0, 2), new ChartPoint(2, 0), new ChartPoint(3, 2) }).WithStackedAreaMissingData(policy));
        chart.AddSeries(new ChartSeries(new ChartSeriesId("upper"), "Upper", ChartSeriesKind.StackedArea,
            new[] { new ChartPoint(0, 3), new ChartPoint(1, 3), new ChartPoint(2, 3), new ChartPoint(3, 3) }).WithStackedAreaMissingData(policy));
        foreach (var series in chart.Series) series.ConfigureMarkers(markers => markers.Enabled = false).WithStrokeWidth(1);
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(600, 400)),
        frame: new VisualFrame(showLegend: false, showSurface: false, transparentBackground: true)));
}
