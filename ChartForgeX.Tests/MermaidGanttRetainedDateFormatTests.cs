using System.Globalization;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttRetainedDateFormatTests {
    [Theory]
    [InlineData("MM-dd HH:mm zzz", "02-20 00:30 +14:00", "2028-02-19T10:30:00")]
    [InlineData("%m/%d %H:%M zzz", "02/20 00:30 +14:00", "2028-02-19T10:30:00")]
    [InlineData("MM-dd HH:mm K", "02-20 00:30 +14:00", "2028-02-19T10:30:00")]
    [InlineData("MM-dd HH:mm K", "02-20 00:30 Z", "2028-02-20T00:30:00")]
    [InlineData("MM-dd HH:mm 'zzz'", "02-20 00:30 zzz", "2028-02-20T00:30:00")]
    [InlineData("M", "February 29", "2028-02-29T00:00:00")]
    [InlineData("m", "February 29", "2028-02-29T00:00:00")]
    public void RetainedFormatsUseTheAnchorAndAHostIndependentTime(string format, string input, string expected) {
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nTask :" + input + ",1d",
            new MermaidParseOptions { GanttDateAnchor = new DateTime(2028, 2, 20) });
        Assert.Empty(result.Diagnostics);
        var task = Assert.Single(result.Document!.Tasks);
        Assert.Equal(expected, task.Start.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
        Assert.Equal(DateTimeKind.Unspecified, task.Start.Kind);
    }

    [Theory]
    [InlineData("2026-01-02T00:30:00+14:00", "2026-01-01T10:30:00")]
    [InlineData("2026-01-02T00:30:00Z", "2026-01-02T00:30:00")]
    [InlineData("2026-01-02T00:30:00", "2026-01-02T00:30:00")]
    public void CompleteIsoFallbacksUseTheSameUtcAndUnspecifiedContract(string input, string expected) {
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat MM-DD\nTask :" + input + ",1d");
        Assert.Empty(result.Diagnostics);
        var task = Assert.Single(result.Document!.Tasks);
        Assert.Equal(expected, task.Start.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
        Assert.Equal(DateTimeKind.Unspecified, task.Start.Kind);
    }

    [Theory]
    [InlineData("  excludes weekends, 02-29", 22)]
    [InlineData("  excludes weekends 02-29", 21)]
    public void MissingCalendarAnchorsLocateTheActualDateToken(string line, int column) {
        var result = new MermaidParser().ParseGantt("gantt\n" + line + "\ndateFormat MM-DD\nTask :2028-02-28,2d");
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Message.Contains("GanttDateAnchor", StringComparison.Ordinal));
        Assert.Equal(2, diagnostic.Span.Line);
        Assert.Equal(column, diagnostic.Span.Column);
        Assert.Equal("02-29", line.Substring(diagnostic.Span.Column - 1, diagnostic.Span.Length));
    }
}
