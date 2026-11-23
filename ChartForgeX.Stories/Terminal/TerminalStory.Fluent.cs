using ChartForgeX.Themes;
using PowerForge.Fluent;
using System.Collections.Generic;
using System.Globalization;
using System;

namespace ChartForgeX.Terminal;

/// <summary>Hand-written fluent methods of <see cref="TerminalStory"/> that do more than set one property.</summary>
public static partial class TerminalStoryFluent {
    /// <summary>Sets the prompt dialect and optional custom prompt.</summary>
    public static global::ChartForgeX.Terminal.TerminalStory Dialect(this global::ChartForgeX.Terminal.TerminalStory terminalStory, global::ChartForgeX.Terminal.TerminalDialect dialect, string? customPrompt = null, global::ChartForgeX.ThemeMode themeMode = global::ChartForgeX.ThemeMode.Dark, global::ChartForgeX.Terminal.TerminalStyleOptions? style = null)
        => (terminalStory ?? throw new System.ArgumentNullException(nameof(terminalStory))).SetDialect(dialect, customPrompt, themeMode, style);
}
