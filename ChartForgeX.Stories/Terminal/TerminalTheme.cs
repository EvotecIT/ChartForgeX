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

    /// <summary>Creates the restrained default dark terminal palette.</summary>
    public static TerminalTheme Dark() => new();

    /// <summary>Creates a PowerShell-oriented dark theme.</summary>
    public static TerminalTheme PowerShell() => new() {
        PageBackground = ChartColor.FromHex("#07101E"),
        Background = ChartColor.FromHex("#091426"),
        HeaderBackground = ChartColor.FromHex("#101C30"),
        Border = ChartColor.FromHex("#253A57"),
        Accent = ChartColor.FromHex("#63D6F2"),
        Success = ChartColor.FromHex("#55D6A9")
    };

    /// <summary>Creates the classic Windows PowerShell blue palette.</summary>
    public static TerminalTheme WindowsPowerShell() => new() {
        PageBackground = ChartColor.FromHex("#071326"),
        Background = ChartColor.FromHex("#012456"),
        HeaderBackground = ChartColor.FromHex("#202020"),
        Border = ChartColor.FromHex("#3B4E70"),
        Text = ChartColor.FromHex("#F2F2F2"),
        Muted = ChartColor.FromHex("#B8C7DF"),
        Accent = ChartColor.FromHex("#5CCFE6"),
        Success = ChartColor.FromHex("#7FDBCA"),
        Warning = ChartColor.FromHex("#FFE66D"),
        Error = ChartColor.FromHex("#FF6B81"),
        Cursor = ChartColor.FromHex("#F2F2F2")
    };

    /// <summary>Creates an Ubuntu terminal-inspired aubergine palette.</summary>
    public static TerminalTheme Ubuntu() => new() {
        PageBackground = ChartColor.FromHex("#14040F"),
        Background = ChartColor.FromHex("#300A24"),
        HeaderBackground = ChartColor.FromHex("#2C2C2C"),
        Border = ChartColor.FromHex("#5E2B4B"),
        Text = ChartColor.FromHex("#F2F2F2"),
        Muted = ChartColor.FromHex("#C8B7C2"),
        Accent = ChartColor.FromHex("#E95420"),
        Success = ChartColor.FromHex("#4E9A06"),
        Warning = ChartColor.FromHex("#FCE94F"),
        Error = ChartColor.FromHex("#EF2929"),
        Cursor = ChartColor.FromHex("#F2F2F2")
    };

    /// <summary>Creates the Windows Terminal Campbell palette.</summary>
    public static TerminalTheme Campbell() => new() {
        PageBackground = ChartColor.FromHex("#090909"),
        Background = ChartColor.FromHex("#0C0C0C"),
        HeaderBackground = ChartColor.FromHex("#2B2B2B"),
        Border = ChartColor.FromHex("#464646"),
        Text = ChartColor.FromHex("#CCCCCC"),
        Muted = ChartColor.FromHex("#8A8A8A"),
        Accent = ChartColor.FromHex("#3A96DD"),
        Success = ChartColor.FromHex("#13A10E"),
        Warning = ChartColor.FromHex("#F9F1A5"),
        Error = ChartColor.FromHex("#E74856"),
        Cursor = ChartColor.FromHex("#FFFFFF")
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

    /// <summary>Creates an independent copy of this palette.</summary>
    public TerminalTheme Copy() => new() {
        PageBackground = PageBackground,
        Background = Background,
        HeaderBackground = HeaderBackground,
        Border = Border,
        Text = Text,
        Muted = Muted,
        Accent = Accent,
        Success = Success,
        Warning = Warning,
        Error = Error,
        Cursor = Cursor,
        FontFamily = FontFamily
    };
}
