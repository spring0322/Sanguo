using System;
using System.Collections.Generic;
using System.Text.Json;
using Tools;

namespace WorldOfTheThreeKingdoms.Serialization
{
    public static class JsonMigrationHelper
    {
        public static T DeserializeObject<T>(string json)
        {
            return SimpleSerializerSystemTextJson.DeserializeJson<T>(json);
        }

        public static void SetPreferSystemTextJson(bool enable)
        {
            // Stub
        }

        public static string SerializeObject(object obj, bool indented = false)
        {
            return SimpleSerializerSystemTextJson.SerializeJson(obj, indented: indented);
        }

        public static string GetMigrationStats()
        {
            return SimpleSerializerSystemTextJson.GetSerializationStats();
        }
    }
}
