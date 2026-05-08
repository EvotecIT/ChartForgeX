using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Sets how multiple bar series are arranged within each category.
    /// </summary>
    /// <param name="mode">The bar arrangement mode.</param>
    /// <returns>The current chart.</returns>
    public Chart WithBarMode(ChartBarMode mode) { Options.BarMode = mode; return this; }

    /// <summary>
    /// Sets the visual treatment used by bar and horizontal-bar renderers.
    /// </summary>
    /// <param name="style">The bar visual style.</param>
    /// <returns>The current chart.</returns>
    public Chart WithBarStyle(ChartBarStyle style) { Options.BarStyle = style; return this; }

    /// <summary>
    /// Sets reusable visual tokens used by bar and horizontal-bar renderers.
    /// </summary>
    /// <param name="style">The bar visual style.</param>
    /// <returns>The current chart.</returns>
    public Chart WithBarVisualStyle(ChartBarVisualStyle style) { Options.BarVisualStyle = style; return this; }

    /// <summary>
    /// Configures reusable visual tokens used by bar and horizontal-bar renderers.
    /// </summary>
    /// <param name="configure">The style configuration callback.</param>
    /// <returns>The current chart.</returns>
    public Chart WithBarVisualStyle(Action<ChartBarVisualStyle> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var style = Options.BarVisualStyle.Clone();
        configure(style);
        Options.BarVisualStyle = style;
        return this;
    }

    /// <summary>
    /// Arranges multiple bar series side by side within each category.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart WithGroupedBars() { Options.BarMode = ChartBarMode.Grouped; return this; }

    /// <summary>
    /// Arranges multiple bar series as stacked segments within each category.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart WithStackedBars() { Options.BarMode = ChartBarMode.Stacked; return this; }

    /// <summary>
    /// Arranges multiple horizontal bar series as stacked segments within each category.
    /// </summary>
    /// <returns>The current chart.</returns>
    public Chart WithStackedHorizontalBars() => WithStackedBars();

    /// <summary>
    /// Sets whether stacked bar totals should be rendered above each category.
    /// </summary>
    /// <param name="visible">True to render stacked bar totals; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithStackTotals(bool visible = true) { Options.ShowStackTotals = visible; return this; }
}
