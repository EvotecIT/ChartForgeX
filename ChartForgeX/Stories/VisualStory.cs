using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Stories;

/// <summary>Specifies how panels are arranged within one visual-story scene.</summary>
public enum VisualStorySceneLayout {
    /// <summary>One primary panel fills the content region.</summary>
    Focus,
    /// <summary>Panels are arranged side by side.</summary>
    Split,
    /// <summary>Panels are arranged from top to bottom.</summary>
    Stacked
}

/// <summary>Represents one titled surface within a scene.</summary>
public sealed class VisualStoryPanel {
    internal VisualStoryPanel(string id, string title, VisualStorySurface surface, double weight) {
        Id = VisualStorySurface.RequireText(id, nameof(id));
        Title = VisualStorySurface.OptionalHeading(title, nameof(title));
        Surface = surface ?? throw new ArgumentNullException(nameof(surface));
        if (double.IsNaN(weight) || double.IsInfinity(weight) || weight <= 0 || weight > 10) throw new ArgumentOutOfRangeException(nameof(weight));
        Weight = weight;
    }

    /// <summary>Gets the stable panel identifier referenced by outcomes.</summary>
    public string Id { get; }

    /// <summary>Gets the optional panel title.</summary>
    public string Title { get; }

    /// <summary>Gets the resolved panel surface.</summary>
    public VisualStorySurface Surface { get; }

    /// <summary>Gets the relative panel size within split or stacked layouts.</summary>
    public double Weight { get; }
}

/// <summary>Represents one timed visual-story scene.</summary>
public sealed class VisualStoryScene {
    private readonly List<VisualStoryPanel> _panels = new();

    internal VisualStoryScene(string id, string title, double durationSeconds, VisualStorySceneLayout layout) {
        Id = VisualStorySurface.RequireText(id, nameof(id));
        Title = VisualStorySurface.RequireHeading(title, nameof(title));
        if (double.IsNaN(durationSeconds) || double.IsInfinity(durationSeconds) || durationSeconds < 0.25 || durationSeconds > 60) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        if (!Enum.IsDefined(typeof(VisualStorySceneLayout), layout)) throw new ArgumentOutOfRangeException(nameof(layout));
        DurationSeconds = durationSeconds;
        Layout = layout;
    }

    /// <summary>Gets the stable scene identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the scene heading.</summary>
    public string Title { get; }

    /// <summary>Gets the scene duration.</summary>
    public double DurationSeconds { get; }

    /// <summary>Gets the panel layout.</summary>
    public VisualStorySceneLayout Layout { get; }

    /// <summary>Gets the ordered scene panels.</summary>
    public IReadOnlyList<VisualStoryPanel> Panels => _panels;

    /// <summary>Adds a resolved panel.</summary>
    public VisualStoryScene Panel(string id, VisualStorySurface surface, string? title = null, double weight = 1) {
        if (_panels.Count >= 4) throw new InvalidOperationException("Visual-story scenes support at most four panels.");
        var normalizedId = VisualStorySurface.RequireText(id, nameof(id));
        if (_panels.Any(panel => string.Equals(panel.Id, normalizedId, StringComparison.Ordinal))) throw new ArgumentException("Panel identifiers must be unique within a scene.", nameof(id));
        _panels.Add(new VisualStoryPanel(normalizedId, title ?? string.Empty, surface, weight));
        return this;
    }

    internal void Validate() {
        if (_panels.Count == 0) throw new InvalidOperationException("Visual-story scenes require at least one panel.");
        if (Layout == VisualStorySceneLayout.Focus && _panels.Count != 1) throw new InvalidOperationException("Focus scenes require exactly one panel.");
        foreach (var panel in _panels) {
            if (panel.Surface is VisualStorySourceSurface source) source.Source.Validate();
            if (panel.Surface is VisualStoryTerminalSurface terminal) terminal.Terminal.Validate();
        }
    }
}

/// <summary>Declares an artifact or result that the completed story promises to reveal.</summary>
public sealed class VisualStoryOutcome {
    internal VisualStoryOutcome(string id, string label, string panelId) {
        Id = VisualStorySurface.RequireText(id, nameof(id));
        Label = VisualStorySurface.RequireSingleLineText(label, nameof(label));
        if (Label.Length > 512) {
            throw new ArgumentOutOfRangeException(nameof(label), "Outcome labels support at most 512 UTF-16 code units.");
        }
        PanelId = VisualStorySurface.RequireText(panelId, nameof(panelId));
    }

    /// <summary>Gets the stable outcome identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the human-readable outcome label.</summary>
    public string Label { get; }

    /// <summary>Gets the panel that must be present in the completed scene.</summary>
    public string PanelId { get; }
}

/// <summary>
/// Models a resolved, deterministic visual story. Execution and tokenization belong to optional adapters.
/// </summary>
public sealed class VisualStory {
    private readonly List<VisualStoryScene> _scenes = new();
    private readonly List<VisualStoryOutcome> _outcomes = new();

    private VisualStory(string title) {
        Title = VisualStorySurface.RequireHeading(title, nameof(title));
    }

    /// <summary>Gets the story title.</summary>
    public string Title { get; }

    /// <summary>Gets the optional story description.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets the logical output width.</summary>
    public int Width { get; private set; } = 1200;

    /// <summary>Gets the logical output height.</summary>
    public int Height { get; private set; } = 675;

    /// <summary>Gets the output theme.</summary>
    public VisualStoryTheme Theme { get; private set; } = VisualStoryTheme.PremiumDark();

    /// <summary>Gets the ordered scenes.</summary>
    public IReadOnlyList<VisualStoryScene> Scenes => _scenes;

    /// <summary>Gets declared outcomes that must be visible in the completed scene.</summary>
    public IReadOnlyList<VisualStoryOutcome> Outcomes => _outcomes;

    /// <summary>Creates a visual story.</summary>
    public static VisualStory Create(string title) => new(title);

    /// <summary>Sets the accessible story description.</summary>
    public VisualStory WithDescription(string description) {
        Description = VisualStorySurface.RequireText(description, nameof(description));
        return this;
    }

    /// <summary>Sets logical output dimensions.</summary>
    public VisualStory WithSize(int width, int height) {
        if (width < 480 || width > 3840) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 320 || height > 2160) throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
        return this;
    }

    /// <summary>Sets the visual-story theme.</summary>
    public VisualStory WithTheme(VisualStoryTheme theme) {
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        return this;
    }

    /// <summary>Adds a scene and returns it for panel configuration.</summary>
    public VisualStoryScene Scene(string id, string title, double durationSeconds = 2.5, VisualStorySceneLayout layout = VisualStorySceneLayout.Focus) {
        if (_scenes.Count >= 24) throw new InvalidOperationException("Visual stories support at most 24 scenes.");
        var normalizedId = VisualStorySurface.RequireText(id, nameof(id));
        if (_scenes.Any(scene => string.Equals(scene.Id, normalizedId, StringComparison.Ordinal))) throw new ArgumentException("Scene identifiers must be unique.", nameof(id));
        var scene = new VisualStoryScene(normalizedId, title, durationSeconds, layout);
        _scenes.Add(scene);
        return scene;
    }

    /// <summary>Declares an outcome that must be visible in the completed scene.</summary>
    public VisualStory Outcome(string id, string label, string completedPanelId) {
        if (_outcomes.Count >= 12) throw new InvalidOperationException("Visual stories support at most 12 declared outcomes.");
        var normalizedId = VisualStorySurface.RequireText(id, nameof(id));
        if (_outcomes.Any(outcome => string.Equals(outcome.Id, normalizedId, StringComparison.Ordinal))) throw new ArgumentException("Outcome identifiers must be unique.", nameof(id));
        _outcomes.Add(new VisualStoryOutcome(normalizedId, label, completedPanelId));
        return this;
    }

    internal double DurationSeconds => _scenes.Sum(scene => scene.DurationSeconds);

    internal void Validate() {
        if (_scenes.Count == 0) throw new InvalidOperationException("Visual stories require at least one scene.");
        if (_outcomes.Count == 0) throw new InvalidOperationException("Visual stories must declare at least one completed outcome.");
        if (string.IsNullOrWhiteSpace(Theme.FontFamily) || string.IsNullOrWhiteSpace(Theme.MonospaceFontFamily)) throw new InvalidOperationException("Visual-story themes require font families.");
        if (Theme.Syntax == null) throw new InvalidOperationException("Visual-story themes require a syntax palette.");
        foreach (var scene in _scenes) {
            scene.Validate();
            var bounds = VisualStoryLayout.Panels(this, scene);
            for (var index = 0; index < scene.Panels.Count; index++) {
                VisualStoryLayout.PanelContent(scene.Panels[index], bounds[index]);
            }
        }
        var completed = _scenes[_scenes.Count - 1];
        foreach (var outcome in _outcomes) {
            if (!completed.Panels.Any(panel => string.Equals(panel.Id, outcome.PanelId, StringComparison.Ordinal))) {
                throw new InvalidOperationException("Completed scene '" + completed.Id + "' does not reveal promised outcome panel '" + outcome.PanelId + "'.");
            }
        }
    }
}
