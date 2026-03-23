

using GameManager;
using System.Runtime.Serialization;

namespace GameObjects
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract]，添加 [JsonConverter]
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class EventList : GameObjectList
    {
        public void AddEventWithEvent(Event te, bool add = true)
        {
            if (add)
            {
                base.Add(te);
            }
            

            
            te.OnApplyEvent += te_OnApplyEvent;
        }

        private void te_OnApplyEvent(Event te, Architecture a, Screen screen)
        {
            screen.ApplyEvent(te, a, screen);
        }
    }
}
