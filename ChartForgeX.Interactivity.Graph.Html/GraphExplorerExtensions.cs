using System;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Graph.Html;

/// <summary>
/// Provides convenience methods for rendering graph scenes through the HTML graph explorer adapter.
/// </summary>
public static class GraphExplorerExtensions {
    /// <summary>
    /// Renders the graph scene to a complete self-contained HTML page.
    /// </summary>
    /// <param name="scene">The graph scene to render.</param>
    /// <param name="configure">Optional adapter configuration callback.</param>
    /// <returns>A complete HTML document.</returns>
    public static string ToGraphExplorerHtmlPage(this GraphScene scene, Action<HtmlGraphExplorerOptions>? configure = null) {
        return new HtmlGraphExplorerRenderer().RenderPage(scene, configure);
    }

    /// <summary>
    /// Renders the graph scene to an embeddable fragment with inline assets.
    /// </summary>
    /// <param name="scene">The graph scene to render.</param>
    /// <param name="configure">Optional adapter configuration callback.</param>
    /// <returns>An embeddable HTML fragment.</returns>
    public static string ToGraphExplorerHtmlFragment(this GraphScene scene, Action<HtmlGraphExplorerOptions>? configure = null) {
        return new HtmlGraphExplorerRenderer().RenderFragment(scene, configure);
    }
}
