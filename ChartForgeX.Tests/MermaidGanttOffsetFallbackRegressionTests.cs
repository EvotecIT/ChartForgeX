using System.Globalization;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttOffsetFallbackRegressionTests {
    [Theory]
    [InlineData("2026-01-02 00:30:00+14:00", "2026-01-01T10:30:00.0000000")]
    [InlineData("2026-01-02 00:30:12.1234567+14:00", "2026-01-01T10:30:12.1234567")]
    [InlineData("2026-01-02 0:30:12-12:00", "2026-01-02T12:30:12.0000000")]
    [InlineData("2026-01-02 00:30:00Z", "2026-01-02T00:30:00.0000000")]
    [InlineData("2026-01-02T0:30:12.1234567+14:00", "2026-01-01T10:30:12.1234567")]
    public void CompleteOffsetFallbackRetainsSecondsFractionAndUtcMeaning(string input, string expected) {
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat MM-DD\nTask :task," + input + ",1d");
        Assert.Empty(result.Diagnostics);
        var start = Assert.Single(result.Document!.Tasks).Start;
        Assert.Equal(expected, start.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        Assert.Equal(DateTimeKind.Unspecified, start.Kind);
    }
}
