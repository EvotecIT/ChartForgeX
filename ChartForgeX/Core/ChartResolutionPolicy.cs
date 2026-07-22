using System;

namespace ChartForgeX.Core;

/// <summary>
/// Converts an intended render width into a bounded point budget for resolution-aware series.
/// </summary>
public sealed class ChartResolutionPolicy {
    private double _pointsPerPixel = 2d;
    private int _minimumPointCount = 64;
    private int _maximumPointCount = int.MaxValue;
    private int _maximumMarkerCount = 160;
    private ChartDecimationMode _decimationMode = ChartDecimationMode.LargestTriangleThreeBuckets;

    /// <summary>Gets or sets the number of rendered points allowed per horizontal pixel.</summary>
    public double PointsPerPixel {
        get => _pointsPerPixel;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d) throw new ArgumentOutOfRangeException(nameof(value), "Points per pixel must be finite and greater than zero.");
            _pointsPerPixel = value;
        }
    }

    /// <summary>Gets or sets the minimum point budget. The value must be at least three.</summary>
    public int MinimumPointCount {
        get => _minimumPointCount;
        set {
            if (value < 3) throw new ArgumentOutOfRangeException(nameof(value), "The minimum point count must be at least three.");
            if (value > _maximumPointCount) throw new ArgumentOutOfRangeException(nameof(value), "The minimum point count cannot exceed the maximum point count.");
            _minimumPointCount = value;
        }
    }

    /// <summary>Gets or sets the maximum point budget.</summary>
    public int MaximumPointCount {
        get => _maximumPointCount;
        set {
            if (value < 3) throw new ArgumentOutOfRangeException(nameof(value), "The maximum point count must be at least three.");
            if (value < _minimumPointCount) throw new ArgumentOutOfRangeException(nameof(value), "The maximum point count cannot be lower than the minimum point count.");
            _maximumPointCount = value;
        }
    }

    /// <summary>
    /// Gets or sets the largest retained point count that still shows optional line or area markers.
    /// Zero suppresses markers for every adaptive series while preserving the connected path.
    /// </summary>
    public int MaximumMarkerCount {
        get => _maximumMarkerCount;
        set {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "The maximum marker count cannot be negative.");
            _maximumMarkerCount = value;
        }
    }

    /// <summary>Gets or sets the deterministic decimation algorithm used when the source exceeds the resolved budget.</summary>
    public ChartDecimationMode DecimationMode {
        get => _decimationMode;
        set {
            if (!Enum.IsDefined(typeof(ChartDecimationMode), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _decimationMode = value;
        }
    }

    /// <summary>Creates the report-friendly trend policy: two points per pixel, a 64-point floor, and automatic marker-density control.</summary>
    public static ChartResolutionPolicy Trend() => new();

    /// <summary>Resolves the maximum rendered point count for an intended horizontal render width.</summary>
    public int ResolvePointBudget(double viewportWidth) {
        if (double.IsNaN(viewportWidth) || double.IsInfinity(viewportWidth) || viewportWidth < 0d) throw new ArgumentOutOfRangeException(nameof(viewportWidth), "Viewport width must be finite and non-negative.");
        var scaled = Math.Ceiling(viewportWidth * PointsPerPixel);
        if (scaled >= MaximumPointCount) return MaximumPointCount;
        return Math.Max(MinimumPointCount, (int)scaled);
    }
}
