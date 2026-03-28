using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  EmperorStateProbe scenario <scenario-json-path>");
    Console.Error.WriteLine("  EmperorStateProbe save <save-sav.gz-path>");
    Console.Error.WriteLine("  EmperorStateProbe roundtrip <scenario-json-path>");
    return 2;
}

string mode = args[0];
string inputPath = Path.GetFullPath(args[1]);
string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
Directory.SetCurrentDirectory(repoRoot);

string gameDllPath = Path.Combine(repoRoot, "WorldOfTheThreeKingdoms", "bin", "Debug", "net8.0", "WorldOfTheThreeKingdoms.dll");
string commonDataPath = Path.Combine(repoRoot, "Content", "Data", "Common", "CommonData.json");
string tempSavePath = Path.Combine(repoRoot, "EmperorStateProbe", "roundtrip_test.sav.gz");

if (!File.Exists(gameDllPath))
{
    Console.WriteLine("PROBE_FAIL");
    Console.WriteLine($"Game assembly not found: {gameDllPath}");
    return 1;
}

var resolver = new AssemblyDependencyResolver(gameDllPath);
AssemblyLoadContext.Default.Resolving += (_, assemblyName) =>
{
    string resolvedPath = resolver.ResolveAssemblyToPath(assemblyName);
    return resolvedPath == null ? null : AssemblyLoadContext.Default.LoadFromAssemblyPath(resolvedPath);
};

Assembly gameAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(gameDllPath);
PreloadCommonData(gameAssembly, commonDataPath);

Type sessionType = gameAssembly.GetType("GameManager.Session", throwOnError: true);
object sessionCurrent = sessionType.GetField("Current", BindingFlags.Public | BindingFlags.Static).GetValue(null);
PropertyInfo sessionScenarioProperty = sessionType.GetProperty("Scenario", BindingFlags.Public | BindingFlags.Instance);

Type managerType = gameAssembly.GetType("WorldOfTheThreeKingdoms.Serialization.SerializationManager", throwOnError: true);
object manager = Activator.CreateInstance(managerType);
MethodInfo loadScenarioMethod = managerType.GetMethod("LoadScenario", [typeof(string)]);
MethodInfo loadGameMethod = managerType.GetMethod("LoadGame", [typeof(string)]);
MethodInfo saveGameMethod = managerType.GetMethod("SaveGame");

try
{
    switch (mode)
    {
        case "scenario":
        {
            ReportScenarioDto("scenario_dto", gameAssembly, inputPath);
            object scenario = loadScenarioMethod.Invoke(manager, [inputPath]);
            ReportScenario("scenario_loaded", scenario, sessionCurrent, sessionScenarioProperty, fromScenario: true);
            return 0;
        }
        case "save":
        {
            object scenario = loadGameMethod.Invoke(manager, [inputPath]);
            ReportScenario("save_loaded", scenario, sessionCurrent, sessionScenarioProperty, fromScenario: false);
            return 0;
        }
        case "roundtrip":
        {
            ReportScenarioDto("roundtrip_dto", gameAssembly, inputPath);
            object scenario = loadScenarioMethod.Invoke(manager, [inputPath]);
            ReportScenario("roundtrip_source", scenario, sessionCurrent, sessionScenarioProperty, fromScenario: true);

            if (File.Exists(tempSavePath))
            {
                File.Delete(tempSavePath);
            }

            saveGameMethod.Invoke(manager, [scenario, tempSavePath]);
            Console.WriteLine($"roundtrip_saved={File.Exists(tempSavePath)}");
            Console.WriteLine($"roundtrip_save_path={tempSavePath}");

            object reloaded = loadGameMethod.Invoke(manager, [tempSavePath]);
            ReportScenario("roundtrip_reloaded", reloaded, sessionCurrent, sessionScenarioProperty, fromScenario: false);
            return 0;
        }
        default:
            Console.Error.WriteLine($"Unknown mode: {mode}");
            return 2;
    }
}
catch (TargetInvocationException tie) when (tie.InnerException != null)
{
    PrintExceptionChain(tie.InnerException);
    return 1;
}
catch (Exception ex)
{
    PrintExceptionChain(ex);
    return 1;
}

static void PreloadCommonData(Assembly gameAssembly, string commonDataPath)
{
    Type loaderType = gameAssembly.GetType("WorldOfTheThreeKingdoms.Serialization.CommonDataLoader", throwOnError: true);
    MethodInfo loadFromFile = loaderType.GetMethod("LoadFromFile", BindingFlags.Public | BindingFlags.Static, [typeof(string)]);
    object commonData = loadFromFile.Invoke(null, [commonDataPath]);

    Type commonDataType = gameAssembly.GetType("GameObjects.CommonData", throwOnError: true);
    FieldInfo currentField = commonDataType.GetField("Current", BindingFlags.Public | BindingFlags.Static);
    FieldInfo readyField = commonDataType.GetField("CurrentReady", BindingFlags.Public | BindingFlags.Static);
    currentField.SetValue(null, commonData);
    readyField.SetValue(null, true);
}

static void ReportScenarioDto(string label, Assembly gameAssembly, string scenarioPath)
{
    string json = File.ReadAllText(scenarioPath);
    int rawTrueCount = CountJsonTrue(json, "\"huangdisuozai\": true");

    Type dtoType = gameAssembly.GetType("WorldOfTheThreeKingdoms.Serialization.DTOs.GameScenarioDTO", throwOnError: true);
    Type architectureDtoType = gameAssembly.GetType("WorldOfTheThreeKingdoms.Serialization.DTOs.ArchitectureDTO", throwOnError: true);
    Type contextType = gameAssembly.GetType("WorldOfTheThreeKingdoms.Serialization.GameJsonContext", throwOnError: true);
    MethodInfo getDefaultOptions = contextType.GetMethod("GetDefaultOptions", BindingFlags.Public | BindingFlags.Static, [typeof(bool)]);
    JsonSerializerOptions options = (JsonSerializerOptions)getDefaultOptions.Invoke(null, [false]);

    bool typeHasFlagProperty = architectureDtoType.GetProperty("huangdisuozai", BindingFlags.Public | BindingFlags.Instance) != null;
    JsonTypeInfo architectureTypeInfo = options.GetTypeInfo(architectureDtoType);
    bool metadataHasFlag = false;
    List<string> metadataProperties = [];
    foreach (JsonPropertyInfo property in architectureTypeInfo.Properties)
    {
        metadataProperties.Add(property.Name);
        if (string.Equals(property.Name, "huangdisuozai", StringComparison.OrdinalIgnoreCase))
        {
            metadataHasFlag = true;
        }
    }

    object dto = JsonSerializer.Deserialize(json, dtoType, options);
    object architectures = dtoType.GetProperty("Architectures", BindingFlags.Public | BindingFlags.Instance)?.GetValue(dto);

    int dtoFlaggedCount = 0;
    List<string> dtoFlagged = [];
    if (architectures is IEnumerable enumerable)
    {
        foreach (object archDto in enumerable)
        {
            if (archDto == null)
            {
                continue;
            }

            PropertyInfo emperorFlagProperty = archDto.GetType().GetProperty("huangdisuozai", BindingFlags.Public | BindingFlags.Instance);
            if (emperorFlagProperty != null && emperorFlagProperty.PropertyType == typeof(bool) && (bool)emperorFlagProperty.GetValue(archDto))
            {
                dtoFlaggedCount++;
                object archName = archDto.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archDto);
                object archId = archDto.GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archDto);
                dtoFlagged.Add($"{archName}#{archId}");
            }
        }
    }

    Console.WriteLine($"[{label}] raw_true_count={rawTrueCount}");
    Console.WriteLine($"[{label}] type_has_huangdisuozai={typeHasFlagProperty}");
    Console.WriteLine($"[{label}] metadata_has_huangdisuozai={metadataHasFlag}");
    Console.WriteLine($"[{label}] metadata_properties={string.Join(",", metadataProperties)}");
    Console.WriteLine($"[{label}] dto_flagged_count={dtoFlaggedCount}");
    Console.WriteLine($"[{label}] dto_flagged={string.Join(",", dtoFlagged)}");
}

static int CountJsonTrue(string json, string token)
{
    int count = 0;
    int start = 0;
    while (true)
    {
        int idx = json.IndexOf(token, start, StringComparison.Ordinal);
        if (idx < 0)
        {
            return count;
        }

        count++;
        start = idx + token.Length;
    }
}

static void ReportScenario(string label, object scenario, object sessionCurrent, PropertyInfo sessionScenarioProperty, bool fromScenario)
{
    sessionScenarioProperty.SetValue(sessionCurrent, scenario);

    MethodInfo processScenarioData = scenario.GetType().GetMethod("ProcessScenarioData", [typeof(bool), typeof(bool)]);
    processScenarioData?.Invoke(scenario, [fromScenario, false]);

    MethodInfo youhuangdiMethod = scenario.GetType().GetMethod("youhuangdi", Type.EmptyTypes);
    bool hasEmperor = (bool)(youhuangdiMethod?.Invoke(scenario, null) ?? false);

    object architectures = scenario.GetType().GetField("Architectures", BindingFlags.Public | BindingFlags.Instance)?.GetValue(scenario)
        ?? scenario.GetType().GetProperty("Architectures", BindingFlags.Public | BindingFlags.Instance)?.GetValue(scenario);
    IEnumerable architectureItems = GetEnumerableGameObjects(architectures);

    List<string> flaggedArchitectures = [];
    foreach (object architecture in architectureItems)
    {
        if (architecture == null)
        {
            continue;
        }

        PropertyInfo emperorFlagProperty = architecture.GetType().GetProperty("huangdisuozai", BindingFlags.Public | BindingFlags.Instance);
        if (emperorFlagProperty != null && emperorFlagProperty.PropertyType == typeof(bool) && (bool)emperorFlagProperty.GetValue(architecture))
        {
            object archName = architecture.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(architecture);
            object archId = architecture.GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.Instance)?.GetValue(architecture);
            flaggedArchitectures.Add($"{archName}#{archId}");
        }
    }

    Console.WriteLine($"[{label}] youhuangdi={hasEmperor}");
    Console.WriteLine($"[{label}] flagged_count={flaggedArchitectures.Count}");
    Console.WriteLine($"[{label}] flagged={string.Join(",", flaggedArchitectures)}");
}

static IEnumerable GetEnumerableGameObjects(object list)
{
    if (list == null)
    {
        yield break;
    }

    PropertyInfo gameObjectsProperty = list.GetType().GetProperty("GameObjects", BindingFlags.Public | BindingFlags.Instance);
    if (gameObjectsProperty?.GetValue(list) is IEnumerable gameObjects)
    {
        foreach (object item in gameObjects)
        {
            yield return item;
        }
        yield break;
    }

    if (list is IEnumerable enumerable)
    {
        foreach (object item in enumerable)
        {
            yield return item;
        }
    }
}

static void PrintExceptionChain(Exception ex)
{
    Console.WriteLine("PROBE_FAIL");
    int depth = 0;
    for (Exception current = ex; current != null; current = current.InnerException)
    {
        Console.WriteLine($"EX{depth}:{current.GetType().FullName}");
        Console.WriteLine(current.Message);
        depth++;
    }

    Console.WriteLine(ex.StackTrace);
}

