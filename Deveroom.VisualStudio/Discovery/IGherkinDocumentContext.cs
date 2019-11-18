using System.Collections.Generic;
using System.Linq;
using Deveroom.VisualStudio.Editor.Services.Parser;
using Gherkin.Ast;

namespace Deveroom.VisualStudio.Discovery
{
    public interface IGherkinDocumentContext
    {
        IGherkinDocumentContext Parent { get; }
        object Node { get; }
    }

    public static class GherkinDocumentContextExtensions
    {
        public static bool IsRoot(this IGherkinDocumentContext context) => context.Parent == null;

        public static IEnumerable<object> GetNodes(this IGherkinDocumentContext context)
        {
            while (context != null)
            {
                if (context.Node != null)
                    yield return context.Node;
                context = context.Parent;
            }
        }

        public static IEnumerable<T> GetNodes<T>(this IGherkinDocumentContext context) 
            => context.GetNodes().OfType<T>();

        public static IEnumerable<Tag> GetTags(this IGherkinDocumentContext context)
        {
            return context.GetNodes<IHasTags>().SelectMany(ht => ht.Tags);
        }

        public static IEnumerable<string> GetTagNames(this IGherkinDocumentContext context)
            => context.GetTags().Select(t => t.Name);

        public static bool IsScenarioOutline(this IGherkinDocumentContext context)
            => context?.Node is ScenarioOutline;

        public static bool IsBackground(this IGherkinDocumentContext context)
            => context?.Node is Background;

        public static IGherkinDocumentContext GetParentOf<T>(this IGherkinDocumentContext context)
        {
            while (context.Parent != null && !(context.Parent.Node is T))
            {
                context = context.Parent;
            }
            return context.Parent;
        }

        public static T AncestorOrSelfNode<T>(this IGherkinDocumentContext context)
            where T : class
        {
            if (context.Node is T)
                return context.Node as T;

            while (context.Parent != null && !(context.Parent.Node is T))
            {
                context = context.Parent;
            }
            return context.Parent?.Node as T;
        }
    }
}