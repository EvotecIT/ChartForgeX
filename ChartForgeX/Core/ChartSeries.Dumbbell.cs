using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartDumbbellOptions _dumbbell = new();

    /// <summary>Gets independent Start and End marker presentation for a dumbbell series.</summary>
    /// <exception cref="InvalidOperationException">The series is not a dumbbell.</exception>
    public ChartDumbbellOptions Dumbbell {
        get { RequireDumbbellOptions(); return _dumbbell; }
    }

    /// <summary>Configures endpoint presentation using detached callback state. A callback failure leaves the current settings unchanged.</summary>
    /// <param name="configure">Configures StartMarker and EndMarker overrides; unspecified settings inherit the shared series markers.</param>
    /// <returns>The current series.</returns>
    /// <exception cref="InvalidOperationException">The series is not a dumbbell.</exception>
    public ChartSeries ConfigureDumbbell(Action<ChartDumbbellOptions> configure) {
        RequireDumbbellOptions();
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        _dumbbell = ChartOptionConfiguration.Configure(_dumbbell, configure, options => options.Clone());
        return this;
    }

    /// <summary>Resolves a detached endpoint configuration through the canonical shared marker override owner.</summary>
    internal ChartMarkerOptions ResolveDumbbellMarker(bool start) => Markers.Overlay(start ? _dumbbell.StartMarker : _dumbbell.EndMarker);

    private void RequireDumbbellOptions() {
        if (Kind != ChartSeriesKind.Dumbbell) throw new InvalidOperationException("Series '" + Name + "' requires the Dumbbell kind for endpoint options.");
    }
}
