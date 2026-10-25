using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class JourneyTimelineInterchangeTests {
    [Theory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public void ParsedNativeDiagramsRoundTripEditAndRenderWithoutFallbackOrLostSourceFacts(bool dark, int width) {
        foreach (var kind in new[] { "journey", "timeline-lr", "timeline-td" }) {
            var artifact = Parsed(kind, width, dark); var original = Assert.IsAssignableFrom<IVisualBlock>(artifact.Model);
            artifact.Metadata["host.note"] = "<literal> & source facts";
            artifact.Accessibility.Name = "Portable categorical diagram"; artifact.Accessibility.Language = "en";
            original.Options.Accessibility.Name = artifact.Accessibility.Name; original.Options.Accessibility.Language = "en";
            var beforeSvg = original.RenderSvg("portable-categorical"); var beforePng = original.ToPng();
            var json = artifact.ToInterchangeJson(); var envelope = VisualArtifactInterchangeEnvelope.FromUtf8Json(artifact.ToInterchangeUtf8Json());
            Assert.Equal(2, envelope.Version); Assert.Equal(VisualArtifactSourceLanguage.Mermaid, envelope.SourceLanguage);
            Assert.Equal(json, envelope.ToJson()); Assert.Equal(json, VisualArtifactInterchangeEnvelope.FromJson(json).ToJson());
            Assert.Equal("<literal> & source facts", envelope.Extensions["host.note"]);
            Assert.Contains(envelope.Extensions, pair => pair.Key.StartsWith("mermaid." + (kind == "journey" ? "journey" : "timeline") + ".sourceSpans.") && pair.Value.Contains("section-0|3|"));
            var restored = Import(envelope);
            Assert.Equal(beforeSvg, restored.RenderSvg("portable-categorical")); Assert.Equal(beforePng, restored.ToPng());
            Assert.Equal(original.Options.Theme.ToJson(), restored.Options.Theme.ToJson());
            Assert.Equal(original.Options.ThemeMode, restored.Options.ThemeMode);
            Assert.Equal("Portable categorical diagram", restored.Options.Accessibility.Name); Assert.Equal("en", restored.Options.Accessibility.Language);
            if (restored is JourneyDiagramBlock journey) {
                Assert.Equal(new[] { 2d, 4d, 5d }, journey.Tasks.Select(task => task.Score));
                Assert.Equal(new[] { "Customer", "Agent" }, journey.Tasks[0].Actors);
                Assert.NotEqual(journey.Tasks[0].SectionId, journey.Tasks[2].SectionId);
                journey.AddTask("added", "Confirm imported edit", 3, new[] { "Reviewer" });
                Assert.Equal(3, envelope.Journey!.Tasks.Count); Assert.Equal(3, ((JourneyDiagramBlock)original).Tasks.Count);
            } else {
                var timeline = Assert.IsType<TimelineDiagramBlock>(restored);
                Assert.Equal(kind.EndsWith("td") ? TimelineDiagramDirection.TopToBottom : TimelineDiagramDirection.LeftToRight, timeline.Direction);
                Assert.Equal(new[] { "Interview users", "Review findings" }, timeline.Periods[0].Events);
                Assert.Equal("Ship product\nand docs", timeline.Periods[1].Events[0]);
                Assert.NotEqual(timeline.Periods[0].SectionId, timeline.Periods[2].SectionId);
                timeline.AddPeriod("added", "After imported edit", new[] { "Confirm imported edit" });
                Assert.Equal(3, envelope.Timeline!.Periods.Count); Assert.Equal(3, ((TimelineDiagramBlock)original).Periods.Count);
            }
            var editedSvg = restored.RenderSvg("portable-categorical"); var editedPng = restored.ToPng();
            Assert.NotEqual(beforeSvg, editedSvg);
            Assert.Contains("imported edit", string.Join(" ", XDocument.Parse(editedSvg).Descendants()
                .Where(element => element.Name.LocalName == "text").Select(element => element.Value)));
            Assert.Equal(beforeSvg, original.RenderSvg("portable-categorical")); Assert.Equal(beforePng, original.ToPng());
            Capture(kind, width, dark, envelope, beforeSvg, beforePng, editedSvg, editedPng);
            var png = RasterImageDecoder.Decode(editedPng); Assert.Equal(width, png.Width); Assert.Equal(640, png.Height);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void HostCollectionsOptionsThemeAndBorrowedChildListsStayDetachedAfterCaptureAndImport(bool timeline) {
        var actors = new List<string> { "Avery", "Morgan" }; var events = new List<string> { "Accepted", "Documented" };
        IVisualBlock source = timeline
            ? TimelineDiagramBlock.Create().AddSection("phase", "Phase").AddPeriod("p", "Not a date", events, "phase")
            : JourneyDiagramBlock.Create().AddSection("phase", "Phase").AddTask("t", "Review", 3.5, actors, "phase");
        source.Options.Padding = new ChartForgeX.Primitives.ChartPadding(31, 17, 23, 19); source.Options.ThemeMode = VisualThemeMode.Dark;
        source.Options.Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);
        source.Options.ShowCard = false; source.Options.TransparentBackground = true; source.Options.PngOutputScale = 2;
        source.Options.HostOwnsFrame = true; source.Options.Accessibility.IsDecorative = true;
        var envelope = VisualArtifact.Create("detached-categorical", VisualArtifactKind.VisualBlock, source).ToInterchangeEnvelope();
        actors.Clear(); events.Clear(); source.Options.PngOutputScale = 1; source.Options.ShowCard = true;
        if (source is TimelineDiagramBlock producerTimeline) producerTimeline.AddPeriod("later", "Later", new[] { "Only producer" });
        else ((JourneyDiagramBlock)source).AddTask("later", "Only producer", 4);
        var imported = Import(envelope); var svg = imported.RenderSvg("detached"); var png = imported.ToPng();
        Assert.Equal(2, imported.Options.PngOutputScale); Assert.False(imported.Options.ShowCard);
        if (imported is TimelineDiagramBlock timelineBlock) Assert.Single(timelineBlock.Periods);
        else { Assert.Single(((JourneyDiagramBlock)imported).Tasks); Assert.Equal(3.5, ((JourneyDiagramBlock)imported).Tasks[0].Score); }
        Assert.True(imported.Options.TransparentBackground); Assert.True(imported.Options.HostOwnsFrame); Assert.True(imported.Options.Accessibility.IsDecorative);
        imported.Options.HostOwnsFrame = false;
        Assert.Equal((31d, 17d, 23d, 19d), (imported.Options.Padding.Left, imported.Options.Padding.Top, imported.Options.Padding.Right, imported.Options.Padding.Bottom));
        imported.Options.HostOwnsFrame = true;
        if (timeline) { Assert.Equal(2, ((TimelineDiagramBlock)imported).Periods[0].Events.Count); envelope.Timeline!.Periods.Clear(); }
        else { Assert.Equal(2, ((JourneyDiagramBlock)imported).Tasks[0].Actors.Count); envelope.Journey!.Tasks.Clear(); }
        Assert.Equal(svg, imported.RenderSvg("detached")); Assert.Equal(png, imported.ToPng());
    }

    [Fact]
    public void ReplacingImmutableWireValuesIsAnEditAndEarlierImportsKeepTheirOwnCollections() {
        var envelope = Parsed("journey", 800, false).ToInterchangeEnvelope(); var first = envelope.ToJourneyDiagramBlock();
        envelope.Journey!.Tasks[0] = new JourneyTask("task-0", "Edited wire task", 1, new[] { "New actor" }, "section-0");
        var second = envelope.ToJourneyDiagramBlock();
        Assert.Equal("Open ticket", first.Tasks[0].Label); Assert.Equal("Edited wire task", second.Tasks[0].Label);
        Assert.Equal(2, first.Tasks[0].Score); Assert.Equal(1, second.Tasks[0].Score);
        Assert.NotEqual(first.RenderSvg("edit"), second.RenderSvg("edit"));
        Assert.NotEmpty(first.ToPng()); Assert.NotEmpty(second.ToPng());
    }

    internal static VisualArtifact Parsed(string kind, int width, bool dark) {
        var source = kind == "journey" ? MermaidJourneyTimelineNativeTests.Journey : kind == "timeline-td"
            ? MermaidJourneyTimelineNativeTests.Timeline : MermaidJourneyTimelineNativeTests.Timeline.Replace("timeline TD", "timeline");
        var options = new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family },
            Journey = new MermaidJourneyRenderOptions { Width = width, Height = 640 },
            Timeline = new MermaidTimelineRenderOptions { Width = width, Height = 640 }
        };
        var parsed = MermaidRenderer.Render(source, options); Assert.Empty(parsed.Diagnostics); return Assert.IsType<VisualArtifact>(parsed.Artifact);
    }
    internal static IVisualBlock Import(VisualArtifactInterchangeEnvelope envelope) => envelope.Family == VisualArtifactInterchangeFamily.Journey
        ? envelope.ToJourneyDiagramBlock() : envelope.ToTimelineDiagramBlock();
    private static void Capture(string kind, int width, bool dark, VisualArtifactInterchangeEnvelope envelope, string original, byte[] originalPng, string edited, byte[] editedPng) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "portable-" + kind + "-" + width + (dark ? "-dark" : "-light"));
        File.WriteAllText(stem + ".json", envelope.ToJson()); File.WriteAllText(stem + ".svg", original); File.WriteAllBytes(stem + ".native.png", originalPng);
        File.WriteAllText(stem + ".edited.svg", edited); File.WriteAllBytes(stem + ".edited.native.png", editedPng);
    }
}
