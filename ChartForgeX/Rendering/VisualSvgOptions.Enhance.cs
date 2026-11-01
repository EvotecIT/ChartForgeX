namespace ChartForgeX.Rendering;

public sealed partial class VisualSvgOptions {
    private bool _enhanceHooks;
    private bool _enhanceData;

    /// <summary>Gets whether the SVG carries the root and legend hooks the optional enhance runtime reads.</summary>
    public bool EnhanceHooks => _enhanceHooks;

    /// <summary>Gets whether the SVG also carries a compact JSON data island for shared-X tooltips and large key sets.</summary>
    public bool EnhanceData => _enhanceData;

    /// <summary>Returns a policy that writes the hooks a page-level enhance runtime needs, retaining the other export settings.</summary>
    /// <param name="enabled">Whether to write the hooks.</param>
    /// <param name="includeData">Whether to add a <c>&lt;metadata data-cfx-enhance-data&gt;</c> JSON island with series names,
    /// colours and X-sorted points. It is inert text: the SVG stays script-free.</param>
    /// <returns>An independent export policy.</returns>
    /// <remarks>
    /// The root receives <c>data-cfx-enhance</c>, its logical size, the plot rectangle, the category-axis domain and scale
    /// kind, and whether crosshair and brush gestures apply. Legend entries receive <c>data-cfx-legend-for</c>.
    /// The static chart is unchanged visually and works without any script; see <c>docs/dom-contract.md</c>.
    /// </remarks>
    public VisualSvgOptions WithEnhanceHooks(bool enabled = true, bool includeData = false) {
        var copy = Copy(); copy._enhanceHooks = enabled; copy._enhanceData = enabled && includeData; return copy;
    }
}
