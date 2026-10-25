using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

internal static partial class Program {
    private static void CategoricalInterchangeContracts() {
        var journey = JourneyDiagramBlock.Create().WithTitle("Review journey").WithSize(800, 500)
            .AddSection("first", "Review").AddSection("second", "Review")
            .AddTask("read", "Read", 2, new[] { "Author", "Reviewer" }, "first")
            .AddTask("accept", "Accept", 5, new[] { "Reviewer" }, "second");
        var timeline = TimelineDiagramBlock.Create().WithTitle("Release history").WithSize(800, 500)
            .AddSection("first", "Delivery").AddSection("second", "Delivery")
            .AddPeriod("before", "Before launch", new[] { "Review", "Document" }, "first")
            .AddPeriod("after", "After launch", new[] { "Observe" }, "second");
        timeline.Direction = TimelineDiagramDirection.TopToBottom;
        var journeyJson = VisualArtifact.Create("journey", VisualArtifactKind.VisualBlock, journey).ToInterchangeJson();
        var timelineJson = VisualArtifact.Create("timeline", VisualArtifactKind.VisualBlock, timeline).ToInterchangeJson();
        var journeyEnvelope = VisualArtifactInterchangeEnvelope.FromJson(journeyJson);
        var timelineEnvelope = VisualArtifactInterchangeEnvelope.FromUtf8Json(System.Text.Encoding.UTF8.GetBytes(timelineJson));
        var importedJourney = journeyEnvelope.ToJourneyDiagramBlock(); var importedTimeline = timelineEnvelope.ToTimelineDiagramBlock();
        PackageAssertions.Require(journeyEnvelope.Version == 2 && timelineEnvelope.Version == 2, "Categorical typed payloads did not retain version 2.");
        PackageAssertions.Require(journey.RenderSvg("consumer") == importedJourney.RenderSvg("consumer") && journey.ToPng().SequenceEqual(importedJourney.ToPng()), "Packed Journey changed native paint across portable restoration.");
        PackageAssertions.Require(timeline.RenderSvg("consumer") == importedTimeline.RenderSvg("consumer") && timeline.ToPng().SequenceEqual(importedTimeline.ToPng()), "Packed Timeline changed native paint across portable restoration.");
        journeyEnvelope.Journey!.Tasks[0] = new JourneyTask("read", "Edited", 1, new[] { "Reviewer" }, "first");
        var edited = journeyEnvelope.ToJourneyDiagramBlock();
        PackageAssertions.Require(edited.Tasks[0].Score == 1 && importedJourney.Tasks[0].Score == 2 && journey.Tasks[0].Score == 2, "Packed categorical editing retained mutable source aliases.");
        PackageAssertions.Require(importedTimeline.Direction == TimelineDiagramDirection.TopToBottom && importedTimeline.Periods[0].Label == "Before launch" && importedTimeline.Periods[0].Events.Count == 2, "Packed categorical chronology was replaced by inferred dates or reordered events.");
        PackageAssertions.Png(edited.ToPng()); PackageAssertions.Png(importedTimeline.ToPng());
    }
}
