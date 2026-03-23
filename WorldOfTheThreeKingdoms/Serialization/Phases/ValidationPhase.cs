using System;
using System.Collections.Generic;
using GameObjects;
using GameObjects.ArchitectureDetail;
using GameObjects.FactionDetail;
using GameObjects.PersonDetail;
using GameObjects.TroopDetail;
using GameObjects.SectionDetail;

namespace WorldOfTheThreeKingdoms.Serialization.Phases
{
    /// <summary>
    /// Phase 4: Validation Phase
    /// Validates data integrity after references have been linked
    /// Applies data sanitization rules to handle invalid references
    /// Generates a report of all issues found and fixes applied
    /// </summary>
    public class ValidationPhase
    {
        /// <summary>
        /// Validate the game scenario and apply data sanitization rules
        /// </summary>
        /// <param name="scenario">The game scenario to validate</param>
        /// <returns>A validation report containing errors, warnings, and fixes</returns>
        public ValidationReport Validate(GameScenario scenario)
        {
            if (scenario == null)
                throw new ArgumentNullException(nameof(scenario));
            
            var report = new ValidationReport();
            
            // Validate all game objects
            ValidatePersons(scenario, report);
            ValidateFactions(scenario, report);
            ValidateArchitectures(scenario, report);
            ValidateLegions(scenario, report);
            ValidateTroops(scenario, report);
            ValidateSections(scenario, report);
            ValidateMilitaries(scenario, report);  // 🔥 新增：验证 Military 数据完整性
            
            // Log summary
            if (report.HasErrors || report.HasWarnings || report.HasFixes)
            {
                System.Diagnostics.Debug.WriteLine(report.GetSummary());
            }
            
            return report;
        }
        
        /// <summary>
        /// Validate all Persons in the scenario
        /// </summary>
        private void ValidatePersons(GameScenario scenario, ValidationReport report)
        {
            if (scenario.Persons == null || scenario.Persons.Count == 0)
                return;
            
            foreach (Person person in scenario.Persons.GetList())
            {
                ValidatePerson(person, scenario, report);
            }
        }
        
        /// <summary>
        /// Validate a single Person
        /// </summary>
        private void ValidatePerson(Person person, GameScenario scenario, ValidationReport report)
        {
            if (person == null)
                return;
            
            // 注意：Person.BelongedFaction 是只读计算属性，通过以下方式计算：
            // - 如果在建筑中 → 返回建筑的势力
            // - 如果在部队中 → 返回部队的势力
            // - 如果是俘虏 → 返回俘虏所属势力
            // 
            // BelongedFactionID 字段不直接用于链接势力，所以即使它引用了不存在的势力ID，
            // 也不会影响游戏运行（只要 LocationArchitecture 或 LocationTroop 正确）
            // 
            // 因此我们不再将 BelongedFactionID 的验证作为错误，只作为警告
            
            if (person.BelongedFactionID > 0 && person.BelongedFaction == null)
            {
                // 检查是否有有效的位置引用
                bool hasValidLocation = (person.LocationArchitectureID > 0 && person.LocationArchitecture != null) ||
                                       (person.LocationTroopID > 0 && person.LocationTroop != null);
                
                if (hasValidLocation)
                {
                    // 有有效的位置引用，BelongedFaction 会通过位置计算，所以这不是问题
                    report.AddInfo($"Person {person.ID} ({person.Name}) has BelongedFactionID={person.BelongedFactionID} but BelongedFaction will be computed from location");
                }
                else
                {
                    // 没有有效的位置引用，这可能是问题
                    report.AddWarning($"Person {person.ID} ({person.Name}) has BelongedFactionID={person.BelongedFactionID} but no valid location (Architecture or Troop)");
                }
            }
            
            // Validate LocationArchitecture reference
            if (person.LocationArchitectureID > 0 && person.LocationArchitecture == null)
            {
                report.AddError($"Person {person.ID} ({person.Name}) has invalid location architecture reference (ID: {person.LocationArchitectureID})");
                
                // Apply sanitization: reset location
                person.LocationArchitectureID = -1;
                // person.LocationArchitecture = null; // Read-only property
                
                report.AddFix($"Reset Person {person.ID} ({person.Name}) location architecture");
            }
            
            // Validate LocationTroop reference
            if (person.LocationTroopID > 0 && person.LocationTroop == null)
            {
                report.AddError($"Person {person.ID} ({person.Name}) has invalid location troop reference (ID: {person.LocationTroopID})");
                
                // Apply sanitization: reset location
                person.LocationTroopID = -1;
                // person.LocationTroop = null; // Read-only property
                
                report.AddFix($"Reset Person {person.ID} ({person.Name}) location troop");
            }
            
            // Validate Treasures collection completeness
            // Check if TreasureIDs and Treasures collection are in sync
            if (person.TreasureIDs != null && person.Treasures != null)
            {
                if (person.TreasureIDs.Count != person.Treasures.Count)
                {
                    report.AddWarning($"Person {person.ID} ({person.Name}) has mismatched treasure counts (IDs: {person.TreasureIDs.Count}, Objects: {person.Treasures.Count})");
                    
                    // The LinkReferencesPhase should have already cleaned invalid IDs
                    // This is just a consistency check
                }
            }
        }
        
        /// <summary>
        /// Validate all Factions in the scenario
        /// </summary>
        private void ValidateFactions(GameScenario scenario, ValidationReport report)
        {
            if (scenario.Factions == null || scenario.Factions.Count == 0)
                return;
            
            foreach (Faction faction in scenario.Factions.GetList())
            {
                ValidateFaction(faction, scenario, report);
            }
        }
        
        /// <summary>
        /// Validate a single Faction
        /// </summary>
        private void ValidateFaction(Faction faction, GameScenario scenario, ValidationReport report)
        {
            if (faction == null)
                return;
            
            // Validate Leader reference
            if (faction.LeaderID > 0 && faction.Leader == null)
            {
                report.AddError($"Faction {faction.ID} ({faction.Name}) has invalid leader reference (ID: {faction.LeaderID})");
                
                // Apply sanitization: reset leader
                faction.LeaderID = -1;
                // faction.Leader = null; // Read-only property
                
                report.AddFix($"Reset Faction {faction.ID} ({faction.Name}) leader");
            }
            
            // Validate Capital reference
            if (faction.CapitalID > 0 && faction.Capital == null)
            {
                report.AddWarning($"Faction {faction.ID} ({faction.Name}) has invalid capital reference (ID: {faction.CapitalID})");
                
                // Apply sanitization: reset capital
                faction.CapitalID = -1;
                // faction.Capital = null; // Read-only property
                
                report.AddFix($"Reset Faction {faction.ID} ({faction.Name}) capital");
            }
            
            // Validate Architectures collection completeness
            if (faction.ArchitectureIDs != null && faction.Architectures != null)
            {
                if (faction.ArchitectureIDs.Count != faction.Architectures.Count)
                {
                    report.AddWarning($"Faction {faction.ID} ({faction.Name}) has mismatched architecture counts (IDs: {faction.ArchitectureIDs.Count}, Objects: {faction.Architectures.Count})");
                }
            }
            
            // Validate Persons collection completeness
            // 🔥 修复：Faction.Persons 是计算属性，不应该与 PersonIDs 比较数量
            // 日期：2026-02-10
            // 问题：Persons 只包含在建筑和部队中的武将，而 PersonIDs 可能包含各种状态的武将
            // 正确的验证：检查 PersonIDs 中的武将是否存在，以及他们的 BelongedFaction 是否正确
            if (faction.PersonIDs != null && faction.PersonIDs.Count > 0)
            {
                int validCount = 0;
                int invalidCount = 0;
                
                foreach (int personID in faction.PersonIDs)
                {
                    Person person = scenario.Persons.GetGameObject(personID) as Person;
                    if (person != null)
                    {
                        validCount++;
                        
                        // 检查武将的 BelongedFaction 是否指向这个势力
                        // 注意：BelongedFaction 是计算属性，可能为 null（如果武将不在建筑或部队中）
                        if (person.BelongedFaction != null && person.BelongedFaction != faction)
                        {
                            report.AddWarning($"Faction {faction.ID} ({faction.Name}) has Person {personID} ({person.Name}) whose BelongedFaction points to different faction {person.BelongedFaction.ID} ({person.BelongedFaction.Name})");
                        }
                    }
                    else
                    {
                        report.AddWarning($"Faction {faction.ID} ({faction.Name}) references non-existent Person {personID}");
                        invalidCount++;
                    }
                }
                
                // 只有在有无效引用时才报告
                if (invalidCount > 0)
                {
                    report.AddWarning($"Faction {faction.ID} ({faction.Name}) has {invalidCount} invalid person references out of {faction.PersonIDs.Count} total");
                }
            }
            
            // Validate Troops collection completeness
            if (faction.TroopIDs != null && faction.Troops != null)
            {
                if (faction.TroopIDs.Count != faction.Troops.Count)
                {
                    report.AddWarning($"Faction {faction.ID} ({faction.Name}) has mismatched troop counts (IDs: {faction.TroopIDs.Count}, Objects: {faction.Troops.Count})");
                }
            }
        }
        
        /// <summary>
        /// Validate all Architectures in the scenario
        /// </summary>
        private void ValidateArchitectures(GameScenario scenario, ValidationReport report)
        {
            if (scenario.Architectures == null || scenario.Architectures.Count == 0)
                return;
            
            foreach (Architecture architecture in scenario.Architectures.GetList())
            {
                ValidateArchitecture(architecture, scenario, report);
            }
        }
        
        /// <summary>
        /// Validate a single Architecture
        /// </summary>
        private void ValidateArchitecture(Architecture architecture, GameScenario scenario, ValidationReport report)
        {
            if (architecture == null)
                return;
            
            // Validate BelongedFaction reference
            if (architecture.BelongedFactionID > 0 && architecture.BelongedFaction == null)
            {
                report.AddWarning($"Architecture {architecture.ID} ({architecture.Name}) has invalid faction reference (ID: {architecture.BelongedFactionID})");
                
                // Apply sanitization: reset faction
                architecture.BelongedFactionID = -1;
                // architecture.BelongedFaction = null; // Read-only property
                
                report.AddFix($"Reset Architecture {architecture.ID} ({architecture.Name}) faction");
            }
            
            // Validate BelongedSection reference
            if (architecture.BelongedSectionID > 0 && architecture.BelongedSection == null)
            {
                report.AddWarning($"Architecture {architecture.ID} ({architecture.Name}) has invalid section reference (ID: {architecture.BelongedSectionID})");
                
                // Apply sanitization: reset section
                architecture.BelongedSectionID = -1;
                // architecture.BelongedSection = null; // Read-only property
                
                report.AddFix($"Reset Architecture {architecture.ID} ({architecture.Name}) section");
            }
            
            // Validate Mayor reference
            if (architecture.MayorID > 0 && architecture.Mayor == null)
            {
                report.AddWarning($"Architecture {architecture.ID} ({architecture.Name}) has invalid mayor reference (ID: {architecture.MayorID})");
                
                // Apply sanitization: reset mayor
                architecture.MayorID = -1;
                // architecture.Mayor = null; // Read-only property
                
                report.AddFix($"Reset Architecture {architecture.ID} ({architecture.Name}) mayor");
            }
        }
        
        /// <summary>
        /// Validate all Legions in the scenario
        /// </summary>
        private void ValidateLegions(GameScenario scenario, ValidationReport report)
        {
            if (scenario.Legions == null || scenario.Legions.Count == 0)
                return;
            
            foreach (Legion legion in scenario.Legions.GetList())
            {
                ValidateLegion(legion, scenario, report);
            }
        }
        
        /// <summary>
        /// Validate a single Legion
        /// </summary>
        private void ValidateLegion(Legion legion, GameScenario scenario, ValidationReport report)
        {
            if (legion == null)
                return;
            
            // Validate BelongedFaction reference
            if (legion.BelongedFactionID >= 0 && legion.BelongedFaction == null)
            {
                report.AddWarning($"Legion {legion.ID} has invalid faction reference (ID: {legion.BelongedFactionID})");
                
                // Apply sanitization: reset faction
                legion.BelongedFactionID = -1;
                // legion.BelongedFaction = null; // Read-only property
                
                report.AddFix($"Reset Legion {legion.ID} faction");
            }
            
            // Validate Leader reference
            if (legion.LeaderID >= 0 && legion.Leader == null)
            {
                report.AddWarning($"Legion {legion.ID} has invalid leader reference (ID: {legion.LeaderID})");
                
                // Apply sanitization: reset leader
                legion.LeaderID = -1;
                // legion.Leader = null; // Read-only property
                
                report.AddFix($"Reset Legion {legion.ID} leader");
            }
            
            // Validate Troops collection completeness
            if (legion.TroopIDs != null && legion.Troops != null)
            {
                if (legion.TroopIDs.Count != legion.Troops.Count)
                {
                    report.AddWarning($"Legion {legion.ID} has mismatched troop counts (IDs: {legion.TroopIDs.Count}, Objects: {legion.Troops.Count})");
                }
            }
        }
        
        /// <summary>
        /// Validate all Troops in the scenario
        /// </summary>
        private void ValidateTroops(GameScenario scenario, ValidationReport report)
        {
            if (scenario.Troops == null || scenario.Troops.Count == 0)
                return;
            
            foreach (Troop troop in scenario.Troops.GetList())
            {
                ValidateTroop(troop, scenario, report);
            }
        }
        
        /// <summary>
        /// Validate a single Troop
        /// </summary>
        private void ValidateTroop(Troop troop, GameScenario scenario, ValidationReport report)
        {
            if (troop == null)
                return;
            
            // Validate BelongedFaction reference
            if (troop.BelongedFactionID >= 0 && troop.BelongedFaction == null)
            {
                report.AddWarning($"Troop {troop.ID} has invalid faction reference (ID: {troop.BelongedFactionID})");
                
                // Apply sanitization: reset faction
                troop.BelongedFactionID = -1;
                // troop.BelongedFaction = null; // Read-only property
                
                report.AddFix($"Reset Troop {troop.ID} faction");
            }
            
            // Validate Leader reference
            if (troop.LeaderID >= 0 && troop.Leader == null)
            {
                report.AddWarning($"Troop {troop.ID} has invalid leader reference (ID: {troop.LeaderID})");
                
                // Apply sanitization: reset leader
                troop.LeaderID = -1;
                // troop.Leader = null; // Read-only property
                
                report.AddFix($"Reset Troop {troop.ID} leader");
            }
            
            // Validate BelongedLegion reference
            if (troop.BelongedLegionID >= 0 && troop.BelongedLegion == null)
            {
                report.AddWarning($"Troop {troop.ID} has invalid legion reference (ID: {troop.BelongedLegionID})");
                
                // Apply sanitization: reset legion
                troop.BelongedLegionID = -1;
                // troop.BelongedLegion = null; // Read-only property
                
                report.AddFix($"Reset Troop {troop.ID} legion");
            }
            
            // Validate StartingArchitecture reference
            // 🔥 关键修复：ID=0 是有效的（洛阳的 ID 就是 0），必须使用 >= 0
            if (troop.StartingArchitectureID >= 0 && troop.StartingArchitecture == null)
            {
                report.AddWarning($"Troop {troop.ID} has invalid starting architecture reference (ID: {troop.StartingArchitectureID})");
                
                // Apply sanitization: reset starting architecture
                troop.StartingArchitectureID = -1;
                // troop.StartingArchitecture = null; // Read-only property
                
                report.AddFix($"Reset Troop {troop.ID} starting architecture");
            }
        }
        
        /// <summary>
        /// Validate all Sections in the scenario
        /// </summary>
        private void ValidateSections(GameScenario scenario, ValidationReport report)
        {
            if (scenario.Sections == null || scenario.Sections.Count == 0)
                return;
            
            foreach (Section section in scenario.Sections.GetList())
            {
                ValidateSection(section, scenario, report);
            }
        }
        
        /// <summary>
        /// Validate a single Section
        /// </summary>
        private void ValidateSection(Section section, GameScenario scenario, ValidationReport report)
        {
            if (section == null)
                return;
            
            // Validate BelongedFaction reference
            if (section.BelongedFactionID > 0 && section.BelongedFaction == null)
            {
                report.AddWarning($"Section {section.ID} has invalid faction reference (ID: {section.BelongedFactionID})");
                
                // Apply sanitization: reset faction
                section.BelongedFactionID = -1;
                // section.BelongedFaction = null; // Read-only property
                
                report.AddFix($"Reset Section {section.ID} faction");
            }
            
            // Validate Architectures collection completeness
            if (section.ArchitectureIDs != null && section.Architectures != null)
            {
                if (section.ArchitectureIDs.Count != section.Architectures.Count)
                {
                    report.AddWarning($"Section {section.ID} has mismatched architecture counts (IDs: {section.ArchitectureIDs.Count}, Objects: {section.Architectures.Count})");
                }
            }
        }
        
        /// <summary>
        /// Validate all Militaries in the scenario
        /// </summary>
        private void ValidateMilitaries(GameScenario scenario, ValidationReport report)
        {
            if (scenario.Militaries == null || scenario.Militaries.Count == 0)
                return;
            
            foreach (Military military in scenario.Militaries.GetList())
            {
                ValidateMilitary(military, scenario, report);
            }
        }
        
        /// <summary>
        /// Validate a single Military
        /// </summary>
        private void ValidateMilitary(Military military, GameScenario scenario, ValidationReport report)
        {
            if (military == null)
                return;
            
            // 🔥 关键修复：检测 ShelledMilitary 循环引用
            // 日期：2026-02-11
            // 问题：存档数据损坏导致 A.ShelledMilitary → B, B.ShelledMilitary → A
            // 结果：访问 Quantity 属性时栈溢出
            // 解决：检测循环并断开引用
            if (military.ShelledMilitary != null)
            {
                // 使用 HashSet 检测循环（最多检测 100 层，防止无限循环）
                HashSet<int> visited = [];  // C# 12 集合表达式
                var current = military;
                int depth = 0;
                const int maxDepth = 100;
                
                while (current != null && depth < maxDepth)
                {
                    if (!visited.Add(current.ID))
                    {
                        // 检测到循环！
                        report.AddError($"Military {military.ID} ({military.Name}) has circular ShelledMilitary reference (cycle detected at depth {depth})");
                        
                        // 修复：断开循环引用
                        military.ShelledMilitary = null;
                        report.AddFix($"Broke circular ShelledMilitary reference for Military {military.ID}");
                        break;
                    }
                    
                    current = current.ShelledMilitary;
                    depth++;
                }
                
                if (depth >= maxDepth)
                {
                    report.AddError($"Military {military.ID} ({military.Name}) has ShelledMilitary chain exceeding {maxDepth} levels");
                    military.ShelledMilitary = null;
                    report.AddFix($"Broke excessive ShelledMilitary chain for Military {military.ID}");
                }
            }
        }
    }
}
