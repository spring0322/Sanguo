using System.Text.RegularExpressions;
using WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

namespace WorldOfTheThreeKingdoms.ContentTools.Commands;

public sealed class AuditAbsolutePathsCommand : IContentToolCommand
{
    private static readonly string[] DefaultExtensions =
    [
        ".cs",
        ".csproj",
        ".sln",
        ".ps1",
        ".bat",
        ".cmd",
        ".json",
        ".xml"
    ];

    private static readonly Regex QuotedAbsolutePathPattern = new(
        "(@?\"|\')([A-Za-z]:\\\\|[A-Za-z]:\\\\\\\\)",
        RegexOptions.Compiled);

    private static readonly Regex ScriptAbsolutePathPattern = new(
        "(?<![A-Za-z0-9_])[A-Za-z]:\\\\",
        RegexOptions.Compiled);

    public string Name => "audit-absolute-paths";

    public string Description => "Scan source files for hard-coded absolute Windows path literals.";

    public int Execute(string[] args, CommandContext context)
    {
        ArgumentReader reader = new(args);
        string root = ResolvePath(context.RepoRoot, reader.GetValueOrDefault("root", context.RepoRoot));
        bool failOnMatch = reader.HasFlag("fail-on-match");
        bool includeRecord = reader.HasFlag("include-record");
        HashSet<string> extensions = ParseExtensions(reader.GetValueOrDefault("extensions", string.Join(',', DefaultExtensions)));

        if (!Directory.Exists(root))
        {
            context.Error.WriteLine($"Root directory not found: {root}");
            return 1;
        }

        string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
        int matchCount = 0;
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            if (ShouldSkipFile(file, extensions, includeRecord))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (ShouldSkipLine(file, line))
                {
                    continue;
                }

                if (!HasAbsolutePathLiteral(file, line))
                {
                    continue;
                }

                string relativePath = Path.GetRelativePath(root, file).Replace('/', '\\');
                context.Output.WriteLine($"{relativePath}:{lineIndex + 1}:{line.Trim()}");
                matchCount++;
            }
        }

        context.Output.WriteLine();
        context.Output.WriteLine($"Absolute path literal matches: {matchCount}");

        if (matchCount > 0 && failOnMatch)
        {
            return 1;
        }

        return 0;
    }

    public void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("audit-absolute-paths");
        writer.WriteLine("  --root <path>          Root directory to scan. Defaults to repo root.");
        writer.WriteLine("  --extensions <list>    Comma-separated file extensions to scan.");
        writer.WriteLine("  --include-record       Include the historical Record/ directory in the scan.");
        writer.WriteLine("  --fail-on-match        Return exit code 1 when any match is found.");
    }

    private static bool HasAbsolutePathLiteral(string file, string line)
    {
        string extension = Path.GetExtension(file);
        if (extension.Equals(".bat", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase))
        {
            return ScriptAbsolutePathPattern.IsMatch(line);
        }

        if (extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase))
        {
            return line.Contains('"') || line.Contains('\'')
                ? QuotedAbsolutePathPattern.IsMatch(line)
                : ScriptAbsolutePathPattern.IsMatch(line);
        }

        return QuotedAbsolutePathPattern.IsMatch(line);
    }

    private static bool ShouldSkipFile(string file, HashSet<string> extensions, bool includeRecord)
    {
        string extension = Path.GetExtension(file);
        if (!extensions.Contains(extension))
        {
            return true;
        }

        string normalized = file.Replace('/', '\\');
        if (!includeRecord && normalized.Contains("\\Record\\", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalized.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\.git\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\.vs\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\.dotnet\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\.dotnet-home\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\.codex_obj\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\packages\\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldSkipLine(string file, string line)
    {
        string trimmed = line.TrimStart();
        string extension = Path.GetExtension(file);

        if (trimmed.Length == 0)
        {
            return true;
        }

        if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".ps1", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.StartsWith("//", StringComparison.Ordinal) ||
                   trimmed.StartsWith("#", StringComparison.Ordinal);
        }

        if (extension.Equals(".bat", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.StartsWith("REM ", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("::", StringComparison.Ordinal);
        }

        if (extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.StartsWith("<!--", StringComparison.Ordinal);
        }

        return false;
    }

    private static HashSet<string> ParseExtensions(string extensions)
    {
        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);
        string[] parts = extensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string value = parts[i];
            result.Add(value.StartsWith('.') ? value : "." + value);
        }

        return result;
    }

    private static string ResolvePath(string repoRoot, string path)
    {
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(repoRoot, path));
    }
}
