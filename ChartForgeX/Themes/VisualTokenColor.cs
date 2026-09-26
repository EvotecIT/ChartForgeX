using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// A status colour pair: <see cref="Fill"/> paints marks, bars, and dots; <see cref="Ink"/> is the text colour on
/// card surfaces and on tinted chips of the fill.
/// </summary>
public readonly struct VisualTokenColor {
    /// <summary>Initializes a status colour pair.</summary>
    /// <param name="fill">The mark fill colour.</param>
    /// <param name="ink">The text colour.</param>
    public VisualTokenColor(ChartColor fill, ChartColor ink) {
        Fill = fill;
        Ink = ink;
    }

    /// <summary>Gets the mark fill colour.</summary>
    public ChartColor Fill { get; }

    /// <summary>Gets the text colour.</summary>
    public ChartColor Ink { get; }
}
