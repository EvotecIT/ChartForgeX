using System;

namespace ChartForgeX.Topology;

public sealed partial class TopologyNode {
    private int? _paletteIndex;

    /// <summary>Gets or sets a zero-based categorical slot in the active theme palette; larger indexes cycle through that palette.</summary>
    /// <remarks>An explicit <see cref="Color"/> or resolved icon color takes precedence. Null derives the accent from health status. The palette does not change status badges or an explicit <see cref="BackgroundColor"/>.</remarks>
    public int? PaletteIndex {
        get => _paletteIndex;
        set {
            if (value is < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Palette indexes must be nonnegative.");
            _paletteIndex = value;
        }
    }
}

public sealed partial class TopologyEdge {
    private int? _paletteIndex;

    /// <summary>Gets or sets a zero-based categorical slot in the active theme palette; larger indexes cycle through that palette.</summary>
    /// <remarks>An explicit <see cref="Color"/> takes precedence. Null derives color from health status. Muted edges keep their neutral treatment.</remarks>
    public int? PaletteIndex {
        get => _paletteIndex;
        set {
            if (value is < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Palette indexes must be nonnegative.");
            _paletteIndex = value;
        }
    }
}
