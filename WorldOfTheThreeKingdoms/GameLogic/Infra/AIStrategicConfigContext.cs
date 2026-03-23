using System.Text.Json.Serialization;
using System.Text.Json;
using Zhsan.GameLogic.Config;

namespace Zhsan.GameLogic.Infra
{
    // 关键点：指定要生成的类型
    [JsonSerializable(typeof(AIStrategicConfig))]
    [JsonSerializable(typeof(GameBalanceConfig))]
    [JsonSerializable(typeof(TroopConfig))]
    [JsonSerializable(typeof(PersonConfig))]
    [JsonSourceGenerationOptions(
        WriteIndented = true, 
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, // 让JSON用小写开头，C#用大写
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true)] 
    public partial class AIStrategicConfigContext : JsonSerializerContext
    {
    }
}
