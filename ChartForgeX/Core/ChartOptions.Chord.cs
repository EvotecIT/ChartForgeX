using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets native chord geometry, direction, and label settings.</summary>
    public ChartChordOptions Chord {
        get => _chord;
        set => _chord = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartChordOptions _chord = new();
}
