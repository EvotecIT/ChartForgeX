using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

/// <summary>
/// A paint a renderer writes with its colour role, resolved against <see cref="SvgColorVariables"/> when the SVG is
/// finished. <see cref="Literal"/> is a colour the renderer derives (a white sheen, a contrast stroke) that must never
/// take a token's property because it happens to equal its colour; <see cref="Of"/> is a token colour written for a
/// role; <see cref="Mix"/> is a blend of two colours, written as <c>color-mix()</c> of their properties when one of them
/// is mapped. <see cref="Plain(ChartColor)"/> is an ordinary literal that the value-based <see cref="SvgColorVariables.Apply"/>
/// still maps.
/// </summary>
/// <remarks>
/// Typed paints travel through the markup as tokens made of the Unicode noncharacters U+FDD0 and U+FDD1, which the SVG
/// writers replace in any escaped text, so no chart content can forge one. Every renderer resolves them before it
/// returns markup (<see cref="Resolve"/>); without variables they become the same literal colours as before.
/// </remarks>
internal readonly struct SvgPaint {
    private const char Start = '\uFDD0';
    private const char End = '\uFDD1';
    // Only well-formed tokens match; anything else between the noncharacters is left as it is.
    private static readonly Regex Token = new(
        "\uFDD0(?:(?<kind>L)(?<body>[0-9A-F]{8})|(?<kind>P)(?<body>[0-7][0-9A-F]{8})|(?<kind>I)(?<body>[0-7][0-9A-F]{16})|(?<kind>M)(?<body>[0-9A-F]{8}[0-7L][0-9A-F]{8}[0-7L][0-9A-F]{8}[0-9.Ee+-]{1,32}))\uFDD1",
        RegexOptions.CultureInvariant);

    private SvgPaint(string? value, bool raw) {
        Value = value;
        IsRaw = raw;
    }

    /// <summary>Gets the attribute value: a CSS colour, a token when <see cref="IsRaw"/>, or null for no attribute.</summary>
    public string? Value { get; }

    /// <summary>Gets whether <see cref="Value"/> is a token that must be written without escaping.</summary>
    public bool IsRaw { get; }

    /// <summary>An ordinary literal colour, still subject to value-based mapping.</summary>
    public static SvgPaint Plain(ChartColor color) => new(color.ToCss(), raw: false);

    /// <summary>An ordinary CSS paint string, still subject to value-based mapping.</summary>
    /// <remarks>A null paint writes no attribute, as a null attribute value does.</remarks>
    public static SvgPaint Plain(string? css) => new(css, raw: false);

    /// <summary>Rebuilds a typed paint from its token, for markup trees that store attribute values as text.</summary>
    public static SvgPaint FromToken(string token) => new(token ?? throw new ArgumentNullException(nameof(token)), raw: true);

    /// <summary>Replaces the noncharacters of paint tokens in markup that is inserted without escaping (icon artwork).</summary>
    public static string Sanitize(string markup) => markup.IndexOf(Start) < 0 && markup.IndexOf(End) < 0 ? markup : markup.Replace(Start, '\uFFFD').Replace(End, '\uFFFD');

    /// <summary>A derived colour that stays literal.</summary>
    public static SvgPaint Literal(ChartColor color) => new(Start + "L" + Hex(color) + End, raw: true);

    /// <summary>A token colour written for <paramref name="role"/>.</summary>
    public static SvgPaint Of(ChartColor color, SvgColorRole role) => new(Start + "P" + Digit(role) + Hex(color) + End, raw: true);

    /// <summary>Black or white ink paired to this fill; an explicitly configured ink property follows theme changes.</summary>
    public static SvgPaint Contrast(ChartColor fill, SvgColorRole role) =>
        new(Start + "I" + Digit(role) + Hex(fill) + Hex(ChartColorMath.AccessibleTextOnBackground(fill)) + End, raw: true);

    /// <summary>
    /// A blend of <paramref name="from"/> towards <paramref name="to"/> by <paramref name="amount"/> (0 is
    /// <paramref name="from"/>). <paramref name="result"/> is the blended colour the renderer computes, written when no
    /// variable applies, so the literal output equals the raster output exactly. A null role marks an operand the
    /// renderer derives (white, black) that is always written literally.
    /// </summary>
    public static SvgPaint Mix(ChartColor result, ChartColor from, SvgColorRole? fromRole, ChartColor to, SvgColorRole? toRole, double amount) =>
        new(Start + "M" + Hex(result) + Digit(fromRole) + Hex(from) + Digit(toRole) + Hex(to) + Clamp(amount).ToString("R", CultureInfo.InvariantCulture) + End, raw: true);

    /// <summary>
    /// Replaces the paint tokens in <paramref name="svg"/>. With <paramref name="keepLiterals"/> derived literals stay
    /// tokens, so a host renderer that applies its own variables afterwards (a chart grid) does not map them by value.
    /// </summary>
    public static string Resolve(string svg, SvgColorVariables? variables, bool keepLiterals = false) {
        if (svg.IndexOf(Start) < 0) return svg;
        return Token.Replace(svg, match => {
            var body = match.Groups["body"].Value;
            switch (match.Groups["kind"].Value) {
                case "L":
                    return keepLiterals ? match.Value : Color(body, 0).ToCss();
                case "P": {
                    var color = Color(body, 1);
                    var role = Role(body[0]);
                    return variables != null && role.HasValue && variables.TryPaint(color, role.Value, out var paint) ? paint : color.ToCss();
                }
                case "I": {
                    var fill = Color(body, 1);
                    var ink = Color(body, 9);
                    var role = Role(body[0])!.Value;
                    if (variables != null && variables.TryInk(fill, role, out var paint)) return paint;
                    return keepLiterals ? Literal(ink).Value! : ink.ToCss();
                }
                default: {
                    var result = Color(body, 0);
                    var from = Color(body, 9);
                    var to = Color(body, 18);
                    if (!double.TryParse(body.Substring(26), NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)) amount = 0;
                    if (variables != null && TryMix(variables, from, Role(body[8]), to, Role(body[17]), amount, out var mix)) return mix;
                    return keepLiterals ? Literal(result).Value! : result.ToCss();
                }
            }
        });
    }

    private static bool TryMix(SvgColorVariables variables, ChartColor from, SvgColorRole? fromRole, ChartColor to, SvgColorRole? toRole, double amount, out string paint) {
        paint = string.Empty;
        // color-mix interpolates premultiplied colours; only opaque operands give exactly the raster blend.
        if (from.A != 255 || to.A != 255) return false;
        var fromPaint = string.Empty;
        var toPaint = string.Empty;
        var fromMapped = fromRole.HasValue && variables.TryPaint(from, fromRole.Value, out fromPaint);
        var toMapped = toRole.HasValue && variables.TryPaint(to, toRole.Value, out toPaint);
        if (!fromMapped && !toMapped) return false;
        if (!fromMapped) fromPaint = from.ToHex();
        if (!toMapped) toPaint = to.ToHex();
        if (amount <= 0.00005) {
            paint = fromPaint;
            return true;
        }

        if (amount >= 0.99995) {
            paint = toPaint;
            return true;
        }

        // Only the second operand carries a percentage: CSS gives the first the remainder, so the shares always add up to
        // 100% and the mix stays opaque (two separately rounded percentages can total 99.99%).
        var builder = new StringBuilder("color-mix(in srgb, ", 96);
        builder.Append(fromPaint).Append(", ");
        builder.Append(toPaint).Append(' ').Append((amount * 100).ToString("0.##", CultureInfo.InvariantCulture)).Append("%)");
        paint = builder.ToString();
        return true;
    }

    private static string Hex(ChartColor color) => color.ToHexRgba().Substring(1);

    private static ChartColor Color(string body, int offset) => ChartColor.FromRgba(uint.Parse(body.Substring(offset, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    private const char LiteralOperand = 'L';

    private static char Digit(SvgColorRole? role) => role.HasValue ? (char)('0' + (int)role.Value) : LiteralOperand;

    private static SvgColorRole? Role(char digit) => digit == LiteralOperand ? null : (SvgColorRole)(digit - '0');

    private static double Clamp(double value) => double.IsNaN(value) ? 0 : Math.Max(0, Math.Min(1, value));
}
