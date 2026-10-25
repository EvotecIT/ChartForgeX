/// <summary>Core-only alpha-outline, alpha-glow and luminance-bloom captures retain their editable chart facts separately.</summary>
public static class NativeAppearanceExample {
    public static CapturedVisualLayer Create(string effect = "bloom", string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var source = PrepareSource(variant, mode);
        var options = new VisualRenderOptions(supersampling: 1);
        if (effect == "outline") return source.CaptureLayer(new ChartForgeX.Effects.VisualOutline(
            ChartColor.FromHex(mode == VisualThemeMode.Dark ? "#FBBF24" : "#C2410C"), 2), options);
        if (effect == "inner") return source.CaptureLayer(InnerShadowPolicy(mode), options);
        if (effect == "glow") return source.CaptureLayer(AlphaGlowPolicy(mode), options);
        if (effect == "bloom") return source.CaptureLayer(new ChartForgeX.Effects.VisualBloom(
            3, threshold: .35, strength: .8), options);
        throw new ArgumentException("Select outline, glow, inner or bloom.", nameof(effect));
    }

    /// <summary>Returns this example's finite zero-offset alpha-glow recipe through the existing immutable shadow request.</summary>
    public static ChartForgeX.Effects.VisualDropShadow AlphaGlowPolicy(VisualThemeMode mode = VisualThemeMode.Light) {
        if (mode != VisualThemeMode.Light && mode != VisualThemeMode.Dark) throw new ArgumentOutOfRangeException(nameof(mode));
        return new ChartForgeX.Effects.VisualDropShadow(
            ChartColor.FromHex(mode == VisualThemeMode.Dark ? "#38BDF8" : "#0369A1"),
            0, 0, 3, spread: 1, orderedDither: false, opacity: .6);
    }

    /// <summary>Returns the same finite alpha-glow policy as an explicit source-import example.</summary>
    public static string AlphaGlowFilterSource(VisualThemeMode mode = VisualThemeMode.Light) {
        var policy = AlphaGlowPolicy(mode);
        return "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='48'><defs>"
            + "<filter id='alpha-glow' x='0' y='0' width='64' height='48' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>"
            + "<feMorphology in='SourceAlpha' operator='dilate' radius='1' result='spread'/>"
            + "<feGaussianBlur in='spread' stdDeviation='3' edgeMode='none' result='soft'/>"
            + "<feOffset in='soft' dx='0' dy='0' result='shifted'/><feFlood flood-color='" + policy.Color.ToCss()
            + "' flood-opacity='" + policy.Opacity.ToString("R", CultureInfo.InvariantCulture) + "' result='tint'/>"
            + "<feComposite in='tint' in2='shifted' operator='in' result='shadowColor'/>"
            + "<feMerge><feMergeNode in='shadowColor'/><feMergeNode in='SourceGraphic'/></feMerge></filter></defs>"
            + "<rect id='alpha-glow-source' x='24' y='16' width='8' height='8' fill='#101010' fill-opacity='0.5019607843137255' filter='url(#alpha-glow)'/></svg>";
    }

    /// <summary>Returns this example's finite alpha-preserving inset policy; positive spread thickens the inset.</summary>
    public static ChartForgeX.Effects.VisualInnerShadow InnerShadowPolicy(VisualThemeMode mode = VisualThemeMode.Light) {
        if (mode != VisualThemeMode.Light && mode != VisualThemeMode.Dark) throw new ArgumentOutOfRangeException(nameof(mode));
        return new ChartForgeX.Effects.VisualInnerShadow(
            ChartColor.FromHex(mode == VisualThemeMode.Dark ? "#0F172A" : "#075985"),
            2, 2, 2, spread: 1, orderedDither: false, opacity: .65);
    }

    public static PreparedVisual PrepareSource(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant != "compact" && variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        if (mode != VisualThemeMode.Light && mode != VisualThemeMode.Dark) throw new ArgumentOutOfRangeException(nameof(mode));
        var width = variant == "compact" ? 390 : 760;
        var theme = VisualTheme.Graphite();
        var chart = Chart.Create().WithSize(width, 280).WithTheme(theme, mode)
            .WithTitle("Capacity signals").WithXAxis("Cycle").WithYAxis("Ready")
            .WithDataLabels(false).AddLine("Completed", new[] {
                new ChartPoint(1, 4), new ChartPoint(2, 8), new ChartPoint(3, 6), new ChartPoint(4, 12)
            });
        return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 280), 24),
            theme, mode, new VisualFrame(showLegend: false, transparentBackground: true)));
    }
}
