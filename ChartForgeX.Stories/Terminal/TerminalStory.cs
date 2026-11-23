using ChartForgeX.Themes;
using System;
using System.Collections.Generic;
using System.Globalization;
using PowerForge.Fluent;

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
/// Specifies a reusable terminal-story playback pace.
/// </summary>
public enum TerminalStoryPlaybackSpeed {
    /// <summary>Leaves extra time for reading commands, output, and tab contents.</summary>
    Slow,
    /// <summary>Uses balanced presentation timing.</summary>
    Normal,
    /// <summary>Uses compact timing for short demonstrations.</summary>
    Fast
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
    Table,
    /// <summary>Declares a persistent terminal tab without activating it.</summary>
    DeclareTab,
    /// <summary>Declares and activates a persistent terminal tab.</summary>
    OpenTab,
    /// <summary>Activates a previously declared terminal tab.</summary>
    SelectTab
}

/// <summary>
/// Represents one typed terminal command, output block, pause, blank line, or table.
/// </summary>
public sealed class TerminalStoryStep {
    internal TerminalStoryStep(TerminalStoryStepKind kind, string text, Tone tone, double durationSeconds, TerminalTable? table, string tabId, TerminalTab? tab = null) {
        Kind = kind;
        Text = text;
        Tone = tone;
        DurationSeconds = durationSeconds;
        Table = table;
        TabId = tabId;
        Tab = tab;
    }

    /// <summary>Gets the step kind.</summary>
    public TerminalStoryStepKind Kind { get; }

    /// <summary>Gets the command or output text.</summary>
    public string Text { get; }

    /// <summary>Gets the semantic output tone.</summary>
    public Tone Tone { get; }

    /// <summary>Gets an explicit duration, or zero for automatic timing.</summary>
    public double DurationSeconds { get; }

    /// <summary>Gets the table payload, when the step is a table.</summary>
    public TerminalTable? Table { get; }

    /// <summary>Gets the persistent terminal tab affected by the step.</summary>
    public string TabId { get; }

    /// <summary>Gets the declared tab payload, when the step opens a tab.</summary>
    public TerminalTab? Tab { get; }
}

/// <summary>
/// Models a deterministic, script-free animated terminal presentation.
/// </summary>
[Fluent]
public sealed partial class TerminalStory {
    private const int MaximumTabTitleLength = 256;
    private const string DefaultTabId = "main";
    private readonly List<TerminalStoryStep> _steps = new();
    private readonly List<TerminalTab> _tabs = new();
    private readonly Dictionary<string, TerminalTab> _tabsById = new(StringComparer.OrdinalIgnoreCase);
    private string _activeTabId = DefaultTabId;

    private TerminalStory() {
        var initialTab = new TerminalTab(
            DefaultTabId,
            "PowerShell",
            TerminalDialect.PowerShell,
            @"C:\",
            string.Empty,
            TerminalThemes.Default(),
            TerminalTabIcons.PowerShell);
        _tabs.Add(initialTab);
        _tabsById.Add(initialTab.Id, initialTab);
    }

    /// <summary>Gets the configured steps.</summary>
    public IReadOnlyList<TerminalStoryStep> Steps => _steps;

    /// <summary>Gets the persistent terminal tabs in display order.</summary>
    public IReadOnlyList<TerminalTab> Tabs => _tabs;

    /// <summary>Gets the tab that will be active when the completed story is shown.</summary>
    public string ActiveTabId => _activeTabId;

    /// <summary>Gets or sets the initial-tab terminal theme. Set it before timeline authoring.</summary>
    public VisualTheme Theme {
        get => FirstTab.Theme;
        set {
            EnsureInitialConfiguration();
            FirstTab.Theme = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
    /// <summary>Gets or sets the explicit initial-tab light or dark appearance. Set it before timeline authoring.</summary>
    public ThemeMode ThemeMode {
        get => FirstTab.ThemeMode;
        set {
            EnsureInitialConfiguration();
            FirstTab.ThemeMode = value;
        }
    }
    /// <summary>Gets the initial-tab cursor and title-bar styling.</summary>
    /// <remarks>Setting the style stores a copy and must happen before timeline authoring.</remarks>
    public TerminalStyleOptions Style {
        get => FirstTab.Style;
        set {
            EnsureInitialConfiguration();
            FirstTab.Style = (value ?? throw new ArgumentNullException(nameof(value))).Clone();
        }
    }
    internal ResolvedTerminalStyle ResolvedStyle => FirstTab.ResolvedStyle;

    private TerminalWindowStyle _windowStyle = TerminalWindowStyle.MacOS;
    private int _width = 886;
    private double _initialDelaySeconds = 0.35;
    private double _charactersPerSecond = 42;
    private double _lineDelaySeconds = 0.08;
    private double _tabHoldSeconds = 1.2;
    private int _pngOutputScale = 2;

    /// <summary>Gets or sets the visible terminal window chrome independently of colors and command syntax.</summary>
    public TerminalWindowStyle WindowStyle {
        get => _windowStyle;
        set {
            TerminalWindowChrome.Validate(value);
            _windowStyle = value;
        }
    }

    /// <summary>Gets or sets the initial-tab title-bar text. Set it before timeline authoring.</summary>
    public string Title {
        get => FirstTab.Title;
        set {
            EnsureInitialConfiguration();
            FirstTab.Title = TabTitle(value, nameof(value));
        }
    }

    /// <summary>Gets or sets the working directory shown in initial-tab prompts. Set it before timeline authoring.</summary>
    public string WorkingDirectory {
        get => FirstTab.WorkingDirectory;
        set {
            EnsureInitialConfiguration();
            FirstTab.WorkingDirectory = OneLine(value, nameof(value), allowEmpty: false);
        }
    }

    /// <summary>Gets the prompt dialect.</summary>
    public TerminalDialect Dialect => FirstTab.Dialect;

    /// <summary>Gets the explicit custom prompt, when configured.</summary>
    public string CustomPrompt => FirstTab.CustomPrompt;

    /// <summary>Gets or sets the logical SVG width, from 480 to 1800.</summary>
    public int Width {
        get => _width;
        set {
            if (value < 480 || value > 1800) throw new ArgumentOutOfRangeException(nameof(value), "Terminal story width must be between 480 and 1800.");
            _width = value;
        }
    }

    /// <summary>Gets the terminal font size.</summary>
    public double FontSize { get; private set; } = 14;

    /// <summary>Gets the terminal line height.</summary>
    public double LineHeight { get; private set; } = 16.8;

    /// <summary>Gets or sets the initial animation delay, from 0 to 10 seconds.</summary>
    public double InitialDelaySeconds {
        get => _initialDelaySeconds;
        set {
            FiniteRange(value, 0, 10, nameof(value));
            _initialDelaySeconds = value;
        }
    }

    /// <summary>Gets or sets the simulated typing speed, from 5 to 200 characters per second.</summary>
    public double CharactersPerSecond {
        get => _charactersPerSecond;
        set {
            FiniteRange(value, 5, 200, nameof(value));
            _charactersPerSecond = value;
        }
    }

    /// <summary>Gets or sets the delay between output lines, from 0 to 3 seconds.</summary>
    public double LineDelaySeconds {
        get => _lineDelaySeconds;
        set {
            FiniteRange(value, 0, 3, nameof(value));
            _lineDelaySeconds = value;
        }
    }

    /// <summary>Gets or sets the minimum reading time (0 to 10 seconds) after new content appears and before the active tab changes.</summary>
    public double TabHoldSeconds {
        get => _tabHoldSeconds;
        set {
            FiniteRange(value, 0, 10, nameof(value));
            _tabHoldSeconds = value;
        }
    }

    /// <summary>Gets or sets whether the completed story shows a final prompt and cursor.</summary>
    public bool ShowFinalPrompt { get; set; } = true;

    /// <summary>Gets or sets the PNG output density multiplier, from 1 to 4.</summary>
    public int PngOutputScale {
        get => _pngOutputScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value));
            _pngOutputScale = value;
        }
    }

    /// <summary>Creates an empty terminal story.</summary>
    public static TerminalStory Create() => new();

    /// <summary>Sets the prompt dialect and optional custom prompt.</summary>
    internal TerminalStory SetDialect(TerminalDialect dialect, string? customPrompt = null, ThemeMode themeMode = ThemeMode.Dark, TerminalStyleOptions? style = null) {
        EnsureInitialConfiguration();
        ValidateEnum(dialect, nameof(dialect));
        var normalizedPrompt = customPrompt == null ? string.Empty : OneLine(customPrompt, nameof(customPrompt), allowEmpty: false);
        if (dialect == TerminalDialect.Custom && normalizedPrompt.Length == 0) throw new ArgumentException("Custom terminal dialects require a prompt.", nameof(customPrompt));
        FirstTab.Dialect = dialect;
        FirstTab.CustomPrompt = normalizedPrompt;
        return this;
    }

    /// <summary>Sets terminal typography.</summary>
    public TerminalStory Typography(double fontSize, double lineHeight) {
        FiniteRange(fontSize, 10, 24, nameof(fontSize));
        FiniteRange(lineHeight, fontSize * 0.8, 40, nameof(lineHeight));
        FontSize = fontSize;
        LineHeight = lineHeight;
        return this;
    }

    /// <summary>Applies a reusable playback pace for typing, output reveals, and tab reading time.</summary>
    public TerminalStory PlaybackSpeed(TerminalStoryPlaybackSpeed speed) {
        ValidateEnum(speed, nameof(speed));
        switch (speed) {
            case TerminalStoryPlaybackSpeed.Slow:
                InitialDelaySeconds = 0.6;
                CharactersPerSecond = 28;
                LineDelaySeconds = 0.14;
                TabHoldSeconds = 2;
                break;
            case TerminalStoryPlaybackSpeed.Normal:
                InitialDelaySeconds = 0.35;
                CharactersPerSecond = 42;
                LineDelaySeconds = 0.08;
                TabHoldSeconds = 1.2;
                break;
            case TerminalStoryPlaybackSpeed.Fast:
                InitialDelaySeconds = 0.15;
                CharactersPerSecond = 72;
                LineDelaySeconds = 0.04;
                TabHoldSeconds = 0.55;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(speed));
        }
        return this;
    }

    /// <summary>Calculates automatic typing time: visible text elements divided by characters per second, clamped to 0.35–4.5 seconds.</summary>
    /// <remarks>The prompt is excluded. An explicit positive duration passed to Command overrides this calculation.</remarks>
    public double GetCommandDurationSeconds(string command) {
        if (command == null) throw new ArgumentNullException(nameof(command));
        return Math.Max(0.35, Math.Min(4.5, TerminalStoryLayout.VisibleTextElementCount(command) / CharactersPerSecond));
    }

    /// <summary>Adds a typed command.</summary>
    public TerminalStory Command(string command, double durationSeconds = 0) {
        return Add(new TerminalStoryStep(TerminalStoryStepKind.Command, OneLine(command, nameof(command), allowEmpty: false), Tone.None, Duration(durationSeconds), null, _activeTabId));
    }

    /// <summary>Adds one or more output lines.</summary>
    public TerminalStory Output(string text, Tone? tone = null) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        return Add(new TerminalStoryStep(TerminalStoryStepKind.Output, TerminalTextSanitizer.Transcript(text), tone ?? Tone.None, 0, null, _activeTabId));
    }

    /// <summary>Adds captured transcript lines.</summary>
    public TerminalStory Transcript(IEnumerable<string> lines, Tone? tone = null) {
        if (lines == null) throw new ArgumentNullException(nameof(lines));
        var prepared = new List<TerminalStoryStep>();
        foreach (var line in lines) {
            if (_steps.Count + prepared.Count >= 120) {
                throw new InvalidOperationException("Terminal stories support at most 120 steps.");
            }
            prepared.Add(new TerminalStoryStep(
                TerminalStoryStepKind.Output,
                TerminalTextSanitizer.Transcript(line ?? string.Empty),
                tone ?? Tone.None,
                0,
                null,
                _activeTabId));
        }
        _steps.AddRange(prepared);
        return this;
    }

    /// <summary>Adds a blank output line.</summary>
    public TerminalStory Blank() => Add(new TerminalStoryStep(TerminalStoryStepKind.Blank, string.Empty, Tone.None, 0, null, _activeTabId));

    /// <summary>Adds a silent pause to the animation timeline.</summary>
    public TerminalStory Pause(double seconds) {
        FiniteRange(seconds, 0.01, 10, nameof(seconds));
        return Add(new TerminalStoryStep(TerminalStoryStepKind.Pause, string.Empty, Tone.None, seconds, null, _activeTabId));
    }

    /// <summary>Adds a formatted terminal table.</summary>
    public TerminalStory Table(TerminalTable table) {
        if (table == null) throw new ArgumentNullException(nameof(table));
        table.Validate();
        return Add(new TerminalStoryStep(TerminalStoryStepKind.Table, string.Empty, Tone.None, 0, table, _activeTabId));
    }

    /// <summary>Defines the initial persistent terminal tab before timeline authoring begins.</summary>
    public TerminalStory InitialTab(
        string id,
        string title,
        TerminalDialect dialect,
        string workingDirectory,
        VisualTheme theme,
        Icon? icon = null,
        string? customPrompt = null, ThemeMode themeMode = ThemeMode.Dark, TerminalStyleOptions? style = null) {
        EnsureInitialConfiguration();
        var tab = CreateTab(id, title, dialect, workingDirectory, theme, icon, customPrompt, themeMode, style);
        _tabs[0] = tab;
        _tabsById.Clear();
        _tabsById.Add(tab.Id, tab);
        _activeTabId = tab.Id;
        return this;
    }

    /// <summary>Declares a persistent terminal tab without changing the active tab.</summary>
    public TerminalStory DeclareTab(
        string id,
        string title,
        TerminalDialect dialect,
        string workingDirectory,
        VisualTheme theme,
        Icon? icon = null,
        string? customPrompt = null, ThemeMode themeMode = ThemeMode.Dark, TerminalStyleOptions? style = null) {
        EnsureStepCapacity();
        var tab = CreateTab(id, title, dialect, workingDirectory, theme, icon, customPrompt, themeMode, style);
        if (_tabsById.ContainsKey(tab.Id)) throw new ArgumentException("Terminal tab identifiers must be unique.", nameof(id));
        if (_tabs.Count >= 8) throw new InvalidOperationException("Terminal stories support at most eight tabs.");
        _tabs.Add(tab);
        _tabsById.Add(tab.Id, tab);
        return Add(new TerminalStoryStep(TerminalStoryStepKind.DeclareTab, string.Empty, Tone.None, 0, null, tab.Id, tab));
    }

    /// <summary>Declares and activates a persistent terminal tab.</summary>
    public TerminalStory OpenTab(
        string id,
        string title,
        TerminalDialect dialect,
        string workingDirectory,
        VisualTheme theme,
        Icon? icon = null,
        string? customPrompt = null,
        double transitionSeconds = 0.2, ThemeMode themeMode = ThemeMode.Dark, TerminalStyleOptions? style = null) {
        EnsureStepCapacity();
        var transitionDuration = TransitionDuration(transitionSeconds);
        var tab = CreateTab(id, title, dialect, workingDirectory, theme, icon, customPrompt, themeMode, style);
        if (_tabsById.ContainsKey(tab.Id)) throw new ArgumentException("Terminal tab identifiers must be unique.", nameof(id));
        if (_tabs.Count >= 8) throw new InvalidOperationException("Terminal stories support at most eight tabs.");
        _tabs.Add(tab);
        _tabsById.Add(tab.Id, tab);
        _activeTabId = tab.Id;
        return Add(new TerminalStoryStep(TerminalStoryStepKind.OpenTab, string.Empty, Tone.None, transitionDuration, null, tab.Id, tab));
    }

    /// <summary>Activates a previously declared terminal tab without clearing its buffer.</summary>
    public TerminalStory SelectTab(string id, double transitionSeconds = 0.2, ThemeMode themeMode = ThemeMode.Dark, TerminalStyleOptions? style = null) {
        EnsureStepCapacity();
        var transitionDuration = TransitionDuration(transitionSeconds);
        var normalizedId = TabId(id, nameof(id));
        if (!_tabsById.ContainsKey(normalizedId)) throw new ArgumentException("The selected terminal tab has not been declared.", nameof(id));
        if (string.Equals(_activeTabId, normalizedId, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The selected terminal tab is already active.");
        _activeTabId = _tabsById[normalizedId].Id;
        return Add(new TerminalStoryStep(TerminalStoryStepKind.SelectTab, string.Empty, Tone.None, transitionDuration, null, _activeTabId));
    }

    /// <summary>Adds a compact text progress bar.</summary>
    public TerminalStory Progress(string label, double fraction, int width = 22) {
        if (double.IsNaN(fraction) || double.IsInfinity(fraction) || fraction < 0 || fraction > 1) throw new ArgumentOutOfRangeException(nameof(fraction));
        if (width < 8 || width > 60) throw new ArgumentOutOfRangeException(nameof(width));
        var filled = (int)Math.Round(width * fraction);
        var bar = new string('#', filled) + new string('-', width - filled);
        return Output("[" + bar + "] " + (fraction * 100).ToString("0", CultureInfo.InvariantCulture) + "%  " + OneLine(label, nameof(label), false), Tone.Success);
    }

    internal string Prompt() => FirstTab.Prompt();

    internal TerminalTab GetTab(string id) => _tabsById[id];

    internal void Validate() {
        if (_steps.Count == 0) throw new InvalidOperationException("Terminal stories require at least one command, output line, table, or pause.");
        if (_steps.Count > 120) throw new InvalidOperationException("Terminal stories support at most 120 steps.");
        foreach (var tab in _tabs) {
            if (string.IsNullOrWhiteSpace(tab.Theme.Typography.MonospaceFamily)) throw new InvalidOperationException("Terminal tab themes require a font family.");
            if (tab.Dialect == TerminalDialect.Custom && tab.CustomPrompt.Length == 0) throw new InvalidOperationException("Custom terminal dialects require a prompt.");
        }
        if (WindowStyle == TerminalWindowStyle.WindowsTerminal && _tabs.Count * 72 > Width - 230) {
            throw new InvalidOperationException("The Windows Terminal tab strip needs at least 72 pixels per tab. Increase the story width or reduce the tab count.");
        }
    }

    private TerminalStory Add(TerminalStoryStep step) {
        EnsureStepCapacity();
        _steps.Add(step);
        return this;
    }

    private void EnsureStepCapacity() {
        if (_steps.Count >= 120) throw new InvalidOperationException("Terminal stories support at most 120 steps.");
    }

    private static double Duration(double value) {
        if (value == 0) return 0;
        FiniteRange(value, 0.05, 20, nameof(value));
        return value;
    }

    private static double TransitionDuration(double value) {
        if (value == 0) return 0;
        FiniteRange(value, 0.05, 2, nameof(value));
        return value;
    }

    private static TerminalTab CreateTab(
        string id,
        string title,
        TerminalDialect dialect,
        string workingDirectory,
        VisualTheme theme,
        Icon? icon,
        string? customPrompt, ThemeMode themeMode, TerminalStyleOptions? style) {
        var normalizedId = TabId(id, nameof(id));
        ValidateEnum(dialect, nameof(dialect));
        var normalizedPrompt = customPrompt == null ? string.Empty : OneLine(customPrompt, nameof(customPrompt), allowEmpty: false);
        if (dialect == TerminalDialect.Custom && normalizedPrompt.Length == 0) throw new ArgumentException("Custom terminal dialects require a prompt.", nameof(customPrompt));
        return new TerminalTab(
            normalizedId,
            TabTitle(title, nameof(title)),
            dialect,
            OneLine(workingDirectory, nameof(workingDirectory), allowEmpty: false),
            normalizedPrompt,
            theme ?? throw new ArgumentNullException(nameof(theme)),
            icon ?? TerminalTabIcons.Terminal, themeMode, style);
    }

    private static string TabId(string value, string name) {
        var normalized = OneLine(value, name, allowEmpty: false).Trim();
        if (normalized.Length > 40) throw new ArgumentOutOfRangeException(name, "Terminal tab identifiers support at most 40 characters.");
        for (var index = 0; index < normalized.Length; index++) {
            var character = normalized[index];
            if (!(character >= 'a' && character <= 'z') &&
                !(character >= 'A' && character <= 'Z') &&
                !(character >= '0' && character <= '9') &&
                character != '-' && character != '_') {
                throw new ArgumentException("Terminal tab identifiers may contain only letters, numbers, hyphens, and underscores.", name);
            }
        }
        return normalized;
    }

    private static string TabTitle(string value, string name) {
        var normalized = OneLine(value, name, allowEmpty: false);
        if (normalized.Length > MaximumTabTitleLength) {
            throw new ArgumentOutOfRangeException(name, "Terminal tab titles support at most " + MaximumTabTitleLength + " UTF-16 code units.");
        }
        return normalized;
    }

    private TerminalTab FirstTab => _tabs[0];

    private void EnsureInitialConfiguration() {
        if (_steps.Count != 0) throw new InvalidOperationException("Configure the initial terminal tab before adding story steps.");
    }

    private static string OneLine(string value, string name, bool allowEmpty) {
        return TerminalTextSanitizer.OneLine(value, name, " ", allowEmpty);
    }

    private static void FiniteRange(double value, double minimum, double maximum, string name) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateEnum<T>(T value, string name) where T : struct, Enum {
        if (!Enum.IsDefined(typeof(T), value)) throw new ArgumentOutOfRangeException(name);
    }
}
