using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApiLedger;

/// <summary>Loads external audit locations without copying their identities into public evidence.</summary>
internal sealed record PrivateConsumerMapping(string NeutralName, string Path) {
    internal static PrivateConsumerMapping[] Load(string repository, string mappingPath) {
        string relative = System.IO.Path.GetRelativePath(System.IO.Path.GetFullPath(repository), System.IO.Path.GetFullPath(mappingPath));
        if (!System.IO.Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Private consumer mappings must remain outside the repository.");
        var mappings = JsonSerializer.Deserialize<PrivateConsumerMapping[]>(File.ReadAllText(mappingPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Private consumer mappings must be a JSON array.");
        foreach (var mapping in mappings) {
            if (mapping is null || !Regex.IsMatch(mapping.NeutralName ?? "", @"^private-consumer-[a-z0-9]+(?:-[a-z0-9]+)*$"))
                throw new ArgumentException("Use a neutral private-consumer-<role> name in external mappings.");
            if (string.IsNullOrWhiteSpace(mapping.Path)) throw new ArgumentException("Private consumer mappings require a source path.");
        }
        return mappings;
    }
}
