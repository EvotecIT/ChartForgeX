using System.Text;

namespace ApiLedger;

/// <summary>Writes deterministic UTF-8 CSV with LF row separators and quoted cells.</summary>
internal static class LedgerCsv {
    internal static void Write(string path, Dictionary<string, object?>[] rows) {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.NewLine = "\n";
        if (rows.Length == 0) return;
        var columns = rows[0].Keys.ToArray();
        writer.WriteLine(string.Join(",", columns.Select(Escape)));
        foreach (var row in rows) writer.WriteLine(string.Join(",", columns.Select(column => Escape(Convert.ToString(row[column], System.Globalization.CultureInfo.InvariantCulture) ?? ""))));
    }

    private static string Escape(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
