namespace ChartForgeX.VisualArtifacts;

internal static class VisualArtifactInterchangeValueBudget {
    public static long Remaining(VisualArtifactInterchangeEnvelope envelope) =>
        VisualArtifactInterchangeEnvelope.MaximumJsonValues - VisualArtifactInterchangeJson.CountValues(envelope);

    public static void Validate(VisualArtifactInterchangeEnvelope envelope) {
        long count = VisualArtifactInterchangeJson.CountValues(envelope);
        if (count > VisualArtifactInterchangeEnvelope.MaximumJsonValues) {
            throw new System.ArgumentOutOfRangeException(nameof(envelope), count, "Interchange envelopes must not exceed " + VisualArtifactInterchangeEnvelope.MaximumJsonValues + " materialized JSON values.");
        }
    }
}
