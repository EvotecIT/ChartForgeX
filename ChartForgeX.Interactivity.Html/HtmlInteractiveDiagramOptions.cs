using PowerForge.Fluent;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Holds settings for interactive flow and sequence diagram HTML.</summary>
[Fluent]
public sealed class HtmlInteractiveDiagramOptions {
    /// <summary>Gets the control messages and their language; authored diagram text is unchanged.</summary>
    public HtmlChartLocalization Localization { get; } = new HtmlChartLocalization();

    /// <summary>Gets or sets the content security policy nonce. The script element carries it, and the output has no
    /// <c>style</c> attributes: inline styles and style elements move into one <c>style</c> element with this nonce
    /// (see <see cref="ChartForgeX.Html.HtmlStrictStyles"/>), and the runtime restores the inline styles through the CSS object model.</summary>
    public string? Nonce { get; set; }
}
