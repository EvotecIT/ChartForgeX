using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
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
        var chart = Dense(kind, 100).WithLegendPosition(position).WithLegendBudget(maximumRows: 3);
        var prepared = PrepareDefaults(chart); var svg = XDocument.Parse(chart.ToSvg());
        var shown = ByRole(svg, "legend-entry").Count(element => !IsOverflow(element));
        var omitted = ByRole(svg, "legend-entry-omitted").Length;
        var entryCount = kind == "pie" ? chart.Options.MaximumPieSlices : 100;
        Assert.Equal(entryCount, shown + omitted); Assert.True(shown > 0);
        if (omitted > 0) Assert.Contains(ByRole(svg, "legend-label"), element => element.Value.Contains(omitted.ToString(CultureInfo.InvariantCulture) + " more"));
        if (kind != "pie") Assert.True(omitted > 0);
        Assert.Equal(entryCount, prepared.Regions.Count(region => region.Role == "legend" && region.Id != "legend-overflow"));
        var role = kind == "pie" ? "pie-slice" : kind == "radial" ? "radial-bar-ring" : "point";
        Assert.Equal(kind == "line" ? 200 : entryCount, prepared.Regions.Count(region => region.Role == role));
        if (kind == "pie") {
            var sourcePoints = ByRole(svg, "radial-point").SelectMany(element => element.Attribute("data-cfx-source-points")!.Value.Split(','))
                .Select(value => int.Parse(value, CultureInfo.InvariantCulture)).OrderBy(value => value).ToArray();
            Assert.Equal(Enumerable.Range(0, 100).ToArray(), sourcePoints);
        }
        Assert.Equal(kind == "line" ? 100 : 1, chart.Series.Count);
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(240, .35)]
    [InlineData(560, 1)]
    [InlineData(1000, 1)]
    public void SideLegendHeightBudgetKeepsPaintedRowsInsideTheViewport(int height, double fraction) {
        var chart = Dense("line", 100).WithSize(900, height).WithLegendPosition(ChartLegendPosition.Right).WithLegendBudget(fraction);
        var prepared = PrepareDefaults(chart);
        var visible = prepared.Regions.Where(region => region.Role == "legend" && region.Bounds.Height > 0).ToArray();
        Assert.NotEmpty(visible); Assert.Contains(visible, region => region.Id == "legend-overflow");
        Assert.All(visible, region => { Assert.InRange(region.Bounds.Top, 0, height); Assert.InRange(region.Bounds.Bottom, 0, height); });
        Assert.True(visible.Max(region => region.Bounds.Bottom) - visible.Min(region => region.Bounds.Top) <= height * fraction);
    }

    [Theory]
    [InlineData(ChartLegendPosition.TopLeft, .25)]
    [InlineData(ChartLegendPosition.Top, .5)]
    [InlineData(ChartLegendPosition.TopRight, .75)]
    public void HorizontalOverflowSummaryHonorsCommonFrameAlignment(ChartLegendPosition position, double expectedFraction) {
        var chart = Dense("line", 100).WithLegendPosition(position).WithLegendBudget(maximumRows: 2);
        var prepared = PrepareDefaults(chart); var overflow = prepared.Regions.Single(region => region.Id == "legend-overflow");
        var center = overflow.Bounds.Left + overflow.Bounds.Width / 2;
        if (expectedFraction < .5) Assert.True(center < prepared.Size.Width * .4);
        else if (expectedFraction > .5) Assert.True(center > prepared.Size.Width * .6);
        else Assert.Equal((chart.Options.Padding.Left + prepared.Size.Width - chart.Options.Padding.Right) / 2, center, 5);
    }

    [Fact]
    public void OmittedWideSideLabelsDoNotShrinkThePlot() {
        static Chart Create(string lastLabel) {
            var chart = Chart.Create().WithSize(700, 400).WithLegendPosition(ChartLegendPosition.Right).WithLegendBudget(maximumRows: 3);
            for (var index = 0; index < 8; index++) chart.AddLine(index == 7 ? lastLabel : "Service " + index,
                new[] { new ChartPoint(0, index), new ChartPoint(1, index + 1) });
            return chart;
        }
        var ordinary = PrepareDefaults(Create("Ordinary omitted label"));
        var extreme = PrepareDefaults(Create(new string('W', 400)));
        Assert.Equal(ordinary.Regions.Where(region => region.Role == "point").Select(region => region.Bounds).ToArray(),
            extreme.Regions.Where(region => region.Role == "point").Select(region => region.Bounds).ToArray());
    }

    [Fact]
    public void OverflowSummaryUsesResolvedLegendTypographyAndFullLabelsRemainAvailable() {
        var chart = Dense("line", 40).WithLegendBudget(maximumRows: 2)
            .WithLegendStyle(style => style.WithTextCase(TextCaseTransform.Uppercase).WithFontSize(18));
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Contains(ByRole(svg, "legend-label"), element => element.Value.Contains("MORE ENTRIES"));
        Assert.Contains(ByRole(svg, "legend-entry-omitted"), element => ((string?)element.Attribute("aria-label"))?.StartsWith("Service", StringComparison.Ordinal) == true);
        Assert.All(ByRole(svg, "legend-label").SelectMany(element => element.Descendants().Where(child => child.Name.LocalName == "text")),
            text => Assert.Equal("18", text.Attribute("font-size")?.Value));
    }

    [Theory]
    [InlineData("line")]
    [InlineData("pie")]
    [InlineData("radial")]
    [InlineData("gauge")]
    [InlineData("waterfall")]
    public void ImpossibleLegendBudgetLeavesPreparedGeometryAndRasterUnchanged(string kind) {
        var shown = Dense(kind, 40).WithLegendPosition(ChartLegendPosition.Right).WithLegendBudget(.001);
        var hidden = Dense(kind, 40).WithLegendPosition(ChartLegendPosition.Right).WithLegendBudget(.001).WithLegend(false);
        var withLegend = PrepareDefaults(shown); var withoutLegend = PrepareDefaults(hidden);
        Assert.Empty(ByRole(XDocument.Parse(shown.ToSvg()), "legend-entry"));
        Assert.Equal(withoutLegend.Regions.Where(region => region.Role != "legend").Select(region => region.Bounds).ToArray(),
            withLegend.Regions.Where(region => region.Role != "legend").Select(region => region.Bounds).ToArray());
        Assert.Equal(hidden.ToPng(), shown.ToPng());
    }

    [Theory]
    [InlineData(500)]
    [InlineData(1000)]
    public void DenseRadialRingsRetainEverySourceAndRemainOutsideTheCenter(int count) {
        var layout = RadialBarRingLayout.Create(28, count, 1, 18);
        Assert.True(layout.StrokeWidth > 0);
        Assert.True(layout.StrokeWidth <= (layout.OuterRadius - layout.CenterRadius - 2) / count);
        for (var index = 0; index < count; index++) Assert.True(layout.RadiusAt(index) - layout.StrokeWidth / 2 >= layout.CenterRadius + 2 - .000001);
        var chart = Dense("radial", count).WithLegend(false); var prepared = PrepareDefaults(chart);
        Assert.Equal(count, prepared.Regions.Count(region => region.Role == "radial-bar-ring"));
        Assert.Equal(count, ByRole(XDocument.Parse(chart.ToSvg()), "radial-bar-track").Length);
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void SmallLegendsStayCompleteAndInvalidBudgetChangesAreAtomic() {
        var chart = Dense("line", 1).WithLegend(true);
        Assert.Empty(ByRole(XDocument.Parse(chart.ToSvg()), "legend-entry-omitted"));
        Assert.Single(ByRole(XDocument.Parse(chart.ToSvg()), "legend-entry"));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendBudget(.5, 0));
        Assert.Equal(.35, chart.Options.LegendMaximumHeightFraction); Assert.Null(chart.Options.LegendMaximumRows);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendBudget(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendBudget(1.1));
    }

    private static Chart Dense(string kind, int count) {
        var chart = Chart.Create().WithTheme(ChartTheme.Light()).WithSize(900, 560).WithTitle("Dense legend");
        var points = Enumerable.Range(0, count).Select(index => new ChartPoint(index, index % 100 + 1)).ToArray();
        chart.WithXLabels(points.Select(point => "Service " + point.X).ToArray());
        if (kind == "pie") chart.AddPie("Values", points);
        else if (kind == "radial") chart.AddRadialBar("Values", points);
        else if (kind == "gauge") chart.AddGauge("Value", 73);
        else if (kind == "waterfall") chart.AddWaterfall("Delta", new[] { new ChartPoint(0, 18), new ChartPoint(1, -7), new ChartPoint(2, 12) });
        else foreach (var point in points) chart.AddLine("Service " + point.X, new[] { new ChartPoint(0, point.Y), new ChartPoint(1, point.Y + 1) });
        return chart;
    }
    private static PreparedVisual PrepareDefaults(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static XElement[] ByRole(XDocument document, string role) => document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    private static bool IsOverflow(XElement element) => (string?)element.Attribute("data-cfx-source-id") == "legend-overflow";
}
