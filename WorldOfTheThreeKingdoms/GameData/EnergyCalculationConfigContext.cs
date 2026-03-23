using System.Text.Json;
using System.Text.Json.Serialization;
using Zhsan.GameLogic.Config;

namespace WorldOfTheThreeKingdoms.GameData;

/// <summary>
/// 能量计算配置的 AOT 序列化上下文
/// 日期：2026-03-17
/// 用途：支持 Native AOT 编译
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(EnergyCalculationConfig))]
[JsonSerializable(typeof(TroopEnergyConfig))]
[JsonSerializable(typeof(ArchitectureEnergyConfig))]
[JsonSerializable(typeof(EnergyRange))]
[JsonSerializable(typeof(CombatWeights))]
[JsonSerializable(typeof(OfficerMeritSystem))]
[JsonSerializable(typeof(BaseStatsWeights))]
[JsonSerializable(typeof(GarrisonWeights))]
[JsonSerializable(typeof(ArchitectureOfficerMeritSystem))]
[JsonSerializable(typeof(PerformanceSettings))]
public partial class EnergyCalculationConfigContext : JsonSerializerContext
{
}
