using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianStackTotalLayoutTests {
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void DashboardRowsKeepEveryFullTotalBesideItsStack(int sign) {
        var chart = DashboardRows(sign);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var totals = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "stack-total-label").ToArray();
        Assert.Equal(new[] { 107 * sign, 102 * sign, 113 * sign }.Select(value => value.ToString(CultureInfo.InvariantCulture)),
            totals.Select(node => node.Text.Lines.Single().Text));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        foreach (var total in totals) {
            var row = prepared.Regions.Where(region => region.Role == "point"
                && Math.Abs(region.Bounds.Top + region.Bounds.Height / 2 - (total.Baseline - total.Text.Ascent + total.Text.Metrics.Height / 2)) < .001).ToArray();
            Assert.Equal(3, row.Length);
            Assert.True(total.Text.Metrics.Height > total.Text.Size, "The total lane must preserve the full styled line height.");
            if (sign > 0) Assert.True(total.X >= row.Max(region => region.Bounds.Right) + 2);
            else Assert.True(total.X + total.Text.Metrics.Width <= row.Min(region => region.Bounds.Left) - 2);
            Assert.InRange(total.X, 0, 640 - total.Text.Metrics.Width);
        }
        var svg = prepared.ToSvg();
        Assert.Equal(3, XDocument.Parse(svg).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "stack-total-label"));
        Assert.Equal(svg, chart.ToSvg());
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void OppositeTotalGuttersReuseTheResolvedFormatterSnapshot() {
        var chart = DashboardRows(1).WithAxes(false);
        chart.AddHorizontalBar("Negative", new[] { new ChartPoint(1, -18), new ChartPoint(2, -22), new ChartPoint(3, -16) });
        foreach (var series in chart.Series)
            for (var point = 0; point < series.Points.Count; point++) series.WithPointLabel(point, "Observation");
        var calls = 0;
        chart.WithValueFormatter(value => (++calls).ToString(CultureInfo.InvariantCulture) + ":" + value.ToString(CultureInfo.InvariantCulture));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(6, calls);
        var totals = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "stack-total-label").ToArray();
        Assert.Equal(6, totals.Length);
        var regions = prepared.Regions.Where(region => region.Role == "stack-total").ToArray();
        foreach (var total in totals) Assert.Contains(regions, region => region.Label!.StartsWith(total.Text.Lines.Single().Text + " category=", StringComparison.Ordinal));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        prepared.ToSvg(); prepared.ToPng();
        Assert.Equal(6, calls);
    }

    private static Chart DashboardRows(int sign) => Chart.Create().WithSize(640, 300).WithTheme(ChartTheme.DashboardLight())
        .WithDashboardStackedRowStyle(showTotals: true).WithXLabels("Engineering", "Maintenance", "HSEQ")
        .AddHorizontalBar("All employee", Points(68, 62, 74, sign), ChartColor.FromHex("#7057E6"))
        .AddHorizontalBar("Terminated", Points(25, 28, 24, sign), ChartColor.FromHex("#5FD3D9"))
        .AddHorizontalBar("New hires", Points(14, 12, 15, sign), ChartColor.FromHex("#FFB05C"));

    private static ChartPoint[] Points(double first, double second, double third, int sign) =>
        new[] { new ChartPoint(1, first * sign), new ChartPoint(2, second * sign), new ChartPoint(3, third * sign) };
}
