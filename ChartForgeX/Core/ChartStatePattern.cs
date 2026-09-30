namespace ChartForgeX.Core;

/// <summary>
/// Defines how marks of a state category are filled, so states that share a colour (for example the neutral
/// "not observable", "unknown", and "could not evaluate") stay distinguishable without relying on colour.
/// </summary>
public enum ChartStatePattern {
    /// <summary>A solid fill in the category colour.</summary>
    Solid,

    /// <summary>The category colour with diagonal lines in the plot background colour.</summary>
    Hatched,

    /// <summary>The category colour with crossed diagonal lines in the plot background colour.</summary>
    CrossHatched,

    /// <summary>An outline in the category colour around a faint tint of it.</summary>
    Outlined
}

/// <summary>
/// Defines how much a state category draws the eye. It changes the weight of the mark, never its meaning: the
/// category keeps its colour, label, legend entry, and accessible text.
/// </summary>
public enum ChartStateEmphasis {
    /// <summary>The mark is drawn at full strength.</summary>
    Normal,

    /// <summary>The mark is drawn lighter, for the expected state (passed, up), so the other states stand out.</summary>
    Quiet
}