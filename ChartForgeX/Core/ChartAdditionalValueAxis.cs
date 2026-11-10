using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>
/// An additional vertical value axis that stacks outward beside the primary (start) or secondary (end) axis.
/// Create one with <see cref="Chart.AddValueAxis"/> and bind series to it with <see cref="ChartSeries.UseValueAxis"/>.
/// </summary>
/// <remarks>
/// The axis owns its own domain, ticks, formatter and grid policy through <see cref="Axis"/>. It draws no grid lines
/// unless its grid style asks for them. Its captions, rule and title use <see cref="Color"/>, or the colour of the
/// first bound series, so the reader can match the axis to its series.
/// </remarks>
public sealed class ChartAdditionalValueAxis {
    private string _title;

    internal ChartAdditionalValueAxis(Chart owner, ChartAxisPlacement placement, string title) {
        if (!Enum.IsDefined(typeof(ChartAxisPlacement), placement)) throw new ArgumentOutOfRangeException(nameof(placement), placement, "Unknown axis placement.");
        Owner = owner; Placement = placement;
        _title = title ?? throw new ArgumentNullException(nameof(title));
    }

    internal Chart Owner { get; }

    /// <summary>Gets the side that holds the axis.</summary>
    public ChartAxisPlacement Placement { get; }

    /// <summary>Gets the axis scale, bounds, tick, formatter and grid policy.</summary>
    public ChartAxis Axis { get; } = new();

    /// <summary>Gets or sets the axis title shown above the plot. An empty title is omitted.</summary>
    public string Title {
        get => _title;
        set => _title = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the colour of the axis captions, rule and title. Null uses the first bound series colour.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Configures the axis policy.</summary>
    /// <param name="configure">The axis configuration.</param>
    /// <returns>The current axis.</returns>
    public ChartAdditionalValueAxis Configure(Action<ChartAxis> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Axis);
        return this;
    }

    /// <summary>Sets the colour of the axis captions, rule and title.</summary>
    /// <param name="color">The colour, or null to follow the first bound series.</param>
    /// <returns>The current axis.</returns>
    public ChartAdditionalValueAxis WithColor(ChartColor? color) { Color = color; return this; }

    /// <summary>Gets the zero-based position of this axis among the additional axes on its side, counted outward.</summary>
    internal int SideIndex => Owner.AdditionalValueAxes.Where(axis => axis.Placement == Placement).ToList().IndexOf(this);

    internal void Validate(Chart chart) {
        if (!chart.AdditionalValueAxes.Contains(this))
            throw new InvalidOperationException("A series uses an additional value axis that belongs to another chart.");
        if (Axis.Scale == ChartScaleKind.Time) throw new NotSupportedException("An additional value axis supports linear and logarithmic scales, not a time scale.");
        if (Axis.CrossAt.HasValue || Axis.RuleCrossAt.HasValue) throw new NotSupportedException("An additional value axis always sits outside the plot and cannot cross it.");
        Axis.ValidateTickPolicy("An additional value axis");
    }
}
