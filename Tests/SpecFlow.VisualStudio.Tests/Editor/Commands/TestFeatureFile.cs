namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public class TestFeatureFile
    {
        public string FileName { get; }
        public string Content { get; }

        public TestFeatureFile(string fileName, string content)
        {
            FileName = fileName;
            Content = content;
        }
    }
}