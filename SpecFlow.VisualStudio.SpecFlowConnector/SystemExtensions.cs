namespace SpecFlow.VisualStudio.SpecFlowConnector;

public static class SystemExtensions
{
    public static TResult TryCatch<TSource, TResult>(
        this TSource source,
        Func<TSource, TResult> bodyFn,
        Func<Exception, TResult> catchFn)
    {
        try
        {
            return bodyFn(source);
        }
        catch (Exception e)
        {
            return catchFn(e);
        }
    }
}
