using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void Waterfall(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeWaterfallArtifact? source) {
        if (source == null) return;
        writer.Property("waterfall"); writer.StartObject();
        Boolean(writer, "typedItems", source.TypedItems); String(writer, "seriesId", source.SeriesId);
        Boolean(writer, "automaticSeriesId", source.AutomaticSeriesId); String(writer, "seriesName", source.SeriesName);
        String(writer, "valueAxis", source.ValueAxis.ToString()); String(writer, "stateRole", source.StateRole.ToString());
        OptionalString(writer, "color", WaterfallColor(source.Color)); String(writer, "fillPattern", source.FillPattern.ToString());
        Boolean(writer, "showInLegend", source.ShowInLegend); OptionalString(writer, "semanticRole", source.SemanticRole);
        if (source.SeriesShowDataLabels.HasValue) Boolean(writer, "seriesShowDataLabels", source.SeriesShowDataLabels.Value);
        OptionalString(writer, "seriesDataLabelPlacement", source.SeriesDataLabelPlacement?.ToString());
        writer.Property("seriesDataLabelStyle"); writer.StartObject(); DetailTextStyle(writer, source.SeriesDataLabelStyle); writer.EndObject();
        OptionalNumber(writer, "strokeWidth", source.StrokeWidth);
        writer.Property("waterfallItems"); writer.StartArray();
        foreach (var item in source.Items) {
            writer.StartObject(); Number(writer, "x", item.X); String(writer, "kind", item.Kind.ToString()); OptionalNumber(writer, "value", item.Value); writer.EndObject();
        }
        writer.EndArray(); writer.Property("waterfallDeltas"); writer.StartArray();
        foreach (var point in source.DeltaPoints) {
            writer.StartObject(); Number(writer, "x", point.X); Number(writer, "y", point.Y);
            Boolean(writer, "breakBefore", point.BreakBefore); OptionalNumber(writer, "colorValue", point.ColorValue); writer.EndObject();
        }
        writer.EndArray(); writer.Property("waterfallPointPresentation"); writer.StartArray();
        foreach (var point in source.PointPresentation.OrderBy(point => point.Index)) {
            writer.StartObject(); Number(writer, "index", point.Index); OptionalString(writer, "observationId", point.ObservationId);
            OptionalString(writer, "color", WaterfallColor(point.Color)); OptionalString(writer, "fillPattern", point.FillPattern?.ToString());
            OptionalString(writer, "label", point.Label); DetailTextStyle(writer, point.TextStyle); writer.EndObject();
        }
        writer.EndArray(); WaterfallPresentation(writer, source); writer.EndObject();
    }

    private static VisualArtifactInterchangeWaterfallArtifact? ReadWaterfall(Dictionary<string, GeoJsonValue> root) {
        var fields = OptionalObject(root, "waterfall"); if (fields == null) return null;
        var source = new VisualArtifactInterchangeWaterfallArtifact {
            TypedItems = OptionalBool(fields, "typedItems") ?? true, SeriesId = RequiredString(fields, "seriesId"),
            AutomaticSeriesId = OptionalBool(fields, "automaticSeriesId") ?? false, SeriesName = RequiredStringAllowEmpty(fields, "seriesName"),
            ValueAxis = OptionalEnum<ChartAxisSide>(fields, "valueAxis") ?? ChartAxisSide.Primary,
            StateRole = OptionalEnum<ChartSeriesState>(fields, "stateRole") ?? ChartSeriesState.None,
            Color = ReadWaterfallColor(fields, "color"), FillPattern = OptionalEnum<ChartFillPattern>(fields, "fillPattern") ?? ChartFillPattern.None,
            ShowInLegend = OptionalBool(fields, "showInLegend") ?? true, SemanticRole = OptionalString(fields, "semanticRole"),
            SeriesShowDataLabels = OptionalBool(fields, "seriesShowDataLabels"),
            SeriesDataLabelPlacement = OptionalEnum<ChartDataLabelPlacement>(fields, "seriesDataLabelPlacement"), StrokeWidth = OptionalNumber(fields, "strokeWidth")
        };
        var seriesStyle = OptionalObject(fields, "seriesDataLabelStyle");
        if (seriesStyle != null) source.SeriesDataLabelStyle = ReadDetailTextStyle(seriesStyle) ?? new TextStyleOverride();
        foreach (var value in OptionalArray(fields, "waterfallItems")) {
            var item = value.AsObject("waterfall item"); var x = RequiredNumber(item, "x");
            var kind = RequiredEnum<ChartWaterfallItemKind>(item, "kind"); var amount = OptionalNumber(item, "value");
            source.Items.Add(kind switch {
                ChartWaterfallItemKind.OpeningBalance => ChartWaterfallItem.OpeningBalance(x, amount ?? throw new ArgumentException("Opening balances require an authored value.")),
                ChartWaterfallItemKind.Delta => ChartWaterfallItem.Delta(x, amount ?? throw new ArgumentException("Deltas require an authored value.")),
                ChartWaterfallItemKind.Subtotal when !amount.HasValue => ChartWaterfallItem.Subtotal(x),
                ChartWaterfallItemKind.Total when !amount.HasValue => ChartWaterfallItem.Total(x),
                _ => throw new ArgumentException("Calculated checkpoints cannot carry an authored value.")
            });
        }
        foreach (var value in OptionalArray(fields, "waterfallDeltas")) {
            var point = value.AsObject("waterfall delta"); source.DeltaPoints.Add(new ChartPoint(RequiredNumber(point, "x"), RequiredNumber(point, "y"),
                OptionalBool(point, "breakBefore") ?? false).WithColorValue(OptionalNumber(point, "colorValue")));
        }
        foreach (var value in OptionalArray(fields, "waterfallPointPresentation")) {
            var point = value.AsObject("waterfall point presentation"); source.PointPresentation.Add(new VisualArtifactInterchangeWaterfallPoint {
                Index = RequiredInt(point, "index"), ObservationId = OptionalString(point, "observationId"), Color = ReadWaterfallColor(point, "color"),
                FillPattern = OptionalEnum<ChartFillPattern>(point, "fillPattern"), Label = OptionalString(point, "label"), TextStyle = ReadDetailTextStyle(point)
            });
        }
        ReadWaterfallPresentation(fields, source); return source;
    }

    private static string? WaterfallColor(ChartColor? color) => color is { } value ? value.A == 255 ? value.ToHex() : value.ToHexRgba() : null;
    private static ChartColor? ReadWaterfallColor(Dictionary<string, GeoJsonValue> fields, string name) =>
        OptionalString(fields, name) is string text ? ChartColor.Parse(text) : null;
}
