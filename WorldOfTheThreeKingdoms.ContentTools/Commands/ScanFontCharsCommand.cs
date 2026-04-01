using System.Text;
using WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

namespace WorldOfTheThreeKingdoms.ContentTools.Commands;

public sealed class ScanFontCharsCommand : IContentToolCommand
{
    public string Name => "scan-font-chars";

    public string Description => "Collect unique characters from selected text-like files.";

    public int Execute(string[] args, CommandContext context)
    {
        ArgumentReader reader = new(args);
        string root = ResolvePath(
            context.RepoRoot,
            reader.GetValueOrDefault("root", context.RepoRoot));
        string extensions = reader.GetValueOrDefault("extensions", ".cs,.json,.xml,.txt,.md");

        if (!Directory.Exists(root))
        {
            context.Error.WriteLine($"Root directory not found: {root}");
            return 1;
        }

        HashSet<string> allowedExtensions = ParseExtensions(extensions);
        SortedSet<char> characters = [];
        string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string extension = Path.GetExtension(file);
            if (!allowedExtensions.Contains(extension))
            {
                continue;
            }

            string text = File.ReadAllText(file, Encoding.UTF8);
            for (int chIndex = 0; chIndex < text.Length; chIndex++)
            {
                char ch = text[chIndex];
                if (char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t')
                {
                    continue;
                }

                characters.Add(ch);
            }
        }

        StringBuilder builder = new(characters.Count);
        foreach (char ch in characters)
        {
            builder.Append(ch);
        }

        if (reader.TryGetValue("output", out string? outputPath))
        {
            string resolvedOutput = ResolvePath(context.RepoRoot, outputPath);
            string? outputDirectory = Path.GetDirectoryName(resolvedOutput);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllText(resolvedOutput, builder.ToString(), Encoding.UTF8);
            context.Output.WriteLine($"Wrote {characters.Count} characters to {resolvedOutput}");
            return 0;
        }

        context.Output.Write(builder.ToString());
        return 0;
    }

    public void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("scan-font-chars");
        writer.WriteLine("  --root <path>         Root directory to scan. Defaults to repo root.");
        writer.WriteLine("  --extensions <list>   Comma-separated list like .cs,.json,.xml,.txt,.md");
        writer.WriteLine("  --output <path>       Optional output file. Prints to stdout when omitted.");
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
