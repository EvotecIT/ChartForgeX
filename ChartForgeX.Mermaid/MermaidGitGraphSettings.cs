using System;
using System.Xml;

namespace ChartForgeX.Mermaid;

/// <summary>Resolves supported Git graph source scalars at the shared configuration boundary.</summary>
internal sealed class MermaidGitGraphSettings {
    internal string MainBranchName { get; private set; } = "main";
    internal int MainBranchOrder { get; private set; }
    internal bool? ShowBranches { get; private set; }
    internal bool? ShowCommitLabels { get; private set; }
    internal bool? RotateCommitLabels { get; private set; }
    internal bool? ParallelCommits { get; private set; }
    internal static bool IsScalarPath(string path) => path == "gitGraph.mainBranchName" || path == "gitGraph.mainBranchOrder"
        || path == "gitGraph.showBranches" || path == "gitGraph.showCommitLabel" || path == "gitGraph.rotateCommitLabel" || path == "gitGraph.parallelCommits";
    internal bool Handles(string path) {
        if (!path.StartsWith("gitGraph.", StringComparison.Ordinal)) return false;
        var dot = path.IndexOf('.', "gitGraph.".Length);
        return IsScalarPath(path) || dot >= 0 && IsScalarPath(path.Substring(0, dot));
    }
    internal static MermaidGitGraphSettings Read(MermaidSourceConfiguration configuration, Action<MermaidConfigurationSetting, string> error) {
        var settings = new MermaidGitGraphSettings();
        var name = configuration.Last("gitGraph.mainBranchName");
        if (name != null) {
            var valid = name.IsString && !string.IsNullOrWhiteSpace(name.Value) && name.Value!.Length <= 4096;
            if (valid) try { XmlConvert.VerifyXmlChars(name.Value!); } catch (XmlException) { valid = false; }
            if (valid) settings.MainBranchName = name.Value!;
            else Invalid(name, "must be a non-empty XML-safe string of at most 4096 characters");
        }
        var order = configuration.Last("gitGraph.mainBranchOrder");
        if (order != null) {
            if (order.TryGetNumber(out var number) && number >= 0 && number <= int.MaxValue && number == Math.Floor(number)) settings.MainBranchOrder = (int)number;
            else Invalid(order, "must be a non-negative 32-bit integer");
        }
        settings.ShowBranches = Boolean("showBranches"); settings.ShowCommitLabels = Boolean("showCommitLabel");
        settings.RotateCommitLabels = Boolean("rotateCommitLabel"); settings.ParallelCommits = Boolean("parallelCommits");
        return settings;
        bool? Boolean(string key) {
            var setting = configuration.Last("gitGraph." + key); if (setting == null) return null;
            if (!setting.IsString && bool.TryParse(setting.Value, out var value)) return value;
            Invalid(setting, "must be a boolean"); return null;
        }
        void Invalid(MermaidConfigurationSetting setting, string reason) => error(setting, "Mermaid configuration '" + setting.Path + "' " + reason + ".");
    }
}
