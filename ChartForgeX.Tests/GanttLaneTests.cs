using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttLaneTests {
    private static readonly DateTime Start = new(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly VisualStatusTokens Status = new();

    [Fact]
    public void ToSvg_GroupsLanesAndColoursItemsBySeverity() {
        var svg = XDocument.Parse(CreateChart().ToSvg());
        Assert.Equal(new[] { "Warsaw", "London" }, Texts(svg, "gantt-lane-group"));
        Assert.Equal(new[] { "LDAP", "Replication", "Certificates" }, Texts(svg, "gantt-lane-label"));
        var items = ByRole(svg, "gantt-lane-item");
        Assert.Equal(5, items.Length);
        Assert.Equal(new[] { "high", "critical", "medium", "medium", "info" }, items.Select(item => (string)item.Attribute("data-cfx-status")!).ToArray());
        Assert.Equal(Status.Critical.Fill.ToCss(), (string)items[1].Attribute("fill")!);
        Assert.Equal(new[] { "Critical", "High", "Medium", "Low", "Informational" }, Texts(svg, "state-legend-label"));
        Assert.Equal(new[] { "2", "3" }, Texts(svg, "gantt-lane-summary"));
        Assert.Equal(new[] { "Incidents" }, Texts(svg, "gantt-lanes-summary-header"));
        Assert.Contains("USN rollback", Texts(svg, "gantt-lane-item-label"));
        Assert.Empty(ByRole(svg, "legend-item"));
    }

    [Fact]
    public void ToSvg_OverlappingItems_StackIntoSubRowsWithoutOverlap() {
        var items = ByRole(XDocument.Parse(CreateChart().ToSvg()), "gantt-lane-item").Where(item => (string)item.Attribute("data-cfx-series")! == "1").ToArray();
        Assert.Equal(new[] { "0", "1", "1" }, items.Select(item => (string)item.Attribute("data-cfx-sub-row")!).ToArray());
        Assert.True(Number(items[1], "y") >= Number(items[0], "y") + Number(items[0], "height"));
        Assert.Equal(Number(items[1], "y"), Number(items[2], "y"), 6);
        Assert.True(Number(items[2], "x") >= Number(items[1], "x") + Number(items[1], "width") - 0.001);
    }

    [Fact]
    public void ToSvg_OpenItem_RunsToNowAndIsMarkedOngoing() {
        var svg = XDocument.Parse(CreateChart().ToSvg());
        var now = ByRole(svg, "gantt-lanes-now").Single();
        var open = ByRole(svg, "gantt-lane-item").Single(item => (string?)item.Attribute("data-cfx-meta-ongoing") == "true");
        Assert.Equal(Number(now, "x1"), Number(open, "x") + Number(open, "width"), 3);
        Assert.Null(open.Attribute("data-cfx-end"));
        Assert.Contains("ongoing", Title(open), StringComparison.Ordinal);
        Assert.Equal(new[] { "Now" }, Texts(svg, "gantt-lanes-now-label"));

        var withoutToday = Chart.Create().WithSize(640, 240).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("A", new[] { new ChartGanttLaneItem(Start, Start.AddHours(4), "low"), new ChartGanttLaneItem(Start.AddHours(1), null, "high") });
        var fallback = XDocument.Parse(withoutToday.ToSvg());
        var openItem = ByRole(fallback, "gantt-lane-item").Single(item => (string?)item.Attribute("data-cfx-meta-ongoing") == "true");
        var closed = ByRole(fallback, "gantt-lane-item").First();
        Assert.True(Number(openItem, "x") + Number(openItem, "width") > Number(closed, "x") + Number(closed, "width") + 2, "Without a current time, open items extend past the latest data.");
        Assert.Empty(ByRole(fallback, "gantt-lanes-now"));

        var lateStart = Chart.Create().WithSize(640, 240).WithGanttToday(Start.AddHours(2)).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("A", new[] { new ChartGanttLaneItem(Start, Start.AddHours(6), "low"), new ChartGanttLaneItem(Start.AddHours(4), null, "high") });
        var late = ByRole(XDocument.Parse(lateStart.ToSvg()), "gantt-lane-item");
        Assert.Equal(Number(late[0], "x") + Number(late[0], "width"), Number(late[1], "x") + Number(late[1], "width"), 3);
    }

    [Fact]
    public void ToSvg_PackingSkipsHiddenItemsReusesTouchingRowsAndSeparatesUngroupedLanes() {
        var chart = Chart.Create().WithSize(720, 320).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Grouped", new[] { new ChartGanttLaneItem(Start, Start.AddHours(2), "low"), new ChartGanttLaneItem(Start.AddHours(2), Start.AddHours(4), "high") }, "Site")
            .AddGanttLane("Loose", new[] { new ChartGanttLaneItem(Start.AddHours(-10), Start.AddHours(-9), "critical"), new ChartGanttLaneItem(Start.AddHours(-10), Start.AddHours(-8), "critical"), new ChartGanttLaneItem(Start.AddHours(1), Start.AddHours(3), "medium") });
        chart.Options.XAxis.WithBounds(Start.ToOADate(), Start.AddHours(5).ToOADate());
        var svg = XDocument.Parse(chart.ToSvg());
        var items = ByRole(svg, "gantt-lane-item");
        Assert.Equal(new[] { "0", "0", "0" }, items.Select(item => (string)item.Attribute("data-cfx-sub-row")!).ToArray());
        Assert.Equal(new[] { "Site" }, Texts(svg, "gantt-lane-group"));
        Assert.Single(ByRole(svg, "gantt-lane-group-rule"));
    }

    [Fact]
    public void ToPng_ItemColoursMatchSvgGeometryAndRenderDeterministically() {
        var chart = CreateChart();
        var item = ByRole(XDocument.Parse(chart.ToSvg()), "gantt-lane-item")[1];
        var image = PngReader.Decode(chart.ToPng());
        var scale = image.Width / (double)chart.Options.Size.Width;
        var x = (int)((Number(item, "x") + Number(item, "width") - 6) * scale);
        var y = (int)((Number(item, "y") + 3) * scale);
        var offset = (y * image.Width + x) * 4;
        var critical = Status.Critical.Fill;
        Assert.True(Math.Abs(image.Pixels[offset] - critical.R) <= 12 && Math.Abs(image.Pixels[offset + 1] - critical.G) <= 12 && Math.Abs(image.Pixels[offset + 2] - critical.B) <= 12);
        Assert.Equal(chart.ToPng(), CreateChart().ToPng());
        Assert.Equal(chart.ToSvg(), CreateChart().ToSvg());
    }

    [Fact]
    public void ToInteractiveHtmlFragment_ItemsExposeHoverMetadata() {
        var html = CreateChart().ToInteractiveHtmlFragment();
        Assert.Contains("data-cfx-role=\"gantt-lane-item\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-meta-state=\"Critical\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-meta-detail=\"p95 above 250 ms\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"LDAP · High · Bind latency · 2026-09-24 02:00 UTC", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_LocalizedLabels_ReplaceNowOngoingAndSoFar() {
        var chart = CreateChart().WithLabels(labels => {
            labels.Now = "Teraz";
            labels.Ongoing = "trwa";
            labels.SoFar = "dotąd";
        });
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Equal(new[] { "Teraz" }, Texts(svg, "gantt-lanes-now-label"));
        var open = ByRole(svg, "gantt-lane-item").Single(item => (string?)item.Attribute("data-cfx-meta-ongoing") == "true");
        Assert.Contains("– trwa (", Title(open), StringComparison.Ordinal);
        Assert.Contains(" dotąd)", Title(open), StringComparison.Ordinal);
        Assert.NotEqual(CreateChart().ToPng(), chart.ToPng());
        Assert.Throws<ArgumentException>(() => chart.Options.Labels.Now = " ");

        var edge = CreateChart().WithGanttToday(Start.AddHours(47.5)).WithLabels(labels => labels.Now = "Aktualny czas systemowy");
        var edgeSvg = XDocument.Parse(edge.ToSvg());
        var label = ByRole(edgeSvg, "gantt-lanes-now-label").Single();
        var axis = ByRole(edgeSvg, "gantt-lanes-axis").Single();
        var now = ByRole(edgeSvg, "gantt-lanes-now").Single();
        Assert.Equal("middle", (string)label.Attribute("text-anchor")!);
        // The marker sits at (or next to) the right edge, so a long centred label must shift left of it (font-independent).
        Assert.True(Number(axis, "x2") - Number(now, "x1") < 20);
        Assert.True(Number(label, "x") < Number(now, "x1") - 20, "A long label near the right edge is pulled inside the plot.");
        Assert.True(Number(label, "x") > Number(axis, "x1"));
    }

    [Fact]
    public void Validation_RejectsInvalidItemsAndEmptyCharts() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartGanttLaneItem(Start, Start, "low"));
        Assert.Throws<ArgumentException>(() => new ChartGanttLaneItem(Start, null, " "));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddGanttLane("A", new[] { default(ChartGanttLaneItem) }));
        var empty = Chart.Create().AddGanttLane("A", Array.Empty<ChartGanttLaneItem>());
        Assert.Throws<InvalidOperationException>(() => empty.ToSvg());
        Assert.Throws<InvalidOperationException>(() => empty.ToPng());
        var local = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Local);
        Assert.Equal(local.ToUniversalTime().ToOADate(), new ChartGanttLaneItem(local, null, "low").Start, 9);
    }

    [Fact]
    public void Render_ManyLanesAndDisabledChrome_StayInsidePlot() {
        var chart = Chart.Create().WithSize(480, 260).WithLegend(false).WithAxes(false).WithStateCategories(Status.SeverityCategories());
        for (var lane = 0; lane < 60; lane++) chart.AddGanttLane("L" + lane.ToString(CultureInfo.InvariantCulture), new[] { new ChartGanttLaneItem(Start, Start.AddHours(1 + lane % 3), "low") }, lane % 10 == 0 ? "G" + lane.ToString(CultureInfo.InvariantCulture) : null);
        var svg = XDocument.Parse(chart.ToSvg());
        var items = ByRole(svg, "gantt-lane-item");
        Assert.Equal(60, items.Length);
        for (var i = 1; i < items.Length; i++) Assert.True(Number(items[i], "y") >= Number(items[i - 1], "y") + Number(items[i - 1], "height") - 0.001);
        Assert.Empty(ByRole(svg, "gantt-lane-label"));
        Assert.Empty(ByRole(svg, "state-legend-label"));
        Assert.True(chart.ToPng().Length > 200);
    }

    private static Chart CreateChart() {
        ChartGanttLaneItem Incident(double from, double? to, string severity, string label, string? detail = null) =>
            new(Start.AddHours(from), to.HasValue ? Start.AddHours(to.Value) : null, severity, label, detail);
        var chart = Chart.Create().WithSize(900, 480).WithXAxisTimeScale(showTimeZone: true).WithGanttToday(Start.AddHours(44))
            .WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("LDAP", new[] { Incident(2, 5.5, "high", "Bind latency", "p95 above 250 ms") }, "Warsaw", "2")
            .AddGanttLane("Replication", new[] { Incident(8, 30, "critical", "USN rollback"), Incident(26, null, "medium", "Link flapping"), Incident(12, 16, "medium", "Backlog") }, "Warsaw", "3")
            .AddGanttLane("Certificates", new[] { Incident(1, 3, "info", "Drift") }, "London");
        chart.Options.LaneSummaryHeader = "Incidents";
        return chart;
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static string[] Texts(XDocument svg, string role) => ByRole(svg, role).Select(element => element.Value).ToArray();

    private static string Title(XElement element) => element.Elements().Single(child => child.Name.LocalName == "title").Value;

    private static double Number(XElement element, string attribute) => double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);
}
