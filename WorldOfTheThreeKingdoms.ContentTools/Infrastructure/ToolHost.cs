using WorldOfTheThreeKingdoms.ContentTools.Commands;

namespace WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

public static class ToolHost
{
    public static int Run(string[] args, IReadOnlyList<IContentToolCommand> commands, CommandContext context)
    {
        if (args.Length == 0)
        {
            WriteHelp(commands, context.Output);
            return 0;
        }

        string commandName = args[0];
        if (string.Equals(commandName, "help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(commandName, "--help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(commandName, "-h", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length == 1)
            {
                WriteHelp(commands, context.Output);
                return 0;
            }

            IContentToolCommand? specificCommand = FindCommand(commands, args[1]);
            if (specificCommand == null)
            {
                context.Error.WriteLine($"Unknown command: {args[1]}");
                return 1;
            }

            specificCommand.WriteUsage(context.Output);
            return 0;
        }

        IContentToolCommand? command = FindCommand(commands, commandName);
        if (command == null)
        {
            context.Error.WriteLine($"Unknown command: {commandName}");
            context.Error.WriteLine();
            WriteHelp(commands, context.Error);
            return 1;
        }

        string[] tailArgs = args.Length > 1 ? args[1..] : [];
        return command.Execute(tailArgs, context);
    }

    private static IContentToolCommand? FindCommand(IReadOnlyList<IContentToolCommand> commands, string name)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            if (string.Equals(commands[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return commands[i];
            }
        }

        return null;
    }

    private static void WriteHelp(IReadOnlyList<IContentToolCommand> commands, TextWriter writer)
    {
        writer.WriteLine("WorldOfTheThreeKingdoms.ContentTools");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project WorldOfTheThreeKingdoms.ContentTools -- <command> [options]");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        for (int i = 0; i < commands.Count; i++)
        {
            writer.Write("  ");
            writer.Write(commands[i].Name);
            writer.Write("  ");
            writer.WriteLine(commands[i].Description);
        }
        writer.WriteLine();
        writer.WriteLine("Run `help <command>` for command-specific usage.");
    }
}
