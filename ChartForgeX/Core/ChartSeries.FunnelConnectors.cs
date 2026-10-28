using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private readonly List<ChartFunnelConnectorStyle?> _funnelConnectorStyles = new();
    private IReadOnlyList<ChartFunnelConnectorStyle?>? _funnelConnectorStylesView;

    /// <summary>Gets source-indexed paint for the outgoing connection from each Funnel observation.</summary>
    /// <remarks>The final observation can retain a style before another source is appended. Insert/remove/keyed changes carry the style with its source; clearing the data clears overrides.</remarks>
    public IReadOnlyList<ChartFunnelConnectorStyle?> FunnelConnectorStyles => _funnelConnectorStylesView ??= _funnelConnectorStyles.AsReadOnly();

    /// <summary>Sets an existing Funnel source's outgoing connection paint; null restores the family default.</summary>
    /// <param name="fromSourceIndex">The zero-based existing source observation, including the current last source.</param>
    /// <param name="style">Immutable paint, or null to inherit.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithFunnelConnectorStyle(int fromSourceIndex, ChartFunnelConnectorStyle? style) {
        if (Kind != ChartSeriesKind.Funnel) throw new NotSupportedException("Connection paint applies only to Funnel source observations.");
        ValidatePointIndex(fromSourceIndex);
        while (_funnelConnectorStyles.Count <= fromSourceIndex) _funnelConnectorStyles.Add(null);
        _funnelConnectorStyles[fromSourceIndex] = style;
        return this;
    }
}
