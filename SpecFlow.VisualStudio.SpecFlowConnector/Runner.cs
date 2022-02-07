using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using SpecFlow.VisualStudio.SpecFlowConnector.Tests;
using SpecFlowConnector.Discovery;

namespace SpecFlowConnector;

public class Runner
{
    private readonly ILogger _log;

    public Runner(ILogger log)
    {
        _log = log;
    }

    public int Run(string[] args, Func<string, Assembly> testAssemblyFactory, IFileSystem fileSystem)
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
                .Map(options => ReflectionExecutor.Execute((DiscoveryOptions)options, fileSystem, testAssemblyFactory, _log))
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
    public static string Execute(DiscoveryOptions options, IFileSystem fileSystem,
        Func<string, Assembly> testAssemblyFactory, ILogger _log)
    {
        //_log.Debug($"Loading {options.AssemblyFile.FullName}");
        //var testAssembly = testAssemblyFactory(options.AssemblyFile);
        //_log.Debug($"Loaded: {testAssembly}");

        //var testAssemblyContext = new TestAssemblyLoadContext(testAssembly);

        var testAssemblyContext = new TestAssemblyLoadContext(options.AssemblyFile);
        var testAssembly = testAssemblyContext.Assembly;

        var executorType = typeof(ReflectionExecutor);
        string executorTypeName = executorType.FullName!;
        var executorAssembly = testAssemblyContext.LoadFromAssemblyPath(executorType.Assembly.Location);
        var reflectedType = executorAssembly.GetType(executorTypeName)!;

        var executorInstance = Activator.CreateInstance(reflectedType);

        var optionsJson = JsonConvert.SerializeObject(options);

        return executorInstance.ReflectionCallMethod<string>(
            nameof(Execute),
            new[] {typeof(string), typeof(IFileSystem), typeof(Assembly), typeof(AssemblyLoadContext) },
            optionsJson, fileSystem, testAssembly, testAssemblyContext);
    }

    private string Execute(string optionsJson, IFileSystem fileSystem, Assembly testAssembly, AssemblyLoadContext assemblyLoadContext)
    {
        var log = new StringBuilderLogger();
        var options = JsonConvert.DeserializeObject<DiscoveryOptions>(optionsJson);

        return new CommandFactory(log, fileSystem, options, testAssembly)
            .Map(factory => factory.CreateCommand())
            .Map(cmd => cmd.Execute(assemblyLoadContext))
            .Map(result => JsonSerialization.MarkResult(result.Json))
            .Reduce(ex => ex.ToString());
    }
}