using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ChartForgeX.Tests;

/// <summary>Reports optional browser facts as skipped when their required test environment is absent.</summary>
[TraitDiscoverer("ChartForgeX.Tests.BrowserTraitDiscoverer", "ChartForgeX.Tests")]
public sealed class BrowserFactAttribute : FactAttribute, ITraitAttribute {
    public BrowserFactAttribute() => Skip = BrowserTestPrerequisites.SkipReason();

    /// <summary>Whether this test also needs the generated gallery served on loopback.</summary>
    public bool RequiresGallery {
        get => _requiresGallery;
        set { _requiresGallery = value; Skip = BrowserTestPrerequisites.SkipReason(value); }
    }

    private bool _requiresGallery;
}

/// <summary>Reports optional browser theories as skipped without enumerating unexercised input rows.</summary>
[TraitDiscoverer("ChartForgeX.Tests.BrowserTraitDiscoverer", "ChartForgeX.Tests")]
public sealed class BrowserTheoryAttribute : TheoryAttribute, ITraitAttribute {
    public BrowserTheoryAttribute() => Skip = BrowserTestPrerequisites.SkipReason();

    /// <summary>Whether this test also needs the generated gallery served on loopback.</summary>
    public bool RequiresGallery {
        get => _requiresGallery;
        set { _requiresGallery = value; Skip = BrowserTestPrerequisites.SkipReason(value); }
    }

    private bool _requiresGallery;
}

/// <summary>Lets CI select browser contracts without coupling the filter to class or file names.</summary>
public sealed class BrowserTraitDiscoverer : ITraitDiscoverer {
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute) {
        yield return new KeyValuePair<string, string>("Category", "Browser");
    }
}

/// <summary>Chart-specific prerequisites; installation and browser session lifetime belong to HtmlTinkerX.</summary>
internal static class BrowserTestPrerequisites {
    internal const string RequiredVariable = "CFX_TEST_REQUIRE_BROWSER";
    internal const string GalleryVariable = "CFX_V2_GALLERY_URL";
    internal static bool Required => Environment.GetEnvironmentVariable(RequiredVariable) == "1";
    internal static bool Enabled => Environment.GetEnvironmentVariable(InteractiveChartBrowser.EnableVariable) == "1";

    internal static string? SkipReason(bool requiresGallery = false) {
        // Required CI must execute and fail visibly if setup is incomplete, never discover a skip.
        if (Required) return null;
        if (!Enabled) return "Browser proof is optional here; set " + InteractiveChartBrowser.EnableVariable + "=1 to execute it.";
        if (requiresGallery && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(GalleryVariable)))
            return "Gallery proof requires " + GalleryVariable + " pointing to a generated gallery served on loopback.";
        return null;
    }

    internal static bool IsEnabled() {
        if (Required && !Enabled)
            throw new InvalidOperationException("Required browser proof needs " + InteractiveChartBrowser.EnableVariable + "=1.");
        return Enabled;
    }

    internal static string RequireGalleryUrl() {
        var url = Environment.GetEnvironmentVariable(GalleryVariable);
        if (!IsEnabled() || string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Browser gallery proof needs " + InteractiveChartBrowser.EnableVariable + "=1 and " + GalleryVariable + ".");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Scheme != Uri.UriSchemeHttp)
            throw new InvalidOperationException(GalleryVariable + " must be a task-owned loopback HTTP URL.");
        return url;
    }
}
