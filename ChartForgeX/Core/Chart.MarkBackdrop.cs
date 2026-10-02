namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Sets the surface treated as the colour behind the marks (see <see cref="ChartOptions.MarkBackdrop"/>), for
    /// example <see cref="ChartMarkBackdrop.Card"/> for a transparent chart placed on a host's card.
    /// </summary>
    /// <param name="backdrop">The surface.</param>
    /// <returns>The current chart.</returns>
    public Chart WithMarkBackdrop(ChartMarkBackdrop backdrop) {
        Options.MarkBackdrop = backdrop;
        return this;
    }
}
