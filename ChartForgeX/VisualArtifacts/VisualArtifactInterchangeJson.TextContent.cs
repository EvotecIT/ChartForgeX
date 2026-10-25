using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void CaptionRuns(VisualArtifactInterchangeJsonWriter writer, string name, TextContent? content) {
        if (content == null || !content.IsStructured) return;
        writer.Property(name); writer.StartArray();
        foreach (var run in content.Runs) {
            writer.StartObject(); String(writer, "text", run.Text);
            if ((run.Emphasis & TextInlineEmphasis.Bold) != 0) Boolean(writer, "bold", true);
            if ((run.Emphasis & TextInlineEmphasis.Italic) != 0) Boolean(writer, "italic", true);
            writer.EndObject();
        }
        writer.EndArray();
    }
    private static TextContent? ReadCaptionRuns(Dictionary<string, GeoJsonValue> item, string name) {
        if (!item.TryGetValue(name, out var value) || value.IsNull) return null;
        var runs = value.AsArray(name);
        if (runs.Count > VisualArtifactInterchangeValidation.MaximumTextCharacters) throw new ArgumentException("Caption run count exceeds the portable text limit.");
        var result = new List<TextContentRun>(); var characters = 0;
        foreach (var entry in runs) {
            var run = entry.AsObject("caption run"); var text = RequiredStringAllowEmpty(run, "text");
            characters += text.Length;
            if (characters > VisualArtifactInterchangeValidation.MaximumTextCharacters) throw new ArgumentException("Caption runs exceed the portable text limit.");
            var emphasis = (OptionalBool(run, "bold") == true ? TextInlineEmphasis.Bold : TextInlineEmphasis.Normal)
                | (OptionalBool(run, "italic") == true ? TextInlineEmphasis.Italic : TextInlineEmphasis.Normal);
            result.Add(new TextContentRun(text, emphasis));
        }
        return TextContent.FromRuns(result);
    }
}
