using System;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Holds settings used by the self-contained HTML dashboard interaction adapter.
/// </summary>
public sealed class HtmlInteractiveDashboardOptions {
    private string? _pageTitle;
    private string? _idScope;
    private string? _scriptNonce;
    private int _columns = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlInteractiveDashboardOptions"/> class.
    /// </summary>
    public HtmlInteractiveDashboardOptions() {
        Interaction = ChartInteractionOptions.ReportReview();
        IncludeResetButton = true;
        ResponsiveLayout = HtmlChartResponsiveLayout.Readable;
        TooltipMode = HtmlChartTooltipMode.SharedX;
    }

    /// <summary>
    /// Gets the host-neutral interaction options shared by all dashboard charts.
    /// </summary>
    public ChartInteractionOptions Interaction { get; }

    /// <summary>
    /// Gets or sets the optional HTML document title.
    /// </summary>
    public string? PageTitle {
        get => _pageTitle;
        set => _pageTitle = NormalizeOptionalText(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets a deterministic ID scope used for generated child chart IDs.
    /// </summary>
    public string? IdScope {
        get => _idScope;
        set => _idScope = NormalizeOptionalText(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets an optional nonce applied to the generated script tag for content security policies.
    /// </summary>
    public string? ScriptNonce {
        get => _scriptNonce;
        set => _scriptNonce = NormalizeOptionalText(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets shared asset references. When set, the page links the chart runtime and stylesheet from
    /// <see cref="HtmlAssetReferences.BasePath"/> instead of inlining them (see <see cref="HtmlInteractiveAssetFiles.Charts"/>).
    /// </summary>
    public HtmlAssetReferences? ExternalAssets { get; set; }

    /// <summary>
    /// Gets or sets the preferred dashboard column count.
    /// </summary>
    public int Columns {
        get => _columns;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), "Dashboard columns must be between 1 and 4.");
            _columns = value;
        }
    }

    /// <summary>
    /// Gets or sets whether each chart includes a contextual "Reset view" button. The button stays hidden until
    /// the reader selects marks, zooms, pans, brushes, mutes, or isolates series, and hides again after the view is reset.
    /// </summary>
    public bool IncludeResetButton { get; set; }

    /// <summary>Gets or sets how every dashboard chart protects readability in narrow containers.</summary>
    public HtmlChartResponsiveLayout ResponsiveLayout { get; set; }

    /// <summary>
    /// Gets or sets the tooltip mode shared by every dashboard chart. Shared-x tooltips fall back to one target
    /// when comparable numeric x/y observations are unavailable. The <see cref="ChartInteractionFeatures.Tooltips"/>
    /// feature controls whether tooltips are enabled.
    /// </summary>
    public HtmlChartTooltipMode TooltipMode { get; set; }

    private static string? NormalizeOptionalText(string? value, string parameterName) {
        if (value == null) return null;
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("HTML dashboard option values must not be empty.", parameterName);
        return trimmed;
    }
}
