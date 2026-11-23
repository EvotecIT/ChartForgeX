namespace ChartForgeX.Core;

public sealed partial class Chart {

    /// <summary>
    /// Sets whether axes, tick labels, and axis titles should be rendered.
    /// </summary>
    /// <param name="visible">True to render axes; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart Axes(bool visible = true) {
        Options.ShowAxes = visible;
        Options.XAxis.Visible = visible;
        Options.YAxis.Visible = visible;
        Options.ShowAxisLines = visible;
        return this;
    }
}
