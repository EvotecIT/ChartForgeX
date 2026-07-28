using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Motion;

/// <summary>Defines the script-free entrance or emphasis effect applied to a named visual target.</summary>
public enum VisualMotionEffect {
    /// <summary>Fades the target into view.</summary>
    Fade,

    /// <summary>Moves the target upward while fading it into view.</summary>
    Rise,

    /// <summary>Reveals the target from left to right.</summary>
    Reveal,

    /// <summary>Gently scales the target into view.</summary>
    Scale,

    /// <summary>Applies one restrained emphasis pulse without hiding the target.</summary>
    Pulse
}

/// <summary>Defines the timing curve used by a visual motion cue.</summary>
public enum VisualMotionEasing {
    /// <summary>Uses constant-speed motion.</summary>
    Linear,

    /// <summary>Decelerates smoothly into the final state.</summary>
    EaseOut,

    /// <summary>Accelerates and decelerates smoothly.</summary>
    EaseInOut,

    /// <summary>Uses a restrained emphasized easing curve suitable for report storytelling.</summary>
    Emphasized
}

/// <summary>Describes one deterministic motion cue for a named target.</summary>
public sealed class VisualMotionCue {
    private string _targetId;
    private VisualMotionEffect _effect;
    private VisualMotionEasing _easing = VisualMotionEasing.Emphasized;
    private double _delaySeconds;
    private double _durationSeconds = 0.7;
    private double _distancePixels = 12;

    /// <summary>Creates a motion cue for a named target.</summary>
    public VisualMotionCue(string targetId, VisualMotionEffect effect) {
        _targetId = VisualMotionGuards.RequiredTargetId(targetId, nameof(targetId));
        Effect = effect;
    }

    /// <summary>Gets or sets the stable target id.</summary>
    public string TargetId {
        get => _targetId;
        set => _targetId = VisualMotionGuards.RequiredTargetId(value, nameof(value));
    }

    /// <summary>Gets or sets the visual effect.</summary>
    public VisualMotionEffect Effect {
        get => _effect;
        set {
            VisualMotionGuards.EnumDefined(value, nameof(value));
            _effect = value;
        }
    }

    /// <summary>Gets or sets the easing curve.</summary>
    public VisualMotionEasing Easing {
        get => _easing;
        set {
            VisualMotionGuards.EnumDefined(value, nameof(value));
            _easing = value;
        }
    }

    /// <summary>Gets or sets the delay before this cue starts, in seconds.</summary>
    public double DelaySeconds {
        get => _delaySeconds;
        set => _delaySeconds = VisualMotionGuards.NonNegativeFinite(value, nameof(value));
    }

    /// <summary>Gets or sets the cue duration, in seconds.</summary>
    public double DurationSeconds {
        get => _durationSeconds;
        set => _durationSeconds = VisualMotionGuards.PositiveFinite(value, nameof(value));
    }

    /// <summary>Gets or sets the travel distance used by positional effects, in pixels.</summary>
    public double DistancePixels {
        get => _distancePixels;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 80) {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Visual motion distance must be between zero and 80 pixels.");
            }

            _distancePixels = value;
        }
    }

    /// <summary>Sets the cue timing.</summary>
    public VisualMotionCue WithTiming(double delaySeconds, double durationSeconds) {
        DelaySeconds = delaySeconds;
        DurationSeconds = durationSeconds;
        return this;
    }

    /// <summary>Sets the cue easing curve.</summary>
    public VisualMotionCue WithEasing(VisualMotionEasing easing) {
        Easing = easing;
        return this;
    }

    /// <summary>Sets the travel distance used by positional effects.</summary>
    public VisualMotionCue WithDistance(double pixels) {
        DistancePixels = pixels;
        return this;
    }
}

/// <summary>
/// Defines an ordered, script-free visual story for SVG and HTML surfaces.
/// Motion is decorative: reduced-motion and print output always expose the completed state.
/// </summary>
public sealed class VisualMotionTimeline {
    private readonly List<VisualMotionCue> _cues = new();

    /// <summary>Gets the configured motion cues.</summary>
    public IReadOnlyList<VisualMotionCue> Cues => _cues;

    /// <summary>Creates an empty visual motion timeline.</summary>
    public static VisualMotionTimeline Create() => new();

    /// <summary>Adds a configured motion cue.</summary>
    public VisualMotionTimeline Add(VisualMotionCue cue) {
        if (cue == null) throw new ArgumentNullException(nameof(cue));
        if (_cues.Count >= 64) throw new InvalidOperationException("Visual motion timelines support at most 64 cues.");
        foreach (var existing in _cues) {
            if (string.Equals(existing.TargetId, cue.TargetId, StringComparison.Ordinal)) {
                throw new ArgumentException("A visual motion timeline can target each id only once.", nameof(cue));
            }
        }

        _cues.Add(cue);
        return this;
    }

    /// <summary>Adds a motion cue using explicit timing.</summary>
    public VisualMotionTimeline Add(string targetId, VisualMotionEffect effect, double delaySeconds = 0, double durationSeconds = 0.7, VisualMotionEasing easing = VisualMotionEasing.Emphasized, double distancePixels = 12) =>
        Add(new VisualMotionCue(targetId, effect)
            .WithTiming(delaySeconds, durationSeconds)
            .WithEasing(easing)
            .WithDistance(distancePixels));

    /// <summary>Adds a fade cue.</summary>
    public VisualMotionTimeline Fade(string targetId, double delaySeconds = 0, double durationSeconds = 0.7) =>
        Add(targetId, VisualMotionEffect.Fade, delaySeconds, durationSeconds);

    /// <summary>Adds an upward entrance cue.</summary>
    public VisualMotionTimeline Rise(string targetId, double delaySeconds = 0, double durationSeconds = 0.7, double distancePixels = 12) =>
        Add(targetId, VisualMotionEffect.Rise, delaySeconds, durationSeconds, distancePixels: distancePixels);

    /// <summary>Adds a left-to-right reveal cue.</summary>
    public VisualMotionTimeline Reveal(string targetId, double delaySeconds = 0, double durationSeconds = 0.8) =>
        Add(targetId, VisualMotionEffect.Reveal, delaySeconds, durationSeconds);

    /// <summary>Adds a gentle scale entrance cue.</summary>
    public VisualMotionTimeline Scale(string targetId, double delaySeconds = 0, double durationSeconds = 0.7) =>
        Add(targetId, VisualMotionEffect.Scale, delaySeconds, durationSeconds);

    /// <summary>Adds one restrained emphasis pulse.</summary>
    public VisualMotionTimeline Pulse(string targetId, double delaySeconds = 0, double durationSeconds = 0.8) =>
        Add(targetId, VisualMotionEffect.Pulse, delaySeconds, durationSeconds, VisualMotionEasing.EaseInOut);

    /// <summary>Adds evenly staggered cues for a sequence of named targets.</summary>
    public VisualMotionTimeline Cascade(IEnumerable<string> targetIds, VisualMotionEffect effect = VisualMotionEffect.Rise, double initialDelaySeconds = 0, double intervalSeconds = 0.12, double durationSeconds = 0.7) {
        if (targetIds == null) throw new ArgumentNullException(nameof(targetIds));
        var delay = VisualMotionGuards.NonNegativeFinite(initialDelaySeconds, nameof(initialDelaySeconds));
        var interval = VisualMotionGuards.NonNegativeFinite(intervalSeconds, nameof(intervalSeconds));
        foreach (var targetId in targetIds) {
            Add(targetId, effect, delay, durationSeconds);
            delay += interval;
        }

        return this;
    }

    internal void Validate() {
        if (_cues.Count == 0) throw new InvalidOperationException("Visual motion timelines require at least one cue.");
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cue in _cues) {
            if (cue == null) throw new InvalidOperationException("Visual motion timelines cannot contain null cues.");
            VisualMotionGuards.RequiredTargetId(cue.TargetId, nameof(cue.TargetId));
            if (!targets.Add(cue.TargetId)) throw new InvalidOperationException("Visual motion timelines can target each id only once.");
            VisualMotionGuards.EnumDefined(cue.Effect, nameof(cue.Effect));
            VisualMotionGuards.EnumDefined(cue.Easing, nameof(cue.Easing));
            VisualMotionGuards.NonNegativeFinite(cue.DelaySeconds, nameof(cue.DelaySeconds));
            VisualMotionGuards.PositiveFinite(cue.DurationSeconds, nameof(cue.DurationSeconds));
            if (cue.DelaySeconds + cue.DurationSeconds > 60) {
                throw new InvalidOperationException("Visual motion cues must complete within 60 seconds.");
            }
        }
    }
}

internal static class VisualMotionCss {
    public static string Build(string rootSelector, VisualMotionTimeline timeline, string keyframeScope) {
        if (rootSelector == null) throw new ArgumentNullException(nameof(rootSelector));
        if (timeline == null) throw new ArgumentNullException(nameof(timeline));
        timeline.Validate();
        var css = new StringBuilder(1024);
        var completedStateSelectors = new StringBuilder();
        for (var i = 0; i < timeline.Cues.Count; i++) {
            var cue = timeline.Cues[i];
            var selector = rootSelector + " [data-cfx-motion-target=\"" + cue.TargetId + "\"]";
            var keyframe = keyframeScope + "-motion-" + i.ToString(CultureInfo.InvariantCulture);
            WriteKeyframes(css, keyframe, cue);
            css.Append(selector)
                .Append("{transform-box:fill-box;transform-origin:center;animation:")
                .Append(keyframe).Append(' ')
                .Append(Seconds(cue.DurationSeconds)).Append(' ')
                .Append(Easing(cue.Easing)).Append(' ')
                .Append(Seconds(cue.DelaySeconds))
                .Append(" both}");
            if (completedStateSelectors.Length > 0) completedStateSelectors.Append(',');
            completedStateSelectors.Append(selector);
        }

        css.Append("@media (prefers-reduced-motion:reduce){")
            .Append(completedStateSelectors)
            .Append("{animation:none!important;opacity:1!important;transform:none!important;clip-path:none!important}}")
            .Append("@media print{")
            .Append(completedStateSelectors)
            .Append("{animation:none!important;opacity:1!important;transform:none!important;clip-path:none!important}}");
        return css.ToString();
    }

    public static double Duration(VisualMotionTimeline timeline) {
        timeline.Validate();
        var duration = 0d;
        foreach (var cue in timeline.Cues) duration = Math.Max(duration, cue.DelaySeconds + cue.DurationSeconds);
        return duration;
    }

    private static void WriteKeyframes(StringBuilder css, string name, VisualMotionCue cue) {
        css.Append("@keyframes ").Append(name).Append('{');
        switch (cue.Effect) {
            case VisualMotionEffect.Fade:
                css.Append("0%{opacity:0}100%{opacity:1}");
                break;
            case VisualMotionEffect.Rise:
                css.Append("0%{opacity:0;transform:translateY(").Append(Pixels(cue.DistancePixels)).Append(")}100%{opacity:1;transform:translateY(0)}");
                break;
            case VisualMotionEffect.Reveal:
                css.Append("0%{opacity:0;clip-path:inset(0 100% 0 0)}100%{opacity:1;clip-path:inset(0 0 0 0)}");
                break;
            case VisualMotionEffect.Scale:
                css.Append("0%{opacity:0;transform:scale(.965)}100%{opacity:1;transform:scale(1)}");
                break;
            case VisualMotionEffect.Pulse:
                css.Append("0%,100%{transform:scale(1)}50%{transform:scale(1.018)}");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cue.Effect), cue.Effect, "Unknown visual motion effect.");
        }

        css.Append('}');
    }

    private static string Easing(VisualMotionEasing easing) {
        switch (easing) {
            case VisualMotionEasing.Linear:
                return "linear";
            case VisualMotionEasing.EaseOut:
                return "cubic-bezier(.16,1,.3,1)";
            case VisualMotionEasing.EaseInOut:
                return "cubic-bezier(.65,0,.35,1)";
            case VisualMotionEasing.Emphasized:
                return "cubic-bezier(.22,1,.36,1)";
            default:
                throw new ArgumentOutOfRangeException(nameof(easing), easing, "Unknown visual motion easing.");
        }
    }

    private static string Seconds(double value) => value.ToString("0.###", CultureInfo.InvariantCulture) + "s";

    private static string Pixels(double value) => value.ToString("0.###", CultureInfo.InvariantCulture) + "px";
}

internal static class VisualMotionGuards {
    public static string RequiredTargetId(string value, string parameterName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("Visual motion target ids cannot be empty.", parameterName);
        foreach (var ch in trimmed) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') continue;
            throw new ArgumentException("Visual motion target ids may contain only letters, digits, dots, underscores, and hyphens.", parameterName);
        }

        return trimmed;
    }

    public static void EnumDefined<TEnum>(TEnum value, string parameterName) where TEnum : struct {
        if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown " + typeof(TEnum).Name + " value.");
    }

    public static double NonNegativeFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and non-negative.");
        return value;
    }

    public static double PositiveFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than zero.");
        return value;
    }
}
