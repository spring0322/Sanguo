using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// Architecture类型处理器
    /// 提供AOT兼容的Architecture对象创建和属性访问
    /// </summary>
    public class ArchitectureTypeHandler : ITypeHandler
    {
        public Type HandledType => typeof(Architecture);

        public GameObject CreateInstance()
        {
            try
            {
                var architecture = new Architecture();
                Debug.WriteLine($"[ArchitectureTypeHandler] 创建Architecture实例: ID={architecture.ID}");
                return architecture;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ArchitectureTypeHandler] 创建Architecture实例失败: {ex.Message}");
                throw;
            }
        }

        public void InitializeObject(GameObject obj)
        {
            if (obj is not Architecture architecture) return;

            try
            {
                // 初始化Architecture特有属性
                if (string.IsNullOrEmpty(architecture.Name))
                {
                    architecture.Name = $"城市{architecture.ID}";
                }

                // 初始化基础资源
                if (architecture.Population <= 0) architecture.Population = 10000;
                if (architecture.Fund <= 0) architecture.Fund = 5000;
                if (architecture.Food <= 0) architecture.Food = 3000;
                if (architecture.Morale <= 0) architecture.Morale = 80;
                if (architecture.Endurance <= 0) architecture.Endurance = 1000;
                // if (architecture.Defense <= 0) architecture.Defense = 500;

                // 初始化发展度
                if (architecture.Agriculture <= 0) architecture.Agriculture = 50;
                if (architecture.Commerce <= 0) architecture.Commerce = 50;
                if (architecture.Technology <= 0) architecture.Technology = 50;
                // if (architecture.Recruit <= 0) architecture.Recruit = 50;

                Debug.WriteLine($"[ArchitectureTypeHandler] 初始化Architecture完成: {architecture.Name}[{architecture.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ArchitectureTypeHandler] 初始化Architecture失败: {ex.Message}");
            }
        }

        public List<AOTPropertyInfo> GetProperties()
        {
            return new List<AOTPropertyInfo>
            {
                new AOTPropertyInfo { Name = "ID", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Name", PropertyType = typeof(string), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "AreaX", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "AreaY", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Population", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Fund", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Food", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Morale", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Endurance", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "Defense", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Agriculture", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Commerce", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Technology", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "Recruit", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "BelongedFaction", PropertyType = typeof(Faction), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Mayor", PropertyType = typeof(Person), CanRead = true, CanWrite = true, IsPublic = true }
            };
        }

        public bool SetProperty(GameObject obj, string propertyName, object value)
        {
            if (obj is not Architecture architecture) return false;

            try
            {
                switch (propertyName)
                {
                    case "ID":
                        if (value is int id) { architecture.ID = id; return true; }
                        break;
                    case "Name":
                        if (value is string name) { architecture.Name = name; return true; }
                        break;
                    // case "AreaX":
                    //    if (value is int areaX) { architecture.AreaX = areaX; return true; }
                    //    break;
                    // case "AreaY":
                    //    if (value is int areaY) { architecture.AreaY = areaY; return true; }
                    //    break;
                    case "Population":
                        if (value is int population) { architecture.Population = population; return true; }
                        break;
                    case "Fund":
                        if (value is int fund) { architecture.Fund = fund; return true; }
                        break;
                    case "Food":
                        if (value is int food) { architecture.Food = food; return true; }
                        break;
                    case "Morale":
                        if (value is int morale) { architecture.Morale = morale; return true; }
                        break;
                    case "Endurance":
                        if (value is int endurance) { architecture.Endurance = endurance; return true; }
                        break;
                    // case "Defense":
                    //    if (value is int defense) { architecture.Defense = defense; return true; }
                    //    break;
                    case "Agriculture":
                        if (value is int agriculture) { architecture.Agriculture = agriculture; return true; }
                        break;
                    case "Commerce":
                        if (value is int commerce) { architecture.Commerce = commerce; return true; }
                        break;
                    case "Technology":
                        if (value is int technology) { architecture.Technology = technology; return true; }
                        break;
                    // case "Recruit":
                    //    if (value is int recruit) { architecture.Recruit = recruit; return true; }
                    //    break;
                    case "BelongedFaction":
                        if (value is Faction faction) { architecture.BelongedFaction = faction; return true; }
                        break;
                    case "Mayor":
                        if (value is Person mayor) { architecture.Mayor = mayor; return true; }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ArchitectureTypeHandler] 设置属性失败 {propertyName}: {ex.Message}");
            }

            return false;
        }

        public object GetProperty(GameObject obj, string propertyName)
        {
            if (obj is not Architecture architecture) return null;

            try
            {
                switch (propertyName)
                {
                    case "ID": return architecture.ID;
                    case "Name": return architecture.Name;
                    // case "AreaX": return architecture.AreaX;
                    // case "AreaY": return architecture.AreaY;
                    case "Population": return architecture.Population;
                    case "Fund": return architecture.Fund;
                    case "Food": return architecture.Food;
                    case "Morale": return architecture.Morale;
                    case "Endurance": return architecture.Endurance;
                    // case "Defense": return architecture.Defense;
                    case "Agriculture": return architecture.Agriculture;
                    case "Commerce": return architecture.Commerce;
                    case "Technology": return architecture.Technology;
                    // case "Recruit": return architecture.Recruit;
                    case "BelongedFaction": return architecture.BelongedFaction;
                    case "Mayor": return architecture.Mayor;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ArchitectureTypeHandler] 获取属性失败 {propertyName}: {ex.Message}");
            }

            return null;
        }
    }
}