using ChartForgeX.Primitives;
using ChartForgeX.Terminal;

namespace ChartForgeX.Stories;

public sealed partial class VisualStoryTheme {
    private ChartColor? _windowHeader;
    /// <summary>Gets or sets the source and replay window chrome. Minimal preserves the restrained story presentation.</summary>
    public TerminalWindowStyle WindowStyle { get; set; } = TerminalWindowStyle.Minimal;
    /// <summary>Gets or sets the desktop window title-bar color. Defaults to the panel color until explicitly set.</summary>
    public ChartColor WindowHeader { get => _windowHeader ?? Panel; set => _windowHeader = value; }
    /// <summary>Gets or sets an optional second background color for a diagonal desktop gradient.</summary>
    public ChartColor? BackgroundEnd { get; set; }

    /// <summary>Creates a colorful macOS-inspired story with traffic-light controls and a violet desktop.</summary>
    public static VisualStoryTheme MacOS(bool light = false) => Desktop(TerminalWindowStyle.MacOS, light,
        light ? "#E2E7FF" : "#293D73", light ? "#F5DDF4" : "#74477D", light ? "#E8EAF8" : "#29364D", "#A68BFA");
    /// <summary>Creates a Windows-inspired story with tab chrome and a blue desktop.</summary>
    public static VisualStoryTheme Windows(bool light = false) => Desktop(TerminalWindowStyle.WindowsTerminal, light,
        light ? "#D7ECFF" : "#084D7D", light ? "#E8E5FF" : "#19336A", light ? "#E5EDFA" : "#1B354E", "#63CAFF");
    /// <summary>Creates a Linux-inspired story with right-hand window controls and an aubergine desktop.</summary>
    public static VisualStoryTheme Linux(bool light = false) => Desktop(TerminalWindowStyle.Linux, light,
        light ? "#FFE4DA" : "#65304A", light ? "#EFDEFF" : "#342147", light ? "#F4E7EF" : "#3B2941", "#FFB071");

    private static VisualStoryTheme Desktop(TerminalWindowStyle style, bool light, string background, string end, string header, string accent) {
        var theme = light ? Light() : PremiumDark();
        theme.WindowStyle = style;
        theme.Background = ChartColor.FromHex(background);
        theme.BackgroundEnd = ChartColor.FromHex(end);
        theme.WindowHeader = ChartColor.FromHex(header);
        theme.Panel = ChartColor.FromHex(light ? "#FAFCFF" : "#172238");
        theme.Border = ChartColor.FromHex(light ? "#BAC5DC" : "#4B5C7A");
        theme.Text = ChartColor.FromHex(light ? "#24324D" : "#F0F4FF");
        theme.Muted = ChartColor.FromHex(light ? "#52617B" : "#CAD5EB");
        theme.Accent = ChartColor.FromHex(light ? "#4C59AD" : accent);
        theme.Success = ChartColor.FromHex(light ? "#187545" : "#9DE4B3");
        theme.Syntax = new StorySyntaxPalette {
            Plain = theme.Text,
            Keyword = ChartColor.FromHex(light ? "#A12572" : "#F5A1D7"),
            Type = ChartColor.FromHex(light ? "#00777A" : "#69D4BE"),
            Command = ChartColor.FromHex(light ? "#965100" : "#F5D58B"),
            Parameter = ChartColor.FromHex(light ? "#846015" : "#E9B34A"),
            Variable = ChartColor.FromHex(light ? "#175FD4" : "#74C7FF"),
            Property = ChartColor.FromHex(light ? "#8A4B18" : "#FAB387"),
            String = ChartColor.FromHex(light ? "#167645" : "#B1E593"),
            Number = ChartColor.FromHex(light ? "#864FC1" : "#C5A2FF"),
            Comment = ChartColor.FromHex(light ? "#586477" : "#91A4C3"),
            Operator = ChartColor.FromHex(light ? "#31415F" : "#96B7FF"),
            Punctuation = ChartColor.FromHex(light ? "#52637D" : "#C9D5E9")
        };
        return theme;
    }

    internal TerminalTheme TerminalPalette() => new() {
        PageBackground = Background, Background = Panel, HeaderBackground = WindowHeader,
        Border = Border, Text = Text, Muted = Muted, Accent = Accent, Success = Success,
        Warning = Syntax.Parameter, Error = Syntax.Keyword, Cursor = Text, FontFamily = MonospaceFontFamily
    };
}
