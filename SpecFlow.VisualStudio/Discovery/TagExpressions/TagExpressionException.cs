using System;

namespace SpecFlow.VisualStudio.Discovery.TagExpressions
{
    public class TagExpressionException : Exception
    {
        public TagExpressionException(string message) : base(message)
        {
        }

        public TagExpressionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
