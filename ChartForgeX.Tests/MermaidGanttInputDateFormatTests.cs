using System.Globalization;
using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidGanttInputDateFormatTests {
    [Theory]
    [MemberData(nameof(ReferenceDates))]
    public void ExplicitInputFieldsPreserveReferenceDatesRegardlessOfHostCulture(string format, string start, string end, string expectedStart, string expectedEnd) {
        var previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pl-PL");
            var source = "gantt\ndateFormat " + format + "\nTask :task," + start + "," + end;
            var parsed = new MermaidParser().ParseGantt(source);
            Assert.Empty(parsed.Diagnostics);
            var document = parsed.Document!;
            var task = Assert.Single(document.Tasks);
            Assert.Equal(expectedStart, Stamp(task.Start));
            Assert.Equal(expectedEnd, Stamp(task.End));
            var chart = document.ToChart();
            Assert.Equal(task.Start.ToOADate(), chart.Series[0].Points[0].X);
            Assert.Equal(task.End.ToOADate(), chart.Series[0].Points[0].Y);
            Assert.Equal(document.ToSvg(), Assert.Single(new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```").Artifacts).ToSvg());
            Assert.NotEmpty(document.ToPng());
        } finally { CultureInfo.CurrentCulture = previous; }
    }

    public static IEnumerable<object[]> ReferenceDates() {
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "gantt-input-dates.expected.json")));
        foreach (var values in expected.RootElement.GetProperty("ganttInputDates").EnumerateArray())
            yield return values.EnumerateArray().Select(value => (object)value.GetString()!).ToArray();
    }

    [Theory]
    [InlineData("X", "1767312000.123", "1767312000.456", "2026-01-02T00:00:00.123", "2026-01-02T00:00:00.456")]
    [InlineData("YYYY-DDD", "2024-60", "2024-61", "2024-02-29T00:00:00.000", "2024-03-01T00:00:00.000")]
    [InlineData("YYYY-DDDD", "2024-060", "2024-061", "2024-02-29T00:00:00.000", "2024-03-01T00:00:00.000")]
    [InlineData("YYYY-MM-DD HH:mm Z", "2026-01-02 13:02 +02:00", "2026-01-02 14:02 +02:00", "2026-01-02T11:02:00.000", "2026-01-02T12:02:00.000")]
    [InlineData("YYYY-MM-DD HH:mm:ss.S", "2026-01-02 13:02:03.1", "2026-01-02 13:02:03.4", "2026-01-02T13:02:03.100", "2026-01-02T13:02:03.400")]
    public void DocumentedFieldsKeepTheirMeaningWhereReferenceEnginesHaveKnownGaps(string format, string start, string end, string expectedStart, string expectedEnd) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nTask :task," + start + "," + end);
        Assert.Empty(parsed.Diagnostics);
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(expectedStart, Stamp(task.Start));
        Assert.Equal(expectedEnd, Stamp(task.End));
    }

    [Fact]
    public void UnixSecondsPreserveDocumentedMeaningAndTheRecordedReferenceVersionDifference() {
        var root = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures");
        var parsed = new MermaidParser().ParseGantt(File.ReadAllText(Path.Combine(root, "gantt-unix-seconds.mmd")));
        Assert.Empty(parsed.Diagnostics);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "gantt-unix-seconds.expected.json")));
        var dates = expected.RootElement.GetProperty("nativeTaskTimestamps")[0];
        var task = Assert.Single(parsed.Document!.Tasks);
        Assert.Equal(dates[1].GetString(), Stamp(task.Start));
        Assert.Equal(dates[2].GetString(), Stamp(task.End));
        using var legacy = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "gantt-unix-seconds.expected.10.json")));
        Assert.Equal(legacy.RootElement.GetProperty("taskTimestamps")[0][1].GetString(), Stamp(task.Start));
        Assert.NotEqual(expected.RootElement.GetProperty("taskTimestamps")[0][1].GetString(), Stamp(task.Start));
    }

    [Fact]
    public void OrdinalInputFormatAlsoControlsCalendarExclusionsAndInclusions() {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat Do-MMMM-YYYY\nexcludes 3rd-January-2026 4th-January-2026\nincludes 4th-January-2026\nTask :task,2nd-January-2026,2d");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new DateTime(2026, 1, 5), Assert.Single(parsed.Document!.Tasks).End);
    }

    [Theory]
    [InlineData("YYYY-MM-DD[ffff]", "2026-01-02ffff", "2026-01-03ffff", "2026-01-04ffff")]
    [InlineData("YYYY-MM-DD[ffff']", "2026-01-02ffff'", "2026-01-03ffff'", "2026-01-04ffff'")]
    [InlineData("YYYY-MM-DD[%]", "2026-01-02%", "2026-01-03%", "2026-01-04%")]
    [InlineData("dd/MM/yyyy", "02/01/2026", "03/01/2026", "04/01/2026")]
    [InlineData("YYYY/MM/dd", "2026/01/02", "2026/01/03", "2026/01/04")]
    [InlineData("YYYY-MM-DDTHH:mm:ss.fff", "2026-01-02T00:00:00.000", "2026-01-03T00:00:00.000", "2026-01-04T00:00:00.000")]
    [InlineData("%Y-%m-%d", "2026-01-02", "2026-01-03", "2026-01-04")]
    public void CalendarMatchingUsesTheSameFormatLanguageAsTaskDates(string format, string start, string excluded, string included) {
        var source = "gantt\ndateFormat " + format + "\nexcludes " + excluded + "," + included + "\nincludes " + included + "\nTask :task," + start + ",2d";
        var parsed = new MermaidParser().ParseGantt(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new DateTime(2026, 1, 5), Assert.Single(parsed.Document!.Tasks).End);
    }

    [Fact]
    public void HundredthsKeepBothAuthoredEndpoints() {
        var parser = new MermaidParser();
        var fraction = parser.ParseGantt("gantt\ndateFormat YYYY-MM-DD HH:mm:ss.SS\nTask :task,2026-01-02 13:02:03.12,2026-01-02 13:02:03.34");
        Assert.Empty(fraction.Diagnostics);
        Assert.Equal(120, Assert.Single(fraction.Document!.Tasks).Start.Millisecond);
        Assert.Equal(340, fraction.Document!.Tasks[0].End.Millisecond);
    }

    [Fact]
    public void ExplicitFormatSizeBoundaryKeepsValidLiteralsAndRejectsLargerInputs() {
        var parser = new MermaidParser();
        var literal = new string('a', 244);
        var accepted = parser.ParseGantt("gantt\ndateFormat YYYY-MM-DD[" + literal + "]\nTask :task,2026-01-02" + literal + ",1d");
        Assert.Empty(accepted.Diagnostics);
        Assert.Equal(new DateTime(2026, 1, 3), Assert.Single(accepted.Document!.Tasks).End);
        var rejected = parser.ParseGantt("gantt\ndateFormat YYYY-MM-DD[" + literal + "a]\nTask :task,2026-01-02" + literal + "a,1d");
        Assert.True(rejected.HasErrors);
        Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Span.Line == 3);
    }

    [Theory]
    [InlineData("YYYY-DDD", "2023-366")]
    [InlineData("Do-MMMM-YYYY", "2st-January-2026")]
    [InlineData("X", "999999999999999999999999999999")]
    [InlineData("Q", "not-a-date")]
    public void InvalidFieldsProduceLocatedScheduleErrors(string format, string value) {
        var parsed = new MermaidParser().ParseGantt("gantt\ndateFormat " + format + "\nTask :task," + value + ",1d");
        Assert.True(parsed.HasErrors);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Span.Line == 3 && diagnostic.Severity == MermaidDiagnosticSeverity.Error);
    }

    private static string Stamp(DateTime date) => date.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture);
}
