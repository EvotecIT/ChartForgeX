using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Stories;

/// <summary>
/// Defines the renderer-neutral syntax palette used by visual stories and terminal commands.
/// </summary>
public sealed class StorySyntaxPalette {
    /// <summary>Gets or sets plain source text color.</summary>
    public ChartColor Plain { get; set; } = ChartColor.FromHex("#E6EDF7");
    /// <summary>Gets or sets keyword color.</summary>
    public ChartColor Keyword { get; set; } = ChartColor.FromHex("#FF7AB2");
    /// <summary>Gets or sets type color.</summary>
    public ChartColor Type { get; set; } = ChartColor.FromHex("#4EC9B0");
    /// <summary>Gets or sets command color.</summary>
    public ChartColor Command { get; set; } = ChartColor.FromHex("#DCDCAA");
    /// <summary>Gets or sets parameter color.</summary>
    public ChartColor Parameter { get; set; } = ChartColor.FromHex("#9CDCFE");
    /// <summary>Gets or sets variable color.</summary>
    public ChartColor Variable { get; set; } = ChartColor.FromHex("#9CDCFE");
    /// <summary>Gets or sets property color.</summary>
    public ChartColor Property { get; set; } = ChartColor.FromHex("#D7BA7D");
    /// <summary>Gets or sets string color.</summary>
    public ChartColor String { get; set; } = ChartColor.FromHex("#CE9178");
    /// <summary>Gets or sets number color.</summary>
    public ChartColor Number { get; set; } = ChartColor.FromHex("#B5CEA8");
    /// <summary>Gets or sets comment color.</summary>
    public ChartColor Comment { get; set; } = ChartColor.FromHex("#6A9955");
    /// <summary>Gets or sets operator color.</summary>
    public ChartColor Operator { get; set; } = ChartColor.FromHex("#D4D4D4");
    /// <summary>Gets or sets punctuation color.</summary>
    public ChartColor Punctuation { get; set; } = ChartColor.FromHex("#AAB7C8");

    /// <summary>Resolves a semantic syntax category to its configured color.</summary>
    public ChartColor Resolve(StorySyntaxKind kind) {
        switch (kind) {
            case StorySyntaxKind.Keyword: return Keyword;
            case StorySyntaxKind.Type: return Type;
            case StorySyntaxKind.Command: return Command;
            case StorySyntaxKind.Parameter: return Parameter;
            case StorySyntaxKind.Variable: return Variable;
            case StorySyntaxKind.Property: return Property;
            case StorySyntaxKind.String: return String;
            case StorySyntaxKind.Number: return Number;
            case StorySyntaxKind.Comment: return Comment;
            case StorySyntaxKind.Operator: return Operator;
            case StorySyntaxKind.Punctuation: return Punctuation;
            default: return Plain;
        }
    }

    /// <summary>Creates a shallow copy of this palette.</summary>
    public StorySyntaxPalette Clone() => (StorySyntaxPalette)MemberwiseClone();
}

/// <summary>
/// Defines premium but restrained visual-story colors and typography.
/// </summary>
public sealed class VisualStoryTheme {
    /// <summary>Gets or sets the outer background color.</summary>
    public ChartColor Background { get; set; } = ChartColor.FromHex("#050B16");
    /// <summary>Gets or sets the panel background color.</summary>
    public ChartColor Panel { get; set; } = ChartColor.FromHex("#0A1424");
    /// <summary>Gets or sets the panel border color.</summary>
    public ChartColor Border { get; set; } = ChartColor.FromHex("#22344F");
    /// <summary>Gets or sets primary text color.</summary>
    public ChartColor Text { get; set; } = ChartColor.FromHex("#F1F5FB");
    /// <summary>Gets or sets secondary text color.</summary>
    public ChartColor Muted { get; set; } = ChartColor.FromHex("#9BACBF");
    /// <summary>Gets or sets accent color.</summary>
    public ChartColor Accent { get; set; } = ChartColor.FromHex("#5ED7F2");
    /// <summary>Gets or sets success/evidence color.</summary>
    public ChartColor Success { get; set; } = ChartColor.FromHex("#55D6A9");
    /// <summary>Gets or sets the general font-family stack.</summary>
    public string FontFamily { get; set; } = ChartFontStacks.SystemSans;
    /// <summary>Gets or sets the monospace font-family stack.</summary>
    public string MonospaceFontFamily { get; set; } = ChartFontStacks.Mono;
    /// <summary>Gets or sets the syntax palette.</summary>
    public StorySyntaxPalette Syntax { get; set; } = new();

    /// <summary>Creates the default premium dark visual-story theme.</summary>
    public static VisualStoryTheme PremiumDark() => new();

    /// <summary>Creates a light visual-story theme suitable for documentation and printing.</summary>
    public static VisualStoryTheme Light() => new() {
        Background = ChartColor.FromHex("#E9EEF5"),
        Panel = ChartColor.FromHex("#FFFFFF"),
        Border = ChartColor.FromHex("#C7D1DE"),
        Text = ChartColor.FromHex("#172033"),
        Muted = ChartColor.FromHex("#617086"),
        Accent = ChartColor.FromHex("#0969DA"),
        Success = ChartColor.FromHex("#18794E"),
        Syntax = new StorySyntaxPalette {
            Plain = ChartColor.FromHex("#172033"),
            Keyword = ChartColor.FromHex("#A626A4"),
            Type = ChartColor.FromHex("#0184BC"),
            Command = ChartColor.FromHex("#795E26"),
            Parameter = ChartColor.FromHex("#005CC5"),
            Variable = ChartColor.FromHex("#005CC5"),
            Property = ChartColor.FromHex("#795E26"),
            String = ChartColor.FromHex("#50A14F"),
            Number = ChartColor.FromHex("#986801"),
            Comment = ChartColor.FromHex("#6A737D"),
            Operator = ChartColor.FromHex("#24292F"),
            Punctuation = ChartColor.FromHex("#586069")
        }
    };

    /// <summary>Creates a shallow copy with an independent syntax palette.</summary>
    public VisualStoryTheme Clone() {
        var clone = (VisualStoryTheme)MemberwiseClone();
        clone.Syntax = Syntax.Clone();
        return clone;
    }
}
