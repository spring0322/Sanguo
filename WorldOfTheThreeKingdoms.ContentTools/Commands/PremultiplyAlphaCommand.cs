using WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

namespace WorldOfTheThreeKingdoms.ContentTools.Commands;

public sealed class PremultiplyAlphaCommand : IContentToolCommand
{
    public string Name => "premultiply-alpha";

    public string Description => "Reserved command slot for the future alpha preprocessing backend.";

    public int Execute(string[] args, CommandContext context)
    {
        ArgumentReader reader = new(args);
        string input = reader.GetValueOrDefault("input", Path.Combine(context.RepoRoot, "Content", "Textures"));
        string output = reader.GetValueOrDefault("output", Path.Combine(context.RepoRoot, "Content", "TexturesAlpha"));

        context.Output.WriteLine("premultiply-alpha is scaffolded but intentionally not implemented yet.");
        context.Output.WriteLine("Recommended next step: choose a dedicated image backend for .NET 8 and wire it here.");
        context.Output.WriteLine($"Planned input root: {ResolvePath(context.RepoRoot, input)}");
        context.Output.WriteLine($"Planned output root: {ResolvePath(context.RepoRoot, output)}");
        return 2;
    }

    public void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("premultiply-alpha");
        writer.WriteLine("  --input <path>   Input texture root. Defaults to <repo>/Content/Textures");
        writer.WriteLine("  --output <path>  Output root. Defaults to <repo>/Content/TexturesAlpha");
        writer.WriteLine();
        writer.WriteLine("This command is scaffold-only in the current revision.");
    }

    private static string ResolvePath(string repoRoot, string path)
    {
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(repoRoot, path));
    }
}
