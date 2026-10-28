using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private ChartAxis? _bulletAxis;
    /// <summary>Gets a detached copy of the optional local numeric bullet axis. Null uses the reading domain and default axis presentation.</summary>
    /// <remarks>Changes to this copy are not installed. Use ConfigureBulletAxis; local axes require ChartBulletOptions.IndependentAxes.</remarks>
    public ChartAxis? BulletAxis => _bulletAxis?.Clone();

    /// <summary>Atomically captures one bullet row's numeric bounds, scale, reversal and caption policy.</summary>
    public ChartSeries ConfigureBulletAxis(Action<ChartAxis> configure) {
        RequireObservationKind(ChartSeriesKind.Bullet);
        var axis = ChartOptionConfiguration.Configure(_bulletAxis ?? new ChartAxis(), configure, candidate => candidate.Clone());
        ValidateBulletAxis(axis); _bulletAxis = axis; return this;
    }

    /// <summary>Clears this row's local axis; the reading's domain remains unchanged.</summary>
    public ChartSeries ClearBulletAxis() { RequireObservationKind(ChartSeriesKind.Bullet); _bulletAxis = null; return this; }

    internal ChartAxis CaptureBulletAxis() => _bulletAxis?.Clone() ?? new ChartAxis();
    internal bool HasBulletAxis => _bulletAxis != null;
    private static void ValidateBulletAxis(ChartAxis axis) {
        if (axis.Scale == ChartScaleKind.Time || axis.TimeSessions != null || axis.TimeSpacing != null)
            throw new NotSupportedException("Bullet row axes require a numeric scale without time spacing or sessions.");
        if (axis.LabelOrientation != ChartAxisLabelOrientation.Fixed || axis.LabelAngle != 0 || axis.CrossingCoordinate.HasValue)
            throw new NotSupportedException("Bullet row axes use fixed unrotated scale captions and their track-edge rule.");
        foreach (var label in axis.Labels) if (label.Text == null)
            throw new InvalidOperationException("Bullet row axis labels require text.");
    }
}
