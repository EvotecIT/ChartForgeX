using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed class SvgRasterDefinitions {
    private readonly Dictionary<string, SvgRasterElement> _elements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgRasterElement> _gradientElements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgRasterElement> _patternElements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgRasterClipPath> _clipPaths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgRasterMask> _masks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgRasterLinearGradient> _linearGradients = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgRasterRadialGradient> _radialGradients = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgRasterPattern> _patterns = new(StringComparer.Ordinal);

    public static SvgRasterDefinitions From(SvgRasterDocument document) {
        var definitions = new SvgRasterDefinitions();
        foreach (var child in document.Children) definitions.Collect(child);
        return definitions;
    }

    public bool TryGetLinearGradient(string? id, out SvgRasterLinearGradient gradient) {
        if (id != null && ResolveLinearGradient(id, new HashSet<string>(StringComparer.Ordinal), out gradient!)) return true;
        gradient = null!;
        return false;
    }

    public bool TryGetRadialGradient(string? id, out SvgRasterRadialGradient gradient) {
        if (id != null && ResolveRadialGradient(id, new HashSet<string>(StringComparer.Ordinal), out gradient!)) return true;
        gradient = null!;
        return false;
    }

    public bool TryGetPattern(string? id, out SvgRasterPattern pattern) {
        if (id != null && ResolvePattern(id, new HashSet<string>(StringComparer.Ordinal), out pattern!)) return true;
        pattern = null!;
        return false;
    }

    public bool TryGetElement(string? id, out SvgRasterElement element) {
        if (id != null && _elements.TryGetValue(id, out element!)) return true;
        element = null!;
        return false;
    }

    public bool TryGetClipPath(string? id, out SvgRasterClipPath clipPath) {
        if (id != null && _clipPaths.TryGetValue(id, out clipPath!)) return true;
        clipPath = null!;
        return false;
    }

    public bool TryGetMask(string? id, out SvgRasterMask mask) {
        if (id != null && _masks.TryGetValue(id, out mask!)) return true;
        mask = null!;
        return false;
    }

    private bool ResolveLinearGradient(string id, HashSet<string> visiting, out SvgRasterLinearGradient gradient) {
        if (_linearGradients.TryGetValue(id, out gradient!)) return true;
        if (!visiting.Add(id) || !_gradientElements.TryGetValue(id, out var element) || !string.Equals(element.Name, "linearGradient", StringComparison.Ordinal)) {
            gradient = null!;
            return false;
        }

        SvgRasterLinearGradient? inherited = null;
        var referenceId = ReferenceId(element);
        if (referenceId != null) ResolveLinearGradient(referenceId, visiting, out inherited!);
        gradient = SvgRasterLinearGradient.From(element, inherited);
        _linearGradients[id] = gradient;
        visiting.Remove(id);
        return true;
    }

    private bool ResolveRadialGradient(string id, HashSet<string> visiting, out SvgRasterRadialGradient gradient) {
        if (_radialGradients.TryGetValue(id, out gradient!)) return true;
        if (!visiting.Add(id) || !_gradientElements.TryGetValue(id, out var element) || !string.Equals(element.Name, "radialGradient", StringComparison.Ordinal)) {
            gradient = null!;
            return false;
        }

        SvgRasterRadialGradient? inherited = null;
        var referenceId = ReferenceId(element);
        if (referenceId != null) ResolveRadialGradient(referenceId, visiting, out inherited!);
        gradient = SvgRasterRadialGradient.From(element, inherited);
        _radialGradients[id] = gradient;
        visiting.Remove(id);
        return true;
    }

    private bool ResolvePattern(string id, HashSet<string> visiting, out SvgRasterPattern pattern) {
        if (_patterns.TryGetValue(id, out pattern!)) return true;
        if (!visiting.Add(id) || !_patternElements.TryGetValue(id, out var element)) {
            pattern = null!;
            return false;
        }

        SvgRasterPattern? inherited = null;
        var referenceId = ReferenceId(element);
        if (referenceId != null) ResolvePattern(referenceId, visiting, out inherited!);
        pattern = SvgRasterPattern.From(element, inherited);
        _patterns[id] = pattern;
        visiting.Remove(id);
        return true;
    }

    private void Collect(SvgRasterElement element) {
        if (IsReusableElement(element) && element.TryGet("id", out var reusableId) && !string.IsNullOrWhiteSpace(reusableId)) {
            _elements[reusableId] = element;
        }

        if ((string.Equals(element.Name, "linearGradient", StringComparison.Ordinal) || string.Equals(element.Name, "radialGradient", StringComparison.Ordinal)) && element.TryGet("id", out var id) && !string.IsNullOrWhiteSpace(id)) {
            _gradientElements[id] = element;
        }

        if (string.Equals(element.Name, "pattern", StringComparison.Ordinal) && element.TryGet("id", out var patternId) && !string.IsNullOrWhiteSpace(patternId)) {
            _patternElements[patternId] = element;
        }

        if (string.Equals(element.Name, "clipPath", StringComparison.Ordinal) && element.TryGet("id", out var clipId) && !string.IsNullOrWhiteSpace(clipId)) {
            _clipPaths[clipId] = new SvgRasterClipPath(element);
        }

        if (string.Equals(element.Name, "mask", StringComparison.Ordinal) && element.TryGet("id", out var maskId) && !string.IsNullOrWhiteSpace(maskId)) {
            _masks[maskId] = new SvgRasterMask(element);
        }

        foreach (var child in element.Children) Collect(child);
    }

    private static string? ReferenceId(SvgRasterElement element) {
        var href = element.Get("href");
        if (string.IsNullOrWhiteSpace(href) || !href!.Trim().StartsWith("#", StringComparison.Ordinal)) return null;
        return href.Trim().Substring(1);
    }

    private static bool IsReusableElement(SvgRasterElement element) =>
        !string.Equals(element.Name, "defs", StringComparison.Ordinal) &&
        !string.Equals(element.Name, "userDefs", StringComparison.Ordinal) &&
        !string.Equals(element.Name, "linearGradient", StringComparison.Ordinal) &&
        !string.Equals(element.Name, "radialGradient", StringComparison.Ordinal) &&
        !string.Equals(element.Name, "pattern", StringComparison.Ordinal) &&
        !string.Equals(element.Name, "clipPath", StringComparison.Ordinal) &&
        !string.Equals(element.Name, "mask", StringComparison.Ordinal) &&
        !string.Equals(element.Name, "stop", StringComparison.Ordinal) &&
        !string.Equals(element.Name, "title", StringComparison.Ordinal) &&
        !string.Equals(element.Name, "desc", StringComparison.Ordinal);
}

internal sealed class SvgRasterClipPath {
    public SvgRasterClipPath(SvgRasterElement element) {
        Element = element;
    }

    public SvgRasterElement Element { get; }
}

internal sealed class SvgRasterMask {
    public SvgRasterMask(SvgRasterElement element) {
        Element = element;
    }

    public SvgRasterElement Element { get; }
}

internal sealed class SvgRasterPattern {
    private SvgRasterPattern(double x, double y, double width, double height, bool userSpaceOnUse, bool contentUserSpaceOnUse, SvgRasterMatrix transform, string? viewBox, string? preserveAspectRatio, IReadOnlyList<SvgRasterElement> children) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        UserSpaceOnUse = userSpaceOnUse;
        ContentUserSpaceOnUse = contentUserSpaceOnUse;
        Transform = transform;
        ViewBox = viewBox;
        PreserveAspectRatio = preserveAspectRatio;
        Children = children;
    }

    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public bool UserSpaceOnUse { get; }
    public bool ContentUserSpaceOnUse { get; }
    public SvgRasterMatrix Transform { get; }
    public string? ViewBox { get; }
    public string? PreserveAspectRatio { get; }
    public IReadOnlyList<SvgRasterElement> Children { get; }

    public static SvgRasterPattern From(SvgRasterElement element, SvgRasterPattern? inherited) {
        var patternUnits = element.Get("patternUnits");
        var contentUnits = element.Get("patternContentUnits");
        var children = element.Children.Count == 0 && inherited != null ? inherited.Children : element.Children;
        return new SvgRasterPattern(
            SvgRasterGradientValues.ParseCoordinate(element.Get("x"), inherited?.X ?? 0),
            SvgRasterGradientValues.ParseCoordinate(element.Get("y"), inherited?.Y ?? 0),
            SvgRasterGradientValues.ParseCoordinate(element.Get("width"), inherited?.Width ?? 0),
            SvgRasterGradientValues.ParseCoordinate(element.Get("height"), inherited?.Height ?? 0),
            string.Equals(patternUnits, "userSpaceOnUse", StringComparison.Ordinal) || (patternUnits == null && inherited?.UserSpaceOnUse == true),
            !string.Equals(contentUnits, "objectBoundingBox", StringComparison.Ordinal) && (contentUnits != null || inherited?.ContentUserSpaceOnUse != false),
            element.Get("patternTransform") == null ? inherited?.Transform ?? SvgRasterMatrix.Identity : SvgRasterMatrix.ParseTransform(element.Get("patternTransform")),
            element.Get("viewBox") ?? inherited?.ViewBox,
            element.Get("preserveAspectRatio") ?? inherited?.PreserveAspectRatio,
            children);
    }
}

internal sealed class SvgRasterLinearGradient {
    private SvgRasterLinearGradient(double x1, double y1, double x2, double y2, bool userSpaceOnUse, SvgRasterMatrix transform, RasterGradientSpreadMethod spreadMethod, IReadOnlyList<RasterGradientStop> stops) {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        UserSpaceOnUse = userSpaceOnUse;
        Transform = transform;
        SpreadMethod = spreadMethod;
        Stops = stops;
    }

    public double X1 { get; }
    public double Y1 { get; }
    public double X2 { get; }
    public double Y2 { get; }
    public bool UserSpaceOnUse { get; }
    public SvgRasterMatrix Transform { get; }
    public RasterGradientSpreadMethod SpreadMethod { get; }
    public IReadOnlyList<RasterGradientStop> Stops { get; }

    public static SvgRasterLinearGradient From(SvgRasterElement element, SvgRasterLinearGradient? inherited) {
        var userSpace = string.Equals(element.Get("gradientUnits"), "userSpaceOnUse", StringComparison.Ordinal) || (element.Get("gradientUnits") == null && inherited?.UserSpaceOnUse == true);
        var stops = SvgRasterGradientValues.ReadStops(element);
        if (stops.Count == 0) stops = inherited?.Stops ?? SvgRasterGradientValues.BlackStops;
        var transform = element.Get("gradientTransform") == null ? inherited?.Transform ?? SvgRasterMatrix.Identity : SvgRasterMatrix.ParseTransform(element.Get("gradientTransform"));
        return new SvgRasterLinearGradient(
            SvgRasterGradientValues.ParseCoordinate(element.Get("x1"), inherited?.X1 ?? 0),
            SvgRasterGradientValues.ParseCoordinate(element.Get("y1"), inherited?.Y1 ?? 0),
            SvgRasterGradientValues.ParseCoordinate(element.Get("x2"), inherited?.X2 ?? (userSpace ? 0 : 1)),
            SvgRasterGradientValues.ParseCoordinate(element.Get("y2"), inherited?.Y2 ?? 0),
            userSpace,
            transform,
            SvgRasterGradientValues.ParseSpreadMethod(element.Get("spreadMethod"), inherited?.SpreadMethod ?? RasterGradientSpreadMethod.Pad),
            stops);
    }

    public void Endpoints(IReadOnlyList<List<ChartPoint>> contours, SvgRasterMatrix matrix, out ChartPoint start, out ChartPoint end) {
        if (UserSpaceOnUse) {
            var transformed = matrix.Multiply(Transform);
            start = transformed.Transform(new ChartPoint(X1, Y1));
            end = transformed.Transform(new ChartPoint(X2, Y2));
            return;
        }

        var bounds = SvgRasterGradientValues.Bounds(contours);
        start = SvgRasterGradientValues.MapObjectPoint(Transform.Transform(new ChartPoint(X1, Y1)), bounds);
        end = SvgRasterGradientValues.MapObjectPoint(Transform.Transform(new ChartPoint(X2, Y2)), bounds);
    }
}

internal sealed class SvgRasterRadialGradient {
    private SvgRasterRadialGradient(double cx, double cy, double r, bool userSpaceOnUse, SvgRasterMatrix transform, RasterGradientSpreadMethod spreadMethod, IReadOnlyList<RasterGradientStop> stops) {
        Cx = cx;
        Cy = cy;
        Radius = r;
        UserSpaceOnUse = userSpaceOnUse;
        Transform = transform;
        SpreadMethod = spreadMethod;
        Stops = stops;
    }

    public double Cx { get; }
    public double Cy { get; }
    public double Radius { get; }
    public bool UserSpaceOnUse { get; }
    public SvgRasterMatrix Transform { get; }
    public RasterGradientSpreadMethod SpreadMethod { get; }
    public IReadOnlyList<RasterGradientStop> Stops { get; }

    public static SvgRasterRadialGradient From(SvgRasterElement element, SvgRasterRadialGradient? inherited) {
        var userSpace = string.Equals(element.Get("gradientUnits"), "userSpaceOnUse", StringComparison.Ordinal) || (element.Get("gradientUnits") == null && inherited?.UserSpaceOnUse == true);
        var stops = SvgRasterGradientValues.ReadStops(element);
        if (stops.Count == 0) stops = inherited?.Stops ?? SvgRasterGradientValues.BlackStops;
        var transform = element.Get("gradientTransform") == null ? inherited?.Transform ?? SvgRasterMatrix.Identity : SvgRasterMatrix.ParseTransform(element.Get("gradientTransform"));
        return new SvgRasterRadialGradient(
            SvgRasterGradientValues.ParseCoordinate(element.Get("cx"), inherited?.Cx ?? 0.5),
            SvgRasterGradientValues.ParseCoordinate(element.Get("cy"), inherited?.Cy ?? 0.5),
            SvgRasterGradientValues.ParseCoordinate(element.Get("r"), inherited?.Radius ?? 0.5),
            userSpace,
            transform,
            SvgRasterGradientValues.ParseSpreadMethod(element.Get("spreadMethod"), inherited?.SpreadMethod ?? RasterGradientSpreadMethod.Pad),
            stops);
    }

    public void Axes(IReadOnlyList<List<ChartPoint>> contours, SvgRasterMatrix matrix, out ChartPoint center, out ChartPoint radiusX, out ChartPoint radiusY) {
        if (UserSpaceOnUse) {
            var transformed = matrix.Multiply(Transform);
            center = transformed.Transform(new ChartPoint(Cx, Cy));
            radiusX = transformed.Transform(new ChartPoint(Cx + Math.Max(0.000001, Radius), Cy));
            radiusY = transformed.Transform(new ChartPoint(Cx, Cy + Math.Max(0.000001, Radius)));
            return;
        }

        var bounds = SvgRasterGradientValues.Bounds(contours);
        var radius = Math.Max(0.000001, Radius);
        center = SvgRasterGradientValues.MapObjectPoint(Transform.Transform(new ChartPoint(Cx, Cy)), bounds);
        radiusX = SvgRasterGradientValues.MapObjectPoint(Transform.Transform(new ChartPoint(Cx + radius, Cy)), bounds);
        radiusY = SvgRasterGradientValues.MapObjectPoint(Transform.Transform(new ChartPoint(Cx, Cy + radius)), bounds);
    }
}

internal static class SvgRasterGradientValues {
    public static readonly IReadOnlyList<RasterGradientStop> BlackStops = new[] { new RasterGradientStop(0, ChartColor.Black), new RasterGradientStop(1, ChartColor.Black) };

    public static IReadOnlyList<RasterGradientStop> ReadStops(SvgRasterElement element) {
        return element.Children
            .Where(child => string.Equals(child.Name, "stop", StringComparison.Ordinal))
            .Select(ReadStop)
            .OrderBy(stop => stop.Offset)
            .ToArray();
    }

    public static double ParseCoordinate(string? value, double fallback) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var trimmed = value!.Trim();
        var percent = trimmed.EndsWith("%", StringComparison.Ordinal);
        if (percent) trimmed = trimmed.Substring(0, trimmed.Length - 1);
        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return fallback;
        return percent ? parsed / 100.0 : parsed;
    }

    public static RasterGradientSpreadMethod ParseSpreadMethod(string? value, RasterGradientSpreadMethod fallback) {
        if (string.Equals(value, "reflect", StringComparison.Ordinal)) return RasterGradientSpreadMethod.Reflect;
        if (string.Equals(value, "repeat", StringComparison.Ordinal)) return RasterGradientSpreadMethod.Repeat;
        if (string.Equals(value, "pad", StringComparison.Ordinal)) return RasterGradientSpreadMethod.Pad;
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return fallback;
    }

    public static ChartPoint MapObjectPoint(ChartPoint point, GradientBounds bounds) =>
        new(bounds.Left + bounds.Width * point.X, bounds.Top + bounds.Height * point.Y);

    public static GradientBounds Bounds(IReadOnlyList<List<ChartPoint>> contours) {
        var left = double.PositiveInfinity;
        var top = double.PositiveInfinity;
        var right = double.NegativeInfinity;
        var bottom = double.NegativeInfinity;
        foreach (var contour in contours) foreach (var point in contour) {
            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
        }

        if (double.IsInfinity(left) || double.IsInfinity(top)) return new GradientBounds(0, 0, 1, 1);
        return new GradientBounds(left, top, Math.Max(0.000001, right - left), Math.Max(0.000001, bottom - top));
    }

    private static RasterGradientStop ReadStop(SvgRasterElement element) {
        var stopColor = element.Get("stop-color");
        var stopOpacity = element.Get("stop-opacity");
        var inline = element.Get("style");
        if (!string.IsNullOrWhiteSpace(inline)) {
            foreach (var declaration in ChartForgeX.Svg.SvgStyleDeclarationList.Parse(inline!).Declarations) {
                if (string.Equals(declaration.Name, "stop-color", StringComparison.Ordinal)) stopColor = declaration.Value;
                else if (string.Equals(declaration.Name, "stop-opacity", StringComparison.Ordinal)) stopOpacity = declaration.Value;
            }
        }

        if (!ChartColor.TryParse(stopColor, out var color)) color = ChartColor.Black;
        return new RasterGradientStop(Math.Max(0, Math.Min(1, ParseCoordinate(element.Get("offset"), 0))), WithOpacity(color, ParseOpacity(stopOpacity, 1)));
    }

    private static double ParseOpacity(string? value, double fallback) =>
        value == null ? fallback : Math.Max(0, Math.Min(1, ParseCoordinate(value, fallback)));

    private static ChartColor WithOpacity(ChartColor color, double opacity) =>
        ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity));

    public readonly struct GradientBounds {
        public readonly double Left;
        public readonly double Top;
        public readonly double Width;
        public readonly double Height;

        public GradientBounds(double left, double top, double width, double height) {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }
}
