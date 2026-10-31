using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static readonly HashSet<string> CrosshairKinds = new(StringComparer.Ordinal) {
        "Line", "StepLine", "Area", "StepArea", "StackedArea", "RangeArea", "RangeBand", "TrendLine", "DifferenceArea"
    };

    /// <summary>Opt-in hook decisions for one emission: series ordinals by key and the scene groups that are interaction targets.</summary>
    private sealed class EnhancePlan {
        internal readonly Dictionary<string, string> Series = new(StringComparer.Ordinal);
        internal readonly HashSet<int> Marks = new();
        internal readonly Dictionary<string, ChartRect> Bounds = new(StringComparer.Ordinal);
    }

    /// <summary>Writes the opt-in root hooks for the enhance runtime and plans the per-group hooks.</summary>
    private static EnhancePlan? WriteEnhanceRootAttributes(SvgMarkupWriter writer, VisualScene scene, VisualSvgOptions? options, string view) {
        if (options?.EnhanceHooks != true) return null;
        var plan = new EnhancePlan();
        // Marks without painted geometry (for example line points without markers) still need a logical anchor.
        foreach (var region in scene.Regions) if (!plan.Bounds.ContainsKey(region.Id)) plan.Bounds.Add(region.Id, region.Bounds);
        var crosshair = false;
        // A target is the innermost series group that identifies one datum in any family: a point, a typed
        // target (hierarchy and flow nodes/links), a single-value source or a dated cell. Legends are controls, not data.
        var open = new Stack<int>();
        for (var index = 0; index < scene.Nodes.Count; index++) {
            if (scene.Nodes[index] is VisualSceneEndGroup) { if (open.Count > 0) open.Pop(); continue; }
            if (scene.Nodes[index] is not VisualSceneGroup group) continue;
            open.Push(index);
            var metadata = group.Metadata;
            if (!metadata.TryGetValue(ChartDomRoles.SeriesAttribute, out var ordinal)) continue;
            if (metadata.TryGetValue(ChartDomRoles.SeriesKeyAttribute, out var key) && group.Role != ChartDomRoles.LegendEntry && !plan.Series.ContainsKey(key))
                plan.Series.Add(key, ordinal);
            if (group.Role == "series" && metadata.TryGetValue("data-cfx-kind", out var kind) && CrosshairKinds.Contains(kind)) crosshair = true;
            if (group.Role == ChartDomRoles.LegendEntry || !(metadata.ContainsKey(ChartDomRoles.PointAttribute) || metadata.ContainsKey("data-cfx-target-id")
                || metadata.ContainsKey("data-cfx-source-point") || metadata.ContainsKey("data-cfx-date"))) continue;
            foreach (var ancestor in open) if (ancestor != index) plan.Marks.Remove(ancestor);
            plan.Marks.Add(index);
        }
        writer.Attribute(ChartDomRoles.EnhanceAttribute, "1").Attribute(ChartDomRoles.ViewAttribute, view)
            .Attribute(ChartDomRoles.SizeAttribute, N(scene.Size.Width) + " " + N(scene.Size.Height));
        var projection = scene.DataProjection;
        if (projection != null) {
            var plot = projection.Plot;
            writer.Attribute(ChartDomRoles.PlotAttribute, N(plot.X) + " " + N(plot.Y) + " " + N(plot.Width) + " " + N(plot.Height));
            if (projection.HasX) {
                var axis = projection.Horizontal && projection.Y != null ? projection.Y : projection.X;
                writer.Attribute(ChartDomRoles.XDomainAttribute, N(axis.Minimum) + " " + N(axis.Maximum))
                    .Attribute(ChartDomRoles.XKindAttribute, ScaleName(axis.Scale));
            }
            if (projection.Horizontal) writer.Attribute(ChartDomRoles.OrientationAttribute, "horizontal");
            crosshair &= !projection.Horizontal;
            if (crosshair) {
                writer.Attribute(ChartDomRoles.CrosshairAttribute, "true");
                if (projection.X.Scale is ChartScaleKind.Linear or ChartScaleKind.Time) writer.Attribute(ChartDomRoles.BrushAttribute, "true");
            }
        }
        return plan;
    }

    private static string ScaleName(ChartScaleKind scale) => scale switch {
        ChartScaleKind.Time => "time",
        ChartScaleKind.Logarithmic => "log",
        ChartScaleKind.SymmetricLogarithmic => "symlog",
        _ => "linear"
    };

    private static void WriteEnhanceGroupHooks(SvgMarkupWriter writer, VisualSceneGroup group, int index, EnhancePlan? plan) {
        if (plan == null) return;
        if (plan.Marks.Contains(index)) {
            writer.Attribute(ChartDomRoles.MarkAttribute, "true");
            if (group.Id != null && plan.Bounds.TryGetValue(group.Id, out var bounds))
                writer.Attribute(ChartDomRoles.BoundsAttribute, N(bounds.X) + " " + N(bounds.Y) + " " + N(bounds.Width) + " " + N(bounds.Height));
        }
        if (group.Role == ChartDomRoles.LegendEntry && group.Metadata.TryGetValue(ChartDomRoles.SeriesKeyAttribute, out var key) && plan.Series.TryGetValue(key, out var ordinal))
            writer.Attribute(ChartDomRoles.LegendForAttribute, ordinal);
    }

    /// <summary>Writes inert JSON describing series and X-sorted points. Metadata text never executes and needs no CSP allowance.</summary>
    private static void WriteEnhanceData(SvgMarkupWriter writer, VisualScene scene, VisualSvgOptions? options) {
        if (options?.EnhanceData != true) return;
        var names = new SortedDictionary<int, string>();
        var keys = new Dictionary<int, string>();
        var colors = new Dictionary<int, string>();
        var legendColors = new Dictionary<string, string>(StringComparer.Ordinal);
        var points = new List<(double X, int Series, int Point, string Json)>();
        var stack = new Stack<int>();
        string? legendKey = null;
        foreach (var node in scene.Nodes) {
            if (node is VisualSceneGroup group) {
                var ordinal = group.Metadata.TryGetValue(ChartDomRoles.SeriesAttribute, out var text)
                    && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : -1;
                if (ordinal < 0 && stack.Count > 0) ordinal = stack.Peek();
                stack.Push(ordinal);
                legendKey = group.Role == ChartDomRoles.LegendEntry && group.Metadata.TryGetValue(ChartDomRoles.SeriesKeyAttribute, out var legend) ? legend : legendKey;
                if (ordinal < 0) continue;
                if (group.Metadata.TryGetValue(ChartDomRoles.SeriesKeyAttribute, out var key) && !keys.ContainsKey(ordinal)) keys[ordinal] = key;
                if (group.Metadata.TryGetValue(ChartDomRoles.SeriesNameAttribute, out var name)) names[ordinal] = name;
                else if (!names.ContainsKey(ordinal)) names[ordinal] = string.Empty;
                if (group.Metadata.ContainsKey(ChartDomRoles.PointAttribute)) points.Add(EnhancePoint(group.Metadata, ordinal));
            } else if (node is VisualSceneEndGroup) {
                if (stack.Count > 0) stack.Pop();
                if (stack.Count == 0) legendKey = null;
            } else if (node is VisualSceneMark mark) {
                var paint = mark.Fill.HasValue && mark.Fill.Value.A > 0 ? ResolvePaint(mark.Fill, mark.Paint?.Fill, options)
                    : mark.Stroke.HasValue ? ResolvePaint(mark.Stroke, mark.Paint?.Stroke, options) : null;
                if (paint == null) continue;
                if (legendKey != null && mark.Role == "legend-swatch" && !legendColors.ContainsKey(legendKey)) legendColors[legendKey] = paint;
                var owner = stack.Count > 0 ? stack.Peek() : -1;
                if (owner >= 0 && !colors.ContainsKey(owner)) colors[owner] = paint;
            }
        }
        var json = new StringBuilder("{\"v\":1,\"series\":[");
        var first = true;
        foreach (var entry in names) {
            if (!first) json.Append(',');
            first = false;
            json.Append("{\"i\":").Append(entry.Key.ToString(CultureInfo.InvariantCulture));
            keys.TryGetValue(entry.Key, out var key);
            if (key != null) { json.Append(",\"key\":"); ChartLinkRecord.AppendString(json, key); }
            json.Append(",\"name\":"); ChartLinkRecord.AppendString(json, entry.Value);
            var color = key != null && legendColors.TryGetValue(key, out var swatch) ? swatch : colors.TryGetValue(entry.Key, out var painted) ? painted : null;
            if (color != null) { json.Append(",\"color\":"); ChartLinkRecord.AppendString(json, color); }
            json.Append('}');
        }
        json.Append("],\"points\":[");
        json.Append(string.Join(",", points.OrderBy(point => point.X).ThenBy(point => point.Series).ThenBy(point => point.Point).Select(point => point.Json)));
        json.Append("]}");
        writer.StartElement("metadata").Attribute(ChartDomRoles.EnhanceDataAttribute, "true").EndStartElement().Text(json.ToString()).EndElement();
    }

    private static (double X, int Series, int Point, string Json) EnhancePoint(IReadOnlyDictionary<string, string> metadata, int series) {
        var point = metadata.TryGetValue(ChartDomRoles.PointAttribute, out var text) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        var x = metadata.TryGetValue(ChartDomRoles.XAttribute, out var rawX) && double.TryParse(rawX, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : double.NaN;
        var json = new StringBuilder("{\"s\":").Append(series.ToString(CultureInfo.InvariantCulture)).Append(",\"p\":").Append(point.ToString(CultureInfo.InvariantCulture));
        if (ChartMath.IsFinite(x)) json.Append(",\"x\":").Append(x.ToString("R", CultureInfo.InvariantCulture));
        void Text(string name, string attribute) {
            if (!metadata.TryGetValue(attribute, out var value)) return;
            json.Append(",\"").Append(name).Append("\":"); ChartLinkRecord.AppendString(json, value);
        }
        Text("xi", ChartDomRoles.XInstantAttribute);
        Text("l", ChartDomRoles.DisplayLabelAttribute);
        Text("v", ChartDomRoles.FormattedValueAttribute);
        if (!metadata.ContainsKey(ChartDomRoles.FormattedValueAttribute)) Text("v", ChartDomRoles.RangeLabelAttribute);
        if (metadata.TryGetValue(ChartDomRoles.KeysAttribute, out var keys)) json.Append(",\"k\":").Append(keys);
        return (ChartMath.IsFinite(x) ? x : double.MaxValue, series, point, json.Append('}').ToString());
    }
}
