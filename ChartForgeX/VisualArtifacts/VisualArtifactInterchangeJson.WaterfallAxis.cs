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
        if (axis.Culture != null) {
            String(writer, "culture", PortableTimeCulture(axis.Culture));
            WriteCultureFormat(writer, axis.Culture);
        }
        writer.Property("waterfallAxisLabels"); writer.StartArray();
        foreach (var label in axis.Labels) {
            writer.StartObject(); Number(writer, "value", label.Value); String(writer, "text", label.Text);
            writer.Property("waterfallCategoryGroups"); writer.StartArray(); foreach (var group in label.Groups) writer.String(group);
            writer.EndArray(); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();
    }

    // Portable axes carry the culture by name, plus every date and time setting that differs from the standard named culture
    // (Windows user overrides or authored patterns), so a restored view formats time labels exactly as the original did.
    // The invariant culture travels as its empty name, so an explicit invariant axis stays explicit.
    private static string PortableTimeCulture(CultureInfo culture) => culture.Name;

    private static readonly string[] CulturePatternNames = {
        "shortDatePattern", "longTimePattern", "shortTimePattern", "monthDayPattern", "dateSeparator", "timeSeparator", "amDesignator", "pmDesignator", "numberDecimalSeparator"
    };
    private static readonly string[] CultureNameListNames = { "monthNames", "abbreviatedMonthNames", "monthGenitiveNames", "abbreviatedMonthGenitiveNames" };

    private static string CultureSetting(CultureInfo culture, string name) => name switch {
        "shortDatePattern" => culture.DateTimeFormat.ShortDatePattern, "longTimePattern" => culture.DateTimeFormat.LongTimePattern,
        "shortTimePattern" => culture.DateTimeFormat.ShortTimePattern, "monthDayPattern" => culture.DateTimeFormat.MonthDayPattern,
        "dateSeparator" => culture.DateTimeFormat.DateSeparator, "timeSeparator" => culture.DateTimeFormat.TimeSeparator,
        "amDesignator" => culture.DateTimeFormat.AMDesignator, "pmDesignator" => culture.DateTimeFormat.PMDesignator,
        "numberDecimalSeparator" => culture.NumberFormat.NumberDecimalSeparator, _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static string[] CultureNameList(CultureInfo culture, string name) => name switch {
        "monthNames" => culture.DateTimeFormat.MonthNames, "abbreviatedMonthNames" => culture.DateTimeFormat.AbbreviatedMonthNames,
        "monthGenitiveNames" => culture.DateTimeFormat.MonthGenitiveNames, "abbreviatedMonthGenitiveNames" => culture.DateTimeFormat.AbbreviatedMonthGenitiveNames,
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static void WriteCultureFormat(VisualArtifactInterchangeJsonWriter writer, CultureInfo culture) {
        var standard = CultureInfo.GetCultureInfo(culture.Name);
        var settings = CulturePatternNames.Where(name => CultureSetting(culture, name) != CultureSetting(standard, name)).ToArray();
        var lists = CultureNameListNames.Where(name => !CultureNameList(culture, name).SequenceEqual(CultureNameList(standard, name))).ToArray();
        if (settings.Length == 0 && lists.Length == 0) return;
        writer.Property("cultureFormat"); writer.StartObject();
        foreach (var name in settings) String(writer, name, CultureSetting(culture, name));
        foreach (var name in lists) {
            writer.Property(name); writer.StartArray(); foreach (var item in CultureNameList(culture, name)) writer.String(item); writer.EndArray();
        }
        writer.EndObject();
    }

    private static CultureInfo ReadCulture(Dictionary<string, GeoJsonValue> fields, string name) {
        var standard = CultureInfo.GetCultureInfo(name);
        var format = OptionalObject(fields, "cultureFormat");
        if (format == null) return standard;
        var culture = (CultureInfo)standard.Clone(); var date = culture.DateTimeFormat;
        foreach (var setting in CulturePatternNames) {
            if (OptionalString(format, setting) is not string value) continue;
            switch (setting) {
                case "shortDatePattern": date.ShortDatePattern = value; break;
                case "longTimePattern": date.LongTimePattern = value; break;
                case "shortTimePattern": date.ShortTimePattern = value; break;
                case "monthDayPattern": date.MonthDayPattern = value; break;
                case "dateSeparator": date.DateSeparator = value; break;
                case "timeSeparator": date.TimeSeparator = value; break;
                case "amDesignator": date.AMDesignator = value; break;
                case "pmDesignator": date.PMDesignator = value; break;
                case "numberDecimalSeparator": culture.NumberFormat.NumberDecimalSeparator = value; break;
            }
        }
        foreach (var list in CultureNameListNames) {
            var values = OptionalArray(format, list).Select(item => item.AsString("culture " + list)).ToArray();
            if (values.Length == 0) continue;
            if (values.Length != 13) throw new ArgumentException("Culture " + list + " must contain 13 entries.");
            switch (list) {
                case "monthNames": date.MonthNames = values; break;
                case "abbreviatedMonthNames": date.AbbreviatedMonthNames = values; break;
                case "monthGenitiveNames": date.MonthGenitiveNames = values; break;
                case "abbreviatedMonthGenitiveNames": date.AbbreviatedMonthGenitiveNames = values; break;
            }
        }
        return CultureInfo.ReadOnly(culture);
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
        if (OptionalString(fields, "culture") is string culture) axis.Culture = ReadCulture(fields, culture);
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
