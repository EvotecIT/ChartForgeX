using System;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Provides adapter-owned interactive HTML for flow and sequence diagrams.</summary>
/// <remarks>The page embeds the same script-free SVG as the static export and adds hover and keyboard exploration,
/// a readout of the focused step or message and, for sequences, message playback. Static output is unchanged.</remarks>
public static class InteractiveDiagramExtensions {
    /// <summary>Renders a complete self-contained interactive flow page.</summary>
    /// <param name="flow">The flow to render.</param>
    /// <param name="context">Optional size, theme and frame; omitted uses the flow's own export request.</param>
    /// <param name="configure">Optional localization and content security policy settings.</param>
    public static string ToInteractiveHtmlPage(this FlowArtifact flow, VisualRenderContext? context = null,
        Action<HtmlInteractiveDiagramOptions>? configure = null) => Renderer(configure).Render(flow, context, page: true);

    /// <summary>Renders a self-contained interactive flow fragment with its style and runtime inlined.</summary>
    /// <param name="flow">The flow to render.</param>
    /// <param name="context">Optional size, theme and frame; omitted uses the flow's own export request.</param>
    /// <param name="configure">Optional localization and content security policy settings.</param>
    public static string ToInteractiveHtmlFragment(this FlowArtifact flow, VisualRenderContext? context = null,
        Action<HtmlInteractiveDiagramOptions>? configure = null) => Renderer(configure).Render(flow, context, page: false);


    /// <summary>Renders a complete self-contained interactive sequence page with message playback.</summary>
    /// <param name="sequence">The sequence to render.</param>
    /// <param name="context">Optional size, theme and frame; omitted uses the sequence's natural size and theme.</param>
    /// <param name="configure">Optional localization and content security policy settings.</param>
    public static string ToInteractiveHtmlPage(this SequenceArtifact sequence, VisualRenderContext? context = null,
        Action<HtmlInteractiveDiagramOptions>? configure = null) => Renderer(configure).Render(sequence, context, page: true);

    /// <summary>Renders a self-contained interactive sequence fragment with its style and runtime inlined.</summary>
    /// <param name="sequence">The sequence to render.</param>
    /// <param name="context">Optional size, theme and frame; omitted uses the sequence's natural size and theme.</param>
    /// <param name="configure">Optional localization and content security policy settings.</param>
    public static string ToInteractiveHtmlFragment(this SequenceArtifact sequence, VisualRenderContext? context = null,
        Action<HtmlInteractiveDiagramOptions>? configure = null) => Renderer(configure).Render(sequence, context, page: false);


    private static HtmlInteractiveDiagramRenderer Renderer(Action<HtmlInteractiveDiagramOptions>? configure) {
        var renderer = new HtmlInteractiveDiagramRenderer();
        configure?.Invoke(renderer.Options);
        return renderer;
    }
}
