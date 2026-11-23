using PowerForge.Fluent;
namespace ChartForgeX.Interactivity.Html;
/// <summary>Host-only participant navigation behavior; static sequence output never creates menus.</summary>
[Fluent]
public sealed class HtmlSequenceNavigationOptions {
    /// <summary>Gets or sets whether native details menus start expanded. Users retain ordinary disclosure keyboard behavior.</summary>
    public bool InitiallyExpanded {get;set;}
    /// <summary>Gets or sets the content security policy nonce. The page then has no <c>style</c> attributes: inline styles
    /// and style elements move into one <c>style</c> element with this nonce.</summary>
    public string? Nonce {get;set;}
}
