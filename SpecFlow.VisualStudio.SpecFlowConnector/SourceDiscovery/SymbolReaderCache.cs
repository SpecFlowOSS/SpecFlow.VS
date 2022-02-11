using SpecFlowConnector.SourceDiscovery.DnLib;

namespace SpecFlowConnector.SourceDiscovery;

public class SymbolReaderCache
{
    private readonly ILogger _log;
    private readonly Dictionary<Assembly, Option<DeveroomSymbolReader>> _symbolReaders = new(2);

    public SymbolReaderCache(ILogger log)
    {
        _log = log;
    }

    public Option<DeveroomSymbolReader> this[Assembly assembly] => GetOrCreateSymbolReader(assembly);

    private Option<DeveroomSymbolReader> GetOrCreateSymbolReader(Assembly assembly) 
    {
        if (_symbolReaders.TryGetValue(assembly, out var symbolReader))
            return symbolReader;

        return CreateSymbolReader(assembly.Location)
#if NETFRAMEWORK
            .Or(()=>CreateSymbolReader(new Uri(assembly.CodeBase).LocalPath))
#endif
            .Or(()=>CreateSymbolReader(new Uri(assembly.Location).LocalPath))
            .Tie(reader => _symbolReaders.Add(assembly, reader));
    }

    protected Option<DeveroomSymbolReader> CreateSymbolReader(
        string assemblyFilePath) =>
        SymbolReaderFactories(assemblyFilePath)
            .SelectOptional(TryCreateReader)
            .FirstOrNone();

    private IEnumerable<Func<DeveroomSymbolReader>> SymbolReaderFactories(string path)
    {
        return new Func<DeveroomSymbolReader>[]
        {
            () => DnLibDeveroomSymbolReader.Create(_log,path)
            //path => new ComDeveroomSymbolReader(path),
        };
    }

    private Option<DeveroomSymbolReader> TryCreateReader(Func<DeveroomSymbolReader> factory)
    {
        try
        {
            return factory();
        }
        catch (Exception ex)
        {
            _log.Info(ex.ToString());
        }

        return None<DeveroomSymbolReader>.Value;
    }

    private static bool CreationFailed(Option<DeveroomSymbolReader> reader)
    {
        return reader
            .Map(_ => false)
            .Reduce(true);
    }

    private static Option<Exception> CompressWarnings(ImmutableArray<Exception> warnings)
    {
        return warnings.Aggregate(
                (acc, cur) =>
                {
                    var exceptions = ImmutableHashSet.CreateBuilder<Exception>();
                    if (acc is AggregateException ae) exceptions.UnionWith(ae.InnerExceptions);

                    exceptions.Add(cur);
                    return new AggregateException(exceptions.ToImmutable());
                })
            .Map(e => (Option<Exception>)e);
    }

}
