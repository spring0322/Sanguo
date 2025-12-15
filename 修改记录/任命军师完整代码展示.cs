// ===================================================================
// 任命军师完整代码展示 - 修复前后对比
// ===================================================================

// ===================================================================
// 1. ScreenManager.cs - 处理UI交互和任命逻辑
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class ScreenManager
    {
        // 修复前的代码 (有问题)
        private void FrameFunction_Faction_AppointAdvisor_OLD()  //军师 - 修复前
        {
            this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as Person;
            if (this.CurrentPerson != null)
            {
                this.CurrentFaction.AdvisorID = this.CurrentPerson.ID;
                this.CurrentFaction.AppointAdvisor(this.CurrentPerson);
                // ❌ 问题：没有强制刷新advisor缓存
            }
        }

        // 修复后的代码 (正确)
        private void FrameFunction_Faction_AppointAdvisor()  //军师 - 修复后
        {
            this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as Person;
            if (this.CurrentPerson != null)
            {
                // 设置军师ID
                this.CurrentFaction.AdvisorID = this.CurrentPerson.ID;
                
                // 调用任命方法（内部会处理缓存刷新）
                this.CurrentFaction.AppointAdvisor(this.CurrentPerson);
                
                // ✅ 修复：AppointAdvisor方法内部会清空并重新设置advisor缓存
            }
        }

        // 处理FrameFunction调用
        private void HandleFrameFunction(FrameFunction function)
        {
            switch (function)
            {
                case FrameFunction.AppointAdvisor: //任命军师
                    this.FrameFunction_Faction_AppointAdvisor();
                    break;
                // ... 其他case
            }
        }
    }
}

// ===================================================================
// 2. Faction.cs - 核心的军师管理逻辑
// ===================================================================

namespace GameObjects
{
    [DataContract]
    public class Faction : GameObject
    {
        // 私有字段
        private Person advisor = null;
        private int advisorID = -1;

        // 军师ID属性
        [DataMember]
        public int AdvisorID
        {
            get { return this.advisorID; }
            set { this.advisorID = value; }
        }

        // 军师对象属性 - 这里是关键的缓存逻辑
        public Person Advisor
        {
            get
            {
                // 如果缓存为空且有有效ID，则从Scenario中获取
                if (this.advisor == null && this.advisorID != -1 && 
                    Session.Current.Scenario != null && Session.Current.Scenario.Persons != null)
                {
                    this.advisor = Session.Current.Scenario.Persons.GetGameObject(this.AdvisorID) as Person;
                }
                
                return this.advisor;
            }
            set
            {
                // 设置军师时同步更新ID
                if (value != null)
                {
                    this.AdvisorID = value.ID;
                }
                else
                {
                    this.AdvisorID = -1;
                }
                this.advisor = value;
            }
        }

        // 军师姓名属性
        public string AdvisorName
        {
            get
            {
                return (this.Advisor != null) ? this.Advisor.Name : "";
            }
        }

        // 检查是否可以任命军师
        public bool AppointAdvisorAvail()
        {
            if (this.Leader != null && this.Leader.BelongedCaptive == null && this.AdvisorID == -1)
            {
                if (Session.Current.Scenario.IsPlayer(this) && this.AdvisorCandicate.Count > 0)
                {
                    return true;
                }

                if (!Session.Current.Scenario.IsPlayer(this) && this.AIAdvisorCandicate.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        // 军师候选人列表（玩家用）
        public PersonList AdvisorCandicate
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Person p in this.Persons)
                {
                    if (p != this.Leader && p != this.Advisor && p.Available && p.Alive && 
                        p.BelongedCaptive == null && p.LocationTroop == null && p.Intelligence >= 70)
                    {
                        result.Add(p);
                    }
                }
                return result;
            }
        }

        // AI军师候选人列表（按智力排序）
        public PersonList AIAdvisorCandicate
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Person p in this.Persons)
                {
                    if (p != this.Leader && p != this.Advisor && p.Available && p.Alive && 
                        p.BelongedCaptive == null && p.LocationTroop == null && p.Intelligence >= 70)
                    {
                        result.Add(p);
                    }
                }
                // 按智力降序排序
                result.Sort((p1, p2) => p2.Intelligence.CompareTo(p1.Intelligence));
                return result;
            }
        }

        // 修复前的任命军师方法 (有问题)
        public void AppointAdvisor_OLD(Person person)  // 修复前
        {
            Session.Current.Scenario.YearTable.addAppointAdvisorEntry(Session.Current.Scenario.Date, person, this.Leader);
            if (this.OnAppointAdvisor != null)
            {
                this.OnAppointAdvisor(this.Leader, person);
            }
            // ❌ 问题：没有清空advisor缓存，导致Advisor属性仍然返回旧值
        }

        // 修复后的任命军师方法 (正确)
        public void AppointAdvisor(Person person)  // 修复后
        {
            // 记录到年表
            Session.Current.Scenario.YearTable.addAppointAdvisorEntry(Session.Current.Scenario.Date, person, this.Leader);
            
            // ✅ 关键修复：清空advisor缓存并重新设置，确保立即生效
            this.advisor = null;        // 清空缓存
            this.advisor = person;      // 重新设置
            
            // 触发事件
            if (this.OnAppointAdvisor != null)
            {
                this.OnAppointAdvisor(this.Leader, person);
            }
        }

        // 罢免军师方法（新增，可选）
        public void RecallAdvisor()
        {
            if (this.Advisor != null)
            {
                Person formerAdvisor = this.Advisor;
                
                // 清空军师
                this.AdvisorID = -1;
                this.advisor = null;
                
                // 可以添加年表记录和事件触发
                // Session.Current.Scenario.YearTable.addRecallAdvisorEntry(...);
                // if (this.OnRecallAdvisor != null) { ... }
            }
        }

        // AI自动任命军师
        private void AIAppointAdvisor()
        {
            if (!Session.Current.Scenario.IsPlayer(this))
            {
                if (this.AppointAdvisorAvail())
                {
                    Person person = this.AIAdvisorCandicate[0] as Person;
                    this.AdvisorID = person.ID;
                    this.AppointAdvisor(person);
                }
            }
        }

        // 玩家AI辅助任命军师
        private void PlayerAIAppointAdvisor()
        {
            AIAppointAdvisor();
        }

        // 任命军师事件
        public event AppointAdvisorDelegate OnAppointAdvisor;
        public delegate void AppointAdvisorDelegate(Person leader, Person advisor);
    }
}

// ===================================================================
// 3. MGSContextMenu.cs - 右键菜单处理
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class MGSContextMenu
    {
        private void HandleContextMenuResult(ContextMenuResult result)
        {
            switch (result)
            {
                // 任命军师
                case ContextMenuResult.Person_Appointment_AppointAdvisor:
                    this.ShowTabListInFrame(
                        UndoneWorkKind.Frame, 
                        FrameKind.Person, 
                        FrameFunction.AppointAdvisor, 
                        false, true, true, false, 
                        this.CurrentFaction.AdvisorCandicate, 
                        null, 
                        "任命军师", 
                        ""
                    );
                    break;

                // 罢免军师 - 修复前 (有问题)
                case ContextMenuResult.Person_Appointment_RecallAdvisor_OLD:
                    this.CurrentFaction.AdvisorID = -1;
                    // ❌ 问题：没有清空advisor缓存
                    break;

                // 罢免军师 - 修复后 (正确)
                case ContextMenuResult.Person_Appointment_RecallAdvisor:
                    this.CurrentFaction.AdvisorID = -1;
                    this.CurrentFaction.Advisor = null;  // ✅ 清空缓存
                    break;

                // 或者使用新的罢免方法
                case ContextMenuResult.Person_Appointment_RecallAdvisor:
                    this.CurrentFaction.RecallAdvisor();  // ✅ 使用专门的罢免方法
                    break;
            }
        }
    }
}

// ===================================================================
// 4. MGSPersonText.cs - 文本消息和动画处理
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class MGSPersonText
    {
        public override void AppointAdvisor(Person p, Person q)  //军师
        {
            if ((Session.Current.Scenario.IsCurrentPlayer(p.BelongedFaction)) && 
                Session.Current.Scenario.IsCurrentPlayer(q.BelongedFaction))
            {
                q.TextResultString = p.Name;
                p.TextDestinationString = q.BelongedFaction.Name;
                
                // 显示任命军师的文本消息和动画
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    p, p, 
                    TextMessageKind.AppointAdvisor, 
                    "AppointAdvisor", 
                    "AppointAdvisor.jpg", 
                    ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, Session.MainGame.mainGameScreen);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                this.Plugins.GameRecordPlugin.AddBranch(p, "AppointAdvisor", p.Position);
            }
        }
    }
}

// ===================================================================
// 5. FactionList.cs - 事件处理
// ===================================================================

namespace GameObjects
{
    public class FactionList : GameObjectList
    {
        public void AddFactionEventHandlers(Faction faction)
        {
            // 添加军师任命事件处理
            faction.OnAppointAdvisor += new Faction.AppointAdvisorDelegate(this.faction_OnAppointAdvisor);
        }

        private void faction_OnAppointAdvisor(Person leader, Person advisor)
        {
            if (Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.AppointAdvisor(leader, advisor);
            }
        }
    }
}

// ===================================================================
// 6. YearTable.cs - 年表记录
// ===================================================================

namespace GameObjects
{
    public class YearTable
    {
        public void addAppointAdvisorEntry(GameDate date, Person advisor, Person leader)
        {
            this.addTableEntry(
                date, 
                composeFactionList(advisor.BelongedFaction),
                String.Format(
                    yearTableStrings["appointAdvisor"], 
                    advisor.Name, 
                    advisor.BelongedFaction.Name, 
                    leader.Name
                ), 
                false
            );
            
            // 添加到个人传记
            this.addPersonInGameBiography(
                advisor, 
                date,
                String.Format("被{0}任命为{1}的军师", leader.Name, advisor.BelongedFaction.Name)
            );
        }
    }
}

// ===================================================================
// 7. 枚举定义
// ===================================================================

namespace GameGlobal
{
    public enum ContextMenuResult
    {
        // ... 其他枚举值
        Person_Appointment_AppointAdvisor, //任命军师
        Person_Appointment_RecallAdvisor,  //罢免军师
        // ... 其他枚举值
    }

    public enum FrameFunction
    {
        // ... 其他枚举值
        AppointAdvisor, //任命军师
        // ... 其他枚举值
    }

    public enum TextMessageKind
    {
        // ... 其他枚举值
        AppointAdvisor, // 任命军师消息
        // ... 其他枚举值
    }
}

// ===================================================================
// 8. 使用示例和测试代码
// ===================================================================

public class AdvisorAppointmentExample
{
    public static void TestAdvisorAppointment()
    {
        // 获取当前势力
        Faction currentFaction = Session.Current.Scenario.CurrentFaction;
        
        // 检查是否可以任命军师
        if (currentFaction.AppointAdvisorAvail())
        {
            Console.WriteLine("可以任命军师");
            
            // 获取候选人列表
            PersonList candidates = currentFaction.AdvisorCandicate;
            Console.WriteLine($"候选人数量: {candidates.Count}");
            
            if (candidates.Count > 0)
            {
                // 选择智力最高的候选人
                Person bestCandidate = candidates[0];
                foreach (Person p in candidates)
                {
                    if (p.Intelligence > bestCandidate.Intelligence)
                    {
                        bestCandidate = p;
                    }
                }
                
                Console.WriteLine($"选择候选人: {bestCandidate.Name} (智力: {bestCandidate.Intelligence})");
                
                // 任命军师
                currentFaction.AdvisorID = bestCandidate.ID;
                currentFaction.AppointAdvisor(bestCandidate);
                
                // 验证任命结果
                Console.WriteLine($"任命后军师ID: {currentFaction.AdvisorID}");
                Console.WriteLine($"任命后军师对象: {currentFaction.Advisor?.Name}");
                Console.WriteLine($"任命后军师姓名: {currentFaction.AdvisorName}");
                
                // 验证是否成功
                if (currentFaction.Advisor != null && currentFaction.Advisor.ID == bestCandidate.ID)
                {
                    Console.WriteLine("✅ 军师任命成功！");
                }
                else
                {
                    Console.WriteLine("❌ 军师任命失败！");
                }
            }
        }
        else
        {
            Console.WriteLine("当前无法任命军师");
            Console.WriteLine($"当前军师ID: {currentFaction.AdvisorID}");
            Console.WriteLine($"当前军师: {currentFaction.AdvisorName}");
        }
    }
    
    public static void TestAdvisorRecall()
    {
        Faction currentFaction = Session.Current.Scenario.CurrentFaction;
        
        if (currentFaction.Advisor != null)
        {
            Console.WriteLine($"当前军师: {currentFaction.AdvisorName}");
            
            // 罢免军师
            currentFaction.RecallAdvisor();
            
            // 验证罢免结果
            Console.WriteLine($"罢免后军师ID: {currentFaction.AdvisorID}");
            Console.WriteLine($"罢免后军师对象: {currentFaction.Advisor?.Name ?? "null"}");
            
            if (currentFaction.AdvisorID == -1 && currentFaction.Advisor == null)
            {
                Console.WriteLine("✅ 军师罢免成功！");
            }
            else
            {
                Console.WriteLine("❌ 军师罢免失败！");
            }
        }
        else
        {
            Console.WriteLine("当前没有军师可以罢免");
        }
    }
}