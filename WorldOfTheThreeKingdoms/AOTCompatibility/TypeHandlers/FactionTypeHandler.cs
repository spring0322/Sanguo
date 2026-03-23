using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// Faction类型处理器
    /// 提供AOT兼容的Faction对象创建和属性访问
    /// </summary>
    public class FactionTypeHandler : ITypeHandler
    {
        public Type HandledType => typeof(Faction);

        public GameObject CreateInstance()
        {
            try
            {
                var faction = new Faction();
                Debug.WriteLine($"[FactionTypeHandler] 创建Faction实例: ID={faction.ID}");
                return faction;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FactionTypeHandler] 创建Faction实例失败: {ex.Message}");
                throw;
            }
        }

        public void InitializeObject(GameObject obj)
        {
            if (obj is not Faction faction) return;

            try
            {
                // 初始化Faction特有属性
                if (string.IsNullOrEmpty(faction.Name))
                {
                    faction.Name = $"势力{faction.ID}";
                }

                if (faction.ColorIndex <= 0)
                {
                    faction.ColorIndex = 1;
                }

                // 初始化基础资源
                // if (faction.BaseMoney <= 0)
                // {
                //     faction.BaseMoney = 1000;
                // }

                // if (faction.BaseFood <= 0)
                // {
                //     faction.BaseFood = 500;
                // }

                if (faction.TechniquePoint <= 0)
                {
                    faction.TechniquePoint = 100;
                }

                Debug.WriteLine($"[FactionTypeHandler] 初始化Faction完成: {faction.Name}[{faction.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FactionTypeHandler] 初始化Faction失败: {ex.Message}");
            }
        }

        public List<AOTPropertyInfo> GetProperties()
        {
            return new List<AOTPropertyInfo>
            {
                new AOTPropertyInfo { Name = "ID", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Name", PropertyType = typeof(string), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "ColorIndex", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "LeaderID", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Leader", PropertyType = typeof(Person), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Passed", PropertyType = typeof(bool), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "BaseMoney", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "BaseFood", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "TechniquePoint", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Reputation", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true }
            };
        }

        public bool SetProperty(GameObject obj, string propertyName, object value)
        {
            if (obj is not Faction faction) return false;

            try
            {
                switch (propertyName)
                {
                    case "ID":
                        if (value is int id) { faction.ID = id; return true; }
                        break;
                    case "Name":
                        if (value is string name) { faction.Name = name; return true; }
                        break;
                    case "ColorIndex":
                        if (value is int colorIndex) { faction.ColorIndex = colorIndex; return true; }
                        break;
                    // case "LeaderID":
                    //    if (value is int leaderID) { faction.LeaderID = leaderID; return true; }
                    //    break;
                    case "Leader":
                        if (value is Person leader) { faction.Leader = leader; return true; }
                        break;
                    case "Passed":
                        if (value is bool passed) { faction.Passed = passed; return true; }
                        break;
                    // case "BaseMoney":
                    //    if (value is int baseMoney) { faction.BaseMoney = baseMoney; return true; }
                    //    break;
                    // case "BaseFood":
                    //    if (value is int baseFood) { faction.BaseFood = baseFood; return true; }
                    //    break;
                    case "TechniquePoint":
                        if (value is int techniquePoint) { faction.TechniquePoint = techniquePoint; return true; }
                        break;
                    case "Reputation":
                        if (value is int reputation) { faction.Reputation = reputation; return true; }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FactionTypeHandler] 设置属性失败 {propertyName}: {ex.Message}");
            }

            return false;
        }

        public object GetProperty(GameObject obj, string propertyName)
        {
            if (obj is not Faction faction) return null;

            try
            {
                switch (propertyName)
                {
                    case "ID": return faction.ID;
                    case "Name": return faction.Name;
                    case "ColorIndex": return faction.ColorIndex;
                    case "LeaderID": return faction.LeaderID;
                    case "Leader": return faction.Leader;
                    case "Passed": return faction.Passed;
                    // case "BaseMoney": return faction.BaseMoney;
                    // case "BaseFood": return faction.BaseFood;
                    case "TechniquePoint": return faction.TechniquePoint;
                    case "Reputation": return faction.Reputation;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FactionTypeHandler] 获取属性失败 {propertyName}: {ex.Message}");
            }

            return null;
        }
    }
}