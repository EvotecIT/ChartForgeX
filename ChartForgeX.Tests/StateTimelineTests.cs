using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
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

        var segments = ByRole(svg, "state-segment");
        Assert.Equal(6, segments.Length);
        Assert.Equal(new[] { "up", "down", "up", "up", "notObservable", "up" }, segments.Select(segment => (string)segment.Attribute("data-cfx-status")!).ToArray());
        Assert.Equal(Down.ToCss(), (string)segments[1].Attribute("fill")!);
        Assert.Contains("DC01 · Down · 2026-09-25 06:00 UTC – 2026-09-25 09:00 UTC (3h) · LDAP bind failed", segments[1].Elements().Single(child => child.Name.LocalName == "title").Value, StringComparison.Ordinal);
        Assert.Equal(2, ByRole(svg, "state-segment-hatch").Length);

        Assert.Equal(new[] { "DC01", "DC02", "DC03" }, Texts(svg, "state-lane-label"));
        Assert.Equal(new[] { "87.5%", "100%" }, Texts(svg, "state-lane-summary"));
        Assert.Equal(new[] { "Available" }, Texts(svg, "state-summary-header"));
        Assert.Equal(new[] { "Up", "Down", "Not observable" }, Texts(svg, "state-legend-label"));
        Assert.Contains("06:00", Texts(svg, "state-timeline-tick-label"));
        Assert.Contains(">UTC</text>", CreateChart().ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void ToSvg_UncoveredTime_LeavesGapBetweenSegments() {
        var svg = XDocument.Parse(CreateChart().ToSvg());
        var lane = ByRole(svg, "state-segment").Where(segment => (string)segment.Attribute("data-cfx-series")! == "1").ToArray();
        Assert.Equal(2, lane.Length);
        var firstEnd = Number(lane[0], "x") + Number(lane[0], "width");
        Assert.True(Number(lane[1], "x") - firstEnd > 100, "Uncovered time between segments should stay empty.");
    }

    [Fact]
    public void ToSvg_ContiguousBucketsInSameState_DrawAsOneRun() {
        var buckets = Enumerable.Range(0, 4).Select(index => new ChartStateTimelineSegment(Day.AddHours(index), Day.AddHours(index + 1), "up")).ToList();
        buckets.Add(new ChartStateTimelineSegment(Day.AddHours(4), Day.AddHours(5), "up", "patched"));
        var chart = Chart.Create().WithSize(640, 240).WithStateCategories(new ChartStateCategory("up", "Up", Up)).AddStateTimelineLane("DC01", buckets);
        var segments = ByRole(XDocument.Parse(chart.ToSvg()), "state-segment");
        Assert.Equal(2, segments.Length);
        Assert.Equal(new[] { "0", "4" }, segments.Select(segment => (string)segment.Attribute("data-cfx-point")!).ToArray());
    }

    [Fact]
    public void ToPng_StateColoursAndGapsMatchSvgGeometry() {
        var chart = CreateChart();
        var svg = XDocument.Parse(chart.ToSvg());
        var down = ByRole(svg, "state-segment")[1];
        var image = PngReader.Decode(chart.ToPng());
        var scale = image.Width / (double)chart.Options.Size.Width;
        var centerX = (Number(down, "x") + Number(down, "width") / 2) * scale;
        var centerY = (Number(down, "y") + Number(down, "height") / 2) * scale;
        Assert.True(IsClose(Pixel(image, centerX, centerY), Down), "The PNG down segment should use the caller's state colour.");

        var gapLane = ByRole(svg, "state-segment").Where(segment => (string)segment.Attribute("data-cfx-series")! == "1").ToArray();
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
        var chart = Chart.Create().WithSize(640, 240).AddStateTimelineLane("DC01", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(2), "pending") });
        var segment = ByRole(XDocument.Parse(chart.ToSvg()), "state-segment").Single();
        Assert.Equal(chart.Options.Theme.MutedText.ToCss(), (string)segment.Attribute("fill")!);
        Assert.Equal("DC01 · pending", (string)segment.Attribute("data-cfx-label")!);
    }

    [Fact]
    public void ToSvg_DisplayTimeZone_ShiftsTickLabels() {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test/Plus2", TimeSpan.FromHours(2), "Test +02", "Test +02");
        var chart = CreateChart().WithXAxisTimeScale(zone, true, "CEST");
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Contains("2026-09-25 08:00 CEST", ByRole(svg, "state-segment")[1].Elements().Single(child => child.Name.LocalName == "title").Value, StringComparison.Ordinal);
        Assert.Contains("CEST", svg.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_RejectsInvalidSegmentsStatesAndEmptyCharts() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartStateTimelineSegment(Day, Day, "up"));
        Assert.Throws<ArgumentException>(() => new ChartStateTimelineSegment(Day, Day.AddHours(1), " "));
        Assert.Throws<ArgumentException>(() => Chart.Create().WithStateCategories(new ChartStateCategory("up", "Up", Up), new ChartStateCategory("up", "Again", Down)));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddStateTimelineLane("DC01", new[] { default(ChartStateTimelineSegment) }));
        var empty = Chart.Create().AddStateTimelineLane("DC01", Array.Empty<ChartStateTimelineSegment>());
        Assert.Throws<InvalidOperationException>(() => empty.ToSvg());
        Assert.Throws<InvalidOperationException>(() => empty.ToPng());
    }

    [Fact]
    public void ToSvg_ExplicitAxisBounds_ClipSegmentsAndKeepEmptyLanes() {
        var chart = CreateChart().AddStateTimelineLane("DC04", Array.Empty<ChartStateTimelineSegment>());
        chart.Options.XAxis.WithBounds(Day.AddHours(7).ToOADate(), Day.AddHours(20).ToOADate());
        var svg = XDocument.Parse(chart.ToSvg());
        var track = ByRole(svg, "state-lane-track")[0];
        var segments = ByRole(svg, "state-segment");
        Assert.Equal(4, ByRole(svg, "state-lane-track").Length);
        Assert.Equal(Number(track, "x"), Number(segments[0], "x"), 6);
        Assert.All(segments, segment => Assert.True(Number(segment, "x") + Number(segment, "width") <= Number(track, "x") + Number(track, "width") + 0.001));
        Assert.DoesNotContain(segments, segment => (string)segment.Attribute("data-cfx-point")! == "0" && (string)segment.Attribute("data-cfx-series")! == "1");
    }

    [Fact]
    public void Render_LegendAndAxesDisabled_OmitsThemInSvgAndPng() {
        var chart = CreateChart().WithLegend(false).WithAxes(false);
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Empty(ByRole(svg, "state-legend-label"));
        Assert.Empty(ByRole(svg, "state-lane-label"));
        Assert.Empty(ByRole(svg, "state-timeline-tick-label"));
        Assert.NotEqual(CreateChart().ToPng(), chart.ToPng());
    }

    [Fact]
    public void Render_ManyLanes_KeepsBandsInsideTheirSlots() {
        var chart = Chart.Create().WithSize(480, 240);
        for (var lane = 0; lane < 80; lane++) chart.AddStateTimelineLane("L" + lane.ToString(CultureInfo.InvariantCulture), new[] { new ChartStateTimelineSegment(Day, Day.AddHours(1), "up") });
        var tracks = ByRole(XDocument.Parse(chart.ToSvg()), "state-lane-track");
        for (var i = 1; i < tracks.Length; i++) Assert.True(Number(tracks[i], "y") >= Number(tracks[i - 1], "y") + Number(tracks[i - 1], "height"));
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
        Assert.Contains("data-cfx-role=\"state-segment\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-status=\"down\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-meta-duration=\"3h\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-meta-detail=\"LDAP bind failed\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-series-key=\"DC01\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"DC01 · Down · 2026-09-25 06:00 UTC", html, StringComparison.Ordinal);
        Assert.Contains("<script", html, StringComparison.Ordinal);
    }

    private static Chart CreateChart() {
        var chart = Chart.Create().WithSize(720, 300).WithXAxisTimeScale(showTimeZone: true)
            .WithStateCategories(
                new ChartStateCategory("up", "Up", Up),
                new ChartStateCategory("down", "Down", Down),
                new ChartStateCategory("notObservable", "Not observable", Neutral, hatched: true))
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
        chart.Options.StateTimelineSummaryHeader = "Available";
        return chart;
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static string[] Texts(XDocument svg, string role) => ByRole(svg, role).Select(element => element.Value).ToArray();

    private static double Number(XElement element, string attribute) => double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);

    private static (byte R, byte G, byte B) Pixel(RgbaImage image, double x, double y) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        return (image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2]);
    }

    private static bool IsClose((byte R, byte G, byte B) pixel, ChartColor color) =>
        Math.Abs(pixel.R - color.R) <= 12 && Math.Abs(pixel.G - color.G) <= 12 && Math.Abs(pixel.B - color.B) <= 12;
}
