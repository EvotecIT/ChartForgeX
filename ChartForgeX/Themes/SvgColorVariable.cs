using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>One colour mapped to a CSS custom property by <see cref="SvgColorVariables"/>.</summary>
public readonly struct SvgColorVariable {
    internal SvgColorVariable(string name, ChartColor color, bool appliesToText) {
        Name = name;
        Color = color;
        AppliesToText = appliesToText;
    }

    /// <summary>Gets the custom property name, for example <c>--brand-series-1</c>.</summary>
    public string Name { get; }

    /// <summary>Gets the colour the property stands for; its literal value is the fallback.</summary>
    public ChartColor Color { get; }

    /// <summary>
    /// Gets whether the variable is also used for the fill of text. Surface colours set this to false, so text that
    /// happens to have a surface colour (such as white labels on dark marks) stays literal instead of following the
    /// surface into another theme.
    /// </summary>
    public bool AppliesToText { get; }
}
