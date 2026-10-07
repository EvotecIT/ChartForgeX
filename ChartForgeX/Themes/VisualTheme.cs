using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>A paired immutable theme shared by charts, diagrams and future composition packages.</summary>
public sealed partial class VisualTheme {
    private static readonly Lazy<VisualTheme> Default = new(CreateGraphite);
    private readonly VisualThemeColors _light;
    private readonly VisualThemeColors _dark;
    /// <summary>Creates a paired theme from independent token snapshots and geometry settings.</summary>
    public VisualTheme(VisualDesignTokens light, VisualDesignTokens dark, VisualTypography? typography = null,
        double spacing = 12, double seriesStrokeWidth = 2, double markerRadius = 3,
        double areaOpacity = 0.18, double barRadius = 3, double gridStrokeWidth = 1, double axisStrokeWidth = 1) {
        _light = new VisualThemeColors(light ?? throw new ArgumentNullException(nameof(light)));
        _dark = new VisualThemeColors(dark ?? throw new ArgumentNullException(nameof(dark)));
        Typography = typography ?? new VisualTypography();
        Spacing = NonNegative(spacing, nameof(spacing)); SeriesStrokeWidth = NonNegative(seriesStrokeWidth, nameof(seriesStrokeWidth));
        MarkerRadius = NonNegative(markerRadius, nameof(markerRadius)); BarRadius = NonNegative(barRadius, nameof(barRadius));
        GridStrokeWidth = NonNegative(gridStrokeWidth, nameof(gridStrokeWidth)); AxisStrokeWidth = NonNegative(axisStrokeWidth, nameof(axisStrokeWidth));
        if (double.IsNaN(areaOpacity) || areaOpacity < 0 || areaOpacity > 1) throw new ArgumentOutOfRangeException(nameof(areaOpacity));
        AreaOpacity = areaOpacity;
    }
    /// <summary>Gets the typography scale.</summary>
    public VisualTypography Typography { get; }
    /// <summary>Gets the standard layout gap.</summary>
    public double Spacing { get; }
    /// <summary>Gets the default data-line width.</summary>
    public double SeriesStrokeWidth { get; }
    /// <summary>Gets the default point radius.</summary>
    public double MarkerRadius { get; }
    /// <summary>Gets the area fill opacity.</summary>
    public double AreaOpacity { get; }
    /// <summary>Gets the bar corner radius.</summary>
    public double BarRadius { get; }
    /// <summary>Gets the guide width.</summary>
    public double GridStrokeWidth { get; }
    /// <summary>Gets the axis width.</summary>
    public double AxisStrokeWidth { get; }
    /// <summary>Resolves the selected immutable color snapshot.</summary>
    public VisualThemeColors Resolve(VisualThemeMode mode) => mode switch {
        VisualThemeMode.Light => _light, VisualThemeMode.Dark => _dark,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
    /// <summary>Gets the Graphite layout preset with the canonical HtmlForgeX light/dark colors.</summary>
    public static VisualTheme Graphite() => Default.Value;
    /// <summary>Creates a paired theme from the generated HtmlForgeX chart token document.</summary>
    public static VisualTheme FromJson(string json, VisualTypography? typography = null) => new(
        VisualDesignTokens.FromJson(json, VisualThemeMode.Light), VisualDesignTokens.FromJson(json, VisualThemeMode.Dark), typography);
    private static VisualTheme CreateGraphite() {
        using var stream = typeof(VisualTheme).Assembly.GetManifestResourceStream("ChartForgeX.Themes.Tokens.evotec.chartforgex.tokens.json")
            ?? throw new InvalidOperationException("The canonical theme resource is missing.");
        using var reader = new StreamReader(stream);
        return FromJson(reader.ReadToEnd());
    }
    private static double NonNegative(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}

/// <summary>Immutable semantic color roles for one theme mode.</summary>
public sealed class VisualThemeColors {
    private readonly VisualDesignTokens _tokens;
    internal VisualThemeColors(VisualDesignTokens tokens) { _tokens = tokens.Clone(); Palette = Array.AsReadOnly(_tokens.Palette); }
    internal VisualDesignTokens ToTokens() => _tokens.Clone();
    /// <summary>Gets the canvas color.</summary>
    public ChartColor Background => _tokens.Background;
    /// <summary>Gets the plot surface color.</summary>
    public ChartColor Surface => _tokens.Surface;
    /// <summary>Gets the card surface color.</summary>
    public ChartColor ElevatedSurface => _tokens.ElevatedSurface;
    /// <summary>Gets the primary text color.</summary>
    public ChartColor Foreground => _tokens.Foreground;
    /// <summary>Gets the secondary text color.</summary>
    public ChartColor MutedForeground => _tokens.MutedForeground;
    /// <summary>Gets the guide and border color.</summary>
    public ChartColor Border => _tokens.Border;
    /// <summary>Gets the accent color.</summary>
    public ChartColor Accent => _tokens.Accent;
    /// <summary>Gets the ordered categorical palette.</summary>
    public IReadOnlyList<ChartColor> Palette { get; }
    /// <summary>Gets an independent copy of the severity, outcome and state color pairs.</summary>
    public VisualStatusTokens Status => _tokens.Status.Clone();
    /// <summary>Gets the sequential ramp, weakest to strongest.</summary>
    public IReadOnlyList<ChartColor> SequentialRamp => Array.AsReadOnly(_tokens.SequentialRamp ?? Array.Empty<ChartColor>());
    /// <summary>Gets the immutable diverging ramp.</summary>
    public VisualDivergingRamp? DivergingRamp => _tokens.DivergingRamp;
}

/// <summary>The shared logical typography scale; the default follows the chart look specification.</summary>
public sealed class VisualTypography {
    /// <summary>Creates a validated immutable font scale.</summary>
    public VisualTypography(string family = "Calibri, Carlito, Segoe UI, system-ui, sans-serif",
        double titleSize = 22, double subtitleSize = 13, double axisSize = 11, double legendSize = 12, double dataLabelSize = 11) {
        if (string.IsNullOrWhiteSpace(family)) throw new ArgumentException("A font family is required.", nameof(family));
        Family = family; TitleSize = Positive(titleSize, nameof(titleSize)); SubtitleSize = Positive(subtitleSize, nameof(subtitleSize));
        AxisSize = Positive(axisSize, nameof(axisSize)); LegendSize = Positive(legendSize, nameof(legendSize)); DataLabelSize = Positive(dataLabelSize, nameof(dataLabelSize));
    }
    /// <summary>Gets the fallback family stack.</summary>
    public string Family { get; }
    /// <summary>Gets the title size.</summary>
    public double TitleSize { get; }
    /// <summary>Gets the subtitle size.</summary>
    public double SubtitleSize { get; }
    /// <summary>Gets the axis size.</summary>
    public double AxisSize { get; }
    /// <summary>Gets the legend size.</summary>
    public double LegendSize { get; }
    /// <summary>Gets the data-label size.</summary>
    public double DataLabelSize { get; }
    private static double Positive(double value, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
        return value;
    }
}
