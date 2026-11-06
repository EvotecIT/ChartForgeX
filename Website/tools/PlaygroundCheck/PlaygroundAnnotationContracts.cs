using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Playground;

/// <summary>Checks the annotation page's native host through the same calls the browser runtime makes.</summary>
internal static class PlaygroundAnnotationContracts {
    internal static void Check(List<string> failures) {
        var opened = Parse(PlaygroundAnnotations.Open(null, "light"));
        var basis = Require(failures, "open", opened);
        if (basis == null) return;
        if (Anchor(opened, "review") != "3") failures.Add("annotation host: the starting scene lost its review note");

        var added = Parse(PlaygroundAnnotations.Prepare(basis, "light", "add", "note-1", "Note", 5, 50, double.NaN, double.NaN, 0, "Added", "Primary"));
        var addedBasis = Require(failures, "add", added);
        if (Anchor(added, "note-1") != "5") failures.Add("annotation host: an added note is not at its source X");

        // A request carries the basis it was drawn on, so replaying the earlier scene still applies exactly.
        var removed = Parse(PlaygroundAnnotations.Prepare(basis, "dark", "remove", "review", null, 0, 0, 0, 0, 0, null, null));
        Require(failures, "remove", removed);
        if (Anchor(removed, "review") != null || Anchor(removed, "note-1") != null)
            failures.Add("annotation host: remove did not apply to the scene named by its basis");

        if (addedBasis != null && Parse(PlaygroundAnnotations.Prepare(addedBasis, "light", "add", "note-1", "Note", 1, 1, 0, 0, 0, "", "Primary"))
                .GetProperty("ok").GetBoolean())
            failures.Add("annotation host: a duplicate add was accepted");
        if (Parse(PlaygroundAnnotations.Prepare("scene-missing", "light", "remove", "plan", null, 0, 0, 0, 0, 0, null, null)).GetProperty("ok").GetBoolean())
            failures.Add("annotation host: an unknown basis was accepted");
        if (!Parse(PlaygroundAnnotations.RuntimeJson()).GetProperty("script").GetString()!.Contains("ChartForgeXCharts", System.StringComparison.Ordinal))
            failures.Add("annotation host: the runtime export does not carry the chart runtime");
        System.Console.WriteLine("Playground annotation host contracts: open, add, remove by basis and rejected edits.");
    }

    private static string? Require(List<string> failures, string step, JsonElement reply) {
        if (reply.GetProperty("ok").GetBoolean()) return reply.GetProperty("basis").GetString();
        failures.Add("annotation host " + step + ": " + reply.GetProperty("error").GetString());
        return null;
    }

    // Adaptive sections hold a wide and a narrow drawing; both carry the same source anchor.
    private static string? Anchor(JsonElement reply, string id) {
        if (!reply.TryGetProperty("html", out var html)) return null;
        var match = Regex.Match(html.GetString()!, "<[^>]*data-cfx-annotation-id=\"" + Regex.Escape(id) + "\"[^>]*>");
        return match.Success ? Regex.Match(match.Value, "data-cfx-source-x=\"([^\"]*)\"").Groups[1].Value : null;
    }

    private static JsonElement Parse(string json) {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
