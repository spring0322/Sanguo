using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.FactionDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using GameObjects.SectionDetail;

namespace WorldOfTheThreeKingdoms.Serialization.Phases
{
    /// <summary>
    /// Interface for reference linkers
    /// Each linker is responsible for linking references for a specific game object type
    /// </summary>
    /// <typeparam name="T">The game object type to link references for</typeparam>
    public interface IReferenceLinker<T>
    {
        /// <summary>
        /// Link all references for the given object
        /// </summary>
        /// <param name="obj">The object to link references for</param>
        /// <param name="scenario">The game scenario containing all objects</param>
        void LinkReferences(T obj, GameScenario scenario);
    }
    
    /// <summary>
    /// Phase 3: Link References Phase
    /// Resolves ID references back to object references after all objects have been created
    /// Handles invalid references gracefully by logging warnings and resetting to safe values
    /// 
    /// Performance Optimization:
    /// - Pre-builds Dictionary lookup tables for O(1) reference resolution
    /// - Avoids O(n) linear searches through collections
    /// </summary>
    public class LinkReferencesPhase
    {
        private readonly PersonReferenceLinker _personLinker;
        private readonly FactionReferenceLinker _factionLinker;
        private readonly ArchitectureReferenceLinker _architectureLinker;
        private readonly LegionReferenceLinker _legionLinker;
        private readonly TroopReferenceLinker _troopLinker;
        private readonly SectionReferenceLinker _sectionLinker;
        
        /// <summary>
        /// Constructor - initializes all reference linkers
        /// </summary>
        public LinkReferencesPhase()
        {
            _personLinker = new PersonReferenceLinker();
            _factionLinker = new FactionReferenceLinker();
            _architectureLinker = new ArchitectureReferenceLinker();
            _legionLinker = new LegionReferenceLinker();
            _troopLinker = new TroopReferenceLinker();
            _sectionLinker = new SectionReferenceLinker();
        }
        
        /// <summary>
        /// Main entry point for Link References Phase
        /// Links all references for all game objects in the scenario
        /// 
        /// Performance Optimization:
        /// Pre-builds lookup tables for all collections to enable O(1) lookups
        /// </summary>
        /// <param name="scenario">The game scenario with all objects created but references not yet linked</param>
        public void LinkReferences(GameScenario scenario)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));
            
            // 馃敟 璇婃柇锛氱‘璁?LinkReferencesPhase 鏄惁鎵ц
            // 鏃ユ湡锛?026-02-11
            // System.Diagnostics.Debug.WriteLine("馃敟馃敟馃敟 [LinkReferencesPhase.Execute] 寮€濮嬫墽琛岋紒");
            
            // Performance Optimization: Pre-build lookup tables for O(1) reference resolution
            // This avoids O(n) linear searches through collections during reference linking
            // System.Diagnostics.Debug.WriteLine("[LinkReferencesPhase] Building lookup tables for O(1) reference resolution...");
            var lookupTables = BuildLookupTables(scenario);
            // System.Diagnostics.Debug.WriteLine($"[LinkReferencesPhase] Lookup tables built: {lookupTables.Persons.Count} persons, {lookupTables.Factions.Count} factions, {lookupTables.Architectures.Count} architectures, {lookupTables.MilitaryKinds.Count} militaryKinds");
            
            LinkReferences(scenario, lookupTables);
        }

        /// <summary>
        /// Link references using pre-built lookup tables
        /// Allows for external profiling of the linking process separate from table building
        /// </summary>
        /// <param name="scenario">The game scenario</param>
        /// <param name="lookupTables">Pre-built lookup tables</param>
        public void LinkReferences(GameScenario scenario, LookupTables lookupTables)
        {
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (lookupTables == null) throw new ArgumentNullException(nameof(lookupTables));

            // System.Diagnostics.Debug.WriteLine($"  - Scenario: {(scenario != null ? "瀛樺湪" : "null")}");
            // System.Diagnostics.Debug.WriteLine($"  - Troops: {scenario?.Troops?.Count ?? 0}");
            // System.Diagnostics.Debug.WriteLine($"  - Militaries: {scenario?.Militaries?.Count ?? 0}");
            // System.Diagnostics.Debug.WriteLine($"  - GameCommonData: {(scenario?.GameCommonData != null ? "瀛樺湪" : "null")}");
            // System.Diagnostics.Debug.WriteLine($"  - AllMilitaryKinds: {(scenario?.GameCommonData?.AllMilitaryKinds != null ? "瀛樺湪" : "null")}");
            
            // Link references in dependency order
            // 1. Persons (depend on Factions, Treasures, Architectures, Troops)
            LinkPersonReferences(scenario, lookupTables);
            
            // 2. Factions (depend on Persons, Architectures, Militaries, Legions, Troops, Sections)
            LinkFactionReferences(scenario, lookupTables);
            
            // 3. Architectures (depend on Factions, Sections, Persons, Militaries, Facilities)
            LinkArchitectureReferences(scenario, lookupTables);
            
            // 4. Legions (depend on Factions, Persons, Troops)
            LinkLegionReferences(scenario, lookupTables);
            
            // 馃敟 淇锛氭坊鍔?Military 閾炬帴姝ラ
            // 鏃ユ湡锛?026-02-10
            // 闂锛歁ilitary.Kind 寮曠敤浠庢潵娌℃湁琚摼鎺ヨ繃
            // 5. Militaries (depend on MilitaryKinds)
            LinkMilitaryReferences(scenario, lookupTables);
            
            // 6. Troops (depend on Factions, Legions, Architectures, Persons, Militaries)
            LinkTroopReferences(scenario, lookupTables);
            
            // 7. Sections (depend on Factions, Architectures)
            LinkSectionReferences(scenario, lookupTables);
            
            // 馃敟 淇锛氭坊鍔?Information 閾炬帴姝ラ
            // 鏃ユ湡锛?026-03-07
            // 闂锛欼nformation.BelongedFaction 鍜?BelongedArchitecture 寮曠敤浠庢湭琚摼鎺?
            // 8. Informations (depend on Factions, Architectures)
            LinkInformationReferences(scenario, lookupTables);
            
            // 馃敟 淇锛氭坊鍔?States 鍜?Regions 閾炬帴姝ラ
            // 鏃ユ湡锛?026-02-16
            // 闂锛歋tates 鍜?Regions 鐨勫叧鑱斿叧绯讳粠鏈閾炬帴
            // 9. States and Regions (depend on Architectures)
            LinkStatesAndRegions(scenario, lookupTables);
            
            // 馃敟 淇锛氫负娌℃湁 LinkedRegion 鐨?State 鍒涘缓榛樿 Region
            // 鏃ユ湡锛?026-02-16
            // 闂锛氭棫瀛樻。娌℃湁 Region 鏁版嵁锛屽鑷?State.LinkedRegion 涓?null
            // 瑙ｅ喅锛氳嚜鍔ㄤ负姣忎釜 State 鍒涘缓瀵瑰簲鐨?Region
            EnsureStatesHaveRegions(scenario);
            
            // 馃敟 璇婃柇锛氶獙璇侀摼鎺ョ粨鏋?
            // 鏃ユ湡锛?026-02-11
            int nullArmyCount = 0;
            int nullKindCount = 0;
            foreach (Troop troop in scenario.Troops.GetList())
            {
                if (troop.Army == null) nullArmyCount++;
                else if (troop.Army.Kind == null) nullKindCount++;
            }
            System.Diagnostics.Debug.WriteLine($"馃敟 [LinkReferencesPhase] 閾炬帴瀹屾垚鍚庣粺璁?");
            System.Diagnostics.Debug.WriteLine($"  - Troop.Army 涓?null: {nullArmyCount} / {scenario.Troops.Count}");
            System.Diagnostics.Debug.WriteLine($"  - Troop.Army.Kind 涓?null: {nullKindCount} / {scenario.Troops.Count}");
            
            // 8. Rebuild caches after all references are linked
            // System.Diagnostics.Debug.WriteLine("[LinkReferencesPhase] Rebuilding caches...");
            scenario.CreatePersonStatusCache();
            scenario.CreatePersonWorkCache();
            // System.Diagnostics.Debug.WriteLine("[LinkReferencesPhase] 鉁?Caches rebuilt");
            // System.Diagnostics.Debug.WriteLine("馃敟馃敟馃敟 [LinkReferencesPhase.Execute] 鎵ц瀹屾垚锛?);
        }
        
        /// <summary>
        /// Build lookup tables for all game object collections
        /// This enables O(1) lookups instead of O(n) linear searches
        /// </summary>
        public LookupTables BuildLookupTables(GameScenario scenario)
        {
            var tables = new LookupTables();
            
            // Build Person lookup table
            if (scenario.Persons != null && scenario.Persons.Count > 0)
            {
                foreach (Person person in scenario.Persons.GetList())
                {
                    if (person != null && !tables.Persons.ContainsKey(person.ID))
                    {
                        tables.Persons[person.ID] = person;
                    }
                }
            }
            
            // Build Faction lookup table
            if (scenario.Factions != null && scenario.Factions.Count > 0)
            {
                foreach (Faction faction in scenario.Factions.GetList())
                {
                    if (faction != null && !tables.Factions.ContainsKey(faction.ID))
                    {
                        tables.Factions[faction.ID] = faction;
                    }
                }
            }
            
            // Build Architecture lookup table
            if (scenario.Architectures != null && scenario.Architectures.Count > 0)
            {
                foreach (Architecture architecture in scenario.Architectures.GetList())
                {
                    if (architecture != null && !tables.Architectures.ContainsKey(architecture.ID))
                    {
                        tables.Architectures[architecture.ID] = architecture;
                    }
                }
            }
            
            // Build ArchitectureKind lookup table
            // 馃敟 淇锛氭坊鍔犺缁嗚瘖鏂?
            // 鏃ユ湡锛?026-02-10
            // System.Diagnostics.Debug.WriteLine("[BuildLookupTables] 寮€濮嬫瀯寤?ArchitectureKind 鏌ユ壘琛?..");
            // System.Diagnostics.Debug.WriteLine($"  - scenario.GameCommonData: {(scenario.GameCommonData != null ? "瀛樺湪" : "null")}");
            // System.Diagnostics.Debug.WriteLine($"  - AllArchitectureKinds: {(scenario.GameCommonData?.AllArchitectureKinds != null ? "瀛樺湪" : "null")}");
            // System.Diagnostics.Debug.WriteLine($"  - ArchitectureKinds瀛楀吀: {(scenario.GameCommonData?.AllArchitectureKinds?.ArchitectureKinds != null ? "瀛樺湪" : "null")}");
            
            if (scenario.GameCommonData?.AllArchitectureKinds?.ArchitectureKinds != null)
            {
                System.Diagnostics.Debug.WriteLine($"  - ArchitectureKinds.Count: {scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Count}");
                
                foreach (var kvp in scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds)
                {
                    if (kvp.Value != null && !tables.ArchitectureKinds.ContainsKey(kvp.Key))
                    {
                        tables.ArchitectureKinds[kvp.Key] = kvp.Value;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"  - 鏋勫缓瀹屾垚锛屾煡鎵捐〃鍖呭惈 {tables.ArchitectureKinds.Count} 涓?ArchitectureKind");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("  - [WARN] ArchitectureKinds is null; lookup table is empty.");
            }
            
            // Build Treasure lookup table
            // 馃敟 璇婃柇锛氱‘璁ゅ疂鐗╂煡鎵捐〃鏄惁姝ｇ‘鏋勫缓
            // 鏃ユ湡锛?026-03-17
            System.Diagnostics.Debug.WriteLine("[BuildLookupTables] 寮€濮嬫瀯寤?Treasure 鏌ユ壘琛?..");
            System.Diagnostics.Debug.WriteLine($"  - scenario.Treasures: {(scenario.Treasures != null ? "瀛樺湪" : "null")}");
            System.Diagnostics.Debug.WriteLine($"  - scenario.Treasures.Count: {scenario.Treasures?.Count ?? 0}");
            
            if (scenario.Treasures != null && scenario.Treasures.Count > 0)
            {
                foreach (Treasure treasure in scenario.Treasures.GetList())
                {
                    if (treasure != null && !tables.Treasures.ContainsKey(treasure.ID))
                    {
                        tables.Treasures[treasure.ID] = treasure;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"  - 鏋勫缓瀹屾垚锛屾煡鎵捐〃鍖呭惈 {tables.Treasures.Count} 涓?Treasure");
                
                // 鏄剧ず鍓?涓疂鐗╃殑ID
                var first5 = tables.Treasures.Keys.Take(5).ToList();
                System.Diagnostics.Debug.WriteLine($"  - 鍓?涓疂鐗㊣D: {string.Join(", ", first5)}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("  - [WARN] Treasures is empty; lookup table is empty.");
            }
            
            // Build Legion lookup table
            if (scenario.Legions != null && scenario.Legions.Count > 0)
            {
                foreach (Legion legion in scenario.Legions.GetList())
                {
                    if (legion != null && !tables.Legions.ContainsKey(legion.ID))
                    {
                        tables.Legions[legion.ID] = legion;
                    }
                }
            }
            
            // Build Troop lookup table
            if (scenario.Troops != null && scenario.Troops.Count > 0)
            {
                foreach (Troop troop in scenario.Troops.GetList())
                {
                    if (troop != null && !tables.Troops.ContainsKey(troop.ID))
                    {
                        tables.Troops[troop.ID] = troop;
                    }
                }
            }
            
            // Build Section lookup table
            if (scenario.Sections != null && scenario.Sections.Count > 0)
            {
                foreach (Section section in scenario.Sections.GetList())
                {
                    if (section != null && !tables.Sections.ContainsKey(section.ID))
                    {
                        tables.Sections[section.ID] = section;
                    }
                }
            }
            
            // Build Military lookup table
            if (scenario.Militaries != null && scenario.Militaries.Count > 0)
            {
                foreach (Military military in scenario.Militaries.GetList())
                {
                    if (military != null && !tables.Militaries.ContainsKey(military.ID))
                    {
                        tables.Militaries[military.ID] = military;
                    }
                }
            }
            
            // Build Facility lookup table
            // 馃敟 2026-03-16 璇婃柇锛氱‘璁よ鏂芥暟鎹槸鍚︽纭姞杞?
            // System.Diagnostics.Debug.WriteLine("[BuildLookupTables] 寮€濮嬫瀯寤?Facility 鏌ユ壘琛?..");
            // System.Diagnostics.Debug.WriteLine($"  - scenario.Facilities: {(scenario.Facilities != null ? "瀛樺湪" : "null")}");
            // System.Diagnostics.Debug.WriteLine($"  - scenario.Facilities.Count: {scenario.Facilities?.Count ?? 0}");
            
            if (scenario.Facilities != null && scenario.Facilities.Count > 0)
            {
                foreach (Facility facility in scenario.Facilities.GetList())
                {
                    if (facility != null && !tables.Facilities.ContainsKey(facility.ID))
                    {
                        tables.Facilities[facility.ID] = facility;
                    }
                }
                
                // System.Diagnostics.Debug.WriteLine($"  - 鏋勫缓瀹屾垚锛屾煡鎵捐〃鍖呭惈 {tables.Facilities.Count} 涓?Facility");
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine("  - 鈿狅笍 璀﹀憡锛欶acilities 鏁版嵁涓虹┖锛屾煡鎵捐〃涓虹┖锛?);
            }
            
            // 馃敟 淇锛氭瀯寤?MilitaryKind 鏌ユ壘琛?
            // 鏃ユ湡锛?026-02-10
            // 闂锛歁ilitary.Kind 寮曠敤浠庢潵娌℃湁琚摼鎺ヨ繃
            // System.Diagnostics.Debug.WriteLine("[BuildLookupTables] 寮€濮嬫瀯寤?MilitaryKind 鏌ユ壘琛?..");
            // System.Diagnostics.Debug.WriteLine($"  - scenario.GameCommonData: {(scenario.GameCommonData != null ? "瀛樺湪" : "null")}");
            // System.Diagnostics.Debug.WriteLine($"  - AllMilitaryKinds: {(scenario.GameCommonData?.AllMilitaryKinds != null ? "瀛樺湪" : "null")}");
            // System.Diagnostics.Debug.WriteLine($"  - MilitaryKinds瀛楀吀: {(scenario.GameCommonData?.AllMilitaryKinds?.MilitaryKinds != null ? "瀛樺湪" : "null")}");
            
            if (scenario.GameCommonData?.AllMilitaryKinds?.MilitaryKinds != null)
            {
                // System.Diagnostics.Debug.WriteLine($"  - MilitaryKinds.Count: {scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count}");
                
                foreach (var kvp in scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds)
                {
                    if (kvp.Value != null && !tables.MilitaryKinds.ContainsKey(kvp.Key))
                    {
                        tables.MilitaryKinds[kvp.Key] = kvp.Value;
                    }
                }
                
                // System.Diagnostics.Debug.WriteLine($"  - 鏋勫缓瀹屾垚锛屾煡鎵捐〃鍖呭惈 {tables.MilitaryKinds.Count} 涓?MilitaryKind");
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine("  - 鈿狅笍 璀﹀憡锛歁ilitaryKinds 鏁版嵁涓?null锛屾煡鎵捐〃涓虹┖锛?);
            }
            
            // 馃敟 淇锛氭瀯寤?SectionAIDetail 鏌ユ壘琛?
            // 鏃ユ湡锛?026-02-13
            // 闂锛歋ection.AIDetail 寮曠敤浠庢潵娌℃湁琚摼鎺ヨ繃锛屽鑷磋妗ｅ悗鐜╁鍔垮姏琚?AI 鎺у埗
            // System.Diagnostics.Debug.WriteLine("[BuildLookupTables] 寮€濮嬫瀯寤?SectionAIDetail 鏌ユ壘琛?..");
            // System.Diagnostics.Debug.WriteLine($"  - scenario.GameCommonData: {(scenario.GameCommonData != null ? "瀛樺湪" : "null")}");
            // System.Diagnostics.Debug.WriteLine($"  - AllSectionAIDetails: {(scenario.GameCommonData?.AllSectionAIDetails != null ? "瀛樺湪" : "null")}");
            // System.Diagnostics.Debug.WriteLine($"  - SectionAIDetails瀛楀吀: {(scenario.GameCommonData?.AllSectionAIDetails?.SectionAIDetails != null ? "瀛樺湪" : "null")}");
            
            if (scenario.GameCommonData?.AllSectionAIDetails?.SectionAIDetails != null)
            {
                // System.Diagnostics.Debug.WriteLine($"  - SectionAIDetails.Count: {scenario.GameCommonData.AllSectionAIDetails.SectionAIDetails.Count}");
                
                foreach (var kvp in scenario.GameCommonData.AllSectionAIDetails.SectionAIDetails)
                {
                    if (kvp.Value != null)
                    {
                        tables.SectionAIDetails[kvp.Key] = kvp.Value;
                        // System.Diagnostics.Debug.WriteLine($"    - ID={kvp.Key}: {kvp.Value.Description}, AutoRun={kvp.Value.AutoRun}");
                    }
                }
                
                // System.Diagnostics.Debug.WriteLine($"  - 鏋勫缓瀹屾垚锛屾煡鎵捐〃鍖呭惈 {tables.SectionAIDetails.Count} 涓?SectionAIDetail");
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine("  - 鈿狅笍 璀﹀憡锛歋ectionAIDetails 鏁版嵁涓?null锛屾煡鎵捐〃涓虹┖锛?);
            }
            
            // 馃敟 淇锛氭瀯寤?State 鏌ユ壘琛?
            // 鏃ユ湡锛?026-02-16
            // 闂锛歋tates 闆嗗悎娌℃湁琚簭鍒楀寲锛屽鑷磋妗ｅ悗 LocationState 涓?null
            // System.Diagnostics.Debug.WriteLine("[BuildLookupTables] 寮€濮嬫瀯寤?State 鏌ユ壘琛?..");
            if (scenario.States != null && scenario.States.Count > 0)
            {
                // System.Diagnostics.Debug.WriteLine($"  - States.Count: {scenario.States.Count}");
                
                foreach (State state in scenario.States.GetList())
                {
                    if (state != null && !tables.States.ContainsKey(state.ID))
                    {
                        tables.States[state.ID] = state;
                    }
                }
                
                // System.Diagnostics.Debug.WriteLine($"  - 鏋勫缓瀹屾垚锛屾煡鎵捐〃鍖呭惈 {tables.States.Count} 涓?State");
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine("  - 鈿狅笍 璀﹀憡锛歋tates 闆嗗悎涓虹┖锛?);
            }
            
            // 馃敟 淇锛氭瀯寤?Region 鏌ユ壘琛?
            // 鏃ユ湡锛?026-02-16
            // System.Diagnostics.Debug.WriteLine("[BuildLookupTables] 寮€濮嬫瀯寤?Region 鏌ユ壘琛?..");
            if (scenario.Regions != null && scenario.Regions.Count > 0)
            {
                // System.Diagnostics.Debug.WriteLine($"  - Regions.Count: {scenario.Regions.Count}");
                
                foreach (Region region in scenario.Regions.GetList())
                {
                    if (region != null && !tables.Regions.ContainsKey(region.ID))
                    {
                        tables.Regions[region.ID] = region;
                    }
                }
                
                // System.Diagnostics.Debug.WriteLine($"  - 鏋勫缓瀹屾垚锛屾煡鎵捐〃鍖呭惈 {tables.Regions.Count} 涓?Region");
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine("  - 鈿狅笍 璀﹀憡锛歊egions 闆嗗悎涓虹┖锛?);
            }
            
            return tables;
        }
        
        /// <summary>
        /// Link references for all Persons
        /// </summary>
        private void LinkPersonReferences(GameScenario scenario, LookupTables lookupTables)
        {
            if (scenario.Persons == null || scenario.Persons.Count == 0)
                return;
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LinkPersonReferences] 寮€濮嬮摼鎺?{scenario.Persons.Count} 涓汉鐗╃殑寮曠敤...");
            #endif
            
            foreach (Person person in scenario.Persons.GetList())
            {
                _personLinker.LinkReferences(person, scenario, lookupTables);
            }
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LinkPersonReferences] Linked {scenario.Persons.Count} persons.");
            #endif
        }
        
        /// <summary>
        /// Link references for all Factions
        /// </summary>
        private void LinkFactionReferences(GameScenario scenario, LookupTables lookupTables)
        {
            if (scenario.Factions == null || scenario.Factions.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LinkFactionReferences] 鈿狅笍 Factions 闆嗗悎涓虹┖");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LinkFactionReferences] 寮€濮嬮摼鎺?{scenario.Factions.Count} 涓娍鍔涚殑寮曠敤...");
            System.Diagnostics.Debug.WriteLine($"[LinkFactionReferences] lookupTables.Persons.Count = {lookupTables.Persons.Count}");
            System.Diagnostics.Debug.WriteLine($"[LinkFactionReferences] lookupTables.Architectures.Count = {lookupTables.Architectures.Count}");
            
            int linkedLeaderCount = 0;
            int linkedCapitalCount = 0;
            
            foreach (Faction faction in scenario.Factions.GetList())
            {
                System.Diagnostics.Debug.WriteLine($"[LinkFactionReferences] 澶勭悊鍔垮姏 {faction.ID} ({faction.Name}): LeaderID={faction.LeaderID}, CapitalID={faction.CapitalID}");
                
                _factionLinker.LinkReferences(faction, scenario, lookupTables);
                
                if (faction.Leader != null) linkedLeaderCount++;
                if (faction.Capital != null) linkedCapitalCount++;
                
                System.Diagnostics.Debug.WriteLine($"[LinkFactionReferences]   缁撴灉: Leader={faction.Leader?.Name ?? "null"}, Capital={faction.Capital?.Name ?? "null"}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[LinkFactionReferences] 鉁?瀹屾垚: Leader 閾炬帴 {linkedLeaderCount}/{scenario.Factions.Count}, Capital 閾炬帴 {linkedCapitalCount}/{scenario.Factions.Count}");
        }
        
        /// <summary>
        /// Link references for all Architectures
        /// </summary>
        private void LinkArchitectureReferences(GameScenario scenario, LookupTables lookupTables)
        {
            if (scenario.Architectures == null || scenario.Architectures.Count == 0)
                return;
            
            foreach (Architecture architecture in scenario.Architectures.GetList())
            {
                _architectureLinker.LinkReferences(architecture, scenario, lookupTables);
            }
        }
        
        /// <summary>
        /// Link references for all Legions
        /// </summary>
        private void LinkLegionReferences(GameScenario scenario, LookupTables lookupTables)
        {
            if (scenario.Legions == null || scenario.Legions.Count == 0)
                return;
            
            foreach (Legion legion in scenario.Legions.GetList())
            {
                // Use extension method for O(1) lookups
                _legionLinker.LinkReferences(legion, scenario, lookupTables);
            }
        }
        
        /// <summary>
        /// 馃敟 淇锛歀ink references for all Militaries
        /// 鏃ユ湡锛?026-02-10
        /// 闂锛歁ilitary.Kind 寮曠敤浠庢潵娌℃湁琚摼鎺ヨ繃
        /// </summary>
        private void LinkMilitaryReferences(GameScenario scenario, LookupTables lookupTables)
        {
            if (scenario.Militaries == null || scenario.Militaries.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LinkMilitaryReferences] 鈿狅笍 璀﹀憡锛歁ilitaries 闆嗗悎涓虹┖");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] 寮€濮嬮摼鎺?{scenario.Militaries.Count} 涓?Military 鐨勫紩鐢?..");
            
            int kindLinkedCount = 0;
            int kindFailedCount = 0;
            int archLinkedCount = 0;
            int archFailedCount = 0;
            
            foreach (Military military in scenario.Militaries.GetList())
            {
                if (military == null)
                    continue;
                
                // Link Kind reference
                // 馃敟 鏍规湰淇锛欿indID=0 鏄湁鏁堢殑锛堟鍏甸槦锛夛紝蹇呴』鐢?>= 0
                if (military.KindID >= 0)
                {
                    if (lookupTables.MilitaryKinds.TryGetValue(military.KindID, out MilitaryKind kind))
                    {
                        military.Kind = kind;
                        kindLinkedCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] 鈿狅笍 璀﹀憡锛歁ilitary {military.ID} ({military.Name}) 寮曠敤浜嗕笉瀛樺湪鐨?MilitaryKind {military.KindID}");
                        System.Diagnostics.Debug.WriteLine($"  - 鍙敤鐨?MilitaryKind IDs: {string.Join(", ", lookupTables.MilitaryKinds.Keys.Take(10))}...");
                        // 娉ㄦ剰锛氫笉鍐嶈缃?military.Kind = null锛屽洜涓?Kind setter 浼氭妸 kindID 鏀逛负 -1锛岀牬鍧忓師濮嬫暟鎹?
                        kindFailedCount++;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] 鈿狅笍 璀﹀憡锛歁ilitary {military.ID} ({military.Name}) 鐨?KindID 涓?{military.KindID}锛岃烦杩嘖ind閾炬帴");
                    // 娉ㄦ剰锛氫笉鍐嶈缃?military.Kind = null锛岄伩鍏?Kind setter 鎶?kindID 鏀逛负 -1
                    kindFailedCount++;
                }
                
                // 馃敟 鏍规湰淇锛歀ink BelongedArchitecture reference
                // 娉ㄦ剰锛欰rchitecture ID 鍙兘浠?0 寮€濮嬶紝鎵€浠ヤ娇鐢?>= 0
                if (military.BelongedArchitectureID >= 0)
                {
                    if (lookupTables.Architectures.TryGetValue(military.BelongedArchitectureID, out Architecture architecture))
                    {
                        military.BelongedArchitecture = architecture;
                        archLinkedCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] 鈿狅笍 璀﹀憡锛歁ilitary {military.ID} ({military.Name}) 寮曠敤浜嗕笉瀛樺湪鐨?Architecture {military.BelongedArchitectureID}");
                        military.BelongedArchitectureID = -1;
                        military.BelongedArchitecture = null;
                        archFailedCount++;
                    }
                }
            }
            
            // 馃敟 2026-02-11 鏂板锛歀ink Leader reference
            int leaderLinkedCount = 0, leaderFailedCount = 0;
            int followedLeaderLinkedCount = 0, followedLeaderFailedCount = 0;
            int targetArchLinkedCount = 0, targetArchFailedCount = 0;
            int startingArchLinkedCount = 0, startingArchFailedCount = 0;
            
            foreach (Military military in scenario.Militaries.GetList())
            {
                if (military == null) continue;
                
                // Link Leader reference
                if (military.LeaderID > 0)
                {
                    if (lookupTables.Persons.TryGetValue(military.LeaderID, out Person leader))
                    {
                        military.Leader = leader;
                        leaderLinkedCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] 鈿狅笍 璀﹀憡锛歁ilitary {military.ID} ({military.Name}) 寮曠敤浜嗕笉瀛樺湪鐨?Leader {military.LeaderID}");
                        military.LeaderID = -1;
                        military.Leader = null;
                        leaderFailedCount++;
                    }
                }
                
                // Link FollowedLeader reference
                if (military.FollowedLeaderID > 0)
                {
                    if (lookupTables.Persons.TryGetValue(military.FollowedLeaderID, out Person followedLeader))
                    {
                        military.FollowedLeader = followedLeader;
                        followedLeaderLinkedCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] 鈿狅笍 璀﹀憡锛歁ilitary {military.ID} ({military.Name}) 寮曠敤浜嗕笉瀛樺湪鐨?FollowedLeader {military.FollowedLeaderID}");
                        military.FollowedLeaderID = -1;
                        military.FollowedLeader = null;
                        followedLeaderFailedCount++;
                    }
                }
                
                // Link TargetArchitecture reference
                // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛屽繀椤讳娇鐢?>= 0
                // ANTI-BAND-AID锛氬鏋滃紩鐢ㄤ笉瀛樺湪锛屾姏鍑哄紓甯歌€屼笉鏄潤榛樻竻绌?
                if (military.TargetArchitectureID >= 0)
                {
                    if (lookupTables.Architectures.TryGetValue(military.TargetArchitectureID, out Architecture targetArch))
                    {
                        military.TargetArchitecture = targetArch;
                        targetArchLinkedCount++;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Data corruption: Military {military.ID} ({military.Name}) references missing TargetArchitecture {military.TargetArchitectureID}.");
                    }
                }
                
                // Link StartingArchitecture reference
                // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛屽繀椤讳娇鐢?>= 0
                // ANTI-BAND-AID锛氬鏋滃紩鐢ㄤ笉瀛樺湪锛屾姏鍑哄紓甯歌€屼笉鏄潤榛樻竻绌?
                if (military.StartingArchitectureID >= 0)
                {
                    if (lookupTables.Architectures.TryGetValue(military.StartingArchitectureID, out Architecture startingArch))
                    {
                        military.StartingArchitecture = startingArch;
                        startingArchLinkedCount++;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Data corruption: Military {military.ID} ({military.Name}) references missing StartingArchitecture {military.StartingArchitectureID}.");
                    }
                }
            }
            // 馃敟 2026-02-11 鏂板锛歀ink ShelledMilitary reference锛堝繀椤诲湪鎵€鏈?Military 鍔犺浇鍚庯級
            // 闂锛歋helledMilitary 寰幆寮曠敤瀵艰嚧鏍堟孩鍑?
            // 瑙ｅ喅锛氫娇鐢?ShelledMilitaryID 妫€娴嬪惊鐜紝閬垮厤璁块棶 ShelledMilitary 瀛楁
            // 馃敟 鎬ц兘浼樺寲锛氱Щ闄ゅ惊鐜唴鐨?Debug.WriteLine锛岄伩鍏?I/O 闃诲
            int shelledLinkedCount = 0, shelledFailedCount = 0;

            foreach (Military military in scenario.Militaries.GetList())
            {
                if (military == null) continue;

                // Link ShelledMilitary reference
                // 馃敟 鏍规湰淇锛氬彧澶勭悊鏈夋晥鐨?ShelledMilitaryID锛? 0锛?
                if (military.ShelledMilitaryID > 0)
                {
                    if (lookupTables.Militaries.TryGetValue(military.ShelledMilitaryID, out Military shelledMilitary))
                    {
                        // 馃敟 鍏抽敭淇锛氫娇鐢?ID 妫€娴嬪惊鐜紩鐢紝閬垮厤璁块棶 ShelledMilitary 瀛楁
                        bool wouldCreateCycle = false;
                        
                        // 妫€鏌?锛氱洿鎺ヨ嚜寮曠敤锛圓 鈫?A锛?
                        if (shelledMilitary.ID == military.ID)
                        {
                            wouldCreateCycle = true;
                        }
                        // 妫€鏌?锛氬弻鍚戝惊鐜紙A 鈫?B 涓?B 鈫?A锛?
                        else if (shelledMilitary.ShelledMilitaryID == military.ID)
                        {
                            wouldCreateCycle = true;
                        }
                        // 妫€鏌?锛氬灞傚惊鐜紙A 鈫?B 鈫?C 鈫?... 鈫?A锛?
                        else
                        {
                            // 浣跨敤 ID 杩借釜閾炬潯锛岄伩鍏嶈闂?ShelledMilitary 瀛楁
                            int currentID = shelledMilitary.ShelledMilitaryID;
                            int depth = 1;
                            const int maxDepth = 100;
                            HashSet<int> visited = [military.ID, shelledMilitary.ID];
                            
                            while (currentID > 0 && depth < maxDepth)
                            {
                                if (currentID == military.ID)
                                {
                                    // 妫€娴嬪埌寰幆锛氶摼鏉℃渶缁堟寚鍚?military 鑷繁
                                    wouldCreateCycle = true;
                                    break;
                                }
                                
                                if (visited.Contains(currentID))
                                {
                                    // 妫€娴嬪埌鍏朵粬寰幆锛堜笉娑夊強 military锛屼絾浠嶇劧鏄惊鐜級
                                    wouldCreateCycle = true;
                                    break;
                                }
                                
                                visited.Add(currentID);
                                
                                // 浣跨敤 lookupTables 鏌ユ壘涓嬩竴涓?Military 鐨?ShelledMilitaryID
                                if (lookupTables.Militaries.TryGetValue(currentID, out Military nextMilitary))
                                {
                                    currentID = nextMilitary.ShelledMilitaryID;
                                }
                                else
                                {
                                    // 閾炬潯鏂锛堝紩鐢ㄤ簡涓嶅瓨鍦ㄧ殑 Military锛?
                                    break;
                                }
                                
                                depth++;
                            }
                            
                            if (depth >= maxDepth)
                            {
                                wouldCreateCycle = true;
                            }
                        }
                        
                        if (wouldCreateCycle)
                        {
                            military.ShelledMilitaryID = 0;
                            military.ShelledMilitary = null;
                            shelledFailedCount++;
                        }
                        else
                        {
                            military.ShelledMilitary = shelledMilitary;
                            shelledLinkedCount++;
                        }
                    }
                    else
                    {
                        military.ShelledMilitaryID = 0;
                        military.ShelledMilitary = null;
                        shelledFailedCount++;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] Kind linked={kindLinkedCount}, failed={kindFailedCount}");
            System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] BelongedArchitecture linked={archLinkedCount}, failed={archFailedCount}");
            System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] Leader linked={leaderLinkedCount}, failed={leaderFailedCount}");
            System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] FollowedLeader linked={followedLeaderLinkedCount}, failed={followedLeaderFailedCount}");
            System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] TargetArchitecture linked={targetArchLinkedCount}, failed={targetArchFailedCount}");
            System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] StartingArchitecture linked={startingArchLinkedCount}, failed={startingArchFailedCount}");
            System.Diagnostics.Debug.WriteLine($"[LinkMilitaryReferences] ShelledMilitary linked={shelledLinkedCount}, failed={shelledFailedCount}");
        }
        
        /// <summary>
        /// Link references for all Troops
        /// 馃敟 鎬ц兘浼樺寲锛氱Щ闄ゅ惊鐜唴鐨?Debug.WriteLine锛岄伩鍏?I/O 闃诲
        /// </summary>
        private void LinkTroopReferences(GameScenario scenario, LookupTables lookupTables)
        {
            if (scenario.Troops == null || scenario.Troops.Count == 0)
                return;
            
            System.Diagnostics.Debug.WriteLine($"[LinkTroopReferences] 寮€濮嬮摼鎺?{scenario.Troops.Count} 涓?Troop 鐨勫紩鐢?..");
            
            int armyLinkedCount = 0, armyFailedCount = 0;
            
            foreach (Troop troop in scenario.Troops.GetList())
            {
                // 馃敟 鎬ц兘浼樺寲锛氱Щ闄ゅ惊鐜唴鐨勮瘖鏂棩蹇楋紙I/O 闃诲锛?
                // 濡傞渶璋冭瘯鐗瑰畾 Troop锛屽湪寰幆澶栧崟鐙鐞?
                
                // Use extension method for O(1) lookups
                _troopLinker.LinkReferences(troop, scenario, lookupTables);
                
                // 缁熻 Army 閾炬帴缁撴灉
                if (troop.Army != null)
                    armyLinkedCount++;
                else if (troop.MilitaryID > 0)
                    armyFailedCount++;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LinkTroopReferences] Army linked={armyLinkedCount}, failed={armyFailedCount}");
        }
        
        /// <summary>
        /// Link references for all Sections
        /// </summary>
        private void LinkSectionReferences(GameScenario scenario, LookupTables lookupTables)
        {
            if (scenario.Sections == null || scenario.Sections.Count == 0)
                return;
            
            foreach (Section section in scenario.Sections.GetList())
            {
                // Use extension method for O(1) lookups
                _sectionLinker.LinkReferences(section, scenario, lookupTables);
            }
        }
        
        /// <summary>
        /// Link references for all Informations
        /// 馃敟 淇锛氭仮澶?Information.BelongedFaction 鍜?BelongedArchitecture 寮曠敤
        /// 鏃ユ湡锛?026-03-07
        /// </summary>
        private void LinkInformationReferences(GameScenario scenario, LookupTables lookupTables)
        {
            if (scenario.Informations == null || scenario.Informations.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LinkInformationReferences] 鈿狅笍 Informations 鍒楄〃涓虹┖");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[LinkInformationReferences] 寮€濮嬮摼鎺?{scenario.Informations.Count} 涓儏鎶ュ紩鐢?..");
            
            int linkedFactionCount = 0;
            int linkedArchitectureCount = 0;
            int failedCount = 0;
            
            foreach (Information info in scenario.Informations.GetList())
            {
                bool success = true;
                
                // Link BelongedFaction
                if (info.BelongedFactionID > 0)
                {
                    if (lookupTables.Factions.TryGetValue(info.BelongedFactionID, out var faction))
                    {
                        info.BelongedFaction = faction;
                        linkedFactionCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LinkInformationReferences] Missing faction reference: info={info.ID}, factionID={info.BelongedFactionID}");
                        success = false;
                    }
                }
                
                // Link BelongedArchitecture
                if (info.BelongedArchitectureID > 0)
                {
                    if (lookupTables.Architectures.TryGetValue(info.BelongedArchitectureID, out var architecture))
                    {
                        info.BelongedArchitecture = architecture;
                        linkedArchitectureCount++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LinkInformationReferences] Missing architecture reference: info={info.ID}, architectureID={info.BelongedArchitectureID}");
                        success = false;
                    }
                }
                
                if (!success)
                {
                    failedCount++;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LinkInformationReferences] 鉁?瀹屾垚閾炬帴:");
            System.Diagnostics.Debug.WriteLine($"  - 閾炬帴鍔垮姏: {linkedFactionCount}");
            System.Diagnostics.Debug.WriteLine($"  - 閾炬帴寤虹瓚: {linkedArchitectureCount}");
            System.Diagnostics.Debug.WriteLine($"  - 澶辫触: {failedCount}");
        }
        
        /// <summary>
        /// Link references for all States and Regions
        /// 馃敟 淇锛氭仮澶?States 鍜?Regions 鐨勫叧鑱斿叧绯?
        /// 鏃ユ湡锛?026-02-16
        /// </summary>
        private void LinkStatesAndRegions(GameScenario scenario, LookupTables lookupTables)
        {
            System.Diagnostics.Debug.WriteLine("[LinkStatesAndRegions] 寮€濮嬮摼鎺?States 鍜?Regions 寮曠敤...");
            
            // Link States
            if (scenario.States != null && scenario.States.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[LinkStatesAndRegions] 閾炬帴 {scenario.States.Count} 涓窞鍩?..");
                
                foreach (State state in scenario.States.GetList())
                {
                    // Link LinkedRegion
                    // 馃敟 ID 鍒ゆ柇淇锛欼D=0 鏄湁鏁堢殑锛堜腑鍘熷湴鍩?ID=0锛?
                    // 鏃ユ湡锛?026-03-17
                    if (state.LinkedRegionID >= 0 && lookupTables.Regions.TryGetValue(state.LinkedRegionID, out var region))
                    {
                        state.LinkedRegion = region;
                    }
                    
                    // Link ContactStates
                    if (state.ContactStateIDs != null && state.ContactStateIDs.Count > 0)
                    {
                        state.ContactStates.Clear();
                        foreach (int contactStateID in state.ContactStateIDs)
                        {
                            if (lookupTables.States.TryGetValue(contactStateID, out var contactState))
                            {
                                state.ContactStates.Add(contactState);
                            }
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[LinkStatesAndRegions] 鉁?States 閾炬帴瀹屾垚");
            }
            
            // Link Regions - 鍙摼鎺?RegionCore锛屼笉璋冪敤 LoadStatesFromString
            if (scenario.Regions != null && scenario.Regions.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[LinkStatesAndRegions] 閾炬帴 {scenario.Regions.Count} 涓湴鍩?..");
                
                foreach (Region region in scenario.Regions.GetList())
                {
                    // 馃敟 淇锛氬彧閾炬帴 RegionCore锛屼笉瑕佽皟鐢?LoadStatesFromString
                    // 鍘熷洜锛歀oadStatesFromString 鍙兘浼氫慨鏀规暟鎹紝褰卞搷鍚庣画鐨?ApplyInfluences
                    // 馃敟 ID 鍒ゆ柇淇锛欼D=0 鏄湁鏁堢殑锛堟礇闃?ID=0锛?
                    // 鏃ユ湡锛?026-03-17
                    if (region.RegionCoreID >= 0 && lookupTables.Architectures.TryGetValue(region.RegionCoreID, out var regionCore))
                    {
                        region.RegionCore = regionCore;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[LinkStatesAndRegions] 鉁?Regions 閾炬帴瀹屾垚");
            }
        }
        
        /// <summary>
        /// 馃敟 淇锛氱‘淇濇墍鏈?State 閮芥湁 LinkedRegion
        /// 鏃ユ湡锛?026-02-16
        /// 闂锛氭棫瀛樻。娌℃湁 Region 鏁版嵁锛屽鑷?State.LinkedRegion 涓?null
        /// 瑙ｅ喅锛氫负姣忎釜娌℃湁 LinkedRegion 鐨?State 鑷姩鍒涘缓瀵瑰簲鐨?Region
        /// </summary>
        private void EnsureStatesHaveRegions(GameScenario scenario)
        {
            if (scenario.States == null || scenario.States.Count == 0)
                return;
            
            System.Diagnostics.Debug.WriteLine("[EnsureStatesHaveRegions] 妫€鏌?States 鏄惁閮芥湁 LinkedRegion...");
            
            int createdCount = 0;
            int nextRegionID = scenario.Regions.Count > 0 
                ? scenario.Regions.GetList().Cast<Region>().Max(r => r.ID) + 1 
                : 1;
            
            foreach (State state in scenario.States.GetList())
            {
                if (state.LinkedRegion == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnsureStatesHaveRegions] State {state.ID} ({state.Name}) 娌℃湁 LinkedRegion锛岃嚜鍔ㄥ垱寤?..");
                    
                    // 鍒涘缓瀵瑰簲鐨?Region
                    var region = new Region
                    {
                        ID = nextRegionID++,
                        Name = $"{state.Name}鍦板煙"  // 渚嬪锛?骞藉窞" 鈫?"骞藉窞鍦板煙"
                    };
                    
                    region.Init();
                    scenario.Regions.Add(region);
                    
                    // 閾炬帴 State 鍜?Region
                    state.LinkedRegion = region;
                    state.LinkedRegionID = region.ID;
                    
                    createdCount++;
                }
            }
            
            if (createdCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureStatesHaveRegions] 鉁?涓?{createdCount} 涓?State 鍒涘缓浜嗛粯璁?Region");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureStatesHaveRegions] 鉁?鎵€鏈?State 閮芥湁 LinkedRegion");
            }
        }
    }
    
    /// <summary>
    /// Lookup tables for O(1) reference resolution
    /// Pre-built dictionaries for all game object collections
    /// </summary>
    public class LookupTables
    {
        public Dictionary<int, Person> Persons { get; } = new Dictionary<int, Person>();
        public Dictionary<int, Faction> Factions { get; } = new Dictionary<int, Faction>();
        public Dictionary<int, Architecture> Architectures { get; } = new Dictionary<int, Architecture>();
        public Dictionary<int, ArchitectureKind> ArchitectureKinds { get; } = new Dictionary<int, ArchitectureKind>();
        public Dictionary<int, Treasure> Treasures { get; } = new Dictionary<int, Treasure>();
        public Dictionary<int, Legion> Legions { get; } = new Dictionary<int, Legion>();
        public Dictionary<int, Troop> Troops { get; } = new Dictionary<int, Troop>();
        public Dictionary<int, Section> Sections { get; } = new Dictionary<int, Section>();
        public Dictionary<int, Military> Militaries { get; } = new Dictionary<int, Military>();
        public Dictionary<int, Facility> Facilities { get; } = new Dictionary<int, Facility>();
        // 馃敟 淇锛氭坊鍔?MilitaryKinds 鏌ユ壘琛?
        // 鏃ユ湡锛?026-02-10
        // 闂锛歁ilitary.Kind 寮曠敤浠庢潵娌℃湁琚摼鎺ヨ繃
        public Dictionary<int, MilitaryKind> MilitaryKinds { get; } = new Dictionary<int, MilitaryKind>();
        // 馃敟 淇锛氭坊鍔?SectionAIDetails 鏌ユ壘琛?
        // 鏃ユ湡锛?026-02-13
        // 闂锛歋ection.AIDetail 寮曠敤浠庢潵娌℃湁琚摼鎺ヨ繃锛屽鑷磋妗ｅ悗鐜╁鍔垮姏琚?AI 鎺у埗
        public Dictionary<int, SectionAIDetail> SectionAIDetails { get; } = new Dictionary<int, SectionAIDetail>();
        // 馃敟 淇锛氭坊鍔?States 鍜?Regions 鏌ユ壘琛?
        // 鏃ユ湡锛?026-02-16
        // 闂锛歋tates 鍜?Regions 鐨勫叧鑱斿叧绯讳粠鏈閾炬帴
        public Dictionary<int, State> States { get; } = [];
        public Dictionary<int, Region> Regions { get; } = [];
    }

    /// <summary>
    /// Reference linker for Person objects
    /// Links BelongedFaction reference and Treasures collection
    /// Uses pre-built lookup tables for O(1) reference resolution
    /// </summary>
    public class PersonReferenceLinker : IReferenceLinker<Person>
    {
        /// <summary>
        /// Link all references for a Person (legacy method for backward compatibility)
        /// </summary>
        public void LinkReferences(Person person, GameScenario scenario)
        {
            // Build minimal lookup tables for this single call
            var lookupTables = new LookupTables();
            
            // Only build the tables we need for Person
            if (scenario.Factions != null)
            {
                foreach (Faction f in scenario.Factions.GetList())
                {
                    if (f != null && !lookupTables.Factions.ContainsKey(f.ID))
                        lookupTables.Factions[f.ID] = f;
                }
            }
            
            if (scenario.Architectures != null)
            {
                foreach (Architecture a in scenario.Architectures.GetList())
                {
                    if (a != null && !lookupTables.Architectures.ContainsKey(a.ID))
                        lookupTables.Architectures[a.ID] = a;
                }
            }
            
            if (scenario.Troops != null)
            {
                foreach (Troop t in scenario.Troops.GetList())
                {
                    if (t != null && !lookupTables.Troops.ContainsKey(t.ID))
                        lookupTables.Troops[t.ID] = t;
                }
            }
            
            if (scenario.Persons != null)
            {
                foreach (Person p in scenario.Persons.GetList())
                {
                    if (p != null && !lookupTables.Persons.ContainsKey(p.ID))
                        lookupTables.Persons[p.ID] = p;
                }
            }
            
            if (scenario.Treasures != null)
            {
                foreach (Treasure tr in scenario.Treasures.GetList())
                {
                    if (tr != null && !lookupTables.Treasures.ContainsKey(tr.ID))
                        lookupTables.Treasures[tr.ID] = tr;
                }
            }
            
            LinkReferences(person, scenario, lookupTables);
        }
        
        /// <summary>
        /// Link all references for a Person using pre-built lookup tables
        /// This is the optimized version that uses O(1) dictionary lookups
        /// </summary>
        public void LinkReferences(Person person, GameScenario scenario, LookupTables lookupTables)
        {
            if (person == null || scenario == null || lookupTables == null)
                return;
            
            // Link BelongedFaction reference
            LinkBelongedFaction(person, lookupTables);
            
            // Link LocationArchitecture reference
            LinkLocationArchitecture(person, lookupTables);
            
            // Link LocationTroop reference
            LinkLocationTroop(person, lookupTables);
            
            // Link ConvincingPerson reference
            LinkConvincingPerson(person, lookupTables);
            
            // Link Treasures collection
            LinkTreasures(person, lookupTables);
            
            // 馃敟 淇锛氳В鏋?Skills銆丼tunts銆乀itles 闆嗗悎
            // 鏃ユ湡锛?026-03-17
            // 闂锛歋killsString銆丼tuntsString銆乀itlesString 琚纭姞杞斤紝浣嗘病鏈夎瑙ｆ瀽鎴愰泦鍚堝璞?
            #if DEBUG
            if (person.ID <= 2)
            {
                System.Diagnostics.Debug.WriteLine($"[PersonReferenceLinker] {person.Name}(ID:{person.ID}): 鍑嗗璋冪敤 LinkSkills");
            }
            #endif
            
            LinkSkills(person, scenario);
            LinkStunts(person, scenario);
            LinkTitles(person, scenario);
            
            // 馃敟 淇锛氶摼鎺?PersonBiography 寮曠敤
            // 鏃ユ湡锛?026-03-17
            // 闂锛歅ersonBiography 瀵硅薄娌℃湁琚摼鎺ワ紝瀵艰嚧鍒椾紶涓嶆樉绀?
            LinkBiography(person, scenario);
            
            // 馃敟 2026-03-13 淇锛氶摼鎺?IdealTendency 寮曠敤
            LinkIdealTendency(person, scenario);
            
            // 馃敟 2026-03-13 淇锛氶摼鎺?Character 寮曠敤
            LinkCharacter(person, scenario);
            
            // 馃敟 2026-03-13 淇锛氶摼鎺?TrainPolicy 寮曠敤
            LinkTrainPolicy(person, scenario);
        }
        
        /// <summary>
        /// Link BelongedFaction reference using O(1) dictionary lookup
        /// 
        /// 娉ㄦ剰锛歅erson.BelongedFaction 鏄彧璇昏绠楀睘鎬э紝閫氳繃浠ヤ笅鏂瑰紡璁＄畻锛?
        /// - 濡傛灉鍦ㄥ缓绛戜腑 鈫?杩斿洖寤虹瓚鐨勫娍鍔?
        /// - 濡傛灉鍦ㄩ儴闃熶腑 鈫?杩斿洖閮ㄩ槦鐨勫娍鍔?
        /// - 濡傛灉鏄繕铏?鈫?杩斿洖淇樿檹鎵€灞炲娍鍔?
        /// 
        /// BelongedFactionID 瀛楁鐢ㄤ簬鍏朵粬鐩殑锛屼笉鐩存帴鐢ㄤ簬閾炬帴鍔垮姏
        /// 鐪熸鐨勫娍鍔涘叧绯婚€氳繃 LocationArchitecture 鎴?LocationTroop 纭畾
        /// </summary>
        private void LinkBelongedFaction(Person person, LookupTables lookupTables)
        {
            // 馃敟 璇婃柇锛氳褰?Person 14 鐨勫鐞?
            if (person.ID == 14)
            {
                /*
                System.Diagnostics.Debug.WriteLine($"[LinkReferencesPhase] 妫€鏌?Person 14 ({person.Name}) 鐨勫娍鍔涘叧绯?);
                System.Diagnostics.Debug.WriteLine($"  - BelongedFactionID: {person.BelongedFactionID}");
                System.Diagnostics.Debug.WriteLine($"  - LocationArchitectureID: {person.LocationArchitectureID}");
                System.Diagnostics.Debug.WriteLine($"  - LocationTroopID: {person.LocationTroopID}");
                System.Diagnostics.Debug.WriteLine($"  - Status: {person.Status}");
                
                // BelongedFaction 鏄绠楀睘鎬э紝閫氳繃 LocationArchitecture 鎴?LocationTroop 鑾峰彇
                // 鎵€浠ユ垜浠彧闇€瑕佺‘淇濊繖浜涘紩鐢ㄨ姝ｇ‘閾炬帴鍗冲彲
                
                if (person.BelongedFactionID > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  - lookupTables.Factions 鍖呭惈 ID {person.BelongedFactionID}: {lookupTables.Factions.ContainsKey(person.BelongedFactionID)}");
                    
                    if (lookupTables.Factions.ContainsKey(person.BelongedFactionID))
                    {
                        var faction = lookupTables.Factions[person.BelongedFactionID];
                        System.Diagnostics.Debug.WriteLine($"  - 鍔垮姏 {person.BelongedFactionID} 瀛樺湪: {faction.Name}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  - 鉂?鍔垮姏 {person.BelongedFactionID} 涓嶅瓨鍦紒");
                        System.Diagnostics.Debug.WriteLine($"  - 鍙敤鐨勫娍鍔?IDs: {string.Join(", ", lookupTables.Factions.Keys.Take(20))}");
                    }
                }
                */
            }
            
            // Person.BelongedFaction 鏄彧璇昏绠楀睘鎬э紝涓嶈兘鐩存帴璧嬪€?
            // 瀹冧細鑷姩閫氳繃 LocationArchitecture.BelongedFaction 鎴?LocationTroop.BelongedFaction 璁＄畻
            // 鎵€浠ヨ繖閲屼笉闇€瑕佸仛浠讳綍鎿嶄綔锛屽彧瑕佺‘淇?LocationArchitecture 鍜?LocationTroop 琚纭摼鎺ュ嵆鍙?
            
            // 楠岃瘉 BelongedFactionID 鐨勬湁鏁堟€э紙鐢ㄤ簬璇婃柇锛?
            if (person.BelongedFactionID > 0)
            {
                if (!lookupTables.Factions.ContainsKey(person.BelongedFactionID))
                {
                    // BelongedFactionID 寮曠敤浜嗕笉瀛樺湪鐨勫娍鍔?
                    // 杩欏彲鑳芥槸鏁版嵁鎹熷潖锛屼絾涓嶅奖鍝嶆父鎴忚繍琛岋紙鍥犱负 BelongedFaction 閫氳繃鍏朵粬鏂瑰紡璁＄畻锛?
                    LogWarning($"Person {person.ID} ({person.Name}) has BelongedFactionID={person.BelongedFactionID} which doesn't exist (but this is OK - BelongedFaction is computed from LocationArchitecture/LocationTroop)");
                }
            }
        }
        
        /// <summary>
        /// Link LocationArchitecture reference using O(1) dictionary lookup
        /// </summary>
        private void LinkLocationArchitecture(Person person, LookupTables lookupTables)
        {
            /*
            #if DEBUG
            // 馃敟 璇婃柇锛氳褰曟墍鏈夋灏嗙殑 LocationArchitectureID
            if (person.ID <= 20)  // 鍙褰曞墠20涓灏?
            {
                System.Diagnostics.Debug.WriteLine($"[LinkLocationArchitecture] Person {person.ID} ({person.Name}) LocationArchitectureID={person.LocationArchitectureID}");
            }
            #endif
            */
            
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堟礇闃崇殑 ID=0锛?
            // 鏃ユ湡锛?026-03-16
            // 闂锛氫娇鐢?> 0 浼氳烦杩?ID=0 鐨勬礇闃筹紝瀵艰嚧姝﹀皢鏃犳硶閾炬帴鍒版礇闃?
            if (person.LocationArchitectureID >= 0)
            {
                // O(1) dictionary lookup
                if (lookupTables.Architectures.TryGetValue(person.LocationArchitectureID, out Architecture architecture))
                {
                    person.LocationArchitecture = architecture;
                    
                    /*
                    #if DEBUG
                    if (person.ID <= 20)
                    {
                        System.Diagnostics.Debug.WriteLine($"  鉁?閾炬帴鎴愬姛: Person {person.ID} 鈫?Architecture {architecture.ID} ({architecture.Name})");
                    }
                    #endif
                    */
                }
                else
                {
                    /*
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"  鉂?閾炬帴澶辫触: Person {person.ID} ({person.Name}) 寮曠敤浜嗕笉瀛樺湪鐨?Architecture {person.LocationArchitectureID}");
                    #endif
                    */
                    
                    LogWarning($"Person {person.ID} ({person.Name}) references non-existent Architecture {person.LocationArchitectureID}");
                    person.LocationArchitectureID = -1;
                    person.LocationArchitecture = null;
                }
            }
            else
            {
                /*
                #if DEBUG
                if (person.ID <= 20)
                {
                    System.Diagnostics.Debug.WriteLine($"  鈿?鍦ㄩ噹姝﹀皢: Person {person.ID} ({person.Name}) LocationArchitectureID={person.LocationArchitectureID}");
                }
                #endif
                */
                
                person.LocationArchitecture = null;
            }
        }
        
        /// <summary>
        /// Link LocationTroop reference using O(1) dictionary lookup
        /// </summary>
        private void LinkLocationTroop(Person person, LookupTables lookupTables)
        {
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堝鏋滃瓨鍦?ID=0 鐨勯儴闃燂級
            // 鏃ユ湡锛?026-03-16
            if (person.LocationTroopID >= 0)
            {
                // O(1) dictionary lookup
                if (lookupTables.Troops.TryGetValue(person.LocationTroopID, out Troop troop))
                {
                    person.LocationTroop = troop;
                }
                else
                {
                    LogWarning($"Person {person.ID} ({person.Name}) references non-existent Troop {person.LocationTroopID}");
                    person.LocationTroopID = -1;
                    person.LocationTroop = null;
                }
            }
            else
            {
                person.LocationTroop = null;
            }
        }
        
        /// <summary>
        /// Link ConvincingPerson reference using O(1) dictionary lookup
        /// </summary>
        private void LinkConvincingPerson(Person person, LookupTables lookupTables)
        {
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堥樋浼氬杻鐨?ID=0锛?
            // 鏃ユ湡锛?026-03-16
            if (person.ConvincingPersonID >= 0)
            {
                // O(1) dictionary lookup
                if (lookupTables.Persons.TryGetValue(person.ConvincingPersonID, out Person convincingPerson))
                {
                    person.ConvincingPerson = convincingPerson;
                }
                else
                {
                    LogWarning($"Person {person.ID} ({person.Name}) references non-existent ConvincingPerson {person.ConvincingPersonID}");
                    person.ConvincingPersonID = -1;
                    person.ConvincingPerson = null;
                }
            }
            else
            {
                person.ConvincingPerson = null;
            }
        }
        
        /// <summary>
        /// Link Treasures collection using O(1) dictionary lookups
        /// 馃敟 2026-03-18 Person-Centric 妯″瀷锛氫粠 Person.TreasureIDs 閾炬帴瀹濈墿锛屽悓鏃惰缃弽鍚戝紩鐢?
        /// </summary>
        private void LinkTreasures(Person person, LookupTables lookupTables)
        {
            // Clear existing collection
            if (person.Treasures == null)
                person.Treasures = new TreasureList();
            else
                person.Treasures.Clear();
            
            // 馃敟 璇婃柇锛氳褰曞墠20涓灏嗙殑瀹濈墿閾炬帴鎯呭喌
            // 鏃ユ湡锛?026-03-17
            #if DEBUG
            // 馃敟 2026-03-18 淇锛氭墿澶ц瘖鏂寖鍥村埌鍓?00涓灏嗭紝纭繚瑕嗙洊鏇规搷锛圛D=343锛?
            if (person.ID <= 500)
            {

            }
            #endif
            
            // Link each treasure by ID using O(1) dictionary lookup
            if (person.TreasureIDs != null && person.TreasureIDs.Count > 0)
            {
                // 馃敟 娉ㄦ剰锛歍reasureList 涓嶆敮鎸?EnsureCapacity
                // 鏃ユ湡锛?026-03-18
                // 鍘熷洜锛歍reasureList 鏄嚜瀹氫箟闆嗗悎绫伙紝娌℃湁瀹炵幇 EnsureCapacity 鏂规硶
                // 鏈潵浼樺寲锛氬彲浠ュ湪 TreasureList 涓坊鍔?EnsureCapacity 鏀寔
                
                // Create a list to track valid IDs
                List<int> validTreasureIDs = [];
                
                foreach (int treasureID in person.TreasureIDs)
                {
                    // O(1) dictionary lookup instead of O(n) linear search
                    if (lookupTables.Treasures.TryGetValue(treasureID, out Treasure treasure))
                    {
                        person.Treasures.Add(treasure);
                        validTreasureIDs.Add(treasureID);
                        
                        // 馃敟 2026-03-18 Person-Centric 妯″瀷锛氳缃弽鍚戝紩鐢?
                        // 鍘熷洜锛氬疂鐗╁綊灞炲叧绯荤敱 Person.TreasureIDs 绠＄悊
                        //       璇绘。鏃堕渶瑕侀噸寤?Treasure.BelongedPerson 鍙嶅悜寮曠敤
                        treasure.BelongedPerson = person;
                        treasure.BelongedPersonIDString = person.ID;
                        
                        #if DEBUG
                        if (person.ID <= 20)
                        {
                            System.Diagnostics.Debug.WriteLine($"    鉁?閾炬帴鎴愬姛: Treasure {treasureID} ({treasure.Name})");
                        }
                        #endif
                    }
                    else
                    {
                        // Invalid reference - log warning and skip
                        LogWarning($"Person {person.ID} ({person.Name}) references non-existent Treasure {treasureID}");
                        
                        #if DEBUG
                        if (person.ID <= 20)
                        {
                            System.Diagnostics.Debug.WriteLine($"    [FAILED] Treasure {treasureID} does not exist.");
                        }
                        #endif
                    }
                }
                
                // Clean up TreasureIDs list to only contain valid IDs
                if (validTreasureIDs.Count != person.TreasureIDs.Count)
                {
                    person.TreasureIDs.Clear();
                    person.TreasureIDs.AddRange(validTreasureIDs);
                }
                
                #if DEBUG
                if (person.ID <= 20)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Final linked treasures: {person.Treasures.Count}");
                }
                #endif
            }
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        private void LogWarning(string message)
        {
            // Use System.Diagnostics for logging
            // System.Diagnostics.Debug.WriteLine($"[LinkReferencesPhase] WARNING: {message}");
            
            // Could also use a proper logging framework if available
            // Logger.Warning($"[LinkReferencesPhase] {message}");
        }
        
        /// <summary>
        /// 閾炬帴 IdealTendency 寮曠敤锛堝嚭浠曞織鍚戣€冭檻锛?
        /// 馃敟 鍏抽敭锛欼D=0 鏄湁鏁堢殑 IdealTendencyKind锛堥粯璁ょ悊鎯冲€惧悜锛?
        /// </summary>
        private void LinkIdealTendency(Person person, GameScenario scenario)
        {
            // 馃敟 鍏抽敭锛欼D >= 0 琛ㄧず鏈夋晥寮曠敤锛圛D=0 鏄湁鏁堢殑锛?
            if (person.IdealTendencyIDString >= 0)
            {
                // ANTI-BAND-AID锛欸ameCommonData 蹇呴』瀛樺湪锛屽惁鍒欐槸鏁版嵁鎹熷潖
                if (scenario.GameCommonData == null)
                {
                    throw new InvalidOperationException($"鏁版嵁鎹熷潖锛欸ameCommonData 涓?null锛屾棤娉曢摼鎺?Person {person.ID} 鐨?IdealTendency 寮曠敤");
                }
                
                if (scenario.GameCommonData.AllIdealTendencyKinds == null)
                {
                    throw new InvalidOperationException($"鏁版嵁鎹熷潖锛欰llIdealTendencyKinds 涓?null锛屾棤娉曢摼鎺?Person {person.ID} 鐨?IdealTendency 寮曠敤");
                }
                
                person.IdealTendency = scenario.GameCommonData.AllIdealTendencyKinds
                    .GetGameObject(person.IdealTendencyIDString) as IdealTendencyKind;
                
                if (person.IdealTendency == null)
                {
                    LogWarning($"Person {person.ID} ({person.Name}) references non-existent IdealTendencyKind {person.IdealTendencyIDString}");
                }
            }
        }
        
        /// <summary>
        /// 閾炬帴 Character 寮曠敤锛堟€ф牸锛?
        /// </summary>
        private void LinkCharacter(Person person, GameScenario scenario)
        {
            if (person.PCharacter >= 0)
            {
                // ANTI-BAND-AID锛欸ameCommonData 蹇呴』瀛樺湪锛屽惁鍒欐槸鏁版嵁鎹熷潖
                if (scenario.GameCommonData == null)
                {
                    throw new InvalidOperationException($"鏁版嵁鎹熷潖锛欸ameCommonData 涓?null锛屾棤娉曢摼鎺?Person {person.ID} 鐨?Character 寮曠敤");
                }
                
                if (scenario.GameCommonData.AllCharacterKinds == null)
                {
                    throw new InvalidOperationException($"鏁版嵁鎹熷潖锛欰llCharacterKinds 涓?null锛屾棤娉曢摼鎺?Person {person.ID} 鐨?Character 寮曠敤");
                }
                
                if (person.PCharacter < scenario.GameCommonData.AllCharacterKinds.Count)
                {
                    person.Character = scenario.GameCommonData.AllCharacterKinds[person.PCharacter];
                }
                else
                {
                    LogWarning($"Person {person.ID} ({person.Name}) references non-existent CharacterKind {person.PCharacter} (Count={scenario.GameCommonData.AllCharacterKinds.Count})");
                }
            }
        }
        
        /// <summary>
        /// 馃敟 淇锛氳В鏋?Skills锛堟妧鑳斤級闆嗗悎
        /// 鏃ユ湡锛?026-03-17
        /// 闂锛歋killsString 琚纭姞杞斤紝浣嗘病鏈夎瑙ｆ瀽鎴?Skills 闆嗗悎瀵硅薄
        /// </summary>
        private void LinkSkills(Person person, GameScenario scenario)
        {
            // 2026-03-18锛氭敼涓轰娇鐢?SkillIDs锛圠ist<int>锛夌洿鎺ユ煡鎵撅紝涓嶅啀渚濊禆 SkillsString
            // 2026-03-20锛氱Щ闄よ繃搴︾殑璋冭瘯杈撳嚭锛岄伩鍏?ExecutionEngineException
            
            foreach (int skillID in person.SkillIDs)
            {
                if (scenario.GameCommonData.AllSkills.Skills.TryGetValue(skillID, out var skill))
                {
                    person.Skills.AddSkill(skill);
                }
                else
                {
                    LogWarning($"鏁版嵁鎹熷潖锛歅erson {person.ID}({person.Name}) 寮曠敤浜嗕笉瀛樺湪鐨?Skill {skillID}");
                }
            }
        }
        
        private void LinkStunts(Person person, GameScenario scenario)
        {
            // 2026-03-18锛氭敼涓轰娇鐢?StuntIDs锛圠ist<int>锛夌洿鎺ユ煡鎵撅紝涓嶅啀渚濊禆 StuntsString
            // 2026-03-20锛氱Щ闄よ繃搴︾殑璋冭瘯杈撳嚭锛岄伩鍏?ExecutionEngineException
            
            foreach (int stuntID in person.StuntIDs)
            {
                if (scenario.GameCommonData.AllStunts.Stunts.TryGetValue(stuntID, out var stunt))
                {
                    person.Stunts.AddStunt(stunt);
                }
                else
                {
                    LogWarning($"鏁版嵁鎹熷潖锛歅erson {person.ID}({person.Name}) 寮曠敤浜嗕笉瀛樺湪鐨?Stunt {stuntID}");
                }
            }
        }
        
        private void LinkTitles(Person person, GameScenario scenario)
        {
            // 2026-03-18锛氭敼涓轰娇鐢?TitleIDs锛圠ist<int>锛夌洿鎺ユ煡鎵撅紝涓嶅啀渚濊禆 RealTitlesString
            foreach (int titleID in person.TitleIDs)
            {
                if (scenario.GameCommonData.AllTitles.Titles.TryGetValue(titleID, out var title))
                    person.RealTitles.Add(title);
                else
                    LogWarning($"鏁版嵁鎹熷潖锛歅erson {person.ID}({person.Name}) 寮曠敤浜嗕笉瀛樺湪鐨?Title {titleID}");
            }
        }
        
        /// <summary>
        /// 馃敟 淇锛氶摼鎺?PersonBiography锛堝垪浼狅級寮曠敤
        /// 鏃ユ湡锛?026-03-17
        /// 闂锛歅ersonBiography 瀵硅薄娌℃湁琚摼鎺ワ紝瀵艰嚧鍒椾紶涓嶆樉绀?
        /// 娉ㄦ剰锛欱iography 鐨?ID 閫氬父绛変簬 Person 鐨?ID锛堥樋浼氬杻鐨?ID=0锛屽叾鍒椾紶鐨?ID 涔熸槸 0锛?
        /// 馃敟 鍏抽敭锛欼D >= 0 琛ㄧず鏈夋晥寮曠敤锛圛D=0 鏄湁鏁堢殑锛屽闃夸細鍠冨垪浼狅級
        /// </summary>
        private void LinkBiography(Person person, GameScenario scenario)
        {
            // 馃敟 鍏抽敭锛欼D >= 0 琛ㄧず鏈夋晥寮曠敤锛圛D=0 鏄樋浼氬杻鍒椾紶锛?
            if (person.ID >= 0 && person.PersonBiography == null)
            {
                // 灏濊瘯閫氳繃 Person.ID 鏌ユ壘瀵瑰簲鐨?Biography
                // 娉ㄦ剰锛欰llBiographies 鍦?GameScenario 涓瓧娈靛垵濮嬪寲锛? new BiographyTable()锛夛紝姘歌繙涓嶄负 null
                var biography = scenario.AllBiographies.GetBiography(person.ID);
                
                if (biography != null)
                {
                    person.PersonBiography = biography;
                    
                    /*
                    #if DEBUG
                    if (person.ID <= 20)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LinkBiography] {person.Name}(ID:{person.ID}) 閾炬帴浜嗗垪浼? {biography.Name}");
                    }
                    #endif
                    */
                }
                else
                {
                    /*
                    #if DEBUG
                    if (person.ID <= 5)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LinkBiography] 鈿狅笍 {person.Name}(ID:{person.ID}) 娌℃湁鎵惧埌瀵瑰簲鐨勫垪浼?);
                    }
                    #endif
                    */
                }
            }
        }
        
        /// <summary>
        /// 閾炬帴 TrainPolicy 寮曠敤锛堣缁冩柟閽堬級
        /// 馃敟 鍏抽敭锛欼D >= 0 琛ㄧず鏈夋晥寮曠敤
        /// </summary>
        private void LinkTrainPolicy(Person person, GameScenario scenario)
        {
            // 馃敟 鍏抽敭锛歍rainPolicy 鐨?ID 浠?1 寮€濮嬶紝ID=0 琛ㄧず鏃犲紩鐢?
            // 鏃ユ湡锛?026-03-17
            // 鍘熷洜锛歍rainPolicy 涓嶉伒寰」鐩殑 ID=0 瑙勮寖锛堜笌 Architecture銆丮ilitaryKind 绛変笉鍚岋級
            if (person.TrainPolicyIDString > 0)
            {
                // ANTI-BAND-AID锛欸ameCommonData 蹇呴』瀛樺湪锛屽惁鍒欐槸鏁版嵁鎹熷潖
                if (scenario.GameCommonData == null)
                {
                    throw new InvalidOperationException($"鏁版嵁鎹熷潖锛欸ameCommonData 涓?null锛屾棤娉曢摼鎺?Person {person.ID} 鐨?TrainPolicy 寮曠敤");
                }
                
                if (scenario.GameCommonData.AllTrainPolicies == null)
                {
                    throw new InvalidOperationException($"鏁版嵁鎹熷潖锛欰llTrainPolicies 涓?null锛屾棤娉曢摼鎺?Person {person.ID} 鐨?TrainPolicy 寮曠敤");
                }
                
                person.TrainPolicy = scenario.GameCommonData.AllTrainPolicies
                    .GetGameObject(person.TrainPolicyIDString) as TrainPolicy;
                
                if (person.TrainPolicy == null)
                {
                    LogWarning($"Person {person.ID} ({person.Name}) references non-existent TrainPolicy {person.TrainPolicyIDString}");
                }
            }
        }
    }

    /// <summary>
    /// Reference linker for Faction objects
    /// Links Leader, Capital, and collection references (Architectures, Persons, Militaries, etc.)
    /// Uses pre-built lookup tables for O(1) reference resolution
    /// </summary>
    public class FactionReferenceLinker : IReferenceLinker<Faction>
    {
        /// <summary>
        /// Link all references for a Faction (legacy method for backward compatibility)
        /// </summary>
        public void LinkReferences(Faction faction, GameScenario scenario)
        {
            // For backward compatibility, build minimal lookup tables
            var lookupTables = new LookupTables();
            // Build only what we need...
            // (Implementation omitted for brevity - similar to PersonReferenceLinker)
            LinkReferences(faction, scenario, lookupTables);
        }
        
        /// <summary>
        /// Link all references for a Faction using pre-built lookup tables (optimized)
        /// </summary>
        public void LinkReferences(Faction faction, GameScenario scenario, LookupTables lookupTables)
        {
            if (faction == null || scenario == null || lookupTables == null)
                return;
            
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堟礇闃炽€侀樋浼氬杻绛夛級锛屽繀椤讳娇鐢?>= 0
            // 鏃ユ湡锛?026-03-16
            // Link Leader reference using O(1) lookup
            if (faction.LeaderID >= 0 && lookupTables.Persons.TryGetValue(faction.LeaderID, out Person leader))
            {
                faction.Leader = leader;
            }
            
            // 馃敟 鏍规湰淇锛氶摼鎺ュ啗甯堝紩鐢?
            // 鏃ユ湡锛?026-03-18
            // 闂锛氫换鍛藉啗甯堝悗淇濆瓨璇绘。锛屽啗甯堜涪澶?
            // 鍘熷洜锛歀inkReferencesPhase 鏈鐞?AdvisorID 寮曠敤閾炬帴
            if (faction.AdvisorID >= 0 && lookupTables.Persons.TryGetValue(faction.AdvisorID, out Person advisor))
            {
                faction.Advisor = advisor;
            }
            
            // Link Capital reference using O(1) lookup
            if (faction.CapitalID >= 0 && lookupTables.Architectures.TryGetValue(faction.CapitalID, out Architecture capital))
            {
                faction.Capital = capital;
            }
            
            // Link collections using O(1) lookups
            LinkArchitectures(faction, lookupTables);
            LinkPersons(faction, lookupTables);
            LinkMilitaries(faction, lookupTables);
            LinkLegions(faction, lookupTables);
            LinkTroops(faction, lookupTables);
            LinkSections(faction, lookupTables);
        }
        
        private void LinkArchitectures(Faction faction, LookupTables lookupTables)
        {
            if (faction.Architectures == null)
                faction.Architectures = new ArchitectureList();
            else
                faction.Architectures.Clear();
            
            if (faction.ArchitectureIDs != null)
            {
                foreach (int id in faction.ArchitectureIDs)
                {
                    if (lookupTables.Architectures.TryGetValue(id, out Architecture arch))
                    {
                        faction.Architectures.Add(arch);
                        // 馃敟 鍏抽敭淇锛氬弽鍚戣缃?Architecture.BelongedFaction
                        // 鏃ユ湡锛?026-03-16
                        // 鍘熷洜锛氭棫鍓ф湰 JSON 娌℃湁 Architecture.BelongedFactionID 瀛楁
                        //       鍩庢睜褰掑睘鍙瓨鍦?Faction.ArchitecturesString 閲岋紝蹇呴』鍦ㄨ繖閲屽弽鍚戣缃?
                        arch.BelongedFaction = faction;
                        arch.BelongedFactionID = faction.ID;
                    }
                }
            }
        }
        
        private void LinkPersons(Faction faction, LookupTables lookupTables)
        {
            /*
            if (faction.Persons == null)
                faction.Persons = new PersonList();
            else
                faction.Persons.Clear();
            */
            
            if (faction.PersonIDs != null)
            {
                foreach (int id in faction.PersonIDs)
                {
                    if (lookupTables.Persons.TryGetValue(id, out Person person))
                    {
                        // faction.Persons.Add(person); // Read-only computed property
                    }
                }
            }
        }
        
        private void LinkMilitaries(Faction faction, LookupTables lookupTables)
        {
            /*
            if (faction.Militaries == null)
                faction.Militaries = new MilitaryList();
            else
                faction.Militaries.Clear();
            */
            
            if (faction.MilitaryIDs != null)
            {
                foreach (int id in faction.MilitaryIDs)
                {
                    if (lookupTables.Militaries.TryGetValue(id, out Military military))
                    {
                        faction.Militaries.Add(military);
                    }
                }
            }
        }
        
        private void LinkLegions(Faction faction, LookupTables lookupTables)
        {
            if (faction.Legions == null)
                faction.Legions = new LegionList();
            else
                faction.Legions.Clear();
            
            if (faction.LegionIDs != null)
            {
                foreach (int id in faction.LegionIDs)
                {
                    if (lookupTables.Legions.TryGetValue(id, out Legion legion))
                    {
                        faction.Legions.Add(legion);
                    }
                }
            }
        }
        
        private void LinkTroops(Faction faction, LookupTables lookupTables)
        {
            if (faction.Troops == null)
                faction.Troops = new TroopListWithQueue();
            else
                faction.Troops.Clear();
            
            if (faction.TroopIDs != null)
            {
                foreach (int id in faction.TroopIDs)
                {
                    if (lookupTables.Troops.TryGetValue(id, out Troop troop))
                    {
                        faction.Troops.Add(troop);
                    }
                }
            }
        }
        
        private void LinkSections(Faction faction, LookupTables lookupTables)
        {
            if (faction.Sections == null)
                faction.Sections = new SectionList();
            else
                faction.Sections.Clear();
            
            if (faction.SectionIDs != null)
            {
                foreach (int id in faction.SectionIDs)
                {
                    if (lookupTables.Sections.TryGetValue(id, out Section section))
                    {
                        faction.Sections.Add(section);
                    }
                }
                }
            }

        
        /// <summary>
        /// Link Leader reference
        /// </summary>
        private void LinkLeader(Faction faction, GameScenario scenario)
        {
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堥樋浼氬杻锛夛紝蹇呴』浣跨敤 >= 0
            // 鏃ユ湡锛?026-03-16
            if (faction.LeaderID >= 0)
            {
                Person leader = scenario.Persons.GetGameObject(faction.LeaderID) as Person;
                
                if (leader != null)
                {
                    faction.Leader = leader;
                }
                else
                {
                    LogWarning($"Faction {faction.ID} ({faction.Name}) references non-existent Leader {faction.LeaderID}");
                    faction.LeaderID = -1;
                    faction.Leader = null;
                }
            }
            else
            {
                faction.Leader = null;
            }
        }
        
        /// <summary>
        /// Link Capital reference
        /// </summary>
        private void LinkCapital(Faction faction, GameScenario scenario)
        {
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堟礇闃筹級锛屽繀椤讳娇鐢?>= 0
            // 鏃ユ湡锛?026-03-16
            if (faction.CapitalID >= 0)
            {
                Architecture capital = scenario.Architectures.GetGameObject(faction.CapitalID) as Architecture;
                
                if (capital != null)
                {
                    faction.Capital = capital;
                }
                else
                {
                    LogWarning($"Faction {faction.ID} ({faction.Name}) references non-existent Capital {faction.CapitalID}");
                    faction.CapitalID = -1;
                    faction.Capital = null;
                }
            }
            else
            {
                faction.Capital = null;
            }
        }
        
        /// <summary>
        /// Link Architectures collection
        /// </summary>
        private void LinkArchitectures(Faction faction, GameScenario scenario)
        {
            // Clear existing collection
            if (faction.Architectures == null)
                faction.Architectures = new ArchitectureList();
            else
                faction.Architectures.Clear();
            
            // Link each architecture by ID
            if (faction.ArchitectureIDs != null && faction.ArchitectureIDs.Count > 0)
            {
                foreach (int architectureID in faction.ArchitectureIDs)
                {
                    Architecture architecture = scenario.Architectures.GetGameObject(architectureID) as Architecture;
                    
                    if (architecture != null)
                    {
                        faction.Architectures.Add(architecture);
                        // 馃敟 鍏抽敭淇锛氬弽鍚戣缃?Architecture.BelongedFaction
                        // 鏃ユ湡锛?026-03-16
                        architecture.BelongedFaction = faction;
                        architecture.BelongedFactionID = faction.ID;
                    }
                    else
                    {
                        LogWarning($"Faction {faction.ID} ({faction.Name}) references non-existent Architecture {architectureID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Link Persons collection
        /// </summary>
        private void LinkPersons(Faction faction, GameScenario scenario)
        {
            // Clear existing collection
            /*
            if (faction.Persons == null)
                faction.Persons = new PersonList();
            else
                faction.Persons.Clear();
            */
            
            // Link each person by ID
            if (faction.PersonIDs != null && faction.PersonIDs.Count > 0)
            {
                foreach (int personID in faction.PersonIDs)
                {
                    Person person = scenario.Persons.GetGameObject(personID) as Person;
                    
                    if (person != null)
                    {
                        // faction.Persons.Add(person);
                    }
                    else
                    {
                        LogWarning($"Faction {faction.ID} ({faction.Name}) references non-existent Person {personID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Link Militaries collection
        /// </summary>
        private void LinkMilitaries(Faction faction, GameScenario scenario)
        {
            // Clear existing collection
            /*
            if (faction.Militaries == null)
                faction.Militaries = new MilitaryList();
            else
                faction.Militaries.Clear();
            */
            
            // Link each military by ID
            if (faction.MilitaryIDs != null && faction.MilitaryIDs.Count > 0)
            {
                foreach (int militaryID in faction.MilitaryIDs)
                {
                    Military military = scenario.Militaries.GetGameObject(militaryID) as Military;
                    
                    if (military != null)
                    {
                        // faction.Militaries.Add(military);
                    }
                    else
                    {
                        LogWarning($"Faction {faction.ID} ({faction.Name}) references non-existent Military {militaryID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Link Legions collection
        /// </summary>
        private void LinkLegions(Faction faction, GameScenario scenario)
        {
            // Clear existing collection
            if (faction.Legions == null)
                faction.Legions = new LegionList();
            else
                faction.Legions.Clear();
            
            // Link each legion by ID
            if (faction.LegionIDs != null && faction.LegionIDs.Count > 0)
            {
                foreach (int legionID in faction.LegionIDs)
                {
                    Legion legion = scenario.Legions.GetGameObject(legionID) as Legion;
                    
                    if (legion != null)
                    {
                        faction.Legions.Add(legion);
                    }
                    else
                    {
                        LogWarning($"Faction {faction.ID} ({faction.Name}) references non-existent Legion {legionID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Link Troops collection
        /// </summary>
        private void LinkTroops(Faction faction, GameScenario scenario)
        {
            // Clear existing collection
            if (faction.Troops == null)
                faction.Troops = new TroopListWithQueue();
            else
                faction.Troops.Clear();
            
            // Link each troop by ID
            if (faction.TroopIDs != null && faction.TroopIDs.Count > 0)
            {
                foreach (int troopID in faction.TroopIDs)
                {
                    Troop troop = scenario.Troops.GetGameObject(troopID) as Troop;
                    
                    if (troop != null)
                    {
                        faction.Troops.Add(troop);
                    }
                    else
                    {
                        LogWarning($"Faction {faction.ID} ({faction.Name}) references non-existent Troop {troopID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Link Sections collection
        /// </summary>
        private void LinkSections(Faction faction, GameScenario scenario)
        {
            // Clear existing collection
            if (faction.Sections == null)
                faction.Sections = new SectionList();
            else
                faction.Sections.Clear();
            
            // Link each section by ID
            if (faction.SectionIDs != null && faction.SectionIDs.Count > 0)
            {
                foreach (int sectionID in faction.SectionIDs)
                {
                    Section section = scenario.Sections.GetGameObject(sectionID) as Section;
                    
                    if (section != null)
                    {
                        faction.Sections.Add(section);
                    }
                    else
                    {
                        LogWarning($"Faction {faction.ID} ({faction.Name}) references non-existent Section {sectionID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        private void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[LinkReferencesPhase] WARNING: {message}");
        }
    }

    /// <summary>
    /// <summary>
    /// Reference linker for Architecture objects
    /// Links BelongedFaction, BelongedSection, Mayor, and collection references
    /// 
    /// Performance Note: This linker supports both legacy O(n) lookups via GetGameObject()
    /// and optimized O(1) lookups via pre-built lookup tables (LookupTables parameter).
    /// The LinkReferencesPhase automatically uses the optimized version.
    /// </summary>
    public class ArchitectureReferenceLinker : IReferenceLinker<Architecture>
    {
        /// <summary>
        /// Link all references for an Architecture (legacy method - uses O(n) lookups)
        /// </summary>
        public void LinkReferences(Architecture architecture, GameScenario scenario)
        {
            if (architecture == null || scenario == null)
                return;
            
            // 馃敟 棣栧厛閾炬帴 Kind锛堝緢澶氬叾浠栭€昏緫渚濊禆 Kind锛?
            LinkKind(architecture, scenario);
            
            // Link BelongedFaction reference
            LinkBelongedFaction(architecture, scenario);
            
            // Link BelongedSection reference
            LinkBelongedSection(architecture, scenario);
            
            // Link Mayor reference
            LinkMayor(architecture, scenario);
            
            // Link Persons collection
            LinkPersons(architecture, scenario);
            
            // Link Militaries collection
            LinkMilitaries(architecture, scenario);
            
            // Link Facilities collection
            LinkFacilities(architecture, scenario);
            
            // 馃敟 淇锛氳В鏋?Characteristics锛堝煄甯傜壒鑹诧級
            // 鏃ユ湡锛?026-03-17
            // 闂锛欳haracteristicsString 琚纭姞杞斤紝浣嗘病鏈夎瑙ｆ瀽鎴?Characteristics 闆嗗悎瀵硅薄
            LinkCharacteristics(architecture, scenario);
        }
        
        /// <summary>
        /// Link Kind reference
        /// </summary>
        private void LinkKind(Architecture architecture, GameScenario scenario)
        {
            if (architecture.KindID > 0)
            {
                ArchitectureKind kind = scenario.GameCommonData?.AllArchitectureKinds?.GetArchitectureKind(architecture.KindID);
                
                if (kind != null)
                {
                    architecture.Kind = kind;
                }
                else
                {
                    // 馃敟 淇锛氳緭鍑鸿缁嗙殑璇婃柇淇℃伅
                    // 鏃ユ湡锛?026-02-10
                    LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent ArchitectureKind {architecture.KindID}");
                    
                    var allKinds = scenario.GameCommonData?.AllArchitectureKinds?.ArchitectureKinds;
                    if (allKinds != null)
                    {
                        LogWarning($"  - AllArchitectureKinds.Count: {allKinds.Count}");
                        LogWarning($"  - Available ArchitectureKind IDs: {string.Join(", ", allKinds.Keys.OrderBy(x => x))}");
                    }
                    else
                    {
                        LogWarning($"  - AllArchitectureKinds is null!");
                    }
                    
                    // 馃敟 淇锛氫笉瑕佷娇鐢ㄩ粯璁ゅ€硷紒鎶涘嚭寮傚父璁╅棶棰樻毚闇插嚭鏉?
                    // 鏃ユ湡锛?026-02-10
                    throw new InvalidOperationException(
                        $"Architecture {architecture.ID} ({architecture.Name}) references non-existent ArchitectureKind {architecture.KindID}. " +
                        $"Available IDs: {(allKinds != null ? string.Join(", ", allKinds.Keys.OrderBy(x => x)) : "null")}. " +
                        $"This indicates a data corruption or loading order issue that must be fixed at the root cause.");
                }
            }
            else
            {
                // KindID <= 0 鐨勬儏鍐?- 璇存槑淇濆瓨鏃?Kind 灏辨槸 null
                LogWarning($"鈿狅笍 Architecture {architecture.ID} ({architecture.Name}) has invalid KindID: {architecture.KindID} - Kind was null when saved!");
                
                // 馃敟 淇锛氫笉瑕佷娇鐢ㄩ粯璁ゅ€硷紒鎶涘嚭寮傚父
                // 鏃ユ湡锛?026-02-10
                throw new InvalidOperationException(
                    $"Architecture {architecture.ID} ({architecture.Name}) has invalid KindID: {architecture.KindID}. " +
                    $"This indicates the Architecture was saved without a valid Kind, which is a data integrity issue that must be fixed.");
            }
        }

        
        /// <summary>
        /// Link BelongedFaction reference
        /// </summary>
        private void LinkBelongedFaction(Architecture architecture, GameScenario scenario)
        {
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堟礇闃筹級锛屽繀椤讳娇鐢?>= 0
            // 鏃ユ湡锛?026-03-16
            // 鍘熷洜锛欱elongedFactionID=0 琛ㄧず姹夊娍鍔涳紙ID=0锛夛紝鐢?> 0 浼氬鑷存礇闃冲綊灞炰涪澶?
            if (architecture.BelongedFactionID >= 0)
            {
                Faction faction = scenario.Factions.GetGameObject(architecture.BelongedFactionID) as Faction;
                
                if (faction != null)
                {
                    architecture.BelongedFaction = faction;
                }
                else
                {
                    LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent Faction {architecture.BelongedFactionID}");
                    System.Diagnostics.Debug.WriteLine($"[LinkReferences] Missing faction for architecture {architecture.ID} ({architecture.Name}), factionID={architecture.BelongedFactionID}");
                    architecture.BelongedFactionID = -1;
                    architecture.BelongedFaction = null;
                }
            }
            else
            {
                architecture.BelongedFaction = null;
            }
        }
        
        /// <summary>
        /// Link BelongedSection reference
        /// </summary>
        private void LinkBelongedSection(Architecture architecture, GameScenario scenario)
        {
            if (architecture.BelongedSectionID > 0)
            {
                Section section = scenario.Sections.GetGameObject(architecture.BelongedSectionID) as Section;
                
                if (section != null)
                {
                    architecture.BelongedSection = section;
                }
                else
                {
                    LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent Section {architecture.BelongedSectionID}");
                    architecture.BelongedSectionID = -1;
                    architecture.BelongedSection = null;
                }
            }
            else
            {
                architecture.BelongedSection = null;
            }
        }
        
        /// <summary>
        /// Link Mayor reference
        /// </summary>
        private void LinkMayor(Architecture architecture, GameScenario scenario)
        {
            if (architecture.MayorID > 0)
            {
                Person mayor = scenario.Persons.GetGameObject(architecture.MayorID) as Person;
                
                if (mayor != null)
                {
                    architecture.Mayor = mayor;
                }
                else
                {
                    LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent Mayor {architecture.MayorID}");
                    architecture.MayorID = -1;
                    architecture.Mayor = null;
                }
            }
            else
            {
                architecture.Mayor = null;
            }
        }
        
        /// <summary>
        /// Link Persons collection
        /// </summary>
        private void LinkPersons(Architecture architecture, GameScenario scenario)
        {
            // Clear existing collection
            /*
            if (architecture.Persons == null)
                architecture.Persons = new PersonList();
            else
                architecture.Persons.Clear();
            */
            
            // Link each person by ID
            if (architecture.PersonIDs != null && architecture.PersonIDs.Count > 0)
            {
                foreach (int personID in architecture.PersonIDs)
                {
                    Person person = scenario.Persons.GetGameObject(personID) as Person;
                    
                    if (person != null)
                    {
                        // architecture.Persons.Add(person);
                    }
                    else
                    {
                        LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent Person {personID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Link Militaries collection
        /// </summary>
        private void LinkMilitaries(Architecture architecture, GameScenario scenario)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LinkMilitaries] 寤虹瓚: {architecture.Name}(ID:{architecture.ID}), MilitaryIDs.Count: {architecture.MilitaryIDs?.Count ?? 0}");
            #endif
            
            // 馃敟 淇锛氬彇娑堟敞閲婏紝纭繚Militaries鍒楄〃琚纭垵濮嬪寲鍜屾竻绌?
            if (architecture.Militaries == null)
                architecture.Militaries = new MilitaryList();
            else
                architecture.Militaries.Clear();
            
            // Link each military by ID
            if (architecture.MilitaryIDs != null && architecture.MilitaryIDs.Count > 0)
            {
                int addedCount = 0;
                foreach (int militaryID in architecture.MilitaryIDs)
                {
                    Military military = scenario.Militaries.GetGameObject(militaryID) as Military;
                    
                    if (military != null)
                    {
                        // 馃敟 淇锛氬彇娑堟敞閲婏紝灏嗙紪闃熸坊鍔犲埌寤虹瓚鐨凪ilitaries鍒楄〃
                        architecture.Militaries.Add(military);
                        addedCount++;
                    }
                    else
                    {
                        LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent Military {militaryID}");
                    }
                }
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LinkMilitaries]   娣诲姞浜?{addedCount} 涓紪闃熷埌 {architecture.Name}");
                #endif
            }
        }
        
        /// <summary>
        /// Link Facilities collection
        /// </summary>
        private void LinkFacilities(Architecture architecture, GameScenario scenario)
        {
            // Clear existing collection
            if (architecture.Facilities == null)
                architecture.Facilities = new FacilityList();
            else
                architecture.Facilities.Clear();
            
            // Link each facility by ID
            if (architecture.FacilityIDs != null && architecture.FacilityIDs.Count > 0)
            {
                foreach (int facilityID in architecture.FacilityIDs)
                {
                    Facility facility = scenario.Facilities.GetGameObject(facilityID) as Facility;
                    
                    if (facility != null)
                    {
                        architecture.Facilities.Add(facility);
                    }
                    else
                    {
                        LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent Facility {facilityID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 馃敟 淇锛氳В鏋?Characteristics锛堝煄甯傜壒鑹诧級
        /// 鏃ユ湡锛?026-03-17
        /// 闂锛欳haracteristicsString 琚纭姞杞斤紝浣嗘病鏈夎瑙ｆ瀽鎴?Characteristics 闆嗗悎瀵硅薄
        /// </summary>
        private void LinkCharacteristics(Architecture architecture, GameScenario scenario)
        {
            if (!string.IsNullOrEmpty(architecture.CharacteristicsString))
            {
                var errors = architecture.Characteristics.LoadFromString(
                    scenario.GameCommonData.AllInfluences, 
                    architecture.CharacteristicsString);
                
                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        LogWarning($"Architecture {architecture.ID} ({architecture.Name}) Characteristics parsing error: {error}");
                    }
                }
                
                /*#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LinkCharacteristics] {architecture.Name}(ID:{architecture.ID}) 瑙ｆ瀽浜?{architecture.Characteristics.Count} 涓壒鑹?);
                #endif*/
            }
        }
        
        /// <summary>
        /// Link all references for an Architecture using pre-built lookup tables (optimized - O(1) lookups)
        /// </summary>
        public void LinkReferences(Architecture architecture, GameScenario scenario, LookupTables lookupTables)
        {
            if (architecture == null || scenario == null || lookupTables == null)
                return;
            
            // 馃敟 棣栧厛閾炬帴Kind锛堝緢澶氬叾浠栭€昏緫渚濊禆Kind锛?
            if (architecture.KindID > 0 && lookupTables.ArchitectureKinds.TryGetValue(architecture.KindID, out ArchitectureKind kind))
            {
                architecture.Kind = kind;
            }
            else if (architecture.KindID > 0)
            {
                // 馃敟 淇锛氳緭鍑鸿缁嗙殑璇婃柇淇℃伅
                // 鏃ユ湡锛?026-02-10
                // 闂锛氭壘涓嶅埌 ArchitectureKind锛岄渶瑕佺煡閬撲负浠€涔?
                LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent ArchitectureKind {architecture.KindID}");
                LogWarning($"  - LookupTables.ArchitectureKinds.Count: {lookupTables.ArchitectureKinds.Count}");
                LogWarning($"  - Available ArchitectureKind IDs: {string.Join(", ", lookupTables.ArchitectureKinds.Keys.OrderBy(x => x))}");
                
                // 馃敟 淇锛氫笉瑕佷娇鐢ㄩ粯璁ゅ€硷紒鎶涘嚭寮傚父璁╅棶棰樻毚闇插嚭鏉?
                // 鏃ユ湡锛?026-02-10
                throw new InvalidOperationException(
                    $"Architecture {architecture.ID} ({architecture.Name}) references non-existent ArchitectureKind {architecture.KindID}. " +
                    $"Available IDs: {string.Join(", ", lookupTables.ArchitectureKinds.Keys.OrderBy(x => x))}. " +
                    $"This indicates a data corruption or loading order issue that must be fixed at the root cause.");
            }
            else // KindID <= 0 鐨勬儏鍐?- 璇存槑淇濆瓨鏃?Kind 灏辨槸 null
            {
                LogWarning($"鈿狅笍 Architecture {architecture.ID} ({architecture.Name}) has invalid KindID: {architecture.KindID} - Kind was null when saved!");
                
                // 馃敟 淇锛氫笉瑕佷娇鐢ㄩ粯璁ゅ€硷紒鎶涘嚭寮傚父
                // 鏃ユ湡锛?026-02-10
                throw new InvalidOperationException(
                    $"Architecture {architecture.ID} ({architecture.Name}) has invalid KindID: {architecture.KindID}. " +
                    $"This indicates the Architecture was saved without a valid Kind, which is a data integrity issue that must be fixed.");
            }
            
            // 馃敟 淇锛氶摼鎺?LocationState 寮曠敤
            // 鏃ユ湡锛?026-03-17
            // 闂锛?鎵€鍦ㄥ窞"鍜?鍦板煙"鏄剧ず涓?鏈煡宸炲煙"鍜?鏈煡鍦板尯"
            // 鍘熷洜锛欰rchitecture.LocationState 寮曠敤浠庢潵娌℃湁琚摼鎺ヨ繃
            // ID 鍒ゆ柇锛欼D=0 鏄湁鏁堢殑锛堝徃闅?ID=0锛夛紝蹇呴』浣跨敤 >= 0
            if (architecture.StateID >= 0 && lookupTables.States.TryGetValue(architecture.StateID, out State locationState))
            {
                architecture.LocationState = locationState;
            }
            else if (architecture.StateID >= 0)
            {
                LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent State {architecture.StateID}");
                architecture.StateID = -1;
                architecture.LocationState = null;
            }
            else
            {
                architecture.LocationState = null;
            }
            
            // 馃敟 淇锛氶摼鎺?BelongedFaction 寮曠敤锛屽苟妫€娴嬫暟鎹畬鏁存€?
            // 鏃ユ湡锛?026-02-13
            // 娉ㄦ剰锛欱elongedFactionID 鍙互鏄?-1锛堢┖鍩庯級锛?= 0 鏄湁鍔垮姏鐨勫煄甯?
            
            // 馃敟 璇婃柇锛氳褰曟礇闃崇殑閾炬帴杩囩▼
            // 鏃ユ湡锛?026-03-16
            /*if (architecture.ID == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[LinkReferences] 閾炬帴娲涢槼 (ID=0):");
                System.Diagnostics.Debug.WriteLine($"  - BelongedFactionID: {architecture.BelongedFactionID}");
                System.Diagnostics.Debug.WriteLine($"  - lookupTables.Factions.Count: {lookupTables.Factions.Count}");
                if (architecture.BelongedFactionID >= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  - lookupTables.Factions 鍖呭惈 ID {architecture.BelongedFactionID}: {lookupTables.Factions.ContainsKey(architecture.BelongedFactionID)}");
                }
            }*/
            
            if (architecture.BelongedFactionID >= 0)
            {
                if (lookupTables.Factions.TryGetValue(architecture.BelongedFactionID, out Faction faction))
                {
                    architecture.BelongedFaction = faction;
                    
                    // 馃敟 璇婃柇锛氱‘璁ら摼鎺ユ垚鍔?
                    /*if (architecture.ID == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - 鉁?閾炬帴鎴愬姛: {faction.Name}");
                    }*/
                }
                else
                {
                    LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent Faction {architecture.BelongedFactionID}");
                    // System.Diagnostics.Debug.WriteLine($"[LinkReferences] 鉂?Architecture {architecture.ID} ({architecture.Name}) 寮曠敤鐨勫娍鍔?{architecture.BelongedFactionID} 涓嶅瓨鍦?);
                    
                    // 馃敟 Cold Path锛氳瘖鏂棩蹇楋紝鍙鎬т紭鍏?
                    var availableIds = new System.Text.StringBuilder();
                    int count = 0;
                    foreach (var id in lookupTables.Factions.Keys)
                    {
                        if (count > 0) availableIds.Append(", ");
                        availableIds.Append(id);
                        if (++count >= 20) break;
                    }
                    // System.Diagnostics.Debug.WriteLine($"[LinkReferences] 鍙敤鐨勫娍鍔?IDs: {availableIds}");
                    
                    throw new InvalidOperationException($"Architecture {architecture.ID} ({architecture.Name}) references missing Faction ID={architecture.BelongedFactionID}.");
                }
            }
            else if (architecture.BelongedFactionID < -1)
            {
                // BelongedFactionID < -1 鏄暟鎹敊璇?
                LogWarning($"Architecture {architecture.ID} ({architecture.Name}) has invalid BelongedFactionID: {architecture.BelongedFactionID}");
                throw new InvalidOperationException($"Architecture {architecture.ID} ({architecture.Name}) has invalid BelongedFactionID={architecture.BelongedFactionID} (< -1).");
            }
            else // BelongedFactionID == -1
            {
                // 馃敟 璇婃柇锛氳褰曠┖鍩?
                /*if (architecture.ID == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  - 鈿狅笍 BelongedFactionID=-1锛岃繖鏄┖鍩?);
                }*/
            }
            // BelongedFactionID == -1 琛ㄧず绌哄煄锛孊elongedFaction 鑷劧淇濇寔涓?null锛屾棤闇€鏄惧紡璁剧疆
            
            // 馃敟 淇锛氶摼鎺?BelongedSection 寮曠敤锛屽苟妫€娴嬫暟鎹畬鏁存€?
            // 鏃ユ湡锛?026-02-13
            // 娉ㄦ剰锛欱elongedSectionID 鍙互鏄?-1锛堟病鏈夊啗鍖猴級锛?= 0 鏄湁鍐涘尯鐨勫煄甯?
            if (architecture.BelongedSectionID >= 0)
            {
                if (lookupTables.Sections.TryGetValue(architecture.BelongedSectionID, out Section section))
                {
                    architecture.BelongedSection = section;
                }
                else
                {
                    LogWarning($"Architecture {architecture.ID} ({architecture.Name}) references non-existent Section {architecture.BelongedSectionID}");
                    throw new InvalidOperationException($"Architecture {architecture.ID} ({architecture.Name}) references missing Section ID={architecture.BelongedSectionID}.");
                }
            }
            else if (architecture.BelongedSectionID < -1)
            {
                // BelongedSectionID < -1 鏄暟鎹敊璇?
                LogWarning($"Architecture {architecture.ID} ({architecture.Name}) has invalid BelongedSectionID: {architecture.BelongedSectionID}");
                throw new InvalidOperationException($"Architecture {architecture.ID} ({architecture.Name}) has invalid BelongedSectionID={architecture.BelongedSectionID} (< -1).");
            }
            // else: BelongedSectionID == -1 琛ㄧず娌℃湁鍐涘尯锛孊elongedSection 鑷劧淇濇寔涓?null锛屾棤闇€鏄惧紡璁剧疆
            
            if (architecture.MayorID > 0 && lookupTables.Persons.TryGetValue(architecture.MayorID, out Person mayor))
                architecture.Mayor = mayor;
            
            // 馃敟 鍏抽敭璇存槑锛欰rchitecture.Persons 鏄绠楀睘鎬э紝涓嶈兘鐩存帴淇敼
            // 鏃ユ湡锛?026-03-16
            // Architecture.Persons 閫氳繃 Session.Current.Scenario.GetPersonList(this) 鑾峰彇
            // 杩斿洖鐨勬槸 immutable list锛屼笉鑳?Clear() 鎴?Add()
            // 姝﹀皢鏁版嵁鐢?Scenario 鐨勪腑蹇冨寲瀛樺偍绠＄悊锛屼笉鍦?LinkReferencesPhase 涓鐞?
            
            // Link collections (璺宠繃 Persons 鍜?Militaries锛屽畠浠敱 Scenario 绠＄悊)
            /*
            if (architecture.Persons == null) architecture.Persons = new PersonList(); else architecture.Persons.Clear();
            if (architecture.PersonIDs != null)
                foreach (int id in architecture.PersonIDs)
                    if (lookupTables.Persons.TryGetValue(id, out Person person))
                        architecture.Persons.Add(person);
            
            if (architecture.Militaries == null) architecture.Militaries = new MilitaryList(); else architecture.Militaries.Clear();
            if (architecture.MilitaryIDs != null)
                foreach (int id in architecture.MilitaryIDs)
                    if (lookupTables.Militaries.TryGetValue(id, out Military military))
                        architecture.Militaries.Add(military);
            */
            
            if (architecture.Facilities == null) architecture.Facilities = new FacilityList(); else architecture.Facilities.Clear();
            if (architecture.FacilityIDs != null)
            {
                // 馃敟 2026-03-16 璇婃柇锛氳褰曡鏂介摼鎺ヨ繃绋?
                /*if (architecture.FacilityIDs.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[LinkReferences] 寤虹瓚 {architecture.Name}(ID:{architecture.ID}) 鏈?{architecture.FacilityIDs.Count} 涓鏂絀D闇€瑕侀摼鎺?);
                    System.Diagnostics.Debug.WriteLine($"  - lookupTables.Facilities.Count: {lookupTables.Facilities.Count}");
                }*/
                
                foreach (int id in architecture.FacilityIDs)
                {
                    if (lookupTables.Facilities.TryGetValue(id, out Facility facility))
                    {
                        architecture.Facilities.Add(facility);
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"  - 鉂?璁炬柦 ID={id} 鍦?lookupTables.Facilities 涓笉瀛樺湪");
                    }
                }
                
                /*if (architecture.FacilityIDs.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  - 鉁?鎴愬姛閾炬帴 {architecture.Facilities.Count}/{architecture.FacilityIDs.Count} 涓鏂?);
                }*/
            }
            
            // 馃敟 淇锛氳В鏋?Characteristics锛堝煄甯傜壒鑹诧級
            // 鏃ユ湡锛?026-03-17
            // 闂锛欳haracteristicsString 琚纭姞杞斤紝浣嗘病鏈夎瑙ｆ瀽鎴?Characteristics 闆嗗悎瀵硅薄
            LinkCharacteristics(architecture, scenario);
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        private void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[LinkReferencesPhase] WARNING: {message}");
        }
    }

    /// <summary>
    /// Reference linker for Legion objects
    /// Links BelongedFaction, Leader, and Troops collection
    /// </summary>
    public class LegionReferenceLinker : IReferenceLinker<Legion>
    {
        /// <summary>
        /// Link all references for a Legion
        /// </summary>
        public void LinkReferences(Legion legion, GameScenario scenario)
        {
            if (legion == null || scenario == null)
                return;
            
            // Link BelongedFaction reference
            LinkBelongedFaction(legion, scenario);
            
            // Link Leader reference
            LinkLeader(legion, scenario);
            
            // 馃敟 鏂板锛歀ink Target reference (鏂板瓧娈?
            // 鏃ユ湡锛?026-03-22
            // 鍘熷洜锛氳妗ｅ悗鍐涘洟鐨勭洰鏍囧紩鐢ㄦ病鏈夎鎭㈠锛屽鑷翠娇鐢ㄩ敊璇殑鐩爣
            LinkTarget(legion, scenario);
            
            // 馃敟 鏂板锛歀ink WillArchitecture reference (鏃у瓧娈碉紝淇濇寔鍏煎鎬?
            // 鏃ユ湡锛?026-03-22
            LinkWillArchitecture(legion, scenario);
            
            // Link Troops collection
            LinkTroops(legion, scenario);
            RecoverMissingMission(legion, scenario);
        }
        
        /// <summary>
        /// Link BelongedFaction reference
        /// </summary>
        private void LinkBelongedFaction(Legion legion, GameScenario scenario)
        {
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堟眽鍔垮姏锛夛紝蹇呴』浣跨敤 >= 0
            // 鏃ユ湡锛?026-03-16
            if (legion.BelongedFactionID >= 0)
            {
                Faction faction = scenario.Factions.GetGameObject(legion.BelongedFactionID) as Faction;
                
                if (faction != null)
                {
                    legion.BelongedFaction = faction;
                }
                else
                {
                    LogWarning($"Legion {legion.ID} ({legion.Name}) references non-existent Faction {legion.BelongedFactionID}");
                    legion.BelongedFactionID = -1;
                    legion.BelongedFaction = null;
                }
            }
            else
            {
                legion.BelongedFaction = null;
            }
        }
        
        /// <summary>
        /// Link Leader reference
        /// </summary>
        private void LinkLeader(Legion legion, GameScenario scenario)
        {
            if (legion.LeaderID >= 0)
            {
                Person leader = scenario.Persons.GetGameObject(legion.LeaderID) as Person;
                
                if (leader != null)
                {
                    legion.Leader = leader;
                }
                else
                {
                    LogWarning($"Legion {legion.ID} ({legion.Name}) references non-existent Leader {legion.LeaderID}");
                    legion.LeaderID = -1;
                    legion.Leader = null;
                }
            }
            else
            {
                legion.Leader = null;
            }
        }
        
        /// <summary>
        /// 馃敟 鏂板锛歀ink Target reference (鏂板瓧娈?
        /// 鏃ユ湡锛?026-03-22
        /// 鍘熷洜锛氳妗ｅ悗鍐涘洟鐨勭洰鏍囧紩鐢ㄦ病鏈夎鎭㈠
        /// </summary>
        private void LinkTarget(Legion legion, GameScenario scenario)
        {
            // 馃敟 鍏抽敭锛欼D=0 鏄湁鏁堢殑锛堟礇闃筹級锛屽繀椤讳娇鐢?>= 0
            if (legion.TargetArchitectureID >= 0)
            {
                Architecture target = scenario.Architectures.GetGameObject(legion.TargetArchitectureID) as Architecture;
                
                if (target != null)
                {
                    legion.Target = target;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[LegionLoad][Link] 军团 {legion.Name}(ID:{legion.ID}) " +
                        $"閾炬帴 Target: {target.Name}(ID:{target.ID}) 褰掑睘:{target.BelongedFaction?.Name ?? "null"}");
                    #endif
                }
                else
                {
                    LogWarning($"Legion {legion.ID} ({legion.Name}) references non-existent Target {legion.TargetArchitectureID}");
                    legion.TargetArchitectureID = -1;
                    legion.Target = null;
                }
            }
            else
            {
                legion.Target = null;
            }
        }
        
        /// <summary>
        /// 馃敟 鏂板锛歀ink WillArchitecture reference (鏃у瓧娈碉紝淇濇寔鍏煎鎬?
        /// 鏃ユ湡锛?026-03-22
        /// 鍘熷洜锛氭棫瀛樻。浣跨敤 WillArchitectureString 瀛楁
        /// </summary>
        private void LinkWillArchitecture(Legion legion, GameScenario scenario)
        {
            // 馃敟 鍏抽敭锛欼D=0 鏄湁鏁堢殑锛堟礇闃筹級锛屽繀椤讳娇鐢?>= 0
            if (legion.WillArchitectureString >= 0)
            {
                Architecture willArch = scenario.Architectures.GetGameObject(legion.WillArchitectureString) as Architecture;
                
                if (willArch != null)
                {
                    legion.WillArchitecture = willArch;
                    
                    // 馃敟 鍏煎鎬э細濡傛灉 Target 涓虹┖锛屼娇鐢?WillArchitecture
                    if (legion.Target == null)
                    {
                        legion.Target = willArch;
                        legion.TargetArchitectureID = willArch.ID;
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine(
                            $"[LegionLoad][Link] 军团 {legion.Name}(ID:{legion.ID}) " +
                            $"浠?WillArchitecture 鎭㈠ Target: {willArch.Name}(ID:{willArch.ID})");
                        #endif
                    }
                }
                else
                {
                    LogWarning($"Legion {legion.ID} ({legion.Name}) references non-existent WillArchitecture {legion.WillArchitectureString}");
                    legion.WillArchitectureString = -1;
                    legion.WillArchitecture = null;
                }
            }
            else
            {
                legion.WillArchitecture = null;
            }
        }
        
        private static void RecoverMissingMission(Legion legion, GameScenario scenario)
        {
            if (legion.Kind != LegionKind.AI || legion.Mission != LegionMission.None)
            {
                return;
            }

            Architecture target = legion.Target ?? legion.WillArchitecture;
            if (target == null)
            {
                target = TryResolveTargetFromLegionName(legion, scenario);
                if (target != null)
                {
                    legion.Target = target;
                    legion.TargetArchitectureID = target.ID;
                    if (legion.WillArchitecture == null)
                    {
                        legion.WillArchitecture = target;
                        legion.WillArchitectureString = target.ID;
                    }
                    System.Diagnostics.Debug.WriteLine(
                        $"[LegionLoad][Recover] Target by legion name: {legion.Name} -> {target.Name}(ID:{target.ID})");
                }
            }

            if (target != null && !string.IsNullOrEmpty(legion.Name))
            {
                LegionMission[] candidates =
                [
                    LegionMission.Attack,
                    LegionMission.Defend,
                    LegionMission.Retreat,
                    LegionMission.Patrol
                ];

                for (int i = 0; i < candidates.Length; i++)
                {
                    LegionMission candidate = candidates[i];
                    string expectedName = LegionKindConverter.GenerateLegionName(legion.Kind, candidate, target);
                    if (string.Equals(legion.Name, expectedName, StringComparison.Ordinal))
                    {
                        legion.Mission = candidate;
                        System.Diagnostics.Debug.WriteLine(
                            $"[LegionLoad][Recover] Mission by name: " +
                            $"{legion.Name} -> {candidate} (Target:{target.Name}, ID:{legion.ID})");
                        return;
                    }
                }
            }

            if (target != null && legion.BelongedFaction != null)
            {
                legion.Mission = target.BelongedFaction == legion.BelongedFaction
                    ? LegionMission.Defend
                    : LegionMission.Attack;
                System.Diagnostics.Debug.WriteLine(
                    $"[LegionLoad][Recover] Mission by faction relation: " +
                    $"{legion.Name} -> {legion.Mission} (Target:{target.Name}, TargetFaction:{target.BelongedFaction?.Name ?? "null"}, LegionFaction:{legion.BelongedFaction.Name}, ID:{legion.ID})");
                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[LegionLoad][Warn] Legion {legion.ID} ({legion.Name}) has Mission=None and cannot be recovered.");
        }

        private static Architecture TryResolveTargetFromLegionName(Legion legion, GameScenario scenario)
        {
            if (scenario?.Architectures == null || string.IsNullOrEmpty(legion?.Name))
            {
                return null;
            }

            int lastUnderscore = legion.Name.LastIndexOf('_');
            if (lastUnderscore < 0 || lastUnderscore >= legion.Name.Length - 1)
            {
                return null;
            }

            string targetName = legion.Name[(lastUnderscore + 1)..].Trim();
            if (string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            foreach (Architecture architecture in scenario.Architectures.GetList())
            {
                if (architecture != null && string.Equals(architecture.Name, targetName, StringComparison.Ordinal))
                {
                    return architecture;
                }
            }

            return null;
        }

        /// <summary>
        /// Link Troops collection
        /// </summary>
        private void LinkTroops(Legion legion, GameScenario scenario)
        {
            // Clear existing collection
            if (legion.Troops == null)
                legion.Troops = new TroopList();
            else
                legion.Troops.Clear();
            
            // Link each troop by ID
            if (legion.TroopIDs != null && legion.TroopIDs.Count > 0)
            {
                foreach (int troopID in legion.TroopIDs)
                {
                    Troop troop = scenario.Troops.GetGameObject(troopID) as Troop;
                    
                    if (troop != null)
                    {
                        legion.Troops.Add(troop);
                    }
                    else
                    {
                        LogWarning($"Legion {legion.ID} ({legion.Name}) references non-existent Troop {troopID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        private void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[LegionLoad][Warn] {message}");
        }
    }

    /// <summary>
    /// Reference linker for Troop objects
    /// Links all object references (Leader, Army, BelongedFaction, BelongedLegion, BelongedArchitecture, Persons)
    /// </summary>
    public class TroopReferenceLinker : IReferenceLinker<Troop>
    {
        /// <summary>
        /// Link all references for a Troop
        /// </summary>
        public void LinkReferences(Troop troop, GameScenario scenario)
        {
            if (troop == null || scenario == null)
                return;
            
            // Link BelongedFaction reference
            LinkBelongedFaction(troop, scenario);
            
            // Link BelongedLegion reference
            LinkBelongedLegion(troop, scenario);
            
            // Link BelongedArchitecture reference
            LinkBelongedArchitecture(troop, scenario);
            
            // 馃敟 淇锛歀ink StartingArchitecture reference
            LinkStartingArchitecture(troop, scenario);
            
            // Link Leader reference
            LinkLeader(troop, scenario);
            
            // Link Army (Military) reference
            LinkArmy(troop, scenario);
            
            // Link Persons collection
            LinkPersons(troop, scenario);
            
            // 馃敟 淇锛歀ink CurrentStunt reference锛堥槻姝㈣妗ｅ悗鑷姩瑙﹀彂鐗规妧锛?
            LinkCurrentStunt(troop, scenario);
        }
        
        /// <summary>
        /// Link BelongedFaction reference
        /// </summary>
        private void LinkBelongedFaction(Troop troop, GameScenario scenario)
        {
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堟眽鍔垮姏锛夛紝蹇呴』浣跨敤 >= 0
            // 鏃ユ湡锛?026-03-16
            if (troop.BelongedFactionID >= 0)
            {
                Faction faction = scenario.Factions.GetGameObject(troop.BelongedFactionID) as Faction;
                
                if (faction != null)
                {
                    troop.BelongedFaction = faction;
                }
                else
                {
                    LogWarning($"Troop {troop.ID} ({troop.Name}) references non-existent Faction {troop.BelongedFactionID}");
                    troop.BelongedFactionID = -1;
                    troop.BelongedFaction = null;
                }
            }
            else
            {
                troop.BelongedFaction = null;
            }
        }
        
        /// <summary>
        /// Link BelongedLegion reference
        /// </summary>
        private void LinkBelongedLegion(Troop troop, GameScenario scenario)
        {
            if (troop.BelongedLegionID >= 0)
            {
                Legion legion = scenario.Legions.GetGameObject(troop.BelongedLegionID) as Legion;
                
                if (legion != null)
                {
                    troop.BelongedLegion = legion;
                }
                else
                {
                    LogWarning($"Troop {troop.ID} ({troop.Name}) references non-existent Legion {troop.BelongedLegionID}");
                    troop.BelongedLegionID = -1;
                    troop.BelongedLegion = null;
                }
            }
            else
            {
                troop.BelongedLegion = null;
            }
        }
        
        /// <summary>
        /// Link BelongedArchitecture reference
        /// </summary>
        private void LinkBelongedArchitecture(Troop troop, GameScenario scenario)
        {
            if (troop.BelongedArchitectureID > 0)
            {
                Architecture architecture = scenario.Architectures.GetGameObject(troop.BelongedArchitectureID) as Architecture;
                
                if (architecture != null)
                {
                    troop.BelongedArchitecture = architecture;
                }
                else
                {
                    LogWarning($"Troop {troop.ID} ({troop.Name}) references non-existent Architecture {troop.BelongedArchitectureID}");
                    troop.BelongedArchitectureID = -1;
                    troop.BelongedArchitecture = null;
                }
            }
            else
            {
                troop.BelongedArchitecture = null;
            }
        }
        
        /// <summary>
        /// 馃敟 淇锛歀ink StartingArchitecture reference
        /// </summary>
        private void LinkStartingArchitecture(Troop troop, GameScenario scenario)
        {
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堟礇闃崇殑 ID 灏辨槸 0锛夛紝蹇呴』浣跨敤 >= 0
            if (troop.StartingArchitectureID >= 0)
            {
                var architecture = scenario.Architectures.GetGameObject(troop.StartingArchitectureID) as Architecture;
                
                if (architecture is not null)
                {
                    troop.StartingArchitecture = architecture;
                    System.Diagnostics.Debug.WriteLine($"[LinkStartingArchitecture] 鉁?閮ㄩ槦 {troop.ID} ({troop.Name}) 閾炬帴鍑哄彂鍩庡競: {architecture.Name}");
                }
                else
                {
                    LogWarning($"Troop {troop.ID} ({troop.Name}) references non-existent StartingArchitecture {troop.StartingArchitectureID}");
                    System.Diagnostics.Debug.WriteLine($"[LinkStartingArchitecture] WARNING: Troop {troop.ID} ({troop.Name}) missing StartingArchitectureID={troop.StartingArchitectureID}.");
                    troop.StartingArchitectureID = -1;
                    troop.StartingArchitecture = null;
                }
            }
            else
            {
                troop.StartingArchitecture = null;
                
                // 馃敟 鏍规湰淇锛氭鏌?ID=0 鏄惁鍚堟硶
                // 鏃ユ湡锛?026-03-07
                // 鍘熷洜锛欼D=0 鍙兘鏄悎娉曠殑娲涢槼锛屼篃鍙兘鏄暟鎹崯鍧?
                // 瑙ｅ喅锛氬皾璇曞姞杞?ID=0 鐨勫缓绛戯紝妫€鏌ユ槸鍚﹀睘浜庨儴闃熺殑鍔垮姏
                //       濡傛灉涓嶅睘浜庯紝鍒欎慨姝ｄ负 -1
                if (troop.StartingArchitectureID == 0)
                {
                    var arch0 = scenario.Architectures.GetGameObject(0) as Architecture;
                    if (arch0 != null && arch0.BelongedFaction == troop.BelongedFaction)
                    {
                        // ID=0 鐨勫煄甯傚睘浜庨儴闃熺殑鍔垮姏锛屽悎娉?
                        troop.StartingArchitecture = arch0;
                        System.Diagnostics.Debug.WriteLine($"[LinkStartingArchitecture] 鉁?閮ㄩ槦 {troop.ID} ({troop.Name}) 鐨勫嚭鍙戝煄甯傛槸 {arch0.Name}锛圛D=0锛屽悎娉曪級");
                    }
                    else
                    {
                        // ID=0 鐨勫煄甯備笉灞炰簬閮ㄩ槦鐨勫娍鍔涳紝鎴栬€呬笉瀛樺湪锛屾暟鎹崯鍧?
                        System.Diagnostics.Debug.WriteLine($"[LinkStartingArchitecture] 鉂?閮ㄩ槦 {troop.ID} ({troop.Name}) 鐨?StartingArchitectureID=0 浣嗗煄甯備笉灞炰簬璇ュ娍鍔涳紙鏁版嵁鎹熷潖锛夛紝淇涓?-1");
                        troop.StartingArchitectureID = -1;
                    }
                }
            }
        }
        
        /// <summary>
        /// Link Leader reference
        /// </summary>
        private void LinkLeader(Troop troop, GameScenario scenario)
        {
            if (troop.LeaderID >= 0)
            {
                Person leader = scenario.Persons.GetGameObject(troop.LeaderID) as Person;
                
                if (leader != null)
                {
                    troop.Leader = leader;
                }
                else
                {
                    LogWarning($"Troop {troop.ID} ({troop.Name}) references non-existent Leader {troop.LeaderID}");
                    troop.LeaderID = -1;
                    troop.Leader = null;
                }
            }
            else
            {
                troop.Leader = null;
            }
        }
        
        /// <summary>
        /// Link Army (Military) reference
        /// </summary>
        private void LinkArmy(Troop troop, GameScenario scenario)
        {
            // 馃敟 鏍规湰淇锛歁ilitaryID >= 0 鎵嶆槸鏈夋晥寮曠敤锛圛D=0 鏄鍏碉級
            // 鏃ユ湡锛?026-03-20
            // 闂锛氫箣鍓嶄娇鐢?> 0 鍒ゆ柇锛屽鑷?MilitaryID=0锛堟鍏碉級琚烦杩?
            // 鏍规嵁 ID鍒ゆ柇瑙勮寖.md锛欼D=0 鏄湁鏁堢殑锛圡ilitaryKind ID=0 鏄鍏碉級
            // 瑙ｅ喅锛氫娇鐢?>= 0 鍒ゆ柇锛岀鍚堥」鐩鑼?
            if (troop.MilitaryID >= 0)
            {
                Military army = scenario.Militaries.GetGameObject(troop.MilitaryID) as Military;
                
                if (army != null)
                {
                    troop.Army = army;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LinkArmy] 鈿狅笍 璀﹀憡锛歍roop {troop.ID} ({troop.Name}) 寮曠敤浜嗕笉瀛樺湪鐨?Military {troop.MilitaryID}");
                    System.Diagnostics.Debug.WriteLine($"  - Militaries.Count: {scenario.Militaries.Count}");
                    troop.MilitaryID = -1;
                    troop.Army = null;
                }
            }
            else
            {
                // MilitaryID < 0 琛ㄧず鏃犳晥寮曠敤锛?1 琛ㄧず鏃?Army锛?
                troop.Army = null;
            }
        }
        
        /// <summary>
        /// Link Persons collection
        /// </summary>
        private void LinkPersons(Troop troop, GameScenario scenario)
        {
            // 馃敟 淇锛氭仮澶?Persons 闆嗗悎鐨勯摼鎺?
            // 鏃ユ湡锛?026-02-12
            // 闂锛氳妗ｅ悗 troop.Persons 涓虹┖锛屽鑷?閮ㄩ槦浜虹墿"鑿滃崟鏃犳晥
            // 瑙ｅ喅锛氬彇娑堟敞閲婏紝姝ｇ‘閾炬帴 Persons 闆嗗悎
            // 娉ㄦ剰锛歅ersons 鏄彧璇诲睘鎬э紝涓嶈兘璧嬪€硷紝鍙兘璋冪敤 Clear() 鍜?Add()
            
            // Clear existing collection (Persons 鏄彧璇诲睘鎬э紝杩斿洖绉佹湁瀛楁 persons)
            troop.Persons.Clear();
            
            // Link each person by ID
            if (troop.PersonIDs is { Count: > 0 })
            {
                foreach (var personID in troop.PersonIDs)
                {
                    var person = scenario.Persons.GetGameObject(personID) as Person;
                    
                    // 馃敟 ANTI-BAND-AID: 涓嶆帺鐩栨暟鎹敊璇紝鐩存帴鎶涘嚭寮傚父
                    // 鏃ユ湡锛?026-02-12
                    // 濡傛灉 Person 涓嶅瓨鍦紝璇存槑鏁版嵁鎹熷潖锛屽繀椤诲湪鏍规簮淇
                    if (person is null)
                    {
                        throw new InvalidOperationException(
                            $"Data corruption: Troop {troop.ID} ({troop.Name}) references missing Person {personID}. PersonIDs=[{string.Join(", ", troop.PersonIDs)}].");
                    }
                    
                    troop.Persons.Add(person);
                }
            }
        }
        
        /// <summary>
        /// 馃敟 淇锛歀ink CurrentStunt reference
        /// 鏃ユ湡锛?026-02-12
        /// 闂锛氳妗ｅ悗 CurrentStunt 涓?null锛屽鑷?AI 璇垽骞惰嚜鍔ㄨЕ鍙戠壒鎶€
        /// 瑙ｅ喅锛氭牴鎹?CurrentStuntIDString 鎭㈠ CurrentStunt 寮曠敤
        /// </summary>
        private void LinkCurrentStunt(Troop troop, GameScenario scenario)
        {
            if (troop.CurrentStuntIDString <= 0)
            {
                troop.CurrentStunt = null;
                return;
            }
            
            var stunt = scenario.GameCommonData.AllStunts.GetStunt(troop.CurrentStuntIDString);
            
            if (stunt is not null)
            {
                troop.CurrentStunt = stunt;
                System.Diagnostics.Debug.WriteLine($"[LinkCurrentStunt] 鉁?閮ㄩ槦 {troop.ID} ({troop.Name}) 鎭㈠鐗规妧: {stunt.Name}, 鍓╀綑澶╂暟: {troop.StuntDayLeft}");
                System.Diagnostics.Debug.WriteLine($"[LinkCurrentStunt]   - 鏄惁鐜╁鍔垮姏: {troop.BelongedFaction != null && scenario.IsPlayer(troop.BelongedFaction)}");
                System.Diagnostics.Debug.WriteLine($"[LinkCurrentStunt]   - 閮ㄩ槦鍙敤鐗规妧鏁? {troop.Stunts.Count}");
            }
            else
            {
                LogWarning($"Troop {troop.ID} ({troop.Name}) references non-existent Stunt {troop.CurrentStuntIDString}");
                troop.CurrentStuntIDString = -1;
                troop.CurrentStunt = null;
            }
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        private void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[LinkReferencesPhase] WARNING: {message}");
        }
    }

    /// <summary>
    /// Reference linker for Section objects
    /// Links BelongedFaction, AIDetail, and Architectures collection
    /// </summary>
    public class SectionReferenceLinker : IReferenceLinker<Section>
    {
        /// <summary>
        /// Link all references for a Section
        /// </summary>
        public void LinkReferences(Section section, GameScenario scenario)
        {
            if (section == null || scenario == null)
                return;
            
            // Link BelongedFaction reference
            LinkBelongedFaction(section, scenario);
            
            // Link AIDetail reference (if applicable)
            // LinkAIDetail(section, scenario);
            
            // Link Architectures collection
            LinkArchitectures(section, scenario);
        }
        
        /// <summary>
        /// Link BelongedFaction reference
        /// </summary>
        private void LinkBelongedFaction(Section section, GameScenario scenario)
        {
            // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堟眽鍔垮姏锛夛紝蹇呴』浣跨敤 >= 0
            // 鏃ユ湡锛?026-03-16
            if (section.BelongedFactionID >= 0)
            {
                Faction faction = scenario.Factions.GetGameObject(section.BelongedFactionID) as Faction;
                
                if (faction != null)
                {
                    section.BelongedFaction = faction;
                }
                else
                {
                    LogWarning($"Section {section.ID} ({section.Name}) references non-existent Faction {section.BelongedFactionID}");
                    section.BelongedFactionID = -1;
                    section.BelongedFaction = null;
                }
            }
            else
            {
                section.BelongedFaction = null;
            }
        }
        
        /// <summary>
        /// Link Architectures collection
        /// </summary>
        private void LinkArchitectures(Section section, GameScenario scenario)
        {
            // Clear existing collection
            if (section.Architectures == null)
                section.Architectures = new ArchitectureList();
            else
                section.Architectures.Clear();
            
            // Link each architecture by ID
            if (section.ArchitectureIDs != null && section.ArchitectureIDs.Count > 0)
            {
                foreach (int architectureID in section.ArchitectureIDs)
                {
                    Architecture architecture = scenario.Architectures.GetGameObject(architectureID) as Architecture;
                    
                    if (architecture != null)
                    {
                        section.Architectures.Add(architecture);
                    }
                    else
                    {
                        LogWarning($"Section {section.ID} ({section.Name}) references non-existent Architecture {architectureID}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        private void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[LinkReferencesPhase] WARNING: {message}");
        }
    }


// ============================================================================
// PERFORMANCE OPTIMIZATION EXTENSIONS
// ============================================================================
// The following extensions add O(1) lookup support to the remaining linkers
// These methods are called by LinkReferencesPhase when lookup tables are available
// ============================================================================

// Extension for LegionReferenceLinker
public static class LegionReferenceLinkerExtensions
{
    public static void LinkReferences(this LegionReferenceLinker linker, Legion legion, GameScenario scenario, LookupTables lookupTables)
    {
        if (legion == null || lookupTables == null) return;
        
        if (legion.BelongedFactionID >= 0 && lookupTables.Factions.TryGetValue(legion.BelongedFactionID, out Faction faction))
            legion.BelongedFaction = faction;
        else
        {
            if (legion.BelongedFactionID >= 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[LegionLoad][Warn] Legion {legion.ID} ({legion.Name}) missing Faction {legion.BelongedFactionID} in fast link.");
            }
            legion.BelongedFaction = null;
        }
        
        if (legion.LeaderID >= 0 && lookupTables.Persons.TryGetValue(legion.LeaderID, out Person leader))
            legion.Leader = leader;
        else
        {
            if (legion.LeaderID >= 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[LegionLoad][Warn] Legion {legion.ID} ({legion.Name}) missing Leader {legion.LeaderID} in fast link.");
            }
            legion.Leader = null;
        }

        if (legion.TargetArchitectureID >= 0 && lookupTables.Architectures.TryGetValue(legion.TargetArchitectureID, out Architecture target))
            legion.Target = target;
        else
        {
            if (legion.TargetArchitectureID >= 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[LegionLoad][Warn] Legion {legion.ID} ({legion.Name}) missing TargetArchitecture {legion.TargetArchitectureID} in fast link.");
            }
            legion.Target = null;
        }

        if (legion.WillArchitectureString >= 0 && lookupTables.Architectures.TryGetValue(legion.WillArchitectureString, out Architecture willArch))
            legion.WillArchitecture = willArch;
        else
        {
            if (legion.WillArchitectureString >= 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[LegionLoad][Warn] Legion {legion.ID} ({legion.Name}) missing WillArchitecture {legion.WillArchitectureString} in fast link.");
            }
            legion.WillArchitecture = null;
        }

        if (legion.Target == null && legion.WillArchitecture != null)
        {
            legion.Target = legion.WillArchitecture;
            legion.TargetArchitectureID = legion.WillArchitecture.ID;
        }

        if (legion.Troops == null) legion.Troops = new TroopList(); else legion.Troops.Clear();
        if (legion.TroopIDs != null)
            foreach (int id in legion.TroopIDs)
                if (lookupTables.Troops.TryGetValue(id, out Troop troop))
                    legion.Troops.Add(troop);

        RecoverMissingMissionFast(legion, lookupTables);
    }

    private static void RecoverMissingMissionFast(Legion legion, LookupTables lookupTables)
    {
        if (legion.Kind != LegionKind.AI || legion.Mission != LegionMission.None)
        {
            return;
        }

        Architecture target = legion.Target ?? legion.WillArchitecture;
        if (target == null)
        {
            target = TryResolveTargetFromLegionNameFast(legion, lookupTables);
            if (target != null)
            {
                legion.Target = target;
                legion.TargetArchitectureID = target.ID;
                if (legion.WillArchitecture == null)
                {
                    legion.WillArchitecture = target;
                    legion.WillArchitectureString = target.ID;
                }
                System.Diagnostics.Debug.WriteLine(
                    $"[LegionLoad][Recover] Target by legion name (fast): {legion.Name} -> {target.Name}(ID:{target.ID})");
            }
        }

        if (target != null && !string.IsNullOrEmpty(legion.Name))
        {
            LegionMission[] candidates =
            [
                LegionMission.Attack,
                LegionMission.Defend,
                LegionMission.Retreat,
                LegionMission.Patrol
            ];

            for (int i = 0; i < candidates.Length; i++)
            {
                LegionMission candidate = candidates[i];
                string expectedName = LegionKindConverter.GenerateLegionName(legion.Kind, candidate, target);
                if (string.Equals(legion.Name, expectedName, StringComparison.Ordinal))
                {
                    legion.Mission = candidate;
                    System.Diagnostics.Debug.WriteLine(
                        $"[LegionLoad][Recover] Mission by name (fast): {legion.Name} -> {candidate} (Target:{target.Name}, ID:{legion.ID})");
                    return;
                }
            }
        }

        if (target != null && legion.BelongedFaction != null)
        {
            legion.Mission = target.BelongedFaction == legion.BelongedFaction
                ? LegionMission.Defend
                : LegionMission.Attack;
            System.Diagnostics.Debug.WriteLine(
                $"[LegionLoad][Recover] Mission by faction relation (fast): {legion.Name} -> {legion.Mission} (Target:{target.Name}, ID:{legion.ID})");
            return;
        }

        System.Diagnostics.Debug.WriteLine(
            $"[LegionLoad][Warn] Legion {legion.ID} ({legion.Name}) Mission=None cannot be recovered in fast link.");
    }

    private static Architecture TryResolveTargetFromLegionNameFast(Legion legion, LookupTables lookupTables)
    {
        if (lookupTables?.Architectures == null || string.IsNullOrEmpty(legion?.Name))
        {
            return null;
        }

        int lastUnderscore = legion.Name.LastIndexOf('_');
        if (lastUnderscore < 0 || lastUnderscore >= legion.Name.Length - 1)
        {
            return null;
        }

        string targetName = legion.Name[(lastUnderscore + 1)..].Trim();
        if (string.IsNullOrEmpty(targetName))
        {
            return null;
        }

        foreach (Architecture architecture in lookupTables.Architectures.Values)
        {
            if (architecture != null && string.Equals(architecture.Name, targetName, StringComparison.Ordinal))
            {
                return architecture;
            }
        }

        return null;
    }
}

// Extension for TroopReferenceLinker
public static class TroopReferenceLinkerExtensions
{
    public static void LinkReferences(this TroopReferenceLinker linker, Troop troop, GameScenario scenario, LookupTables lookupTables)
    {
        if (troop == null || lookupTables == null) return;
        
        if (troop.BelongedFactionID >= 0 && lookupTables.Factions.TryGetValue(troop.BelongedFactionID, out Faction faction))
            troop.BelongedFaction = faction;
        
        if (troop.BelongedLegionID >= 0 && lookupTables.Legions.TryGetValue(troop.BelongedLegionID, out Legion legion))
            troop.BelongedLegion = legion;
        
        if (troop.BelongedArchitectureID >= 0 && lookupTables.Architectures.TryGetValue(troop.BelongedArchitectureID, out Architecture architecture))
            troop.BelongedArchitecture = architecture;
        
        if (troop.LeaderID >= 0 && lookupTables.Persons.TryGetValue(troop.LeaderID, out Person leader))
            troop.Leader = leader;
        
        // 馃敟 鏍规湰淇锛氶摼鎺?StartingArchitecture 鍜?WillArchitecture
        // 鏃ユ湡锛?026-03-09
        // 闂锛氶儴闃熻妗ｅ悗 StartingArchitecture 鍜?WillArchitecture 娌℃湁琚摼鎺ワ紝瀵艰嚧寮曠敤閿欒鐨勫煄甯?
        // 瑙ｅ喅锛氫粠 lookupTables 涓仮澶嶈繖涓や釜寮曠敤
        // ANTI-BAND-AID锛氬鏋?ID 鎸囧悜涓嶅瓨鍦ㄧ殑鍩庡競锛屾姏鍑哄紓甯歌€屼笉鏄潤榛樻竻绌?
        // 馃敟 鍏抽敭淇锛欼D=0 鏄湁鏁堢殑锛堟礇闃崇殑 ID 灏辨槸 0锛夛紝蹇呴』浣跨敤 >= 0 鑰屼笉鏄?> 0
        if (troop.StartingArchitectureID >= 0)
        {
            if (lookupTables.Architectures.TryGetValue(troop.StartingArchitectureID, out Architecture startArch))
            {
                troop.StartingArchitecture = startArch;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Data corruption: Troop {troop.ID} ({troop.Name}) references missing StartingArchitecture {troop.StartingArchitectureID}.");
            }
        }
        
        if (troop.WillArchitectureID >= 0)
        {
            if (lookupTables.Architectures.TryGetValue(troop.WillArchitectureID, out Architecture willArch))
            {
                troop.WillArchitecture = willArch;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Data corruption: Troop {troop.ID} ({troop.Name}) references missing WillArchitecture {troop.WillArchitectureID}.");
            }
        }
        
        // 馃敟 2026-02-11 淇锛氬鐞?Army (Military) 寮曠敤
        // 娉ㄦ剰锛歁ilitary ID=0 鏄湁鏁堢殑锛堣櫧鐒朵笉鎺ㄨ崘锛夛紝鎵€浠ヤ娇鐢?>= 0
        if (troop.MilitaryID >= 0)
        {
            if (lookupTables.Militaries.TryGetValue(troop.MilitaryID, out Military military))
            {
                // 馃敟 2026-02-11 淇锛氶獙璇?Military 鐨?Kind 鏄惁鏈夋晥
                // Military.Kind getter 鍦?KindID 鏃犳晥鏃朵細鎶涘嚭 InvalidDataException
                // 鎵€浠ラ渶瑕佸厛妫€鏌?KindID 鍜?RealMilitaryKind
                if (military.RealKindID >= 0 && military.RealMilitaryKind != null)
                {
                    troop.Army = military;
                    military.BelongedTroop = troop;  // 鍙屽悜閾炬帴
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LinkArmy] WARNING: Troop {troop.ID} references Military {military.ID} with invalid KindID={military.RealKindID}; cleared.");
                    troop.MilitaryID = -1;
                    troop.Army = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"鈿狅笍 Troop {troop.ID} 寮曠敤浜嗕笉瀛樺湪鐨?Military {troop.MilitaryID}");
                troop.MilitaryID = -1;
                troop.Army = null;
            }
        }
        
        // 馃敟 鏍规湰淇锛氭仮澶?Persons 闆嗗悎鐨勯摼鎺?
        // 鏃ユ湡锛?026-02-12
        // 闂锛氭墿灞曟柟娉曚腑鐨?Persons 閾炬帴浠ｇ爜琚敞閲婃帀锛屽鑷磋妗ｅ悗"閮ㄩ槦浜虹墿"鑿滃崟鏃犳晥
        // 瑙ｅ喅锛氬彇娑堟敞閲婂苟鏀硅繘锛屼娇鐢?ANTI-BAND-AID 鍘熷垯锛堟姏鍑哄紓甯歌€屼笉鏄潤榛樿烦杩囷級
        troop.Persons.Clear();
        if (troop.PersonIDs is { Count: > 0 })
        {
            System.Diagnostics.Debug.WriteLine($"[LinkPersons] Troop {troop.ID} ({troop.Name}) linking {troop.PersonIDs.Count} persons...");
            
            foreach (int personID in troop.PersonIDs)
            {
                if (lookupTables.Persons.TryGetValue(personID, out Person person))
                {
                    troop.Persons.Add(person);
                    System.Diagnostics.Debug.WriteLine($"[LinkPersons]   Linked: {person.Name} (ID={personID})");
                }
                else
                {
                    // 馃敟 ANTI-BAND-AID: 涓嶆帺鐩栨暟鎹敊璇紝鐩存帴鎶涘嚭寮傚父
                    System.Diagnostics.Debug.WriteLine($"[LinkPersons] ERROR: Person ID={personID} not found in lookupTables.");
                    throw new InvalidOperationException(
                        $"Data corruption: Troop {troop.ID} ({troop.Name}) references missing Person {personID}. PersonIDs=[{string.Join(", ", troop.PersonIDs)}].");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[LinkPersons] Troop {troop.ID} ({troop.Name}) linked. Persons.Count={troop.Persons.Count}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[LinkPersons] Troop {troop.ID} ({troop.Name}) has empty PersonIDs.");
        }
    }
}

// Extension for SectionReferenceLinker
public static class SectionReferenceLinkerExtensions
{
    public static void LinkReferences(this SectionReferenceLinker linker, Section section, GameScenario scenario, LookupTables lookupTables)
    {
        if (section == null || lookupTables == null) return;
        
        // 馃敟 淇锛氶摼鎺?BelongedFaction 寮曠敤锛屽苟妫€娴嬫暟鎹畬鏁存€?
        // 鏃ユ湡锛?026-02-13
        // 娉ㄦ剰锛欱elongedFactionID 鍙互鏄?0锛堝悎娉曠殑鍔垮姏 ID锛夛紝鍙湁 < 0 鎵嶆槸鏃犳晥鐨?
        if (section.BelongedFactionID >= 0)
        {
            if (lookupTables.Factions.TryGetValue(section.BelongedFactionID, out Faction faction))
            {
                section.BelongedFaction = faction;
            }
            else
            {
                // 鏁版嵁瀹屾暣鎬ч敊璇細寮曠敤鐨勫娍鍔涗笉瀛樺湪
                // 鏋勫缓鍙敤鍔垮姏 ID 鍒楄〃锛堝墠10涓級鐢ㄤ簬璇婃柇
                var factionKeys = lookupTables.Factions.Keys;
                var sb = new System.Text.StringBuilder();
                int count = 0;
                foreach (var factionID in factionKeys)
                {
                    if (count >= 10) break;
                    if (count > 0) sb.Append(", ");
                    sb.Append(factionID);
                    count++;
                }
                
                System.Diagnostics.Debug.WriteLine($"[LinkSectionFaction] ERROR: Section {section.ID} ({section.Name}) references missing Faction ID={section.BelongedFactionID}.");
                System.Diagnostics.Debug.WriteLine($"[LinkSectionFaction]    鍙敤鐨勫娍鍔?ID锛堝墠10涓級: {sb}");
                System.Diagnostics.Debug.WriteLine($"[LinkSectionFaction]    鎬诲娍鍔涙暟: {lookupTables.Factions.Count}");
                throw new InvalidOperationException($"Section {section.ID} ({section.Name}) references missing Faction ID={section.BelongedFactionID}.");
            }
        }
        else
        {
            // 馃敟 鍚戝悗鍏煎锛欱elongedFactionID < 0锛屽皾璇曢€氳繃 Architecture 鎺ㄦ柇
            // 鏃ユ湡锛?026-03-16
            // 鍘熷洜锛氭棫鍓ф湰鍙兘娌℃湁 BelongedFactionID 瀛楁
            System.Diagnostics.Debug.WriteLine($"[LinkSectionFaction] 鈿狅笍 鍐涘尯 {section.ID} ({section.Name}) 鐨?BelongedFactionID={section.BelongedFactionID} 鏃犳晥锛屽皾璇曢€氳繃 Architecture 鎺ㄦ柇");
            
            // 閫氳繃鍐涘尯绠¤緰鐨勭涓€涓?Architecture 鎺ㄦ柇鎵€灞炲娍鍔?
            if (section.ArchitectureIDs != null && section.ArchitectureIDs.Count > 0)
            {
                int firstArchID = section.ArchitectureIDs[0];
                if (lookupTables.Architectures.TryGetValue(firstArchID, out Architecture firstArch))
                {
                    if (firstArch.BelongedFaction != null)
                    {
                        section.BelongedFaction = firstArch.BelongedFaction;
                        section.BelongedFactionID = firstArch.BelongedFaction.ID;
                        System.Diagnostics.Debug.WriteLine($"[LinkSectionFaction] 鉁?閫氳繃 Architecture {firstArch.Name} 鎺ㄦ柇鍔垮姏: {firstArch.BelongedFaction.Name} (ID:{firstArch.BelongedFaction.ID})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LinkSectionFaction] ERROR: Architecture {firstArch.Name} has null BelongedFaction; cannot infer section faction.");
                        throw new InvalidOperationException($"Section {section.ID} ({section.Name}) has invalid BelongedFactionID and cannot infer from Architecture.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LinkSectionFaction] 鉂?Architecture ID={firstArchID} 涓嶅瓨鍦紝鏃犳硶鎺ㄦ柇鍔垮姏");
                    throw new InvalidOperationException($"Section {section.ID} ({section.Name}) references missing Architecture ID={firstArchID}.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[LinkSectionFaction] ERROR: Section {section.ID} ({section.Name}) has no ArchitectureIDs; cannot infer faction.");
                throw new InvalidOperationException($"Section {section.ID} ({section.Name}) has invalid BelongedFactionID and no ArchitectureIDs.");
            }
        }
        
        // 馃敟 淇锛氶摼鎺?AIDetail 寮曠敤
        // 鏃ユ湡锛?026-02-13
        // 闂锛氳妗ｅ悗 Section.AIDetail 涓?null 鎴栨寚鍚戦敊璇殑瀵硅薄锛屽鑷寸帺瀹跺娍鍔涜 AI 鎺у埗
        // 瑙ｅ喅锛氭牴鎹?AIDetailID 浠?lookupTables 涓仮澶?AIDetail 寮曠敤
        // 娉ㄦ剰锛欰IDetailID 鍙互鏄?0锛圙ameObject ID 鍙互浠?0 寮€濮嬶級锛屽彧鏈?< 0 鎵嶆槸鏃犳晥鐨?
        if (section.AIDetailID >= 0)
        {
            if (lookupTables.SectionAIDetails.TryGetValue(section.AIDetailID, out SectionAIDetail aiDetail))
            {
                section.AIDetail = aiDetail;
                
                // 馃敟 璇婃柇锛氭鏌ョ帺瀹跺娍鍔涚殑鍐涘尯鏄惁琚敊璇湴璁剧疆涓?AutoRun
                bool isPlayerSection = section.BelongedFaction != null && 
                                      scenario != null && 
                                      scenario.IsPlayer(section.BelongedFaction);
                
                if (isPlayerSection && aiDetail.AutoRun)
                {
                    System.Diagnostics.Debug.WriteLine($"[LinkSectionAIDetail] WARNING: Player section is AutoRun.");
                    System.Diagnostics.Debug.WriteLine($"  - 鍐涘尯: {section.ID} ({section.Name})");
                    System.Diagnostics.Debug.WriteLine($"  - 鍔垮姏: {section.BelongedFaction.Name}");
                    System.Diagnostics.Debug.WriteLine($"  - AIDetail ID: {aiDetail.ID}");
                    System.Diagnostics.Debug.WriteLine($"  - AIDetail AutoRun: {aiDetail.AutoRun}");
                    System.Diagnostics.Debug.WriteLine($"  - AIDetail Description: {aiDetail.Description}");
                }
                
                System.Diagnostics.Debug.WriteLine($"[LinkSectionAIDetail] 鉁?鍐涘尯 {section.ID} ({section.Name}) 閾炬帴 AIDetail ID={aiDetail.ID}, AutoRun={aiDetail.AutoRun}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[LinkSectionAIDetail] ERROR: Section {section.ID} ({section.Name}) references missing AIDetail ID={section.AIDetailID}.");
                throw new InvalidOperationException($"Section {section.ID} ({section.Name}) references missing AIDetail ID={section.AIDetailID}.");
            }
        }
        else
        {
            // AIDetailID < 0 鏄暟鎹敊璇?
            System.Diagnostics.Debug.WriteLine($"[LinkSectionAIDetail] ERROR: Section {section.ID} ({section.Name}) has invalid AIDetailID={section.AIDetailID} (< 0).");
            System.Diagnostics.Debug.WriteLine($"[LinkSectionAIDetail]    杩欒鏄庡啗鍖哄湪鍒涘缓鎴栧姞杞芥椂娌℃湁姝ｇ‘璁剧疆 AIDetail");
            throw new InvalidOperationException($"Section {section.ID} ({section.Name}) has invalid AIDetailID={section.AIDetailID} (< 0).");
        }
        
        // 馃敟 淇锛氶摼鎺ラ兘鐫ｏ紙SectionLeader锛夊紩鐢?
        // 鏃ユ湡锛?026-02-17
        // 闂锛氶兘鐫ｆ病鏈夎閾炬帴锛屽鑷磋妗ｅ悗閮界潱涓㈠け
        // ANTI-BAND-AID锛氬尯鍒嗗悎娉曠┖鍊煎拰鏁版嵁鎹熷潖
        if (section.SectionLeaderID > 0)
        {
            // 鏁版嵁瀹屾暣鎬ф鏌ワ細閮界潱 ID > 0 浣?Person 涓嶅瓨鍦?= 鏁版嵁鎹熷潖
            if (!lookupTables.Persons.TryGetValue(section.SectionLeaderID, out Person leader))
            {
                System.Diagnostics.Debug.WriteLine($"[LinkSectionLeader] ERROR: Section {section.ID} ({section.Name}) references missing Leader ID={section.SectionLeaderID}.");
                System.Diagnostics.Debug.WriteLine($"[LinkSectionLeader]    鍙敤鐨?Person 鎬绘暟: {lookupTables.Persons.Count}");
                throw new InvalidOperationException(
                    $"Data corruption: Section {section.ID} ({section.Name}) references missing Leader ID={section.SectionLeaderID}.");
            }
            
            section.SectionLeader = leader;
            System.Diagnostics.Debug.WriteLine($"[LinkSectionLeader] 鉁?鍐涘尯 {section.ID} ({section.Name}) 閾炬帴閮界潱: {leader.Name} (ID={leader.ID})");
        }
        else if (section.SectionLeaderID == -1)
        {
            // 鍚堟硶鐨勭┖鍊硷細娌℃湁璁剧疆閮界潱
            section.SectionLeader = null;
        }
        else
        {
            // SectionLeaderID < -1 鏄暟鎹敊璇?
            System.Diagnostics.Debug.WriteLine($"[LinkSectionLeader] ERROR: Section {section.ID} ({section.Name}) has invalid SectionLeaderID={section.SectionLeaderID}.");
            throw new InvalidOperationException($"Section {section.ID} ({section.Name}) has invalid SectionLeaderID={section.SectionLeaderID}.");
        }
        
        // 馃敟 淇锛氶摼鎺ユ柟鍚戠洰鏍囧紩鐢?
        // 鏃ユ湡锛?026-02-17
        // 闂锛歄rientationFaction/Section/State/Architecture 娌℃湁琚摼鎺ワ紝瀵艰嚧璇绘。鍚庡啗鍖烘柟鍚戠洰鏍囦涪澶?
        // 娉ㄦ剰锛氭柟鍚戠洰鏍囨槸鍙€夌殑锛堢帺瀹跺彲鑳芥病鏈夎缃級锛屾墍浠ユ壘涓嶅埌鏃跺彧闇€娓呯┖锛屼笉鎶涘紓甯?
        // 浣嗘槸濡傛灉 ID > 0 鍗存壘涓嶅埌瀵硅薄锛岃鏄庣洰鏍囧凡琚垹闄わ紙渚嬪鍔垮姏鐏骸銆佸缓绛戣鍗犻锛夛紝杩欐槸鍚堟硶鐨勬父鎴忕姸鎬?
        
        if (section.OrientationFactionID > 0)
        {
            if (lookupTables.Factions.TryGetValue(section.OrientationFactionID, out Faction orientationFaction))
            {
                section.OrientationFaction = orientationFaction;
                System.Diagnostics.Debug.WriteLine($"[LinkSectionOrientation] 鉁?鍐涘尯 {section.ID} ({section.Name}) 閾炬帴鐩爣鍔垮姏: {orientationFaction.Name}");
            }
            else
            {
                // 鐩爣鍔垮姏宸茬伃浜★紝娓呯┖鐩爣锛堝悎娉曠殑娓告垙鐘舵€侊級
                System.Diagnostics.Debug.WriteLine($"[LinkSectionOrientation] 鈿狅笍 鍐涘尯 {section.ID} ({section.Name}) 鐨勭洰鏍囧娍鍔?ID={section.OrientationFactionID} 宸蹭笉瀛樺湪锛堝彲鑳藉凡鐏骸锛夛紝娓呯┖鐩爣");
                section.OrientationFactionID = -1;
                section.OrientationFaction = null;
            }
        }
        else if (section.OrientationFactionID == -1)
        {
            section.OrientationFaction = null;
        }
        
        if (section.OrientationSectionID > 0)
        {
            if (lookupTables.Sections.TryGetValue(section.OrientationSectionID, out Section orientationSection))
            {
                section.OrientationSection = orientationSection;
                System.Diagnostics.Debug.WriteLine($"[LinkSectionOrientation] 鉁?鍐涘尯 {section.ID} ({section.Name}) 閾炬帴鐩爣鍐涘尯: {orientationSection.Name}");
            }
            else
            {
                // 鐩爣鍐涘尯宸茶В鏁ｏ紝娓呯┖鐩爣锛堝悎娉曠殑娓告垙鐘舵€侊級
                System.Diagnostics.Debug.WriteLine($"[LinkSectionOrientation] 鈿狅笍 鍐涘尯 {section.ID} ({section.Name}) 鐨勭洰鏍囧啗鍖?ID={section.OrientationSectionID} 宸蹭笉瀛樺湪锛堝彲鑳藉凡瑙ｆ暎锛夛紝娓呯┖鐩爣");
                section.OrientationSectionID = -1;
                section.OrientationSection = null;
            }
        }
        else if (section.OrientationSectionID == -1)
        {
            section.OrientationSection = null;
        }
        
        if (section.OrientationStateID > 0)
        {
            if (lookupTables.States.TryGetValue(section.OrientationStateID, out State orientationState))
            {
                section.OrientationState = orientationState;
                System.Diagnostics.Debug.WriteLine($"[LinkSectionOrientation] 鉁?鍐涘尯 {section.ID} ({section.Name}) 閾炬帴鐩爣宸炲煙: {orientationState.Name}");
            }
            else
            {
                // 鐩爣宸炲煙涓嶅瓨鍦紝娓呯┖鐩爣锛堝悎娉曠殑娓告垙鐘舵€侊級
                System.Diagnostics.Debug.WriteLine($"[LinkSectionOrientation] 鈿狅笍 鍐涘尯 {section.ID} ({section.Name}) 鐨勭洰鏍囧窞鍩?ID={section.OrientationStateID} 涓嶅瓨鍦紝娓呯┖鐩爣");
                section.OrientationStateID = -1;
                section.OrientationState = null;
            }
        }
        else if (section.OrientationStateID == -1)
        {
            section.OrientationState = null;
        }
        
        if (section.OrientationArchitectureID > 0)
        {
            if (lookupTables.Architectures.TryGetValue(section.OrientationArchitectureID, out Architecture orientationArchitecture))
            {
                section.OrientationArchitecture = orientationArchitecture;
                System.Diagnostics.Debug.WriteLine($"[LinkSectionOrientation] 鉁?鍐涘尯 {section.ID} ({section.Name}) 閾炬帴鐩爣寤虹瓚: {orientationArchitecture.Name}");
            }
            else
            {
                // 鐩爣寤虹瓚宸茶鍗犻鎴栨懅姣侊紝娓呯┖鐩爣锛堝悎娉曠殑娓告垙鐘舵€侊級
                System.Diagnostics.Debug.WriteLine($"[LinkSectionOrientation] WARNING: Section {section.ID} ({section.Name}) orientation architecture ID={section.OrientationArchitectureID} missing; cleared.");
                section.OrientationArchitectureID = -1;
                section.OrientationArchitecture = null;
            }
        }
        else if (section.OrientationArchitectureID == -1)
        {
            section.OrientationArchitecture = null;
        }
        
        if (section.Architectures == null) section.Architectures = new ArchitectureList(); else section.Architectures.Clear();
        if (section.ArchitectureIDs != null)
            foreach (int id in section.ArchitectureIDs)
                if (lookupTables.Architectures.TryGetValue(id, out Architecture architecture))
                    section.Architectures.Add(architecture);
    }
}
}

