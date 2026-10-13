using System;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Connects retained chart series identities to host-neutral scenario routes.</summary>
public static class ChartSeriesScenarioExtensions {
    /// <summary>Adds a scenario step targeting the retained series identity, independent of display text and paint order.</summary>
    /// <param name="scenario">The scenario receiving the ordered step.</param>
    /// <param name="seriesId">The stable series identity supplied at construction or allocated by its chart.</param>
    /// <param name="label">The optional step label.</param>
    /// <param name="description">The optional step description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario AddSeriesStep(this ChartInteractionScenario scenario, ChartSeriesId seriesId,
        string? label = null, string? description = null, Action<ChartInteractionScenarioStep>? configure = null) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        return ChartInteractionScenarioExtensions.AddSeriesStep(scenario, seriesId.Value, label, description, configure);
    }
}
