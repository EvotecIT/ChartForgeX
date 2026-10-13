using System;

namespace ChartForgeX.Core;

/// <summary>Controls range-bar direction and captions without changing category coordinates or authored endpoints.</summary>
public sealed class ChartRangeBarOptions {
    private ChartOrientation _orientation;
    private ChartRangeBarLabelContent _labelContent;

    /// <summary>Gets or sets whether data labels show one combined caption or separate authored endpoint captions. Default: Combined.</summary>
    public ChartRangeBarLabelContent LabelContent {
        get => _labelContent;
        set {
            if (!Enum.IsDefined(typeof(ChartRangeBarLabelContent), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _labelContent = value;
        }
    }

    /// <summary>Gets or sets the optional formatter for separate Start and End captions. Null uses the chart's numeric value format.</summary>
    /// <remarks>Used only with Endpoints content. Evaluated once for Start then End per authored observation during preparation, including hidden or omitted captions. Null, empty or whitespace output suppresses that endpoint. Complete captions remain in source metadata. Exceptions propagate; prepared exports never invoke the formatter again. Custom point labels remain observation headings.</remarks>
    public Func<ChartRangeBarEndpointLabelContext, string?>? EndpointLabelFormatter { get; set; }

    /// <summary>Gets or sets the value direction. Vertical is the default; horizontal uses X for values and Y for categories.</summary>
    public ChartOrientation Orientation {
        get => _orientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _orientation = value;
        }
    }

    /// <summary>Copies the configured state for isolated chart configuration.</summary>
    internal ChartRangeBarOptions Clone() => (ChartRangeBarOptions)MemberwiseClone();
}
