﻿namespace SpecFlow.VisualStudio.Tests.Editor.Services;

public class FeatureFileTaggerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public FeatureFileTaggerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Parse_triggered_in_background_after_instantiation()
    {
        //arrange
        using var sut = TaggerSut.Arrange(_testOutputHelper);
        var parserInvoked = false;
        sut.TagParser.Setup(s => s.Parse(It.IsAny<ITextSnapshot>()))
            .Returns(() =>
            {
                parserInvoked = true;
                return ImmutableArray<DeveroomTag>.Empty;
            });
        var tagger = sut.BuildFeatureFileTagger();

        //act
        tagger.GetUpToDateDeveroomTagsForSpan(sut.CurrentSnapshotSpan);

        //assert
        parserInvoked.Should().BeTrue();
    }

    [Fact]
    public void GetTags_returns_with_the_parsed_values()
    {
        //arrange
        using var sut = TaggerSut.Arrange(_testOutputHelper);
        var view = sut.IdeScope.CreateTextView(new TestText(Array.Empty<string>()), sut.ProjectScope.ProjectFullPath);

        IReadOnlyCollection<DeveroomTag> parseResult = ImmutableArray<DeveroomTag>.Empty;

        sut.TagParser
            .Setup(s => s.Parse(It.IsAny<ITextSnapshot>()))
            .Returns<ITextSnapshot>(snapShot =>
            {
                return parseResult = Enumerable
                    .Repeat(() => new DeveroomTag(string.Empty, new SnapshotSpan(snapShot, 0, 0)), 10)
                    .Select(d => d())
                    .ToImmutableArray();
            });

        var tagger = sut.BuildInitializedFeatureFileTagger();

        //act
        var tags = tagger.GetTags(sut.CurrentSnapshotSpanCollection).Select(t => t.Tag);

        //assert
        tags.Should().BeEquivalentTo(parseResult);
    }

    [Fact]
    public void TagsChanged_event_is_fired_when_TextBuffer_is_modified()
    {
        //arrange
        using var sut = TaggerSut.Arrange(_testOutputHelper);
        sut.BuildInitializedFeatureFileTagger();

        //act
        sut.StubTextBuffer.InvokeChangedOnBackground();

        //assert
        sut.WaitForTagsChangedEvent().Should().HaveCount(2);
    }

    [Fact]
    public void Returns_the_updated_tags_after_change()
    {
        //arrange
        using var sut = TaggerSut.Arrange(_testOutputHelper).WithRealDeveroomTagParser();
        var tagger = sut.BuildInitializedFeatureFileTagger();

        //act
        sut.StubTextBuffer.InvokeChangedOnBackground();
        var tags = tagger.GetTags(sut.CurrentSnapshotSpanCollection);

        //assert
        tags.Single().Tag.Type.Should().Be("Document", "the feature file has no content");
    }

    [Fact]
    public void Parsed_once_only_when_TextBuffer_ChangedOnBackground_fired_multiple_times_for_the_same_TextBuffer()
    {
        //arrange
        using var sut = TaggerSut.Arrange(_testOutputHelper);

        sut.BuildInitializedFeatureFileTagger();
        sut.TagParser.Verify(s => s.Parse(It.IsAny<ITextSnapshot>()), Times.Once);

        //act
        sut.StubTextBuffer.InvokeChangedOnBackground();

        //assert
        sut.TagParser.Verify(s => s.Parse(It.IsAny<ITextSnapshot>()), Times.Exactly(2));
    }

    [Fact]
    public void GetTags_does_not_trigger_parse()
    {
        //arrange
        using var sut = TaggerSut.Arrange(_testOutputHelper);

        IReadOnlyCollection<DeveroomTag> parseResult = ImmutableArray.Create<DeveroomTag>(VoidDeveroomTag.Instance);

        sut.TagParser
            .Setup(s => s.Parse(It.IsAny<ITextSnapshot>()))
            .Returns<ITextSnapshot>(snapShot =>
            {
                parseResult.Should().HaveCount(1, "the initial parse is called only");
                return parseResult = Enumerable
                    .Repeat(() => new DeveroomTag(string.Empty, new SnapshotSpan(snapShot, 0, 0)), 10)
                    .Select(d => d())
                    .ToImmutableArray();
            });

        var tagger = sut.BuildInitializedFeatureFileTagger();

        sut.StubTextBuffer.ModifyContent("123456789");

        //act
        var tags1 = tagger.GetTags(sut.CurrentSnapshotSpanCollection).Select(t => t.Tag).ToArray();
        var tags2 = tagger.GetTags(sut.CurrentSnapshotSpanCollection).Select(t => t.Tag).ToArray();

        //assert
        tags1.Should().BeEquivalentTo(parseResult);
        tags2.Should().BeEquivalentTo(parseResult);
        sut.TagsChangedEvents.Should().HaveCount(1, "the initial parse is called only");
    }

    [Fact]
    public void Works_when_TextBuffer_is_shortened()
    {
        //arrange
        using var sut = TaggerSut.Arrange(_testOutputHelper);
        sut.IdeScope.TextViewFactory = (inputText, filePath) => StubWpfTextView.CreateTextView(inputText, text =>
        {
            var textBuffer = VsxStubObjects.CreateTextBuffer("0123456789ABCDEF", VsContentTypes.FeatureFile);
            textBuffer.Properties.AddProperty(typeof(IProjectScope), sut.ProjectScope);
            textBuffer.Properties.AddProperty(typeof(IVsTextBuffer), new FilePathProvider(filePath));
            return textBuffer;
        });

        IReadOnlyCollection<DeveroomTag> parseResult = ImmutableArray<DeveroomTag>.Empty;

        DeveroomTag TagFactory(int i, ITextSnapshot snapshot)
        {
            return new DeveroomTag(VsContentTypes.FeatureFile, new SnapshotSpan(snapshot, i, 1));
        }

        sut.TagParser
            .Setup(s => s.Parse(It.IsAny<ITextSnapshot>()))
            .Returns<ITextSnapshot>(snapshot =>
            {
                parseResult.Should().BeEmpty("the initial parse is called only");
                return parseResult = Enumerable.Range(0, 10)
                    .Select(i => TagFactory(i, snapshot))
                    .ToImmutableArray();
            });

        var tagger = sut.BuildInitializedFeatureFileTagger();

        //act
        var span = new SnapshotSpan(sut.IdeScope.CurrentTextView.Caret.Position.BufferPosition, 5);
        var spanCollection = new NormalizedSnapshotSpanCollection(span);
        var tags1 = tagger.GetTags(spanCollection).Select(t => t.Tag);

        //assert
        tags1.Should().BeEquivalentTo(parseResult.Take(6));
        sut.TagsChangedEvents.Should().HaveCount(1, "the initial parse is called only");
    }

    [Fact]
    public void Reparse_when_binding_registry_is_changed()
    {
        //arrange
        using var sut = TaggerSut.Arrange(_testOutputHelper);
        var discoveryService = new Mock<IDiscoveryService>(MockBehavior.Strict);
        sut.ProjectScope.Properties.AddProperty(typeof(IDiscoveryService), discoveryService.Object);
        sut.BuildInitializedFeatureFileTagger();
        sut.WaitForTagsChangedEvent();

        //act
        discoveryService.Raise(ds => ds.WeakBindingRegistryChanged -= null!, EventArgs.Empty);

        //assert
        sut.WaitForTagsChangedEvent().Should().HaveCount(2);
    }

    [Fact]
    public void Deregister_events_when_TextBuffer_is_not_a_feature_file()
    {
        //arrange
        using var sut = TaggerSut.Arrange(_testOutputHelper);
        var discoveryService = new Mock<IDiscoveryService>(MockBehavior.Strict);
        sut.ProjectScope.Properties.AddProperty(typeof(IDiscoveryService), discoveryService.Object);
        sut.BuildFeatureFileTagger();
        sut.StubTextBuffer.StubContentType = sut.StubTextBuffer.StubContentType with {TypeName = "inert"};

        //act
        discoveryService.Raise(ds => ds.WeakBindingRegistryChanged -= null!, EventArgs.Empty);

        //assert
        sut.StubTextBuffer.Properties.TryGetProperty<ITagger<DeveroomTag>>(typeof(ITagger<DeveroomTag>), out var _)
            .Should()
            .BeFalse();
        discoveryService.VerifyRemove(m => m.WeakBindingRegistryChanged -= It.IsAny<EventHandler<EventArgs>>());
        sut.StubTextBuffer.VerifyRemove(
            tb => tb.ChangedOnBackground -= It.IsAny<EventHandler<TextContentChangedEventArgs>>());
    }
}
