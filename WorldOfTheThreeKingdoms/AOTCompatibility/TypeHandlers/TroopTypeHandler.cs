using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// Troop类型处理器
    /// 提供AOT兼容的Troop对象创建和属性访问
    /// </summary>
    public class TroopTypeHandler : ITypeHandler
    {
        public Type HandledType => typeof(Troop);

        public GameObject CreateInstance()
        {
            try
            {
                var troop = new Troop();
                Debug.WriteLine($"[TroopTypeHandler] 创建Troop实例: ID={troop.ID}");
                return troop;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TroopTypeHandler] 创建Troop实例失败: {ex.Message}");
                throw;
            }
        }

        public void InitializeObject(GameObject obj)
        {
            if (obj is not Troop troop) return;

            try
            {
                // 初始化Troop特有属性
                if (string.IsNullOrEmpty(troop.Name))
                {
                    // troop.Name = $"部队{troop.ID}";
                }

                // 初始化基础属性
                if (troop.Quantity <= 0) troop.Quantity = 1000;
                if (troop.Morale <= 0) troop.Morale = 80;
                if (troop.Combativity <= 0) troop.Combativity = 80;
                // if (troop.Experience <= 0) troop.Experience = 50;
                if (troop.Food <= 0) troop.Food = 500;
                if (troop.Fund <= 0) troop.Fund = 200;
                // if (troop.Will <= 0) troop.Will = 80;

                // 设置默认状态
                // troop.AutoRun = false;
                // troop.Controllable = true;
                troop.Destroyed = false;

                Debug.WriteLine($"[TroopTypeHandler] 初始化Troop完成: {troop.Name}[{troop.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TroopTypeHandler] 初始化Troop失败: {ex.Message}");
            }
        }

        public List<AOTPropertyInfo> GetProperties()
        {
            return new List<AOTPropertyInfo>
            {
                new AOTPropertyInfo { Name = "ID", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Name", PropertyType = typeof(string), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Quantity", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Morale", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Combativity", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "Experience", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Food", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Fund", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "Will", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "AutoRun", PropertyType = typeof(bool), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "Controllable", PropertyType = typeof(bool), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Destroyed", PropertyType = typeof(bool), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "BelongedFaction", PropertyType = typeof(Faction), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "BelongedLegion", PropertyType = typeof(Legion), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Leader", PropertyType = typeof(Person), CanRead = true, CanWrite = true, IsPublic = true },
                
                // UI 扩展属性 (为部队列表等控件供反射使用)
                new AOTPropertyInfo { Name = "TroopStrength", PropertyType = typeof(int), CanRead = true, CanWrite = false, IsPublic = true },
                new AOTPropertyInfo { Name = "TroopCommand", PropertyType = typeof(int), CanRead = true, CanWrite = false, IsPublic = true },
                new AOTPropertyInfo { Name = "TroopIntelligence", PropertyType = typeof(int), CanRead = true, CanWrite = false, IsPublic = true },
                new AOTPropertyInfo { Name = "CombatTitleString", PropertyType = typeof(string), CanRead = true, CanWrite = false, IsPublic = true },
                new AOTPropertyInfo { Name = "FightingForce", PropertyType = typeof(int), CanRead = true, CanWrite = false, IsPublic = true },
                new AOTPropertyInfo { Name = "RationDaysString", PropertyType = typeof(string), CanRead = true, CanWrite = false, IsPublic = true }
            };
        }

        public bool SetProperty(GameObject obj, string propertyName, object value)
        {
            if (obj is not Troop troop) return false;

            try
            {
                switch (propertyName)
                {
                    case "ID":
                        if (value is int id) { troop.ID = id; return true; }
                        break;
                    // case "Name":
                    //    if (value is string name) { troop.Name = name; return true; }
                    //    break;
                    case "Quantity":
                        if (value is int quantity) { troop.Quantity = quantity; return true; }
                        break;
                    case "Morale":
                        if (value is int morale) { troop.Morale = morale; return true; }
                        break;
                    case "Combativity":
                        if (value is int combativity) { troop.Combativity = combativity; return true; }
                        break;
                    // case "Experience":
                    //    if (value is int experience) { troop.Experience = experience; return true; }
                    //    break;
                    case "Food":
                        if (value is int food) { troop.Food = food; return true; }
                        break;
                    case "Fund":
                        if (value is int fund) { troop.Fund = fund; return true; }
                        break;
                    // case "Will":
                    //    if (value is int will) { troop.Will = will; return true; }
                    //    break;
                    // case "AutoRun":
                    //    if (value is bool autoRun) { troop.AutoRun = autoRun; return true; }
                    //    break;
                    // case "Controllable":
                    //    if (value is bool controllable) { troop.Controllable = controllable; return true; }
                    //    break;
                    case "Destroyed":
                        if (value is bool destroyed) { troop.Destroyed = destroyed; return true; }
                        break;
                    case "BelongedFaction":
                        if (value is Faction faction) { troop.BelongedFaction = faction; return true; }
                        break;
                    case "BelongedLegion":
                        if (value is Legion legion) { troop.BelongedLegion = legion; return true; }
                        break;
                    case "Leader":
                        if (value is Person leader) { troop.Leader = leader; return true; }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TroopTypeHandler] 设置属性失败 {propertyName}: {ex.Message}");
            }

            return false;
        }

        public object GetProperty(GameObject obj, string propertyName)
        {
            if (obj is not Troop troop) return null;

            try
            {
                switch (propertyName)
                {
                    case "ID": return troop.ID;
                    case "Name": return troop.Name;
                    case "Quantity": return troop.Quantity;
                    case "Morale": return troop.Morale;
                    case "Combativity": return troop.Combativity;
                    // case "Experience": return troop.Experience;
                    case "Food": return troop.Food;
                    case "Fund": return troop.Fund;
                    // case "Will": return troop.Will;
                    // case "AutoRun": return troop.AutoRun;
                    // case "Controllable": return troop.Controllable;
                    case "Destroyed": return troop.Destroyed;
                    case "BelongedFaction": return troop.BelongedFaction;
                    case "BelongedLegion": return troop.BelongedLegion;
                    case "Leader": return troop.Leader;

                    // UI 扩展属性
                    case "TroopStrength": return troop.TroopStrength;
                    case "TroopCommand": return troop.TroopCommand;
                    case "TroopIntelligence": return troop.TroopIntelligence;
                    case "CombatTitleString": return troop.CombatTitleString;
                    case "FightingForce": return troop.FightingForce;
                    case "RationDaysString": return troop.RationDaysString;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TroopTypeHandler] 获取属性失败 {propertyName}: {ex.Message}");
            }

            return null;
        }
    }
}