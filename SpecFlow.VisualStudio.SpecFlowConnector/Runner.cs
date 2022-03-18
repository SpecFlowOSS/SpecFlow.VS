using System.Runtime.Versioning;

namespace SpecFlowConnector;

public class Runner
{
    private readonly ILogger _log;

    public Runner(ILogger log)
    {
        _log = log;
    }

    public int Run(string[] args, Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory)
    {
        try
        {
            var analytics = new AnalyticsContainer();
            analytics.AddAnalyticsProperty("Connector", GetType().Assembly.ToString());
            return args
                .Map(ConnectorOptions.Parse)
                .Tie(DumpOptions)
                .Map(options =>
                    ReflectionExecutor.Execute((DiscoveryOptions) options, testAssemblyFactory, _log, analytics))
                .Map(result => result.Reduce(errorMessage => new DiscoveryResult(ImmutableArray<StepDefinition>.Empty,
                    ImmutableSortedDictionary<string, string>.Empty,
                    ImmutableSortedDictionary<string, string>.Empty,
                    analytics,
                    errorMessage)))
                .Map(JsonSerialization.SerializeObject)
                .Map(JsonSerialization.MarkResult)
                .Tie(PrintResult)
                .Map(_ => 0);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    public void DumpOptions(ConnectorOptions options) => _log.Info(options.ToString());


    private void PrintResult(string result)
    {
        _log.Info(result);
    }

    private int HandleException(Exception ex)
    {
        return ex.Tie(e => _log.Error(e.ToString()))
            .Map(e => e is ArgumentException ? 3 : 4);
    }
}

public class ReflectionExecutor
{
    public static Either<string, DiscoveryResult> Execute(DiscoveryOptions options,
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

        return typeof(ReflectionExecutor)
            .Map(t => (typeName: t.FullName!, assembly: testAssemblyContext.LoadFromAssemblyPath(t.Assembly.Location)))
            .Map(x => x.assembly.GetType(x.typeName)!)
            .Map(CreateInstance)
            .Map(instance => instance.ReflectionCallMethod<string>(
                    nameof(Execute),
                    JsonSerialization.SerializeObject(options), testAssemblyContext.TestAssembly, testAssemblyContext,
                    analytics)
                .Map(s => JsonSerialization.DeserializeObject<RunnerResult>(s)
                    .Reduce(new RunnerResult(null!, $"Unable to deserialize{s}")))
                .Map(result =>
                {
                    var (discoveryResult, log) = result;
                    _log.Info(log);
                    return discoveryResult ?? (Either<string, DiscoveryResult>) log;
                }))
            .Reduce(reflectedType => $"Could not create instance from:{reflectedType}");
    }

    private static Either<Type, object> CreateInstance(Type reflectedType) =>
        Activator.CreateInstance(reflectedType) ?? reflectedType;

    public string Execute(string optionsJson, Assembly testAssembly,
        AssemblyLoadContext assemblyLoadContext, IDictionary<string, string> analyticsProperties)
    {
        var analytics = new AnalyticsContainer(analyticsProperties);
        var log = new StringWriterLogger();
        return JsonSerialization.DeserializeObject<DiscoveryOptions>(optionsJson)
            .Map(options => Execute(log, options, testAssembly, assemblyLoadContext, analytics)
                .Reduce(ex =>
                {
                    var errorMessage = ex.ToString();
                    log.Error(errorMessage);
                    return new DiscoveryResult(ImmutableArray<StepDefinition>.Empty,
                        ImmutableSortedDictionary<string, string>.Empty,
                        ImmutableSortedDictionary<string, string>.Empty,
                        analytics,
                        errorMessage);
                })
                .Map(dr => new RunnerResult(dr, log.ToString()))
                .Map(JsonSerialization.SerializeObject)
            )
            .Reduce($"Unable to deserialize {optionsJson}");
    }

    private Either<Exception, DiscoveryResult> Execute(ILogger log, DiscoveryOptions options, Assembly testAssembly,
        AssemblyLoadContext assemblyLoadContext, IAnalyticsContainer analytics)
    {
        try
        {
            return new CommandFactory(log, options, testAssembly, analytics)
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
