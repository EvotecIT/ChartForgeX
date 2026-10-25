using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

internal static partial class Program {
    private static void ChordDisplayContracts() {
        var series = ChartSeries.CreateChord(new ChartSeriesId("package-transfers"), "Transfers",
            new[] { new ChartNode("a", "Support"), new ChartNode("b", "Support"), new ChartNode("idle", "Standby") },
            new[] { new ChartFlowLink("ab", "a", "b", 7), new ChartFlowLink("parallel", "a", "b", 2), new ChartFlowLink("self", "a", "a", 1), new ChartFlowLink("zero", "b", "a", 0) });
        var chart = Chart.Create().AddSeries(series).WithSize(390, 360).WithLegend(false).ConfigureChord(options => {
            options.Ordering = new ChartChordOrdering(new[] { "b", "a", "idle" }, new[] { "parallel", "ab", "self", "zero" }, new[] { "self", "zero", "ab", "parallel" });
            options.RibbonTension = 1;
        });
        var visual = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(390, 360))));
        var svg = visual.ToSvg("packed-chord"); var png = visual.ToPng(); var xml = XDocument.Parse(svg);
        var flows = xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "chord-link").ToArray();
        PackageAssertions.Require(flows.Select(node => (string?)node.Attribute("data-cfx-target-id")).SequenceEqual(new[] { "ab", "parallel", "self", "zero" })
            && flows.All(node => node.Attribute("data-cfx-point") == null) && (string?)flows[3].Attribute("data-cfx-value") == "0",
            "Packed Chord display order rewrote source identities, zero facts or semantic traversal.");
        chart.ConfigureChord(options => { options.Ordering = null; options.RibbonTension = 0; });
        series.ReplaceFlowData(Array.Empty<ChartNode>(), Array.Empty<ChartFlowLink>());
        PackageAssertions.Require(visual.ToSvg("packed-chord") == svg && visual.ToPng().SequenceEqual(png),
            "Packed Chord source/options edits changed detached geometry or native exports.");
        PackageAssertions.Png(png);
        Console.WriteLine("Packed Chord ordering, tension, identities and detached exports passed.");
    }
}
