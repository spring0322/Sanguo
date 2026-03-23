using System;

Console.WriteLine("=== 设施增益累加问题诊断 ===\n");
            
            Console.WriteLine("📋 诊断目标：");
            Console.WriteLine("  检查统治上限是否在每次调用 ApplyFacilityInfluences 时累加\n");
            
            Console.WriteLine("🔍 关键检查点：");
            Console.WriteLine("  1. ApplyFacilityInfluences() 是否正确清除旧记录");
            Console.WriteLine("  2. appliedArch.Add() 是否返回 false（表示重复应用）");
            Console.WriteLine("  3. IncrementOfDominationCeiling 是否在多次调用后保持不变\n");
            
            Console.WriteLine("🐛 可能的根本原因：");
            Console.WriteLine();
            
            Console.WriteLine("【原因1】RemoveWhere 条件不正确");
            Console.WriteLine("  当前代码：");
            Console.WriteLine("    influence.appliedArch.RemoveWhere(a => a.arch == this && a.applier == Applier.Facility);");
            Console.WriteLine();
            Console.WriteLine("  问题：只检查 applier 类型，没有检查 applierID");
            Console.WriteLine("  结果：如果建筑有多个相同类型的设施，只会移除第一个");
            Console.WriteLine();
            Console.WriteLine("  修复方案：");
            Console.WriteLine("    influence.appliedArch.RemoveWhere(a => ");
            Console.WriteLine("        a.arch == this && ");
            Console.WriteLine("        a.applier == Applier.Facility && ");
            Console.WriteLine("        a.applierID == facility.ID);");
            Console.WriteLine();
            
            Console.WriteLine("【原因2】设施维护逻辑导致重复应用");
            Console.WriteLine("  场景：FacilityMaintenance() 每天调用");
            Console.WriteLine("  代码路径：");
            Console.WriteLine("    FacilityMaintenance()");
            Console.WriteLine("      → this.FacilityEnabled = true;");
            Console.WriteLine("      → this.ApplyFacilityInfluences(true);");
            Console.WriteLine();
            Console.WriteLine("  问题：如果 RemoveWhere 没有正确清除，每次都会累加");
            Console.WriteLine();
            
            Console.WriteLine("【原因3】读档后 appliedArch 引用失效");
            Console.WriteLine("  场景：存档 → 读档 → ApplyInfluences()");
            Console.WriteLine("  问题：appliedArch 中的 Architecture 引用可能指向旧对象");
            Console.WriteLine("  结果：RemoveWhere 无法匹配，导致重复应用");
            Console.WriteLine();
            
            Console.WriteLine("🔧 推荐的诊断步骤：");
            Console.WriteLine();
            Console.WriteLine("1. 在 InfluenceKind1004.ApplyInfluenceKind 中添加日志：");
            Console.WriteLine("   System.Diagnostics.Debug.WriteLine($\"[统治上限] 应用增益 +{increment}\");");
            Console.WriteLine("   System.Diagnostics.Debug.WriteLine($\"  建筑: {architecture.Name}\");");
            Console.WriteLine("   System.Diagnostics.Debug.WriteLine($\"  当前值: {architecture.IncrementOfDominationCeiling}\");");
            Console.WriteLine();
            
            Console.WriteLine("2. 在 InfluenceKind.ApplyInfluenceKind 中添加日志：");
            Console.WriteLine("   bool added = i.appliedArch.Add(new ApplyingArchitecture(...));");
            Console.WriteLine("   System.Diagnostics.Debug.WriteLine($\"[应用检查] added={added}, 建筑={architecture.Name}\");");
            Console.WriteLine();
            
            Console.WriteLine("3. 在 ApplyFacilityInfluences 中添加日志：");
            Console.WriteLine("   int removedCount = influence.appliedArch.RemoveWhere(...);");
            Console.WriteLine("   System.Diagnostics.Debug.WriteLine($\"[清除记录] 移除了 {removedCount} 条记录\");");
            Console.WriteLine();
            
            Console.WriteLine("4. 游戏中测试：");
            Console.WriteLine("   a) 建造一个有统治上限增益的设施");
            Console.WriteLine("   b) 记录 IncrementOfDominationCeiling 的值");
            Console.WriteLine("   c) 等待几天（触发 FacilityMaintenance）");
            Console.WriteLine("   d) 检查值是否增加");
            Console.WriteLine("   e) 存档 → 读档 → 检查值是否再次增加");
            Console.WriteLine();
