/// <summary>A complete Core-only prepared chart receives one captured spread/blurred alpha shadow.</summary>
public static class NativeDropShadowExample {
    /// <summary>Creates compact or wide output with explicit paired theme, literal shadow and retained source identities.</summary>
    public static CapturedVisualLayer Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var width = variant == "compact" ? 320 : 740; var theme = VisualTheme.Graphite();
        var source = ChartFor(variant, mode).Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 240), 24),
            theme, mode, new VisualFrame(showLegend: false, transparentBackground: true)));
        var tint = mode == VisualThemeMode.Dark ? ChartColor.FromRgba(96, 165, 250, 96) : ChartColor.FromRgba(15, 23, 42, 112);
        return source.CaptureLayer(new ChartForgeX.Effects.VisualDropShadow(tint, 3, 5, 2.5, 1.5, spread: 1),
            new VisualRenderOptions(supersampling: 2));
    }
    /// <summary>Creates the ordinary editable source chart separately from its static captured layer.</summary>
    public static Chart ChartFor(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var series = new ChartSeries(new ChartSeriesId("finished"), "Finished", ChartSeriesKind.Line,
            new[] { new ChartPoint(1, 9), new ChartPoint(2, 24), new ChartPoint(3, 17), new ChartPoint(4, 31) })
            .WithObservationId(1, new ChartObservationId("second-cycle"));
        return Chart.Create().WithSize(variant == "compact" ? 320 : 740, 240).WithTheme(VisualTheme.Graphite(), mode)
            .WithTitle("Finished service requests").WithSubtitle("A native chart and its caption form one captured unit")
            .WithXAxis("Cycle").WithYAxis("Finished").WithDataLabels(false).AddSeries(series);
    }
    /// <summary>Returns the finite source filter used by the ordinary opt-in SVG importer example.</summary>
    public static string FilterSource() => "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='48'><defs>"
        + "<filter id='shadow' x='0' y='0' width='64' height='48' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>"
        + "<feMorphology in='SourceAlpha' operator='dilate' radius='1' result='spread'/>"
        + "<feGaussianBlur in='spread' stdDeviation='0' edgeMode='none' result='soft'/>"
        + "<feOffset in='soft' dx='4' dy='2' result='shifted'/><feFlood flood-color='#0000ff' flood-opacity='.5' result='tint'/>"
        + "<feComposite in='tint' in2='shifted' operator='in' result='shadowColor'/>"
        + "<feMerge><feMergeNode in='shadowColor'/><feMergeNode in='SourceGraphic'/></feMerge></filter></defs>"
        + "<rect id='source' x='24' y='16' width='8' height='8' fill='#ff0000' fill-opacity='0.5019607843137255' filter='url(#shadow)'/></svg>";
}
