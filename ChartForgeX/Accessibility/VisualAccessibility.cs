using System;

namespace ChartForgeX.Accessibility;

/// <summary>
/// Defines the renderer-independent text alternative and language metadata for a visual.
/// </summary>
public sealed class VisualAccessibility {
    private string? _name;
    private string? _description;
    private string? _language;

    /// <summary>Gets or sets the concise accessible name. Renderers use a visual-specific fallback when omitted.</summary>
    public string? Name { get => _name; set => _name = OptionalText(value); }

    /// <summary>Gets or sets the longer accessible description. Renderers can derive a data summary when omitted.</summary>
    public string? Description { get => _description; set => _description = OptionalText(value); }

    /// <summary>Gets or sets the optional BCP 47 language tag emitted by capable renderers.</summary>
    public string? Language { get => _language; set => _language = OptionalText(value); }

    /// <summary>Gets or sets a value indicating that the visual is decorative and should be hidden from assistive technology.</summary>
    public bool IsDecorative { get; set; }

    /// <summary>Creates an independent copy of this accessibility metadata.</summary>
    public VisualAccessibility Clone() => new() {
        Name = Name,
        Description = Description,
        Language = Language,
        IsDecorative = IsDecorative
    };

    /// <summary>Sets the accessible name and optional description and language.</summary>
    public VisualAccessibility WithTextAlternative(string name, string? description = null, string? language = null) {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Accessible name must not be empty.", nameof(name));
        Name = name;
        Description = description;
        Language = language;
        IsDecorative = false;
        return this;
    }

    /// <summary>Marks the visual as decorative.</summary>
    public VisualAccessibility AsDecorative(bool decorative = true) {
        IsDecorative = decorative;
        return this;
    }

    private static string? OptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
}
