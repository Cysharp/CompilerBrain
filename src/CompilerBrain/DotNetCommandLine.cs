using Zx;

namespace CompilerBrain;

public static class DotNetCommandLine
{
    public static async Task<string> GetVersionAsync()
    {
        // same as Environment.Version?
        return await "dotnet --version";
    }
}
