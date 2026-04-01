using WorldOfTheThreeKingdoms.ContentTools.Commands;
using WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

string repoRoot = AppContext.BaseDirectory;
for (int i = 0; i < 4; i++)
{
    string candidate = Path.GetFullPath(Path.Combine(repoRoot, ".."));
    if (File.Exists(Path.Combine(candidate, "WorldOfTheThreeKingdoms.sln")))
    {
        repoRoot = candidate;
        break;
    }

    repoRoot = candidate;
}

IReadOnlyList<IContentToolCommand> commands =
[
    new AuditAbsolutePathsCommand(),
    new BuildContentLinksCommand(),
    new NormalizeContentCaseCommand(),
    new PremultiplyAlphaCommand(),
    new ScanFontCharsCommand()
];

CommandContext context = new(repoRoot, Console.Out, Console.Error);
return ToolHost.Run(args, commands, context);
