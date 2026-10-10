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
    [InlineData(1, false)]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    public void DenseVerticalTotalsReserveMeasuredSpaceAboveAndBelowExactValueBounds(int sign, bool reversed) {
        var font = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(font));
        var chart = Chart.Create().WithSize(300, 220).WithAxes(false).WithLegend(false).WithHeader(false)
            .WithPngFont(font).WithStackedBars().WithStackTotals().ConfigureDataLabelStyle(style => style.WithFontSize(18))
            .WithYAxisBounds(sign < 0 ? -16 : sign > 0 ? 0 : -16, sign < 0 ? 0 : 16);
        chart.Options.YAxis.WithReversal(reversed);
        var signs = sign == 0 ? new[] { 1, -1 } : new[] { sign };
        foreach (var direction in signs) {
            chart.AddBar("Passed " + direction, Enumerable.Range(1, 16).Select(x => new ChartPoint(x, 13 * direction)).ToArray());
            chart.AddBar("Warnings " + direction, Enumerable.Range(1, 16).Select(x => new ChartPoint(x, 3 * direction)).ToArray());
        }
        foreach (var series in chart.Series)
            for (var point = 0; point < series.Points.Count; point++) series.WithPointLabel(point, "Observation");
        var calls = 0;
        chart.WithValueFormatter(value => { calls++; return "Total " + value.ToString(CultureInfo.InvariantCulture); });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(16 * signs.Length, calls);
        var totals = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "stack-total-label").ToArray();
        var regions = prepared.Regions.Where(region => region.Role == "stack-total").ToArray();
        Assert.Equal(16 * signs.Length, regions.Length);
        foreach (var direction in signs)
            Assert.Contains(totals, total => regions.Any(region => total.Id == region.Id + "-label" && region.Label == "Total " + (16 * direction)));
        var boxes = totals.Select(total => new ChartRect(total.X, total.Baseline - total.Text.Ascent,
            total.Text.Metrics.Width, total.Text.Metrics.Height)).ToArray();
        var marks = prepared.Regions.Where(region => region.Role == "point").Select(region => region.Bounds).ToArray();
        for (var index = 0; index < boxes.Length; index++) {
            Assert.True(boxes[index].Top >= 0 && boxes[index].Bottom <= 220);
            Assert.True(boxes[index].Left >= 0 && boxes[index].Right <= 300);
            Assert.DoesNotContain(marks, mark => Overlaps(boxes[index], mark));
            for (var other = index + 1; other < boxes.Length; other++) Assert.False(Overlaps(boxes[index], boxes[other]));
        }
        var svg = prepared.ToSvg();
        Assert.Equal(totals.Length, XDocument.Parse(svg).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "stack-total-label"));
        Assert.True(prepared.ToPng().Length > 64);
        Assert.Equal(16 * signs.Length, calls);

        static bool Overlaps(ChartRect first, ChartRect second) => first.Left < second.Right && first.Right > second.Left
            && first.Top < second.Bottom && first.Bottom > second.Top;
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    public void DashboardRowsKeepEveryFullTotalBesideItsStack(int sign, bool reversed) {
        var chart = DashboardRows(sign);
        chart.Options.XAxis.WithReversal(reversed);
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
            if (sign > 0 != reversed) Assert.True(total.X >= row.Max(region => region.Bounds.Right) + 2);
            else Assert.True(total.X + total.Text.Metrics.Width <= row.Min(region => region.Bounds.Left) - 2);
            Assert.InRange(total.X, 0, 640 - total.Text.Metrics.Width);
        }
        var svg = prepared.ToSvg();
        Assert.Equal(3, XDocument.Parse(svg).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "stack-total-label"));
        Assert.Equal(svg, chart.ToSvg());
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OppositeTotalGuttersReuseTheResolvedFormatterSnapshot(bool reversed) {
        var chart = DashboardRows(1).WithAxes(false);
        chart.Options.XAxis.WithReversal(reversed);
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

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(false, false, -1)]
    [InlineData(false, true, -1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(true, false, -1)]
    [InlineData(true, true, -1)]
    public void CompactExactBoundsKeepEveryTotalOutsideItsMappedStack(bool horizontal, bool reversed, int sign) {
        var chart = Chart.Create().WithSize(300, 220).WithAxes(false).WithLegend(false).WithHeader(false)
            .WithBarStyle(ChartBarStyle.Flat).WithStackedBars().WithStackTotals().WithDataLabels(false);
        var categories = horizontal ? new[] { 1 } : Enumerable.Range(1, 12).ToArray();
        var first = categories.Select(category => new ChartPoint(category, 10 * sign));
        var second = categories.Select(category => new ChartPoint(category, 20 * sign));
        if (horizontal) chart.AddHorizontalBar("First", first).AddHorizontalBar("Second", second);
        else chart.AddBar("First", first).AddBar("Second", second);
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis)
            .WithBounds(sign > 0 ? 0 : -30, sign > 0 ? 30 : 0).WithReversal(reversed);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var totals = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "stack-total-label").ToArray();
        var regions = prepared.Regions.Where(region => region.Role == "stack-total").ToArray();
        Assert.Equal(categories.Length, regions.Length); Assert.Equal(regions.Length, totals.Length);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        foreach (var total in totals) {
            var anchor = regions.Single(region => total.Id == region.Id + "-label").Bounds;
            if (horizontal) {
                if (sign > 0 != reversed) Assert.True(total.X >= anchor.X + 2);
                else Assert.True(total.X + total.Text.Metrics.Width <= anchor.X - 2);
            } else {
                var top = total.Baseline - total.Text.Ascent;
                if (sign > 0 != reversed) Assert.True(top + total.Text.Metrics.Height <= anchor.Y - 2);
                else Assert.True(top >= anchor.Y + 2);
            }
        }
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void OppositeAxisReversalsKeepPrimaryAndSecondaryTotalGutters() {
        var chart = Chart.Create().WithSize(620, 260).WithAxes(false).WithLegend(false).WithHeader(false)
            .WithStackedBars().WithStackTotals().WithDataLabels(false).WithBarStyle(ChartBarStyle.Flat);
        chart.AddBar("Primary first", new[] { new ChartPoint(1, 10) }).AddBar("Primary second", new[] { new ChartPoint(1, 20) });
        chart.AddBar("Secondary first", new[] { new ChartPoint(1, 10) }).AddBar("Secondary second", new[] { new ChartPoint(1, 20) });
        chart.Series[2].YAxis = ChartAxisSide.Secondary; chart.Series[3].YAxis = ChartAxisSide.Secondary;
        chart.Options.YAxis.WithBounds(0, 30);
        chart.Options.SecondaryYAxis.WithBounds(0, 30).WithReversal();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var totals = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "stack-total-label").ToArray();
        Assert.Equal(2, totals.Length);
        foreach (var total in totals) {
            var anchor = prepared.Regions.Single(region => total.Id == region.Id + "-label").Bounds;
            var top = total.Baseline - total.Text.Ascent;
            if (total.Id!.Contains("secondary", StringComparison.Ordinal)) Assert.True(top >= anchor.Y + 2);
            else Assert.True(top + total.Text.Metrics.Height <= anchor.Y - 2);
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
    }

    private static Chart DashboardRows(int sign) => Chart.Create().WithSize(640, 300).WithTheme(ChartTheme.DashboardLight())
        .WithDashboardStackedRowStyle(showTotals: true).WithXLabels("Engineering", "Maintenance", "HSEQ")
        .AddHorizontalBar("All employee", Points(68, 62, 74, sign), ChartColor.FromHex("#7057E6"))
        .AddHorizontalBar("Terminated", Points(25, 28, 24, sign), ChartColor.FromHex("#5FD3D9"))
        .AddHorizontalBar("New hires", Points(14, 12, 15, sign), ChartColor.FromHex("#FFB05C"));

    private static ChartPoint[] Points(double first, double second, double third, int sign) =>
        new[] { new ChartPoint(1, first * sign), new ChartPoint(2, second * sign), new ChartPoint(3, third * sign) };
}
