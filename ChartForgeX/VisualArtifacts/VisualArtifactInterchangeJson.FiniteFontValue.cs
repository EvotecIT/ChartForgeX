using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    // The existing typed role codec owns the finite font-value vocabulary and axis limits.
    private static void FiniteFontValue(VisualArtifactInterchangeJsonWriter writer,FontSpec font,string name) {
        writer.Property(name);writer.StartObject();FontValueFields(writer,font);writer.EndObject();
    }
    private static void FontValueFields(VisualArtifactInterchangeJsonWriter writer,FontSpec font) {
        DetailTextStyle(writer,new TextStyleOverride{FontFamily=font.Family,FontWeight=font.Weight.ToString(CultureInfo.InvariantCulture),
            Italic=font.Italic,ColorPaletteIndex=font.ColorPaletteIndex,Variations=font.Variations},"policy");
        OptionalNumber(writer,"collection",font.CollectionIndex);OptionalString(writer,"face",font.FaceName);
    }
    private static FontSpec ReadFiniteFontValue(Dictionary<string,GeoJsonValue> owner,string name) {
        var item=OptionalObject(owner,name)??throw new ArgumentException("A finite font value is required.");
        foreach(var key in item.Keys)if(key is not("policy" or "collection" or "face"))throw new ArgumentException("A finite font value cannot introduce file/resource bindings.");
        return ReadFontValueFields(item);
    }
    private static FontSpec ReadFontValueFields(Dictionary<string,GeoJsonValue> item) {
        var fields=OptionalObject(item,"policy")??throw new ArgumentException("A finite font policy is required.");
        foreach(var key in fields.Keys)if(key is not("fontFamily" or "fontWeight" or "italic" or "colorPaletteIndex" or "variations"))throw new ArgumentException("A font value cannot introduce text-role fields.");
        var value=ReadDetailTextStyle(item,"policy")!;
        if(!int.TryParse(value.FontWeight,NumberStyles.None,CultureInfo.InvariantCulture,out var weight))throw new ArgumentException("A finite font weight must be numeric.");
        return new FontSpec{Family=value.FontFamily??throw new ArgumentException("A font family is required."),Weight=weight,Italic=value.ItalicOverride??false,
            Variations=value.Variations??FontVariationSettings.Default,ColorPaletteIndex=value.ColorPaletteIndex??0,CollectionIndex=OptionalInt(item,"collection"),FaceName=OptionalString(item,"face")};
    }
}
