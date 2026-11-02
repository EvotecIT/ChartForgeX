using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Stories;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StorySourceTimelineTests {
    [Theory]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("\t ")]
    public void EmptySourceDocumentsKeepExactWhitespaceAndAMeaningfulTextAlternative(string whitespace) {
        var source = new VisualStorySourceSurface(StorySourceText.Create(whitespace));
        Assert.Equal(whitespace, source.Source.Text);
        Assert.Contains("Empty source document", source.AccessibleText);
        var editor = StorySourceTimeline.Create(StorySourceText.Create("A" + whitespace)).Delete(0, 1, TimeSpan.FromSeconds(1));
        var prepared = Story(editor).Prepare();
        Assert.Equal(whitespace, editor.Source.Text);
        Assert.Contains("Empty source document", prepared.ToTranscript());
        Assert.True(prepared.ToPng().Length > 64);
        XDocument.Parse(prepared.ToSvg());
    }

    [Fact]
    public void ExactEditCompletionIncludesAnImmediateFollowingPaste() {
        var editor = StorySourceTimeline.Create(StorySourceText.Create(""))
            .Pause(TimeSpan.FromTicks(1_000_000))
            .Type("AB", TimeSpan.FromTicks(2_000_000))
            .Paste(StorySourceText.Create("C"));
        var prepared = Story(editor).Prepare(new VisualStoryPlaybackOptions(TimeSpan.Zero, TimeSpan.Zero, 1));
        Assert.Equal("ABC", SourceText(prepared.ToSvg(editor.Duration)));
        Assert.Equal("A", SourceText(prepared.ToSvg(editor.Duration - TimeSpan.FromTicks(1))));
    }

    [Theory]
    [InlineData(false, false, 7_000_000, 63)]
    [InlineData(true, false, 7_000_000, 27)]
    [InlineData(true, true, 17_000_000, 63)]
    public void PartialEditsRevealTheExactNumberOfWholeElements(bool delete, bool replace, long ticks, int count) {
        var editor = StorySourceTimeline.Create(StorySourceText.Create(delete ? new string('A', 90) : ""));
        if (replace) editor.Edit(0, 90, StorySourceText.Create(new string('B', 90)), TimeSpan.FromSeconds(2));
        else if (delete) editor.Delete(0, 90, TimeSpan.FromSeconds(1));
        else editor.Type(new string('B', 90), TimeSpan.FromSeconds(1));
        Assert.Equal(new string(delete && !replace ? 'A' : 'B', count), SourceText(Story(editor).Prepare().ToSvg(TimeSpan.FromTicks(ticks))));
        if (delete && !replace) {
            var completed = Story(editor).Prepare();
            Assert.Equal("", SourceText(completed.ToSvg()));
            Assert.Contains("Empty source document", completed.ToTranscript());
        }
    }

    [Fact]
    public void EditorChromeCannotSilentlyConsumeTheWholeSourceViewport() {
        var story = VisualStory.Create("Narrow editor").WithSize(600, 400);
        story.Scene("edit", "Edit", 2, VisualStorySceneLayout.Split)
            .Panel("source", new VisualStorySourceSurface(StorySourceText.Create("Ready"), options: new VisualStorySourceOptions("demo.cs")), weight: .15)
            .Panel("result", new VisualStoryTextSurface("Ready"), weight: .85);
        story.Outcome("source", "Source", "source");
        Assert.Throws<InvalidOperationException>(() => story.Prepare().ToPng());
    }
    [Fact]
    public void CompletedEditorKeepsItsCaretViewportAndFullTranscript() {
        var text = string.Join("\n", Enumerable.Range(1, 200).Select(index => "line" + index.ToString("000")));
        var editor = StorySourceTimeline.Create(StorySourceText.Create("" )).Type(text, TimeSpan.FromSeconds(1));
        var prepared = Story(editor).Prepare();
        var visible = SourceText(prepared.ToSvg());
        Assert.Contains("line200", visible);
        Assert.DoesNotContain("line001", visible);
        Assert.Contains("line001", prepared.ToTranscript());
        Assert.Contains("line200", prepared.ToTranscript());
    }
    [Fact]
    public void TypingACombiningMarkPreservesTheNeighbouringSyntaxAndCaretBoundary() {
        var editor = StorySourceTimeline.Create(StorySourceText.Create("e").AddSpan(0, 1, StorySyntaxKind.Variable))
            .Type("\u0301", TimeSpan.FromSeconds(1)).Type("!", TimeSpan.Zero);
        Assert.Equal("e\u0301!", editor.Source.Text);
        Assert.Equal(2, editor.Source.Spans.Single().Length);
        Assert.Equal("e\u0301!", SourceText(Story(editor).Prepare().ToSvg()));
    }
    [Fact]
    public void EditsPreserveWholeUnicodeElementsAndCompletedExactSource() {
        var editor = StorySourceTimeline.Create(StorySourceText.Create(""))
            .Type("e\u0301👩‍💻 ready", TimeSpan.FromSeconds(2));
        var story = Story(editor);
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.Zero, TimeSpan.Zero, 1));
        var partial = SourceText(prepared.ToSvg(TimeSpan.FromSeconds(0.6)));
        Assert.Equal("e\u0301👩‍💻", partial);
        Assert.Equal("e\u0301👩‍💻 ready", SourceText(prepared.ToSvg()));
        Assert.Throws<ArgumentException>(() => editor.Delete(1, 1, TimeSpan.Zero));
        Assert.Throws<ArgumentException>(() => editor.Delete(2, 1, TimeSpan.Zero));
        editor.Type(" changed", TimeSpan.Zero);
        Assert.DoesNotContain("changed", prepared.ToTranscript());
        Assert.Equal("e\u0301👩‍💻 ready", SourceText(prepared.ToSvg()));
    }

    [Fact]
    public void SelectionReplacementAndPasteUseResolvedSyntax() {
        var editor = StorySourceTimeline.Create(StorySourceText.Create("Invoke-Bad", "powershell"))
            .Select(7, 3, TimeSpan.FromSeconds(1))
            .Edit(7, 3, StorySourceText.Create("Good").AddSpan(0, 4, StorySyntaxKind.Command), TimeSpan.FromSeconds(1))
            .Pause(TimeSpan.FromSeconds(1));
        var prepared = Story(editor).Prepare();
        var selection = XDocument.Parse(prepared.ToSvg(TimeSpan.FromSeconds(0.5)));
        Assert.Contains(selection.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "source-selection");
        Assert.Equal("Invoke-Good", SourceText(prepared.ToSvg()));
        Assert.Equal(StorySyntaxKind.Command, editor.Source.Spans.Single().Kind);
    }

    [Fact]
    public void PortraitReflowKeepsReadableEditorTextAndChaptersInThePlayer() {
        var editor = StorySourceTimeline.Create(StorySourceText.Create(""))
            .Type("ready", TimeSpan.FromSeconds(1));
        var story = Story(editor).WithFormat(VisualStoryFormat.Portrait, 480);
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.Zero, TimeSpan.Zero, 1));
        var svg = prepared.ToAnimatedSvg(new VisualStoryFrameOptions(4));
        var root = XDocument.Parse(svg).Root!;
        Assert.Equal("2", (string?)root.Attribute("data-cfx-motion-duration"));
        Assert.Equal("1", (string?)root.Attribute("data-cfx-motion-plays"));
        Assert.Contains("s steps(1,end) 1 both", svg);
        var page = new HtmlMotionPlayerRenderer().RenderPage(svg, "Editor");
        Assert.Contains("aria-label=\"Story position\"", page);
        Assert.Contains("data-cfx-chapter-start=\"0\"", page);
        Assert.Contains("Read the transcript", page);
        Assert.Contains("overflow:visible", page);
        Assert.Contains("background:transparent", page);
        Assert.Contains("ready", page);
        Assert.DoesNotContain("@keyframes", page);
        Assert.Throws<ArgumentException>(() => new HtmlMotionPlayerRenderer().RenderPage(svg.Replace("<g ", "<g onclick=\"alert(1)\" ", StringComparison.Ordinal)));
        var rootEnd = svg.IndexOf('>', svg.IndexOf("<svg", StringComparison.Ordinal));
        foreach (var active in new[] { "<?probe ><script>window.cfxProbe=1</script>?>", "<![CDATA[</svg><script>window.cfxProbe=1</script>]]>", "<!-- harmless XML-only node -->" })
            Assert.Throws<ArgumentException>(() => new HtmlMotionPlayerRenderer().RenderPage(svg.Insert(rootEnd + 1, active)));
    }

    private static VisualStory Story(StorySourceTimeline editor) {
        var story = VisualStory.Create("Source editing").WithSize(600, 400);
        story.Scene("edit", "Edit", 2).Panel("result", new VisualStorySourceSurface(editor, options: new VisualStorySourceOptions("demo.ps1")));
        story.Outcome("source", "Completed source", "result"); return story;
    }
    private static string SourceText(string svg) => string.Concat(XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "source-text").Select(element => element.Value));
}
