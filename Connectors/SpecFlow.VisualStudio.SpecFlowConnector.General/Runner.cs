using System.Runtime.Versioning;

namespace SpecFlowConnector;

public class Runner
{
    private readonly ILogger _log;
    readonly AnalyticsContainer _analytics;

    public enum ExecutionResult
    {
        Succeed = 0,
        ArgumentError = 3,
        GenericError = 4
    };

    public Runner(ILogger log)
    {
        _log = log;
        _analytics = new AnalyticsContainer();
        _analytics.AddAnalyticsProperty("Connector", GetType().Assembly.ToString());
    }

    public ExecutionResult Run(string[] args, Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory)
    {
        try
        {
            return args
                .Map(ConnectorOptions.Parse)
                .Tie(DumpOptions)
                .Map(options => ExecuteDiscovery((DiscoveryOptions)options, testAssemblyFactory))
                .Map(JsonSerialization.SerializeObject)
                .Map(JsonSerialization.MarkResult)
                .Tie(PrintResult)
                .Map(_=>ExecutionResult.Succeed);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    public void DumpOptions(ConnectorOptions options) => _log.Info(options.ToString());

    public ConnectorResult ExecuteDiscovery(DiscoveryOptions options, Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory)
        => ReflectionExecutor.Execute(options, testAssemblyFactory, _log, _analytics);

    private void PrintResult(string result)
    {
        _log.Info(result);
    }
 
    private ExecutionResult HandleException(Exception ex)
    {
        return ex.Tie(e => _log.Error(e.ToString()))
            .Map(e => e is ArgumentException 
                        ? ExecutionResult.ArgumentError 
                        : ExecutionResult.GenericError
            );
    }
}

public class ReflectionExecutor
{
    public static ConnectorResult Execute(DiscoveryOptions options,
        Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory, ILogger _log, IAnalyticsContainer analytics)
    {
        _log.Info($"Loading {options.AssemblyFile}");
        var testAssemblyContext = new TestAssemblyLoadContext(options.AssemblyFile, testAssemblyFactory, _log);
        analytics.AddAnalyticsProperty("ImageRuntimeVersion", testAssemblyContext.TestAssembly.ImageRuntimeVersion);

        testAssemblyContext.TestAssembly.CustomAttributes
            .Where(a => a.AttributeType == typeof(TargetFrameworkAttribute))
            .FirstOrNone()
            .Tie(tf => analytics
                .AddAnalyticsProperty("TargetFramework", tf.ConstructorArguments.First().ToString().Trim('\"'))
            );

        return TypeFromAssemblyLoadContext(typeof(ReflectionExecutor), testAssemblyContext)
            .Map(CreateInstance)
            .Map(instance => instance.ReflectionCallMethod<string>(
                    nameof(Execute),
                    JsonSerialization.SerializeObject(options), testAssemblyContext.TestAssembly, testAssemblyContext,
                    analytics)
                .Map(s => JsonSerialization.DeserializeObject<RunnerResult>(s)
                    .Reduce(new RunnerResult(_log.ToString()!, analytics.ToImmutable(), null!, $"Unable to parse JSON text:{s}")))
                .Map(result =>
                {
                    var (log, analyticsProperties, discoveryResult, errorMessage) = result;
                    _log.Info(log);
                    if (discoveryResult != null)
                    {
                        return new ConnectorResult(
                            discoveryResult.StepDefinitions,
                            discoveryResult.SourceFiles,
                            discoveryResult.TypeNames,
                            analyticsProperties,
                            errorMessage);
                    }
                    return new ConnectorResult(ImmutableArray<StepDefinition>.Empty,
                    ImmutableSortedDictionary<string, string>.Empty,
                    ImmutableSortedDictionary<string, string>.Empty,
                    analytics.ToImmutable(),
                    log);
                }))
            .Reduce(new ConnectorResult(ImmutableArray<StepDefinition>.Empty,
                    ImmutableSortedDictionary<string, string>.Empty,
                    ImmutableSortedDictionary<string, string>.Empty,
                    analytics.ToImmutable(),
                    $"Could not create instance from: {typeof(ReflectionExecutor)}")
            );
    }

    public static Type TypeFromAssemblyLoadContext(Type reType, TestAssemblyLoadContext testAssemblyContext) 
        => testAssemblyContext.LoadFromAssemblyPath(reType.Assembly.Location).GetType(reType.FullName!)!;

    private static Option<object> CreateInstance(Type reflectedType) =>
        Activator.CreateInstance(reflectedType);

    public string Execute(string optionsJson, Assembly testAssembly,
        AssemblyLoadContext assemblyLoadContext, IDictionary<string, string> analyticsProperties)
    {
        var analytics = new AnalyticsContainer(analyticsProperties);
        var log = new StringWriterLogger();
        return JsonSerialization.DeserializeObject<DiscoveryOptions>(optionsJson)
            .Map(options => EitherAdapters.Try(
                    () => new CommandFactory(log, options, testAssembly, analytics)
                    .Map(factory => factory.CreateCommand())
                    .Map(cmd => cmd.Execute(assemblyLoadContext))
                )
                .Map(dr => new RunnerResult(log.ToString(), analytics.ToImmutable(), dr, null))
                .Reduce(ex =>
                {
                    var errorMessage = ex.ToString();
                    log.Error(errorMessage);
                    return new RunnerResult(
                        log.ToString(),
                        analytics,
                        null,
                        errorMessage);
                })
                
            )
            .Reduce(new RunnerResult(log.ToString(), analytics, null, $"Unable to deserialize {optionsJson}"))
            .Map(JsonSerialization.SerializeObject);
    }
    public record RunnerResult(string Log, ImmutableSortedDictionary<string, string> AnalyticsProperties, DiscoveryResult? DiscoveryResult, string? errorMessage);
}
