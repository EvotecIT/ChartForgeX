namespace ChartForgeX.Interactivity.Html;

/// <summary>Controls which rendered observations an HTML chart tooltip describes.</summary>
public enum HtmlChartTooltipMode {
    /// <summary>Describes the pointed or focused target, including its available metadata.</summary>
    Single,
    /// <summary>
    /// Shows one visible observation per series at the target's numeric x coordinate, retaining source values.
    /// Targets without comparable numeric x/y data use a single-target tooltip.
    /// </summary>
    SharedX
}
