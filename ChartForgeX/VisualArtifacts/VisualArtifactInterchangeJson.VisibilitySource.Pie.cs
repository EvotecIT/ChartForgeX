using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void VisibilitySourcePie(VisualArtifactInterchangeJsonWriter writer, ChartVisibilitySource source, bool includeDerivedMembership = true) {
        var pie = source.Pie!; writer.Property("radial"); writer.StartObject();
        Number(writer, "maximumSlices", pie.Maximum); Number(writer, "hole", pie.Hole); Boolean(writer, "center", pie.Center);
        OptionalString(writer, "centerValue", pie.CenterValue); OptionalString(writer, "centerLabel", pie.CenterLabel);
        Number(writer, "outside", pie.Outside); String(writer, "content", pie.Content.ToString()); OptionalNumber(writer, "inside", pie.Inside);
        OptionalNumber(writer, "opacity", pie.Opacity); Boolean(writer, "sourceOrder", pie.PreserveOrder); OptionalString(writer, "legendContent", pie.LegendContent?.ToString());
        VisibilitySourceStroke(writer, "sliceOutline", pie.SliceOutline); VisibilitySourceStroke(writer, "outerOutline", pie.OuterOutline);
        if (pie.Bands != null) { Number(writer, "bandHole", pie.Bands.InnerRatio); Number(writer, "bandGap", pie.Bands.GapRatio); }
        if (includeDerivedMembership) {
        writer.Property("otherMembership"); writer.StartArray();
        foreach (var series in source.SeriesRows) { writer.StartArray(); writer.String(series.Id.Value); foreach (var index in source.RadialOtherMembers(series.Id.Value)) writer.Number(index); writer.EndArray(); }
        writer.EndArray();
        } writer.EndObject();
    }
    private static void VisibilitySourceStroke(VisualArtifactInterchangeJsonWriter writer, string name, ChartStrokeStyle? stroke) {
        if (stroke == null) return;
        writer.Property(name); writer.StartObject(); OptionalString(writer, "color", WaterfallColor(stroke.Color)); OptionalNumber(writer, "width", stroke.Width); Number(writer, "opacity", stroke.Opacity);
        if (stroke.Dash != null) { writer.Property("dash"); writer.StartArray(); foreach (var value in stroke.Dash) writer.Number(value); writer.EndArray(); }
        writer.EndObject();
    }
}
