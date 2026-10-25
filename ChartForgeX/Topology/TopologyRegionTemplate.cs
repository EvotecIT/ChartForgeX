using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Topology;

/// <summary>Compiles the small, trusted SVG anatomy asset into the existing native scene primitives.</summary>
internal static class TopologyRegionTemplate {
    private static readonly Lazy<XElement> Template = new(() => XElement.Parse(Read("svg")));
    private static readonly Lazy<XElement> StatusTemplate = new(() => XElement.Parse(ReadResource("topology-region-status.svg")));
    internal static readonly Lazy<string> Styles = new(() => Read("css"));
    internal static string BadgeSymbol(string glyph) {
        var id = glyph == "crown" ? "badge" : glyph;
        var symbol = Template.Value.Elements().FirstOrDefault(element => (string?)element.Attribute("id") == id);
        if (symbol == null || !((string?)symbol.Attribute("class") ?? "").Split(' ').Contains("badge"))
            throw new ArgumentException("No named badge anatomy exists in the topology template: " + glyph, nameof(glyph));
        return id;
    }
    private static string Read(string extension) {
        return ReadResource("topology-regions." + extension);
    }
    private static string ReadResource(string name) {
        using var stream = typeof(TopologyRegionTemplate).Assembly.GetManifestResourceStream("ChartForgeX.Topology.Assets." + name)
            ?? throw new InvalidOperationException("Missing topology region template asset.");
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
    internal static void Draw(TopologyRegionPaintBuilder builder, string name, IReadOnlyDictionary<string, string> values,
        IReadOnlyDictionary<string, string>? metadata = null, string? role = null, string? id = null, Action? content = null) {
        var symbol = Template.Value.Elements().Concat(StatusTemplate.Value.Elements()).Single(element => (string?)element.Attribute("id") == name);
        var attributes = metadata == null ? new Dictionary<string, string>() : metadata.ToDictionary(pair => pair.Key, pair => pair.Value);
        var classes = Expand((string?)symbol.Attribute("class") ?? "", values);
        attributes["class"] = attributes.TryGetValue("class", out var extra) && !string.IsNullOrWhiteSpace(extra) ? classes + " " + extra : classes;
        using (builder.PushGroup(id, role, attributes)) {
            foreach (var element in symbol.Elements()) DrawPart(builder, element, values);
            content?.Invoke();
        }
    }
    private static string Expand(string value, IReadOnlyDictionary<string, string> values) {
        return Regex.Replace(value, @"\{\{([^{}]+)\}\}", match => values.TryGetValue(match.Groups[1].Value, out var replacement)
            ? replacement : throw new InvalidOperationException("Unresolved region template substitution: " + match.Value));
    }
    private static void DrawPart(TopologyRegionPaintBuilder builder, XElement part, IReadOnlyDictionary<string, string> values) {
        string Attr(string name, string fallback = "") => Expand((string?)part.Attribute(name) ?? fallback, values);
        double Number(string name, string fallback = "0") => double.Parse(Attr(name, fallback), CultureInfo.InvariantCulture);
        ChartColor? Color(string name) => Attr(name) is var value && value.Length > 0 && value != "none" ? ChartColor.Parse(value) : null;
        SvgPaint? Paint(string name) {
            var key = ((string?)part.Attribute(name) ?? "").Trim('{', '}');
            var token = key switch {
                "surface" => VisualThemeColorToken.ElevatedSurface, "surface-sunken" => VisualThemeColorToken.Surface,
                "border" => VisualThemeColorToken.Border, "text" => VisualThemeColorToken.Foreground,
                "text-muted" => VisualThemeColorToken.MutedForeground, "halo" => VisualThemeColorToken.Surface,
                _ => values.TryGetValue(key == "status" ? "status-token" : "badge-token", out var value)
                    && Enum.TryParse(value, out VisualThemeColorToken resolved) ? resolved : (VisualThemeColorToken?)null
            };
            var color = Color(name);
            return token.HasValue && color.HasValue ? SvgPaint.ThemeToken(color.Value, token.Value, SvgColorRole.Any) : null;
        }
        var metadata = new Dictionary<string, string> { ["class"] = Attr("class") };
        using (builder.PushGroup(null, part.Name.LocalName == "circle" && Attr("class").Split(' ').Contains("core") ? "topology-node-health-ring" : null, metadata)) {
            switch (part.Name.LocalName) {
                case "rect": builder.Rect(new ChartRect(Number("x"), Number("y"), Number("width"), Number("height")), Color("fill"), Color("stroke"), Number("stroke-width", "1"), Number("rx"), paint: new VisualScenePaintBinding(Paint("fill"), Paint("stroke"))); break;
                case "circle": builder.Ellipse(Number("cx"), Number("cy"), Number("r"), Number("r"), Color("fill"), Color("stroke"), Number("stroke-width", "1"), paint: new VisualScenePaintBinding(Paint("fill"), Paint("stroke"))); break;
                case "text":
                    var text = Expand(part.Value, values); if (text.Length == 0) break;
                    builder.Text(text, Number("x"), Number("y"), Number("font-size", "12"), Color("fill") ?? ChartColor.Black,
                        (int)Number("font-weight", "400"), alignment: Attr("text-anchor") == "middle" ? TextAlignment.Center : TextAlignment.Left,
                        stroke: Color("stroke"), strokeWidth: Number("stroke-width"), paint: Paint("fill"), strokePaint: Paint("stroke")); break;
                case "polygon":
                    var points = Attr("points").Split(' ').Where(value => value.Length > 0).Select(value => value.Split(',')).ToArray();
                    var commands = points.Select((point, index) => index == 0 ? ChartPathCommand.MoveTo(double.Parse(point[0], CultureInfo.InvariantCulture), double.Parse(point[1], CultureInfo.InvariantCulture))
                        : ChartPathCommand.LineTo(double.Parse(point[0], CultureInfo.InvariantCulture), double.Parse(point[1], CultureInfo.InvariantCulture))).ToArray();
                    builder.Path(new ChartPath(commands), Color("fill"), Color("stroke"), Number("stroke-width", "1"), close: true, paint: new VisualScenePaintBinding(Paint("fill"), Paint("stroke"))); break;
                default: throw new InvalidOperationException("Unsupported region template element: " + part.Name.LocalName);
            }
        }
    }
}
