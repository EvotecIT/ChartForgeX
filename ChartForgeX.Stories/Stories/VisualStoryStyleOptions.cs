using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Terminal;
using ChartForgeX.Rendering;

namespace ChartForgeX.Stories;

/// <summary>Story-specific window, desktop and syntax controls. Shared colors and fonts remain in <see cref="VisualTheme"/>.</summary>
public sealed class VisualStoryStyleOptions {
    private TerminalWindowStyle _windowStyle = TerminalWindowStyle.Minimal;
    /// <summary>Chooses source/replay window chrome without changing the common palette.</summary>
    public TerminalWindowStyle WindowStyle { get => _windowStyle; set { TerminalWindowChrome.Validate(value); _windowStyle = value; } }
    /// <summary>Overrides source and replay title-bar paint.</summary>
    public ChartColor? WindowHeader { get; set; }
    /// <summary>Adds a second desktop paint for a diagonal gradient.</summary>
    public ChartColor? BackgroundEnd { get; set; }
    /// <summary>Overrides semantic source colors. Null derives colors from the shared palette, using the foreground when a syntax color lacks readable contrast.</summary>
    public StorySyntaxPalette? Syntax { get; set; }
    /// <summary>Creates independent local controls and syntax colors.</summary>
    public VisualStoryStyleOptions Clone() => new() { WindowStyle = WindowStyle, WindowHeader = WindowHeader, BackgroundEnd = BackgroundEnd, Syntax = Syntax?.Clone() };
    /// <summary>Creates traffic-light window chrome and a violet desktop gradient.</summary>
    public static VisualStoryStyleOptions MacOS(bool light = false) => FromRecipe(ResolvedVisualStoryStyle.MacOS(light));
    /// <summary>Creates tab-aware window chrome and a blue desktop gradient.</summary>
    public static VisualStoryStyleOptions Windows(bool light = false) => FromRecipe(ResolvedVisualStoryStyle.Windows(light));
    /// <summary>Creates right-hand window chrome and an aubergine desktop gradient.</summary>
    public static VisualStoryStyleOptions Linux(bool light = false) => FromRecipe(ResolvedVisualStoryStyle.Linux(light));
    private static VisualStoryStyleOptions FromRecipe(ResolvedVisualStoryStyle recipe) => new() {
        WindowStyle = recipe.WindowStyle, WindowHeader = recipe.WindowHeader,
        BackgroundEnd = recipe.BackgroundEnd, Syntax = recipe.Syntax.Clone()
    };
}

/// <summary>Story palette recipes expressed as shared immutable light/dark theme pairs.</summary>
public static class VisualStoryThemes {
    /// <summary>Creates the restrained premium story palette with a printable light mode.</summary>
    public static VisualTheme Premium() => Pair(ResolvedVisualStoryStyle.Light(), ResolvedVisualStoryStyle.PremiumDark());
    /// <summary>Creates paired violet desktop colors.</summary>
    public static VisualTheme MacOS() => Pair(ResolvedVisualStoryStyle.MacOS(true), ResolvedVisualStoryStyle.MacOS(false));
    /// <summary>Creates paired blue desktop colors.</summary>
    public static VisualTheme Windows() => Pair(ResolvedVisualStoryStyle.Windows(true), ResolvedVisualStoryStyle.Windows(false));
    /// <summary>Creates paired aubergine desktop colors.</summary>
    public static VisualTheme Linux() => Pair(ResolvedVisualStoryStyle.Linux(true), ResolvedVisualStoryStyle.Linux(false));
    private static VisualTheme Pair(ResolvedVisualStoryStyle light, ResolvedVisualStoryStyle dark) => new(Tokens(light), Tokens(dark),
        new VisualTypography(dark.FontFamily, monospaceFamily: dark.MonospaceFontFamily));
    private static VisualDesignTokens Tokens(ResolvedVisualStoryStyle style) {
        var tokens = new VisualDesignTokens {
            Background = style.Background, Surface = style.WindowHeader, ElevatedSurface = style.Panel,
            Border = style.Border, Foreground = style.Text, MutedForeground = style.Muted, Accent = style.Accent
        };
        tokens.Status.Pass = new VisualTokenColor(style.Success, style.Success);
        tokens.Palette = new[] { style.Syntax.Parameter, style.Syntax.Type, style.Syntax.Keyword,
            style.Syntax.Property, style.Syntax.String, style.Syntax.Number, style.Syntax.Command };
        return tokens;
    }
}

internal sealed partial class ResolvedVisualStoryStyle {
    internal static ResolvedVisualStoryStyle FromTheme(VisualTheme theme, VisualThemeMode mode, VisualStoryStyleOptions? style = null) {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        var colors = theme.Resolve(mode);
        var panel = ChartColorMath.CompositeOverOpaque(colors.ElevatedSurface, colors.Background);
        ChartColor Readable(ChartColor ink) => ChartColorMath.ContrastRatio(ChartColorMath.CompositeOverOpaque(ink, panel), panel) >= 4.5 ? ink : colors.Foreground;
        ChartColor Series(int index) => Readable(colors.Palette[index % colors.Palette.Count]);
        var syntax = style?.Syntax?.Clone() ?? new StorySyntaxPalette {
            Plain = colors.Foreground, Keyword = Series(2), Type = Series(1), Command = Readable(colors.Accent),
            Parameter = Series(0), Variable = Series(0), Property = Series(3), String = Series(4), Number = Series(5),
            Comment = colors.MutedForeground, Operator = colors.Foreground, Punctuation = colors.MutedForeground
        };
        return new ResolvedVisualStoryStyle {
            Background = colors.Background, Panel = colors.ElevatedSurface, Border = colors.Border,
            Text = colors.Foreground, Muted = colors.MutedForeground, Accent = colors.Accent,
            Success = colors.Status.Pass.Ink, FontFamily = theme.Typography.Family,
            MonospaceFontFamily = theme.Typography.MonospaceFamily, Syntax = syntax,
            WindowStyle = style?.WindowStyle ?? TerminalWindowStyle.Minimal,
            WindowHeader = style?.WindowHeader ?? colors.ElevatedSurface, BackgroundEnd = style?.BackgroundEnd
        };
    }
}

public sealed partial class VisualStory {
    private VisualThemeMode _themeMode = VisualThemeMode.Dark;
    private VisualStoryStyleOptions _style = new();
    /// <summary>Gets or sets the explicit light or dark appearance.</summary>
    public VisualThemeMode ThemeMode { get => _themeMode; set { if (!Enum.IsDefined(typeof(VisualThemeMode), value)) throw new ArgumentOutOfRangeException(nameof(value)); _themeMode = value; } }
    /// <summary>Gets or sets local window and source styling.</summary>
    public VisualStoryStyleOptions Style { get => _style; set => _style = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Changes appearance without replacing the immutable theme snapshot.</summary>
    public VisualStory WithThemeMode(VisualThemeMode mode) { ThemeMode = mode; return this; }
    /// <summary>Sets local desktop, window and syntax controls.</summary>
    public VisualStory WithStyle(VisualStoryStyleOptions style) { Style = style; return this; }
    internal ResolvedVisualStoryStyle ResolvedStyle => ResolvedVisualStoryStyle.FromTheme(Theme, ThemeMode, Style);
}
