/// <summary>Creates a request sequence with participant notation, fragments, notes and activation.</summary>
public static class SequenceRecipe {
    /// <summary>Creates the expanded request-processing sequence model.</summary>
    /// <param name="variant">Use wide or expanded for the sequence.</param>
    /// <param name="mode">The rendering mode; diagram colors are resolved when the model is prepared.</param>
    public static SequenceArtifact Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "expanded"))
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Use wide or expanded.");

        var model = SequenceArtifact.Create("request-sequence").WithSize(960, 760)
            .AddParticipant("client", "Client", SequenceArtifactParticipantKind.Actor)
            .AddParticipant("service", "Service", SequenceArtifactParticipantKind.Control)
            .AddParticipant("store", "Store", SequenceArtifactParticipantKind.Database)
            .AddParticipant("queue", "Queue", SequenceArtifactParticipantKind.Queue)
            .AddMessage("client", "service", "Submit request")
            .AddMessage("service", "service", "Validate payload")
            .AddMessage("service", "store", "Persist result")
            .AddMessage("service", "queue", "Publish event", kind: SequenceArtifactMessageKind.Async)
            .AddMessage("service", "client", "Return result", SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return)
            .AddBlock(SequenceArtifactBlockKind.Alt, "Validation outcome", 1, 4)
            .AddBlock(SequenceArtifactBlockKind.Loop, "Retry storage", 2, 2, depth: 1)
            .AddActivation("service", true, 0)
            .AddActivation("service", false, 5)
            .AddNote(SequenceArtifactNotePlacement.Over, new[] { "service", "store" }, "Durable result");
        model.Notes[0].StepIndex = 2;
        return model;
    }
}
