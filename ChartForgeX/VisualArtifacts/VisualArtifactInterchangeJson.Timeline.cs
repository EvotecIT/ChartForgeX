using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void Timeline(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeTimelineArtifact? source) {
        if (source == null) return;
        writer.Property("timeline"); writer.StartObject();
        String(writer, "themeJson", source.Theme.ToJson()); String(writer, "themeMode", source.ThemeMode.ToString());
        Number(writer, "paddingLeft", source.Padding.Left); Number(writer, "paddingTop", source.Padding.Top);
        Number(writer, "paddingRight", source.Padding.Right); Number(writer, "paddingBottom", source.Padding.Bottom);
        Boolean(writer, "transparentBackground", source.TransparentBackground); Boolean(writer, "showCard", source.ShowCard);
        Boolean(writer, "hostOwnsFrame", source.HostOwnsFrame); Number(writer, "pngOutputScale", source.PngOutputScale);
        String(writer, "direction", source.Direction.ToString());
        writer.Property("timelineSections"); writer.StartArray();
        foreach (var item in source.Sections) { writer.StartObject(); String(writer, "id", item.Id); String(writer, "label", item.Label); writer.EndObject(); }
        writer.EndArray(); writer.Property("timelinePeriods"); writer.StartArray();
        foreach (var item in source.Periods) {
            writer.StartObject(); String(writer, "id", item.Id); String(writer, "label", item.Label); OptionalString(writer, "sectionId", item.SectionId);

            writer.Property("timelineEvents"); writer.StartArray(); foreach (var text in item.Events) writer.String(text);
            writer.EndArray(); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();
    }

    private static VisualArtifactInterchangeTimelineArtifact? ReadTimeline(Dictionary<string, GeoJsonValue> root) {
        var fields = OptionalObject(root, "timeline"); if (fields == null) return null;
        var source = new VisualArtifactInterchangeTimelineArtifact {
            ThemeMode = OptionalEnum<VisualThemeMode>(fields, "themeMode") ?? VisualThemeMode.Light,
            Padding = new ChartPadding(OptionalNumber(fields, "paddingLeft") ?? 18, OptionalNumber(fields, "paddingTop") ?? 16,
                OptionalNumber(fields, "paddingRight") ?? 18, OptionalNumber(fields, "paddingBottom") ?? 12),
            TransparentBackground = OptionalBool(fields, "transparentBackground") ?? false,
            ShowCard = OptionalBool(fields, "showCard") ?? true, HostOwnsFrame = OptionalBool(fields, "hostOwnsFrame") ?? false,
            PngOutputScale = OptionalInt(fields, "pngOutputScale") ?? 1,
            Direction = RequiredEnum<TimelineDiagramDirection>(fields, "direction")
        };
        var theme = OptionalString(fields, "themeJson"); if (theme != null) source.Theme = VisualTheme.FromJson(theme);
        var sections = OptionalArray(fields, "timelineSections"); if (sections.Count > 128) throw new ArgumentException("Timeline sections exceed the native limit.");
        foreach (var record in sections) { var item = record.AsObject("timeline section"); source.Sections.Add(new TimelineSection(RequiredString(item, "id"), RequiredString(item, "label"))); }
        var records = OptionalArray(fields, "timelinePeriods"); if (records.Count > 512) throw new ArgumentException("Timeline periods exceed the native limit.");
        var totalEvents = 0;
        foreach (var record in records) {
            var item = record.AsObject("timeline period"); var values = new List<string>();
            foreach (var value in OptionalArray(item, "timelineEvents")) {
                if (++totalEvents > 2048) throw new ArgumentException("Timeline events exceed the native limit.");
                values.Add(value.AsString("timeline event"));
            }
            source.Periods.Add(new TimelinePeriod(RequiredString(item, "id"), RequiredString(item, "label"),
                values, OptionalString(item, "sectionId")));
        }
        return source;
    }
}
