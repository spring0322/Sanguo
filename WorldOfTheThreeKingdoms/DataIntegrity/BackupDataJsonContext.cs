using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace WorldOfTheThreeKingdoms.DataIntegrity
{
    [JsonSourceGenerationOptions(
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false)]
    [JsonSerializable(typeof(BackupObjectData))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<string, int>))]
    [JsonSerializable(typeof(Dictionary<string, bool>))]
    [JsonSerializable(typeof(Dictionary<string, float>))]
    [JsonSerializable(typeof(Dictionary<string, double>))]
    [JsonSerializable(typeof(Dictionary<string, long>))]
    [JsonSerializable(typeof(Point))]
    public partial class BackupDataJsonContext : JsonSerializerContext
    {
    }
}
