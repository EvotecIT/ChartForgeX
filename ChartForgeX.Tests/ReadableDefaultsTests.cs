using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Defaults from the chart quality bar: readable heatmap rows, regulated tick density, formatted readouts, units and locale.</summary>
public sealed class ReadableDefaultsTests {
    private static readonly DateTime Start = new(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

    private static XElement[] ByRole(XDocument svg, string role) =>
        svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    internal static Chart Availability(int width) {
        var chart = Chart.Create().WithTheme(VisualTheme.Report()).WithSize(width, 420).WithTitle("Daily availability");
        for (var row = 0; row < 14; row++)
            chart.AddHeatmapRow($"DC{row + 1}.ad.evotec.pl", Enumerable.Range(0, 30).Select(day => new ChartPoint(day, 99 + (row + day) % 10 / 10d)));
        return chart;
    }

    [Theory]
    [InlineData(390)]
    [InlineData(1100)]
    public void ToSvg_DenseHeatmap_DrawsEveryRowLabelAtTheReadableFloor(int width) {
        var svg = XDocument.Parse(Availability(width).ToSvg());
        var labels = ByRole(svg, "heatmap-row-label").SelectMany(label => label.DescendantsAndSelf().Where(element => element.Name.LocalName == "text")).ToArray();
        Assert.Equal(14, labels.Length);
        Assert.All(labels, label => Assert.True(double.Parse((string)label.Attribute("font-size")!, CultureInfo.InvariantCulture) >= 10));
    }

    [Fact]
    public void ToSvg_SmallHeatmapCells_StayRoundedSquaresRatherThanDots() {
        var svg = XDocument.Parse(Availability(390).ToSvg());
        var cell = ByRole(svg, "heatmap-cell-shape").First();
        var size = Math.Min(double.Parse((string)cell.Attribute("width")!, CultureInfo.InvariantCulture), double.Parse((string)cell.Attribute("height")!, CultureInfo.InvariantCulture));
        var radius = double.Parse((string?)cell.Attribute("rx") ?? "0", CultureInfo.InvariantCulture);
        Assert.InRange(radius, 0, size * .2 + .001);
    }

    [Fact]
    public void ToSvg_WideNumericBarAxis_KeepsAboutOneTickPerEightyPixels() {
        var chart = Chart.Create().WithSize(900, 360).WithLegend(false)
            .AddBar("Orders", Enumerable.Range(0, 7).Select(index => new ChartPoint(index * 8, 20 + index * 3)));
        var ticks = ByRole(XDocument.Parse(chart.ToSvg()), "axis-x-label");
        Assert.InRange(ticks.Length, 4, 12);
    }

    [Theory]
    [InlineData(220)]
    [InlineData(360)]
    [InlineData(520)]
    public void ToSvg_ValueAxisOfUsefulLength_HasFourToNineTicks(int height) {
        var chart = Chart.Create().WithSize(640, height).WithLegend(false).WithHeader(false)
            .AddLine("Requests", new[] { new ChartPoint(1, 3), new ChartPoint(2, 41), new ChartPoint(3, 27), new ChartPoint(4, 45) });
        var ticks = ByRole(XDocument.Parse(chart.ToSvg()), "axis-y-label");
        Assert.InRange(ticks.Length, 4, 9);
    }

    private static Chart Latency() => Chart.Create().WithSize(800, 320).WithXAxisTimeScale(TimeZoneInfo.Utc)
        .AddLine("dc-03", Enumerable.Range(0, 6).Select(index => new ChartPoint(Start.AddHours(index * 4), 50 + index * 1.5)));

    [Fact]
    public void ToSvg_TimeSeriesPoint_AnnouncesFormattedDateAndValueInsteadOfRawCoordinates() {
        var points = ByRole(XDocument.Parse(Latency().ToSvg()), "point");
        var label = (string)points[1].Attribute("data-cfx-description")!;
        Assert.StartsWith("dc-03, ", label, StringComparison.Ordinal);
        Assert.Contains("04:00", label, StringComparison.Ordinal);
        Assert.DoesNotContain("46316", label, StringComparison.Ordinal);
        Assert.DoesNotContain("(", label, StringComparison.Ordinal);
    }

    [Fact]
    public void WithValueUnit_HeatmapCellReadoutsCarryTheUnit() {
        var chart = Chart.Create().WithSize(600, 300).WithXLabels("14 Oct", "15 Oct").WithValueUnit("%")
            .WithValueFormat(ChartValueFormat.Number("0.#", CultureInfo.InvariantCulture))
            .AddHeatmapRow("dc1", new double?[] { 99.5, 100 });

        var cell = ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-cell").First();

        Assert.Equal("dc1, 14 Oct: 99.5%", (string?)cell.Attribute("data-cfx-description"));
        Assert.Equal("99.5%", (string?)cell.Attribute("data-cfx-formatted-value"));
    }

    [Theory]
    // An appended word unit uses a non-breaking space; a format that writes the unit keeps its own spacing.
    [InlineData("0.0", "12.5\u00A0ms")]
    [InlineData("0.0 'ms'", "12.5 ms")]
    public void WithValueUnit_HeatmapColorDimensionFormatsKeepOneUnit(string format, string expected) {
        var chart = Chart.Create().WithSize(600, 300).WithXLabels("14 Oct", "15 Oct").WithValueUnit("ms")
            .AddHeatmapRow("dc1", new double?[] { 12.5, 30 })
            .ConfigureHeatmapColorDimension(options => options.ValueFormat = ChartValueFormat.Number(format, CultureInfo.InvariantCulture));

        var cell = ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-cell").First();

        Assert.Equal("dc1, 14 Oct: " + expected, (string?)cell.Attribute("data-cfx-description"));
        Assert.Equal(expected, (string?)cell.Attribute("data-cfx-formatted-value"));
    }

    [Theory]
    [InlineData("0.0", "12.5 ms")]
    [InlineData("0.0 'ms'", "12.5 ms")]
    public void WithValueUnit_CalendarReadoutsKeepOneUnit(string format, string expected) {
        var chart = Chart.Create().WithSize(600, 300).WithValueUnit("ms")
            .AddCalendarHeatmap("Latency", new[] { new ChartCalendarHeatmapItem(new DateTime(2026, 10, 1), 12.5) })
            .WithValueFormat(ChartValueFormat.Number(format, CultureInfo.InvariantCulture));

        var day = ByRole(XDocument.Parse(chart.ToSvg()), "calendar-cell").First(cell => (string?)cell.Attribute("data-cfx-empty") == "false");

        Assert.EndsWith(": " + expected, (string?)day.Attribute("data-cfx-description"));
        Assert.Equal(expected, (string?)day.Attribute("data-cfx-formatted-value"));
    }

    [Fact]
    public void WithValueUnit_CartesianFormatThatWritesTheUnitKeepsOneCopy() {
        var chart = Chart.Create().WithSize(600, 300).WithXLabels("Mon", "Tue").WithValueUnit("ms")
            .WithValueFormat(ChartValueFormat.Number("0.0 'ms'", CultureInfo.InvariantCulture))
            .AddLine("dc1", new[] { new ChartPoint(1, 12.5), new ChartPoint(2, 30) });

        var svg = chart.ToSvg();

        Assert.Contains("12.5 ms", svg);
        Assert.DoesNotContain("ms ms", svg);
        Assert.DoesNotContain("ms ms", svg);
    }

    [Fact]
    public void WithValueUnit_WaterfallTooltipValuesCarryTheUnitWhileTheirAccessibleNamesStayAsDescribed() {
        var chart = Chart.Create().WithSize(600, 300).WithXLabels("Open", "Change", "Total").WithValueUnit("ms")
            .AddSeries(ChartSeries.CreateWaterfall(new ChartSeriesId("w"), "Budget",
                new[] { ChartWaterfallItem.OpeningBalance(1, 100), ChartWaterfallItem.Delta(2, -20), ChartWaterfallItem.Total(3) }));

        var change = XDocument.Parse(chart.ToSvg()).Descendants().First(element => (string?)element.Attribute("data-cfx-formatted-delta") != null);

        Assert.Equal("-20\u00A0ms", (string?)change.Attribute("data-cfx-formatted-delta"));
        Assert.Equal("80\u00A0ms", (string?)change.Attribute("data-cfx-formatted-end"));
        Assert.Equal("Budget: Change -20 start=100 end=80 Change=-20", (string?)change.Attribute("data-cfx-description"));
    }

    [Fact]
    public void WithValueUnit_CategoricalHeatmapCellsNameTheirStateWithoutAUnit() {
        var chart = Chart.Create().WithSize(600, 300).WithXLabels("14 Oct", "15 Oct").WithValueUnit("%")
            .WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#1d8a52")))
            .AddHeatmapCategoryRow("dc1", new ChartHeatmapCell?[] { new ChartHeatmapCell("up"), new ChartHeatmapCell("up") });

        var cell = ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-cell").First();

        Assert.Equal("dc1, 14 Oct: Up", (string?)cell.Attribute("data-cfx-description"));
        Assert.Null(cell.Attribute("data-cfx-formatted-value"));
    }

    [Theory]
    [InlineData("0.0 'ms'", "s", "12.0 ms s")]
    [InlineData("0.0 's'", "s", "12.0 s")]
    [InlineData("0.0", "s", "12.0 s")]
    public void WithValueUnit_OnlyAWholeMatchingUnitCountsAsAlreadyWritten(string format, string unit, string expected) {
        var chart = Chart.Create().WithSize(600, 300).WithXLabels("Mon", "Tue").WithValueUnit(unit)
            .WithValueFormat(ChartValueFormat.Number(format, CultureInfo.InvariantCulture))
            .AddLine("dc1", new[] { new ChartPoint(1, 12), new ChartPoint(2, 30) });

        var point = XDocument.Parse(chart.ToSvg()).Descendants().First(element => (string?)element.Attribute("data-cfx-formatted-value") != null);

        Assert.Equal(expected, (string?)point.Attribute("data-cfx-formatted-value"));
    }

    [Fact]
    public void WithValueUnit_RoundedTimeReadoutKeepsTheExactValueInItsAccessibleName() {
        var chart = Chart.Create().WithSize(600, 300).WithXAxisTimeScale(TimeZoneInfo.Utc).WithValueUnit("ms")
            .WithValueFormat(ChartValueFormat.Number("0.0", CultureInfo.InvariantCulture))
            .AddLine("dc-03", new[] { new ChartPoint(Start, 52.734), new ChartPoint(Start.AddHours(1), 40) });

        var point = ByRole(XDocument.Parse(chart.ToSvg()), "point").First();

        Assert.EndsWith("52.7 ms (52.734 ms)", (string?)point.Attribute("data-cfx-description"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValueUnit_BlankSetThroughOptionsMeansNoUnit(string unit) {
        var line = Chart.Create().WithSize(600, 300).WithXLabels("Mon", "Tue").AddLine("dc1", new[] { new ChartPoint(1, 12), new ChartPoint(2, 30) });
        line.Options.ValueUnit = unit;
        var heatmap = Chart.Create().WithSize(600, 300).WithXLabels("Mon", "Tue").AddHeatmapRow("dc1", new double?[] { 99.5, 100 });
        heatmap.Options.ValueUnit = unit;
        var waterfall = WaterfallInterchangeTests.Model();
        waterfall.Options.ValueUnit = unit;
        var restored = VisualArtifactInterchangeEnvelope.FromJson(waterfall.ToVisualArtifact().ToInterchangeJson()).ToWaterfallChart();

        var point = ByRole(XDocument.Parse(line.ToSvg()), "point").First();
        Assert.Equal("12", (string?)point.Attribute("data-cfx-formatted-value") ?? "12");
        Assert.Equal("dc1, Mon: 99.5", (string?)ByRole(XDocument.Parse(heatmap.ToSvg()), "heatmap-cell").First().Attribute("data-cfx-description"));
        Assert.NotEmpty(restored.ToSvg());
    }

    [Fact]
    public void WithValueUnit_AppearsInReadoutsButNotOnTicks() {
        var svg = XDocument.Parse(Latency().WithValueUnit("ms").ToSvg());
        var point = ByRole(svg, "point")[1];
        Assert.Equal("51.5\u00A0ms", (string?)point.Attribute("data-cfx-formatted-value"));
        Assert.EndsWith("51.5\u00A0ms", (string)point.Attribute("data-cfx-description")!, StringComparison.Ordinal);
        Assert.DoesNotContain(ByRole(svg, "axis-y-label"), tick => tick.Value.Contains("ms", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithValueUnit_TransitionFramesMatchThePreparedReadoutsAndNames(bool time) {
        Chart Build(double value) => NativeCartesianValueBeforeControls.Model(false, time ? "elapsed-time" : "linear", value).WithValueUnit("ms");
        var renderer = new ChartForgeX.Interactivity.Html.HtmlInteractiveChartRenderer();
        var from = renderer.PrepareSection(Build(20)); var to = renderer.PrepareSection(Build(80)); var middle = renderer.PrepareSection(Build(50));
        var frames = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3);
        Assert.Equal(middle.PreparedVisual.ToSvg("unit-middle"), frames[1].ToSvg("unit-middle"));
        Assert.Contains("50\u00A0ms", frames[1].ToSvg("unit-middle"), StringComparison.Ordinal);
    }

    [Fact]
    public void ToSvg_OverfullHeatmap_HidesRowLabelsRatherThanOverlappingThem() {
        var chart = Chart.Create().WithSize(600, 260).WithLegend(false);
        for (var row = 0; row < 30; row++) chart.AddHeatmapRow($"target-{row:00}", Enumerable.Range(0, 10).Select(day => new ChartPoint(day, row + day)));
        var labels = ByRole(XDocument.Parse(chart.ToSvg()), "heatmap-row-label").SelectMany(label => label.DescendantsAndSelf().Where(element => element.Name.LocalName == "text"))
            .Select(label => (Y: double.Parse((string)label.Attribute("y")!, CultureInfo.InvariantCulture), Size: double.Parse((string)label.Attribute("font-size")!, CultureInfo.InvariantCulture)))
            .OrderBy(label => label.Y).ToArray();
        Assert.All(labels, label => Assert.True(label.Size >= 10));
        for (var index = 1; index < labels.Length; index++) Assert.True(labels[index].Y - labels[index - 1].Y >= labels[index].Size);
    }

    [Fact]
    public void TimeSeriesPoint_AuthoredCaptionThatPrefixesTheValueKeepsBothCaptionAndValue() {
        var chart = Latency().WithValueUnit("ms").WithDataLabels();
        chart.Series[0].WithPointLabel(0, "5");
        var label = (string)ByRole(XDocument.Parse(chart.ToSvg()), "point")[0].Attribute("data-cfx-description")!;
        Assert.EndsWith(", 5 (50\u00A0ms)", label, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.Scatter)]
    public void NumericPoint_AccessibleNameIncludesUnitAndExactCoordinates(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(800, 320).WithValueUnit("ms")
            .AddSeries(new ChartSeries(new ChartSeriesId("requests"), "Requests", kind,
                new[] { new ChartPoint(1.125, 50.25), new ChartPoint(2, 60) }));
        var point = ByRole(XDocument.Parse(chart.ToSvg()), "point")[0];
        var label = (string)point.Attribute("data-cfx-description")!;
        Assert.Contains("50.25\u00A0ms", label, StringComparison.Ordinal);
        Assert.EndsWith("(1.125, 50.25)", label, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ChartTimeTickUnit.Day)]
    [InlineData(ChartTimeTickUnit.Millisecond)]
    public void Gantt_FixedTimeTicksUseGermanCulture(ChartTimeTickUnit unit) {
        var end = unit == ChartTimeTickUnit.Day ? Start.AddDays(3) : Start.AddMilliseconds(800);
        var chart = Chart.Create().WithSize(1000, 320).WithLegend(false).WithCulture(CultureInfo.GetCultureInfo("de-DE"))
            .AddGanttTask("Task", Start, end).WithGanttTickInterval(new ChartTimeTickInterval(unit, unit == ChartTimeTickUnit.Day ? 1 : 200));
        var labels = ByRole(XDocument.Parse(chart.ToSvg()), "schedule-tick-label").Select(node => node.Value).ToArray();
        Assert.NotEmpty(labels);
        Assert.Contains(labels, label => label.Contains("21.10.2026", StringComparison.Ordinal));
        if (unit == ChartTimeTickUnit.Millisecond) Assert.Contains(labels, label => label.Contains(",200", StringComparison.Ordinal));
    }

    [Fact]
    public void MixedGantt_FixedMillisecondTicksKeepLocalizedDistinctInstants() {
        var chart = Chart.Create().WithSize(1000, 400).WithLegend(false).WithCulture(CultureInfo.GetCultureInfo("de-DE"))
            .AddGanttTask("Task", Start, Start.AddMilliseconds(800))
            .AddGanttLane("Service", new[] { new ChartGanttLaneItem(Start, Start.AddMilliseconds(800), "up") })
            .WithGanttTickInterval(new ChartTimeTickInterval(ChartTimeTickUnit.Millisecond, 200));
        var labels = ByRole(XDocument.Parse(chart.ToSvg()), "schedule-tick-label").Select(node => node.Value).ToArray();
        Assert.Contains(labels, label => label.Contains(",200", StringComparison.Ordinal));
        Assert.Equal(labels.Length, labels.Distinct().Count());
    }

    [Fact]
    public void WithCulture_FractionalSecondInstantsUseTheCulture() {
        var chart = Chart.Create().WithSize(800, 320).WithXAxisTimeScale(TimeZoneInfo.Utc)
            .AddLine("dc-03", Enumerable.Range(0, 4).Select(index => new ChartPoint(Start.AddSeconds(index + .25), 50 + index)))
            .WithCulture(CultureInfo.GetCultureInfo("de-DE"));
        var label = (string)ByRole(XDocument.Parse(chart.ToSvg()), "point")[0].Attribute("data-cfx-description")!;
        Assert.Contains("21.10.2026 00:00:00,250", label, StringComparison.Ordinal);
    }

    [Fact]
    public void WithValueUnit_SymbolAttachesWithoutSpace() {
        var point = ByRole(XDocument.Parse(Latency().WithValueUnit("%").ToSvg()), "point")[1];
        Assert.Equal("51.5%", (string?)point.Attribute("data-cfx-formatted-value"));
    }

    [Fact]
    public void Default_TimeLabelsStayInvariantIso() {
        var ticks = ByRole(XDocument.Parse(Latency().ToSvg()), "axis-x-label").Select(tick => tick.Value).ToArray();
        Assert.Contains(ticks, tick => tick.StartsWith("2026-10-2", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("de-DE", "Okt")]
    [InlineData("pl-PL", "paź")]
    [InlineData("en-US", "Oct")]
    public void WithCulture_LocalisesDayLabelsAndKeepsNumbersAndCompactTicks(string name, string month) {
        var chart = Chart.Create().WithSize(900, 320).WithXAxisTimeScale(TimeZoneInfo.Utc)
            .AddLine("Requests", Enumerable.Range(0, 8).Select(index => new ChartPoint(Start.AddDays(index), 1000 + index * 700)))
            .WithCulture(CultureInfo.GetCultureInfo(name));
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Contains(ByRole(svg, "axis-x-label"), tick => tick.Value.Contains(month, StringComparison.OrdinalIgnoreCase));
        // The default value policy, including compact value ticks, is unchanged by a date culture.
        var invariant = XDocument.Parse(Chart.Create().WithSize(900, 320).WithXAxisTimeScale(TimeZoneInfo.Utc)
            .AddLine("Requests", Enumerable.Range(0, 8).Select(index => new ChartPoint(Start.AddDays(index), 1000 + index * 700))).ToSvg());
        Assert.Equal(ByRole(invariant, "axis-y-label").Select(tick => tick.Value), ByRole(svg, "axis-y-label").Select(tick => tick.Value));
        Assert.Equal((string?)ByRole(invariant, "point")[1].Attribute("data-cfx-formatted-value"), (string?)ByRole(svg, "point")[1].Attribute("data-cfx-formatted-value"));
    }

    [Fact]
    public void WithCulture_YearlyTicksKeepTheirYear() {
        var chart = Chart.Create().WithSize(900, 320).WithXAxisTimeScale(TimeZoneInfo.Utc)
            .AddLine("Certificates", Enumerable.Range(0, 5).Select(index => new ChartPoint(new DateTime(2022 + index, 1, 1, 0, 0, 0, DateTimeKind.Utc), index)))
            .WithCulture(CultureInfo.GetCultureInfo("en-GB"));
        var ticks = ByRole(XDocument.Parse(chart.ToSvg()), "axis-x-label").Select(tick => tick.Value).ToArray();
        Assert.Equal(ticks.Length, ticks.Distinct().Count());
        Assert.Contains(ticks, tick => tick.Contains("2023", StringComparison.Ordinal));
    }

    [Fact]
    public void WithCulture_InvariantRestoresIsoDates() {
        var ticks = ByRole(XDocument.Parse(Latency().WithCulture(CultureInfo.GetCultureInfo("de-DE")).WithCulture(CultureInfo.InvariantCulture).ToSvg()), "axis-x-label")
            .Select(tick => tick.Value).ToArray();
        Assert.Contains(ticks, tick => tick.StartsWith("2026-10-2", StringComparison.Ordinal));
    }

    [Fact]
    public void WithCulture_KeepsAnAuthoredValueFormat() {
        var chart = Latency().WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.GetCultureInfo("de-DE"))).WithCulture(CultureInfo.GetCultureInfo("de-DE"));
        Assert.Equal("51,50", (string?)ByRole(XDocument.Parse(chart.ToSvg()), "point")[1].Attribute("data-cfx-formatted-value"));
    }
}
