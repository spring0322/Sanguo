using WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

namespace WorldOfTheThreeKingdoms.ContentTools.Commands;

public interface IContentToolCommand
{
    string Name { get; }

    string Description { get; }

    int Execute(string[] args, CommandContext context);

    void WriteUsage(TextWriter writer);
}
