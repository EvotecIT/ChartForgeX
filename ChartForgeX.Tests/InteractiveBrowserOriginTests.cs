using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveBrowserOriginTests {
    [BrowserFact(RequiresGallery = true)]
    public async Task EmptyOriginUsesNoSocketAndReleasesItsRouteBeforeGalleryNavigation() {
        if (!Enabled) return;
        var gallery = BrowserTestPrerequisites.RequireGalleryUrl();
        var origin = new Uri(new Uri(gallery), "__cfx_browser_origin.html").AbsoluteUri;
        await using var session = await OpenAsync("<!doctype html><html><body>Secure origin</body></html>");

        Assert.Equal("", await session.Page.EvaluateAsync<string>("()=>performance.getEntriesByType('navigation')[0].nextHopProtocol"));
        Assert.Equal(origin, session.Page.Url);
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>isSecureContext&&typeof crypto.subtle.digest==='function'"));

        var galleryResponse = await session.Page.GotoAsync(gallery);
        Assert.NotNull(galleryResponse);
        Assert.Equal(200, galleryResponse.Status);
        Assert.NotNull(await galleryResponse.ServerAddrAsync());

        var subsequentOrigin = await session.Page.GotoAsync(origin);
        Assert.NotNull(subsequentOrigin);
        Assert.Equal(200, subsequentOrigin.Status);
        Assert.NotNull(await subsequentOrigin.ServerAddrAsync());
        AssertNoConsoleErrors(session);
    }
}
