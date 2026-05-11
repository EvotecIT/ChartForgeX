using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// Defines reusable topology icon packs for product-neutral and vendor-specific diagrams.
/// </summary>
public sealed class TopologyIconCatalog {
    private readonly List<TopologyIconPack> _packs = new();

    /// <summary>
    /// Creates a catalog with the built-in topology icon packs.
    /// </summary>
    /// <returns>A catalog containing the built-in icon packs.</returns>
    public static TopologyIconCatalog Default() {
        return new TopologyIconCatalog()
            .AddPack(BuiltInCommon())
            .AddPack(BuiltInNetwork())
            .AddPack(BuiltInActiveDirectory())
            .AddPack(BuiltInCloud())
            .AddPack(BuiltInPeople());
    }

    /// <summary>Gets the registered icon packs.</summary>
    public IReadOnlyList<TopologyIconPack> Packs => _packs;

    /// <summary>
    /// Adds an icon pack to the catalog.
    /// </summary>
    /// <param name="pack">The pack to add.</param>
    /// <returns>The current catalog.</returns>
    public TopologyIconCatalog AddPack(TopologyIconPack pack) {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (_packs.Any(existing => string.Equals(existing.Id, pack.Id, StringComparison.OrdinalIgnoreCase))) {
            throw new ArgumentException("Topology icon pack '" + pack.Id + "' is already registered.", nameof(pack));
        }

        _packs.Add(pack);
        return this;
    }

    /// <summary>
    /// Adds an icon pack or replaces an existing pack with the same id.
    /// </summary>
    /// <param name="pack">The pack to add or replace.</param>
    /// <returns>The current catalog.</returns>
    public TopologyIconCatalog AddOrReplacePack(TopologyIconPack pack) {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        RemovePack(pack.Id);
        _packs.Add(pack);
        return this;
    }

    /// <summary>
    /// Removes a registered icon pack by id.
    /// </summary>
    /// <param name="packId">The pack id.</param>
    /// <returns>The current catalog.</returns>
    public TopologyIconCatalog RemovePack(string packId) {
        if (string.IsNullOrWhiteSpace(packId)) throw new ArgumentException("Value cannot be empty.", nameof(packId));
        _packs.RemoveAll(pack => string.Equals(pack.Id, packId.Trim(), StringComparison.OrdinalIgnoreCase));
        return this;
    }

    /// <summary>
    /// Returns whether a pack id is already registered.
    /// </summary>
    /// <param name="packId">The pack id.</param>
    /// <returns>True when the pack id exists.</returns>
    public bool ContainsPack(string packId) {
        if (string.IsNullOrWhiteSpace(packId)) return false;
        return _packs.Any(pack => string.Equals(pack.Id, packId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolves an icon by fully qualified id such as <c>network:switch</c>, or by unique local id.
    /// </summary>
    /// <param name="iconId">The icon id.</param>
    /// <returns>The resolved icon definition, or null when no icon matches.</returns>
    public TopologyIconDefinition? Resolve(string? iconId) {
        if (string.IsNullOrWhiteSpace(iconId)) return null;
        var normalized = iconId!.Trim();
        foreach (var pack in _packs) {
            var icon = pack.Resolve(normalized);
            if (icon != null) return icon;
        }

        var matches = _packs.Select(pack => pack.ResolveLocal(normalized)).Where(icon => icon != null).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    private static TopologyIconPack BuiltInCommon() {
        return new TopologyIconPack("common", "Common Infrastructure", isBuiltIn: true)
            .WithTags("common", "infrastructure")
            .AddIcon("server", "Server", TopologyNodeKind.Server, TopologyIconShape.Server, "SRV", "#2563EB", "Compute")
            .AddIcon("database", "Database", TopologyNodeKind.Database, TopologyIconShape.Database, "DB", "#0F766E", "Data")
            .AddIcon("storage", "Storage", TopologyNodeKind.Storage, TopologyIconShape.Storage, "ST", "#64748B", "Data")
            .AddIcon("application", "Application", TopologyNodeKind.Application, TopologyIconShape.Application, "APP", "#7C3AED", "Application")
            .AddIcon("service", "Service", TopologyNodeKind.Service, TopologyIconShape.Service, "SVC", "#2563EB", "Application")
            .AddIcon("endpoint", "Endpoint", TopologyNodeKind.Endpoint, TopologyIconShape.Desktop, "EP", "#475569", "Endpoint")
            .AddIcon("laptop", "Laptop", TopologyNodeKind.Endpoint, TopologyIconShape.Laptop, "PC", "#475569", "Endpoint")
            .AddIcon("desktop", "Desktop", TopologyNodeKind.Endpoint, TopologyIconShape.Desktop, "PC", "#475569", "Endpoint")
            .AddIcon("certificate", "Certificate", TopologyNodeKind.Certificate, TopologyIconShape.Certificate, "TLS", "#D97706", "Security");
    }

    private static TopologyIconPack BuiltInNetwork() {
        return new TopologyIconPack("network", "Network", isBuiltIn: true)
            .WithTags("network", "connectivity")
            .AddIcon("switch", "Switch", TopologyNodeKind.Network, TopologyIconShape.NetworkSwitch, "SW", "#0891B2", "Network")
            .AddIcon("router", "Router", TopologyNodeKind.Gateway, TopologyIconShape.Router, "RTR", "#2563EB", "Network")
            .AddIcon("firewall", "Firewall", TopologyNodeKind.Gateway, TopologyIconShape.Firewall, "FW", "#EA580C", "Security")
            .AddIcon("load-balancer", "Load Balancer", TopologyNodeKind.Gateway, TopologyIconShape.LoadBalancer, "LB", "#7C3AED", "Network")
            .AddIcon("subnet", "Subnet", TopologyNodeKind.NetworkSegment, TopologyIconShape.NetworkSegment, "NET", "#0891B2", "Network")
            .AddIcon("wan-link", "WAN Link", TopologyNodeKind.Network, TopologyIconShape.Network, "WAN", "#2563EB", "Network")
            .WithIconTags("load-balancer", "loadbalancer", "lb")
            .WithIconTags("subnet", "cidr", "network-segment");
    }

    private static TopologyIconPack BuiltInActiveDirectory() {
        return new TopologyIconPack("microsoft-ad", "Microsoft Active Directory", vendor: "Microsoft", isBuiltIn: true)
            .WithTags("active-directory", "directory", "microsoft", "ad")
            .AddIcon("forest", "Forest", TopologyNodeKind.Namespace, TopologyIconShape.Forest, "FOR", "#16A34A", "Directory")
            .AddIcon("domain", "Domain", TopologyNodeKind.Namespace, TopologyIconShape.Domain, "DOM", "#2563EB", "Directory")
            .AddIcon("domain-controller", "Domain Controller", TopologyNodeKind.Server, TopologyIconShape.DomainController, "DC", "#2563EB", "Directory")
            .AddIcon("read-only-domain-controller", "Read Only Domain Controller", TopologyNodeKind.Server, TopologyIconShape.ReadOnlyDomainController, "RODC", "#7C3AED", "Directory")
            .AddIcon("global-catalog", "Global Catalog", TopologyNodeKind.Server, TopologyIconShape.DomainController, "GC", "#0F766E", "Directory")
            .AddIcon("site", "AD Site", TopologyNodeKind.Location, TopologyIconShape.Site, "SITE", "#16A34A", "Directory")
            .AddIcon("site-link", "AD Site Link", TopologyNodeKind.Network, TopologyIconShape.Network, "LINK", "#2563EB", "Directory")
            .AddIcon("ad-subnet", "AD Subnet", TopologyNodeKind.NetworkSegment, TopologyIconShape.NetworkSegment, "NET", "#0891B2", "Directory")
            .AddIcon("bridgehead", "Bridgehead Server", TopologyNodeKind.Server, TopologyIconShape.DomainController, "BH", "#0F766E", "Directory")
            .WithIconTags("domain-controller", "dc", "ldap", "kerberos")
            .WithIconTags("read-only-domain-controller", "rodc", "read-only")
            .WithIconTags("global-catalog", "gc")
            .WithIconTags("site-link", "replication", "transport")
            .WithIconTags("ad-subnet", "subnet", "cidr", "network-segment")
            .WithIconTags("bridgehead", "replication", "inter-site");
    }

    private static TopologyIconPack BuiltInCloud() {
        return new TopologyIconPack("cloud", "Cloud", isBuiltIn: true)
            .WithTags("cloud")
            .AddIcon("cloud", "Cloud", TopologyNodeKind.Cloud, TopologyIconShape.Cloud, "CLD", "#2563EB", "Cloud")
            .AddIcon("microsoft-365", "Microsoft 365", TopologyNodeKind.Cloud, TopologyIconShape.Cloud, "M365", "#2563EB", "Cloud")
            .AddIcon("azure", "Azure", TopologyNodeKind.Cloud, TopologyIconShape.Cloud, "AZ", "#2563EB", "Cloud")
            .AddIcon("aws", "AWS", TopologyNodeKind.Cloud, TopologyIconShape.Cloud, "AWS", "#EA580C", "Cloud")
            .AddIcon("tenant", "Tenant", TopologyNodeKind.Namespace, TopologyIconShape.Domain, "TEN", "#7C3AED", "Cloud");
    }

    private static TopologyIconPack BuiltInPeople() {
        return new TopologyIconPack("people", "People and Teams", isBuiltIn: true)
            .WithTags("people", "team")
            .AddIcon("person", "Person", TopologyNodeKind.Person, TopologyIconShape.Person, "USR", "#475569", "People")
            .AddIcon("team", "Team", TopologyNodeKind.Team, TopologyIconShape.Team, "TEAM", "#2563EB", "People")
            .AddIcon("operator", "Operator", TopologyNodeKind.Person, TopologyIconShape.Person, "OPS", "#0F766E", "People")
            .AddIcon("owner", "Owner", TopologyNodeKind.Person, TopologyIconShape.Person, "OWN", "#7C3AED", "People");
    }
}

/// <summary>
/// Represents a named set of reusable topology icons.
/// </summary>
public sealed class TopologyIconPack {
    private readonly List<TopologyIconDefinition> _icons = new();
    private string _id;
    private string _label;

    /// <summary>
    /// Initializes a topology icon pack.
    /// </summary>
    public TopologyIconPack(string id, string label, string? vendor = null, string? version = null, bool isBuiltIn = false) {
        _id = RequiredText(id, nameof(id));
        _label = RequiredText(label, nameof(label));
        Vendor = vendor;
        Version = version;
        IsBuiltIn = isBuiltIn;
    }

    /// <summary>Gets or sets the stable pack id.</summary>
    public string Id { get => _id; set => _id = RequiredText(value, nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = RequiredText(value, nameof(value)); }

    /// <summary>Gets or sets the optional pack vendor.</summary>
    public string? Vendor { get; set; }

    /// <summary>Gets or sets the optional pack version.</summary>
    public string? Version { get; set; }

    /// <summary>Gets whether the pack is supplied by ChartForgeX.</summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>Gets icons in the pack.</summary>
    public IReadOnlyList<TopologyIconDefinition> Icons => _icons;

    /// <summary>Gets arbitrary metadata for host-side pickers and vendor adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();

    /// <summary>Gets search tags for host-side pickers.</summary>
    public List<string> Tags { get; } = new();

    /// <summary>
    /// Adds an icon definition to the pack.
    /// </summary>
    public TopologyIconPack AddIcon(string id, string label, TopologyNodeKind nodeKind, TopologyIconShape shape = TopologyIconShape.Auto, string? symbol = null, string? color = null, string? category = null, TopologyNodeDisplayMode? displayMode = null) {
        var icon = new TopologyIconDefinition(Id, id, label, nodeKind, shape) {
            Symbol = symbol,
            Color = color,
            Category = category,
            DisplayMode = displayMode
        };
        return AddIcon(icon);
    }

    /// <summary>
    /// Adds a pre-built icon definition to the pack.
    /// </summary>
    public TopologyIconPack AddIcon(TopologyIconDefinition icon) {
        if (icon == null) throw new ArgumentNullException(nameof(icon));
        if (!string.Equals(icon.PackId, Id, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Topology icon pack id '" + icon.PackId + "' does not match pack '" + Id + "'.", nameof(icon));
        }

        if (_icons.Any(existing => string.Equals(existing.Id, icon.Id, StringComparison.OrdinalIgnoreCase))) {
            throw new ArgumentException("Topology icon '" + icon.Id + "' is already registered in pack '" + Id + "'.", nameof(icon));
        }

        _icons.Add(icon);
        return this;
    }

    /// <summary>
    /// Adds or replaces pack metadata and returns the current pack.
    /// </summary>
    public TopologyIconPack WithMetadata(string key, string value) {
        Metadata[RequiredText(key, nameof(key))] = value ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Adds pack-level search tags and returns the current pack.
    /// </summary>
    public TopologyIconPack WithTags(params string[] tags) {
        AddTags(Tags, tags);
        return this;
    }

    /// <summary>
    /// Adds search tags to an icon inside this pack.
    /// </summary>
    public TopologyIconPack WithIconTags(string iconId, params string[] tags) {
        var id = RequiredText(iconId, nameof(iconId));
        var icon = ResolveLocal(id);
        if (icon == null) throw new ArgumentException("Topology icon '" + id + "' was not found in pack '" + Id + "'.", nameof(iconId));
        icon.WithTags(tags);
        return this;
    }

    internal TopologyIconDefinition? Resolve(string iconId) {
        foreach (var icon in _icons) {
            if (string.Equals(icon.QualifiedId, iconId, StringComparison.OrdinalIgnoreCase)) return icon;
            if (string.Equals(icon.Id, iconId, StringComparison.OrdinalIgnoreCase) && iconId.IndexOf(':') >= 0) return icon;
        }

        return null;
    }

    internal TopologyIconDefinition? ResolveLocal(string iconId) {
        return _icons.FirstOrDefault(icon => string.Equals(icon.Id, iconId, StringComparison.OrdinalIgnoreCase));
    }

    private static string RequiredText(string? value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", parameterName);
        return value!.Trim();
    }

    private static void AddTags(List<string> target, string[] tags) {
        if (tags == null) return;
        foreach (var tag in tags) {
            if (string.IsNullOrWhiteSpace(tag)) continue;
            var normalized = tag.Trim();
            if (!target.Any(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase))) target.Add(normalized);
        }
    }
}

/// <summary>
/// Describes one topology icon that can be referenced by nodes and groups.
/// </summary>
public sealed class TopologyIconDefinition {
    private string _packId;
    private string _id;
    private string _label;
    private TopologyNodeKind _nodeKind;
    private TopologyIconShape _shape;
    private TopologyNodeDisplayMode? _displayMode;

    /// <summary>
    /// Initializes a topology icon definition.
    /// </summary>
    public TopologyIconDefinition(string packId, string id, string label, TopologyNodeKind nodeKind, TopologyIconShape shape = TopologyIconShape.Auto) {
        _packId = RequiredText(packId, nameof(packId));
        _id = RequiredText(id, nameof(id));
        _label = RequiredText(label, nameof(label));
        NodeKind = nodeKind;
        Shape = shape;
    }

    /// <summary>Gets or sets the source pack id.</summary>
    public string PackId { get => _packId; set => _packId = RequiredText(value, nameof(value)); }

    /// <summary>Gets or sets the local icon id inside the pack.</summary>
    public string Id { get => _id; set => _id = RequiredText(value, nameof(value)); }

    /// <summary>Gets the fully qualified icon id.</summary>
    public string QualifiedId => PackId + ":" + Id;

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = RequiredText(value, nameof(value)); }

    /// <summary>Gets or sets the default topology node kind for this icon.</summary>
    public TopologyNodeKind NodeKind {
        get => _nodeKind;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _nodeKind = value;
        }
    }

    /// <summary>Gets or sets the renderer-owned shape hint.</summary>
    public TopologyIconShape Shape {
        get => _shape;
        set {
            TopologyModelGuards.EnumDefined(value, nameof(value));
            _shape = value;
        }
    }

    /// <summary>Gets or sets the short fallback symbol.</summary>
    public string? Symbol { get; set; }

    /// <summary>Gets or sets the optional identity color used when the node does not set a color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the optional catalog category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets an optional default display mode for this icon.</summary>
    public TopologyNodeDisplayMode? DisplayMode {
        get => _displayMode;
        set {
            if (value.HasValue) TopologyModelGuards.EnumDefined(value.Value, nameof(value));
            _displayMode = value;
        }
    }

    /// <summary>Gets or sets optional SVG/image artwork used by SVG and HTML renderers.</summary>
    public TopologyIconArtwork? Artwork { get; set; }

    /// <summary>Gets arbitrary metadata for host-side pickers and vendor adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new();

    /// <summary>Gets search tags for host-side pickers.</summary>
    public List<string> Tags { get; } = new();

    /// <summary>
    /// Adds or replaces icon metadata and returns the current icon definition.
    /// </summary>
    public TopologyIconDefinition WithMetadata(string key, string value) {
        Metadata[RequiredText(key, nameof(key))] = value ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Sets icon artwork and returns the current icon definition.
    /// </summary>
    public TopologyIconDefinition WithArtwork(TopologyIconArtwork artwork) {
        Artwork = artwork ?? throw new ArgumentNullException(nameof(artwork));
        return this;
    }

    /// <summary>
    /// Adds icon-level search tags and returns the current icon definition.
    /// </summary>
    public TopologyIconDefinition WithTags(params string[] tags) {
        AddTags(Tags, tags);
        return this;
    }

    private static string RequiredText(string? value, string parameterName) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", parameterName);
        return value!.Trim();
    }

    private static void AddTags(List<string> target, string[] tags) {
        if (tags == null) return;
        foreach (var tag in tags) {
            if (string.IsNullOrWhiteSpace(tag)) continue;
            var normalized = tag.Trim();
            if (!target.Any(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase))) target.Add(normalized);
        }
    }
}
