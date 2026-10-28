using ChartForgeX;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class JourneyTimelineInterchangeRejectionTests {
    [Theory]
    [InlineData("journey")] [InlineData("timeline-td")]
    public void TypedFamiliesRejectDowngradeWrongFamilyAndForeignSemanticRoots(string kind) {
        var envelope = JourneyTimelineInterchangeTests.Parsed(kind, 800, false).ToInterchangeEnvelope(); var json = envelope.ToJson();
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"version\":2", "\"version\":1")));
        if (kind == "journey") Assert.Throws<NotSupportedException>(() => envelope.ToTimelineDiagramBlock());
        else Assert.Throws<NotSupportedException>(() => envelope.ToJourneyDiagramBlock());
        envelope.TreeView = new VisualArtifactInterchangeTreeViewArtifact(); Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.TreeView = null; envelope.Family = VisualArtifactInterchangeFamily.None; Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
    }

    [Theory]
    [InlineData("empty")] [InlineData("duplicate")] [InlineData("unknown-section")]
    [InlineData("score-zero")] [InlineData("score-nan")] [InlineData("duplicate-actor")] [InlineData("actors-limit")]
    public void InvalidJourneySemanticsFailBeforeImportWithoutChangingEarlierNativeOutput(string mutation) {
        var envelope = JourneyTimelineInterchangeTests.Parsed("journey", 800, false).ToInterchangeEnvelope();
        var imported = envelope.ToJourneyDiagramBlock(); var svg = imported.RenderSvg("earlier"); var source = envelope.Journey!;
        switch (mutation) {
            case "empty": source.Tasks.Clear(); break;
            case "duplicate": source.Tasks.Add(source.Tasks[0]); break;
            case "unknown-section": source.Tasks[0] = new JourneyTask("task-0", "Task", 3, sectionId: "missing"); break;
            case "score-zero": source.Tasks[0] = new JourneyTask("task-0", "Task", 0); break;
            case "score-nan": source.Tasks[0] = new JourneyTask("task-0", "Task", double.NaN); break;
            case "duplicate-actor": source.Tasks[0] = new JourneyTask("task-0", "Task", 3, new[] { "Actor", "Actor" }); break;
            case "actors-limit": source.Tasks[0] = new JourneyTask("task-0", "Task", 3, Enumerable.Range(0, 65).Select(index => "Actor" + index)); break;
        }
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJourneyDiagramBlock());
        Assert.Equal(svg, imported.RenderSvg("earlier"));
    }

    [Theory]
    [InlineData("empty")] [InlineData("duplicate")] [InlineData("unknown-section")]
    [InlineData("no-events")] [InlineData("blank-event")] [InlineData("events-limit")] [InlineData("direction")]
    public void InvalidTimelineSemanticsFailBeforeImportWithoutInventingAnEmptyOrDatedPeriod(string mutation) {
        var envelope = JourneyTimelineInterchangeTests.Parsed("timeline-td", 800, false).ToInterchangeEnvelope(); var source = envelope.Timeline!;
        switch (mutation) {
            case "empty": source.Periods.Clear(); break;
            case "duplicate": source.Periods.Add(source.Periods[0]); break;
            case "unknown-section": source.Periods[0] = new TimelinePeriod("period-0", "Still text", new[] { "Event" }, "missing"); break;
            case "no-events": source.Periods[0] = new TimelinePeriod("period-0", "Still text", Array.Empty<string>()); break;
            case "blank-event": source.Periods[0] = new TimelinePeriod("period-0", "Still text", new[] { " " }); break;
            case "events-limit": source.Periods[0] = new TimelinePeriod("period-0", "Still text", Enumerable.Repeat("Event", 2049)); break;
            case "direction": source.Direction = (TimelineDiagramDirection)123; break;
        }
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToTimelineDiagramBlock());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FramesPresentationAndCollectionsRetainExistingPortableSafetyBounds(bool timeline) {
        var envelope = JourneyTimelineInterchangeTests.Parsed(timeline ? "timeline-td" : "journey", 800, false).ToInterchangeEnvelope();
        envelope.Width = 800.5; Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson()); envelope.Width = 800;
        envelope.Height = null; Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson()); envelope.Height = 640;
        if (timeline) envelope.Timeline!.PngOutputScale = 5; else envelope.Journey!.PngOutputScale = 5;
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        if (timeline) { envelope.Timeline!.PngOutputScale = 1; envelope.Timeline.Periods.Add(null!); }
        else { envelope.Journey!.PngOutputScale = 1; envelope.Journey.Tasks.Add(null!); }
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
    }
}
