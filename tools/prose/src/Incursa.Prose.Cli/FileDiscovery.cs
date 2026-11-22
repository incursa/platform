namespace Incursa.Prose.Cli;

internal static class FileDiscovery
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md",
        ".markdown",
        ".txt",
    };

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".svn",
        "artifacts",
        "bin",
        "node_modules",
        "obj",
    };

    public static IReadOnlyList<string> Resolve(IEnumerable<string> paths, string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        var results = new HashSet<string>(StringComparer.Ordinal);
        foreach (var inputPath in paths)
        {
            var fullPath = Path.GetFullPath(inputPath, currentDirectory);
            if (File.Exists(fullPath))
            {
                EnsureSupported(fullPath);
                results.Add(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                DiscoverDirectory(fullPath, results);
            }
            else
            {
                throw new FileNotFoundException($"Path '{inputPath}' does not exist.", fullPath);
            }
        }

        return results.OrderBy(path => path, StringComparer.Ordinal).ToArray();
    }

    public static string GetDisplayPath(string fullPath, string currentDirectory)
    {
        var relative = Path.GetRelativePath(currentDirectory, fullPath);
        return relative.StartsWith("..", StringComparison.Ordinal) ? fullPath : relative;
    }

    private static void DiscoverDirectory(string root, HashSet<string> results)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out var directory))
        {
            foreach (var file in Directory.EnumerateFiles(directory).OrderBy(path => path, StringComparer.Ordinal))
            {
                if (SupportedExtensions.Contains(Path.GetExtension(file)))
                {
                    results.Add(Path.GetFullPath(file));
                }
            }

            foreach (var child in Directory.EnumerateDirectories(directory).OrderByDescending(path => path, StringComparer.Ordinal))
            {
                var info = new DirectoryInfo(child);
                if (!ExcludedDirectories.Contains(info.Name) && !info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    pending.Push(child);
                }
            }
        }
    }

    private static void EnsureSupported(string path)
    {
        if (!SupportedExtensions.Contains(Path.GetExtension(path)))
        {
            throw new NotSupportedException($"File '{path}' is not Markdown or plain text (.md, .markdown, .txt).");
        }
    }
}
