using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Svg;
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Rendering;

/// <summary>Captures table paint decisions for native scenes and the incumbent factual SVG backend.</summary>
internal sealed partial class NativeTablePaintPlan {
    private readonly VisualSceneBuilder _builder;
    private readonly List<SvgElement> _svg = new();
    private readonly Dictionary<int, VisualSceneTextFace> _faces = new();
    private readonly NativeTableData _data;
    private VisualScene _scene = null!;
    private NativeTablePaintPlan(NativeTableData data) {
        ChartGuards.Finite(data.Scale, nameof(data.Scale));
        if (data.Scale <= 0) throw new ArgumentOutOfRangeException(nameof(data.Scale));
        if (data.AbsoluteDataLabelSize is double absolute) {
            ChartGuards.Finite(absolute, nameof(data.AbsoluteDataLabelSize));
            if (absolute <= 0) throw new ArgumentOutOfRangeException(nameof(data.AbsoluteDataLabelSize));
        }
        _data = data;
        _builder = new VisualSceneBuilder(new VisualSize(Math.Max(1, data.Bounds.Right), Math.Max(1, data.Bounds.Bottom)), data.Font);
    }
    internal double RequiredWidth { get; private set; }
    internal double RequiredHeight { get; private set; }
    internal int VisibleRows { get; private set; }
    internal bool HasShortenedText { get; private set; }
    internal void AppendTo(VisualSceneBuilder builder) => builder.Append(_scene);
    internal void WriteSvg(SvgMarkupWriter writer) {
        foreach (var item in _svg) {
            if (item.Name == null) { writer.EndElement().Line(); continue; }
            writer.StartElement(item.Name);
            foreach (var attribute in item.Attributes) if (attribute.Value != null) writer.Attribute(attribute.Key, attribute.Value);
            if (item.Group) writer.EndStartElement().Line();
            else if (item.Text != null) writer.Text(item.Text).EndElement().Line();
            else writer.EndEmptyElement().Line();
        }
    }
    private sealed class SvgElement {
        internal SvgElement(string? name, KeyValuePair<string, string?>[] attributes, string? text = null, bool group = false) { Name = name; Attributes = attributes; Text = text; Group = group; }
        internal string? Name { get; }
        internal KeyValuePair<string, string?>[] Attributes { get; }
        internal string? Text { get; }
        internal bool Group { get; }
    }
    private static KeyValuePair<string, string?> A(string name, object? value) => new(name, value switch {
        null => null, double number => SvgMarkupWriter.FormatNumber(number), int count => count.ToString(CultureInfo.InvariantCulture), _ => value.ToString()
    });
    private void Rect(string role, ChartRect box, double radius, ChartColor fill, ChartColor? stroke = null, string? cssClass = null) {
        fill = Paint(fill); if (stroke.HasValue) stroke = Paint(stroke.Value);
        _svg.Add(new SvgElement("rect", new[] { A("data-cfx-role", role), A("class", cssClass), A("x", box.X), A("y", box.Y),
            A("width", box.Width), A("height", box.Height), A("rx", radius), A("fill", fill.ToCss()), A("stroke", stroke?.ToCss()) }));
        _builder.Rect(box, fill, stroke, _data.Scale, radius, role);
    }
    private void Circle(string role, double x, double y, double radius, ChartColor fill) {
        fill = Paint(fill);
        _svg.Add(new SvgElement("circle", new[] { A("data-cfx-role", role), A("cx", x), A("cy", y), A("r", radius), A("fill", fill.ToCss()) }));
        _builder.Ellipse(x, y, radius, radius, fill, role: role);
    }
    private void Text(string value, double x, double baseline, double width, TextAlignment alignment, ChartColor color, double size, int weight, string role = "visual-text") {
        color = Paint(color);
        weight = _data.WeightOverride ?? weight;
        if (!_faces.TryGetValue(weight, out var face)) _faces.Add(weight, face = new VisualSceneTextFace(_data.Font, weight));
        var normal = value;
        var fitted = VisualBlockRendering.FitMeasuredText(normal, size, Math.Max(_data.Scale, width), _data.Font.Family, weight);
        HasShortenedText |= fitted != normal;
        var captured = face.Prepare(fitted, size);
        var anchor = alignment == TextAlignment.Center ? "middle" : alignment == TextAlignment.Right ? "end" : "start";
        var left = alignment == TextAlignment.Center ? x + width / 2 : alignment == TextAlignment.Right ? x + width : x;
        _svg.Add(new SvgElement("text", new[] { A("data-cfx-role", role), A("x", left), A("y", baseline), A("text-anchor", anchor),
            A("fill", color.ToCss()), A("font-family", _data.Font.Family), A("font-size", size), A("font-weight", weight) }, fitted));
        _builder.PreparedText(captured, left, baseline, color, alignment, role);
    }
    private static double OutputFontSize(NativeTableData data, double roleSize) => data.AbsoluteDataLabelSize is double absolute
        ? absolute * (roleSize / data.Theme.DataLabelFontSize) : roleSize * data.Scale;
    private ChartColor Paint(ChartColor color) => _data.Opacity == 1 ? color : color.WithOpacity(color.A / 255d * _data.Opacity);
    private IDisposable Identity(string? id, string role, IReadOnlyDictionary<string, string> facts) => _builder.PushGroup(id, role, facts);
    private string? ChildId(string kind, string key) => _data.Id.Length == 0 ? null : _data.Id + "/" + kind + "/" + key.Length.ToString(CultureInfo.InvariantCulture) + ":" + key;
    private void Polyline(IReadOnlyList<ChartPoint> points, ChartColor color, double width, string role) {
        color = Paint(color);
        var text = new string[points.Count]; var commands = new ChartPathCommand[points.Count];
        for (var i = 0; i < points.Count; i++) { text[i] = SvgMarkupWriter.FormatNumber(points[i].X) + "," + SvgMarkupWriter.FormatNumber(points[i].Y);
            commands[i] = i == 0 ? ChartPathCommand.MoveTo(points[i].X, points[i].Y) : ChartPathCommand.LineTo(points[i].X, points[i].Y); }
        _svg.Add(new SvgElement("polyline", new[] { A("data-cfx-role", role), A("points", string.Join(" ", text)), A("fill", "none"),
            A("stroke", color.ToCss()), A("stroke-width", width), A("stroke-linecap", "round"), A("stroke-linejoin", "round") }));
        _builder.Path(new ChartPath(commands), stroke: color, strokeWidth: width, role: role);
    }
}
