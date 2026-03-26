using System;
using System.IO;
using System.IO.Compression;
using GameManager;
using GameObjects;
using WorldOfTheThreeKingdoms.Serialization;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string jsonPath = Path.Combine("Record", "temp_save.json");
string gzipPath = Path.Combine("Record", "temp_save.sav.gz");

if (!File.Exists(jsonPath))
{
    Console.WriteLine($"存档不存在: {jsonPath}");
    return;
}

using (FileStream source = File.OpenRead(jsonPath))
using (FileStream target = File.Create(gzipPath))
using (GZipStream gzip = new(target, CompressionLevel.Optimal))
{
    source.CopyTo(gzip);
}

try
{
    SerializationManager manager = new();
    GameScenario scenario = manager.LoadGame(gzipPath);
    Session.Current.Scenario = scenario;

    Architecture architecture = scenario.Architectures.GetGameObject(144) as Architecture
        ?? throw new InvalidOperationException("未找到土垠(ID:144)");
    Faction faction = scenario.Factions.GetGameObject(8) as Faction
        ?? throw new InvalidOperationException("未找到公孙瓒势力(ID:8)");

    Console.WriteLine($"场景: {scenario.ScenarioTitle}");
    Console.WriteLine($"势力: {faction.Name}");
    Console.WriteLine($"城池: {architecture.Name}");
    Console.WriteLine($"人物数: {architecture.Persons.Count}");
    Console.WriteLine($"编队数: {architecture.Militaries.Count}");

    architecture.RunPrepareAI();
    architecture.RunMilitaryAI();

    Console.WriteLine("RunMilitaryAI 执行完成，无异常。");
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    Environment.ExitCode = 1;
}
