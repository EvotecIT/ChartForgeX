using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed partial class ChartGrid {
    /// <summary>
    /// Gets or sets the CSS custom properties the grid SVG writes for mapped colours (see
    /// <see cref="ChartForgeX.Themes.SvgColorVariables"/>): the grid title, background, and every panel, including charts
    /// without their own variables. Null (the default) writes literal colours; PNG output always uses literal colours.
    /// </summary>
    public SvgColorVariables? SvgColorVariables { get; set; }

    /// <summary>Writes mapped colours in the grid SVG as CSS custom properties with the literal colour as fallback.</summary>
    /// <param name="variables">The colour variables, or null to write literal colours.</param>
    /// <returns>The current grid.</returns>
    public ChartGrid WithSvgColorVariables(SvgColorVariables? variables) {
        SvgColorVariables = variables;
        return this;
    }
}
