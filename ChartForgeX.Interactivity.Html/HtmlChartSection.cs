using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

/// <summary>An asset-free interactive chart section and the detached scene used to render it.</summary>
/// <remarks>The section and prepared visual are captured together by one preparation. They do not retain
/// the mutable chart or configuration callback. Register the shared HTML assets once in the host document.</remarks>
public sealed class HtmlChartSection {
    internal HtmlChartSection(string html, PreparedVisual preparedVisual) {
        Html = html;
        PreparedVisual = preparedVisual;
    }

    /// <summary>Gets the interactive section markup, without stylesheet or script assets.</summary>
    public string Html { get; }

    /// <summary>Gets the detached native scene and X projection matching the section markup.</summary>
    public PreparedVisual PreparedVisual { get; }
}
