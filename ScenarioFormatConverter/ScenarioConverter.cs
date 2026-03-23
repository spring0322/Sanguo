using System.Text.Json.Nodes;

namespace ScenarioFormatConverter;

/// <summary>
/// 剧本转换器核心逻辑 v4.0
/// 在 Person/Architecture/Section 对象中添加缺失的引用字段
/// </summary>
class ScenarioConverter
{
    private readonly JsonNode _jsonDoc;
    public ConversionStats Stats { get; } = new();

    public ScenarioConverter(JsonNode jsonDoc)
    {
        _jsonDoc = jsonDoc;
    }

    public void Convert()
    {
        Console.WriteLine("  🔄 开始转换...");

        // 1. 添加 Person.LocationArchitectureID 和 Person.BelongedFactionID
        AddPersonFields();

        // 2. 添加 Architecture.BelongedFactionID
        AddArchitectureFactionField();

        // 3. 🔥 新增：转换 Biography 数据结构
        // 日期：2026-03-17
        // 原因：旧格式使用 AllBiographies.Biographys（嵌套字典），新格式使用 Biographies（平面数组）
        ConvertBiographies();

        // 4. 🔥 新增：添加 Architecture.BelongedSectionID
        // 日期：2026-03-17
        // 原因：旧剧本缺少此字段，导致军团信息无法显示
        AddArchitectureSectionField();

        // 5. 🔥 新增：添加 Section.BelongedFactionID
        // 日期：2026-03-17
        // 原因：旧剧本部分 Section 缺少此字段，导致军团-势力关系丢失
        AddSectionFactionField();

        // 6. 🔥 新增：添加 State.LinkedRegionID
        // 日期：2026-03-17
        // 原因：旧剧本缺少此字段，导致州域-地域关系无法显示
        AddStateLinkedRegionField();

        // 7. 🔥 新增：转换宝物归属关系（Treasure-Centric → Person-Centric）
        // 日期：2026-03-18
        // 原因：新的序列化格式使用 Person.TreasureIDs 管理宝物归属
        ConvertTreasureOwnership();

        // 8. 🔥 新增：转换 Person 集合字段（String → List<int>）
        // 日期：2026-03-18
        // 原因：新的序列化格式使用 SkillIDs/StuntIDs/TitleIDs，旧格式使用 SkillsString/StuntsString/RealTitlesString
        ConvertPersonCollections();

        Console.WriteLine("  ✅ 转换完成");
    }

    private JsonArray? GetGameObjectsArray(string key)
    {
        var node = _jsonDoc[key];
        if (node is JsonObject jsonObj)
        {
            return jsonObj["GameObjects"]?.AsArray();
        }
        else if (node is JsonArray jsonArray)
        {
            return jsonArray;
        }
        return null;
    }

    /// <summary>
    /// 从 Architecture.PersonsString 推断并添加 Person 的引用字段
    /// </summary>
    private void AddPersonFields()
    {
        var architectures = GetGameObjectsArray("Architectures");
        var persons = GetGameObjectsArray("Persons");
        var factions = GetGameObjectsArray("Factions");

        if (architectures == null || persons == null || factions == null)
            throw new InvalidOperationException("剧本数据损坏：缺少 Architectures/Persons/Factions 节点");

        // 创建 Architecture ID → Faction ID 的映射
        Dictionary<int, int> archToFactionMap = [];
        foreach (var faction in factions)
        {
            if (faction == null)
                throw new InvalidOperationException("剧本数据损坏：Factions 数组中存在 null 元素");
            
            int factionID = faction["ID"]?.GetValue<int>() ?? -1;
            if (factionID < 0) continue;

            // 从 ArchitecturesString 解析建筑 ID
            string? archString = faction["ArchitecturesString"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(archString))
            {
                var archIDs = ParseIDString(archString);
                foreach (int archID in archIDs)
                {
                    archToFactionMap[archID] = factionID;
                }
            }
        }

        // 创建 Person ID → (ArchID, FactionID, Status) 的映射
        Dictionary<int, (int archID, int factionID, int status)> personDataMap = [];
        
        // 遍历建筑，记录每个武将的位置和状态
        foreach (var arch in architectures)
        {
            if (arch == null)
                throw new InvalidOperationException("剧本数据损坏：Architectures 数组中存在 null 元素");

            int archID = arch["ID"]?.GetValue<int>() ?? -1;
            if (archID < 0) continue;

            // 获取建筑的势力 ID
            int factionID = archToFactionMap.GetValueOrDefault(archID, -1);

            // 🔥 关键修复：处理 PersonsString（有势力的武将）
            string? personsString = arch["PersonsString"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(personsString))
            {
                var personIDs = ParseIDString(personsString);
                foreach (int personID in personIDs)
                {
                    // PersonStatus.Normal = 2
                    personDataMap[personID] = (archID, factionID, 2);
                }
            }

            // 🔥 关键修复：处理 NoFactionPersonsString（在野武将）
            string? noFactionPersonsString = arch["NoFactionPersonsString"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(noFactionPersonsString))
            {
                var personIDs = ParseIDString(noFactionPersonsString);
                foreach (int personID in personIDs)
                {
                    // PersonStatus.NoFaction = 0
                    personDataMap[personID] = (archID, -1, 0);
                }
            }
        }

        // 遍历所有Person对象，添加字段
        foreach (var person in persons)
        {
            if (person == null)
                throw new InvalidOperationException("剧本数据损坏：Persons 数组中存在 null 元素");
            
            int personID = person["ID"]?.GetValue<int>() ?? -1;
            if (personID < 0) continue;

            // 检查是否已有字段（避免重复转换）
            if (person["LocationArchitectureID"] != null)
                continue;

            // 查找武将的数据
            if (personDataMap.TryGetValue(personID, out var data))
            {
                // 在建筑中的武将（有势力或在野）
                person["LocationArchitectureID"] = data.archID;
                person["BelongedFactionID"] = data.factionID;
                person["Status"] = data.status;
                
                Stats.PersonLocationSet++;
                Stats.PersonFactionSet++;
            }
            else
            {
                // 不在任何建筑中的武将（未出场）
                person["LocationArchitectureID"] = -1;
                person["BelongedFactionID"] = -1;
                person["Status"] = 0;  // NoFaction
                
                Stats.PersonLocationSet++;
                Stats.PersonFactionSet++;
            }
        }

        Console.WriteLine($"    Person.LocationArchitectureID: {Stats.PersonLocationSet}");
        Console.WriteLine($"    Person.BelongedFactionID: {Stats.PersonFactionSet}");
    }

    /// <summary>
    /// 从 Faction.ArchitecturesString 推断并添加 Architecture.BelongedFactionID
    /// </summary>
    private void AddArchitectureFactionField()
    {
        var architectures = GetGameObjectsArray("Architectures");
        var factions = GetGameObjectsArray("Factions");

        if (architectures == null || factions == null)
            throw new InvalidOperationException("剧本数据损坏：缺少 Architectures/Factions 节点");

        // 创建 Architecture ID → Architecture Node 的映射
        Dictionary<int, JsonNode> archMap = [];
        foreach (var arch in architectures)
        {
            if (arch == null)
                throw new InvalidOperationException("剧本数据损坏：Architectures 数组中存在 null 元素");
            
            int id = arch["ID"]?.GetValue<int>() ?? -1;
            if (id >= 0)  // 🔥 ID=0 是有效的（洛阳）
            {
                archMap[id] = arch;
            }
        }

        // 遍历势力，为每个建筑添加字段
        foreach (var faction in factions)
        {
            if (faction == null)
                throw new InvalidOperationException("剧本数据损坏：Factions 数组中存在 null 元素");

            int factionID = faction["ID"]?.GetValue<int>() ?? -1;
            if (factionID < 0) continue;

            // 从 ArchitecturesString 解析建筑 ID
            string? archString = faction["ArchitecturesString"]?.GetValue<string>();
            if (string.IsNullOrEmpty(archString))
                continue;

            var archIDs = ParseIDString(archString);

            // 为每个建筑添加字段
            foreach (int archID in archIDs)
            {
                if (archMap.TryGetValue(archID, out var arch))
                {
                    arch["BelongedFactionID"] = factionID;
                    Stats.ArchitectureFactionSet++;
                }
            }
        }

        Console.WriteLine($"    Architecture.BelongedFactionID: {Stats.ArchitectureFactionSet}");
    }

    /// <summary>
    /// 解析 ID 字符串（空格分隔）
    /// </summary>
    private static List<int> ParseIDString(string s)
    {
        if (string.IsNullOrEmpty(s))
            return [];

        List<int> result = [];
        foreach (var part in s.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part, out int id))
            {
                result.Add(id);
            }
        }
        return result;
    }

    /// <summary>
    /// 🔥 新增：转换 Biography 数据结构
    /// 日期：2026-03-17
    /// 原因：旧格式使用 AllBiographies.Biographys（嵌套字典），新格式使用 Biographies（平面数组）
    /// </summary>
    private void ConvertBiographies()
    {
        // 先检查是否有包含 Biographys (旧拼写) 字典的 AllBiographies 对象
        var allBiographies = _jsonDoc["AllBiographies"] as JsonObject;
        if (allBiographies == null)
        {
            Console.WriteLine("    ⚠️ 未找到 AllBiographies 节点，跳过 Biography 转换");
            return;
        }

        // 检查是否已经有新格式的 Biographies 节点
        if (_jsonDoc["Biographies"] != null)
        {
            Console.WriteLine("    ℹ️ 已存在 Biographies 节点，跳过转换");
            return;
        }

        // 获取旧格式的 Biographys 字典
        var biographysDict = allBiographies["Biographys"]?.AsObject();
        if (biographysDict == null)
        {
            Console.WriteLine("    ⚠️ AllBiographies.Biographys 为空，跳过转换");
            return;
        }

        // 创建新格式的 Biographies 数组
        JsonArray biographiesArray = [];

        // 遍历字典，转换为数组
        foreach (var kvp in biographysDict)
        {
            var biography = kvp.Value;
            if (biography != null)
            {
                biographiesArray.Add(biography.DeepClone());
                Stats.BiographiesConverted++;
            }
        }

        // 添加到根节点
        _jsonDoc["Biographies"] = biographiesArray;

        Console.WriteLine($"    Biography 转换完成: {Stats.BiographiesConverted} 条");
    }

    /// <summary>
    /// 🔥 新增：从 Section.ArchitecturesString 推断并添加 Architecture.BelongedSectionID
    /// 日期：2026-03-17
    /// 原因：旧剧本缺少此字段，导致军团信息无法显示
    /// </summary>
    private void AddArchitectureSectionField()
    {
        var architectures = GetGameObjectsArray("Architectures");
        var sections = GetGameObjectsArray("Sections");

        if (architectures == null || sections == null)
        {
            Console.WriteLine("    ⚠️ 缺少 Architectures/Sections 节点，跳过 Architecture.BelongedSectionID 转换");
            return;
        }

        // 创建 Architecture ID → Architecture Node 的映射
        Dictionary<int, JsonNode> archMap = [];
        foreach (var arch in architectures)
        {
            if (arch == null)
                throw new InvalidOperationException("剧本数据损坏：Architectures 数组中存在 null 元素");

            int id = arch["ID"]?.GetValue<int>() ?? -1;
            if (id >= 0)  // 🔥 ID=0 是有效的（洛阳）
            {
                archMap[id] = arch;
            }
        }

        // 遍历军区，为每个建筑添加字段
        foreach (var section in sections)
        {
            if (section == null)
                throw new InvalidOperationException("剧本数据损坏：Sections 数组中存在 null 元素");

            int sectionID = section["ID"]?.GetValue<int>() ?? -1;
            if (sectionID < 0) continue;

            // 从 ArchitecturesString 解析建筑 ID
            string? archString = section["ArchitecturesString"]?.GetValue<string>();
            if (string.IsNullOrEmpty(archString))
                continue;

            var archIDs = ParseIDString(archString);

            // 为每个建筑添加字段
            foreach (int archID in archIDs)
            {
                if (archMap.TryGetValue(archID, out var arch))
                {
                    // 检查是否已有字段（避免重复转换）
                    if (arch["BelongedSectionID"] == null)
                    {
                        arch["BelongedSectionID"] = sectionID;
                        Stats.ArchitectureSectionSet++;
                    }
                }
            }
        }

        Console.WriteLine($"    Architecture.BelongedSectionID: {Stats.ArchitectureSectionSet}");
    }

    /// <summary>
    /// 🔥 新增：从 Architecture.BelongedSectionID 推断并添加 Section.BelongedFactionID
    /// 日期：2026-03-17
    /// 原因：旧剧本部分 Section 缺少此字段，导致军团-势力关系丢失
    /// 逻辑：从军区的第一个建筑推断势力归属
    /// </summary>
    private void AddSectionFactionField()
    {
        var architectures = GetGameObjectsArray("Architectures");
        var sections = GetGameObjectsArray("Sections");

        if (architectures == null || sections == null)
        {
            Console.WriteLine("    ⚠️ 缺少 Architectures/Sections 节点，跳过 Section.BelongedFactionID 转换");
            return;
        }

        // 创建 Architecture ID → BelongedFactionID 的映射
        Dictionary<int, int> archToFactionMap = [];
        foreach (var arch in architectures)
        {
            if (arch == null)
                throw new InvalidOperationException("剧本数据损坏：Architectures 数组中存在 null 元素");

            int archID = arch["ID"]?.GetValue<int>() ?? -1;
            int factionID = arch["BelongedFactionID"]?.GetValue<int>() ?? -1;

            if (archID >= 0)  // 🔥 ID=0 是有效的（洛阳）
            {
                archToFactionMap[archID] = factionID;
            }
        }

        // 遍历军区，从第一个建筑推断势力归属
        foreach (var section in sections)
        {
            if (section == null)
                throw new InvalidOperationException("剧本数据损坏：Sections 数组中存在 null 元素");

            // 检查是否已有字段（避免重复转换）
            if (section["BelongedFactionID"] != null)
            {
                int existingFactionID = section["BelongedFactionID"]?.GetValue<int>() ?? -1;
                if (existingFactionID >= 0)
                    continue;  // 已有有效的势力ID，跳过
            }

            // 从 ArchitecturesString 解析建筑 ID
            string? archString = section["ArchitecturesString"]?.GetValue<string>();
            if (string.IsNullOrEmpty(archString))
            {
                // 军区没有建筑，设置为无势力
                section["BelongedFactionID"] = -1;
                continue;
            }

            var archIDs = ParseIDString(archString);
            if (archIDs.Count == 0)
            {
                section["BelongedFactionID"] = -1;
                continue;
            }

            // 从第一个建筑推断势力归属
            int firstArchID = archIDs[0];
            if (archToFactionMap.TryGetValue(firstArchID, out int factionID))
            {
                section["BelongedFactionID"] = factionID;
                Stats.SectionFactionSet++;
            }
            else
            {
                // 建筑不存在，设置为无势力
                section["BelongedFactionID"] = -1;
            }
        }

        Console.WriteLine($"    Section.BelongedFactionID: {Stats.SectionFactionSet}");
    }

    /// <summary>
    /// 🔥 新增：从 Region.StatesListString 推断并添加 State.LinkedRegionID
    /// 日期：2026-03-17
    /// 原因：旧剧本缺少此字段，导致州域-地域关系无法显示
    /// </summary>
    private void AddStateLinkedRegionField()
    {
        var states = GetGameObjectsArray("States");
        var regions = GetGameObjectsArray("Regions");

        if (states == null || regions == null)
        {
            Console.WriteLine("    ⚠️ 缺少 States/Regions 节点，跳过 State.LinkedRegionID 转换");
            return;
        }

        // 创建 State ID → State Node 的映射
        Dictionary<int, JsonNode> stateMap = [];
        foreach (var state in states)
        {
            if (state == null)
                throw new InvalidOperationException("剧本数据损坏：States 数组中存在 null 元素");

            int id = state["ID"]?.GetValue<int>() ?? -1;
            if (id >= 0)  // 🔥 ID=0 是有效的（司隶 ID=0）
            {
                stateMap[id] = state;
            }
        }

        // 遍历地域，为每个州域添加 LinkedRegionID
        foreach (var region in regions)
        {
            if (region == null)
                throw new InvalidOperationException("剧本数据损坏：Regions 数组中存在 null 元素");

            int regionID = region["ID"]?.GetValue<int>() ?? -1;
            if (regionID < 0) continue;

            // 从 StatesListString 解析州域 ID
            string? statesString = region["StatesListString"]?.GetValue<string>();
            if (string.IsNullOrEmpty(statesString))
                continue;

            var stateIDs = ParseIDString(statesString);

            // 为每个州域添加 LinkedRegionID
            foreach (int stateID in stateIDs)
            {
                if (stateMap.TryGetValue(stateID, out var state))
                {
                    // 检查是否已有字段（避免重复转换）
                    if (state["LinkedRegionID"] == null)
                    {
                        state["LinkedRegionID"] = regionID;
                        Stats.StateLinkedRegionSet++;
                    }
                }
            }
        }

        Console.WriteLine($"    State.LinkedRegionID: {Stats.StateLinkedRegionSet}");
    }

    /// <summary>
    /// 🔥 新增：转换宝物归属关系（Treasure-Centric → Person-Centric）
    /// 日期：2026-03-18
    /// 原因：新的序列化格式使用 Person.TreasureIDs 管理宝物归属
    /// 逻辑：从 Treasure.BelongedPersonIDString 提取关联，写入 Person.TreasureIDs
    /// </summary>
    private void ConvertTreasureOwnership()
    {
        var treasures = GetGameObjectsArray("Treasures");
        var persons = GetGameObjectsArray("Persons");

        if (treasures == null || persons == null)
        {
            Console.WriteLine("    ⚠️ 缺少 Treasures/Persons 节点，跳过宝物归属转换");
            return;
        }

        // 步骤 1：从 Treasure.BelongedPersonIDString 提取关联
        // 创建 Person ID → Treasure IDs 的映射
        Dictionary<int, List<int>> personTreasures = [];
        
        foreach (var treasure in treasures)
        {
            if (treasure == null)
                throw new InvalidOperationException("剧本数据损坏：Treasures 数组中存在 null 元素");

            // 🔥 ANTI-BAND-AID：ID 字段必须存在，否则 Fail Fast
            int treasureID = treasure["ID"]?.GetValue<int>() 
                ?? throw new InvalidOperationException("剧本数据损坏：Treasure 缺少 ID 字段");

            // 获取宝物的持有人 ID
            // 🔥 关键：ID >= 0 表示有效引用（ID=0 是有效的，如阿会喃）
            // 注意：BelongedPersonIDString 可以为 null（无持有人的宝物）
            int belongedPersonID = treasure["BelongedPersonIDString"]?.GetValue<int>() ?? -1;
            
            if (belongedPersonID >= 0)
            {
                // 有持有人的宝物
                if (!personTreasures.ContainsKey(belongedPersonID))
                {
                    personTreasures[belongedPersonID] = [];
                }
                personTreasures[belongedPersonID].Add(treasureID);
                Stats.TreasuresWithOwner++;
            }
            else
            {
                // 无持有人的宝物（隐藏或未发现）
                Stats.TreasuresWithoutOwner++;
            }
        }

        // 步骤 2：写入 Person.TreasureIDs
        foreach (var person in persons)
        {
            if (person == null)
                throw new InvalidOperationException("剧本数据损坏：Persons 数组中存在 null 元素");

            // 🔥 ANTI-BAND-AID：ID 字段必须存在，否则 Fail Fast
            int personID = person["ID"]?.GetValue<int>() 
                ?? throw new InvalidOperationException("剧本数据损坏：Person 缺少 ID 字段");

            // 检查是否已有 TreasureIDs 字段（避免重复转换）
            if (person["TreasureIDs"] != null)
            {
                var existingTreasureIDs = person["TreasureIDs"]?.AsArray();
                if (existingTreasureIDs != null && existingTreasureIDs.Count > 0)
                {
                    continue;  // 已有宝物数据，跳过
                }
            }

            // 查找该人物的宝物
            if (personTreasures.TryGetValue(personID, out var treasureIDs))
            {
                // 创建 TreasureIDs 数组
                JsonArray treasureIDsArray = [];
                foreach (int treasureID in treasureIDs)
                {
                    treasureIDsArray.Add(treasureID);
                }
                
                person["TreasureIDs"] = treasureIDsArray;
                Stats.PersonTreasureIDsSet++;
            }
            else
            {
                // 该人物没有宝物，设置为空数组
                person["TreasureIDs"] = new JsonArray();
            }
        }

        // 步骤 3：清除 Treasure.BelongedPersonIDString（可选，保留以兼容旧版本）
        // 注意：不清除此字段，以保持向后兼容性
        // 新版本会忽略此字段，旧版本仍然可以读取

        Console.WriteLine($"    宝物归属转换完成:");
        Console.WriteLine($"      - 有持有人的宝物: {Stats.TreasuresWithOwner}");
        Console.WriteLine($"      - 无持有人的宝物: {Stats.TreasuresWithoutOwner}");
        Console.WriteLine($"      - Person.TreasureIDs 设置: {Stats.PersonTreasureIDsSet}");
    }

    /// <summary>
    /// 🔥 新增：转换 Person 集合字段（String → List&lt;int&gt;）
    /// 日期：2026-03-18
    /// 原因：新的序列化格式使用 SkillIDs/StuntIDs/TitleIDs
    ///       旧格式使用 SkillsString/StuntsString/RealTitlesString（空格分隔）
    /// </summary>
    private void ConvertPersonCollections()
    {
        var persons = GetGameObjectsArray("Persons");
        if (persons == null)
        {
            Console.WriteLine("    ⚠️ 缺少 Persons 节点，跳过集合字段转换");
            return;
        }

        foreach (var person in persons)
        {
            if (person == null)
                throw new InvalidOperationException("剧本数据损坏：Persons 数组中存在 null 元素");

            // SkillsString → SkillIDs
            if (person["SkillIDs"] == null)
            {
                string? skillsStr = person["SkillsString"]?.GetValue<string>();
                person["SkillIDs"] = ParseIDStringToJsonArray(skillsStr);
                Stats.PersonSkillIDsSet++;
            }

            // StuntsString → StuntIDs
            if (person["StuntIDs"] == null)
            {
                string? stuntsStr = person["StuntsString"]?.GetValue<string>();
                person["StuntIDs"] = ParseIDStringToJsonArray(stuntsStr);
                Stats.PersonStuntIDsSet++;
            }

            // RealTitlesString → TitleIDs
            if (person["TitleIDs"] == null)
            {
                string? titlesStr = person["RealTitlesString"]?.GetValue<string>();
                person["TitleIDs"] = ParseIDStringToJsonArray(titlesStr);
                Stats.PersonTitleIDsSet++;
            }
        }

        Console.WriteLine($"    Person 集合字段转换完成:");
        Console.WriteLine($"      - SkillIDs 设置: {Stats.PersonSkillIDsSet}");
        Console.WriteLine($"      - StuntIDs 设置: {Stats.PersonStuntIDsSet}");
        Console.WriteLine($"      - TitleIDs 设置: {Stats.PersonTitleIDsSet}");
    }

    /// <summary>
    /// 将空格分隔的 ID 字符串解析为 JsonArray
    /// </summary>
    private static JsonArray ParseIDStringToJsonArray(string? s)
    {
        JsonArray array = [];
        if (string.IsNullOrWhiteSpace(s))
            return array;

        foreach (var part in s.Split([' ', '\t', '\n', '\r', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part, out int id))
                array.Add(id);
        }
        return array;
    }
}
