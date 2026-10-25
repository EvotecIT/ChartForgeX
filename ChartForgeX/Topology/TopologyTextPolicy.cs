using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

/// <summary>Resolves reusable topology typography over an already selected native face policy.</summary>
internal static class TopologyTextPolicy {
    internal static TextStyle Resolve(TextStyleOverride? policy, FontSpec font, double size, int weight) {
        var fallback = new TextStyle { Font = font.Clone(), FontSize = size, LineHeight = 1 };
        fallback.Font.Weight = weight;
        var resolved = policy?.Resolve(fallback) ?? fallback;
        // Native emphasis is the fallback; an explicit authored weight remains authoritative.
        return resolved;
    }
    /// <summary>Structured captions deliberately start regular; an authored base weight still takes precedence.</summary>
    internal static TextStyle ResolveStructured(TextStyleOverride? policy, FontSpec font, double size) {
        var fallback = new TextStyle { Font = font.Clone(), FontSize = size, LineHeight = 1 };
        fallback.Font.Weight = 400;
        return policy?.Resolve(fallback) ?? fallback;
    }
    internal static TextStyle ResolveRole(TextStyleOverride? policy, TextStyleOverride? role, FontSpec font, double size, int weight) {
        var style = role?.Resolve(Resolve(policy, font, size, weight)) ?? Resolve(policy, font, size, weight);
        style.FontSize = size; // The shared role owner has already selected this logical or fitted candidate.
        return style;
    }
    internal static double Size(TextStyleOverride? policy, double ordinary, double baseSize) =>
        policy?.FontSize is double authored ? ordinary * authored / baseSize : ordinary;
    internal static TextMeasurementContext Measurement(TextStyleOverride? policy, TextMeasurementContext measurement, int weight) =>
        policy == null || !policy.HasOverrides ? measurement
            : new TextMeasurementContext(Resolve(policy, measurement.Font, 1, weight).Font, measurement.Mode);
}
