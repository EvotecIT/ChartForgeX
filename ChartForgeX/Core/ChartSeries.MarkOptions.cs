namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets optional series hooks and default metadata for marks without their own options.</summary>
    public ChartMarkOptions? MarkOptions { get; private set; }

    /// <summary>Configures series hooks and the default navigation/accessibility policy for its marks.</summary>
    /// <param name="options">Immutable metadata, or null to clear it. Item options take precedence.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithMarkOptions(ChartMarkOptions? options) { MarkOptions = options; return this; }

    /// <summary>Gets optional metadata for a schedule row's label/summary or matrix row group.</summary>
    public ChartMarkOptions? RowOptions { get; private set; }

    /// <summary>Configures independent row hooks, navigation and current/selected state.</summary>
    /// <param name="options">Immutable row metadata, or null to clear it.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithRowOptions(ChartMarkOptions? options) { RowOptions = options; return this; }
}
