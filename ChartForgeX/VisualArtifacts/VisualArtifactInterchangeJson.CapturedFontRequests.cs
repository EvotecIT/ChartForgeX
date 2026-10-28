using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void CapturedFontRequests(VisualArtifactInterchangeJsonWriter writer, VisualSceneTextCapture? text, OwnedFontResources roots) {
        if (text == null) return;
        writer.Property("fontRequests"); writer.StartArray();
        foreach (var request in text.Requests) {
            writer.StartObject(); var style = request.Style; FontValue(writer, style.Font, "font", roots);
            OptionalString(writer, "language", style.OpenTypeLanguageTag); String(writer, "sourceCase", style.TextCase.ToString());
            writer.Property("cases"); writer.StartArray(); foreach (var value in request.Cases.OrderBy(value => value)) writer.String(value.ToString()); writer.EndArray();
            Boolean(writer, "synthesizeBold", request.Face.SynthesizeBold); Boolean(writer, "synthesizeItalic", request.Face.SynthesizeItalic);
            writer.Property("face"); CapturedFontView(writer, request.Face.Font!, roots);
            if (request.Clusters.Decisions.Count > 0) {
                writer.Property("fontClusters"); writer.StartArray();
                foreach (var decision in request.Clusters.Decisions) {
                    writer.StartObject(); Scalars("inputScalars", decision.Raw); Scalars("outputScalars", decision.Output);
                    writer.Property("face"); CapturedFontView(writer, decision.Face, roots); writer.EndObject();
                }
                writer.EndArray();
            }
            writer.EndObject();
        }
        writer.EndArray();
        void Scalars(string name, IReadOnlyList<int> values) { writer.Property(name); writer.StartArray(); foreach (var value in values) writer.Number(value); writer.EndArray(); }
    }
    private static VisualSceneTextCapture? ReadCapturedFontRequests(Dictionary<string, GeoJsonValue> owner, OwnedFontRootReader roots) {
        if (!owner.ContainsKey("fontRequests")) return null;
        var requests = new List<VisualSceneTextCapture.Request>();
        foreach (var value in OptionalArray(owner, "fontRequests")) {
            var item = value.AsObject("captured font request"); var font = ReadFontValue(item, "font", roots);
            var style = new TextStyle { Font = font, OpenTypeLanguageTag = OptionalString(item, "language"), TextCase = RequiredEnum<TextCaseTransform>(item, "sourceCase") };
            var face = ReadCapturedFontView(OptionalObject(item, "face") ?? throw new ArgumentException("A font request requires its owned actual face."), roots);
            var resolved = new ResolvedTypeface(face,
                OptionalBool(item, "synthesizeBold") ?? throw new ArgumentException("A captured font requires its bold synthesis fact."),
                OptionalBool(item, "synthesizeItalic") ?? throw new ArgumentException("A captured font requires its italic synthesis fact."));
            var clusters = new TextShaper.CapturedClusters(); var count = 0;
            foreach (var entry in OptionalArray(item, "fontClusters")) {
                var fields = entry.AsObject("captured font fallback cluster");
                var raw = Scalars(fields, "inputScalars"); var output = Scalars(fields, "outputScalars");
                if (raw.Count == 0 || ++count > TextShaper.CapturedClusters.MaximumClusters)
                    throw new ArgumentException("Captured fallback clusters require finite nonempty input within the existing cluster limit.");
                var fallback = ReadCapturedFontView(OptionalObject(fields, "face") ?? throw new ArgumentException("A fallback cluster requires its actual owned face."), roots);
                clusters.Observe(raw, 0, raw.Count, fallback, output);
                if (!clusters.Supported || clusters.Decisions.Count != count) throw new ArgumentException("Captured fallback clusters exceed limits or repeat contradictory input.");
            }
            var cases = new List<TextCaseTransform>(); foreach (var entry in OptionalArray(item, "cases")) {
                if (!Enum.TryParse<TextCaseTransform>(entry.AsString("captured text case"), out var selected) || !Enum.IsDefined(typeof(TextCaseTransform), selected))
                    throw new ArgumentException("Captured text-case policy is undefined.");
                cases.Add(selected);
            }
            requests.Add(VisualSceneTextCapture.Request.FromOwnedFacts(style, resolved, clusters, cases));
        }
        return VisualSceneTextCapture.FromOwnedRequests(requests);
        static IReadOnlyList<int> Scalars(Dictionary<string, GeoJsonValue> item, string name) {
            var values = OptionalArray(item, name); if (values.Count > TextShaper.CapturedClusters.MaximumScalars) throw new ArgumentException("Captured font scalars exceed the existing bound.");
            var result = new int[values.Count];
            for (var index = 0; index < values.Count; index++) {
                var value = values[index].AsNumber("captured Unicode scalar");
                if (value < 0 || value > 0x10FFFF || value != Math.Truncate(value) || value >= 0xD800 && value <= 0xDFFF)
                    throw new ArgumentException("Captured font clusters require Unicode scalar values.");
                result[index] = (int)value;
            }
            return result;
        }
    }
}
