using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSequenceRectNumberingCausalTests {
    [Fact]
    public void SourceRectPaintsItsNativeBackgroundWithoutDisplayingTheColorAsAFragmentCaption() {
        const string source = "sequenceDiagram\nparticipant A\nparticipant B\nrect rgb(255,0,0)\nA->>B: Highlighted message\nend";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(rendered.Artifact);
        var xml = XDocument.Parse(artifact.ToSvg());
        Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "sequence-message");
        var background = Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "sequence-block-background");
        Assert.Equal("#FF0000", (string?)background.Attribute("fill"));
        Assert.DoesNotContain(xml.Descendants().Where(element => element.Name.LocalName == "text"), element => element.Value.Contains("rgb(", StringComparison.Ordinal));
    }

    [Fact]
    public void AuthoredNumberControlsApplyAtTheirOwnMessageOccurrenceAndResumeAfterHiddenMessages() {
        const string source = "sequenceDiagram\nA->>B: Before\nautonumber 5 0.5\nA->>B: Started\n" +
            "autonumber off\nB-->>A: Hidden\nautonumber\nA->>B: Resumed\nautonumber 10\nB-->>A: Reset";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(rendered.Artifact);
        var labels = XDocument.Parse(artifact.ToSvg()).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        Assert.Contains("Before", labels); Assert.Contains("Hidden", labels);
        Assert.Contains("Started", labels); Assert.Contains("Resumed", labels); Assert.Contains("Reset", labels);
        var numbers = XDocument.Parse(artifact.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "sequence-number").Select(element => element.Value).ToArray();
        Assert.Equal(new[] { "5", "6", "10" }, numbers);
        Assert.DoesNotContain(labels, label => label.EndsWith(". Before", StringComparison.Ordinal) || label.EndsWith(". Hidden", StringComparison.Ordinal));
    }
}
