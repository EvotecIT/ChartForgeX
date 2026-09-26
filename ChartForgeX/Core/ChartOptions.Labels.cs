namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the words ChartForgeX writes into charts on its own, such as the Gantt lane "Now" marker, so hosts can localize them.</summary>
    public ChartLabels Labels { get; } = new();
}
