using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: ScenarioLoadProbe <scenario-json-path>");
    return 2;
}

string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
Directory.SetCurrentDirectory(repoRoot);

string scenarioPath = Path.GetFullPath(args[0]);
string gameDllPath = Path.Combine(repoRoot, "WorldOfTheThreeKingdoms", "bin", "Debug", "net8.0", "WorldOfTheThreeKingdoms.dll");
string commonDataPath = Path.Combine(repoRoot, "Content", "Data", "Common", "CommonData.json");
if (!File.Exists(gameDllPath))
{
    Console.WriteLine("LOAD_FAIL");
    Console.WriteLine($"Game assembly not found: {gameDllPath}");
    return 1;
}

var resolver = new AssemblyDependencyResolver(gameDllPath);
AssemblyLoadContext.Default.Resolving += (_, assemblyName) =>
{
    string resolvedPath = resolver.ResolveAssemblyToPath(assemblyName);
    return resolvedPath == null ? null : AssemblyLoadContext.Default.LoadFromAssemblyPath(resolvedPath);
};

Console.WriteLine($"RepoRoot={repoRoot}");
Console.WriteLine($"ScenarioPath={scenarioPath}");
Console.WriteLine($"GameDll={gameDllPath}");
Console.WriteLine($"CommonDataPath={commonDataPath}");

try
{
    Assembly gameAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(gameDllPath);
    PreloadCommonData(gameAssembly, commonDataPath);

    Type managerType = gameAssembly.GetType("WorldOfTheThreeKingdoms.Serialization.SerializationManager", throwOnError: true);
    object manager = Activator.CreateInstance(managerType);
    MethodInfo loadScenario = managerType.GetMethod("LoadScenario", [typeof(string)]);
    object scenario = loadScenario.Invoke(manager, [scenarioPath]);
    if (scenario == null)
    {
        Console.WriteLine("LOAD_NULL");
        return 1;
    }

    Console.WriteLine("LOAD_OK");
    Console.WriteLine($"Title={GetStringProperty(scenario, "ScenarioTitle")}");
    Console.WriteLine($"Architectures={GetCountProperty(scenario, "Architectures")}");
    Console.WriteLine($"Persons={GetCountProperty(scenario, "Persons")}");
    Console.WriteLine($"Factions={GetCountProperty(scenario, "Factions")}");
    return 0;
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

static string GetStringProperty(object target, string propertyName)
{
    object value = target.GetType().GetProperty(propertyName)?.GetValue(target);
    return value?.ToString() ?? string.Empty;
}

static int GetCountProperty(object target, string propertyName)
{
    object value = target.GetType().GetProperty(propertyName)?.GetValue(target);
    if (value == null)
    {
        return -1;
    }

    PropertyInfo countProperty = value.GetType().GetProperty("Count");
    if (countProperty?.GetValue(value) is int count)
    {
        return count;
    }

    return -1;
}

static void PrintExceptionChain(Exception ex)
{
    Console.WriteLine("LOAD_FAIL");
    int depth = 0;
    for (Exception current = ex; current != null; current = current.InnerException)
    {
        Console.WriteLine($"EX{depth}:{current.GetType().FullName}");
        Console.WriteLine(current.Message);
        depth++;
    }

    Console.WriteLine(ex.StackTrace);
}
