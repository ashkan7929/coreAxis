namespace CoreAxis.SharedKernel.Context;

public class ContextPath
{
    public string Path { get; }
    public string[] Segments { get; }

    public ContextPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));

        Path = path;
        Segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
    }

    public static implicit operator string(ContextPath path) => path.Path;
    public static implicit operator ContextPath(string path) => new(path);
    
    public override string ToString() => Path;
}
