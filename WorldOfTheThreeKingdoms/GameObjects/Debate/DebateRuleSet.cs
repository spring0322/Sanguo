using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace WorldOfTheThreeKingdoms.GameObjects.Debate;

public sealed class DebateRuleSet
{
    private static DebateRuleSet cached;

    public int LogicTicksPerSecond { get; init; } = 60;

    public int CommandWindowTicks { get; init; } = 72;

    public int PlaybackTicks { get; init; } = 24;

    public int ResultDisplayTicks { get; init; } = 18;

    public float StartingMomentum { get; init; } = 100f;

    public int StartingFocus { get; init; }

    public int MaxFocus { get; init; } = 100;

    public int MaxRounds { get; init; } = 12;

    public int BaseDamage { get; init; } = 10;

    public float AttributeScale { get; init; } = 0.8f;

    public float AttributeDiffScale { get; init; } = 0.15f;

    public float AdvantageModifier { get; init; } = 1.25f;

    public float NeutralModifier { get; init; } = 1f;

    public float DisadvantageModifier { get; init; } = 0.75f;

    public int FocusGainPerRound { get; init; } = 5;

    public int FocusGainOnAdvantage { get; init; } = 15;

    public int FocusGainOnHit { get; init; } = 10;

    public int FocusGainOnTie { get; init; } = 10;

    public static DebateRuleSet Load(string configPath = @"Content\Data\DebateRules.json")
    {
        if (cached != null)
        {
            return cached;
        }

        string resolvedPath = ResolveConfigPath(configPath);
        if (!File.Exists(resolvedPath))
        {
            cached = new DebateRuleSet();
            return cached;
        }

        string json = File.ReadAllText(resolvedPath, Encoding.UTF8);
        cached = JsonSerializer.Deserialize(json, DebateRuleSetJsonContext.Default.DebateRuleSet)
            ?? throw new InvalidOperationException($"Debate rules deserialization returned null: {resolvedPath}");
        return cached;
    }

    public static void ClearCache()
    {
        cached = null;
    }

    private static string ResolveConfigPath(string configPath)
    {
        string runtimePath = Path.Combine(AppContext.BaseDirectory, configPath.Replace('\\', Path.DirectorySeparatorChar));
        if (File.Exists(runtimePath))
        {
            return runtimePath;
        }

        string workspacePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", configPath.Replace('\\', Path.DirectorySeparatorChar)));
        return workspacePath;
    }
}
