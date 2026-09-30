using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Writes mapped colours in SVG output as CSS custom properties with the literal colour as fallback, so a host page
    /// can restyle the chart from its design tokens (see <see cref="ChartForgeX.Themes.SvgColorVariables"/> and
    /// <see cref="VisualDesignTokens.ToSvgColorVariables"/>). PNG output keeps literal colours.
    /// </summary>
    /// <param name="variables">The colour variables, or null to write literal colours.</param>
    /// <returns>The current chart.</returns>
    public Chart WithSvgColorVariables(SvgColorVariables? variables) {
        Options.SvgColorVariables = variables;
        return this;
    }
}
