namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the shared week, localized names, and cell sizing options for calendar heatmaps.</summary>
    public ChartCalendarOptions Calendar { get; internal set; } = new();
}
