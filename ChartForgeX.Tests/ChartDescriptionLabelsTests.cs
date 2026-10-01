using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>The automatic accessible name, description, and group names go through the chart labels.</summary>
public sealed class ChartDescriptionLabelsTests {
    [Fact]
    public void Defaults_KeepTheEnglishText() {
        var lines = Chart.Create().WithTitle("Traffic").AddLine("Inbound", Points(1, 2)).AddLine("Outbound", Points(2, 1));
        Assert.Equal("Traffic with 2 data series: Inbound, Outbound.", Desc(lines));
        var untitled = Chart.Create();
        Assert.Equal("Chart with no data series.", Desc(untitled));
        Assert.Equal("ChartForgeX chart", Element(untitled, "title").Value);
        Assert.Equal("Chart with no data points.", Desc(Chart.Create().AddLine("Empty", Array.Empty<ChartPoint>())));
        Assert.Equal("Changes calendar heatmap for Days from 2026-09-07 to 2026-09-08 with 2 days with a value.", Desc(Calendar()));
        Assert.Equal("Days calendar heatmap from 2026-09-06 to 2026-09-12 with 2 days with a value and 5 days without data", AriaLabel(Calendar(), "calendar-heatmap"));
        Assert.Equal("Coverage region map for Sites on United States states with 2 filled regions and 49 missing regions.", Desc(RegionMap()));
        Assert.Equal("Sites region map with 2 filled regions and 49 missing regions", AriaLabel(RegionMap(), "region-map"));
        Assert.Contains("<html lang=\"en\">", Html(Chart.Create()), StringComparison.Ordinal);
    }

    [Fact]
    public void CalendarFacts_CountDaysAtZeroApartFromDaysWithAValue() {
        // A day at zero is drawn neutral: a day in the data without changes, not a day with a value.
        var facts = new List<ChartDescriptionFacts>();
        _ = Chart.Create().WithLabels(labels => labels.AccessibleTextFormatter = item => {
            facts.Add(item);
            return null;
        }).AddCalendarHeatmap("Changes", new[] {
            new ChartCalendarHeatmapItem(new DateTime(2026, 9, 7), 1),
            new ChartCalendarHeatmapItem(new DateTime(2026, 9, 8), 0),
            new ChartCalendarHeatmapItem(new DateTime(2026, 9, 10), 4)
        }).ToSvg();

        var description = facts.Single(item => item.Kind == ChartDescriptionKind.CalendarHeatmap);
        var group = facts.Single(item => item.Kind == ChartDescriptionKind.CalendarHeatmapGroup);
        Assert.Equal(new[] { 2, 1, 1 }, new[] { description.Count, description.ZeroCount, description.MissingCount });
        Assert.Equal(new[] { 2, 1, 4 }, new[] { group.Count, group.ZeroCount, group.MissingCount });
    }

    [Fact]
    public void AccessibleTextFormatter_ReceivesTheFactsAndWritesTheDescription() {
        ChartDescriptionFacts? seen = null;
        var chart = Chart.Create().WithTitle("Verkehr").AddLine("Eingang", Points(1, 2)).AddLine("Ausgang", Points(2, 1))
            .WithLabels(labels => labels.AccessibleTextFormatter = facts => {
                seen = facts;
                return facts.Kind == ChartDescriptionKind.Series ? facts.Title + " mit " + facts.Count + " Datenreihen: " + string.Join(", ", facts.SeriesNames) + "." : null;
            });

        Assert.Equal("Verkehr mit 2 Datenreihen: Eingang, Ausgang.", Desc(chart));
        Assert.Equal(ChartDescriptionKind.Series, seen!.Kind);
        Assert.Equal(new[] { "Eingang", "Ausgang" }, seen.SeriesNames);
        Assert.Equal("Verkehr with 2 data series: Eingang, Ausgang.", seen.EnglishText);
    }

    [Fact]
    public void AccessibleTextFormatter_GetsCalendarAndMapFactsForTheChartAndItsGroup() {
        var facts = new List<ChartDescriptionFacts>();
        string? Collect(ChartDescriptionFacts description) {
            facts.Add(description);
            return null;
        }

        _ = Calendar().WithLabels(labels => labels.AccessibleTextFormatter = Collect).ToSvg();
        var calendar = facts.Single(item => item.Kind == ChartDescriptionKind.CalendarHeatmap);
        Assert.Equal(new DateTime(2026, 9, 7), calendar.FirstDate);
        Assert.Equal(new DateTime(2026, 9, 8), calendar.LastDate);
        Assert.Equal(2, calendar.Count);
        var calendarGroup = facts.Single(item => item.Kind == ChartDescriptionKind.CalendarHeatmapGroup);
        Assert.Equal(new DateTime(2026, 9, 6), calendarGroup.FirstDate);
        Assert.Equal(5, calendarGroup.MissingCount);

        facts.Clear();
        _ = RegionMap().WithLabels(labels => labels.AccessibleTextFormatter = Collect).ToSvg();
        foreach (var kind in new[] { ChartDescriptionKind.RegionMap, ChartDescriptionKind.RegionMapGroup }) {
            var region = facts.Single(item => item.Kind == kind);
            Assert.Equal("United States states", region.MapName);
            Assert.Equal(2, region.Count);
            Assert.Equal(49, region.MissingCount);
            Assert.Equal(new[] { "Sites" }, region.SeriesNames);
        }

        facts.Clear();
        _ = Chart.Create().WithLabels(labels => labels.AccessibleTextFormatter = Collect).ToSvg();
        Assert.Null(facts.Single().Title);
        Assert.Equal(ChartDescriptionKind.NoSeries, facts.Single().Kind);
    }

    [Fact]
    public void GroupNames_UseTheFormatterWithPluralsTheHostChooses() {
        static string? Polish(ChartDescriptionFacts facts) {
            static string Points(int count) => count == 1 ? "punkt" : count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 10 || count % 100 >= 20) ? "punkty" : "punktów";
            return facts.Kind switch {
                ChartDescriptionKind.DottedMapGroup => facts.SeriesNames[0] + ": " + facts.Count + " " + Points(facts.Count),
                ChartDescriptionKind.TileMapGroup => facts.SeriesNames[0] + ": " + facts.Count + "/" + (facts.Count + facts.MissingCount),
                _ => null
            };
        }

        var dotted = Chart.Create().WithLabels(labels => labels.AccessibleTextFormatter = Polish)
            .AddDottedMap("Biura", new[] { new ChartMapPoint("Warszawa", 21, 52), new ChartMapPoint("Kraków", 19.9, 50), new ChartMapPoint("Wrocław", 17, 51.1) });
        Assert.Equal("Biura: 3 punkty", AriaLabel(dotted, "dotted-map"));
        Assert.Contains("dotted world map for Biura with 3 highlighted points.", Desc(dotted), StringComparison.Ordinal);

        var tiles = Chart.Create().WithLabels(labels => labels.AccessibleTextFormatter = Polish)
            .AddTileMap("Biura", ChartTileMapCatalog.Get("us-states"), new[] { new ChartRegionMapItem("CA", 1) });
        Assert.StartsWith("Biura: 1/", AriaLabel(tiles, "tile-map"), StringComparison.Ordinal);
    }

    [Fact]
    public void Formatter_WhiteSpaceFallsBackAndAnExplicitDescriptionIsNotFormatted() {
        Assert.Equal("Chart with 1 data series: A.", Desc(Chart.Create().AddLine("A", Points(1, 2)).WithLabels(labels => labels.AccessibleTextFormatter = _ => "  ")));

        var calls = 0;
        var chart = Chart.Create().AddLine("A", Points(1, 2))
            .WithLabels(labels => labels.AccessibleTextFormatter = _ => {
                calls++;
                return "formatted";
            })
            .WithAccessibility(accessibility => accessibility.Description = "explicit");
        Assert.Equal("explicit", Desc(chart));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void UntitledChartAndPageLanguage_AreLocalized() {
        var chart = RegionMap().WithTitle(string.Empty)
            .WithLabels(labels => labels.UntitledChart = "Diagramm")
            .WithAccessibility(accessibility => accessibility.Language = "de");
        Assert.Equal("Diagramm", Element(chart, "title").Value);
        var html = Html(chart);
        Assert.Contains("<title>Diagramm</title>", html, StringComparison.Ordinal);
        Assert.Contains("<html lang=\"de\">", html, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Chart.Create().WithLabels(labels => labels.UntitledChart = " "));
    }

    [Fact]
    public void Facts_AreValidatedAndCopied() {
        var names = new List<string> { "A" };
        var facts = new ChartDescriptionFacts(ChartDescriptionKind.Series, " ", names, 1);
        names.Add("B");
        Assert.Single(facts.SeriesNames);
        Assert.Null(facts.Title);
        Assert.Equal("Chart with 1 data series: A.", facts.EnglishText);
        Assert.Throws<ArgumentNullException>(() => new ChartDescriptionFacts(ChartDescriptionKind.Series, null, null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartDescriptionFacts(ChartDescriptionKind.Series, null, names, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartDescriptionFacts(ChartDescriptionKind.RegionMap, null, names, 0, -1));
    }

    private static Chart Calendar() => Chart.Create().WithTitle("Changes").AddCalendarHeatmap("Days", new[] {
        new ChartCalendarHeatmapItem(new DateTime(2026, 9, 7), 1),
        new ChartCalendarHeatmapItem(new DateTime(2026, 9, 8), 3)
    });

    private static Chart RegionMap() => Chart.Create().WithTitle("Coverage").WithMapLabels(false)
        .AddRegionMap("Sites", ChartMapCatalog.Get("us-states"), new[] { new ChartRegionMapItem("CA", 1), new ChartRegionMapItem("NY", 2) });

    private static ChartPoint[] Points(double first, double second) => new[] { new ChartPoint(0, first), new ChartPoint(1, second) };

    private static string Html(Chart chart) => new HtmlChartRenderer().RenderPage(chart);

    private static string Desc(Chart chart) => Element(chart, "desc").Value;

    private static XElement Element(Chart chart, string name) => XDocument.Parse(chart.ToSvg()).Descendants().First(element => element.Name.LocalName == name);

    private static string AriaLabel(Chart chart, string role) =>
        (string)XDocument.Parse(chart.ToSvg()).Descendants().First(element => (string?)element.Attribute("data-cfx-role") == role).Attribute("aria-label")!;
}
