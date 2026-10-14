/// <summary>Creates a two-participant exchange with a multiline request and dashed return.</summary>
public static class SimpleSequenceRecipe {
    /// <summary>Creates the simple client and server sequence model.</summary>
    /// <param name="variant">Use wide or feasibility for the simple sequence.</param>
    /// <param name="mode">The rendering mode; diagram colors are resolved when the model is prepared.</param>
    public static SequenceArtifact Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "feasibility"))
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Use wide or feasibility.");

        return SequenceArtifact.Create("session").WithSize(640, 400)
            .AddParticipant("client", "Client")
            .AddParticipant("server", "Server")
            .AddMessage("client", "server", "Request\npayload")
            .AddMessage("server", "client", "Response", SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return);
    }
}
