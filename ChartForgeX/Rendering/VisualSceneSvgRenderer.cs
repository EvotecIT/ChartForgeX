using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

/// <summary>Serializes native scene decisions; it never lays out or parses a rendered visual.</summary>
internal static class VisualSceneSvgRenderer {
    internal static string Render(VisualScene scene, string? title = null, string? description = null,
        string? language = null, bool decorative = false, string? idPrefix = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        var prefix = idPrefix == null ? Identity(scene, title, description, language, decorative) : ValidatePrefix(idPrefix);
        var writer = new SvgMarkupWriter(4096);
        writer.StartElement("svg").Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("width", scene.Size.Width).Attribute("height", scene.Size.Height)
            .Attribute("viewBox", "0 0 " + N(scene.Size.Width) + " " + N(scene.Size.Height))
            .Attribute("lang", language).Attribute("xml:lang", language);
        if (decorative) writer.Attribute("aria-hidden", true).Attribute("focusable", false);
        else {
            writer.Attribute("role", "img").Attribute("aria-label", title);
            if (!string.IsNullOrEmpty(description)) writer.Attribute("aria-describedby", prefix + "-description");
        }
        writer.EndStartElement();
        if (!decorative && !string.IsNullOrEmpty(title)) writer.StartElement("title").Attribute("id", prefix + "-title").Text(title!).EndElement();
        if (!decorative && !string.IsNullOrEmpty(description)) writer.StartElement("desc").Attribute("id", prefix + "-description").Text(description!).EndElement();
        WriteClips(writer, scene, prefix);
        for (var i = 0; i < scene.Nodes.Count; i++) {
            var node = scene.Nodes[i];
            if (node is VisualSceneGroup group) {
                writer.StartElement("g"); Semantics(writer, group, prefix, i);
                foreach (var item in group.Metadata) writer.Attribute(item.Key, item.Value);
                if (group.Clip.HasValue) writer.Attribute("clip-path", "url(#" + prefix + "-clip-" + i.ToString(CultureInfo.InvariantCulture) + ")");
                writer.EndStartElement();
            } else if (node is VisualSceneEndGroup) writer.EndElement();
            else if (node is VisualSceneRectangle rect) {
                writer.StartElement("rect").Attribute("x", rect.Bounds.X).Attribute("y", rect.Bounds.Y)
                    .Attribute("width", rect.Bounds.Width).Attribute("height", rect.Bounds.Height).Attribute("rx", rect.Radius);
                Paint(writer, rect, prefix, i); writer.EndEmptyElement();
            } else if (node is VisualSceneEllipse ellipse) {
                writer.StartElement("ellipse").Attribute("cx", ellipse.Cx).Attribute("cy", ellipse.Cy).Attribute("rx", ellipse.Rx).Attribute("ry", ellipse.Ry);
                Paint(writer, ellipse, prefix, i); writer.EndEmptyElement();
            } else if (node is VisualSceneLine line) {
                writer.StartElement("line").Attribute("x1", line.Start.X).Attribute("y1", line.Start.Y).Attribute("x2", line.End.X).Attribute("y2", line.End.Y);
                Paint(writer, line, prefix, i);
                if (line.Dash != null) {
                    var dash = new StringBuilder();
                    foreach (var length in line.Dash) { if (dash.Length > 0) dash.Append(' '); dash.Append(N(length)); }
                    writer.Attribute("stroke-dasharray", dash.ToString());
                }
                writer.EndEmptyElement();
            } else if (node is VisualScenePath path) {
                writer.StartElement("path").Attribute("d", PathData(path)); Paint(writer, path, prefix, i); writer.EndEmptyElement();
            } else if (node is VisualSceneSlice slice) {
                if (slice.Outer <= 0 || slice.Sweep <= 0) continue;
                writer.StartElement("path").Attribute("d", ChartSlicePathGeometry.BuildPath(slice.Cx, slice.Cy, slice.Outer,
                    slice.Inner, slice.Start, slice.Start + slice.Sweep));
                Paint(writer, slice, prefix, i); writer.EndEmptyElement();
            } else if (node is VisualSceneText text) WriteText(writer, text, prefix, i);
        }
        writer.EndElement(); return writer.Build();
    }

    private static void WriteClips(SvgMarkupWriter writer, VisualScene scene, string prefix) {
        var opened = false;
        for (var i = 0; i < scene.Nodes.Count; i++) {
            if (!(scene.Nodes[i] is VisualSceneGroup group) || !group.Clip.HasValue) continue;
            if (!opened) { writer.StartElement("defs").EndStartElement(); opened = true; }
            var clip = group.Clip.Value;
            writer.StartElement("clipPath").Attribute("id", prefix + "-clip-" + i.ToString(CultureInfo.InvariantCulture)).EndStartElement()
                .StartElement("rect").Attribute("x", clip.X).Attribute("y", clip.Y).Attribute("width", clip.Width).Attribute("height", clip.Height).EndEmptyElement().EndElement();
        }
        if (opened) writer.EndElement();
    }

    private static void Paint(SvgMarkupWriter writer, VisualSceneMark mark, string prefix, int index) {
        Semantics(writer, mark, prefix, index);
        writer.Attribute("fill", mark.Fill?.ToCss() ?? "none").Attribute("fill-rule", "evenodd")
            .Attribute("stroke", mark.Stroke?.ToCss() ?? "none").Attribute("stroke-width", mark.StrokeWidth)
            .Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round");
    }

    private static void Semantics(SvgMarkupWriter writer, VisualSceneNode node, string prefix, int index) {
        if (node.Id != null) {
            writer.Attribute("id", prefix + "-node-" + index.ToString(CultureInfo.InvariantCulture));
            if (!(node is VisualSceneGroup group) || !group.Metadata.ContainsKey("data-cfx-source-id"))
                writer.Attribute("data-cfx-source-id", node.Id);
        }
        writer.Attribute("data-cfx-role", node.Role);
    }

    private static void WriteText(SvgMarkupWriter writer, VisualSceneText node, string prefix, int index) {
        var prepared = node.Text; var style = prepared.Style;
        writer.StartElement("g"); Semantics(writer, node, prefix, index); writer.EndStartElement();
        for (var i = 0; i < prepared.Lines.Count; i++) {
            var line = prepared.Lines[i];
            writer.StartElement("text").Attribute("x", node.LineLeft(line)).Attribute("y", node.Baseline + i * prepared.Metrics.LineHeight)
                .Attribute("font-family", style.Font.Family).Attribute("font-size", prepared.Size).Attribute("font-weight", style.Font.Weight)
                .Attribute("font-style", style.Font.Italic ? "italic" : "normal").Attribute("fill", node.Color.ToCss())
                .Attribute("xml:space", "preserve");
            var css = "white-space:pre";
            if (style.Font.Variations.Count > 0) css += ";font-variation-settings:" + style.Font.Variations.Css;
            if (style.OpenTypeLanguageTag != null) css += ";font-language-override:'" + style.OpenTypeLanguageTag + "'";
            writer.Attribute("style", css).Text(line.Text).EndElement();
        }
        writer.EndElement();
    }

    private static string PathData(VisualScenePath path) {
        var writer = new SvgPathDataBuilder(); var hasSubpath = false;
        foreach (var command in path.Commands) {
            if (command.Kind == ChartPathCommandKind.MoveTo) {
                if (hasSubpath && path.Close) writer.Close();
                writer.MoveTo(command.X, command.Y); hasSubpath = true;
            } else if (hasSubpath && command.Kind == ChartPathCommandKind.LineTo) writer.LineTo(command.X, command.Y);
            else if (hasSubpath && command.Kind == ChartPathCommandKind.CubicTo)
                writer.CubicTo(command.Control1X, command.Control1Y, command.Control2X, command.Control2Y, command.X, command.Y);
        }
        if (hasSubpath && path.Close) writer.Close();
        return writer.Build();
    }

    private static string N(double value) => SvgMarkupWriter.FormatNumber(value);

    /// <summary>Hashes detached typed content for prepared exports without SVG intermediates or random identities.</summary>
    internal static string Identity(VisualScene scene, string? title, string? description, string? language, bool decorative) {
        using var hash = SHA256.Create();
        using var stream = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
        // Batch primitive writes without changing the identity byte stream. Dense scenes otherwise
        // send thousands of tiny updates through the crypto transform while exporting SVG.
        using var buffered = new BufferedStream(stream, 16384);
        using (var writer = new BinaryWriter(buffered, Encoding.UTF8, leaveOpen: true)) {
            writer.Write("cfx-native-svg-1");
            writer.Write(scene.Size.Width); writer.Write(scene.Size.Height);
            Text(writer, title); Text(writer, description); Text(writer, language); writer.Write(decorative);
            writer.Write(scene.Nodes.Count);
            foreach (var node in scene.Nodes) {
                writer.Write(node switch {
                    VisualSceneGroup => 1, VisualSceneEndGroup => 2, VisualSceneRectangle => 3, VisualSceneEllipse => 4,
                    VisualSceneLine => 5, VisualScenePath => 6, VisualSceneSlice => 7, VisualSceneText => 8,
                    _ => throw new NotSupportedException("Unknown native scene command.")
                });
                Text(writer, node.Id); Text(writer, node.Role);
                if (node is VisualSceneMark mark) { Color(writer, mark.Fill); Color(writer, mark.Stroke); writer.Write(mark.StrokeWidth); }
                if (node is VisualSceneGroup group) {
                    writer.Write(group.Clip.HasValue);
                    if (group.Clip.HasValue) Rectangle(writer, group.Clip.Value);
                    writer.Write(group.Metadata.Count);
                    // The immutable scene sorts metadata with ordinal keys before export.
                    foreach (var item in group.Metadata) { writer.Write(item.Key); writer.Write(item.Value); }
                } else if (node is VisualSceneEndGroup) { }
                else if (node is VisualSceneRectangle rect) { Rectangle(writer, rect.Bounds); writer.Write(rect.Radius); }
                else if (node is VisualSceneEllipse ellipse) {
                    writer.Write(ellipse.Cx); writer.Write(ellipse.Cy); writer.Write(ellipse.Rx); writer.Write(ellipse.Ry);
                } else if (node is VisualSceneLine line) {
                    Point(writer, line.Start); Point(writer, line.End);
                    writer.Write(line.Dash?.Count ?? 0);
                    if (line.Dash != null) foreach (var length in line.Dash) writer.Write(length);
                } else if (node is VisualScenePath path) {
                    writer.Write(path.Close); writer.Write(path.Commands.Count);
                    foreach (var command in path.Commands) {
                        writer.Write((int)command.Kind); writer.Write(command.X); writer.Write(command.Y);
                        writer.Write(command.Control1X); writer.Write(command.Control1Y); writer.Write(command.Control2X); writer.Write(command.Control2Y);
                    }
                } else if (node is VisualSceneSlice slice) {
                    writer.Write(slice.Cx); writer.Write(slice.Cy); writer.Write(slice.Outer);
                    writer.Write(slice.Inner); writer.Write(slice.Start); writer.Write(slice.Sweep);
                } else if (node is VisualSceneText text) {
                    writer.Write(text.X); writer.Write(text.Baseline); writer.Write((int)text.Alignment); Color(writer, text.Color);
                    var prepared = text.Text; var style = prepared.Style; var font = style.Font;
                    writer.Write(prepared.Size); writer.Write(prepared.Ascent); writer.Write(prepared.Metrics.Width);
                    writer.Write(prepared.Metrics.Height); writer.Write(prepared.Metrics.LineHeight);
                    writer.Write(font.Family); Text(writer, font.FilePath); Text(writer, font.FaceName);
                    writer.Write(font.CollectionIndex ?? -1); writer.Write(font.Weight); writer.Write(font.Italic);
                    writer.Write(font.Variations.Css); writer.Write(font.ColorPaletteIndex);
                    writer.Write(style.FontSize); writer.Write(style.LineHeight); Color(writer, style.Color);
                    writer.Write((int)style.Alignment); writer.Write((int)style.UnderlineStyle); writer.Write((int)style.StrikethroughStyle);
                    writer.Write((int)style.Baseline); writer.Write((int)style.TextCase); writer.Write((int)style.Hinting); Text(writer, style.OpenTypeLanguageTag);
                    writer.Write(prepared.Lines.Count);
                    foreach (var preparedLine in prepared.Lines) { writer.Write(preparedLine.Text); writer.Write(preparedLine.Width); }
                } else throw new NotSupportedException("Unknown native scene command.");
            }
        }
        buffered.Flush();
        stream.FlushFinalBlock();
        var result = new StringBuilder("cfx-v2-");
        foreach (var value in hash.Hash!) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return result.ToString();
    }

    internal static string ValidatePrefix(string idPrefix) {
        if (idPrefix == null) throw new ArgumentNullException(nameof(idPrefix));
        if (idPrefix.Length == 0 || !Letter(idPrefix[0])) throw new ArgumentException("SVG ID prefixes must begin with an ASCII letter.", nameof(idPrefix));
        foreach (var c in idPrefix) if (!(Letter(c) || c >= '0' && c <= '9' || c == '-' || c == '_' || c == '.'))
            throw new ArgumentException("SVG ID prefixes may contain only ASCII letters, digits, hyphens, underscores and periods.", nameof(idPrefix));
        return idPrefix;
    }

    private static bool Letter(char value) => value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z';
    private static void Text(BinaryWriter writer, string? value) { writer.Write(value != null); if (value != null) writer.Write(value); }
    private static void Point(BinaryWriter writer, ChartPoint value) { writer.Write(value.X); writer.Write(value.Y); writer.Write(value.BreakBefore); }
    private static void Rectangle(BinaryWriter writer, ChartRect value) { writer.Write(value.X); writer.Write(value.Y); writer.Write(value.Width); writer.Write(value.Height); }
    private static void Color(BinaryWriter writer, ChartColor? value) {
        writer.Write(value.HasValue);
        if (value.HasValue) { writer.Write(value.Value.R); writer.Write(value.Value.G); writer.Write(value.Value.B); writer.Write(value.Value.A); }
    }
}
