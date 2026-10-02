using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    private ChartMarkBackdrop _markBackdrop = ChartMarkBackdrop.Layered;

    /// <summary>
    /// Gets or sets the surface treated as the colour behind the marks (state pattern lines, outlined and quiet heatmap
    /// cells, neutral zero counts, text on translucent marks). The default <see cref="ChartMarkBackdrop.Layered"/> composites
    /// the theme background with the card and plot background where they are drawn; a host that places a transparent
    /// chart on its own card sets <see cref="ChartMarkBackdrop.Card"/>.
    /// </summary>
    public ChartMarkBackdrop MarkBackdrop {
        get => _markBackdrop;
        set {
            if (!Enum.IsDefined(typeof(ChartMarkBackdrop), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown mark backdrop.");
            _markBackdrop = value;
        }
    }
}
