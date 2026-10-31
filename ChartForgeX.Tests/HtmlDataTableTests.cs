using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HtmlDataTableTests {
    [Fact]
    public void DefaultModeRetainsTheExistingFragmentBytesAndSvgPolicy() {
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Requests").AddBar("Count", new[] { new ChartPoint(1, 12) });
        var renderer = new HtmlChartRenderer();
        var options = new VisualSvgOptions("requests", chart.Options.SvgColorVariables, responsive: true);
        var html = renderer.RenderFragment(chart, options);
        Assert.Equal(renderer.RenderFragment(chart, "requests"), html);
        Assert.Equal(renderer.RenderFragment(chart), renderer.RenderFragment(chart, new VisualSvgOptions(responsive: true)));
        Assert.Equal(renderer.RenderFragment(chart, options, ChartDataTableMode.None), html);
        Assert.DoesNotContain("<table", html, StringComparison.Ordinal);
        Assert.StartsWith("<div class=\"chartforgex-chart\" style=\"width:100%;max-width:640px;box-sizing:border-box;overflow:visible\"><svg", html, StringComparison.Ordinal);
        Assert.Contains("id=\"requests", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("bar")]
    [InlineData("horizontal-bar")]
    [InlineData("stacked")]
    [InlineData("scatter")]
    [InlineData("sparkline")]
    public void NumericTablesPreserveAuthoredValuesFormattingLabelsAndSafeMarkup(string family) {
        var points = new[] { new ChartPoint(1, 12.5), new ChartPoint(3, -2) };
        var chart = Chart.Create().WithTitle("Load <today> & yesterday").WithXLabels("One <1>", "Absent", "Three")
            .WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.InvariantCulture)).WithValueUnit("ms");
        switch (family) {
            case "bar": chart.AddBar("Service & API", points); break;
            case "horizontal-bar": chart.AddHorizontalBar("Service & API", points); break;
            case "stacked": chart.AddStackedArea("Service & API", points).AddStackedArea("Other", new[] { new ChartPoint(1, 5), new ChartPoint(3, 4) }); break;
            case "scatter": chart.AddScatter("Service & API", points); break;
            default: chart.AddLine("Service & API", points).WithSparkline(); break;
        }
        chart.Series[0].WithPointLabel(0, "<script>unsafe & text</script>");
        var html = new HtmlChartRenderer().RenderFragment(chart, new VisualSvgOptions("data"), ChartDataTableMode.Details);
        var root = XDocument.Parse(html).Root!;
        var details = Assert.Single(root.Elements("details"));
        Assert.Equal("View chart data: Load <today> & yesterday", details.Element("summary")!.Value);
        var table = details.Elements("table").First();
        Assert.Equal("Load <today> & yesterday — Service & API", table.Element("caption")!.Value);
        Assert.All(table.Element("thead")!.Descendants("th"), cell => Assert.Equal("col", cell.Attribute("scope")!.Value));
        var rows = table.Element("tbody")!.Elements("tr").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(new[] { "One <1>", "12.50\u00a0ms", "<script>unsafe & text</script>" }, Cells(rows[0]));
        Assert.Equal(new[] { "Three", "-2.00\u00a0ms", "" }, Cells(rows[1]));
        Assert.All(rows, row => Assert.Equal("row", row.Element("th")!.Attribute("scope")!.Value));
        Assert.Empty(root.Descendants("script"));
        Assert.Contains("&lt;script&gt;unsafe &amp; text&lt;/script&gt;", html, StringComparison.Ordinal);
        if (family == "stacked") Assert.Equal(new[] { "One <1>", "5.00\u00a0ms", "" }, Cells(details.Elements("table").Last().Element("tbody")!.Elements("tr").First()));
    }

    [Fact]
    public void HiddenTablesStayAccessibleAndAreHiddenOnlyInScreenMedia() {
        var chart = Chart.Create().AddBar("Requests", new[] { new ChartPoint(1, 7) });
        var html = new HtmlChartRenderer().RenderFragment(chart, new VisualSvgOptions(), ChartDataTableMode.VisuallyHidden);
        var root = XDocument.Parse(html).Root!;
        var wrapper = Assert.Single(root.Elements("div"));
        Assert.Equal("chartforgex-data-tables chartforgex-data-table-hidden", wrapper.Attribute("class")!.Value);
        Assert.Null(wrapper.Attribute("hidden"));
        Assert.Null(wrapper.Attribute("aria-hidden"));
        Assert.Single(wrapper.Elements("table"));
        var css = root.Element("style")!.Value;
        Assert.Contains("@media screen{.chartforgex-data-table-hidden{position:absolute;width:1px;height:1px;", css, StringComparison.Ordinal);
        Assert.DoesNotContain("display:none", css, StringComparison.Ordinal);
        Assert.DoesNotContain("visibility:hidden", css, StringComparison.Ordinal);
        Assert.Contains("@media print", css, StringComparison.Ordinal);
    }

    [Fact]
    public void GanttTablesExposeTaskMilestoneMarkerAndOpenEndWithoutProjectionValues() {
        var chart = Chart.Create().WithXAxisBounds(0, 12).WithXAxisValueFormatter(value => "Day " + value.ToString(CultureInfo.InvariantCulture))
            .AddGanttTask("Build", 1, 5, .5).AddGanttMilestone("Launch", 6).AddGanttTask("Support", 7, 10);
        chart.Series[2].ReplaceGanttItems(new[] { ChartGanttItem.OpenEnded(7, .25) });
        chart.AddGanttTask("Review marker", 4, 4);
        chart.Series[3].ReplaceGanttItems(new[] { new ChartGanttItem(4, 4, isVerticalMarker: true) });
        var tables = Tables(chart);
        Assert.Equal(new[] { "Day 1", "Day 5", "50 %", "Task", "" }, Cells(tables[0].Element("tbody")!.Elements("tr").Single()));
        Assert.Equal(new[] { "Day 6", "Day 6", "100 %", "Milestone", "" }, Cells(tables[1].Element("tbody")!.Elements("tr").Single()));
        Assert.Equal(new[] { "Day 7", "ongoing", "25 %", "Task", "" }, Cells(tables[2].Element("tbody")!.Elements("tr").Single()));
        Assert.Equal("Marker", Cells(tables[3].Element("tbody")!.Elements("tr").Single())[3]);
    }

    [Fact]
    public void LaneAndTimelineTablesRetainStateLabelsDetailsAndUnknownEnds() {
        var from = new DateTime(2026, 1, 2);
        var until = from.AddDays(2);
        var chart = Chart.Create().WithStateCategories(new ChartStateCategory("warn", "Warning & review", ChartColor.FromHex("#C00000")))
            .WithXAxisValueFormatter(value => DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .WithGanttLaneNow(until)
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(from, null, "warn", "Incident <7>", "Still & investigating"),
                new ChartGanttLaneItem(from, until, "warn", "Closed") }, group: "Production", summary: "Two incidents");
        var lane = Assert.Single(Tables(chart));
        Assert.Equal("ChartForgeX chart — Service — Production — Two incidents", lane.Element("caption")!.Value);
        var items = lane.Element("tbody")!.Elements("tr").ToArray();
        Assert.Equal(new[] { "2026-01-02", "2026-01-04", "Warning & review", "Closed", "" }, Cells(items[0]));
        Assert.Equal(new[] { "2026-01-02", "ongoing", "Warning & review", "Incident <7>", "Still & investigating" }, Cells(items[1]));
        var state = Chart.Create().WithStateCategories(new ChartStateCategory("warn", "Warning & review", ChartColor.FromHex("#C00000")))
            .WithXAxisValueFormatter(value => "T" + value.ToString(CultureInfo.InvariantCulture))
            .AddStateTimelineLane("State", new[] { new ChartStateTimelineSegment(1, 4, "warn", "Check <source>") });
        Assert.Equal(new[] { "T1", "T4", "Warning & review", "Check <source>", "" }, OnlyRow(state));
        var timeline = Chart.Create().WithXAxisValueFormatter(value => "T" + value.ToString(CultureInfo.InvariantCulture)).AddTimelineRange("Release", 2, 9);
        Assert.Equal(new[] { "T2", "T9", "" }, OnlyRow(timeline));
    }

    [Theory]
    [InlineData("interval", "Start", "End")]
    [InlineData("dumbbell", "Start", "End")]
    [InlineData("band", "Lower", "Upper")]
    [InlineData("area", "Lower", "Upper")]
    public void PairedTablesReadBothAuthoredBoundsRatherThanEncodedRenderPoints(string family, string lower, string upper) {
        var chart = Chart.Create().WithXLabels("North").WithValueFormatter(value => value.ToString("0.0", CultureInfo.InvariantCulture));
        switch (family) {
            case "interval": chart.AddRangeBar("Window", new[] { new ChartInterval(1, 2, 8) }); break;
            case "dumbbell": chart.AddDumbbell("Window", new[] { new ChartDumbbell(1, 2, 8) }); break;
            case "area": chart.AddRangeArea("Window", new[] { new ChartRangeBand(1, 2, 8), new ChartRangeBand(2, 3, 9) }); break;
            default: chart.AddRangeBand("Window", new[] { new ChartRangeBand(1, 2, 8), new ChartRangeBand(2, 3, 9) }); break;
        }
        var table = Assert.Single(Tables(chart));
        Assert.Equal(new[] { "Category", lower, upper, "Label" }, table.Element("thead")!.Descendants("th").Select(cell => cell.Value));
        Assert.Equal(new[] { "North", "2.0", "8.0", "" }, Cells(table.Element("tbody")!.Elements("tr").First()));
    }

    [Fact]
    public void CalendarTablesUseAuthoredDatesAndDateFormatterWithoutInventingMissingDays() {
        var chart = Chart.Create().WithValueFormatter(value => value.ToString("0.0", CultureInfo.InvariantCulture))
            .AddCalendarHeatmap("Daily", new[] { new ChartCalendarHeatmapItem(new DateTime(2026, 1, 1), 0), new ChartCalendarHeatmapItem(new DateTime(2026, 1, 3), 8) });
        chart.Options.Labels.DateFormatter = day => day.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        var rows = Assert.Single(Tables(chart)).Element("tbody")!.Elements("tr").ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(new[] { "01 Jan 2026", "0.0", "" }, Cells(rows[0]));
        Assert.Equal(new[] { "03 Jan 2026", "8.0", "" }, Cells(rows[1]));
    }

    [Fact]
    public void MaskedHeatmapTablesKeepColumnPositionsAndDistinguishMissingFromZero() {
        var chart = Chart.Create().WithXLabels("First", "Missing", "Third").AddHeatmapRow("Checks", new double?[] { 0, null, 3 });
        var rows = Assert.Single(Tables(chart)).Element("tbody")!.Elements("tr").ToArray();
        Assert.Equal(new[] { "First", "0", "" }, Cells(rows[0]));
        Assert.Equal(new[] { "Missing", "No data", "" }, Cells(rows[1]));
        Assert.Equal(new[] { "Third", "3", "" }, Cells(rows[2]));
        var states = Chart.Create().WithXLabels("First", "Second").WithStateCategories(new ChartStateCategory("ok", "Healthy", ChartColor.FromHex("#008000")))
            .AddHeatmapCategoryRow("Checks", new ChartHeatmapCell?[] { null, new("ok", "3/3", "Complete <pass>") });
        var stateRows = Assert.Single(Tables(states)).Element("tbody")!.Elements("tr").ToArray();
        Assert.Equal(new[] { "First", "No data", "", "" }, Cells(stateRows[0]));
        Assert.Equal(new[] { "Second", "Healthy", "3/3", "Complete <pass>" }, Cells(stateRows[1]));
    }

    [Fact]
    public void EmptySeriesAreNamedAndUnsupportedTypedDataIsNotSilentlyLost() {
        var empty = Chart.Create().WithTitle("Empty").AddLine("Readings", Array.Empty<ChartPoint>());
        var table = Assert.Single(Tables(empty));
        Assert.Equal("Empty — Readings", table.Element("caption")!.Value);
        Assert.Equal("No data", table.Element("tbody")!.Value);
        Assert.Equal("3", table.Descendants("td").Single().Attribute("colspan")!.Value);
        var unsupported = Chart.Create().AddBubble("Bubble", new[] { new ChartBubble(1, 2, 3) });
        var renderer = new HtmlChartRenderer();
        Assert.Throws<NotSupportedException>(() => renderer.RenderFragment(unsupported, new VisualSvgOptions(), ChartDataTableMode.Details));
        Assert.DoesNotContain("<table", renderer.RenderFragment(unsupported), StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.RenderFragment(empty, new VisualSvgOptions(), (ChartDataTableMode)42));
    }

    private static XElement[] Tables(Chart chart) => XDocument.Parse(new HtmlChartRenderer().RenderFragment(chart,
        new VisualSvgOptions("table-test"), ChartDataTableMode.Details)).Descendants("table").ToArray();

    private static string[] OnlyRow(Chart chart) => Cells(Assert.Single(Tables(chart)).Element("tbody")!.Elements("tr").Single());

    private static string[] Cells(XElement row) => row.Elements().Select(cell => cell.Value).ToArray();
}
