using System.Security;
using System.Text;
using WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

namespace WorldOfTheThreeKingdoms.ContentTools.Commands;

public sealed class BuildContentLinksCommand : IContentToolCommand
{
    public string Name => "build-content-links";

    public string Description => "Generate MSBuild content link items from a source directory.";

    public int Execute(string[] args, CommandContext context)
    {
        ArgumentReader reader = new(args);
        string sourceRoot = ResolvePath(
            context.RepoRoot,
            reader.GetValueOrDefault("source-root", Path.Combine(context.RepoRoot, "Content")));
        string includePrefix = reader.GetValueOrDefault("include-prefix", @"..\Content");
        string linkPrefix = reader.GetValueOrDefault("link-prefix", @"Content");
        string extensions = reader.GetValueOrDefault("extensions", string.Empty);

        if (!Directory.Exists(sourceRoot))
        {
            context.Error.WriteLine($"Source directory not found: {sourceRoot}");
            return 1;
        }

        HashSet<string>? allowedExtensions = ParseExtensions(extensions);
        string[] files = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);
        StringBuilder builder = new(files.Length * 96);
        int writtenCount = 0;
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            string extension = Path.GetExtension(file);
            if (allowedExtensions != null && !allowedExtensions.Contains(extension))
            {
                continue;
            }

            string relative = Path.GetRelativePath(sourceRoot, file).Replace('/', '\\');
            string includePath = CombineMsBuildPath(includePrefix, relative);
            string linkPath = CombineMsBuildPath(linkPrefix, relative);

            builder.Append("<Content Include=\"");
            builder.Append(SecurityElement.Escape(includePath));
            builder.AppendLine("\">");
            builder.Append("  <Link>");
            builder.Append(SecurityElement.Escape(linkPath));
            builder.AppendLine("</Link>");
            builder.AppendLine("  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");
            builder.AppendLine("</Content>");
            writtenCount++;
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
            context.Output.WriteLine($"Wrote {writtenCount} items to {resolvedOutput}");
            return 0;
        }

        context.Output.Write(builder.ToString());
        return 0;
    }

    public void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("build-content-links");
        writer.WriteLine("  --source-root <path>     Source directory to scan. Defaults to <repo>/Content");
        writer.WriteLine("  --include-prefix <path>  Include prefix. Defaults to ..\\Content");
        writer.WriteLine("  --link-prefix <path>     Link prefix. Defaults to Content");
        writer.WriteLine("  --extensions <list>      Comma-separated list like .png,.jpg");
        writer.WriteLine("  --output <path>          Optional output file. Prints to stdout when omitted.");
    }

    private static HashSet<string>? ParseExtensions(string extensions)
    {
        if (string.IsNullOrWhiteSpace(extensions))
        {
            return null;
        }

        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);
        string[] parts = extensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string value = parts[i];
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            result.Add(value.StartsWith('.') ? value : "." + value);
        }

        return result;
    }

    private static string CombineMsBuildPath(string prefix, string relative)
    {
        return prefix.TrimEnd('\\', '/') + "\\" + relative;
    }

    private static string ResolvePath(string repoRoot, string path)
    {
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(repoRoot, path));
    }
}
