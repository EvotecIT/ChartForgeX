using System;
using System.IO;
using ChartForgeX;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

/// <summary>Complete public Core consumer: save native categorical facts, edit the imported values and export both static formats.</summary>
internal static class JourneyTimelineInterchangeExamples {
    internal static void Write(string output) {
        foreach (var dark in new[] { false, true }) foreach (var width in new[] { 390, 1100 }) {
            var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
            var theme = VisualTheme.Graphite().WithFontFamily(ExampleTypography.FontFamily);
            var journey = JourneyDiagramBlock.Create().WithTitle("Review journey").WithSize(width, 500).WithTheme(theme, mode)
                .AddSection("draft", "Review").AddSection("accepted", "Review")
                .AddTask("read", "Read proposal", 2, new[] { "Author", "Reviewer" }, "draft")
                .AddTask("accept", "Accept update", 5, new[] { "Reviewer" }, "accepted");
            var timeline = TimelineDiagramBlock.Create().WithTitle("Delivery history").WithSize(width, 500).WithTheme(theme, mode)
                .AddSection("before", "Delivery").AddSection("after", "Delivery")
                .AddPeriod("prepare", "Before launch", new[] { "Review", "Document" }, "before")
                .AddPeriod("observe", "After launch", new[] { "Observe" }, "after");
            timeline.Direction = TimelineDiagramDirection.TopToBottom;
            var journeyEnvelope = VisualArtifactInterchangeEnvelope.FromJson(VisualArtifact.Create("journey", VisualArtifactKind.VisualBlock, journey).ToInterchangeJson());
            var timelineEnvelope = VisualArtifactInterchangeEnvelope.FromJson(VisualArtifact.Create("timeline", VisualArtifactKind.VisualBlock, timeline).ToInterchangeJson());
            journeyEnvelope.Journey!.Tasks[0] = new JourneyTask("read", "Read imported proposal", 3, new[] { "Reviewer" }, "draft");
            timelineEnvelope.Timeline!.Periods[0] = new TimelinePeriod("prepare", "Before launch", new[] { "Review imported facts", "Document" }, "before");
            Write(journeyEnvelope, journeyEnvelope.ToJourneyDiagramBlock(), "journey");
            Write(timelineEnvelope, timelineEnvelope.ToTimelineDiagramBlock(), "timeline");
            void Write(VisualArtifactInterchangeEnvelope saved, IVisualBlock block, string kind) {
                var stem = Path.Combine(output, "portable-" + kind + "-" + width + (dark ? "-dark" : "-light"));
                File.WriteAllText(stem + ".json", saved.ToJson()); File.WriteAllText(stem + ".svg", block.RenderSvg("portable-example"));
                File.WriteAllBytes(stem + ".png", block.ToPng());
                File.WriteAllText(stem + ".html", VisualArtifact.Create(kind, VisualArtifactKind.VisualBlock, block).ToHtmlPage());
            }
        }
    }
}
