using ChartForgeX.Themes;
using System;

namespace ChartForgeX.Terminal;

/// <summary>The icon keys a terminal tab draws. <c>ubuntu</c> draws the Ubuntu mark, <see cref="Icon.None"/> draws
/// nothing, and every other key draws the terminal prompt mark.</summary>
public static class TerminalTabIcons {
    /// <summary>Generic terminal: <c>terminal</c>.</summary>
    public static Icon Terminal { get; } = Icon.Named("terminal");
    /// <summary>PowerShell: <c>powershell</c>.</summary>
    public static Icon PowerShell { get; } = Icon.Named("powershell");
    /// <summary>Windows PowerShell: <c>windows-powershell</c>.</summary>
    public static Icon WindowsPowerShell { get; } = Icon.Named("windows-powershell");
    /// <summary>POSIX shell: <c>bash</c>.</summary>
    public static Icon Bash { get; } = Icon.Named("bash");
    /// <summary>Ubuntu: <c>ubuntu</c>.</summary>
    public static Icon Ubuntu { get; } = Icon.Named("ubuntu");
    /// <summary>Windows command prompt: <c>command-prompt</c>.</summary>
    public static Icon CommandPrompt { get; } = Icon.Named("command-prompt");
}

/// <summary>
/// Defines one persistent terminal session in a terminal story.
/// </summary>
public sealed class TerminalTab {
    internal TerminalTab(
        string id,
        string title,
        TerminalDialect dialect,
        string workingDirectory,
        string customPrompt,
        VisualTheme theme,
        Icon icon, ThemeMode themeMode = ThemeMode.Dark, TerminalStyleOptions? style = null) {
        Id = id;
        Title = title;
        Dialect = dialect;
        WorkingDirectory = workingDirectory;
        CustomPrompt = customPrompt;
        Theme = theme ?? throw new System.ArgumentNullException(nameof(theme));
        ThemeMode = themeMode; Style = style?.Clone() ?? new TerminalStyleOptions();
        Icon = icon;
    }

    /// <summary>Gets the stable identifier used by tab-selection steps.</summary>
    public string Id { get; }

    /// <summary>Gets the visible tab title.</summary>
    public string Title { get; internal set; }

    /// <summary>Gets the prompt dialect used by this tab.</summary>
    public TerminalDialect Dialect { get; internal set; }

    /// <summary>Gets the working directory shown by this tab's prompt.</summary>
    public string WorkingDirectory { get; internal set; }

    /// <summary>Gets the caller-defined prompt used by a custom dialect.</summary>
    public string CustomPrompt { get; internal set; }

    /// <summary>Gets the independent color palette used by this tab.</summary>
    public VisualTheme Theme { get; internal set; }
    private ThemeMode _themeMode;
    /// <summary>Gets the explicit appearance of this tab.</summary>
    public ThemeMode ThemeMode { get => _themeMode; internal set { if (!System.Enum.IsDefined(typeof(ThemeMode), value)) throw new System.ArgumentOutOfRangeException(nameof(value)); _themeMode = value; } }
    /// <summary>Gets local title-bar and cursor treatments.</summary>
    public TerminalStyleOptions Style { get; internal set; }
    internal ResolvedTerminalStyle ResolvedStyle => TerminalThemeProjection.Resolve(Theme, ThemeMode, Style);

    /// <summary>Gets the semantic icon shown in tab-aware window chrome.</summary>
    public Icon Icon { get; internal set; }

    internal string Prompt() {
        switch (Dialect) {
            case TerminalDialect.PowerShell: return "PS " + WorkingDirectory + "> ";
            case TerminalDialect.Bash: return WorkingDirectory + " $ ";
            case TerminalDialect.CommandPrompt: return WorkingDirectory + "> ";
            case TerminalDialect.Python: return ">>> ";
            case TerminalDialect.CSharp: return "> ";
            case TerminalDialect.Custom: return CustomPrompt;
            default: throw new InvalidOperationException("Unknown terminal dialect.");
        }
    }
}
