using System;
using System.Collections.Generic;
using System.Globalization;
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
/// Colours are matched by value (red, green, and blue; a paint is only matched when it is no more opaque than the
/// variable). When two variables share a colour, the one added first is used. Any paint with a mapped colour takes the
/// variable, including colours ChartForgeX derives that happen to equal it, except that variables added with
/// <c>appliesToText: false</c> are not used for text fills. Derived colours that differ from every mapped colour
/// (blends, most contrast text) stay literal.
/// </remarks>
public sealed class SvgColorVariables {
    private static readonly Regex VariableName = new("^--[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant);
    private const string Paint = @"#[0-9A-Fa-f]{6}(?![0-9A-Fa-f])|rgba?\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*(?:,\s*[0-9]*\.?[0-9]+\s*)?\)";
    private static readonly Regex StartTag = new(@"<(?<tag>[A-Za-z][\w:.-]*)(?<attrs>(?:[^<>""']|""[^""]*""|'[^']*')*)>", RegexOptions.CultureInvariant);
    private static readonly Regex StyleElement = new(@"(?<open><style\b(?:[^<>""']|""[^""]*""|'[^']*')*>)(?<css>.*?)(?<close></style>)", RegexOptions.CultureInvariant | RegexOptions.Singleline);
    private static readonly Regex PaintAttribute = new(
        @"(?<lead>(?<![\w:-])(?:fill|stroke|stop-color|flood-color|lighting-color|color)\s*=\s*"")(?<value>" + Paint + @")(?="")",
        RegexOptions.CultureInvariant);
    private static readonly Regex StyleAttribute = new(@"(?<open>(?<![\w:-])style\s*=\s*"")(?<css>[^""]*)(?<close>"")", RegexOptions.CultureInvariant);
    private static readonly Regex PaintDeclaration = new(
        @"(?<lead>(?<![\w-])(?<property>fill|stroke|stop-color|flood-color|lighting-color|color)\s*:\s*)(?<value>" + Paint + @")(?=\s*(?:[;}]|!important|$))",
        RegexOptions.CultureInvariant);

    private readonly List<SvgColorVariable> _variables = new();
    private readonly Dictionary<int, SvgColorVariable> _any = new();
    private readonly Dictionary<int, SvgColorVariable> _text = new();

    /// <summary>Gets the variables in the order they were added.</summary>
    public IReadOnlyList<SvgColorVariable> Variables => _variables.AsReadOnly();

    /// <summary>
    /// Adds a variable. When a variable for the same colour was added before, that one keeps the colour and this one is
    /// only listed.
    /// </summary>
    /// <param name="name">The custom property name, for example <c>--brand-series-1</c>.</param>
    /// <param name="color">The colour it stands for; its literal value is the fallback.</param>
    /// <param name="appliesToText">False keeps text fills of this colour literal; use it for surface colours.</param>
    /// <returns>The same instance.</returns>
    /// <exception cref="ArgumentException">The name is not <c>--</c> followed by letters, digits, <c>-</c>, or <c>_</c>.</exception>
    public SvgColorVariables Add(string name, ChartColor color, bool appliesToText = true) {
        if (name == null || !VariableName.IsMatch(name)) throw new ArgumentException("A CSS custom property name starts with '--' and holds letters, digits, '-', or '_'.", nameof(name));
        var variable = new SvgColorVariable(name, color, appliesToText);
        _variables.Add(variable);
        var key = Rgb(color.R, color.G, color.B);
        if (!_any.ContainsKey(key)) _any[key] = variable;
        if (appliesToText && !_text.ContainsKey(key)) _text[key] = variable;
        return this;
    }

    /// <summary>Creates an independent copy.</summary>
    public SvgColorVariables Clone() {
        var copy = new SvgColorVariables();
        foreach (var variable in _variables) copy.Add(variable.Name, variable.Color, variable.AppliesToText);
        return copy;
    }

    /// <summary>Writes mapped paint values in <paramref name="svg"/> as custom properties. Applying it twice changes nothing more.</summary>
    /// <param name="svg">SVG markup, for example from <c>ToSvg()</c>.</param>
    /// <returns>The markup with mapped paints replaced.</returns>
    public string Apply(string svg) {
        if (svg == null) throw new ArgumentNullException(nameof(svg));
        if (_any.Count == 0) return svg;
        var result = StyleElement.Replace(svg, match => match.Groups["open"].Value + Declarations(match.Groups["css"].Value, textElement: false) + match.Groups["close"].Value);
        return StartTag.Replace(result, ReplaceTag);
    }

    private string ReplaceTag(Match match) {
        var attrs = match.Groups["attrs"].Value;
        if (attrs.Length == 0) return match.Value;
        var tag = match.Groups["tag"].Value;
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
        var variableAlpha = variable.Color.A / 255.0;
        if (variableAlpha <= 0 || alpha > variableAlpha + 0.002) return match.Value;
        var literal = variable.Color.A == 255 ? variable.Color.ToHex() : variable.Color.ToHexRgba();
        var paint = "var(" + variable.Name + ", " + literal + ")";
        var share = alpha / variableAlpha;
        if (share < 0.9995) paint = "color-mix(in srgb, " + paint + " " + (share * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%, transparent)";
        return match.Groups["lead"].Value + paint;
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
}
