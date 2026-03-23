using System;
using System.Linq;
using System.Reflection;

namespace TestFacilityBonus
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("测试设施增益系统");
            Console.WriteLine("========================================");
            Console.WriteLine();
            
            Console.WriteLine("这个测试需要在游戏运行时进行。");
            Console.WriteLine("请按照以下步骤操作：");
            Console.WriteLine();
            Console.WriteLine("1. 启动游戏");
            Console.WriteLine("2. 选择一个城市");
            Console.WriteLine("3. 查看当前统治上限");
            Console.WriteLine("4. 建造一个增加统治上限的设施");
            Console.WriteLine("5. 等待设施建造完成");
            Console.WriteLine("6. 再次查看统治上限");
            Console.WriteLine();
            Console.WriteLine("预期结果：");
            Console.WriteLine("- 统治上限应该增加");
            Console.WriteLine("- 增加的数值应该等于设施的增益值");
            Console.WriteLine();
            Console.WriteLine("如果统治上限没有增加，请检查：");
            Console.WriteLine();
            Console.WriteLine("1. FacilityEnabled 是否为 true");
            Console.WriteLine("   - 在城市详情中查看");
            Console.WriteLine("   - 或者在代码中添加日志");
            Console.WriteLine();
            Console.WriteLine("2. BuildFacility 方法是否被调用");
            Console.WriteLine("   - 在 Architecture.cs 第 5892 行添加断点");
            Console.WriteLine("   - 或者添加日志：");
            Console.WriteLine("     System.Diagnostics.Debug.WriteLine($\"BuildFacility: {facilityKind.Name}\");");
            Console.WriteLine();
            Console.WriteLine("3. ApplyInfluence 是否被调用");
            Console.WriteLine("   - 在 Architecture.cs 第 5893 行添加断点");
            Console.WriteLine("   - 或者添加日志：");
            Console.WriteLine("     System.Diagnostics.Debug.WriteLine($\"ApplyInfluence: FacilityEnabled={this.FacilityEnabled}\");");
            Console.WriteLine();
            Console.WriteLine("4. appliedArch.Add 是否返回 true");
            Console.WriteLine("   - 在 InfluenceKind.cs 的 ApplyInfluenceKind 方法中添加日志");
            Console.WriteLine("   - 检查 HashSet 是否已经包含这个建筑");
            Console.WriteLine();
            Console.WriteLine("5. IncrementOfDominationCeiling 的值");
            Console.WriteLine("   - 在 BuildFacility 方法后添加日志：");
            Console.WriteLine("     System.Diagnostics.Debug.WriteLine($\"IncrementOfDominationCeiling: {this.IncrementOfDominationCeiling}\");");
            Console.WriteLine();
            
            Console.WriteLine("========================================");
            Console.WriteLine("调试建议");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("在 Architecture.cs 的 BuildFacility 方法中添加以下代码：");
            Console.WriteLine();
            Console.WriteLine("public void BuildFacility(FacilityKind facilityKind)");
            Console.WriteLine("{");
            Console.WriteLine("    System.Diagnostics.Debug.WriteLine($\"[BuildFacility] 开始建造设施: {facilityKind.Name}\");");
            Console.WriteLine("    System.Diagnostics.Debug.WriteLine($\"[BuildFacility] FacilityEnabled: {this.FacilityEnabled}\");");
            Console.WriteLine("    System.Diagnostics.Debug.WriteLine($\"[BuildFacility] IncrementOfDominationCeiling (before): {this.IncrementOfDominationCeiling}\");");
            Console.WriteLine();
            Console.WriteLine("    Facility facility = new Facility();");
            Console.WriteLine("    facility.ID = Session.Current.Scenario.Facilities.GetFreeGameObjectID();");
            Console.WriteLine("    facility.KindID = facilityKind.ID;");
            Console.WriteLine("    facility.Endurance = facilityKind.Endurance;");
            Console.WriteLine("    this.Facilities.AddFacility(facility);");
            Console.WriteLine("    Session.Current.Scenario.Facilities.AddFacility(facility);");
            Console.WriteLine();
            Console.WriteLine("    System.Diagnostics.Debug.WriteLine($\"[BuildFacility] 设施已添加到列表\");");
            Console.WriteLine();
            Console.WriteLine("    if (this.FacilityEnabled)");
            Console.WriteLine("    {");
            Console.WriteLine("        System.Diagnostics.Debug.WriteLine($\"[BuildFacility] 开始应用影响\");");
            Console.WriteLine("        facility.Influences.ApplyInfluence(this, Applier.Facility, facility.ID);");
            Console.WriteLine("        System.Diagnostics.Debug.WriteLine($\"[BuildFacility] 影响已应用\");");
            Console.WriteLine("    }");
            Console.WriteLine("    else");
            Console.WriteLine("    {");
            Console.WriteLine("        System.Diagnostics.Debug.WriteLine($\"[BuildFacility] FacilityEnabled=false，跳过应用影响\");");
            Console.WriteLine("    }");
            Console.WriteLine();
            Console.WriteLine("    System.Diagnostics.Debug.WriteLine($\"[BuildFacility] IncrementOfDominationCeiling (after): {this.IncrementOfDominationCeiling}\");");
            Console.WriteLine("    System.Diagnostics.Debug.WriteLine($\"[BuildFacility] DominationCeiling: {this.DominationCeiling}\");");
            Console.WriteLine("}");
            Console.WriteLine();
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
