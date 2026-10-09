using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    internal Chart PanelView() {
        if (!Options.Theme.UseGraphiteLayout) return this;
        var view = (Chart)MemberwiseClone();
        view.Options = Options.PanelView();
        return view;
    }
    /// <summary>Assigns a semantic colour to the Sankey node with the specified authored ID.</summary>
    public Chart WithSankeyNodeState(string id, ChartSeriesState state) {
        if (!Enum.IsDefined(typeof(ChartSeriesState), state)) throw new ArgumentOutOfRangeException(nameof(state));
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Node ID must not be empty.", nameof(id));
        var found = false;
        foreach (var series in Series) if (series.Kind == ChartSeriesKind.Sankey && series.Relationships?.ContainsNode(id) == true) { found = true; break; }
        if (!found) throw new ArgumentException("The chart has no Sankey node with this ID.", nameof(id));
        Options.SankeyNodeStates[id] = state;
        return this;
    }
    /// <summary>Omits the outer chart frame and padding when the host already provides them.</summary>
    public Chart WithHostFrame(bool hostOwnsFrame = true) { Options.HostOwnsFrame = hostOwnsFrame; return this; }
    /// <summary>Sets optional line markers while keeping all data available for interaction and accessibility.</summary>
    public Chart WithLineMarkers(ChartLineMarkerMode mode) {
        if (!Enum.IsDefined(typeof(ChartLineMarkerMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        Options.LineMarkerMode = mode; return this;
    }
    /// <summary>Declares a series state independently of its display name and the categorical palette.</summary>
    public Chart WithSeriesState(string name, ChartSeriesState role) {
        if (!Enum.IsDefined(typeof(ChartSeriesState), role)) throw new ArgumentOutOfRangeException(nameof(role));
        var found = false;
        foreach (var series in Series) if (series.Name == name) { series.StateRole = role; found = true; }
        if (!found) throw new ArgumentException("The chart has no series with this name.", nameof(name));
        return this;
    }
}
