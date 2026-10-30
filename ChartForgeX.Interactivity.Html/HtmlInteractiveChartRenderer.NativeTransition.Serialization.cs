using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static void CheckNativeTransitionBudget(StringBuilder json) {
        if (Encoding.UTF8.GetByteCount(json.ToString()) > 1024 * 1024)
            throw new InvalidOperationException("Native transition exceeds the one MiB presentation budget. Reduce frameCount or native source size.");
    }
    private static void AppendNativeTransitionDrawing(StringBuilder json, PreparedVisual from, PreparedVisual to,
        VisualSvgEmission? source, VisualSvgEmission? destination, IReadOnlyList<PreparedVisual> frames,
        HtmlNativeTransitionOptions options, string name, CancellationToken cancellation,
        IReadOnlyList<(PreparedVisual Frame, bool Source, double Opacity)>? reflowFrames = null) {
        if (source == null || destination == null || source.CapturePngText != destination.CapturePngText
            || source.Options.TextPresentation != destination.Options.TextPresentation)
            throw new NotSupportedException("Native transitions require the same captured native HTML export mode.");
        PreparedVisual Mode(PreparedVisual value) => source.CapturePngText ? value.ForInteractivePngExport() : value;
        if (Mode(from).ToSvg(source.Options) != source.Svg || Mode(to).ToSvg(destination.Options) != destination.Svg)
            throw new NotSupportedException("Native transition endpoints must exactly reproduce the same-emission HTML export facts.");
        var sourceWithDestinationPolicy = destination.Options.WithIdPrefix(source.Prefix);
        if (Mode(from).ToSvg(sourceWithDestinationPolicy) != source.Svg)
            throw new NotSupportedException("Native transition export paint, link, responsive and pattern policy must remain captured and unchanged.");
        var initialFade = options.Effect == HtmlNativeTransitionEffect.InitialFade;
        var reflow = reflowFrames != null;
        var firstIsDestination = options.Effect == HtmlNativeTransitionEffect.ValueFlash;
        var expectedFirst = initialFade ? Mode(VisualValueCapture.OpacityFrame(to, 0)).ToSvg(destination.Options) : firstIsDestination ? destination.Svg : source.Svg;
        if (Mode(frames[0]).ToSvg(initialFade || firstIsDestination ? destination.Options : source.Options) != expectedFirst
            || Mode(frames[frames.Count - 1]).ToSvg(destination.Options) != destination.Svg)
            throw new NotSupportedException("Native transition frame endpoints must equal their captured native emission exactly.");
        json.Append("{\"capturePngText\":").Append(source.CapturePngText ? "true" : "false")
            .Append(",\"sourcePrefix\":").Append(HtmlJsonString.Encode(source.Prefix))
            .Append(",\"destinationPrefix\":").Append(HtmlJsonString.Encode(destination.Prefix))
            .Append(",\"frames\":[");
        for (var index = 0; index < frames.Count; index++) {
            cancellation.ThrowIfCancellationRequested(); if (index > 0) json.Append(',');
            var prefix = destination.Prefix + "-native-presentation-" + name + "-" + index.ToString(CultureInfo.InvariantCulture);
            var policy = destination.Options.WithIdPrefix(prefix);
            json.Append("{\"prefix\":").Append(HtmlJsonString.Encode(prefix)).Append(",\"svg\":").Append(HtmlJsonString.Encode(Mode(frames[index]).ToSvg(policy)));
            if (reflow) {
                var fact = reflowFrames![index];
                json.Append(",\"basis\":").Append(HtmlJsonString.Encode(fact.Source ? "source" : "destination"))
                    .Append(",\"nativeOpacity\":").Append(HtmlJsonString.Encode(ChartForgeX.Svg.SvgMarkupWriter.FormatNumber(fact.Opacity)));
            }
            var nativeValues = PreparedNativeValueSource(frames[index], reflow ? reflowFrames![index].Source ? from : to : options.Effect == HtmlNativeTransitionEffect.ValueFlash ? to : null);
            if (nativeValues != null) json.Append(",\"nativeValueSource\":").Append(nativeValues);
            if (initialFade) json.Append(",\"nativeOpacity\":").Append(HtmlJsonString.Encode(ChartForgeX.Svg.SvgMarkupWriter.FormatNumber(index / (double)(frames.Count - 1))));
            json.Append('}');
            CheckNativeTransitionBudget(json);
        }
        json.Append("]}");
    }
    private static void ValidateNativeTransitionReadings(IVisualValuePlan first, IVisualValuePlan last, IVisualValuePlan? narrowFirst, IVisualValuePlan? narrowLast) {
        var identities = new HashSet<(string SeriesKey, string ObservationId, string? Component)>();
        for (var index = 0; index < first.Readings.Count; index++) {
            var a = first.Readings[index];
            if (a.ObservationId == null || !identities.Add((a.SeriesKey ?? first.SeriesKey, a.ObservationId, a.Component)))
                throw new NotSupportedException("Native transitions require distinct ordered authored series and observation identities.");
        }
        if (narrowFirst == null && narrowLast == null) return;
        if (narrowFirst == null || narrowLast == null || narrowFirst.Kind != first.Kind || narrowFirst.Readings.Count != first.Readings.Count || narrowLast.Readings.Count != last.Readings.Count)
            throw new NotSupportedException("Adaptive drawings must retain the same native source readings.");
        for (var index = 0; index < first.Readings.Count; index++) {
            var a = first.Readings[index]; var b = narrowFirst.Readings[index]; var x = last.Readings[index]; var y = narrowLast.Readings[index];
            if ((a.SeriesKey ?? first.SeriesKey) != (b.SeriesKey ?? narrowFirst.SeriesKey) || a.ObservationId != b.ObservationId || a.Component != b.Component || x.Component != y.Component || a.Value != b.Value || x.Value != y.Value
                || a.Minimum != b.Minimum || a.Maximum != b.Maximum || x.Minimum != y.Minimum || x.Maximum != y.Maximum)
                throw new NotSupportedException("Adaptive drawings retain the same ordered native values, identities and domains.");
        }
    }
    private static void AppendNativeTransitionReadings(StringBuilder json, IVisualValuePlan first, IVisualValuePlan last) {
        string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        json.Append(",\"readings\":[");
        for (var index = 0; index < first.Readings.Count; index++) {
            if (index > 0) json.Append(','); var a = first.Readings[index]; var b = last.Readings[index];
            json.Append("{\"seriesKey\":").Append(HtmlJsonString.Encode(a.SeriesKey ?? first.SeriesKey)).Append(first.Kind == "hierarchy-value" ? ",\"nodeId\":" : ",\"observationId\":").Append(HtmlJsonString.Encode(a.ObservationId!))
                .Append(",\"fromValue\":").Append(N(a.Value)).Append(",\"toValue\":").Append(N(b.Value))
                .Append(",\"minimum\":").Append(N(a.Minimum)).Append(",\"maximum\":").Append(N(a.Maximum));
            if (a.Component != null) json.Append(",\"component\":").Append(HtmlJsonString.Encode(a.Component));
            json.Append('}');
        }
        json.Append(']');
    }
    private static void AppendNativeTransitionVisibility(StringBuilder json, PreparedVisual from, PreparedVisual to) {
        var first = from.Scene.VisibilityCapture!; var last = to.Scene.VisibilityCapture!;
        json.Append(",\"visibility\":{\"seriesKeys\":[");
        for (var index = 0; index < first.SeriesKeys.Count; index++) { if (index > 0) json.Append(','); json.Append(HtmlJsonString.Encode(first.SeriesKeys[index])); }
        json.Append("],\"fromHidden\":["); Append(first); json.Append("],\"toHidden\":["); Append(last);
        json.Append("],\"labelLayering\":").Append(HtmlJsonString.Encode(first.View.LabelLayering == ChartForgeX.Core.ChartVisibilityLabelLayering.GroupWithSeries ? "groupWithSeries" : "preserveOverlay")).Append('}');
        void Append(VisualSeriesVisibilityCapture value) {
            var comma = false; foreach (var key in value.SeriesKeys) if (value.View.IsHidden(key)) { if (comma) json.Append(','); comma = true; json.Append(HtmlJsonString.Encode(key)); }
        }
    }
}
