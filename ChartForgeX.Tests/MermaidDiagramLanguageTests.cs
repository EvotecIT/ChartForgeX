using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Unthemed Mermaid families share flat connectors, content-fitted cards and balanced C4 framing.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidDiagramLanguageTests {
    private const string C4 = "C4Container\ntitle Checkout platform\nPerson(customer, \"Customer\", \"Places an order\")\n"
        + "Enterprise_Boundary(company, \"Company\") {\n  System_Boundary(shop, \"Store\") {\n"
        + "    Container(web, \"Web app\", \"Browser\", \"Browse products and complete checkout\")\n"
        + "    Container(api, \"Order service\", \".NET\", \"Validate orders and coordinate payment\")\n"
        + "    ContainerDb(db, \"Orders\", \"SQL\", \"Store orders and their fulfilment state\")\n  }\n}\n"
        + "Rel(customer, web, \"Places an order\")\nRel(web, api, \"Submits checkout\", \"HTTPS\")\nRel(api, db, \"Stores the order\")\n";

    [Theory]
    [InlineData(920, 690, .85, 1.6)]
    [InlineData(1280, 640, 1.8, 3.2)]
    public void C4BoundaryChainFollowsTheTargetLongAxisAndCardsFitTheirContent(int width, int height, double minimumAspect, double maximumAspect) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var svg = Render(C4, new MermaidRenderOptions { C4 = new MermaidTopologyRenderOptions { Width = width, Height = height } }, mode);
            var box = ((string)svg.Root!.Attribute("viewBox")!).Split(' ').Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray();
            Assert.InRange(box[2] / box[3], minimumAspect, maximumAspect);
            // The person card keeps its natural width; long descriptions wrap at the shared card limit.
            Assert.True(SurfaceWidth(svg, "customer") < SurfaceWidth(svg, "web") - 40);
            Assert.True(SurfaceWidth(svg, "web") <= DiagramTheme.CardTextMaxWidth + 32 + 20 + 1);
        }
    }

    [Fact]
    public void UnthemedSequenceIsFlatWhileAnExplicitNeoLookKeepsMermaidElevation() {
        const string body = "sequenceDiagram\nparticipant A as Review\nparticipant B as Archive\nA->>B: Save\n";
        var flat = Sequence(body);
        Assert.All(flat.Participants, participant => Assert.Null(participant.SurfaceShadow));
        var document = (MermaidSequenceDocument)new MermaidParser().Parse(body).Document!;
        Assert.DoesNotContain("<filter", document.ToVisualArtifact().ToSvg(), StringComparison.Ordinal);
        var neo = Sequence("---\nconfig:\n  look: neo\n---\n" + body);
        Assert.Contains(neo.Participants, participant => participant.SurfaceShadow != null);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)] [InlineData(VisualThemeMode.Dark)]
    public void ConnectorsAreSingleSolidStrokesUnlessTheNotationIsDashed(VisualThemeMode mode) {
        var architecture = Render("architecture-beta\ngroup api(cloud)[API]\nservice db(database)[Database] in api\nservice server(server)[Server] in api\ndb:L -- R:server\n",
            new MermaidRenderOptions(), mode);
        var lines = Role(architecture, "topology-edge-line").ToArray();
        Assert.NotEmpty(lines);
        Assert.All(lines, line => {
            Assert.Null(line.Attribute("stroke-dasharray"));
            Assert.Equal(DiagramTheme.ConnectorStrokeWidth, double.Parse((string)line.Attribute("stroke-width")!, CultureInfo.InvariantCulture), 3);
        });
        Assert.Empty(Role(architecture, "topology-edge-line-halo"));
        var requirement = Render("requirementDiagram\nrequirement r { id: 1\ntext: Sign in\n}\nelement e { type: service\n}\ne - satisfies -> r\n", new MermaidRenderOptions(), mode);
        Assert.All(Role(requirement, "topology-edge-line"), line => Assert.NotNull(line.Attribute("stroke-dasharray")));
    }

    [Fact]
    public void EntityAttributeGridHeaderIsAQuietCaptionRowInsideTheCard() {
        var svg = Render("erDiagram\nCUSTOMER {\n  string id PK\n  string name\n}\n", new MermaidRenderOptions(), VisualThemeMode.Light);
        var header = Assert.Single(Role(svg, "table-header"));
        Assert.True((string?)header.Attribute("stroke") is null or "none");
        Assert.NotEmpty(Role(svg, "table-header-cell"));
    }

    private static XDocument Render(string source, MermaidRenderOptions options, VisualThemeMode mode) {
        var document = new MermaidParser().Parse(source).Document!;
        options.Presentation ??= new MermaidPresentationOptions(); options.Presentation.ThemeMode = mode;
        options.Presentation.FontFamily = BrowserProofFonts.Family;
        return XDocument.Parse(document.ToVisualArtifact(options).ToSvg());
    }
    private static SequenceArtifact Sequence(string source) =>
        ((MermaidSequenceDocument)new MermaidParser().Parse(source).Document!).ToSequenceArtifact();
    private static IEnumerable<XElement> Role(XDocument svg, string role) => svg.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == role);
    private static double SurfaceWidth(XDocument svg, string id) {
        var node = svg.Descendants().First(item => (string?)item.Attribute("data-node-id") == id && (string?)item.Attribute("data-cfx-role") == "topology-node");
        var surface = node.Descendants().First(item => (string?)item.Attribute("data-cfx-role") == "topology-node-surface");
        var xs = ((string)surface.Attribute("d")!).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .Select(token => double.Parse(token, CultureInfo.InvariantCulture)).Where((_, index) => index % 2 == 0).ToArray();
        return xs.Max() - xs.Min();
    }
}
