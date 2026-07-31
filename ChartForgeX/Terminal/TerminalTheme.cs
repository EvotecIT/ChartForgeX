using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Terminal;

/// <summary>
/// Defines the visual tokens used by terminal story renderers.
/// </summary>
public sealed class TerminalTheme {
    /// <summary>Gets or sets the outer page color.</summary>
    public ChartColor PageBackground { get; set; } = ChartColor.FromHex("#07111F");

    /// <summary>Gets or sets the terminal content color.</summary>
    public ChartColor Background { get; set; } = ChartColor.FromHex("#0A1322");

    /// <summary>Gets or sets the terminal title-bar color.</summary>
    public ChartColor HeaderBackground { get; set; } = ChartColor.FromHex("#111D2E");

    /// <summary>Gets or sets the frame and divider color.</summary>
    public ChartColor Border { get; set; } = ChartColor.FromHex("#24354D");

    /// <summary>Gets or sets the normal output color.</summary>
    public ChartColor Text { get; set; } = ChartColor.FromHex("#E6EDF7");

    /// <summary>Gets or sets the subdued output color.</summary>
    public ChartColor Muted { get; set; } = ChartColor.FromHex("#8394AB");

    /// <summary>Gets or sets the prompt and command accent color.</summary>
    public ChartColor Accent { get; set; } = ChartColor.FromHex("#67D8F3");

    /// <summary>Gets or sets the success color.</summary>
    public ChartColor Success { get; set; } = ChartColor.FromHex("#55D6A9");

    /// <summary>Gets or sets the warning color.</summary>
    public ChartColor Warning { get; set; } = ChartColor.FromHex("#F4C76B");

    /// <summary>Gets or sets the error color.</summary>
    public ChartColor Error { get; set; } = ChartColor.FromHex("#FB7185");

    /// <summary>Gets or sets the cursor color.</summary>
    public ChartColor Cursor { get; set; } = ChartColor.FromHex("#E6EDF7");

    /// <summary>Gets or sets the CSS font-family stack.</summary>
    public string FontFamily { get; set; } = ChartFontStacks.Mono;

    /// <summary>Creates the restrained dark theme used by Windows Terminal-style presentations.</summary>
    public static TerminalTheme WindowsTerminal() => new();

    /// <summary>Creates a PowerShell-oriented dark theme.</summary>
    public static TerminalTheme PowerShell() => new() {
        PageBackground = ChartColor.FromHex("#07101E"),
        Background = ChartColor.FromHex("#091426"),
        HeaderBackground = ChartColor.FromHex("#101C30"),
        Border = ChartColor.FromHex("#253A57"),
        Accent = ChartColor.FromHex("#63D6F2"),
        Success = ChartColor.FromHex("#55D6A9")
    };

    /// <summary>Creates a classic near-black terminal theme.</summary>
    public static TerminalTheme Classic() => new() {
        PageBackground = ChartColor.FromHex("#090B0F"),
        Background = ChartColor.FromHex("#0C0F13"),
        HeaderBackground = ChartColor.FromHex("#15191F"),
        Border = ChartColor.FromHex("#30363D"),
        Text = ChartColor.FromHex("#E6EDF3"),
        Muted = ChartColor.FromHex("#8B949E"),
        Accent = ChartColor.FromHex("#58A6FF"),
        Success = ChartColor.FromHex("#3FB950"),
        Warning = ChartColor.FromHex("#D29922"),
        Error = ChartColor.FromHex("#F85149"),
        Cursor = ChartColor.FromHex("#E6EDF3")
    };

    /// <summary>Creates a light terminal theme suitable for printed documentation.</summary>
    public static TerminalTheme Light() => new() {
        PageBackground = ChartColor.FromHex("#E8EDF4"),
        Background = ChartColor.FromHex("#F8FAFC"),
        HeaderBackground = ChartColor.FromHex("#E9EEF5"),
        Border = ChartColor.FromHex("#C4CEDB"),
        Text = ChartColor.FromHex("#172033"),
        Muted = ChartColor.FromHex("#607089"),
        Accent = ChartColor.FromHex("#0969DA"),
        Success = ChartColor.FromHex("#18794E"),
        Warning = ChartColor.FromHex("#9A6700"),
        Error = ChartColor.FromHex("#CF222E"),
        Cursor = ChartColor.FromHex("#172033")
    };
}
