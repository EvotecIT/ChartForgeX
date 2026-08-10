using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    public static bool TryRenderFragment(string svgBody, string? viewBox, string? preserveAspectRatio, int width, int height, out byte[] rgba) {
        rgba = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(svgBody) || width <= 0 || height <= 0) return false;

        try {
            var document = SvgRasterParser.ParseFragment(svgBody, viewBox);
            rgba = RenderDocument(document, preserveAspectRatio, width, height);
            return HasVisiblePixel(rgba);
        } catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException || ex is ArgumentException || ex is System.Xml.XmlException) {
            return false;
        }
    }

    public static bool TryRenderDocument(string svg, string? preserveAspectRatio, int width, int height, out byte[] rgba) {
        rgba = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(svg) || width <= 0 || height <= 0) return false;

        try {
            rgba = RenderDocument(SvgRasterParser.ParseDocument(svg), preserveAspectRatio, width, height);
            return true;
        } catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException || ex is ArgumentException || ex is System.Xml.XmlException) {
            return false;
        }
    }

    private static byte[] RenderDocument(SvgRasterDocument document, string? preserveAspectRatio, int width, int height, int imageDepth = 0) {
        var definitions = SvgRasterDefinitions.From(document);
        var canvas = new RgbaCanvas(width, height, 1);
        var rootStyle = SvgRasterStyle.Resolve(SvgRasterStyle.Default, document.Root, definitions.StyleSheet);
        var matrix = SvgRasterMatrix.FromFit(document.ViewBox, width, height, preserveAspectRatio)
            .Multiply(SvgRasterMatrix.ParseTransform(document.Root.Get("transform")));
        var viewport = new SvgRasterViewport(document.ViewBox.Width, document.ViewBox.Height);
        var ancestors = new List<SvgRasterElement> { document.Root };
        var rootOpacity = rootStyle.Opacity;
        rootStyle.Opacity = 1;
        var hasClipPath = definitions.TryGetClipPath(ParseReference(rootStyle.ClipPath) ?? ReferenceId(document.Root, "clip-path"), out var clipPath);
        var hasMask = definitions.TryGetMask(ReferenceId(document.Root, "mask"), out var maskDefinition);
        var target = rootOpacity < 0.999 || hasClipPath || hasMask ? new RgbaCanvas(width, height, 1) : canvas;
        foreach (var child in document.Children) RenderElement(target, child, rootStyle, matrix, definitions, width, height, imageDepth, ancestors, viewport);
        if (ReferenceEquals(target, canvas)) return canvas.Pixels;

        if (hasClipPath) {
            var clipped = new RgbaCanvas(width, height, 1);
            var clipMask = new RgbaCanvas(width, height, 1);
            RenderClipPath(clipMask, clipPath, matrix, definitions, width, height, target.Pixels, viewport, document.Root, rootStyle, ancestors);
            clipped.DrawImageMasked(0, 0, width, height, target.Pixels, clipMask.Pixels);
            target = clipped;
        }

        if (hasMask) {
            var masked = new RgbaCanvas(width, height, 1);
            var mask = new RgbaCanvas(width, height, 1);
            RenderMask(mask, maskDefinition, matrix, definitions, width, height, target.Pixels, viewport, document.Root, rootStyle, ancestors);
            masked.DrawImageMasked(0, 0, width, height, target.Pixels, mask.Pixels, maskDefinition.UsesAlpha);
            target = masked;
        }

        canvas.DrawImage(0, 0, width, height, rootOpacity < 0.999 ? ApplyOpacity(target.Pixels, rootOpacity) : target.Pixels);
        return canvas.Pixels;
    }

    private static void RenderElement(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle parentStyle, SvgRasterMatrix parentMatrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        var style = SvgRasterStyle.Resolve(parentStyle, element, definitions.StyleSheet, ancestors);
        if (!style.Displayed) return;

        var elementMatrix = parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(element.Get("transform")));
        SvgRasterNestedViewport? nestedViewport = null;
        if (string.Equals(element.Name, "svg", StringComparison.Ordinal)) {
            nestedViewport = ResolveNestedSvgViewport(element, viewport);
            if (nestedViewport.Value.Width <= 0 || nestedViewport.Value.Height <= 0) return;
        }
        var viewportClip = nestedViewport.HasValue && !string.Equals(style.Overflow, "visible", StringComparison.OrdinalIgnoreCase)
            ? nestedViewport.Value.Contour(elementMatrix)
            : null;
        var matrix = nestedViewport.HasValue ? ApplyNestedSvgViewport(element, elementMatrix, nestedViewport.Value) : elementMatrix;
        var childViewport = nestedViewport?.UserViewport ?? viewport;
        if (IsDefinitionElement(element.Name)) return;
        var hasClipPath = definitions.TryGetClipPath(ParseReference(style.ClipPath) ?? ReferenceId(element, "clip-path"), out var clipPath);
        var hasMask = definitions.TryGetMask(ReferenceId(element, "mask"), out var maskDefinition);
        var compositeOpacity = RequiresOpacityLayer(element, style) && style.Opacity < 0.999;
        if (hasClipPath || hasMask || compositeOpacity || viewportClip != null) {
            var content = new RgbaCanvas(width, height, 1);
            var contentStyle = style;
            if (compositeOpacity) {
                contentStyle = style.Inherit();
                contentStyle.Opacity = 1;
            }
            RenderElementCore(content, element, contentStyle, matrix, definitions, width, height, referenceDepth, ancestors, childViewport);
            if (hasClipPath) {
                var clippedContent = new RgbaCanvas(width, height, 1);
                var clipMask = new RgbaCanvas(width, height, 1);
                RenderClipPath(clipMask, clipPath, matrix, definitions, width, height, content.Pixels, childViewport, element, style, ancestors);
                clippedContent.DrawImageMasked(0, 0, width, height, content.Pixels, clipMask.Pixels);
                content = clippedContent;
            }

            if (hasMask) {
                var mask = new RgbaCanvas(width, height, 1);
                RenderMask(mask, maskDefinition, matrix, definitions, width, height, content.Pixels, childViewport, element, style, ancestors);
                var maskedContent = new RgbaCanvas(width, height, 1);
                maskedContent.DrawImageMasked(0, 0, width, height, content.Pixels, mask.Pixels, maskDefinition.UsesAlpha);
                content = maskedContent;
            }

            if (viewportClip != null) {
                var viewportMask = new RgbaCanvas(width, height, 1);
                viewportMask.FillPolygon(viewportClip, ChartColor.FromRgba(255, 255, 255, 255));
                var clippedContent = new RgbaCanvas(width, height, 1);
                clippedContent.DrawImageMasked(0, 0, width, height, content.Pixels, viewportMask.Pixels, useAlphaMask: true);
                content = clippedContent;
            }

            var pixels = compositeOpacity ? ApplyOpacity(content.Pixels, style.Opacity) : content.Pixels;
            canvas.DrawImage(0, 0, width, height, pixels);

            return;
        }

        RenderElementCore(canvas, element, style, matrix, definitions, width, height, referenceDepth, ancestors, childViewport);
    }

    private static bool RequiresOpacityLayer(SvgRasterElement element, SvgRasterStyle style) {
        if (element.Children.Count > 0 || string.Equals(element.Name, "use", StringComparison.Ordinal)) return true;
        if (HasMarkerPaint(element)) return true;
        if (style.Fill.IsNone || style.FillOpacity <= 0 || style.Stroke.IsNone || style.StrokeOpacity <= 0 || style.StrokeWidth <= 0) return false;
        return string.Equals(element.Name, "path", StringComparison.Ordinal) ||
               string.Equals(element.Name, "rect", StringComparison.Ordinal) ||
               string.Equals(element.Name, "circle", StringComparison.Ordinal) ||
               string.Equals(element.Name, "ellipse", StringComparison.Ordinal) ||
               string.Equals(element.Name, "polygon", StringComparison.Ordinal) ||
               string.Equals(element.Name, "text", StringComparison.Ordinal);
    }

    private static bool HasMarkerPaint(SvgRasterElement element) =>
        ReferenceId(element, "marker-start") != null || ReferenceId(element, "marker-end") != null;

    private static void RenderElementCore(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        switch (element.Name) {
            case "g":
            case "svg":
                break;
            case "use":
                RenderUse(canvas, element, style, matrix, definitions, width, height, referenceDepth, ancestors, viewport);
                break;
            case "path":
                if (style.VisibilityVisible) RenderPath(canvas, element, style, matrix, definitions, width, height, referenceDepth, ancestors, viewport);
                break;
            case "rect":
                if (style.VisibilityVisible) RenderRect(canvas, element, style, matrix, definitions, viewport);
                break;
            case "circle":
                if (style.VisibilityVisible) {
                    var radius = DiagonalLength(element, "r", viewport);
                    RenderEllipse(canvas, HorizontalLength(element, "cx", viewport), VerticalLength(element, "cy", viewport), radius, radius, style, matrix, definitions, viewport);
                }
                break;
            case "ellipse":
                if (style.VisibilityVisible) RenderEllipse(canvas, HorizontalLength(element, "cx", viewport), VerticalLength(element, "cy", viewport), HorizontalLength(element, "rx", viewport), VerticalLength(element, "ry", viewport), style, matrix, definitions, viewport);
                break;
            case "line":
                if (style.VisibilityVisible) RenderLine(canvas, element, style, matrix, definitions, width, height, referenceDepth, ancestors, viewport);
                break;
            case "polyline":
                if (style.VisibilityVisible) RenderPointList(canvas, element, style, matrix, definitions, width, height, referenceDepth, ancestors, viewport, close: false);
                break;
            case "polygon":
                if (style.VisibilityVisible) RenderPointList(canvas, element, style, matrix, definitions, width, height, referenceDepth, ancestors, viewport, close: true);
                break;
            case "text":
                RenderText(canvas, element, style, matrix, definitions, width, height, ancestors, viewport);
                return;
            case "image":
                if (style.VisibilityVisible) RenderImage(canvas, element, style, matrix, referenceDepth, viewport);
                return;
        }

        ancestors.Add(element);
        foreach (var child in element.Children) RenderElement(canvas, child, style, matrix, definitions, width, height, referenceDepth, ancestors, viewport);
        ancestors.RemoveAt(ancestors.Count - 1);
    }

    private static void RenderImage(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, int imageDepth, SvgRasterViewport viewport) {
        var width = HorizontalLength(element, "width", viewport);
        var height = VerticalLength(element, "height", viewport);
        if (width <= 0 || height <= 0 || style.Opacity <= 0) return;
        var x = HorizontalLength(element, "x", viewport);
        var y = VerticalLength(element, "y", viewport);
        var origin = matrix.Transform(new ChartPoint(x, y));
        var xAxis = matrix.Transform(new ChartPoint(x + width, y));
        var yAxis = matrix.Transform(new ChartPoint(x, y + height));
        (var localWidth, var localHeight) = ResolveTransformedImageDimensions(origin, xAxis, yAxis, canvas.Width, canvas.Height);
        if (!TryDecodeImage(element.Get("href"), imageDepth, localWidth, localHeight, element.Get("preserveAspectRatio"), out var image)) return;
        var pixels = style.Opacity >= 0.999 ? image.Pixels : ApplyOpacity(image.Pixels, style.Opacity);
        var placement = SvgRasterImagePlacement.Resolve(localWidth, localHeight, image.Width, image.Height, element.Get("preserveAspectRatio"));
        var imageBox = new RgbaCanvas(localWidth, localHeight, 1);
        imageBox.DrawImageScaled(placement.X, placement.Y, placement.Width, placement.Height, image.Width, image.Height, pixels, placement.SourceX, placement.SourceY, placement.SourceWidth, placement.SourceHeight);
        if (IsCenteredCircleClipPath(style.ClipPath)) {
            RgbaCanvas.ApplyCenteredCircleAlphaMask(localWidth, localHeight, imageBox.Pixels);
        }

        var imageMatrix = matrix
            .Multiply(SvgRasterMatrix.Translate(x, y))
            .Multiply(SvgRasterMatrix.Scale(width / localWidth, height / localHeight));
        canvas.DrawImageTransformed(
            localWidth,
            localHeight,
            imageBox.Pixels,
            imageMatrix.A,
            imageMatrix.B,
            imageMatrix.C,
            imageMatrix.D,
            imageMatrix.E,
            imageMatrix.F);
    }

    private static bool IsCenteredCircleClipPath(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var compact = value!.Replace(" ", string.Empty).Replace("\t", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
        return string.Equals(compact, "circle(50%)", StringComparison.OrdinalIgnoreCase) || string.Equals(compact, "circle(closest-side)", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryDecodeImage(string? href, int imageDepth, int targetWidth, int targetHeight, string? preserveAspectRatio, out RgbaImage image) {
        image = default;
        if (imageDepth >= 4 || string.IsNullOrWhiteSpace(href) || !href!.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) return false;
        var comma = href.IndexOf(',');
        if (comma < 0) return false;
        var header = href.Substring(5, comma - 5);
        var payload = href.Substring(comma + 1);
        try {
            if (header.StartsWith("image/svg+xml", StringComparison.OrdinalIgnoreCase)) {
                var markup = header.IndexOf(";base64", StringComparison.OrdinalIgnoreCase) >= 0
                    ? Encoding.UTF8.GetString(Convert.FromBase64String(payload))
                    : Uri.UnescapeDataString(payload);
                var document = SvgRasterParser.ParseDocument(markup);
                var embeddedWidth = Math.Max(1, targetWidth);
                var embeddedHeight = Math.Max(1, targetHeight);
                var rgba = RenderDocument(document, preserveAspectRatio, embeddedWidth, embeddedHeight, imageDepth + 1);
                image = new RgbaImage(embeddedWidth, embeddedHeight, rgba);
                return true;
            }
            var data = header.IndexOf(";base64", StringComparison.OrdinalIgnoreCase) >= 0
                ? Convert.FromBase64String(payload)
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
            return RasterImageDecoder.TryDecode(data, out image);
        } catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is InvalidOperationException || ex is System.Xml.XmlException) {
            return false;
        }
    }

    private static byte[] ApplyOpacity(byte[] pixels, double opacity) {
        var result = new byte[pixels.Length];
        Buffer.BlockCopy(pixels, 0, result, 0, pixels.Length);
        for (var index = 3; index < result.Length; index += 4) result[index] = (byte)Math.Round(result[index] * Math.Max(0, Math.Min(1, opacity)));
        return result;
    }

    private static void RenderUse(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        if (referenceDepth >= 8 || !definitions.TryGetElement(HrefReferenceId(element), out var referenced)) return;
        var useMatrix = matrix.Multiply(SvgRasterMatrix.Translate(HorizontalLength(element, "x", viewport), VerticalLength(element, "y", viewport)));
        var referencedAncestors = new List<SvgRasterElement>(definitions.AncestorsFor(referenced));
        if (IsSymbolElement(referenced)) {
            var symbolStyle = ResolveReferencedStyle(style, referenced, definitions);
            if (!symbolStyle.Displayed) return;
            var viewBox = referenced.Get("viewBox");
            var symbolWidth = HorizontalLength(element, "width", viewport, viewport.Width);
            var symbolHeight = VerticalLength(element, "height", viewport, viewport.Height);
            if (symbolWidth <= 0 || symbolHeight <= 0) return;
            var symbolViewport = new SvgRasterViewport(symbolWidth, symbolHeight);
            if (!string.IsNullOrWhiteSpace(viewBox)) {
                var parsed = SvgRasterViewBox.Parse(viewBox);
                useMatrix = useMatrix.Multiply(SvgRasterMatrix.FromFit(parsed, (int)Math.Round(symbolWidth), (int)Math.Round(symbolHeight), referenced.Get("preserveAspectRatio")));
                symbolViewport = new SvgRasterViewport(parsed.Width, parsed.Height);
            }

            useMatrix = useMatrix.Multiply(SvgRasterMatrix.ParseTransform(referenced.Get("transform")));
            referencedAncestors.Add(referenced);
            foreach (var child in referenced.Children) RenderElement(canvas, child, symbolStyle, useMatrix, definitions, width, height, referenceDepth + 1, referencedAncestors, symbolViewport);
            return;
        }

        RenderElement(canvas, referenced, style, useMatrix, definitions, width, height, referenceDepth + 1, referencedAncestors, viewport);
    }

    private static SvgRasterStyle ResolveReferencedStyle(SvgRasterStyle parentStyle, SvgRasterElement referenced, SvgRasterDefinitions definitions) {
        var inherited = parentStyle.Inherit();
        var definitionAncestors = definitions.AncestorsFor(referenced);
        foreach (var property in SvgRasterStyle.ResolveCustomProperties(definitions.StyleSheet, definitionAncestors, referenced)) {
            if (!inherited.CustomProperties.ContainsKey(property.Key)) inherited.CustomProperties[property.Key] = property.Value;
        }
        return SvgRasterStyle.Resolve(inherited, referenced, definitions.StyleSheet, definitionAncestors);
    }

    private static void RenderPath(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        var d = element.Get("d");
        if (string.IsNullOrWhiteSpace(d)) return;
        var subpaths = ChartMapPathParser.ParseSubpaths(d!);
        var sourceRings = new List<List<ChartPoint>>(subpaths.Count);
        var fillContours = new List<List<ChartPoint>>(subpaths.Count);
        var strokeContours = new List<List<ChartPoint>>(subpaths.Count);
        foreach (var subpath in subpaths) {
            sourceRings.Add(subpath.Points);
            var transformed = TransformRing(subpath.Points, matrix);
            var contour = ClosedRing(transformed);
            if (contour.Count >= 3) fillContours.Add(contour);
            strokeContours.Add(subpath.IsClosed ? contour : transformed);
        }
        Fill(canvas, fillContours, style, matrix, definitions, viewport);
        foreach (var strokeContour in strokeContours) Stroke(canvas, strokeContour, style, matrix.ScaleFactor, definitions);
        RenderMarkers(canvas, element, style, matrix, definitions, sourceRings, width, height, referenceDepth, ancestors);
    }

    private static void RenderRect(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport) {
        var x = HorizontalLength(element, "x", viewport);
        var y = VerticalLength(element, "y", viewport);
        var width = HorizontalLength(element, "width", viewport);
        var height = VerticalLength(element, "height", viewport);
        if (width <= 0 || height <= 0) return;
        ResolveRoundedRectRadii(element, viewport, width, height, out var rx, out var ry);
        var ring = rx <= 0 || ry <= 0 ? RectRing(x, y, width, height) : RoundedRectRing(x, y, width, height, rx, ry);
        FillAndStroke(canvas, new[] { TransformRing(ring, matrix) }, style, true, matrix, definitions, viewport);
    }

    private static void RenderEllipse(RgbaCanvas canvas, double cx, double cy, double rx, double ry, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport) {
        if (rx <= 0 || ry <= 0) return;
        FillAndStroke(canvas, new[] { TransformRing(EllipseRing(cx, cy, rx, ry, 36), matrix) }, style, true, matrix, definitions, viewport);
    }

    private static void RenderLine(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        var sourcePoints = new[] {
            new ChartPoint(HorizontalLength(element, "x1", viewport), VerticalLength(element, "y1", viewport)),
            new ChartPoint(HorizontalLength(element, "x2", viewport), VerticalLength(element, "y2", viewport))
        };
        var points = new[] { matrix.Transform(sourcePoints[0]), matrix.Transform(sourcePoints[1]) };
        Stroke(canvas, points, style, matrix.ScaleFactor, definitions);
        RenderMarkers(canvas, element, style, matrix, definitions, new[] { new List<ChartPoint>(sourcePoints) }, width, height, referenceDepth, ancestors);
    }

    private static void RenderPointList(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, List<SvgRasterElement> ancestors, SvgRasterViewport viewport, bool close) {
        var points = ReadPointList(element.Get("points"));
        if (points.Count == 0) return;
        var transformed = TransformRing(points, matrix);
        FillAndStroke(canvas, new[] { transformed }, style, close, matrix, definitions, viewport);
        RenderMarkers(canvas, element, style, matrix, definitions, new[] { points }, width, height, referenceDepth, ancestors);
    }

    private static void FillAndStroke(RgbaCanvas canvas, IEnumerable<List<ChartPoint>> rings, SvgRasterStyle style, bool closeStroke, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport) {
        var contours = new List<List<ChartPoint>>();
        var strokeRings = new List<List<ChartPoint>>();
        foreach (var ring in rings) {
            if (ring.Count == 0) continue;
            var contour = ClosedRing(ring);
            if (contour.Count >= 3) contours.Add(contour);
            strokeRings.Add(closeStroke ? contour : new List<ChartPoint>(ring));
        }

        Fill(canvas, contours, style, matrix, definitions, viewport);
        foreach (var ring in strokeRings) Stroke(canvas, ring, style, matrix.ScaleFactor, definitions);
    }

    private static void Fill(RgbaCanvas canvas, IReadOnlyList<List<ChartPoint>> contours, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport, SvgRasterObjectPaint? objectPaint = null) {
        if (contours.Count == 0 || style.Fill.IsNone) return;
        var fillRule = FillRule(style.FillRule);
        var paintOpacity = style.Opacity * style.FillOpacity;
        if (style.Fill.IsReference && definitions.TryGetLinearGradient(style.Fill.ReferenceId, out var gradient)) {
            ChartPoint start;
            ChartPoint end;
            if (objectPaint.HasValue && !gradient.UserSpaceOnUse) gradient.ObjectEndpoints(objectPaint.Value.Bounds, objectPaint.Value.Matrix, out start, out end);
            else gradient.Endpoints(contours, matrix, out start, out end);
            canvas.FillContoursLinearGradient(contours, start, end, gradient.Stops, gradient.SpreadMethod, fillRule, paintOpacity);
            return;
        }
        if (style.Fill.IsReference && definitions.TryGetRadialGradient(style.Fill.ReferenceId, out var radialGradient)) {
            ChartPoint center;
            ChartPoint radiusX;
            ChartPoint radiusY;
            if (objectPaint.HasValue && !radialGradient.UserSpaceOnUse) radialGradient.ObjectAxes(objectPaint.Value.Bounds, objectPaint.Value.Matrix, out center, out radiusX, out radiusY);
            else radialGradient.Axes(contours, matrix, out center, out radiusX, out radiusY);
            canvas.FillContoursRadialGradient(contours, center, radiusX, radiusY, radialGradient.Stops, radialGradient.SpreadMethod, fillRule, paintOpacity);
            return;
        }
        if (style.Fill.IsReference && definitions.TryGetPattern(style.Fill.ReferenceId, out var pattern) && TryRenderPatternTile(contours, matrix, pattern, definitions, viewport, objectPaint, out var tile)) {
            canvas.FillContoursPattern(contours, tile.Width, tile.Height, tile.Pixels, tile.CanvasToTile.A, tile.CanvasToTile.B, tile.CanvasToTile.C, tile.CanvasToTile.D, tile.CanvasToTile.E, tile.CanvasToTile.F, fillRule, paintOpacity);
            return;
        }

        var fill = style.FillColor();
        if (fill.A != 0) canvas.FillContours(contours, fill, fillRule);
    }

    private static void Stroke(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, SvgRasterStyle style, double scale, SvgRasterDefinitions definitions) {
        var stroke = ResolveColor(style.Stroke, style.Opacity * style.StrokeOpacity, definitions);
        if (stroke.A == 0 || style.StrokeWidth <= 0 || points.Count < 2) return;
        canvas.DrawPolyline(points, stroke, Math.Max(0.5, style.StrokeWidth * scale), LineCap(style.StrokeLineCap), LineJoin(style.StrokeLineJoin), ScaledDashArray(style.StrokeDashArray, scale), style.StrokeMiterLimit);
    }

    private static ChartColor ResolveColor(SvgRasterPaint paint, double opacity, SvgRasterDefinitions definitions) {
        if (paint.IsNone) return ChartColor.Transparent;
        if (paint.Color.HasValue) return WithOpacity(paint.Color.Value, opacity);
        if (paint.IsReference && definitions.TryGetLinearGradient(paint.ReferenceId, out var gradient) && gradient.Stops.Count > 0) return WithOpacity(gradient.Stops[0].Color, opacity);
        if (paint.IsReference && definitions.TryGetRadialGradient(paint.ReferenceId, out var radialGradient) && radialGradient.Stops.Count > 0) return WithOpacity(radialGradient.Stops[0].Color, opacity);
        return ChartColor.Transparent;
    }

    private static bool TryRenderPatternTile(IReadOnlyList<List<ChartPoint>> contours, SvgRasterMatrix matrix, SvgRasterPattern pattern, SvgRasterDefinitions definitions, SvgRasterViewport viewport, SvgRasterObjectPaint? objectPaint, out PatternTile tile) {
        tile = default;
        if (pattern.Width <= 0 || pattern.Height <= 0 || pattern.Children.Count == 0) return false;
        var bounds = objectPaint?.Bounds ?? SvgRasterGradientValues.Bounds(contours);
        var objectMatrix = objectPaint?.Matrix ?? SvgRasterMatrix.Identity;
        var objectToCanvas = objectMatrix
            .Multiply(SvgRasterMatrix.Translate(bounds.Left, bounds.Top))
            .Multiply(SvgRasterMatrix.Scale(bounds.Width, bounds.Height));
        var patternToCanvas = pattern.UserSpaceOnUse
            ? matrix.Multiply(pattern.Transform)
            : objectToCanvas.Multiply(pattern.Transform);
        var frame = CreatePatternFrame(pattern, patternToCanvas);
        if (frame.Width <= 0 || frame.Height <= 0) return false;
        var tileWidth = Math.Max(1, (int)Math.Ceiling(frame.Width));
        var tileHeight = Math.Max(1, (int)Math.Ceiling(frame.Height));
        var tileToCanvas = new SvgRasterMatrix(
            (frame.Right.X - frame.Origin.X) / tileWidth,
            (frame.Right.Y - frame.Origin.Y) / tileWidth,
            (frame.Bottom.X - frame.Origin.X) / tileHeight,
            (frame.Bottom.Y - frame.Origin.Y) / tileHeight,
            frame.Origin.X,
            frame.Origin.Y);
        if (!tileToCanvas.TryInvert(out var canvasToTile)) return false;
        var tileCanvas = new RgbaCanvas(tileWidth, tileHeight, 1);
        var contentMatrix = PatternContentMatrix(pattern, matrix, objectMatrix, objectToCanvas, canvasToTile, objectPaint.HasValue, tileWidth, tileHeight);
        var contentViewport = !string.IsNullOrWhiteSpace(pattern.ViewBox)
            ? ViewportFromViewBox(pattern.ViewBox!)
            : pattern.ContentUserSpaceOnUse ? viewport : new SvgRasterViewport(1, 1);
        var ancestors = new List<SvgRasterElement>();
        foreach (var child in pattern.Children) RenderElement(tileCanvas, child, SvgRasterStyle.Default, contentMatrix, definitions, tileWidth, tileHeight, 0, ancestors, contentViewport);
        if (!HasVisiblePixel(tileCanvas.Pixels)) return false;
        tile = new PatternTile(tileWidth, tileHeight, tileCanvas.Pixels, canvasToTile);
        return true;
    }

    private static SvgRasterViewport ViewportFromViewBox(string value) {
        var viewBox = SvgRasterViewBox.Parse(value);
        return new SvgRasterViewport(viewBox.Width, viewBox.Height);
    }

    private static PatternFrame CreatePatternFrame(SvgRasterPattern pattern, SvgRasterMatrix patternToCanvas) {
        var origin = patternToCanvas.Transform(new ChartPoint(pattern.X, pattern.Y));
        var right = patternToCanvas.Transform(new ChartPoint(pattern.X + pattern.Width, pattern.Y));
        var bottom = patternToCanvas.Transform(new ChartPoint(pattern.X, pattern.Y + pattern.Height));
        return new PatternFrame(origin, right, bottom);
    }

    private static SvgRasterMatrix PatternContentMatrix(SvgRasterPattern pattern, SvgRasterMatrix matrix, SvgRasterMatrix objectMatrix, SvgRasterMatrix objectToCanvas, SvgRasterMatrix canvasToTile, bool hasObjectPaint, int tileWidth, int tileHeight) {
        if (!string.IsNullOrWhiteSpace(pattern.ViewBox)) {
            var viewBox = SvgRasterViewBox.Parse(pattern.ViewBox!);
            return SvgRasterMatrix.FromFit(viewBox, tileWidth, tileHeight, pattern.PreserveAspectRatio);
        }

        if (!pattern.ContentUserSpaceOnUse) return canvasToTile.Multiply(objectToCanvas);
        return canvasToTile.Multiply(pattern.UserSpaceOnUse ? matrix.Multiply(pattern.Transform) : hasObjectPaint ? objectMatrix : matrix);
    }

    private static void RenderClipPath(RgbaCanvas mask, SvgRasterClipPath clipPath, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, byte[] targetPixels, SvgRasterViewport viewport, SvgRasterElement targetElement, SvgRasterStyle targetStyle, IReadOnlyList<SvgRasterElement> targetAncestors) {
        var clipMatrix = matrix.Multiply(SvgRasterMatrix.ParseTransform(clipPath.Element.Get("transform")));
        var clipViewport = viewport;
        if (!clipPath.UserSpaceOnUse) {
            if (!TryVisibleBounds(targetPixels, width, height, matrix, out var paintedBounds)) return;
            var bounds = TryObjectBounds(targetElement, targetStyle, targetAncestors, definitions, viewport, out var objectBounds) ? objectBounds : paintedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            clipMatrix = matrix
                .Multiply(SvgRasterMatrix.Translate(bounds.Left, bounds.Top))
                .Multiply(SvgRasterMatrix.Scale(bounds.Width, bounds.Height))
                .Multiply(SvgRasterMatrix.ParseTransform(clipPath.Element.Get("transform")));
            clipViewport = new SvgRasterViewport(1, 1);
        }
        var ancestors = new List<SvgRasterElement>();
        var clipStyle = SvgRasterStyle.Default;
        foreach (var ancestor in clipPath.Ancestors) {
            clipStyle = SvgRasterStyle.Resolve(clipStyle, ancestor, definitions.StyleSheet, ancestors);
            ancestors.Add(ancestor);
        }
        clipStyle = SvgRasterStyle.Resolve(clipStyle, clipPath.Element, definitions.StyleSheet, ancestors);
        ancestors.Add(clipPath.Element);
        foreach (var child in clipPath.Element.Children) {
            var clipChild = clipPath.UserSpaceOnUse ? child : ResolveObjectBoundingBoxContent(child, definitions, 0);
            RenderClipElement(mask, clipChild, clipStyle, clipMatrix, definitions, ancestors, clipViewport, 0);
        }
    }

    private static void RenderMask(RgbaCanvas mask, SvgRasterMask maskDefinition, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, int width, int height, byte[] targetPixels, SvgRasterViewport viewport, SvgRasterElement targetElement, SvgRasterStyle targetStyle, IReadOnlyList<SvgRasterElement> targetAncestors) {
        if (!TryVisibleBounds(targetPixels, width, height, matrix, out var paintedBounds)) return;
        var bounds = TryObjectBounds(targetElement, targetStyle, targetAncestors, definitions, viewport, out var objectBounds) ? objectBounds : paintedBounds;
        var content = new RgbaCanvas(width, height, 1);
        var maskMatrix = matrix.Multiply(SvgRasterMatrix.ParseTransform(maskDefinition.Element.Get("transform")));
        if (!maskDefinition.ContentUserSpaceOnUse) {
            maskMatrix = matrix
                .Multiply(SvgRasterMatrix.Translate(bounds.Left, bounds.Top))
                .Multiply(SvgRasterMatrix.Scale(bounds.Width, bounds.Height))
                .Multiply(SvgRasterMatrix.ParseTransform(maskDefinition.Element.Get("transform")));
        }
        var ancestors = new List<SvgRasterElement>(maskDefinition.Ancestors) { maskDefinition.Element };
        foreach (var child in maskDefinition.Element.Children) {
            var maskChild = maskDefinition.ContentUserSpaceOnUse ? child : ResolveObjectBoundingBoxContent(child, definitions, 0);
            RenderElement(content, maskChild, maskDefinition.RootStyle, maskMatrix, definitions, width, height, 0, ancestors, viewport);
        }

        var region = MaskRegion(maskDefinition, matrix, bounds, viewport);
        if (region.Count == 0) return;
        var regionMask = new RgbaCanvas(width, height, 1);
        regionMask.FillPolygon(region, ChartColor.FromRgba(255, 255, 255, 255));
        mask.DrawImageMasked(0, 0, width, height, content.Pixels, regionMask.Pixels, useAlphaMask: true);
    }

    private static void RenderClipElement(RgbaCanvas mask, SvgRasterElement element, SvgRasterStyle parentStyle, SvgRasterMatrix parentMatrix, SvgRasterDefinitions definitions, List<SvgRasterElement> ancestors, SvgRasterViewport viewport, int referenceDepth) {
        if (IsDefinitionElement(element.Name)) return;
        var style = SvgRasterStyle.Resolve(parentStyle, element, definitions.StyleSheet, ancestors);
        if (!style.Displayed) return;
        var matrix = parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(element.Get("transform")));
        if (string.Equals(element.Name, "svg", StringComparison.Ordinal)) {
            var nested = ResolveNestedSvgViewport(element, viewport);
            if (nested.Width <= 0 || nested.Height <= 0) return;
            matrix = ApplyNestedSvgViewport(element, matrix, nested);
            viewport = nested.UserViewport;
        }
        if (string.Equals(element.Name, "use", StringComparison.Ordinal)) {
            if (referenceDepth >= 8 || !definitions.TryGetElement(HrefReferenceId(element), out var referenced)) return;
            var useMatrix = matrix.Multiply(SvgRasterMatrix.Translate(HorizontalLength(element, "x", viewport), VerticalLength(element, "y", viewport)));
            var referencedAncestors = new List<SvgRasterElement>(definitions.AncestorsFor(referenced));
            if (IsSymbolElement(referenced)) {
                var symbolStyle = ResolveReferencedStyle(style, referenced, definitions);
                if (!symbolStyle.Displayed) return;
                var symbolWidth = HorizontalLength(element, "width", viewport, viewport.Width);
                var symbolHeight = VerticalLength(element, "height", viewport, viewport.Height);
                if (symbolWidth <= 0 || symbolHeight <= 0) return;
                var symbolViewport = new SvgRasterViewport(symbolWidth, symbolHeight);
                var viewBox = referenced.Get("viewBox");
                if (!string.IsNullOrWhiteSpace(viewBox)) {
                    var parsed = SvgRasterViewBox.Parse(viewBox);
                    useMatrix = useMatrix.Multiply(SvgRasterMatrix.FromFit(parsed, (int)Math.Round(symbolWidth), (int)Math.Round(symbolHeight), referenced.Get("preserveAspectRatio")));
                    symbolViewport = new SvgRasterViewport(parsed.Width, parsed.Height);
                }
                useMatrix = useMatrix.Multiply(SvgRasterMatrix.ParseTransform(referenced.Get("transform")));
                referencedAncestors.Add(referenced);
                foreach (var child in referenced.Children) RenderClipElement(mask, child, symbolStyle, useMatrix, definitions, referencedAncestors, symbolViewport, referenceDepth + 1);
            } else {
                RenderClipElement(mask, referenced, style, useMatrix, definitions, referencedAncestors, viewport, referenceDepth + 1);
            }
            return;
        }
        if (style.VisibilityVisible) {
            var contours = ClipContours(element, matrix, viewport);
            if (contours.Count > 0) mask.FillContours(contours, ChartColor.FromRgba(255, 255, 255, 255), FillRule(style.ClipRule));
        }
        ancestors.Add(element);
        foreach (var child in element.Children) RenderClipElement(mask, child, style, matrix, definitions, ancestors, viewport, referenceDepth);
        ancestors.RemoveAt(ancestors.Count - 1);
    }

    private static List<List<ChartPoint>> ClipContours(SvgRasterElement element, SvgRasterMatrix matrix, SvgRasterViewport viewport) {
        switch (element.Name) {
            case "path":
                var d = element.Get("d");
                return string.IsNullOrWhiteSpace(d) ? new List<List<ChartPoint>>() : TransformRings(ChartMapPathParser.ParseRings(d!), matrix);
            case "rect":
                var width = HorizontalLength(element, "width", viewport);
                var height = VerticalLength(element, "height", viewport);
                if (width <= 0 || height <= 0) return new List<List<ChartPoint>>();
                var x = HorizontalLength(element, "x", viewport);
                var y = VerticalLength(element, "y", viewport);
                ResolveRoundedRectRadii(element, viewport, width, height, out var rx, out var ry);
                return new List<List<ChartPoint>> { TransformRing(rx <= 0 || ry <= 0 ? RectRing(x, y, width, height) : RoundedRectRing(x, y, width, height, rx, ry), matrix) };
            case "circle":
                var r = DiagonalLength(element, "r", viewport);
                return r <= 0 ? new List<List<ChartPoint>>() : new List<List<ChartPoint>> { TransformRing(EllipseRing(HorizontalLength(element, "cx", viewport), VerticalLength(element, "cy", viewport), r, r, 36), matrix) };
            case "ellipse":
                var rxEllipse = HorizontalLength(element, "rx", viewport);
                var ryEllipse = VerticalLength(element, "ry", viewport);
                return rxEllipse <= 0 || ryEllipse <= 0 ? new List<List<ChartPoint>>() : new List<List<ChartPoint>> { TransformRing(EllipseRing(HorizontalLength(element, "cx", viewport), VerticalLength(element, "cy", viewport), rxEllipse, ryEllipse, 36), matrix) };
            case "polygon":
                var points = ReadPointList(element.Get("points"));
                return points.Count == 0 ? new List<List<ChartPoint>>() : new List<List<ChartPoint>> { TransformRing(points, matrix) };
            default:
                return new List<List<ChartPoint>>();
        }
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

    private static List<ChartPoint> RoundedRectRing(double x, double y, double width, double height, double rx, double ry) {
        var points = new List<ChartPoint>();
        AddArc(points, x + width - rx, y + ry, rx, ry, -Math.PI / 2, 0);
        AddArc(points, x + width - rx, y + height - ry, rx, ry, 0, Math.PI / 2);
        AddArc(points, x + rx, y + height - ry, rx, ry, Math.PI / 2, Math.PI);
        AddArc(points, x + rx, y + ry, rx, ry, Math.PI, Math.PI * 1.5);
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

    private static void AddArc(List<ChartPoint> points, double cx, double cy, double rx, double ry, double start, double end) {
        const int segments = 8;
        for (var i = 0; i <= segments; i++) {
            var angle = start + (end - start) * i / segments;
            points.Add(new ChartPoint(cx + Math.Cos(angle) * rx, cy + Math.Sin(angle) * ry));
        }
    }

    private static List<ChartPoint> ReadPointList(string? value) {
        var numbers = SvgRasterNumbers.ParseList(value);
        var points = new List<ChartPoint>(numbers.Count / 2);
        for (var i = 0; i + 1 < numbers.Count; i += 2) points.Add(new ChartPoint(numbers[i], numbers[i + 1]));
        return points;
    }

    private static List<ChartPoint> ClosedRing(IReadOnlyList<ChartPoint> points) {
        var closed = new List<ChartPoint>(points);
        if (closed.Count > 1 && DistanceSquared(closed[0], closed[closed.Count - 1]) > 0.000001) closed.Add(closed[0]);
        return closed;
    }

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
        public readonly ChartPoint Origin;
        public readonly ChartPoint Right;
        public readonly ChartPoint Bottom;

        public PatternFrame(ChartPoint origin, ChartPoint right, ChartPoint bottom) {
            Origin = origin;
            Right = right;
            Bottom = bottom;
        }

        public double Width => Distance(Origin, Right);
        public double Height => Distance(Origin, Bottom);
    }

    private readonly struct PatternTile {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Pixels;
        public readonly SvgRasterMatrix CanvasToTile;

        public PatternTile(int width, int height, byte[] pixels, SvgRasterMatrix canvasToTile) {
            Width = width;
            Height = height;
            Pixels = pixels;
            CanvasToTile = canvasToTile;
        }
    }

    private readonly struct SvgRasterObjectPaint {
        public SvgRasterObjectPaint(SvgRasterGradientValues.GradientBounds bounds, SvgRasterMatrix matrix) {
            Bounds = bounds;
            Matrix = matrix;
        }

        public SvgRasterGradientValues.GradientBounds Bounds { get; }
        public SvgRasterMatrix Matrix { get; }
    }
}
