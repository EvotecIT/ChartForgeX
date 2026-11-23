using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using System.Collections.Generic;
using PowerForge.Fluent;

namespace ChartForgeX.Core;

/// <summary>
/// Defines renderer-independent options for a chart.
/// </summary>
[Fluent]
public sealed partial class ChartOptions {
    private ChartSize _size = new(1000, 560);
    private ChartPadding _padding = ChartPadding.All(24);
    private VisualTheme _theme = VisualTheme.Graphite();
    private ThemeMode _themeMode;
    private ResolvedVisualTheme? _resolvedTheme;
    private bool? _showPlotBackground;

    /// <summary>
    /// Gets or sets the rendered chart size in pixels.
    /// </summary>
    public ChartSize Size {
        get => _size;
        set {
            if (value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Chart size must have positive dimensions.");
            _size = value;
        }
    }

    /// <summary>
    /// Gets or sets the outer frame padding. Headings, legends and axes reserve their measured space inside it.
    /// The default is the same for every theme and chart family. Explicit values are preserved by export.
    /// </summary>
    public ChartPadding Padding {
        get => HostOwnsFrame ? new ChartPadding(0, 0, 0, 0) : _padding;
        set {
            ChartGuards.Finite(value.Left, nameof(value));
            ChartGuards.Finite(value.Top, nameof(value));
            ChartGuards.Finite(value.Right, nameof(value));
            ChartGuards.Finite(value.Bottom, nameof(value));
            if (value.Left < 0 || value.Top < 0 || value.Right < 0 || value.Bottom < 0) throw new ArgumentOutOfRangeException(nameof(value), "Chart padding values must be non-negative.");
            _padding = value;
            HasExplicitPadding = true;
        }
    }

    /// <summary>Gets whether <see cref="Padding"/> was set, by the caller or by a builder, rather than left at its default.</summary>
    internal bool HasExplicitPadding { get; private set; }

    /// <summary>Gets authored padding independently of temporary host-frame ownership.</summary>
    internal ChartPadding ConfiguredPadding => _padding;

    /// <summary>
    /// Gets or sets the visual theme used by renderers.
    /// </summary>
    public VisualTheme Theme {
        get => _theme;
        set { _theme = value ?? throw new ArgumentNullException(nameof(value)); _resolvedTheme = null; }
    }

    /// <summary>Gets or sets the explicit color-palette mode.</summary>
    public ThemeMode ThemeMode {
        get => _themeMode;
        set { if (!Enum.IsDefined(typeof(ThemeMode), value)) throw new ArgumentOutOfRangeException(nameof(value)); _themeMode = value; _resolvedTheme = null; }
    }
    internal ResolvedVisualTheme ResolvedTheme => _resolvedTheme ??= Theme.ResolvePresentation(ThemeMode);

    /// <summary>Gets or sets whether Cartesian series geometry is clipped to the plot rectangle.</summary>
    /// <remarks>Markers with centers inside the rectangle may extend beyond it by their radius. Disable this for intentional overflow.</remarks>
    public bool ClipMarksToPlot { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the title and subtitle are rendered.
    /// </summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the outer card surface is rendered.
    /// </summary>
    public bool ShowCard { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the plot background surface is rendered.
    /// </summary>
    public bool ShowPlotBackground { get => _showPlotBackground ?? false; set => _showPlotBackground = value; }

    /// <summary>
    /// Gets or sets a value indicating whether grid lines are rendered.
    /// </summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the chart background should be transparent.
    /// </summary>
    public bool TransparentBackground { get; set; } = true;

    /// <summary>
    /// Gets or sets a formatter used for y-axis ticks, data labels, stack totals, and donut totals.
    /// </summary>
    [PowerForge.Fluent.Fluent("FormatValuesBy")]
public Func<double, string>? ValueFormatter {
        get => ReferenceEquals(ValueFormat, ChartValueFormat.ExistingValue) ? null : ValueFormat.Callback;
        set => ValueFormat = value == null ? ChartValueFormat.ExistingValue : ChartValueFormat.Custom(value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the chart is rendered as a compact sparkline.
    /// </summary>
    [NoFluent]
    public bool IsSparkline { get; set; }

    // Original compact sample slots determine bar spacing independently of retained observations.
    // Set only by the standalone Sparkline owner; general chart bars keep their coordinate-based layout.
    internal int SparklineSampleCount { get; set; }

    internal static void ValidateNullableFinite(double? value, string parameterName) {
        if (value.HasValue) ChartGuards.Finite(value.Value, parameterName);
    }

    internal static void ValidateSvgPathData(string value, string parameterName) {
        if (value.Length == 0) throw new ArgumentException("Pictorial SVG path data must not be empty.", parameterName);
        if (value.Length > 4096) throw new ArgumentException("Pictorial SVG path data must be 4096 characters or fewer.", parameterName);
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (char.IsWhiteSpace(ch) || char.IsDigit(ch) || ch == ',' || ch == '.' || ch == '-' || ch == '+') continue;
            if ("MmZzLlHhVvCcSsQqTtAaEe".IndexOf(ch) >= 0) continue;
            throw new ArgumentException("Pictorial SVG path data contains unsupported characters.", parameterName);
        }
    }
}
