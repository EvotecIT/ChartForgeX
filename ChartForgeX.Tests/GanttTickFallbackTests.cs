using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GanttTickFallbackTests {
    [Theory]
    [InlineData(-300, "2026-01-08", "2026-01-22")]
    [InlineData(0, "2026-01-01", "2026-01-15", "2026-01-29")]
    [InlineData(345, "2026-01-01", "2026-01-15", "2026-01-29")]
    public void WeeklyParityUsesTheEpochInstantInTheDisplayZone(int offsetMinutes, params string[] expected) {
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        var zone = TimeZoneInfo.CreateCustomTimeZone("TickParity" + offsetMinutes, offset, "Tick parity", "Tick parity");
        var ticks = ChartTimeScale.GenerateFixed(new ChartAxis { TimeZone = zone }, new DateTime(2025, 12, 31).ToOADate(),
            new DateTime(2026, 2, 1).ToOADate(), new ChartTimeTickInterval(ChartTimeTickUnit.Week, 2, DayOfWeek.Thursday));
        Assert.NotNull(ticks);
        var local = ticks.Select(value => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(DateTime.FromOADate(value), DateTimeKind.Utc), zone))
            .Where(value => value.Month == 1).Select(value => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Assert.Equal(expected, local);
    }

    [Fact]
    public void DenseMermaidIntervalsUseTheAutomaticLabelFormatAndKeepExplicitOverrides() {
        const string source = "gantt\ntickInterval 1millisecond\nTask :2026-01-01, 1d";
        var document = new MermaidParser().ParseGantt(source).Document!;
        var automatic = new MermaidParser().ParseGantt(source.Replace("tickInterval 1millisecond\n", "")).Document!.ToChart();
        var chart = document.ToChart();
        Assert.Equal(Labels(automatic), Labels(chart));
        Assert.NotEmpty(chart.ToPng());
        document.AxisFormat = "%d/%m";
        var authored = document.ToChart();
        Assert.All(Labels(authored), label => Assert.Matches("^0[12]/01$", label));
        authored.WithXAxisValueFormatter(value => "Host " + value.ToString("0", CultureInfo.InvariantCulture));
        Assert.All(Labels(authored), label => Assert.StartsWith("Host ", label));
    }

    [Fact]
    public void HostWindowChangesReevaluateTheEffectiveIntervalAndDefaultFormat() {
        var chart = new MermaidParser().ParseGantt("gantt\ntickInterval 100millisecond\nTask :2026-01-01, 300ms").Document!.ToChart();
        Assert.Contains("2026-01-01 00:00:00.100", Labels(chart));
        chart.ConfigureXAxis(axis => { axis.Minimum = new DateTime(2026, 1, 1).ToOADate(); axis.Maximum = new DateTime(2026, 1, 2).ToOADate(); });
        Assert.All(Labels(chart), label => Assert.DoesNotContain(" ", label));
        chart.ConfigureXAxis(axis => axis.Maximum = new DateTime(2026, 1, 1).AddMilliseconds(300).ToOADate());
        Assert.Contains("2026-01-01 00:00:00.100", Labels(chart));
    }

    private static string[] Labels(Chart chart) => XDocument.Parse(chart.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "schedule-tick-label").Select(element => element.Value).ToArray();
}
