using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    /// <summary>Captures bounded native SVG frames for one typed, identified Progress, LayeredRadial, Circle, ProgressRing, Gauge, Bullet or compatible fixed Line/Bar, positive Pie/Donut or fixed authored Treemap/Sunburst value edit.</summary>
    /// <param name="from">The retained committed native section used as the host request basis.</param>
    /// <param name="to">The newly prepared destination section returned by the same native host.</param>
    /// <param name="frameCount">The finite number of captured native observations, including both endpoints; 2..16.</param>
    /// <param name="cancellationToken">Cancels native capture before a complete response is published.</param>
    /// <returns>JSON for the optional native reply's transition field. The normal html/basis response remains authoritative.</returns>
    /// <remarks>Both sections must retain matching captured native readings, authored identities, row domains, built-in
    /// numeric policy, captured native faces, frame and presentation. Exactly one raw value may differ; Progress and ProgressRing require in-domain readings; LayeredRadial, Circle, Gauge and Bullet retain native raw-value clamping.
    /// Transitions also require unchanged surrounding native presentation, including any value-dependent legend.
    /// Every responsive drawing is captured together. The browser schedules these completed native frames and
    /// never formats values or calculates intermediate geometry. This does not change static SVG/PNG output,
    /// start playback, retain mutable chart data or add an acquisition scheduler.</remarks>
    public string PrepareValueTransitionJson(HtmlChartSection from, HtmlChartSection to, int frameCount = 12,
        CancellationToken cancellationToken = default) {
        if (from == null) throw new ArgumentNullException(nameof(from));
        if (to == null) throw new ArgumentNullException(nameof(to));
        if ((from.NarrowPreparedVisual == null) != (to.NarrowPreparedVisual == null))
            throw new NotSupportedException("A native value transition cannot add or remove an adaptive drawing.");
        if (from.SvgOptions.IdPrefix != to.SvgOptions.IdPrefix)
            throw new NotSupportedException("Native value transitions retain the host's captured ID scope.");
        var wide = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, frameCount, cancellationToken);
        var first = from.PreparedVisual.Scene.ValueCapture!.Plan;
        var last = to.PreparedVisual.Scene.ValueCapture!.Plan;
        var changed = Enumerable.Range(0, first.Readings.Count).Single(index => first.Readings[index].Value != last.Readings[index].Value);
        var row = first.Readings[changed]; var seriesKey = row.SeriesKey ?? first.SeriesKey;
        var json = new StringBuilder("{\"kind\":").Append(HtmlJsonString.Encode(first.Kind)).Append(",\"seriesKey\":")
            .Append(HtmlJsonString.Encode(seriesKey)).Append(first.Kind == "hierarchy-value" ? ",\"nodeId\":" : ",\"observationId\":").Append(HtmlJsonString.Encode(row.ObservationId!))
            .Append(",\"fromValue\":").Append(N(row.Value)).Append(",\"toValue\":").Append(N(last.Readings[changed].Value))
            .Append(",\"minimum\":").Append(N(row.Minimum)).Append(",\"maximum\":").Append(N(row.Maximum))
            .Append(",\"wide\":[");
        Append(wide, "wide");
        json.Append(']');
        if (from.NarrowPreparedVisual != null) {
            var narrow = VisualValueCapture.Frames(from.NarrowPreparedVisual, to.NarrowPreparedVisual!, frameCount, cancellationToken);
            var narrowFrom = from.NarrowPreparedVisual.Scene.ValueCapture!.Plan;
            var narrowTo = to.NarrowPreparedVisual!.Scene.ValueCapture!.Plan;
            var narrowChanged = Enumerable.Range(0, narrowFrom.Readings.Count).Single(index => narrowFrom.Readings[index].Value != narrowTo.Readings[index].Value);
            var narrowRow = narrowFrom.Readings[narrowChanged];
            if (first.Kind != narrowFrom.Kind || seriesKey != (narrowRow.SeriesKey ?? narrowFrom.SeriesKey) || row.ObservationId != narrowRow.ObservationId
                || row.Value != narrowRow.Value || last.Readings[changed].Value != narrowTo.Readings[narrowChanged].Value
                || row.Minimum != narrowRow.Minimum || row.Maximum != narrowRow.Maximum)
                throw new NotSupportedException("Responsive native drawings must retain the same source value edit.");
            json.Append(",\"narrow\":["); Append(narrow, "narrow"); json.Append(']');
        }
        cancellationToken.ThrowIfCancellationRequested();
        json.Append('}');
        if (Encoding.UTF8.GetByteCount(json.ToString()) > 1024 * 1024)
            throw new InvalidOperationException("Native value transition JSON exceeds the one MiB presentation budget. Reduce frameCount.");
        return json.ToString();

        string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        void Append(System.Collections.Generic.IReadOnlyList<PreparedVisual> frames, string drawing) {
            for (var index = 0; index < frames.Count; index++) {
                cancellationToken.ThrowIfCancellationRequested();
                if (index > 0) json.Append(',');
                var svg = from.SvgOptions.WithIdPrefix((from.SvgOptions.IdPrefix ?? "cfx-native-value") + "-motion-" + drawing + "-" + index.ToString(CultureInfo.InvariantCulture));
                json.Append(HtmlJsonString.Encode(frames[index].ToSvg(svg)));
                if (json.Length > 1024 * 1024) throw new InvalidOperationException("Native value transition presentation budget exceeded.");
            }
        }
    }
}
