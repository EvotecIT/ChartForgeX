using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void VisibilitySourceHierarchy(VisualArtifactInterchangeJsonWriter writer, ChartVisibilitySource.HierarchyPolicy source) {
        writer.Property("hierarchy"); writer.StartObject(); OptionalString(writer, "root", source.Root?.RootId);
        if (source.CopyTreemap() is { } treemap) {
            String(writer, "kind", "treemap"); Number(writer, "gap", treemap.Gap); Number(writer, "padding", treemap.GroupPadding);
            Boolean(writer, "groupLabels", treemap.ShowGroupLabels); Boolean(writer, "values", treemap.ShowValues);
            OptionalNumber(writer, "minimumFont", treemap.MinimumLabelFontSize); String(writer, "trimming", treemap.LabelTrimming.ToString());
            VisibilitySourceHierarchyColor(writer, treemap.ColorScale, treemap.ColorDimension);
        }
        if (source.CopySunburst() is { } sunburst) {
            String(writer, "kind", "sunburst"); String(writer, "parentValues", sunburst.ParentValuePolicy.ToString());
            Number(writer, "corner", sunburst.CornerRadius); Number(writer, "gap", sunburst.SectorGap); Boolean(writer, "curved", sunburst.CurvedLabels);
            OptionalNumber(writer, "minimumFont", sunburst.MinimumLabelFontSize); Number(writer, "secondarySpacing", sunburst.SecondaryLabelSpacing);
            DetailTextStyle(writer, sunburst.SecondaryLabelStyle); VisibilitySourceHierarchyColor(writer, sunburst.ColorScale, sunburst.ColorDimension);
        }
        writer.EndObject();
    }
    private static void VisibilitySourceHierarchySeries(VisualArtifactInterchangeJsonWriter writer, ChartVisibilitySource.Series source, bool includeValues) {
        writer.Property("hierarchyItems"); writer.StartArray();
        foreach (var item in source.HierarchyItems) {
            writer.StartObject(); String(writer, "id", item.Id); String(writer, "label", item.Label); OptionalString(writer, "parent", item.ParentId);
            Boolean(writer, "authoredSize", item.Value.HasValue); if (includeValues) OptionalNumber(writer, "value", item.Value);
            OptionalNumber(writer, "color", item.ColorValue); writer.EndObject();
        }
        writer.EndArray(); writer.Property("nodeStates"); writer.StartArray();
        foreach (var pair in source.NodeStates.OrderBy(pair => pair.Key, System.StringComparer.Ordinal)) { writer.StartArray(); writer.String(pair.Key); writer.String(pair.Value.ToString()); writer.EndArray(); }
        writer.EndArray(); writer.Property("nodeStrokes"); writer.StartArray(); foreach (var stroke in source.PointStrokes) writer.String(WaterfallColor(stroke)); writer.EndArray();
    }
    private static void VisibilitySourceHierarchyColor(VisualArtifactInterchangeJsonWriter writer, ChartColorScale? scale, ChartColorDimensionOptions dimension) {
        writer.Property("colorDimension"); writer.StartObject(); OptionalString(writer, "name", dimension.Name); Boolean(writer, "show", dimension.ShowLegend); String(writer, "legend", dimension.LegendStyle.ToString());
        if (dimension.ValueFormat is { } format) { var policy = format.PortablePolicy(); String(writer, "notation", policy.Notation); String(writer, "format", policy.Format); String(writer, "culture", policy.Culture); }
        writer.EndObject(); if (scale == null) return;
        writer.Property("colorScale"); writer.StartObject(); String(writer, "mode", scale.Mode.ToString()); OptionalNumber(writer, "midIndex", scale.MidpointIndex);
        OptionalNumber(writer, "minimum", scale.MinimumValue); OptionalNumber(writer, "midpoint", scale.MidpointValue); OptionalNumber(writer, "maximum", scale.MaximumValue);
        OptionalString(writer, "noData", WaterfallColor(scale.NoDataColor)); OptionalString(writer, "lowLabel", scale.LowLabel); OptionalString(writer, "midLabel", scale.MidpointLabel); OptionalString(writer, "highLabel", scale.HighLabel);
        writer.Property("colors"); writer.StartArray(); foreach (var color in scale.Colors) writer.String(color.ToHexRgba()); writer.EndArray();
        writer.Property("stops"); writer.StartArray(); foreach (var stop in scale.Stops) { writer.StartArray(); writer.Number(stop.Value); writer.String(stop.Color.ToHexRgba()); writer.EndArray(); } writer.EndArray();
        writer.Property("bands"); writer.StartArray(); foreach (var band in scale.Bands) { writer.StartObject(); OptionalNumber(writer, "upper", band.UpperBound); String(writer, "color", band.Color.ToHexRgba()); OptionalString(writer, "label", band.Label); writer.EndObject(); } writer.EndArray();
        writer.Property("captions"); writer.StartArray(); foreach (var caption in scale.Captions) { writer.StartArray(); writer.Number(caption.Value); writer.String(caption.Text); writer.EndArray(); } writer.EndArray(); writer.EndObject();
    }
}
