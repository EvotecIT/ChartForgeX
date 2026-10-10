using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NumericRadialOwnerClosureTests {
    [Theory]
    [InlineData(true, "zero")]
    [InlineData(false, "zero")]
    [InlineData(true, "precision-collapse")]
    [InlineData(false, "precision-collapse")]
    [InlineData(true, "clipped")]
    [InlineData(false, "clipped")]
    [InlineData(true, "clipped-zero")]
    [InlineData(false, "clipped-zero")]
    public void UnpaintedObservationsDistinguishRetainedFactsFromClippedValues(bool bars, string status) {
        var (chart, series) = RetainedFact(bars, status);
        var svg = XDocument.Parse(chart.ToSvg());
        var point = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point"
            && (string?)node.Attribute("data-cfx-series") == series.ToString(CultureInfo.InvariantCulture)
            && (string?)node.Attribute("data-cfx-point") == "0");
        Assert.DoesNotContain(point.Descendants(), node => node.Name.LocalName == "path");
        var clipped = status.StartsWith("clipped", StringComparison.Ordinal);
        Assert.Equal(clipped ? null : status, (string?)point.Attribute("data-cfx-geometry-status"));
        Assert.Equal(clipped ? "true" : "false", (string?)point.Attribute("data-cfx-clipped"));
        Assert.Equal(status is "zero" or "clipped-zero" ? "0" : clipped ? "50" : "1", (string?)point.Attribute("data-cfx-y"));
        Assert.True(chart.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TransparentPaintDoesNotDeclareNumericGeometryCollapseAndLogarithmicZerosRemainInvalid(bool bars) {
        var chart = NumericRadialSeriesTests.Add(Chart.Create().WithSize(600, 440), bars, "Observed", new ChartPoint(1, 50));
        chart.Series[0].WithPointColor(0, ChartColor.FromHex("#2468AC").WithAlpha(0));
        var point = XDocument.Parse(chart.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point");
        Assert.Contains(point.Descendants(), node => (string?)node.Attribute("data-cfx-role") == (bars ? "radial-bar" : "radial-column"));
        Assert.Null(point.Attribute("data-cfx-geometry-status"));
        chart.Series[0].Points[0] = new ChartPoint(1, 0);
        chart.Options.YAxis.WithScale(ChartScaleKind.Logarithmic);
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void EmptySeriesRetainLegendAndSlotWithoutAnAutomaticAxis(bool bars, bool emptyPrimary) {
        var first = EmptyAxisChart(bars, emptyPrimary, false);
        var second = EmptyAxisChart(bars, emptyPrimary, true);
        var scene = first.Prepare(VisualExportRequest.ForChart(first).Context).Scene;
        var longScene = second.Prepare(VisualExportRequest.ForChart(second).Context).Scene;
        var emptySide = emptyPrimary ? "primary" : "secondary";
        var axis = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "radial-value-axis");
        Assert.NotEqual(emptySide, axis.Metadata["data-cfx-axis"]);
        Assert.Contains(scene.Regions, region => region.Role == "legend" && region.Label == "Pending");
        Assert.Single(NumericRadialSeriesTests.Marks(scene));
        var mark = NumericRadialSeriesTests.Marks(scene)[0]; var longMark = NumericRadialSeriesTests.Marks(longScene)[0];
        Assert.Equal(mark.Inner, longMark.Inner); Assert.Equal(mark.Outer, longMark.Outer);
        Assert.Equal(scene.Nodes.Count(node => node.Role == "radial-value-grid"), longScene.Nodes.Count(node => node.Role == "radial-value-grid"));
        if (emptyPrimary) Assert.DoesNotContain(scene.Nodes, node => node.Role == "radial-value-grid");
        Assert.Single(scene.Nodes, node => node.Role == "radial-value-rule");
        var grouped = NumericRadialSeriesTests.Marks(NumericRadialSeriesTests.Compile(first))[0];
        first.Series.RemoveAt(1);
        var single = NumericRadialSeriesTests.Marks(NumericRadialSeriesTests.Compile(first))[0];
        Assert.Equal(2, bars ? (single.Outer - single.Inner) / (grouped.Outer - grouped.Inner) : single.Sweep / grouped.Sweep, 10);
        var partial = EmptyAxisChart(bars, emptyPrimary, false);
        (emptyPrimary ? partial.Options.YAxis : partial.Options.SecondaryYAxis).Minimum = 10;
        Assert.Single(partial.Prepare(VisualExportRequest.ForChart(partial).Context).Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "radial-value-axis");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExplicitEmptyAxisBoundsRemainVisibleAndAllEmptyChartsKeepNoData(bool bars) {
        var chart = EmptyAxisChart(bars, false, false);
        chart.Options.SecondaryYAxis.WithBounds(10, 100);
        var scene = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene;
        var axis = scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Role == "radial-value-axis" && group.Metadata["data-cfx-axis"] == "secondary");
        Assert.Equal("10", axis.Metadata["data-cfx-min"]); Assert.Equal("100", axis.Metadata["data-cfx-max"]);
        Assert.Contains(scene.Regions, region => region.Role == "radial-value-label" && region.Label!.StartsWith("empty-", StringComparison.Ordinal));
        chart.Series[0].Points.Clear();
        var empty = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene;
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code == "numeric-radial.no-data");
        Assert.DoesNotContain(empty.Nodes, node => node.Role is "radial-value-label" or "radial-value-rule" or "radial-value-grid");
    }

    [Theory]
    [InlineData(true, ChartScaleKind.Linear)]
    [InlineData(false, ChartScaleKind.Linear)]
    [InlineData(true, ChartScaleKind.SymmetricLogarithmic)]
    [InlineData(false, ChartScaleKind.SymmetricLogarithmic)]
    public void AutomaticSignedDomainsIncludeTheirFirstTickWhileExplicitBoundsAndLabelsStayAuthoritative(bool bars, ChartScaleKind kind) {
        var chart = NumericRadialSeriesTests.Add(Chart.Create().WithSize(800, 520).WithLegend(false).WithDataLabels(false)
            .WithRadialGeometry(new(-75, 195)), bars, "Observed", new(1, -1), new(2, 100));
        chart.Options.YAxis.WithScale(kind);
        var scale = RadialValueScale.Create(chart.Options.YAxis, new[] { -1d, 0d, 100d }, "Numeric radial", true);
        Assert.True(scale.Minimum < -1); Assert.Equal(scale.Ticks[0], scale.Minimum); Assert.Contains(scale.Ticks, tick => tick < 0);
        var scene = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene;
        Assert.Contains(scene.Regions, region => region.Role == "radial-value-label" && region.Label!.StartsWith("-", StringComparison.Ordinal));
        chart.Options.YAxis.WithBounds(-1, 100);
        scale = RadialValueScale.Create(chart.Options.YAxis, new[] { -1d, 0d, 100d }, "Numeric radial", true);
        Assert.Equal(-1, scale.Minimum); Assert.Equal(100, scale.Maximum); Assert.All(scale.Ticks, tick => Assert.InRange(tick, -1, 100));
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(-20, "outside"));
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(0, "baseline"));
        scale = RadialValueScale.Create(chart.Options.YAxis, new[] { -1d, 0d, 100d }, "Numeric radial", true);
        Assert.Equal(-1, scale.Minimum); Assert.DoesNotContain(-20, scale.Ticks); Assert.Contains(0, scale.Ticks);
    }

    internal static (Chart Chart, int Series) RetainedFact(bool bars, string status) {
        var chart = Chart.Create().WithSize(600, 440).WithHeader(false).WithDataLabels(false).WithAxes(false).WithGrid(false).WithLegend();
        if (status == "precision-collapse") {
            NumericRadialSeriesTests.Add(chart, bars, "Large", new ChartPoint(1, 1e20));
            NumericRadialSeriesTests.Add(chart, bars, "Retained", new ChartPoint(1, 1));
            foreach (var series in chart.Series) series.WithStackGroup("same");
            return (chart, 1);
        }
        NumericRadialSeriesTests.Add(chart, bars, "Retained", new(1, status is "zero" or "clipped-zero" ? 0 : 50), new(2, 100));
        if (status.StartsWith("clipped", StringComparison.Ordinal)) chart.WithYAxisBounds(100, 200);
        return (chart, 0);
    }

    private static Chart EmptyAxisChart(bool bars, bool emptyPrimary, bool longText) {
        var chart = NumericRadialSeriesTests.Add(Chart.Create().WithSize(600, 440).WithDataLabels(false).WithLegend()
            .WithRadialGeometry(new(-75, 195)), bars, "Observed", new ChartPoint(1, 100));
        NumericRadialSeriesTests.Add(chart, bars, "Pending");
        chart.Series[emptyPrimary ? 0 : 1].UseSecondaryYAxis();
        var axis = emptyPrimary ? chart.Options.YAxis : chart.Options.SecondaryYAxis;
        axis.WithLabelFormatter(_ => longText ? "empty-caption-that-must-not-reserve-a-radius-lane" : "empty-x");
        return chart;
    }
}
