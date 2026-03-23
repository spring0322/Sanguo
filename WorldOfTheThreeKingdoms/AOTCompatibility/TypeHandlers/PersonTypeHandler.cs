using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// Person类型处理器
    /// 提供AOT兼容的Person对象创建和属性访问
    /// </summary>
    public class PersonTypeHandler : ITypeHandler
    {
        public Type HandledType => typeof(Person);

        public GameObject CreateInstance()
        {
            try
            {
                var person = new Person();
                Debug.WriteLine($"[PersonTypeHandler] 创建Person实例: ID={person.ID}");
                return person;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PersonTypeHandler] 创建Person实例失败: {ex.Message}");
                throw;
            }
        }

        public void InitializeObject(GameObject obj)
        {
            if (obj is not Person person) return;

            try
            {
                // 初始化Person特有属性
                if (string.IsNullOrEmpty(person.Name))
                {
                    // person.Name = $"人物{person.ID}";
                }

                if (string.IsNullOrEmpty(person.SurName))
                {
                    person.SurName = "未知";
                }

                if (string.IsNullOrEmpty(person.GivenName))
                {
                    person.GivenName = "人物";
                }

                // 设置默认状态
                person.Alive = true;
                person.Available = true;

                // 初始化基础属性（如果为0）
                if (person.Command <= 0) person.Command = 50;
                if (person.Strength <= 0) person.Strength = 50;
                if (person.Intelligence <= 0) person.Intelligence = 50;
                if (person.Politics <= 0) person.Politics = 50;
                if (person.Glamour <= 0) person.Glamour = 50;
                // if (person.Loyalty <= 0) person.Loyalty = 80;
                if (person.Ambition <= 0) person.Ambition = 50;

                Debug.WriteLine($"[PersonTypeHandler] 初始化Person完成: {person.Name}[{person.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PersonTypeHandler] 初始化Person失败: {ex.Message}");
            }
        }

        public List<AOTPropertyInfo> GetProperties()
        {
            return new List<AOTPropertyInfo>
            {
                new AOTPropertyInfo { Name = "ID", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Name", PropertyType = typeof(string), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "SurName", PropertyType = typeof(string), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "GivenName", PropertyType = typeof(string), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "CalledName", PropertyType = typeof(string), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Sex", PropertyType = typeof(bool), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Alive", PropertyType = typeof(bool), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Available", PropertyType = typeof(bool), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Command", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Strength", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Intelligence", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Politics", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Glamour", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "Loyalty", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "Ambition", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                // new AOTPropertyInfo { Name = "BelongedFactionID", PropertyType = typeof(int), CanRead = true, CanWrite = true, IsPublic = true },
                new AOTPropertyInfo { Name = "BelongedFaction", PropertyType = typeof(Faction), CanRead = true, CanWrite = true, IsPublic = true }
            };
        }

        public bool SetProperty(GameObject obj, string propertyName, object value)
        {
            if (obj is not Person person) return false;

            try
            {
                switch (propertyName)
                {
                    case "ID":
                        if (value is int id) { person.ID = id; return true; }
                        break;
                    case "Name":
                        // if (value is string name) { person.Name = name; return true; }
                        break;
                    case "SurName":
                        if (value is string surName) { person.SurName = surName; return true; }
                        break;
                    case "GivenName":
                        if (value is string givenName) { person.GivenName = givenName; return true; }
                        break;
                    case "CalledName":
                        if (value is string calledName) { person.CalledName = calledName; return true; }
                        break;
                    case "Sex":
                        if (value is bool sex) { person.Sex = sex; return true; }
                        break;
                    case "Alive":
                        if (value is bool alive) { person.Alive = alive; return true; }
                        break;
                    case "Available":
                        if (value is bool available) { person.Available = available; return true; }
                        break;
                    case "Command":
                        if (value is int command) { person.Command = command; return true; }
                        break;
                    case "Strength":
                        if (value is int strength) { person.Strength = strength; return true; }
                        break;
                    case "Intelligence":
                        if (value is int intelligence) { person.Intelligence = intelligence; return true; }
                        break;
                    case "Politics":
                        if (value is int politics) { person.Politics = politics; return true; }
                        break;
                    case "Glamour":
                        if (value is int glamour) { person.Glamour = glamour; return true; }
                        break;
                    // case "Loyalty":
                    //    if (value is int loyalty) { person.Loyalty = loyalty; return true; }
                    //    break;
                    case "Ambition":
                        if (value is int ambition) { person.Ambition = ambition; return true; }
                        break;
                    // case "BelongedFactionID":
                    //    if (value is int factionId) { person.BelongedFactionID = factionId; return true; }
                    //    break;
                    case "BelongedFaction":
                        // if (value is Faction faction) { person.BelongedFaction = faction; return true; }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PersonTypeHandler] 设置属性失败 {propertyName}: {ex.Message}");
            }

            return false;
        }

        public object GetProperty(GameObject obj, string propertyName)
        {
            if (obj is not Person person) return null;

            try
            {
                switch (propertyName)
                {
                    case "ID": return person.ID;
                    case "Name": return person.Name;
                    case "SurName": return person.SurName;
                    case "GivenName": return person.GivenName;
                    case "CalledName": return person.CalledName;
                    case "Sex": return person.Sex;
                    case "Alive": return person.Alive;
                    case "Available": return person.Available;
                    case "Command": return person.Command;
                    case "Strength": return person.Strength;
                    case "Intelligence": return person.Intelligence;
                    case "Politics": return person.Politics;
                    case "Glamour": return person.Glamour;
                    // case "Loyalty": return person.Loyalty;
                    case "Ambition": return person.Ambition;
                    // case "BelongedFactionID": return person.BelongedFactionID;
                    case "BelongedFaction": return person.BelongedFaction;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PersonTypeHandler] 获取属性失败 {propertyName}: {ex.Message}");
            }

            return null;
        }
    }
}