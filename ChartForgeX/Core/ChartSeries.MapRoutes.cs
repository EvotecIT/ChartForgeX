using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private IReadOnlyList<ChartMapConnector> _mapConnectors = Array.Empty<ChartMapConnector>();

    /// <summary>Gets the detached ordered route facts owned by this dotted-map dataset, independently of its point observations.</summary>
    public IReadOnlyList<ChartMapConnector> MapConnectors => _mapConnectors;

    /// <summary>Replaces this dotted-map dataset's route facts atomically, copying every route and waypoint.</summary>
    /// <param name="connectors">The ordered complete route collection. Empty removes the dataset's routes.</param>
    /// <returns>The current series. Point observations, identities and presentation are unchanged.</returns>
    public ChartSeries ReplaceMapConnectors(IEnumerable<ChartMapConnector> connectors) {
        RequireObservationKind(ChartSeriesKind.DottedMap);
        if (connectors == null) throw new ArgumentNullException(nameof(connectors));
        var snapshot = new List<ChartMapConnector>();
        foreach (var connector in connectors) snapshot.Add(connector.ValidatedCopy());
        _mapConnectors = snapshot.AsReadOnly();
        return this;
    }
}
