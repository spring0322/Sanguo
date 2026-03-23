using System;
using System.Runtime.Serialization;

namespace GameObjects
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract]，添加 [JsonConverter]
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class FacilityList : GameObjectList
    {
        public void AddFacility(Facility facility)
        {
            base.GameObjects.Add(facility);
        }

        public void DecreaseEndurance(int decrement)
        {
            if (decrement > 0)
            {
                // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
                // 日期：2026-03-21
                // 原因：AOT 环境下 foreach (Facility in List<GameObject>) 隐式转换失败
                // 解决：使用 for 循环 + as 类型转换 + Fail Fast
                for (int i = 0; i < base.GameObjects.Count; i++)
                {
                    Facility facility = base.GameObjects[i] as Facility;
                    if (facility == null)
                    {
                        throw new InvalidOperationException($"FacilityList 中存在非 Facility 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                    }
                    
                    if (facility.location.CanRemoveFacility(facility))
                    {
                        facility.DecreaseEndurance(decrement);
                    }
                }
            }
        }

        public void RecoverEndurance(int extraInc)
        {
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (Facility in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                Facility facility = base.GameObjects[i] as Facility;
                if (facility == null)
                {
                    throw new InvalidOperationException($"FacilityList 中存在非 Facility 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                facility.RecoverEndurance(extraInc);
            }
        }
    }
}

