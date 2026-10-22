using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the bounded shared paint cascade without replacing state identity, controls or concurrent regions.</summary>
public sealed class MermaidStateStyleTests {
    [Theory]
    [InlineData("TB", false, 360)]
    [InlineData("TB", true, 900)]
    [InlineData("LR", true, 360)]
    [InlineData("LR", false, 900)]
    public void OrderedNamedAndDirectPaintsReachCompositeAndStatePairedArtifacts(string direction, bool dark, int width) {
        var source = StyledSource(direction);
        var options = new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light,
                FontFamily = BrowserProofFonts.Family, FontSize = 12 },
            State = new MermaidTopologyRenderOptions { Width = width, Height = 640 }
        };
        var direct = MermaidRenderer.Render(source, options);
        Assert.Empty(direct.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        var document = Assert.IsType<MermaidStateDocument>(direct.Document);
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal(new[] { "first", "inline", "last" }, document.States.Single(state => state.Id == "A").Classes);
        Assert.Equal(new[] { "default", "first", "first", "inline", "shell", "last" }, document.ClassDefinitions.Select(item => item.Name));
        var a = chart.Nodes.Single(node => node.Id == "A");
        Assert.Equal(("#86efac", "#d97706", "#14532d"), (a.BackgroundColor, a.Color, a.LabelColor));
        var b = chart.Nodes.Single(node => node.Id == "B");
        Assert.Equal(("#fef9c3", "#d97706", "#111827"), (b.BackgroundColor, b.Color, b.LabelColor));
        Assert.Equal("#dbeafe", chart.Nodes.Single(node => node.Id == "Done").BackgroundColor);
        var shell = chart.Groups.Single(group => group.Id == "Active");
        Assert.Equal(("#fce7f3", "#be185d", "#831843"), (shell.BackgroundColor, shell.BorderColor, shell.LabelColor));
        Assert.Equal("shell", shell.Metadata["mermaid.classes"]);
        Assert.Equal(2, chart.Groups.Count(group => group.Presentation == TopologyGroupPresentation.Compartment));
        Assert.All(chart.Groups.Where(group => group.Presentation == TopologyGroupPresentation.Compartment), group => {
            Assert.Null(group.BackgroundColor);
            Assert.False(group.Metadata.ContainsKey("mermaid.style.fill"));
        });
        Assert.Equal(3, chart.Nodes.Count(node => node.Metadata["mermaid.kind"] is "start" or "end"));
        Assert.All(chart.Nodes.Where(node => node.Metadata["mermaid.kind"] is "start" or "end"), node => Assert.Null(node.BackgroundColor));
        Assert.Equal(TopologyNodeShape.Diamond, chart.Nodes.Single(node => node.Id == "Choice").Shape);
        Assert.Equal("A", Assert.Single(chart.Annotations).TargetId);
        Assert.Equal(document.Transitions.Select(edge => (edge.SourceId, edge.TargetId, edge.Label)),
            chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId, edge.Label)));
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fence = Assert.Single(fenced.Artifacts);
        var svg = artifact.ToSvg();
        var png = artifact.ToPng();
        Assert.Equal(svg, fence.ToSvg());
        Assert.Equal(png, fence.ToPng());
        Assert.Equal(chart.Prepare().ToSvg(), chart.Prepare().ToSvg());
        var xml = XDocument.Parse(svg);
        var pixels = RasterImageDecoder.Decode(png);
        foreach (var color in new[] { "#86efac", "#fef9c3", "#fce7f3" }) {
            Assert.Contains(xml.Descendants(), element => string.Equals((string?)element.Attribute("fill"), color, StringComparison.OrdinalIgnoreCase));
            Assert.True(Count(pixels.Pixels, ChartColor.Parse(color)) > 20, "Authored state and composite paints must reach the native PNG.");
        }
        var facts = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(chart.Nodes.Select(node => node.BackgroundColor), facts.Nodes.Select(node => node.BackgroundColor));
        Assert.Equal("#fce7f3", facts.Groups.Single(group => group.Id == "Active").BackgroundColor);
        Assert.Equal("#be185d", facts.Groups.Single(group => group.Id == "Active").BorderColor);
        Assert.Equal("#831843", facts.Groups.Single(group => group.Id == "Active").LabelColor);
        Assert.Equal("first,inline,last", facts.Nodes.Single(node => node.Id == "A").Extensions["mermaid.classes"]);
        Save(svg, png, facts, direction, dark, width);
    }

    [Fact]
    public void DefaultNodePaintDoesNotImplicitlyPaintShellsAndDirectGroupPaintWinsNamedClasses() {
        const string source = "stateDiagram-v2\nclassDef default fill:#dbeafe,stroke:#1e3a8a,color:#0f172a\n"
            + "classDef shell fill:#fce7f3,stroke:#be185d,color:#831843\n"
            + "state Plain {\nA\n}\nstate Styled:::shell {\nB\n}\nstyle Styled fill:#dcfce7,color:#166534";
        var parsed = new MermaidParser().ParseState(source);
        Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart();
        var plain = chart.Groups.Single(group => group.Id == "Plain");
        Assert.Null(plain.BackgroundColor);
        Assert.Null(plain.BorderColor);
        Assert.Null(plain.LabelColor);
        var styled = chart.Groups.Single(group => group.Id == "Styled");
        Assert.Equal(("#dcfce7", "#be185d", "#166534"), (styled.BackgroundColor, styled.BorderColor, styled.LabelColor));
        Assert.All(chart.Nodes, node => Assert.Equal("#dbeafe", node.BackgroundColor));
    }

    [Fact]
    public void InlineClassDelimitersDoNotConsumeTransitionLabelsOrQuotedStateDescriptions() {
        const string source = "stateDiagram-v2\nclassDef red fill:#fecaca\nclassDef blue fill:#bfdbfe\n"
            + "state \"Name:::literal: intact\" as A:::red\nA:::red --> B:::blue: accepted: detail\nB:::blue: Display: intact";
        var parsed = new MermaidParser().ParseState(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(new[] { "A", "B" }, document.States.Select(state => state.Id));
        Assert.Equal("Name:::literal: intact", document.States[0].Label);
        Assert.Equal("Display: intact", document.States[1].Label);
        Assert.Equal("accepted: detail", Assert.Single(document.Transitions).Label);
        Assert.All(document.States.SelectMany(state => state.Classes), name => Assert.DoesNotContain(":", name));
        Assert.Equal(new[] { "#fecaca", "#bfdbfe" }, document.ToTopologyChart().Nodes.Select(node => node.BackgroundColor));
    }

    [Theory]
    [InlineData("style A font-weight:bold", "font-weight")]
    [InlineData("style A fill:javascript:alert(1)", "fill")]
    [InlineData("classDef marked fill:url(https://example.test/paint)\nclass A marked", "fill")]
    public void UnsupportedOrUnsafeDeclarationsAreLocatedAndRetainedWithoutNativeApplication(string statement, string property) {
        var rendered = MermaidRenderer.Render("stateDiagram-v2\nA\n  " + statement);
        Assert.False(rendered.HasErrors);
        var diagnostic = Assert.Single(rendered.Diagnostics);
        Assert.Equal((MermaidDiagnosticCodes.UnsupportedStatement, 3, 3), (diagnostic.Code, diagnostic.Span.Line, diagnostic.Span.Column));
        var node = Assert.Single(Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Nodes);
        Assert.Null(node.BackgroundColor);
        Assert.True(node.Metadata.ContainsKey("mermaid.style." + property));
        var svg = XDocument.Parse(rendered.Artifact.ToSvg());
        Assert.DoesNotContain(svg.Descendants().Attributes("fill"), attribute => attribute.Value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
            || attribute.Value.StartsWith("url(", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(rendered.Artifact.ToPng());
    }

    [Theory]
    [InlineData("class Missing marked")]
    [InlineData("style Missing fill:#fef9c3")]
    public void UnmatchedStyleTargetsRemainLocatedWithoutCreatingStatesOrTransitions(string statement) {
        var rendered = MermaidRenderer.Render("stateDiagram-v2\nA\n  " + statement);
        Assert.False(rendered.HasErrors);
        var diagnostic = Assert.Single(rendered.Diagnostics);
        Assert.Equal((MermaidDiagnosticCodes.UnsupportedStatement, 3, 3, statement.Length),
            (diagnostic.Code, diagnostic.Span.Line, diagnostic.Span.Column, diagnostic.Span.Length));
        var document = Assert.IsType<MermaidStateDocument>(rendered.Document);
        Assert.Equal(statement, Assert.Single(document.RawStatements).Text);
        Assert.Equal("A", Assert.Single(document.States).Id);
        Assert.Empty(document.Transitions);
        Assert.Equal("A", Assert.Single(Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Nodes).Id);
    }

    [Theory]
    [InlineData("classDef")]
    [InlineData("class A")]
    [InlineData("class , marked")]
    [InlineData("style A")]
    [InlineData("A:::")]
    [InlineData("A:::one:::two")]
    [InlineData("A --> B:::")]
    [InlineData("state Group::: {\nA\n}")]
    public void MalformedStyleSyntaxRejectsArtifactsWithTheOriginalLine(string statement) {
        var source = "stateDiagram-v2\n  " + statement;
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors);
        Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 2 && diagnostic.Span.Column == 3);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
    }

    [Fact]
    public void SharedTypedDefinitionsAndMembershipRemainUsableWithoutAParser() {
        var span = new MermaidSourceSpan(1, 1, 1);
        var document = new MermaidStateDocument();
        var definition = new MermaidStyleDefinition("marked", span);
        definition.Styles["fill"] = "#fef9c3";
        definition.Styles["stroke"] = "#92400e";
        document.ClassDefinitions.Add(definition);
        var state = new MermaidStateNode("A", span);
        state.Classes.Add("marked");
        state.Styles["color"] = "#111827";
        document.States.Add(state);
        var node = Assert.Single(document.ToTopologyChart().Nodes);
        Assert.Equal(("#fef9c3", "#92400e", "#111827"), (node.BackgroundColor, node.Color, node.LabelColor));
    }

    [Theory]
    [InlineData("stateDiagram-v2\nA\nclassDef marked fill:#fef9c3,stroke:#92400e,color:#111827", 4)]
    [InlineData("stateDiagram-v2\nA\nclass A one,two,three,four", 4)]
    public void DefinitionsAndMembershipConsumeTheCapturedSemanticBudget(string source, int maximum) {
        Assert.False(new MermaidParser().Parse(source).HasErrors);
        var result = new MermaidParser().Parse(source, new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumElements = maximum }
        });
        Assert.Null(result.Document);
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        Assert.Contains(nameof(MermaidResourceLimits.MaximumElements), diagnostic.Message);
        Assert.Equal(3, diagnostic.Span.Line);
    }

    [Fact]
    public void ReturnedStateStyleModelCannotExceedTheBudgetAfterAssignmentsAreResolved() {
        const string source = "stateDiagram-v2\nA\nclassDef marked fill:#fef9c3\nclass A marked";
        var parser = new MermaidParser();
        var accepted = parser.Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 6 } });
        Assert.Empty(accepted.Diagnostics);
        Assert.NotNull(accepted.Document);
        var rejected = parser.Parse(source, new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 5 } });
        Assert.Null(rejected.Document);
        Assert.Equal(MermaidDiagnosticCodes.ResourceLimitExceeded, Assert.Single(rejected.Diagnostics).Code);
    }

    private static string StyledSource(string direction) => "stateDiagram-v2\ndirection " + direction
        + "\nclassDef default fill:#dbeafe,stroke:#1e3a8a,color:#0f172a\nclass A first\nstate Active:::shell {\ndirection " + direction
        + "\n[*] --> A:::inline : begin\nA: A label\nstate Choice <<choice>>\nA --> Choice\nnote right of A: keeps note\n--\n[*] --> B:::first\nB --> [*]\n}\nActive --> Done\n"
        + "classDef first fill:#fef9c3,stroke:#d97706,color:#78350f\nclassDef first color:#111827\nclassDef inline fill:#c4b5fd\n"
        + "classDef shell fill:#fce7f3,stroke:#be185d,color:#831843\nclass A last\nclassDef last fill:#fed7aa\nstyle A fill:#86efac,color:#14532d";

    private static int Count(byte[] pixels, ChartColor color) {
        var count = 0;
        for (var index = 0; index < pixels.Length; index += 4)
            if (pixels[index] == color.R && pixels[index + 1] == color.G && pixels[index + 2] == color.B) count++;
        return count;
    }

    private static void Save(string svg, byte[] png, VisualArtifactInterchangeEnvelope facts, string direction, bool dark, int width) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var path = Path.Combine(output, "state-style-" + direction.ToLowerInvariant() + "-" + width + (dark ? "-dark" : "-light"));
        File.WriteAllText(path + ".native.svg", svg);
        File.WriteAllBytes(path + ".native.png", png);
        File.WriteAllText(path + ".interchange.json", facts.ToJson());
    }
}
