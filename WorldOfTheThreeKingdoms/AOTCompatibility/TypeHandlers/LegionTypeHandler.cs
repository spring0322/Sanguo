using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// Legion类型处理器
    /// 提供AOT兼容的Legion对象创建和属性访问
    /// </summary>
    public class LegionTypeHandler : ITypeHandler
    {
        public Type HandledType => typeof(Legion);

        public GameObject CreateInstance()
        {
            try
            {
                var legion = new Legion();
                Debug.WriteLine($"[LegionTypeHandler] 创建Legion实例: ID={legion.ID}");
                return legion;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LegionTypeHandler] 创建Legion实例失败: {ex.Message}");
                throw;
            }
        }

        public void InitializeObject(GameObject obj)
        {
            if (obj is not Legion legion) return;

            try
            {
                // 初始化Legion特有属性
                if (string.IsNullOrEmpty(legion.Name))
                {
                    legion.Name = $"军团{legion.ID}";
                }

                // 🔥 重构：使用新的Kind+Mission系统
                // 日期：2026-03-09
                legion.Kind = LegionKind.AI;
                legion.Mission = LegionMission.Attack;

                Debug.WriteLine($"[LegionTypeHandler] 初始化Legion完成: {legion.Name}[{legion.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LegionTypeHandler] 初始化Legion失败: {ex.Message}");
            }
        }

        public List<AOTPropertyInfo> GetProperties()
        {
            return new List<AOTPropertyInfo>
            {
                new AOTPropertyInfo { Name = "ID", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Name", PropertyType = typeof(string), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Kind", PropertyType = typeof(LegionKind), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "Status", PropertyType = typeof(LegionStatus), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "AutoRun", PropertyType = typeof(bool), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "Controllable", PropertyType = typeof(bool), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "Will", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "BelongedFaction", PropertyType = typeof(Faction), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Leader", PropertyType = typeof(Person), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "StartArchitecture", PropertyType = typeof(Architecture), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "TargetArchitecture", PropertyType = typeof(Architecture), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "CoreTroop", PropertyType = typeof(Troop), CanRead = true, CanWrite = true, IsPublic = true }
            };
        }

        public bool SetProperty(GameObject obj, string propertyName, object value)
        {
            if (obj is not Legion legion) return false;

            try
            {
                switch (propertyName)
                {
                    case "ID":
                        if (value is int id) { legion.ID = id; return true; }
                        break;
                    case "Name":
                        if (value is string name) { legion.Name = name; return true; }
                        break;
                    case "Kind":
                        if (value is LegionKind kind) { legion.Kind = kind; return true; }
                        break;
                    // case "Status":
                    //    if (value is LegionStatus status) { legion.Status = status; return true; }
                    //    break;
                    // case "AutoRun":
                    //    if (value is bool autoRun) { legion.AutoRun = autoRun; return true; }
                    //    break;
                    // case "Controllable":
                    //    if (value is bool controllable) { legion.Controllable = controllable; return true; }
                    //    break;
                    // case "Will":
                    //    if (value is int will) { legion.Will = will; return true; }
                    //    break;
                    case "BelongedFaction":
                        if (value is Faction faction) { legion.BelongedFaction = faction; return true; }
                        break;
                    case "Leader":
                        // if (value is Person leader) { legion.Leader = leader; return true; }
                        break;
                    case "StartArchitecture":
                        if (value is Architecture startArch) { legion.StartArchitecture = startArch; return true; }
                        break;
                    // case "TargetArchitecture":
                    //    if (value is Architecture targetArch) { legion.TargetArchitecture = targetArch; return true; }
                    //    break;
                    case "CoreTroop":
                        if (value is Troop coreTroop) { legion.CoreTroop = coreTroop; return true; }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LegionTypeHandler] 设置属性失败 {propertyName}: {ex.Message}");
            }

            return false;
        }

        public object GetProperty(GameObject obj, string propertyName)
        {
            if (obj is not Legion legion) return null;

            try
            {
                switch (propertyName)
                {
                    case "ID": return legion.ID;
                    case "Name": return legion.Name;
                    case "Kind": return legion.Kind;
                    // case "Status": return legion.Status;
                    // case "AutoRun": return legion.AutoRun;
                    // case "Controllable": return legion.Controllable;
                    // case "Will": return legion.Will;
                    case "BelongedFaction": return legion.BelongedFaction;
                    // case "Leader": return legion.Leader;
                    case "StartArchitecture": return legion.StartArchitecture;
                    // case "TargetArchitecture": return legion.TargetArchitecture;
                    case "CoreTroop": return legion.CoreTroop;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LegionTypeHandler] 获取属性失败 {propertyName}: {ex.Message}");
            }

            return null;
        }
    }
}