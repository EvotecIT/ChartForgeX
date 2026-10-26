using System;
using System.Text;
using System.Threading;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    /// <summary>Captures a whole-visual initial fade over one authoritative native final section.</summary>
    /// <param name="final">The already prepared immutable final native section; presentInitial requires this exact accepted destination.</param>
    /// <param name="options">InitialFade and its finite frame count. Value/geometry effects retain their explicit baseline overload.</param>
    /// <param name="cancellationToken">Cancels before a complete payload is published.</param>
    /// <remarks>The existing initial presentation owner retains accepted source, revision, history, export and cancellation semantics.
    /// Every captured native chart family can fade without inventing zero, logarithmic, calendar or hierarchy data.
    /// Fractional native opacity uses the existing isolated compositor and its finite shared resource budget.
    /// Automatic font-root destinations omit repeated binary text while retaining exact root identities, declared lengths
    /// and face descriptors from the accepted section. Other destination fields and SVG endpoints stay exact, within one MiB.</remarks>
    public string PrepareNativeTransitionJson(HtmlChartSection final, HtmlNativeTransitionOptions options, CancellationToken cancellationToken = default) {
        if (final == null) throw new ArgumentNullException(nameof(final));
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (options.Effect != HtmlNativeTransitionEffect.InitialFade)
            throw new ArgumentException("The final-only native transition overload requires InitialFade.", nameof(options));
        cancellationToken.ThrowIfCancellationRequested();
        var wide = VisualValueCapture.InitialFadeFrames(final.PreparedVisual, options.FrameCount, cancellationToken);
        var narrow = final.NarrowPreparedVisual == null ? null : VisualValueCapture.InitialFadeFrames(final.NarrowPreparedVisual, options.FrameCount, cancellationToken);
        var json = new StringBuilder("{\"kind\":\"native-transition\",\"effect\":\"initial-fade\",\"sourceKind\":\"native-visual-opacity\",\"destination\":")
            .Append(HtmlJsonString.Encode(final.TransitionHtml)).Append(",\"readings\":[],\"drawings\":{\"wide\":");
        AppendNativeTransitionDrawing(json, final.PreparedVisual, final.PreparedVisual, final.WideEmission, final.WideEmission, wide, options, "wide", cancellationToken);
        if (narrow != null) {
            json.Append(",\"narrow\":"); AppendNativeTransitionDrawing(json, final.NarrowPreparedVisual!, final.NarrowPreparedVisual!, final.NarrowEmission, final.NarrowEmission, narrow, options, "narrow", cancellationToken);
        }
        json.Append("}}"); CheckNativeTransitionBudget(json); cancellationToken.ThrowIfCancellationRequested(); return json.ToString();
    }
}
