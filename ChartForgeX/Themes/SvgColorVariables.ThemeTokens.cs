using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

public sealed partial class SvgColorVariables {
    private readonly Dictionary<VisualThemeColorToken, string> _themeTokens = new();

    /// <summary>Binds a canonical theme color to a CSS custom property while preserving the token's identity.</summary>
    /// <remarks>Token-aware SVG renderers use this binding even when multiple theme colors share one RGB value.
    /// The renderer supplies the selected theme's concrete fallback. Value-based <see cref="Apply"/> mappings are configured separately with <see cref="Add"/>.</remarks>
    /// <param name="token">The canonical theme color to bind.</param>
    /// <param name="cssVariableName">The CSS custom property name, for example <c>--app-surface</c>.</param>
    /// <returns>The same instance. Adding the same token again replaces its previous binding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The theme token is not defined.</exception>
    /// <exception cref="ArgumentException">The CSS custom property name is invalid.</exception>
    public SvgColorVariables AddThemeToken(VisualThemeColorToken token, string cssVariableName) {
        if (!Enum.IsDefined(typeof(VisualThemeColorToken), token)) throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown theme color token.");
        if (cssVariableName == null || !VariableName.IsMatch(cssVariableName))
            throw new ArgumentException("A CSS custom property name starts with '--' and holds letters, digits, '-', or '_'.", nameof(cssVariableName));
        _themeTokens[token] = cssVariableName;
        return this;
    }

    /// <summary>Resolves a token's validated CSS reference using the selected theme's concrete color as fallback.</summary>
    internal string ResolveThemeToken(VisualThemeColorToken token, ChartColor fallback) =>
        _themeTokens.TryGetValue(token, out var name) ? "var(" + name + ", " + fallback.ToCss() + ")" : fallback.ToCss();

    /// <summary>Resolves a deferred canonical paint only when this SVG scope binds its authored token.</summary>
    internal bool TryThemeTokenPaint(VisualThemeColorToken token, ChartColor fallback, out string paint) {
        paint = _themeTokens.TryGetValue(token, out var name) ? "var(" + name + ", " + fallback.ToCss() + ")" : string.Empty;
        return paint.Length > 0;
    }
}
