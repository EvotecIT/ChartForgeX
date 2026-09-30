using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>
    /// Gets or sets the CSS custom properties SVG output writes for mapped colours (see <see cref="ChartForgeX.Themes.SvgColorVariables"/>);
    /// null (the default) writes literal colours. PNG output always uses literal colours.
    /// </summary>
    public SvgColorVariables? SvgColorVariables { get; set; }
}
