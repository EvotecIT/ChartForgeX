namespace ChartForgeX.Rendering;

public sealed partial class VisualSvgOptions {
    private bool _fluidWidth;

    /// <summary>Gets whether horizontal positions fill the host while height, text and marker sizes remain in CSS pixels.</summary>
    public bool FluidWidth => _fluidWidth;

    /// <summary>Returns a script-free, horizontally fluid SVG policy retaining the other export settings.</summary>
    /// <param name="enabled">Whether to fill the host width at the prepared logical height.</param>
    /// <returns>An independent export policy.</returns>
    /// <remarks>
    /// This takes precedence over proportional responsive sizing. It stretches horizontal geometry without
    /// reflowing prepared labels or changing tick density. Text and point markers retain their prepared sizes.
    /// Cartesian, schedule and strip geometry is supported; unsupported polar geometry, arbitrary path clips,
    /// rotated geometry and captured images/effects throw <see cref="System.NotSupportedException"/> at export.
    /// Rotated text, translations and rectangular or rounded-rectangular clips are supported.
    /// </remarks>
    public VisualSvgOptions WithFluidWidth(bool enabled = true) {
        var copy = Copy(); copy._fluidWidth = enabled; return copy;
    }
}
