using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Typography;

/// <summary>The Unicode bidirectional character types (UAX #9, table 4).</summary>
internal enum BidiClass : byte {
    L, R, AL, EN, ES, ET, AN, CS, NSM, BN, B, S, WS, ON, LRE, LRO, RLE, RLO, PDF, LRI, RLI, FSI, PDI
}

/// <summary>
/// Bidirectional character data without the Unicode Character Database: explicit tables for the
/// formatting characters, separators, digits, and terminators UAX #9 treats specially, the
/// right-to-left script blocks, and the .NET general category for everything else (marks are NSM,
/// letters L, punctuation and symbols ON). The result matches <c>DerivedBidiClass.txt</c> for the
/// scripts and punctuation text uses in practice; rare symbols may differ.
/// </summary>
internal static class BidiCharacterData {
    private static readonly BidiClass[] Ascii = BuildAscii();
    private static readonly Dictionary<int, int> Mirrors = BuildMirrors();
    private static readonly Dictionary<int, int> OpeningBrackets = new();
    private static readonly Dictionary<int, int> ClosingBrackets = new();

    // Bidi_Paired_Bracket pairs (BidiBrackets.txt).
    private static readonly int[] BracketPairs = {
        0x0028, 0x0029, 0x005B, 0x005D, 0x007B, 0x007D, 0x0F3A, 0x0F3B, 0x0F3C, 0x0F3D, 0x169B, 0x169C,
        0x2045, 0x2046, 0x207D, 0x207E, 0x208D, 0x208E, 0x2308, 0x2309, 0x230A, 0x230B, 0x2329, 0x232A,
        0x2768, 0x2769, 0x276A, 0x276B, 0x276C, 0x276D, 0x276E, 0x276F, 0x2770, 0x2771, 0x2772, 0x2773,
        0x2774, 0x2775, 0x27C5, 0x27C6, 0x27E6, 0x27E7, 0x27E8, 0x27E9, 0x27EA, 0x27EB, 0x27EC, 0x27ED,
        0x27EE, 0x27EF, 0x2983, 0x2984, 0x2985, 0x2986, 0x2987, 0x2988, 0x2989, 0x298A, 0x298B, 0x298C,
        0x298D, 0x2990, 0x298F, 0x298E, 0x2991, 0x2992, 0x2993, 0x2994, 0x2995, 0x2996, 0x2997, 0x2998,
        0x29D8, 0x29D9, 0x29DA, 0x29DB, 0x29FC, 0x29FD, 0x2E22, 0x2E23, 0x2E24, 0x2E25, 0x2E26, 0x2E27,
        0x2E28, 0x2E29, 0x3008, 0x3009, 0x300A, 0x300B, 0x300C, 0x300D, 0x300E, 0x300F, 0x3010, 0x3011,
        0x3014, 0x3015, 0x3016, 0x3017, 0x3018, 0x3019, 0x301A, 0x301B, 0xFE59, 0xFE5A, 0xFE5B, 0xFE5C,
        0xFE5D, 0xFE5E, 0xFF08, 0xFF09, 0xFF3B, 0xFF3D, 0xFF5B, 0xFF5D, 0xFF5F, 0xFF60, 0xFF62, 0xFF63
    };

    static BidiCharacterData() {
        for (var i = 0; i + 1 < BracketPairs.Length; i += 2) {
            OpeningBrackets[BracketPairs[i]] = BracketPairs[i + 1];
            ClosingBrackets[BracketPairs[i + 1]] = BracketPairs[i];
        }
    }

    /// <summary>The bidirectional type of a code point.</summary>
    internal static BidiClass Classify(int cp) {
        if (cp < 0x80) return Ascii[cp < 0 ? 0 : cp];
        switch (cp) {
            case 0x0085: case 0x2029: return BidiClass.B;
            case 0x00A0: case 0x060C: case 0x202F: case 0x2044: case 0xFE50: case 0xFE52: case 0xFE55: case 0xFF0C: case 0xFF0E: case 0xFF0F: case 0xFF1A: return BidiClass.CS;
            case 0x00AD: case 0x180E: return BidiClass.BN;
            case 0x00B2: case 0x00B3: case 0x00B9: case 0x2070: return BidiClass.EN;
            case 0x00B0: case 0x00B1: case 0x0609: case 0x060A: case 0x066A: case 0x2213: case 0x212E: return BidiClass.ET;
            case 0x066B: case 0x066C: case 0x06DD: case 0x08E2: return BidiClass.AN;
            case 0x1680: case 0x2028: case 0x205F: case 0x3000: return BidiClass.WS;
            case 0x200E: return BidiClass.L;
            case 0x200F: return BidiClass.R;
            case 0x061C: return BidiClass.AL;
            case 0x202A: return BidiClass.LRE;
            case 0x202B: return BidiClass.RLE;
            case 0x202C: return BidiClass.PDF;
            case 0x202D: return BidiClass.LRO;
            case 0x202E: return BidiClass.RLO;
            case 0x2066: return BidiClass.LRI;
            case 0x2067: return BidiClass.RLI;
            case 0x2068: return BidiClass.FSI;
            case 0x2069: return BidiClass.PDI;
            case 0x207A: case 0x207B: case 0x208A: case 0x208B: case 0x2212: case 0xFB29: case 0xFE62: case 0xFE63: case 0xFF0B: case 0xFF0D: return BidiClass.ES;
            case 0x060E: case 0x060F: case 0x06DE: case 0x06E9: case 0xFD3E: case 0xFD3F: case 0xFDFD: return BidiClass.ON;
        }

        if (cp <= 0x009F) return BidiClass.BN;
        if (cp >= 0x2000 && cp <= 0x200A) return BidiClass.WS;
        if ((cp >= 0x0660 && cp <= 0x0669) || (cp >= 0x0600 && cp <= 0x0605) || (cp >= 0x10D30 && cp <= 0x10D39) || (cp >= 0x10E60 && cp <= 0x10E7E)) return BidiClass.AN;
        if ((cp >= 0x06F0 && cp <= 0x06F9) || (cp >= 0x2074 && cp <= 0x2079) || (cp >= 0x2080 && cp <= 0x2089) || (cp >= 0x2488 && cp <= 0x249B) ||
            (cp >= 0xFF10 && cp <= 0xFF19) || (cp >= 0x1D7CE && cp <= 0x1D7FF) || (cp >= 0x1F100 && cp <= 0x1F10A) || (cp >= 0x1FBF0 && cp <= 0x1FBF9)) return BidiClass.EN;
        if ((cp >= 0x2030 && cp <= 0x2034) || (cp >= 0xFF03 && cp <= 0xFF05) || cp == 0xFE5F || cp == 0xFE69 || cp == 0xFE6A) return BidiClass.ET;

        var category = Category(cp);
        if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.EnclosingMark) return BidiClass.NSM;
        if (IsArabicLetterBlock(cp)) return BidiClass.AL;
        if (IsRightToLeftBlock(cp)) return BidiClass.R;
        switch (category) {
            case UnicodeCategory.Format:
            case UnicodeCategory.Control:
                return BidiClass.BN;
            case UnicodeCategory.SpaceSeparator:
            case UnicodeCategory.LineSeparator:
                return BidiClass.WS;
            case UnicodeCategory.ParagraphSeparator:
                return BidiClass.B;
            case UnicodeCategory.CurrencySymbol:
                return BidiClass.ET;
            case UnicodeCategory.OtherNumber:
            case UnicodeCategory.ConnectorPunctuation:
            case UnicodeCategory.DashPunctuation:
            case UnicodeCategory.OpenPunctuation:
            case UnicodeCategory.ClosePunctuation:
            case UnicodeCategory.InitialQuotePunctuation:
            case UnicodeCategory.FinalQuotePunctuation:
            case UnicodeCategory.OtherPunctuation:
            case UnicodeCategory.MathSymbol:
            case UnicodeCategory.ModifierSymbol:
            case UnicodeCategory.OtherSymbol:
                return BidiClass.ON;
            default:
                return BidiClass.L;
        }
    }

    /// <summary>True for types that make a run need the bidirectional algorithm at all.</summary>
    internal static bool IsRightToLeftOrExplicit(BidiClass type) =>
        type == BidiClass.R || type == BidiClass.AL || type == BidiClass.AN || type >= BidiClass.LRE;

    /// <summary>The Bidi_Mirroring_Glyph of a code point, or the code point itself.</summary>
    internal static int Mirror(int cp) => Mirrors.TryGetValue(cp, out var mirrored) ? mirrored : cp;

    /// <summary>For an opening paired bracket, its closing bracket; otherwise -1.</summary>
    internal static int ClosingBracketFor(int cp) => OpeningBrackets.TryGetValue(Canonical(cp), out var closing) ? closing : -1;

    /// <summary>True for a closing paired bracket.</summary>
    internal static bool IsClosingBracket(int cp) => ClosingBrackets.ContainsKey(Canonical(cp));

    /// <summary>Angle brackets U+2329/U+232A are canonically equivalent to U+3008/U+3009 (BD16).</summary>
    internal static int Canonical(int cp) => cp == 0x2329 ? 0x3008 : cp == 0x232A ? 0x3009 : cp;

    internal static UnicodeCategory Category(int cp) =>
        cp <= 0xFFFF ? CharUnicodeInfo.GetUnicodeCategory((char)cp) : cp <= 0x10FFFF ? CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(cp), 0) : UnicodeCategory.OtherNotAssigned;

    // Arabic, Syriac, Thaana, their supplements and extensions, and the Arabic presentation forms.
    private static bool IsArabicLetterBlock(int cp) =>
        (cp >= 0x0600 && cp <= 0x07BF) || (cp >= 0x0860 && cp <= 0x08FF) || (cp >= 0xFB50 && cp <= 0xFDCF) || (cp >= 0xFDF0 && cp <= 0xFDFF) ||
        (cp >= 0xFE70 && cp <= 0xFEFE) || (cp >= 0x10D00 && cp <= 0x10D3F) || (cp >= 0x10EC0 && cp <= 0x10EFF) || (cp >= 0x10F30 && cp <= 0x10F6F) ||
        (cp >= 0x1EC70 && cp <= 0x1ECBF) || (cp >= 0x1ED00 && cp <= 0x1ED4F) || (cp >= 0x1EE00 && cp <= 0x1EEFF);

    // Hebrew, NKo, Samaritan, Mandaic, the Hebrew presentation forms, and the historic right-to-left scripts.
    private static bool IsRightToLeftBlock(int cp) =>
        (cp >= 0x0590 && cp <= 0x05FF) || (cp >= 0x07C0 && cp <= 0x085F) || (cp >= 0xFB1D && cp <= 0xFB4F) ||
        (cp >= 0x10800 && cp <= 0x10FFF) || (cp >= 0x1E800 && cp <= 0x1EFFF);

    private static BidiClass[] BuildAscii() {
        var table = new BidiClass[0x80];
        for (var i = 0; i < table.Length; i++) table[i] = BidiClass.ON;
        for (var i = 0x00; i <= 0x08; i++) table[i] = BidiClass.BN;
        for (var i = 0x0E; i <= 0x1B; i++) table[i] = BidiClass.BN;
        table[0x7F] = BidiClass.BN;
        table[0x09] = table[0x0B] = table[0x1F] = BidiClass.S;
        table[0x0A] = table[0x0D] = table[0x1C] = table[0x1D] = table[0x1E] = BidiClass.B;
        table[0x0C] = table[0x20] = BidiClass.WS;
        table['#'] = table['$'] = table['%'] = BidiClass.ET;
        table['+'] = table['-'] = BidiClass.ES;
        table[','] = table['.'] = table['/'] = table[':'] = BidiClass.CS;
        for (var i = '0'; i <= '9'; i++) table[i] = BidiClass.EN;
        for (var i = 'A'; i <= 'Z'; i++) table[i] = BidiClass.L;
        for (var i = 'a'; i <= 'z'; i++) table[i] = BidiClass.L;
        return table;
    }

    private static Dictionary<int, int> BuildMirrors() {
        // Bidi_Mirroring_Glyph pairs (BidiMirroring.txt) for brackets, quotation marks, and common relations.
        int[] pairs = {
            0x0028, 0x0029, 0x003C, 0x003E, 0x005B, 0x005D, 0x007B, 0x007D, 0x00AB, 0x00BB, 0x0F3A, 0x0F3B, 0x0F3C, 0x0F3D,
            0x169B, 0x169C, 0x2039, 0x203A, 0x2045, 0x2046, 0x207D, 0x207E, 0x208D, 0x208E, 0x2208, 0x220B, 0x2209, 0x220C,
            0x220A, 0x220D, 0x2215, 0x29F5, 0x223C, 0x223D, 0x2243, 0x22CD, 0x2252, 0x2253, 0x2254, 0x2255, 0x2264, 0x2265,
            0x2266, 0x2267, 0x2268, 0x2269, 0x226A, 0x226B, 0x226E, 0x226F, 0x2270, 0x2271, 0x2272, 0x2273, 0x2274, 0x2275,
            0x2276, 0x2277, 0x2278, 0x2279, 0x227A, 0x227B, 0x227C, 0x227D, 0x2280, 0x2281, 0x2282, 0x2283, 0x2284, 0x2285,
            0x2286, 0x2287, 0x2288, 0x2289, 0x228A, 0x228B, 0x228F, 0x2290, 0x2291, 0x2292, 0x22A2, 0x22A3, 0x22B0, 0x22B1,
            0x22B2, 0x22B3, 0x22B4, 0x22B5, 0x22D6, 0x22D7, 0x22D8, 0x22D9, 0x22DA, 0x22DB, 0x22DC, 0x22DD, 0x22DE, 0x22DF,
            0x2308, 0x2309, 0x230A, 0x230B, 0x2329, 0x232A, 0x2768, 0x2769, 0x276A, 0x276B, 0x276C, 0x276D, 0x276E, 0x276F,
            0x2770, 0x2771, 0x2772, 0x2773, 0x2774, 0x2775, 0x27C3, 0x27C4, 0x27C5, 0x27C6, 0x27E6, 0x27E7, 0x27E8, 0x27E9,
            0x27EA, 0x27EB, 0x27EC, 0x27ED, 0x27EE, 0x27EF, 0x2983, 0x2984, 0x2985, 0x2986, 0x2987, 0x2988, 0x2989, 0x298A,
            0x298B, 0x298C, 0x298D, 0x2990, 0x298E, 0x298F, 0x2991, 0x2992, 0x2993, 0x2994, 0x2995, 0x2996, 0x2997, 0x2998,
            0x29D8, 0x29D9, 0x29DA, 0x29DB, 0x29FC, 0x29FD, 0x2E02, 0x2E03, 0x2E04, 0x2E05, 0x2E09, 0x2E0A, 0x2E0C, 0x2E0D,
            0x2E1C, 0x2E1D, 0x2E20, 0x2E21, 0x2E22, 0x2E23, 0x2E24, 0x2E25, 0x2E26, 0x2E27, 0x2E28, 0x2E29, 0x3008, 0x3009,
            0x300A, 0x300B, 0x300C, 0x300D, 0x300E, 0x300F, 0x3010, 0x3011, 0x3014, 0x3015, 0x3016, 0x3017, 0x3018, 0x3019,
            0x301A, 0x301B, 0xFE59, 0xFE5A, 0xFE5B, 0xFE5C, 0xFE5D, 0xFE5E, 0xFE64, 0xFE65, 0xFF08, 0xFF09, 0xFF1C, 0xFF1E,
            0xFF3B, 0xFF3D, 0xFF5B, 0xFF5D, 0xFF5F, 0xFF60, 0xFF62, 0xFF63
        };
        var mirrors = new Dictionary<int, int>();
        for (var i = 0; i + 1 < pairs.Length; i += 2) {
            mirrors[pairs[i]] = pairs[i + 1];
            mirrors[pairs[i + 1]] = pairs[i];
        }

        return mirrors;
    }
}
