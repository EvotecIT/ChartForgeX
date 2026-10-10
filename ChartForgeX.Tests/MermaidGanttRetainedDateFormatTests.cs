using System.Globalization;
using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttRetainedDateFormatTests {
    [Fact]
    public void MermaidFractionFormatsStillAllowCompleteIsoFallbacksAndDiagnoseInvalidInput() {
        var parser = new MermaidParser();
        var result = parser.ParseGantt("gantt\ndateFormat S\nTask :2026-01-02,1d");
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new DateTime(2026, 1, 2), Assert.Single(result.Document!.Tasks).Start);
        Assert.True(parser.ParseGantt("gantt\ndateFormat S\nTask :invalid,1d").HasErrors);
    }

    [Theory]
    [InlineData("zzz", "-12:00", "+00:00")]
    [InlineData("zz", "-12", "+00")]
    public void OffsetCalendarRulesMatchTheUtcDateAcrossTheHostDstBoundary(string field, string excludedOffset, string startOffset) {
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat yyyy-MM-dd " + field
            + "\nexcludes 2026-03-29 " + excludedOffset + "\nTask :2026-03-28 " + startOffset + ",2d");
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new DateTime(2026, 3, 31), Assert.Single(result.Document!.Tasks).End);
    }

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
    [InlineData("  excludes weekends 02-29 monday", 21)]
    public void MissingCalendarAnchorsLocateTheActualDateToken(string line, int column) {
        var result = new MermaidParser().ParseGantt("gantt\n" + line + "\ndateFormat MM-DD\nTask :2028-02-28,2d");
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Message.Contains("GanttDateAnchor", StringComparison.Ordinal));
        Assert.Equal(2, diagnostic.Span.Line);
        Assert.Equal(column, diagnostic.Span.Column);
        Assert.Equal("02-29", line.Substring(diagnostic.Span.Column - 1, diagnostic.Span.Length));
    }

    [Theory]
    [InlineData("MM-DD", "weekends 02-29 03-01 monday", "02-29", "03-01")]
    [InlineData("MMM D", "weekends Feb 29 monday Mar 1", "Feb 29", "Mar 1")]
    [InlineData("M", "weekends February 29 monday March 1", "February 29", "March 1")]
    [InlineData("M", "2 3", "2", "3")]
    [InlineData("D", "2 3", "2", "3")]
    [InlineData("m", "2 3", "2", "3")]
    public void MissingCalendarAnchorsLocateEachDateWithoutSwallowingWeekdays(string format, string tokens, string first, string second) {
        var line = "excludes " + tokens;
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\n" + line + "\nTask :2028-02-28,2d");
        var dates = result.Diagnostics.Where(item => item.Message.Contains("GanttDateAnchor", StringComparison.Ordinal))
            .Select(item => line.Substring(item.Span.Column - 1, item.Span.Length)).ToArray();
        Assert.Equal(new[] { first, second }, dates);
    }
}
