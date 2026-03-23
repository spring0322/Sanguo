using GameObjects.TroopDetail;

namespace GameObjects.Events
{
    /// <summary>
    /// 计略事件系统使用示例
    /// 🔥 此文件仅作为文档参考，不会被编译到游戏中
    /// 日期：2026-02-27
    /// </summary>
    public static class StratagemEventsExample
    {
        /// <summary>
        /// 示例 1：在游戏初始化时绑定计略事件
        /// 位置：GameScenario.InitializeStratagemEvents()
        /// </summary>
        public static void Example_InitializeEvents()
        {
            // 清空旧的事件订阅（避免重复绑定）
            StratagemEvents.ClearAllSubscriptions();
            
            // 绑定对目标施放计略的事件
            StratagemEvents.OnStratagemCastCompleted += HandleStratagemCast;
            
            // 绑定自我施放计略的事件
            StratagemEvents.OnSelfStratagemCastCompleted += HandleSelfStratagemCast;
        }
        
        /// <summary>
        /// 示例 2：处理对目标施放的计略
        /// </summary>
        private static void HandleStratagemCast(Troop caster, Troop target, Stratagem stratagem)
        {
            // 根据计略名称执行不同的逻辑
            switch (stratagem.Name)
            {
                case "火攻":
                    // 在目标位置生成火势
                    ApplyFireDamage(caster, target);
                    break;
                    
                case "混乱":
                    // 播放混乱特效
                    PlayChaosEffect(target);
                    break;
                    
                case "攻心":
                    // 记录战斗日志
                    LogStratagemUsage(caster, target, stratagem);
                    break;
                    
                default:
                    // 通用处理
                    System.Diagnostics.Debug.WriteLine(
                        $"[计略] {caster.DisplayName} 对 {target.DisplayName} 使用了 {stratagem.Name}");
                    break;
            }
        }
        
        /// <summary>
        /// 示例 3：处理自我施放的计略
        /// </summary>
        private static void HandleSelfStratagemCast(Troop caster, Stratagem stratagem)
        {
            switch (stratagem.Name)
            {
                case "鼓舞":
                    // 影响周围友军
                    BoostNearbyAllies(caster);
                    break;
                    
                case "疗伤":
                    // 恢复伤兵
                    HealInjuredTroops(caster);
                    break;
                    
                case "铁壁":
                    // 播放防御特效
                    PlayDefenseEffect(caster);
                    break;
                    
                default:
                    System.Diagnostics.Debug.WriteLine(
                        $"[计略] {caster.DisplayName} 使用了 {stratagem.Name}");
                    break;
            }
        }
        
        // ========================================
        // 辅助方法示例
        // ========================================
        
        private static void ApplyFireDamage(Troop caster, Troop target)
        {
            // 示例：根据地形生成火势
            // TerrainDetail terrain = Session.Current.Scenario.GetTerrainDetailByPosition(target.Position);
            // if (terrain?.FireDamageRate > 0)
            // {
            //     float fireScale = caster.GenerateFireDamageScale(1.0f, terrain);
            //     target.SetOnFire(fireScale);
            // }
        }
        
        private static void PlayChaosEffect(Troop target)
        {
            // 示例：播放混乱音效或特效
            // Session.MainGame?.PlaySound("chaos_effect.wav");
            // Session.MainGame?.ShowEffect("chaos_icon", target.Position);
        }
        
        private static void LogStratagemUsage(Troop caster, Troop target, Stratagem stratagem)
        {
            // 示例：记录到年表或战斗日志
            // Session.Current.Scenario.YearTable?.AddStratagemEntry(
            //     Session.Current.Scenario.Date, caster, target, stratagem);
        }
        
        private static void BoostNearbyAllies(Troop caster)
        {
            // 示例：影响周围 3 格内的友军
            // var nearbyTroops = Session.Current.Scenario.Troops
            //     .GetList()
            //     .Where(t => t.BelongedFaction == caster.BelongedFaction 
            //              && GetDistance(t.Position, caster.Position) <= 3
            //              && t != caster)
            //     .ToList();
            // 
            // foreach (var troop in nearbyTroops)
            // {
            //     troop.IncreaseMorale(5);
            // }
        }
        
        private static void HealInjuredTroops(Troop caster)
        {
            // 示例：恢复一半伤兵
            // int healAmount = caster.InjuryQuantity / 2;
            // if (healAmount > 0)
            // {
            //     caster.DecreaseInjuryQuantity(healAmount);
            //     caster.IncreaseQuantity(healAmount);
            // }
        }
        
        private static void PlayDefenseEffect(Troop caster)
        {
            // 示例：播放防御特效
            // Session.MainGame?.ShowEffect("defense_shield", caster.Position);
        }
    }
    
    /// <summary>
    /// 示例 4：Mod 或插件如何扩展计略系统
    /// </summary>
    public class MyCustomMod
    {
        public void OnModLoad()
        {
            // 在 Mod 初始化时订阅事件
            StratagemEvents.OnStratagemCastCompleted += OnCustomStratagemCast;
        }
        
        private void OnCustomStratagemCast(Troop caster, Troop target, Stratagem stratagem)
        {
            // 只处理自定义计略
            if (stratagem.Name == "我的自定义计略")
            {
                // 自定义逻辑
                System.Diagnostics.Debug.WriteLine(
                    $"[自定义Mod] {caster.DisplayName} 使用了自定义计略！");
                
                // 例如：给目标添加特殊状态
                // target.AddCustomStatus("MyModStatus", 10);
            }
        }
        
        public void OnModUnload()
        {
            // Mod 卸载时取消订阅
            StratagemEvents.OnStratagemCastCompleted -= OnCustomStratagemCast;
        }
    }
}
