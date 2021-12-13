namespace SpecFlow.VisualStudio.Common;

public sealed record FileDetails
{
    public static FileDetails Missing = new (new FileInfo(string.Empty));

    private readonly FileInfo _file;

    private FileDetails(FileInfo file)
    {
        _file = file;
    }

    public static FileDetails Combine(string path1, string path2) => FromPath(Path.Combine(path1, path2));

    public string FullName => _file.FullName;
    public string Name => _file.Name;

    public static FileDetails FromPath(string path) => new(new FileInfo(path));
    public static implicit operator string(FileDetails path) => path.FullName;

    public override string ToString() => _file.FullName;
}
