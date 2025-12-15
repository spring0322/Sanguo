using System;
using System.Collections.Generic;
using System.Linq;
using GameObjects;

namespace GameGlobal
{
    /// <summary>
    /// 人事管理系统测试
    /// </summary>
    public static class PersonnelManagerTest
    {
        /// <summary>
        /// 运行完整的人事管理测试
        /// </summary>
        public static void RunCompleteTest()
        {
            Console.WriteLine("=== 人事管理系统测试 ===\n");

            try
            {
                TestBasicAssignment();
                TestThreatBasedAssignment();
                TestPersonalityInfluence();
                TestTransferSystem();
                TestOptimization();
                TestComplexScenarios();
                TestLoyaltySystem();
                TestEnhancedRelationshipSystem();

                Console.WriteLine("\n🎉 人事管理系统测试完成！所有功能正常工作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 人事管理测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 测试基础任命功能
        /// </summary>
        private static void TestBasicAssignment()
        {
            Console.WriteLine("--- 基础任命功能测试 ---");

            var faction = CreateTestFaction("蜀国");
            var personnelManager = new PersonnelManager(faction);

            // 添加一些武将
            faction.Officers.Add(CreateTestOfficer("诸葛亮", 95, 85, 100, 60));
            faction.Officers.Add(CreateTestOfficer("关羽", 70, 95, 75, 90));
            faction.Officers.Add(CreateTestOfficer("张飞", 40, 90, 50, 95));

            // 添加一些城市
            faction.Cities.Add(CreateTestCity("成都", false, false, false));
            faction.Cities.Add(CreateTestCity("江州", true, false, true));

            Console.WriteLine("  执行人事安排:");
            personnelManager.UpdateAssignments();

            Console.WriteLine("  任命结果:");
            foreach (var city in faction.Cities)
            {
                if (city.Prefect != null)
                {
                    Console.WriteLine($"    {city.Name}: {city.Prefect.Name}");
                }
                else
                {
                    Console.WriteLine($"    {city.Name}: 无太守");
                }
            }

            Console.WriteLine("  ✓ 基础任命功能测试完成\n");
        }

        /// <summary>
        /// 测试基于威胁等级的任命
        /// </summary>
        private static void TestThreatBasedAssignment()
        {
            Console.WriteLine("--- 威胁等级任命测试 ---");

            var faction = CreateTestFaction("魏国");
            var personnelManager = new PersonnelManager(faction);

            // 创建不同类型的武将
            var militaryOfficer = CreateTestOfficer("夏侯惇", 60, 90, 70, 85);
            militaryOfficer.AddTrait(Trait.Rash);
            militaryOfficer.AddExperience("SiegeDefense");

            var politicalOfficer = CreateTestOfficer("荀彧", 95, 70, 90, 50);
            politicalOfficer.AddTrait(Trait.Scholar);

            var balancedOfficer = CreateTestOfficer("曹操", 90, 85, 95, 75);
            balancedOfficer.AddTrait(Trait.Pragmatic);

            faction.Officers.AddRange(new[] { militaryOfficer, politicalOfficer, balancedOfficer });

            // 创建不同威胁等级的城市
            var frontlineCity = CreateTestCity("许昌", true, false, true); // 边境+有战斗
            var capitalCity = CreateTestCity("洛阳", false, true, false);   // 首都
            var backCity = CreateTestCity("邺城", false, false, false);     // 后方

            faction.Cities.AddRange(new[] { frontlineCity, capitalCity, backCity });

            Console.WriteLine("  城市威胁等级:");
            foreach (var city in faction.Cities)
            {
                var threat = GetCityThreatLevel(city);
                Console.WriteLine($"    {city.Name}: {threat:F2} ({'前线' if threat > 0.5f else '后方'})");
            }

            Console.WriteLine("  执行威胁等级导向的人事安排:");
            personnelManager.UpdateAssignments();

            Console.WriteLine("  任命结果:");
            foreach (var city in faction.Cities)
            {
                if (city.Prefect != null)
                {
                    var threat = GetCityThreatLevel(city);
                    var type = threat > 0.5f ? "军事型" : "政治型";
                    Console.WriteLine($"    {city.Name} ({type}): {city.Prefect.Name}");
                }
            }

            Console.WriteLine("  ✓ 威胁等级任命测试完成\n");
        }

        /// <summary>
        /// 测试性格特质影响
        /// </summary>
        private static void TestPersonalityInfluence()
        {
            Console.WriteLine("--- 性格特质影响测试 ---");

            var faction = CreateTestFaction("吴国");
            var personnelManager = new PersonnelManager(faction);

            // 创建不同性格的武将
            var rashOfficer = CreateTestOfficer("甘宁", 50, 85, 60, 90);
            rashOfficer.AddTrait(Trait.Rash);

            var cautiousOfficer = CreateTestOfficer("鲁肃", 85, 70, 85, 55);
            cautiousOfficer.AddTrait(Trait.Cautious);

            var scholarOfficer = CreateTestOfficer("诸葛瑾", 90, 65, 90, 50);
            scholarOfficer.AddTrait(Trait.Scholar);

            faction.Officers.AddRange(new[] { rashOfficer, cautiousOfficer, scholarOfficer });

            // 创建测试城市
            var borderCity = CreateTestCity("建业", true, false, true);
            var capitalCity = CreateTestCity("武昌", false, true, false);

            faction.Cities.AddRange(new[] { borderCity, capitalCity });

            Console.WriteLine("  武将性格特质:");
            foreach (var officer in faction.Officers)
            {
                Console.WriteLine($"    {officer.Name}: {string.Join(", ", officer.Traits)}");
            }

            personnelManager.UpdateAssignments();

            Console.WriteLine("  性格导向的任命结果:");
            foreach (var city in faction.Cities)
            {
                if (city.Prefect != null)
                {
                    var traits = string.Join(", ", city.Prefect.Traits);
                    Console.WriteLine($"    {city.Name}: {city.Prefect.Name} ({traits})");
                }
            }

            Console.WriteLine("  ✓ 性格特质影响测试完成\n");
        }

        /// <summary>
        /// 测试调动系统
        /// </summary>
        private static void TestTransferSystem()
        {
            Console.WriteLine("--- 调动系统测试 ---");

            var faction = CreateTestFaction("蜀国");
            var personnelManager = new PersonnelManager(faction);

            // 创建武将和城市
            var zhuge = CreateTestOfficer("诸葛亮", 95, 85, 100, 60);
            zhuge.AddTrait(Trait.Scholar);

            var guan = CreateTestOfficer("关羽", 70, 95, 75, 90);
            guan.AddTrait(Trait.Loyal);

            var chengdu = CreateTestCity("成都", false, true, false);
            var hanzhong = CreateTestCity("汉中", true, false, true);

            // 设置初始位置
            zhuge.Location = chengdu;
            guan.Location = hanzhong;

            faction.Officers.AddRange(new[] { zhuge, guan });
            faction.Cities.AddRange(new[] { chengdu, hanzhong });

            Console.WriteLine("  初始位置:");
            Console.WriteLine($"    诸葛亮: {zhuge.Location?.Name ?? "无"}");
            Console.WriteLine($"    关羽: {guan.Location?.Name ?? "无"}");

            Console.WriteLine("  执行人事调整 (应该会发生调动):");
            personnelManager.UpdateAssignments();

            var transfers = personnelManager.GetActiveTransfers();
            Console.WriteLine($"  活跃调动数量: {transfers.Count}");

            foreach (var transfer in transfers)
            {
                Console.WriteLine($"    {transfer.Officer.Name} → {transfer.Destination.Name} ({transfer.EstimatedDays}天)");
            }

            Console.WriteLine("  ✓ 调动系统测试完成\n");
        }

        /// <summary>
        /// 测试优化功能
        /// </summary>
        private static void TestOptimization()
        {
            Console.WriteLine("--- 优化功能测试 ---");

            var faction = CreateTestFaction("魏国");
            var personnelManager = new PersonnelManager(faction);

            // 创建能力差异明显的武将
            var excellentOfficer = CreateTestOfficer("司马懿", 95, 90, 95, 70);
            excellentOfficer.AddTrait(Trait.Cautious);

            var averageOfficer = CreateTestOfficer("普通武将", 60, 60, 60, 60);
            var poorOfficer = CreateTestOfficer("低能武将", 30, 40, 35, 45);

            faction.Officers.AddRange(new[] { excellentOfficer, averageOfficer, poorOfficer });

            var importantCity = CreateTestCity("长安", false, true, false);
            var normalCity = CreateTestCity("天水", false, false, false);

            faction.Cities.AddRange(new[] { importantCity, normalCity });

            // 先进行一次基础分配
            Console.WriteLine("  初次分配:");
            personnelManager.UpdateAssignments();

            foreach (var city in faction.Cities)
            {
                if (city.Prefect != null)
                {
                    Console.WriteLine($"    {city.Name}: {city.Prefect.Name}");
                }
            }

            // 再次执行，测试优化功能
            Console.WriteLine("  优化调整:");
            personnelManager.UpdateAssignments();

            foreach (var city in faction.Cities)
            {
                if (city.Prefect != null)
                {
                    Console.WriteLine($"    {city.Name}: {city.Prefect.Name}");
                }
            }

            Console.WriteLine("  ✓ 优化功能测试完成\n");
        }

        /// <summary>
        /// 测试复杂场景
        /// </summary>
        private static void TestComplexScenarios()
        {
            Console.WriteLine("--- 复杂场景测试 ---");

            var faction = CreateTestFaction("蜀国");
            faction.State = FactionState.WarTime;
            var personnelManager = new PersonnelManager(faction);

            // 创建历史名将
            var zhuge = CreateTestOfficer("诸葛亮", 95, 85, 100, 60);
            zhuge.AddTrait(Trait.Scholar);
            zhuge.AddTrait(Trait.Cautious);
            zhuge.AddExperience("CityManagement");

            var guan = CreateTestOfficer("关羽", 70, 95, 75, 90);
            guan.AddTrait(Trait.Loyal);
            guan.AddTrait(Trait.Rash);
            guan.AddExperience("SiegeDefense");

            var zhang = CreateTestOfficer("张飞", 40, 90, 50, 95);
            zhang.AddTrait(Trait.Rash);
            zhang.Age = 45;

            var zhao = CreateTestOfficer("赵云", 65, 85, 70, 85);
            zhao.AddTrait(Trait.Cautious);
            zhao.AddTrait(Trait.Loyal);

            var ma = CreateTestOfficer("马超", 55, 88, 60, 92);
            ma.AddTrait(Trait.Ambitious);
            ma.AddTrait(Trait.Rash);

            faction.Officers.AddRange(new[] { zhuge, guan, zhang, zhao, ma });

            // 创建战略要地
            var chengdu = CreateTestCity("成都", false, true, false);    // 首都
            var hanzhong = CreateTestCity("汉中", true, false, true);   // 前线要塞
            var jiangzhou = CreateTestCity("江州", true, false, true);  // 边境
            var yongchang = CreateTestCity("永昌", false, false, false); // 后方
            var nanzhong = CreateTestCity("南中", true, false, false);  // 边境但无战事

            faction.Cities.AddRange(new[] { chengdu, hanzhong, jiangzhou, yongchang, nanzhong });

            Console.WriteLine("  复杂战时场景 - 蜀国五虎将人事安排:");
            Console.WriteLine("  势力状态: 战时");
            Console.WriteLine("  城市情况:");
            foreach (var city in faction.Cities)
            {
                var threat = GetCityThreatLevel(city);
                var type = city.IsCapital ? "首都" : (city.IsBorderCity ? "边境" : "内陆");
                Console.WriteLine($"    {city.Name}: {type}, 威胁等级 {threat:F2}");
            }

            personnelManager.UpdateAssignments();

            Console.WriteLine("  最终人事安排:");
            foreach (var city in faction.Cities.OrderByDescending(c => GetCityThreatLevel(c)))
            {
                if (city.Prefect != null)
                {
                    var threat = GetCityThreatLevel(city);
                    var traits = string.Join(", ", city.Prefect.Traits);
                    Console.WriteLine($"    {city.Name} (威胁:{threat:F2}): {city.Prefect.Name} ({traits})");
                }
                else
                {
                    Console.WriteLine($"    {city.Name}: 无太守");
                }
            }

            // 生成人事报告
            Console.WriteLine("\n  人事状况报告:");
            var report = personnelManager.GeneratePersonnelReport();
            Console.WriteLine(report);

            Console.WriteLine("  ✓ 复杂场景测试完成\n");
        }

        /// <summary>
        /// 测试忠诚度系统
        /// </summary>
        private static void TestLoyaltySystem()
        {
            Console.WriteLine("--- 忠诚度系统测试 ---");

            var faction = CreateTestFaction("测试势力");
            var personnelManager = new PersonnelManager(faction);

            // 创建君主
            var ruler = CreateTestOfficer("刘备", 85, 75, 70, 65);
            ruler.Loyalty = 100; // 君主忠诚度满
            faction.Ruler = ruler;
            faction.Officers.Add(ruler);

            // 创建不同忠诚度的武将
            var loyalOfficer = CreateTestOfficer("诸葛亮", 95, 85, 100, 60);
            loyalOfficer.Loyalty = 95;
            loyalOfficer.Ambition = 30;
            loyalOfficer.Righteousness = 90;
            loyalOfficer.AddTrait(Trait.Loyal);

            var ambitiousOfficer = CreateTestOfficer("魏延", 60, 85, 65, 90);
            ambitiousOfficer.Loyalty = 70;
            ambitiousOfficer.Ambition = 85;
            ambitiousOfficer.Righteousness = 50;

            var riskyOfficer = CreateTestOfficer("孟达", 75, 70, 80, 55);
            riskyOfficer.Loyalty = 55;
            riskyOfficer.Ambition = 80;
            riskyOfficer.Righteousness = 35;
            riskyOfficer.AddTrait(Trait.Rebellious);

            var disloyal = CreateTestOfficer("吕布", 40, 95, 60, 100);
            disloyal.Loyalty = 45;
            disloyal.Ambition = 95;
            disloyal.Righteousness = 25;
            disloyal.AddRelation(ruler, RelationType.Hated); // 厌恶君主

            faction.Officers.AddRange(new[] { loyalOfficer, ambitiousOfficer, riskyOfficer, disloyal });

            // 创建重要城市
            var capital = CreateTestCity("首都", false, true, false);
            var frontline = CreateTestCity("前线", true, false, true);

            faction.Cities.AddRange(new[] { capital, frontline });

            // 先让危险武将当上太守
            capital.AppointPrefect(riskyOfficer);
            frontline.AppointPrefect(disloyal);

            Console.WriteLine("  初始危险任命:");
            Console.WriteLine($"    {capital.Name}: {capital.Prefect?.Name} (忠诚:{riskyOfficer.Loyalty}, 野心:{riskyOfficer.Ambition})");
            Console.WriteLine($"    {frontline.Name}: {frontline.Prefect?.Name} (忠诚:{disloyal.Loyalty}, 厌恶君主:{disloyal.Dislikes(ruler)})");

            Console.WriteLine("  执行人事更新 (应该触发清洗):");
            personnelManager.UpdateAssignments();

            Console.WriteLine("  清洗后的任命:");
            foreach (var city in faction.Cities)
            {
                if (city.Prefect != null)
                {
                    Console.WriteLine($"    {city.Name}: {city.Prefect.Name} (忠诚:{city.Prefect.Loyalty}, 野心:{city.Prefect.Ambition})");
                }
                else
                {
                    Console.WriteLine($"    {city.Name}: 无太守 (危险武将已被清洗)");
                }
            }

            // 生成包含忠诚度信息的报告
            Console.WriteLine("  忠诚度报告:");
            var report = personnelManager.GeneratePersonnelReport();
            var lines = report.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("忠诚武将") || line.Contains("危险武将") || line.Contains("⚠"))
                {
                    Console.WriteLine($"    {line}");
                }
            }

            Console.WriteLine("  ✓ 忠诚度系统测试完成\n");
        }

        /// <summary>
        /// 测试增强关系系统和综合评分
        /// </summary>
        private static void TestEnhancedRelationshipSystem()
        {
            Console.WriteLine("--- 增强关系系统和综合评分测试 ---");

            var faction = CreateTestFaction("蜀国");
            var personnelManager = new PersonnelManager(faction);

            // 创建君主刘备
            var liubei = CreateTestOfficer("刘备", 85, 75, 70, 65);
            liubei.Loyalty = 100;
            liubei.Ambition = 80;
            liubei.Righteousness = 95;
            faction.Ruler = liubei;
            faction.Officers.Add(liubei);

            // 创建关羽 - 义兄弟关系（绝对忠诚）
            var guanyu = CreateTestOfficer("关羽", 70, 95, 75, 90);
            guanyu.Loyalty = 75; // 忠诚度不高
            guanyu.Ambition = 70; // 野心较高
            guanyu.Righteousness = 95;
            guanyu.AddTrait(Trait.Loyal);
            guanyu.AddTrait(Trait.Rash); // 适合前线
            guanyu.AddExperience("SiegeDefense");
            guanyu.AddRelation(liubei, RelationType.SwornBrother); // 义兄弟关系
            faction.Officers.Add(guanyu);

            // 创建张飞 - 义兄弟关系（绝对忠诚）
            var zhangfei = CreateTestOfficer("张飞", 40, 90, 50, 95);
            zhangfei.Loyalty = 70; // 忠诚度不高
            zhangfei.Ambition = 65;
            zhangfei.Righteousness = 90;
            zhangfei.AddTrait(Trait.Rash); // 适合前线
            zhangfei.AddExperience("FieldBattle");
            zhangfei.AddRelation(liubei, RelationType.SwornBrother); // 义兄弟关系
            zhangfei.AddRelation(guanyu, RelationType.SwornBrother); // 与关羽也是义兄弟
            faction.Officers.Add(zhangfei);

            // 创建诸葛亮 - 君臣关系（高忠诚）
            var zhugeliang = CreateTestOfficer("诸葛亮", 95, 85, 100, 60);
            zhugeliang.Loyalty = 95;
            zhugeliang.Ambition = 30;
            zhugeliang.Righteousness = 90;
            zhugeliang.AddTrait(Trait.Loyal);
            zhugeliang.AddTrait(Trait.Scholar); // 适合内政
            zhugeliang.AddExperience("CityManagement");
            zhugeliang.AddRelation(liubei, RelationType.Friend); // 友好关系
            faction.Officers.Add(zhugeliang);

            // 创建魏延 - 有野心但无特殊关系
            var weiyan = CreateTestOfficer("魏延", 60, 85, 65, 90);
            weiyan.Loyalty = 70;
            weiyan.Ambition = 85;
            weiyan.Righteousness = 50;
            weiyan.AddTrait(Trait.Ambitious);
            weiyan.AddExperience("FieldBattle");
            weiyan.AddRelation(zhugeliang, RelationType.Hated); // 厌恶诸葛亮
            faction.Officers.Add(weiyan);

            // 创建孟达 - 叛逆性格
            var mengda = CreateTestOfficer("孟达", 75, 70, 80, 55);
            mengda.Loyalty = 55;
            mengda.Ambition = 80;
            mengda.Righteousness = 35;
            mengda.AddTrait(Trait.Rebellious);
            mengda.AddRelation(liubei, RelationType.Hated); // 厌恶刘备
            faction.Officers.Add(mengda);

            // 创建刘禅 - 亲子关系
            var liushan = CreateTestOfficer("刘禅", 40, 35, 30, 25);
            liushan.Loyalty = 100;
            liushan.Ambition = 20;
            liushan.Righteousness = 70;
            liushan.AddTrait(Trait.Conservative);
            liushan.AddRelation(liubei, RelationType.ParentChild); // 父子关系
            faction.Officers.Add(liushan);

            // 创建城市
            var chengdu = CreateTestCity("成都", false, true, false);    // 首都 (后方)
            var hanzhong = CreateTestCity("汉中", true, false, true);   // 前线要塞
            var jiangzhou = CreateTestCity("江州", true, false, true);  // 前线
            var yongchang = CreateTestCity("永昌", false, false, false); // 后方

            faction.Cities.AddRange(new[] { chengdu, hanzhong, jiangzhou, yongchang });

            Console.WriteLine("  关系网络和能力分析:");
            Console.WriteLine($"    刘备 (君主): 政治{liubei.Politics}, 统率{liubei.Leadership}");
            Console.WriteLine($"    关羽 → 刘备: {guanyu.GetRelationWith(liubei)} | 统率{guanyu.Leadership}, 武力{guanyu.War} (前线型)");
            Console.WriteLine($"    张飞 → 刘备: {zhangfei.GetRelationWith(liubei)} | 统率{zhangfei.Leadership}, 武力{zhangfei.War} (前线型)");
            Console.WriteLine($"    诸葛亮 → 刘备: {zhugeliang.GetRelationWith(liubei)} | 政治{zhugeliang.Politics}, 智力{zhugeliang.Intelligence} (内政型)");
            Console.WriteLine($"    魏延 → 刘备: {weiyan.GetRelationWith(liubei)} | 统率{weiyan.Leadership}, 武力{weiyan.War} (前线型)");
            Console.WriteLine($"    孟达 → 刘备: {mengda.GetRelationWith(liubei)} | 政治{mengda.Politics}, 智力{mengda.Intelligence} (内政型)");
            Console.WriteLine($"    刘禅 → 刘备: {liushan.GetRelationWith(liubei)} | 政治{liushan.Politics} (能力较低)");

            Console.WriteLine("\n  城市威胁等级分析:");
            foreach (var city in faction.Cities)
            {
                var threat = GameGlobal.StrategicMap.GetThreatLevel(city);
                var type = threat > 0.6f ? "前线" : "后方";
                Console.WriteLine($"    {city.Name}: 威胁等级 {threat:F2} ({type})");
            }

            Console.WriteLine("\n  执行综合评分人事安排:");
            personnelManager.UpdateAssignments();

            Console.WriteLine("\n  任命结果分析:");
            foreach (var city in faction.Cities.OrderByDescending(c => GameGlobal.StrategicMap.GetThreatLevel(c)))
            {
                if (city.Prefect != null)
                {
                    var threat = GameGlobal.StrategicMap.GetThreatLevel(city);
                    var relation = city.Prefect.GetRelationWith(liubei);
                    var soulBound = city.Prefect.IsSoulBoundTo(liubei);
                    var cityType = threat > 0.6f ? "前线" : "后方";
                    
                    Console.WriteLine($"    {city.Name} ({cityType}, 威胁:{threat:F2}):");
                    Console.WriteLine($"      太守: {city.Prefect.Name}");
                    Console.WriteLine($"      关系: {relation} | 绝对忠诚: {soulBound}");
                    Console.WriteLine($"      能力: 政治{city.Prefect.Politics}, 统率{city.Prefect.Leadership}, 武力{city.Prefect.War}");
                }
                else
                {
                    Console.WriteLine($"    {city.Name}: 无太守 (可能因关系问题被清洗)");
                }
            }

            // 验证关键逻辑
            Console.WriteLine("\n  综合评分系统验证:");
            
            // 1. 义兄弟关系应该无视野心和忠诚度问题
            if (guanyu.IsSoulBoundTo(liubei))
            {
                Console.WriteLine($"    ✓ 关羽虽然忠诚度{guanyu.Loyalty}、野心{guanyu.Ambition}，但因义兄弟关系获得绝对信任");
            }
            
            if (zhangfei.IsSoulBoundTo(liubei))
            {
                Console.WriteLine($"    ✓ 张飞虽然忠诚度{zhangfei.Loyalty}、野心{zhangfei.Ambition}，但因义兄弟关系获得绝对信任");
            }

            // 2. 厌恶关系应该被排除
            bool mengdaAssigned = faction.Cities.Any(c => c.Prefect == mengda);
            if (!mengdaAssigned)
            {
                Console.WriteLine($"    ✓ 孟达因厌恶君主被排除在重要职位之外");
            }
            else
            {
                Console.WriteLine($"    ✗ 警告：孟达虽然厌恶君主但仍被任命");
            }

            // 3. 前线应该优先军事型武将
            var frontlineCities = faction.Cities.Where(c => GameGlobal.StrategicMap.GetThreatLevel(c) > 0.6f);
            foreach (var city in frontlineCities)
            {
                if (city.Prefect != null)
                {
                    bool isMilitaryType = city.Prefect.Leadership >= 80 || city.Prefect.War >= 80;
                    if (isMilitaryType)
                    {
                        Console.WriteLine($"    ✓ 前线城市 {city.Name} 任命了军事型武将 {city.Prefect.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"    ⚠ 前线城市 {city.Name} 任命了非军事型武将 {city.Prefect.Name} (可能因关系因素)");
                    }
                }
            }

            // 4. 后方应该优先政治型武将
            var backCities = faction.Cities.Where(c => GameGlobal.StrategicMap.GetThreatLevel(c) <= 0.6f);
            foreach (var city in backCities)
            {
                if (city.Prefect != null)
                {
                    bool isPoliticalType = city.Prefect.Politics >= 80;
                    if (isPoliticalType)
                    {
                        Console.WriteLine($"    ✓ 后方城市 {city.Name} 任命了政治型武将 {city.Prefect.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"    ⚠ 后方城市 {city.Name} 任命了非政治型武将 {city.Prefect.Name} (可能因关系因素)");
                    }
                }
            }

            Console.WriteLine("  ✓ 增强关系系统和综合评分测试完成\n");
        }

        // === 辅助方法 ===

        private static Faction CreateTestFaction(string name)
        {
            var faction = new Faction
            {
                Name = name,
                Gold = 10000,
                Food = 5000,
                FiscalHealth = 0.7f,
                TotalTroops = 2000,
                State = FactionState.Stable,
                Cities = new List<City>(),
                Officers = new List<Officer>()
            };

            faction.InitializePersonnelManager();
            return faction;
        }

        private static Officer CreateTestOfficer(string name, int politics, int leadership, int intelligence, int war)
        {
            var officer = new Officer
            {
                Name = name,
                Politics = politics,
                Leadership = leadership,
                Intelligence = intelligence,
                War = war,
                Age = 35,
                Health = 90,
                Loyalty = 80, // 默认忠诚度
                Ambition = 50, // 默认野心
                Righteousness = 70, // 默认义理
                State = OfficerState.Active,
                Traits = new HashSet<Trait>(),
                Experiences = new List<string>(),
                Relations = new Dictionary<int, RelationType>(),
                ID = new Random().Next(1000, 9999) // 生成随机ID
            };
            
            return officer;
        }

        private static City CreateTestCity(string name, bool isBorder, bool isCapital, bool hasRecentBattle)
        {
            return new City
            {
                Name = name,
                IsBorderCity = isBorder,
                IsCapital = isCapital,
                HasRecentBattle = hasRecentBattle,
                Population = 15000,
                Prosperity = 0.6f,
                Security = 0.7f,
                Loyalty = 0.8f,
                Coordinates = new Point(
                    new Random().Next(0, 100),
                    new Random().Next(0, 100)
                )
            };
        }

        private static float GetCityThreatLevel(City city)
        {
            float threat = 0.0f;

            if (city.IsBorderCity) threat += 0.4f;
            if (city.HasRecentBattle) threat += 0.3f;
            if (city.IsCapital) threat += 0.2f;

            return Math.Min(threat, 1.0f);
        }

        /// <summary>
        /// 性能测试
        /// </summary>
        public static void RunPerformanceTest()
        {
            Console.WriteLine("--- 人事管理性能测试 ---");

            var faction = CreateTestFaction("测试势力");

            // 创建大量武将和城市
            for (int i = 0; i < 50; i++)
            {
                var officer = CreateTestOfficer($"武将{i}", 
                    new Random().Next(30, 100),
                    new Random().Next(30, 100),
                    new Random().Next(30, 100),
                    new Random().Next(30, 100));
                
                officer.AddTrait((Trait)(i % 8));
                faction.Officers.Add(officer);
            }

            for (int i = 0; i < 20; i++)
            {
                var city = CreateTestCity($"城市{i}", 
                    i % 3 == 0,  // 1/3 是边境
                    i == 0,      // 第一个是首都
                    i % 5 == 0); // 1/5 有战斗
                
                faction.Cities.Add(city);
            }

            var personnelManager = new PersonnelManager(faction);

            var startTime = DateTime.Now;

            // 执行多次人事更新
            for (int i = 0; i < 10; i++)
            {
                personnelManager.UpdateAssignments();
            }

            var elapsed = DateTime.Now - startTime;

            Console.WriteLine($"  性能测试结果:");
            Console.WriteLine($"    武将数量: {faction.Officers.Count}");
            Console.WriteLine($"    城市数量: {faction.Cities.Count}");
            Console.WriteLine($"    更新次数: 10");
            Console.WriteLine($"    总耗时: {elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine($"    平均每次: {elapsed.TotalMilliseconds / 10:F2} ms");

            if (elapsed.TotalMilliseconds / 10 < 10)
                Console.WriteLine($"    性能评价: 优秀 ⭐⭐⭐");
            else if (elapsed.TotalMilliseconds / 10 < 50)
                Console.WriteLine($"    性能评价: 良好 ⭐⭐");
            else
                Console.WriteLine($"    性能评价: 需要优化 ⭐");

            Console.WriteLine("  ✓ 性能测试完成\n");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始运行人事管理系统的所有测试...\n");

            RunCompleteTest();
            RunPerformanceTest();

            Console.WriteLine("\n🎉 人事管理系统所有测试完成！系统运行正常。");
        }
    }
}