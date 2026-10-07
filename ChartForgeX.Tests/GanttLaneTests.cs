using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttLaneTests {
    private static readonly DateTime Start = new(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly VisualStatusTokens Status = new();

    [Fact]
    public void CurrentInstant_LocalTime_AlignsWithOpenLaneItem() {
        var start = new DateTime(2026, 9, 24, 10, 0, 0, DateTimeKind.Local);
        var now = start.AddHours(1);
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 240).WithGanttLaneNow(now)
            .WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("A", new[] { new ChartGanttLaneItem(start, null, "low") });
        Assert.Equal(now.ToUniversalTime().ToOADate(), chart.Options.GanttToday);
        var item = Assert.Single(ByRole(XDocument.Parse(chart.ToSvg()), "gantt-lane-item"));
        Assert.Contains("1h", Title(item), StringComparison.Ordinal);
        chart.WithGanttLaneNow(null);
        Assert.Null(chart.Options.GanttToday);
    }

    [Fact]
    public void ToSvg_GroupsLanesAndColoursItemsBySeverity() {
        var svg = XDocument.Parse(CreateChart().ToSvg());
        Assert.Equal(new[] { "Warsaw", "London" }, Texts(svg, "gantt-lane-group"));
        Assert.Equal(new[] { "LDAP", "Replication", "Certificates" }, Texts(svg, "schedule-row-label"));
        var items = ByRole(svg, "gantt-lane-item");
        Assert.Equal(5, items.Length);
        Assert.Equal(new[] { "high", "critical", "medium", "medium", "info" }, items.Select(item => (string)item.Attribute("data-cfx-status")!).ToArray());
        Assert.Equal(Status.Critical.Fill.ToCss(), (string)items[1].RenderedAttribute("fill")!);
        Assert.Equal(new[] { "Critical", "High", "Medium", "Low", "Informational" }, Texts(svg, "legend-label"));
        Assert.Equal(new[] { "2", "3" }, Texts(svg, "lane-summary"));
        Assert.Equal(new[] { "Incidents" }, Texts(svg, "lane-summary-header"));
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
        Assert.Equal("none", (string?)now.Attribute("pointer-events"));
        var open = ByRole(svg, "gantt-lane-item").Single(item => (string?)item.Attribute("data-cfx-open") == "true");
        Assert.Equal(Number(now, "x1"), Number(open, "x") + Number(open, "width"), 3);
        Assert.NotNull(open.Attribute("data-cfx-end")); // Prepared output snapshots the resolved current endpoint.
        Assert.Contains("ongoing", Title(open), StringComparison.Ordinal);
        Assert.Equal(new[] { "Now" }, Texts(svg, "gantt-lanes-now-label"));

        var withoutToday = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 240).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("A", new[] { new ChartGanttLaneItem(Start, Start.AddHours(4), "low"), new ChartGanttLaneItem(Start.AddHours(1), null, "high") });
        var fallback = XDocument.Parse(withoutToday.ToSvg());
        var openItem = ByRole(fallback, "gantt-lane-item").Single(item => (string?)item.Attribute("data-cfx-open") == "true");
        var closed = ByRole(fallback, "gantt-lane-item").First();
        Assert.True(Number(openItem, "x") + Number(openItem, "width") > Number(closed, "x") + Number(closed, "width") + 2, "Without a current time, open items extend past the latest data.");
        Assert.Empty(ByRole(fallback, "gantt-lanes-now"));

        var lateStart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 240).WithGanttToday(Start.AddHours(2)).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("A", new[] { new ChartGanttLaneItem(Start, Start.AddHours(6), "low"), new ChartGanttLaneItem(Start.AddHours(4), null, "high") });
        var late = ByRole(XDocument.Parse(lateStart.ToSvg()), "gantt-lane-item");
        Assert.Equal(Number(late[0], "x") + Number(late[0], "width"), Number(late[1], "x") + Number(late[1], "width"), 3);
    }

    [Fact]
    public void Render_WideFiniteBounds_KeepLaneCoordinatesFinite()
    {
        var chart = CreateChart().ConfigureXAxis(axis => axis.WithBounds(-1e308, 1e308));
        var model = ChartGanttLaneModel.Build(chart);
        var plot = new ChartRect(10, 10, 500, 100);
        Assert.Equal(plot.Left, model.X(model.Min, plot), 6);
        Assert.Equal(plot.Left + plot.Width / 2, model.X(0, plot), 6);
        Assert.Equal(plot.Right, model.X(model.Max, plot), 6);
        Assert.DoesNotContain("NaN", chart.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void ToSvg_SubpixelTouchingItems_UseSeparateVisibleRows() {
        var first = Start;
        var second = first.AddTicks(1000);
        var third = second.AddTicks(1000);
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 280).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Brief", new[] { new ChartGanttLaneItem(first, second, "low"), new ChartGanttLaneItem(second, third, "high") });
        chart.Options.XAxis.WithBounds(first.ToOADate(), first.AddDays(1).ToOADate());
        var bars = ByRole(XDocument.Parse(chart.ToSvg()), "gantt-lane-item");
        Assert.Equal(2, bars.Length);
        Assert.NotEqual((string?)bars[0].Attribute("data-cfx-sub-row"), (string?)bars[1].Attribute("data-cfx-sub-row"));
        Assert.True(Number(bars[1], "y") >= Number(bars[0], "y") + Number(bars[0], "height"));
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void Render_NearPixelThreshold_UsesSharedSubRowsForSvgAndPng() {
        var boundary = 2.0 / 467.9;
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 280).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Brief", new[] { new ChartGanttLaneItem(0, boundary, "low"), new ChartGanttLaneItem(boundary, boundary + 0.001, "high") });
        chart.Options.ShowLegend = false;
        chart.Options.XAxis.WithBounds(0, 1);
        var rows = ChartGanttLaneModel.Build(chart).Rows.Single().Items.Select(item => item.SubRow).ToArray();
        Assert.Equal(new[] { 0, 1 }, rows);
        Assert.Equal(new[] { "0", "1" }, ByRole(XDocument.Parse(chart.ToSvg()), "gantt-lane-item")
            .Select(item => (string?)item.Attribute("data-cfx-sub-row")).ToArray());
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void Render_PaddedNarrowPlot_DoesNotOverlapMinimumWidthItems() {
        var boundary = 2.0 / 300;
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 280).WithPadding(220, 10, 220, 10).WithAxes(false)
            .WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Brief", new[] { new ChartGanttLaneItem(0, boundary, "low"), new ChartGanttLaneItem(boundary, boundary + 0.001, "high") });
        chart.Options.XAxis.WithBounds(0, 1);
        var bars = ByRole(XDocument.Parse(chart.ToSvg()), "gantt-lane-item");
        Assert.Equal(new[] { "0", "1" }, bars.Select(item => (string?)item.Attribute("data-cfx-sub-row")).ToArray());
        Assert.True(Number(bars[1], "y") >= Number(bars[0], "y") + Number(bars[0], "height"));
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_OneSidedNumericLimit_RejectsAnUnrepresentableWindow(bool lowerBound) {
        var chart = CreateChart();
        if (lowerBound) chart.Options.XAxis.Minimum = double.MaxValue;
        else chart.Options.XAxis.Maximum = -double.MaxValue;
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartGanttLaneModel.Build(chart));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ToSvg());
    }

    [Fact]
    public void Render_OpenItemWithExtremeFiniteRange_KeepsAutomaticBoundsFinite() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 280).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Range", new[] { new ChartGanttLaneItem(-1e308, 1e308, "low"), new ChartGanttLaneItem(0, null, "high") });
        var model = ChartGanttLaneModel.Build(chart);
        Assert.True(double.IsFinite(model.Min));
        Assert.True(double.IsFinite(model.Max));
        Assert.True(model.Max > 1e308);
        var svg = chart.ToSvg();
        Assert.DoesNotContain("Infinity", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("NaN", svg, StringComparison.Ordinal);
        Assert.Contains("duration exceeds numeric range", svg, StringComparison.Ordinal);
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void ToSvg_PackingSkipsHiddenItemsReusesTouchingRowsAndSeparatesUngroupedLanes() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 320).WithStateCategories(Status.SeverityCategories())
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
        var open = ByRole(svg, "gantt-lane-item").Single(item => (string?)item.Attribute("data-cfx-open") == "true");
        Assert.Contains("– trwa (", Title(open), StringComparison.Ordinal);
        Assert.Contains(" dotąd)", Title(open), StringComparison.Ordinal);
        Assert.NotEqual(CreateChart().ToPng(), chart.ToPng());
        Assert.Throws<ArgumentException>(() => chart.Options.Labels.Now = " ");

        var edge = CreateChart().WithGanttToday(Start.AddHours(47.5)).WithLabels(labels => labels.Now = "Aktualny czas systemowy");
        var edgeSvg = XDocument.Parse(edge.ToSvg());
        var label = ByRole(edgeSvg, "gantt-lanes-now-label").Single();
        var axis = ByRole(edgeSvg, "schedule-axis").Single();
        var now = ByRole(edgeSvg, "gantt-lanes-now").Single();
        Assert.Equal("middle", (string)label.RenderedAttribute("text-anchor")!);
        // The marker sits at (or next to) the right edge, so a long centred label must shift left of it (font-independent).
        Assert.True(Number(axis, "x2") - Number(now, "x1") < 20);
        Assert.True(Number(label, "x") < Number(now, "x1") - 20, "A long label near the right edge is pulled inside the plot.");
        Assert.True(Number(label, "x") > Number(axis, "x1"));
    }

    [Fact]
    public void Validation_RejectsInvalidItemsAndEmptyCharts() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartGanttLaneItem(Start, Start, "low"));
        Assert.Throws<ArgumentException>(() => new ChartGanttLaneItem(Start, null, " "));
        Assert.Throws<ArgumentException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).AddGanttLane("A", new[] { default(ChartGanttLaneItem) }));
        var empty = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).AddGanttLane("A", Array.Empty<ChartGanttLaneItem>());
        Assert.Throws<InvalidOperationException>(() => empty.ToSvg());
        Assert.Throws<InvalidOperationException>(() => empty.ToPng());
        var local = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Local);
        Assert.Equal(local.ToUniversalTime().ToOADate(), new ChartGanttLaneItem(local, null, "low").Start, 9);
    }

    [Fact]
    public void Validation_DoubleIntervalBelowDisplayResolution_RejectsMisleadingMetadata() {
        double start = Start.ToOADate();
        double shortEnd = start + TimeSpan.FromTicks(200).TotalDays;
        Assert.True(shortEnd > start);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartGanttLaneItem(start, shortEnd, "low"));

        double visibleEnd = start + TimeSpan.FromTicks(2_000).TotalDays;
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithXAxisTimeScale().WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(start, visibleEnd, "low") });
        var item = Assert.Single(ByRole(XDocument.Parse(chart.ToSvg()), "gantt-lane-item"));
        Assert.NotEqual((string?)item.Attribute("data-cfx-start"), (string?)item.Attribute("data-cfx-end"));
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void Validation_ReplacedPublicPoint_CannotDisagreeWithLaneItem() {
        var chart = CreateChart();
        ChartSeries lane = chart.Series[1];
        ChartPoint original = lane.Points[0];
        lane.Points[0] = new ChartPoint(original.X + 1, original.Y);
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        Assert.Throws<InvalidOperationException>(() => chart.ToPng());

        lane.Points[0] = original;
        lane.Points.Reverse();
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
    }

    [Fact]
    public void Render_ManyLanesAndDisabledChrome_StayInsidePlot() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(480, 260).WithLegend(false).WithAxes(false).WithStateCategories(Status.SeverityCategories());
        for (var lane = 0; lane < 60; lane++) chart.AddGanttLane("L" + lane.ToString(CultureInfo.InvariantCulture), new[] { new ChartGanttLaneItem(Start, Start.AddHours(1 + lane % 3), "low") }, lane % 10 == 0 ? "G" + lane.ToString(CultureInfo.InvariantCulture) : null);
        var svg = XDocument.Parse(chart.ToSvg());
        var items = ByRole(svg, "gantt-lane-item");
        Assert.Equal(60, items.Length);
        for (var i = 1; i < items.Length; i++) Assert.True(Number(items[i], "y") >= Number(items[i - 1], "y") + Number(items[i - 1], "height") - 0.001);
        Assert.Empty(ByRole(svg, "schedule-row-label"));
        Assert.Empty(ByRole(svg, "legend-label"));
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void Render_AxisVisibilityDensityAndGridStyle_FollowSharedAxisControls() {
        var chart = CreateChart().WithSize(390, 300);
        var all = CreateChart().WithSize(390, 300);
        all.Options.XAxisLabelDensity = ChartLabelDensity.All;
        var automaticLabels = Texts(XDocument.Parse(chart.ToSvg()), "schedule-tick-label");
        var allLabels = Texts(XDocument.Parse(all.ToSvg()), "schedule-tick-label");
        Assert.True(automaticLabels.Length >= 2 && automaticLabels.Length < allLabels.Length);

        chart.Options.ShowXAxis = false;
        var hiddenX = XDocument.Parse(chart.ToSvg());
        Assert.Empty(ByRole(hiddenX, "schedule-tick-label"));
        Assert.Empty(ByRole(hiddenX, "schedule-axis"));
        Assert.Empty(ByRole(hiddenX, "gantt-lanes-now-label"));
        Assert.NotEmpty(ByRole(hiddenX, "schedule-row-label"));
        chart.Options.ShowXAxis = true;
        chart.Options.ShowYAxis = false;
        chart.ConfigureXAxis(axis => axis.ShowLine = false);
        var hiddenY = XDocument.Parse(chart.ToSvg());
        Assert.Empty(ByRole(hiddenY, "schedule-row-label"));
        Assert.Empty(ByRole(hiddenY, "gantt-lane-group"));
        Assert.Empty(ByRole(hiddenY, "schedule-axis"));
        Assert.NotEmpty(ByRole(hiddenY, "schedule-tick-label"));

        chart.WithGridStyle(style => { style.StrokeWidth = 3; style.VerticalOpacity = 0.8; style.Dash = 4; style.Gap = 6; });
        var grid = ByRole(XDocument.Parse(chart.ToSvg()), "schedule-grid");
        Assert.NotEmpty(grid);
        Assert.All(grid, line => { Assert.Equal(3, Number(line, "stroke-width")); Assert.Equal(0.8, Number(line, "opacity")); Assert.Equal("4 6", (string?)line.RenderedAttribute("stroke-dasharray")); });
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void Render_ExplicitAndSubsecondTicks_PreserveIdentityAndInteractiveLabels() {
        var chart = CreateChart();
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(Start.AddHours(5).ToOADate(), "Five hours"));
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Equal(new[] { "Five hours" }, Texts(svg, "schedule-tick-label"));
        Assert.All(ByRole(svg, "gantt-lane-item-label"), label =>
            Assert.Equal("none", (string?)label.Parent?.Attribute("pointer-events")));

        var shortChart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 300).WithXAxisTimeScale()
            .WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(Start, Start.AddMilliseconds(80), "low", "Brief") });
        var shortSvg = XDocument.Parse(shortChart.ToSvg());
        var tickLabels = Texts(shortSvg, "schedule-tick-label");
        Assert.True(tickLabels.Length > 1);
        Assert.Equal(tickLabels.Length, tickLabels.Distinct().Count());
        var item = Assert.Single(ByRole(shortSvg, "gantt-lane-item"));
        Assert.NotEqual((string?)item.Attribute("data-cfx-start"), (string?)item.Attribute("data-cfx-end"));
        Assert.Equal("80ms", (string?)item.Attribute("data-cfx-meta-duration"));
    }

    [Fact]
    public void Render_RepeatedHourMetadataAndSummaryHeader_StayUnambiguousAndBounded() {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var start = new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(390, 300).WithXAxisTimeScale(zone)
            .WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(start, start.AddHours(1), "low", "Repeated hour") }, summary: "1");
        chart.Options.StateTimelineSummaryHeader = "A very long summary header that cannot fit the reserved column";
        Assert.Equal(chart.Options.StateTimelineSummaryHeader, chart.Options.LaneSummaryHeader);
        var svg = XDocument.Parse(chart.ToSvg());
        var item = Assert.Single(ByRole(svg, "gantt-lane-item"));
        Assert.Contains("+02:00", (string?)item.Attribute("data-cfx-meta-start"));
        Assert.Contains("+01:00", (string?)item.Attribute("data-cfx-meta-end"));
        Assert.True(Assert.Single(Texts(svg, "lane-summary-header")).Length < chart.Options.LaneSummaryHeader!.Length);
        chart.Options.LaneSummaryHeader = "Incidents";
        Assert.Equal("Incidents", chart.Options.StateTimelineSummaryHeader);
    }

    [Fact]
    public void Render_UpperBoundOnly_BeforeAllItems_KeepsTheRequestedEmptyWindow() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 240).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(Start, Start.AddHours(2), "low") });
        chart.Options.XAxis.Maximum = Start.AddHours(-1).ToOADate();
        Assert.Empty(ByRole(XDocument.Parse(chart.ToSvg()), "gantt-lane-item"));
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void Validation_RejectsNonlinearGanttLaneAxes() {
        var chart = CreateChart();
        chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic;
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        Assert.Throws<InvalidOperationException>(() => chart.ToPng());
        chart.Options.XAxis.Scale = ChartScaleKind.SymmetricLogarithmic;
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
    }

    [Fact]
    public void Render_OpenItemStartingAtNow_IsAnInstantMark() {
        var now = Start.AddHours(1);
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 300).WithGanttLaneNow(now).WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Service", new[] {
                new ChartGanttLaneItem(Start, Start.AddHours(4), "low"),
                new ChartGanttLaneItem(now, null, "high")
            });
        var svg = XDocument.Parse(chart.ToSvg());
        var marker = Assert.Single(ByRole(svg, "gantt-lanes-now"));
        var open = Assert.Single(ByRole(svg, "gantt-lane-item"), item => (string?)item.Attribute("data-cfx-open") == "true");
        Assert.Equal(Number(marker, "x1"), Number(open, "x"), 2);
        Assert.InRange(Number(open, "width"), 2, 3);
        Assert.Equal("0s", (string?)open.Attribute("data-cfx-meta-duration"));
        Assert.Contains("0s", Title(open), StringComparison.Ordinal);
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void Render_CompactPlot_TrimsLongLocalizedNowLabel() {
        var label = new string('N', 160);
        var chart = CreateChart().WithSize(390, 300).WithLabels(labels => labels.Now = label);
        var svg = XDocument.Parse(chart.ToSvg());
        var shown = Assert.Single(Texts(svg, "gantt-lanes-now-label"));
        Assert.NotEmpty(shown);
        Assert.True(shown.Length < label.Length);
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void DateTimeBeforeOaEpoch_IsRejectedForItemsAndNow() {
        var beforeEpoch = new DateTime(1899, 12, 29, 6, 0, 0, DateTimeKind.Utc);
        Assert.Contains("1899-12-30", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ChartGanttLaneItem(beforeEpoch, beforeEpoch.AddHours(6), "low")).Message);
        Assert.Contains("1899-12-30", Assert.Throws<ArgumentOutOfRangeException>(() =>
            Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithGanttLaneNow(beforeEpoch)).Message);
    }

    [Fact]
    public void Render_SubmillisecondEndpoints_KeepDistinctMetadataAndDuration() {
        var first = Start.AddHours(12).AddTicks(1_000);
        var second = Start.AddHours(12).AddTicks(2_000);
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 300).WithXAxisTimeScale()
            .WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(first, second, "low") });
        var item = Assert.Single(ByRole(XDocument.Parse(chart.ToSvg()), "gantt-lane-item"));
        Assert.NotEqual((string?)item.Attribute("data-cfx-start"), (string?)item.Attribute("data-cfx-end"));
        Assert.Contains("12:00:00.0001", (string?)item.Attribute("data-cfx-meta-start"));
        Assert.Contains("12:00:00.0002", (string?)item.Attribute("data-cfx-meta-end"));
        Assert.Equal("100µs", (string?)item.Attribute("data-cfx-meta-duration"));
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void DateTimeIntervalBelowDisplayResolution_IsRejectedClearly() {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ChartGanttLaneItem(start, start.AddTicks(1), "low"));
        Assert.Contains("100-microsecond", error.Message);
    }

    [Fact]
    public void DateTimeAxisLabelAtSubmillisecondStart_RemainsVisible() {
        var start = Start.AddTicks(1_000);
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 300).WithXAxisTimeScale()
            .WithStateCategories(Status.SeverityCategories())
            .WithXLabels(new[] { new ChartAxisLabel(start, "Start") })
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(start, start.AddMinutes(1), "low") });
        Assert.Contains("Start", Texts(XDocument.Parse(chart.ToSvg()), "schedule-tick-label"));
    }

    private static Chart CreateChart() {
        ChartGanttLaneItem Incident(double from, double? to, string severity, string label, string? detail = null) =>
            new(Start.AddHours(from), to.HasValue ? Start.AddHours(to.Value) : null, severity, label, detail);
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(900, 480).WithXAxisTimeScale(showTimeZone: true).WithGanttToday(Start.AddHours(44))
            .WithStateCategories(Status.SeverityCategories())
            .AddGanttLane("LDAP", new[] { Incident(2, 5.5, "high", "Bind latency", "p95 above 250 ms") }, "Warsaw", "2")
            .AddGanttLane("Replication", new[] { Incident(8, 30, "critical", "USN rollback"), Incident(26, null, "medium", "Link flapping"), Incident(12, 16, "medium", "Backlog") }, "Warsaw", "3")
            .AddGanttLane("Certificates", new[] { Incident(1, 3, "info", "Drift") }, "London");
        chart.Options.LaneSummaryHeader = "Incidents";
        return chart;
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role
        && (role != "gantt-lane-item" || element.Descendants().Any(child => (string?)child.Attribute("data-cfx-role") == "gantt-lane-item-shape"))).ToArray();

    private static string[] Texts(XDocument svg, string role) => ByRole(svg, role).Select(element => element.Value).ToArray();

    private static string Title(XElement element) => element.Tooltip();

    private static double Number(XElement element, string attribute) => double.Parse((string)element.RenderedAttribute(attribute)!, CultureInfo.InvariantCulture);
}
