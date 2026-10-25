using System;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidC4Document {
    private int? _maximumShapesPerRow, _maximumBoundariesPerRow;
    /// <summary>Gets or sets an authored UpdateLayoutConfig shape-row limit; null keeps the native default layout.</summary>
    public int? MaximumShapesPerRow {
        get => _maximumShapesPerRow;
        set { if (value is <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _maximumShapesPerRow = value; }
    }
    /// <summary>Gets or sets an authored UpdateLayoutConfig boundary-row limit; null keeps the native default layout.</summary>
    public int? MaximumBoundariesPerRow {
        get => _maximumBoundariesPerRow;
        set { if (value is <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _maximumBoundariesPerRow = value; }
    }
}
