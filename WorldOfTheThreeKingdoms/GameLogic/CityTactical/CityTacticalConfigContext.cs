using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.GameLogic.CityTactical;

/// <summary>
/// AOT 源生成器上下文 - 城市战术配置
/// 日期：2026-03-11
/// </summary>
[JsonSerializable(typeof(CityTacticalConfig))]
[JsonSerializable(typeof(ResourceThresholds))]
[JsonSerializable(typeof(ActionCosts))]
[JsonSerializable(typeof(UtilityWeights))]
[JsonSerializable(typeof(BaseScores))]
[JsonSerializable(typeof(CampaignRequirements))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
public partial class CityTacticalConfigContext : JsonSerializerContext
{
}
