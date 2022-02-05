using SpecFlowConnector;

var log = new ConsoleLogger();
var fileSystem = new FileSystem();

TestAssemblyLoadContext TestAssemblyLoadContext(string path)
{
    return new TestAssemblyLoadContext(path);
}

return new Runner(log)
    .Run(args, TestAssemblyLoadContext, fileSystem); 
