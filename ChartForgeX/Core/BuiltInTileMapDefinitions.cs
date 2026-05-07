using System.Collections.Generic;

namespace ChartForgeX.Core;

internal static class BuiltInTileMapDefinitions {
    private static ChartTileMapDefinition? _usStates;
    private static ChartTileMapDefinition[]? _all;

    public static IReadOnlyList<ChartTileMapDefinition> All() {
        return _all ??= new[] { UsStates() };
    }

    public static bool TryGet(string id, out ChartTileMapDefinition definition) {
        foreach (var candidate in All()) {
            if (string.Equals(candidate.Id, id, System.StringComparison.OrdinalIgnoreCase)) {
                definition = candidate;
                return true;
            }
        }

        definition = null!;
        return false;
    }

    public static ChartTileMapDefinition UsStates() {
        if (_usStates != null) return _usStates;
        var regions = new List<ChartTileMapRegion>(UsStateTiles.Length);
        foreach (var tile in UsStateTiles) {
            var name = BuiltInMapDefinitions.UsStateName(tile.Code);
            regions.Add(new ChartTileMapRegion(tile.Code, name, tile.Column, tile.Row, BuiltInMapDefinitions.UsStateAliases(tile.Code, name)));
        }

        _usStates = new ChartTileMapDefinition("us-states", "United States states", regions);
        return _usStates;
    }

    private readonly struct BuiltInTile {
        public readonly string Code;
        public readonly int Column;
        public readonly int Row;

        public BuiltInTile(string code, int column, int row) {
            Code = code;
            Column = column;
            Row = row;
        }
    }

    private static readonly BuiltInTile[] UsStateTiles = {
        new("AK", 0, 0), new("ME", 11, 0),
        new("VT", 9, 1), new("NH", 10, 1),
        new("WA", 0, 2), new("MT", 1, 2), new("ND", 2, 2), new("MN", 3, 2), new("WI", 4, 2), new("MI", 5, 2), new("NY", 8, 2), new("MA", 10, 2), new("RI", 11, 2),
        new("OR", 0, 3), new("ID", 1, 3), new("SD", 2, 3), new("IA", 3, 3), new("IL", 4, 3), new("IN", 5, 3), new("OH", 6, 3), new("PA", 7, 3), new("NJ", 8, 3), new("CT", 9, 3),
        new("CA", 0, 4), new("NV", 1, 4), new("WY", 2, 4), new("NE", 3, 4), new("MO", 4, 4), new("KY", 5, 4), new("WV", 6, 4), new("VA", 7, 4), new("MD", 8, 4), new("DE", 9, 4),
        new("AZ", 1, 5), new("UT", 2, 5), new("CO", 3, 5), new("KS", 4, 5), new("AR", 5, 5), new("TN", 6, 5), new("NC", 7, 5), new("SC", 8, 5), new("DC", 9, 5),
        new("HI", 0, 6), new("NM", 2, 6), new("OK", 3, 6), new("LA", 4, 6), new("MS", 5, 6), new("AL", 6, 6), new("GA", 7, 6),
        new("TX", 3, 7), new("FL", 8, 7)
    };
}
