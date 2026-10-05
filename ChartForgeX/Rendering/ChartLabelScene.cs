using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>One positioned text scene serialized by SVG and painted over native PNG marks.</summary>
internal sealed partial class ChartLabelScene {
    private static readonly LabelPlacementService Measurements = new();
    private readonly List<Entry> _labels = new();
    private readonly List<Mark> _marks = new();
    private readonly List<LabelObstacle> _obstacles = new();
    private readonly FontSpec _font;
    private readonly ChartRect _bounds;
    private readonly XDocument _document;
    private readonly SvgRasterDefinitions _definitions;
    [ThreadStatic] private static FontSpec? CurrentFont;

    private ChartLabelScene(XDocument document, FontSpec font, bool place = true) {
        _document = document; _font = font;
        var root = document.Root!;
        SvgRasterParser.ValidateElementDepth(root);
        var definitions = new XElement(root.Name, root.Attributes(), root.Elements().Where(e => e.Name.LocalName is "defs" or "style"));
        var raster = SvgRasterParser.FromDocumentRoot(definitions);
        _definitions = SvgRasterDefinitions.From(raster);
        var outputWidth = Number(root, "width", raster.ViewBox.Width); var outputHeight = Number(root, "height", raster.ViewBox.Height);
        var fit = Math.Min(outputWidth / raster.ViewBox.Width, outputHeight / raster.ViewBox.Height);
        var margin = (string?)root.Attribute("data-cfx-host-frame") == "true" ? 0
            : Math.Max(4, (Math.Min(outputWidth, outputHeight) * 0.01 + 2) / Math.Max(0.0001, fit));
        _bounds = new ChartRect(raster.ViewBox.X + margin, raster.ViewBox.Y + margin, Math.Max(1, raster.ViewBox.Width - margin * 2), Math.Max(1, raster.ViewBox.Height - margin * 2));
        Collect(root, SvgRasterParser.ReadStyleElement(root), SvgRasterStyle.Default, SvgRasterMatrix.Identity, new List<SvgRasterElement>());
        if (place) Place();
    }

    internal static ChartLabelScene Create(string svg, FontSpec font) {
        using var source = new StringReader(svg);
        using var reader = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return new ChartLabelScene(XDocument.Load(reader, LoadOptions.PreserveWhitespace), font);
    }
    internal string ToSvg() => _document.ToString(SaveOptions.DisableFormatting);
    internal ChartRect PlotBounds {
        get {
            var rect = _document.Descendants().First(e => e.Name.LocalName == "clipPath" && ((string?)e.Attribute("id"))?.EndsWith("-plotClip", StringComparison.Ordinal) == true)
                .Elements().First(e => e.Name.LocalName == "rect");
            return new ChartRect(Number(rect, "x"), Number(rect, "y"), Number(rect, "width"), Number(rect, "height"));
        }
    }
    internal ChartRect? PieBounds {
        get {
            var slice = _document.Descendants().FirstOrDefault(e => e.Attribute("data-cfx-pie-radius") != null);
            if (slice == null) return null;
            var radius = Number(slice, "data-cfx-pie-radius");
            return new ChartRect(Number(slice, "data-cfx-pie-center-x") - radius, Number(slice, "data-cfx-pie-center-y") - radius, radius * 2, radius * 2);
        }
    }
    internal void Paint(RgbaCanvas canvas, TrueTypeFont? explicitFont = null) {
        var layer = PaintLayer(_document.Root!);
        if (layer != null) SvgRasterRenderer.PaintLabelLayer(canvas, SvgRasterParser.FromDocumentRoot(layer), explicitFont);
    }

    private static XElement? PaintLayer(XElement element) {
        if ((string?)element.Attribute("display") == "none" || Role(element) == "topology-icon-artwork") return null;
        if (element.Name.LocalName is "defs" or "style" or "text" || (string?)element.Attribute("data-cfx-label-decoration") == "true") {
            var copy = new XElement(element);
            // Inside-mark ink is resolved after placement. Native label painting needs its current-theme literal;
            // SVG serialization keeps the paired paint so host colour properties can switch themes.
            foreach (var paint in copy.DescendantsAndSelf().Attributes().Where(a => a.Name.LocalName is "fill" or "stroke"))
                paint.Value = Themes.SvgPaint.Resolve(paint.Value, null);
            return copy;
        }
        var children = new List<XElement>();
        foreach (var child in element.Elements()) { var copy = PaintLayer(child); if (copy != null) children.Add(copy); }
        return children.Count == 0 ? null : new XElement(element.Name, element.Attributes(), children);
    }

    internal static IDisposable OpenFontScope(FontSpec font) => new FontScope(font);
    internal static double MeasureText(string text, double size) => Measurements.Measure(text, new TextStyle { Font = (CurrentFont ?? FontSpec.SystemSans()).Clone(), FontSize = size, LineHeight = 1 }).Width;
    internal static TextMetrics MeasureText(string text, TextStyle style) => Measurements.Measure(text, style);
    internal static double MeasureText(string text, double size, TextStyleOverride style, int weight) => Measurements.Measure(text, ResolveTextStyle(size, style, weight)).Width;
    internal static TextStyle ResolveTextStyle(double size, TextStyleOverride style, int weight) {
        var font = (CurrentFont ?? FontSpec.SystemSans()).Clone();
        font.Family = style.FontFamily ?? font.Family; font.Weight = Math.Max(100, Math.Min(900, style.ResolveFontWeight(weight))); font.Italic = style.Italic;
        font.Variations = style.Variations ?? FontVariationSettings.Default;
        return new TextStyle { Font = font, FontSize = size, LineHeight = 1, OpenTypeLanguageTag = style.OpenTypeLanguageTag == "normal" ? null : style.OpenTypeLanguageTag };
    }

    private void Collect(XElement element, SvgRasterElement raster, SvgRasterStyle parentStyle, SvgRasterMatrix parentMatrix, List<SvgRasterElement> ancestors, ChartRect? clip = null) {
        // Imported artwork is an atomic asset, already painted by the native mark renderer.
        // Its internal captions must not be relocated or painted a second time as chart labels.
        if (element.Name.LocalName is "defs" or "style" or "title" or "desc" || Role(element) == "topology-icon-artwork") return;
        var style = SvgRasterStyle.Resolve(parentStyle, raster, _definitions.StyleSheet, ancestors);
        if (!style.Displayed || !style.VisibilityVisible || style.Opacity == 0) return;
        var matrix = parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(raster.Get("transform")));
        if (element.Name.LocalName == "svg" && element.Parent != null && element.Attribute("viewBox") is { } viewBox) {
            var view = SvgRasterViewBox.Parse(viewBox.Value);
            var width = Number(element, "width", view.Width); var height = Number(element, "height", view.Height);
            var scale = Math.Min(width / view.Width, height / view.Height);
            var align = (string?)element.Attribute("preserveAspectRatio") ?? "xMidYMid meet";
            var x = Number(element, "x") + (align.Contains("xMin") ? 0 : (width - view.Width * scale) / 2);
            var y = Number(element, "y") + (align.Contains("YMin") ? 0 : (height - view.Height * scale) / 2);
            matrix = matrix.Multiply(SvgRasterMatrix.Translate(x, y)).Multiply(SvgRasterMatrix.Scale(scale, scale)).Multiply(SvgRasterMatrix.Translate(-view.X, -view.Y));
        }
        clip = ResolveMarkClip(style.ClipPath, matrix, clip);
        var role = Role(element);
        var isLegendItem = role is "legend-item" or "slice-legend-item";
        var isGroup = isLegendItem || role == "topology-edge-label";
        if (element.Name.LocalName == "text" || isGroup) {
            var textElements = isGroup ? element.Elements().Where(e => e.Name.LocalName == "text").ToArray() : new[] { element };
            if (textElements.Length != 0) {
                var box = default(ChartRect);
                var first = true;
                foreach (var textElement in textElements) {
                    var textRaster = isGroup ? SvgRasterParser.ReadStyleElement(textElement) : raster;
                    var textStyle = isGroup ? SvgRasterStyle.Resolve(style, textRaster, _definitions.StyleSheet, ancestors) : style;
                    var textBox = TextBox(textElement, textStyle, matrix);
                    box = first ? textBox : Union(box, textBox); first = false;
                }
                var contentBox = box;
                var value = isGroup ? string.Join(" ", textElements.Select(e => e.Value)) : element.Value;
                var decorations = isGroup ? element.Elements().Where(e => e.Name.LocalName != "text").ToList() : Decorations(element, false);
                foreach (var decoration in decorations) {
                    decoration.SetAttributeValue("data-cfx-label-decoration", "true");
                    var decorationBox = IsLeader(decoration) ? null : Shape(decoration, matrix, null)?.Bounds;
                    if (decorationBox.HasValue) box = Union(box, decorationBox.Value);
                }
                var resolved = TextStyle(style);
                _labels.Add(new Entry(element, value, resolved, box, contentBox, matrix, parentMatrix, decorations, isLegendItem, clip, style.Fill.Color));
            }
            return;
        }
        var markRole = role.Length == 0 && element.Parent is { } owner && Role(owner) == "topology-node-body" ? "topology-node-body" : role;
        if (IsMark(markRole)) {
            var shape = Shape(element, matrix, style);
            if (shape != null && clip.HasValue) shape = shape.WithClip(clip.Value);
            if (shape != null && shape.Bounds.Width > 0 && shape.Bounds.Height > 0) {
                var id = "m" + _marks.Count.ToString(CultureInfo.InvariantCulture);
                _marks.Add(new Mark(element, id, shape, markRole));
                element.SetAttributeValue("data-cfx-mark-key", id);
                _obstacles.Add(new LabelObstacle(id, shape));
            }
        }
        ancestors.Add(raster);
        foreach (var child in element.Elements()) Collect(child, SvgRasterParser.ReadStyleElement(child), style, matrix, ancestors, clip);
        ancestors.RemoveAt(ancestors.Count - 1);
    }

    private void Place() {
        var requests = new List<LabelPlacementRequest>(_labels.Count);
        foreach (var label in _labels) {
            var role = LabelRole(label.Element);
            var associated = AssociatedMark(label);
            label.Associated = associated;
            var candidates = Candidates(label, role);
            var request = new LabelPlacementRequest(label.Text, new ChartPoint(label.Box.X, label.Box.Y), label.Style, candidates, Priority(role)) {
                AssociatedMarkId = associated?.Id,
                Bounds = LabelBounds(label, associated, role),
                HasLeaderLine = CanMove(role) && !label.IsLegendItem,
                Fallback = label.IsLegendItem || label.Element.HasElements || !CanMove(role) ? LabelFallbackRule.Drop : LabelFallbackRule.EllipsisThenDrop,
                MeasuredSize = new TextMetrics(label.Box.Width, label.Box.Height, label.Box.Height),
                DecorationSize = new TextMetrics(Math.Max(0, label.Box.Width - Measurements.Measure(label.Text, label.Style).Width), Math.Max(0, label.Box.Height - Measurements.Measure(label.Text, label.Style).Height), 0)
            };
            requests.Add(request);
        }
        var results = Measurements.Place(requests, _bounds, _obstacles);
        for (var i = 0; i < results.Count; i++) Apply(_labels[i], results[i]);
        _document.Root!.SetAttributeValue("data-cfx-label-layout", "measured");
        _document.Root.SetAttributeValue("data-cfx-label-count", results.Count);
        _document.Root.SetAttributeValue("data-cfx-label-dropped", results.Count(label => label.IsDropped));
    }

    private ChartRect? LabelBounds(Entry label, Mark? associated, string role) {
        var bounds = role is "funnel-label" or "funnel-value" ? FunnelLabelBounds(label, associated) : null;
        if (!label.Clip.HasValue) return bounds;
        if (!bounds.HasValue) return label.Clip;
        var clip = label.Clip.Value;
        return new ChartRect(Math.Max(bounds.Value.Left, clip.Left), Math.Max(bounds.Value.Top, clip.Top),
            Math.Max(0, Math.Min(bounds.Value.Right, clip.Right) - Math.Max(bounds.Value.Left, clip.Left)),
            Math.Max(0, Math.Min(bounds.Value.Bottom, clip.Bottom) - Math.Max(bounds.Value.Top, clip.Top)));
    }

    private ChartRect? FunnelLabelBounds(Entry label, Mark? associated) {
        if (associated == null) return null;
        var stage = associated.Shape.Bounds;
        var outside = label.Element.AncestorsAndSelf().Any(e => (string?)e.Attribute("data-cfx-label-lane") == "outside");
        return outside
            ? new ChartRect(stage.Right, stage.Top, Math.Max(1, _bounds.Right - stage.Right), stage.Height)
            : stage;
    }

    internal static string Role(XElement element) => (string?)element.Attribute("data-cfx-role") ?? "";
    private static double Number(XElement element, string name, double fallback = 0) => double.TryParse((string?)element.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static ChartRect Union(ChartRect a, ChartRect b) => new(Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top), Math.Max(a.Right, b.Right) - Math.Min(a.Left, b.Left), Math.Max(a.Bottom, b.Bottom) - Math.Min(a.Top, b.Top));

    private sealed class Entry {
        internal Entry(XElement element, string text, TextStyle style, ChartRect box, ChartRect contentBox, SvgRasterMatrix matrix, SvgRasterMatrix parentMatrix, List<XElement> decorations, bool legend, ChartRect? clip, ChartColor? ink) {
            Element = element; Text = text; Style = style; Box = box; ContentBox = contentBox; Matrix = matrix; ParentMatrix = parentMatrix; Decorations = decorations; IsLegendItem = legend; Clip = clip; Ink = ink;
        }
        internal XElement Element { get; }
        internal string Text { get; }
        internal TextStyle Style { get; }
        internal ChartRect Box { get; }
        internal ChartRect ContentBox { get; }
        internal SvgRasterMatrix Matrix { get; }
        internal SvgRasterMatrix ParentMatrix { get; }
        internal List<XElement> Decorations { get; }
        internal bool IsLegendItem { get; }
        internal ChartRect? Clip { get; }
        internal ChartColor? Ink { get; }
        internal Mark? Associated { get; set; }
    }
    private sealed class FontScope : IDisposable {
        private readonly FontSpec? _previous = CurrentFont;
        internal FontScope(FontSpec font) { CurrentFont = font; }
        public void Dispose() { CurrentFont = _previous; }
    }
}
