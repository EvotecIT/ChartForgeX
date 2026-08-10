using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static bool TryObjectBounds(SvgRasterElement element, SvgRasterStyle style, IReadOnlyList<SvgRasterElement> targetAncestors, SvgRasterDefinitions definitions, SvgRasterViewport viewport, out PixelBounds bounds) {
        var points = new List<ChartPoint>();
        var ancestors = new List<SvgRasterElement>(targetAncestors);
        if (ancestors.Count > 0 && ReferenceEquals(ancestors[ancestors.Count - 1], element)) ancestors.RemoveAt(ancestors.Count - 1);
        if (!CollectObjectPoints(element, style, ancestors, SvgRasterMatrix.Identity, definitions, viewport, 0, applyElementTransform: false, resolveStyle: false, points) || points.Count == 0) {
            bounds = default;
            return false;
        }
        var left = double.PositiveInfinity;
        var top = double.PositiveInfinity;
        var right = double.NegativeInfinity;
        var bottom = double.NegativeInfinity;
        foreach (var point in points) IncludePoint(point, ref left, ref top, ref right, ref bottom);
        bounds = new PixelBounds(left, top, right - left, bottom - top);
        return !double.IsInfinity(left);
    }

    private static bool CollectObjectPoints(SvgRasterElement element, SvgRasterStyle parentStyle, List<SvgRasterElement> ancestors, SvgRasterMatrix parentMatrix, SvgRasterDefinitions definitions, SvgRasterViewport viewport, int referenceDepth, bool applyElementTransform, bool resolveStyle, List<ChartPoint> points) {
        if (IsDefinitionElement(element.Name) && !IsSymbolElement(element)) return true;
        var style = resolveStyle ? SvgRasterStyle.Resolve(parentStyle, element, definitions.StyleSheet, ancestors) : parentStyle;
        if (!style.Displayed) return true;
        var matrix = applyElementTransform ? parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(element.Get("transform"))) : parentMatrix;
        if (applyElementTransform && string.Equals(element.Name, "svg", StringComparison.Ordinal)) {
            var nested = ResolveNestedSvgViewport(element, viewport);
            if (nested.Width <= 0 || nested.Height <= 0) return true;
            matrix = ApplyNestedSvgViewport(element, matrix, nested);
            viewport = nested.UserViewport;
        }
        var contours = ClipContours(element, matrix, viewport);
        if (contours.Count > 0) {
            foreach (var contour in contours) points.AddRange(contour);
            return true;
        }
        switch (element.Name) {
            case "line":
                points.Add(matrix.Transform(new ChartPoint(HorizontalLength(element, "x1", viewport), VerticalLength(element, "y1", viewport))));
                points.Add(matrix.Transform(new ChartPoint(HorizontalLength(element, "x2", viewport), VerticalLength(element, "y2", viewport))));
                return true;
            case "polyline":
                points.AddRange(TransformRing(ReadPointList(element.Get("points")), matrix));
                return true;
            case "image":
                var width = HorizontalLength(element, "width", viewport);
                var height = VerticalLength(element, "height", viewport);
                if (width > 0 && height > 0) points.AddRange(TransformRing(RectRing(HorizontalLength(element, "x", viewport), VerticalLength(element, "y", viewport), width, height), matrix));
                return true;
            case "use":
                if (referenceDepth >= 8 || !definitions.TryGetElement(HrefReferenceId(element), out var referenced)) return false;
                var useMatrix = matrix.Multiply(SvgRasterMatrix.Translate(HorizontalLength(element, "x", viewport), VerticalLength(element, "y", viewport)));
                var symbolWidth = HorizontalLength(element, "width", viewport, viewport.Width);
                var symbolHeight = VerticalLength(element, "height", viewport, viewport.Height);
                var referencedViewport = IsSymbolElement(referenced) ? new SvgRasterViewport(symbolWidth, symbolHeight) : viewport;
                if (IsSymbolElement(referenced) && (symbolWidth <= 0 || symbolHeight <= 0)) return true;
                if (IsSymbolElement(referenced) && !string.IsNullOrWhiteSpace(referenced.Get("viewBox"))) {
                    var viewBox = SvgRasterViewBox.Parse(referenced.Get("viewBox")!);
                    if (symbolWidth <= 0 || symbolHeight <= 0) return true;
                    useMatrix = useMatrix.Multiply(SvgRasterMatrix.FromFit(viewBox, (int)Math.Round(symbolWidth), (int)Math.Round(symbolHeight), referenced.Get("preserveAspectRatio")));
                    referencedViewport = new SvgRasterViewport(viewBox.Width, viewBox.Height);
                }
                return CollectObjectPoints(referenced, style, ancestors, useMatrix, definitions, referencedViewport, referenceDepth + 1, applyElementTransform: true, resolveStyle: true, points);
            case "g":
            case "svg":
            case "symbol":
                ancestors.Add(element);
                foreach (var child in element.Children) {
                    if (!CollectObjectPoints(child, style, ancestors, matrix, definitions, viewport, referenceDepth, applyElementTransform: true, resolveStyle: true, points)) {
                        ancestors.RemoveAt(ancestors.Count - 1);
                        return false;
                    }
                }
                ancestors.RemoveAt(ancestors.Count - 1);
                return true;
            default:
                return false;
        }
    }

    private static SvgRasterElement ResolveObjectBoundingBoxContent(SvgRasterElement element, SvgRasterDefinitions definitions, int referenceDepth, bool normalizePercentages = true) {
        var attributes = element.CopyAttributes();
        if (normalizePercentages) {
            foreach (var name in new List<string>(attributes.Keys)) {
                if (IsObjectBoundingBoxGeometryAttribute(name)) attributes[name] = PercentageToUnit(attributes[name]);
            }
        }
        if (string.Equals(element.Name, "use", StringComparison.Ordinal) && referenceDepth < 8 && definitions.TryGetElement(HrefReferenceId(element), out var referenced)) {
            var x = SvgRasterNumbers.TryParse(attributes.TryGetValue("x", out var xValue) ? xValue : null, out var parsedX) ? parsedX : 0;
            var y = SvgRasterNumbers.TryParse(attributes.TryGetValue("y", out var yValue) ? yValue : null, out var parsedY) ? parsedY : 0;
            var width = SvgRasterNumbers.TryParse(attributes.TryGetValue("width", out var widthValue) ? widthValue : null, out var parsedWidth) ? parsedWidth : double.NaN;
            var height = SvgRasterNumbers.TryParse(attributes.TryGetValue("height", out var heightValue) ? heightValue : null, out var parsedHeight) ? parsedHeight : double.NaN;
            var transform = attributes.TryGetValue("transform", out var declaredTransform) ? declaredTransform + " " : string.Empty;
            attributes["transform"] = transform + "translate(" + x.ToString("0.################", CultureInfo.InvariantCulture) + " " + y.ToString("0.################", CultureInfo.InvariantCulture) + ")";
            attributes.Remove("href");
            attributes.Remove("xlink:href");
            attributes.Remove("x");
            attributes.Remove("y");
            attributes.Remove("width");
            attributes.Remove("height");
            var referencedContent = ResolveObjectBoundingBoxContent(referenced, definitions, referenceDepth + 1, normalizePercentages);
            if (IsSymbolElement(referenced)) {
                var symbolAttributes = referencedContent.CopyAttributes();
                symbolAttributes.Remove("id");
                var symbolViewBox = string.IsNullOrWhiteSpace(referenced.Get("viewBox")) ? (SvgRasterViewBox?)null : SvgRasterViewBox.Parse(referenced.Get("viewBox")!);
                var symbolWidth = double.IsNaN(width) ? symbolViewBox?.Width ?? 1 : width;
                var symbolHeight = double.IsNaN(height) ? symbolViewBox?.Height ?? 1 : height;
                symbolAttributes["x"] = "0";
                symbolAttributes["y"] = "0";
                symbolAttributes["width"] = symbolWidth.ToString("0.################", CultureInfo.InvariantCulture);
                symbolAttributes["height"] = symbolHeight.ToString("0.################", CultureInfo.InvariantCulture);
                referencedContent = new SvgRasterElement("svg", symbolAttributes, referencedContent.Children, string.Empty);
            }
            return new SvgRasterElement("g", attributes, new[] { referencedContent }, element.Text);
        }
        var children = new List<SvgRasterElement>(element.Children.Count);
        var content = new List<SvgRasterContent>(element.Content.Count);
        var normalizeChildren = normalizePercentages && !string.Equals(element.Name, "svg", StringComparison.Ordinal) && !IsSymbolElement(element);
        foreach (var item in element.Content) {
            if (item.Text != null) {
                content.Add(SvgRasterContent.FromText(item.Text));
            } else if (item.Element != null) {
                var child = ResolveObjectBoundingBoxContent(item.Element, definitions, referenceDepth, normalizeChildren);
                children.Add(child);
                content.Add(SvgRasterContent.FromElement(child));
            }
        }
        if (children.Count == 0 && element.Children.Count > 0) {
            foreach (var childElement in element.Children) children.Add(ResolveObjectBoundingBoxContent(childElement, definitions, referenceDepth, normalizeChildren));
        }
        return new SvgRasterElement(element.Name, attributes, children, element.Text, content);
    }

    private static bool IsObjectBoundingBoxGeometryAttribute(string name) => name is
        "x" or "y" or "x1" or "y1" or "x2" or "y2" or "cx" or "cy" or "r" or "rx" or "ry" or "width" or "height" or "dx" or "dy";

    private static string PercentageToUnit(string value) {
        var trimmed = value.Trim();
        if (!trimmed.EndsWith("%", StringComparison.Ordinal)) return value;
        return double.TryParse(trimmed.Substring(0, trimmed.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var percentage)
            ? (percentage / 100.0).ToString("0.################", CultureInfo.InvariantCulture)
            : value;
    }
}
