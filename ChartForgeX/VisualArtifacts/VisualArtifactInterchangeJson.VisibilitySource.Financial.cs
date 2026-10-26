using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    /// <summary>The same copied financial policy reconstructs native frames and binds canonical source identity.</summary>
    private static void VisibilitySourceFinancial(VisualArtifactInterchangeJsonWriter writer, ChartVisibilitySource.Series source, bool includeValues) {
        writer.Property("financialValues"); writer.StartArray();
        foreach (var item in source.FinancialValues) {
            writer.StartObject(); Number(writer, "x", item.X);
            if (includeValues) { Number(writer, "open", item.Open); Number(writer, "high", item.High); Number(writer, "low", item.Low); Number(writer, "close", item.Close); }
            writer.EndObject();
        }
        writer.EndArray(); var policy = source.CopyFinancial(); writer.Property("financial"); writer.StartObject();
        writer.Property("rising"); VisibilitySourceFinancialItem(writer, policy.Rising);
        writer.Property("falling"); VisibilitySourceFinancialItem(writer, policy.Falling); writer.EndObject();
        writer.Property("financialStyles"); writer.StartArray();
        foreach (var style in source.CopyFinancialStyles()) {
            if (style == null) writer.String(null); else VisibilitySourceFinancialItem(writer, style);
        }
        writer.EndArray();
    }
    private static void VisibilitySourceFinancialItem(VisualArtifactInterchangeJsonWriter writer, ChartFinancialItemOptions source) {
        writer.StartObject(); Boolean(writer, "configured", source.IsConfigured); Boolean(writer, "hasFill", source.HasFill);
        OptionalString(writer, "fill", WaterfallColor(source.Fill)); OptionalNumber(writer, "fillOpacity", source.FillOpacity);
        OptionalString(writer, "stroke", WaterfallColor(source.Stroke)); OptionalNumber(writer, "strokeOpacity", source.StrokeOpacity);
        OptionalNumber(writer, "strokeWidth", source.StrokeWidth); OptionalNumber(writer, "dashOffset", source.DashOffset);
        VisibilitySourceFinancialDash(writer, "dash", source.DashPattern);
        var wick = source.Wick; writer.Property("wick"); writer.StartObject(); Boolean(writer, "configured", wick.IsConfigured);
        OptionalString(writer, "stroke", WaterfallColor(wick.Stroke)); OptionalNumber(writer, "strokeOpacity", wick.StrokeOpacity);
        OptionalNumber(writer, "strokeWidth", wick.StrokeWidth); OptionalNumber(writer, "dashOffset", wick.DashOffset);
        VisibilitySourceFinancialDash(writer, "dash", wick.DashPattern); writer.EndObject(); writer.EndObject();
    }
    private static void VisibilitySourceFinancialDash(VisualArtifactInterchangeJsonWriter writer, string name, System.Collections.Generic.IReadOnlyList<double>? dash) {
        writer.Property(name); if (dash == null) { writer.String(null); return; }
        writer.StartArray(); foreach (var value in dash) writer.Number(value); writer.EndArray();
    }
}
