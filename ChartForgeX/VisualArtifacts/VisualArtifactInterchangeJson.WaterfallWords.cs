using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void WaterfallWords(VisualArtifactInterchangeJsonWriter writer, ChartLabels labels) {
        writer.Property("waterfallWords"); writer.StartObject();
        String(writer, "now", labels.Now);
        String(writer, "ongoing", labels.Ongoing);
        String(writer, "soFar", labels.SoFar);
        String(writer, "hourOfDay", labels.HourOfDay);
        String(writer, "level", labels.Level);
        String(writer, "less", labels.Less);
        String(writer, "more", labels.More);
        String(writer, "noData", labels.NoData);
        String(writer, "color", labels.Color);
        String(writer, "size", labels.Size);
        String(writer, "target", labels.Target);
        String(writer, "authoredValue", labels.AuthoredValue);
        String(writer, "remainder", labels.Remainder);
        String(writer, "allValues", labels.AllValues);
        String(writer, "value", labels.Value);
        String(writer, "change", labels.Change);
        String(writer, "category", labels.Category);
        String(writer, "start", labels.Start);
        String(writer, "end", labels.End);
        String(writer, "minimum", labels.Minimum);
        String(writer, "q1", labels.Q1);
        String(writer, "median", labels.Median);
        String(writer, "q3", labels.Q3);
        String(writer, "maximum", labels.Maximum);
        String(writer, "openingBalance", labels.OpeningBalance);
        String(writer, "subtotal", labels.Subtotal);
        String(writer, "total", labels.Total);
        String(writer, "untitledChart", labels.UntitledChart);
        String(writer, "lowerWhisker", labels.LowerWhisker);
        String(writer, "upperWhisker", labels.UpperWhisker);
        String(writer, "sampleCount", labels.SampleCount);
        String(writer, "sourceMinimum", labels.SourceMinimum);
        String(writer, "sourceMaximum", labels.SourceMaximum);
        String(writer, "inlierMinimum", labels.InlierMinimum);
        String(writer, "inlierMaximum", labels.InlierMaximum);
        String(writer, "outlierCount", labels.OutlierCount);
        String(writer, "quartileEstimator", labels.QuartileEstimator);
        String(writer, "whiskerPolicy", labels.WhiskerPolicy);
        String(writer, "iqrMultiplier", labels.IqrMultiplier);
        String(writer, "noInliers", labels.NoInliers);
        String(writer, "outlier", labels.Outlier);
        writer.EndObject();
    }

    private static ChartLabels ReadWaterfallWords(Dictionary<string, GeoJsonValue> fields) {
        var labels = new ChartLabels(); var values = OptionalObject(fields, "waterfallWords"); if (values == null) return labels;
        if (OptionalString(values, "now") is string now) labels.Now = now;
        if (OptionalString(values, "ongoing") is string ongoing) labels.Ongoing = ongoing;
        if (OptionalString(values, "soFar") is string soFar) labels.SoFar = soFar;
        if (OptionalString(values, "hourOfDay") is string hourOfDay) labels.HourOfDay = hourOfDay;
        if (OptionalString(values, "level") is string level) labels.Level = level;
        if (OptionalString(values, "less") is string less) labels.Less = less;
        if (OptionalString(values, "more") is string more) labels.More = more;
        if (OptionalString(values, "noData") is string noData) labels.NoData = noData;
        if (OptionalString(values, "color") is string color) labels.Color = color;
        if (OptionalString(values, "size") is string size) labels.Size = size;
        if (OptionalString(values, "target") is string target) labels.Target = target;
        if (OptionalString(values, "authoredValue") is string authoredValue) labels.AuthoredValue = authoredValue;
        if (OptionalString(values, "remainder") is string remainder) labels.Remainder = remainder;
        if (OptionalString(values, "allValues") is string allValues) labels.AllValues = allValues;
        if (OptionalString(values, "value") is string value) labels.Value = value;
        if (OptionalString(values, "change") is string change) labels.Change = change;
        if (OptionalString(values, "category") is string category) labels.Category = category;
        if (OptionalString(values, "start") is string start) labels.Start = start;
        if (OptionalString(values, "end") is string end) labels.End = end;
        if (OptionalString(values, "minimum") is string minimum) labels.Minimum = minimum;
        if (OptionalString(values, "q1") is string q1) labels.Q1 = q1;
        if (OptionalString(values, "median") is string median) labels.Median = median;
        if (OptionalString(values, "q3") is string q3) labels.Q3 = q3;
        if (OptionalString(values, "maximum") is string maximum) labels.Maximum = maximum;
        if (OptionalString(values, "openingBalance") is string openingBalance) labels.OpeningBalance = openingBalance;
        if (OptionalString(values, "subtotal") is string subtotal) labels.Subtotal = subtotal;
        if (OptionalString(values, "total") is string total) labels.Total = total;
        if (OptionalString(values, "untitledChart") is string untitledChart) labels.UntitledChart = untitledChart;
        if (OptionalString(values, "lowerWhisker") is string lowerWhisker) labels.LowerWhisker = lowerWhisker;
        if (OptionalString(values, "upperWhisker") is string upperWhisker) labels.UpperWhisker = upperWhisker;
        if (OptionalString(values, "sampleCount") is string sampleCount) labels.SampleCount = sampleCount;
        if (OptionalString(values, "sourceMinimum") is string sourceMinimum) labels.SourceMinimum = sourceMinimum;
        if (OptionalString(values, "sourceMaximum") is string sourceMaximum) labels.SourceMaximum = sourceMaximum;
        if (OptionalString(values, "inlierMinimum") is string inlierMinimum) labels.InlierMinimum = inlierMinimum;
        if (OptionalString(values, "inlierMaximum") is string inlierMaximum) labels.InlierMaximum = inlierMaximum;
        if (OptionalString(values, "outlierCount") is string outlierCount) labels.OutlierCount = outlierCount;
        if (OptionalString(values, "quartileEstimator") is string quartileEstimator) labels.QuartileEstimator = quartileEstimator;
        if (OptionalString(values, "whiskerPolicy") is string whiskerPolicy) labels.WhiskerPolicy = whiskerPolicy;
        if (OptionalString(values, "iqrMultiplier") is string iqrMultiplier) labels.IqrMultiplier = iqrMultiplier;
        if (OptionalString(values, "noInliers") is string noInliers) labels.NoInliers = noInliers;
        if (OptionalString(values, "outlier") is string outlier) labels.Outlier = outlier;
        return labels;
    }
}
