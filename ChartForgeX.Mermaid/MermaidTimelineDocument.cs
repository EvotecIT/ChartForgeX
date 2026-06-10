using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Defines Mermaid timeline direction tokens.
/// </summary>
public enum MermaidTimelineDirection {
    /// <summary>Left-to-right direction.</summary>
    LeftToRight,
    /// <summary>Top-down direction.</summary>
    TopDown
}

/// <summary>
/// Describes a parsed Mermaid timeline document.
/// </summary>
public sealed class MermaidTimelineDocument : MermaidDocument {
    /// <summary>Gets raw timeline statements retained with source spans.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets the optional Mermaid timeline title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the declared timeline direction.</summary>
    public MermaidTimelineDirection Direction { get; set; }

    /// <summary>Gets timeline sections in source order.</summary>
    public List<MermaidTimelineSection> Sections { get; } = new();

    /// <summary>Gets timeline periods in source order.</summary>
    public List<MermaidTimelinePeriod> Periods { get; } = new();
}

/// <summary>
/// Describes a Mermaid timeline section.
/// </summary>
public sealed class MermaidTimelineSection : MermaidAstNode {
    private string _name;

    /// <summary>Initializes a timeline section.</summary>
    public MermaidTimelineSection(string name, MermaidSourceSpan span) : base(span) {
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>Gets or sets the section name.</summary>
    public string Name { get => _name; set => _name = value ?? throw new ArgumentNullException(nameof(value)); }
}

/// <summary>
/// Describes one Mermaid timeline time period and its events.
/// </summary>
public sealed class MermaidTimelinePeriod : MermaidAstNode {
    private string _text;

    /// <summary>Initializes a timeline period.</summary>
    public MermaidTimelinePeriod(string text, string? section, MermaidSourceSpan span) : base(span) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        Section = section;
    }

    /// <summary>Gets or sets the period text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the containing section name.</summary>
    public string? Section { get; set; }

    /// <summary>Gets events declared for the period.</summary>
    public List<MermaidTimelineEvent> Events { get; } = new();
}

/// <summary>
/// Describes one Mermaid timeline event.
/// </summary>
public sealed class MermaidTimelineEvent : MermaidAstNode {
    private string _text;

    /// <summary>Initializes a timeline event.</summary>
    public MermaidTimelineEvent(string text, MermaidSourceSpan span) : base(span) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets or sets the event text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }
}
