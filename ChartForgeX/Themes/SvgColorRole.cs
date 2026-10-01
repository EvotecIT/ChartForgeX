namespace ChartForgeX.Themes;

/// <summary>
/// The role of a colour variable in <see cref="SvgColorVariables"/>. When two variables share a colour, a paint written
/// for a role (a series bar, a status mark, a ramp step, the surface behind marks) takes the variable of that role, so
/// the property names the same token in every theme. Paints without a role match by colour alone.
/// </summary>
public enum SvgColorRole {
    /// <summary>No particular role: the variable is used for any paint of its colour.</summary>
    Any,

    /// <summary>
    /// A surface: page, card, plot, or line colour. Text matched by colour value never takes it; text a renderer writes
    /// in the surface colour on purpose (a count on a strong heatmap cell) does.
    /// </summary>
    Surface,

    /// <summary>A text colour.</summary>
    Text,

    /// <summary>A categorical series colour.</summary>
    Series,

    /// <summary>A severity, outcome, or operational-state colour.</summary>
    Status,

    /// <summary>A step of a sequential or diverging ramp.</summary>
    Ramp
}
