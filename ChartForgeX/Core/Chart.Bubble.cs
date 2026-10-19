using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Configures the shared source-size domain and logical radius range for all bubble series.</summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The current chart.</returns>
    public Chart ConfigureBubble(Action<ChartBubbleOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Options.Bubble = ChartOptionConfiguration.Configure(Options.Bubble, configure, options => options.Clone());
        return this;
    }

    /// <summary>
    /// Adds a bubble chart series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="bubbles">The bubble values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBubble(string name, IEnumerable<ChartBubble> bubbles, ChartColor? color = null) {
        var series = ChartSeries.CreateBubble(name, bubbles, color);
        AppendSeries(series);
        return this;
    }
}
