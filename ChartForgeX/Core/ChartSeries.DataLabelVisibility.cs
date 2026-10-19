namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Resolves an explicit series override, then the chart setting, then the owning family's established default.</summary>
    internal bool ResolveDataLabels(ChartOptions options, bool defaultVisible) =>
        ShowDataLabels ?? options.ShowDataLabels ?? defaultVisible;
}
