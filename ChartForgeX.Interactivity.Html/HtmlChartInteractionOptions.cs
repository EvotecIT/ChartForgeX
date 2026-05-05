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
    /// Gets or sets whether the generated page includes a reset button for selections and legend toggles.
    /// </summary>
    public bool IncludeResetButton { get; set; }

    private static string? NormalizeOptionalText(string? value, string parameterName) {
        if (value == null) return null;
        var trimmed = value.Trim();
        if (trimmed.Length == 0) throw new ArgumentException("HTML interaction option values must not be empty.", parameterName);
        return trimmed;
    }
}
