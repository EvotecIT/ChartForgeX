using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

/// <summary>An ordered user journey with scored tasks, explicit sections, and participating actors.</summary>
public sealed class JourneyDiagramBlock : VisualBlock<JourneyDiagramBlock> {
    private readonly List<JourneySection> _sections = new();
    private readonly List<JourneyTask> _tasks = new();
    private readonly IReadOnlyList<JourneySection> _sectionView;
    private readonly IReadOnlyList<JourneyTask> _taskView;
    /// <summary>Initializes an empty native diagram.</summary>
    public JourneyDiagramBlock() { _sectionView = _sections.AsReadOnly(); _taskView = _tasks.AsReadOnly(); }
    /// <summary>Gets a concise accessible diagram name.</summary>
    public override string AccessibleName => Options.Accessibility.Name ?? (Title.Length == 0 ? "Journey diagram" : Title);
    /// <summary>Gets sections in authored order, including repeated display labels.</summary>
    public IReadOnlyList<JourneySection> Sections => _sectionView;
    /// <summary>Gets tasks in their journey order.</summary>
    public IReadOnlyList<JourneyTask> Tasks => _taskView;
    /// <summary>Creates an empty journey.</summary>
    public static JourneyDiagramBlock Create() => new();
    /// <summary>Adds a section with a stable identity independent of its display label.</summary>
    public JourneyDiagramBlock AddSection(string id, string label) { _sections.Add(new JourneySection(id, label)); return this; }
    /// <summary>Adds a task with a score from one through five and its participating actors.</summary>
    public JourneyDiagramBlock AddTask(string id, string label, double score, IEnumerable<string>? actors = null, string? sectionId = null) {
        _tasks.Add(new JourneyTask(id, label, score, actors, sectionId)); return this;
    }
}

/// <summary>A named phase of an ordered journey.</summary>
public sealed class JourneySection {
    /// <summary>Initializes a phase.</summary>
    public JourneySection(string id, string label) { Id = id ?? throw new ArgumentNullException(nameof(id)); Label = label ?? throw new ArgumentNullException(nameof(label)); }
    /// <summary>Gets the stable phase identity.</summary>
    public string Id { get; }
    /// <summary>Gets the display label.</summary>
    public string Label { get; }
}

/// <summary>A task's exact score and actor membership; scores are ordinal satisfaction values, not durations.</summary>
public sealed class JourneyTask {
    private readonly IReadOnlyList<string> _actors;
    /// <summary>Initializes a scored task.</summary>
    public JourneyTask(string id, string label, double score, IEnumerable<string>? actors = null, string? sectionId = null) {
        Id = id ?? throw new ArgumentNullException(nameof(id)); Label = label ?? throw new ArgumentNullException(nameof(label));
        Score = score; SectionId = sectionId; _actors = (actors == null ? new List<string>() : new List<string>(actors)).AsReadOnly();
    }
    /// <summary>Gets the stable task identity.</summary>
    public string Id { get; }
    /// <summary>Gets the task label.</summary>
    public string Label { get; }
    /// <summary>Gets the score, on the inclusive one-to-five scale.</summary>
    public double Score { get; }
    /// <summary>Gets the containing phase, or null for an unsectioned task.</summary>
    public string? SectionId { get; }
    /// <summary>Gets the participating actors in authored order.</summary>
    public IReadOnlyList<string> Actors => _actors;
}
