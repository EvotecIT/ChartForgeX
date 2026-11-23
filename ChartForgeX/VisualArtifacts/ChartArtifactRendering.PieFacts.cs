using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

public static partial class ChartArtifactRendering {
    /// <summary>Describes optional native radial policies beside rendered Chart fallback; these facts are not an editable Chart schema.</summary>
    private static void WritePiePresentationFacts(Chart chart, IDictionary<string, string> target) {
        if (!chart.Series.Any(series => ChartSeriesKindTraits.IsPieLikeKind(series.Kind))) return;
        if (chart.Options.Pie.SliceOpacity.HasValue)
            target["chart.radial.pie.sliceOpacity"] = chart.Options.Pie.SliceOpacity.Value.ToString("R", CultureInfo.InvariantCulture);
        if (chart.Options.Pie.InsideLabelRadiusRatio.HasValue)
            target["chart.radial.pie.labelRadiusRatio"] = chart.Options.Pie.InsideLabelRadiusRatio.Value.ToString("R", CultureInfo.InvariantCulture);
        if (chart.Options.Pie.PreserveSourceOrder) target["chart.radial.pie.sliceOrder"] = "source";
        if (chart.Options.Pie.LegendContent.HasValue) target["chart.radial.pie.legendContent"] = chart.Options.Pie.LegendContent.Value.ToString();
        if (chart.Options.Pie.SliceOutline != null) target["chart.radial.pie.sliceOutline"] = StrokeFact(chart.Options.Pie.SliceOutline);
        if (chart.Options.Pie.OuterOutline != null) target["chart.radial.pie.outerOutline"] = StrokeFact(chart.Options.Pie.OuterOutline);
    }

    private static string StrokeFact(ChartStrokeStyle style) {
        var writer = new VisualArtifactInterchangeJsonWriter(); writer.StartObject();
        writer.Property("color"); writer.String(style.Color?.ToCss());
        writer.Property("width"); if (style.Width.HasValue) writer.Number(style.Width.Value); else writer.String(null);
        writer.Property("opacity"); writer.Number(style.Opacity);
        writer.Property("dash");
        if (style.Dash == null) writer.String(null);
        else { writer.StartArray(); foreach (var value in style.Dash) writer.Number(value); writer.EndArray(); }
        writer.EndObject(); return writer.ToString();
    }
}
