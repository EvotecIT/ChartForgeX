using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSequenceParticipantCausalTests {
    [Fact]
    public void OfficialParticipantMetadataSelectsItsExistingNativeGlyphAliasAndSingleJsonLink() {
        const string source = "sequenceDiagram\nparticipant A@{type: database, alias: Storage}\nparticipant B\nlinks A: {\"Guide\":\"https://example.test/guide\"}\nA->>B: Read";
        var rendered = MermaidRenderer.Render(source); Assert.Empty(rendered.Diagnostics);
        var xml = XDocument.Parse(rendered.Artifact!.ToSvg());
        Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-kind") == "Database");
        Assert.Contains(xml.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Storage");
        Assert.Contains(xml.Descendants(), element => element.Name.LocalName == "a" && element.Attributes().Any(attribute => attribute.Name.LocalName == "href" && attribute.Value == "https://example.test/guide"));
    }
}
