using SpecFlowConnector;
using SpecFlowConnector.Optional.Extensions;

var log = new ConsoleLogger();
var fileSystem = new FileSystem();

Assembly TestAssemblyFactory(AssemblyLoadContext context, string testAssemblyPath) => context.LoadFromAssemblyPath(testAssemblyPath);

return new Runner(log)
    .Run(args, TestAssemblyFactory, fileSystem); 
