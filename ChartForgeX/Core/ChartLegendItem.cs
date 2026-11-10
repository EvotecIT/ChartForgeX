using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>An immutable legend entry that does not come from a series, such as a reference line, threshold band or annotation.</summary>
/// <remarks>Custom items follow the series entries. They describe the chart and are never visibility toggles.</remarks>
public sealed class ChartLegendItem {
    private readonly double[]? _dash;

    private ChartLegendItem(string label, ChartColor color, ChartLegendKeyShape key, ChartMarkerShape markerShape,
        double[]? dash, double strokeWidth, double fillOpacity, double? keySize) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Color = color; Key = key; MarkerShape = markerShape; StrokeWidth = strokeWidth; FillOpacity = fillOpacity; KeySize = keySize;
        _dash = dash;
        Dash = dash == null ? null : Array.AsReadOnly(dash);
    }

    /// <summary>Gets the dash and gap lengths chart reference lines use by default.</summary>
    public static IReadOnlyList<double> ReferenceDash { get; } = Array.AsReadOnly(new[] { 6d, 5d });

    /// <summary>Gets the label shown beside the key.</summary>
    public string Label { get; }

    /// <summary>Gets the key colour.</summary>
    public ChartColor Color { get; }

    /// <summary>Gets the key shape: <see cref="ChartLegendKeyShape.Line"/>, <see cref="ChartLegendKeyShape.Square"/> or <see cref="ChartLegendKeyShape.Marker"/>.</summary>
    public ChartLegendKeyShape Key { get; }

    /// <summary>Gets the marker shape drawn by a <see cref="ChartLegendKeyShape.Marker"/> key.</summary>
    public ChartMarkerShape MarkerShape { get; }

    /// <summary>Gets the dash and gap lengths of a line key, or null for a solid line.</summary>
    public IReadOnlyList<double>? Dash { get; }

    /// <summary>Gets the stroke width of a line key, or of a band key's outline.</summary>
    public double StrokeWidth { get; }

    /// <summary>Gets the fill opacity of a band key.</summary>
    public double FillOpacity { get; }

    /// <summary>Gets the key size in chart pixels, or null to use the legend's key size.</summary>
    public double? KeySize { get; }

    internal double[]? DashArray => _dash;

    /// <summary>Creates a line key, for example for a target or reference line.</summary>
    /// <param name="label">The legend label.</param>
    /// <param name="color">The line colour.</param>
    /// <param name="dash">Optional dash and gap lengths from 0.25 through 1024 chart pixels; null or empty draws a solid line. <see cref="ReferenceDash"/> matches chart reference lines.</param>
    /// <param name="strokeWidth">The line width, greater than zero and at most eight chart pixels.</param>
    public static ChartLegendItem Line(string label, ChartColor color, IEnumerable<double>? dash = null, double strokeWidth = 2) {
        ChartGuards.Finite(strokeWidth, nameof(strokeWidth));
        if (strokeWidth <= 0 || strokeWidth > 8) throw new ArgumentOutOfRangeException(nameof(strokeWidth), strokeWidth, "Legend line width must be greater than zero and at most eight chart pixels.");
        var pattern = dash?.ToArray();
        if (pattern != null) foreach (var length in pattern) {
            ChartGuards.Finite(length, nameof(dash));
            if (length < .25 || length > 1024) throw new ArgumentOutOfRangeException(nameof(dash), "Dash lengths must be from 0.25 through 1024 chart pixels.");
        }
        return new ChartLegendItem(label, color, ChartLegendKeyShape.Line, ChartMarkerShape.Circle,
            pattern == null || pattern.Length == 0 ? null : pattern, strokeWidth, 1, null);
    }

    /// <summary>Creates a filled square key outlined in the item colour, for example for a threshold band or target range.</summary>
    /// <param name="label">The legend label.</param>
    /// <param name="color">The band colour.</param>
    /// <param name="fillOpacity">The fill opacity from zero through one; one draws a solid swatch.</param>
    public static ChartLegendItem Band(string label, ChartColor color, double fillOpacity = .2) {
        ChartGuards.UnitInterval(fillOpacity, nameof(fillOpacity));
        return new ChartLegendItem(label, color, ChartLegendKeyShape.Square, ChartMarkerShape.Square, null, 1, fillOpacity, null);
    }

    /// <summary>Creates a marker key, for example for an annotation or event marker.</summary>
    /// <param name="label">The legend label.</param>
    /// <param name="color">The marker colour.</param>
    /// <param name="shape">The marker shape.</param>
    public static ChartLegendItem Marker(string label, ChartColor color, ChartMarkerShape shape = ChartMarkerShape.Circle) {
        if (!Enum.IsDefined(typeof(ChartMarkerShape), shape)) throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown marker shape.");
        return new ChartLegendItem(label, color, ChartLegendKeyShape.Marker, shape, null, 1, 1, null);
    }

    /// <summary>Returns a copy of this item with its own key size.</summary>
    /// <param name="size">The key size from 4 through 24 chart pixels.</param>
    public ChartLegendItem WithKeySize(double size) =>
        new(Label, Color, Key, MarkerShape, _dash, StrokeWidth, FillOpacity, ChartLegendOptions.ValidateKeySize(size, nameof(size)));
}
