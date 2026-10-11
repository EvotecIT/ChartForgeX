using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttCommaTaskDateTests {
    [Theory]
    [InlineData("active")]
    [InlineData("done")]
    [InlineData("crit")]
    [InlineData("milestone")]
    [InlineData("vert")]
    [InlineData("")]
    public void ConfiguredLiteralPrefixesAreDatesRatherThanStatusTags(string literal) {
        var source = "gantt\ndateFormat [" + literal + ",]YYYY-MM-DD\nTask :" + literal + ",2026-01-02,1d";
        var parsed = new MermaidParser().ParseGantt(source);
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Null(task.Id); Assert.Empty(task.Tags);
        Assert.Equal(0, task.Progress); Assert.False(task.IsMilestone); Assert.False(task.IsVerticalMarker);
        Assert.Equal(new DateTime(2026, 1, 2), task.Start);
        Assert.Equal(new DateTime(2026, 1, 3), task.End);
    }

    [Fact]
    public void StatusTagsBeforeConfiguredLiteralDatesKeepTheirMeaning() {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat [done,]YYYY-MM-DD\nTask :active,done,2026-01-02,1d");
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(new[] { "active" }, task.Tags);
        Assert.Equal(0, task.Progress); Assert.False(task.IsMilestone);
        Assert.Equal(new DateTime(2026, 1, 2), task.Start);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("active,", null)]
    [InlineData("task,", "task")]
    [InlineData("2026,", "2026")]
    public void CompleteConfiguredDateTakesPriorityOverAnIsoSuffix(string prefix, string? expectedId) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat [prefix,]YYYY-MM-DD\nTask :" + prefix + "prefix,2026-01-02,1d");
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(expectedId, task.Id);
        Assert.Equal(new DateTime(2026, 1, 2), task.Start);
        Assert.Equal(new DateTime(2026, 1, 3), task.End);
    }

    [Fact]
    public void RepeatedConfiguredDatePrefixesDoNotCreateDuplicateTaskIds() {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat [prefix,]YYYY-MM-DD\nFirst :prefix,2026-01-02,1d\nSecond :prefix,2026-01-04,prefix,2026-01-05");
        Assert.Empty(parsed.Diagnostics);
        Assert.All(parsed.Document!.Tasks, task => Assert.Null(task.Id));
        Assert.Equal(new DateTime(2026, 1, 4), parsed.Document.Tasks[1].Start);
    }

    [Fact]
    public void ConfiguredDatePrefixCannotBecomeADependencyTarget() {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat [prefix,]YYYY-MM-DD\nFirst :prefix,2026-01-02,1d\nSecond :after prefix,1d");
        Assert.True(parsed.HasErrors);
    }

    [Theory]
    [InlineData("task,", "task")]
    [InlineData("", null)]
    public void ConfiguredTimestampsKeepEveryCommaBeforeTheDuration(string idPrefix, string? expectedId) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat MMM D[,] YYYY[,] HH:mm\nTask :" + idPrefix + "Jan 2, 2026, 12:00,1d");
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(expectedId, task.Id);
        Assert.Equal(new DateTime(2026, 1, 2, 12, 0, 0), task.Start);
        Assert.Equal(task.Start.AddDays(1), task.End);
    }

    [Theory]
    [InlineData("Task :January 2, 2026,1d", "", null)]
    [InlineData("Task :Jan 2, 2026,Jan 3, 2026", "", null)]
    [InlineData("Task :active,task,January 2, 2026,1d", "", "task")]
    [InlineData("Task :task,Jan 2, 2026,Jan 3, 2026", "dateFormat MMM D[,] YYYY\n", "task")]
    [InlineData("Task :task,Jan 2, 2026,1d\ndateFormat MMM D[,] YYYY", "", "task")]
    public void CompleteDatesKeepTheirCommasInsideTaskMetadata(string taskSource, string directives, string? expectedId) {
        var source = "gantt\n" + directives + taskSource;
        var parsed = new MermaidParser().ParseGantt(source);
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(expectedId, task.Id);
        Assert.Equal(new DateTime(2026, 1, 2), task.Start);
        Assert.Equal(new DateTime(2026, 1, 3), task.End);
        Assert.Contains("2, 2026", task.RawMetadata, StringComparison.Ordinal);
        var fence = new string((char)96, 3);
        var markdown = new MermaidVisualMarkupParser().Parse(fence + "mermaid\n" + source + "\n" + fence);
        Assert.False(markdown.HasErrors);
        Assert.Equal(parsed.Document.ToSvg(), Assert.Single(markdown.Artifacts).ToSvg());
    }
}
