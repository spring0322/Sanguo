using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldOfTheThreeKingdoms.GameObjects.Duel;

[JsonSerializable(typeof(DuelRuleSet))]
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNameCaseInsensitive = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
internal partial class DuelRuleSetJsonContext : JsonSerializerContext
{
}
