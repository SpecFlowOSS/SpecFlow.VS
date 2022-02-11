using Newtonsoft.Json;
using SpecFlowConnector.AssemblyLoading;
using SpecFlowConnector.Discovery;
using SpecFlowConnector.Tests;

namespace SpecFlowConnector;

public class Runner
{
    private readonly ILogger _log;

    public Runner(ILogger log)
    {
        _log = log;
    }

    public int Run(string[] args, Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory,
        IFileSystem fileSystem)
    {
        var internalLogger =
#if DEBUG
            _log;
#else
        new StringWriterLogger();
#endif
        try
        {
            return args
                .Map(ConnectorOptions.Parse)
                .Tie(DumpOptions)
                .Map(options =>
                    ReflectionExecutor.Execute((DiscoveryOptions) options, testAssemblyFactory, _log))
                //.Map(result=>result.MapLeft(errorMessage => new Exception(errorMessage)))
                .Map(result => result.Reduce(errorMessage => new DiscoveryResult(ImmutableArray<StepDefinition>.Empty,
                        ImmutableSortedDictionary<string, string>.Empty,
                        ImmutableSortedDictionary<string, string>.Empty,
                        errorMessage)))
                .Map<Exception, DiscoveryResult, string>(result => JsonConvert.SerializeObject(result, Formatting.Indented))
                .Map(JsonSerialization.MarkResult)
                .Tie(PrintResult)
                .Map<Exception, string, int>(_ => 0)
                .Reduce(HandleException);
        }
        catch (Exception ex)
        {
            _log.Debug(internalLogger.ToString());
            return HandleException(ex);
        }
    }

    public void DumpOptions(ConnectorOptions options) => _log.Debug(options.ToString());


    private void PrintResult(string result)
    {
        _log.Info(result);
    }

    private int HandleException(Exception ex)
    {
        return ex.Tie(e => _log.Error(e.Dump()))
            .Map(e => e is ArgumentException ? 3 : 4);
    }
}

public class ReflectionExecutor
{
    public static Either<string, DiscoveryResult> Execute(DiscoveryOptions options,
        Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory, ILogger _log)
    {
        _log.Debug($"Loading {options.AssemblyFile}");
        var testAssemblyContext = new TestAssemblyLoadContext(options.AssemblyFile, testAssemblyFactory, _log);
        var testAssembly = testAssemblyContext.TestAssembly;

        var executorType = typeof(ReflectionExecutor);
        string executorTypeName = executorType.FullName!;
        var executorAssembly = testAssemblyContext.LoadFromAssemblyPath(executorType.Assembly.Location);
        var reflectedType = executorAssembly.GetType(executorTypeName)!;

        var executorInstance = Activator.CreateInstance(reflectedType);

        var optionsJson = JsonConvert.SerializeObject(options);

        return executorInstance.ReflectionCallMethod<string>(
                nameof(Execute),
                new[] {typeof(string), typeof(Assembly), typeof(AssemblyLoadContext)},
                optionsJson, testAssembly, testAssemblyContext)
            .Map(JsonConvert.DeserializeObject<RunnerResult>)
            .Map(result =>
            {
                var (discoveryResult, log) = result;
                _log.Info(log);
                return discoveryResult ?? (Either<string, DiscoveryResult>) log;
            });
    }

    public string Execute(string optionsJson, Assembly testAssembly,
        AssemblyLoadContext assemblyLoadContext)
    {
        var log = new StringWriterLogger();
        var options = JsonConvert.DeserializeObject<DiscoveryOptions>(optionsJson);

        return Execute(log, options, testAssembly, assemblyLoadContext)
            .Reduce(ex =>
            {
                log.Error(ex.ToString());
                return null!;
            })
            .Map(dr => new RunnerResult(dr, log.ToString()))
            .Map(JsonConvert.SerializeObject);
    }

    private Either<Exception, DiscoveryResult> Execute(ILogger log, DiscoveryOptions options, Assembly testAssembly,
        AssemblyLoadContext assemblyLoadContext)
    {
        try
        {
            return new CommandFactory(log, new FileSystem(), options, testAssembly)
                .Map(factory => factory.CreateCommand())
                .Map(cmd => cmd.Execute(assemblyLoadContext));
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public record RunnerResult(DiscoveryResult? DiscoveryResult, string Log);
}
