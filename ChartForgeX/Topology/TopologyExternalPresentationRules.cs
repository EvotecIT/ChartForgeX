using System;

namespace ChartForgeX.Topology;

/// <summary>Owns the finite native and portable external-presentation admission contract.</summary>
internal static class TopologyExternalPresentationRules {
    internal static string? Error(TopologyNodeDisplayMode mode, TopologyNodeShape? shape, bool table,
        TopologyLayoutMode layout, TopologyCaptionPlacement? placement, TopologyCaptionSurface? surface,
        double? gap, double? glyphScale, double? radius, bool captionTextStyle = false) {
        if (placement.HasValue && !Enum.IsDefined(typeof(TopologyCaptionPlacement), placement.Value)
            || surface.HasValue && !Enum.IsDefined(typeof(TopologyCaptionSurface), surface.Value)) return "has an undefined external caption policy";
        if (InvalidNonNegative(gap) || InvalidNonNegative(radius) || glyphScale.HasValue && (!Finite(glyphScale.Value) || glyphScale.Value <= 0))
            return "requires finite nonnegative caption gap/corner radius and finite positive glyph scale";
        if (TopologyPreparedNodeBasis.IsActor(shape) && (placement.HasValue || surface.HasValue || gap.HasValue || captionTextStyle || glyphScale.HasValue))
            return "external caption and glyph policies do not apply to Actor shapes";
        if (placement.HasValue || surface.HasValue || gap.HasValue || captionTextStyle) {
            if (table || mode is not (TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Artwork))
                return "external caption policy requires Icon, Tile or Artwork presentation without an embedded table";
            if ((placement == TopologyCaptionPlacement.Top || gap.HasValue) && layout is not (TopologyLayoutMode.Manual or TopologyLayoutMode.Layered))
                return "explicit Top captions and caption gaps currently require Manual or Layered layout";
        }
        if (glyphScale.HasValue && (table || mode is not (TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile)))
            return "glyph scale currently requires external Icon or Tile presentation without an embedded table";
        if (radius.HasValue && (table || mode is TopologyNodeDisplayMode.Dot or TopologyNodeDisplayMode.Hidden
            || shape.HasValue && shape is not (TopologyNodeShape.Rectangle or TopologyNodeShape.Rounded)
            || !shape.HasValue && mode is not (TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Artwork)))
            return "corner radius requires Rectangle, Rounded or an unshaped card/artwork surface";
        return null;
    }
    internal static bool HasPolicy(TopologyNode node) => TopologyExternalCaption.HasPolicy(node) || node.GlyphScale.HasValue || node.CornerRadius.HasValue;
    private static bool InvalidNonNegative(double? value) => value.HasValue && (!Finite(value.Value) || value.Value < 0);
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
