using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// Builds product-neutral semantic color palettes. A VisualTheme snapshots these mutable values.
/// </summary>
public sealed partial class VisualDesignTokens {
    private ChartColor[] _palette = ChartPalettes.Categorical;
    private VisualStatusTokens _status = new();
    private ChartColor[]? _sequentialRamp;

    /// <summary>Gets or sets the page or canvas background color.</summary>
    public ChartColor Background { get; set; } = ChartColor.FromHex("#FFFFFF");
    /// <summary>Gets or sets the recessed surface color.</summary>
    public ChartColor Surface { get; set; } = ChartColor.FromHex("#F8FAFC");
    /// <summary>Gets or sets the elevated card surface color.</summary>
    public ChartColor ElevatedSurface { get; set; } = ChartColor.FromHex("#FFFFFF");
    /// <summary>Gets or sets the primary foreground color.</summary>
    public ChartColor Foreground { get; set; } = ChartColor.FromHex("#0F172A");
    /// <summary>Gets or sets the secondary foreground color.</summary>
    public ChartColor MutedForeground { get; set; } = ChartColor.FromHex("#475569");
    /// <summary>Gets or sets the guide and border color.</summary>
    public ChartColor Border { get; set; } = ChartColor.FromHex("#CBD5E1");
    /// <summary>Gets or sets the primary accent color.</summary>
    public ChartColor Accent { get; set; } = ChartColor.FromHex("#2563EB");
    /// <summary>Gets or sets the secondary accent color.</summary>
    public ChartColor SecondaryAccent { get; set; } = ChartColor.FromHex("#06B6D4");
    /// <summary>Gets or sets the positive semantic color.</summary>
    public ChartColor Positive { get; set; } = ChartColor.FromHex("#16A34A");
    /// <summary>Gets or sets the warning semantic color.</summary>
    public ChartColor Warning { get; set; } = ChartColor.FromHex("#F59E0B");
    /// <summary>Gets or sets the negative semantic color.</summary>
    public ChartColor Negative { get; set; } = ChartColor.FromHex("#EF4444");
    /// <summary>Gets or sets the disabled semantic color.</summary>
    public ChartColor Disabled { get; set; } = ChartColor.FromHex("#94A3B8");

    /// <summary>Gets or sets the qualitative data palette.</summary>
    public ChartColor[] Palette {
        get => (ChartColor[])_palette.Clone();
        set {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.Length == 0) throw new ArgumentException("Palette must contain at least one color.", nameof(value));
            _palette = (ChartColor[])value.Clone();
        }
    }

    /// <summary>
    /// Gets or sets the severity, outcome, and operational-state colours. Use
    /// <see cref="VisualStatusTokens.OperationalStateCategories"/> and related methods with
    /// <see cref="ChartForgeX.Core.Chart.WithStateCategories(ChartForgeX.Core.ChartStateCategory[])"/>.
    /// </summary>
    public VisualStatusTokens Status {
        get => _status;
        set => _status = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the optional sequential ramp (weakest to strongest, one hue) for counts and magnitudes. Used for numeric heatmap scales; null keeps the default blend.
    /// </summary>
    public ChartColor[]? SequentialRamp {
        get => _sequentialRamp == null ? null : (ChartColor[])_sequentialRamp.Clone();
        set {
            if (value != null && value.Length < 2) throw new ArgumentException("A sequential ramp needs at least two colours.", nameof(value));
            _sequentialRamp = value == null ? null : (ChartColor[])value.Clone();
        }
    }

    /// <summary>Gets or sets the optional diverging ramp for values around a midpoint (see <see cref="VisualDivergingRamp.ToColorScale"/>).</summary>
    public VisualDivergingRamp? DivergingRamp { get; set; }

    /// <summary>Returns a sequential numeric color scale with every ramp color, weakest to strongest, or null without a ramp.</summary>
    public ChartColorScale? ToSequentialColorScale() =>
        _sequentialRamp == null ? null : ChartColorScale.Sequential(_sequentialRamp);

    /// <summary>Replaces this editable palette with an independent copy of another palette.</summary>
    public void CopyFrom(VisualDesignTokens source) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var copy = source.Clone();
        Background = copy.Background; Surface = copy.Surface; ElevatedSurface = copy.ElevatedSurface;
        Foreground = copy.Foreground; MutedForeground = copy.MutedForeground; Border = copy.Border;
        Accent = copy.Accent; SecondaryAccent = copy.SecondaryAccent; Positive = copy.Positive;
        Warning = copy.Warning; Negative = copy.Negative; Disabled = copy.Disabled; Palette = copy.Palette;
        Status = copy.Status; SequentialRamp = copy.SequentialRamp; DivergingRamp = copy.DivergingRamp;
        PlotBorder = copy.PlotBorder; Muted = copy.Muted; Grid = copy.Grid; Axis = copy.Axis;
        Info = copy.Info; Quiet = copy.Quiet; QuietLine = copy.QuietLine;
        Neutral = copy.Neutral; Neutral2 = copy.Neutral2; Neutral3 = copy.Neutral3;
    }

    /// <summary>Creates an independent copy.</summary>
    public VisualDesignTokens Clone() => new() {
        Background = Background,
        Surface = Surface,
        ElevatedSurface = ElevatedSurface,
        Foreground = Foreground,
        MutedForeground = MutedForeground,
        Border = Border,
        Accent = Accent,
        SecondaryAccent = SecondaryAccent,
        Positive = Positive,
        Warning = Warning,
        Negative = Negative,
        Disabled = Disabled,
        Palette = Palette,
        Status = Status.Clone(),
        SequentialRamp = SequentialRamp,
        DivergingRamp = DivergingRamp,
        PlotBorder = PlotBorder, Muted = Muted, Grid = Grid, Axis = Axis,
        Info = Info, Quiet = Quiet, QuietLine = QuietLine, Neutral = Neutral, Neutral2 = Neutral2, Neutral3 = Neutral3
    };

    /// <summary>Creates a dark token set suitable for reports, dashboards, and wallpapers.</summary>
    public static VisualDesignTokens Dark() => new() {
        Background = ChartColor.FromHex("#0B1120"),
        Surface = ChartColor.FromHex("#111827"),
        ElevatedSurface = ChartColor.FromHex("#172033"),
        Foreground = ChartColor.FromHex("#F8FAFC"),
        MutedForeground = ChartColor.FromHex("#CBD5E1"),
        Border = ChartColor.FromHex("#334155"),
        Accent = ChartColor.FromHex("#60A5FA"),
        SecondaryAccent = ChartColor.FromHex("#22D3EE"),
        Positive = ChartColor.FromHex("#34D399"),
        Warning = ChartColor.FromHex("#FBBF24"),
        Negative = ChartColor.FromHex("#F87171"),
        Palette = ChartPalettes.CategoricalDark,
        // Status colours are the Graphite palette v1 dark set, validated together for colour-vision deficiency.
        Status = new VisualStatusTokens {
            Critical = VisualStatusTokens.Pair("#F47171", "#F47171"),
            High = VisualStatusTokens.Pair("#F7924F", "#F7924F"),
            Medium = VisualStatusTokens.Pair("#EEB640", "#EEB640"),
            Low = VisualStatusTokens.Pair("#3EC3DD", "#3EC3DD"),
            Info = VisualStatusTokens.Pair("#74A2F7", "#74A2F7"),
            Pass = VisualStatusTokens.Pair("#4CC78A", "#4CC78A"),
            Neutral = VisualStatusTokens.Pair("#8A8F98", "#A0A5AE"),
            Maintenance = VisualStatusTokens.Pair("#A497F2", "#A497F2")
        }
    };

}
