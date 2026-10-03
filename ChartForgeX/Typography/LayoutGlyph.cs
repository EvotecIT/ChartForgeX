using System;
using System.Globalization;

namespace ChartForgeX.Typography;

/// <summary>A mutable glyph during font layout. Positions and advances are in its face's design units.</summary>
internal sealed class LayoutGlyph {
    internal LayoutGlyph(ushort glyph, int codePoint, int cluster) {
        Glyph = glyph; CodePoint = codePoint; Cluster = cluster;
        var category = BidiCharacterData.Category(codePoint);
        IsMark = category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark;
        Ignorable = TextShaper.IsIgnorable(codePoint);
    }
    internal ushort Glyph;
    internal readonly int CodePoint;
    internal int Cluster;
    internal readonly bool IsMark;
    internal bool Ignorable;
    internal bool SkipForSubstitution;
    internal uint Features = uint.MaxValue;
    internal double XAdvance, YAdvance, XOffset, YOffset;
    internal LayoutGlyph? Ligature;
    internal int Component;
    internal int[]? ComponentClusters;
    internal LayoutGlyph? Attachment;
    internal double AnchorX, AnchorY, AttachmentAdjustmentX, AttachmentAdjustmentY;
    internal bool CursiveAttachment;
    internal LayoutGlyph Copy(ushort glyph) => new(glyph, CodePoint, Cluster) {
        Ignorable = Ignorable, SkipForSubstitution = SkipForSubstitution, Features = Features, Ligature = Ligature, Component = Component
    };
    internal static uint FeatureMask(string feature) => feature switch {
        "isol" => 1u, "fina" => 2u, "init" => 4u, "medi" => 8u,
        "rphf" => 16u, "half" => 32u, "blwf" => 64u, "pstf" => 128u,
        "pref" => 256u, "vatu" => 512u, _ => 0u
    };
    internal bool Allows(string feature) { var mask = FeatureMask(feature); return mask == 0 || (Features & mask) != 0; }
}
