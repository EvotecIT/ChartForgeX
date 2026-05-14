using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static class SvgRasterRenderer {
    public static bool TryRenderFragment(string svgBody, string? viewBox, string? preserveAspectRatio, int width, int height, out byte[] rgba) {
        rgba = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(svgBody) || width <= 0 || height <= 0) return false;

        try {
            var document = SvgRasterParser.ParseFragment(svgBody, viewBox);
            var definitions = SvgRasterDefinitions.From(document);
            var canvas = new RgbaCanvas(width, height, 1);
            var matrix = SvgRasterMatrix.FromFit(document.ViewBox, width, height, preserveAspectRatio);
            var ancestors = new List<SvgRasterElement>();
            foreach (var child in document.Children) RenderElement(canvas, child, SvgRasterStyle.Default, matrix, definitions, width, height, 0, ancestors);
            rgba = canvas.Pixels;
            return HasVisiblePixel(rgba);
        } catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException || ex is ArgumentException || ex is System.Xml.XmlException) {
            return false;
        }
    }

    private static void RenderElement(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle parentStyle, SvgRasterMatrix parentMatrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors) {
        var style = SvgRasterStyle.Resolve(parentStyle, element, definitions.StyleSheet, ancestors);
        if (!style.Visible) return;

        var matrix = parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(element.Get("transform")));
        if (string.Equals(element.Name, "svg", StringComparison.Ordinal)) matrix = ApplyNestedSvgViewport(element, matrix);
        if (IsDefinitionElement(element.Name)) return;
        var hasClipPath = definitions.TryGetClipPath(ReferenceId(element, "clip-path"), out var clipPath);
        var hasMask = definitions.TryGetMask(ReferenceId(element, "mask"), out var maskDefinition);
        if (hasClipPath || hasMask) {
            var content = new RgbaCanvas(width, height, 1);
            RenderElementCore(content, element, style, matrix, definitions, width, height, referenceDepth, ancestors);
            if (hasClipPath) {
                var clippedContent = new RgbaCanvas(width, height, 1);
                var clipMask = new RgbaCanvas(width, height, 1);
                RenderClipPath(clipMask, clipPath, matrix, definitions);
                clippedContent.DrawImageMasked(0, 0, width, height, content.Pixels, clipMask.Pixels);
                content = clippedContent;
            }

            if (hasMask) {
                var mask = new RgbaCanvas(width, height, 1);
                RenderMask(mask, maskDefinition, matrix, definitions, width, height);
                canvas.DrawImageMasked(0, 0, width, height, content.Pixels, mask.Pixels);
            } else {
                canvas.DrawImage(0, 0, width, height, content.Pixels);
            }

            return;
        }

        RenderElementCore(canvas, element, style, matrix, definitions, width, height, referenceDepth, ancestors);
    }

    private static void RenderElementCore(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors) {
        switch (element.Name) {
            case "g":
            case "svg":
                break;
            case "use":
                RenderUse(canvas, element, style, matrix, definitions, width, height, referenceDepth, ancestors);
                break;
            case "path":
                RenderPath(canvas, element, style, matrix, definitions);
                break;
            case "rect":
                RenderRect(canvas, element, style, matrix, definitions);
                break;
            case "circle":
                RenderEllipse(canvas, element.GetDouble("cx"), element.GetDouble("cy"), element.GetDouble("r"), element.GetDouble("r"), style, matrix, definitions);
                break;
            case "ellipse":
                RenderEllipse(canvas, element.GetDouble("cx"), element.GetDouble("cy"), element.GetDouble("rx"), element.GetDouble("ry"), style, matrix, definitions);
                break;
            case "line":
                RenderLine(canvas, element, style, matrix, definitions);
                break;
            case "polyline":
                RenderPointList(canvas, element, style, matrix, definitions, close: false);
                break;
            case "polygon":
                RenderPointList(canvas, element, style, matrix, definitions, close: true);
                break;
            case "text":
                RenderText(canvas, element, style, matrix, definitions.StyleSheet, ancestors);
                return;
        }

        ancestors.Add(element);
        foreach (var child in element.Children) RenderElement(canvas, child, style, matrix, definitions, width, height, referenceDepth, ancestors);
        ancestors.RemoveAt(ancestors.Count - 1);
    }

    private static void RenderUse(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors) {
        if (referenceDepth >= 8 || !definitions.TryGetElement(HrefReferenceId(element), out var referenced)) return;
        var useMatrix = matrix.Multiply(SvgRasterMatrix.Translate(element.GetDouble("x"), element.GetDouble("y")));
        if (IsSymbolElement(referenced)) {
            var viewBox = referenced.Get("viewBox");
            if (!string.IsNullOrWhiteSpace(viewBox)) {
                var parsed = SvgRasterViewBox.Parse(viewBox);
                var symbolWidth = element.GetDouble("width", parsed.Width);
                var symbolHeight = element.GetDouble("height", parsed.Height);
                if (symbolWidth <= 0 || symbolHeight <= 0) return;
                useMatrix = useMatrix.Multiply(SvgRasterMatrix.FromFit(parsed, (int)Math.Round(symbolWidth), (int)Math.Round(symbolHeight), referenced.Get("preserveAspectRatio")));
            }

            ancestors.Add(referenced);
            foreach (var child in referenced.Children) RenderElement(canvas, child, style, useMatrix, definitions, width, height, referenceDepth + 1, ancestors);
            ancestors.RemoveAt(ancestors.Count - 1);
            return;
        }

        RenderElement(canvas, referenced, style, useMatrix, definitions, width, height, referenceDepth + 1, ancestors);
    }

    private static void RenderPath(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        var d = element.Get("d");
        if (string.IsNullOrWhiteSpace(d)) return;
        var rings = TransformRings(ChartMapPathParser.ParseRings(d!), matrix);
        FillAndStroke(canvas, rings, style, PathHasClose(d!), matrix, definitions);
    }

    private static void RenderRect(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        var x = element.GetDouble("x");
        var y = element.GetDouble("y");
        var width = element.GetDouble("width");
        var height = element.GetDouble("height");
        if (width <= 0 || height <= 0) return;
        var rx = Math.Max(0, Math.Min(element.GetDouble("rx", element.GetDouble("ry")), Math.Min(width, height) / 2.0));
        var ring = rx <= 0 ? RectRing(x, y, width, height) : RoundedRectRing(x, y, width, height, rx);
        FillAndStroke(canvas, new[] { TransformRing(ring, matrix) }, style, true, matrix, definitions);
    }

    private static void RenderEllipse(RgbaCanvas canvas, double cx, double cy, double rx, double ry, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        if (rx <= 0 || ry <= 0) return;
        FillAndStroke(canvas, new[] { TransformRing(EllipseRing(cx, cy, rx, ry, 36), matrix) }, style, true, matrix, definitions);
    }

    private static void RenderLine(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        var points = new[] {
            matrix.Transform(new ChartPoint(element.GetDouble("x1"), element.GetDouble("y1"))),
            matrix.Transform(new ChartPoint(element.GetDouble("x2"), element.GetDouble("y2")))
        };
        Stroke(canvas, points, style, matrix.ScaleFactor, definitions);
    }

    private static void RenderPointList(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, bool close) {
        var points = ReadPointList(element.Get("points"));
        if (points.Count == 0) return;
        var transformed = TransformRing(points, matrix);
        FillAndStroke(canvas, new[] { transformed }, style, close, matrix, definitions);
    }

    private static void RenderText(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterStyleSheet styleSheet, IReadOnlyList<SvgRasterElement> ancestors) {
        var spanChildren = TextSpanChildren(element);
        if (spanChildren.Count == 0) {
            DrawTextRun(canvas, element.Text, element.GetDouble("x") + FirstNumber(element.Get("dx")), element.GetDouble("y") + FirstNumber(element.Get("dy")), style, matrix);
            return;
        }

        var cursorX = element.GetDouble("x") + FirstNumber(element.Get("dx"));
        var cursorY = element.GetDouble("y") + FirstNumber(element.Get("dy"));
        var spanAncestors = new List<SvgRasterElement>(ancestors) { element };
        foreach (var span in spanChildren) {
            var spanStyle = SvgRasterStyle.Resolve(style, span, styleSheet, spanAncestors);
            if (span.TryGet("x", out _)) cursorX = span.GetDouble("x");
            if (span.TryGet("y", out _)) cursorY = span.GetDouble("y");
            cursorX += FirstNumber(span.Get("dx"));
            cursorY += FirstNumber(span.Get("dy"));
            var width = DrawTextRun(canvas, span.Text, cursorX, cursorY, spanStyle, matrix);
            cursorX += width / matrix.ScaleFactor;
        }
    }

    private static double DrawTextRun(RgbaCanvas canvas, string value, double x, double y, SvgRasterStyle style, SvgRasterMatrix matrix) {
        var color = style.FillColor();
        var text = NormalizeText(value);
        if (color.A == 0 || text.Length == 0) return 0;
        var point = matrix.Transform(new ChartPoint(x, y));
        var fontSize = Math.Max(1, style.FontSize * matrix.ScaleFactor);
        var width = RgbaCanvas.MeasureTextWidth(text, fontSize, null);
        var drawX = point.X;
        if (string.Equals(style.TextAnchor, "middle", StringComparison.OrdinalIgnoreCase)) drawX -= width / 2.0;
        else if (string.Equals(style.TextAnchor, "end", StringComparison.OrdinalIgnoreCase)) drawX -= width;
        var drawY = TextTop(point.Y, fontSize, style.DominantBaseline);
        if (IsBold(style.FontWeight)) canvas.DrawTextEmphasized(drawX, drawY, text, color, fontSize);
        else canvas.DrawText(drawX, drawY, text, color, fontSize);
        return width;
    }

    private static void FillAndStroke(RgbaCanvas canvas, IEnumerable<List<ChartPoint>> rings, SvgRasterStyle style, bool close, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        var contours = new List<List<ChartPoint>>();
        var strokeRings = new List<List<ChartPoint>>();
        foreach (var ring in rings) {
            if (ring.Count == 0) continue;
            var contour = close ? ClosedRing(ring) : new List<ChartPoint>(ring);
            if (close && contour.Count >= 3) contours.Add(contour);
            strokeRings.Add(contour);
        }

        Fill(canvas, contours, style, matrix, definitions);
        foreach (var ring in strokeRings) Stroke(canvas, ring, style, matrix.ScaleFactor, definitions);
    }

    private static void Fill(RgbaCanvas canvas, IReadOnlyList<List<ChartPoint>> contours, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        if (contours.Count == 0 || style.Fill.IsNone) return;
        var fillRule = FillRule(style.FillRule);
        if (style.Fill.IsReference && definitions.TryGetLinearGradient(style.Fill.ReferenceId, out var gradient)) {
            gradient.Endpoints(contours, matrix, out var start, out var end);
            canvas.FillContoursLinearGradient(contours, start, end, gradient.Stops, gradient.SpreadMethod, fillRule);
            return;
        }
        if (style.Fill.IsReference && definitions.TryGetRadialGradient(style.Fill.ReferenceId, out var radialGradient)) {
            radialGradient.Axes(contours, matrix, out var center, out var radiusX, out var radiusY);
            canvas.FillContoursRadialGradient(contours, center, radiusX, radiusY, radialGradient.Stops, radialGradient.SpreadMethod, fillRule);
            return;
        }
        if (style.Fill.IsReference && definitions.TryGetPattern(style.Fill.ReferenceId, out var pattern) && TryRenderPatternTile(contours, matrix, pattern, definitions, out var tile)) {
            canvas.FillContoursPattern(contours, tile.OriginX, tile.OriginY, tile.Width, tile.Height, tile.Pixels, fillRule);
            return;
        }

        var fill = style.FillColor();
        if (fill.A != 0) canvas.FillContours(contours, fill, fillRule);
    }

    private static void Stroke(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, SvgRasterStyle style, double scale, SvgRasterDefinitions definitions) {
        var stroke = ResolveColor(style.Stroke, style.Opacity * style.StrokeOpacity, definitions);
        if (stroke.A == 0 || style.StrokeWidth <= 0 || points.Count < 2) return;
        canvas.DrawPolyline(points, stroke, Math.Max(0.5, style.StrokeWidth * scale), LineCap(style.StrokeLineCap), LineJoin(style.StrokeLineJoin), ScaledDashArray(style.StrokeDashArray, scale));
    }

    private static ChartColor ResolveColor(SvgRasterPaint paint, double opacity, SvgRasterDefinitions definitions) {
        if (paint.IsNone) return ChartColor.Transparent;
        if (paint.Color.HasValue) return WithOpacity(paint.Color.Value, opacity);
        if (paint.IsReference && definitions.TryGetLinearGradient(paint.ReferenceId, out var gradient) && gradient.Stops.Count > 0) return WithOpacity(gradient.Stops[0].Color, opacity);
        if (paint.IsReference && definitions.TryGetRadialGradient(paint.ReferenceId, out var radialGradient) && radialGradient.Stops.Count > 0) return WithOpacity(radialGradient.Stops[0].Color, opacity);
        return ChartColor.Transparent;
    }

    private static bool TryRenderPatternTile(IReadOnlyList<List<ChartPoint>> contours, SvgRasterMatrix matrix, SvgRasterPattern pattern, SvgRasterDefinitions definitions, out PatternTile tile) {
        tile = default;
        if (pattern.Width <= 0 || pattern.Height <= 0 || pattern.Children.Count == 0) return false;
        var bounds = SvgRasterGradientValues.Bounds(contours);
        var frame = CreatePatternFrame(pattern, matrix, bounds);
        if (frame.Width <= 0 || frame.Height <= 0) return false;
        var tileWidth = Math.Max(1, (int)Math.Ceiling(frame.Width));
        var tileHeight = Math.Max(1, (int)Math.Ceiling(frame.Height));
        var tileCanvas = new RgbaCanvas(tileWidth, tileHeight, 1);
        var contentMatrix = PatternContentMatrix(pattern, matrix, bounds, frame, tileWidth, tileHeight);
        var ancestors = new List<SvgRasterElement>();
        foreach (var child in pattern.Children) RenderElement(tileCanvas, child, SvgRasterStyle.Default, contentMatrix, definitions, tileWidth, tileHeight, 0, ancestors);
        if (!HasVisiblePixel(tileCanvas.Pixels)) return false;
        tile = new PatternTile(frame.Left, frame.Top, tileWidth, tileHeight, tileCanvas.Pixels);
        return true;
    }

    private static PatternFrame CreatePatternFrame(SvgRasterPattern pattern, SvgRasterMatrix matrix, SvgRasterGradientValues.GradientBounds bounds) {
        if (!pattern.UserSpaceOnUse) {
            return new PatternFrame(
                bounds.Left + bounds.Width * pattern.X,
                bounds.Top + bounds.Height * pattern.Y,
                Math.Max(0, bounds.Width * pattern.Width),
                Math.Max(0, bounds.Height * pattern.Height));
        }

        var patternMatrix = matrix.Multiply(pattern.Transform);
        var origin = patternMatrix.Transform(new ChartPoint(pattern.X, pattern.Y));
        var right = patternMatrix.Transform(new ChartPoint(pattern.X + pattern.Width, pattern.Y));
        var bottom = patternMatrix.Transform(new ChartPoint(pattern.X, pattern.Y + pattern.Height));
        return new PatternFrame(origin.X, origin.Y, Distance(origin, right), Distance(origin, bottom));
    }

    private static SvgRasterMatrix PatternContentMatrix(SvgRasterPattern pattern, SvgRasterMatrix matrix, SvgRasterGradientValues.GradientBounds bounds, PatternFrame frame, int tileWidth, int tileHeight) {
        var local = SvgRasterMatrix.Translate(-frame.Left, -frame.Top);
        if (!string.IsNullOrWhiteSpace(pattern.ViewBox)) {
            var viewBox = SvgRasterViewBox.Parse(pattern.ViewBox!);
            return SvgRasterMatrix.FromFit(viewBox, tileWidth, tileHeight, pattern.PreserveAspectRatio);
        }

        if (!pattern.ContentUserSpaceOnUse) {
            return local.Multiply(SvgRasterMatrix.Translate(bounds.Left, bounds.Top)).Multiply(SvgRasterMatrix.Scale(bounds.Width, bounds.Height));
        }

        return pattern.UserSpaceOnUse ? local.Multiply(matrix.Multiply(pattern.Transform)) : local.Multiply(matrix);
    }

    private static void RenderClipPath(RgbaCanvas mask, SvgRasterClipPath clipPath, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        var clipMatrix = matrix.Multiply(SvgRasterMatrix.ParseTransform(clipPath.Element.Get("transform")));
        var ancestors = new List<SvgRasterElement>();
        var clipStyle = SvgRasterStyle.Resolve(SvgRasterStyle.Default, clipPath.Element, definitions.StyleSheet, ancestors);
        ancestors.Add(clipPath.Element);
        foreach (var child in clipPath.Element.Children) RenderClipElement(mask, child, clipStyle, clipMatrix, definitions.StyleSheet, ancestors);
    }

    private static void RenderMask(RgbaCanvas mask, SvgRasterMask maskDefinition, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height) {
        var maskMatrix = matrix.Multiply(SvgRasterMatrix.ParseTransform(maskDefinition.Element.Get("transform")));
        var ancestors = new List<SvgRasterElement> { maskDefinition.Element };
        foreach (var child in maskDefinition.Element.Children) RenderElement(mask, child, SvgRasterStyle.Default, maskMatrix, definitions, width, height, 0, ancestors);
    }

    private static void RenderClipElement(RgbaCanvas mask, SvgRasterElement element, SvgRasterStyle parentStyle, SvgRasterMatrix parentMatrix, SvgRasterStyleSheet styleSheet, List<SvgRasterElement> ancestors) {
        if (IsDefinitionElement(element.Name)) return;
        var style = SvgRasterStyle.Resolve(parentStyle, element, styleSheet, ancestors);
        if (!style.Visible) return;
        var matrix = parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(element.Get("transform")));
        if (string.Equals(element.Name, "svg", StringComparison.Ordinal)) matrix = ApplyNestedSvgViewport(element, matrix);
        var contours = ClipContours(element, matrix);
        if (contours.Count > 0) mask.FillContours(contours, ChartColor.FromRgba(255, 255, 255, 255), FillRule(style.ClipRule));
        ancestors.Add(element);
        foreach (var child in element.Children) RenderClipElement(mask, child, style, matrix, styleSheet, ancestors);
        ancestors.RemoveAt(ancestors.Count - 1);
    }

    private static List<List<ChartPoint>> ClipContours(SvgRasterElement element, SvgRasterMatrix matrix) {
        switch (element.Name) {
            case "path":
                var d = element.Get("d");
                return string.IsNullOrWhiteSpace(d) ? new List<List<ChartPoint>>() : TransformRings(ChartMapPathParser.ParseRings(d!), matrix);
            case "rect":
                var width = element.GetDouble("width");
                var height = element.GetDouble("height");
                if (width <= 0 || height <= 0) return new List<List<ChartPoint>>();
                var x = element.GetDouble("x");
                var y = element.GetDouble("y");
                var rx = Math.Max(0, Math.Min(element.GetDouble("rx", element.GetDouble("ry")), Math.Min(width, height) / 2.0));
                return new List<List<ChartPoint>> { TransformRing(rx <= 0 ? RectRing(x, y, width, height) : RoundedRectRing(x, y, width, height, rx), matrix) };
            case "circle":
                var r = element.GetDouble("r");
                return r <= 0 ? new List<List<ChartPoint>>() : new List<List<ChartPoint>> { TransformRing(EllipseRing(element.GetDouble("cx"), element.GetDouble("cy"), r, r, 36), matrix) };
            case "ellipse":
                var rxEllipse = element.GetDouble("rx");
                var ryEllipse = element.GetDouble("ry");
                return rxEllipse <= 0 || ryEllipse <= 0 ? new List<List<ChartPoint>>() : new List<List<ChartPoint>> { TransformRing(EllipseRing(element.GetDouble("cx"), element.GetDouble("cy"), rxEllipse, ryEllipse, 36), matrix) };
            case "polygon":
                var points = ReadPointList(element.Get("points"));
                return points.Count == 0 ? new List<List<ChartPoint>>() : new List<List<ChartPoint>> { TransformRing(points, matrix) };
            default:
                return new List<List<ChartPoint>>();
        }
    }

    private static SvgRasterMatrix ApplyNestedSvgViewport(SvgRasterElement element, SvgRasterMatrix matrix) {
        var viewBox = element.Get("viewBox");
        if (string.IsNullOrWhiteSpace(viewBox)) return matrix;
        var parsed = SvgRasterViewBox.Parse(viewBox);
        var x = element.GetDouble("x");
        var y = element.GetDouble("y");
        var width = element.GetDouble("width", parsed.Width);
        var height = element.GetDouble("height", parsed.Height);
        if (width <= 0 || height <= 0) return matrix;
        return matrix.Multiply(SvgRasterMatrix.Translate(x, y)).Multiply(SvgRasterMatrix.FromFit(parsed, (int)Math.Round(width), (int)Math.Round(height), element.Get("preserveAspectRatio")));
    }

    private static List<List<ChartPoint>> TransformRings(IReadOnlyList<List<ChartPoint>> rings, SvgRasterMatrix matrix) {
        var transformed = new List<List<ChartPoint>>(rings.Count);
        foreach (var ring in rings) transformed.Add(TransformRing(ring, matrix));
        return transformed;
    }

    private static List<ChartPoint> TransformRing(IReadOnlyList<ChartPoint> points, SvgRasterMatrix matrix) {
        var transformed = new List<ChartPoint>(points.Count);
        foreach (var point in points) transformed.Add(matrix.Transform(point));
        return transformed;
    }

    private static List<ChartPoint> RectRing(double x, double y, double width, double height) =>
        new() { new ChartPoint(x, y), new ChartPoint(x + width, y), new ChartPoint(x + width, y + height), new ChartPoint(x, y + height) };

    private static List<ChartPoint> RoundedRectRing(double x, double y, double width, double height, double radius) {
        var points = new List<ChartPoint>();
        AddArc(points, x + width - radius, y + radius, radius, -Math.PI / 2, 0);
        AddArc(points, x + width - radius, y + height - radius, radius, 0, Math.PI / 2);
        AddArc(points, x + radius, y + height - radius, radius, Math.PI / 2, Math.PI);
        AddArc(points, x + radius, y + radius, radius, Math.PI, Math.PI * 1.5);
        return points;
    }

    private static List<ChartPoint> EllipseRing(double cx, double cy, double rx, double ry, int segments) {
        var points = new List<ChartPoint>(segments);
        for (var i = 0; i < segments; i++) {
            var angle = Math.PI * 2 * i / segments;
            points.Add(new ChartPoint(cx + Math.Cos(angle) * rx, cy + Math.Sin(angle) * ry));
        }

        return points;
    }

    private static void AddArc(List<ChartPoint> points, double cx, double cy, double radius, double start, double end) {
        const int segments = 8;
        for (var i = 0; i <= segments; i++) {
            var angle = start + (end - start) * i / segments;
            points.Add(new ChartPoint(cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius));
        }
    }

    private static List<ChartPoint> ReadPointList(string? value) {
        var numbers = SvgRasterNumbers.ParseList(value);
        var points = new List<ChartPoint>(numbers.Count / 2);
        for (var i = 0; i + 1 < numbers.Count; i += 2) points.Add(new ChartPoint(numbers[i], numbers[i + 1]));
        return points;
    }

    private static IReadOnlyList<SvgRasterElement> TextSpanChildren(SvgRasterElement element) {
        var spans = new List<SvgRasterElement>();
        foreach (var child in element.Children) if (string.Equals(child.Name, "tspan", StringComparison.Ordinal)) spans.Add(child);
        return spans;
    }

    private static string NormalizeText(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static double FirstNumber(string? value) {
        var numbers = SvgRasterNumbers.ParseList(value);
        return numbers.Count == 0 ? 0 : numbers[0];
    }

    private static double TextTop(double y, double fontSize, string baseline) {
        if (string.Equals(baseline, "middle", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(baseline, "central", StringComparison.OrdinalIgnoreCase)) return y - fontSize * 0.5;
        if (string.Equals(baseline, "hanging", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(baseline, "text-before-edge", StringComparison.OrdinalIgnoreCase)) return y;
        if (string.Equals(baseline, "text-after-edge", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(baseline, "ideographic", StringComparison.OrdinalIgnoreCase)) return y - fontSize;
        return y - fontSize * 0.82;
    }

    private static List<ChartPoint> ClosedRing(IReadOnlyList<ChartPoint> points) {
        var closed = new List<ChartPoint>(points);
        if (closed.Count > 1 && DistanceSquared(closed[0], closed[closed.Count - 1]) > 0.000001) closed.Add(closed[0]);
        return closed;
    }

    private static bool PathHasClose(string pathData) =>
        pathData.IndexOf('z') >= 0 || pathData.IndexOf('Z') >= 0;

    private static bool IsDefinitionElement(string name) =>
        string.Equals(name, "defs", StringComparison.Ordinal) || string.Equals(name, "userDefs", StringComparison.Ordinal) || string.Equals(name, "pattern", StringComparison.Ordinal) || string.Equals(name, "clipPath", StringComparison.Ordinal) || string.Equals(name, "mask", StringComparison.Ordinal) || string.Equals(name, "style", StringComparison.Ordinal) || IsSymbolElement(name) || string.Equals(name, "title", StringComparison.Ordinal) || string.Equals(name, "desc", StringComparison.Ordinal);

    private static string? ReferenceId(SvgRasterElement element, string propertyName) {
        var value = element.Get(propertyName);
        var inline = element.Get("style");
        if (!string.IsNullOrWhiteSpace(inline)) {
            foreach (var declaration in ChartForgeX.Svg.SvgStyleDeclarationList.Parse(inline!).Declarations) {
                if (string.Equals(declaration.Name, propertyName, StringComparison.Ordinal)) value = declaration.Value;
            }
        }

        return ParseReference(value);
    }

    private static string? ParseReference(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value!.Trim();
        if (!trimmed.StartsWith("url(", StringComparison.OrdinalIgnoreCase)) return null;
        var close = trimmed.IndexOf(')');
        if (close < 0) return null;
        var body = trimmed.Substring(4, close - 4).Trim().Trim('\'', '"');
        return body.StartsWith("#", StringComparison.Ordinal) && body.Length > 1 ? body.Substring(1) : null;
    }

    private static string? HrefReferenceId(SvgRasterElement element) {
        var href = element.Get("href");
        if (string.IsNullOrWhiteSpace(href)) return null;
        var trimmed = href!.Trim().Trim('\'', '"');
        return trimmed.StartsWith("#", StringComparison.Ordinal) && trimmed.Length > 1 ? trimmed.Substring(1) : null;
    }

    private static bool IsSymbolElement(SvgRasterElement element) =>
        IsSymbolElement(element.Name);

    private static bool IsSymbolElement(string name) =>
        string.Equals(name, "symbol", StringComparison.Ordinal);

    private static bool IsBold(string value) =>
        string.Equals(value, "bold", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "600", StringComparison.Ordinal) || string.Equals(value, "700", StringComparison.Ordinal) || string.Equals(value, "800", StringComparison.Ordinal) || string.Equals(value, "900", StringComparison.Ordinal);

    private static RasterLineCap LineCap(string value) =>
        string.Equals(value, "round", StringComparison.OrdinalIgnoreCase) ? RasterLineCap.Round : RasterLineCap.Butt;

    private static RasterLineJoin LineJoin(string value) =>
        string.Equals(value, "round", StringComparison.OrdinalIgnoreCase) ? RasterLineJoin.Round : string.Equals(value, "bevel", StringComparison.OrdinalIgnoreCase) ? RasterLineJoin.Bevel : RasterLineJoin.Miter;

    private static RasterFillRule FillRule(string value) =>
        string.Equals(value, "evenodd", StringComparison.OrdinalIgnoreCase) ? RasterFillRule.EvenOdd : RasterFillRule.NonZero;

    private static IReadOnlyList<double>? ScaledDashArray(IReadOnlyList<double>? dashArray, double scale) {
        if (dashArray == null || dashArray.Count == 0) return null;
        var scaled = new double[dashArray.Count];
        for (var i = 0; i < dashArray.Count; i++) scaled[i] = dashArray[i] * scale;
        return scaled;
    }

    private static double DistanceSquared(ChartPoint a, ChartPoint b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static double Distance(ChartPoint a, ChartPoint b) =>
        Math.Sqrt(DistanceSquared(a, b));

    private static ChartColor WithOpacity(ChartColor color, double opacity) {
        opacity = Math.Max(0, Math.Min(1, opacity));
        return ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity));
    }

    private static bool HasVisiblePixel(byte[] rgba) {
        for (var i = 3; i < rgba.Length; i += 4) if (rgba[i] != 0) return true;
        return false;
    }

    private readonly struct PatternFrame {
        public readonly double Left;
        public readonly double Top;
        public readonly double Width;
        public readonly double Height;

        public PatternFrame(double left, double top, double width, double height) {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }

    private readonly struct PatternTile {
        public readonly double OriginX;
        public readonly double OriginY;
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Pixels;

        public PatternTile(double originX, double originY, int width, int height, byte[] pixels) {
            OriginX = originX;
            OriginY = originY;
            Width = width;
            Height = height;
            Pixels = pixels;
        }
    }
}
