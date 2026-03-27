// 🎖️ 军师系统 - 智能风险评估与建议系统
// 实现军师在时间跳过中的智能打断和建议功能

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameManager
{
    /// <summary>
    /// 风险等级枚举 - 升级版
    /// </summary>
    public enum RiskLevel
    {
        None,       // 无事发生
        Info,       // 普通建议 (记录日志，不打断)
        Critical    // 紧急军情 (强制打断时间)
    }
    
    /// <summary>
    /// 建议类型枚举
    /// </summary>
    public enum AdviceType
    {
        General,      // 一般建议
        Military,     // 军事建议
        Diplomatic,   // 外交建议
        Internal,     // 内政建议
        Recruitment,  // 招募建议
        Battle,       // 战斗建议
        Emergency     // 紧急建议
    }
    
    /// <summary>
    /// 升级版建议数据包
    /// </summary>
    public class AdviceData
    {
        public RiskLevel Level;             // 风险等级
        public string Title;                // 标题 (如：【军情急报】 或 【军师锦囊】)
        public string Content;              // 内容
        public string ButtonText;           // 按钮文字 (如：【前往处理】)
        public Action OnClick;              // 点击按钮后的跳转逻辑
        public AdviceType Type;             // 类型
        public object RelatedObject;        // 相关对象 (城市、武将等)
        public DateTime Timestamp;          // 时间戳
        
        public AdviceData()
        {
            Timestamp = DateTime.Now;
            ButtonText = "【确定】";
            Level = RiskLevel.None;
        }
    }
    
    /// <summary>
    /// 🎖️ 军师管理器 - 简化版智能建议系统
    /// </summary>
    public static class StrategistManager
    {
        // === A. 主动咨询 (花费金钱，玩家点击触发) ===
        public static AdviceData AskForAdvice(Faction faction)
        {
            if (faction == null || faction.Destroyed)
                return null;
            
            try
            {
                // 1. 检查钱和次数 (逻辑略) ...
                // faction.Gold -= 200;
                
                // 2. 获取军师
                var strategist = GetStrategist(faction);
                string strategistName = strategist?.Name ?? "军师";
                
                // 3. 生成日常建议内容
                string adviceContent = GenerateGeneralAdvice(faction, strategistName);
                
                // 4. 创建建议数据
                var advice = new AdviceData
                {
                    Level = RiskLevel.Info,
                    Title = "【军师锦囊】",
                    Content = $"军师【{strategistName}】：{adviceContent}",
                    ButtonText = "退下",
                    Type = AdviceType.General,
                    OnClick = null // 只是日常建议，不需要跳转
                };
                
                // 5. 记录到日志
                faction.AddAdviceToLog($"{advice.Title} 主动咨询 - {adviceContent}");
                
                return advice;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AskForAdvice 错误: {ex.Message}");
                return null;
            }
        }
        
        // === B. 被动扫描 (回合开始自动跑，免费) ===
        public static void CheckCriticalRisks(Faction faction)
        {
            if (faction == null || faction.Destroyed)
                return;
            
            try
            {
                // 只有这里检测到【极度危险】时，才触发事件
                // 避免玩家每回合都被弹窗烦死
                
                var strategist = GetStrategist(faction);
                string strategistName = strategist?.Name ?? "军师";
                
                // 1. 检查忠诚度危机
                foreach (Person officer in faction.Persons.GetList())
                {
                    if (officer.Loyalty < 40) // 极低忠诚
                    {
                        // 组装数据 - 使用配置化对话
                        string content = AdvisorDialogueManager.GetGenericDialogue(faction, strategist, "Emergency_Rebellion", new Dictionary<string, string> { { "{Person}", officer.Name } });
                        var data = new AdviceData
                        {
                            Level = RiskLevel.Critical,
                            Title = "【叛乱预警！】",
                            Content = $"军师【{strategistName}】：{content}",
                            ButtonText = "立即赏赐",
                            Type = AdviceType.Emergency,
                            OnClick = () => { /* 打开赏赐界面的代码 */ }
                        };
                        
                        // 重点：【触发事件】
                        // 就像发广播一样，UI 听到后会自动弹出来
                        EventManager.TriggerStrategistEvent(data);
                        return; // 每次只报一个最紧急的，避免弹窗重叠
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"军师系统检查异常: {ex.Message}");
            }
        }
        
        // === 辅助方法 ===
        
        /// <summary>
        /// 生成日常建议内容
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="strategistName">军师名字</param>
        /// <returns>建议内容</returns>
        private static string GenerateGeneralAdvice(Faction faction, string strategistName)
        {
            try
            {
                // 计算总体状况
                int totalFood = 0, totalFund = 0;
                foreach (Architecture arch in faction.Architectures.GetList())
                {
                    totalFood += arch.Food;
                    totalFund += arch.Fund;
                }
                
                // 根据情况生成不同建议 - 使用配置化对话
                var strategist = GetStrategist(faction);
                if (totalFund > 50000)
                {
                    return AdvisorDialogueManager.GetGenericDialogue(faction, strategist, "Advice_FundHigh", new Dictionary<string, string> { { "{Fund}", totalFund.ToString() } });
                }
                else if (totalFood > 20000)
                {
                    return AdvisorDialogueManager.GetGenericDialogue(faction, strategist, "Advice_FoodHigh", new Dictionary<string, string> { { "{Food}", totalFood.ToString() } });
                }
                else if (faction.Troops.Count < faction.Architectures.Count * 2)
                {
                    return AdvisorDialogueManager.GetGenericDialogue(faction, strategist, "Advice_LowTroops", null);
                }
                else if (faction.Persons.Count < 10)
                {
                    return AdvisorDialogueManager.GetGenericDialogue(faction, strategist, "Advice_LowPersons", null);
                }
                else
                {
                    return AdvisorDialogueManager.GetGenericDialogue(faction, strategist, "Advice_Stable", null);
                }
            }
            catch
            {
                return AdvisorDialogueManager.GetGenericDialogue(faction, null, "Advice_Loading", null);
            }
        }
        
        /// <summary>
        /// 获取势力的军师
        /// </summary>
        /// <param name="faction">势力</param>
        /// <returns>军师武将，如果没有则返回null</returns>
        private static Person GetStrategist(Faction faction)
        {
            try
            {
                // 查找智力最高的武将作为军师
                Person strategist = null;
                int maxIntelligence = 0;
                
                foreach (Person person in faction.Persons.GetList())
                {
                    if (person != null && person.Intelligence > maxIntelligence)
                    {
                        maxIntelligence = person.Intelligence;
                        strategist = person;
                    }
                }
                
                // 只有智力超过80的才能当军师
                return (strategist?.Intelligence >= 80) ? strategist : null;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 检查风险 (被动触发版 - 供TimeManager调用)
        /// </summary>
        /// <param name="faction">要检查的势力</param>
        /// <param name="currentDateString">当前日期字符串（用于日志）</param>
        /// <returns>建议数据（包含风险等级）</returns>
        public static AdviceData CheckRisks(Faction faction, string currentDateString = null)
        {
            if (faction == null || faction.Destroyed)
                return new AdviceData { Level = RiskLevel.None };
            
            try
            {
                // 获取当前日期字符串
                if (string.IsNullOrEmpty(currentDateString))
                {
                    currentDateString = GetCurrentDateString();
                }
                
                var strategist = GetStrategist(faction);
                string strategistName = strategist?.Name ?? "军师";
                
                // --- 1. 优先检查致命威胁 (Critical) ---
                
                // A. 忠诚度极低检查
                foreach (Person officer in faction.Persons.GetList())
                {
                    if (officer.Loyalty < 40)
                    {
                        string content = AdvisorDialogueManager.GetGenericDialogue(faction, strategist, "Emergency_Rebellion", new Dictionary<string, string> { { "{Person}", officer.Name } });
                        return new AdviceData
                        {
                            Level = RiskLevel.Critical,
                            Title = "【紧急兵变】",
                            Content = $"军师急报：{content}",
                            ButtonText = "立即赏赐",
                            Type = AdviceType.Emergency,
                            RelatedObject = officer,
                            OnClick = () => {
                                System.Diagnostics.Debug.WriteLine($"打开赏赐界面 - 武将: {officer.Name}");
                            }
                        };
                    }
                }
                
                // B. 首都被围检查 (威胁度 > 0.9)
                foreach (Architecture city in faction.Architectures.GetList())
                {
                    if (city == null) continue;
                    
                    float threatLevel = GetArchitectureThreatLevel(city);
                    int troopCount = GetArchitectureTroopCount(city);
                    
                    if (threatLevel > 0.9f && troopCount < 500) // 更严格的Critical条件
                    {
                        string content = AdvisorDialogueManager.GetGenericDialogue(faction, strategist, "Emergency_CityUnderAttack", new Dictionary<string, string> { { "{Location}", city.Name } });
                        return new AdviceData
                        {
                            Level = RiskLevel.Critical,
                            Title = "【军情急报】",
                            Content = $"军师急报：{content}",
                            ButtonText = "前往调度",
                            Type = AdviceType.Emergency,
                            RelatedObject = city,
                            OnClick = () => {
                                System.Diagnostics.Debug.WriteLine($"跳转到城市: {city.Name}");
                            }
                        };
                    }
                }
                
                // --- 2. 检查普通建议 (Info) ---
                // 如果没有致命威胁，看看有没有普通建议
                
                // 示例：钱多了，建议造东西
                if (faction.Fund > 50000 && faction.Architectures.Count > 0)
                {
                    // 注意：这里我们不返回 Critical，而是返回 Info
                    // 并且我们在这里直接把日志写进去！
                    string logMsg = $"{currentDateString}: 府库充盈，军师建议扩建设施。";
                    faction.AddAdviceToLog(logMsg);
                    
                    return new AdviceData 
                    { 
                        Level = RiskLevel.Info,
                        Title = "【军师锦囊】",
                        Content = $"军师【{strategistName}】：主公，当前府库充盈({faction.Fund} 金)，正是扩建设施、招贤纳士之时。",
                        ButtonText = "查看详情",
                        Type = AdviceType.General,
                        OnClick = () => {
                            System.Diagnostics.Debug.WriteLine("跳转到建设界面");
                        }
                    };
                }
                
                // 示例：粮食充足，建议扩军
                int totalFood = 0;
                foreach (Architecture arch in faction.Architectures.GetList())
                {
                    totalFood += arch.Food;
                }
                
                if (totalFood > 100000 && faction.Troops.Count < faction.Architectures.Count * 3)
                {
                    string logMsg = $"{currentDateString}: 粮草充足，军师建议扩充军备。";
                    faction.AddAdviceToLog(logMsg);
                    
                    return new AdviceData 
                    { 
                        Level = RiskLevel.Info,
                        Title = "【军师锦囊】",
                        Content = $"军师【{strategistName}】：主公，粮草充足({totalFood} 石)，可考虑扩充军备以备征战。",
                        ButtonText = "查看详情",
                        Type = AdviceType.General,
                        OnClick = () => {
                            System.Diagnostics.Debug.WriteLine("跳转到军事界面");
                        }
                    };
                }
                
                // --- 3. 没事 ---
                return new AdviceData { Level = RiskLevel.None };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查风险时出错: {ex.Message}");
                return new AdviceData { Level = RiskLevel.None };
            }
        }
        
        /// <summary>
        /// 获取当前日期字符串
        /// </summary>
        private static string GetCurrentDateString()
        {
            try
            {
                if (Session.Current?.Scenario?.Date != null)
                {
                    var date = Session.Current.Scenario.Date;
                    return $"{date.Year}年{date.Month}月{date.Day}日";
                }
                return "未知日期";
            }
            catch
            {
                return "日期获取失败";
            }
        }
        
        /// <summary>
        /// 获取建筑威胁等级
        /// </summary>
        private static float GetArchitectureThreatLevel(Architecture architecture)
        {
            try
            {
                // 使用影响力地图计算威胁等级
                if (architecture.BelongedFaction?.StrategicMap != null)
                {
                    float threat = architecture.BelongedFaction.StrategicMap.GetThreat(architecture.Position);
                    // 将威胁值转换为 0-1 的威胁等级
                    return Math.Max(0f, Math.Min(1f, threat / 100f));
                }
                
                // 备用方案：检查周围敌军
                int enemyCount = 0;
                foreach (Point point in architecture.ArchitectureArea.Area)
                {
                    // 简化版本：检查附近的敌军部队
                    foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
                    {
                        if (troop.BelongedFaction != architecture.BelongedFaction)
                        {
                            // 计算距离
                            int distance = Math.Abs(troop.Position.X - point.X) + Math.Abs(troop.Position.Y - point.Y);
                            if (distance <= 3)
                            {
                                enemyCount++;
                            }
                        }
                    }
                }
                
                return Math.Min(1f, enemyCount / 10f);
            }
            catch
            {
                return 0f;
            }
        }
        
        /// <summary>
        /// 获取建筑兵力数量
        /// </summary>
        private static int GetArchitectureTroopCount(Architecture architecture)
        {
            try
            {
                int totalTroops = 0;
                
                // 计算城内军队
                totalTroops += architecture.Militaries.Count * 100; // 假设每个军队平均100人
                
                // 计算附近的己方部队
                foreach (Troop troop in architecture.BelongedFaction.Troops.GetList())
                {
                    // 检查部队是否在该城市
                    int distance = Math.Abs(troop.Position.X - architecture.Position.X) + Math.Abs(troop.Position.Y - architecture.Position.Y);
                    if (distance <= 1) // 在城市附近
                    {
                        totalTroops += troop.Quantity;
                    }
                }
                
                return totalTroops;
            }
            catch
            {
                return 0;
            }
        }
    }
}
