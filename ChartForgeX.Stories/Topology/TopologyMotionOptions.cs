using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines deterministic, script-free topology motion for SVG and sampled raster exports.
/// </summary>
public sealed class TopologyMotionOptions {
    private string? _scenarioId;
    private string? _markerColor;

    /// <summary>Gets or sets the scenario id used as the route source. When unset, the active scenario or first scenario is used.</summary>
    public string? ScenarioId { get => _scenarioId; set => _scenarioId = OptionalScenarioToken(value, nameof(value)); }

    /// <summary>Gets edge ids used as an explicit route source. When set, these edges are animated without requiring a scenario.</summary>
    public List<string> EdgeIds { get; } = new();

    /// <summary>Gets or sets the animation duration in seconds.</summary>
    public double DurationSeconds { get; set; } = 4;

    /// <summary>Gets or sets the sampled frame rate used by animated raster exports.</summary>
    public double FramesPerSecond { get; set; } = 12;

    /// <summary>Gets or sets the maximum number of frames sampled by animated raster exports.</summary>
    public int MaximumRasterFrames { get; set; } = 240;

    /// <summary>Gets or sets whether the animation loops.</summary>
    public bool Loop { get; set; } = true;

    /// <summary>Gets or sets the sampled progress from 0.0 to 1.0 used by raster frame rendering.</summary>
    public double Progress { get; set; }

    /// <summary>Gets or sets the moving route marker radius.</summary>
    public double MarkerRadius { get; set; } = 5.5;

    /// <summary>Gets or sets the optional moving route marker color. When unset, the scenario or edge color is used.</summary>
    public string? MarkerColor { get => _markerColor; set => _markerColor = string.IsNullOrWhiteSpace(value) ? null : value!.Trim(); }

    /// <summary>Gets or sets whether explicit edge motion should pulse source and target nodes.</summary>
    public bool PulseRouteEndpoints { get; set; } = true;

    /// <summary>
    /// Creates a route-pulse motion preset for topology scenarios and map routes.
    /// </summary>
    /// <param name="scenarioId">Optional scenario id to animate.</param>
    /// <param name="edgeIds">Optional explicit edge ids to animate without requiring a scenario.</param>
    /// <returns>Topology motion options.</returns>
    public static TopologyMotionOptions RoutePulse(string? scenarioId = null, params string[] edgeIds) {
        var options = new TopologyMotionOptions { ScenarioId = scenarioId };
        if (edgeIds != null) options.AddEdges(edgeIds);
        return options;
    }

    /// <summary>
    /// Creates a route-pulse motion preset from a topology scenario.
    /// </summary>
    /// <param name="scenarioId">Scenario id to animate.</param>
    /// <returns>Topology motion options.</returns>
    public static TopologyMotionOptions RoutePulseForScenario(string scenarioId) =>
        new TopologyMotionOptions { ScenarioId = RequiredScenarioToken(scenarioId, nameof(scenarioId)) };

    /// <summary>
    /// Creates a route-pulse motion preset from explicit topology edge ids.
    /// </summary>
    /// <param name="edgeIds">Explicit edge ids to animate without requiring a scenario.</param>
    /// <returns>Topology motion options.</returns>
    public static TopologyMotionOptions RoutePulseForEdges(params string[] edgeIds) {
        var options = new TopologyMotionOptions();
        options.AddEdges(edgeIds);
        if (options.EdgeIds.Count == 0) throw new ArgumentException("Topology motion edge routes require at least one edge id.", nameof(edgeIds));
        return options;
    }

    /// <summary>
    /// Adds explicit edge ids to the route motion source.
    /// </summary>
    /// <param name="edgeIds">The edge ids to animate.</param>
    /// <returns>The current motion options.</returns>
    public TopologyMotionOptions AddEdges(params string[] edgeIds) {
        if (edgeIds == null) throw new ArgumentNullException(nameof(edgeIds));
        foreach (var edgeId in edgeIds) EdgeIds.Add(RequiredEdgeId(edgeId, nameof(edgeIds)));
        return this;
    }

    /// <summary>
    /// Sets the animation duration.
    /// </summary>
    /// <param name="seconds">The duration in seconds.</param>
    /// <returns>The current motion options.</returns>
    public TopologyMotionOptions WithDuration(double seconds) {
        DurationSeconds = RequiredPositiveFinite(seconds, nameof(seconds), "Topology motion duration must be a positive finite number.");
        return this;
    }

    /// <summary>
    /// Sets the sampled frame rate used by animated raster exports.
    /// </summary>
    /// <param name="framesPerSecond">The sampled frame rate.</param>
    /// <returns>The current motion options.</returns>
    public TopologyMotionOptions WithFrameRate(double framesPerSecond) {
        FramesPerSecond = RequiredPositiveFinite(framesPerSecond, nameof(framesPerSecond), "Topology motion frame rate must be a positive finite number.");
        return this;
    }

    /// <summary>
    /// Sets the maximum number of frames sampled by animated raster exports.
    /// </summary>
    /// <param name="maximumFrames">The maximum sampled frame count.</param>
    /// <returns>The current motion options.</returns>
    public TopologyMotionOptions WithFrameLimit(int maximumFrames) {
        if (maximumFrames <= 0) throw new ArgumentOutOfRangeException(nameof(maximumFrames), maximumFrames, "Topology motion raster frame limit must be greater than zero.");
        MaximumRasterFrames = maximumFrames;
        return this;
    }

    /// <summary>
    /// Sets the moving route marker radius and optional color.
    /// </summary>
    /// <param name="radius">The marker radius.</param>
    /// <param name="color">Optional marker color.</param>
    /// <returns>The current motion options.</returns>
    public TopologyMotionOptions WithMarker(double radius, string? color = null) {
        MarkerRadius = RequiredPositiveFinite(radius, nameof(radius), "Topology motion marker radius must be a positive finite number.");
        MarkerColor = color;
        return this;
    }

    /// <summary>
    /// Sets the optional moving route marker color.
    /// </summary>
    /// <param name="color">The marker color. Empty values clear the override.</param>
    /// <returns>The current motion options.</returns>
    public TopologyMotionOptions WithMarkerColor(string? color) {
        MarkerColor = color;
        return this;
    }

    /// <summary>
    /// Sets the sampled progress used by static raster frame rendering.
    /// </summary>
    /// <param name="progress">Progress from 0.0 to 1.0.</param>
    /// <returns>The current motion options.</returns>
    public TopologyMotionOptions AtProgress(double progress) {
        if (progress < 0 || progress > 1 || double.IsNaN(progress) || double.IsInfinity(progress)) throw new ArgumentOutOfRangeException(nameof(progress), progress, "Topology motion progress must be between 0.0 and 1.0.");
        Progress = progress;
        return this;
    }

    /// <summary>
    /// Enables or disables endpoint node pulses for explicit-edge route motion.
    /// </summary>
    /// <param name="enabled">Whether endpoint nodes should pulse.</param>
    /// <returns>The current motion options.</returns>
    public TopologyMotionOptions WithEndpointPulses(bool enabled = true) {
        PulseRouteEndpoints = enabled;
        return this;
    }

    internal TopologyMotionOptions Clone() {
        var copy = new TopologyMotionOptions {
            ScenarioId = ScenarioId,
            DurationSeconds = DurationSeconds,
            FramesPerSecond = FramesPerSecond,
            MaximumRasterFrames = MaximumRasterFrames,
            Loop = Loop,
            Progress = Progress,
            MarkerRadius = MarkerRadius,
            MarkerColor = MarkerColor,
            PulseRouteEndpoints = PulseRouteEndpoints
        };
        copy.EdgeIds.AddRange(EdgeIds);
        return copy;
    }

    internal void Validate() {
        if (DurationSeconds <= 0 || double.IsNaN(DurationSeconds) || double.IsInfinity(DurationSeconds)) throw new ArgumentOutOfRangeException(nameof(DurationSeconds), DurationSeconds, "Topology motion duration must be a positive finite number.");
        if (FramesPerSecond <= 0 || double.IsNaN(FramesPerSecond) || double.IsInfinity(FramesPerSecond)) throw new ArgumentOutOfRangeException(nameof(FramesPerSecond), FramesPerSecond, "Topology motion frame rate must be a positive finite number.");
        if (MaximumRasterFrames <= 0) throw new ArgumentOutOfRangeException(nameof(MaximumRasterFrames), MaximumRasterFrames, "Topology motion raster frame limit must be greater than zero.");
        if (MarkerRadius <= 0 || double.IsNaN(MarkerRadius) || double.IsInfinity(MarkerRadius)) throw new ArgumentOutOfRangeException(nameof(MarkerRadius), MarkerRadius, "Topology motion marker radius must be a positive finite number.");
        if (Progress < 0 || Progress > 1 || double.IsNaN(Progress) || double.IsInfinity(Progress)) throw new ArgumentOutOfRangeException(nameof(Progress), Progress, "Topology motion progress must be between 0.0 and 1.0.");
        foreach (var edgeId in EdgeIds) {
            RequiredEdgeId(edgeId, nameof(EdgeIds));
        }
    }

    private static string? OptionalScenarioToken(string? value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? null : RequiredScenarioToken(value!, parameterName);

    private static string RequiredScenarioToken(string value, string parameterName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("Topology motion scenario ids cannot be empty.", parameterName);
        foreach (var ch in trimmed) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') continue;
            throw new ArgumentException("Topology motion scenario ids may contain only letters, digits, dots, underscores, and hyphens.", parameterName);
        }

        return trimmed;
    }

    private static string RequiredEdgeId(string? value, string parameterName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("Topology motion edge ids cannot be empty.", parameterName);
        return trimmed;
    }

    private static double RequiredPositiveFinite(double value, string parameterName, string message) {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, message);
        return value;
    }
}
