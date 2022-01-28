namespace SpecFlow.VisualStudio.SpecFlowConnector;

public static class FunctionalExtensions
{
    public static TResult Map<TSource, TResult>(this TSource @this, Func<TSource, TResult> fn) => fn(@this);
}
