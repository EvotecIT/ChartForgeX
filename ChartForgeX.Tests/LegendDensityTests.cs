using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Svg;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LegendDensityTests {
    [Theory]
    [InlineData("line", ChartLegendPosition.Bottom)]
    [InlineData("line", ChartLegendPosition.Top)]
    [InlineData("line", ChartLegendPosition.Right)]
    [InlineData("pie", ChartLegendPosition.Bottom)]
    [InlineData("pie", ChartLegendPosition.Left)]
    [InlineData("radial", ChartLegendPosition.Bottom)]
    [InlineData("radial", ChartLegendPosition.Right)]
    public void DenseLegendsDiscloseOverflowAndRetainAllData(string kind, ChartLegendPosition position) {
        var chart = Chart.Create().WithSize(900, 560).WithTitle("Dense legend").WithLegendPosition(position).WithLegendBudget(maximumRows: 3);
        var points = Enumerable.Range(0, 100).Select(i => new ChartPoint(i, i + 1)).ToArray();
        if (kind == "pie") chart.AddPie("Values", points);
        else if (kind == "radial") chart.AddRadialBar("Values", points);
        else foreach (var point in points) chart.AddLine("Service " + point.X, new[] { new ChartPoint(0, point.Y), new ChartPoint(1, point.Y + 1) });
        var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
        var summary = Assert.Single(svg.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "legend-overflow");
        var omitted = (int)summary.Attribute("data-cfx-omitted")!;
        var role = kind == "pie" ? "slice-legend-swatch" : kind == "radial" ? "radial-bar-legend-marker" : "legend-item";
        var visible = svg.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == role);
        Assert.Equal(100, visible + omitted);
        Assert.True(visible > 0 && omitted > 0);
        Assert.Contains(omitted.ToString(), summary.Value);
        Assert.InRange((double)summary.Attribute("y")!, 1, 559);
        Assert.Equal(kind == "line" ? 100 : 1, chart.Series.Count);
        Assert.Equal(kind == "line" ? 200 : 100, chart.Series.Sum(series => series.Points.Count));
        if (kind == "radial") {
            Assert.Equal(100, svg.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == "radial-bar-track"));
            Assert.Equal(100, svg.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == "radial-bar-ring"));
        }
        Assert.NotEmpty(new PngChartRenderer().Render(chart));
    }

    [Theory]
    [InlineData("line", 560, 1.0)]
    [InlineData("pie", 560, 1.0)]
    [InlineData("radial", 560, 1.0)]
    [InlineData("line", 1000, 1.0)]
    [InlineData("pie", 1000, 1.0)]
    [InlineData("radial", 1000, 1.0)]
    [InlineData("line", 240, 0.35)]
    [InlineData("pie", 240, 0.35)]
    [InlineData("radial", 240, 0.35)]
    public void SideLegendBudgetUsesDrawableHeightAndKeepsOverflowVisible(string kind, int height, double maximumHeightFraction) {
        var chart = Chart.Create()
            .WithSize(900, height)
            .WithTitle("Bounded side legend")
            .WithLegendPosition(ChartLegendPosition.Right)
            .WithLegendBudget(maximumHeightFraction);
        var points = Enumerable.Range(0, 100).Select(i => new ChartPoint(i, i + 1)).ToArray();
        if (kind == "pie") chart.AddPie("Values", points);
        else if (kind == "radial") chart.AddRadialBar("Values", points);
        else foreach (var point in points) chart.AddLine("Service " + point.X, new[] { new ChartPoint(0, point.Y), new ChartPoint(1, point.Y + 1) });

        var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
        var summary = Assert.Single(svg.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "legend-overflow");
        var omitted = (int)summary.Attribute("data-cfx-omitted")!;
        var role = kind == "pie" ? "slice-legend-swatch" : kind == "radial" ? "radial-bar-legend-marker" : "legend-item";
        var visible = svg.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == role);
        Assert.Equal(100, visible + omitted);
        Assert.True(omitted > 0);
        Assert.InRange((double)summary.Attribute("y")!, 1, height - 1);
        Assert.NotEmpty(new PngChartRenderer().Render(chart));
    }

    [Theory]
    [InlineData(ChartLegendPosition.TopLeft, 40, 180)]
    [InlineData(ChartLegendPosition.Top, 300, 600)]
    [InlineData(ChartLegendPosition.TopRight, 680, 860)]
    public void HorizontalOverflowSummaryHonorsLegendAlignment(ChartLegendPosition position, double minimumX, double maximumX) {
        var chart = Chart.Create().WithSize(900, 560).WithLegendPosition(position).WithLegendBudget(maximumRows: 2);
        for (var index = 0; index < 40; index++) {
            chart.AddLine("Service " + index, new[] { new ChartPoint(0, index), new ChartPoint(1, index + 1) });
        }

        var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
        var summary = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "legend-overflow");

        Assert.InRange((double)summary.Attribute("x")!, minimumX, maximumX);
        Assert.NotEmpty(new PngChartRenderer().Render(chart));
    }

    [Fact]
    public void OmittedWideSideLabelDoesNotShrinkThePlot() {
        static Chart Create(string finalLabel) {
            var chart = Chart.Create().WithSize(700, 400).WithLegendPosition(ChartLegendPosition.Right).WithLegendBudget(maximumRows: 3);
            for (var index = 0; index < 8; index++) {
                chart.AddLine(index == 7 ? finalLabel : "Service " + index, new[] { new ChartPoint(0, index), new ChartPoint(1, index + 1) });
            }
            return chart;
        }
        static double PlotWidth(Chart chart) {
            var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
            var clip = svg.Descendants().Single(element => element.Name.LocalName == "clipPath" && ((string?)element.Attribute("id"))?.EndsWith("-plotClip", StringComparison.Ordinal) == true);
            return (double)clip.Elements().Single(element => element.Name.LocalName == "rect").Attribute("width")!;
        }

        double ordinary = PlotWidth(Create("Ordinary omitted label"));
        double extreme = PlotWidth(Create(new string('W', 400)));

        Assert.Equal(ordinary, extreme, 6);
        Assert.NotEmpty(new PngChartRenderer().Render(Create(new string('W', 400))));
    }

    [Fact]
    public void HorizontalLegendWidthsNeverExceedTheirDrawableLane() {
        Assert.Equal(48, Rendering.LegendRowBudget.HorizontalItemWidth(new string('W', 200), 12, 48, 52));
        Assert.Equal(7, Rendering.LegendRowBudget.HorizontalItemWidth("Value", 12, 7, 52));
    }

    [Fact]
    public void HorizontalLegendReserveHonorsHeightBudgetIncludingPlotGap() {
        var chart = Chart.Create().WithSize(900, 480).WithLegendBudget(0.35);
        for (var index = 0; index < 100; index++) chart.AddLine("Service " + index, new[] { new ChartPoint(0, index), new ChartPoint(1, index + 1) });

        var rows = Rendering.LegendRowBudget.MaximumRows(chart);
        Assert.True(Rendering.LegendRowBudget.HorizontalReserve(chart, rows) <= chart.Options.Size.Height * chart.Options.LegendMaximumHeightFraction);

        chart.WithLegendBudget(0.05);
        rows = Rendering.LegendRowBudget.MaximumRows(chart);
        Assert.True(Rendering.LegendRowBudget.HorizontalReserve(chart, rows) <= chart.Options.Size.Height * chart.Options.LegendMaximumHeightFraction);
        Assert.NotEmpty(new PngChartRenderer().Render(chart));

        chart.WithLegendBudget(0.01);
        Assert.Equal(0, Rendering.LegendRowBudget.MaximumRows(chart));
        Assert.DoesNotContain(XDocument.Parse(new SvgChartRenderer().Render(chart)).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "legend-row");
    }

    [Fact]
    public void OverflowSummaryUsesLegendTextCaseInSvg() {
        var chart = Chart.Create().WithSize(900, 560).WithLegendBudget(maximumRows: 2)
            .WithLegendStyle(style => style.WithTextCase(TextCaseTransform.Uppercase));
        for (var index = 0; index < 40; index++) chart.AddLine("Service " + index, new[] { new ChartPoint(0, index), new ChartPoint(1, index + 1) });

        var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
        var summary = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "legend-overflow");
        Assert.Contains("MORE ENTRIES", summary.Value);
        Assert.DoesNotContain("more entries", summary.Value);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(1000)]
    public void DenseRadialRingLayoutKeepsEveryRingOutsideTheCenterDisk(int count) {
        var layout = Rendering.RadialBarRingLayout.Create(28, count, 1, 18);

        Assert.True(layout.StrokeWidth > 0);
        Assert.True(layout.StrokeWidth <= (layout.OuterRadius - layout.CenterRadius - 2) / count);
        for (var index = 0; index < count; index++) {
            var radius = layout.RadiusAt(index);
            Assert.True(radius > 0);
            Assert.True(radius - layout.StrokeWidth / 2 >= layout.CenterRadius + 2 - 0.000001);
        }

        var chart = Chart.Create().WithSize(900, 560).WithLegend(false);
        chart.AddRadialBar("Dense radial", Enumerable.Range(0, count).Select(index => new ChartPoint(index, 35 + index % 61)));
        var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
        Assert.Equal(count, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "radial-bar-track"));
        Assert.Equal(count, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "radial-bar-ring"));
    }

    [Fact]
    public void LongRadialCenterLabelsCannotConsumeTheRingBand() {
        var layout = Rendering.RadialBarRingLayout.Create(100, 40, 1, 10_000);
        Assert.Equal(55, layout.CenterRadius, 6);
        Assert.True(layout.StrokeWidth > 0);
        Assert.True(layout.RadiusAt(39) - layout.StrokeWidth / 2 >= layout.CenterRadius + 2 - 0.000001);

        var chart = Chart.Create().WithSize(900, 560).WithLegend(false).WithPngOutputScale(2)
            .AddRadialBar(new string('W', 100), Enumerable.Range(0, 500).Select(index => new ChartPoint(index, 35 + index % 61)));
        var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
        var firstRing = svg.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "radial-bar-ring");
        Assert.InRange((double)firstRing.Attribute("stroke-width")!, 0.000001, 0.999999);
        Assert.NotEmpty(new PngChartRenderer().Render(chart));
    }

    [Theory]
    [InlineData("line")]
    [InlineData("pie")]
    [InlineData("radial")]
    [InlineData("gauge")]
    public void ImpossibleSideLegendBudgetDoesNotReserveAPlotLane(string kind) {
        static Chart Create(string chartKind, bool showLegend) {
            var chart = Chart.Create().WithSize(900, 560).WithLegendPosition(ChartLegendPosition.Right).WithLegendBudget(0.01).WithLegend(showLegend);
            var points = Enumerable.Range(0, 40).Select(index => new ChartPoint(index, 35 + index % 61)).ToArray();
            if (chartKind == "gauge") chart.AddGauge("Value", 73);
            else if (chartKind == "pie") chart.AddPie("Values", points);
            else if (chartKind == "radial") chart.AddRadialBar("Values", points);
            else foreach (var point in points) chart.AddLine("Service " + point.X, new[] { new ChartPoint(0, point.Y), new ChartPoint(1, point.Y + 1) });
            return chart;
        }

        static string PlotSignature(string chartKind, Chart chart) {
            var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
            Assert.DoesNotContain(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "legend-row");
            if (chartKind == "line") {
                var clip = svg.Descendants().Single(element => element.Name.LocalName == "clipPath" && ((string?)element.Attribute("id"))?.EndsWith("-plotClip", StringComparison.Ordinal) == true);
                var rect = clip.Elements().Single(element => element.Name.LocalName == "rect");
                return string.Join("|", rect.Attributes().Where(attribute => attribute.Name.LocalName is "x" or "y" or "width" or "height").Select(attribute => attribute.Value));
            }
            if (chartKind == "pie") return (string)svg.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "pie-slice").Attribute("d")!;
            if (chartKind == "gauge") return (string)svg.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "gauge-track").Attribute("d")!;
            var center = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "radial-bar-center");
            return string.Join("|", new[] { (string)center.Attribute("cx")!, (string)center.Attribute("cy")!, (string)center.Attribute("r")! });
        }

        var withLegend = Create(kind, showLegend: true);
        var withoutLegend = Create(kind, showLegend: false);
        Assert.Equal(PlotSignature(kind, withoutLegend), PlotSignature(kind, withLegend));
        Assert.Equal(new PngChartRenderer().Render(withoutLegend), new PngChartRenderer().Render(withLegend));
    }

    [Theory]
    [InlineData(ChartLegendPosition.Top)]
    [InlineData(ChartLegendPosition.Bottom)]
    public void ImpossibleHorizontalPieLegendBudgetPreservesPieGeometry(ChartLegendPosition position) {
        static Chart Create(ChartLegendPosition legendPosition, bool showLegend) => Chart.Create()
            .WithSize(900, 560)
            .WithLegendPosition(legendPosition)
            .WithLegendBudget(0.01)
            .WithLegend(showLegend)
            .AddPie("Values", Enumerable.Range(0, 40).Select(index => new ChartPoint(index, 35 + index % 61)));

        static string FirstSlicePath(Chart chart) {
            var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
            Assert.DoesNotContain(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "legend-row");
            return (string)svg.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "pie-slice").Attribute("d")!;
        }

        var withLegend = Create(position, showLegend: true);
        var withoutLegend = Create(position, showLegend: false);
        Assert.Equal(FirstSlicePath(withoutLegend), FirstSlicePath(withLegend));
        Assert.Equal(new PngChartRenderer().Render(withoutLegend), new PngChartRenderer().Render(withLegend));
    }

    [Fact]
    public void SmallLegendsStayCompleteAndInvalidBudgetIsAtomic() {
        var chart = Chart.Create().WithSize(900, 560).AddLine("Value", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        Assert.DoesNotContain("legend-overflow", new SvgChartRenderer().Render(chart));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendBudget(0.5, 0));
        Assert.Equal(0.35, chart.Options.LegendMaximumHeightFraction);
        Assert.Null(chart.Options.LegendMaximumRows);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendBudget(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendBudget(1.1));
    }
}
