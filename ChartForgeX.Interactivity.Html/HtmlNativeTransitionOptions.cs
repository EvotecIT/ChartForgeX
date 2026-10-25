using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Interactivity.Html;

/// <summary>The finite native presentation intent captured by the supported native value painters.</summary>
public enum HtmlNativeTransitionEffect {
    /// <summary>Interpolates one or more identified readings through their retained native painter.</summary>
    Values,
    /// <summary>Captures an explicit native baseline to the authoritative initial final scene.</summary>
    InitialValues,
    /// <summary>Paints accepted values with a bounded native value-ink flash returning to their exact final paint.</summary>
    ValueFlash,
    /// <summary>Composites the retained native whole-series Line/Bar visibility views.</summary>
    LegendVisibility,
    /// <summary>Fades one accepted immutable native visual from transparent to its exact final paint; no value or geometry morph is implied.</summary>
    InitialFade = 4,
    /// <summary>Changes native source values and absolute Reflow membership atomically using old/new native fade, zero switch and fade-in; no joint geometry morph is implied.</summary>
    SourceReflow = 5,
    /// <summary>Interpolates admitted native Line/Bar values and absolute whole-series Preserve visibility together through their existing native owners.</summary>
    ValuesAndVisibility = 6
}

/// <summary>Immutable native capture policy; browser duration remains an independent existing host presentation choice.</summary>
public sealed class HtmlNativeTransitionOptions {
    /// <summary>Creates finite native value capture options.</summary>
    /// <param name="effect">A supported native capture intent.</param>
    /// <param name="frameCount">Complete observations including endpoints;2..16, or3..16 for flash and SourceReflow.</param>
    /// <param name="flashColor">Explicit native peak paint, required only for ValueFlash.</param>
    public HtmlNativeTransitionOptions(HtmlNativeTransitionEffect effect = HtmlNativeTransitionEffect.Values, int frameCount = 12, ChartColor? flashColor = null) {
        if (!Enum.IsDefined(typeof(HtmlNativeTransitionEffect), effect)) throw new ArgumentOutOfRangeException(nameof(effect));
        if (frameCount < (effect is HtmlNativeTransitionEffect.ValueFlash or HtmlNativeTransitionEffect.SourceReflow ? 3 : 2) || frameCount > 16) throw new ArgumentOutOfRangeException(nameof(frameCount));
        if ((effect == HtmlNativeTransitionEffect.ValueFlash) != flashColor.HasValue)
            throw new ArgumentException("Only ValueFlash requires an explicit native peak color.", nameof(flashColor));
        Effect = effect; FrameCount = frameCount; FlashColor = flashColor;
    }
    /// <summary>Gets the supported native presentation intent.</summary>
    public HtmlNativeTransitionEffect Effect { get; }
    /// <summary>Gets the bounded number of complete native observations.</summary>
    public int FrameCount { get; }
    /// <summary>Gets the explicit peak native paint for ValueFlash.</summary>
    public ChartColor? FlashColor { get; }
}
