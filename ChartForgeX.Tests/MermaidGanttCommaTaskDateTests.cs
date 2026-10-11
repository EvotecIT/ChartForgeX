using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttCommaTaskDateTests {
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
