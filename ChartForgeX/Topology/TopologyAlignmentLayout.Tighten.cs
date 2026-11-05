using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal sealed partial class TopologyAlignmentLayout {
    /// <summary>
    /// Shrinks each shell, innermost first, to its children plus the group padding and header. The longest-path
    /// solution keeps a shell's preferred size and its earliest origin, which leaves empty bands when an alignment
    /// pulls the children elsewhere. Shrinking inside the solved bounds cannot introduce a sibling overlap.
    /// </summary>
    private void Tighten(double[] horizontal, double[] vertical) {
        var ordered = groups.Values.OrderByDescending(Depth).ToArray();
        foreach (var item in ordered) {
            budget.Work();
            var group = item.Group!;
            if (group.Presentation == TopologyGroupPresentation.Compartment) continue;
            var children = items.Where(child => child.Parent == group.Id && child.Visible).ToArray();
            budget.Work(items.Count);
            if (children.Length == 0 || children.Any(child => child.Group?.Presentation == TopologyGroupPresentation.Compartment)) continue;
            double Left(Item child) => child.Group != null ? horizontal[child.Low] : horizontal[child.Low] + child.Left;
            double Right(Item child) => child.Group != null ? horizontal[child.High] : horizontal[child.Low] + child.Right;
            double Top(Item child) => child.Group != null ? vertical[child.Low] : vertical[child.Low] + child.Top;
            double Bottom(Item child) => child.Group != null ? vertical[child.High] : vertical[child.Low] + child.Bottom;
            var oldLeft = horizontal[item.Low]; var oldRight = horizontal[item.High];
            var oldTop = vertical[item.Low]; var oldBottom = vertical[item.High];
            var header = ParentInset(item);
            var left = Math.Max(oldLeft, children.Min(Left) - Padding);
            var right = Math.Min(oldRight, Math.Max(children.Max(Right) + Padding, left + MinimumShellWidth(group)));
            // A narrower shell can wrap its title onto more lines; keep the solved width when that would happen.
            group.X = left; group.Width = right - left;
            if (ParentInset(item) > header + 0.000001) { left = oldLeft; right = oldRight; group.X = left; group.Width = right - left; }
            horizontal[item.Low] = left; horizontal[item.High] = right;
            vertical[item.Low] = Math.Max(oldTop, children.Min(Top) - header);
            vertical[item.High] = Math.Min(oldBottom, children.Max(Bottom) + Padding);
            group.Y = vertical[item.Low]; group.Height = vertical[item.High] - group.Y;
        }

        int Depth(Item item) {
            var depth = 0;
            for (var parent = item.Parent; parent != null && depth <= groups.Count; parent = groups[parent].Parent) { budget.Work(); depth++; }
            return depth;
        }
    }

    private static void RepackTightened(TopologyChart chart, TopologyRenderOptions options) {
        var nodes = chart.Nodes.Select(node => (node.X, node.Y)).ToArray();
        var shells = chart.Groups.Select(group => (group.X, group.Y, group.Width, group.Height)).ToArray();
        try {
            new TopologyAlignmentLayout(chart, options).Solve();
        } catch (Exception exception) when (exception is TopologyValidationException or TopologyAlignmentLimitException) {
            // The tightened first solution remains valid; keep it when the repack cannot be completed.
            for (var i = 0; i < nodes.Length; i++) { chart.Nodes[i].X = nodes[i].X; chart.Nodes[i].Y = nodes[i].Y; }
            for (var i = 0; i < shells.Length; i++) {
                var group = chart.Groups[i];
                group.X = shells[i].X; group.Y = shells[i].Y; group.Width = shells[i].Width; group.Height = shells[i].Height;
            }
        }
    }

    // The header owns its title and icon; a shell never becomes narrower than that measured run.
    private double MinimumShellWidth(TopologyGroup group) {
        if (!group.ShowLabel || string.IsNullOrWhiteSpace(group.Label) || !TopologyGroupHeader.IsDrawn(options)) return 0;
        return TopologyRenderPrimitives.EstimateTextWidth(group.Label, 12, true, chart.TextMeasurement) + 48;
    }
}
