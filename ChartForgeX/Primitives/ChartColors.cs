using System;
using System.Collections.Generic;

namespace ChartForgeX.Primitives;

#pragma warning disable CS1591

/// <summary>
/// Provides dependency-free named color tokens for ChartForgeX themes and consumers.
/// </summary>
public static class ChartColors {
    private static readonly Dictionary<string, ChartColor> NamedColors = new(StringComparer.OrdinalIgnoreCase) {
        ["AliceBlue"] = AliceBlue, ["AntiqueWhite"] = AntiqueWhite, ["Aqua"] = Aqua, ["Aquamarine"] = Aquamarine,
        ["Azure"] = Azure, ["Beige"] = Beige, ["Bisque"] = Bisque, ["Black"] = Black,
        ["BlanchedAlmond"] = BlanchedAlmond, ["Blue"] = Blue, ["BlueViolet"] = BlueViolet, ["Brown"] = Brown,
        ["BurlyWood"] = BurlyWood, ["CadetBlue"] = CadetBlue, ["Chartreuse"] = Chartreuse, ["Chocolate"] = Chocolate,
        ["Coral"] = Coral, ["CornflowerBlue"] = CornflowerBlue, ["Cornsilk"] = Cornsilk, ["Crimson"] = Crimson,
        ["Cyan"] = Cyan, ["DarkBlue"] = DarkBlue, ["DarkCyan"] = DarkCyan, ["DarkGoldenrod"] = DarkGoldenrod,
        ["DarkGoldenRod"] = DarkGoldenrod, ["DarkGray"] = DarkGray, ["DarkGrey"] = DarkGray, ["DarkGreen"] = DarkGreen,
        ["DarkKhaki"] = DarkKhaki, ["DarkMagenta"] = DarkMagenta, ["DarkOliveGreen"] = DarkOliveGreen, ["DarkOrange"] = DarkOrange,
        ["DarkOrchid"] = DarkOrchid, ["DarkRed"] = DarkRed, ["DarkSalmon"] = DarkSalmon, ["DarkSeaGreen"] = DarkSeaGreen,
        ["DarkSlateBlue"] = DarkSlateBlue, ["DarkSlateGray"] = DarkSlateGray, ["DarkSlateGrey"] = DarkSlateGray, ["DarkTurquoise"] = DarkTurquoise,
        ["DarkViolet"] = DarkViolet, ["DeepPink"] = DeepPink, ["DeepSkyBlue"] = DeepSkyBlue, ["DimGray"] = DimGray,
        ["DimGrey"] = DimGray, ["DodgerBlue"] = DodgerBlue, ["Firebrick"] = Firebrick, ["FloralWhite"] = FloralWhite,
        ["ForestGreen"] = ForestGreen, ["Fuchsia"] = Fuchsia, ["Gainsboro"] = Gainsboro, ["GhostWhite"] = GhostWhite,
        ["Gold"] = Gold, ["Goldenrod"] = Goldenrod, ["GoldenRod"] = Goldenrod, ["Gray"] = Gray,
        ["Grey"] = Gray, ["Green"] = Green, ["GreenYellow"] = GreenYellow, ["Honeydew"] = Honeydew,
        ["HotPink"] = HotPink, ["IndianRed"] = IndianRed, ["Indigo"] = Indigo, ["Ivory"] = Ivory,
        ["Khaki"] = Khaki, ["Lavender"] = Lavender, ["LavenderBlush"] = LavenderBlush, ["LawnGreen"] = LawnGreen,
        ["LemonChiffon"] = LemonChiffon, ["LightBlue"] = LightBlue, ["LightCoral"] = LightCoral, ["LightCyan"] = LightCyan,
        ["LightGoldenrodYellow"] = LightGoldenrodYellow, ["LightGoldenRodYellow"] = LightGoldenrodYellow, ["LightGray"] = LightGray, ["LightGrey"] = LightGray,
        ["LightGreen"] = LightGreen, ["LightPink"] = LightPink, ["LightSalmon"] = LightSalmon, ["LightSeaGreen"] = LightSeaGreen,
        ["LightSkyBlue"] = LightSkyBlue, ["LightSlateGray"] = LightSlateGray, ["LightSlateGrey"] = LightSlateGray, ["LightSteelBlue"] = LightSteelBlue,
        ["LightYellow"] = LightYellow, ["Lime"] = Lime, ["LimeGreen"] = LimeGreen, ["Linen"] = Linen,
        ["Magenta"] = Magenta, ["Maroon"] = Maroon, ["MediumAquamarine"] = MediumAquamarine, ["MediumBlue"] = MediumBlue,
        ["MediumOrchid"] = MediumOrchid, ["MediumPurple"] = MediumPurple, ["MediumSeaGreen"] = MediumSeaGreen, ["MediumSlateBlue"] = MediumSlateBlue,
        ["MediumSpringGreen"] = MediumSpringGreen, ["MediumTurquoise"] = MediumTurquoise, ["MediumVioletRed"] = MediumVioletRed, ["MidnightBlue"] = MidnightBlue,
        ["MintCream"] = MintCream, ["MistyRose"] = MistyRose, ["Moccasin"] = Moccasin, ["NavajoWhite"] = NavajoWhite,
        ["Navy"] = Navy, ["OldLace"] = OldLace, ["Olive"] = Olive, ["OliveDrab"] = OliveDrab,
        ["Orange"] = Orange, ["OrangeRed"] = OrangeRed, ["Orchid"] = Orchid, ["PaleGoldenrod"] = PaleGoldenrod,
        ["PaleGoldenRod"] = PaleGoldenrod, ["PaleGreen"] = PaleGreen, ["PaleTurquoise"] = PaleTurquoise, ["PaleVioletRed"] = PaleVioletRed,
        ["PapayaWhip"] = PapayaWhip, ["PeachPuff"] = PeachPuff, ["Peru"] = Peru, ["Pink"] = Pink,
        ["Plum"] = Plum, ["PowderBlue"] = PowderBlue, ["Purple"] = Purple, ["RebeccaPurple"] = RebeccaPurple,
        ["Red"] = Red, ["RosyBrown"] = RosyBrown, ["RoyalBlue"] = RoyalBlue, ["SaddleBrown"] = SaddleBrown,
        ["Salmon"] = Salmon, ["SandyBrown"] = SandyBrown, ["SeaGreen"] = SeaGreen, ["SeaShell"] = SeaShell,
        ["Sienna"] = Sienna, ["Silver"] = Silver, ["SkyBlue"] = SkyBlue, ["SlateBlue"] = SlateBlue,
        ["SlateGray"] = SlateGray, ["SlateGrey"] = SlateGray, ["Snow"] = Snow, ["SpringGreen"] = SpringGreen,
        ["SteelBlue"] = SteelBlue, ["Tan"] = Tan, ["Teal"] = Teal, ["Thistle"] = Thistle,
        ["Tomato"] = Tomato, ["Transparent"] = Transparent, ["Turquoise"] = Turquoise, ["Violet"] = Violet,
        ["Wheat"] = Wheat, ["White"] = White, ["WhiteSmoke"] = WhiteSmoke, ["Yellow"] = Yellow,
        ["YellowGreen"] = YellowGreen
    };

    private static readonly Dictionary<string, ChartColor> TokenColors = new(StringComparer.OrdinalIgnoreCase) {
        ["Slate50"] = Slate50, ["Slate100"] = Slate100, ["Slate200"] = Slate200, ["Slate300"] = Slate300,
        ["Slate400"] = Slate400, ["Slate500"] = Slate500, ["Slate600"] = Slate600, ["Slate700"] = Slate700,
        ["Slate800"] = Slate800, ["Slate900"] = Slate900, ["Slate950"] = Slate950,
        ["Sky400"] = Sky400, ["Sky500"] = Sky500, ["Blue400"] = Blue400, ["Blue600"] = Blue600,
        ["Cyan400"] = Cyan400, ["Teal400"] = Teal400, ["Emerald400"] = Emerald400, ["Emerald500"] = Emerald500,
        ["Green400"] = Green400, ["Lime400"] = Lime400, ["Amber400"] = Amber400, ["Yellow400"] = Yellow400,
        ["Orange400"] = Orange400, ["Red400"] = Red400, ["Rose400"] = Rose400, ["Pink400"] = Pink400,
        ["Violet400"] = Violet400, ["Purple400"] = Purple400, ["Indigo400"] = Indigo400
    };

    /// <summary>
    /// Attempts to get a named color token.
    /// </summary>
    /// <param name="name">The color name.</param>
    /// <param name="color">The resolved chart color.</param>
    /// <returns>True when the name is known.</returns>
    public static bool TryGet(string? name, out ChartColor color) {
        color = default;
        if (string.IsNullOrWhiteSpace(name)) return false;
        var trimmed = name!.Trim();
        return NamedColors.TryGetValue(trimmed, out color) || TokenColors.TryGetValue(trimmed, out color);
    }

    /// <summary>
    /// Gets all stable named web colors known to ChartForgeX.
    /// </summary>
    /// <returns>A copy of the named color map.</returns>
    public static IReadOnlyDictionary<string, ChartColor> GetNamedColors() => new Dictionary<string, ChartColor>(NamedColors, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all ChartForgeX design tokens used by palettes and overlay themes.
    /// </summary>
    /// <returns>A copy of the named token color map.</returns>
    public static IReadOnlyDictionary<string, ChartColor> GetTokenColors() => new Dictionary<string, ChartColor>(TokenColors, StringComparer.OrdinalIgnoreCase);

    public static ChartColor Transparent => ChartColor.Transparent;
    public static ChartColor AliceBlue => ChartColor.FromRgb(240, 248, 255);
    public static ChartColor AntiqueWhite => ChartColor.FromRgb(250, 235, 215);
    public static ChartColor Aqua => ChartColor.FromRgb(0, 255, 255);
    public static ChartColor Aquamarine => ChartColor.FromRgb(127, 255, 212);
    public static ChartColor Azure => ChartColor.FromRgb(240, 255, 255);
    public static ChartColor Beige => ChartColor.FromRgb(245, 245, 220);
    public static ChartColor Bisque => ChartColor.FromRgb(255, 228, 196);
    public static ChartColor Black => ChartColor.FromRgb(0, 0, 0);
    public static ChartColor BlanchedAlmond => ChartColor.FromRgb(255, 235, 205);
    public static ChartColor Blue => ChartColor.FromRgb(0, 0, 255);
    public static ChartColor BlueViolet => ChartColor.FromRgb(138, 43, 226);
    public static ChartColor Brown => ChartColor.FromRgb(165, 42, 42);
    public static ChartColor BurlyWood => ChartColor.FromRgb(222, 184, 135);
    public static ChartColor CadetBlue => ChartColor.FromRgb(95, 158, 160);
    public static ChartColor Chartreuse => ChartColor.FromRgb(127, 255, 0);
    public static ChartColor Chocolate => ChartColor.FromRgb(210, 105, 30);
    public static ChartColor Coral => ChartColor.FromRgb(255, 127, 80);
    public static ChartColor CornflowerBlue => ChartColor.FromRgb(100, 149, 237);
    public static ChartColor Cornsilk => ChartColor.FromRgb(255, 248, 220);
    public static ChartColor Crimson => ChartColor.FromRgb(220, 20, 60);
    public static ChartColor Cyan => ChartColor.FromRgb(0, 255, 255);
    public static ChartColor DarkBlue => ChartColor.FromRgb(0, 0, 139);
    public static ChartColor DarkCyan => ChartColor.FromRgb(0, 139, 139);
    public static ChartColor DarkGoldenrod => ChartColor.FromRgb(184, 134, 11);
    public static ChartColor DarkGray => ChartColor.FromRgb(169, 169, 169);
    public static ChartColor DarkGrey => DarkGray;
    public static ChartColor DarkGreen => ChartColor.FromRgb(0, 100, 0);
    public static ChartColor DarkKhaki => ChartColor.FromRgb(189, 183, 107);
    public static ChartColor DarkMagenta => ChartColor.FromRgb(139, 0, 139);
    public static ChartColor DarkOliveGreen => ChartColor.FromRgb(85, 107, 47);
    public static ChartColor DarkOrange => ChartColor.FromRgb(255, 140, 0);
    public static ChartColor DarkOrchid => ChartColor.FromRgb(153, 50, 204);
    public static ChartColor DarkRed => ChartColor.FromRgb(139, 0, 0);
    public static ChartColor DarkSalmon => ChartColor.FromRgb(233, 150, 122);
    public static ChartColor DarkSeaGreen => ChartColor.FromRgb(143, 188, 143);
    public static ChartColor DarkSlateBlue => ChartColor.FromRgb(72, 61, 139);
    public static ChartColor DarkSlateGray => ChartColor.FromRgb(47, 79, 79);
    public static ChartColor DarkSlateGrey => DarkSlateGray;
    public static ChartColor DarkTurquoise => ChartColor.FromRgb(0, 206, 209);
    public static ChartColor DarkViolet => ChartColor.FromRgb(148, 0, 211);
    public static ChartColor DeepPink => ChartColor.FromRgb(255, 20, 147);
    public static ChartColor DeepSkyBlue => ChartColor.FromRgb(0, 191, 255);
    public static ChartColor DimGray => ChartColor.FromRgb(105, 105, 105);
    public static ChartColor DimGrey => DimGray;
    public static ChartColor DodgerBlue => ChartColor.FromRgb(30, 144, 255);
    public static ChartColor Firebrick => ChartColor.FromRgb(178, 34, 34);
    public static ChartColor FloralWhite => ChartColor.FromRgb(255, 250, 240);
    public static ChartColor ForestGreen => ChartColor.FromRgb(34, 139, 34);
    public static ChartColor Fuchsia => ChartColor.FromRgb(255, 0, 255);
    public static ChartColor Gainsboro => ChartColor.FromRgb(220, 220, 220);
    public static ChartColor GhostWhite => ChartColor.FromRgb(248, 248, 255);
    public static ChartColor Gold => ChartColor.FromRgb(255, 215, 0);
    public static ChartColor Goldenrod => ChartColor.FromRgb(218, 165, 32);
    public static ChartColor Gray => ChartColor.FromRgb(128, 128, 128);
    public static ChartColor Grey => Gray;
    public static ChartColor Green => ChartColor.FromRgb(0, 128, 0);
    public static ChartColor GreenYellow => ChartColor.FromRgb(173, 255, 47);
    public static ChartColor Honeydew => ChartColor.FromRgb(240, 255, 240);
    public static ChartColor HotPink => ChartColor.FromRgb(255, 105, 180);
    public static ChartColor IndianRed => ChartColor.FromRgb(205, 92, 92);
    public static ChartColor Indigo => ChartColor.FromRgb(75, 0, 130);
    public static ChartColor Ivory => ChartColor.FromRgb(255, 255, 240);
    public static ChartColor Khaki => ChartColor.FromRgb(240, 230, 140);
    public static ChartColor Lavender => ChartColor.FromRgb(230, 230, 250);
    public static ChartColor LavenderBlush => ChartColor.FromRgb(255, 240, 245);
    public static ChartColor LawnGreen => ChartColor.FromRgb(124, 252, 0);
    public static ChartColor LemonChiffon => ChartColor.FromRgb(255, 250, 205);
    public static ChartColor LightBlue => ChartColor.FromRgb(173, 216, 230);
    public static ChartColor LightCoral => ChartColor.FromRgb(240, 128, 128);
    public static ChartColor LightCyan => ChartColor.FromRgb(224, 255, 255);
    public static ChartColor LightGoldenrodYellow => ChartColor.FromRgb(250, 250, 210);
    public static ChartColor LightGray => ChartColor.FromRgb(211, 211, 211);
    public static ChartColor LightGrey => LightGray;
    public static ChartColor LightGreen => ChartColor.FromRgb(144, 238, 144);
    public static ChartColor LightPink => ChartColor.FromRgb(255, 182, 193);
    public static ChartColor LightSalmon => ChartColor.FromRgb(255, 160, 122);
    public static ChartColor LightSeaGreen => ChartColor.FromRgb(32, 178, 170);
    public static ChartColor LightSkyBlue => ChartColor.FromRgb(135, 206, 250);
    public static ChartColor LightSlateGray => ChartColor.FromRgb(119, 136, 153);
    public static ChartColor LightSlateGrey => LightSlateGray;
    public static ChartColor LightSteelBlue => ChartColor.FromRgb(176, 196, 222);
    public static ChartColor LightYellow => ChartColor.FromRgb(255, 255, 224);
    public static ChartColor Lime => ChartColor.FromRgb(0, 255, 0);
    public static ChartColor LimeGreen => ChartColor.FromRgb(50, 205, 50);
    public static ChartColor Linen => ChartColor.FromRgb(250, 240, 230);
    public static ChartColor Magenta => ChartColor.FromRgb(255, 0, 255);
    public static ChartColor Maroon => ChartColor.FromRgb(128, 0, 0);
    public static ChartColor MediumAquamarine => ChartColor.FromRgb(102, 205, 170);
    public static ChartColor MediumBlue => ChartColor.FromRgb(0, 0, 205);
    public static ChartColor MediumOrchid => ChartColor.FromRgb(186, 85, 211);
    public static ChartColor MediumPurple => ChartColor.FromRgb(147, 112, 219);
    public static ChartColor MediumSeaGreen => ChartColor.FromRgb(60, 179, 113);
    public static ChartColor MediumSlateBlue => ChartColor.FromRgb(123, 104, 238);
    public static ChartColor MediumSpringGreen => ChartColor.FromRgb(0, 250, 154);
    public static ChartColor MediumTurquoise => ChartColor.FromRgb(72, 209, 204);
    public static ChartColor MediumVioletRed => ChartColor.FromRgb(199, 21, 133);
    public static ChartColor MidnightBlue => ChartColor.FromRgb(25, 25, 112);
    public static ChartColor MintCream => ChartColor.FromRgb(245, 255, 250);
    public static ChartColor MistyRose => ChartColor.FromRgb(255, 228, 225);
    public static ChartColor Moccasin => ChartColor.FromRgb(255, 228, 181);
    public static ChartColor NavajoWhite => ChartColor.FromRgb(255, 222, 173);
    public static ChartColor Navy => ChartColor.FromRgb(0, 0, 128);
    public static ChartColor OldLace => ChartColor.FromRgb(253, 245, 230);
    public static ChartColor Olive => ChartColor.FromRgb(128, 128, 0);
    public static ChartColor OliveDrab => ChartColor.FromRgb(107, 142, 35);
    public static ChartColor Orange => ChartColor.FromRgb(255, 165, 0);
    public static ChartColor OrangeRed => ChartColor.FromRgb(255, 69, 0);
    public static ChartColor Orchid => ChartColor.FromRgb(218, 112, 214);
    public static ChartColor PaleGoldenrod => ChartColor.FromRgb(238, 232, 170);
    public static ChartColor PaleGreen => ChartColor.FromRgb(152, 251, 152);
    public static ChartColor PaleTurquoise => ChartColor.FromRgb(175, 238, 238);
    public static ChartColor PaleVioletRed => ChartColor.FromRgb(219, 112, 147);
    public static ChartColor PapayaWhip => ChartColor.FromRgb(255, 239, 213);
    public static ChartColor PeachPuff => ChartColor.FromRgb(255, 218, 185);
    public static ChartColor Peru => ChartColor.FromRgb(205, 133, 63);
    public static ChartColor Pink => ChartColor.FromRgb(255, 192, 203);
    public static ChartColor Plum => ChartColor.FromRgb(221, 160, 221);
    public static ChartColor PowderBlue => ChartColor.FromRgb(176, 224, 230);
    public static ChartColor Purple => ChartColor.FromRgb(128, 0, 128);
    public static ChartColor RebeccaPurple => ChartColor.FromRgb(102, 51, 153);
    public static ChartColor Red => ChartColor.FromRgb(255, 0, 0);
    public static ChartColor RosyBrown => ChartColor.FromRgb(188, 143, 143);
    public static ChartColor RoyalBlue => ChartColor.FromRgb(65, 105, 225);
    public static ChartColor SaddleBrown => ChartColor.FromRgb(139, 69, 19);
    public static ChartColor Salmon => ChartColor.FromRgb(250, 128, 114);
    public static ChartColor SandyBrown => ChartColor.FromRgb(244, 164, 96);
    public static ChartColor SeaGreen => ChartColor.FromRgb(46, 139, 87);
    public static ChartColor SeaShell => ChartColor.FromRgb(255, 245, 238);
    public static ChartColor Sienna => ChartColor.FromRgb(160, 82, 45);
    public static ChartColor Silver => ChartColor.FromRgb(192, 192, 192);
    public static ChartColor SkyBlue => ChartColor.FromRgb(135, 206, 235);
    public static ChartColor SlateBlue => ChartColor.FromRgb(106, 90, 205);
    public static ChartColor SlateGray => ChartColor.FromRgb(112, 128, 144);
    public static ChartColor SlateGrey => SlateGray;
    public static ChartColor Snow => ChartColor.FromRgb(255, 250, 250);
    public static ChartColor SpringGreen => ChartColor.FromRgb(0, 255, 127);
    public static ChartColor SteelBlue => ChartColor.FromRgb(70, 130, 180);
    public static ChartColor Tan => ChartColor.FromRgb(210, 180, 140);
    public static ChartColor Teal => ChartColor.FromRgb(0, 128, 128);
    public static ChartColor Thistle => ChartColor.FromRgb(216, 191, 216);
    public static ChartColor Tomato => ChartColor.FromRgb(255, 99, 71);
    public static ChartColor Turquoise => ChartColor.FromRgb(64, 224, 208);
    public static ChartColor Violet => ChartColor.FromRgb(238, 130, 238);
    public static ChartColor Wheat => ChartColor.FromRgb(245, 222, 179);
    public static ChartColor White => ChartColor.FromRgb(255, 255, 255);
    public static ChartColor WhiteSmoke => ChartColor.FromRgb(245, 245, 245);
    public static ChartColor Yellow => ChartColor.FromRgb(255, 255, 0);
    public static ChartColor YellowGreen => ChartColor.FromRgb(154, 205, 50);

    public static ChartColor Slate50 => ChartColor.FromRgb(248, 250, 252);
    public static ChartColor Slate100 => ChartColor.FromRgb(241, 245, 249);
    public static ChartColor Slate200 => ChartColor.FromRgb(226, 232, 240);
    public static ChartColor Slate300 => ChartColor.FromRgb(203, 213, 225);
    public static ChartColor Slate400 => ChartColor.FromRgb(148, 163, 184);
    public static ChartColor Slate500 => ChartColor.FromRgb(100, 116, 139);
    public static ChartColor Slate600 => ChartColor.FromRgb(71, 85, 105);
    public static ChartColor Slate700 => ChartColor.FromRgb(51, 65, 85);
    public static ChartColor Slate800 => ChartColor.FromRgb(30, 41, 59);
    public static ChartColor Slate900 => ChartColor.FromRgb(15, 23, 42);
    public static ChartColor Slate950 => ChartColor.FromRgb(2, 6, 23);
    public static ChartColor Sky400 => ChartColor.FromRgb(56, 189, 248);
    public static ChartColor Sky500 => ChartColor.FromRgb(14, 165, 233);
    public static ChartColor Blue400 => ChartColor.FromRgb(96, 165, 250);
    public static ChartColor Blue600 => ChartColor.FromRgb(37, 99, 235);
    public static ChartColor Cyan400 => ChartColor.FromRgb(34, 211, 238);
    public static ChartColor Teal400 => ChartColor.FromRgb(45, 212, 191);
    public static ChartColor Emerald400 => ChartColor.FromRgb(52, 211, 153);
    public static ChartColor Emerald500 => ChartColor.FromRgb(16, 185, 129);
    public static ChartColor Green400 => ChartColor.FromRgb(74, 222, 128);
    public static ChartColor Lime400 => ChartColor.FromRgb(163, 230, 53);
    public static ChartColor Amber400 => ChartColor.FromRgb(251, 191, 36);
    public static ChartColor Yellow400 => ChartColor.FromRgb(250, 204, 21);
    public static ChartColor Orange400 => ChartColor.FromRgb(251, 146, 60);
    public static ChartColor Red400 => ChartColor.FromRgb(248, 113, 113);
    public static ChartColor Rose400 => ChartColor.FromRgb(251, 113, 133);
    public static ChartColor Pink400 => ChartColor.FromRgb(244, 114, 182);
    public static ChartColor Violet400 => ChartColor.FromRgb(167, 139, 250);
    public static ChartColor Purple400 => ChartColor.FromRgb(192, 132, 252);
    public static ChartColor Indigo400 => ChartColor.FromRgb(129, 140, 248);
}

#pragma warning restore CS1591
