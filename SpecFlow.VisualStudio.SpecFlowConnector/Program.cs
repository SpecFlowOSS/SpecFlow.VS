var log = new ConsoleLogger();
var fileSystem = new FileSystem();

Assembly AssemblyFromPath(string path)
{
    return new TestAssemblyLoadContext(typeof(Program).Assembly.Location)
        .LoadFromAssemblyPath(path);
}

return new Runner(log)
    .Run(args, AssemblyFromPath, fileSystem); 
