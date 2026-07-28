using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Terminal;

/// <summary>
/// Specifies the prompt dialect presented by a terminal story.
/// </summary>
public enum TerminalDialect {
    /// <summary>PowerShell prompt.</summary>
    PowerShell,
    /// <summary>POSIX shell prompt.</summary>
    Bash,
    /// <summary>Windows command prompt.</summary>
    CommandPrompt,
    /// <summary>Python interactive prompt.</summary>
    Python,
    /// <summary>C# interactive prompt.</summary>
    CSharp,
    /// <summary>Caller-defined prompt.</summary>
    Custom
}

/// <summary>
/// Specifies the semantic color of terminal output.
/// </summary>
public enum TerminalTextTone {
    /// <summary>Normal output.</summary>
    Default,
    /// <summary>Subdued output.</summary>
    Muted,
    /// <summary>Accent output.</summary>
    Accent,
    /// <summary>Successful output.</summary>
    Success,
    /// <summary>Warning output.</summary>
    Warning,
    /// <summary>Error output.</summary>
    Error
}

/// <summary>
/// Specifies one terminal story step.
/// </summary>
public enum TerminalStoryStepKind {
    /// <summary>A typed command.</summary>
    Command,
    /// <summary>One or more output lines.</summary>
    Output,
    /// <summary>A blank line.</summary>
    Blank,
    /// <summary>A silent timeline pause.</summary>
    Pause,
    /// <summary>A formatted table.</summary>
    Table
}

/// <summary>
/// Represents one typed terminal command, output block, pause, blank line, or table.
/// </summary>
public sealed class TerminalStoryStep {
    internal TerminalStoryStep(TerminalStoryStepKind kind, string text, TerminalTextTone tone, double durationSeconds, TerminalTable? table) {
        Kind = kind;
        Text = text;
        Tone = tone;
        DurationSeconds = durationSeconds;
        Table = table;
    }

    /// <summary>Gets the step kind.</summary>
    public TerminalStoryStepKind Kind { get; }

    /// <summary>Gets the command or output text.</summary>
    public string Text { get; }

    /// <summary>Gets the semantic output tone.</summary>
    public TerminalTextTone Tone { get; }

    /// <summary>Gets an explicit duration, or zero for automatic timing.</summary>
    public double DurationSeconds { get; }

    /// <summary>Gets the table payload, when the step is a table.</summary>
    public TerminalTable? Table { get; }
}

/// <summary>
/// Models a deterministic, script-free animated terminal presentation.
/// </summary>
public sealed class TerminalStory {
    private readonly List<TerminalStoryStep> _steps = new();

    /// <summary>Gets the configured steps.</summary>
    public IReadOnlyList<TerminalStoryStep> Steps => _steps;

    /// <summary>Gets the terminal theme.</summary>
    public TerminalTheme Theme { get; private set; } = TerminalTheme.WindowsTerminal();

    /// <summary>Gets the title-bar text.</summary>
    public string Title { get; private set; } = "PowerShell";

    /// <summary>Gets the working directory shown in prompts.</summary>
    public string WorkingDirectory { get; private set; } = @"C:\";

    /// <summary>Gets the prompt dialect.</summary>
    public TerminalDialect Dialect { get; private set; } = TerminalDialect.PowerShell;

    /// <summary>Gets the explicit custom prompt, when configured.</summary>
    public string CustomPrompt { get; private set; } = string.Empty;

    /// <summary>Gets the logical SVG width.</summary>
    public int Width { get; private set; } = 886;

    /// <summary>Gets the terminal font size.</summary>
    public double FontSize { get; private set; } = 14;

    /// <summary>Gets the terminal line height.</summary>
    public double LineHeight { get; private set; } = 22;

    /// <summary>Gets the initial animation delay.</summary>
    public double InitialDelaySeconds { get; private set; } = 0.35;

    /// <summary>Gets the simulated typing speed.</summary>
    public double CharactersPerSecond { get; private set; } = 42;

    /// <summary>Gets the delay between output lines.</summary>
    public double LineDelaySeconds { get; private set; } = 0.08;

    /// <summary>Gets whether a final prompt and cursor are shown.</summary>
    public bool ShowFinalPrompt { get; private set; } = true;

    /// <summary>Gets the PNG output scale.</summary>
    public int PngOutputScale { get; private set; } = 2;

    /// <summary>Creates an empty terminal story.</summary>
    public static TerminalStory Create() => new();

    /// <summary>Sets the terminal title.</summary>
    public TerminalStory WithTitle(string title) {
        Title = OneLine(title, nameof(title), allowEmpty: false);
        return this;
    }

    /// <summary>Sets the prompt dialect and optional custom prompt.</summary>
    public TerminalStory WithDialect(TerminalDialect dialect, string? customPrompt = null) {
        ValidateEnum(dialect, nameof(dialect));
        var normalizedPrompt = customPrompt == null ? string.Empty : OneLine(customPrompt, nameof(customPrompt), allowEmpty: false);
        if (dialect == TerminalDialect.Custom && normalizedPrompt.Length == 0) throw new ArgumentException("Custom terminal dialects require a prompt.", nameof(customPrompt));
        Dialect = dialect;
        CustomPrompt = normalizedPrompt;
        return this;
    }

    /// <summary>Sets the working directory used by shell prompts.</summary>
    public TerminalStory WithWorkingDirectory(string workingDirectory) {
        WorkingDirectory = OneLine(workingDirectory, nameof(workingDirectory), allowEmpty: false);
        return this;
    }

    /// <summary>Sets the terminal theme.</summary>
    public TerminalStory WithTheme(TerminalTheme theme) {
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        return this;
    }

    /// <summary>Sets the logical SVG width.</summary>
    public TerminalStory WithWidth(int width) {
        if (width < 480 || width > 1800) throw new ArgumentOutOfRangeException(nameof(width), "Terminal story width must be between 480 and 1800.");
        Width = width;
        return this;
    }

    /// <summary>Sets terminal typography.</summary>
    public TerminalStory WithTypography(double fontSize, double lineHeight) {
        FiniteRange(fontSize, 10, 24, nameof(fontSize));
        FiniteRange(lineHeight, fontSize + 3, 40, nameof(lineHeight));
        FontSize = fontSize;
        LineHeight = lineHeight;
        return this;
    }

    /// <summary>Sets automatic animation timing.</summary>
    public TerminalStory WithTiming(double initialDelaySeconds, double charactersPerSecond, double lineDelaySeconds) {
        FiniteRange(initialDelaySeconds, 0, 10, nameof(initialDelaySeconds));
        FiniteRange(charactersPerSecond, 5, 200, nameof(charactersPerSecond));
        FiniteRange(lineDelaySeconds, 0, 3, nameof(lineDelaySeconds));
        InitialDelaySeconds = initialDelaySeconds;
        CharactersPerSecond = charactersPerSecond;
        LineDelaySeconds = lineDelaySeconds;
        return this;
    }

    /// <summary>Configures whether the completed story shows a final prompt.</summary>
    public TerminalStory WithFinalPrompt(bool visible = true) {
        ShowFinalPrompt = visible;
        return this;
    }

    /// <summary>Sets the PNG output density multiplier.</summary>
    public TerminalStory WithPngOutputScale(int scale) {
        if (scale < 1 || scale > 4) throw new ArgumentOutOfRangeException(nameof(scale));
        PngOutputScale = scale;
        return this;
    }

    /// <summary>Adds a typed command.</summary>
    public TerminalStory Command(string command, double durationSeconds = 0) {
        return Add(new TerminalStoryStep(TerminalStoryStepKind.Command, OneLine(command, nameof(command), allowEmpty: false), TerminalTextTone.Default, Duration(durationSeconds), null));
    }

    /// <summary>Adds one or more output lines.</summary>
    public TerminalStory Output(string text, TerminalTextTone tone = TerminalTextTone.Default) {
        ValidateEnum(tone, nameof(tone));
        if (text == null) throw new ArgumentNullException(nameof(text));
        return Add(new TerminalStoryStep(TerminalStoryStepKind.Output, TerminalTextSanitizer.Transcript(text), tone, 0, null));
    }

    /// <summary>Adds captured transcript lines.</summary>
    public TerminalStory Transcript(IEnumerable<string> lines, TerminalTextTone tone = TerminalTextTone.Default) {
        if (lines == null) throw new ArgumentNullException(nameof(lines));
        foreach (var line in lines) Output(line ?? string.Empty, tone);
        return this;
    }

    /// <summary>Adds a blank output line.</summary>
    public TerminalStory Blank() => Add(new TerminalStoryStep(TerminalStoryStepKind.Blank, string.Empty, TerminalTextTone.Default, 0, null));

    /// <summary>Adds a silent pause to the animation timeline.</summary>
    public TerminalStory Pause(double seconds) {
        FiniteRange(seconds, 0.01, 10, nameof(seconds));
        return Add(new TerminalStoryStep(TerminalStoryStepKind.Pause, string.Empty, TerminalTextTone.Default, seconds, null));
    }

    /// <summary>Adds a formatted terminal table.</summary>
    public TerminalStory Table(TerminalTable table) {
        if (table == null) throw new ArgumentNullException(nameof(table));
        table.Validate();
        return Add(new TerminalStoryStep(TerminalStoryStepKind.Table, string.Empty, TerminalTextTone.Default, 0, table));
    }

    /// <summary>Adds a compact text progress bar.</summary>
    public TerminalStory Progress(string label, double fraction, int width = 22) {
        if (double.IsNaN(fraction) || double.IsInfinity(fraction) || fraction < 0 || fraction > 1) throw new ArgumentOutOfRangeException(nameof(fraction));
        if (width < 8 || width > 60) throw new ArgumentOutOfRangeException(nameof(width));
        var filled = (int)Math.Round(width * fraction);
        var bar = new string('#', filled) + new string('-', width - filled);
        return Output("[" + bar + "] " + (fraction * 100).ToString("0", CultureInfo.InvariantCulture) + "%  " + OneLine(label, nameof(label), false), TerminalTextTone.Success);
    }

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

    internal void Validate() {
        if (_steps.Count == 0) throw new InvalidOperationException("Terminal stories require at least one command, output line, table, or pause.");
        if (_steps.Count > 120) throw new InvalidOperationException("Terminal stories support at most 120 steps.");
        if (string.IsNullOrWhiteSpace(Theme.FontFamily)) throw new InvalidOperationException("Terminal themes require a font family.");
        if (Dialect == TerminalDialect.Custom && CustomPrompt.Length == 0) throw new InvalidOperationException("Custom terminal dialects require a prompt.");
    }

    private TerminalStory Add(TerminalStoryStep step) {
        if (_steps.Count >= 120) throw new InvalidOperationException("Terminal stories support at most 120 steps.");
        _steps.Add(step);
        return this;
    }

    private static double Duration(double value) {
        if (value == 0) return 0;
        FiniteRange(value, 0.05, 20, nameof(value));
        return value;
    }

    private static string OneLine(string value, string name, bool allowEmpty) {
        return TerminalTextSanitizer.OneLine(value, name, " ", allowEmpty);
    }

    private static void FiniteRange(double value, double minimum, double maximum, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateEnum<T>(T value, string name) where T : struct {
        if (!Enum.IsDefined(typeof(T), value)) throw new ArgumentOutOfRangeException(name);
    }
}
