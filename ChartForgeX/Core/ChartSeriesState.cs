namespace ChartForgeX.Core;

/// <summary>Declares the semantic colour and drawing priority of a series.</summary>
public enum ChartSeriesState {
    /// <summary>Uses an ordinary categorical colour.</summary>
    None,
    /// <summary>Uses the danger colour, drawn above other states.</summary>
    Danger,
    /// <summary>Uses the warning colour.</summary>
    Warning,
    /// <summary>Uses the quiet healthy colour and draws beneath other states.</summary>
    Success,
    /// <summary>Uses the quiet healthy colour and draws beneath other states.</summary>
    Quiet,
    /// <summary>Uses the informational state colour.</summary>
    Info,
    /// <summary>Uses the neutral colour.</summary>
    Neutral
}
