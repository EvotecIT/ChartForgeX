using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid class diagram.
/// </summary>
public sealed class MermaidClassDocument : MermaidDocument {
    /// <summary>Gets or sets the authored whole-diagram direction.</summary>
    public string? Direction { get; set; }

    /// <summary>Gets classes in source order.</summary>
    public List<MermaidClassNode> Classes { get; } = new();

    /// <summary>Gets explicit and intermediate namespaces in first-declaration order.</summary>
    public List<MermaidClassNamespace> Namespaces { get; } = new();

    /// <summary>Gets class relationships in source order.</summary>
    public List<MermaidClassRelationship> Relationships { get; } = new();

    /// <summary>Gets source-ordered styling and retained interaction statements.</summary>
    public List<MermaidClassStyleStatement> StyleStatements { get; } = new();

    /// <summary>Gets named style definitions in source order.</summary>
    public List<MermaidStyleDefinition> ClassDefinitions { get; } = new();

    /// <summary>Gets visible floating or class-attached notes in source order.</summary>
    public List<MermaidDiagramNote> Notes { get; } = new();
}

/// <summary>
/// Describes one Mermaid class.
/// </summary>
public sealed class MermaidClassNode : MermaidAstNode {
    /// <summary>Initializes a class node.</summary>
    public MermaidClassNode(string id, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Class id must not be empty.", nameof(id));
        Id = id;
    }

    /// <summary>Gets the class id.</summary>
    public string Id { get; }

    /// <summary>Gets or sets an optional display label.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the enclosing namespace path.</summary>
    public string? Namespace { get; set; }

    /// <summary>Gets class annotations such as interface or abstract.</summary>
    public List<string> Annotations { get; } = new();

    /// <summary>Gets parsed class members.</summary>
    public List<MermaidClassMember> Members { get; } = new();

    /// <summary>Gets style classes assigned through inline or <c>cssClass</c> declarations, in assignment order.</summary>
    public List<string> Classes { get; } = new();

    /// <summary>Gets resolved style declarations, including properties diagnosed without a native mapping.</summary>
    public Dictionary<string, string> Styles { get; } = new();

    /// <summary>Gets or sets an authored link; native URI safety policy decides whether an exporter can expose it.</summary>
    public string? Href { get; set; }

    /// <summary>Gets or sets the authored link tooltip as inert text.</summary>
    public string? Tooltip { get; set; }
}

/// <summary>
/// Describes one class member.
/// </summary>
public sealed class MermaidClassMember : MermaidAstNode {
    /// <summary>Initializes a class member.</summary>
    public MermaidClassMember(string text, bool method, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Class member text must not be empty.", nameof(text));
        Text = text;
        IsMethod = method;
    }

    /// <summary>Gets the member text.</summary>
    public string Text { get; }

    /// <summary>Gets whether the member is a method/function.</summary>
    public bool IsMethod { get; }

    /// <summary>Gets the authored visibility marker, or null when no visibility was specified.</summary>
    public char? Visibility => Text.Length > 0 && "+-#~".IndexOf(Text[0]) >= 0 ? Text[0] : null;

    /// <summary>Gets whether the member carries the abstract classifier.</summary>
    public bool IsAbstract => ClassifierIndex >= 0 && Text[ClassifierIndex] == '*';

    /// <summary>Gets whether the member carries the static classifier.</summary>
    public bool IsStatic => ClassifierIndex >= 0 && Text[ClassifierIndex] == '$';

    /// <summary>Gets native display text with generic delimiters normalized and supported classifiers expressed through typography.</summary>
    public string DisplayText => MermaidNotationText.Generics(ClassifierIndex < 0 ? Text : Text.Remove(ClassifierIndex, 1));

    // Mermaid allows a method classifier immediately after ')' or after its return type.
    private int ClassifierIndex {
        get {
            var closing = IsMethod ? Text.LastIndexOf(')') : -1;
            if (closing >= 0 && closing + 1 < Text.Length && IsClassifier(Text[closing + 1])) return closing + 1;
            return Text.Length > 0 && IsClassifier(Text[Text.Length - 1]) ? Text.Length - 1 : -1;
        }
    }
    private static bool IsClassifier(char value) => value == '*' || value == '$';
}

/// <summary>
/// Describes a Mermaid class relationship.
/// </summary>
public sealed class MermaidClassRelationship : MermaidAstNode {
    /// <summary>Initializes a class relationship.</summary>
    public MermaidClassRelationship(string sourceId, string targetId, string connector, string? label, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source class id must not be empty.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("Target class id must not be empty.", nameof(targetId));
        if (string.IsNullOrWhiteSpace(connector)) throw new ArgumentException("Class relationship connector must not be empty.", nameof(connector));
        SourceId = sourceId;
        TargetId = targetId;
        Connector = connector;
        Label = label;
    }

    /// <summary>Gets the source class id.</summary>
    public string SourceId { get; }

    /// <summary>Gets the target class id.</summary>
    public string TargetId { get; }

    /// <summary>Gets the Mermaid relationship connector.</summary>
    public string Connector { get; }

    /// <summary>Gets whether the source endpoint is a distinct lollipop interface rather than a class.</summary>
    public bool SourceIsInterface => Connector.StartsWith("()", StringComparison.Ordinal);

    /// <summary>Gets whether the target endpoint is a distinct lollipop interface rather than a class.</summary>
    public bool TargetIsInterface => Connector.EndsWith("()", StringComparison.Ordinal);

    /// <summary>Gets or sets the source endpoint multiplicity.</summary>
    public string? SourceMultiplicity { get; set; }

    /// <summary>Gets or sets the target endpoint multiplicity.</summary>
    public string? TargetMultiplicity { get; set; }

    /// <summary>Gets the optional relationship label.</summary>
    public string? Label { get; }
}

/// <summary>
/// Describes a source styling, assignment or retained interaction statement.
/// </summary>
public sealed class MermaidClassStyleStatement : MermaidAstNode {
    /// <summary>Initializes a retained class styling statement.</summary>
    public MermaidClassStyleStatement(string text, MermaidSourceSpan span) : base(span) {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets the raw statement text.</summary>
    public string Text { get; }
}
