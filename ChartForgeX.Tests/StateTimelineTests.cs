using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StateTimelineTests {
    private static readonly DateTime Day = new(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc);
    private static readonly ChartColor Up = ChartColor.FromHex("#1d8a52");
    private static readonly ChartColor Down = ChartColor.FromHex("#d4302f");
    private static readonly ChartColor Neutral = ChartColor.FromHex("#7c818a");

    [Fact]
    public void ToSvg_LanesWithStates_RendersSegmentsSummaryLegendAndTimeTicks() {
        var svg = XDocument.Parse(CreateChart().ToSvg());

        var segments = ByRole(svg, "state-timeline-segment");
        Assert.Equal(6, segments.Length);
        Assert.Equal(new[] { "up", "down", "up", "up", "notObservable", "up" }, segments.Select(segment => (string)segment.Attribute("data-cfx-status")!).ToArray());
        Assert.Equal(Down.ToCss(), (string)segments[1].RenderedAttribute("fill")!);
        Assert.Contains("DC01 · Down · 2026-09-25 06:00 UTC – 2026-09-25 09:00 UTC (3h) · LDAP bind failed", segments[1].Tooltip(), StringComparison.Ordinal);
        Assert.NotEmpty(ByRole(svg, "state-timeline-segment-shape-hatch"));
        Assert.NotEmpty(ByRole(svg, "legend-swatch-hatch"));

        Assert.Equal(new[] { "DC01", "DC02", "DC03" }, Texts(svg, "schedule-row-label"));
        Assert.Equal(new[] { "87.5%", "100%" }, Texts(svg, "lane-summary"));
        Assert.Equal(new[] { "Available" }, Texts(svg, "lane-summary-header"));
        Assert.Equal(new[] { "Up", "Down", "Not observable" }, Texts(svg, "legend-label"));
        Assert.Contains("06:00", Texts(svg, "schedule-tick-label"));
        Assert.Contains(">UTC</text>", CreateChart().ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void ToSvg_UncoveredTime_LeavesGapBetweenSegments() {
        var svg = XDocument.Parse(CreateChart().ToSvg());
        var lane = ByRole(svg, "state-timeline-segment").Where(segment => (string)segment.Attribute("data-cfx-series")! == "1").ToArray();
        Assert.Equal(2, lane.Length);
        var firstEnd = Number(lane[0], "x") + Number(lane[0], "width");
        Assert.True(Number(lane[1], "x") - firstEnd > 100, "Uncovered time between segments should stay empty.");
    }

    [Fact]
    public void ToSvg_ContiguousBucketsInSameState_DrawAsOneRun() {
        var buckets = Enumerable.Range(0, 4).Select(index => new ChartStateTimelineSegment(Day.AddHours(index), Day.AddHours(index + 1), "up")).ToList();
        buckets.Add(new ChartStateTimelineSegment(Day.AddHours(4), Day.AddHours(5), "up", "patched"));
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 240).WithStateCategories(new ChartStateCategory("up", "Up", Up)).AddStateTimelineLane("DC01", buckets);
        var segments = ByRole(XDocument.Parse(chart.ToSvg()), "state-timeline-segment");
        Assert.Equal(2, segments.Length);
        Assert.Equal(new[] { "0", "4" }, segments.Select(segment => (string)segment.Attribute("data-cfx-point")!).ToArray());
    }

    [Fact]
    public void ToPng_StateColoursAndGapsMatchSvgGeometry() {
        var chart = CreateChart();
        var svg = XDocument.Parse(chart.ToSvg());
        var down = ByRole(svg, "state-timeline-segment")[1];
        var image = PngReader.Decode(chart.ToPng());
        var scale = image.Width / (double)chart.Options.Size.Width;
        var centerX = (Number(down, "x") + Number(down, "width") / 2) * scale;
        var centerY = (Number(down, "y") + Number(down, "height") / 2) * scale;
        Assert.True(IsClose(Pixel(image, centerX, centerY), Down), "The PNG down segment should use the caller's state colour.");

        var gapLane = ByRole(svg, "state-timeline-segment").Where(segment => (string)segment.Attribute("data-cfx-series")! == "1").ToArray();
        var gapX = (Number(gapLane[0], "x") + Number(gapLane[0], "width") + 40) * scale;
        var gapY = (Number(gapLane[0], "y") + Number(gapLane[0], "height") / 2) * scale;
        Assert.False(IsClose(Pixel(image, gapX, gapY), Up), "The PNG gap should not be painted with a state colour.");
    }

    [Fact]
    public void Render_RepeatedCalls_AreByteIdentical() {
        Assert.Equal(CreateChart().ToSvg(), CreateChart().ToSvg());
        Assert.Equal(CreateChart().ToPng(), CreateChart().ToPng());
    }

    [Fact]
    public void ToSvg_UnregisteredState_UsesMutedColourAndRawKey() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(640, 240).AddStateTimelineLane("DC01", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(2), "pending") });
        var segment = ByRole(XDocument.Parse(chart.ToSvg()), "state-timeline-segment").Single();
        Assert.Equal(chart.Options.Theme.MutedText.ToCss(), (string)segment.RenderedAttribute("fill")!);
        Assert.Equal("DC01 · pending", (string)segment.Attribute("data-cfx-label")!);
    }

    [Fact]
    public void ToSvg_DisplayTimeZone_ShiftsTickLabels() {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test/Plus2", TimeSpan.FromHours(2), "Test +02", "Test +02");
        var chart = CreateChart().WithXAxisTimeScale(zone, true, "CEST");
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Contains("2026-09-25 08:00 CEST", ByRole(svg, "state-timeline-segment")[1].Tooltip(), StringComparison.Ordinal);
        Assert.Contains("CEST", svg.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_RejectsInvalidSegmentsStatesAndEmptyCharts() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartStateTimelineSegment(Day, Day, "up"));
        Assert.Throws<ArgumentException>(() => new ChartStateTimelineSegment(Day, Day.AddHours(1), " "));
        Assert.Throws<ArgumentException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithStateCategories(new ChartStateCategory("up", "Up", Up), new ChartStateCategory("up", "Again", Down)));
        Assert.Throws<ArgumentException>(() => Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).AddStateTimelineLane("DC01", new[] { default(ChartStateTimelineSegment) }));
        var empty = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).AddStateTimelineLane("DC01", Array.Empty<ChartStateTimelineSegment>());
        Assert.Throws<InvalidOperationException>(() => empty.ToSvg());
        Assert.Throws<InvalidOperationException>(() => empty.ToPng());
    }

    [Fact]
    public void ToSvg_ExplicitAxisBounds_ClipSegmentsAndKeepEmptyLanes() {
        var chart = CreateChart().AddStateTimelineLane("DC04", Array.Empty<ChartStateTimelineSegment>());
        chart.Options.XAxis.WithBounds(Day.AddHours(7).ToOADate(), Day.AddHours(20).ToOADate());
        var svg = XDocument.Parse(chart.ToSvg());
        var lanes = Lanes(chart); var track = lanes[0].Bounds;
        var segments = ByRole(svg, "state-timeline-segment");
        Assert.Equal(4, lanes.Length);
        Assert.Equal(track.Left, Number(segments[0], "x"), 3);
        Assert.All(segments, segment => Assert.True(Number(segment, "x") + Number(segment, "width") <= track.Right + 0.001));
        Assert.DoesNotContain(segments, segment => (string)segment.Attribute("data-cfx-point")! == "0" && (string)segment.Attribute("data-cfx-series")! == "1");
    }

    [Fact]
    public void Render_LegendAndAxesDisabled_OmitsThemInSvgAndPng() {
        var chart = CreateChart().WithLegend(false).WithAxes(false);
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Empty(ByRole(svg, "legend-label"));
        Assert.Empty(ByRole(svg, "schedule-row-label"));
        Assert.Empty(ByRole(svg, "schedule-tick-label"));
        Assert.NotEqual(CreateChart().ToPng(), chart.ToPng());
    }

    [Fact]
    public void Render_ManyLanes_KeepsBandsInsideTheirSlots() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(480, 240);
        for (var lane = 0; lane < 80; lane++) chart.AddStateTimelineLane("L" + lane.ToString(CultureInfo.InvariantCulture), new[] { new ChartStateTimelineSegment(Day, Day.AddHours(1), "up") });
        var tracks = Lanes(chart);
        Assert.Equal(80, tracks.Length);
        for (var i = 1; i < tracks.Length; i++) Assert.True(tracks[i].Bounds.Top >= tracks[i - 1].Bounds.Bottom);
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void Segment_LocalDateTime_IsConvertedToUtc() {
        var local = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Local);
        var segment = new ChartStateTimelineSegment(local, local.AddHours(1), "up");
        Assert.Equal(local.ToUniversalTime().ToOADate(), segment.Start, 9);
    }

    [Fact]
    public void Validation_RejectsNullAndDuplicateStatesAddedDirectly() {
        var withNull = CreateChart();
        withNull.Options.StateCategories.Add(null!);
        Assert.Throws<InvalidOperationException>(() => withNull.ToSvg());
        var duplicate = CreateChart();
        duplicate.Options.StateCategories.Add(new ChartStateCategory("up", "Again", Down));
        Assert.Throws<InvalidOperationException>(() => duplicate.ToPng());
    }

    [Fact]
    public void ToInteractiveHtmlFragment_SegmentsExposeHoverTargets() {
        var html = CreateChart().ToInteractiveHtmlFragment();
        Assert.Contains("data-cfx-role=\"state-timeline-segment\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-status=\"down\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-meta-duration=\"3h\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-meta-detail=\"LDAP bind failed\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-series-key=\"DC01\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"DC01 · Down · 2026-09-25 06:00 UTC", html, StringComparison.Ordinal);
        Assert.Contains("<script", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_XAxisHidden_HidesOnlyTimeAxisAndReclaimsSpace(bool configureAxis)
    {
        var shown = CreateChart();
        var hidden = CreateChart();
        if (configureAxis) hidden.ConfigureXAxis(axis => axis.Visible = false);
        else hidden.Options.ShowXAxis = false;
        var svg = XDocument.Parse(hidden.ToSvg());
        Assert.Empty(ByRole(svg, "schedule-tick-label"));
        Assert.Empty(ByRole(svg, "schedule-axis"));
        Assert.Empty(ByRole(svg, "schedule-x-axis-title"));
        Assert.Equal(3, ByRole(svg, "schedule-row-label").Length);
        var bounds = new ChartForgeX.Primitives.ChartRect(0, 0, 600, 240);
        var a = ChartStateTimelineModel.Build(shown).PlotArea(bounds, 30, 30, 12, 20);
        var z = ChartStateTimelineModel.Build(hidden).PlotArea(bounds, 30, 30, 12, 20);
        Assert.True(z.Height > a.Height);
        Assert.NotEqual(shown.ToPng(), hidden.ToPng());
    }

    [Fact]
    public void Render_SubSecondTicks_RemainDistinctAndExactLabelsWin()
    {
        var end = Day.AddMilliseconds(80);
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 300).WithXAxisTimeScale()
            .WithStateCategories(new ChartStateCategory("up", "Up", Up))
            .AddStateTimelineLane("DC", new[] { new ChartStateTimelineSegment(Day, end, "up") });
        var model = ChartStateTimelineModel.Build(chart);
        var labels = Texts(XDocument.Parse(chart.ToSvg()), "schedule-tick-label");
        Assert.True(labels.Length > 1);
        Assert.Equal(labels.Length, labels.Distinct().Count());
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(Day.ToOADate(), "Start"));
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(end.ToOADate(), "End"));
        Assert.Equal("End", ChartStateTimelineModel.Build(chart).FormatTick(end.ToOADate()));
    }

    [Fact]
    public void Render_LongSummaryHeader_FitsReservedColumn()
    {
        var chart = CreateChart();
        chart.Options.LaneSummaryHeader = "Very long summary header that must stay outside the lane plot";
        var header = Texts(XDocument.Parse(chart.ToSvg()), "lane-summary-header").Single();
        Assert.NotEqual(chart.Options.LaneSummaryHeader, header);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var column = Assert.Single(prepared.Regions, region => region.Role == "lane-summary-header");
        var text = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "lane-summary-header");
        Assert.Equal(chart.Options.LaneSummaryHeader, column.Label);
        Assert.All(text.Text.Lines, line => Assert.True(line.Width <= column.Bounds.Width + .001));
    }

    [Fact]
    public void Render_ConfiguredVerticalGridStyle_AppliesToBothOutputs()
    {
        var chart = CreateChart();
        chart.WithGridStyle(style => { style.StrokeWidth = 3; style.VerticalOpacity = 0.8; style.Dash = 4; style.Gap = 6; });
        var lines = ByRole(XDocument.Parse(chart.ToSvg()), "schedule-grid");
        Assert.NotEmpty(lines);
        Assert.All(lines, line => { Assert.Equal(3, Number(line, "stroke-width")); Assert.Equal("4 6", (string?)line.RenderedAttribute("stroke-dasharray")); });
        var preparedGrid = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "schedule-grid");
        Assert.All(preparedGrid, line => Assert.Equal((byte)204, line.Stroke!.Value.A));
        Assert.NotEqual(CreateChart().ToPng(), chart.ToPng());
    }

    [Fact]
    public void Render_VerticalGridDisabled_MatchesGlobalGridDisabled()
    {
        var vertical = CreateChart().WithGridStyle(style => style.ShowVerticalLines = false);
        var global = CreateChart(); global.Options.ShowGrid = false;
        Assert.Empty(ByRole(XDocument.Parse(vertical.ToSvg()), "schedule-grid"));
        Assert.NotEmpty(ByRole(XDocument.Parse(vertical.ToSvg()), "schedule-row-grid"));
        vertical.WithGridStyle(style => style.ShowHorizontalLines = false);
        Assert.Equal(global.ToPng(), vertical.ToPng());
    }

    [Fact]
    public void Render_YAxisHidden_ReclaimsLaneLabelColumnButKeepsTimeAxis() {
        var shown = CreateChart();
        var hidden = CreateChart().ConfigureYAxis(axis => axis.Visible = false);
        var svg = XDocument.Parse(hidden.ToSvg());
        Assert.Empty(ByRole(svg, "schedule-row-label"));
        Assert.NotEmpty(ByRole(svg, "schedule-tick-label"));
        var a = Lanes(shown)[0].Bounds; var b = Lanes(hidden)[0].Bounds;
        Assert.True(b.Left < a.Left);
        Assert.NotEqual(shown.ToPng(), hidden.ToPng());
    }

    [Fact]
    public void Render_XAxisLineHidden_KeepsTickLabelsAndTitle() {
        var chart = CreateChart().ConfigureXAxis(axis => axis.ShowLine = false);
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Empty(ByRole(svg, "schedule-axis"));
        Assert.NotEmpty(ByRole(svg, "schedule-tick-label"));
        Assert.NotEqual(CreateChart().ToPng(), chart.ToPng());
    }

    [Fact]
    public void Render_SubSecondSegment_MetadataRetainsEndpointsAndDuration() {
        var end = Day.AddMilliseconds(80);
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 300).WithXAxisTimeScale()
            .WithStateCategories(new ChartStateCategory("up", "Up", Up))
            .AddStateTimelineLane("DC", new[] { new ChartStateTimelineSegment(Day, end, "up") });
        var segment = ByRole(XDocument.Parse(chart.ToSvg()), "state-timeline-segment").Single();
        Assert.NotEqual((string?)segment.Attribute("data-cfx-start"), (string?)segment.Attribute("data-cfx-end"));
        Assert.Equal("80ms", (string?)segment.Attribute("data-cfx-meta-duration"));
    }

    [Fact]
    public void Render_CompactTimeline_UsesAxisLabelDensity() {
        var auto = CreateChart().WithSize(390, 300);
        var all = CreateChart().WithSize(390, 300);
        all.Options.XAxisLabelDensity = ChartLabelDensity.All;
        var a = Texts(XDocument.Parse(auto.ToSvg()), "schedule-tick-label");
        var b = Texts(XDocument.Parse(all.ToSvg()), "schedule-tick-label");
        Assert.True(a.Length >= 2 && a.Length < b.Length);
        Assert.Equal(b[0], a[0]);
        Assert.Equal(b[b.Length - 1], a[a.Length - 1]);
        Assert.NotEqual(auto.ToPng(), all.ToPng());
    }

    [Fact]
    public void Render_DefaultDateTimeLane_UsesDistinctCalendarLabelsWithoutMutatingAxis() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 300)
            .AddStateTimelineLane("DC", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(6), "up") });
        var labels = Texts(XDocument.Parse(chart.ToSvg()), "schedule-tick-label");
        Assert.True(labels.Length > 1);
        Assert.Equal(labels.Length, labels.Distinct().Count());
        Assert.Contains(labels, label => label.Contains(":"));
        Assert.Equal(ChartScaleKind.Linear, chart.Options.XAxis.Scale);
    }

    [Theory]
    [InlineData(-45)]
    [InlineData(45)]
    public void Render_ConfiguredTickRotation_AppliesToBothOutputsAndReservesHeight(double angle) {
        var normal = CreateChart();
        var rotated = CreateChart().ConfigureXAxis(axis => axis.LabelAngle = angle);
        var svg = XDocument.Parse(rotated.ToSvg());
        Assert.All(ByRole(svg, "schedule-tick-label"), label => Assert.Contains("rotate(" + angle.ToString(CultureInfo.InvariantCulture), (string?)label.RenderedAttribute("transform") ?? ""));
        Assert.True(Lanes(rotated).Last().Bounds.Bottom < Lanes(normal).Last().Bounds.Bottom);
        Assert.NotEqual(normal.ToPng(), rotated.ToPng());
    }

    [Fact]
    public void Render_DefaultDateTimeLane_SvgEndpointAnchorsMatchActualTextEdges() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 300)
            .AddStateTimelineLane("Default DateTime lane", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(6), "up") });
        var svg = XDocument.Parse(chart.ToSvg());
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = prepared.Regions.Single(region => region.Role == "schedule-plot").Bounds;
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "schedule-tick-label").ToArray();
        Assert.True(labels.Length >= 2);
        Assert.All(labels, label => Assert.All(label.Text.Lines, line => {
            Assert.InRange(label.LineLeft(line), plot.Left - .001, plot.Right);
            Assert.InRange(label.LineLeft(line) + line.Width, plot.Left, plot.Right + .001);
        }));
    }

    [Theory]
    [InlineData(390, 300)]
    [InlineData(720, 400)]
    public void Render_ManyStateCategories_BudgetsLegendAndKeepsVisibleLane(int width, int height) {
        var states = Enumerable.Range(0, 40).Select(i => new ChartStateCategory("s" + i, "Long category " + i, Up)).ToArray();
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(width, height).WithStateCategories(states)
            .AddStateTimelineLane("Lane", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(6), "s0") });
        var svg = XDocument.Parse(chart.ToSvg());
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var overflow = prepared.Regions.Single(region => region.Id == "legend-overflow");
        Assert.Contains("more entries", overflow.Label);
        Assert.NotEmpty(ByRole(svg, "legend-entry-omitted"));
        var track = prepared.Regions.Single(region => region.Role == "schedule-lane").Bounds;
        var plot = prepared.Regions.Single(region => region.Role == "schedule-plot").Bounds;
        Assert.True(track.Height >= 18);
        var swatches = ByRole(svg, "state-legend-swatch");
        Assert.NotEmpty(swatches);
        Assert.All(swatches, item => {
            Assert.InRange(Number(item, "y"), chart.Options.Padding.Top, plot.Top);
            Assert.True(Number(item, "y") + Number(item, "height") <= plot.Top);
        });
        var legends = prepared.Regions.Where(region => region.Role == "legend" && region.Bounds.Height > 0).ToArray();
        Assert.NotEmpty(legends);
        Assert.All(legends, legend => Assert.True(legend.Bounds.Bottom <= plot.Top));
        Assert.True(overflow.Bounds.Bottom <= plot.Top);
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_OneSidedBoundOutsideSegments_PreservesBoundAndEmptyWindow(bool maximum) {
        var chart = CreateChart();
        var bound = maximum ? Day.AddHours(-1).ToOADate() : Day.AddDays(2).ToOADate();
        if (maximum) chart.Options.XAxis.Maximum = bound;
        else chart.Options.XAxis.Minimum = bound;
        var model = ChartStateTimelineModel.Build(chart);
        Assert.Equal(bound, maximum ? model.Max : model.Min);
        Assert.True(model.Max > model.Min);
        Assert.Empty(ByRole(XDocument.Parse(chart.ToSvg()), "state-timeline-segment"));
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(false, ChartLegendPosition.TopLeft)]
    [InlineData(true, ChartLegendPosition.TopLeft)]
    [InlineData(false, ChartLegendPosition.Bottom)]
    [InlineData(true, ChartLegendPosition.Bottom)]
    public void Render_LargeAxisStyles_KeepTicksTitleAndLegendSeparated(bool largeTicks, ChartLegendPosition position) {
        var chart = CreateChart().WithSize(720, 500).WithXAxis("Time");
        chart.WithLegendPosition(position);
        if (largeTicks) chart.Options.TickLabelStyle.FontSize = 32;
        chart.Options.XAxis.LabelFormatter = _ => "T";
        chart.Options.AxisTitleStyle.FontSize = 42;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = prepared.Regions.Single(region => region.Role == "schedule-plot").Bounds;
        Assert.True(plot.Height >= 18);
        var ticks = VisibleTextBounds(prepared, "schedule-tick-label");
        var title = Assert.Single(VisibleTextBounds(prepared, "schedule-x-axis-title"));
        Assert.NotEmpty(ticks);
        Assert.All(ticks, tick => {
            Assert.True(tick.Top >= plot.Bottom);
            Assert.True(tick.Bottom <= title.Top);
        });
        var legends = prepared.Regions.Where(region => region.Role == "legend" && region.Bounds.Height > 0).ToArray();
        Assert.NotEmpty(legends);
        Assert.All(legends, legend => {
            Assert.True(position == ChartLegendPosition.Bottom ? title.Bottom <= legend.Bounds.Top : legend.Bounds.Bottom <= plot.Top);
        });
        Assert.True(title.Bottom <= chart.Options.Size.Height - chart.Options.Padding.Bottom);
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void Render_LargeLegendWithoutAxes_KeepsDefaultTopLegendAboveVisibleLanes() {
        var chart = CreateChart().WithSize(390, 400).WithAxes(false);
        chart.Options.LegendStyle.FontSize = 32;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var lanes = prepared.Regions.Where(region => region.Role == "schedule-lane").ToArray();
        var firstLaneTop = lanes.Min(region => region.Bounds.Top);
        Assert.All(lanes, lane => Assert.True(lane.Bounds.Height >= 18));
        var labels = VisibleTextBounds(prepared, "legend-label");
        Assert.NotEmpty(labels);
        Assert.All(labels, label => {
            Assert.True(label.Top >= chart.Options.Padding.Top);
            Assert.True(label.Bottom < firstLaneTop);
        });
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(-1e308, 1e308)]
    [InlineData(null, -1e20)]
    [InlineData(1e20, null)]
    public void Render_FiniteNumericBounds_UseFiniteNonzeroWindow(double? minimum, double? maximum) {
        var chart = CreateChart().ConfigureXAxis(axis => { axis.Minimum = minimum; axis.Maximum = maximum; });
        var model = ChartStateTimelineModel.Build(chart);
        Assert.True(model.Max > model.Min);
        var plot = new ChartRect(10, 10, 500, 100);
        Assert.Equal(plot.Left, model.X(model.Min, plot), 6);
        Assert.Equal(plot.Right, model.X(model.Max, plot), 6);
        Assert.DoesNotContain("NaN", chart.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Render_OneSidedNumericLimit_RejectsAnUnrepresentableWindow(bool lowerBound) {
        var chart = CreateChart();
        if (lowerBound) chart.Options.XAxis.Minimum = double.MaxValue;
        else chart.Options.XAxis.Maximum = -double.MaxValue;
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartStateTimelineModel.Build(chart));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ToSvg());
    }

    private static Chart CreateChart() {
        // Tall enough for three readable lanes below the legend and summary header, with space for the axis and its title.
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(720, 360).WithXAxisTimeScale(showTimeZone: true)
            .WithStateCategories(
                new ChartStateCategory("up", "Up", Up),
                new ChartStateCategory("down", "Down", Down),
                new ChartStateCategory("notObservable", "Not observable", Neutral, ChartStatePattern.Hatched))
            .AddStateTimelineLane("DC01", new[] {
                new ChartStateTimelineSegment(Day, Day.AddHours(6), "up"),
                new ChartStateTimelineSegment(Day.AddHours(6), Day.AddHours(9), "down", "LDAP bind failed"),
                new ChartStateTimelineSegment(Day.AddHours(9), Day.AddHours(24), "up")
            }, "87.5%")
            .AddStateTimelineLane("DC02", new[] {
                new ChartStateTimelineSegment(Day, Day.AddHours(6), "up"),
                new ChartStateTimelineSegment(Day.AddHours(12), Day.AddHours(24), "notObservable")
            }, "100%")
            .AddStateTimelineLane("DC03", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(24), "up") });
        chart.Options.LaneSummaryHeader = "Available";
        return chart;
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role
        && (role != "state-timeline-segment" || element.Descendants().Any(child => (string?)child.Attribute("data-cfx-role") == "state-timeline-segment-shape"))).ToArray();

    private static string[] Texts(XDocument svg, string role) => ByRole(svg, role).Select(element => element.Value).ToArray();

    private static VisualSemanticRegion[] Lanes(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context)
        .Regions.Where(region => region.Role == "schedule-lane").ToArray();

    private static ChartRect[] VisibleTextBounds(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role == role).Select(text => new ChartRect(text.Text.Lines.Min(text.LineLeft),
            text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height)).ToArray();

    private static double Number(XElement element, string attribute) => double.Parse((string)element.RenderedAttribute(attribute)!, CultureInfo.InvariantCulture);

    private static (byte R, byte G, byte B) Pixel(RgbaImage image, double x, double y) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        return (image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2]);
    }

    private static bool IsClose((byte R, byte G, byte B) pixel, ChartColor color) =>
        Math.Abs(pixel.R - color.R) <= 12 && Math.Abs(pixel.G - color.G) <= 12 && Math.Abs(pixel.B - color.B) <= 12;
}
