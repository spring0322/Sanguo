// ===================================================================
// 军师任命条件调整 - 允许重新任命军师
// ===================================================================

// 在 Faction.cs 中的调整代码

namespace GameObjects
{
    public partial class Faction : GameObject
    {
        /// <summary>
        /// 检查是否可以任命军师 - 调整后版本
        /// 放宽条件：允许重新任命（已有军师时也显示按钮）
        /// </summary>
        public bool AppointAdvisorAvail()
        {
            // 放宽条件：允许重新任命（已有军师时也显示按钮）
            if (this.Leader != null && this.Leader.BelongedCaptive == null)
            {
                // 检查是否有候选人（排除当前军师）
                PersonList candidates = this.GetAdvisorCandidates(true); // true表示排除当前军师
                return candidates.Count > 0;
            }
            return false;
        }

        /// <summary>
        /// 获取军师候选人列表 - 新增方法
        /// </summary>
        /// <param name="excludeCurrentAdvisor">是否排除当前军师</param>
        /// <returns>候选人列表</returns>
        public PersonList GetAdvisorCandidates(bool excludeCurrentAdvisor = true)
        {
            PersonList result = new PersonList();
            
            foreach (Person p in this.Persons)
            {
                // 基本条件检查
                if (p != this.Leader &&           // 不是领袖
                    p.Available &&                // 可用
                    p.Alive &&                   // 存活
                    p.BelongedCaptive == null &&  // 未被俘虏
                    p.LocationTroop == null &&    // 不在部队中
                    p.Intelligence >= 70)         // 智力要求
                {
                    // 根据参数决定是否排除当前军师
                    if (excludeCurrentAdvisor)
                    {
                        if (p != this.Advisor)  // 排除当前军师
                        {
                            result.Add(p);
                        }
                    }
                    else
                    {
                        result.Add(p);  // 不排除当前军师
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 军师候选人列表（玩家用） - 调整后版本
        /// 现在使用统一的GetAdvisorCandidates方法
        /// </summary>
        public PersonList AdvisorCandicate
        {
            get
            {
                // 对于玩家，显示所有候选人（包括当前军师，用于重新任命）
                return this.GetAdvisorCandidates(false);
            }
        }

        /// <summary>
        /// AI军师候选人列表（按智力排序） - 调整后版本
        /// </summary>
        public PersonList AIAdvisorCandicate
        {
            get
            {
                // 对于AI，排除当前军师（避免重复任命同一人）
                PersonList candidates = this.GetAdvisorCandidates(true);
                
                // 按智力降序排序
                candidates.Sort((p1, p2) => p2.Intelligence.CompareTo(p1.Intelligence));
                
                return candidates;
            }
        }

        /// <summary>
        /// 任命军师 - 增强版本，支持重新任命
        /// </summary>
        /// <param name="person">被任命的人物</param>
        public void AppointAdvisor(Person person)
        {
            // 如果是重新任命同一个人，直接返回
            if (this.Advisor != null && this.Advisor.ID == person.ID)
            {
                Console.WriteLine($"{person.Name} 已经是军师了");
                return;
            }

            // 记录原军师（用于日志）
            Person formerAdvisor = this.Advisor;
            
            // 记录到年表
            if (formerAdvisor != null)
            {
                // 重新任命的情况
                Session.Current.Scenario.YearTable.addReappointAdvisorEntry(
                    Session.Current.Scenario.Date, 
                    person, 
                    formerAdvisor, 
                    this.Leader
                );
            }
            else
            {
                // 首次任命的情况
                Session.Current.Scenario.YearTable.addAppointAdvisorEntry(
                    Session.Current.Scenario.Date, 
                    person, 
                    this.Leader
                );
            }
            
            // 设置新军师
            this.AdvisorID = person.ID;
            
            // ✅ 关键修复：清空advisor缓存并重新设置
            this.advisor = null;        // 清空缓存
            this.advisor = person;      // 重新设置
            
            // 触发事件
            if (this.OnAppointAdvisor != null)
            {
                this.OnAppointAdvisor(this.Leader, person);
            }

            // 日志输出
            if (formerAdvisor != null)
            {
                Console.WriteLine($"重新任命军师：{formerAdvisor.Name} → {person.Name}");
            }
            else
            {
                Console.WriteLine($"任命军师：{person.Name}");
            }
        }

        /// <summary>
        /// AI自动任命军师 - 调整后版本
        /// </summary>
        private void AIAppointAdvisor()
        {
            if (!Session.Current.Scenario.IsPlayer(this))
            {
                if (this.AppointAdvisorAvail())
                {
                    PersonList candidates = this.AIAdvisorCandicate;
                    if (candidates.Count > 0)
                    {
                        Person bestCandidate = candidates[0]; // 已按智力排序
                        
                        // AI只在没有军师或找到更好的候选人时才重新任命
                        if (this.Advisor == null || 
                            (bestCandidate.Intelligence > this.Advisor.Intelligence + 10)) // 智力差距超过10才换
                        {
                            this.AdvisorID = bestCandidate.ID;
                            this.AppointAdvisor(bestCandidate);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查是否需要显示"更换军师"按钮
        /// </summary>
        public bool CanChangeAdvisor()
        {
            if (this.Advisor != null)
            {
                // 有军师的情况下，检查是否有更好的候选人
                PersonList candidates = this.GetAdvisorCandidates(true); // 排除当前军师
                
                foreach (Person candidate in candidates)
                {
                    // 如果有智力更高的候选人，显示更换按钮
                    if (candidate.Intelligence > this.Advisor.Intelligence)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// 获取推荐的军师候选人（智力最高的）
        /// </summary>
        public Person GetRecommendedAdvisor()
        {
            PersonList candidates = this.GetAdvisorCandidates(true);
            
            if (candidates.Count > 0)
            {
                // 返回智力最高的候选人
                Person best = candidates[0];
                foreach (Person p in candidates)
                {
                    if (p.Intelligence > best.Intelligence)
                    {
                        best = p;
                    }
                }
                return best;
            }
            
            return null;
        }
    }
}

// ===================================================================
// YearTable.cs 中需要添加的新方法
// ===================================================================

namespace GameObjects
{
    public partial class YearTable
    {
        /// <summary>
        /// 添加重新任命军师的年表记录
        /// </summary>
        public void addReappointAdvisorEntry(GameDate date, Person newAdvisor, Person formerAdvisor, Person leader)
        {
            this.addTableEntry(
                date, 
                composeFactionList(newAdvisor.BelongedFaction),
                String.Format(
                    "君主{0}罢免了军师{1}，重新任命{2}为军师", 
                    leader.Name,
                    formerAdvisor.Name,
                    newAdvisor.Name
                ), 
                false
            );
            
            // 添加到新军师的个人传记
            this.addPersonInGameBiography(
                newAdvisor, 
                date,
                String.Format("接替{0}，被{1}任命为{2}的军师", 
                    formerAdvisor.Name, 
                    leader.Name, 
                    newAdvisor.BelongedFaction.Name)
            );
            
            // 添加到前军师的个人传记
            this.addPersonInGameBiography(
                formerAdvisor, 
                date,
                String.Format("被{0}罢免军师职务，由{1}接替", 
                    leader.Name, 
                    newAdvisor.Name)
            );
        }
    }
}

// ===================================================================
// MGSContextMenu.cs 中的调整
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class MGSContextMenu
    {
        private void UpdateAdvisorMenuItems()
        {
            // 任命/重新任命军师
            if (this.CurrentFaction.AppointAdvisorAvail())
            {
                if (this.CurrentFaction.Advisor != null)
                {
                    // 已有军师，显示"重新任命军师"
                    this.AddMenuItem("重新任命军师", ContextMenuResult.Person_Appointment_AppointAdvisor);
                }
                else
                {
                    // 没有军师，显示"任命军师"
                    this.AddMenuItem("任命军师", ContextMenuResult.Person_Appointment_AppointAdvisor);
                }
            }

            // 罢免军师（只有在有军师时才显示）
            if (this.CurrentFaction.Advisor != null)
            {
                this.AddMenuItem("罢免军师", ContextMenuResult.Person_Appointment_RecallAdvisor);
            }
        }

        private void HandleAdvisorAppointment()
        {
            switch (result)
            {
                case ContextMenuResult.Person_Appointment_AppointAdvisor:
                    // 获取候选人列表（包括当前军师，用于重新任命）
                    PersonList candidates = this.CurrentFaction.AdvisorCandicate;
                    
                    string title = this.CurrentFaction.Advisor != null ? "重新任命军师" : "任命军师";
                    
                    this.ShowTabListInFrame(
                        UndoneWorkKind.Frame, 
                        FrameKind.Person, 
                        FrameFunction.AppointAdvisor, 
                        false, true, true, false, 
                        candidates, 
                        null, 
                        title, 
                        ""
                    );
                    break;

                case ContextMenuResult.Person_Appointment_RecallAdvisor:
                    this.CurrentFaction.AdvisorID = -1;
                    this.CurrentFaction.Advisor = null;
                    break;
            }
        }
    }
}

// ===================================================================
// 使用示例和测试代码
// ===================================================================

public class EnhancedAdvisorExample
{
    public static void TestEnhancedAdvisorSystem()
    {
        Faction faction = Session.Current.Scenario.CurrentFaction;
        
        Console.WriteLine("=== 增强军师系统测试 ===");
        
        // 1. 测试候选人获取
        Console.WriteLine("\n1. 候选人列表测试：");
        PersonList allCandidates = faction.GetAdvisorCandidates(false);
        PersonList excludeCurrentCandidates = faction.GetAdvisorCandidates(true);
        
        Console.WriteLine($"所有候选人数量: {allCandidates.Count}");
        Console.WriteLine($"排除当前军师的候选人数量: {excludeCurrentCandidates.Count}");
        
        // 2. 测试任命可用性
        Console.WriteLine("\n2. 任命可用性测试：");
        Console.WriteLine($"当前军师: {faction.AdvisorName}");
        Console.WriteLine($"可以任命军师: {faction.AppointAdvisorAvail()}");
        Console.WriteLine($"可以更换军师: {faction.CanChangeAdvisor()}");
        
        // 3. 测试推荐候选人
        Console.WriteLine("\n3. 推荐候选人测试：");
        Person recommended = faction.GetRecommendedAdvisor();
        if (recommended != null)
        {
            Console.WriteLine($"推荐候选人: {recommended.Name} (智力: {recommended.Intelligence})");
            
            if (faction.Advisor != null)
            {
                Console.WriteLine($"当前军师智力: {faction.Advisor.Intelligence}");
                Console.WriteLine($"智力提升: {recommended.Intelligence - faction.Advisor.Intelligence}");
            }
        }
        else
        {
            Console.WriteLine("没有合适的候选人");
        }
        
        // 4. 测试重新任命
        if (recommended != null && faction.Advisor != null && 
            recommended.Intelligence > faction.Advisor.Intelligence)
        {
            Console.WriteLine("\n4. 重新任命测试：");
            Console.WriteLine($"准备将军师从 {faction.AdvisorName} 更换为 {recommended.Name}");
            
            // 执行重新任命
            faction.AppointAdvisor(recommended);
            
            Console.WriteLine($"更换后军师: {faction.AdvisorName}");
        }
    }
    
    public static void ShowAdvisorComparisonTable(Faction faction)
    {
        Console.WriteLine("\n=== 军师候选人对比表 ===");
        Console.WriteLine("姓名\t\t智力\t政治\t状态");
        Console.WriteLine("----------------------------------------");
        
        // 显示当前军师
        if (faction.Advisor != null)
        {
            Console.WriteLine($"{faction.Advisor.Name}\t\t{faction.Advisor.Intelligence}\t{faction.Advisor.Politics}\t当前军师");
        }
        
        // 显示候选人
        PersonList candidates = faction.GetAdvisorCandidates(true);
        foreach (Person p in candidates)
        {
            string status = "候选人";
            if (faction.Advisor != null && p.Intelligence > faction.Advisor.Intelligence)
            {
                status = "推荐★";
            }
            
            Console.WriteLine($"{p.Name}\t\t{p.Intelligence}\t{p.Politics}\t{status}");
        }
    }
}