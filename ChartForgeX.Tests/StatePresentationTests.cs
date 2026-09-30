using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Group headers for state timeline lanes and categorical heatmap rows, rows without cells, state patterns and
/// emphasis, and the accessible name of a heatmap cell with a tooltip.
/// </summary>
public sealed class StatePresentationTests {
    private static readonly DateTime Day = new(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc);
    private static readonly ChartColor Pass = ChartColor.FromHex("#1d8a52");
    private static readonly ChartColor Critical = ChartColor.FromHex("#d4302f");
    private static readonly ChartColor Neutral = ChartColor.FromHex("#7c818a");

    [Fact]
    public void StateTimeline_GroupedLanes_DrawOneHeaderPerGroupAboveItsLanes() {
        var chart = Timeline();
        var svg = XDocument.Parse(chart.ToSvg());

        Assert.Equal(new[] { "North", "South" }, ByRole(svg, "state-lane-group").Select(text => text.Value).ToArray());
        var headers = ByRole(svg, "state-lane-group").Select(text => Number(text, "y")).ToArray();
        var lanes = ByRole(svg, "state-lane-track").Select(track => Number(track, "y")).ToArray();
        Assert.Equal(4, lanes.Length);
        Assert.True(headers[0] < lanes[0] && lanes[1] < headers[1] && headers[1] < lanes[2], "Each header sits above the lanes of its group.");
        // The ungrouped last lane is set apart from the group above it by a rule and a small gap, without a header.
        Assert.Equal(2, ByRole(svg, "state-lane-group-rule").Length);
        Assert.True(lanes[3] - lanes[2] > lanes[1] - lanes[0]);
        Assert.NotEqual(Timeline(grouped: false).ToPng(), chart.ToPng());
    }

    [Fact]
    public void StateTimeline_SummaryHeader_SitsInsideThePlotFrame() {
        var svg = XDocument.Parse(Timeline().ToSvg());
        var header = ByRole(svg, "state-summary-header").Single();
        var frameTop = Number(ByRole(svg, "plot-inner-highlight").Single(), "y");
        var firstLane = ByRole(svg, "state-lane-track").Min(track => Number(track, "y"));
        var fontSize = Number(header, "font-size");
        Assert.True(Number(header, "y") - fontSize * 0.8 >= frameTop + 2, "The header text must not touch the top border of the plot frame.");
        Assert.True(Number(header, "y") < firstLane, "The header stays above the lanes.");
    }

    [Fact]
    public void CategoricalHeatmap_GroupedRows_DrawHeadersAndKeepRowsBelowThem() {
        var chart = Matrix();
        var svg = XDocument.Parse(chart.ToSvg());

        Assert.Equal(new[] { "North", "South" }, ByRole(svg, "heatmap-row-group").Select(text => text.Value).ToArray());
        var headers = ByRole(svg, "heatmap-row-group").Select(text => Number(text, "y")).ToArray();
        var rows = ByRole(svg, "heatmap-row-label").Select(text => Number(text, "y")).ToArray();
        Assert.Equal(new[] { "A1", "A2", "B1", "Empty" }, ByRole(svg, "heatmap-row-label").Select(text => text.Value).ToArray());
        Assert.True(headers[0] < rows[0] && rows[1] < headers[1] && headers[1] < rows[2]);
        Assert.Single(ByRole(svg, "heatmap-row-group-rule"));
        var cellHeights = ByRole(svg, "heatmap-cell").Select(cell => Number(cell, "height")).Distinct().ToArray();
        Assert.Single(cellHeights);
        Assert.NotEqual(Matrix(grouped: false).ToPng(), chart.ToPng());
    }

    [Fact]
    public void CategoricalHeatmap_RowWithoutCells_KeepsItsLabelAndAnEmptyRow() {
        var chart = Matrix();
        var svg = XDocument.Parse(chart.ToSvg());

        Assert.Contains("Empty", ByRole(svg, "heatmap-row-label").Select(text => text.Value));
        Assert.DoesNotContain(ByRole(svg, "heatmap-cell"), cell => (string?)cell.Attribute("data-cfx-row") == "3");
        Assert.Equal(7, ByRole(svg, "heatmap-cell").Length);
        Assert.True(chart.ToPng().Length > 64);

        // A chart whose first row is empty is still categorical, and one with only empty rows still renders.
        var emptyFirst = Chart.Create().WithSize(640, 380).WithStateCategories(new ChartStateCategory("pass", "Passed", Pass))
            .WithXLabels("One", "Two")
            .AddHeatmapCategoryRow("Nothing", new ChartHeatmapCell?[] { null, null })
            .AddHeatmapCategoryRow("Something", new ChartHeatmapCell("pass"), new ChartHeatmapCell("pass"));
        Assert.Equal(new[] { "Passed" }, ByRole(XDocument.Parse(emptyFirst.ToSvg()), "state-legend-label").Select(text => text.Value).ToArray());
        var onlyEmpty = Chart.Create().WithSize(640, 260).WithXLabels("One", "Two").AddHeatmapCategoryRow("Nothing", new ChartHeatmapCell?[] { null, null });
        Assert.Empty(ByRole(XDocument.Parse(onlyEmpty.ToSvg()), "heatmap-cell"));
        Assert.True(onlyEmpty.ToPng().Length > 64);
    }

    [Fact]
    public void CategoricalHeatmap_CellTooltip_AddsToTheAccessibleNameInsteadOfReplacingIt() {
        var svg = XDocument.Parse(Matrix().ToSvg());
        var cell = ByRole(svg, "heatmap-cell").Single(element => (string?)element.Attribute("data-cfx-id") == "heatmap:1:1");

        Assert.Equal("A2, Two: Critical. Backup is 9 days old", (string?)cell.Attribute("aria-label"));
        Assert.Equal("Backup is 9 days old", cell.Elements().Single(child => child.Name.LocalName == "title").Value);
        var plain = ByRole(svg, "heatmap-cell").Single(element => (string?)element.Attribute("data-cfx-id") == "heatmap:0:0");
        Assert.Equal("A1, One: Passed", (string?)plain.Attribute("aria-label"));
        Assert.Equal("A1, One: Passed", plain.Elements().Single(child => child.Name.LocalName == "title").Value);
    }

    [Fact]
    public void StatePatterns_DrawDistinctMarksForStatesThatShareAColour() {
        var chart = Matrix();
        var svg = XDocument.Parse(chart.ToSvg());
        XElement Cell(string state) => ByRole(svg, "heatmap-cell").First(cell => (string?)cell.Attribute("data-cfx-status") == state);

        Assert.Null(Cell("critical").Attribute("data-cfx-pattern"));
        Assert.Equal("hatched", (string?)Cell("notEvaluated").Attribute("data-cfx-pattern"));
        Assert.Equal("cross-hatched", (string?)Cell("unknown").Attribute("data-cfx-pattern"));
        Assert.Equal("outlined", (string?)Cell("couldNotEvaluate").Attribute("data-cfx-pattern"));

        var hatches = ByRole(svg, "heatmap-cell-hatch").Select(hatch => (string)hatch.Attribute("fill")!).ToArray();
        Assert.Equal(2, hatches.Length);
        Assert.Single(hatches, fill => fill.EndsWith("-cross)", StringComparison.Ordinal));
        var outlined = Cell("couldNotEvaluate");
        var outline = ByRole(svg, "heatmap-cell-outline").Single();
        Assert.Equal(Neutral.ToCss(), (string?)outline.Attribute("stroke"));
        Assert.True(Number(outline, "x") > Number(outlined, "x") && Number(outline, "width") < Number(outlined, "width"), "The outline stays inside the cell.");
        Assert.True(Number(outlined, "fill-opacity") < 0.2, "An outlined state is not filled.");

        // In the raster output the outlined cell is mostly background and the hatched cells differ from each other.
        var image = PngReader.Decode(chart.ToPng());
        var scale = image.Width / (double)chart.Options.Size.Width;
        (byte R, byte G, byte B) Centre(XElement cell) => Pixel(image, (Number(cell, "x") + Number(cell, "width") / 2) * scale, (Number(cell, "y") + Number(cell, "height") / 2) * scale);
        Assert.False(IsClose(Centre(outlined), Neutral), "The outlined cell centre should not carry the solid state colour.");
        Assert.True(IsClose(Centre(Cell("critical")), Critical));
        Assert.NotEqual(InkCount(image, Cell("notEvaluated"), scale, Neutral), InkCount(image, Cell("unknown"), scale, Neutral));
    }

    [Fact]
    public void StatePatterns_LinesTakeTheSurfaceBehindTheMarks() {
        string[] Strokes(Chart chart) => XDocument.Parse(chart.ToSvg()).Descendants().Where(element => element.Name.LocalName == "pattern")
            .SelectMany(pattern => pattern.Elements()).Select(line => (string)line.Attribute("stroke")!).Distinct().ToArray();

        var dark = Matrix().WithTheme(ChartTheme.Dark());
        var darkStroke = ChartColor.Parse(Strokes(dark).Single());
        Assert.Equal(255, darkStroke.A);
        Assert.True(darkStroke.R < 96 && darkStroke.G < 96 && darkStroke.B < 96, "On a dark theme the lines are dark.");
        Assert.NotEqual(Matrix().ToPng(), dark.ToPng());

        // A theme without any opaque surface still gets visible lines, and the hatched cell still differs from a solid one.
        var overlay = Matrix().WithTheme(ChartTheme.TransparentOverlayDark());
        var overlayStroke = ChartColor.Parse(Strokes(overlay).Single());
        Assert.Equal(255, overlayStroke.A);
        var svg = XDocument.Parse(overlay.ToSvg());
        var image = PngReader.Decode(overlay.ToPng());
        var scale = image.Width / (double)overlay.Options.Size.Width;
        var hatched = ByRole(svg, "heatmap-cell").First(cell => (string?)cell.Attribute("data-cfx-status") == "notEvaluated");
        var crossed = ByRole(svg, "heatmap-cell").First(cell => (string?)cell.Attribute("data-cfx-status") == "unknown");
        Assert.NotEqual(InkCount(image, hatched, scale, Neutral), InkCount(image, crossed, scale, Neutral));
    }

    [Fact]
    public void CategoricalHeatmap_ManyGroupsInAShortChart_KeepRowsInsideThePlot() {
        var chart = Chart.Create().WithSize(640, 300).WithStateCategories(new ChartStateCategory("pass", "Passed", Pass)).WithXLabels("One", "Two");
        chart.Options.ShowLegend = false;
        for (var row = 0; row < 24; row++) chart.AddHeatmapCategoryRow("R" + row, new ChartHeatmapCell?[] { new ChartHeatmapCell("pass"), new ChartHeatmapCell("pass") }, "Group " + (row / 2));
        var svg = XDocument.Parse(chart.ToSvg());
        var cells = ByRole(svg, "heatmap-cell");
        var columnLabels = ByRole(svg, "heatmap-column-label").Min(label => Number(label, "y"));
        Assert.Equal(48, cells.Length);
        Assert.All(cells, cell => Assert.True(Number(cell, "y") + Number(cell, "height") <= columnLabels, "Rows must stay above the column labels."));
        Assert.True(chart.ToPng().Length > 64);
    }

    [Fact]
    public void GanttLanes_SummaryHeaderAndNowLabel_SitInsideThePlotFrame() {
        var chart = Chart.Create().WithSize(760, 420).WithXAxisTimeScale(showTimeZone: true).WithGanttLaneNow(Day.AddHours(10))
            .WithStateCategories(new ChartStateCategory("up", "Up", Pass))
            .AddGanttLane("A", new[] { new ChartGanttLaneItem(Day, Day.AddHours(6), "up") }, "North", "1");
        chart.Options.LaneSummaryHeader = "Items";
        var svg = XDocument.Parse(chart.ToSvg());
        var frameTop = Number(ByRole(svg, "plot-inner-highlight").Single(), "y");
        foreach (var role in new[] { "gantt-lanes-summary-header", "gantt-lanes-now-label" }) {
            var text = ByRole(svg, role).Single();
            Assert.True(Number(text, "y") - Number(text, "font-size") * 0.8 >= frameTop + 2, role + " touches the top border of the plot frame.");
        }
    }
    [Fact]
    public void QuietState_IsDrawnLighterAndKeepsItsMeaning() {
        var chart = Matrix();
        var svg = XDocument.Parse(chart.ToSvg());
        var quiet = ByRole(svg, "heatmap-cell").First(cell => (string?)cell.Attribute("data-cfx-status") == "pass");
        var loud = ByRole(svg, "heatmap-cell").First(cell => (string?)cell.Attribute("data-cfx-status") == "critical");

        Assert.Equal("quiet", (string?)quiet.Attribute("data-cfx-emphasis"));
        Assert.Equal(Pass.ToCss(), (string?)quiet.Attribute("fill"));
        Assert.InRange(Number(quiet, "fill-opacity"), 0.2, 0.6);
        Assert.Null(loud.Attribute("fill-opacity"));
        Assert.Equal("A1, One: Passed", (string?)quiet.Attribute("aria-label"));
        Assert.Contains("Passed", ByRole(svg, "state-legend-label").Select(text => text.Value));
        var swatch = ByRole(svg, "state-legend-swatch").First(element => (string?)element.Attribute("data-cfx-status") == "pass");
        Assert.Equal("quiet", (string?)swatch.Attribute("data-cfx-emphasis"));

        var image = PngReader.Decode(chart.ToPng());
        var scale = image.Width / (double)chart.Options.Size.Width;
        var pixel = Pixel(image, (Number(quiet, "x") + Number(quiet, "width") / 2) * scale, (Number(quiet, "y") + Number(quiet, "height") / 2) * scale);
        Assert.False(IsClose(pixel, Pass), "The quiet cell should be lighter than the state colour in the PNG too.");
        Assert.True(pixel.G > pixel.R && pixel.G > pixel.B, "The quiet cell keeps the hue of its state.");
    }

    [Fact]
    public void StatePatterns_ApplyToTimelineSegmentsGanttItemsAndTheLegend() {
        var states = new[] {
            new ChartStateCategory("up", "Up", Pass, emphasis: ChartStateEmphasis.Quiet),
            new ChartStateCategory("unknown", "Unknown", Neutral, ChartStatePattern.CrossHatched),
            new ChartStateCategory("skipped", "Skipped", Neutral, ChartStatePattern.Outlined)
        };
        var timeline = XDocument.Parse(Chart.Create().WithSize(720, 320).WithStateCategories(states)
            .AddStateTimelineLane("A", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(4), "up"), new ChartStateTimelineSegment(Day.AddHours(4), Day.AddHours(8), "unknown"), new ChartStateTimelineSegment(Day.AddHours(8), Day.AddHours(12), "skipped") }).ToSvg());
        Assert.Equal(new string?[] { null, "cross-hatched", "outlined" }, ByRole(timeline, "state-segment").Select(segment => (string?)segment.Attribute("data-cfx-pattern")).ToArray());
        Assert.Equal("quiet", (string?)ByRole(timeline, "state-segment")[0].Attribute("data-cfx-emphasis"));
        Assert.Equal(new string?[] { null, "cross-hatched", "outlined" }, ByRole(timeline, "state-legend-swatch").Select(swatch => (string?)swatch.Attribute("data-cfx-pattern")).ToArray());

        var lanes = Chart.Create().WithSize(720, 320).WithStateCategories(states)
            .AddGanttLane("A", new[] { new ChartGanttLaneItem(Day, Day.AddHours(4), "up"), new ChartGanttLaneItem(Day.AddHours(5), Day.AddHours(8), "unknown"), new ChartGanttLaneItem(Day.AddHours(9), Day.AddHours(12), "skipped") });
        var gantt = XDocument.Parse(lanes.ToSvg());
        Assert.Equal(new string?[] { null, "cross-hatched", "outlined" }, ByRole(gantt, "gantt-lane-item").Select(item => (string?)item.Attribute("data-cfx-pattern")).ToArray());
        Assert.Single(ByRole(gantt, "gantt-lane-item-hatch"));
        Assert.True(lanes.ToPng().Length > 64);
    }

    [Fact]
    public void StatusTokens_GiveTheNeutralStatesDifferentPatterns() {
        var tokens = new VisualStatusTokens();
        var operational = tokens.OperationalStateCategories().ToDictionary(state => state.Key, state => state.Pattern);
        Assert.Equal(ChartStatePattern.Hatched, operational["notObservable"]);
        Assert.Equal(ChartStatePattern.CrossHatched, operational["unknown"]);
        var outcomes = tokens.OutcomeCategories().ToDictionary(state => state.Key, state => state.Pattern);
        Assert.Equal(ChartStatePattern.Hatched, outcomes["notEvaluated"]);
        Assert.Equal(ChartStatePattern.Outlined, outcomes["couldNotEvaluate"]);
    }

    private static Chart Timeline(bool grouped = true) {
        var chart = Chart.Create().WithSize(760, 420).WithXAxisTimeScale(showTimeZone: true)
            .WithStateCategories(new ChartStateCategory("up", "Up", Pass), new ChartStateCategory("down", "Down", Critical));
        chart.Options.LaneSummaryHeader = "Available";
        ChartStateTimelineSegment[] Segments() => new[] { new ChartStateTimelineSegment(Day, Day.AddHours(18), "up"), new ChartStateTimelineSegment(Day.AddHours(18), Day.AddHours(24), "down") };
        return chart
            .AddStateTimelineLane("A1", Segments(), "75%", grouped ? "North" : null)
            .AddStateTimelineLane("A2", Segments(), "75%", grouped ? "North" : null)
            .AddStateTimelineLane("B1", Segments(), "75%", grouped ? "South" : null)
            .AddStateTimelineLane("Spare", Segments(), "75%");
    }

    private static Chart Matrix(bool grouped = true) => Chart.Create().WithSize(760, 440)
        .WithStateCategories(
            new ChartStateCategory("pass", "Passed", Pass, emphasis: ChartStateEmphasis.Quiet),
            new ChartStateCategory("critical", "Critical", Critical),
            new ChartStateCategory("notEvaluated", "Not evaluated", Neutral, ChartStatePattern.Hatched),
            new ChartStateCategory("unknown", "Unknown", Neutral, ChartStatePattern.CrossHatched),
            new ChartStateCategory("couldNotEvaluate", "Could not evaluate", Neutral, ChartStatePattern.Outlined))
        .WithXLabels("One", "Two", "Three")
        .AddHeatmapCategoryRow("A1", new ChartHeatmapCell?[] { new ChartHeatmapCell("pass"), new ChartHeatmapCell("critical"), new ChartHeatmapCell("notEvaluated") }, grouped ? "North" : null)
        .AddHeatmapCategoryRow("A2", new ChartHeatmapCell?[] { new ChartHeatmapCell("unknown"), new ChartHeatmapCell("critical", tooltip: "Backup is 9 days old"), new ChartHeatmapCell("couldNotEvaluate") }, grouped ? "North" : null)
        .AddHeatmapCategoryRow("B1", new ChartHeatmapCell?[] { new ChartHeatmapCell("pass"), null, null }, grouped ? "South" : null)
        .AddHeatmapCategoryRow("Empty", new ChartHeatmapCell?[] { null, null, null }, grouped ? "South" : null);

    private static int InkCount(RgbaImage image, XElement cell, double scale, ChartColor color) {
        var count = 0;
        for (var y = (int)(Number(cell, "y") * scale) + 2; y < (int)((Number(cell, "y") + Number(cell, "height")) * scale) - 2; y++) {
            for (var x = (int)(Number(cell, "x") * scale) + 2; x < (int)((Number(cell, "x") + Number(cell, "width")) * scale) - 2; x++) {
                if (IsClose(Pixel(image, x, y), color)) count++;
            }
        }

        return count;
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static double Number(XElement element, string attribute) => double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);

    private static (byte R, byte G, byte B) Pixel(RgbaImage image, double x, double y) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        return (image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2]);
    }

    private static bool IsClose((byte R, byte G, byte B) pixel, ChartColor color) =>
        Math.Abs(pixel.R - color.R) <= 12 && Math.Abs(pixel.G - color.G) <= 12 && Math.Abs(pixel.B - color.B) <= 12;
}
