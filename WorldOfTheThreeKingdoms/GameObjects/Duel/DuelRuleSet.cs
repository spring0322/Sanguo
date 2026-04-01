using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

public sealed class DuelRuleSet
{
    private static DuelRuleSet cached;

    public int LogicTicksPerSecond { get; init; } = 60;

    public int CommandWindowTicks { get; init; } = 72;

    public int ResultDisplayTicks { get; init; } = 18;

    public float StartingLife { get; init; } = 100f;

    public int BaseDamageDivisor { get; init; } = 5;

    public float BaseDamageScale { get; init; } = 0.8f;

    public float MinimumDamage { get; init; } = 0.2f;

    public float AggressiveAttackModifier { get; init; } = 1.5f;

    public float DefensiveAttackModifier { get; init; } = 0.6f;

    public float AggressiveDefenseModifier { get; init; } = 1.3f;

    public float DefensiveDefenseModifier { get; init; } = 0.5f;

    public int CriticalChancePercent { get; init; } = 10;

    public float CriticalDamageMultiplier { get; init; } = 1.35f;

    public float DefensiveLifeRatioThreshold { get; init; } = 0.3f;

    public int DefensiveBiasPercent { get; init; } = 70;

    public int AggressiveBiasPercent { get; init; } = 35;

    public static DuelRuleSet Load(string configPath = @"Content\Data\DuelRules.json")
    {
        if (cached != null)
        {
            return cached;
        }

        string resolvedPath = ResolveConfigPath(configPath);
        if (!File.Exists(resolvedPath))
        {
            cached = new DuelRuleSet();
            return cached;
        }

        string json = File.ReadAllText(resolvedPath, Encoding.UTF8);
        cached = JsonSerializer.Deserialize(json, DuelRuleSetJsonContext.Default.DuelRuleSet)
            ?? throw new InvalidOperationException($"单挑规则文件反序列化返回 null: {resolvedPath}");
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
