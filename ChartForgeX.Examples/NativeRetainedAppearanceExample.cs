/// <summary>Core-only retained vector presentation and explicit source/output-frame save-load-edit workflow.</summary>
public static class NativeRetainedAppearanceExample {
    public static PreparedVisual PrepareSource(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var width = Width(variant);
        var model = SequenceArtifact.Create("retained-appearance").WithSize(width, 420)
            .AddParticipant("review", "Review").AddParticipant("archive", "Archive")
            .AddMessage("review", "archive", "Save").AddMessage("archive", "review", "Stored");
        model.Participants[0].Href = "#guide";
        model.Theme = VisualTheme.Graphite(); model.ThemeMode = mode;
        model.AddNote(SequenceArtifactNotePlacement.Over, new[] { "review", "archive" }, "Facts stay editable");
        model.Notes[0].StepIndex = 1;
        return model.Prepare(Context(width, mode));
    }

    public static PreparedVisual Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) =>
        PrepareSource(variant, mode)
            .WithDropShadow(new ChartForgeX.Effects.VisualDropShadow(ChartColor.Black, 2, 3, 1, opacity: .15))
            .WithOutline(new ChartForgeX.Effects.VisualOutline(ChartColor.FromHex(mode == VisualThemeMode.Dark ? "#38BDF8" : "#0369A1"), 1))
            .WithBloom(new ChartForgeX.Effects.VisualBloom(1, threshold: .7, strength: .3));

    public static CapturedVisualLayer Capture(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var source = Create(variant, mode);
        var layer = source.CaptureLayer(new ChartForgeX.Effects.VisualLayerBlur(1, orderedDither: false),
            new VisualRenderOptions(supersampling: 1));
        var width = checked((int)source.Size.Width + 16); var height = checked((int)source.Size.Height + 16);
        var pixels = new byte[checked(width * height * 4)];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var at = (y * width + x) * 4;
            pixels[at] = 37; pixels[at + 1] = 123; pixels[at + 2] = 219;
            pixels[at + 3] = (byte)(255 - x * 96 / width);
        }
        return layer.WithMask(new ChartForgeX.Effects.VisualMask(new ChartForgeX.Raster.RgbaImage(width, height, pixels),
            new ChartPoint(-8, -8), orderedDither: false));
    }

    public static PreparedVisual RestoreRoot(string saved, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light,
        string? messageText = null) {
        var (envelope, source) = RestoreSource(saved, variant, mode, messageText);
        return envelope.ApplyCapturedEffects(source);
    }

    public static CapturedVisualLayer RestoreLayer(string saved, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light,
        string? messageText = null) {
        var (envelope, source) = RestoreSource(saved, variant, mode, messageText);
        return envelope.ApplyCapturedLayer(source);
    }

    private static (VisualArtifactInterchangeEnvelope Envelope, PreparedVisual Source) RestoreSource(string saved,
        string variant, VisualThemeMode mode, string? messageText) {
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(saved);
        var facts = VisualArtifactInterchangeEnvelope.FromJson(saved);
        facts.Presentation!.CapturedEffects.Clear();
        var model = facts.ToSequenceArtifact();
        if (messageText != null) model.Messages[0].Text = messageText;
        return (envelope, model.Prepare(Context(Width(variant), mode)));
    }

    private static int Width(string variant) => variant == "compact" ? 390 : variant == "wide" ? 760
        : throw new ArgumentOutOfRangeException(nameof(variant));
    private static VisualRenderContext Context(int width, VisualThemeMode mode) =>
        new(new VisualLayoutOptions(new VisualSize(width, 420), 24), VisualTheme.Graphite(), mode,
            new VisualFrame("Service review", showLegend: false, transparentBackground: true));
}
