using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Calendar weeks, localized words, cell sizing, and neutral zero counts.</summary>
public sealed class CalendarHeatmapOptionsTests {
    private static readonly string[] GermanDays = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };
    private static readonly string[] GermanMonths = { "Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };

    [Fact]
    public void FirstDayOfWeek_StartsEveryWeekOnThatDay() {
        var svg = XDocument.Parse(TwoWeeks(DayOfWeek.Monday).ToSvg());
        var calendar = ByRole(svg, "calendar-heatmap").Single();
        Assert.Equal("2026-09-07", (string?)calendar.Attribute("data-cfx-start"));
        Assert.Equal("Monday", (string?)calendar.Attribute("data-cfx-first-day"));

        var cells = ByRole(svg, "calendar-cell");
        var monday = cells.Single(cell => (string?)cell.Attribute("data-cfx-date") == "2026-09-07");
        var sunday = cells.Single(cell => (string?)cell.Attribute("data-cfx-date") == "2026-09-13");
        Assert.Equal("0", (string?)monday.Attribute("data-cfx-row"));
        Assert.Equal("6", (string?)sunday.Attribute("data-cfx-row"));
        Assert.Equal((string?)monday.Attribute("data-cfx-week-index"), (string?)sunday.Attribute("data-cfx-week-index"));
        Assert.Equal("0", (string?)sunday.Attribute("data-cfx-weekday-index"));
        Assert.NotEqual(TwoWeeks(DayOfWeek.Sunday).ToPng(), TwoWeeks(DayOfWeek.Monday).ToPng());
    }

    [Fact]
    public void DayAndMonthNamesAndScaleWords_AreLocalized() {
        // Small cells: weekday labels go on Monday, Wednesday, and Friday only.
        var chart = TwoWeeks(DayOfWeek.Monday, GermanDays, GermanMonths).WithCalendarHeatmapCells(maximumSize: 9).WithLabels(labels => {
            labels.Less = "Weniger";
            labels.More = "Mehr";
            labels.NoData = "Keine Daten";
            labels.AccessibleTextFormatter = facts => facts.Kind == ChartDescriptionKind.CalendarHeatmapGroup
                ? string.Format(CultureInfo.InvariantCulture, "{0}: {1:yyyy-MM-dd} bis {2:yyyy-MM-dd}, {3} Tage mit Änderungen von {4} Tagen, {5} ohne Daten", facts.SeriesNames[0], facts.FirstDate, facts.LastDate, facts.Count, facts.Count + facts.ZeroCount, facts.MissingCount)
                : null;
        });
        var svg = chart.ToSvg();
        var document = XDocument.Parse(svg);

        var weekdays = ByRole(document, "calendar-weekday").Select(label => label.Value).ToArray();
        Assert.Equal(new[] { "Mo", "Mi", "Fr" }, weekdays);
        Assert.All(weekdays, day => Assert.Contains(day, GermanDays));
        Assert.Equal(new[] { "Sep" }, ByRole(document, "calendar-month").Select(label => label.Value).ToArray());
        Assert.Equal(new[] { "Keine Daten", "Weniger – Mehr" }, ByRole(document, "calendar-scale-label").Select(label => label.Value).ToArray());
        Assert.Contains("Keine Daten", svg, StringComparison.Ordinal);
        // The Tuesday at zero is a day in the data without changes; the weekends have no data.
        Assert.Equal("Changes: 2026-09-07 bis 2026-09-20, 9 Tage mit Änderungen von 10 Tagen, 4 ohne Daten", (string?)ByRole(document, "calendar-heatmap").Single().Attribute("aria-label"));
        Assert.DoesNotContain(">Less<", svg, StringComparison.Ordinal);
        Assert.DoesNotContain(">Mon<", svg, StringComparison.Ordinal);
        Assert.NotEqual(TwoWeeks(DayOfWeek.Monday).ToPng(), chart.ToPng());
    }

    [Fact]
    public void Names_MustCoverEveryDayAndMonth() {
        Assert.Throws<ArgumentException>(() => Chart.Create().AddCalendarHeatmap("C", Items(), dayNames: new[] { "Mo" }));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddCalendarHeatmap("C", Items(), monthNames: GermanDays));
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart.Create().WithCalendarHeatmapCells(size: 1));
    }

    [Fact]
    public void Cells_GrowToTheShorterSideAndStopAtTheMaximum() {
        double CellSize(Chart chart) => double.Parse((string)ByRole(XDocument.Parse(chart.ToSvg()), "calendar-cell")[0].RenderedAttribute("width")!, CultureInfo.InvariantCulture);

        var small = CellSize(TwoWeeks(DayOfWeek.Monday).WithSize(560, 300));
        var tall = CellSize(TwoWeeks(DayOfWeek.Monday).WithSize(560, 560));
        Assert.True(tall > small * 2, "Taller charts give the calendar larger cells.");
        Assert.Equal(20, CellSize(TwoWeeks(DayOfWeek.Monday).WithSize(560, 560).WithCalendarHeatmapCells(maximumSize: 20)), 3);
        Assert.Equal(16, CellSize(TwoWeeks(DayOfWeek.Monday).WithSize(560, 560).WithCalendarHeatmapCells(size: 16)), 3);
        Assert.Equal(small, CellSize(TwoWeeks(DayOfWeek.Monday).WithSize(560, 300).WithCalendarHeatmapCells(size: 60)), 3);
        var gapped = ByRole(XDocument.Parse(TwoWeeks(DayOfWeek.Monday).WithSize(560, 560).WithCalendarHeatmapCells(size: 16, gap: 6).ToSvg()), "calendar-cell");
        var first = double.Parse((string)gapped[0].RenderedAttribute("y")!, CultureInfo.InvariantCulture);
        var second = double.Parse((string)gapped[1].RenderedAttribute("y")!, CultureInfo.InvariantCulture);
        Assert.Equal(22, second - first, 3);
    }

    [Fact]
    public void ZeroCount_IsNeutralAndTheRampStartsAtTheSmallestNonZeroValue() {
        var chart = TwoWeeks(DayOfWeek.Monday);
        var svg = XDocument.Parse(chart.ToSvg());
        XElement Day(string date) => ByRole(svg, "calendar-cell").Single(cell => (string?)cell.Attribute("data-cfx-date") == date);

        var zero = Day("2026-09-08");
        var one = Day("2026-09-07");
        Assert.Equal("0", (string?)zero.Attribute("data-cfx-level"));
        Assert.Equal("1", (string?)one.Attribute("data-cfx-level"));
        Assert.NotEqual((string?)zero.RenderedAttribute("fill"), (string?)one.RenderedAttribute("fill"));
        Assert.Equal((string?)ByRole(svg, "calendar-scale-step").Single(step => step.Attribute("data-cfx-zero")?.Value == "true").RenderedAttribute("fill"), (string?)zero.RenderedAttribute("fill"));
        var firstStep = ByRole(svg, "calendar-scale-step").First(step => step.Attribute("data-cfx-empty")?.Value == "false" && step.Attribute("data-cfx-zero")?.Value == "false");
        Assert.Equal((string?)firstStep.RenderedAttribute("fill"), (string?)one.RenderedAttribute("fill"));
        Assert.Equal((string?)one.Attribute("data-cfx-level"), (string?)firstStep.Attribute("data-cfx-level"));
        var zeroColour = ChartColor.Parse((string)zero.RenderedAttribute("fill")!);
        Assert.True(Math.Abs(zeroColour.R - zeroColour.G) < 12 && Math.Abs(zeroColour.G - zeroColour.B) < 12, "A zero count is neutral, not a shade of the ramp hue.");
    }

    [Fact]
    public void CountHeatmap_ZeroBucket_IsNeutralInSvgAndPng() {
        var monday = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);
        var events = new[] { new ChartTimedValue(monday.AddHours(9)), new ChartTimedValue(monday.AddHours(9)), new ChartTimedValue(monday.AddHours(10)) };
        var chart = Chart.Create().WithSize(900, 360).AddHourWeekdayHeatmap(events, color: ChartColor.FromHex("#2a78d6"));
        var svg = XDocument.Parse(chart.ToSvg());
        var cells = ByRole(svg, "heatmap-cell");
        var empty = cells.First(cell => (string?)cell.Attribute("data-cfx-level") == "0");
        var busy = cells.First(cell => (string?)cell.Attribute("data-cfx-level") == "4");
        var neutral = ChartColor.Parse((string)empty.RenderedAttribute("fill")!);
        Assert.True(Math.Abs(neutral.R - neutral.B) < 12, "An hour with no events is neutral, not a pale blue.");
        Assert.NotEqual((string?)empty.RenderedAttribute("fill"), (string?)busy.RenderedAttribute("fill"));
        Assert.True(chart.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Saturday)]
    public void AnyFirstDay_KeepsWholeWeeksAndLabelsTheSameRowsInSvgAndPng(DayOfWeek firstDay) {
        var chart = TwoWeeks(firstDay).WithCalendarHeatmapCells(maximumSize: 9);
        var svg = XDocument.Parse(chart.ToSvg());
        var cells = ByRole(svg, "calendar-cell");
        Assert.Equal(0, cells.Length % 7);
        Assert.Equal(firstDay, cells.Select(cell => DateTime.Parse((string)cell.Attribute("data-cfx-date")!, CultureInfo.InvariantCulture)).Min().DayOfWeek);
        Assert.All(cells, cell => Assert.Equal(((int.Parse((string)cell.Attribute("data-cfx-weekday-index")!, CultureInfo.InvariantCulture) - (int)firstDay + 7) % 7).ToString(CultureInfo.InvariantCulture), (string?)cell.Attribute("data-cfx-row")));
        Assert.Equal(new[] { "Fri", "Mon", "Wed" }, ByRole(svg, "calendar-weekday").Select(label => label.Value).OrderBy(day => day, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void WindowStartingAtTheEndOfAMonth_NamesTheNextMonthInstead() {
        var items = Enumerable.Range(0, 60).Select(day => new ChartCalendarHeatmapItem(new DateTime(2026, 1, 31).AddDays(day), day % 4)).ToArray();
        var svg = XDocument.Parse(Chart.Create().WithSize(760, 300).WithCalendarHeatmapCells(maximumSize: 10).AddCalendarHeatmap("C", items).ToSvg());
        var months = ByRole(svg, "calendar-month").Select(label => label.Value).ToArray();
        Assert.Equal("Feb", months[0]);
        Assert.Contains("Mar", months);
        Assert.DoesNotContain("Jan", months);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShortCard_TakesThePaddingSoCellsStayReadable(bool header) {
        // A 230 px card with the default padding used to leave the calendar 1 px cells.
        var items = Enumerable.Range(0, 90).Select(day => new ChartCalendarHeatmapItem(new DateTime(2026, 6, 1).AddDays(day), day % 5)).ToArray();
        var chart = Chart.Create().WithSize(760, 230).WithHeader(header).WithTitle("Changes").AddCalendarHeatmap("Changes", items);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = XDocument.Parse(prepared.ToSvg());
        var cells = ByRole(svg, "calendar-cell");
        var size = double.Parse((string)cells[0].RenderedAttribute("width")!, CultureInfo.InvariantCulture);
        Assert.True(size >= 8, "Cells should stay readable, got " + size.ToString(CultureInfo.InvariantCulture) + " px.");
        var bottom = cells.Max(cell => double.Parse((string)cell.RenderedAttribute("y")!, CultureInfo.InvariantCulture)) + size;
        var scale = ByRole(svg, "calendar-scale-step").Max(step => double.Parse((string)step.RenderedAttribute("y")!, CultureInfo.InvariantCulture) + double.Parse((string)step.RenderedAttribute("height")!, CultureInfo.InvariantCulture));
        Assert.True(bottom < scale && scale <= 230, "The scale stays below the days and inside the chart.");
        if (header) Assert.True(cells.Min(cell => double.Parse((string)cell.RenderedAttribute("y")!, CultureInfo.InvariantCulture)) > 60, "The days stay below the title.");

        // Weekday captions identify fixed rows. Collision handling may omit a caption, but must not move it
        // onto another row or over a day (the compact Linux face previously moved Friday into the first cell).
        var weekdayLabels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(label => label.Role == "calendar-weekday").ToArray();
        Assert.NotEmpty(weekdayLabels);
        var firstCellX = cells.Min(cell => double.Parse((string)cell.RenderedAttribute("x")!, CultureInfo.InvariantCulture));
        Assert.All(weekdayLabels, label => {
            var line = Assert.Single(label.Text.Lines);
            var row = int.Parse(label.Id!.Substring("calendar-weekday-".Length), CultureInfo.InvariantCulture);
            var rowTop = cells.Where(cell => (string?)cell.Attribute("data-cfx-row") == row.ToString(CultureInfo.InvariantCulture))
                .Min(cell => double.Parse((string)cell.RenderedAttribute("y")!, CultureInfo.InvariantCulture));
            Assert.True(label.LineLeft(line) + line.Width < firstCellX, "Weekday captions stay beside the days.");
            var center = label.Baseline - label.Text.Ascent + label.Text.Metrics.Height / 2;
            Assert.InRange(Math.Abs(center - (rowTop + size / 2)), 0, .001);
        });

        // The PNG lays the days out in the same frame: the darkest day is at the same place.
        var darkest = cells.Where(cell => (string?)cell.Attribute("data-cfx-level") == "4").First();
        var image = ChartForgeX.Raster.PngReader.Decode(chart.ToPng());
        var scaleFactor = image.Width / 760.0;
        var x = (int)((double.Parse((string)darkest.RenderedAttribute("x")!, CultureInfo.InvariantCulture) + size / 2) * scaleFactor);
        var y = (int)((double.Parse((string)darkest.RenderedAttribute("y")!, CultureInfo.InvariantCulture) + size / 2) * scaleFactor);
        var fill = ChartColor.Parse((string)darkest.RenderedAttribute("fill")!);
        var offset = (y * image.Width + x) * 4;
        Assert.True(Math.Abs(image.Pixels[offset] - fill.R) < 16 && Math.Abs(image.Pixels[offset + 1] - fill.G) < 16 && Math.Abs(image.Pixels[offset + 2] - fill.B) < 16, "The PNG draws the same day at the same place.");
    }

    [Fact]
    public void PaddingSetOnTheChart_IsHonouredByTheCalendar() {
        var items = Enumerable.Range(0, 90).Select(day => new ChartCalendarHeatmapItem(new DateTime(2026, 6, 1).AddDays(day), day % 5)).ToArray();
        double Top(Chart chart) => ByRole(XDocument.Parse(chart.ToSvg()), "calendar-cell").Min(cell => double.Parse((string)cell.RenderedAttribute("y")!, CultureInfo.InvariantCulture));
        var padded = Chart.Create().WithSize(760, 360).WithHeader(false).WithPadding(40, 120, 40, 20).AddCalendarHeatmap("Changes", items);
        Assert.True(Top(padded) >= 120 + 24, "The calendar starts below the padding and its month labels.");
        var automatic = Chart.Create().WithSize(760, 360).WithHeader(false).AddCalendarHeatmap("Changes", items);
        Assert.True(Top(automatic) < 78, "With the default padding the calendar uses the chart area.");
    }

    [Fact]
    public void DateFormatter_NamesDaysInTheLabelsLanguage_AndKeepsIsoData() {
        var german = CultureInfo.GetCultureInfo("de-DE");
        var chart = TwoWeeks(DayOfWeek.Monday).WithLabels(labels => labels.DateFormatter = day => day.ToString("D", german));
        var cell = ByRole(XDocument.Parse(chart.ToSvg()), "calendar-cell").Single(element => (string?)element.Attribute("data-cfx-date") == "2026-09-07");
        var name = new DateTime(2026, 9, 7).ToString("D", german);
        Assert.Equal("Changes, " + name + ": 1", (string?)cell.Attribute("aria-label"));
        Assert.Equal("Changes, " + name + ": 1", cell.Tooltip());
        Assert.Null(cell.Attribute("tabindex"));

        var iso = ByRole(XDocument.Parse(TwoWeeks(DayOfWeek.Monday).WithLabels(labels => labels.DateFormatter = _ => " ").ToSvg()), "calendar-cell")
            .Single(element => (string?)element.Attribute("data-cfx-date") == "2026-09-07");
        Assert.Equal("Changes, 2026-09-07: 1", (string?)iso.Attribute("aria-label"));
    }

    [Fact]
    public void WithCalendarHeatmapCells_InvalidValue_ChangesNothing() {
        var chart = Chart.Create().WithCalendarHeatmapCells(size: 12, maximumSize: 30, gap: 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithCalendarHeatmapCells(size: 14, maximumSize: 500));
        Assert.Equal(12, chart.Options.CalendarCellSize);
        Assert.Equal(30, chart.Options.CalendarMaximumCellSize);
        Assert.Equal(2, chart.Options.CalendarCellGap);
    }

    [Fact]
    public void YearCalendar_LargePreferredGap_KeepsCellsInsideViewport() {
        var chart = Chart.Create().WithSize(760, 220).WithHeader(false).WithCard(false)
            .WithCalendarHeatmapCells(gap: 24)
            .AddCalendarHeatmap("Year", Enumerable.Range(0, 365).Select(i => new ChartCalendarHeatmapItem(new DateTime(2025, 1, 1).AddDays(i), i % 5)).ToArray());
        var cells = ByRole(XDocument.Parse(chart.ToSvg()), "calendar-cell");
        Assert.Equal(371, cells.Length); // Whole weeks include six days outside the requested year.
        foreach (var cell in cells) {
            double Number(string name) => double.Parse((string)cell.RenderedAttribute(name)!, CultureInfo.InvariantCulture);
            Assert.InRange(Number("x") + Number("width"), 0, 760);
            Assert.InRange(Number("y") + Number("height"), 0, 220);
        }
        Assert.True(chart.ToPng().Length > 64);
    }

    private static Chart TwoWeeks(DayOfWeek firstDay, IReadOnlyList<string>? days = null, IReadOnlyList<string>? months = null) =>
        Chart.Create().WithSize(560, 300).AddCalendarHeatmap("Changes", Items(), ChartColor.FromHex("#2a78d6"), firstDay, days, months);

    private static ChartCalendarHeatmapItem[] Items() {
        // Two weeks from Monday 2026-09-07; the Tuesday has an explicit zero and the weekend has no data.
        var values = new[] { 1, 0, 3, 5, 2, -1, -1, 4, 6, 2, 1, 3, -1, -1 };
        var items = new List<ChartCalendarHeatmapItem>();
        for (var i = 0; i < values.Length; i++) {
            if (values[i] >= 0) items.Add(new ChartCalendarHeatmapItem(new DateTime(2026, 9, 7).AddDays(i), values[i]));
        }

        return items.ToArray();
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
