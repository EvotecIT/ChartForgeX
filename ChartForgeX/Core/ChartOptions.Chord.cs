namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets native chord geometry, direction, and label settings.</summary>
    public ChartChordOptions Chord { get; } = new();
}
