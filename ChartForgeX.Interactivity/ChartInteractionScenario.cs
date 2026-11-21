using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Interactivity;

/// <summary>
/// Describes a named interactive path, flow, or alternate analytical state for a chart adapter.
/// </summary>
public sealed class ChartInteractionScenario {
    private string _id = string.Empty;
    private string _label = string.Empty;
    private int _playbackDelayMilliseconds = 900;
    private ChartInteractionScenarioFocusMode _focusMode = ChartInteractionScenarioFocusMode.Highlight;

    /// <summary>Gets or sets the stable scenario identifier.</summary>
    public string Id { get => _id; set => _id = ChartInteractionText.RequiredToken(value, nameof(value), "Interaction scenario ids"); }

    /// <summary>Gets or sets the scenario label shown by host controls.</summary>
    public string Label { get => _label; set => _label = ChartInteractionText.RequiredText(value, nameof(value), "Interaction scenario labels"); }

    /// <summary>Gets or sets an optional scenario description.</summary>
    public string? Description { get; set; }

    private string? _color;

    /// <summary>Gets or sets an optional scenario accent color: a CSS color such as <c>#2563eb</c>, <c>rgb(37 99 235)</c> or a named color.</summary>
    /// <exception cref="ArgumentException">The value contains characters outside a CSS color.</exception>
    public string? Color {
        get => _color;
        set {
            var color = string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
            if (color != null && (color.Length > 80 || !color.All(ch => char.IsLetterOrDigit(ch) || ch is '#' or '(' or ')' or ',' or '.' or '%' or '-' or ' ' or '/')))
                throw new ArgumentException("Scenario colors must be CSS colors.", nameof(value));
            _color = color;
        }
    }

    /// <summary>Gets or sets the default time each step remains visible during playback.</summary>
    public int PlaybackDelayMilliseconds {
        get => _playbackDelayMilliseconds;
        set {
            if (value < 200 || value > 60_000) throw new ArgumentOutOfRangeException(nameof(value), "Scenario playback delays must be between 200 and 60000 milliseconds.");
            _playbackDelayMilliseconds = value;
        }
    }

    /// <summary>Gets or sets whether playback returns to the first step after the last step.</summary>
    public bool LoopPlayback { get; set; }

    /// <summary>Gets or sets whether a browser adapter may start playback automatically when motion is allowed.</summary>
    public bool AutoPlay { get; set; }

    /// <summary>Gets or sets how strongly non-target context is visually de-emphasized.</summary>
    public ChartInteractionScenarioFocusMode FocusMode {
        get => _focusMode;
        set {
            if (!Enum.IsDefined(typeof(ChartInteractionScenarioFocusMode), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown scenario focus mode.");
            _focusMode = value;
        }
    }

    /// <summary>Gets ordered target references that participate in the scenario.</summary>
    public List<ChartInteractionScenarioStep> Steps { get; } = new();

    /// <summary>Gets arbitrary scenario metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// Describes one target reference inside a host-neutral interaction scenario.
/// </summary>
public sealed class ChartInteractionScenarioStep {
    private string _targetKind = string.Empty;
    private string _targetId = string.Empty;
    private int? _durationMilliseconds;

    /// <summary>Gets or sets the adapter-defined target kind, such as series, point, node, edge, or annotation.</summary>
    public string TargetKind { get => _targetKind; set => _targetKind = ChartInteractionText.RequiredToken(value, nameof(value), "Interaction scenario target kinds"); }

    /// <summary>Gets or sets the adapter-defined target identifier.</summary>
    public string TargetId { get => _targetId; set => _targetId = ChartInteractionText.RequiredText(value, nameof(value), "Interaction scenario target ids"); }

    /// <summary>Gets or sets an optional step label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets an optional step description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets an optional playback duration that overrides the scenario default.</summary>
    public int? DurationMilliseconds {
        get => _durationMilliseconds;
        set {
            if (value.HasValue && (value.Value < 200 || value.Value > 60_000)) throw new ArgumentOutOfRangeException(nameof(value), "Scenario step durations must be between 200 and 60000 milliseconds.");
            _durationMilliseconds = value;
        }
    }

    /// <summary>Gets arbitrary step metadata for route inspectors and host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>Controls whether a scenario preserves surrounding context or spotlights only its targets.</summary>
public enum ChartInteractionScenarioFocusMode {
    /// <summary>Emphasizes active targets while retaining the surrounding visual context.</summary>
    Highlight,
    /// <summary>Dims non-target data marks to create a stronger focused review.</summary>
    Spotlight
}
