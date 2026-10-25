using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>Defines the mathematical scale used by a chart axis.</summary>
public enum ChartScaleKind {
    /// <summary>Use an unmodified continuous numeric scale.</summary>
    Linear,

    /// <summary>Use the configured logarithm base and explicitly selected positive or negative domain.</summary>
    Logarithmic,

    /// <summary>Use a symmetric logarithmic scale that remains linear around zero.</summary>
    SymmetricLogarithmic,

    /// <summary>
    /// Use OLE Automation date values as UTC instants. Ticks snap to whole seconds, minutes, hours, days, weeks,
    /// months, or years chosen from the visible range, and labels are shown in UTC unless <see cref="ChartAxis.TimeZone"/> is set.
    /// </summary>
    Time
}

/// <summary>
/// Defines renderer-neutral bounds, scale, ticks, labels, and visibility for one chart axis.
/// </summary>
public sealed class ChartAxis {
    private double? _minimum;
    private double? _maximum;
    private ChartScaleKind _scale;
    private bool _hasExplicitScale;
    /// <summary>Gets whether the scale was authored rather than supplied by native defaults.</summary>
    internal bool HasExplicitScale => _hasExplicitScale;
    private double _symmetricLogarithmThreshold = 1;
    private double _logarithmBase = 10;
    private ChartLogarithmicDomain _logarithmicDomain;
    private int _tickCount = 6;
    private bool _hasExplicitTickCount;
    private ChartLabelDensity _labelDensity = ChartLabelDensity.Auto;
    private double _labelAngle;
    private bool _hasExplicitLabelAngle;
    private ChartAxisLabelOrientation _labelOrientation;

    /// <summary>Gets or sets an explicit minimum. Null enables automatic bounds.</summary>
    public double? Minimum {
        get => _minimum;
        set {
            ValidateFinite(value, nameof(value));
            ValidateBounds(value, _maximum, nameof(value));
            ValidateLogarithmicBound(value, nameof(value));
            ValidateTimeSpacingBound(value, nameof(value));
            _minimum = value;
        }
    }

    /// <summary>Gets or sets an explicit maximum. Null enables automatic bounds.</summary>
    public double? Maximum {
        get => _maximum;
        set {
            ValidateFinite(value, nameof(value));
            ValidateBounds(_minimum, value, nameof(value));
            ValidateLogarithmicBound(value, nameof(value));
            ValidateTimeSpacingBound(value, nameof(value));
            _maximum = value;
        }
    }

    /// <summary>Gets or sets the mathematical axis scale.</summary>
    public ChartScaleKind Scale {
        get => _scale;
        set {
            if (!Enum.IsDefined(typeof(ChartScaleKind), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown chart scale.");
            if (_timeSpacing != null && value != ChartScaleKind.Time) throw new InvalidOperationException("Remove discrete time spacing before selecting another scale.");
            if (value == ChartScaleKind.Logarithmic && ((_minimum.HasValue && !IsInLogarithmicDomain(_minimum.Value)) || (_maximum.HasValue && !IsInLogarithmicDomain(_maximum.Value)))) {
                throw new InvalidOperationException("Logarithmic explicit bounds must belong to the selected " + LogarithmicDomainName + " domain.");
            }

            _scale = value;
            _hasExplicitScale = true;
        }
    }

    /// <summary>Gets or sets the finite base greater than one used by logarithmic and symmetric logarithmic scales. Default: ten.</summary>
    /// <remarks>Axis reversal controls direction independently of the base. Other scale kinds retain this configuration without applying it.</remarks>
    public double LogarithmBase {
        get => _logarithmBase;
        set { ValidateLogarithmBase(value); _logarithmBase = value; }
    }

    /// <summary>Gets or sets the explicitly accepted sign domain for a logarithmic scale. Default: positive.</summary>
    /// <remarks>Zero and values of the other sign reject; no observations are silently omitted. Symmetric logarithmic axes accept both signs and ignore this policy.</remarks>
    public ChartLogarithmicDomain LogarithmicDomain {
        get => _logarithmicDomain;
        set {
            ValidateLogarithmicDomain(value);
            if (Scale == ChartScaleKind.Logarithmic && ((_minimum.HasValue && !InLogarithmicDomain(_minimum.Value, value))
                || (_maximum.HasValue && !InLogarithmicDomain(_maximum.Value, value))))
                throw new InvalidOperationException("Existing logarithmic bounds must belong to the selected sign domain.");
            _logarithmicDomain = value;
        }
    }

    /// <summary>Lets a typed date builder select calendar ticks while preserving an authored axis scale and configuration.</summary>
    internal void UseTimeScaleByDefault() {
        if (!_hasExplicitScale) _scale = ChartScaleKind.Time;
    }

    /// <summary>Gets or sets the positive linear threshold used by symmetric logarithmic scaling.</summary>
    public double SymmetricLogarithmThreshold {
        get => _symmetricLogarithmThreshold;
        set {
            if (!IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Symmetric logarithm threshold must be finite and greater than zero.");
            _symmetricLogarithmThreshold = value;
        }
    }

    /// <summary>Gets or sets the preferred number of generated ticks. An explicit value limits automatic fitting; the default lets measured horizontal numeric and time labels use the available room.</summary>
    public int TickCount {
        get => _tickCount;
        set {
            if (value < 2 || value > 100) throw new ArgumentOutOfRangeException(nameof(value), value, "Tick count must be from two through one hundred.");
            _tickCount = value;
            _hasExplicitTickCount = true;
        }
    }

    internal bool HasExplicitTickCount => _hasExplicitTickCount;

    /// <summary>Gets or sets whether generated linear ticks use whole-number positions and steps of at least one. Explicit labels and other scales retain their own tick policy.</summary>
    public bool IntegerTicks { get; set; }

    /// <summary>Uses whole-number positions for generated linear ticks without rounding data values or explicit bounds.</summary>
    /// <param name="enabled">Whether to restrict generated linear ticks to integers.</param>
    /// <returns>The current axis.</returns>
    public ChartAxis WithIntegerTicks(bool enabled = true) { IntegerTicks = enabled; return this; }

    /// <summary>Restores automatic tick density, including measured horizontal numeric and time-axis spacing.</summary>
    /// <returns>The current axis.</returns>
    public ChartAxis WithAutomaticTickCount() { _tickCount = 6; _hasExplicitTickCount = false; return this; }

    /// <summary>Gets or sets label density.</summary>
    public ChartLabelDensity LabelDensity {
        get => _labelDensity;
        set {
            if (!Enum.IsDefined(typeof(ChartLabelDensity), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown axis label density.");
            _labelDensity = value;
        }
    }

    /// <summary>Gets or sets label rotation in degrees, or an offset from the selected relative orientation.</summary>
    /// <remarks>Fixed orientation uses screen degrees without an upright adjustment. Setting zero explicitly keeps matrix captions horizontal; an unauthored matrix angle may rotate crowded captions automatically. Relative numeric radial labels add this offset to their local axis direction and keep the resulting baseline upright.</remarks>
    public double LabelAngle {
        get => _labelAngle;
        set {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Axis label angle must be finite.");
            _labelAngle = value;
            _hasExplicitLabelAngle = true;
        }
    }

    internal bool HasExplicitLabelAngle => _hasExplicitLabelAngle;

    /// <summary>Restores automatic label rotation for crowded matrices and the default zero angle for other axes.</summary>
    /// <returns>The current axis.</returns>
    public ChartAxis WithAutomaticLabelAngle() { _labelAngle = 0; _hasExplicitLabelAngle = false; return this; }

    /// <summary>Gets or sets how labels follow the axis. The default is a fixed screen orientation.</summary>
    /// <remarks>Relative orientations are supported by numeric radial bars and columns. Parallel follows the tangent of an angular axis or the spoke of a radius axis; perpendicular uses the other direction. The label angle is added before keeping the text upright. Other chart families reject a nonfixed orientation during preparation.</remarks>
    public ChartAxisLabelOrientation LabelOrientation {
        get => _labelOrientation;
        set {
            if (!Enum.IsDefined(typeof(ChartAxisLabelOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown axis label orientation.");
            _labelOrientation = value;
        }
    }

    /// <summary>Gets or sets a value indicating whether the axis and its labels are visible.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Gets or sets whether increasing values advance in the reverse visual direction. Bounds and source labels keep their original values.</summary>
    /// <remarks>Supported by Cartesian projections, numeric radial axes, radar/polar numeric radii and angular axes, and polar-area category angles. Angular reversal changes category order or the direction of authored polar radians without changing source values. Schedule and polar-area radius reversal reject during preparation.</remarks>
    public bool Reversed { get; set; }

    /// <summary>Gets or sets a value indicating whether the axis rule is visible.</summary>
    public bool ShowLine { get => _showLine; set { _showLine = value; HasExplicitLine = true; } }
    private bool _showLine = true;
    internal bool HasExplicitLine { get; private set; }

    private double? _ruleCrossAt;
    /// <summary>Gets or sets the coordinate on the opposite primary physical axis at which this axis rule is drawn. Null retains its frame edge.</summary>
    /// <remarks>Moves only the rule: ticks, captions and titles retain their measured edge gutters. X and secondary X rules use primary Y coordinates;
    /// Y and secondary Y rules use primary X coordinates, including horizontal charts. Uses the opposite axis's scale and reversal, never expands domains,
    /// and omits the rule when outside the resolved viewport. Cartesian non-sparkline charts only. Compressed-time closures and invalid logarithmic signs reject.</remarks>
    public double? RuleCrossAt {
        get => _ruleCrossAt;
        set {
            ValidateFinite(value, nameof(value));
            if (value.HasValue && _crossAt.HasValue) throw new InvalidOperationException("Clear CrossAt before configuring a rule-only crossing.");
            _ruleCrossAt = value;
        }
    }

    /// <summary>Moves only the axis rule to an opposite primary physical-axis coordinate, or restores its frame edge with null.</summary>
    /// <param name="value">A finite source coordinate, or null.</param>
    /// <returns>The current axis.</returns>
    public ChartAxis WithRuleCrossAt(double? value) { RuleCrossAt = value; return this; }

    private double? _crossAt;
    /// <summary>Gets or sets the opposite primary physical coordinate for the complete Cartesian axis, or null for the frame edge.</summary>
    /// <remarks>Moves the rule, tick captions, category parent bands and title together. Primary X captions follow below the rule, secondary X above;
    /// primary Y captions follow left and secondary Y right. Horizontal Y titles follow their track's top endpoint, retaining the existing horizontal treatment.
    /// Existing measured plot reservations and mark projection are preserved. Actual marks and previously painted axis text constrain visible captions;
    /// omitted text retains complete semantic facts. Outside the resolved/windowed opposite domain omits the complete axis, without clamping.
    /// Uses the same opposite PRIMARY physical coordinates, transforms, reversal and restrictions as RuleCrossAt; the two policies are mutually exclusive.</remarks>
    public double? CrossAt {
        get => _crossAt;
        set {
            ValidateFinite(value, nameof(value));
            if (value.HasValue && _ruleCrossAt.HasValue) throw new InvalidOperationException("Clear RuleCrossAt before configuring a complete axis crossing.");
            _crossAt = value;
        }
    }

    /// <summary>Moves the complete Cartesian axis to an opposite primary physical coordinate, or restores its frame edge with null.</summary>
    /// <param name="value">A finite source coordinate, or null. Clear any rule-only crossing first.</param>
    /// <returns>The current axis.</returns>
    public ChartAxis WithCrossAt(double? value) { CrossAt = value; return this; }

    internal double? CrossingCoordinate => CrossAt ?? RuleCrossAt;

    /// <summary>Gets or sets a formatter for generated labels.</summary>
    public Func<double, string>? LabelFormatter {
        get => ValueFormat?.Callback;
        set => ValueFormat = value == null ? null : ChartValueFormat.Custom(value);
    }

    /// <summary>Gets or sets the numeric display policy. Null uses the chart fallback or the scale-aware default for time axes.</summary>
    public ChartValueFormat? ValueFormat {
        get => _valueFormat;
        set { _valueFormat = value; TimeTickLabelFormatter = null; }
    }
    private ChartValueFormat? _valueFormat;

    // Source adapters can supply interval-aware defaults. An explicit format assignment,
    // including null, replaces that source policy through every public axis entry point.
    internal Func<double, ChartTimeTickUnit?, string>? TimeTickLabelFormatter { get; set; }

    /// <summary>
    /// Gets or sets the display time zone for <see cref="ChartScaleKind.Time"/> axes. Axis values are UTC instants;
    /// null keeps ticks and labels in UTC. A zone moves tick alignment and labels to that zone's wall-clock time.
    /// </summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>Gets or sets explicitly included intervals for a compressed time X axis, or null for continuous elapsed time.</summary>
    /// <remarks>Supported vertical Cartesian series share this immutable domain. Closed gaps have no width;
    /// observations and source annotation anchors must belong to included sessions. Raw viewport bounds may lie
    /// in closures when the window still contains included time. Y/XY compression and market-calendar inference are not provided.</remarks>
    public ChartTimeSessionDomain? TimeSessions {
        get => _timeSessions;
        set {
            if (value != null && _timeSpacing != null) throw new InvalidOperationException("Time sessions and discrete time spacing cannot be combined.");
            _timeSessions = value;
        }
    }
    private ChartTimeSessionDomain? _timeSessions;

    /// <summary>Gets or sets ordinal or calendar-band spacing, or null for continuous elapsed time.</summary>
    /// <remarks>Requires a time scale and cannot be combined with TimeSessions. Captured domains contain at most 100,000 entries.
    /// Unit spacing preserves missing bands and rejects multiple observations from one series in the same band.</remarks>
    public ChartTimeSpacing? TimeSpacing {
        get => _timeSpacing;
        set {
            if (value != null && _timeSessions != null) throw new InvalidOperationException("Discrete time spacing and time sessions cannot be combined.");
            if (value != null && _hasExplicitScale && Scale != ChartScaleKind.Time) throw new InvalidOperationException("Discrete time spacing requires a time scale.");
            if (value != null) { ValidateOrderedTimeBound(_minimum, nameof(Minimum)); ValidateOrderedTimeBound(_maximum, nameof(Maximum)); }
            _timeSpacing = value;
        }
    }
    private ChartTimeSpacing? _timeSpacing;
    internal ChartTimeSpacingDomain? PreparedTimeDomain { get; set; }
    // Derived only from the immutable public request in the private presentation view.
    // Every layout/paint/tick owner therefore resolves the same explicit source edge policy.
    internal ChartAxisWindow? PreparedViewportWindow { get; set; }

    /// <summary>
    /// Gets or sets the time-zone designator shown when <see cref="ShowTimeZone"/> is enabled.
    /// Null uses <c>UTC</c> when <see cref="TimeZone"/> is null, otherwise the zone identifier. Zone identifiers differ
    /// between Windows and IANA systems, so set an explicit label when output must match across platforms.
    /// </summary>
    public string? TimeZoneLabel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether time axes append the time-zone designator to the axis title,
    /// or show it as the title when no title is set. Horizontal line charts also decorate their physical Y category title.
    /// </summary>
    public bool ShowTimeZone { get; set; }

    /// <summary>Gets explicit value-to-label mappings.</summary>
    /// <remarks>For grouped rectangular Heatmap labels, X values are actual column coordinates and Y values
    /// are one-based source row slots in series order. Series identities remain independent of axis labels.</remarks>
    public List<ChartAxisLabel> Labels { get; private set; } = new();

    /// <summary>Copies the axis's authored policy and detaches its explicit label collection.</summary>
    internal ChartAxis Clone() {
        var copy = (ChartAxis)MemberwiseClone();
        copy.Labels = new List<ChartAxisLabel>(Labels);
        return copy;
    }

    /// <summary>Sets explicit bounds.</summary>
    public ChartAxis WithBounds(double minimum, double maximum) {
        if (!IsFinite(minimum)) throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Axis minimum must be finite.");
        if (!IsFinite(maximum)) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Axis maximum must be finite.");
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Axis maximum must be greater than minimum.");
        ValidateTimeSpacingBound(minimum, nameof(minimum)); ValidateTimeSpacingBound(maximum, nameof(maximum));
        if (Scale == ChartScaleKind.Logarithmic && (!IsInLogarithmicDomain(minimum) || !IsInLogarithmicDomain(maximum)))
            throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Logarithmic bounds must belong to the selected " + LogarithmicDomainName + " domain.");
        _minimum = minimum;
        _maximum = maximum;
        return this;
    }

    /// <summary>Clears explicit bounds.</summary>
    public ChartAxis WithAutomaticBounds() { _minimum = null; _maximum = null; return this; }

    /// <summary>Sets the mathematical scale.</summary>
    public ChartAxis WithScale(ChartScaleKind scale) { Scale = scale; return this; }

    /// <summary>Selects a logarithmic scale, base and sign domain as one validated configuration.</summary>
    /// <param name="logarithmBase">A finite base greater than one.</param>
    /// <param name="domain">The positive or negative source domain.</param>
    /// <returns>The current axis. Existing bounds must belong to the selected domain; clear them before changing their sign.</returns>
    public ChartAxis WithLogarithmicScale(double logarithmBase = 10, ChartLogarithmicDomain domain = ChartLogarithmicDomain.Positive) {
        ValidateLogarithmBase(logarithmBase); ValidateLogarithmicDomain(domain);
        if (TimeSpacing != null) throw new InvalidOperationException("Remove discrete time spacing before selecting a logarithmic scale.");
        if ((_minimum.HasValue && !InLogarithmicDomain(_minimum.Value, domain)) || (_maximum.HasValue && !InLogarithmicDomain(_maximum.Value, domain)))
            throw new InvalidOperationException("Existing logarithmic bounds must belong to the selected sign domain.");
        _logarithmBase = logarithmBase; _logarithmicDomain = domain; _scale = ChartScaleKind.Logarithmic; _hasExplicitScale = true;
        return this;
    }

    /// <summary>Sets axis visibility.</summary>
    public ChartAxis WithVisibility(bool visible = true) { Visible = visible; return this; }

    /// <summary>Reverses the visual direction while retaining bounds and source values.</summary>
    public ChartAxis WithReversal(bool reversed = true) { Reversed = reversed; return this; }

    /// <summary>
    /// Uses a UTC time scale displayed in <paramref name="timeZone"/> (UTC when null) and optionally shows the zone designator.
    /// </summary>
    /// <param name="timeZone">The display time zone, or null for UTC.</param>
    /// <param name="showTimeZone">Whether the axis title shows the zone designator.</param>
    /// <param name="label">An optional designator such as <c>CET</c> or <c>Europe/Warsaw</c>.</param>
    /// <returns>The current axis.</returns>
    public ChartAxis WithTimeScale(TimeZoneInfo? timeZone = null, bool showTimeZone = false, string? label = null) {
        Scale = ChartScaleKind.Time;
        TimeZone = timeZone;
        ShowTimeZone = showTimeZone;
        TimeZoneLabel = label;
        return this;
    }

    /// <summary>Selects a time scale with an explicit shared session domain; null restores continuous elapsed time.</summary>
    /// <param name="domain">The immutable included intervals, or null to remove compression.</param>
    /// <returns>The current axis. Existing display zone and formatting policies are retained.</returns>
    public ChartAxis WithTimeSessions(ChartTimeSessionDomain? domain) {
        if (domain != null && TimeSpacing != null) throw new InvalidOperationException("Time sessions and discrete time spacing cannot be combined.");
        Scale = ChartScaleKind.Time; TimeSessions = domain; return this;
    }

    /// <summary>Selects a time scale and an explicit spacing policy; null restores elapsed-time spacing.</summary>
    /// <remarks>Existing formatting and display-zone settings are retained. Remove sessions before selecting discrete spacing.</remarks>
    public ChartAxis WithTimeSpacing(ChartTimeSpacing? spacing) {
        if (spacing != null && TimeSessions != null) throw new InvalidOperationException("Discrete time spacing and time sessions cannot be combined.");
        if (spacing != null) { ValidateOrderedTimeBound(_minimum, nameof(Minimum)); ValidateOrderedTimeBound(_maximum, nameof(Maximum)); }
        Scale = ChartScaleKind.Time; TimeSpacing = spacing; return this;
    }

    /// <summary>Sets the label formatter.</summary>
    public ChartAxis WithLabelFormatter(Func<double, string>? formatter) { LabelFormatter = formatter; return this; }

    /// <summary>Sets the numeric display policy; null restores scale-aware automatic formatting.</summary>
    public ChartAxis WithValueFormat(ChartValueFormat? format) { ValueFormat = format; return this; }

    private void ValidateTimeSpacingBound(double? value, string parameterName) { if (_timeSpacing != null) ValidateOrderedTimeBound(value, parameterName); }
    private static void ValidateOrderedTimeBound(double? value, string parameterName) {
        if (value.HasValue && !ChartDateTime.TryFromOrderedOaDate(value.Value, out _))
            throw new ArgumentOutOfRangeException(parameterName, value, "Discrete time bounds require ordered UTC instants on or after 1899-12-30.");
    }

    private void ValidateLogarithmicBound(double? value, string parameterName) {
        if (Scale == ChartScaleKind.Logarithmic && value.HasValue && !IsInLogarithmicDomain(value.Value))
            throw new ArgumentOutOfRangeException(parameterName, value, "Logarithmic bounds must belong to the selected " + LogarithmicDomainName + " domain.");
    }

    internal bool IsInLogarithmicDomain(double value) => InLogarithmicDomain(value, LogarithmicDomain);
    internal string LogarithmicDomainName => LogarithmicDomain == ChartLogarithmicDomain.Negative ? "negative" : "positive";
    private static bool InLogarithmicDomain(double value, ChartLogarithmicDomain domain) => IsFinite(value)
        && (domain == ChartLogarithmicDomain.Negative ? value < 0 : value > 0);
    private static void ValidateLogarithmicDomain(ChartLogarithmicDomain domain) {
        if (!Enum.IsDefined(typeof(ChartLogarithmicDomain), domain)) throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown logarithmic domain.");
    }
    private static void ValidateLogarithmBase(double value) {
        if (!IsFinite(value) || value <= 1) throw new ArgumentOutOfRangeException(nameof(value), value, "The logarithm base must be finite and greater than one.");
    }

    private static void ValidateBounds(double? minimum, double? maximum, string parameterName) {
        if (minimum.HasValue && maximum.HasValue && maximum.Value <= minimum.Value) throw new ArgumentOutOfRangeException(parameterName, maximum, "Axis maximum must be greater than minimum.");
    }

    private static void ValidateFinite(double? value, string parameterName) {
        if (value.HasValue && !IsFinite(value.Value)) throw new ArgumentOutOfRangeException(parameterName, value, "Axis bounds must be finite.");
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
