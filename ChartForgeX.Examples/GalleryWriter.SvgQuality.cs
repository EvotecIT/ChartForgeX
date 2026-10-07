using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using SvgRasterViewport = ChartForgeX.SvgRaster.SvgRasterRenderer.SvgRasterViewport;

/// <summary>SVG-specific artifact quality helpers for generated examples.</summary>
public static partial class GalleryWriter {
    private static SvgTextQuality ReadSvgTextQuality(string svg, AssetDimensions dimensions) {
        if (dimensions.Width <= 0 || dimensions.Height <= 0) return default;
        var document = SvgRasterParser.ParseDocument(svg);
        var coordinates = document.ViewBox;
        var definitions = SvgRasterDefinitions.From(document);
        var clipped = 0;
        var nearEdge = 0;
        var tinyText = 0;
        var minimumFontSize = double.PositiveInfinity;
        var strokedNodes = 0;
        var tinyStroke = 0;
        var minimumStrokeWidth = double.PositiveInfinity;
        var markerNodes = 0;
        var tinyMarker = 0;
        var minimumMarkerRadius = double.PositiveInfinity;
        var ancestors = new List<SvgRasterElement>();
        Inspect(document.Root, SvgRasterStyle.Default, SvgRasterMatrix.Identity, new SvgRasterViewport(coordinates.Width, coordinates.Height));

        return new SvgTextQuality(double.IsInfinity(minimumFontSize) ? 0 : minimumFontSize, tinyText, strokedNodes,
            double.IsInfinity(minimumStrokeWidth) ? 0 : minimumStrokeWidth, tinyStroke, markerNodes,
            double.IsInfinity(minimumMarkerRadius) ? 0 : minimumMarkerRadius, tinyMarker, clipped, nearEdge);

        void Inspect(SvgRasterElement element, SvgRasterStyle parent, SvgRasterMatrix parentMatrix, SvgRasterViewport viewport) {
            if (element.Name is "defs" or "style" or "title" or "desc") return;
            var style = SvgRasterStyle.Resolve(parent, element, definitions.StyleSheet, ancestors);
            if (!style.Displayed || !style.VisibilityVisible || style.Opacity == 0) return;
            var matrix = parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(element.Get("transform")));
            if (element.Name == "svg" && ancestors.Count != 0) {
                var nested = SvgRasterRenderer.ResolveNestedSvgViewport(element, viewport);
                matrix = SvgRasterRenderer.ApplyNestedSvgViewport(element, matrix, nested);
                viewport = nested.UserViewport;
            }
            // Keep the gallery's explicit-size readability contract; use computed styles for visibility.
            // Counting inherited group strokes or CSS-only text changes that contract independently of placement.
            if (element.Get("stroke") is { } stroke && stroke != "none" && stroke != "transparent"
                && !IsSvgDecorativeStroke(element, ancestors)
                && !style.Stroke.IsNone && style.StrokeWidth > 0 && style.StrokeOpacity > 0 && (style.Stroke.Color?.A ?? 255) > 0) {
                strokedNodes++;
                minimumStrokeWidth = Math.Min(minimumStrokeWidth, style.StrokeWidth);
                if (style.StrokeWidth < MinimumReadableSvgStrokeWidth) tinyStroke++;
            }
            if (IsSvgDataMarker(element) && Number(element, "r") is var radius && radius > 0) {
                markerNodes++;
                minimumMarkerRadius = Math.Min(minimumMarkerRadius, radius);
                if (radius < MinimumReadableSvgMarkerRadius) tinyMarker++;
            }
            if (element.Name == "text" && !IsSvgLegendGlyphText(element, ancestors)) {
                if (double.TryParse(element.Get("font-size"), NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSize) && fontSize > 0) {
                    minimumFontSize = Math.Min(minimumFontSize, fontSize);
                    if (fontSize < MinimumReadableSvgTextFontSize) tinyText++;
                }
                var point = matrix.Transform(new ChartPoint(Number(element, "x"), Number(element, "y")));
                if (point.X < coordinates.X || point.Y < coordinates.Y || point.X > coordinates.X + coordinates.Width || point.Y > coordinates.Y + coordinates.Height) clipped++;
                else if (point.X <= coordinates.X + SvgTextCanvasMargin || point.Y <= coordinates.Y + SvgTextCanvasMargin
                    || point.X >= coordinates.X + coordinates.Width - SvgTextCanvasMargin || point.Y >= coordinates.Y + coordinates.Height - SvgTextCanvasMargin) nearEdge++;
            }
            ancestors.Add(element);
            foreach (var child in element.Children) Inspect(child, style, matrix, viewport);
            ancestors.RemoveAt(ancestors.Count - 1);
        }
        static double Number(SvgRasterElement element, string name) =>
            double.TryParse(element.Get(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static bool IsSvgDecorativeStroke(SvgRasterElement element, List<SvgRasterElement> ancestors) {
        // Context geography, grids and icon artwork may deliberately use hairlines. Node outlines,
        // relationship routes, arrowheads and data markers still carry information at their actual size.
        var role = element.Get("data-cfx-role");
        return role is "topology-map-boundary" or "dotted-map-boundary" or "topology-grid"
            or "topology-icon-surface" or "topology-node-icon"
            || string.IsNullOrEmpty(role) && ancestors.Any(parent => parent.Get("data-cfx-role") == "topology-node-icon");
    }

    private static bool IsSvgLegendGlyphText(SvgRasterElement element, List<SvgRasterElement> ancestors) {
        // A legend icon's miniature lettering is artwork beside a separately measured legend label.
        // Authored node symbols, node captions and legend labels themselves retain the text-size gate.
        return string.IsNullOrEmpty(element.Get("data-cfx-role"))
            && ancestors.Any(parent => parent.Get("data-cfx-role") == "topology-node-icon")
            && ancestors.Any(parent => parent.Get("data-cfx-role") == "topology-legend-item");
    }

    private static bool IsSvgDataMarker(SvgRasterElement element) {
        if (element.Name != "circle") return false;
        var role = element.Get("data-cfx-role") ?? "";
        return role.Contains("marker", StringComparison.OrdinalIgnoreCase) || role.Contains("point", StringComparison.OrdinalIgnoreCase)
            || role == "bubble" || role.StartsWith("dumbbell-", StringComparison.OrdinalIgnoreCase) || role.StartsWith("slope-", StringComparison.OrdinalIgnoreCase);
    }

    private readonly struct SvgTextQuality {
        public SvgTextQuality(double minimumFontSize, int tinyTextNodes, int strokedNodes, double minimumStrokeWidth, int tinyStrokeNodes,
            int markerNodes, double minimumMarkerRadius, int tinyMarkerNodes, int clippedTextNodes, int nearEdgeTextNodes) {
            MinimumFontSize = minimumFontSize;
            TinyTextNodes = tinyTextNodes;
            StrokedNodes = strokedNodes;
            MinimumStrokeWidth = minimumStrokeWidth;
            TinyStrokeNodes = tinyStrokeNodes;
            MarkerNodes = markerNodes;
            MinimumMarkerRadius = minimumMarkerRadius;
            TinyMarkerNodes = tinyMarkerNodes;
            ClippedTextNodes = clippedTextNodes;
            NearEdgeTextNodes = nearEdgeTextNodes;
        }
        public double MinimumFontSize { get; }
        public int TinyTextNodes { get; }
        public int StrokedNodes { get; }
        public double MinimumStrokeWidth { get; }
        public int TinyStrokeNodes { get; }
        public int MarkerNodes { get; }
        public double MinimumMarkerRadius { get; }
        public int TinyMarkerNodes { get; }
        public int ClippedTextNodes { get; }
        public int NearEdgeTextNodes { get; }
    }
}
