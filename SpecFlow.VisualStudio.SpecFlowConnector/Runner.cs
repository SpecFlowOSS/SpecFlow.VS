using Newtonsoft.Json;
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
        new StringBuilderLogger();
#endif
        try
        {
            return args
                .Map(ConnectorOptions.Parse)
                .Tie(DumpOptions)
                .Map(options =>
                    ReflectionExecutor.Execute((DiscoveryOptions) options, testAssemblyFactory, _log))
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
    public record RunnerResult(DiscoveryResult? DiscoveryResult, string log);
    
    public static string Execute(DiscoveryOptions options, Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory, ILogger _log)
    {
        _log.Debug($"Loading {options.AssemblyFile}");
        var testAssemblyContext = new TestAssemblyLoadContext(options.AssemblyFile, testAssemblyFactory);
        var testAssembly = testAssemblyContext.Assembly;
        _log.Debug($"Loaded: {testAssembly}");

        var executorType = typeof(ReflectionExecutor);
        string executorTypeName = executorType.FullName!;
        var executorAssembly = testAssemblyContext.LoadFromAssemblyPath(executorType.Assembly.Location);
        var reflectedType = executorAssembly.GetType(executorTypeName)!;

        var executorInstance = Activator.CreateInstance(reflectedType);

        var optionsJson = JsonConvert.SerializeObject(options);

        var m = reflectedType.GetMethods();
        var mi = m[1];
        mi.Invoke(executorInstance, new object[]{ optionsJson, testAssembly, testAssemblyContext});

        return executorInstance.ReflectionCallMethod<string>(
                nameof(Execute),
                new[] {typeof(string), typeof(Assembly), typeof(AssemblyLoadContext)},
                optionsJson, testAssembly, testAssemblyContext as AssemblyLoadContext)
            .Map(JsonConvert.DeserializeObject<RunnerResult>)
            .Map(result =>
            {
                var (discoveryResult, log) = result;
                _log.Info(log);
                return discoveryResult;
            })
            .Map(dr=>JsonConvert.SerializeObject(dr, Formatting.Indented))
            .Map(JsonSerialization.MarkResult);
    }

    public string Execute(string optionsJson,  Assembly testAssembly,
        AssemblyLoadContext assemblyLoadContext)
    {
        var log = new StringBuilderLogger();
        var options = JsonConvert.DeserializeObject<DiscoveryOptions>(optionsJson);

        return new CommandFactory(log, null, options, testAssembly)
            .Map(factory => factory.CreateCommand())
            .Map(cmd => cmd.Execute(assemblyLoadContext))
            .Reduce(ex =>
            {
                log.Error(ex.ToString());
                return null!;
            })
            .Map(dr => new RunnerResult(dr, log.ToString()))
            .Map(JsonConvert.SerializeObject);
    }
}


