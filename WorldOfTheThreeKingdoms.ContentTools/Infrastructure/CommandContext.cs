namespace WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

public sealed class CommandContext(string repoRoot, TextWriter output, TextWriter error)
{
    public string RepoRoot { get; } = repoRoot;

    public TextWriter Output { get; } = output;

    public TextWriter Error { get; } = error;
}
