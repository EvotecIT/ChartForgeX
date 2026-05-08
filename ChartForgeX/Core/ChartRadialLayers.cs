using System;
using System.Collections;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Fluent collection for layered radial chart and radial metric card layers.
/// </summary>
public sealed class ChartRadialLayers : IReadOnlyList<ChartRadialLayer> {
    private readonly List<ChartRadialLayer> _layers = new();

    /// <summary>Gets the layer at the specified index.</summary>
    public ChartRadialLayer this[int index] => _layers[index];

    /// <summary>Gets the number of layers.</summary>
    public int Count => _layers.Count;

    /// <summary>Creates an empty radial layer collection.</summary>
    public static ChartRadialLayers Create() => new();

    /// <summary>Adds one existing layer.</summary>
    public ChartRadialLayers Add(ChartRadialLayer layer) {
        _layers.Add(layer ?? throw new ArgumentNullException(nameof(layer)));
        return this;
    }

    /// <summary>Adds one layer and optionally configures its geometry and styling.</summary>
    public ChartRadialLayers Add(string name, double value, double minimum = 0, double maximum = 100, ChartColor? color = null, Func<ChartRadialLayer, ChartRadialLayer>? configure = null) {
        var layer = ChartRadialLayer.Create(name, value, minimum, maximum, color);
        if (configure != null) layer = configure(layer) ?? throw new InvalidOperationException("Radial layer configuration cannot return null.");
        return Add(layer);
    }

    /// <summary>Gets a collection enumerator.</summary>
    public IEnumerator<ChartRadialLayer> GetEnumerator() => _layers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
