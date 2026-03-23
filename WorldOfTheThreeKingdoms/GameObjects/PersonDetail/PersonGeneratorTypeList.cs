using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;



namespace GameObjects.PersonDetail
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract]，添加 [JsonConverter]
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class PersonGeneratorTypeList : GameObjectList
    {
     
    }
}
