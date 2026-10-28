using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    internal TopologyPreparedNodeBasis? PreparedNodeBasis { get; set; }
}

/// <summary>One immutable captured unit basis for qualified native figures, external glyphs and captions.</summary>
internal sealed partial class TopologyPreparedNodeBasis {
    internal TopologyPreparedNodeBasis(double factor, double width, double height, TopologyNodeShape? shape,
        TopologyNodeDisplayMode displayMode, TopologyNodeKind kind, TopologyIconShape? iconShape, string? iconId, string? symbol,
        double glyphAnchorX, double glyphAnchorY, ChartRect originalBody, double? contentPadding = null, double? maximumTextWidth = null, double? cornerRadius = null, double? glyphScale = null, TopologyPreparedNodeProjection? projection = null) {
        Factor = factor; Width = width; Height = height; Shape = shape; DisplayMode = displayMode; Kind = kind;
        IconShape = iconShape; IconId = iconId; Symbol = symbol; GlyphAnchor = new ChartPoint(glyphAnchorX, glyphAnchorY);
        OriginalBody = originalBody; ContentPadding = contentPadding; MaximumTextWidth = maximumTextWidth;
        CornerRadius = cornerRadius; GlyphScale = glyphScale; Projection = projection;
        Validate();
    }
    internal double Factor { get; }
    internal double Width { get; }
    internal double Height { get; }
    internal TopologyNodeShape? Shape { get; }
    internal TopologyNodeDisplayMode DisplayMode { get; }
    internal TopologyNodeKind Kind { get; }
    internal TopologyIconShape? IconShape { get; }
    internal string? IconId { get; }
    internal string? Symbol { get; }
    internal ChartPoint GlyphAnchor { get; }
    internal ChartRect OriginalBody { get; }
    internal double? ContentPadding { get; }
    internal double? MaximumTextWidth { get; }
    internal double? CornerRadius { get; }
    internal double? GlyphScale { get; }
    internal TopologyPreparedNodeProjection? Projection { get; }

    internal bool Matches(TopologyNode node) => node.Width == Width && node.Height == Height && node.Shape == Shape
        && (node.DisplayMode ?? TopologyNodeDisplayMode.Card) == DisplayMode && node.Kind == Kind && SameIconPolicy(node.IconShape, IconShape)
        && string.Equals(node.IconId, IconId, StringComparison.Ordinal) && string.Equals(node.Symbol, Symbol, StringComparison.Ordinal)
        && node.ContentPadding == ContentPadding && node.MaximumTextWidth == MaximumTextWidth
        && node.CornerRadius == CornerRadius && node.GlyphScale == GlyphScale;
    internal static bool SameIconPolicy(TopologyIconShape? a, TopologyIconShape? b) => a == b
        || !TopologyRenderPrimitives.HasExplicitIconShape(a) && !TopologyRenderPrimitives.HasExplicitIconShape(b);
    internal static TopologyPreparedNodeBasis? Resolve(TopologyNode node) => node.PreparedNodeBasis is {} basis && basis.Matches(node) ? basis : null;
    internal static double Units(TopologyNode node) => Resolve(node)?.Factor ?? 1;
    internal static TopologyPreparedNodeBasis? ForPresentation(TopologyNode node) => Resolve(node) is {} basis && basis.UsesOriginalPresentation ? basis : null;
    internal static bool Recognized(TopologyNodeShape? shape, TopologyNodeDisplayMode mode) => Enum.IsDefined(typeof(TopologyNodeDisplayMode), mode)
        && (IsActor(shape) || mode is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Pill or TopologyNodeDisplayMode.Dot or TopologyNodeDisplayMode.Artwork);
    internal static bool Supported(TopologyNodeShape? shape, TopologyNodeDisplayMode mode) =>
        IsActor(shape) && mode is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard
        || mode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Artwork
        || !IsActor(shape) && mode is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Pill or TopologyNodeDisplayMode.Dot;
    internal static bool IsActor(TopologyNodeShape? shape) => shape is TopologyNodeShape.Actor or TopologyNodeShape.ActorHollow
        or TopologyNodeShape.ActorBusiness or TopologyNodeShape.ActorBusinessHollow or TopologyNodeShape.ActorSilhouette or TopologyNodeShape.ActorIcon;

    internal bool UsesOriginalPresentation => DisplayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Artwork
        || !IsActor(Shape) && DisplayMode is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Pill or TopologyNodeDisplayMode.Dot;

    // Re-evaluate the existing surface owner at the exact original body, retaining its authored path quantization.
    // The clone is one node, not a parallel raw graph, and cannot retain another derived basis.
    internal TopologyNode OriginalNode(TopologyNode node) {
        var raw = TopologyLayoutEngine.Clone(node); raw.PreparedNodeBasis = null;
        raw.X = OriginalBody.X; raw.Y = OriginalBody.Y; raw.Width = OriginalBody.Width; raw.Height = OriginalBody.Height;
        // These authored role sizes were already projected into portable output units by SemanticSnapshot.
        // Existing raw embedded text-run facts and dimensionless glyph policy keep their own native units.
        UnprojectStyle(raw.TextStyle);
        UnprojectStyle(raw.SubtitleTextStyle);
        UnprojectStyle(raw.CaptionTextStyle);
        if (raw.CornerRadius.HasValue) raw.CornerRadius /= Factor;
        if (raw.CaptionGap.HasValue) raw.CaptionGap /= Factor;
        if (raw.StrokeWidth.HasValue) raw.StrokeWidth /= Factor;
        for (var index = 0; index < raw.DashPattern.Count; index++) raw.DashPattern[index] /= Factor;
        foreach (var detail in raw.Details) {
            UnprojectStyle(detail.TextStyle);
            if (detail.BadgeStyle != null) detail.BadgeStyle = detail.BadgeStyle.Scale(1 / Factor);
        }
        if (raw.TableContent != null) raw.TableContent.CapturedPresentationScale /= Factor;
        return raw;
    }
    // SemanticSnapshot stores both authored text lengths in output units. Replay
    // normalizes each once before the existing logical measurement and painter.
    private void UnprojectStyle(TextStyleOverride? style) {
        if (style?.FontSize is double size) style.FontSize = size / Factor;
        if (style?.LetterSpacing is double spacing) style.LetterSpacing = spacing / Factor;
    }
    internal ChartPoint Project(TopologyNode node, ChartPoint point) {
        var origin = Projection?.OffsetFor(node);
        var x = origin.HasValue ? origin.Value.X + point.X * Factor : node.X + (point.X - OriginalBody.X) * Factor;
        var y = origin.HasValue ? origin.Value.Y + point.Y * Factor : node.Y + (point.Y - OriginalBody.Y) * Factor;
        Bounded(x); Bounded(y); return new ChartPoint(x, y, point.BreakBefore).WithColorValue(point.ColorValue);
    }
    internal ChartPoint Unproject(TopologyNode node, ChartPoint point) {
        var origin = Projection?.OffsetFor(node);
        var x = origin.HasValue ? (point.X - origin.Value.X) / Factor : OriginalBody.X + (point.X - node.X) / Factor;
        var y = origin.HasValue ? (point.Y - origin.Value.Y) / Factor : OriginalBody.Y + (point.Y - node.Y) / Factor;
        // A remote toward-point is a direction input, not an authored raw coordinate. It may exceed the raw body domain,
        // but must remain finite for the incumbent intersection owner. Published basis/output coordinates stay bounded.
        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
            throw new ArgumentOutOfRangeException(nameof(point), "Captured node projection cannot retain a finite attachment direction.");
        return new ChartPoint(x, y);
    }
    internal ChartRect Project(TopologyNode node, ChartRect rect) {
        var point = Project(node, new ChartPoint(rect.X, rect.Y)); return new ChartRect(point.X, point.Y, rect.Width * Factor, rect.Height * Factor);
    }
    internal void ValidateNode(TopologyNode node) => ValidateDerived(node.X, node.Y, node.ContentPadding, node.MaximumTextWidth,
        node.TextStyle?.FontSize, node.SubtitleTextStyle?.FontSize);
    internal void ValidateDerived(double x, double y, double? padding, double? textWidth, double? titleSize, double? subtitleSize) {
        Bounded(x); Bounded(y); Bounded(x + Width); Bounded(y + Height);
        Projection?.ValidateAt(x, y, OriginalBody, Factor);
        Bounded(x - 76 * Factor); Bounded(y + 128 * Factor);
        if (padding.HasValue) Bounded(padding.Value * Factor);
        if (textWidth.HasValue) Bounded(textWidth.Value * Factor);
        if (titleSize.HasValue) Bounded(titleSize.Value);
        if (subtitleSize.HasValue) Bounded(subtitleSize.Value);
    }
    internal static void ApplyLifecycle(TopologyChart chart) {
        foreach (var node in chart.Nodes) {
            if (chart.LayoutMode != TopologyLayoutMode.Manual || !chart.UsesPreparedSceneCoordinates || Resolve(node) == null) node.PreparedNodeBasis = null;
            else node.PreparedNodeBasis!.ValidateNode(node);
        }
    }
    internal void Validate() {
        Positive(Factor); Positive(Width); Positive(Height); Bounded(GlyphAnchor.X); Bounded(GlyphAnchor.Y);
        Bounded(OriginalBody.X); Bounded(OriginalBody.Y); Positive(OriginalBody.Width); Positive(OriginalBody.Height);
        Bounded(OriginalBody.Right); Bounded(OriginalBody.Bottom); Positive(76 * Factor);
        Positive(OriginalBody.Width * Factor); Positive(OriginalBody.Height * Factor);
        DerivedSpan(Width, OriginalBody.Width); DerivedSpan(Height, OriginalBody.Height);
        Projection?.Validate(OriginalBody, Factor, Width, Height);
        if (ContentPadding.HasValue) { Bounded(ContentPadding.Value * Factor); if (ContentPadding.Value < 0) throw new ArgumentOutOfRangeException(nameof(ContentPadding)); }
        if (MaximumTextWidth.HasValue) Positive(MaximumTextWidth.Value * Factor);
        // Radius is a finite clamped policy, not an unclamped geometry extent. The canonical surface owner clamps it to the body.
        if (CornerRadius.HasValue && (double.IsNaN(CornerRadius.Value) || double.IsInfinity(CornerRadius.Value) || CornerRadius.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(CornerRadius));
        if (GlyphScale.HasValue) Positive(GlyphScale.Value);
        Bounded(GlyphAnchor.X - 76); Bounded(GlyphAnchor.X + 76); Bounded(GlyphAnchor.Y - 76); Bounded(GlyphAnchor.Y + 76);
        if (!Recognized(Shape, DisplayMode) || Shape.HasValue && !Enum.IsDefined(typeof(TopologyNodeShape), Shape.Value)
            || !Enum.IsDefined(typeof(TopologyNodeKind), Kind) || IconShape.HasValue && !Enum.IsDefined(typeof(TopologyIconShape), IconShape.Value))
            throw new NotSupportedException("Prepared node units require a defined qualified native shape and presentation policy.");
    }
    private void DerivedSpan(double captured, double original) {
        var expected = original * Factor;
        // Repeated IEEE-754 projection may reassociate a bounded multiply. The current body/policy guard stays exact.
        const double machineEpsilon = 2.2204460492503131e-16;
        if (Math.Abs(captured - expected) > 16 * machineEpsilon * Math.Max(1, Math.Abs(captured)))
            throw new ArgumentException("Captured node body dimensions must agree with their original uniform unit basis.");
    }
    private static void Positive(double value) { Bounded(value); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Prepared node units must be positive."); }
    private static void Bounded(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > TopologyPathGeometry.MaximumCoordinate)
            throw new ArgumentOutOfRangeException(nameof(value), "Prepared node coordinates and derived extents must be finite within one billion units.");
    }
}
