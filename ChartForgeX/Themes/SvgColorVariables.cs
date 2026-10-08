using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// Maps colours to CSS custom properties so SVG output can follow a host's design tokens: every paint value
/// (<c>fill</c>, <c>stroke</c>, <c>stop-color</c>, <c>flood-color</c>, <c>lighting-color</c>, <c>color</c>, in
/// attributes, <c>style</c> attributes, and <c>&lt;style&gt;</c> elements) whose colour is mapped is written as
/// <c>var(--name, #rrggbb)</c>, with the literal colour as the fallback, so the SVG looks the same where the property
/// is not defined. A paint more transparent than its variable is written as
/// <c>color-mix(in srgb, var(--name, #rrggbb) N%, transparent)</c>. PNG output is not affected.
/// </summary>
/// <remarks>
/// <para>
/// Set on a chart, grid, or topology (<c>Chart.WithSvgColorVariables</c>, <c>ChartGrid.WithSvgColorVariables</c>,
/// <c>TopologyRenderOptions.SvgColorVariables</c>), the renderers write colours by role. A colour the renderer derives
/// (the white sheen of a line, the highlight of a surface, the contrast stroke of a topology icon) stays literal even
/// when it equals a token colour. A blend of token colours (bar gradients, heatmap and calendar ramp steps, neutral zero
/// and empty days, topology tints) is written as <c>color-mix(in srgb, …)</c> of their properties, so it follows a theme
/// switch. A colour written for a role (a series, a status, a ramp step, the surface behind marks) takes the variable of
/// that role when several share its colour. Text on filled marks (heatmap and hexbin values, categorical cell text, Gantt
/// lane labels) is written as the text colour or, on strong marks, the surface behind the marks, by role. Other paints
/// match by colour value.
/// </para>
/// <para>
/// <see cref="Apply"/> on finished markup only matches by colour value (red, green, and blue; a paint is only matched
/// when it is no more opaque than the variable): every paint of a mapped colour takes the variable, including derived
/// colours that happen to equal it. When two variables share a colour, the one added first is used, and
/// <see cref="SvgColorRole.Surface"/> variables are not used for text fills. Do not apply variables to SVG rendered with
/// variables: its derived colours are literal on purpose and would be mapped by value.
/// </para>
/// <para>
/// Roles tell token kinds apart (a series colour from a ramp step of the same value), not tokens within a kind: when two
/// surface tokens share a colour in one theme, the one added first names it.
/// </para>
/// </remarks>
public sealed partial class SvgColorVariables {
    private static readonly Regex VariableName = new("^--[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant);
    private const string Paint = @"#[0-9A-Fa-f]{6}(?![0-9A-Fa-f])|rgba?\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*(?:,\s*[0-9]*\.?[0-9]+\s*)?\)";
    // Compiled: Apply scans every tag of finished, often very large, markup. Compilation does not change what matches.
    private static readonly Regex StartTag = new(@"<(?<tag>[A-Za-z][\w:.-]*)(?<attrs>(?:[^<>""']|""[^""]*""|'[^']*')*)>", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex StyleElement = new(@"(?<open><style\b(?:[^<>""']|""[^""]*""|'[^']*')*>)(?<css>.*?)(?<close></style>)", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex PaintAttribute = new(
        @"(?<lead>(?<![\w:-])(?:fill|stroke|stop-color|flood-color|lighting-color|color)\s*=\s*"")(?<value>" + Paint + @")(?="")",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex StyleAttribute = new(@"(?<open>(?<![\w:-])style\s*=\s*"")(?<css>[^""]*)(?<close>"")", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex PaintDeclaration = new(
        @"(?<lead>(?<![\w-])(?<property>fill|stroke|stop-color|flood-color|lighting-color|color)\s*:\s*)(?<value>" + Paint + @")(?=\s*(?:[;}]|!important|$))",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly List<SvgColorVariable> _variables = new();
    private readonly Dictionary<int, SvgColorVariable> _any = new();
    private readonly Dictionary<int, SvgColorVariable> _text = new();
    private readonly Dictionary<long, SvgColorVariable> _byRole = new();
    private readonly Dictionary<long, SvgColorVariable> _markInk = new();

    /// <summary>Gets the variables in the order they were added.</summary>
    public IReadOnlyList<SvgColorVariable> Variables => _variables.AsReadOnly();

    /// <summary>
    /// Adds a variable. When a variable for the same colour was added before, that one keeps the colour for paints
    /// without a role and this one is only used by paints of its own role.
    /// </summary>
    /// <param name="name">The custom property name, for example <c>--brand-series-1</c>.</param>
    /// <param name="color">The colour it stands for; its literal value is the fallback.</param>
    /// <param name="role">The role of the colour; <see cref="SvgColorRole.Surface"/> keeps text fills of this colour literal when they are matched by value.</param>
    /// <returns>The same instance.</returns>
    /// <exception cref="ArgumentException">The name is not <c>--</c> followed by letters, digits, <c>-</c>, or <c>_</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The role is not defined.</exception>
    public SvgColorVariables Add(string name, ChartColor color, SvgColorRole role = SvgColorRole.Any) {
        if (name == null || !VariableName.IsMatch(name)) throw new ArgumentException("A CSS custom property name starts with '--' and holds letters, digits, '-', or '_'.", nameof(name));
        if (!Enum.IsDefined(typeof(SvgColorRole), role)) throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown colour role.");
        var variable = new SvgColorVariable(name, color, role);
        _variables.Add(variable);
        var key = Rgb(color.R, color.G, color.B);
        if (!_any.ContainsKey(key)) _any[key] = variable;
        if (role != SvgColorRole.Surface && !_text.ContainsKey(key)) _text[key] = variable;
        var roleKey = RoleKey(role, key);
        if (!_byRole.ContainsKey(roleKey)) _byRole[roleKey] = variable;
        return this;
    }

    /// <summary>Creates an independent copy.</summary>
    public SvgColorVariables Clone() {
        var copy = new SvgColorVariables();
        copy._variables.AddRange(_variables);
        foreach (var entry in _any) copy._any.Add(entry.Key, entry.Value);
        foreach (var entry in _text) copy._text.Add(entry.Key, entry.Value);
        foreach (var entry in _byRole) copy._byRole.Add(entry.Key, entry.Value);
        foreach (var entry in _markInk) copy._markInk.Add(entry.Key, entry.Value);
        return copy;
    }

    /// <summary>
    /// Adds the contrasting text property paired with a filled mark. The renderer selects this property by the fill,
    /// so black or white text follows that mark when the host switches themes, without changing unrelated text.
    /// </summary>
    /// <param name="name">The CSS custom property for the ink, for example <c>--cfx-series-1-ink</c>.</param>
    /// <param name="fill">The opaque mark colour in this theme.</param>
    /// <param name="ink">The contrasting text colour in this theme.</param>
    /// <param name="fillRole">The role of the filled mark.</param>
    /// <returns>The same instance.</returns>
    /// <exception cref="ArgumentException">The property name is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The role is not defined.</exception>
    public SvgColorVariables AddInk(string name, ChartColor fill, ChartColor ink, SvgColorRole fillRole) {
        if (name == null || !VariableName.IsMatch(name)) throw new ArgumentException("Invalid CSS custom property name.", nameof(name));
        if (!Enum.IsDefined(typeof(SvgColorRole), fillRole)) throw new ArgumentOutOfRangeException(nameof(fillRole));
        var variable = new SvgColorVariable(name, ink, SvgColorRole.Text);
        _variables.Add(variable);
        _markInk[RoleKey(fillRole, Rgb(fill.R, fill.G, fill.B))] = variable;
        return this;
    }

    internal bool TryInk(ChartColor fill, SvgColorRole role, out string paint) {
        paint = string.Empty;
        return _markInk.TryGetValue(RoleKey(role, Rgb(fill.R, fill.G, fill.B)), out var variable) && TryWrite(variable, 1, out paint);
    }

    /// <summary>
    /// Writes mapped paint values in finished <paramref name="svg"/> as custom properties, by colour value. Applying it
    /// twice changes nothing more. Renderers given these variables also write derived colours by role (see remarks).
    /// </summary>
    /// <param name="svg">SVG markup, for example from <c>ToSvg()</c>.</param>
    /// <returns>The markup with mapped paints replaced.</returns>
    public string Apply(string svg) {
        if (svg == null) throw new ArgumentNullException(nameof(svg));
        if (_any.Count == 0) return svg;
        var result = svg.IndexOf("<style", StringComparison.Ordinal) < 0 ? svg
            : StyleElement.Replace(svg, match => match.Groups["open"].Value + Declarations(match.Groups["css"].Value, textElement: false) + match.Groups["close"].Value);
        return ReplaceTags(result);
    }

    /// <summary>
    /// Replaces the paints of every start tag, as <c>StartTag.Replace(svg, ReplaceTag)</c> does. Start tags are found by a
    /// scanner that follows the pattern exactly for ASCII tag names (markup with any other tag-name character takes the
    /// pattern itself), only tags whose attributes could hold a mapped paint are rebuilt, and the markup is copied once,
    /// only when a tag changed.
    /// </summary>
    private string ReplaceTags(string svg) {
        StringBuilder? builder = null;
        var copied = 0;
        var position = 0;
        while (true) {
            var open = svg.IndexOf('<', position);
            if (open < 0) break;
            var outcome = ReadStartTag(svg, open, out var nameEnd, out var close);
            if (outcome == TagScan.Unsupported) return ReplaceTagsWithPattern(svg);
            if (outcome == TagScan.NoTag) {
                position = open + 1;
                continue;
            }

            position = close + 1;
            if (!MayHoldPaint(svg, nameEnd, close - nameEnd)) continue;
            var replaced = ReplaceTag(svg.Substring(open + 1, nameEnd - open - 1), svg.Substring(nameEnd, close - nameEnd));
            var length = close + 1 - open;
            if (replaced.Length == length && string.CompareOrdinal(replaced, 0, svg, open, length) == 0) continue;
            builder ??= new StringBuilder(svg.Length + svg.Length / 8);
            builder.Append(svg, copied, open - copied).Append(replaced);
            copied = close + 1;
        }

        if (builder == null) return svg;
        builder.Append(svg, copied, svg.Length - copied);
        return builder.ToString();
    }

    private enum TagScan { NoTag, Tag, Unsupported }

    // Matches StartTag at open: '<', an ASCII letter, name characters, then attribute text of characters other than
    // <>"' and quoted strings, up to '>'. A failed match is final at this '<' (the pattern cannot backtrack into a
    // different split), so scanning resumes at the next character, as the pattern's search does.
    private static TagScan ReadStartTag(string svg, int open, out int nameEnd, out int close) {
        nameEnd = close = -1;
        var i = open + 1;
        if (i >= svg.Length || !(svg[i] is >= 'A' and <= 'Z' or >= 'a' and <= 'z')) return TagScan.NoTag;
        i++;
        while (i < svg.Length && svg[i] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or ':' or '.' or '-') i++;
        // \w also matches non-ASCII letters and digits; leave such names to the pattern.
        if (i < svg.Length && svg[i] >= 0x80) return TagScan.Unsupported;
        nameEnd = i;
        while (i < svg.Length) {
            var c = svg[i];
            if (c == '>') {
                close = i;
                return TagScan.Tag;
            }

            if (c == '<') return TagScan.NoTag;
            if (c is '"' or '\'') {
                var end = svg.IndexOf(c, i + 1);
                if (end < 0) return TagScan.NoTag;
                i = end + 1;
                continue;
            }

            i++;
        }

        return TagScan.NoTag;
    }

    // A paint attribute name contains "fill", "stroke" or "color" and a style attribute "style"; a mappable paint value
    // starts with '#' or "rgb". Attributes with neither cannot change.
    private static bool MayHoldPaint(string svg, int start, int length) {
        if (length == 0) return false;
        if (svg.IndexOf('#', start, length) < 0 && svg.IndexOf("rgb", start, length, StringComparison.Ordinal) < 0) return false;
        return svg.IndexOf("fill", start, length, StringComparison.Ordinal) >= 0 || svg.IndexOf("stroke", start, length, StringComparison.Ordinal) >= 0
            || svg.IndexOf("color", start, length, StringComparison.Ordinal) >= 0 || svg.IndexOf("style", start, length, StringComparison.Ordinal) >= 0;
    }

    private string ReplaceTagsWithPattern(string svg) {
        StringBuilder? builder = null;
        var copied = 0;
        for (var match = StartTag.Match(svg); match.Success; match = match.NextMatch()) {
            var attrs = match.Groups["attrs"];
            if (!MayHoldPaint(svg, attrs.Index, attrs.Length)) continue;
            var replaced = ReplaceTag(match.Groups["tag"].Value, attrs.Value);
            if (replaced.Length == match.Length && string.CompareOrdinal(replaced, 0, svg, match.Index, match.Length) == 0) continue;
            builder ??= new StringBuilder(svg.Length + svg.Length / 8);
            builder.Append(svg, copied, match.Index - copied).Append(replaced);
            copied = match.Index + match.Length;
        }

        if (builder == null) return svg;
        builder.Append(svg, copied, svg.Length - copied);
        return builder.ToString();
    }

    /// <summary>
    /// Returns the CSS paint for a colour written for <paramref name="role"/>: the variable of that role with that colour,
    /// else the variable a paint without a role would take (never a surface variable for <see cref="SvgColorRole.Text"/>).
    /// </summary>
    internal bool TryPaint(ChartColor color, SvgColorRole role, out string paint) {
        paint = string.Empty;
        if (color.A == 0) return false;
        return TryVariable(color, role, out var variable) && TryWrite(variable, color.A / 255.0, out paint);
    }

    internal bool TryVariable(ChartColor color, SvgColorRole role, out SvgColorVariable variable) {
        var key = Rgb(color.R, color.G, color.B);
        if (!(role != SvgColorRole.Any && _byRole.TryGetValue(RoleKey(role, key), out variable)) &&
            !(role == SvgColorRole.Text ? _text : _any).TryGetValue(key, out variable)) return false;
        return true;
    }

    private string ReplaceTag(string tag, string attrs) {
        var text = tag is "text" or "tspan" or "textPath";
        attrs = PaintAttribute.Replace(attrs, paint => ReplacePaint(paint, text && IsFill(paint.Value)));
        attrs = StyleAttribute.Replace(attrs, style => style.Groups["open"].Value + Declarations(style.Groups["css"].Value, text) + style.Groups["close"].Value);
        return "<" + tag + attrs + ">";
    }

    private string Declarations(string css, bool textElement) =>
        PaintDeclaration.Replace(css, declaration => ReplacePaint(declaration, textElement && declaration.Groups["property"].Value == "fill"));

    private static bool IsFill(string attribute) => attribute.StartsWith("fill", StringComparison.Ordinal);

    private string ReplacePaint(Match match, bool textFill) {
        var value = match.Groups["value"].Value;
        if (!TryParse(value, out var r, out var g, out var b, out var alpha) || alpha <= 0) return match.Value;
        if (!(textFill ? _text : _any).TryGetValue(Rgb(r, g, b), out var variable)) return match.Value;
        return TryWrite(variable, alpha, out var paint) ? match.Groups["lead"].Value + paint : match.Value;
    }

    /// <summary>Writes the variable, mixed with transparent when the paint is more transparent than the variable.</summary>
    private static bool TryWrite(SvgColorVariable variable, double alpha, out string paint) {
        paint = string.Empty;
        var variableAlpha = variable.Color.A / 255.0;
        if (variableAlpha <= 0 || alpha > variableAlpha + 0.002) return false;
        var literal = variable.Color.A == 255 ? variable.Color.ToHex() : variable.Color.ToHexRgba();
        paint = "var(" + variable.Name + ", " + literal + ")";
        var share = alpha / variableAlpha;
        if (share < 0.9995) paint = "color-mix(in srgb, " + paint + " " + (share * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%, transparent)";
        return true;
    }

    private static bool TryParse(string value, out int r, out int g, out int b, out double alpha) {
        r = g = b = 0;
        alpha = 1;
        if (value[0] == '#') {
            r = int.Parse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            g = int.Parse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            b = int.Parse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return true;
        }

        var open = value.IndexOf('(');
        var parts = value.Substring(open + 1, value.Length - open - 2).Split(',');
        if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out r)
            || !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out g)
            || !int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out b)
            || r > 255 || g > 255 || b > 255) return false;
        if (parts.Length == 4 && (!double.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out alpha) || alpha > 1)) return false;
        return true;
    }

    private static int Rgb(int r, int g, int b) => (r << 16) | (g << 8) | b;

    private static long RoleKey(SvgColorRole role, int rgb) => ((long)role << 32) | (uint)rgb;
}
