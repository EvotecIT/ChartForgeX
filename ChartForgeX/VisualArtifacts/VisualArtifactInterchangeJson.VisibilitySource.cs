using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    /// <summary>Canonical finite source facts only; no public envelope/family or alternate JSON writer is introduced.</summary>
    internal static string SerializeVisibilitySource(ChartVisibilitySource source, VisualRenderContext context, bool includeValues = true, bool includeDerivedMembership = true) {
        var writer = new VisualArtifactInterchangeJsonWriter(); writer.StartObject();
        String(writer, "owner", "native-visibility-source"); String(writer, "title", source.Title); String(writer, "subtitle", source.Subtitle);
        Number(writer, "authoredWidth", source.Width); Number(writer, "authoredHeight", source.Height);
        var presentation = source.CopyPresentation(); writer.Property("presentation"); writer.StartObject(); WaterfallPresentation(writer, presentation); writer.EndObject();
        writer.Property("axisExtensions"); writer.StartArray();
        foreach (var axis in new[] { presentation.XAxis, presentation.YAxis, presentation.SecondaryXAxis, presentation.SecondaryYAxis }) {
            writer.StartObject(); OptionalString(writer, "timeZone", axis.TimeZone?.ToSerializedString());
            writer.Property("sessions"); writer.StartArray();
            if (axis.TimeSessions != null) foreach (var session in axis.TimeSessions.Sessions) { writer.StartArray(); writer.Number(session.Minimum); writer.Number(session.Maximum); writer.EndArray(); }
            writer.EndArray();
            if (axis.TimeSpacing != null) { String(writer, "spacing", axis.TimeSpacing.CalendarUnit == null ? "ordinal" : "calendar");
                if (axis.TimeSpacing.CalendarUnit is { } unit) { String(writer, "unit", unit.Unit.ToString()); Number(writer, "count", unit.Count); String(writer, "weekStart", unit.WeekStart.ToString()); } }
            writer.EndObject();
        }
        writer.EndArray();
        String(writer, "barMode", source.BarMode.ToString()); Boolean(writer, "showStackTotals", source.ShowStackTotals); OptionalNumber(writer, "maximumBarWidth", source.MaximumBarWidth);
        String(writer, "lineOrientation", source.LineOrientation.ToString()); OptionalString(writer, "lineMarkers", source.LineMarkers?.ToString());
        var line = source.CopyLine(); writer.Property("lineStyle"); writer.StartObject();
        Number(writer, "ambientOpacity", line.AmbientHaloOpacity); Number(writer, "ambientExtra", line.AmbientHaloStrokeExtra);
        Number(writer, "haloOpacity", line.HaloOpacity); Number(writer, "haloExtra", line.HaloStrokeExtra);
        Number(writer, "highlightOpacity", line.HighlightOpacity); Number(writer, "highlightRatio", line.HighlightStrokeRatio); writer.EndObject();
        var boxes = source.CopyBoxes(); writer.Property("boxes"); writer.StartObject(); String(writer, "orientation", boxes.Orientation.ToString()); String(writer, "labelContent", boxes.LabelContent.ToString());
        writer.Property("componentLabels"); writer.StartArray();
        foreach (var label in new[] { boxes.MinimumLabel, boxes.Q1Label, boxes.MedianLabel, boxes.Q3Label, boxes.MaximumLabel }) {
            writer.StartObject(); Boolean(writer, "visible", label.Visible); OptionalString(writer, "placement", label.Placement?.ToString()); DetailTextStyle(writer, label.Style); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();
        var accessibility = source.CopyAccessibility(); writer.Property("accessibility"); writer.StartObject();
        OptionalString(writer, "name", accessibility.Name); OptionalString(writer, "description", accessibility.Description); OptionalString(writer, "language", accessibility.Language);
        Boolean(writer, "decorative", accessibility.IsDecorative); writer.EndObject();
        VisibilitySourceContext(writer, context, preserveFontPaths: source.Pie == null && source.Hierarchy == null);
        if (source.Pie != null) VisibilitySourcePie(writer, source, includeDerivedMembership);
        if (source.Hierarchy != null) VisibilitySourceHierarchy(writer, source.Hierarchy);
        writer.Property("series"); writer.StartArray();
        foreach (var row in source.SeriesRows) VisibilitySourceSeries(writer, row, includeValues);
        writer.EndArray(); writer.EndObject(); return writer.ToString();
    }

    private static void VisibilitySourceSeries(VisualArtifactInterchangeJsonWriter writer, ChartVisibilitySource.Series source, bool includeValues) {
        writer.StartObject(); String(writer, "id", source.Id.Value); String(writer, "name", source.Name); String(writer, "kind", source.Kind.ToString());
        OptionalString(writer, "color", WaterfallColor(source.Color)); String(writer, "state", source.State.ToString()); String(writer, "pattern", source.Pattern.ToString());
        String(writer, "valueAxis", source.ValueAxis.ToString()); Boolean(writer, "showInLegend", source.ShowInLegend); OptionalString(writer, "semanticRole", source.SemanticRole);
        if (source.Labels.HasValue) Boolean(writer, "labels", source.Labels.Value); OptionalString(writer, "placement", source.Placement?.ToString());
        OptionalNumber(writer, "strokeWidth", source.StrokeWidth); String(writer, "interpolation", source.Interpolation.ToString()); String(writer, "step", source.Step.ToString());
        OptionalString(writer, "stackGroup", source.StackGroup); OptionalNumber(writer, "normalizedTo", source.NormalizedTo); DetailTextStyle(writer, source.CopyLabelStyle());
        if (source.Kind == ChartSeriesKind.StackedArea) String(writer, "stackedAreaMissingData", source.StackedAreaMissingData.ToString());
        VisibilitySourceMarker(writer, source.CopyMarkers());
        writer.Property("points"); writer.StartArray(); foreach (var point in source.Points) { writer.StartArray(); writer.Number(point.X); if (includeValues) writer.Number(point.Y); writer.Boolean(point.BreakBefore); writer.EndArray(); } writer.EndArray();
        writer.Property("suppliedSummaries"); writer.StartArray(); foreach (var box in source.Boxes) { writer.StartArray(); writer.Number(box.X); writer.Number(box.Minimum); writer.Number(box.Q1); writer.Number(box.Median); writer.Number(box.Q3); writer.Number(box.Maximum); writer.EndArray(); } writer.EndArray();
        Boolean(writer, "raw", source.Raw); writer.Property("rawGroups"); writer.StartArray();
        foreach (var group in source.RawGroups) { writer.StartObject(); Number(writer, "x", group.X); writer.Property("samples"); writer.StartArray(); foreach (var sample in group.Samples) writer.Number(sample); writer.EndArray(); writer.EndObject(); }
        writer.EndArray();
        if (source.CopyStatistics() is { } statistics) { String(writer, "estimator", statistics.Estimator.ToString()); String(writer, "whiskers", statistics.WhiskerPolicy.ToString()); Number(writer, "iqrMultiplier", statistics.IqrMultiplier); }
        writer.Property("observationIds"); writer.StartArray(); foreach (var pair in source.ObservationIds.OrderBy(pair => pair.Key)) { writer.StartArray(); writer.Number(pair.Key); writer.String(pair.Value.Value); writer.EndArray(); } writer.EndArray();
        writer.Property("groupIds"); writer.StartArray(); foreach (var pair in source.GroupIds.OrderBy(pair => pair.Key)) { writer.StartArray(); writer.Number(pair.Key); writer.String(pair.Value.Value); writer.EndArray(); } writer.EndArray();
        writer.Property("pointColors"); writer.StartArray(); foreach (var color in source.PointColors) writer.String(WaterfallColor(color)); writer.EndArray();
        writer.Property("pointPatterns"); writer.StartArray(); foreach (var pattern in source.PointPatterns) writer.String(pattern?.ToString()); writer.EndArray();
        writer.Property("pointLabels"); writer.StartArray(); foreach (var label in source.PointLabels) writer.String(label); writer.EndArray();
        writer.Property("pointStyles"); writer.StartArray(); for (var index = 0; index < source.PointStyleCount; index++) {
            var style = source.CopyPointStyle(index); if (style == null) writer.String(null); else { writer.StartObject(); DetailTextStyle(writer, style); writer.EndObject(); }
        }
        writer.EndArray();
        if (ChartSeriesKindTraits.IsPieLikeKind(source.Kind)) {
            writer.Property("sliceOffsets"); writer.StartArray(); foreach (var value in source.SliceOffsets) writer.Number(value); writer.EndArray();
            writer.Property("sliceRadii"); writer.StartArray(); foreach (var value in source.SliceRadiusRatios) { if (value.HasValue) writer.Number(value.Value); else writer.String(null); } writer.EndArray();
            if (source.RadialBand != null) { Number(writer, "bandInner", source.RadialBand.InnerRatio); Number(writer, "bandOuter", source.RadialBand.OuterRatio); }
        }
        if (source.IsHierarchy) VisibilitySourceHierarchySeries(writer, source, includeValues);
        writer.EndObject();
    }
}
