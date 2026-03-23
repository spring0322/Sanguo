using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using GameObjects.FactionDetail;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// AI遗留适配器 - 提供安全的AI操作执行机制
    /// 避免AI操作与游戏主线程冲突
    /// </summary>
    public static class AILegacyAdapter
    {
        private static Queue<AICommand> commandQueue = new Queue<AICommand>();
        private static readonly int MAX_QUEUE_SIZE = 100;
        private static readonly int MAX_BATCH_SIZE = 10;

        /// <summary>
        /// 将AI命令加入队列
        /// </summary>
        public static void Enqueue(AICommand command)
        {
            if (commandQueue.Count >= MAX_QUEUE_SIZE)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] 队列已满，清理旧命令");
                commandQueue.Clear();
            }

            commandQueue.Enqueue(command);
            // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 命令入队: {command}");
        }

        /// <summary>
        /// 批量处理AI命令队列
        /// </summary>
        public static void ProcessCommands()
        {
            int processed = 0;
            
            while (commandQueue.Count > 0 && processed < MAX_BATCH_SIZE)
            {
                var command = commandQueue.Dequeue();
                
                try
                {
                    Execute(command);
                    processed++;
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 命令执行失败: {command} - {ex.Message}");
                }
            }

            if (processed > 0)
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 本次处理了 {processed} 个命令，队列剩余: {commandQueue.Count}");
            }
        }

        /// <summary>
        /// 执行单个AI命令
        /// </summary>
        private static void Execute(AICommand command)
        {
            switch (command.Type)
            {
                case AICommandType.CreateTroop:
                    ExecuteCreateTroop(command);
                    break;
                    
                case AICommandType.RecallPerson:
                    ExecuteRecallPerson(command);
                    break;
                    
                case AICommandType.TroopAction:
                    ExecuteTroopAction(command);
                    break;
                    
                case AICommandType.SpendResource:
                    ExecuteSpendResource(command);
                    break;
                    
                case AICommandType.DiplomaticAction:
                    ExecuteDiplomaticAction(command);
                    break;
                    
                default:
                    // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 未知命令类型: {command.Type}");
                    break;
            }
        }

        /// <summary>
        /// 安全执行组建部队命令
        /// </summary>
        private static void ExecuteCreateTroop(AICommand command)
        {
            // 增强参数验证
            if (command.Parameter == null)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] CreateTroop参数为null");
                return;
            }

            if (!(command.Parameter is Tuple<Architecture, Person, MilitaryKind, int> param))
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] CreateTroop参数格式错误，期望Tuple<Architecture, Person, MilitaryKind, int>，实际类型: {command.Parameter.GetType().Name}");
                return;
            }

            var (city, leader, kind, quantity) = param;

            // 状态验证
            if (!SafeCreateTroopCheck(city, leader, kind, quantity))
            {
                return;
            }

            try
            {
                // 获取对应的Military对象
                Military military = null;
                foreach (Military m in city.Militaries)
                {
                    if (m.Kind == kind)
                    {
                        military = m;
                        break;
                    }
                }
                
                if (military == null) 
                {
                    // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 找不到对应的Military对象: {kind.Name}");
                    return;
                }

                // 创建人员列表
                GameObjectList persons = new GameObjectList();
                persons.Add(leader);

                // 获取合适的位置
                Point position = city.Position;
                var availableArea = city.GetAllAvailableArea(false);
                if (availableArea.Area.Count > 0)
                {
                    position = availableArea.Area[0];
                }

                // 执行组建部队
                var troop = city.CreateTroop(persons, leader, military, 0, position);
                if (troop != null)
                {
                    troop.Army.Quantity = quantity;
                    // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 成功组建部队: {leader.Name} - {quantity}兵");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 组建部队失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全执行召回人员命令
        /// </summary>
        private static void ExecuteRecallPerson(AICommand command)
        {
            // 增强参数验证
            if (command.Parameter == null)
            {
                System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] RecallPerson参数为null");
                return;
            }

            if (!(command.Parameter is Person person))
            {
                System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] RecallPerson参数格式错误，期望Person，实际类型: {command.Parameter.GetType().Name}");
                return;
            }

            // 状态验证
            if (!SafeRecallPersonCheck(person))
            {
                return;
            }

            try
            {
                // 执行召回
                if (person.LocationArchitecture != null)
                {
                    person.WorkKind = ArchitectureWorkKind.无;
                    // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 成功召回人员: {person.Name}");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 召回人员失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全执行部队行动命令
        /// </summary>
        private static void ExecuteTroopAction(AICommand command)
        {
            // 增强参数验证
            if (command.Parameter == null)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] TroopAction参数为null");
                return;
            }

            if (!(command.Parameter is Tuple<Troop, string, object> param))
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] TroopAction参数格式错误，期望Tuple<Troop, string, object>，实际类型: {command.Parameter.GetType().Name}");
                return;
            }

            var (troop, action, target) = param;

            // 状态验证
            if (!SafeTroopActionCheck(troop, action))
            {
                return;
            }

            try
            {
                // 根据行动类型执行
                switch (action.ToLower())
                {
                    case "move":
                        if (target is Point destination)
                        {
                            // 执行移动逻辑
                            // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 部队移动: {troop.Name} -> ({destination.X}, {destination.Y})");
                        }
                        break;
                        
                    case "attack":
                        if (target is Troop enemyTroop)
                        {
                            // 执行攻击逻辑
                            // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 部队攻击: {troop.Name} -> {enemyTroop.Name}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 部队行动失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全执行资源消费命令
        /// </summary>
        private static void ExecuteSpendResource(AICommand command)
        {
            // 增强参数验证
            if (command.Parameter == null)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] SpendResource参数为null");
                return;
            }

            if (!(command.Parameter is Tuple<Architecture, string, int> param))
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] SpendResource参数格式错误，期望Tuple<Architecture, string, int>，实际类型: {command.Parameter.GetType().Name}");
                return;
            }

            var (city, resourceType, amount) = param;

            // 状态验证
            if (!SafeSpendResourceCheck(city, resourceType, amount))
            {
                return;
            }

            try
            {
                // 执行资源消费
                switch (resourceType.ToLower())
                {
                    case "fund":
                        city.DecreaseFund(amount);
                        // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 消费资金: {city.Name} - {amount}");
                        break;
                        
                    case "food":
                        city.DecreaseFood(amount);
                        // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 消费粮草: {city.Name} - {amount}");
                        break;
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 资源消费失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全执行外交行动命令
        /// </summary>
        private static void ExecuteDiplomaticAction(AICommand command)
        {
            // 增强参数验证
            if (command.Parameter == null)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] DiplomaticAction参数为null");
                return;
            }

            if (!(command.Parameter is Tuple<Architecture, Faction, string, Person> param))
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] DiplomaticAction参数格式错误，期望Tuple<Architecture, Faction, string, Person>，实际类型: {command.Parameter.GetType().Name}");
                return;
            }

            var (sourceCity, targetFaction, actionType, diplomat) = param;

            // 状态验证
            if (!SafeDiplomaticActionCheck(sourceCity, targetFaction, actionType, diplomat))
            {
                return;
            }

            try
            {
                // 执行外交行动
                switch (actionType)
                {
                    case "停战":
                        // 执行停战外交
                        // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 执行停战外交: {sourceCity.BelongedFaction.Name} -> {targetFaction.Name}");
                        // 这里可以调用实际的外交系统
                        break;
                        
                    case "亲善":
                        // 执行亲善外交
                        // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 执行亲善外交: {sourceCity.BelongedFaction.Name} -> {targetFaction.Name}");
                        break;
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[AILegacyAdapter] 外交行动失败: {ex.Message}");
            }
        }

        #region 安全检查方法

        private static bool SafeCreateTroopCheck(Architecture city, Person leader, MilitaryKind kind, int quantity)
        {
            if (city == null || leader == null || kind == null)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] CreateTroop: 参数为空");
                return false;
            }

            if (leader.LocationArchitecture != city)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] CreateTroop: 武将不在指定城市");
                return false;
            }

            if (city.Fund < kind.CreateCost * quantity / 100)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] CreateTroop: 资金不足");
                return false;
            }

            if (city.Population < quantity)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] CreateTroop: 人口不足");
                return false;
            }

            return true;
        }

        private static bool SafeRecallPersonCheck(Person person)
        {
            if (person == null)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] RecallPerson: 人员为空");
                return false;
            }

            if (person.LocationArchitecture == null)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] RecallPerson: 人员不在城市中");
                return false;
            }

            return true;
        }

        private static bool SafeTroopActionCheck(Troop troop, string action)
        {
            if (troop == null || string.IsNullOrEmpty(action))
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] TroopAction: 参数为空");
                return false;
            }

            if (troop.Army.Quantity <= 0)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] TroopAction: 部队无兵力");
                return false;
            }

            return true;
        }

        private static bool SafeSpendResourceCheck(Architecture city, string resourceType, int amount)
        {
            if (city == null || string.IsNullOrEmpty(resourceType) || amount <= 0)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] SpendResource: 参数无效");
                return false;
            }

            switch (resourceType.ToLower())
            {
                case "fund":
                    if (city.Fund < amount)
                    {
                        // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] SpendResource: 资金不足");
                        return false;
                    }
                    break;
                    
                case "food":
                    if (city.Food < amount)
                    {
                        // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] SpendResource: 粮草不足");
                        return false;
                    }
                    break;
                    
                default:
                    // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] SpendResource: 未知资源类型");
                    return false;
            }

            return true;
        }

        private static bool SafeDiplomaticActionCheck(Architecture sourceCity, Faction targetFaction, string actionType, Person diplomat)
        {
            if (sourceCity == null || targetFaction == null || string.IsNullOrEmpty(actionType) || diplomat == null)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] DiplomaticAction: 参数为空");
                return false;
            }

            if (diplomat.LocationArchitecture != sourceCity)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] DiplomaticAction: 外交官不在源城市");
                return false;
            }

            if (sourceCity.BelongedFaction == targetFaction)
            {
                // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] DiplomaticAction: 不能对自己势力进行外交");
                return false;
            }

            return true;
        }

        #endregion

        /// <summary>
        /// 获取当前队列长度
        /// </summary>
        public static int GetQueueCount()
        {
            return commandQueue.Count;
        }

        /// <summary>
        /// 清空命令队列
        /// </summary>
        public static void ClearQueue()
        {
            commandQueue.Clear();
            // System.Diagnostics.Debug.WriteLine("[AILegacyAdapter] 命令队列已清空");
        }
    }
}