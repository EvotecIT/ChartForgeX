using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void FrameRequest(VisualArtifactInterchangeJsonWriter writer, VisualFrame frame, Action<string,TextStyle?> text) {
        writer.Property("frame"); writer.StartObject(); OptionalString(writer, "title", frame.Title); OptionalString(writer, "subtitle", frame.Subtitle);
        Boolean(writer, "explicitLegend", frame.HasExplicitLegend); Boolean(writer, "showLegend", frame.ShowLegend);
        Boolean(writer, "explicitLegendPosition", frame.HasExplicitLegendPosition); String(writer, "legendPosition", frame.LegendPosition.ToString());
        Boolean(writer, "surface", frame.ShowSurface); Boolean(writer, "transparent", frame.TransparentBackground); Boolean(writer, "card", frame.ShowCard);
        OptionalNumber(writer, "legendRows", frame.LegendMaximumRows); Number(writer, "legendHeight", frame.LegendMaximumHeightFraction); OptionalString(writer, "legendTitle", frame.LegendTitle);
        text("titleStyle", frame.TitleStyle); text("subtitleStyle", frame.SubtitleStyle); text("legendStyle", frame.LegendStyle);
        writer.EndObject();
    }
    private static void FrameText(VisualArtifactInterchangeJsonWriter writer,string name,TextStyle? style,Action<FontSpec> font) {
        if (style == null) return;
        writer.Property(name); writer.StartObject(); font(style.Font); Number(writer, "size", style.FontSize);
        String(writer, "color", style.Color.ToHexRgba()); String(writer, "alignment", style.Alignment.ToString()); Number(writer, "lineHeight", style.LineHeight);
        String(writer, "underline", style.UnderlineStyle.ToString()); String(writer, "strike", style.StrikethroughStyle.ToString()); String(writer, "baseline", style.Baseline.ToString());
        String(writer, "case", style.TextCase.ToString()); String(writer, "hinting", style.Hinting.ToString()); OptionalString(writer, "language", style.OpenTypeLanguageTag); writer.EndObject();
    }
    private static VisualFrame? ReadFrameRequest(Dictionary<string,GeoJsonValue> owner, OwnedFontRootReader? roots = null) {
        var frame=OptionalObject(owner,"frame");if(frame==null)return null;
        var explicitLegend=Flag("explicitLegend");var legend=Flag("showLegend");var explicitPosition=Flag("explicitLegendPosition");var position=RequiredEnum<ChartLegendPosition>(frame,"legendPosition");
        if(!explicitLegend&&!legend||!explicitPosition&&position!=ChartLegendPosition.TopLeft)throw new ArgumentException("Implicit frame legend values must match the native defaults.");
        return new VisualFrame(OptionalString(frame,"title"),OptionalString(frame,"subtitle"),explicitLegend?legend:null,explicitPosition?position:null,
            Flag("surface"),Flag("transparent"),Text("titleStyle"),Text("subtitleStyle"),Text("legendStyle"),OptionalInt(frame,"legendRows"),RequiredNumber(frame,"legendHeight"),Flag("card"),OptionalString(frame,"legendTitle"));
        bool Flag(string name)=>OptionalBool(frame,name)??throw new ArgumentException("Frame request requires boolean "+name+".");
        TextStyle? Text(string name) {
            var item=OptionalObject(frame,name);if(item==null)return null;
            return new TextStyle{Font=roots == null ? ReadFiniteFontValue(item,"font") : ReadFontValue(item,"font",roots),FontSize=RequiredNumber(item,"size"),Color=ReadWaterfallColor(item,"color")??throw new ArgumentException("Frame text requires literal color."),
                Alignment=RequiredEnum<TextAlignment>(item,"alignment"),LineHeight=RequiredNumber(item,"lineHeight"),UnderlineStyle=RequiredEnum<TextDecorationStyle>(item,"underline"),
                StrikethroughStyle=RequiredEnum<TextDecorationStyle>(item,"strike"),Baseline=RequiredEnum<TextBaseline>(item,"baseline"),TextCase=RequiredEnum<TextCaseTransform>(item,"case"),
                Hinting=RequiredEnum<TextHinting>(item,"hinting"),OpenTypeLanguageTag=OptionalString(item,"language")};
        }
    }
}
