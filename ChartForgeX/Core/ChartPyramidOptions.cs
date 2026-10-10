using System;

namespace ChartForgeX.Core;

/// <summary>Controls triangular partitions without sorting or changing their source categories.</summary>
public sealed class ChartPyramidOptions {
    private ChartPyramidValueEncoding _valueEncoding;
    private ChartOrientation _orientation;
    private double? _aspectRatio;

    /// <summary>Gets or sets the proportional quantity. The default is segment height, independently of orientation.</summary>
    public ChartPyramidValueEncoding ValueEncoding {
        get => _valueEncoding;
        set {
            if (!Enum.IsDefined(typeof(ChartPyramidValueEncoding), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _valueEncoding = value;
        }
    }

    /// <summary>Gets or sets the tip-to-base direction. The default places the tip above the base.</summary>
    public ChartOrientation Orientation {
        get => _orientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _orientation = value;
        }
    }

    /// <summary>Gets or sets whether the tip-to-base direction is reversed. Category order and source identities stay unchanged.</summary>
    public bool Reversed { get; set; }

    /// <summary>Gets or sets the base width divided by tip-to-base length, independently of orientation.</summary>
    /// <remarks>A positive finite ratio fits and centers the triangle in the available content. Null uses the available proportions.</remarks>
    public double? AspectRatio {
        get => _aspectRatio;
        set {
            if (value.HasValue) {
                ChartGuards.Finite(value.Value, nameof(value));
                if (value.Value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Pyramid aspect ratio must be positive.");
            }
            _aspectRatio = value;
        }
    }
}
