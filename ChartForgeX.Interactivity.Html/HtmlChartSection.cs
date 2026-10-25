using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

/// <summary>An asset-free interactive chart section and the detached scene used to render it.</summary>
/// <remarks>The section and prepared visual are captured together by one preparation. They do not retain
/// the mutable chart or configuration callback. Register the shared HTML assets once in the host document.</remarks>
public sealed class HtmlChartSection {
    internal HtmlChartSection(string html, PreparedVisual preparedVisual, PreparedVisual? narrowPreparedVisual = null, VisualSvgOptions? svgOptions = null)
        : this(html, preparedVisual, narrowPreparedVisual, svgOptions, null, null) { }
    internal HtmlChartSection(string html, PreparedVisual preparedVisual, PreparedVisual? narrowPreparedVisual, VisualSvgOptions? svgOptions,
        VisualSvgEmission? wideEmission, VisualSvgEmission? narrowEmission) {
        Html = html; SvgOptions = svgOptions ?? new VisualSvgOptions();
        WideEmission = wideEmission; NarrowEmission = narrowEmission;
        PreparedVisual = preparedVisual;
        NarrowPreparedVisual = narrowPreparedVisual;
    }

    internal VisualSvgOptions SvgOptions { get; }
    internal VisualSvgEmission? WideEmission { get; }
    internal VisualSvgEmission? NarrowEmission { get; }

    /// <summary>Gets the interactive section markup, without stylesheet or script assets.</summary>
    public string Html { get; }

    /// <summary>Gets the detached native scene and X projection matching the section markup.</summary>
    public PreparedVisual PreparedVisual { get; }

    /// <summary>Gets the detached narrow scene embedded by adaptive layout, or null for a single drawing.</summary>
    public PreparedVisual? NarrowPreparedVisual { get; }
}
