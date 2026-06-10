using System;

namespace ChartForgeX.VisualArtifacts;

internal static class VisualArtifactGuards {
    private const VisualArtifactExportFormat AllExportFormats =
        VisualArtifactExportFormat.Svg |
        VisualArtifactExportFormat.Png |
        VisualArtifactExportFormat.Html |
        VisualArtifactExportFormat.Csv |
        VisualArtifactExportFormat.Json |
        VisualArtifactExportFormat.Markdown |
        VisualArtifactExportFormat.Office;

    public static void ExportFormatsDefined(VisualArtifactExportFormat value, string parameterName) {
        if ((value & ~AllExportFormats) != 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must only contain defined visual artifact export format flags.");
    }

    public static void PositiveFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than zero.");
    }

    public static void NonNegativeFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and non-negative.");
    }
}
