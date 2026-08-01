using System;

namespace ChartForgeX.Terminal;

/// <summary>
/// Specifies the semantic icon shown for a terminal tab.
/// </summary>
public enum TerminalTabIcon {
    /// <summary>Generic terminal icon.</summary>
    Terminal,
    /// <summary>Modern PowerShell icon.</summary>
    PowerShell,
    /// <summary>Windows PowerShell icon.</summary>
    WindowsPowerShell,
    /// <summary>POSIX shell icon.</summary>
    Bash,
    /// <summary>Ubuntu icon.</summary>
    Ubuntu,
    /// <summary>Windows command prompt icon.</summary>
    CommandPrompt,
    /// <summary>No icon.</summary>
    None
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
        TerminalTheme theme,
        TerminalTabIcon icon) {
        Id = id;
        Title = title;
        Dialect = dialect;
        WorkingDirectory = workingDirectory;
        CustomPrompt = customPrompt;
        Theme = theme;
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
    public TerminalTheme Theme { get; internal set; }

    /// <summary>Gets the semantic icon shown in tab-aware window chrome.</summary>
    public TerminalTabIcon Icon { get; internal set; }

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
