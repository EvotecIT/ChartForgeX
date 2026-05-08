namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Sets whether grid lines should be rendered.
    /// </summary>
    /// <param name="visible">True to render grid lines; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithGrid(bool visible = true) { Options.ShowGrid = visible; return this; }

    /// <summary>
    /// Sets reusable visual tokens used by cartesian grid and guide lines.
    /// </summary>
    /// <param name="style">The grid line style.</param>
    /// <returns>The current chart.</returns>
    public Chart WithGridStyle(ChartGridLineStyle style) { Options.GridLineStyle = style; return this; }

    /// <summary>
    /// Configures reusable visual tokens used by cartesian grid and guide lines.
    /// </summary>
    /// <param name="configure">The style configuration callback.</param>
    /// <returns>The current chart.</returns>
    public Chart WithGridStyle(System.Action<ChartGridLineStyle> configure) {
        if (configure == null) throw new System.ArgumentNullException(nameof(configure));
        var style = Options.GridLineStyle.Clone();
        configure(style);
        Options.GridLineStyle = style;
        return this;
    }

    /// <summary>
    /// Sets whether axes, tick labels, and axis titles should be rendered.
    /// </summary>
    /// <param name="visible">True to render axes; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithAxes(bool visible = true) {
        Options.ShowAxes = visible;
        Options.ShowXAxis = visible;
        Options.ShowYAxis = visible;
        Options.ShowAxisLines = visible;
        return this;
    }

    /// <summary>
    /// Sets whether axis rules and zero-axis lines should be rendered when axes are enabled.
    /// </summary>
    /// <param name="visible">True to render axis rules; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithAxisLines(bool visible = true) { Options.ShowAxisLines = visible; return this; }

    /// <summary>
    /// Sets whether the x-axis, x tick labels, and x-axis title should be rendered when axes are enabled.
    /// </summary>
    /// <param name="visible">True to render the x-axis; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXAxisVisible(bool visible = true) { Options.ShowXAxis = visible; return this; }

    /// <summary>
    /// Sets whether the y-axis, y tick labels, and y-axis title should be rendered when axes are enabled.
    /// </summary>
    /// <param name="visible">True to render the y-axis; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithYAxisVisible(bool visible = true) { Options.ShowYAxis = visible; return this; }
}
