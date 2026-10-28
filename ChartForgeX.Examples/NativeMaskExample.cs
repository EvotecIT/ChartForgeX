/// <summary>A complete Core-only chart uses a detached alpha mask; source import is a separate explicit workflow.</summary>
public static class NativeMaskExample {
    /// <summary>Creates compact/wide native captures in the selected paired theme.</summary>
    public static CapturedVisualLayer Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var source = PrepareSource(variant, mode); var width = variant == "compact" ? 320 : 740;
        var pixels = new byte[width * 240 * 4];
        for (var y = 0; y < 240; y++) for (var x = 0; x < width; x++) {
            var at = (y * width + x) * 4;
            pixels[at] = pixels[at + 1] = pixels[at + 2] = 255;
            // Author an ordinary fixed-grid fade over the final quarter; source facts and labels stay with Source.
            pixels[at + 3] = (byte)(x < width * 3 / 4 ? 255 : 255 - (x - width * 3 / 4) * 160 / (width / 4));
        }
        return source.CaptureLayer(new ChartForgeX.Effects.VisualMask(new ChartForgeX.Raster.RgbaImage(width, 240, pixels)),
            new VisualRenderOptions(supersampling: 2));
    }
    /// <summary>Returns the ordinary detached native chart before mask capture.</summary>
    public static PreparedVisual PrepareSource(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var width = variant == "compact" ? 320 : 740; var theme = VisualTheme.Graphite();
        var series = ChartSeries.CreateLine(new ChartSeriesId("finished"), "Finished",
            new[] { new ChartPoint(1, 9), new ChartPoint(2, 24), new ChartPoint(3, 17), new ChartPoint(4, 31) })
            .WithObservationId(1, new ChartObservationId("second-cycle"));
        var chart = Chart.Create().WithSize(width, 240).WithTheme(theme, mode).WithDataLabels(false)
            .WithXAxis("Cycle").WithYAxis("Finished").AddSeries(series);
        return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 240), 24), theme, mode,
            new VisualFrame("Finished service requests", "The final cycles fade; the prepared source retains every observation",
                showLegend: false, transparentBackground: true)));
    }
    /// <summary>Returns the finite source-only flood/backdrop blend, independent of the native capture above.</summary>
    public static string FilterSource(string mode = "screen") {
        if (mode is not ("normal" or "multiply" or "screen" or "overlay" or "darken" or "lighten" or "color-dodge" or "color-burn" or "hard-light" or "soft-light" or "difference" or "exclusion" or "hue" or "saturation" or "color" or "luminosity")) throw new ArgumentOutOfRangeException(nameof(mode));
        return "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><defs>"
            + "<filter id='blend' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' color-interpolation-filters='sRGB'>"
            + "<feFlood flood-color='#0000ff' flood-opacity='0.5019607843137255' result='backdrop'/>"
            + "<feBlend in='SourceGraphic' in2='backdrop' mode='" + mode + "'/></filter></defs>"
            + "<rect id='source' x='8' y='8' width='16' height='16' fill='#ff0000' fill-opacity='0.5019607843137255' filter='url(#blend)'/></svg>";
    }
}
