using System;
using System.Globalization;

namespace ChartForgeX.Primitives;

public readonly partial struct ChartColor {
    private ChartColor(byte r, byte g, byte b, byte a, string? cssVariable) {
        R = r; G = g; B = b; A = a; CssVariable = cssVariable;
    }

    /// <summary>Names a page color directly, retaining this color's channels as the raster and CSS fallback.</summary>
    /// <param name="name">A custom property name such as <c>--accent</c>, containing only ASCII letters, digits, underscores and hyphens after <c>--</c>.</param>
    /// <returns>A color that keeps its custom property independently of other colors with the same fallback.</returns>
    /// <remarks>
    /// The alpha channel applies to the resolved property too, using <c>color-mix()</c> for translucent SVG paints.
    /// <see cref="WithAlpha"/> and <see cref="WithOpacity"/> retain the property. Raster rendering and packed/hex exports use only the fallback channels.
    /// The page must define a valid CSS color; ChartForgeX cannot validate the property's browser value.
    /// </remarks>
    /// <exception cref="ArgumentException">The custom property name is invalid.</exception>
    public ChartColor WithCssVariable(string name) => new(R, G, B, A, ValidateCssVariableName(name, nameof(name)));

    /// <summary>Validates the bounded ASCII custom property syntax shared by page color and font policies.</summary>
    internal static string ValidateCssVariableName(string? name, string parameterName) {
        if (name == null || name.Length < 3 || name.Length > 128 || name[0] != '-' || name[1] != '-')
            throw new ArgumentException("A CSS custom property must be '--' followed by 1 to 126 ASCII letters, digits, underscores or hyphens.", parameterName);
        for (var i = 2; i < name.Length; i++) {
            var c = name[i];
            if (!(c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-'))
                throw new ArgumentException("A CSS custom property must contain only ASCII letters, digits, underscores or hyphens after '--'.", parameterName);
        }
        return name;
    }

    private string FallbackCss() => A == 255 ? ToHex() : FormattableString.Invariant($"rgba({R},{G},{B},{A / 255.0:0.###})");

    private string VariableCss() {
        var paint = "var(" + CssVariable + ", " + ToHex() + ")";
        return A == 255 ? paint : "color-mix(in srgb, " + paint + " " + (A / 255.0 * 100).ToString("0.#####", CultureInfo.InvariantCulture) + "%, transparent)";
    }
}
