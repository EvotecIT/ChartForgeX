namespace ChartForgeX.Typography;

internal enum IndicRephMode { Implicit, Explicit, Logical }

/// <summary>Script-specific choices in the OpenType Indic reordering model, independent of any face.</summary>
internal readonly struct IndicScriptProfile {
    internal IndicScriptProfile(string modernTag, int halant, int ra, byte reph, byte top = 7, byte bottom = 7, byte right = 9, IndicRephMode mode = IndicRephMode.Implicit) {
        ModernTag = modernTag; Halant = halant; Ra = ra; RephPosition = reph;
        TopMatra = top; BottomMatra = bottom; RightMatra = right;
        RephMode = mode;
    }
    internal readonly string ModernTag;
    internal readonly int Halant, Ra;
    internal readonly byte RephPosition, TopMatra, BottomMatra, RightMatra;
    internal readonly IndicRephMode RephMode;
    internal static bool TryGet(string script, out IndicScriptProfile profile) {
        profile = script switch {
            "deva" => new("dev2", 0x094d, 0x0930, 8, right: 7),
            "beng" => new("bng2", 0x09cd, 0x09b0, 9),
            "guru" => new("gur2", 0x0a4d, 0x0a30, 5),
            "gujr" => new("gjr2", 0x0acd, 0x0ab0, 8, right: 7),
            "orya" => new("ory2", 0x0b4d, 0x0b30, 5),
            "taml" => new("tml2", 0x0bcd, 0x0bb0, 9, top: 9, bottom: 9),
            "telu" => new("tel2", 0x0c4d, 0x0c30, 9, top: 5, mode: IndicRephMode.Explicit),
            "knda" => new("knd2", 0x0ccd, 0x0cb0, 9, top: 5),
            "mlym" => new("mlm2", 0x0d4d, 0x0d30, 5, top: 9, bottom: 9, mode: IndicRephMode.Logical),
            _ => default
        };
        return profile.ModernTag != null;
    }
}
