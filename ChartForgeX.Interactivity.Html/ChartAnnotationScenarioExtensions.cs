using System;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Connects authored native annotation identities to host-neutral scenario routes.</summary>
public static class ChartAnnotationScenarioExtensions {
    /// <summary>Adds an exact authored annotation reference independent of caption, coordinates and source ordering.</summary>
    /// <param name="scenario">The receiving scenario.</param>
    /// <param name="annotationId">The native source identity.</param>
    /// <param name="label">An optional step label.</param>
    /// <param name="description">An optional step description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static ChartInteractionScenario AddAnnotationStep(this ChartInteractionScenario scenario, ChartAnnotationId annotationId,
        string? label = null, string? description = null, Action<ChartInteractionScenarioStep>? configure = null) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        return ChartInteractionScenarioExtensions.AddAnnotationStep(scenario, VisualAnnotationIdentity.SourceIdFor(annotationId), label, description, configure);
    }
}
