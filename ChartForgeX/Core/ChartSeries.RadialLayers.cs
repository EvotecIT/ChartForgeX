using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private IReadOnlyList<ChartRadialLayer> _radialLayers = Array.Empty<ChartRadialLayer>();

    /// <summary>Replaces the authored radial layers with a detached collection and clears per-observation keys and overrides.</summary>
    /// <remarks>Layers are copied on intake. Their owned typed values and presentation can subsequently be edited through <see cref="RadialLayers"/>.</remarks>
    public ChartSeries ReplaceRadialLayers(IEnumerable<ChartRadialLayer> layers) {
        RequireObservationKind(ChartSeriesKind.LayeredRadial);
        ReplaceObservations(ref _radialLayers, layers, CopyRadialLayer);
        return this;
    }

    /// <summary>Replaces one radial layer while preserving its explicit observation key and presentation overrides.</summary>
    public ChartSeries UpdateRadialLayer(int index, ChartRadialLayer layer) {
        RequireObservationKind(ChartSeriesKind.LayeredRadial);
        UpdateObservation(ref _radialLayers, index, layer, CopyRadialLayer);
        return this;
    }

    /// <summary>Creates a radial group from an ordered snapshot of its typed layers.</summary>
    public static ChartSeries CreateLayeredRadial(ChartSeriesId id, string name, IEnumerable<ChartRadialLayer> layers) =>
        new ChartSeries(id, name, ChartSeriesKind.LayeredRadial, Array.Empty<ChartPoint>()).ReplaceRadialLayers(layers);

    internal static ChartSeries CreateLayeredRadial(string name, IEnumerable<ChartRadialLayer> layers) =>
        new ChartSeries(name, ChartSeriesKind.LayeredRadial, Array.Empty<ChartPoint>()).ReplaceRadialLayers(layers);

    private static ChartRadialLayer CopyRadialLayer(ChartRadialLayer layer) {
        if (layer == null) throw new ArgumentException("Layered radial charts cannot contain null layers.", nameof(layer));
        return new ChartRadialLayer(layer.Name, layer.Value, layer.Minimum, layer.Maximum, layer.Color) {
            RadiusRatio = layer.RadiusRatio, StrokeRatio = layer.StrokeRatio, StartAngleDegrees = layer.StartAngleDegrees,
            SweepAngleDegrees = layer.SweepAngleDegrees, Opacity = layer.Opacity, LineCap = layer.LineCap,
            SeparatorCount = layer.SeparatorCount, SeparatorColor = layer.SeparatorColor,
            SeparatorStrokeWidth = layer.SeparatorStrokeWidth, SeparatorInsetRatio = layer.SeparatorInsetRatio
        };
    }
}
