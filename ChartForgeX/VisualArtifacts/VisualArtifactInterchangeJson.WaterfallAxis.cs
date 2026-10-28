using System;
using System.Collections.Generic;
using System.Globalization;
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
        if (axis.Culture != null) String(writer, "culture", PortableTimeCulture(axis.Culture));
        writer.Property("waterfallAxisLabels"); writer.StartArray();
        foreach (var label in axis.Labels) {
            writer.StartObject(); Number(writer, "value", label.Value); String(writer, "text", label.Text);
            writer.Property("waterfallCategoryGroups"); writer.StartArray(); foreach (var group in label.Groups) writer.String(group);
            writer.EndArray(); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();
    }

    // Portable axes retain named, unmodified cultures; silently dropping custom date patterns would change captions.
    private static string PortableTimeCulture(CultureInfo culture) {
        if (culture.GetType() != typeof(CultureInfo)) throw new NotSupportedException("Portable time axes require standard named cultures.");
        var reference = CultureInfo.GetCultureInfo(culture.Name);
        var actual = culture.DateTimeFormat; var expected = reference.DateTimeFormat;
        if (culture.NumberFormat.NumberDecimalSeparator != reference.NumberFormat.NumberDecimalSeparator
            || actual.ShortDatePattern != expected.ShortDatePattern || actual.MonthDayPattern != expected.MonthDayPattern
            || actual.ShortTimePattern != expected.ShortTimePattern || actual.LongTimePattern != expected.LongTimePattern
            || actual.DateSeparator != expected.DateSeparator || actual.TimeSeparator != expected.TimeSeparator
            || actual.AMDesignator != expected.AMDesignator || actual.PMDesignator != expected.PMDesignator
            || actual.Calendar.GetType() != expected.Calendar.GetType() || actual.Calendar.TwoDigitYearMax != expected.Calendar.TwoDigitYearMax
            || !actual.MonthNames.SequenceEqual(expected.MonthNames) || !actual.AbbreviatedMonthNames.SequenceEqual(expected.AbbreviatedMonthNames)
            || !actual.MonthGenitiveNames.SequenceEqual(expected.MonthGenitiveNames) || !actual.AbbreviatedMonthGenitiveNames.SequenceEqual(expected.AbbreviatedMonthGenitiveNames)
            || !actual.DayNames.SequenceEqual(expected.DayNames) || !actual.AbbreviatedDayNames.SequenceEqual(expected.AbbreviatedDayNames))
            throw new NotSupportedException("Modified time cultures cannot be serialized as portable chart data.");
        return culture.Name;
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
        if (OptionalString(fields, "culture") is string culture) axis.Culture = CultureInfo.GetCultureInfo(culture);
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
