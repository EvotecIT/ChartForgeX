using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    /// <summary>Captures finite native values, explicit initial values, changed-value flash, or finite retained native visibility through existing native owners.</summary>
    /// <param name="from">The captured source, or an explicit native baseline for InitialValues.</param>
    /// <param name="to">The captured destination used by the same host.</param>
    /// <param name="options">Immutable effect, observation count and optional explicit native flash paint.</param>
    /// <param name="cancellationToken">Cancellation checked before publishing a complete bounded payload.</param>
    /// <remarks>Values/InitialValues reuse the typed Progress, LayeredRadial, Circle, ProgressRing, Gauge, Bullet and finite detached Line/Bar plans and positive unaggregated single-series Pie/Donut complete native frames with unchanged slice order and captured primary faces or stable finite actual fallback cluster decisions. Unseen/changing fallback repertoires reject before payload capture. ValueFlash retains the typed gauge/progress plans and actual owned Line/Bar, Pie/Donut, Treemap/Sunburst ink; shared Line paths are series-owned and only changed point markers flash.
    /// Opt-in Bubble Y/Size uses authored component identities, fixed native axes and explicit size domains; raw positive sizes retain native clamping. Component plans use this entry rather than the scalar one-edit API.
    /// Opt-in RangeBand/RangeArea Lower/Upper uses the same complete component owner. ValueFlash repaints each changed series envelope across its real runs and both native markers of changed tuples; other tuple markers and captions remain unchanged.
    /// Opt-in ErrorBar values retain actual nullable uncertainty members, fixed native axes and containing guards; flash uses its real complete changed-point ink owner.
    /// Opt-in Candlestick/OHLC Open/High/Low/Close uses the same typed component owner and native rising/falling/doji policies; changed-point flash retains each complete financial mark.
    /// Component raw values may cross captured display windows under native clipping while intrinsic Log sign/zero and typed constraints remain strict. Fixed primary time X reuses its captured native axis policy; animated members remain numeric.
    /// One or more identified readings may change. InitialValues never invents zero, log/calendar or hierarchy baselines.
    /// ValueFlash retains accepted values/geometry and alters captured changed value ink, returning to its exact final paint.
    /// LegendVisibility uses Preserve whole-series Line/Bar composites or explicit Reflow Line/Bar observations and supplied/raw BoxPlot groups, and single-series Pie/Donut observations or captured full-source Other groups with native source/destination fades. Preserve enabled-label
    /// partial motion requires explicit GroupWithSeries. Browser playback retains the incumbent acceptance/basis/lifetime owner.
    /// Automatic owned-font destinations retain exact root identities, declared byte lengths and face-view descriptors;
    /// repeated binary text in their font-root templates is omitted. The accepted section supplies those immutable bytes.
    /// Every other destination field and captured SVG endpoint remains exact, under the unchanged one MiB presentation budget.
    /// The existing one-edit API and payload bytes remain unchanged. No universal-family or unlimited-size support is implied.</remarks>
    public string PrepareNativeTransitionJson(HtmlChartSection from, HtmlChartSection to, HtmlNativeTransitionOptions options,
        CancellationToken cancellationToken = default) {
        if (from == null) throw new ArgumentNullException(nameof(from));
        if (to == null) throw new ArgumentNullException(nameof(to));
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (options.Effect == HtmlNativeTransitionEffect.InitialFade)
            throw new ArgumentException("InitialFade uses the final-only PrepareNativeTransitionJson overload.", nameof(options));
        if ((from.NarrowPreparedVisual == null) != (to.NarrowPreparedVisual == null))
            throw new NotSupportedException("Native transitions retain both adaptive drawings.");
        cancellationToken.ThrowIfCancellationRequested();
        var joint = options.Effect == HtmlNativeTransitionEffect.ValuesAndVisibility;
        var sourceReflow = options.Effect == HtmlNativeTransitionEffect.SourceReflow;
        var legend = joint || sourceReflow || options.Effect == HtmlNativeTransitionEffect.LegendVisibility;
        var reflow = !joint && legend && from.PreparedVisual.Scene.VisibilityCapture?.ReflowSource != null;
        if (sourceReflow && !reflow) throw new NotSupportedException("SourceReflow requires two explicit native Reflow views with opt-in raw source capture.");
        var wideReflow = reflow ? VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, options.FrameCount, cancellationToken, sourceReflow) : null;
        var narrowReflow = reflow && from.NarrowPreparedVisual != null ? VisualSeriesVisibilityCapture.ReflowFrames(from.NarrowPreparedVisual, to.NarrowPreparedVisual!, options.FrameCount, cancellationToken, sourceReflow) : null;
        var wide = wideReflow?.Select(row => row.Frame).ToArray() ?? Capture(from.PreparedVisual, to.PreparedVisual);
        IReadOnlyList<PreparedVisual>? narrow = from.NarrowPreparedVisual == null ? null : narrowReflow?.Select(row => row.Frame).ToArray() ?? Capture(from.NarrowPreparedVisual, to.NarrowPreparedVisual!);
        var first = from.PreparedVisual.Scene.ValueCapture?.Plan; var last = to.PreparedVisual.Scene.ValueCapture?.Plan;
        if (sourceReflow) {
            VisualValueCapture.TryRawSource(from.PreparedVisual, out var a, out _); VisualValueCapture.TryRawSource(to.PreparedVisual, out var b, out _);
            IVisualRawValuePlan? c = null, d = null;
            if (from.NarrowPreparedVisual != null) { VisualValueCapture.TryRawSource(from.NarrowPreparedVisual, out c, out _); VisualValueCapture.TryRawSource(to.NarrowPreparedVisual!, out d, out _); }
            ValidateNativeTransitionReadings(a!, b!, c, d);
        }
        if (!legend || joint) ValidateNativeTransitionReadings(first!, last!, from.NarrowPreparedVisual?.Scene.ValueCapture?.Plan, to.NarrowPreparedVisual?.Scene.ValueCapture?.Plan);
        var effect = options.Effect switch {
            HtmlNativeTransitionEffect.Values => "values", HtmlNativeTransitionEffect.InitialValues => "initial-values",
            HtmlNativeTransitionEffect.ValueFlash => "value-flash", HtmlNativeTransitionEffect.SourceReflow => "source-reflow", HtmlNativeTransitionEffect.ValuesAndVisibility => "values-and-visibility", _ => "legend-visibility"
        };
        var json = new StringBuilder("{\"kind\":\"native-transition\",\"effect\":").Append(HtmlJsonString.Encode(effect))
            .Append(",\"sourceKind\":").Append(HtmlJsonString.Encode(sourceReflow ? "native-source-reflow" : joint ? first!.Kind : legend ? reflow ? "native-visibility-reflow" : "line-bar-visibility" : first!.Kind))
            .Append(",\"destination\":").Append(HtmlJsonString.Encode(to.TransitionHtml));
        if (sourceReflow) {
            VisualValueCapture.TryRawSource(from.PreparedVisual, out var a, out _); VisualValueCapture.TryRawSource(to.PreparedVisual, out var b, out _);
            AppendNativeTransitionReadings(json, a!, b!);
        } else if (joint) AppendNativeTransitionReadings(json, first!, last!);
        if (reflow) AppendNativeReflowVisibility(json, from.PreparedVisual, to.PreparedVisual);
        else if (legend) AppendNativeTransitionVisibility(json, from.PreparedVisual, to.PreparedVisual);
        else AppendNativeTransitionReadings(json, first!, last!);
        json.Append(",\"drawings\":{\"wide\":");
        AppendNativeTransitionDrawing(json, from.PreparedVisual, to.PreparedVisual, from.WideEmission, to.WideEmission, wide, options, "wide", cancellationToken, wideReflow);
        if (narrow != null) {
            json.Append(",\"narrow\":");
            AppendNativeTransitionDrawing(json, from.NarrowPreparedVisual!, to.NarrowPreparedVisual!, from.NarrowEmission, to.NarrowEmission, narrow, options, "narrow", cancellationToken, narrowReflow);
        }
        json.Append("}}"); CheckNativeTransitionBudget(json); cancellationToken.ThrowIfCancellationRequested(); return json.ToString();
        IReadOnlyList<PreparedVisual> Capture(PreparedVisual a, PreparedVisual b) => joint
            ? VisualSeriesVisibilityCapture.ValueVisibilityFrames(a, b, options.FrameCount, cancellationToken) : legend
            ? VisualSeriesVisibilityCapture.Frames(a, b, options.FrameCount, cancellationToken)
            : VisualValueCapture.NativeFrames(a, b, options.FrameCount, options.FlashColor, cancellationToken);
    }
}
