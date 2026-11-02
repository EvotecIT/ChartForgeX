using System;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Configures browser motion in interactive HTML. Static SVG and PNG output never animates.</summary>
/// <remarks>Readers who ask for reduced motion see every change immediately.</remarks>
public sealed class HtmlChartMotionOptions {
    private int _durationMilliseconds = 300;

    /// <summary>Gets or sets whether charts animate on load and ease toolbar zoom, brush zoom, reset and legend rescaling. The default is true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the ease-out duration in milliseconds, from 0 to 1000. The default is 300.</summary>
    public int DurationMilliseconds {
        get => _durationMilliseconds;
        set {
            if (value < 0 || value > 1000) throw new ArgumentOutOfRangeException(nameof(value), "Motion duration must be between 0 and 1000 milliseconds.");
            _durationMilliseconds = value;
        }
    }

    internal int EffectiveDuration => Enabled ? DurationMilliseconds : 0;

    internal void CopyTo(HtmlChartMotionOptions destination) {
        destination.Enabled = Enabled;
        destination.DurationMilliseconds = DurationMilliseconds;
    }
}
