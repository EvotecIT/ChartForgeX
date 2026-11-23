namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets optional series hooks and default metadata for marks without their own options.</summary>
    public ChartMarkOptions? MarkOptions { get; set; }

    /// <summary>Gets optional metadata for a schedule row's label/summary or matrix row group.</summary>
    public ChartMarkOptions? RowOptions { get; set; }
}
