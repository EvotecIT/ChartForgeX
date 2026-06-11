using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a parsed Mermaid user journey diagram.
/// </summary>
public sealed class MermaidJourneyDocument : MermaidDocument {
    /// <summary>Gets raw journey statements retained with source spans.</summary>
    public List<MermaidRawStatement> Statements { get; } = new();

    /// <summary>Gets or sets the optional Mermaid journey title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets journey sections in source order.</summary>
    public List<MermaidJourneySection> Sections { get; } = new();

    /// <summary>Gets journey tasks in source order.</summary>
    public List<MermaidJourneyTask> Tasks { get; } = new();
}

/// <summary>
/// Describes one Mermaid journey section.
/// </summary>
public sealed class MermaidJourneySection : MermaidAstNode {
    private string _name;

    /// <summary>Initializes a journey section.</summary>
    public MermaidJourneySection(string name, MermaidSourceSpan span) : base(span) {
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>Gets or sets the section name.</summary>
    public string Name { get => _name; set => _name = value ?? throw new ArgumentNullException(nameof(value)); }
}

/// <summary>
/// Describes one scored Mermaid journey task.
/// </summary>
public sealed class MermaidJourneyTask : MermaidAstNode {
    private string _text;

    /// <summary>Initializes a journey task.</summary>
    public MermaidJourneyTask(string text, string? section, double score, MermaidSourceSpan span) : base(span) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        Section = section;
        Score = score;
    }

    /// <summary>Gets or sets the task text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the containing section name.</summary>
    public string? Section { get; set; }

    /// <summary>Gets or sets the Mermaid journey score.</summary>
    public double Score { get; set; }

    /// <summary>Gets actors associated with the task.</summary>
    public List<string> Actors { get; } = new();
}
