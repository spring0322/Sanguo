// ===================================================================
// 说服按钮可用条件完整代码集合
// 包含从UI显示到执行条件的完整实现
// ===================================================================

// ===================================================================
// 1. 核心可用性检查 - Architecture.cs 中的说服功能检查
// ===================================================================

namespace GameObjects
{
    public partial class Architecture : GameObject
    {
        /// <summary>
        /// 检查说服功能是否可用 - 核心方法
        /// </summary>
        /// <returns>true=可用, false=不可用</returns>
        public bool ConvincePersonAvail()
        {
            // 三个必要条件：
            // 1. 有可用人物（排除女官）
            // 2. 资金足够支付说服费用
            // 3. 有可说服的目标区域
            return ((this.HasPerson() && (this.Fund >= this.ConvincePersonFund)) && 
                    (this.GetConvincePersonArchitectureArea().Count > 0));
        }

        /// <summary>
        /// 检查是否有人物（排除女官）
        /// </summary>
        /// <returns>true=有人物, false=无人物</returns>
        public bool HasPerson()
        {
            return (this.Persons.Count > 0);
        }

        /// <summary>
        /// 说服所需资金 - 基于基础费用和建筑修正
        /// </summary>
        public int ConvincePersonFund
        {
            get
            {
                // 基础费用 * 建筑说服费用修正系数
                return (int)(Session.Parameters.ConvincePersonCost * this.RateOfConvincePerson);
            }
        }

        /// <summary>
        /// 说服费用修正系数 - 可被建筑影响和设施修改
        /// </summary>
        public float RateOfConvincePerson = 1f;

        /// <summary>
        /// 最大说服人数 - 基于资金计算
        /// </summary>
        public int ConvincePersonMaxCount
        {
            get
            {
                if (this.ConvincePersonFund == 0) return int.MaxValue;
                return (this.Fund / this.ConvincePersonFund);
            }
        }

        /// <summary>
        /// 获取可说服的目标区域 - 包括己方和敌方建筑
        /// </summary>
        /// <returns>可说服的区域范围</returns>
        public GameArea GetConvincePersonArchitectureArea()
        {
            GameArea area = new GameArea();
            
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                if (architecture.BelongedFaction == this.BelongedFaction)
                {
                    // 己方建筑：必须有俘虏或在野人物才能说服
                    if (!architecture.HasCaptive() && !architecture.HasNoFactionPerson())
                    {
                        continue;
                    }
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
                else
                {
                    // 敌方建筑：必须有人物或在野人物，且建筑已知
                    if ((!architecture.HasPerson() && !architecture.HasNoFactionPerson()) || 
                        !this.BelongedFaction.IsArchitectureKnown(architecture))
                    {
                        continue;
                    }
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
            }
            
            return area;
        }

        /// <summary>
        /// 可执行说服的人物列表（排除女官）
        /// </summary>
        public PersonList PersonsExcludeNvGuan
        {
            get
            {
                PersonList all = Session.Current.Scenario.GetPersonList(this);
                PersonList result = new PersonList();

                foreach (Person p in all)
                {
                    if (!p.NvGuan)  // 排除女官
                    {
                        result.Add(p);
                    }
                }

                result.SetImmutable();
                return result;
            }
        }

        /// <summary>
        /// 检查是否有俘虏
        /// </summary>
        public bool HasCaptive()
        {
            return (this.Captives.Count > 0);
        }

        /// <summary>
        /// 检查是否有在野人物
        /// </summary>
        public bool HasNoFactionPerson()
        {
            return (this.NoFactionPersons.Count > 0);
        }

        /// <summary>
        /// 检查资金是否充足
        /// </summary>
        public bool IsFundEnough
        {
            get
            {
                return this.Fund >= this.ConvincePersonFund;
            }
        }
    }
}

// ===================================================================
// 2. 全局参数配置 - Session.Parameters 中的说服费用设置
// ===================================================================

namespace GameGlobal
{
    public class Parameters
    {
        /// <summary>
        /// 说服基础费用 - 可在游戏设置中调整
        /// </summary>
        public static int ConvincePersonCost = 1000; // 默认1000金
        
        // 其他相关参数...
    }
}

// ===================================================================
// 3. UI菜单显示条件 - ContextMenuData.xml 配置
// ===================================================================

/*
在 Content/Data/Plugins/ContextMenuData.xml 中的配置：

<MenuItem ID="4" Name="Convince" DisplayName="说服" DisplayIfTrue="ConvincePersonAvail" />

这个配置表示：
- ID="4": 菜单项标识符
- Name="Convince": 内部名称
- DisplayName="说服": 显示给用户的文本
- DisplayIfTrue="ConvincePersonAvail": 当ConvincePersonAvail()返回true时才显示此菜单项
*/

// ===================================================================
// 4. 右键菜单处理 - MGSContextMenu.cs 中的说服选项处理
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class MainGameScreen
    {
        /// <summary>
        /// 处理说服菜单选择
        /// </summary>
        private void HandleContextMenuResult(ContextMenuResult result)
        {
            switch (result)
            {
                case ContextMenuResult.Person_Convince:
                    // 设置最大选择人数
                    this.Plugins.TabListPlugin.SetSelectedItemMaxCount(this.CurrentArchitecture.ConvincePersonMaxCount);
                    
                    // 显示人物选择界面
                    this.ShowTabListInFrame(
                        UndoneWorkKind.Frame, 
                        FrameKind.Work, 
                        FrameFunction.GetConvinceSourcePerson, 
                        false, true, true, true, 
                        this.CurrentArchitecture.PersonsExcludeNvGuan,  // 可选择的人物列表
                        null, 
                        "说服",  // 界面标题
                        "说服"   // 界面描述
                    );
                    break;
            }
        }
    }
}

// ===================================================================
// 5. 说服执行逻辑 - ScreenManager.cs 中的说服处理
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class ScreenManager
    {
        /// <summary>
        /// 处理说服人物选择完成后的逻辑
        /// </summary>
        private void FrameFunction_Architecture_AfterGetConvinceSourcePerson()
        {
            // 获取选中的执行说服的人物列表
            this.CurrentGameObjects = this.CurrentArchitecture.PersonsExcludeNvGuan.GetSelectedList();
            
            if (this.CurrentGameObjects != null)
            {
                // 显示目标选择界面（选择要说服的目标）
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.GetConvinceDestinationPerson, 
                    false, true, true, false, 
                    this.CurrentArchitecture.GetConvinceDestinationPersonList(), 
                    null, 
                    "说服", 
                    "Personal"
                );
            }
        }

        /// <summary>
        /// 处理Frame函数调用
        /// </summary>
        private void HandleFrameFunction(FrameFunction function)
        {
            switch (function)
            {
                case FrameFunction.GetConvinceSourcePerson:
                    this.FrameFunction_Architecture_AfterGetConvinceSourcePerson();
                    break;
                // ... 其他case
            }
        }
    }
}

// ===================================================================
// 6. AI自动说服逻辑 - Architecture.cs 中的AI说服判定
// ===================================================================

namespace GameObjects
{
    public partial class Architecture
    {
        /// <summary>
        /// AI自动说服逻辑 - 在AI回合中调用
        /// </summary>
        private void ConvinceNoFactionAI()
        {
            // AI说服条件：
            // 1. 有人物可执行说服
            // 2. 资金充足
            // 3. 有在野人物可说服
            // 4. 没有敌对部队在视野内（安全环境）
            if (this.HasPerson() && this.IsFundEnough && this.HasNoFactionPerson() && !this.HasHostileTroopsInView())
            {
                GameObjectList convincer = this.PersonsExcludeNvGuan.GetList();
                // AI会自动选择合适的人物执行说服
                // 具体实现省略...
            }
        }

        /// <summary>
        /// AI策略中的说服判定
        /// </summary>
        private void AIPersonTactics()
        {
            // AI在策略阶段会考虑说服
            if ((this.HasPerson() && (GameObject.Random(this.Fund) >= this.ConvincePersonFund)) && 
                GameObject.Chance(50) && this.BelongedSection.AIDetail.AllowPersonTactics)
            {
                ArchitectureList targetArchitectures = new ArchitectureList();
                foreach (Architecture architecture in this.BelongedFaction.KnownArchitectures)
                {
                    if (((architecture.BelongedFaction != this.BelongedFaction) && 
                         (architecture.BelongedFaction != null)) && architecture.HasPerson())
                    {
                        targetArchitectures.Add(architecture);
                    }
                }
                
                if (targetArchitectures.Count > 0)
                {
                    // AI会选择目标执行说服
                    // 具体实现省略...
                }
            }
        }
    }
}

// ===================================================================
// 7. 军区AI设置 - SectionAIDetail 中的说服权限控制
// ===================================================================

namespace GameObjects.SectionDetail
{
    public class SectionAIDetail
    {
        /// <summary>
        /// 是否允许使用流言和说服策略
        /// </summary>
        public bool AllowPersonTactics { get; set; } = true;
        
        /// <summary>
        /// 是否允许使用情报和间谍
        /// </summary>
        public bool AllowInvestigateTactics { get; set; } = true;
        
        /// <summary>
        /// 是否允许使用煽动和破坏
        /// </summary>
        public bool AllowOffensiveTactics { get; set; } = true;
    }
}

// ===================================================================
// 8. 说服费用影响系统 - 建筑设施对说服的影响
// ===================================================================

namespace GameObjects.Influences.InfluenceKindPack
{
    /// <summary>
    /// 影响说服费用的设施效果
    /// </summary>
    public class InfluenceKind3092 : InfluenceKind
    {
        /// <summary>
        /// 应用设施影响 - 降低说服费用
        /// </summary>
        public override void ApplyInfluenceKind(Architecture architecture)
        {
            architecture.RateOfConvincePerson -= 1 - this.rate;  // 降低说服费用
        }

        /// <summary>
        /// 移除设施影响 - 恢复说服费用
        /// </summary>
        public override void PurifyInfluenceKind(Architecture architecture)
        {
            architecture.RateOfConvincePerson += 1 - this.rate;  // 恢复说服费用
        }
    }
}

// ===================================================================
// 9. 说服相关枚举定义
// ===================================================================

namespace GameGlobal
{
    /// <summary>
    /// 右键菜单结果枚举
    /// </summary>
    public enum ContextMenuResult
    {
        // ... 其他枚举值
        Person_Convince,        // 说服菜单项
        // ... 其他枚举值
    }

    /// <summary>
    /// Frame函数枚举
    /// </summary>
    public enum FrameFunction
    {
        // ... 其他枚举值
        GetConvinceSourcePerson,        // 选择执行说服的人物
        GetConvinceDestinationPerson,   // 选择说服目标
        // ... 其他枚举值
    }

    /// <summary>
    /// 外出任务类型枚举
    /// </summary>
    public enum OutsideTaskKind
    {
        // ... 其他枚举值
        说服,  // 说服任务
        // ... 其他枚举值
    }
}

// ===================================================================
// 10. 说服执行过程 - Person.cs 中的说服任务执行
// ===================================================================

namespace GameObjects
{
    public partial class Person
    {
        /// <summary>
        /// 正在说服的目标人物
        /// </summary>
        public Person ConvincingPerson { get; set; }

        /// <summary>
        /// 执行说服任务
        /// </summary>
        /// <param name="person">要说服的目标人物</param>
        public void ExecuteConvinceTask(Person person)
        {
            // 设置外出任务
            this.OutsideTask = OutsideTaskKind.说服;
            this.ConvincingPerson = person;
            
            // 扣除说服费用
            this.LocationArchitecture.DecreaseFund(this.LocationArchitecture.ConvincePersonFund);
            
            // 前往目标地点执行说服
            this.GoToDestinationAndReturn(this.OutsideDestination.Value);
            
            // 设置任务天数
            this.TaskDays = (this.ArrivingDays + 1) / 2;
        }
    }
}

// ===================================================================
// 11. 完整的说服可用性检查示例
// ===================================================================

namespace GameManager
{
    /// <summary>
    /// 说服功能可用性检查示例
    /// </summary>
    public class ConvinceAvailabilityChecker
    {
        /// <summary>
        /// 全面检查说服功能是否可用
        /// </summary>
        /// <param name="architecture">要检查的建筑</param>
        /// <returns>检查结果和详细信息</returns>
        public static ConvinceAvailabilityResult CheckConvinceAvailability(Architecture architecture)
        {
            var result = new ConvinceAvailabilityResult();
            
            // 1. 检查是否有可用人物
            if (!architecture.HasPerson())
            {
                result.IsAvailable = false;
                result.Reason = "没有可用人物执行说服";
                return result;
            }
            
            // 2. 检查是否有非女官人物
            if (architecture.PersonsExcludeNvGuan.Count == 0)
            {
                result.IsAvailable = false;
                result.Reason = "没有非女官人物可执行说服";
                return result;
            }
            
            // 3. 检查资金是否充足
            if (architecture.Fund < architecture.ConvincePersonFund)
            {
                result.IsAvailable = false;
                result.Reason = $"资金不足，需要{architecture.ConvincePersonFund}金，当前只有{architecture.Fund}金";
                return result;
            }
            
            // 4. 检查是否有可说服的目标区域
            var targetArea = architecture.GetConvincePersonArchitectureArea();
            if (targetArea.Count == 0)
            {
                result.IsAvailable = false;
                result.Reason = "没有可说服的目标区域";
                return result;
            }
            
            // 5. 检查军区AI设置是否允许
            if (!architecture.BelongedSection.AIDetail.AllowPersonTactics)
            {
                result.IsAvailable = false;
                result.Reason = "军区AI设置不允许使用人物策略";
                return result;
            }
            
            // 所有条件都满足
            result.IsAvailable = true;
            result.Reason = "说服功能可用";
            result.MaxConvinceCount = architecture.ConvincePersonMaxCount;
            result.ConvinceCost = architecture.ConvincePersonFund;
            result.AvailablePersons = architecture.PersonsExcludeNvGuan.Count;
            result.TargetAreaSize = targetArea.Count;
            
            return result;
        }
    }

    /// <summary>
    /// 说服可用性检查结果
    /// </summary>
    public class ConvinceAvailabilityResult
    {
        public bool IsAvailable { get; set; }
        public string Reason { get; set; }
        public int MaxConvinceCount { get; set; }
        public int ConvinceCost { get; set; }
        public int AvailablePersons { get; set; }
        public int TargetAreaSize { get; set; }
    }
}

// ===================================================================
// 12. 使用示例和测试代码
// ===================================================================

namespace GameManager
{
    /// <summary>
    /// 说服功能测试示例
    /// </summary>
    public class ConvinceTestExample
    {
        /// <summary>
        /// 测试说服功能可用性
        /// </summary>
        public static void TestConvinceAvailability()
        {
            // 获取当前建筑
            Architecture currentArch = Session.Current.Scenario.CurrentPlayer?.CapitalArchitecture;
            
            if (currentArch != null)
            {
                Console.WriteLine("=== 说服功能可用性测试 ===");
                
                // 基础检查
                bool basicAvail = currentArch.ConvincePersonAvail();
                Console.WriteLine($"基础可用性: {basicAvail}");
                
                // 详细检查
                var detailResult = ConvinceAvailabilityChecker.CheckConvinceAvailability(currentArch);
                Console.WriteLine($"详细检查结果: {detailResult.IsAvailable}");
                Console.WriteLine($"原因: {detailResult.Reason}");
                
                if (detailResult.IsAvailable)
                {
                    Console.WriteLine($"最大说服次数: {detailResult.MaxConvinceCount}");
                    Console.WriteLine($"单次说服费用: {detailResult.ConvinceCost}");
                    Console.WriteLine($"可用人物数: {detailResult.AvailablePersons}");
                    Console.WriteLine($"目标区域大小: {detailResult.TargetAreaSize}");
                }
                
                // 检查具体条件
                Console.WriteLine("\n=== 详细条件检查 ===");
                Console.WriteLine($"有人物: {currentArch.HasPerson()}");
                Console.WriteLine($"当前资金: {currentArch.Fund}");
                Console.WriteLine($"说服费用: {currentArch.ConvincePersonFund}");
                Console.WriteLine($"资金充足: {currentArch.Fund >= currentArch.ConvincePersonFund}");
                Console.WriteLine($"非女官人物数: {currentArch.PersonsExcludeNvGuan.Count}");
                Console.WriteLine($"目标区域数: {currentArch.GetConvincePersonArchitectureArea().Count}");
                Console.WriteLine($"允许人物策略: {currentArch.BelongedSection.AIDetail.AllowPersonTactics}");
            }
            else
            {
                Console.WriteLine("无法获取当前建筑进行测试");
            }
        }
        
        /// <summary>
        /// 测试说服按钮在不同情况下的显示状态
        /// </summary>
        public static void TestConvinceButtonStates()
        {
            Console.WriteLine("=== 说服按钮状态测试 ===");
            
            foreach (Architecture arch in Session.Current.Scenario.CurrentPlayer.Architectures)
            {
                bool isAvailable = arch.ConvincePersonAvail();
                Console.WriteLine($"{arch.Name}: {(isAvailable ? "✅可用" : "❌不可用")}");
                
                if (!isAvailable)
                {
                    var result = ConvinceAvailabilityChecker.CheckConvinceAvailability(arch);
                    Console.WriteLine($"  原因: {result.Reason}");
                }
            }
        }
    }
}

// ===================================================================
// 13. 完整流程总结
// ===================================================================

/*
说服按钮可用条件完整流程：

1. 【UI显示检查】
   - ContextMenuData.xml 中配置 DisplayIfTrue="ConvincePersonAvail"
   - 只有当 Architecture.ConvincePersonAvail() 返回 true 时才显示说服菜单项

2. 【核心可用性检查】
   - Architecture.ConvincePersonAvail() 方法检查三个条件：
     a) HasPerson() - 建筑内有人物
     b) Fund >= ConvincePersonFund - 资金充足
     c) GetConvincePersonArchitectureArea().Count > 0 - 有可说服的目标区域

3. 【详细条件分解】
   a) 人物条件：
      - 建筑内必须有人物 (Persons.Count > 0)
      - 必须有非女官人物 (PersonsExcludeNvGuan.Count > 0)
   
   b) 资金条件：
      - 当前资金 >= 说服基础费用 * 建筑修正系数
      - 说服费用 = Session.Parameters.ConvincePersonCost * RateOfConvincePerson
   
   c) 目标条件：
      - 己方建筑：有俘虏或在野人物
      - 敌方建筑：有人物或在野人物，且建筑已知

4. 【AI权限检查】
   - 军区AI设置必须允许人物策略 (AllowPersonTactics = true)

5. 【执行流程】
   - 用户点击说服菜单 → 显示人物选择界面
   - 选择执行说服的人物 → 显示目标选择界面
   - 选择说服目标 → 扣除费用并执行说服任务

核心特点：
- 多层次的条件检查确保功能可用性
- 动态的费用计算支持建筑修正
- 智能的目标区域计算
- 完善的AI权限控制
- 详细的调试和测试支持
*/