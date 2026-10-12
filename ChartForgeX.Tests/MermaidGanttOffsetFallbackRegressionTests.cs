using System.Globalization;
using System.Text.Json;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttOffsetFallbackRegressionTests {
    [Fact]
    public void OffsetFallbacksMatchPinnedReferenceTimestamps() {
        var root = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures");
        var parsed = new MermaidParser().ParseGantt(File.ReadAllText(Path.Combine(root, "gantt-offset-fallback.mmd")));
        Assert.Empty(parsed.Diagnostics);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "gantt-offset-fallback.expected.json")));
        var timestamps = expected.RootElement.GetProperty("taskTimestamps").EnumerateArray()
            .Select(item => (item[0].GetString(), item[1].GetString()!, item[2].GetString()!));
        Assert.Equal(timestamps, parsed.Document!.Tasks.Select(task => (task.Id,
            task.Start.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture),
            task.End.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture))));
    }

    [Theory]
    [InlineData("2026-01-02 00:30:00+14:00", "2026-01-01T10:30:00.0000000")]
    [InlineData("2026-01-02 00:30:12.1234567+14:00", "2026-01-01T10:30:12.1234567")]
    [InlineData("2026-01-02 0:30:12-12:00", "2026-01-02T12:30:12.0000000")]
    [InlineData("2026-01-02 00:30:00Z", "2026-01-02T00:30:00.0000000")]
    [InlineData("2026-01-02T0:30:12.1234567+14:00", "2026-01-01T10:30:12.1234567")]
    [InlineData("Jan 2, 2026 1:30 PM +02:00", "2026-01-02T11:30:00.0000000")]
    [InlineData("January 2 2026 1:30:12.1234567 PM +14:00", "2026-01-01T23:30:12.1234567")]
    [InlineData("1/2/2026 1:30 AM -12:00", "2026-01-02T13:30:00.0000000")]
    [InlineData("2 Jan 2026 1:30:12 PMZ", "2026-01-02T13:30:12.0000000")]
    public void CompleteOffsetFallbackRetainsSecondsFractionAndUtcMeaning(string input, string expected) {
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat MM-DD\nTask :task," + input + ",1d");
        Assert.Empty(result.Diagnostics);
        var start = Assert.Single(result.Document!.Tasks).Start;
        Assert.Equal(expected, start.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        Assert.Equal(DateTimeKind.Unspecified, start.Kind);
    }
}
