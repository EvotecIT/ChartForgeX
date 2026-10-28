using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    // Axis input uses native setters and preserves optional/explicit policies; no callback is evaluated.
    private static void WaterfallAxis(VisualArtifactInterchangeJsonWriter writer, string name, ChartAxis axis) {
        writer.Property(name); writer.StartObject();
        if (axis.HasExplicitScale) String(writer, "scale", axis.Scale.ToString());
        Number(writer, "logarithmBase", axis.LogarithmBase); String(writer, "logarithmicDomain", axis.LogarithmicDomain.ToString());
        Number(writer, "symmetricLogarithmThreshold", axis.SymmetricLogarithmThreshold);
        OptionalNumber(writer, "minimum", axis.Minimum); OptionalNumber(writer, "maximum", axis.Maximum);
        if (axis.HasExplicitTickCount) Number(writer, "tickCount", axis.TickCount);
        Boolean(writer, "integerTicks", axis.IntegerTicks); String(writer, "labelDensity", axis.LabelDensity.ToString());
        if (axis.HasExplicitLabelAngle) Number(writer, "labelAngle", axis.LabelAngle);
        String(writer, "labelOrientation", axis.LabelOrientation.ToString());
        Boolean(writer, "visible", axis.Visible); Boolean(writer, "reversed", axis.Reversed);
        if (axis.HasExplicitLine) Boolean(writer, "showLine", axis.ShowLine);
        OptionalNumber(writer, "ruleCrossAt", axis.RuleCrossAt); OptionalNumber(writer, "crossAt", axis.CrossAt);
        WaterfallValueFormat(writer, "valueFormat", axis.ValueFormat);
        OptionalString(writer, "timeZoneLabel", axis.TimeZoneLabel); Boolean(writer, "showTimeZone", axis.ShowTimeZone);
        writer.Property("waterfallAxisLabels"); writer.StartArray();
        foreach (var label in axis.Labels) {
            writer.StartObject(); Number(writer, "value", label.Value); String(writer, "text", label.Text);
            writer.Property("waterfallCategoryGroups"); writer.StartArray(); foreach (var group in label.Groups) writer.String(group);
            writer.EndArray(); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();
    }

    private static ChartAxis ReadWaterfallAxis(Dictionary<string, GeoJsonValue> source, string name) {
        var fields = OptionalObject(source, name); if (fields == null) return new ChartAxis();
        var axis = new ChartAxis {
            LogarithmBase = OptionalNumber(fields, "logarithmBase") ?? 10,
            LogarithmicDomain = OptionalEnum<ChartLogarithmicDomain>(fields, "logarithmicDomain") ?? ChartLogarithmicDomain.Positive,
            SymmetricLogarithmThreshold = OptionalNumber(fields, "symmetricLogarithmThreshold") ?? 1,
            IntegerTicks = OptionalBool(fields, "integerTicks") ?? false,
            LabelDensity = OptionalEnum<ChartLabelDensity>(fields, "labelDensity") ?? ChartLabelDensity.Auto,
            LabelOrientation = OptionalEnum<ChartAxisLabelOrientation>(fields, "labelOrientation") ?? ChartAxisLabelOrientation.Fixed,
            Visible = OptionalBool(fields, "visible") ?? true, Reversed = OptionalBool(fields, "reversed") ?? false,
            ValueFormat = ReadWaterfallValueFormat(fields, "valueFormat"),
            TimeZoneLabel = OptionalString(fields, "timeZoneLabel"), ShowTimeZone = OptionalBool(fields, "showTimeZone") ?? false,
            RuleCrossAt = OptionalNumber(fields, "ruleCrossAt"), CrossAt = OptionalNumber(fields, "crossAt")
        };
        if (OptionalEnum<ChartScaleKind>(fields, "scale") is ChartScaleKind scale) axis.Scale = scale;
        axis.Minimum = OptionalNumber(fields, "minimum"); axis.Maximum = OptionalNumber(fields, "maximum");
        if (OptionalInt(fields, "tickCount") is int ticks) axis.TickCount = ticks;
        if (OptionalNumber(fields, "labelAngle") is double angle) axis.LabelAngle = angle;
        if (OptionalBool(fields, "showLine") is bool line) axis.ShowLine = line;
        foreach (var value in OptionalArray(fields, "waterfallAxisLabels")) {
            var label = value.AsObject("waterfall axis label");
            var groups = OptionalArray(label, "waterfallCategoryGroups").Select(group => group.AsString("waterfall category group")).ToArray();
            axis.Labels.Add(new ChartAxisLabel(RequiredNumber(label, "value"), RequiredStringAllowEmpty(label, "text"), groups));
        }
        return axis;
    }
}
