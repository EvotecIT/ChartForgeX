using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void Journey(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeJourneyArtifact? source) {
        if (source == null) return;
        writer.Property("journey"); writer.StartObject();
        String(writer, "themeJson", source.Theme.ToJson()); String(writer, "themeMode", source.ThemeMode.ToString());
        Number(writer, "paddingLeft", source.Padding.Left); Number(writer, "paddingTop", source.Padding.Top);
        Number(writer, "paddingRight", source.Padding.Right); Number(writer, "paddingBottom", source.Padding.Bottom);
        Boolean(writer, "transparentBackground", source.TransparentBackground); Boolean(writer, "showCard", source.ShowCard);
        Boolean(writer, "hostOwnsFrame", source.HostOwnsFrame); Number(writer, "pngOutputScale", source.PngOutputScale);

        writer.Property("journeySections"); writer.StartArray();
        foreach (var item in source.Sections) { writer.StartObject(); String(writer, "id", item.Id); String(writer, "label", item.Label); writer.EndObject(); }
        writer.EndArray(); writer.Property("journeyTasks"); writer.StartArray();
        foreach (var item in source.Tasks) {
            writer.StartObject(); String(writer, "id", item.Id); String(writer, "label", item.Label); OptionalString(writer, "sectionId", item.SectionId);
            Number(writer, "score", item.Score);
            writer.Property("journeyActors"); writer.StartArray(); foreach (var text in item.Actors) writer.String(text);
            writer.EndArray(); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();
    }

    private static VisualArtifactInterchangeJourneyArtifact? ReadJourney(Dictionary<string, GeoJsonValue> root) {
        var fields = OptionalObject(root, "journey"); if (fields == null) return null;
        var source = new VisualArtifactInterchangeJourneyArtifact {
            ThemeMode = OptionalEnum<VisualThemeMode>(fields, "themeMode") ?? VisualThemeMode.Light,
            Padding = new ChartPadding(OptionalNumber(fields, "paddingLeft") ?? 18, OptionalNumber(fields, "paddingTop") ?? 16,
                OptionalNumber(fields, "paddingRight") ?? 18, OptionalNumber(fields, "paddingBottom") ?? 12),
            TransparentBackground = OptionalBool(fields, "transparentBackground") ?? false,
            ShowCard = OptionalBool(fields, "showCard") ?? true, HostOwnsFrame = OptionalBool(fields, "hostOwnsFrame") ?? false,
            PngOutputScale = OptionalInt(fields, "pngOutputScale") ?? 1

        };
        var theme = OptionalString(fields, "themeJson"); if (theme != null) source.Theme = VisualTheme.FromJson(theme);
        var sections = OptionalArray(fields, "journeySections"); if (sections.Count > 128) throw new ArgumentException("Journey sections exceed the native limit.");
        foreach (var record in sections) { var item = record.AsObject("journey section"); source.Sections.Add(new JourneySection(RequiredString(item, "id"), RequiredString(item, "label"))); }
        var records = OptionalArray(fields, "journeyTasks"); if (records.Count > 512) throw new ArgumentException("Journey tasks exceed the native limit.");
        foreach (var record in records) {
            var item = record.AsObject("journey task"); var values = new List<string>();
            foreach (var value in OptionalArray(item, "journeyActors")) values.Add(value.AsString("journey actor"));
            source.Tasks.Add(new JourneyTask(RequiredString(item, "id"), RequiredString(item, "label"),
                RequiredNumber(item, "score"), values, OptionalString(item, "sectionId")));
        }
        return source;
    }
}
