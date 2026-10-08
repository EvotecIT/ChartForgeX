using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2FramePolicyTests {
    private static readonly string[] KeyFamilies = { "categorical", "numeric", "calendar", "map", "schedule", "state-schedule" };

    public static IEnumerable<object[]> FamilyFrameVisibilityCases() {
        foreach (var family in KeyFamilies)
            foreach (var model in new[] { false, true })
                foreach (var frame in new[] { false, true })
                    yield return new object[] { family, model, frame };
    }

    public static IEnumerable<object[]> FamilyKeyCases() => KeyFamilies.Select(family => new object[] { family });

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void OmittedFramePolicyMatchesConvenienceExportForAutomaticLegends(int seriesCount, bool expectedLegend) {
        var chart = Bars(seriesCount);
        var convenience = XDocument.Parse(chart.ToSvg());
        var prepared = XDocument.Parse(chart.Prepare(Context()).ToSvg());

        Assert.Equal(expectedLegend, HasLegend(convenience));
        Assert.Equal(expectedLegend, HasLegend(prepared));
        Assert.Equal(TextPlacement(convenience), TextPlacement(prepared));
        var title = Assert.Single(prepared.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Assessment results");
        Assert.Equal(24, (double)title.Attribute("x")!);
        Assert.Equal(17, (double)title.Attribute("font-size")!);
        Assert.Equal("700", (string?)title.Attribute("font-weight"));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void OmittedFramePolicyHonorsExplicitModelLegendVisibility(int seriesCount, bool showLegend) {
        var chart = Bars(seriesCount).WithLegend(showLegend);
        var convenience = XDocument.Parse(chart.ToSvg());
        var prepared = XDocument.Parse(chart.Prepare(Context()).ToSvg());

        Assert.Equal(showLegend, HasLegend(prepared));
        Assert.Equal(TextPlacement(convenience), TextPlacement(prepared));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ExplicitFrameVisibilityOverridesTheModel(bool modelVisibility, bool frameVisibility) {
        var chart = Bars(2).WithLegend(modelVisibility);
        var prepared = XDocument.Parse(chart.Prepare(Context(new VisualFrame(showLegend: frameVisibility))).ToSvg());
        Assert.Equal(frameVisibility, HasLegend(prepared));
    }

    [Theory]
    [MemberData(nameof(FamilyFrameVisibilityCases))]
    public void ResolvedFrameVisibilityControlsEveryFamilyKey(string family, bool modelVisibility, bool frameVisibility) {
        var chart = KeyChart(family).WithLegend(modelVisibility);
        var prepared = chart.Prepare(Context(new VisualFrame(showLegend: frameVisibility)));
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(frameVisibility, HasKey(document, family));
        Assert.Contains(prepared.Regions, region => region.Role == DataRole(family) && region.Bounds.Width > 0);
    }

    [Theory]
    [MemberData(nameof(FamilyKeyCases))]
    public void OmittedFamilyFramePolicyKeepsAutomaticAndExplicitModelVisibility(string family) {
        var chart = KeyChart(family);
        Assert.True(HasKey(XDocument.Parse(chart.ToSvg()), family));
        Assert.True(HasKey(XDocument.Parse(chart.Prepare(Context()).ToSvg()), family));
        foreach (var visible in new[] { false, true }) {
            chart.WithLegend(visible);
            Assert.Equal(visible, HasKey(XDocument.Parse(chart.ToSvg()), family));
            Assert.Equal(visible, HasKey(XDocument.Parse(chart.Prepare(Context()).ToSvg()), family));
        }
    }

    [Theory]
    [InlineData("categorical")]
    [InlineData("numeric")]
    [InlineData("calendar")]
    [InlineData("map")]
    public void ExplicitFrameVisibilityRetainsFamilyScaleSuppression(string family) {
        var chart = KeyChart(family).WithLegend(false);
        if (family == "map") chart.WithMapScaleLegend(false);
        else chart.WithHeatmapScaleLegend(false);
        var prepared = chart.Prepare(Context(new VisualFrame(showLegend: true)));
        Assert.False(HasKey(XDocument.Parse(prepared.ToSvg()), family));
        Assert.Contains(prepared.Regions, region => region.Role == DataRole(family) && region.Bounds.Width > 0);
    }

    [Fact]
    public void ExplicitScheduleFrameDoesNotRestoreSuppressedSeriesEntries() {
        var chart = KeyChart("schedule").WithLegend(false);
        chart.Series[0].WithLegendEntry(false);
        var context = Context(new VisualFrame(showLegend: true));
        var prepared = chart.Prepare(context);
        var entries = XDocument.Parse(prepared.ToSvg()).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "legend-entry").ToArray();
        var entry = Assert.Single(entries);
        Assert.Equal("Build", Assert.Single(entry.Descendants(), element => element.Name.LocalName == "text").Value);
        chart.Series[1].WithLegendEntry(false);
        var hidden = chart.Prepare(context);
        Assert.False(HasLegend(XDocument.Parse(hidden.ToSvg())));
        Assert.Equal(2, hidden.Regions.Count(region => region.Role == "timeline-item"));
    }

    [Fact]
    public void OmittedPositionUsesModelPlacementAndExplicitFramePositionOverridesIt() {
        var chart = Bars(2).WithLegendPosition(ChartLegendPosition.Right);
        var modelPlacement = XDocument.Parse(chart.ToSvg());
        var automatic = XDocument.Parse(chart.Prepare(Context()).ToSvg());
        Assert.Equal(TextPlacement(modelPlacement), TextPlacement(automatic));

        var overridePlacement = XDocument.Parse(chart.Prepare(Context(new VisualFrame(legendPosition: ChartLegendPosition.Bottom))).ToSvg());
        var requestedBottom = XDocument.Parse(chart.WithLegendPosition(ChartLegendPosition.Bottom).ToSvg());
        Assert.Equal(TextPlacement(requestedBottom), TextPlacement(overridePlacement));
        Assert.False(TextPlacement(automatic).SequenceEqual(TextPlacement(overridePlacement)));
    }

    [Fact]
    public void SubtitleFollowsTheMeasuredTitleWithATightGap() {
        var chart = Bars(1);
        var together = XDocument.Parse(chart.Prepare(Context()).ToSvg());
        var subtitleOnly = XDocument.Parse(chart.Prepare(Context(new VisualFrame(title: ""))).ToSvg());
        var pairedSubtitle = Assert.Single(together.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Current reporting period");
        var standaloneSubtitle = Assert.Single(subtitleOnly.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Current reporting period");
        var title = Assert.Single(together.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Assessment results");
        var titleStyle = new TextStyle {
            Font = new FontSpec { Family = (string)title.Attribute("font-family")!, Weight = 700 },
            FontSize = (double)title.Attribute("font-size")!, LineHeight = 1
        };
        var measuredTitleHeight = Math.Max(titleStyle.FontSize * 1.3, TextLayoutEngine.Measure("Mg", titleStyle).Height);
        var subtitleDisplacement = (double)pairedSubtitle.Attribute("y")! - (double)standaloneSubtitle.Attribute("y")!;
        Assert.InRange(subtitleDisplacement - measuredTitleHeight, 1.99, 2.01);
    }

    private static Chart Bars(int seriesCount) {
        var chart = Chart.Create().WithSize(640, 400).WithTitle("Assessment results").WithSubtitle("Current reporting period")
            .WithXLabels("First", "Second", "Third");
        for (var index = 0; index < seriesCount; index++) chart.AddBar(index == 0 ? "Reviewed" : "Verified", new[] {
            new ChartPoint(1, 30 + index * 4), new ChartPoint(2, 45 + index * 4), new ChartPoint(3, 38 + index * 4)
        });
        return chart;
    }

    private static Chart KeyChart(string family) {
        var chart = Chart.Create().WithSize(640, 400);
        var states = new[] {
            new ChartStateCategory("pass", "Passed", ChartColor.FromHex("#1D8A52")),
            new ChartStateCategory("fail", "Failed", ChartColor.FromHex("#D4302F"))
        };
        return family switch {
            "categorical" => chart.WithStateCategories(states).WithXLabels("First", "Second")
                .AddHeatmapCategoryRow("Checks", new ChartHeatmapCell("pass"), new ChartHeatmapCell("fail")),
            "numeric" => chart.WithXLabels("First", "Second", "Third").AddHeatmapRow("Count", new[] { 0d, 20, 100 }),
            "calendar" => chart.AddCalendarHeatmap("Activity", new[] {
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 0), new ChartCalendarHeatmapItem(new DateTime(2026, 1, 7), 5)
            }, firstDayOfWeek: DayOfWeek.Monday),
            "map" => chart.AddTileMap("Count", new ChartTileMapDefinition("frame-policy", "Regions", new[] {
                new ChartTileMapRegion("A", "First", 0, 0), new ChartTileMapRegion("B", "Second", 1, 0)
            }), new[] { new ChartRegionMapItem("A", 30), new ChartRegionMapItem("B", 80) }),
            "schedule" => chart.AddTimelineRange("Plan", 1, 3).AddTimelineRange("Build", 2, 4),
            "state-schedule" => chart.WithStateCategories(states).AddStateTimelineLane("Service", new[] {
                new ChartStateTimelineSegment(1, 2, "pass"), new ChartStateTimelineSegment(2, 3, "fail")
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    private static string KeyRole(string family) => family switch {
        "numeric" => "heatmap-scale",
        "calendar" => "calendar-scale",
        "map" => "map-scale",
        _ => "legend-entry"
    };

    private static string DataRole(string family) => family switch {
        "calendar" => "calendar-cell",
        "map" => "tile-map-region",
        "schedule" => "timeline-item",
        "state-schedule" => "state-timeline-segment",
        _ => "heatmap-cell"
    };

    private static bool HasKey(XDocument document, string family) => document.Descendants()
        .Any(element => (string?)element.Attribute("data-cfx-role") == KeyRole(family));

    private static VisualRenderContext Context(VisualFrame? frame = null) =>
        new(new VisualLayoutOptions(new VisualSize(640, 400)), frame: frame);

    private static bool HasLegend(XDocument document) => document.Descendants()
        .Any(element => (string?)element.Attribute("data-cfx-role") == "legend-entry");

    private static string[] TextPlacement(XDocument document) => document.Descendants()
        .Where(element => element.Name.LocalName == "text")
        .Select(element => string.Join("|", element.Value, (string?)element.Attribute("x"), (string?)element.Attribute("y"),
            (string?)element.Attribute("font-family"), (string?)element.Attribute("font-size"),
            (string?)element.Attribute("font-weight"), (string?)element.Attribute("fill")))
        .ToArray();
}
