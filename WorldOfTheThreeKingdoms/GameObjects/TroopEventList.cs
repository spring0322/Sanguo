using GameManager;
using System;
using System.Runtime.Serialization;

namespace GameObjects
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract]，添加 [JsonConverter]
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class TroopEventList : GameObjectList
    {
        /// <summary>
        /// 重写Add方法，确保只能添加TroopEvent对象
        /// </summary>
        public override void Add(GameObject obj)
        {
            if (obj == null)
            {
                System.Diagnostics.Debug.WriteLine($"[TroopEventList.Add] 警告：尝试添加null对象");
                return;
            }
            
            if (!(obj is TroopEvent))
            {
                System.Diagnostics.Debug.WriteLine($"[TroopEventList.Add] 错误：只能添加TroopEvent对象，实际类型: {obj.GetType().Name}，ID: {obj.ID}");
                return;
            }
            
            base.Add(obj);
        }
        
        public void AddTroopEventWithEvent(TroopEvent te, bool add = true)
        {
            if (te == null)
            {
                System.Diagnostics.Debug.WriteLine($"[TroopEventList.AddTroopEventWithEvent] 警告：尝试添加null对象");
                return;
            }
            
            if (add)
            {
                base.Add(te);
            }
            

            
            te.OnApplyTroopEvent += te_OnApplyTroopEvent;
        }
        
        /// <summary>
        /// 清理列表中的非TroopEvent对象
        /// 应该在场景加载后调用一次，而不是在每次迭代时调用
        /// </summary>
        public int CleanupInvalidObjects()
        {
            var list = this.GetList();
            int removedCount = 0;
            
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!(list[i] is TroopEvent))
                {
                    // System.Diagnostics.Debug.WriteLine($"[TroopEventList.CleanupInvalidObjects] 移除非TroopEvent对象: {list[i]?.GetType().Name ?? "null"} at index {i}");
                    list.RemoveAt(i);
                    removedCount++;
                }
            }
            
            if (removedCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[TroopEventList.CleanupInvalidObjects] 总共移除了 {removedCount} 个无效对象");
            }
            
            return removedCount;
        }

        private void te_OnApplyTroopEvent(TroopEvent te, Troop troop)
        {
            Session.MainGame.mainGameScreen.TroopApplyTroopEvent(te, troop);
        }
    }
}

