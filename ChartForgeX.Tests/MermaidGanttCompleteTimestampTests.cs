using System.Globalization;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttCompleteTimestampTests {
    [Theory]
    [InlineData("January 2 2026 13:30", "2026-01-02T13:30:00")]
    [InlineData("Jan 2, 2026 1:30 PM", "2026-01-02T13:30:00")]
    [InlineData("2 January 2026 13:30:15.125", "2026-01-02T13:30:15.125")]
    [InlineData("1/2/2026 13:30", "2026-01-02T13:30:00")]
    [InlineData("2026/1/2 13:30:15", "2026-01-02T13:30:15")]
    [InlineData("1-2-2026 13:30 +02:00", "2026-01-02T11:30:00")]
    public void CompleteFallbackDatesRetainAuthoredTimeWithoutClockDefaults(string input, string expected) {
        var parsed = new MermaidParser().ParseGantt("gantt\nTask :" + input + ",1h");
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(DateTime.Parse(expected, CultureInfo.InvariantCulture), task.Start);
        Assert.Equal(DateTimeKind.Unspecified, task.Start.Kind);
        Assert.Equal(task.Start.AddHours(1), task.End);
    }

    [Theory]
    [InlineData("January 2 13:30")]
    [InlineData("1/2 13:30")]
    [InlineData("13:30")]
    public void TimeBearingPartialDatesStillRequireAnExplicitAnchor(string input) {
        var parsed = new MermaidParser().ParseGantt("gantt\nTask :" + input + ",1h");
        Assert.True(parsed.HasErrors);
    }
}
