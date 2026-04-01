using WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

namespace WorldOfTheThreeKingdoms.ContentTools.Commands;

public sealed class NormalizeContentCaseCommand : IContentToolCommand
{
    public string Name => "normalize-content-case";

    public string Description => "Normalize file extensions to lowercase, dry-run by default.";

    public int Execute(string[] args, CommandContext context)
    {
        ArgumentReader reader = new(args);
        string root = ResolvePath(
            context.RepoRoot,
            reader.GetValueOrDefault("root", Path.Combine(context.RepoRoot, "Content")));
        bool apply = reader.HasFlag("apply");

        if (!Directory.Exists(root))
        {
            context.Error.WriteLine($"Root directory not found: {root}");
            return 1;
        }

        string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
        int renameCount = 0;
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            string extension = Path.GetExtension(file);
            string lowerExtension = extension.ToLowerInvariant();
            if (extension == lowerExtension)
            {
                continue;
            }

            string targetPath = file[..^extension.Length] + lowerExtension;
            context.Output.WriteLine($"{file} -> {targetPath}");
            renameCount++;

            if (!apply)
            {
                continue;
            }

            string tempPath = targetPath + ".casefix.tmp";
            File.Move(file, tempPath, overwrite: true);
            File.Move(tempPath, targetPath, overwrite: true);
        }

        if (!apply)
        {
            context.Output.WriteLine($"Dry run complete. {renameCount} rename(s) detected.");
            return 0;
        }

        context.Output.WriteLine($"Applied {renameCount} rename(s).");
        return 0;
    }

    public void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("normalize-content-case");
        writer.WriteLine("  --root <path>   Root directory to scan. Defaults to <repo>/Content");
        writer.WriteLine("  --apply         Apply renames. Omit to run in dry-run mode.");
    }

    private static string ResolvePath(string repoRoot, string path)
    {
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(repoRoot, path));
    }
}
