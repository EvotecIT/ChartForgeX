using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

internal static class BuiltInMapDefinitions {
    private static ChartMapDefinition? _usStates;
    private static IReadOnlyList<ChartMapDefinition>? _all;

    public static IReadOnlyList<ChartMapDefinition> All() {
        return _all ??= Array.AsReadOnly(new[] { UsStates() });
    }

    public static bool TryGet(string id, out ChartMapDefinition definition) {
        foreach (var candidate in All()) {
            if (string.Equals(candidate.Id, id, System.StringComparison.OrdinalIgnoreCase)) {
                definition = candidate;
                return true;
            }
        }

        definition = null!;
        return false;
    }

    public static ChartMapDefinition UsStates() {
        if (_usStates != null) return _usStates;
        var regions = new List<ChartMapRegion>(UsStateGeoShapes.Shapes.Length);
        foreach (var shape in UsStateGeoShapes.Shapes) {
            var name = UsStateName(shape.Code);
            regions.Add(new ChartMapRegion(shape.Code, name, shape.Path, shape.Label, UsStateAliases(shape.Code, name)));
        }

        _usStates = new ChartMapDefinition("us-states", "United States states", UsStateGeoShapes.Bounds, regions);
        return _usStates;
    }

    internal static IEnumerable<string> UsStateAliases(string code, string name) {
        yield return name;
        if (code == "DC") {
            yield return "D.C.";
            yield return "Washington DC";
            yield return "Washington D.C.";
        }
    }

    internal static string UsStateName(string code) {
        return code switch {
            "AL" => "Alabama",
            "AK" => "Alaska",
            "AZ" => "Arizona",
            "AR" => "Arkansas",
            "CA" => "California",
            "CO" => "Colorado",
            "CT" => "Connecticut",
            "DE" => "Delaware",
            "DC" => "District of Columbia",
            "FL" => "Florida",
            "GA" => "Georgia",
            "HI" => "Hawaii",
            "ID" => "Idaho",
            "IL" => "Illinois",
            "IN" => "Indiana",
            "IA" => "Iowa",
            "KS" => "Kansas",
            "KY" => "Kentucky",
            "LA" => "Louisiana",
            "ME" => "Maine",
            "MD" => "Maryland",
            "MA" => "Massachusetts",
            "MI" => "Michigan",
            "MN" => "Minnesota",
            "MS" => "Mississippi",
            "MO" => "Missouri",
            "MT" => "Montana",
            "NE" => "Nebraska",
            "NV" => "Nevada",
            "NH" => "New Hampshire",
            "NJ" => "New Jersey",
            "NM" => "New Mexico",
            "NY" => "New York",
            "NC" => "North Carolina",
            "ND" => "North Dakota",
            "OH" => "Ohio",
            "OK" => "Oklahoma",
            "OR" => "Oregon",
            "PA" => "Pennsylvania",
            "RI" => "Rhode Island",
            "SC" => "South Carolina",
            "SD" => "South Dakota",
            "TN" => "Tennessee",
            "TX" => "Texas",
            "UT" => "Utah",
            "VT" => "Vermont",
            "VA" => "Virginia",
            "WA" => "Washington",
            "WV" => "West Virginia",
            "WI" => "Wisconsin",
            "WY" => "Wyoming",
            _ => code
        };
    }
}
