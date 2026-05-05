using System;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Holds host-neutral interaction settings that can be consumed by HTML, desktop, or future interactive adapters.
/// </summary>
public sealed class ChartInteractionOptions {
    private string? _chartId;
    private string? _groupName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartInteractionOptions"/> class.
    /// </summary>
    public ChartInteractionOptions() {
        Features = ChartInteractionFeatures.ReportReview;
    }

    /// <summary>
    /// Gets or sets a stable chart identifier used by host adapters for DOM IDs, chart registries, or event routing.
    /// </summary>
    public string? ChartId {
        get => _chartId;
        set => _chartId = NormalizeOptionalToken(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the optional synchronized-chart group name.
    /// </summary>
    public string? GroupName {
        get => _groupName;
        set => _groupName = NormalizeOptionalToken(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the enabled interaction feature flags.
    /// </summary>
    public ChartInteractionFeatures Features { get; set; }

    /// <summary>
    /// Returns a configured copy of the default report-review interaction profile.
    /// </summary>
    /// <returns>A new default interaction options instance.</returns>
    public static ChartInteractionOptions ReportReview() => new();

    /// <summary>
    /// Determines whether the specified interaction feature is enabled.
    /// </summary>
    /// <param name="feature">The interaction feature to check.</param>
    /// <returns><c>true</c> when the feature is enabled; otherwise, <c>false</c>.</returns>
    public bool HasFeature(ChartInteractionFeatures feature) => feature != ChartInteractionFeatures.None && (Features & feature) == feature;

    /// <summary>
    /// Enables one or more interaction features.
    /// </summary>
    /// <param name="features">The features to enable.</param>
    /// <returns>The current options instance.</returns>
    public ChartInteractionOptions Enable(ChartInteractionFeatures features) {
        Features |= features;
        return this;
    }

    /// <summary>
    /// Disables one or more interaction features.
    /// </summary>
    /// <param name="features">The features to disable.</param>
    /// <returns>The current options instance.</returns>
    public ChartInteractionOptions Disable(ChartInteractionFeatures features) {
        Features &= ~features;
        return this;
    }

    private static string? NormalizeOptionalToken(string? value, string parameterName) {
        if (value == null) return null;
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("Interaction identifiers must not be empty.", parameterName);
        return trimmed;
    }
}
