using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidJourneyTimelineNativeTests {
    public const string Journey = "journey\ntitle Support journey\nsection Contact\nOpen ticket: 2: Customer, Agent\nsection Resolve\nDiagnose: 4: Agent\nsection Contact\nConfirm recovery: 5: Customer, Agent";
    public const string Timeline = "timeline TD\ntitle Delivery history\nsection Discovery\nBefore launch: Interview users\n : Review findings\nsection Delivery\nLaunch: Ship product<br>and docs : Invite users\nsection Discovery\nAfter launch: Learn and improve";

    [Fact]
    public void RepeatedJourneySectionLabelsRemainDistinctAndActorsAndScoresAreNativeFacts() {
        var parsed = new MermaidParser().ParseJourney(Journey);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Same(document.Sections[0], document.Tasks[0].Section);
        Assert.Same(document.Sections[2], document.Tasks[2].Section);
        Assert.NotSame(document.Tasks[0].Section, document.Tasks[2].Section);
        var block = document.ToJourneyDiagramBlock();
        Assert.Equal(new[] { 2.0, 4.0, 5.0 }, block.Tasks.Select(task => task.Score));
        Assert.NotEqual(block.Tasks[0].SectionId, block.Tasks[2].SectionId);
        var svg = XDocument.Parse(block.ToSvg());
        Assert.Equal(3, Roles(svg, "journey-section").Length);
        Assert.Equal(5, Roles(svg, "journey-actor-membership").Length);
        Assert.Equal(new[] { "Customer", "Agent" }, Roles(svg, "journey-actor-label").Select(item => item.Element(item.Name.Namespace + "title")!.Value));
        var points = Roles(svg, "journey-score");
        Assert.True(Number(points[0], "cy") > Number(points[1], "cy"));
        Assert.True(Number(points[1], "cy") > Number(points[2], "cy"));
        Assert.Single(Roles(svg, "journey-score-path"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("6")]
    [InlineData("NaN")]
    public void JourneyRejectsScoresOutsideItsDocumentedScale(string score) {
        var result = new MermaidParser().ParseJourney("journey\nInvalid: " + score + ": Actor");
        var error = Assert.Single(result.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code);
        Assert.Equal(2, error.Span.Line);
        Assert.Equal(1, error.Span.Column);
        Assert.Empty(result.Document!.Tasks);
    }

    [Theory]
    [InlineData("section")]
    [InlineData("Missing task separator")]
    [InlineData(": 3: Actor")]
    public void InvalidJourneyStatementsKeepTheirSpecificLocationWithoutAnEmptyDiagramCascade(string statement) {
        var parsed = new MermaidParser().ParseJourney("journey\n  " + statement);
        var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticSeverity.Error, error.Severity);
        Assert.Equal(MermaidDiagnosticCodes.InvalidStatement, error.Code);
        Assert.Equal(2, error.Span.Line); Assert.Equal(3, error.Span.Column);
        Assert.Empty(parsed.Document!.Tasks);
    }

    [Fact]
    public void AnOtherwiseValidEmptyJourneyStillReportsItsMissingTaskAtTheHeader() {
        const string source = "journey\ntitle Empty journey\nsection Work";
        var parsed = new MermaidParser().ParseJourney(source); var error = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MermaidDiagnosticSeverity.Error, error.Severity);
        Assert.Equal(1, error.Span.Line); Assert.Equal(1, error.Span.Column);
        Assert.Empty(parsed.Document!.Tasks);
        Assert.Null(MermaidRenderer.Render(source).Artifact);
    }

    [Fact]
    public void JourneyKeepsIndependentStatementErrorsAndContinuesToSubsequentValidTasks() {
        const string source = "journey\nsection\nMissing task separator\nOut of scale: 0: Actor\nValid task: 3: Actor";
        var parsed = new MermaidParser().ParseJourney(source);
        Assert.Equal(new[] { 2, 3, 4 }, parsed.Diagnostics.Select(item => item.Span.Line));
        Assert.All(parsed.Diagnostics, item => Assert.Equal(MermaidDiagnosticSeverity.Error, item.Severity));
        Assert.Equal("Valid task", Assert.Single(parsed.Document!.Tasks).Text);
        Assert.Null(MermaidRenderer.Render(source).Artifact);
    }

    [Fact]
    public void TimelineKeepsCategoricalPeriodsContinuationEventsAndRepeatedSectionsWithoutDurations() {
        var parsed = new MermaidParser().ParseTimeline(Timeline);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; var block = document.ToTimelineDiagramBlock();
        Assert.Equal(TimelineDiagramDirection.TopToBottom, block.Direction);
        Assert.Same(document.Sections[2], document.Periods[2].Section);
        Assert.Equal(new[] { "Interview users", "Review findings" }, block.Periods[0].Events);
        Assert.Equal("Ship product\nand docs", block.Periods[1].Events[0]);
        var xml = XDocument.Parse(block.ToSvg());
        Assert.Equal(3, Roles(xml, "timeline-period").Length);
        Assert.Equal(5, Roles(xml, "timeline-event").Length);
        Assert.Equal(3, Roles(xml, "timeline-section").Length);
        Assert.DoesNotContain("duration", xml.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Roles(xml, "timeline-period"), item => item.Attribute("data-start") != null || item.Attribute("data-end") != null);
    }

    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void NativeJourneyAndBothTimelineDirectionsShareDirectFencedSvgPngAndRepeatedOutput(bool dark, int width) {
        foreach (var source in new[] { Journey, Timeline, Timeline.Replace("timeline TD", "timeline") }) {
            var options = new MermaidRenderOptions {
                Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family },
                Journey = new MermaidJourneyRenderOptions { Width = width, Height = 640 }, Timeline = new MermaidTimelineRenderOptions { Width = width, Height = 640 }
            };
            var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
            var artifact = Assert.IsType<VisualArtifact>(direct.Artifact);
            Assert.IsAssignableFrom<IVisualBlock>(artifact.Model);
            var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fence.Diagnostics);
            var fenced = Assert.Single(fence.Artifacts); var svg = artifact.ToSvg(); var png = artifact.ToPng();
            Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
            Assert.Equal(svg, artifact.ToSvg()); Assert.Equal(png, artifact.ToPng());
            var xml = XDocument.Parse(svg); var image = RasterImageDecoder.Decode(png);
            Assert.Equal(Number(xml.Root!, "width"), image.Width); Assert.Equal(Number(xml.Root!, "height"), image.Height);
            Assert.DoesNotContain(xml.Descendants(), item => item.Name.LocalName is "script" or "foreignObject");
            var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
            Assert.False(strict.HasErrors);
            Save(source.StartsWith("journey", StringComparison.Ordinal) ? "journey" : source.StartsWith("timeline TD", StringComparison.Ordinal) ? "timeline-td" : "timeline-lr", dark, width, svg, png);
        }
    }

    [Fact]
    public void NativeDiagramModelsRejectUnknownSectionsAndImpossibleJourneyScoresBeforePaint() {
        Assert.Throws<InvalidOperationException>(() => JourneyDiagramBlock.Create().AddTask("task", "Task", 6).ToSvg());
        Assert.Throws<InvalidOperationException>(() => JourneyDiagramBlock.Create().AddTask("task", "Task", 3, sectionId: "missing").ToPng());
        Assert.Throws<InvalidOperationException>(() => TimelineDiagramBlock.Create().AddPeriod("period", "Period", new[] { "Event" }, "missing").ToSvg());
    }

    private static XElement[] Roles(XDocument xml, string role) => xml.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == role).ToArray();
    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static void Save(string family, bool dark, int width, string svg, byte[] png) {
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root); var path = Path.Combine(root, family + "-" + width + "-" + (dark ? "dark" : "light"));
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png);
    }
}
