using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidClassErCaptionTests {
    [Fact]
    public void QuotedClassIdentitySurvivesEverySupportedReferenceWithoutLosingLiteralTildesOrColons() {
        var result = MermaidRenderer.Render(Source("class"), Options(false, 1100));
        Assert.False(result.HasErrors);
        Assert.All(result.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.ApproximateMapping, item.Code));
        var document = Assert.IsType<MermaidClassDocument>(result.Document);
        var owner = document.Classes.Single(node => node.Id == "API: service~literal");
        Assert.Equal("Domain", owner.Namespace);
        Assert.Equal("`**API**<br>service #38; data`", owner.Label);
        Assert.Equal(new[] { "+List<Item> find()", "+int count" }, owner.Members.Select(member => member.DisplayText));
        Assert.True(owner.Members[0].IsAbstract); Assert.True(owner.Members[1].IsStatic);
        Assert.Equal(owner.Id, Assert.Single(document.Notes).NodeId);
        Assert.Equal("https://example.invalid/service", owner.Href);
        var relationship = Assert.Single(document.Relationships);
        Assert.Equal((owner.Id, "Pair", "1", "0..*"), (relationship.SourceId, relationship.TargetId, relationship.SourceMultiplicity, relationship.TargetMultiplicity));
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var native = chart.Nodes.Single(node => node.Id == owner.Id);
        Assert.Equal("API\nservice & data", native.Label);
        Assert.Equal(owner.Label, native.Metadata["mermaid.source.label"]);
        Assert.Equal("#dbeafe", native.BackgroundColor);
        Assert.Equal("Pair<K,List<V>>", document.Classes.Single(node => node.Id == "Pair").Label);
        Assert.Equal("Pair~K,List~V~~", chart.Nodes.Single(node => node.Id == "Pair").Metadata["mermaid.generic.source"]);
        Assert.Equal(TopologyMarkerKind.OpenDiamond, Assert.Single(chart.Edges).SourceMarker);
        Assert.Equal("owns\nmany", Assert.Single(chart.Edges).Label);
        Assert.Equal(2, native.Details.Count);
        Assert.Equal(owner.Members[0].Text, native.Details[0].Metadata["mermaid.member.source"]);
    }

    [Theory]
    [InlineData("Operator --> endpoint")]
    [InlineData("Comma, class")]
    [InlineData("Square [brackets]")]
    [InlineData("Literal ~ suffix")]
    public void BacktickClassNamesStayAtomicAcrossDeclarationsMembersRelationshipsAndLollipops(string identity) {
        var source = "classDiagram\nclass `" + identity + "`\nclass Target\n`" + identity + "` : +run()\n`" + identity + "` \"1\" <|--|> Target : link\nPort ()-- `" + identity + "`\ncssClass `" + identity + "` marked\nclassDef marked fill:#dbeafe";
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidClassDocument>(result.Document);
        Assert.Equal(new[] { identity, "Target" }, document.Classes.Select(node => node.Id));
        Assert.Equal(identity, document.Relationships[0].SourceId);
        Assert.Equal(identity, document.Relationships[1].TargetId);
        Assert.Single(document.Classes[0].Members);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal("#dbeafe", chart.Nodes.Single(node => node.Id == identity).BackgroundColor);
        Assert.Single(chart.Nodes, node => node.Shape == TopologyNodeShape.Circle);
        Assert.Equal(TopologyMarkerKind.OpenTriangle, chart.Edges[0].SourceMarker);
        Assert.Equal(TopologyMarkerKind.OpenTriangle, chart.Edges[0].TargetMarker);
    }

    [Theory]
    [InlineData("class Pair~K,V")]
    [InlineData("class Pair~K,,V~")]
    [InlineData("class Pair~K,V~~")]
    [InlineData("class Pair~K,~")]
    [InlineData("class Pair~K+V~")]
    [InlineData("class Pair~?~")]
    [InlineData("class `unclosed")]
    [InlineData("class ``")]
    [InlineData("class A,B")]
    public void MalformedClassIdentityIsLocatedAndNeverProducesPartialDirectOrFenceArtifacts(string statement) {
        var source = "classDiagram\nclass Existing\n  " + statement;
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line == 3 && item.Span.Column == 3 && item.Span.Length == statement.Length);
        var fenced = new MermaidVisualMarkupParser().Parse("# Heading\n\n```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
        Assert.Contains(fenced.Diagnostics, item => item.Line == 6);
    }

    [Fact]
    public void GenericDepthAndWarningRejectionUseTheExistingCapturedRequestPolicy() {
        var options = Options(false, 390);
        options.ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumNestingDepth = 2 } };
        var result = MermaidRenderer.Render("classDiagram\nclass Deep~List~List~int~~~", options);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line == 2);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\nclassDiagram\nclass Deep~List~List~int~~~\n```");
        Assert.Empty(fence.Artifacts); Assert.True(fence.HasErrors);
        options.ParseOptions = null; options.WarningPolicy = MermaidWarningPolicy.Reject;
        var strict = MermaidRenderer.Render("classDiagram\nclass Pair~K,V~", options);
        Assert.Null(strict.Artifact);
        Assert.Contains(strict.Diagnostics, item => item.Code == MermaidDiagnosticCodes.RejectedWarning);
    }

    [Theory]
    [InlineData("class", false, 390)]
    [InlineData("class", true, 390)]
    [InlineData("class", false, 1100)]
    [InlineData("class", true, 1100)]
    [InlineData("er", false, 390)]
    [InlineData("er", true, 390)]
    [InlineData("er", false, 1100)]
    [InlineData("er", true, 1100)]
    public void NativeAndFencedCaptionsShareMeasuredSvgPngAndPortableSourceFacts(string family, bool dark, int width) {
        var options = Options(dark, width); var source = Source(family);
        var result = MermaidRenderer.Render(source, options); Assert.False(result.HasErrors);
        if (family == "class") Assert.NotEmpty(result.Diagnostics); else Assert.Empty(result.Diagnostics);
        Assert.All(result.Diagnostics, item => { Assert.Equal(MermaidDiagnosticCodes.ApproximateMapping, item.Code); Assert.True(item.Span.Line > 1); });
        var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors); var fenced = Assert.Single(fence.Artifacts);
        Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
        var xml = XDocument.Parse(svg);
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject" or "image");
        var texts = xml.Descendants().Where(element => element.Name.LocalName == "text").ToArray();
        Assert.DoesNotContain(texts, element => element.Value.Contains("**", StringComparison.Ordinal) || element.Value.Contains("<br", StringComparison.Ordinal));
        Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-cfx-caption") == (family == "class" ? "API\nservice & data" : "Customer\naccount"));
        var image = RasterImageDecoder.Decode(png); Assert.True(image.Width >= width); Assert.True(image.Height > 0);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(envelope.ToJson(), VisualArtifactInterchangeEnvelope.FromUtf8Json(envelope.ToUtf8Json()).ToJson());
        Assert.NotNull(envelope.AccessibleName);
        Assert.Contains(envelope.Nodes, node => node.Extensions.ContainsKey("mermaid.source.label"));
        if (family == "class") Assert.Equal("Pair~K,List~V~~", envelope.Nodes.Single(node => node.Id == "Pair").Extensions["mermaid.generic.source"]);
        else {
            Assert.Equal("**Customer**<br>account", envelope.Nodes.Single(node => node.Id == "C").Extensions["mermaid.source.label"]);
            Assert.Equal("**External** key #38; value", envelope.Nodes.Single(node => node.Id == "C").Details[0].Extensions["mermaid.attribute.comment"]);
            Assert.Equal("Domain: core", envelope.Groups.Single(group => group.Id == "Order group").Topology!.ParentGroupId);
        }
        Capture(family + "-captions-" + width + (dark ? "-dark" : "-light"), svg, png);
    }

    [Fact]
    public void NativeEditsReexportIndependentlyOfCapturedSourceFactsAndPortableSnapshots() {
        var result = MermaidRenderer.Render(Source("class"), Options(false, 1100));
        var artifact = result.Artifact!; var chart = Assert.IsType<TopologyChart>(artifact.Model);
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var saved = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); var json = saved.ToJson();
        var node = chart.Nodes.Single(item => item.Id == "API: service~literal");
        node.Label = "Edited service"; node.Details[0].Text = "+edited()";
        Assert.NotEqual(svg, artifact.ToSvg()); Assert.False(png.SequenceEqual(artifact.ToPng()));
        var edited = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).Nodes.Single(item => item.Id == node.Id);
        Assert.Equal("Edited service", edited.Label); Assert.Equal("+edited()", edited.Details[0].Text);
        Assert.Equal("`**API**<br>service #38; data`", edited.Extensions["mermaid.source.label"]);
        Assert.Equal("+List~Item~ find()*", edited.Details[0].Extensions["mermaid.member.source"]);
        Assert.Equal(json, saved.ToJson());
        Assert.Equal("API\nservice & data", saved.Nodes.Single(item => item.Id == node.Id).Label);
        var document = Assert.IsType<MermaidClassDocument>(result.Document); document.Classes.Single(item => item.Id == node.Id).Label = "`**Changed** label`";
        Assert.Equal("Changed label", document.ToTopologyChart().Nodes.Single(item => item.Id == node.Id).Label);
        Assert.Equal("Edited service", node.Label);
    }

    [Theory]
    [InlineData("classDiagram\nclass A[\"<img src='https://example.invalid/a' onerror='alert(1)'><br>$$x$$\"]")]
    [InlineData("erDiagram\nA[\"<img src='https://example.invalid/a' onerror='alert(1)'><br>$$x$$\"]")]
    public void HtmlAndMathAreLocatedLiteralCaptionsAndNeverExecutableAssets(string source) {
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedStatement && item.Span.Line == 2);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Contains("<img", Assert.Single(chart.Nodes).Label, StringComparison.Ordinal);
        var xml = XDocument.Parse(result.Artifact.ToSvg());
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "image" or "img" or "foreignObject");
        Assert.DoesNotContain(xml.Descendants().Attributes(), attribute => attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase));
        Assert.True(result.Artifact.ToPng().Length > 100);
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.Null(strict.Artifact);
    }

    [Theory]
    [InlineData("classDiagram\nA --> B\n  class A[\"`**Updated**`\"]")]
    [InlineData("erDiagram\nA ||--o{ B : link\n  A[\"**Updated**\"]")]
    public void ReplacementCaptionsLocateTheirDeclarationWhileStableIdentityKeepsItsFirstOccurrence(string source) {
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal("Updated", chart.Nodes.Single(node => node.Id == "A").Label);
        var fence = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors); Assert.Empty(fence.Diagnostics);
    }

    [Fact]
    public void FlowchartCaptionWrapperAndDecodeOrderRemainUnchanged() {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA[\"**Literal**\"] -->|\"`**Send**`\"| B[\"`**API**<br>#38; #lt;br#gt;`\"]");
        Assert.False(parsed.HasErrors); var chart = parsed.Document!.ToTopologyChart();
        Assert.Equal("**Literal**", chart.Nodes.Single(node => node.Id == "A").Label);
        Assert.Equal("API\n& <br>", chart.Nodes.Single(node => node.Id == "B").Label);
        Assert.Equal("Send", Assert.Single(chart.Edges).Label);
    }

    internal static string Source(string family) => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "Mermaid", family + "-native-captions.mmd"));
    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, TopologySizing = MermaidTopologySizing.Natural },
        Class = new MermaidTopologyRenderOptions { Width = width, Height = 620 },
        EntityRelationship = new MermaidTopologyRenderOptions { Width = width, Height = 620 }
    };
    private static void Capture(string name, string svg, byte[] png) {
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, name + ".native.svg"), svg); File.WriteAllBytes(Path.Combine(root, name + ".native.png"), png);
    }
}
