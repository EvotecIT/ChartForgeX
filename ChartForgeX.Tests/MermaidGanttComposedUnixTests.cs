using System.Globalization;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttComposedUnixTests {
    [Theory]
    [InlineData("[epoch:]X", "epoch:1767312000.125", "epoch:1767398400.125")]
    [InlineData("[epoch:]x", "epoch:1767312000125", "epoch:1767398400125")]
    [InlineData("X[ seconds]", "1767312000.125 seconds", "1767398400.125 seconds")]
    public void UnixFieldsKeepTheirMeaningInsideLiteralWrappers(string format, string start, string end) {
        var source = "gantt\ndateFormat " + format + "\nTask :task," + start + "," + end;
        var parsed = new MermaidParser().ParseGantt(source);
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal("2026-01-02T00:00:00.125", task.Start.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture));
        Assert.Equal("2026-01-03T00:00:00.125", task.End.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture));
        Assert.Equal(parsed.Document.ToSvg(), Assert.Single(new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```").Artifacts).ToSvg());
    }
}
