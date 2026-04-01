namespace WorldOfTheThreeKingdoms.ContentTools.Infrastructure;

public sealed class ArgumentReader
{
    private readonly Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> flags = new(StringComparer.OrdinalIgnoreCase);

    public ArgumentReader(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string current = args[i];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string key = current[2..];
            int separatorIndex = key.IndexOf('=');
            if (separatorIndex >= 0)
            {
                string inlineKey = key[..separatorIndex];
                string inlineValue = key[(separatorIndex + 1)..];
                values[inlineKey] = inlineValue;
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[key] = args[i + 1];
                i++;
                continue;
            }

            flags.Add(key);
        }
    }

    public bool HasFlag(string name)
    {
        return flags.Contains(name);
    }

    public bool TryGetValue(string name, out string value)
    {
        if (values.TryGetValue(name, out string? resolved))
        {
            value = resolved;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public string GetValueOrDefault(string name, string defaultValue)
    {
        return values.TryGetValue(name, out string? value) ? value : defaultValue;
    }
}
