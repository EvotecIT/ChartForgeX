using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void FontValue(VisualArtifactInterchangeJsonWriter writer, FontSpec font, string name, OwnedFontResources roots) {
        writer.Property(name); writer.StartObject();
        FontValueFields(writer,font);
        if (font.CapturedExplicitFile) Boolean(writer, "explicitFile", true);
        if (font.OwnedFileFont is {} owned) { writer.Property("ownedFile"); CapturedFontView(writer, owned, roots); }
        writer.EndObject();
    }
    private static FontSpec ReadFontValue(Dictionary<string, GeoJsonValue> root, string name, OwnedFontRootReader roots) {
        var item = OptionalObject(root, name) ?? throw new ArgumentException("A captured font request requires its finite font value.");
        foreach(var key in item.Keys)if(key is not("policy" or "collection" or "face" or "ownedFile" or "explicitFile"))
            throw new ArgumentException("An owned font value has an unknown resource field.");
        var font=ReadFontValueFields(item);
        var owned = OptionalObject(item, "ownedFile"); var explicitFile = OptionalBool(item, "explicitFile") ?? false;
        if (owned != null && !explicitFile) throw new ArgumentException("An owned explicit face requires its captured explicit-file request fact.");
        return owned == null && !explicitFile ? font : font.WithOwnedFile(owned == null ? null : ReadCapturedFontView(owned, roots), explicitFile);
    }
}
