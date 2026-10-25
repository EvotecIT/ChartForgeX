using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void WaterfallPresentation(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeWaterfallArtifact source) {
        String(writer, "themeJson", source.Theme.ToJson());
        Number(writer, "paddingLeft", source.Padding.Left); Number(writer, "paddingTop", source.Padding.Top);
        Number(writer, "paddingRight", source.Padding.Right); Number(writer, "paddingBottom", source.Padding.Bottom);
        String(writer, "orientation", source.Orientation.ToString());
        String(writer, "themeMode", source.ThemeMode.ToString());
        Boolean(writer, "explicitPadding", source.ExplicitPadding);
        Boolean(writer, "hostOwnsFrame", source.HostOwnsFrame);
        Boolean(writer, "showHeader", source.ShowHeader);
        Boolean(writer, "showCard", source.ShowCard);
        Boolean(writer, "showPlotBackground", source.ShowPlotBackground);
        Boolean(writer, "transparentBackground", source.TransparentBackground);
        Boolean(writer, "pinStateColorsInForcedColors", source.PinStateColorsInForcedColors);
        if (source.ShowLegend.HasValue) Boolean(writer, "showLegend", source.ShowLegend.Value);
        OptionalString(writer, "legendPosition", source.LegendPosition?.ToString());
        Boolean(writer, "showPointLegend", source.ShowPointLegend);
        OptionalNumber(writer, "legendMaximumRows", source.LegendMaximumRows);
        Number(writer, "legendMaximumHeightFraction", source.LegendMaximumHeightFraction);
        Boolean(writer, "showAxes", source.ShowAxes);
        Boolean(writer, "showGrid", source.ShowGrid);
        Boolean(writer, "clipMarksToPlot", source.ClipMarksToPlot);
        if (source.ShowDataLabels.HasValue) Boolean(writer, "showDataLabels", source.ShowDataLabels.Value);
        String(writer, "dataLabelPlacement", source.DataLabelPlacement.ToString());
        Boolean(writer, "explicitBarCornerRadius", source.ExplicitBarCornerRadius);
        Boolean(writer, "explicitGridStrokeWidth", source.ExplicitGridStrokeWidth);
        String(writer, "xAxisTitle", source.XAxisTitle);
        String(writer, "yAxisTitle", source.YAxisTitle);
        String(writer, "secondaryXAxisTitle", source.SecondaryXAxisTitle);
        String(writer, "secondaryYAxisTitle", source.SecondaryYAxisTitle);
        Number(writer, "pngOutputScale", source.PngOutputScale);
        Number(writer, "pngSupersamplingScale", source.PngSupersamplingScale);
        String(writer, "pngTextHinting", source.PngTextHinting.ToString());
        String(writer, "dataLabelConnectorStyle", source.DataLabelConnectorStyle.ToString());
        Number(writer, "dataLabelConnectorOpacity", source.DataLabelConnectorOpacity);
        Number(writer, "dataLabelConnectorStrokeWidth", source.DataLabelConnectorStrokeWidth);
        OptionalString(writer, "dataLabelConnectorColor", WaterfallColor(source.DataLabelConnectorColor));
        WaterfallValueFormat(writer, "valueFormat", source.ValueFormat);
        WaterfallAxis(writer, "xAxis", source.XAxis); WaterfallAxis(writer, "yAxis", source.YAxis);
        WaterfallAxis(writer, "secondaryXAxis", source.SecondaryXAxis); WaterfallAxis(writer, "secondaryYAxis", source.SecondaryYAxis);
        WaterfallBars(writer, source.BarStyle, source.ExplicitBarCornerRadius); WaterfallGrid(writer, source.GridStyle, source.ExplicitGridStrokeWidth);
        WaterfallWords(writer, source.Labels);
        writer.Property("waterfallTextStyles"); writer.StartArray();
        foreach (var pair in source.TextStyles.OrderBy(pair => pair.Key)) {
            writer.StartObject(); String(writer, "role", pair.Key.ToString()); DetailTextStyle(writer, pair.Value); writer.EndObject();
        }
        writer.EndArray();
    }

    private static void ReadWaterfallPresentation(Dictionary<string, GeoJsonValue> fields, VisualArtifactInterchangeWaterfallArtifact source) {
        if (OptionalString(fields, "themeJson") is string theme) source.Theme = VisualTheme.FromJson(theme);
        source.Padding = new ChartPadding(OptionalNumber(fields, "paddingLeft") ?? 24, OptionalNumber(fields, "paddingTop") ?? 24,
            OptionalNumber(fields, "paddingRight") ?? 24, OptionalNumber(fields, "paddingBottom") ?? 24);
        source.Orientation = OptionalEnum<ChartOrientation>(fields, "orientation") ?? ChartOrientation.Vertical;
        source.ThemeMode = OptionalEnum<VisualThemeMode>(fields, "themeMode") ?? VisualThemeMode.Light;
        source.ExplicitPadding = OptionalBool(fields, "explicitPadding") ?? false;
        source.HostOwnsFrame = OptionalBool(fields, "hostOwnsFrame") ?? false;
        source.ShowHeader = OptionalBool(fields, "showHeader") ?? true;
        source.ShowCard = OptionalBool(fields, "showCard") ?? true;
        source.ShowPlotBackground = OptionalBool(fields, "showPlotBackground") ?? false;
        source.TransparentBackground = OptionalBool(fields, "transparentBackground") ?? true;
        source.PinStateColorsInForcedColors = OptionalBool(fields, "pinStateColorsInForcedColors") ?? false;
        source.ShowLegend = OptionalBool(fields, "showLegend");
        source.LegendPosition = OptionalEnum<ChartLegendPosition>(fields, "legendPosition");
        source.ShowPointLegend = OptionalBool(fields, "showPointLegend") ?? false;
        source.LegendMaximumRows = OptionalInt(fields, "legendMaximumRows");
        source.LegendMaximumHeightFraction = OptionalNumber(fields, "legendMaximumHeightFraction") ?? 0.35;
        source.ShowAxes = OptionalBool(fields, "showAxes") ?? true;
        source.ShowGrid = OptionalBool(fields, "showGrid") ?? true;
        source.ClipMarksToPlot = OptionalBool(fields, "clipMarksToPlot") ?? true;
        source.ShowDataLabels = OptionalBool(fields, "showDataLabels");
        source.DataLabelPlacement = OptionalEnum<ChartDataLabelPlacement>(fields, "dataLabelPlacement") ?? ChartDataLabelPlacement.Auto;
        source.ExplicitBarCornerRadius = OptionalBool(fields, "explicitBarCornerRadius") ?? false;
        source.ExplicitGridStrokeWidth = OptionalBool(fields, "explicitGridStrokeWidth") ?? false;
        source.XAxisTitle = OptionalString(fields, "xAxisTitle") ?? string.Empty;
        source.YAxisTitle = OptionalString(fields, "yAxisTitle") ?? string.Empty;
        source.SecondaryXAxisTitle = OptionalString(fields, "secondaryXAxisTitle") ?? string.Empty;
        source.SecondaryYAxisTitle = OptionalString(fields, "secondaryYAxisTitle") ?? string.Empty;
        source.PngOutputScale = OptionalInt(fields, "pngOutputScale") ?? 1;
        source.PngSupersamplingScale = OptionalInt(fields, "pngSupersamplingScale") ?? 2;
        source.PngTextHinting = OptionalEnum<TextHinting>(fields, "pngTextHinting") ?? TextHinting.Auto;
        source.DataLabelConnectorStyle = OptionalEnum<ChartDataLabelConnectorStyle>(fields, "dataLabelConnectorStyle") ?? ChartDataLabelConnectorStyle.Elbow;
        source.DataLabelConnectorOpacity = OptionalNumber(fields, "dataLabelConnectorOpacity") ?? 0.55;
        source.DataLabelConnectorStrokeWidth = OptionalNumber(fields, "dataLabelConnectorStrokeWidth") ?? 1.2;
        source.DataLabelConnectorColor = ReadWaterfallColor(fields, "dataLabelConnectorColor");
        source.ValueFormat = ReadWaterfallValueFormat(fields, "valueFormat") ?? ChartValueFormat.ExistingValue;
        source.XAxis = ReadWaterfallAxis(fields, "xAxis"); source.YAxis = ReadWaterfallAxis(fields, "yAxis");
        source.SecondaryXAxis = ReadWaterfallAxis(fields, "secondaryXAxis"); source.SecondaryYAxis = ReadWaterfallAxis(fields, "secondaryYAxis");
        source.BarStyle = ReadWaterfallBars(fields, source.ExplicitBarCornerRadius); source.GridStyle = ReadWaterfallGrid(fields, source.ExplicitGridStrokeWidth);
        source.Labels = ReadWaterfallWords(fields);
        foreach (var value in OptionalArray(fields, "waterfallTextStyles")) {
            var style = value.AsObject("waterfall text role"); var role = RequiredEnum<ChartTextRole>(style, "role");
            if (source.TextStyles.ContainsKey(role)) throw new ArgumentException("Duplicate Waterfall text role.");
            source.TextStyles.Add(role, ReadDetailTextStyle(style) ?? new TextStyleOverride());
        }
    }

    private static void WaterfallValueFormat(VisualArtifactInterchangeJsonWriter writer, string name, ChartValueFormat? format) {
        if (format == null) return; var policy = format.PortablePolicy();
        writer.Property(name); writer.StartObject(); String(writer, "notation", policy.Notation);
        String(writer, "format", policy.Format); String(writer, "culture", policy.Culture); writer.EndObject();
    }

    private static ChartValueFormat? ReadWaterfallValueFormat(Dictionary<string, GeoJsonValue> fields, string name) {
        var format = OptionalObject(fields, name); if (format == null) return null;
        return ChartValueFormat.FromPortablePolicy(RequiredString(format, "notation"), RequiredStringAllowEmpty(format, "format"), RequiredStringAllowEmpty(format, "culture"));
    }
}
