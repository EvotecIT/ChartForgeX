using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
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
        var label = (string)points[1].Attribute("aria-label")!;
        Assert.StartsWith("dc-03, ", label, StringComparison.Ordinal);
        Assert.Contains("04:00", label, StringComparison.Ordinal);
        Assert.DoesNotContain("46316", label, StringComparison.Ordinal);
        Assert.DoesNotContain("(", label, StringComparison.Ordinal);
    }

    [Fact]
    public void WithValueUnit_AppearsInReadoutsButNotOnTicks() {
        var svg = XDocument.Parse(Latency().WithValueUnit("ms").ToSvg());
        var point = ByRole(svg, "point")[1];
        Assert.Equal("51.5 ms", (string?)point.Attribute("data-cfx-formatted-value"));
        Assert.EndsWith("51.5 ms", (string)point.Attribute("aria-label")!, StringComparison.Ordinal);
        Assert.DoesNotContain(ByRole(svg, "axis-y-label"), tick => tick.Value.Contains("ms", StringComparison.Ordinal));
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
    [InlineData("de-DE", "Okt", "51,5")]
    [InlineData("pl-PL", "paź", "51,5")]
    [InlineData("en-US", "Oct", "51.5")]
    public void WithCulture_LocalisesDayLabelsAndNumbers(string name, string month, string value) {
        var chart = Chart.Create().WithSize(900, 320).WithXAxisTimeScale(TimeZoneInfo.Utc)
            .AddLine("dc-03", Enumerable.Range(0, 8).Select(index => new ChartPoint(Start.AddDays(index), 50 + index * 1.5)))
            .WithCulture(CultureInfo.GetCultureInfo(name));
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Contains(ByRole(svg, "axis-x-label"), tick => tick.Value.Contains(month, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(value, (string?)ByRole(svg, "point")[1].Attribute("data-cfx-formatted-value"));
    }

    [Fact]
    public void WithCulture_KeepsAnAuthoredValueFormat() {
        var chart = Latency().WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.InvariantCulture)).WithCulture(CultureInfo.GetCultureInfo("de-DE"));
        Assert.Equal("51.50", (string?)ByRole(XDocument.Parse(chart.ToSvg()), "point")[1].Attribute("data-cfx-formatted-value"));
    }
}
