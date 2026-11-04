using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal enum CartesianXMotion { Fixed, XTrack }

internal readonly struct CartesianXInterval {
    internal CartesianXInterval(double minimum, double maximum, string reason, CartesianXSourceAdmission? sourceX = null) {
        Minimum = minimum; Maximum = maximum; Reason = reason; SourceX = sourceX;
    }
    internal double Minimum { get; }
    internal double Maximum { get; }
    internal string Reason { get; }
    internal CartesianXSourceAdmission? SourceX { get; }
}

/// <summary>Native caption paint with raw source admission and prepared horizontal-clearance intervals.</summary>
internal sealed class CartesianXCaption {
    internal CartesianXCaption(string id, string text, ChartRect bounds, CartesianXSourceAdmission? sourceX, string? fittingReason, string? associatedId = null) {
        Id = id; Text = text; Bounds = bounds; SourceX = sourceX; FittingReason = fittingReason; AssociatedId = associatedId;
    }
    internal string Id { get; }
    internal string Text { get; }
    internal string? AssociatedId { get; }
    internal ChartRect Bounds { get; }
    internal CartesianXSourceAdmission? SourceX { get; }
    internal string? FittingReason { get; }
    internal double MinimumDelta { get; set; } = double.NegativeInfinity;
    internal double MaximumDelta { get; set; } = double.PositiveInfinity;
    internal IReadOnlyList<CartesianXInterval> Hidden { get; private set; } = Array.Empty<CartesianXInterval>();
    internal void Seal(IEnumerable<CartesianXInterval> hidden) => Hidden = Array.AsReadOnly(hidden.OrderBy(item => item.Minimum).ToArray());
    internal string? HiddenReason(double delta, double minimum, double maximum) => FittingReason != null && FittingReason != "ellipsized" ? FittingReason
        : SourceX.HasValue && !SourceX.Value.Allows(minimum, maximum) ? SourceX.Value.HiddenReason
        : delta < MinimumDelta || delta > MaximumDelta ? "caption-outside-plot"
        : Hidden.FirstOrDefault(interval => delta >= interval.Minimum && delta <= interval.Maximum
            && (!interval.SourceX.HasValue || interval.SourceX.Value.Allows(minimum, maximum))).Reason;
}

/// <summary>Balanced native display-list fragment. Its clip belongs to the fixed output coordinate space.</summary>
internal sealed class PreparedCartesianXLayer {
    internal PreparedCartesianXLayer(CartesianXMotion motion, ChartRect? clip, VisualScene scene, CartesianXCaption? caption = null) {
        Motion = motion; Clip = clip; Scene = scene; Caption = caption;
    }
    internal CartesianXMotion Motion { get; }
    internal ChartRect? Clip { get; }
    internal VisualScene Scene { get; }
    internal CartesianXCaption? Caption { get; }
}

internal sealed class PreparedCartesianXLevel {
    internal PreparedCartesianXLevel(VisualSize size, CartesianXZoomCoordinates coordinates, IReadOnlyList<PreparedCartesianXLayer> layers, IReadOnlyList<double> ticks) {
        Size = size; Coordinates = coordinates; Layers = Array.AsReadOnly(layers.ToArray()); Ticks = Array.AsReadOnly(ticks.ToArray());
        Statistics = new CartesianXPreparationStatistics(Layers, Ticks.Count);
    }
    internal VisualSize Size { get; }
    internal CartesianXZoomCoordinates Coordinates { get; }
    internal double Travel => Coordinates.Travel;
    internal IReadOnlyList<PreparedCartesianXLayer> Layers { get; }
    internal IReadOnlyList<double> Ticks { get; }
    internal CartesianXPreparationStatistics Statistics { get; }

    internal VisualScene Compose(double delta, double minimum, double maximum) {
        var nodes = new List<VisualSceneNode>(); var diagnostics = new List<VisualDiagnostic>(); var regions = new List<VisualSemanticRegion>();
        foreach (var layer in Layers) {
            var offset = layer.Motion == CartesianXMotion.XTrack ? delta : 0;
            var reason = layer.Caption?.HiddenReason(delta, minimum, maximum);
            if (layer.Caption is CartesianXCaption caption) {
                nodes.Add(new VisualSceneGroup("x-zoom-caption", caption.Id + "-visibility", null, new Dictionary<string, string> {
                    ["data-cfx-label"] = caption.Text, ["data-cfx-label-visibility"] = reason ?? "visible",
                    ["data-cfx-label-fitting"] = caption.FittingReason ?? "full", ["data-cfx-label-for"] = caption.AssociatedId ?? string.Empty,
                    ["aria-label"] = caption.Text
                }));
            }
            if (reason == null) {
                if (layer.Clip.HasValue) nodes.Add(new VisualSceneGroup(null, null, layer.Clip, null));
                if (offset != 0) nodes.Add(new VisualSceneGroup(null, null, null, null, translation: new ChartPoint(offset, 0)));
                CartesianXSourceAdmission.AppendPaint(nodes, layer.Scene.Nodes, minimum, maximum);
                if (offset != 0) nodes.Add(new VisualSceneEndGroup());
                if (layer.Clip.HasValue) nodes.Add(new VisualSceneEndGroup());
            }
            if (layer.Caption != null) nodes.Add(new VisualSceneEndGroup());
            diagnostics.AddRange(layer.Scene.Diagnostics);
            foreach (var region in layer.Scene.Regions) {
                var b = region.Bounds;
                var translated = new ChartRect(b.Left + offset, b.Top, b.Width, b.Height);
                // Extents of passive surfaces describe their visible intersection; point facts retain full source bounds.
                if (layer.Clip.HasValue && region.Role is "plot-region" or "annotation") translated = Intersection(translated, layer.Clip.Value);
                regions.Add(new VisualSemanticRegion(region.Id, region.Role, translated, region.Label));
            }
            if (reason != null) diagnostics.Add(new VisualDiagnostic("cartesian.x-zoom-caption-hidden",
                layer.Caption!.Id + ": " + reason + "."));
        }
        return new VisualScene(Size, nodes, diagnostics, regions);
    }

    private static ChartRect Intersection(ChartRect a, ChartRect b) => new(Math.Max(b.Left, Math.Min(b.Right, a.Left)), Math.Max(b.Top, Math.Min(b.Bottom, a.Top)),
        Math.Max(0, Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left)), Math.Max(0, Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top)));
}

/// <summary>Exact retained native payload counts; serialization is measured lazily, never exposed as product API.</summary>
internal sealed class CartesianXPreparationStatistics {
    private readonly Lazy<long> _svgBytes;
    internal CartesianXPreparationStatistics(IReadOnlyList<PreparedCartesianXLayer> layers, int ticks) {
        Nodes = layers.Sum(layer => (long)layer.Scene.Nodes.Count);
        PathCommands = layers.SelectMany(layer => layer.Scene.Nodes).OfType<VisualScenePath>().Sum(path => (long)path.Commands.Count);
        Glyphs = layers.SelectMany(layer => layer.Scene.Nodes).OfType<VisualSceneText>().Sum(text => text.Text.Lines.Sum(line => (long)line.Glyphs.Count));
        Ticks = ticks; VisibilityIntervals = layers.Sum(layer => (long)(layer.Caption?.Hidden.Count ?? 0)
            + (layer.Caption?.MinimumDelta > double.NegativeInfinity ? 1 : 0) + (layer.Caption?.SourceX.HasValue == true ? 1 : 0));
        _svgBytes = new Lazy<long>(() => layers.Sum(layer => (long)Encoding.UTF8.GetByteCount(new PreparedVisual(layer.Scene).ToSvg("zoomLayer"))));
    }
    internal long Nodes { get; }
    internal long PathCommands { get; }
    internal long Glyphs { get; }
    internal long Ticks { get; }
    /// <summary>Counts caption source/rectangle admission ranges and prepared native-clearance intervals.</summary>
    internal long VisibilityIntervals { get; }
    /// <summary>UTF8 of complete retained layer SVG documents, including scope groups; source admission data stays typed.</summary>
    internal long NativeSvgUtf8Bytes => _svgBytes.Value;
}
