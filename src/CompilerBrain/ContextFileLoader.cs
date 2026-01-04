namespace CompilerBrain;

public static class ContextFileLoader
{
    static readonly string[] FileNames = ["AGENTS.md", "CLAUDE.md"];

    public static ContextFiles Load(string? solutionPath)
    {
        var files = new Dictionary<string, string>();

        // Build search directories: working directory first, then solution root
        var searchDirs = new List<string> { Environment.CurrentDirectory };

        if (!string.IsNullOrEmpty(solutionPath))
        {
            var solutionDir = Path.GetDirectoryName(solutionPath);
            if (!string.IsNullOrEmpty(solutionDir) && solutionDir != Environment.CurrentDirectory)
            {
                searchDirs.Add(solutionDir);
            }
        }

        var seenContents = new HashSet<string>();
        foreach (var fileName in FileNames)
        {
            var content = FindAndReadFile(searchDirs, fileName);
            if (content != null && seenContents.Add(content))
            {
                files[fileName] = content;
            }
        }

        return new ContextFiles(files);
    }

    static string? FindAndReadFile(List<string> searchDirs, string fileName)
    {
        foreach (var dir in searchDirs)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    var name = Path.GetFileName(file);
                    if (name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return File.ReadAllText(file);
                    }
                }
            }
            catch
            {
                // Directory access error - continue to next directory
            }
        }

        return null;
    }
}
