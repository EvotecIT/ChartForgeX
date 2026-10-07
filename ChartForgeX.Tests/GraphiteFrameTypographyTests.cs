using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteFrameTypographyTests {
    public static IEnumerable<object[]> Families() {
        foreach (var dark in new[] { false, true }) {
            foreach (var kind in Enum.GetValues<ChartSeriesKind>()) yield return new object[] { kind, ChartGaugeForm.Arc, dark };
            yield return new object[] { ChartSeriesKind.Gauge, ChartGaugeForm.Needle, dark };
            yield return new object[] { ChartSeriesKind.Gauge, ChartGaugeForm.Linear, dark };
        }
    }

    [Theory]
    [MemberData(nameof(Families))]
    public void EveryFamilyUsesSharedFrameTypography(ChartSeriesKind kind, ChartGaugeForm form, bool dark) {
        var chart = Create(kind).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithSize(640, 360).WithTitle("Shared title").WithSubtitle("Shared subtitle");
        chart.Options.Gauge.Form = form;
        var svg = XDocument.Parse(chart.ToSvg());
        var headings = svg.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "frame-heading")
            .SelectMany(e => e.Descendants().Where(child => child.Name.LocalName == "text")).ToArray();
        var title = Assert.Single(headings, element => element.Value == "Shared title");
        var subtitle = Assert.Single(headings, element => element.Value == "Shared subtitle");
        var context = VisualExportRequest.ForChart(chart).Context;
        var colors = context.Theme.Resolve(context.ThemeMode);
        Assert.Equal(context.Theme.Typography.TitleSize, (double)title.Attribute("font-size")!);
        Assert.Equal("600", (string?)title.Attribute("font-weight"));
        Assert.Equal(colors.Foreground.ToCss(), (string?)title.Attribute("fill"));
        Assert.Equal(context.Theme.Typography.SubtitleSize, (double)subtitle.Attribute("font-size")!);
        Assert.Equal(colors.MutedForeground.ToCss(), (string?)subtitle.Attribute("fill"));
        Assert.Equal(context.Layout.PaddingEdges.Left, (double)title.Attribute("x")!);
        Assert.Equal((double)title.Attribute("x")!, (double)subtitle.Attribute("x")!);
        Assert.Contains((string?)title.Attribute("text-anchor"), new string?[] { null, "start" });
        Assert.Contains((string?)subtitle.Attribute("text-anchor"), new string?[] { null, "start" });
        Assert.True((double)title.Attribute("y")! + context.Theme.Typography.SubtitleSize < (double)subtitle.Attribute("y")!);
        Assert.NotEqual("none", (string?)title.Attribute("display"));
        Assert.NotEqual("none", (string?)subtitle.Attribute("display"));
    }

    private static Chart Create(ChartSeriesKind kind) {
        var chart = Chart.Create();
        var points = new[] { new ChartPoint(1, 30), new ChartPoint(2, 20), new ChartPoint(3, 10) };
        if (ChartSeriesKindTraits.SupportsPointSeriesMapping(kind)) return chart.AddSeries("Measure", kind, points, p => p.X, p => p.Y);
        var ranges = new[] { new ChartRangeBand(1, 10, 30), new ChartRangeBand(2, 15, 25) };
        var candles = new[] { new ChartCandlestick(1, 15, 30, 10, 20) };
        var links = new[] { new ChartTreeLink("Root", "First", 30), new ChartTreeLink("Root", "Second", 20) };
        switch (kind) {
            case ChartSeriesKind.Bubble: return chart.AddBubble("Measure", new[] { new ChartBubble(1, 20, 10) });
            case ChartSeriesKind.ErrorBar: return chart.AddErrorBar("Measure", new[] { new ChartErrorBar(1, 20, 10, 30) });
            case ChartSeriesKind.Candlestick: return chart.AddCandlestick("Measure", candles);
            case ChartSeriesKind.Ohlc: return chart.AddOhlc("Measure", candles);
            case ChartSeriesKind.RangeBand: return chart.AddRangeBand("Measure", ranges);
            case ChartSeriesKind.RangeArea: return chart.AddRangeArea("Measure", ranges);
            case ChartSeriesKind.Dumbbell: return chart.AddDumbbell("Measure", new[] { new ChartDumbbell(1, 10, 30) });
            case ChartSeriesKind.RangeBar: return chart.AddRangeBar("Measure", new[] { new ChartInterval(1, 10, 30) });
            case ChartSeriesKind.BoxPlot: return chart.AddBoxPlot("Measure", 1, new[] { 10d, 15, 20, 25, 30 });
            case ChartSeriesKind.Heatmap: return chart.AddHeatmapRow("Measure", points);
            case ChartSeriesKind.HexbinHeatmap: return chart.AddHexbinHeatmapRow("Measure", points);
            case ChartSeriesKind.CalendarHeatmap: return chart.AddCalendarHeatmap("Measure", new[] { new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 30) });
            case ChartSeriesKind.DottedMap: return chart.AddDottedMap("Measure", new[] { new ChartMapPoint("Warsaw", 21, 52) });
            case ChartSeriesKind.TileMap:
                var tiles = ChartTileMapCatalog.All()[0];
                return chart.AddTileMap("Measure", tiles, new[] { new ChartRegionMapItem(tiles.Regions[0].Code, 30) });
            case ChartSeriesKind.RegionMap:
                var map = ChartMapCatalog.All()[0];
                return chart.AddRegionMap("Measure", map, new[] { new ChartRegionMapItem(map.Regions[0].Code, 30) });
            case ChartSeriesKind.Gauge: return chart.AddGauge("Measure", 30);
            case ChartSeriesKind.Circle: return chart.AddCircle("Measure", 30);
            case ChartSeriesKind.RadialBar: return chart.AddRadialBar("Measure", points);
            case ChartSeriesKind.LayeredRadial: return chart.AddLayeredRadial("Measure", new[] { new ChartRadialLayer("First", 30) });
            case ChartSeriesKind.Bullet: return chart.AddBullet("Measure", 30, 40);
            case ChartSeriesKind.Waterfall: return chart.AddWaterfall("Measure", points);
            case ChartSeriesKind.Radar: return chart.AddRadar("Measure", points);
            case ChartSeriesKind.Funnel: return chart.AddFunnel("Measure", points);
            case ChartSeriesKind.Timeline: return chart.AddTimelineRange("Measure", 1, 3);
            case ChartSeriesKind.Gantt: return chart.AddGanttTask("Measure", 1, 3);
            case ChartSeriesKind.Sankey: return chart.AddSankey("Measure", new[] { new ChartSankeyLink("First", "Second", 30) });
            case ChartSeriesKind.Tree: return chart.AddTree("Measure", links);
            case ChartSeriesKind.Sunburst: return chart.AddSunburst("Measure", links);
            case ChartSeriesKind.Pie: return chart.AddPie("Measure", points);
            case ChartSeriesKind.Donut: return chart.AddDonut("Measure", points);
            case ChartSeriesKind.Slope: return chart.AddSlope("Measure", 30, 40);
            case ChartSeriesKind.Treemap: return chart.AddTreemap("Measure", new[] { new ChartTreemapItem("First", 30) });
            case ChartSeriesKind.Pictorial: return chart.AddPictorial("Measure", new[] { new ChartPictorialItem("First", 30) });
            case ChartSeriesKind.ProgressBar: return chart.AddProgressBars("Measure", new[] { new ChartProgressItem("First", 30) });
            case ChartSeriesKind.WordCloud: return chart.AddWordCloud("Measure", new[] { new ChartWordCloudItem("First", 30) });
            case ChartSeriesKind.PolarArea: return chart.AddPolarArea("Measure", points);
            case ChartSeriesKind.TrendLine: return chart.AddTrendLine("Measure", points);
            case ChartSeriesKind.Polar: return chart.AddPolar("Measure", points);
            case ChartSeriesKind.StateTimeline:
                return chart.WithStateCategories(new ChartStateCategory("ok", "Healthy", ChartColor.FromHex("#2A78D6")))
                    .AddStateTimelineLane("Measure", new[] { new ChartStateTimelineSegment(1, 3, "ok") });
            case ChartSeriesKind.GanttLane:
                return chart.WithStateCategories(new ChartStateCategory("ok", "Healthy", ChartColor.FromHex("#2A78D6")))
                    .AddGanttLane("Measure", new[] { new ChartGanttLaneItem(1, 3, "ok") });
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Add a public-API fixture for this family.");
        }
    }
}
