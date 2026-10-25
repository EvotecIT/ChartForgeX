using ChartForgeX;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Exercises existing public producer and common-envelope APIs before introducing typed restoration members.</summary>
public sealed class CategoricalInterchangeMappingCausalTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExistingNativeCategoricalProducerKeepsEditableFactsAtTheCommonPortableBoundary(bool timeline) {
        IVisualBlock source = timeline
            ? TimelineDiagramBlock.Create().WithTitle("Timeline facts").WithSize(800, 500).AddSection("phase", "Delivery")
                .AddPeriod("p", "Before launch", new[] { "Review", "Document" }, "phase")
            : JourneyDiagramBlock.Create().WithTitle("Journey facts").WithSize(800, 500).AddSection("phase", "Review")
                .AddTask("t", "Read", 3, new[] { "Author", "Reviewer" }, "phase");
        source.Options.Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);
        var svg = source.RenderSvg("categorical-boundary"); var png = source.ToPng();
        var envelope = VisualArtifact.Create("categorical-boundary", VisualArtifactKind.VisualBlock, source).ToInterchangeEnvelope();
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, timeline ? "timeline-boundary" : "journey-boundary");
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + ".native.png", png); File.WriteAllText(stem + ".json", envelope.ToJson());
        }
        Assert.Equal(timeline ? "Timeline" : "Journey", envelope.Family.ToString());
        Assert.Contains(timeline ? "\"timelinePeriods\"" : "\"journeyTasks\"", envelope.ToJson());
        Assert.Equal(svg, source.RenderSvg("categorical-boundary")); Assert.Equal(png, source.ToPng());
    }

    [Fact]
    public void ExistingNativeRowsCaptureBorrowedActorsAndEventsBeforePortableProjection() {
        var actors = new List<string> { "Reviewer" }; var events = new List<string> { "Reviewed" };
        var journey = JourneyDiagramBlock.Create().AddTask("task", "Review", 3, actors);
        var timeline = TimelineDiagramBlock.Create().AddPeriod("period", "Before launch", events);
        actors.Clear(); events.Clear();
        Assert.Equal("Reviewer", Assert.Single(journey.Tasks[0].Actors));
        Assert.Equal("Reviewed", Assert.Single(timeline.Periods[0].Events));
        Assert.Contains("Review", journey.RenderSvg("native-control")); Assert.Contains("Reviewed", timeline.RenderSvg("native-control"));
    }
}
