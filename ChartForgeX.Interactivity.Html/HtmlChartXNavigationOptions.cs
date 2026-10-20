using System;
using System.Collections.Generic;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Optional source-domain controls for a connected native X viewport.</summary>
/// <remarks>The controls remain hidden until a native viewport connection is installed. They retain the
/// full-data Y domain. A navigator uses the ordinary native full-domain preparation, adding one preparation
/// when rendering a windowed section; it does not implement a separate chart projection.</remarks>
public sealed class HtmlChartXNavigationOptions {
    /// <summary>Gets or sets whether supported connected charts show native X navigation. The default is false.</summary>
    public bool Enabled { get; set; }
    /// <summary>Gets or sets whether the controls include a full-domain native overview. The default is true.</summary>
    public bool IncludeNavigator { get; set; } = true;
    /// <summary>Gets or sets whether a position scrollbar moves the current projected span. The default is true.</summary>
    /// <remarks>Compressed time domains retain included-time width while the elapsed wall-clock span may change.</remarks>
    public bool IncludeScrollbar { get; set; } = true;
    /// <summary>Gets the named trailing ranges, measured from the resolved full-domain maximum.</summary>
    public IList<HtmlChartXRangePreset> RangePresets { get; } = new List<HtmlChartXRangePreset>();
}

/// <summary>A named fixed source-coordinate span anchored at the full-domain maximum.</summary>
/// <remarks>Time axes use OLE Automation days. Fixed durations do not represent calendar months, years,
/// business days or trading sessions. A span covering the full domain selects its complete range.</remarks>
public sealed class HtmlChartXRangePreset {
    /// <summary>Creates a numeric source-coordinate range preset.</summary>
    /// <param name="label">The visible and accessible control label.</param>
    /// <param name="span">A finite positive source-coordinate span; time axes use days.</param>
    /// <exception cref="ArgumentException">The label is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The span is nonpositive or nonfinite.</exception>
    public HtmlChartXRangePreset(string label, double span) {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("A range preset requires a label.", nameof(label));
        if (double.IsNaN(span) || double.IsInfinity(span) || span <= 0)
            throw new ArgumentOutOfRangeException(nameof(span), span, "A range preset requires a finite positive span.");
        Label = label.Trim(); Span = span;
    }
    /// <summary>Gets the visible and accessible control label.</summary>
    public string Label { get; }
    /// <summary>Gets the fixed source-coordinate span.</summary>
    public double Span { get; }

    /// <summary>Creates a fixed-duration time-axis preset without requiring OLE Automation unit conversion.</summary>
    /// <param name="label">The visible and accessible control label.</param>
    /// <param name="duration">A positive elapsed duration, independent of display time zone and calendar.</param>
    /// <returns>A preset whose source-coordinate span is the duration in days.</returns>
    /// <exception cref="ArgumentException">The label is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The duration is nonpositive.</exception>
    public static HtmlChartXRangePreset FromDuration(string label, TimeSpan duration) => new HtmlChartXRangePreset(label, duration.TotalDays);
}
