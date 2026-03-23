using System;
using System.Linq;
using GameObjects;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        var scenario = new GameScenario();
        scenario.LoadScenarioData("Content/Data/Scenario/一八四年/GameData.xml");
        
        Console.WriteLine($"AllBiographies总数: {scenario.AllBiographies.Count}");
        Console.WriteLine("\n字典中的所有ID:");
        
        foreach (var kvp in scenario.AllBiographies.Biographys)
        {
            Console.WriteLine($"  ID={kvp.Key}, Name={kvp.Value.Name}, Brief长度={kvp.Value.Brief?.Length ?? 0}");
        }
        
        Console.WriteLine("\n检查韦昭:");
        var weizhao = scenario.AllPersons.Persons.Values.FirstOrDefault(p => p.Name == "韦昭");
        if (weizhao != null)
        {
            Console.WriteLine($"  韦昭 ID={weizhao.ID}, PersonBiographyID={weizhao.PersonBiographyID}");
            Console.WriteLine($"  字典包含PersonBiographyID={weizhao.PersonBiographyID}? {scenario.AllBiographies.Biographys.ContainsKey(weizhao.PersonBiographyID)}");
        }
        
        Console.WriteLine("\n检查阿会喃:");
        var ahuinan = scenario.AllPersons.Persons.Values.FirstOrDefault(p => p.Name == "阿会喃");
        if (ahuinan != null)
        {
            Console.WriteLine($"  阿会喃 ID={ahuinan.ID}, PersonBiographyID={ahuinan.PersonBiographyID}");
            Console.WriteLine($"  字典包含PersonBiographyID={ahuinan.PersonBiographyID}? {scenario.AllBiographies.Biographys.ContainsKey(ahuinan.PersonBiographyID)}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}
