using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttCalendarDateTests {
    [Theory]
    [InlineData("Do MMMM YYYY", "2nd January 2026", "3rd January 2026", "4th January 2026")]
    [InlineData("MMM D YYYY", "Jan 2 2026", "Jan 3 2026", "Jan 4 2026")]
    [InlineData("YYYY-MM-DD HH:mm", "2026-01-02 13:02", "2026-01-03 13:02", "2026-01-04 13:02")]
    [InlineData("Do-MMMM-YYYY", "2nd-January-2026", "03rd-January-2026", "04th-January-2026")]
    [InlineData("D-M-YYYY", "2-1-2026", "03-01-2026", "04-01-2026")]
    [InlineData("YYYY-MM-DD h:m:s A", "2026-01-02 1:2:3 PM", "2026-01-03 01:02:03 PM", "2026-01-04 01:02:03 PM")]
    [InlineData("YYYY-MM-DD HH:mm Z", "2026-01-02 13:02 +02:00", "2026-01-03 12:02 +01:00", "2026-01-04 11:02 +00:00")]
    [InlineData("X", "1767312000.1234567", "1767398400.1234567", "1767484800.1234567")]
    [InlineData("x", "1767312000000.1234", "1767398400000.1234", "1767484800000.1234")]
    public void CalendarDatesShareTaskParsingNormalizationAndPrecision(string format, string start, string excluded, string included) {
        // Declarations before dateFormat prove that collection cannot tokenize dates using a stale format.
        var source = "gantt\nexcludes " + excluded + "," + included + "\nincludes " + included
            + "\ndateFormat " + format + "\nTask :task," + start + ",2d";
        var parsed = new MermaidParser().ParseGantt(source);
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(task.Start.AddDays(3), task.End);
    }

    [Fact]
    public void SpaceSeparatedWeekdaysAndIsoDatesStillWorkWithFormattedDatesAndRepeatedRules() {
        var source = "gantt\ndateFormat MMM D YYYY\nexcludes weekends monday\nexcludes Jan 6 2026,2026-01-07\nincludes Jan 4 2026\nincludes 2026-01-06\nTask :task,Jan 2 2026,2d";
        var parsed = new MermaidParser().ParseGantt(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new DateTime(2026, 1, 6), Assert.Single(parsed.Document!.Tasks).End);
    }

    [Theory]
    [InlineData("YYYY-MM-DD HH:mm", "2026-01-02 13:02", "2026-01-03 14:02")]
    [InlineData("YYYY-MM-DD HH:mm Z", "2026-01-02 13:02 +02:00", "2026-01-03 13:02 +01:00")]
    public void AFormattedTimeCannotBecomeAnUnconditionalIsoDayExclusion(string format, string start, string excluded) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nexcludes " + excluded + "\nTask :task," + start + ",2d");
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(task.Start.AddDays(2), task.End);
    }

    [Theory]
    [InlineData("YYYY-MM-DD", "2026-01-02T13:02", "2026-01-03T14:02")]
    [InlineData("YYYY-MM-DD HH:mm", "2026-01-02 13:02", "2026-01-03 13:02:03")]
    public void TaskFallbackTimestampsDoNotLoseFieldsWhenUsedAsCalendarRules(string format, string start, string calendar) {
        var parser = new MermaidParser();
        var source = "gantt\ndateFormat " + format + "\nexcludes " + calendar + "\nTask :task," + start + ",2d";
        var excluded = parser.ParseGantt(source);
        Assert.Empty(excluded.Diagnostics);
        var task = Assert.Single(excluded.Document!.Tasks);
        Assert.Equal(task.Start.AddDays(2), task.End);
        var included = parser.ParseGantt("gantt\ndateFormat " + format + "\nexcludes 2026-01-03\nincludes " + calendar + "\nTask :task," + start + ",2d");
        Assert.Empty(included.Diagnostics);
        task = Assert.Single(included.Document!.Tasks);
        Assert.Equal(task.Start.AddDays(3), task.End);
    }

    [Theory]
    [InlineData("YYYY-D-M", "2026-2-1", "2026-03-01", 3)]
    [InlineData("YYYY-DD-MM", "2026-02-01", "2026-03-01", 3)]
    [InlineData("YYYY-D-M", "2026-28-2", "2026-03-01", 2)]
    [InlineData("YYYY-DD-MM", "2026-28-02", "2026-03-01", 2)]
    public void ConfiguredCalendarFieldsAndIsoDayKeysCannotCollide(string format, string start, string excluded, int days) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nexcludes " + excluded + "\nTask :task," + start + ",2d");
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(task.Start.AddDays(days), task.End);
    }

    [Fact]
    public void ConfiguredInclusionCannotOverrideAnUnrelatedIsoDayWithTheSameText() {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat YYYY-DD-MM\nexcludes sunday\nincludes 2026-03-01\nTask :task,2026-28-02,2d");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new DateTime(2026, 3, 3), Assert.Single(parsed.Document!.Tasks).End);
    }

    [Fact]
    public void MixedLegacyCalendarShorthandRetainsRecurringWeekdays() {
        var parsed = new MermaidParser().ParseGantt("gantt\nexcludes 2026-01-03 saturday\nTask :task,2026-01-02,10d");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new DateTime(2026, 1, 14), Assert.Single(parsed.Document!.Tasks).End);
    }

    [Theory]
    [InlineData("YYYY-MM-DD HH:mm", "2026-01-02 1:00")]
    [InlineData("YYYY-MM-DD", "January 2 2026")]
    [InlineData("YYYY-MM-DD", "2026-1-2")]
    [InlineData("YYYY-MM-DD", "2026-01-02T01:00")]
    public void RetainedStartDateFallbackMatchesAcceptedReferenceInputs(string format, string value) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nTask :task," + value + ",1d");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new DateTime(2026, 1, 2, value.Contains(":") ? 1 : 0, 0, 0), Assert.Single(parsed.Document!.Tasks).Start);
    }

    [Theory]
    [InlineData("X", "-1.1234567", "86398.8765433")]
    [InlineData("x", "-1.1234", "86399998.8766")]
    public void UnixCalendarMatchingPreservesAcceptedTicksAcrossTheEpoch(string format, string start, string excluded) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nexcludes " + excluded + "\nTask :task," + start + ",1d");
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(task.Start.AddDays(2), task.End);
    }
}
