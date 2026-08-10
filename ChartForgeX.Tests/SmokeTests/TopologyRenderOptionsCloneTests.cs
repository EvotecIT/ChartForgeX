using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TopologyRenderOptionsCloneKeepsIconCatalogIndependent() {
        var catalog = new TopologyIconCatalog().AddPack(
            new TopologyIconPack("vendor", "Vendor", "Example")
                .WithMetadata("source", "original")
                .AddIcon(new TopologyIconDefinition("vendor", "service", "Service", TopologyNodeKind.Service)
                    .WithArtwork(TopologyIconArtwork.InlineSvg("<path d='M0 0h24v24H0z'/>", "0 0 24 24"))
                    .WithMetadata("tier", "one")
                    .WithTags("service"))
                .AddIcon(new TopologyIconDefinition("vendor", "worker", "Worker", TopologyNodeKind.Service)));
        catalog.Packs[0].Tags.Add("  raw pack tag  ");
        catalog.Packs[0].Tags.Add("RAW PACK TAG");
        catalog.Packs[0].Icons[0].Tags.Add("  raw icon tag  ");
        catalog.Packs[0].Icons[1].Id = "service";
        var original = new TopologyRenderOptions { IconCatalog = catalog };

        var clone = original.Clone();
        Assert(clone.IconCatalog != null && !ReferenceEquals(original.IconCatalog, clone.IconCatalog), "Cloned render options should own an independent icon catalog.");
        Assert(clone.IconCatalog!.Packs[0].Icons.Count == 2, "Cloning should preserve mutable duplicate icon ids without revalidating or normalizing catalog state.");
        Assert(string.Join("|", clone.IconCatalog.Packs[0].Tags) == string.Join("|", original.IconCatalog!.Packs[0].Tags), "Cloning should preserve the exact pack tag sequence.");
        Assert(string.Join("|", clone.IconCatalog.Packs[0].Icons[0].Tags) == string.Join("|", original.IconCatalog.Packs[0].Icons[0].Tags), "Cloning should preserve the exact icon tag sequence.");
        clone.IconCatalog.Packs[0].Tags[0] = "clone-only";
        clone.IconCatalog!.Packs[0].Metadata["source"] = "clone";
        clone.IconCatalog.Packs[0].Icons[0].Metadata["tier"] = "two";
        clone.IconCatalog.Packs[0].Icons[0].Artwork!.SvgBody = "<circle cx='12' cy='12' r='10'/>";
        clone.IconCatalog.RemovePack("vendor");

        Assert(original.IconCatalog!.ContainsPack("vendor"), "Mutating a cloned catalog should not remove packs from the original.");
        Assert(original.IconCatalog.Packs[0].Tags[0] != "clone-only", "Cloned tag collections should be independent.");
        Assert(original.IconCatalog.Packs[0].Metadata["source"] == "original", "Cloned pack metadata should be independent.");
        Assert(original.IconCatalog.Packs[0].Icons[0].Metadata["tier"] == "one", "Cloned icon metadata should be independent.");
        Assert(original.IconCatalog.Packs[0].Icons[0].Artwork!.SvgBody!.Contains("path", System.StringComparison.Ordinal), "Cloned icon artwork should be independent.");
    }
}
