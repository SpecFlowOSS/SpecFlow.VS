using SpecFlowConnector;

var log = new ConsoleLogger();
var fileSystem = new FileSystem();

Assembly TestAssemblyFactory(string path) => Assembly.LoadFrom(path);

return new Runner(log)
    .Run(args, TestAssemblyFactory, fileSystem); 
