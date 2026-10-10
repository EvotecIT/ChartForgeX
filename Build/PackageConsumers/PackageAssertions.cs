using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

internal static class PackageAssertions {
    internal static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static bool Contains(string value, string expected) =>
        value.IndexOf(expected, StringComparison.Ordinal) >= 0;

    internal static void Png(byte[] bytes) => Require(bytes.Length > 64 &&
        bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71, "PNG export failed.");

    internal static void Gif(byte[] bytes) => Require(bytes.Length > 16 &&
        bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F', "GIF export failed.");

    internal static void Apng(byte[] bytes) {
        Png(bytes);
        Require(Contains(System.Text.Encoding.ASCII.GetString(bytes), "acTL"), "APNG has no animation control.");
    }

    internal static Chart Chart() => ChartForgeX.Core.Chart.Create().WithSize(320, 200).WithTitle("Packed chart")
        .AddLine("Values", new[] { new ChartPoint(0, 2), new ChartPoint(1, 5), new ChartPoint(2, 3) });

    internal static TopologyChart Topology() => TopologyChart.Create().WithId("packed-topology").WithViewport(420, 260, 20)
        .AddNode("api", "API", 80, 110, TopologyNodeKind.Service)
        .AddNode("db", "Database", 290, 110, TopologyNodeKind.Database)
        .AddEdge("api-db", "api", "db", "queries", TopologyEdgeKind.Dependency);

    internal static void CoreFixtures() {
        var core = typeof(Chart).Assembly;
        Require(core.GetManifestResourceNames().Contains("ChartForgeX.Themes.Tokens.evotec.chartforgex.tokens.json"),
            "Core theme resource is missing.");
        Require(core.GetManifestResourceNames().Contains("ChartForgeX.Topology.Assets.topology.css"),
            "Core topology stylesheet is missing.");
        var theme = VisualTheme.Graphite();
        var prepared = Chart().Prepare(new VisualRenderContext(
            new VisualLayoutOptions(new VisualSize(320, 200), padding: 16),
            frame: new VisualFrame(title: "Packed chart"), theme: theme));
        Require(Contains(prepared.ToSvg(), "<svg"), "Prepared SVG failed.");
        Png(prepared.ToPng());
        var bubbles = ChartForgeX.Core.Chart.Create().WithSize(320, 200)
            .AddBubble("First", new[] { new ChartBubble(1, 20, 100) })
            .AddBubble("Second", new[] { new ChartBubble(2, 30, 100), new ChartBubble(3, 40, 1000) })
            .ConfigureBubble(bubble => { bubble.WithSizeDomain(0, 100); bubble.MinimumRadius = 2; bubble.MaximumRadius = 14; });
        var bubbleSvg = XDocument.Parse(bubbles.ToSvg());
        var bubbleMarks = bubbleSvg.Descendants().Where(mark => (string?)mark.Attribute("data-cfx-role") == "bubble").ToArray();
        Require(bubbleMarks.Length == 3 && bubbleMarks.All(mark => double.Parse(mark.Attribute("rx")!.Value, CultureInfo.InvariantCulture) == 14),
            "Packed bubble size-domain or radius contract failed.");
        Require(bubbleSvg.Descendants().Any(group => (string?)group.Attribute("data-cfx-size") == "1000"),
            "Packed bubble size clamping changed the raw observation.");
        Png(bubbles.ToPng());
        var artifact = prepared.ToArtifact("packed-chart", VisualArtifactKind.Chart);
        Require(Contains(artifact.ToInterchangeJson(), "packed-chart"), "Neutral artifact interchange failed.");
        var topology = Topology();
        Require(Contains(topology.ToSvg(), "<svg"), "Core topology SVG failed.");
        Png(topology.ToPng());
        var table = TableArtifact.Create("neutral-table").AddColumn("state", "State").AddRow("one", "Ready");
        Require(table.Rows.Count == 1 && typeof(TableArtifact).Assembly == core && typeof(VisualStatus).Assembly == core,
            "Neutral table/status contracts moved out of core.");
        foreach (var name in new[] { "WardleyMapBlock", "VennDiagramBlock", "PacketLayoutBlock",
            "GitGraphBlock", "FishboneDiagramBlock", "BlockLayoutBlock" })
            Require(core.GetType("ChartForgeX.VisualBlocks." + name) != null, "Genuine diagram left core: " + name);
        foreach (var name in new[] { "ChartForgeX.Composition.VisualCanvas", "ChartForgeX.Composition.ImageComposition",
            "ChartForgeX.Stories.VisualStory", "ChartForgeX.Terminal.TerminalStory",
            "ChartForgeX.VisualArtifacts.VisualWatermark", "ChartForgeX.Raster.GifWriter", "ChartForgeX.Raster.ApngWriter" })
            Require(core.GetType(name) == null, "Optional producer/encoder remains in core: " + name);
        var gif = new byte[] { 0x47,0x49,0x46,0x38,0x39,0x61,0x02,0x00,0x02,0x00,0x80,0x00,0x00,
            0x00,0xFF,0x00,0xFF,0x00,0x00,0x2C,0x01,0x00,0x01,0x00,0x01,0x00,0x01,0x00,0x00,
            0x02,0x02,0x4C,0x01,0x00,0x3B };
        var decoded = RasterImageDecoder.Decode(gif);
        Require(decoded.Width == 2 && decoded.Height == 2 && decoded.Pixels[1] == 255,
            "Static GIF first-frame input failed.");
        References(core);
    }

    internal static void Owner(Type type, string owner) =>
        Require(type.Assembly.GetName().Name == owner, "Wrong assembly owner for " + type.FullName);

    internal static void References(Assembly assembly, params string[] expected) {
        var actual = assembly.GetReferencedAssemblies().Where(a => a.Name != null && a.Name.StartsWith("ChartForgeX", StringComparison.Ordinal))
            .Select(a => a.Name!).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Require(actual.SequenceEqual(expected.OrderBy(n => n, StringComparer.Ordinal)),
            assembly.GetName().Name + " references unexpected ChartForgeX assemblies: " + string.Join(", ", actual));
    }

    internal static void Payload(params string[] expected) {
        var actual = Directory.GetFiles(AppContext.BaseDirectory, "ChartForgeX*.dll")
            .Select(path => Path.GetFileNameWithoutExtension(path)!).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Require(actual.SequenceEqual(expected.OrderBy(n => n, StringComparer.Ordinal)),
            "Package output has unexpected ChartForgeX assemblies: " + string.Join(", ", actual));
        var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name!)
            .Where(n => n.StartsWith("ChartForgeX", StringComparison.Ordinal)).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Require(loaded.SequenceEqual(expected.OrderBy(n => n, StringComparer.Ordinal)),
            "Runtime loaded unexpected ChartForgeX assemblies: " + string.Join(", ", loaded));
    }
}
