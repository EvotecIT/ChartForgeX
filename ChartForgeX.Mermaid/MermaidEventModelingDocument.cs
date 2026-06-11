using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid Event Modeling diagram.
/// </summary>
public sealed class MermaidEventModelingDocument : MermaidDocument {
    /// <summary>Gets retained non-empty Event Modeling statements.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets parsed timeframe and reset-frame entries in source order.</summary>
    public List<MermaidEventModelingFrame> Frames { get; } = new();

    /// <summary>Gets parsed data blocks keyed by their Mermaid identifier.</summary>
    public List<MermaidEventModelingDataBlock> DataBlocks { get; } = new();

    /// <summary>Gets inferred and explicit timeframe relations in render order.</summary>
    public List<MermaidEventModelingRelation> Relations { get; } = new();
}

/// <summary>
/// Identifies Event Modeling entity families.
/// </summary>
public enum MermaidEventModelingEntityKind {
    /// <summary>Unknown entity kind.</summary>
    Unknown,
    /// <summary>User interface trigger.</summary>
    Ui,
    /// <summary>Automation or processor trigger.</summary>
    Processor,
    /// <summary>Command entity.</summary>
    Command,
    /// <summary>View or read model entity.</summary>
    ReadModel,
    /// <summary>Event entity.</summary>
    Event
}

/// <summary>
/// Describes one Event Modeling timeframe.
/// </summary>
public sealed class MermaidEventModelingFrame : MermaidAstNode {
    private readonly string _number;
    private readonly string _entityIdentifier;

    /// <summary>Initializes an Event Modeling timeframe.</summary>
    public MermaidEventModelingFrame(string number, MermaidEventModelingEntityKind entityKind, string entityIdentifier, bool isReset, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("Timeframe number is required.", nameof(number));
        if (string.IsNullOrWhiteSpace(entityIdentifier)) throw new ArgumentException("Entity identifier is required.", nameof(entityIdentifier));
        _number = number;
        _entityIdentifier = entityIdentifier;
        EntityKind = entityKind;
        IsReset = isReset;
    }

    /// <summary>Gets the unique timeframe number.</summary>
    public string Number => _number;

    /// <summary>Gets the parsed Event Modeling entity kind.</summary>
    public MermaidEventModelingEntityKind EntityKind { get; }

    /// <summary>Gets the full entity identifier, including optional namespace.</summary>
    public string EntityIdentifier => _entityIdentifier;

    /// <summary>Gets whether this frame resets inferred flow.</summary>
    public bool IsReset { get; }

    /// <summary>Gets the optional namespace prefix before the first dot.</summary>
    public string? Namespace { get; set; }

    /// <summary>Gets the entity name without namespace.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional referenced data block id.</summary>
    public string? DataReference { get; set; }

    /// <summary>Gets or sets optional inline data content.</summary>
    public string? InlineData { get; set; }

    /// <summary>Gets or sets optional inline data type.</summary>
    public string? DataType { get; set; }

    /// <summary>Gets explicit source timeframe numbers supplied with the <c>->></c> relation token.</summary>
    public List<string> ExplicitSources { get; } = new();
}

/// <summary>
/// Describes an Event Modeling data block.
/// </summary>
public sealed class MermaidEventModelingDataBlock : MermaidAstNode {
    private readonly string _id;

    /// <summary>Initializes an Event Modeling data block.</summary>
    public MermaidEventModelingDataBlock(string id, string? dataType, string content, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Data block id is required.", nameof(id));
        _id = id;
        DataType = dataType;
        Content = content ?? string.Empty;
    }

    /// <summary>Gets the Mermaid data block id.</summary>
    public string Id => _id;

    /// <summary>Gets the optional Mermaid data type marker.</summary>
    public string? DataType { get; }

    /// <summary>Gets the raw data block content.</summary>
    public string Content { get; }
}

/// <summary>
/// Describes one Event Modeling relation between two timeframes.
/// </summary>
public sealed class MermaidEventModelingRelation : MermaidAstNode {
    private readonly string _sourceNumber;
    private readonly string _targetNumber;

    /// <summary>Initializes an Event Modeling relation.</summary>
    public MermaidEventModelingRelation(string sourceNumber, string targetNumber, bool isInferred, MermaidSourceSpan span) : base(span) {
        if (string.IsNullOrWhiteSpace(sourceNumber)) throw new ArgumentException("Source number is required.", nameof(sourceNumber));
        if (string.IsNullOrWhiteSpace(targetNumber)) throw new ArgumentException("Target number is required.", nameof(targetNumber));
        _sourceNumber = sourceNumber;
        _targetNumber = targetNumber;
        IsInferred = isInferred;
    }

    /// <summary>Gets the source timeframe number.</summary>
    public string SourceNumber => _sourceNumber;

    /// <summary>Gets the target timeframe number.</summary>
    public string TargetNumber => _targetNumber;

    /// <summary>Gets whether the relation was inferred from timeline order.</summary>
    public bool IsInferred { get; }
}
