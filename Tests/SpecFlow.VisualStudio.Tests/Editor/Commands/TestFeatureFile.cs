using System;

namespace SpecFlow.VisualStudio.Tests.Editor.Commands
{
    public class TestFeatureFile
    {
        public static TestFeatureFile Void = new TestFeatureFile(string.Empty, string.Empty);
        public bool IsVoid => string.Empty == FileName && string.Empty == Content;

        public string FileName { get; }
        public string Content { get; }

        public TestFeatureFile(string fileName, string content)
        {
            FileName = fileName;
            Content = content;
        }
    }
}