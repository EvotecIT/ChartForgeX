using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static XElement[] TopologyRoleTexts(string svg, string role) => XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role)
        .SelectMany(element => element.DescendantsAndSelf().Where(child => child.Name.LocalName == "text")).ToArray();

    private static XElement TopologyEntity(string svg, string kind, string id) => XDocument.Parse(svg).Descendants()
        .Single(element => (string?)element.Attribute("data-cfx-role") == "topology-" + kind && (string?)element.Attribute("data-" + kind + "-id") == id);

    private static XElement TopologyEdgeLine(string svg, string id) => TopologyEntity(svg, "edge", id).Descendants()
        .First(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");

    private static ChartRect TopologySurfaceBounds(string svg, string kind, string id) {
        var surface = TopologyEntity(svg, kind, id).Descendants().First(element => (string?)element.Attribute("data-cfx-role") == "topology-" + kind + "-surface");
        return new ChartRect((double)surface.Attribute("x")!, (double)surface.Attribute("y")!, (double)surface.Attribute("width")!, (double)surface.Attribute("height")!);
    }

    private static bool TopologyHasDot(string svg, string? nodeId = null) => XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "topology-node" && (string?)element.Attribute("data-node-display-mode") == "Dot"
            && (nodeId == null || (string?)element.Attribute("data-node-id") == nodeId))
        .Any(element => element.Descendants().Any(mark => mark.Name.LocalName == "ellipse" && (string?)mark.Attribute("data-cfx-role") == "topology-node-surface"
            && (double?)mark.Attribute("rx") > 0 && (double?)mark.Attribute("ry") > 0));

    private static string TopologyLegendMarkup(string svg) {
        var document = XDocument.Parse(svg);
        var entries = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") is "legend-entry" or "legend-title").ToArray();
        Assert(entries.Any(element => (string?)element.Attribute("data-cfx-role") == "legend-entry"), "A visible topology legend should render measured entries.");
        return new XElement(document.Root!.Name, entries.Select(element => new XElement(element))).ToString(SaveOptions.DisableFormatting);
    }
}
