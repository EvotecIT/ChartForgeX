using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void WaterfallBars(VisualArtifactInterchangeJsonWriter writer, ChartBarVisualStyle style, bool explicitRadius) {
        writer.Property("barStyle"); writer.StartObject();
        String(writer, "kind", style.Kind.ToString());
        Number(writer, "bodyOpacity", style.BodyOpacity);
        Number(writer, "capOpacity", style.CapOpacity);
        Number(writer, "capThickness", style.CapThickness);
        Number(writer, "capInset", style.CapInset);
        Number(writer, "capShadowOpacity", style.CapShadowOpacity);
        Number(writer, "capShadowOffset", style.CapShadowOffset);
        Number(writer, "capShadowSpread", style.CapShadowSpread);
        Number(writer, "capHighlightOpacity", style.CapHighlightOpacity);
        if (explicitRadius || style.HasExplicitCornerRadius) Number(writer, "cornerRadius", style.CornerRadius);
        writer.EndObject();
    }

    private static ChartBarVisualStyle ReadWaterfallBars(Dictionary<string, GeoJsonValue> fields, bool explicitRadius) {
        var values = OptionalObject(fields, "barStyle"); var style = ChartBarVisualStyle.Flat(); if (values == null) return style;
        if (OptionalEnum<ChartBarStyle>(values, "kind") is ChartBarStyle kind) style.Kind = kind;
        if (OptionalNumber(values, "bodyOpacity") is double bodyOpacity) style.BodyOpacity = bodyOpacity;
        if (OptionalNumber(values, "capOpacity") is double capOpacity) style.CapOpacity = capOpacity;
        if (OptionalNumber(values, "capThickness") is double capThickness) style.CapThickness = capThickness;
        if (OptionalNumber(values, "capInset") is double capInset) style.CapInset = capInset;
        if (OptionalNumber(values, "capShadowOpacity") is double capShadowOpacity) style.CapShadowOpacity = capShadowOpacity;
        if (OptionalNumber(values, "capShadowOffset") is double capShadowOffset) style.CapShadowOffset = capShadowOffset;
        if (OptionalNumber(values, "capShadowSpread") is double capShadowSpread) style.CapShadowSpread = capShadowSpread;
        if (OptionalNumber(values, "capHighlightOpacity") is double capHighlightOpacity) style.CapHighlightOpacity = capHighlightOpacity;
        if (OptionalNumber(values, "cornerRadius") is double radius) style.CornerRadius = radius;
        else if (explicitRadius) style.CornerRadius = 0;
        return style;
    }

    private static void WaterfallGrid(VisualArtifactInterchangeJsonWriter writer, ChartGridLineStyle style, bool explicitWidth) {
        writer.Property("gridStyle"); writer.StartObject();
        Boolean(writer, "showHorizontalLines", style.ShowHorizontalLines);
        Boolean(writer, "showVerticalLines", style.ShowVerticalLines);
        Number(writer, "horizontalOpacity", style.HorizontalOpacity);
        Number(writer, "verticalOpacity", style.VerticalOpacity);
        Number(writer, "dash", style.Dash);
        Number(writer, "gap", style.Gap);
        if (explicitWidth || style.HasExplicitStrokeWidth) Number(writer, "strokeWidth", style.StrokeWidth);
        writer.EndObject();
    }

    private static ChartGridLineStyle ReadWaterfallGrid(Dictionary<string, GeoJsonValue> fields, bool explicitWidth) {
        var values = OptionalObject(fields, "gridStyle"); var style = ChartGridLineStyle.Default(); if (values == null) return style;
        if (OptionalBool(values, "showHorizontalLines") is bool showHorizontalLines) style.ShowHorizontalLines = showHorizontalLines;
        if (OptionalBool(values, "showVerticalLines") is bool showVerticalLines) style.ShowVerticalLines = showVerticalLines;
        if (OptionalNumber(values, "horizontalOpacity") is double horizontalOpacity) style.HorizontalOpacity = horizontalOpacity;
        if (OptionalNumber(values, "verticalOpacity") is double verticalOpacity) style.VerticalOpacity = verticalOpacity;
        if (OptionalNumber(values, "dash") is double dash) style.Dash = dash;
        if (OptionalNumber(values, "gap") is double gap) style.Gap = gap;
        if (OptionalNumber(values, "strokeWidth") is double width) style.StrokeWidth = width;
        else if (explicitWidth) style.StrokeWidth = 1;
        return style;
    }
}
