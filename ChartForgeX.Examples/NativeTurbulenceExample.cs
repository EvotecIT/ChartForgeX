/// <summary>Complete Core-only native procedural source, generated image and mask consumption.</summary>
public static class NativeTurbulenceExample {
    public static ChartForgeX.Effects.VisualTurbulence Policy() => new(.017, .029, 3, 13,
        ChartForgeX.Effects.VisualTurbulenceType.FractalNoise, stitchTiles: true,
        colorSpace: ChartForgeX.Effects.VisualFilterColorSpace.Srgb);

    public static PreparedVisual Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var width = Width(variant);
        return Policy().Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 300), 24),
            VisualTheme.Graphite(), mode,
            new VisualFrame("Noise as a source", "One retained four-channel procedural surface", showLegend: false)));
    }

    public static ChartForgeX.Raster.RgbaImage Generate(string variant = "wide") =>
        Policy().Generate(new VisualSize(Width(variant), 160));

    public static PreparedVisual MaskSource(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var width = Width(variant);
        var chart = Chart.Create().WithSize(width, 160).WithTheme(VisualTheme.Graphite(), mode)
            .WithLegend(false).WithDataLabels(false).AddLine("Signal", new[] {
                new ChartPoint(1, 2), new ChartPoint(2, 8), new ChartPoint(3, 5), new ChartPoint(4, 9)
            });
        var source = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 160), 16),
            VisualTheme.Graphite(), mode, new VisualFrame(transparentBackground: true, showLegend: false)));
        // Generate owns mutable pixels; VisualMask captures its independent immutable input on this ordinary public boundary.
        return source.WithMask(new ChartForgeX.Effects.VisualMask(Generate(variant), orderedDither: false));
    }

    /// <summary>Saves immutable source policy and source identity through the common typed envelope.</summary>
    public static string Save(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var envelope = Create(variant,mode).ToArtifact("noise-source").ToInterchangeEnvelope();
        envelope.Extensions["sample-origin"] = "native-procedural-source";
        return envelope.ToJson();
    }

    /// <summary>Loads through an explicit fresh same-size native context, without decoding paint into a scene.</summary>
    public static PreparedVisual Load(string json, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) =>
        VisualArtifactInterchangeEnvelope.FromJson(json).PrepareTurbulenceSource(SourceContext(variant,mode));

    /// <summary>Rebuilds one immutable authored seed and prepares again through the same source owner.</summary>
    public static PreparedVisual Edit(string json, double seed, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        var presentation = envelope.Presentation ?? throw new ArgumentException("A typed source is required.",nameof(json));
        var policy = presentation.TurbulenceSource ?? throw new ArgumentException("A typed source is required.",nameof(json));
        presentation.TurbulenceSource = new ChartForgeX.Effects.VisualTurbulence(
            policy.BaseFrequencyX,policy.BaseFrequencyY,policy.NumberOfOctaves,seed,
            policy.Type,policy.StitchTiles,policy.ColorSpace);
        envelope.Extensions["sample-edit"] = "seed";
        return envelope.PrepareTurbulenceSource(SourceContext(variant,mode));
    }

    private static VisualRenderContext SourceContext(string variant, VisualThemeMode mode) =>
        new(new VisualLayoutOptions(new VisualSize(Width(variant),300),24),VisualTheme.Graphite(),mode,
            new VisualFrame("Noise as a source","One retained four-channel procedural surface",showLegend:false));

    /// <summary>Defines one source-independent transformed procedural SVG region for ordinary native import.</summary>
    public static string AffineSource(string name) {
        var transform = name switch {
            "rotate" => "matrix(.8,.6,-.6,.8,32,8)",
            "skew" => "matrix(1,.25,.5,1,8,8)",
            "reflect" => "matrix(-1,0,0,1,48,8)",
            "quarter-turn" => "matrix(0,1,-1,0,48,8)",
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        var policy = Policy();
        string Number(double value) => value.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        return "<svg xmlns='http://www.w3.org/2000/svg' width='96' height='72'><defs>" +
            "<filter id='noise' x='0' y='0' width='24' height='16' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='sRGB'>" +
            "<feTurbulence baseFrequency='" + Number(policy.BaseFrequencyX) + " " + Number(policy.BaseFrequencyY) +
            "' numOctaves='" + policy.NumberOfOctaves.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            "' seed='" + Number(policy.Seed) + "' type='fractalNoise' stitchTiles='stitch'/></filter></defs>" +
            "<rect width='24' height='16' fill='white' transform='" + transform + "' filter='url(#noise)'/></svg>";
    }

    /// <summary>Samples the same finite source through the public native SVG importer; no external renderer is consulted.</summary>
    public static ChartForgeX.Raster.RgbaImage RasterizeAffine(string name) =>
        ChartForgeX.SvgRaster.SvgRasterizer.Rasterize(AffineSource(name), strict: true,
            filters: new ChartForgeX.SvgRaster.SvgRasterFilterOptions()).Image;

    private static int Width(string variant) => variant == "compact" ? 390 : variant == "wide" ? 760
        : throw new ArgumentOutOfRangeException(nameof(variant));
}
