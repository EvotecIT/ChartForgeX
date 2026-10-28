using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Projects inert participant facts through the existing bounded JSON, safe-link and interchange writers.</summary>
internal static class MermaidSequenceParticipantData {
    private static readonly GeoJsonReadLimits Limits = new GeoJsonReadLimits(1024, 256, 256).LimitDepth(8).RejectDuplicates().KeepNumberSpelling();

    internal static void CheckText(string text, MermaidSourceSpan span) =>
        MermaidResourceGuard.CheckInterchangeFactLength(text.Length, span, "MaximumInterchangeTextCharacters");

    internal static Dictionary<string, GeoJsonValue> Object(string raw, MermaidSourceSpan span) {
        CheckText(raw, span);
        return GeoJsonValue.Parse(raw, StringComparer.Ordinal, Limits).AsObject("Sequence participant data");
    }

    internal static List<KeyValuePair<string, string>> Links(MermaidSequenceLink link, Action<string>? warning = null, bool reserve = false, Action<string,string?>? select = null) {
        var entries = new List<KeyValuePair<string, string>>();
        if (link.RawJson == null) Add(link.Label ?? string.Empty, link.Url);
        else foreach (var pair in Object(link.RawJson, link.Span)) {
            if (pair.Value.IsString) Add(pair.Key, pair.Value.AsString("Sequence link URL"));
            else { select?.Invoke(pair.Key,null); warning?.Invoke("Sequence menu URL is retained without navigation because it is not text: " + pair.Key); }
        }
        return entries;

        void Add(string label, string? raw) {
            var href = TopologyRenderPrimitives.SafeHref(raw);
            select?.Invoke(label,string.IsNullOrWhiteSpace(label)?null:href);
            if (string.IsNullOrWhiteSpace(label) || href == null) {
                warning?.Invoke("Sequence menu entry is retained without navigation because its label or URL is unsafe or empty.");
                return;
            }
            if (reserve) MermaidResourceGuard.ReserveElements(1, link.Span);
            entries.Add(new KeyValuePair<string, string>(label, href));
        }
    }

    /// <summary>Uses authored directive order; count-only parser validation precedes any composed portable value materialization.</summary>
    internal static void Project(MermaidSequenceDocument document, Action<string, string, string> fact, Action<string, string?> href, bool materialize = true, Action<string,IReadOnlyList<VisualNavigationLink>>? navigation = null) {
        var links = document.Links.ToLookup(link => link.ParticipantId, StringComparer.Ordinal);
        var properties = document.PropertyStatements.ToLookup(statement => statement.ParticipantId, StringComparer.Ordinal);
        foreach (var participant in document.Participants) {
            MermaidResourceGuard.Checkpoint();
            if (participant.Configuration != null) CheckText(participant.Configuration, participant.Span);
            var ownLinks = links[participant.Id].ToArray();
            if (ownLinks.Length > 0) {
                var resolvedLinks = new List<KeyValuePair<string, string>>[ownLinks.Length];
                var linkNames=new List<string>();var selected=new Dictionary<string,string?>(StringComparer.Ordinal);
                void Select(string label,string? target){if(!selected.ContainsKey(label))linkNames.Add(label);selected[label]=target;}
                for (var index = 0; index < ownLinks.Length; index++) resolvedLinks[index] = Links(ownLinks[index], reserve: materialize,
                    select:materialize&&navigation!=null?Select:(Action<string,string?>?)null);
                var last = ownLinks[ownLinks.Length - 1].Span;
                var declarations = Bounded(writer => {
                    writer.StartArray();
                    foreach (var link in ownLinks) {
                        writer.StartObject(); Source(writer, link.Span);
                        writer.Property("label"); writer.String(link.Label);
                        writer.Property("url"); writer.String(link.Url);
                        writer.Property("rawJson"); writer.String(link.RawJson); writer.EndObject();
                    }
                    writer.EndArray();
                }, last, materialize);
                var entries = Bounded(writer => {
                    writer.StartArray();
                    for (var index = 0; index < ownLinks.Length; index++) foreach (var entry in resolvedLinks[index]) {
                        writer.StartObject(); Source(writer, ownLinks[index].Span);
                        writer.Property("label"); writer.String(entry.Key);
                        writer.Property("url"); writer.String(entry.Value); writer.EndObject();
                    }
                    writer.EndArray();
                }, last, materialize);
                if (materialize) {
                    for (var index = 0; index < ownLinks.Length; index++) {
                        var link = ownLinks[index]; var resolved = resolvedLinks[index];
                        href(participant.Id, resolved.Count == 1 ? resolved[0].Value : null);
                        if (link.Label != null) fact(participant.Id, "mermaid.link.label", link.Label);
                        if (link.Url != null) fact(participant.Id, "mermaid.link.url", link.Url);
                        if (link.RawJson != null) fact(participant.Id, "mermaid.links.json", link.RawJson);
                    }
                    fact(participant.Id, "mermaid.menu.statements", declarations);
                    fact(participant.Id, "mermaid.menu.entries", entries);
                    if(navigation!=null) {
                        var safe=new List<VisualNavigationLink>();foreach(var name in linkNames)if(selected[name] is {} target){MermaidResourceGuard.ReserveElements(1,last);safe.Add(new VisualNavigationLink(name,target));}
                        navigation(participant.Id,safe);
                    }
                }
            }
            var ownProperties = properties[participant.Id].ToArray();
            if (ownProperties.Length == 0) continue;
            var names = new List<string>(); var values = new Dictionary<string, GeoJsonValue>(StringComparer.Ordinal);
            foreach (var statement in ownProperties) foreach (var pair in Object(statement.RawJson, statement.Span)) {
                if (materialize) MermaidResourceGuard.ReserveElements(1, statement.Span);
                if (!values.ContainsKey(pair.Key)) names.Add(pair.Key);
                values[pair.Key] = pair.Value;
            }
            var lastSpan = ownProperties[ownProperties.Length - 1].Span;
            var statements = Bounded(writer => {
                writer.StartArray();
                foreach (var statement in ownProperties) {
                    writer.StartObject(); Source(writer, statement.Span);
                    writer.Property("rawJson"); writer.String(statement.RawJson); writer.EndObject();
                }
                writer.EndArray();
            }, lastSpan, materialize);
            var projected = Bounded(writer => {
                writer.StartArray();
                foreach (var name in names) {
                    var value = values[name];
                    if (value.TryAsObject(out _) || value.TryAsArray(out _)) continue;
                    writer.StartObject(); writer.Property("name"); writer.String(name);
                    writer.Property("kind"); writer.String(value.IsNull ? "null" : value.IsString ? "string" : value.RawValue is bool ? "boolean" : "number");
                    writer.Property("value"); writer.String(value.AsOptionalString()); writer.EndObject();
                }
                writer.EndArray();
            }, lastSpan, materialize);
            if (materialize) {
                fact(participant.Id, "mermaid.properties.statements", statements);
                fact(participant.Id, "mermaid.properties.values", projected);
            }
        }
    }

    private static void Source(VisualArtifactInterchangeJsonWriter writer, MermaidSourceSpan span) {
        writer.Property("line"); writer.Number(span.Line);
        writer.Property("column"); writer.Number(span.Column);
        writer.Property("length"); writer.Number(span.Length);
    }

    private static string Bounded(Action<VisualArtifactInterchangeJsonWriter> write, MermaidSourceSpan span, bool materialize) {
        // Text is counted before allocation; the materializing writer retains its ordinary value limit.
        var count = new VisualArtifactInterchangeJsonWriter(writeText: false, enforceValueLimit: false);
        write(count);
        MermaidResourceGuard.CheckInterchangeFactLength(count.CharactersWritten, span, "MaximumInterchangeTextCharacters");
        if (!materialize) return string.Empty;
        var writer = new VisualArtifactInterchangeJsonWriter(); write(writer);
        return writer.ToString();
    }
}
