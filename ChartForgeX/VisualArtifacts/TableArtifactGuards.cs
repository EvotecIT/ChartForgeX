using System;

namespace ChartForgeX.VisualArtifacts;

internal static class TableArtifactGuards {
    private const TableArtifactCapabilities AllTableCapabilities =
        TableArtifactCapabilities.Search |
        TableArtifactCapabilities.Sort |
        TableArtifactCapabilities.Filter |
        TableArtifactCapabilities.SingleSelection |
        TableArtifactCapabilities.MultiSelection |
        TableArtifactCapabilities.CellSelection |
        TableArtifactCapabilities.Copy |
        TableArtifactCapabilities.Export |
        TableArtifactCapabilities.Virtualization;

    public static void EnumDefined<TEnum>(TEnum value, string parameterName) where TEnum : struct {
        if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be a defined enum member.");
    }

    public static void TableCapabilitiesDefined(TableArtifactCapabilities value, string parameterName) {
        if ((value & ~AllTableCapabilities) != 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must only contain defined table capability flags.");
    }

    public static void PositiveFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than zero.");
    }
}
