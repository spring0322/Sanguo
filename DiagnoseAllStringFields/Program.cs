using System;
using System.Linq;
using System.Reflection;

namespace DiagnoseAllStringFields
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  诊断所有需要从字符串恢复的字段                            ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            // Architecture 类的字符串字段
            Console.WriteLine("【Architecture 类】");
            Console.WriteLine("需要恢复的字段:");
            Console.WriteLine("  1. FacilitiesString → Facilities (设施)");
            Console.WriteLine("  2. MilitariesString → Militaries (编队)");
            Console.WriteLine("  3. PersonsString → Persons (武将)");
            Console.WriteLine("  4. MovingPersonsString → MovingPersons (移动中的武将)");
            Console.WriteLine("  5. NoFactionPersonsString → NoFactionPersons (无势力武将)");
            Console.WriteLine("  6. NoFactionMovingPersonsString → NoFactionMovingPersons");
            Console.WriteLine("  7. feiziliebiaoString → feiziliebiao (妃子列表)");
            Console.WriteLine("  8. CaptivesString → Captives (俘虏)");
            Console.WriteLine("  9. InformationsString → Informations (情报)");
            Console.WriteLine("  10. FundPacksString → FundPacks (资金包)");
            Console.WriteLine("  11. FoodPacksString → FoodPacks (粮食包)");
            Console.WriteLine("  12. PopulationPacksString → PopulationPacks (人口包)");
            Console.WriteLine("  13. MilitaryPopulationPacksString → MilitaryPopulationPacks");
            Console.WriteLine();

            // Person 类的字符串字段
            Console.WriteLine("【Person 类】");
            Console.WriteLine("需要恢复的字段:");
            Console.WriteLine("  1. SkillsString → Skills (技能)");
            Console.WriteLine("  2. StuntsString → Stunts (特技)");
            Console.WriteLine("  3. RealTitlesString → RealTitles (称号)");
            Console.WriteLine("  4. UniqueMilitaryKindsString → UniqueMilitaryKinds (专属兵种)");
            Console.WriteLine("  5. UniqueTitlesString → UniqueTitles (专属称号)");
            Console.WriteLine();

            // Faction 类的字符串字段
            Console.WriteLine("【Faction 类】");
            Console.WriteLine("需要恢复的字段:");
            Console.WriteLine("  1. ArchitecturesString → Architectures (建筑列表)");
            Console.WriteLine("  2. PersonsString → Persons (武将列表)");
            Console.WriteLine("  3. MilitariesString → Militaries (编队列表)");
            Console.WriteLine("  4. TroopsString → Troops (部队列表)");
            Console.WriteLine("  5. CaptivesString → Captives (俘虏列表)");
            Console.WriteLine("  6. InformationsString → Informations (情报列表)");
            Console.WriteLine();

            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  修复建议                                                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("在 AfterLoadSaveFile 中需要调用以下恢复方法:");
            Console.WriteLine();
            Console.WriteLine("// 1. 恢复建筑相关数据");
            Console.WriteLine("foreach (Architecture arch in this.Architectures)");
            Console.WriteLine("{");
            Console.WriteLine("    arch.LoadFacilitiesFromString(this.Facilities, arch.FacilitiesString);");
            Console.WriteLine("    arch.LoadMilitariesFromString(this.Militaries, arch.MilitariesString);");
            Console.WriteLine("    arch.LoadPersonsFromString(persons, arch.PersonsString, PersonStatus.Normal);");
            Console.WriteLine("    arch.LoadPersonsFromString(persons, arch.MovingPersonsString, PersonStatus.Moving);");
            Console.WriteLine("    arch.LoadCaptivesFromString(this.Captives, arch.CaptivesString);");
            Console.WriteLine("    arch.LoadInformationsFromString(this.Informations, arch.InformationsString);");
            Console.WriteLine("    arch.LoadFundPacksFromString(arch.FundPacksString);");
            Console.WriteLine("    arch.LoadFoodPacksFromString(arch.FoodPacksString);");
            Console.WriteLine("    // ... 其他字段");
            Console.WriteLine("}");
            Console.WriteLine();
            Console.WriteLine("// 2. 恢复武将相关数据");
            Console.WriteLine("foreach (Person person in this.Persons)");
            Console.WriteLine("{");
            Console.WriteLine("    person.LoadSkillsFromString(person.SkillsString);");
            Console.WriteLine("    person.LoadStuntsFromString(person.StuntsString);");
            Console.WriteLine("    person.LoadTitleFromString(person.RealTitlesString, allTitles);");
            Console.WriteLine("    // ... 其他字段");
            Console.WriteLine("}");
            Console.WriteLine();
            Console.WriteLine("// 3. 恢复势力相关数据");
            Console.WriteLine("foreach (Faction faction in this.Factions)");
            Console.WriteLine("{");
            Console.WriteLine("    faction.LoadArchitecturesFromString(this.Architectures, faction.ArchitecturesString);");
            Console.WriteLine("    faction.LoadPersonsFromString(this.Persons, faction.PersonsString);");
            Console.WriteLine("    // ... 其他字段");
            Console.WriteLine("}");
            Console.WriteLine();

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
