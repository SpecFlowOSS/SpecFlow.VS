#if NETFRAMEWORK
// ReSharper disable once CheckNamespace
namespace System
{
    public static class StringExtensions
    {
        public static bool Contains(this string @this, string value, StringComparison comparisonType) =>
            @this.Contains(value);
    }
}
#endif