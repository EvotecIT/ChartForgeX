using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class GallerySvgReadabilityTests {
    [Fact]
    public void DecorativeHairlinesAndLegendGlyphsDoNotHideUnreadableInformation() {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-svg-readability-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var chart = Chart.Create().WithSize(320, 180)
                .AddLine("Values", Enumerable.Range(1, 3).Select(i => new ChartForgeX.Primitives.ChartPoint(i, i)));
            const string svg = """
                <svg xmlns="http://www.w3.org/2000/svg" width="320" height="180" viewBox="0 0 320 180">
                  <g data-cfx-role="topology">
                    <path data-cfx-role="topology-map-boundary" d="M20 20L80 20" stroke="black" stroke-width=".5"/>
                    <path data-cfx-role="dotted-map-boundary" d="M20 25L80 25" stroke="black" stroke-width=".5"/>
                    <path data-cfx-role="topology-grid" d="M20 30L80 30" stroke="black" stroke-width=".5"/>
                    <ellipse data-cfx-role="topology-icon-surface" cx="30" cy="50" rx="5" ry="5" stroke="black" stroke-width=".5"/>
                    <g data-cfx-role="topology-legend-item">
                      <g data-cfx-role="topology-node-icon">
                        <path d="M20 60L30 60" stroke="black" stroke-width=".4"/>
                        <text x="20" y="60" font-size="4">GW</text>
                      </g>
                      <text data-cfx-role="legend-label" x="40" y="60" font-size="12">Gateway</text>
                    </g>
                    <text data-cfx-role="topology-node-label" x="20" y="90" font-size="7">Small node label</text>
                    <g data-cfx-role="topology-node-symbol"><text x="20" y="110" font-size="7">DC</text></g>
                    <path data-cfx-role="topology-edge-line" d="M20 125L100 125" stroke="black" stroke-width=".5"/>
                    <rect data-cfx-role="topology-node-surface" x="120" y="100" width="40" height="40" stroke="black" stroke-width=".5"/>
                    <path data-cfx-role="topology-marker" d="M100 125L95 120L95 130Z" stroke="black" stroke-width=".5"/>
                    <circle data-cfx-role="scatter-point" cx="180" cy="110" r="2" fill="black"/>
                  </g>
                </svg>
                """;
            File.WriteAllText(Path.Combine(output, "chart.svg"), svg);
            File.WriteAllText(Path.Combine(output, "chart.html"), chart.ToHtmlPage());
            File.WriteAllBytes(Path.Combine(output, "chart.png"), chart.ToPng());
            GalleryWriter.Write(output);
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
            var quality = document.RootElement.GetProperty("charts")[0].GetProperty("svg");
            Assert.Equal(2, quality.GetProperty("tinyTextNodes").GetInt32());
            Assert.Equal(7, quality.GetProperty("minimumTextFontSize").GetDouble());
            Assert.Equal(3, quality.GetProperty("strokedNodes").GetInt32());
            Assert.Equal(3, quality.GetProperty("tinyStrokeNodes").GetInt32());
            Assert.Equal(.5, quality.GetProperty("minimumStrokeWidth").GetDouble());
            Assert.Equal(1, quality.GetProperty("tinyMarkerNodes").GetInt32());
            Assert.False(quality.GetProperty("healthy").GetBoolean());
        } finally { Directory.Delete(output, true); }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sequence-message-line")]
    [InlineData("sequence-notation")]
    public void SourceLifelineGuidesKeepTheirHalfUnitDefaultWhileMessageAndActorInkStayGated(string? unreadableRole) {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-sequence-readability-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var rendered = MermaidRenderer.Render("sequenceDiagram\nactor A as Review\nparticipant B as Archive\nA->>B: Save");
            Assert.Empty(rendered.Diagnostics);
            var artifact = Assert.IsType<ChartForgeX.VisualArtifacts.VisualArtifact>(rendered.Artifact);
            var svg = XDocument.Parse(artifact.ToSvg());
            var lifelines = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "sequence-lifeline").ToArray();
            Assert.NotEmpty(lifelines);
            Assert.All(lifelines, element => Assert.Equal("0.5", (string?)element.Attribute("stroke-width")));
            if (unreadableRole == "sequence-message-line") {
                var message = svg.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == unreadableRole);
                message.SetAttributeValue("stroke-width", ".5");
            } else if (unreadableRole == "sequence-notation") {
                var actor = svg.Descendants().First(element => (string?)element.Attribute("data-cfx-role") == unreadableRole
                    && (string?)element.Attribute("data-kind") == "Actor");
                actor.Descendants().First(element => element.Attribute("stroke-width") != null).SetAttributeValue("stroke-width", ".5");
            }
            File.WriteAllText(Path.Combine(output, "sequence.svg"), svg.ToString(SaveOptions.DisableFormatting));
            File.WriteAllText(Path.Combine(output, "sequence.html"), artifact.ToHtmlPage());
            File.WriteAllBytes(Path.Combine(output, "sequence.png"), artifact.ToPng());
            GalleryWriter.Write(output);
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
            var quality = document.RootElement.GetProperty("charts")[0].GetProperty("svg");
            Assert.Equal(unreadableRole == null ? 0 : 1, quality.GetProperty("tinyStrokeNodes").GetInt32());
            Assert.Equal(unreadableRole == null, quality.GetProperty("healthy").GetBoolean());
        } finally { Directory.Delete(output, true); }
    }
}
