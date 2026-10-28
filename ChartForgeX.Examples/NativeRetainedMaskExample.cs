/// <summary>Saves, edits and explicitly replays a fixed-grid native mask with retained vector SVG and typed source facts.</summary>
public static class NativeRetainedMaskExample {
    /// <summary>Creates the same detached retained presentation used by the downloadable Core-only program.</summary>
    public static PreparedVisual Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        var source = PrepareSource(variant, mode);
        return source.WithMask(CreateMask(source));
    }

    /// <summary>Prepares the ordinary source before presentation masking; its JSON remains source facts only.</summary>
    public static PreparedVisual PrepareSource(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var width = variant == "compact" ? 390 : 1100;
        var theme = VisualTheme.Graphite();
        var sequence = SequenceArtifact.Create("native-retained-mask").WithSize(width, 620)
            .AddParticipant("review", "Review").AddParticipant("archive", "Archive")
            .AddMessage("review", "archive", "Save").AddMessage("archive", "review", "Stored");
        sequence.Theme = theme;
        sequence.ThemeMode = mode;
        sequence.Participants[0].Href = "#guide";
        sequence.AddNote(SequenceArtifactNotePlacement.Over, new[] { "review", "archive" }, "Retain source facts");
        sequence.Notes[0].StepIndex = 1;
        return sequence.Prepare(Context(width, theme, mode));
    }

    /// <summary>Restores editable source explicitly, optionally changes its first message, and reapplies the saved captured mask.</summary>
    /// <remarks>The caller retains the same authored example context; generic mask replay itself never chooses layout or theme.</remarks>
    public static PreparedVisual Restore(string saved, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light,
        string? messageText = null) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var captured = VisualArtifactInterchangeEnvelope.FromJson(saved);
        var sourceFacts = VisualArtifactInterchangeEnvelope.FromJson(saved);
        sourceFacts.Presentation!.CapturedEffects.Clear(); // Deliberate source-only editing on a detached copy.
        var model = sourceFacts.ToSequenceArtifact();
        if (messageText != null) model.Messages[0].Text = messageText;
        var width = variant == "compact" ? 390 : 1100;
        var source = model.Prepare(Context(width, VisualTheme.Graphite(), mode));
        return captured.ApplyCapturedEffects(source);
    }

    private static VisualRenderContext Context(int width, VisualTheme theme, VisualThemeMode mode) =>
        new(new VisualLayoutOptions(new VisualSize(width, 620), 24), theme, mode,
            new VisualFrame("Masked service review", "The right side fades; vector captions and links remain in the native SVG profile",
                showLegend: false, transparentBackground: true));

    /// <summary>Creates a detached bitmap input in the prepared viewport's existing native grid.</summary>
    public static ChartForgeX.Effects.VisualMask CreateMask(PreparedVisual source) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var width = checked((int)Math.Ceiling(source.Size.Width));
        var height = checked((int)Math.Ceiling(source.Size.Height));
        var pixels = new byte[checked(width * height * 4)];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var at = (y * width + x) * 4;
                pixels[at] = pixels[at + 1] = pixels[at + 2] = 255;
                pixels[at + 3] = (byte)(x < width * 3 / 4 ? 255 :
                    255 - (x - width * 3 / 4) * 144 / (width - width * 3 / 4));
            }
        }
        return new ChartForgeX.Effects.VisualMask(new ChartForgeX.Raster.RgbaImage(width, height, pixels), orderedDither: false);
    }
}
