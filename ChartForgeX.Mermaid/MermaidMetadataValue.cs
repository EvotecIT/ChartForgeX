using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Identifies one inert source metadata value; none of these values executes code or resolves a URL.</summary>
public enum MermaidMetadataKind {
    /// <summary>A mapping.</summary>
    Object,
    /// <summary>An ordered sequence.</summary>
    Array,
    /// <summary>Text.</summary>
    String,
    /// <summary>A finite number.</summary>
    Number,
    /// <summary>A boolean.</summary>
    Boolean,
    /// <summary>An explicit null.</summary>
    Null
}

/// <summary>A detached, immutable metadata value shared by typed Mermaid frontends.</summary>
public sealed class MermaidMetadataValue : MermaidAstNode {
    private MermaidMetadataValue(MermaidMetadataKind kind, MermaidSourceSpan span) : base(span) { Kind = kind; }
    /// <summary>Gets the value kind.</summary>
    public MermaidMetadataKind Kind { get; }
    /// <summary>Gets a string value, or null for another kind.</summary>
    public string? Text { get; private set; }
    /// <summary>Gets a finite number, or null for another kind.</summary>
    public double? Number { get; private set; }
    /// <summary>Gets a boolean, or null for another kind.</summary>
    public bool? Boolean { get; private set; }
    /// <summary>Gets mapping members. Exact prototype-shaped keys are omitted recursively.</summary>
    public IReadOnlyDictionary<string, MermaidMetadataValue> Properties { get; private set; } = new ReadOnlyDictionary<string, MermaidMetadataValue>(new Dictionary<string, MermaidMetadataValue>());
    /// <summary>Gets ordered sequence items.</summary>
    public IReadOnlyList<MermaidMetadataValue> Items { get; private set; } = Array.Empty<MermaidMetadataValue>();

    /// <summary>Creates inert text.</summary>
    public static MermaidMetadataValue FromString(string value, MermaidSourceSpan span) => new(MermaidMetadataKind.String, span) { Text = value ?? throw new ArgumentNullException(nameof(value)) };
    /// <summary>Creates a finite numeric value.</summary>
    public static MermaidMetadataValue FromNumber(double value, MermaidSourceSpan span) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
        return new MermaidMetadataValue(MermaidMetadataKind.Number, span) { Number = value };
    }
    /// <summary>Creates a boolean value.</summary>
    public static MermaidMetadataValue FromBoolean(bool value, MermaidSourceSpan span) => new(MermaidMetadataKind.Boolean, span) { Boolean = value };
    /// <summary>Creates an explicit null.</summary>
    public static MermaidMetadataValue Null(MermaidSourceSpan span) => new(MermaidMetadataKind.Null, span);
    /// <summary>Captures an ordered sequence without retaining the caller's mutable collection.</summary>
    public static MermaidMetadataValue FromArray(IEnumerable<MermaidMetadataValue> values, MermaidSourceSpan span) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var copy = new List<MermaidMetadataValue>();
        foreach (var value in values) copy.Add(value ?? throw new ArgumentException("Metadata items must not be null; use the explicit Null value.", nameof(values)));
        return new MermaidMetadataValue(MermaidMetadataKind.Array, span) { Items = copy.AsReadOnly() };
    }
    /// <summary>Captures a mapping without retaining the caller's mutable collection.</summary>
    public static MermaidMetadataValue FromObject(IEnumerable<KeyValuePair<string, MermaidMetadataValue>> values, MermaidSourceSpan span) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var copy = new Dictionary<string, MermaidMetadataValue>(StringComparer.Ordinal);
        foreach (var pair in values) {
            if (pair.Key == null || pair.Value == null) throw new ArgumentException("Metadata keys and values must not be null.", nameof(values));
            if (!IsPrototypeKey(pair.Key)) copy.Add(pair.Key, pair.Value);
        }
        return new MermaidMetadataValue(MermaidMetadataKind.Object, span) { Properties = new ReadOnlyDictionary<string, MermaidMetadataValue>(copy) };
    }
    /// <summary>Exports inert metadata within the surrounding captured Mermaid operation.</summary>
    internal string ToJson() {
        var writer = new VisualArtifactInterchangeJsonWriter(enforceValueLimit: false);
        Write(writer, 1);
        return writer.ToString();
    }
    internal static bool IsPrototypeKey(string key) => key == "__proto__" || key == "constructor" || key == "prototype";
    private void Write(VisualArtifactInterchangeJsonWriter writer, int depth) {
        MermaidResourceGuard.CheckDepth(depth, Span);
        switch (Kind) {
            case MermaidMetadataKind.Object:
                writer.StartObject();
                foreach (var pair in Properties) { MermaidResourceGuard.Checkpoint(); writer.Property(pair.Key); pair.Value.Write(writer, depth + 1); }
                writer.EndObject(); break;
            case MermaidMetadataKind.Array:
                writer.StartArray();
                foreach (var value in Items) { MermaidResourceGuard.Checkpoint(); value.Write(writer, depth + 1); }
                writer.EndArray(); break;
            case MermaidMetadataKind.String: writer.String(Text); break;
            case MermaidMetadataKind.Number: writer.Number(Number!.Value); break;
            case MermaidMetadataKind.Boolean: writer.Boolean(Boolean!.Value); break;
            default: writer.String(null); break;
        }
    }
}
