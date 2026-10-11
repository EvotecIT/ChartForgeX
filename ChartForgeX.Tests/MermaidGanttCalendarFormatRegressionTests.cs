using ChartForgeX.Mermaid;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttCalendarFormatRegressionTests {
    [Theory]
    [InlineData("q")]
    [InlineData("%")]
    [InlineData("'yyyy")]
    public void InvalidFinalDateFormatHasALocatedDiagnostic(string format) {
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nexcludes weekends\nTask :task,2026-01-02,1d");
        Assert.True(result.HasErrors);
        var error = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Message.Contains("dateFormat", StringComparison.Ordinal));
        Assert.Equal(2, error.Span.Line);
        Assert.Equal(12, error.Span.Column);
        Assert.Equal(format.Length, error.Span.Length);
    }

    [Fact]
    public void AValidFinalFormatSupersedesAnEarlierInvalidDeclaration() {
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat q\ndateFormat YYYY-MM-DD\nexcludes weekends\nTask :task,2026-01-02,1d");
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new DateTime(2026, 1, 5), Assert.Single(result.Document!.Tasks).End);
    }

    [Theory]
    [InlineData("excludes 2026-Q2", 3)]
    [InlineData("excludes 2026-04-02\nincludes 2026-Q2", 3)]
    public void QuarterCalendarRulesRetainTheSingleParsedDay(string rule, int durationDays) {
        var result = new MermaidParser().ParseGantt("gantt\ndateFormat YYYY-[Q]Q\n" + rule + "\nTask :task,2026-03-31,2d");
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new DateTime(2026, 3, 31).AddDays(durationDays), Assert.Single(result.Document!.Tasks).End);
    }
}
