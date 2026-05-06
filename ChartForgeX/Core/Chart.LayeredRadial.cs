using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a layered radial chart where each layer controls its own radius, stroke, color, value scale, and angle range.
    /// </summary>
    /// <param name="name">The layered radial group name.</param>
    /// <param name="layers">The radial layers to render, ordered back to front.</param>
    /// <returns>The current chart.</returns>
    public Chart AddLayeredRadial(string name, IEnumerable<ChartRadialLayer> layers) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (layers == null) throw new ArgumentNullException(nameof(layers));
        var materialized = new List<ChartRadialLayer>();
        foreach (var layer in layers) {
            if (layer == null) throw new ArgumentException("Layered radial charts cannot contain null layers.", nameof(layers));
            if (layer.Maximum <= layer.Minimum) throw new ArgumentOutOfRangeException(nameof(layers), layer.Maximum, "Layer maximum must be greater than minimum.");
            materialized.Add(layer);
        }

        if (materialized.Count == 0) throw new ArgumentException("Layered radial charts must contain at least one layer.", nameof(layers));
        var series = new ChartSeries(name, ChartSeriesKind.LayeredRadial, new[] { new ChartPoint(0, 0) });
        series.RadialLayers.AddRange(materialized);
        Series.Add(series);
        return this;
    }
}
