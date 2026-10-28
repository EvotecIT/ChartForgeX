using System;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>
/// Holds settings used by the self-contained HTML interaction adapter.
/// </summary>
public sealed class HtmlChartInteractionOptions {
    private string? _pageTitle;
    private string? _idScope;
    private string? _scriptNonce;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlChartInteractionOptions"/> class.
    /// </summary>
    public HtmlChartInteractionOptions() {
        Interaction = ChartInteractionOptions.ReportReview();
        IncludeResetButton = true;
        ResponsiveLayout = HtmlChartResponsiveLayout.Readable;
    }

    /// <summary>
    /// Gets the host-neutral interaction options consumed by the HTML adapter.
    /// </summary>
    public ChartInteractionOptions Interaction { get; }

    /// <summary>
    /// Gets or sets the optional HTML document title. When null, the chart title is used.
    /// </summary>
    public string? PageTitle {
        get => _pageTitle;
        set => _pageTitle = NormalizeOptionalText(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets a deterministic SVG ID scope for the embedded chart.
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
    /// Gets or sets whether the generated chart includes a contextual "Reset view" button. The button stays hidden
    /// until the reader selects marks, zooms, pans, brushes, mutes, or isolates series, and hides again after the view is reset.
    /// Resetting also clears selections, focus trails, and pinned tooltips.
    /// </summary>
    public bool IncludeResetButton { get; set; }

    /// <summary>Gets or sets whether right-click and the Context Menu key expose the chart's existing reset and export actions.</summary>
    /// <remarks>Disabled by default. Actions retain their feature flags, translations and ordinary toolbar behavior.</remarks>
    public bool IncludeContextMenu { get; set; }

    /// <summary>
    /// Gets or sets shared asset references. When set, <see cref="HtmlInteractiveChartRenderer.RenderPage(ChartForgeX.Core.Chart, System.Action{HtmlChartInteractionOptions})"/>
    /// links the runtime and stylesheet from <see cref="HtmlAssetReferences.BasePath"/> instead of inlining them; write the
    /// files once per bundle with <see cref="HtmlInteractiveAssetFiles.Charts"/>. Null (the default) keeps pages self-contained.
    /// Fragments are unaffected.
    /// </summary>
    public HtmlAssetReferences? ExternalAssets { get; set; }

    /// <summary>Gets or sets how the adapter protects chart readability in narrow containers.</summary>
    public HtmlChartResponsiveLayout ResponsiveLayout { get; set; }

    /// <summary>Gets or sets opt-in full authored native Pie/Donut raw values or finite Bubble, RangeBand/RangeArea, ErrorBar and Candlestick/OHLC component capture, including actual native domain and membership facts.</summary>
    /// <remarks>False preserves ordinary metadata and finite positive value admission. True requires authored series/observation IDs and representable captured native policy; unsupported capture rejects before a section is returned. Raw source rows do not invent painted targets.</remarks>
    public bool CaptureNativeValueSource { get; set; }

    /// <summary>Gets or sets how HTML supplies prepared text. Automatic delivers eligible owned faces and keeps native contours for unsupported or unavailable browser faces.</summary>
    /// <remarks>NativeContours retains captured glyph placement. Browser uses the host font system. Native static SVG/PNG defaults are independent.</remarks>
    public HtmlChartTextPresentation TextPresentation { get; set; } = HtmlChartTextPresentation.Automatic;

    /// <summary>Gets the native narrow drawing and container breakpoint used by <see cref="HtmlChartResponsiveLayout.Adaptive"/>.</summary>
    public HtmlChartAdaptiveLayoutOptions Adaptive { get; } = new HtmlChartAdaptiveLayoutOptions();

    /// <summary>Gets or sets the toolbar presentation. The default is the framed <see cref="HtmlChartToolbarLayout.Band"/>.</summary>
    public HtmlChartToolbarLayout Toolbar { get; set; } = HtmlChartToolbarLayout.Band;

    /// <summary>
    /// Gets the tooltip content, acquisition, timing and position options.
    /// </summary>
    public HtmlChartTooltipOptions Tooltip { get; } = new HtmlChartTooltipOptions();

    /// <summary>Gets the crosshair presentation options.</summary>
    public HtmlChartCrosshairOptions Crosshair { get; } = new HtmlChartCrosshairOptions();

    /// <summary>Gets the transient mark and branch emphasis options.</summary>
    public HtmlChartHighlightOptions Highlight { get; } = new HtmlChartHighlightOptions();

    /// <summary>Gets opt-in progress editing through the existing native host-preparation connection.</summary>
    public HtmlChartProgressEditingOptions ProgressEditing { get; } = new HtmlChartProgressEditingOptions();

    /// <summary>Gets optional source-domain navigator, range preset and position scrollbar controls.</summary>
    public HtmlChartXNavigationOptions XNavigation { get; } = new HtmlChartXNavigationOptions();

    /// <summary>Gets HTML control translations, language and direction independently from native data formatting.</summary>
    public HtmlChartLocalization Localization { get; } = new HtmlChartLocalization();

    private static string? NormalizeOptionalText(string? value, string parameterName) {
        if (value == null) return null;
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("HTML interaction option values must not be empty.", parameterName);
        return trimmed;
    }
}

/// <summary>Controls how a fixed-design SVG behaves inside a responsive HTML container.</summary>
public enum HtmlChartResponsiveLayout {
    /// <summary>Always scales the full chart to the available width.</summary>
    Fit,
    /// <summary>Preserves a readable minimum chart width inside a contained horizontal viewport on narrow screens.</summary>
    Readable,
    /// <summary>Uses a separately prepared native drawing in narrow containers, retaining interaction state.</summary>
    Adaptive
}

/// <summary>Controls the chart toolbar's frame and button density.</summary>
public enum HtmlChartToolbarLayout {
    /// <summary>Uses right-aligned compact buttons without a surrounding band.</summary>
    Compact,
    /// <summary>Uses the framed toolbar band.</summary>
    Band
}
