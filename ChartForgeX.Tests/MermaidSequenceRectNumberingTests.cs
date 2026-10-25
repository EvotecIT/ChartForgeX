using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidSequenceRectNumberingTests {
    internal static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "sequence-rect-numbering.mmd"));

    [Fact]
    public void OrderedControlsAndTrueBackgroundsReachNativeAndFencedScenesWithoutReplacingSourceFacts() {
        var source = Fixture();
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "sequence-rect-numbering.expected.json")));
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(new[] { (true, "5.5", "0.5", 13), (false, (string?)null, (string?)null, 16), (true, (string?)null, (string?)null, 20), (true, "10", (string?)null, 23) },
            document.AutonumberStatements.Select(control => (control.Enabled, control.Start, control.Increment, control.Span.Line)));
        Assert.Same(document.AutonumberStatements.Last(), document.Autonumber);
        Assert.Equal("hsla(120,45%,70%,0.3)", document.Blocks.Single(block => block.Kind == MermaidSequenceBlockKind.Rect && block.Depth == 1).Text);
        var sequence = document.ToSequenceArtifact();
        Assert.Equal(expected.RootElement.GetProperty("values").EnumerateArray().Select(value => value.GetString()), sequence.Messages.Select(message => message.Metadata["mermaid.autonumber.value"]));
        Assert.Equal(new[] { "false", "true", "false", "true", "true", "true", "true" }, sequence.Messages.Select(message => message.Metadata["mermaid.autonumber.visible"]));
        var prepared = SequencePreparedCompiler.PrepareDefault(sequence);
        var backgrounds = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "sequence-block-background").ToArray();
        Assert.Equal(3, backgrounds.Length);
        Assert.Contains(backgrounds, node => node.Fill.HasValue && node.Fill.Value.Equals(ChartColor.FromRgba(220, 235, 255, 166)));
        Assert.Contains(backgrounds, node => node.Fill.HasValue && node.Fill.Value.Equals(ChartColor.FromRgba(144, 213, 144, 76)));
        Assert.Contains(backgrounds, node => node.Fill.HasValue && node.Fill.Value.Equals(ChartColor.FromRgba(160, 190, 235, 56)));
        var firstBackground = prepared.Scene.Nodes.ToList().FindIndex(node => node is VisualSceneRectangle rect && rect.Role == "sequence-block-background");
        var firstLine = prepared.Scene.Nodes.ToList().FindIndex(node => node is VisualScenePath path && path.Role == "sequence-message-line");
        Assert.InRange(firstBackground, 0, firstLine - 1);
        // The selected default source style now casts a background-surface shadow behind
        // this rectangle. Keep the genuine default scene above, and isolate the existing
        // flat source-RGBA compositing contract without that separately tested effect.
        var flatSequence = parsed.Document!.ToSequenceArtifact();
        flatSequence.Paint.BackgroundShadow = null;
        var flat = SequencePreparedCompiler.PrepareDefault(flatSequence);
        SequenceMessageFixture.Capture("sequence-rect-flat-alpha-control",flat);
        var image = RasterImageDecoder.Decode(flat.ToPng(new VisualRenderOptions(supersampling: 1)));
        var outer = backgrounds.Single(node => node.Fill.HasValue && node.Fill.Value.Equals(ChartColor.FromRgba(220, 235, 255, 166)));
        var expectedSurface = ChartColorMath.CompositeOverOpaque(outer.Fill!.Value, sequence.Theme.Resolve(sequence.ThemeMode).Background);
        var sample = ((int)Math.Ceiling(outer.Bounds.Y + 3) * image.Width + (int)Math.Ceiling(outer.Bounds.X + 3)) * 4;
        Assert.InRange(Math.Abs(image.Pixels[sample] - expectedSurface.R), 0, 1);
        Assert.InRange(Math.Abs(image.Pixels[sample + 1] - expectedSurface.G), 0, 1);
        Assert.InRange(Math.Abs(image.Pixels[sample + 2] - expectedSurface.B), 0, 1);
        Assert.Equal(255, image.Pixels[sample + 3]);

        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().SelectMany(node => node.Text.Lines).Select(line => line.Text).ToArray();
        Assert.DoesNotContain(labels, label => label.Contains("rgba(", StringComparison.Ordinal) || label.Contains("hsla(", StringComparison.Ordinal));
        foreach (var label in new[] { "Before", "Started", "Hidden", "Resumed", "Reset", "Continued", "Default color" }) Assert.Contains(label, labels);
        Assert.Equal(new[] { "5.5", "6.5", "10", "11", "12" }, prepared.Regions.Where(region => region.Role == "sequence-number").Select(region => region.Label));
        Assert.DoesNotContain(labels, label => label.EndsWith(". Before", StringComparison.Ordinal) || label.EndsWith(". Hidden", StringComparison.Ordinal));
        var snapshot = prepared.ToArtifact(); var beforeSvg = prepared.ToSvg(); var beforeJson = snapshot.ToInterchangeJson();
        var envelope = snapshot.ToInterchangeEnvelope();
        Assert.Equal(3, envelope.Annotations.Count(annotation => annotation.Extensions.ContainsKey("chartforgex.sequence.background")));
        sequence.Blocks[0].BackgroundColor = ChartColor.FromRgb(0, 0, 0);
        sequence.Messages[1].Metadata["mermaid.autonumber.value"] = "999";
        Assert.Equal(beforeSvg, prepared.ToSvg()); Assert.Equal(beforeJson, snapshot.ToInterchangeJson());
        Assert.True(prepared.ToPng().Length > 64);
        var direct = MermaidRenderer.Render(source); var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(direct.Diagnostics); Assert.Empty(fenced.Diagnostics);
        Assert.Equal(direct.Artifact!.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        Assert.True(fenced.Artifacts[0].ToPng().Length > 64);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeBackgroundOptionPreservesNullDefaultsAndTypedSnapshotIsolation(bool dark) {
        var sequence = SequenceArtifact.Create("native-rect").AddParticipant("A").AddParticipant("B").AddMessage("A", "B", "Message")
            .AddBlock(SequenceArtifactBlockKind.Rect, "Native highlight", 0, 0);
        sequence.ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        Assert.Null(sequence.Blocks[0].BackgroundColor);
        var baseline = sequence.ToSvg();
        Assert.Contains("Native highlight", baseline);
        sequence.Blocks[0].BackgroundColor = ChartColor.FromRgba(200, 220, 255, 90);
        var prepared = SequencePreparedCompiler.PrepareDefault(sequence);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "sequence-block-background");
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Text.Lines.Any(line => line.Text.Contains("Native highlight", StringComparison.Ordinal)));
        sequence.Blocks[0].BackgroundColor = null;
        Assert.Equal(baseline, sequence.ToSvg());
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void BareControlsResumeCurrentStepAndExplicitStartsResetStepAtTheirExactCompactOccurrence() {
        const string source = "sequenceDiagram\nA->>B: Before; autonumber 5 0.5; A->>B: Start; autonumber off; B-->>A: Hidden; autonumber; A->>B: Resume; autonumber 10; B-->>A: Reset; A->>B: Next";
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(4, parsed.Document!.AutonumberStatements.Count);
        Assert.All(parsed.Document!.AutonumberStatements, control => Assert.Equal(2, control.Span.Line));
        var sequence = parsed.Document!.ToSequenceArtifact();
        Assert.Equal(new[] { "1", "5", "5.5", "6", "10", "11" }, sequence.Messages.Select(message => message.Metadata["mermaid.autonumber.value"]));
        Assert.Equal(new[] { "false", "true", "false", "true", "true", "true" }, sequence.Messages.Select(message => message.Metadata["mermaid.autonumber.visible"]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GlobalShowNumbersRetainsItsDeclaredForceOnMeaningAroundAuthoredOff(bool configured) {
        var source = "%%{init: {'sequence':{'showSequenceNumbers':" + (configured ? "true" : "false") + "}}}%%\n" +
            "sequenceDiagram\nA->>B: Before\nautonumber 5\nA->>B: Start\nautonumber off\nB-->>A: Hidden";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var model = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        Assert.Equal(configured ? "true" : "false", model.Messages[0].Metadata["mermaid.autonumber.visible"]);
        Assert.Equal(configured ? "true" : "false", model.Messages[2].Metadata["mermaid.autonumber.visible"]);
        var occurrence = SequencePreparedCompiler.PrepareDefault(model);
        var occurrenceFacts = occurrence.ToArtifact().ToInterchangeEnvelope();
        Assert.Equal(configured, occurrence.Regions.Any(region => region.Role == "sequence-number" && region.Id == occurrenceFacts.Edges[2].Id && region.Label == "6"));
        Assert.Contains("Hidden", rendered.Artifact.ToSvg());
    }

    [Fact]
    public void SequentialSourceArithmeticDoesNotUseAnOverflowingIndexTimesStepIntermediate() {
        var max = decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var rendered = MermaidRenderer.Render("sequenceDiagram\nautonumber -" + max + " " + max + "\nA->>B: First\nB-->>A: Middle\nA->>B: Last");
        Assert.Empty(rendered.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        Assert.Equal(new[] { "-" + max, "0", max }, sequence.Messages.Select(message => message.Metadata["mermaid.autonumber.value"]));
        var prepared = SequencePreparedCompiler.PrepareDefault(sequence); var facts = prepared.ToArtifact().ToInterchangeEnvelope();
        Assert.Equal("0", prepared.Regions.Single(region => region.Role == "sequence-number" && region.Id == facts.Edges[1].Id).Label);
        Assert.Contains("Middle", rendered.Artifact.ToSvg());
    }

    [Theory]
    [InlineData("rect not-a-color\nA->>B: Message\nend")]
    [InlineData("autonumber off 2\nA->>B: Message")]
    public void InvalidNativeColorOrIncompleteOffControlIsLocatedBeforeDirectAndFenceArtifacts(string body) {
        var source = "sequenceDiagram\n" + body;
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 2);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
    }

    [Fact]
    public void AFinalMaximumDecimalDoesNotOverflowAnUnusedNextCounterButAConsumedOverflowIsLocated() {
        var max = decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var valid = MermaidRenderer.Render("sequenceDiagram\nautonumber " + max + "\nA->>B: Last\nautonumber 1\nB-->>A: Reset");
        Assert.Empty(valid.Diagnostics);
        var validPrepared = SequencePreparedCompiler.PrepareDefault(Assert.IsType<SequenceArtifact>(valid.Artifact!.Model));
        var validFacts = validPrepared.ToArtifact().ToInterchangeEnvelope();
        Assert.Equal("1", validPrepared.Regions.Single(region => region.Role == "sequence-number" && region.Id == validFacts.Edges[1].Id).Label);
        Assert.Contains("Reset", valid.Artifact.ToSvg());
        var invalid = MermaidRenderer.Render("sequenceDiagram\nautonumber " + max + "\nA->>B: Last\nB-->>A: Overflow");
        Assert.True(invalid.HasErrors); Assert.Null(invalid.Artifact);
        Assert.Contains(invalid.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 2);
    }
}
