using System;

namespace ChartForgeX.Core;

public static partial class ChartMapGeoJson {
    // Compute suffix costs without constructing a copy of the feature name for every position.
    private static int PartLabelLength(int labelLength, int part, bool multiple) {
        if (!multiple) return labelLength;
        var ordinal = (long)part + 1;
        var digits = 1;
        while (ordinal >= 10) { digits++; ordinal /= 10; }
        return checked(labelLength + digits + 3);
    }

    private sealed class ObservationImportBudget {
        private long _observations;
        private long _labelCharacters;

        internal ObservationImportBudget(ChartMapGeoJsonOptions options) {
            _observations = options.MaximumObservations;
            _labelCharacters = options.MaximumExpandedLabelCharacters;
        }

        internal void Reserve(long observations, long labelCharacters) {
            if (observations > _observations)
                throw new ArgumentException("GeoJSON observation import exceeds MaximumObservations.", "geoJson");
            if (labelCharacters > _labelCharacters)
                throw new ArgumentException("GeoJSON observation import exceeds MaximumExpandedLabelCharacters.", "geoJson");
            _observations -= observations;
            _labelCharacters -= labelCharacters;
        }
    }
}
