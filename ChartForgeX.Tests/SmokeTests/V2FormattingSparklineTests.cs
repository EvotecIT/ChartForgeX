using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Shared display and sample policies preserve culture, zero observations, gaps and compact-output geometry.</summary>
public sealed class V2FormattingSparklineTests {
    [Fact]
    public void NumericFormatsDetachCultureAndApplyAcrossChartAndFacts() {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("de-DE").Clone();
        var format = ChartValueFormat.Number("N2", culture);
        culture.NumberFormat.NumberDecimalSeparator = "!";
        Assert.Equal("1.234,50", format.Format(1234.5));
        Assert.True(format.Culture.IsReadOnly);
        Assert.Equal("1,2k", ChartValueFormat.Compact(CultureInfo.GetCultureInfo("de-DE")).Format(1234.5));
        var chart = Chart.Create().WithValueFormat(format).WithDataLabels()
            .AddBar("Value", new[] { new ChartPoint(1, 1234.5) });
        Assert.Contains("1.234,50", chart.ToSvg());
        Assert.Contains("1.234,50", chart.Prepare(new VisualRenderContext()).ToSvg());
        Assert.Equal("1.234,50", MetricCard.Create().WithMetric("Value", 1234.5, format).Value);
        Assert.Equal("1.234,50", ChartTableCell.FromValue(1234.5, format).Text);
        Assert.Equal("1,234.5", ChartNumericFormatter.FormatValue(Chart.Create().Options, 1234.5));
        Assert.Equal("1.2k", ChartValueFormat.Compact().Format(1234.5));
    }

    [Fact]
    public void AxisExplicitLabelsAndConvenienceDelegatesUseOneDisplayOwner() {
        var axis = new ChartAxis().WithValueFormat(ChartValueFormat.Number("0.00"));
        Assert.Equal("2.00", ChartAxisValueFormatter.Format(axis, 2));
        axis.Labels.Add(new ChartAxisLabel(2, "Explicit"));
        Assert.Equal("Explicit", ChartAxisValueFormatter.Format(axis, 2));
        axis.LabelFormatter = value => value + " custom";
        Assert.Equal("3 custom", axis.ValueFormat!.Format(3));
        axis.LabelFormatter = null;
        Assert.Null(axis.ValueFormat);
        var chart = Chart.Create().WithValueFormat(ChartValueFormat.Number("0.0"));
        Assert.Equal("2.0", chart.Options.ValueFormatter!(2));
        chart.WithValueFormatter(null);
        Assert.Equal("1,234", chart.Options.ValueFormat.Format(1234));
        Assert.Equal(string.Empty, ChartValueFormat.Custom(_ => null!).Format(2));
    }

    [Fact]
    public void NullableObservationsKeepZeroAndOriginalHorizontalSlots() {
        var slots = new double?[] { null, 0, null, 4, null };
        var gap = ChartPoints.FromValues(slots);
        Assert.Equal(new[] { 2d, 4d }, gap.Select(point => point.X));
        Assert.Equal(new[] { 0d, 4d }, gap.Select(point => point.Y));
        Assert.False(gap[0].BreakBefore); Assert.True(gap[1].BreakBefore);
        Assert.All(ChartPoints.FromValues(slots, ChartMissingDataPolicy.Connect), point => Assert.False(point.BreakBefore));
        var xy = ChartPoints.FromXY(new[] { 10d, 20d, 40d }, new double?[] { 0, null, 3 });
        Assert.Equal(new[] { 10d, 40d }, xy.Select(point => point.X));
        Assert.True(xy[1].BreakBefore);
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartPoints.FromValues(new double?[] { 0, double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartPoints.FromXY(new[] { double.PositiveInfinity }, new double?[] { null }));
        Assert.Throws<ArgumentException>(() => ChartPoints.FromXY(new[] { 1d }, new double?[] { 1, null }));
    }

    [Fact]
    public void SharedProjectionSnapshotsSamplesAndKeepsMissingEndpoints() {
        var slots = new double?[] { null, 0, null, 4, null };
        var data = new SparklineData(slots, 0, 4);
        slots[1] = 99;
        var points = SparklineLayout.Project(data, new ChartRect(0, 0, 80, 40));
        Assert.Equal(0d, data.Values[1]);
        Assert.Equal(new[] { 20d, 60d }, points.Select(point => point.X));
        Assert.Equal(new[] { 40d, 0d }, points.Select(point => point.Y));
        Assert.True(points[1].BreakBefore);
        var anchored = new SparklineData(new double?[] { 2, 4 }, includeZero: true);
        Assert.Equal(0, new ChartTableCell("Trend").WithSparkline(anchored).GetSparklineData().Minimum);
        Assert.Equal(0, VisualBlockRendering.MiniSparklineBounds(MetricCard.Create().WithSparkline(anchored)).Minimum);
        var extreme = new SparklineData(new double?[] { -double.MaxValue, 0, double.MaxValue });
        Assert.Equal(0.5, extreme.Ratio(0), 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SparklineData(new double?[] { 1 }, minimum: double.MaxValue));
    }

    [Fact]
    public void PreparedSparklineExportsActualGapAndConnectPixelsWithoutSvgRoundtrip() {
        var slots = new double?[] { 2, 2, null, 2, 2 };
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(100, 100), 0),
            frame: new VisualFrame(showLegend: false, transparentBackground: true));
        var gap = new Sparkline(new SparklineData(slots, 0, 4)).Prepare(context);
        var connect = new Sparkline(new SparklineData(slots, 0, 4, ChartMissingDataPolicy.Connect)).Prepare(context);
        string Line(PreparedVisual visual) => Role(visual.ToSvg(), "line").Attribute("d")!.Value;
        Assert.Equal(2, Line(gap).Count(character => character == 'M'));
        Assert.Equal(1, Line(connect).Count(character => character == 'M'));
        var gapPixels = gap.ToRgba(); var connectPixels = connect.ToRgba();
        var alpha = (50 * 100 + 50) * 4 + 3;
        Assert.Equal(0, gapPixels.Pixels[alpha]);
        Assert.True(connectPixels.Pixels[alpha] > 0);
        var expected = gap.ToSvg(); slots[1] = 20;
        Assert.Equal(expected, gap.ToSvg());
    }

    [Fact]
    public void AllMissingSparklineReportsNoObservationsRatherThanInventingZeroes() {
        var data = new SparklineData(new double?[] { null, null, null });
        Assert.Empty(data.ToPoints());
        Assert.Equal(0, data.Minimum); Assert.Equal(1, data.Maximum);
        var prepared = new Sparkline(data).Prepare(new VisualRenderContext());
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.no-data");
        Assert.DoesNotContain(prepared.Regions, region => region.Role == "point");
    }

    [Fact]
    public void TableEmbeddingUsesTheSameGapSlotsInSvgAndRaster() {
        ChartTable Table(ChartMissingDataPolicy policy) => ChartTable.Create().WithSize(500, 220)
            .WithColumns("Trend").AddRow("Samples")
            .WithRow(0, row => row.Cells[0].WithSparkline(new SparklineData(new double?[] { 0, 2, null, 3, 4 }, 0, 4, policy)));
        var gap = Table(ChartMissingDataPolicy.Gap); var connect = Table(ChartMissingDataPolicy.Connect);
        Assert.Equal(2, Roles(gap.ToSvg(), "table-cell-sparkline").Count());
        Assert.Single(Roles(connect.ToSvg(), "table-cell-sparkline"));
        Assert.False(gap.ToPng().SequenceEqual(connect.ToPng()));
    }

    [Fact]
    public void MetricEmbeddingSharesDomainAndDoesNotMarkMissingFinalSampleAsCurrent() {
        var card = MetricCard.Create().WithSize(640, 260).WithMetric("Load", "0")
            .WithSparkline(new SparklineData(new double?[] { 0, 2, null }))
            .WithSecondarySparkline(new SparklineData(new double?[] { 10, null, 20 }))
            .WithMiniSparklineStyle(MetricCardSparklineStyle.Line);
        var svg = card.ToSvg(); var group = Role(svg, "metric-mini-sparkline");
        Assert.Equal("0", group.Attribute("data-cfx-min")!.Value);
        Assert.Equal("20", group.Attribute("data-cfx-max")!.Value);
        Assert.Empty(Roles(svg, "metric-mini-sparkline-current"));
        Assert.Equal(2, Role(svg, "metric-mini-sparkline-secondary").Attribute("d")!.Value.Count(character => character == 'M'));
        Assert.NotEmpty(card.ToPng());
        var area = MetricCard.Create().WithMetric("Load", "0").WithSparkline(new SparklineData(new double?[] { 0, 1, null, 3, 4 }));
        Assert.Equal(2, Roles(area.ToSvg(), "metric-mini-sparkline-fill").Count());
        var missing = MetricCard.Create().WithMetric("Load", "Missing").WithMiniSparklineStyle(MetricCardSparklineStyle.Line)
            .WithSparkline(new SparklineData(new double?[] { null, null, null }));
        var primaryOnly = missing.ToPng();
        missing.WithSecondarySparkline(new SparklineData(new double?[] { 10, 20, 30 }));
        Assert.NotEmpty(Role(missing.ToSvg(), "metric-mini-sparkline-secondary").Attribute("d")!.Value);
        Assert.False(primaryOnly.SequenceEqual(missing.ToPng()));
    }

    [Fact]
    public void CanvasEmbeddingRetainsSparseAreaSegmentsAndMissingBars() {
        var tile = new VisualCanvasInfoTileLayer(20, 20, 500, 150, "CPU", "Load", "0")
            .WithSparkline(new SparklineData(new double?[] { 0, 1, null, 3, 4 }, 0, 4), SparklineStyle.Area);
        var canvas = VisualCanvas.Create(560, 210).AddLayer(tile);
        var group = Role(canvas.ToSvg(), "visual-canvas-info-tile-mini-chart");
        var line = group.Elements().Last(element => element.Name.LocalName == "path");
        Assert.Equal(2, line.Attribute("d")!.Value.Count(character => character == 'M'));
        Assert.NotEmpty(canvas.ToPng());
        tile.WithSparkline(new SparklineData(new double?[] { 0, null, 4 }, 0, 4), SparklineStyle.Bars);
        group = Role(canvas.ToSvg(), "visual-canvas-info-tile-mini-chart");
        Assert.Equal(3, group.Elements().Count(element => element.Name.LocalName == "rect")); // Track and two actual observations.
    }

    private static XElement Role(string svg, string role) => Assert.Single(Roles(svg, role));
    private static IEnumerable<XElement> Roles(string svg, string role) =>
        XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role);
}
