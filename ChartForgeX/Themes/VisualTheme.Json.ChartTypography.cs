using System.Globalization;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Themes;

public sealed partial class VisualTheme {
    private static void WriteChartTypography(VisualArtifactInterchangeJsonWriter writer, VisualTypography scale) {
        var wide = scale.WideScale ?? scale;
        var compact = wide.CompactScale;
        if (compact == null) return;
        writer.Property("chart"); writer.StartObject();
        writer.Property("resolvedSize"); writer.String(scale.WideScale != null ? "compact" : "wide");
        void Value(string name, double value, string unit = "px") { writer.Property(name); writer.String(value.ToString("G17", CultureInfo.InvariantCulture) + unit); }
        void Role(string name, double size, double compactSize, int weight, bool values = false) {
            writer.Property(name); writer.StartObject();
            Value("size", size); Value("compactSize", compactSize); Value("weight", weight, "");
            if (values) Value("valueWeight", wide.LegendValueWeight, "");
            writer.EndObject();
        }
        Value("minimumSize", wide.MinimumTextSize); Value("compactBreakpoint", wide.CompactBreakpoint);
        Role("title", wide.TitleSize, compact.TitleSize, wide.TitleWeight);
        Role("subtitle", wide.SubtitleSize, compact.SubtitleSize, wide.SubtitleWeight);
        Role("panelTitle", wide.TileLabelSize, compact.TileLabelSize, wide.PanelTitleWeight);
        Role("axisTitle", wide.AxisTitleSize, compact.AxisTitleSize, wide.AxisTitleWeight);
        Role("tick", wide.AxisSize, compact.AxisSize, wide.TickWeight);
        Role("legend", wide.LegendSize, compact.LegendSize, wide.LegendWeight, true);
        Role("dataLabel", wide.DataLabelSize, compact.DataLabelSize, wide.DataLabelWeight);
        writer.Property("centerValue"); writer.StartObject();
        Value("minimumSize", wide.CenterMinimumSize); Value("maximumSize", wide.CenterMaximumSize);
        Value("radiusFactor", wide.CenterRadiusFactor, ""); Value("weight", wide.CenterValueWeight, "");
        Value("captionSize", wide.CenterCaptionSize); Value("captionGap", wide.CenterCaptionGap);
        writer.EndObject(); writer.EndObject();
    }
}
