using System;
using System.Linq;
using ChartForgeX.Typography;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartLabelScene {
    /// <summary>Re-measures the emitted scene without placing it, for gallery collision diagnostics.</summary>
    internal static LabelOverlapReport Inspect(string svg, FontSpec font) {
        var document = SvgMarkupParser.Parse(svg);
        var dropped = document.Descendants().Count(e => e.Attribute("data-cfx-label-status") == "dropped");
        var scene = new ChartLabelScene(document, font, false);
        var labelPairs = 0; var markPairs = 0; var contained = 0; var details = new System.Text.StringBuilder();
        for (var i = 0; i < scene._labels.Count; i++) {
            var label = scene._labels[i];
            for (var j = i + 1; j < scene._labels.Count; j++) if (Overlaps(label.Box, scene._labels[j].Box)) {
                labelPairs++;
                details.Append(LabelRole(label.Element)).Append(':').Append(label.Text).Append(" -> ")
                    .Append(LabelRole(scene._labels[j].Element)).Append(':').Append(scene._labels[j].Text).Append("; ");
            }
            var associated = scene.AssociatedMark(label);
            foreach (var mark in scene._marks) {
                if (!Overlaps(label.Box, mark.Shape.Bounds) || !mark.Shape.Intersects(label.Box)) continue;
                // Native edge captions deliberately cover their own route with a backplate. Foreign routes remain obstacles.
                if (mark == associated && LabelRole(label.Element).StartsWith("topology-edge", StringComparison.Ordinal)) continue;
                if (mark == associated && mark.Shape.Contains(label.Box)) contained++;
                else { markPairs++; details.Append(Role(label.Element)).Append(':').Append(label.Text).Append(" -> ").Append(mark.Kind).Append(' ').Append(mark.Id).Append("; "); }
            }
        }
        return new LabelOverlapReport(scene._labels.Count, dropped, labelPairs, markPairs, contained, details.ToString());
    }
    // SVG coordinates are serialized to three decimals. Adjacent measured lines can overlap by less than
    // one serialized unit when independently rounded baselines are reconstructed into text boxes.
    private static bool Overlaps(ChartRect a, ChartRect b) => Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left) > .001
        && Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top) > .001;
}

/// <summary>Counts actual emitted text boxes and precise mark intersections; intentional inside-mark labels are reported separately.</summary>
internal readonly struct LabelOverlapReport {
    internal LabelOverlapReport(int visible, int dropped, int labels, int marks, int contained, string details) { Visible = visible; Dropped = dropped; LabelLabel = labels; LabelMark = marks; Contained = contained; Details = details; }
    internal string Details { get; }
    internal int Visible { get; }
    internal int Dropped { get; }
    internal int LabelLabel { get; }
    internal int LabelMark { get; }
    internal int Contained { get; }
}
