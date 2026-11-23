using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed partial class ChartGrid {
    /// <summary>
    /// Gets or sets the CSS custom properties the grid SVG writes for mapped colours (see
    /// <see cref="ChartForgeX.Themes.SvgColorVariables"/>): the grid title, background, and every panel, including charts
    /// without their own variables. With <see cref="UsePanelThemes"/> enabled, this mapping applies only to the outer
    /// frame; each panel retains its chart mapping or literal colours. PNG output always uses literal colours.
    /// </summary>
    public SvgColorVariables? SvgColorVariables { get; set; }
}
