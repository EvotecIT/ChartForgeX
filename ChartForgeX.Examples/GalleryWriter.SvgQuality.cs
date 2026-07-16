using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// SVG-specific artifact quality helpers for generated examples.
/// </summary>
public static partial class GalleryWriter {
    private static SvgTextQuality ReadSvgTextQuality(string svg, AssetDimensions dimensions) {
        var coordinates = ReadSvgCoordinateBounds(svg, dimensions);
        if (coordinates.Width <= 0 || coordinates.Height <= 0) return default;

        var stack = new List<SvgTranslate> { default };
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
        foreach (Match match in Regex.Matches(svg, "<(/?)([a-zA-Z][\\w:-]*)([^>]*)>", RegexOptions.Singleline)) {
            var closing = match.Groups[1].Value.Length > 0;
            var tagName = match.Groups[2].Value;
            var attrs = match.Groups[3].Value;
            if (closing) {
                if (string.Equals(tagName, "g", StringComparison.OrdinalIgnoreCase) && stack.Count > 1) stack.RemoveAt(stack.Count - 1);
                continue;
            }

            var selfClosing = attrs.TrimEnd().EndsWith("/", StringComparison.Ordinal);
            if (string.Equals(tagName, "g", StringComparison.OrdinalIgnoreCase)) {
                var parent = stack[stack.Count - 1];
                var local = ReadSvgTranslate(attrs);
                stack.Add(new SvgTranslate(parent.X + local.X, parent.Y + local.Y));
                if (selfClosing && stack.Count > 1) stack.RemoveAt(stack.Count - 1);
                continue;
            }

            if (HasVisibleSvgStroke(attrs)) {
                var strokeWidth = TryReadSvgDoubleAttribute(attrs, "stroke-width", out var explicitStrokeWidth) && explicitStrokeWidth > 0 ? explicitStrokeWidth : 1;
                strokedNodes++;
                minimumStrokeWidth = Math.Min(minimumStrokeWidth, strokeWidth);
                if (strokeWidth < MinimumReadableSvgStrokeWidth) tinyStroke++;
            }

            if (IsSvgDataMarker(tagName, attrs) && TryReadSvgDoubleAttribute(attrs, "r", out var radius) && radius > 0) {
                markerNodes++;
                minimumMarkerRadius = Math.Min(minimumMarkerRadius, radius);
                if (radius < MinimumReadableSvgMarkerRadius) tinyMarker++;
            }

            if (!string.Equals(tagName, "text", StringComparison.OrdinalIgnoreCase)) continue;
            if (TryReadSvgDoubleAttribute(attrs, "font-size", out var fontSize) && fontSize > 0) {
                minimumFontSize = Math.Min(minimumFontSize, fontSize);
                if (fontSize < MinimumReadableSvgTextFontSize) tinyText++;
            }

            if (!TryReadSvgDoubleAttribute(attrs, "x", out var x) || !TryReadSvgDoubleAttribute(attrs, "y", out var y)) continue;
            var translate = stack[stack.Count - 1];
            x += translate.X;
            y += translate.Y;
            if (x < coordinates.Left || y < coordinates.Top || x > coordinates.Right || y > coordinates.Bottom) clipped++;
            else if (x <= coordinates.Left + SvgTextCanvasMargin || y <= coordinates.Top + SvgTextCanvasMargin || x >= coordinates.Right - SvgTextCanvasMargin || y >= coordinates.Bottom - SvgTextCanvasMargin) nearEdge++;
        }

        return new SvgTextQuality(double.IsInfinity(minimumFontSize) ? 0 : minimumFontSize, tinyText, strokedNodes, double.IsInfinity(minimumStrokeWidth) ? 0 : minimumStrokeWidth, tinyStroke, markerNodes, double.IsInfinity(minimumMarkerRadius) ? 0 : minimumMarkerRadius, tinyMarker, clipped, nearEdge);
    }

    private static SvgCoordinateBounds ReadSvgCoordinateBounds(string svg, AssetDimensions fallback) {
        var root = Regex.Match(svg, "<svg\\b([^>]*)>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (root.Success) {
            var viewBox = ReadSvgRawAttribute(root.Groups[1].Value, "viewBox");
            var values = Regex.Matches(viewBox, "[-+]?(?:\\d+\\.?\\d*|\\.\\d+)(?:[eE][-+]?\\d+)?")
                .Cast<Match>()
                .Select(match => ParseSvgDouble(match.Value))
                .ToArray();
            if (values.Length == 4 && values[2] > 0 && values[3] > 0) return new SvgCoordinateBounds(values[0], values[1], values[2], values[3]);
        }

        return new SvgCoordinateBounds(0, 0, fallback.Width, fallback.Height);
    }

    private static SvgTranslate ReadSvgTranslate(string attrs) {
        var transform = ReadSvgRawAttribute(attrs, "transform");
        if (transform.Length == 0) return default;
        var match = Regex.Match(transform, "translate\\(([-0-9.]+)(?:[, ]+([-0-9.]+))?\\)", RegexOptions.IgnoreCase);
        if (!match.Success) return default;
        var x = ParseSvgDouble(match.Groups[1].Value);
        var y = match.Groups[2].Success ? ParseSvgDouble(match.Groups[2].Value) : 0;
        return new SvgTranslate(x, y);
    }

    private static bool TryReadSvgDoubleAttribute(string attrs, string name, out double value) {
        value = 0;
        var raw = ReadSvgRawAttribute(attrs, name);
        if (raw.Length == 0) return false;
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool HasVisibleSvgStroke(string attrs) {
        var stroke = ReadSvgRawAttribute(attrs, "stroke");
        if (stroke.Length == 0 || string.Equals(stroke, "none", StringComparison.OrdinalIgnoreCase) || string.Equals(stroke, "transparent", StringComparison.OrdinalIgnoreCase)) return false;
        if (TryReadSvgDoubleAttribute(attrs, "stroke-opacity", out var opacity) && opacity <= 0) return false;
        return true;
    }

    private static bool IsSvgDataMarker(string tagName, string attrs) {
        if (!string.Equals(tagName, "circle", StringComparison.OrdinalIgnoreCase)) return false;
        var role = ReadSvgRawAttribute(attrs, "data-cfx-role");
        return role.Contains("marker", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("point", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "bubble", StringComparison.OrdinalIgnoreCase) ||
            role.StartsWith("dumbbell-", StringComparison.OrdinalIgnoreCase) ||
            role.StartsWith("slope-", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadSvgRawAttribute(string attrs, string name) {
        var match = Regex.Match(attrs, "\\b" + Regex.Escape(name) + "=\"([^\"]*)\"", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static double ParseSvgDouble(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : 0;

    private readonly struct SvgTextQuality {
        public SvgTextQuality(double minimumFontSize, int tinyTextNodes, int strokedNodes, double minimumStrokeWidth, int tinyStrokeNodes, int markerNodes, double minimumMarkerRadius, int tinyMarkerNodes, int clippedTextNodes, int nearEdgeTextNodes) {
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

    private readonly struct SvgTranslate {
        public SvgTranslate(double x, double y) {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }
    }

    private readonly struct SvgCoordinateBounds {
        public SvgCoordinateBounds(double left, double top, double width, double height) {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        public double Left { get; }

        public double Top { get; }

        public double Width { get; }

        public double Height { get; }

        public double Right => Left + Width;

        public double Bottom => Top + Height;
    }
}
