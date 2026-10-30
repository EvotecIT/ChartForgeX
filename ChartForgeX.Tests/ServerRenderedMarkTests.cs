using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ServerRenderedMarkTests {
    private static readonly DateTime Day = new(2026, 10, 27);

    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Scatter)]
    public void MarksOutsideTheSharedDomainLeaveNoInvisibleKeyboardLink(ChartSeriesKind kind) {
        var chart = Chart.Create().WithYAxisBounds(0, 2).AddSeries(new ChartSeries(new ChartSeriesId("points"), "Points", kind, new[] {
            new ChartPoint(5, 1).WithMarkOptions(Linked("/inside")),
            new ChartPoint(20, 1).WithMarkOptions(Linked("/outside"))
        }));
        var scale = new ChartStripScale(new ChartAxis { Minimum = 0, Maximum = 10 });
        var prepared = chart.PrepareStrip(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(600, 80), 0)), scale);
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Single(document.Descendants(), element => (string?)element.Attribute("href") == "/inside");
        Assert.DoesNotContain(document.Descendants(), element => (string?)element.Attribute("href") == "/outside");
    }

    [Theory]
    [InlineData(false, null)] [InlineData(true, null)]
    [InlineData(false, "")] [InlineData(true, "")]
    [InlineData(false, " ")] [InlineData(true, " ")]
    public void HiddenLabelsAndBlankSummariesLeaveNoUnpaintedRowLink(bool gantt, string? emptySummary) {
        var chart = Categories(Chart.Create()).WithAxes(false).WithSize(600, 240);
        foreach (var summary in new[] { "Support", emptySummary }) {
            if (gantt) chart.AddGanttLane("Release", new[] { new ChartGanttLaneItem(Day, Day.AddDays(2), "up") }, summary: summary);
            else chart.AddStateTimelineLane("Availability", new[] { new ChartStateTimelineSegment(Day, Day.AddDays(2), "up") }, summary: summary);
        }
        chart.Series[0].WithRowOptions(new ChartMarkOptions(href: "/visible"));
        chart.Series[1].WithRowOptions(new ChartMarkOptions(href: "/empty"));
        var document = XDocument.Parse(chart.ToSvg());
        var link = Assert.Single(document.Descendants(), element => element.Name.LocalName == "a");
        Assert.Equal("/visible", (string?)link.Attribute("href"));
        Assert.Contains(link.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Support");
    }

    [Theory]
    [InlineData(VisualSvgLinkTarget.SameContext, VisualSvgLinkTarget.NewContext, "_self")]
    [InlineData(VisualSvgLinkTarget.NewContext, VisualSvgLinkTarget.SameContext, "_blank")]
    public void LegacyCellNavigationUsesItsNewMarkPolicyWithoutRepeatingTheUrl(VisualSvgLinkTarget target, VisualSvgLinkTarget renderTarget, string expectedTarget) {
        var cell = new ChartHeatmapCell("up", href: "/incident").WithMarkOptions(
            new ChartMarkOptions(linkTarget: target, accessibleLabel: "Incident details", cssClass: "host-cell"));
        var chart = Categories(Chart.Create()).AddSeries(ChartSeries.CreateHeatmapCategoryRow(
            new ChartSeriesId("incidents"), "Incidents", new ChartHeatmapCell?[] { cell }));
        var document = XDocument.Parse(chart.Prepare(new VisualRenderContext()).ToSvg(new VisualSvgOptions(linkTarget: renderTarget)));
        var link = Assert.Single(document.Descendants(), node => node.Name.LocalName == "a");
        Assert.Equal("/incident", (string?)link.Attribute("href"));
        Assert.Equal(expectedTarget, (string?)link.Attribute("target"));
        Assert.Equal("host-cell", (string?)link.Attribute("class"));
        Assert.Equal("heatmap-cell", (string?)link.Attribute("data-cfx-role"));
        Assert.Equal("Incident details", (string?)link.Attribute("aria-label"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdjacentStateIntervalsKeepItemNavigationOrAnHonestSharedRunDefault(bool itemPolicies) {
        var first = new ChartStateTimelineSegment(Day, Day.AddDays(1), "up");
        var second = new ChartStateTimelineSegment(Day.AddDays(1), Day.AddDays(2), "up");
        if (itemPolicies) {
            first = first.WithMarkOptions(Linked("/incident/a"));
            second = second.WithMarkOptions(Linked("/incident/b"));
        }
        var chart = Categories(Chart.Create()).AddStateTimelineLane("Availability", new[] { first, second });
        chart.Series[0].WithMarkOptions(Linked("/incidents"));
        var document = XDocument.Parse(chart.ToSvg());
        var links = document.Descendants().Where(element => element.Name.LocalName == "a").ToArray();
        Assert.Equal(itemPolicies ? new[] { "/incident/a", "/incident/b" } : new[] { "/incidents" },
            links.Select(element => (string)element.Attribute("href")!).ToArray());
        Assert.All(links, link => {
            Assert.Equal("Release 42, current support period", (string?)link.Attribute("aria-label"));
            Assert.Equal("true", (string?)link.Attribute("aria-current"));
        });
        if (!itemPolicies) {
            Assert.Equal("0,1", (string?)links[0].Attribute("data-cfx-source-points"));
            Assert.Null(links[0].Attribute("data-cfx-observation-id"));
        }
    }

    private static ChartMarkOptions Linked(string href = "/record/42", VisualSvgLinkTarget target = VisualSvgLinkTarget.NewContext) =>
        new(href, target, "Open <release> & details", "Release 42, current support period", "release-mark host-accent",
            new Dictionary<string, string> { ["data-record-id"] = "42\"<&" }, current: true, selected: true);

    [Theory]
    [InlineData("column")]
    [InlineData("bar")]
    [InlineData("stacked-column")]
    [InlineData("scatter")]
    [InlineData("line")]
    [InlineData("timeline")]
    [InlineData("gantt")]
    [InlineData("gantt-marker")]
    [InlineData("gantt-lane")]
    [InlineData("state-timeline")]
    [InlineData("calendar")]
    [InlineData("range-bar")]
    [InlineData("range-band")]
    [InlineData("range-area")]
    [InlineData("dumbbell")]
    public void AuthoredMarksEmitSafeAccessibleNavigationAndVerbatimHostHooks(string family) {
        var chart = MarkChart(family, Linked());
        var prepared = chart.Prepare(new VisualRenderContext());
        var document = XDocument.Parse(prepared.ToSvg());
        var link = Assert.Single(document.Descendants(), node => (string?)node.Attribute("href") == "/record/42");
        Assert.Equal("a", link.Name.LocalName);
        Assert.Equal("_blank", (string?)link.Attribute("target"));
        Assert.Equal("noopener noreferrer", (string?)link.Attribute("rel"));
        Assert.Equal("0", (string?)link.Attribute("tabindex"));
        Assert.Equal("true", (string?)link.Attribute("focusable"));
        Assert.Equal("link", (string?)link.Attribute("role"));
        Assert.Equal("Release 42, current support period", (string?)link.Attribute("aria-label"));
        Assert.Equal("Open <release> & details", Assert.Single(link.Elements(), node => node.Name.LocalName == "title").Value);
        Assert.Equal("release-mark host-accent", (string?)link.Attribute("class"));
        Assert.Equal("42\"<&", (string?)link.Attribute("data-record-id"));
        Assert.Equal("true", (string?)link.Attribute("aria-current"));
        Assert.Equal("true", (string?)link.Attribute("data-cfx-selected"));
        Assert.Null(link.Attribute("aria-selected"));
        Assert.Contains(link.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "mark-selected-outline");
        Assert.Contains(link.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "mark-current-outline");
        Assert.DoesNotContain(link.Descendants(), node => node.Name.LocalName == "a");
        Assert.Equal("group", (string?)document.Root!.Attribute("role"));
        Assert.Contains(prepared.Regions, region => region.Label == "Release 42, current support period");
    }

    [Theory]
    [InlineData(SparklineStyle.Line)]
    [InlineData(SparklineStyle.Area)]
    [InlineData(SparklineStyle.Bars)]
    public void SparklineMetadataUsesOriginalSlotsAcrossMissingValues(SparklineStyle style) {
        var original = new SparklineData(new double?[] { 2, null, 5 });
        var data = original.WithPointOptions(2, Linked());
        Assert.Null(original.PointOptions[2]);
        var document = XDocument.Parse(new Sparkline(data).WithStyle(style).Prepare(new VisualRenderContext()).ToSvg());
        var link = Assert.Single(document.Descendants(), node => (string?)node.Attribute("href") == "/record/42");
        Assert.Equal("3", (string?)link.Attribute("data-cfx-x"));
        Assert.Equal("5", (string?)link.Attribute("data-cfx-y"));
        Assert.Equal("42\"<&", (string?)link.Attribute("data-record-id"));
    }

    [Fact]
    public void PerLinkSameContextOverridesExportDefaultAndUnlinkedMarksStayUnlinked() {
        var chart = Chart.Create().AddBar("Releases", new[] {
            new ChartPoint(1, 4).WithMarkOptions(new ChartMarkOptions(href: "#first")),
            new ChartPoint(2, 7)
        });
        var prepared = chart.Prepare(new VisualRenderContext());
        var document = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions(linkTarget: VisualSvgLinkTarget.NewContext)));
        var link = Assert.Single(document.Descendants(), node => node.Name.LocalName == "a");
        Assert.Equal("#first", (string?)link.Attribute("href"));
        Assert.Equal("_self", (string?)link.Attribute("target"));
        Assert.Null(link.Attribute("rel"));
        Assert.False(string.IsNullOrWhiteSpace((string?)link.Attribute("aria-label")));
    }

    [Theory]
    [InlineData(true, false, "mark-current-outline")]
    [InlineData(false, true, "mark-selected-outline")]
    public void StaticPageStatesRetainLinkSemanticsAndDistinctVisibleOutlines(bool current, bool selected, string outline) {
        var point = new ChartPoint(1, 2).WithMarkOptions(new ChartMarkOptions(href: "/day", current: current, selected: selected));
        var document = XDocument.Parse(Chart.Create().AddBar("Updates", new[] { point }).ToSvg());
        var link = Assert.Single(document.Descendants(), node => (string?)node.Attribute("href") == "/day");
        Assert.Equal("link", (string?)link.Attribute("role"));
        Assert.Equal("true", (string?)link.Attribute("aria-current"));
        Assert.Null(link.Attribute("aria-selected"));
        Assert.Contains(link.Descendants(), node => (string?)node.Attribute("data-cfx-role") == outline);
        Assert.Equal(selected ? "true" : null, (string?)link.Attribute("data-cfx-selected"));
    }

    [Fact]
    public void SeriesDefaultsAndRowHooksDoNotNestLinksOrReplaceItemOptions() {
        var chart = MarkChart("gantt-lane", new ChartMarkOptions(href: "#item", cssClass: "item"));
        chart.Series[0].WithMarkOptions(new ChartMarkOptions(href: "#default", cssClass: "series"))
            .WithRowOptions(new ChartMarkOptions(href: "#row", cssClass: "row", current: true));
        var document = XDocument.Parse(chart.ToSvg());
        var item = Assert.Single(document.Descendants(), node => (string?)node.Attribute("href") == "#item");
        var row = Assert.Single(document.Descendants(), node => (string?)node.Attribute("href") == "#row");
        Assert.Equal("item", (string?)item.Attribute("class"));
        Assert.Equal("row", (string?)row.Attribute("class"));
        Assert.Equal("true", (string?)row.Attribute("aria-current"));
        Assert.DoesNotContain(row.Descendants(), node => node.Name.LocalName == "a");
        Assert.DoesNotContain(document.Descendants(), node => (string?)node.Attribute("href") == "#default");
    }

    [Fact]
    public void CalendarDuplicateDatesKeepLastExplicitMetadataAndDoNotLinkEmptyDays() {
        var chart = Chart.Create().AddCalendarHeatmap("Updates", new[] {
            new ChartCalendarHeatmapItem(Day, 2).WithMarkOptions(Linked()),
            new ChartCalendarHeatmapItem(Day, 3)
        });
        Assert.Equal(5, Assert.Single(chart.Series[0].CalendarItems).Value);
        var link = Assert.Single(XDocument.Parse(chart.ToSvg()).Descendants(), node => node.Name.LocalName == "a");
        Assert.Equal("2026-10-27", (string?)link.Attribute("data-cfx-date"));
        Assert.Equal("false", (string?)link.Attribute("data-cfx-empty"));
    }

    [Fact]
    public void ImmutableOptionsDetachHostDictionaryAndPointColorCopiesRetainMetadata() {
        var hooks = new Dictionary<string, string> { ["data-key"] = "before" };
        var options = new ChartMarkOptions(dataAttributes: hooks);
        hooks["data-key"] = "after";
        var point = new ChartPoint(1, 2).WithMarkOptions(options).WithColorValue(7);
        Assert.Equal("before", options.DataAttributes["data-key"]);
        Assert.Same(options, point.MarkOptions);
        Assert.Equal(7, point.ColorValue);
        Assert.Null(new ChartPoint(1, 2).MarkOptions);
    }

    [Fact]
    public void GanttOpenEndAndMetadataSurviveNormalizationAndUpdate() {
        var item = ChartGanttItem.OpenEnded(Day, .4).WithMarkOptions(Linked());
        Assert.Equal(item.Start, item.End);
        var series = ChartSeries.CreateGanttTask(new ChartSeriesId("support"), "Support", 1, 2);
        series.ReplaceGanttItems(new[] { item });
        series.UpdateGanttItem(0, series.GanttItems[0]);
        var retained = Assert.Single(series.GanttItems);
        Assert.True(retained.IsOpenEnded);
        Assert.Equal(.4, retained.Progress);
        Assert.Same(item.MarkOptions, retained.MarkOptions);
        Assert.Equal(item.Start, retained.End);
    }

    [Fact]
    public void GanttOpenEndedRejectsFixedEndsMilestonesAndVerticalMarkers() {
        Assert.Throws<ArgumentException>(() => new ChartGanttItem(1, 2, isOpenEnded: true));
        Assert.Throws<ArgumentException>(() => new ChartGanttItem(1, 1, isMilestone: true, isOpenEnded: true));
        Assert.Throws<ArgumentException>(() => new ChartGanttItem(1, 1, isVerticalMarker: true, isOpenEnded: true));
        Assert.Throws<ArgumentException>(() => new ChartGanttItem(Day, Day.AddDays(1), isOpenEnded: true));
        var numeric = ChartGanttItem.OpenEnded(1);
        Assert.True(numeric.IsOpenEnded);
        Assert.Equal(1, numeric.End);
    }

    [Fact]
    public void MatrixRowModelCarriesIndependentVerbatimHooks() {
        var row = ChartHeatmapRow.CreateMasked("Releases", 1, null, 3)
            .WithMarkOptions(new ChartMarkOptions(cssClass: "release-row", dataAttributes: new Dictionary<string, string> { ["data-release"] = "v42" }));
        var document = XDocument.Parse(Chart.Create().AddHeatmapRows(new[] { row }).ToSvg());
        var host = Assert.Single(document.Descendants(), node => (string?)node.Attribute("class") == "release-row");
        Assert.Equal("series", (string?)host.Attribute("data-cfx-role"));
        Assert.Equal("v42", (string?)host.Attribute("data-release"));
        Assert.Equal(2, host.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "heatmap-cell"));
    }

    [Fact]
    public void LinkedMatrixRowAndLegacyCellKeepIndependentSiblingNavigation() {
        var series = ChartSeries.CreateHeatmapCategoryRow(new ChartSeriesId("row"), "Support", new ChartHeatmapCell?[] {
            new ChartHeatmapCell("up", href: "#cell")
        }).WithRowOptions(new ChartMarkOptions(href: "#row", cssClass: "host-row"));
        var document = XDocument.Parse(Categories(Chart.Create()).AddSeries(series).ToSvg());
        var links = document.Descendants().Where(node => node.Name.LocalName == "a").ToArray();
        Assert.Equal(new[] { "#row", "#cell" }, links.Select(node => (string?)node.Attribute("href")).ToArray());
        Assert.All(links, link => Assert.DoesNotContain(link.Descendants(), node => node.Name.LocalName == "a"));
        var row = Assert.Single(document.Descendants(), node => (string?)node.Attribute("class") == "host-row");
        Assert.Equal("g", row.Name.LocalName);
        Assert.Contains(links, link => link.Parent == row && (string?)link.Attribute("href") == "#row");
    }

    [Fact]
    public void NativeVisibilityReplayPreservesRetainedPointHooksAndSeriesDefaults() {
        var id = new ChartSeriesId("releases");
        var series = new ChartSeries(id, "Releases", ChartSeriesKind.Bar, new[] {
            new ChartPoint(1, 2), new ChartPoint(2, 4).WithMarkOptions(Linked())
        }).WithObservationIds(new[] { new ChartObservationId("old"), new ChartObservationId("current") })
            .WithMarkOptions(new ChartMarkOptions(cssClass: "release-series"));
        var chart = Chart.Create().WithLegend(false).AddSeries(series);
        var view = new ChartVisibilityView(new[] { ChartVisibilityTarget.Observation(id, new ChartObservationId("old")) }, ChartVisibilityLayout.Reflow);
        var visual = new HtmlInteractiveChartRenderer().PrepareVisibilitySection(chart, view).PreparedVisual;
        var document = XDocument.Parse(visual.ToSvg());
        var link = Assert.Single(document.Descendants(), node => (string?)node.Attribute("href") == "/record/42");
        Assert.Equal("current", (string?)link.Attribute("data-cfx-observation-id"));
        Assert.Equal("42\"<&", (string?)link.Attribute("data-record-id"));
        Assert.Contains(document.Descendants(), node => (string?)node.Attribute("class") == "release-series");
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JaVaScRiPt:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("java\tscript:alert(1)")]
    [InlineData("\\javascript:alert(1)")]
    [InlineData("")]
    public void UnsafeLinksAreRejectedAtThePublicBoundary(string href) {
        Assert.Throws<ArgumentException>(() => new ChartMarkOptions(href: href));
        Assert.Throws<ArgumentException>(() => new ChartHeatmapCell("up", href: href));
    }

    [Theory]
    [InlineData("data-cfx-series")]
    [InlineData("data-cfx")]
    [InlineData("onclick")]
    [InlineData("data-x\" onload")]
    [InlineData("data-Uppercase")]
    [InlineData("data-")]
    public void UnsafeAndReservedHostAttributeNamesAreRejected(string name) =>
        Assert.Throws<ArgumentException>(() => new ChartMarkOptions(dataAttributes: new Dictionary<string, string> { [name] = "x" }));

    [Theory]
    [InlineData("x\" onclick=alert")]
    [InlineData("x\ty")]
    [InlineData("1leading-digit")]
    [InlineData("x:y")]
    public void UnsafeClassNamesAreRejectedWithoutRewriting(string cssClass) =>
        Assert.Throws<ArgumentException>(() => new ChartMarkOptions(cssClass: cssClass));

    private static Chart MarkChart(string family, ChartMarkOptions options) {
        var chart = Chart.Create().WithSize(720, 320).WithLegend(false);
        var point = new ChartPoint(1, 4).WithMarkOptions(options);
        switch (family) {
            case "column": return chart.AddBar("Releases", new[] { point });
            case "bar": return chart.AddHorizontalBar("Releases", new[] { point });
            case "stacked-column": return chart.WithStackedBars().AddBar("Supported", new[] { point }).AddBar("Other", new[] { new ChartPoint(1, 2) });
            case "scatter": return chart.AddScatter("Events", new[] { point });
            case "line": return chart.AddLine("Trend", new[] { point, new ChartPoint(2, 3) });
            case "timeline":
                chart.AddTimelineRange("Release", 1, 4);
                chart.Series[0].Points[0] = chart.Series[0].Points[0].WithMarkOptions(options);
                return chart;
            case "gantt":
                chart.AddGanttTask("Release", 1, 4);
                chart.Series[0].UpdateGanttItem(0, chart.Series[0].GanttItems[0].WithMarkOptions(options));
                return chart;
            case "gantt-marker":
                chart.AddGanttTask("Work", 1, 8).AddGanttMarker("Deadline", 4);
                chart.Series[1].UpdateGanttItem(0, chart.Series[1].GanttItems[0].WithMarkOptions(options));
                return chart;
            case "gantt-lane": return Categories(chart).AddGanttLane("Support", new[] { new ChartGanttLaneItem(Day, Day.AddDays(2), "up").WithMarkOptions(options) });
            case "state-timeline": return Categories(chart).AddStateTimelineLane("Support", new[] { new ChartStateTimelineSegment(Day, Day.AddDays(2), "up").WithMarkOptions(options) });
            case "calendar": return chart.AddCalendarHeatmap("Updates", new[] { new ChartCalendarHeatmapItem(Day, 4).WithMarkOptions(options) });
            case "range-bar": return chart.AddRangeBar("Support", new[] { new ChartInterval(1, 2, 4).WithMarkOptions(options) });
            case "range-band": return chart.AddRangeBand("Support", new[] { new ChartRangeBand(1, 2, 4).WithMarkOptions(options), new ChartRangeBand(2, 3, 5) });
            case "range-area": return chart.AddRangeArea("Support", new[] { new ChartRangeBand(1, 2, 4).WithMarkOptions(options), new ChartRangeBand(2, 3, 5) });
            case "dumbbell": return chart.AddDumbbell("Change", new[] { new ChartDumbbell(1, 2, 4).WithMarkOptions(options) });
            default: throw new ArgumentOutOfRangeException(nameof(family));
        }
    }

    private static Chart Categories(Chart chart) => chart.WithStateCategories(new ChartStateCategory("up", "Supported", ChartColor.FromHex("#2563eb")));
}
