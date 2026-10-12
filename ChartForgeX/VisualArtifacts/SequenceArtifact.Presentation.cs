using System;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class SequenceArtifact {
    private VisualTheme _theme = VisualTheme.Graphite();
    private VisualThemeMode _themeMode = VisualThemeMode.Light;

    /// <summary>Gets or sets the paired theme used by convenience SVG, PNG and HTML exports.</summary>
    /// <remarks>An explicit <see cref="Rendering.VisualRenderContext"/> supplied to <see cref="Prepare"/> takes precedence over these authored defaults.</remarks>
    public VisualTheme Theme { get => _theme; set => _theme = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the palette mode used by convenience exports.</summary>
    public VisualThemeMode ThemeMode {
        get => _themeMode;
        set {
            if (!Enum.IsDefined(typeof(VisualThemeMode), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _themeMode = value;
        }
    }
}
