using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ChordSeriesRendersDirectedWeightedNativeFlows() {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Chord, "options");
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720, 460))));
        var svg = prepared.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"chord-ribbon\"", StringComparison.Ordinal), "Chord must render native weighted ribbons.");
        var reverse = XDocument.Parse(svg).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "chord-link"
            && (string?)node.Attribute("data-cfx-target-id") == "south-north");
        Assert((string?)reverse.Attribute("data-cfx-source") == "south" && (string?)reverse.Attribute("data-cfx-target") == "north", "Chord must retain reciprocal source and target IDs.");
        Assert(prepared.Scene.Nodes.Count(node => node.Role == "chord-ribbon") == 8, "Zero flows must not receive a positive ribbon.");
        Assert(prepared.ToPng(new VisualRenderOptions(supersampling: 1)).Length > 0, "Chord must produce native PNG output.");
    }
}
