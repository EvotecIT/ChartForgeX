using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets this dotted-map dataset's weight domain, radius and size-key policy. Explicit <see cref="MarkerRadius"/> takes precedence.</summary>
    public ChartMapPointSizeOptions MapPointSize { get; private set; } = new();

    /// <summary>Configures one dotted-map dataset's marker sizes atomically, independently of its geographic coordinates and numeric color.</summary>
    /// <param name="configure">Edits detached size options; a thrown callback leaves the installed options unchanged.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ConfigureMapPointSize(Action<ChartMapPointSizeOptions> configure) {
        RequireObservationKind(ChartSeriesKind.DottedMap);
        MapPointSize = ChartOptionConfiguration.Configure(MapPointSize, configure, options => options.Clone());
        return this;
    }

    internal void ValidateMapPointSize() {
        if (Kind != ChartSeriesKind.DottedMap && MapPointSize.HasOverrides)
            throw new InvalidOperationException("Map point size options require a DottedMap series.");
        MapPointSize.Validate();
    }
}
