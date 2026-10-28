using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

/// <summary>An asset-free interactive chart section and the detached scene used to render it.</summary>
/// <remarks>The section and prepared visual are captured together by one preparation. They do not retain
/// the mutable chart or configuration callback. Register the shared HTML assets once in the host document.</remarks>
public sealed class HtmlChartSection {
    internal HtmlChartSection(string html, PreparedVisual preparedVisual, PreparedVisual? narrowPreparedVisual = null, VisualSvgOptions? svgOptions = null)
        : this(html, preparedVisual, narrowPreparedVisual, svgOptions, null, null) { }
    internal HtmlChartSection(string html, PreparedVisual preparedVisual, PreparedVisual? narrowPreparedVisual, VisualSvgOptions? svgOptions,
        VisualSvgEmission? wideEmission, VisualSvgEmission? narrowEmission, IReadOnlyList<HtmlCapturedFontAssets.Payload>? fontPayloads = null) {
        Html = html; _fontPayloads = fontPayloads == null ? Array.Empty<HtmlCapturedFontAssets.Payload>() : System.Linq.Enumerable.ToArray(fontPayloads);
        SvgOptions = svgOptions ?? new VisualSvgOptions();
        WideEmission = wideEmission; NarrowEmission = narrowEmission;
        PreparedVisual = preparedVisual;
        NarrowPreparedVisual = narrowPreparedVisual;
    }

    private readonly IReadOnlyList<HtmlCapturedFontAssets.Payload> _fontPayloads;
    // The exact generated root observation retains SHA/length/view identities; only owned byte text is omitted.
    internal string TransitionHtml {
        get {
            var html = Html;
            foreach (var payload in _fontPayloads) {
                var at = html.IndexOf(payload.Full, StringComparison.Ordinal);
                if (at < 0 || html.IndexOf(payload.Full, at + payload.Full.Length, StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("A native font transition requires its exact singular captured root payload.");
                html = html.Remove(at, payload.Full.Length).Insert(at, payload.Observation);
            }
            return html;
        }
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
