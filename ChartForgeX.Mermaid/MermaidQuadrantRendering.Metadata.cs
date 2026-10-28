using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

public static partial class MermaidQuadrantRendering {
    /// <summary>Detaches bounded raw declarations alongside native chart facts; it is not a new editable chart serializer.</summary>
    private static void AttachSourceFacts(VisualArtifact artifact, MermaidQuadrantDocument document) {
        var configuration = document.Configuration.Settings.Where(setting => setting.Path.StartsWith("quadrantChart.", StringComparison.Ordinal)
            || setting.Path.StartsWith("themeVariables.quadrant", StringComparison.Ordinal)).ToArray();
        if (configuration.Length == 0 && document.ClassDefinitions.Count == 0
            && document.Points.All(point => point.ClassName == null && point.Styles.Count == 0)) return;
        var span = document.HeaderSpan;
        // The serialized facts contain each of these source strings. Refuse an oversized lower bound before allocating their metadata graph.
        long textLength = 0;
        void Charge(string? value) { textLength += value?.Length ?? 0; MermaidResourceGuard.CheckInterchangeFactLength(textLength, span); }
        Charge(document.XAxisStart); Charge(document.XAxisEnd); Charge(document.YAxisStart); Charge(document.YAxisEnd);
        foreach (var statement in document.Statements) Charge(statement.Text);
        foreach (var definition in document.ClassDefinitions) {
            Charge(definition.Name); foreach (var style in definition.Styles) { Charge(style.Key); Charge(style.Value); }
        }
        foreach (var point in document.Points) {
            Charge(point.Label); Charge(point.ClassName); foreach (var style in point.Styles) { Charge(style.Key); Charge(style.Value); }
        }
        foreach (var setting in configuration) { Charge(setting.Path); Charge(setting.Value); }
        MermaidMetadataValue Text(string? value, MermaidSourceSpan location) => value == null ? MermaidMetadataValue.Null(location) : MermaidMetadataValue.FromString(value, location);
        MermaidMetadataValue Number(double value, MermaidSourceSpan location) => MermaidMetadataValue.FromNumber(value, location);
        MermaidMetadataValue Object(MermaidSourceSpan location, params (string Name, MermaidMetadataValue Value)[] values) =>
            MermaidMetadataValue.FromObject(values.Select(value => new KeyValuePair<string, MermaidMetadataValue>(value.Name, value.Value)), location);
        MermaidMetadataValue Location(MermaidSourceSpan location) => Object(location,
            ("line", Number(location.Line, location)), ("column", Number(location.Column, location)), ("length", Number(location.Length, location)));
        // Declarations are ordered inert name/value records, preserving even prototype-shaped unsupported property names.
        MermaidMetadataValue Styles(Dictionary<string, string> styles, MermaidSourceSpan location) => MermaidMetadataValue.FromArray(styles.Select(style =>
            Object(location, ("name", Text(style.Key, location)), ("value", Text(style.Value, location)))), location);
        var facts = Object(span,
            ("axes", Object(span, ("xStart", Text(document.XAxisStart, span)), ("xEnd", Text(document.XAxisEnd, span)),
                ("yStart", Text(document.YAxisStart, span)), ("yEnd", Text(document.YAxisEnd, span)))),
            ("classes", MermaidMetadataValue.FromArray(document.ClassDefinitions.Select(definition => Object(definition.Span,
                ("name", Text(definition.Name, definition.Span)), ("styles", Styles(definition.Styles, definition.Span)), ("span", Location(definition.Span)))), span)),
            ("points", MermaidMetadataValue.FromArray(document.Points.Select(point => Object(point.Span,
                ("label", Text(point.Label, point.Span)), ("className", Text(point.ClassName, point.Span)),
                ("x", Number(point.X, point.Span)), ("y", Number(point.Y, point.Span)), ("styles", Styles(point.Styles, point.Span)), ("span", Location(point.Span)))), span)),
            ("statements", MermaidMetadataValue.FromArray(document.Statements.Select(statement => Object(statement.Span,
                ("text", Text(statement.Text, statement.Span)), ("span", Location(statement.Span)))), span)),
            ("configuration", MermaidMetadataValue.FromArray(configuration.Select(setting => Object(setting.Span,
                ("path", Text(setting.Path, setting.Span)), ("value", Text(setting.Value, setting.Span)),
                ("isString", MermaidMetadataValue.FromBoolean(setting.IsString, setting.Span)), ("span", Location(setting.Span)))), span)));
        var json = facts.ToJson();
        MermaidResourceGuard.CheckInterchangeFactLength(json.Length, span);
        artifact.Metadata["mermaid.quadrant.sourceFacts"] = json;
    }
}
