using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using PluginInterface;
using GameObjects.ArchitectureDetail;
using GameObjects.FactionDetail;
using GameObjects.TroopDetail;
using GameObjects.SectionDetail;
using GameObjects.PersonDetail;
using GameObjects.MapDetail;
using GameManager;
using WorldOfTheThreeKingdoms.GameManager;

namespace WorldOfTheThreeKingdoms.GameScreens

{
    public class ScreenManager
    {
        public Troop CreatingTroop;
        public Architecture CurrentArchitecture;
        public ArchitectureWorkKind CurrentArchitectureWorkKind;
        public Faction CurrentFaction;
        public GameObject CurrentGameObject;
        public GameObjectList CurrentGameObjects;
        public Military CurrentMilitary;
        public int CurrentNumber;
        public int Currentzijin;
        public Person CurrentPerson;
        public GameObjectList CurrentPersons;
        public GameObjectList CurrentMilitaries; 
        public Routeway CurrentRouteway;
        public Troop CurrentTroop;
        public DiplomaticRelationDisplay CurrentDiplomaticRelationDisplay;
        
        private FrameFunction lastFrameFunction;

        public ScreenManager()
        {
        }

        private void FrameFunction_Architecture_Afterxuanzemeinv() // 纳妃
        {
            // AOT修复: 原 as Person 转换
            if (Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person selectedPerson)
            {
                this.CurrentPerson = selectedPerson;
                Person tookSpouse = Session.Current.Scenario.CurrentFaction.Leader.XuanZeMeiNv(this.CurrentPerson);

                String msgKey;
                if (this.CurrentPerson.Hates(Session.Current.Scenario.CurrentFaction.Leader))
                {
                    msgKey = "nafeiHate";
                }
                else
                {
                    msgKey = "nafei";
                }
                Session.MainGame.mainGameScreen.xianshishijiantupian(this.CurrentPerson, (Session.Current.Scenario.CurrentFaction.Leader).Name, TextMessageKind.TakePrincess, msgKey, "nafei.jpg", "nafei", true);

                if (tookSpouse != null)
                {
                    Session.MainGame.mainGameScreen.PersonBeiDuoqi(tookSpouse, this.CurrentArchitecture.BelongedFaction);
                }
            }
        }

        private void FrameFunction_Architecture_chongxingmeinv() // 宠幸
        {
            // AOT修复: 原 as Person 转换
            if (Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person selectedPerson)
            {
                this.CurrentPerson = selectedPerson;
                Session.Current.Scenario.CurrentFaction.Leader.GoForHouGong(this.CurrentPerson);
                String msgKey;
                TextMessageKind msgKind;
                if (this.CurrentPerson.Hates(Session.Current.Scenario.CurrentFaction.Leader))
                {
                    msgKey = "chongxingHate";
                    msgKind = TextMessageKind.HougongHate;
                }
                else
                {
                    msgKey = "chongxing";
                    msgKind = TextMessageKind.Hougong;
                }
                Session.MainGame.mainGameScreen.xianshishijiantupian(this.CurrentPerson, Session.Current.Scenario.CurrentFaction.Leader.Name, 
                    msgKind, msgKey, this.CurrentPerson.ID.ToString(), "hougong", true);
                //this.mainGameScreen.DateGo(1);
            }
        }

        private void FrameFunction_Architecture_AfterGetBeDisbandedMilitaries() // 解散编队
        {
            this.CurrentGameObjects = this.CurrentArchitecture.Militaries.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                foreach (Military military in this.CurrentGameObjects)
                {
                    this.CurrentArchitecture.DisbandMilitary(military);
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetBeMergedMilitaries() // 合并
        {
            this.CurrentGameObjects = this.CurrentArchitecture.BeMergedMilitaryList.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                foreach (Military military in this.CurrentGameObjects)
                {
                    int increment = (military.Quantity + this.CurrentMilitary.Quantity) - this.CurrentMilitary.Kind.MaxScale;
                    if (increment > 0)
                    {
                        this.CurrentMilitary.BelongedArchitecture.IncreasePopulation(increment);
                    }
                    if (military.LeaderID == this.CurrentMilitary.LeaderID)
                    {
                        this.CurrentMilitary.IncreaseQuantity(military.Quantity, military.Morale, military.Combativity, military.Experience, military.LeaderExperience);
                    }
                    else
                    {
                        this.CurrentMilitary.IncreaseQuantity(military.Quantity, military.Morale, military.Combativity, military.Experience, 0);
                    }
                }
                foreach (Military military in this.CurrentGameObjects)
                {
                    this.CurrentArchitecture.RemoveMilitary(military);
                    this.CurrentArchitecture.BelongedFaction.RemoveMilitary(military);
                    Session.Current.Scenario.Militaries.Remove(military);
                }
            }
        }

        #region 宝物

        /// <summary>
        /// 没收宝物
        /// </summary>
        private void FrameFunction_Architecture_AfterGetConfiscateTreasure()
        {
            this.CurrentGameObject = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as GameObject;
            if (this.CurrentGameObject != null)
            {
                Treasure currentGameObject = (CurrentGameObject is Treasure ? (Treasure)CurrentGameObject : null);
                if (currentGameObject.BelongedPerson != null)
                {
                    currentGameObject.BelongedPerson.ConfiscatedTreasure(currentGameObject);
                    this.CurrentArchitecture.BelongedFaction.Leader.ReceiveTreasure(currentGameObject);
                }
            }
        }

        /// <summary>
        /// 授予宝物-选择宝物
        /// </summary>
        private void FrameFunction_Architecture_AfterGetAwardTreasure()
        {
            this.CurrentGameObject = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as GameObject;
            if (this.CurrentGameObject != null)
            {
                Treasure currentGameObject = (CurrentGameObject is Treasure ? (Treasure)CurrentGameObject : null);
                if (currentGameObject.BelongedPerson != null)
                {
                    Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.GetAwardTreasurePerson, false, true, true, false, this.CurrentArchitecture.BelongedFaction.PersonsInArchitecturesExceptLeader, null, "", "");
                }
            }
        }

        /// <summary>
        /// 授予宝物-选择人物
        /// </summary>
        private void FrameFunction_Architecture_AfterGetAwardTreasurePerson()
        {
            // AOT修复: 原 as Person 转换
            if (Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person selectedPerson)
            {
                this.CurrentPerson = selectedPerson;
                // AOT修复: 原 as Treasure 转换
                if (this.CurrentGameObject is Treasure currentGameObject)
                {
                    if (currentGameObject.BelongedPerson != null)
                    {
                        this.CurrentArchitecture.BelongedFaction.Leader.LoseTreasure(currentGameObject);
                        this.CurrentPerson.AwardedTreasure(currentGameObject);
                    }
                }
            }
        }

        /// <summary>
        /// 出售宝物
        /// 🔥 2026-03-03 修改：添加确认对话框，显示交易信息
        /// ✅ Anti-Band-Aid: 移除防御性空检查，GetSelectedList() 保证返回非空集合
        /// </summary>
        private void FrameFunction_Architecture_AfterGetSellTreasure()
        {
            GameObjectList selectedTreasures = this.CurrentArchitecture.GetTreasureListOfLeader().GetSelectedList();
            if (selectedTreasures.Count > 0)
            {
                Treasure treasure = selectedTreasures[0] as Treasure;
                
                // 显示确认对话框
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.SetSimpleTextDialog(
                    Session.MainGame.mainGameScreen.Plugins.SimpleTextDialogPlugin);
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.ClearFunctions();
                
                // 设置确认回调：执行出售逻辑
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.AddYesFunction(
                    new GameDelegates.VoidFunction(() =>
                    {
                        // 君主失去宝物
                        this.CurrentArchitecture.BelongedFaction.Leader.LoseTreasure(treasure);
                        
                        // 执行出售（进入市场，不隐藏在建筑中）
                        this.CurrentArchitecture.SellTreasure(treasure);
                    }));
                
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                
                // 设置对话框文本（显示宝物名称和获得的资金）
                Session.MainGame.mainGameScreen.Plugins.SimpleTextDialogPlugin.SetGameObjectBranch(
                    treasure, "SellTreasure");
                
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.IsShowing = true;
            }
        }

        /// <summary>
        /// 购买宝物
        /// 🔥 2026-03-03 修改：添加确认对话框，显示交易信息和资金检查
        /// ✅ Anti-Band-Aid: 移除防御性空检查，GetSelectedList() 保证返回非空集合
        /// </summary>
        private void FrameFunction_Architecture_AfterGetBuyTreasure()
        {
            GameObjectList selectedTreasures = Session.Current.Scenario.SoldTreasures.GetSelectedList();
            if (selectedTreasures.Count > 0)
            {
                Treasure treasure = selectedTreasures[0] as Treasure;
                int buyPrice = (int)(treasure.Worth * 1.2f * 1000);
                
                // 检查资金是否充足
                if (this.CurrentArchitecture.Fund >= buyPrice)
                {
                    // 显示确认对话框
                    Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.SetSimpleTextDialog(
                        Session.MainGame.mainGameScreen.Plugins.SimpleTextDialogPlugin);
                    Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.ClearFunctions();
                    
                    // 设置确认回调：执行购买逻辑
                    Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.AddYesFunction(
                        new GameDelegates.VoidFunction(() =>
                        {
                            this.CurrentArchitecture.BuyTreasure(treasure);
                        }));
                    
                    Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                    
                    // 设置对话框文本（显示宝物名称和需要支付的资金）
                    Session.MainGame.mainGameScreen.Plugins.SimpleTextDialogPlugin.SetGameObjectBranch(
                        treasure, "BuyTreasure");
                    
                    Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.IsShowing = true;
                }
                // 资金不足时静默失败（保持简洁，符合现有UI模式）
            }
        }

        #endregion

        private void FrameFunction_Architecture_AfterGetConvinceDestinationPerson() // 说服
        {
            System.Diagnostics.Debug.WriteLine("[说服调试] 进入FrameFunction_Architecture_AfterGetConvinceDestinationPerson");
            
            this.CurrentGameObjects = this.CurrentArchitecture.ConvinceDestinationPersonList.GetSelectedList();
            System.Diagnostics.Debug.WriteLine($"[说服调试] CurrentGameObjects数量: {this.CurrentGameObjects?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[说服调试] CurrentPersons数量: {this.CurrentPersons?.Count ?? 0}");
            
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                // AOT修复: 原 as Person 转换
                if (this.CurrentGameObjects[0] is Person targetPerson)
                {
                    System.Diagnostics.Debug.WriteLine($"[说服调试] 目标人物: {targetPerson?.Name ?? "null"}");
                    
                    // 尝试显示军师对话
                    if (targetPerson != null && this.CurrentPersons != null && this.CurrentPersons.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[说服调试] 条件满足，调用ShowSimpleAdvisorDialogue");
                        ShowSimpleAdvisorDialogue(targetPerson);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[说服调试] 条件不满足，执行原始逻辑");
                        // 原始逻辑：直接执行说服
                        foreach (Person person in this.CurrentPersons)
                        {
                            person.GoForConvince(targetPerson);
                        }
                        Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[说服调试] CurrentGameObjects条件不满足");
            }
        }

        /// <summary>
        /// 显示简单的军师对话 - 参考军师任命对话的实现模式
        /// </summary>
        private void ShowSimpleAdvisorDialogue(Person targetPerson)
        {
            System.Diagnostics.Debug.WriteLine("[军师对话调试] 进入ShowSimpleAdvisorDialogue");
            
            try
            {
                // 获取军师（智力最高的人）
                Person advisor = GetBestAdvisor();
                System.Diagnostics.Debug.WriteLine($"[军师对话调试] 军师: {advisor?.Name ?? "null"}");
                
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[军师对话调试] 没有军师，直接执行说服");
                    // 没有军师，直接执行说服
                    ExecuteConvinceDirectly(targetPerson);
                    return;
                }

                // 计算成功率
                int successRate = CalculateSimpleSuccessRate(targetPerson);
                System.Diagnostics.Debug.WriteLine($"[军师对话调试] 成功率: {successRate}%");
                
                // 获取对话内容
                string dialogue = GetSimpleAdvisorDialogue(targetPerson, successRate);
                System.Diagnostics.Debug.WriteLine($"[军师对话调试] 对话内容: {dialogue}");
                
                System.Diagnostics.Debug.WriteLine("[军师对话调试] 开始设置对话框");
                
                // 1. 设置确认对话框的回调函数
                Session.MainGame.mainGameScreen.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() => {
                        System.Diagnostics.Debug.WriteLine("[军师对话调试] 用户选择'是'，执行说服");
                        // 点击"是"，执行说服
                        ExecuteConvinceDirectly(targetPerson);
                    }),
                    new GameDelegates.VoidFunction(() => {
                        System.Diagnostics.Debug.WriteLine("[军师对话调试] 用户选择'否'，取消说服");
                        // 点击"否"，取消说服
                    })
                );

                // 2. 设置确认对话框位置
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.SetPosition(WorldOfTheThreeKingdoms.GameGlobal.ShowPosition.Center);

                // 3. 显示军师头像和对话 - 使用空的图片和音频避免文件异常
                Session.MainGame.mainGameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    "AdvisorConvince",                 // 分支名称
                    "",                                // 图片 - 使用空字符串避免文件异常
                    "",                                // 声音 - 使用空字符串避免文件异常
                    dialogue                           // 直接显示的文本
                );

                // 4. 显示对话框
                Session.MainGame.mainGameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[军师对话调试] 军师对话框设置完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师对话调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[军师对话调试] 堆栈: {ex.StackTrace}");
                
                // 出错时执行原始逻辑
                ExecuteConvinceDirectly(targetPerson);
            }
        }

        /// <summary>
        /// 获取最佳军师（智力最高的人）
        /// </summary>
        private Person GetBestAdvisor()
        {
            try
            {
                // 首先尝试获取正式的军师
                if (this.CurrentArchitecture?.BelongedFaction?.Advisor != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[军师选择] 使用正式军师: {this.CurrentArchitecture.BelongedFaction.Advisor.Name}");
                    return this.CurrentArchitecture.BelongedFaction.Advisor;
                }

                // 如果没有正式军师，选择智力最高的人（排除君主）
                if (this.CurrentArchitecture?.BelongedFaction?.Persons == null)
                    return null;

                Person bestAdvisor = null;
                Person leader = this.CurrentArchitecture.BelongedFaction.Leader;
                
                foreach (Person person in this.CurrentArchitecture.BelongedFaction.Persons)
                {
                    // 排除君主
                    if (person == leader) continue;
                    
                    if (bestAdvisor == null || person.Intelligence > bestAdvisor.Intelligence)
                    {
                        bestAdvisor = person;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[军师选择] 使用智力最高的人: {bestAdvisor?.Name ?? "null"}");
                return bestAdvisor;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师选择] 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 计算简单的成功率
        /// </summary>
        private int CalculateSimpleSuccessRate(Person targetPerson)
        {
            try
            {
                if (targetPerson == null || this.CurrentPersons == null || this.CurrentPersons.Count == 0)
                    return 50;

                Person executor = this.CurrentPersons[0] as Person;
                if (executor == null) return 50;
                
                // 基础成功率
                int baseRate = 50;
                
                // 执行者魅力影响
                int charismaBonus = (executor.Glamour - 50) / 5;
                
                // 目标忠诚度影响
                int loyaltyPenalty = targetPerson.Loyalty / 2;
                
                int finalRate = baseRate + charismaBonus - loyaltyPenalty;
                
                // 限制在0-100范围内
                return Math.Max(0, Math.Min(100, finalRate));
            }
            catch
            {
                return 50; // 默认成功率
            }
        }

        /// <summary>
        /// 直接执行说服操作
        /// </summary>
        private void ExecuteConvinceDirectly(Person targetPerson)
        {
            try
            {
                foreach (Person person in this.CurrentPersons)
                {
                    person.GoForConvince(targetPerson);
                }
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ExecuteConvinceDirectly] 执行说服失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取简单的军师对话 - 内联实现避免命名空间问题
        /// </summary>
        private string GetSimpleAdvisorDialogue(Person targetPerson, int successRate)
        {
            string baseDialogue;
            
            if (successRate <= 0)
            {
                baseDialogue = "主公，此人忠心耿耿，绝无可能被说服。建议换个目标或等待时机。";
            }
            else if (successRate <= 33)
            {
                baseDialogue = "主公，此人虽有些许不满，但说服成功的机会微乎其微。";
            }
            else if (successRate <= 66)
            {
                baseDialogue = "主公，此人似有动摇之意，说服有一定的成功几率。";
            }
            else if (successRate <= 99)
            {
                baseDialogue = "主公，此人心有不满，说服成功的概率很高。";
            }
            else
            {
                baseDialogue = "主公，此人早有归顺之心，说服必定成功，万无一失！";
            }
            
            if (targetPerson != null)
            {
                return string.Format("关于说服{0}：{1}", targetPerson.Name, baseDialogue);
            }
            
            return baseDialogue;
        }

        private void FrameFunction_Architecture_AfterGetConvinceTargetForAnalysis() // 智能说服目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能说服调试] 进入FrameFunction_Architecture_AfterGetConvinceTargetForAnalysis");
            
            try
            {
                // 获取选择的目标人物
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    Person targetPerson = (selectedItem is Person ? (Person)selectedItem : null);
                    System.Diagnostics.Debug.WriteLine($"[智能说服调试] 选中的目标: {targetPerson?.Name ?? "null"}");
                    
                    if (targetPerson != null)
                    {
                        // 设置当前目标人物
                        Session.MainGame.mainGameScreen.CurrentPerson = targetPerson;
                        
                        System.Diagnostics.Debug.WriteLine("[智能说服调试] 调用MainGameScreen的复杂对话系统");
                        
                        // 调用MainGameScreen中的复杂军师分析和对话系统
                        // 这样既保留了原有的复杂功能，又确保对话能够显示
                        Session.MainGame.mainGameScreen.PerformAdvisorAnalysisAndRecommendation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能说服调试] 选择的不是Person对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能说服调试] 没有选择任何目标");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能说服调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能说服调试] 堆栈: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetDestroyTargetForAnalysis() // 智能破坏目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能破坏调试] 进入FrameFunction_Architecture_AfterGetDestroyTargetForAnalysis");
            
            try
            {
                // 获取选择的目标建筑
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    Architecture targetArchitecture = (selectedItem is Architecture ? (Architecture)selectedItem : null);
                    System.Diagnostics.Debug.WriteLine($"[智能破坏调试] 选中的目标: {targetArchitecture?.Name ?? "null"}");
                    
                    if (targetArchitecture != null)
                    {
                        // 关键修复：先保存当前的源建筑，因为下一行修改CurrentArchitecture可能会影响this.CurrentArchitecture
                        Architecture sourceArchitecture = this.CurrentArchitecture;
                        System.Diagnostics.Debug.WriteLine($"[智能破坏调试] 源建筑: {sourceArchitecture?.Name ?? "null"}, 目标建筑: {targetArchitecture.Name}");

                        // 设置当前目标建筑
                        Session.MainGame.mainGameScreen.CurrentArchitecture = targetArchitecture;
                        
                        // 设置操作类型为破坏
                        Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.Destroy;
                        
                        // 设置目标建筑（持久化）
                        Session.MainGame.mainGameScreen.CurrentTargetArchitecture = targetArchitecture;
                        
                        // 设置执行建筑（使用之前保存的sourceArchitecture）
                        Session.MainGame.mainGameScreen.CurrentSourceArchitecture = sourceArchitecture;
                        
                        System.Diagnostics.Debug.WriteLine("[智能破坏调试] 调用MainGameScreen的破坏分析系统");
                        
                        // 调用MainGameScreen中的破坏军师分析和对话系统
                        Session.MainGame.mainGameScreen.PerformDestroyAnalysisAndRecommendation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能破坏调试] 选择的不是Architecture对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能破坏调试] 没有选择任何目标");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能破坏调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能破坏调试] 堆栈: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetInstigateTargetForAnalysis() // 智能煽动目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能煽动调试] 进入FrameFunction_Architecture_AfterGetInstigateTargetForAnalysis");
            
            try
            {
                // 获取选择的目标建筑
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    Architecture targetArchitecture = (selectedItem is Architecture ? (Architecture)selectedItem : null);
                    System.Diagnostics.Debug.WriteLine($"[智能煽动调试] 选中的目标: {targetArchitecture?.Name ?? "null"}");
                    
                    if (targetArchitecture != null)
                    {
                        // 关键修复：先保存当前的源建筑，因为下一行修改CurrentArchitecture可能会影响this.CurrentArchitecture
                        Architecture sourceArchitecture = this.CurrentArchitecture;
                        System.Diagnostics.Debug.WriteLine($"[智能煽动调试] 源建筑: {sourceArchitecture?.Name ?? "null"}, 目标建筑: {targetArchitecture.Name}");

                        // 设置当前目标建筑
                        Session.MainGame.mainGameScreen.CurrentArchitecture = targetArchitecture;
                        
                        // 设置操作类型为煽动
                        Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.Instigate;
                        
                        // 设置目标建筑（持久化）
                        Session.MainGame.mainGameScreen.CurrentTargetArchitecture = targetArchitecture;
                        
                        // 设置执行建筑（使用之前保存的sourceArchitecture）
                        Session.MainGame.mainGameScreen.CurrentSourceArchitecture = sourceArchitecture;
                        
                        System.Diagnostics.Debug.WriteLine("[智能煽动调试] 调用MainGameScreen的煽动分析系统");
                        
                        // 调用MainGameScreen中的煽动军师分析和对话系统
                        Session.MainGame.mainGameScreen.PerformInstigateAnalysisAndRecommendation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能煽动调试] 选择的不是Architecture对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能煽动调试] 没有选择任何目标");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能煽动调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能煽动调试] 堆栈: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetGossipTargetForAnalysis() // 智能流言目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能流言调试] 进入FrameFunction_Architecture_AfterGetGossipTargetForAnalysis");
            
            try
            {
                // 获取选择的目标建筑
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    Architecture targetArchitecture = (selectedItem is Architecture ? (Architecture)selectedItem : null);
                    System.Diagnostics.Debug.WriteLine($"[智能流言调试] 选中的目标: {targetArchitecture?.Name ?? "null"}");
                    
                    if (targetArchitecture != null)
                    {
                        // 关键修复：先保存当前的源建筑
                        Architecture sourceArchitecture = this.CurrentArchitecture;
                        System.Diagnostics.Debug.WriteLine($"[智能流言调试] 源建筑: {sourceArchitecture?.Name ?? "null"}, 目标建筑: {targetArchitecture.Name}");

                        // 设置当前目标建筑
                        Session.MainGame.mainGameScreen.CurrentArchitecture = targetArchitecture;
                        
                        // 设置操作类型为流言
                        Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.Gossip;
                        
                        // 设置目标建筑（持久化）
                        Session.MainGame.mainGameScreen.CurrentTargetArchitecture = targetArchitecture;
                        
                        // 设置执行建筑（使用之前保存的sourceArchitecture）
                        Session.MainGame.mainGameScreen.CurrentSourceArchitecture = sourceArchitecture;
                        
                        System.Diagnostics.Debug.WriteLine("[智能流言调试] 调用MainGameScreen的流言分析系统");
                        
                        // 调用MainGameScreen中的流言军师分析和对话系统
                        Session.MainGame.mainGameScreen.PerformGossipAnalysisAndRecommendation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能流言调试] 选择的不是Architecture对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能流言调试] 没有选择任何目标");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能流言调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能流言调试] 堆栈: {ex.StackTrace}");
            }
        }


        private void FrameFunction_Architecture_AfterGetJailBreakTargetForAnalysis() // 智能劫牢目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能劫牢调试] 进入FrameFunction_Architecture_AfterGetJailBreakTargetForAnalysis");
            
            try
            {
                // 获取选择的目标建筑
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    Architecture targetArchitecture = (selectedItem is Architecture ? (Architecture)selectedItem : null);
                    System.Diagnostics.Debug.WriteLine($"[智能劫牢调试] 选中的目标: {targetArchitecture?.Name ?? "null"}");
                    
                    if (targetArchitecture != null)
                    {
                        // 关键修复：先保存当前的源建筑
                        Architecture sourceArchitecture = this.CurrentArchitecture;
                        System.Diagnostics.Debug.WriteLine($"[智能劫牢调试] 源建筑: {sourceArchitecture?.Name ?? "null"}, 目标建筑: {targetArchitecture.Name}");

                        // 设置当前目标建筑
                        Session.MainGame.mainGameScreen.CurrentArchitecture = targetArchitecture;
                        
                        // 设置操作类型为劫牢
                        Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.JailBreak;
                        
                        // 设置目标建筑（持久化）
                        Session.MainGame.mainGameScreen.CurrentTargetArchitecture = targetArchitecture;
                        
                        // 设置执行建筑（使用之前保存的sourceArchitecture）
                        Session.MainGame.mainGameScreen.CurrentSourceArchitecture = sourceArchitecture;
                        
                        System.Diagnostics.Debug.WriteLine("[智能劫牢调试] 调用MainGameScreen的劫牢分析系统");
                        
                        // 调用MainGameScreen中的劫牢军师分析和对话系统
                        Session.MainGame.mainGameScreen.PerformJailBreakAnalysisAndRecommendation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能劫牢调试] 选择的不是Architecture对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能劫牢调试] 没有选择任何目标");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能劫牢调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能劫牢调试] 堆栈: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetAssassinateTargetForAnalysis() // 智能暗杀目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能暗杀调试] 进入FrameFunction_Architecture_AfterGetAssassinateTargetForAnalysis");
            
            try
            {
                // 获取选择的目标人物
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    Person targetPerson = (selectedItem is Person ? (Person)selectedItem : null);
                    System.Diagnostics.Debug.WriteLine($"[智能暗杀调试] 选中的目标: {targetPerson?.Name ?? "null"}");
                    
                    if (targetPerson != null)
                    {
                        // 关键：保存当前的源建筑
                        Architecture sourceArchitecture = this.CurrentArchitecture;
                        System.Diagnostics.Debug.WriteLine($"[智能暗杀调试] 源建筑: {sourceArchitecture?.Name ?? "null"}, 目标人物: {targetPerson.Name}");

                        // 设置操作类型为暗杀
                        Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.Assassinate;
                        
                        // 设置目标人物
                        Session.MainGame.mainGameScreen.CurrentPerson = targetPerson;
                        
                        // 设置执行建筑
                        Session.MainGame.mainGameScreen.CurrentSourceArchitecture = sourceArchitecture;
                        
                        System.Diagnostics.Debug.WriteLine("[智能暗杀调试] 调用MainGameScreen的暗杀分析系统");
                        
                        // 调用MainGameScreen中的暗杀军师分析和对话系统
                        Session.MainGame.mainGameScreen.PerformAssassinateAnalysisAndRecommendation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能暗杀调试] 选择的不是Person对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能暗杀调试] 没有选择任何目标");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能暗杀调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能暗杀调试] 堆栈: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelationTargetForAnalysis() // 智能亲善目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能亲善调试] 进入FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelationTargetForAnalysis");
            
            try
            {
                // 获取选择的外交关系目标
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    DiplomaticRelationDisplay relationDisplay = selectedItem as DiplomaticRelationDisplay;
                    System.Diagnostics.Debug.WriteLine($"[智能亲善调试] 选中的目标: {relationDisplay?.FactionName ?? "null"}");
                    
                    if (relationDisplay != null)
                    {
                        // 保存选择的外交关系
                        this.CurrentDiplomaticRelationDisplay = relationDisplay;
                        
                        // 接下来显示代价选择（金钱或宝物）
                        // 构建代价选项列表
                        GameObjectList costList = new GameObjectList();
                        
                        // 添加金钱选项（用一个特殊的Treasure对象表示）
                        Treasure goldOption = new Treasure();
                        goldOption.ID = -1000;
                        goldOption.Name = "10000金钱";
                        goldOption.Worth = 10000;
                        costList.Add(goldOption);
                        
                        // 添加君主拥有的宝物
                        if (this.CurrentArchitecture?.BelongedFaction?.Leader != null)
                        {
                            foreach (Treasure t in this.CurrentArchitecture.BelongedFaction.Leader.Treasures)
                            {
                                costList.Add(t);
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[智能亲善调试] 显示代价选择，共 {costList.Count} 个选项");
                        
                        // 显示代价选择Frame
                        Session.MainGame.mainGameScreen.ShowTabListInFrame(
                            UndoneWorkKind.Frame,
                            FrameKind.Treasure,
                            FrameFunction.GetEnhanceDiplomaticRelationCost,
                            false, true, true, false,
                            costList,
                            null,
                            "选择赠送代价",
                            ""
                        );
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能亲善调试] 选择的不是DiplomaticRelationDisplay对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能亲善调试] 没有选择任何目标");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能亲善调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能亲善调试] 堆栈: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelationCost() // 智能亲善代价选择
        {
            System.Diagnostics.Debug.WriteLine("[智能亲善调试] 进入FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelationCost");
            
            try
            {
                // 获取选择的代价
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    Treasure cost = (selectedItem is Treasure ? (Treasure)selectedItem : null);
                    System.Diagnostics.Debug.WriteLine($"[智能亲善调试] 选中的代价: {cost?.Name ?? "null"}");
                    
                    if (cost != null)
                    {
                        // 保存选择的代价
                        // ID == -1000 表示金钱选项
                        if (cost.ID == -1000)
                        {
                            Session.MainGame.mainGameScreen.CurrentDiplomaticCost = null; // null表示使用金钱
                        }
                        else
                        {
                            Session.MainGame.mainGameScreen.CurrentDiplomaticCost = cost;
                        }
                        
                        System.Diagnostics.Debug.WriteLine("[智能亲善调试] 调用MainGameScreen的亲善分析系统");
                        
                        // 调用MainGameScreen中的亲善军师分析和对话系统
                        Session.MainGame.mainGameScreen.PerformEnhanceDiplomaticAnalysisAndRecommendation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能亲善调试] 选择的不是Treasure对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能亲善调试] 没有选择任何代价");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能亲善调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能亲善调试] 堆栈: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetTruceDiplomaticRelationTargetForAnalysis() // 智能停战目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能停战调试] 进入FrameFunction_Architecture_AfterGetTruceDiplomaticRelationTargetForAnalysis");
            
            try
            {
                // 获取选择的势力目标
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    Faction targetFaction = (selectedItem is Faction ? (Faction)selectedItem : null);
                    System.Diagnostics.Debug.WriteLine($"[智能停战调试] 选中的目标势力: {targetFaction?.Name ?? "null"}");
                    
                    if (targetFaction != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[智能停战调试] 调用MainGameScreen的停战分析系统");
                        
                        // 调用MainGameScreen中的停战军师分析和对话系统
                        Session.MainGame.mainGameScreen.PerformTruceDiplomaticAnalysisAndRecommendation(targetFaction);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能停战调试] 选择的不是Faction对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能停战调试] 没有选择任何目标");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能停战调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能停战调试] 堆栈: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetInduceSurrenderTargetForAnalysis() // 智能劝降目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能劝降调试] 进入FrameFunction_Architecture_AfterGetInduceSurrenderTargetForAnalysis");
            
            try
            {
                // 获取选择的外交关系目标
                var selectedItem = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem;
                if (selectedItem != null)
                {
                    DiplomaticRelationDisplay relationDisplay = selectedItem as DiplomaticRelationDisplay;
                    System.Diagnostics.Debug.WriteLine($"[智能劝降调试] 选中的目标: {relationDisplay?.FactionName ?? "null"}");
                    
                    if (relationDisplay != null)
                    {
                        // 保存选择的外交关系
                        this.CurrentDiplomaticRelationDisplay = relationDisplay;
                        
                        System.Diagnostics.Debug.WriteLine("[智能劝降调试] 调用MainGameScreen的劝降分析系统");
                        
                        // 调用MainGameScreen中的劝降军师分析和对话系统
                        Session.MainGame.mainGameScreen.PerformInduceSurrenderAnalysisAndRecommendation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能劝降调试] 选择的不是DiplomaticRelationDisplay对象");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能劝降调试] 没有选择任何目标");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能劝降调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能劝降调试] 堆栈: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetInduceSurrenderPerson() // 智能劝降执行人选择
        {
            System.Diagnostics.Debug.WriteLine("[智能劝降调试] 进入FrameFunction_Architecture_AfterGetInduceSurrenderPerson");
            
            try
            {
                var selectedList = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItemList as GameObjectList;
                if (selectedList != null && selectedList.Count == 1)
                {
                    Person executor = selectedList[0] as Person;
                    if (executor != null)
                    {
                        var relationDisplay = Session.MainGame.mainGameScreen.CurrentInduceSurrenderTarget;
                        if (relationDisplay != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[智能劝降调试] 执行人: {executor.Name}, 目标: {relationDisplay.FactionName}");
                            
                            // 检查资金并执行劝降逻辑
                            if (Session.MainGame.mainGameScreen.CurrentArchitecture != null &&
                                Session.MainGame.mainGameScreen.CurrentArchitecture.Fund >= 50000)
                            {
                                Session.MainGame.mainGameScreen.CurrentArchitecture.Fund -= 50000;
                                executor.GoToQuanXiangDiplomatic(relationDisplay);
                                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                            }
                            
                            // 清理状态
                            Session.MainGame.mainGameScreen.CurrentInduceSurrenderTarget = null;
                            Session.MainGame.mainGameScreen.CurrentRecommendedPersonForInduceSurrender = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能劝降调试] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[智能劝降调试] 堆栈: {ex.StackTrace}");
            }
        }


        private void FrameFunction_Architecture_AfterGetYearlyTalentRecommendation() // 年度人才举荐选择
        {
            try
            {
                var selectedList = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItemList as GameObjectList;
                if (selectedList != null && selectedList.Count == 1)
                {
                    Person selectedTalent = selectedList[0] as Person;
                    if (selectedTalent != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ScreenManager] 玩家选择了人才: {selectedTalent.Name}");
                        // 回调管理器
                        WorldOfTheThreeKingdoms.GameManager.YearlyRecommendationManager.Instance.HandleTalentSelection(
                            WorldOfTheThreeKingdoms.GameManager.YearlyRecommendationManager.Instance.CurrentFaction, 
                            selectedTalent
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScreenManager] 处理人才选择异常: {ex.Message}");
            }
        }

        private void FrameFunction_Architecture_AfterGetConvinceSourcePerson() // 说服
        {
            this.CurrentGameObjects = this.CurrentArchitecture.PersonsExcludeNvGuan.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                this.CurrentPersons = this.CurrentGameObjects.GetList();
                Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.ConvincePersonPosition));
            }
        }

        private void FrameFunction_Architecture_AfterPersonManualHire() // 智能说服/破坏 - 执行人员选择完成
        {
            System.Diagnostics.Debug.WriteLine("[FrameFunction_Architecture_AfterPersonManualHire] 执行人员选择完成");
            
            try
            {
                // 确定从哪个建筑获取人员列表
                Architecture listOwner = this.CurrentArchitecture;
                var opType = Session.MainGame.mainGameScreen.CurrentOperationType;
                if ((opType == MainGameScreen.IntelligentOperationType.Destroy || 
                     opType == MainGameScreen.IntelligentOperationType.Instigate || 
                     opType == MainGameScreen.IntelligentOperationType.Convince ||
                     opType == MainGameScreen.IntelligentOperationType.Gossip ||
                     opType == MainGameScreen.IntelligentOperationType.JailBreak ||
                     opType == MainGameScreen.IntelligentOperationType.Assassinate) && 
                    Session.MainGame.mainGameScreen.CurrentSourceArchitecture != null)
                {
                    listOwner = Session.MainGame.mainGameScreen.CurrentSourceArchitecture;
                    System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 智能操作模式，从源建筑获取人员: {listOwner.Name}");
                }

                // 安全检查
                if (listOwner == null || listOwner.PersonsExcludeNvGuan == null)
                {
                    System.Diagnostics.Debug.WriteLine("[FrameFunction_Architecture_AfterPersonManualHire] listOwner 或 PersonsExcludeNvGuan 为 null，退出");
                    return;
                }
                
                this.CurrentGameObjects = listOwner.PersonsExcludeNvGuan.GetSelectedList();
                if (this.CurrentGameObjects != null && this.CurrentGameObjects.Count > 0)
                {
                    this.CurrentPersons = this.CurrentGameObjects.GetList();
                    Person executor = this.CurrentPersons[0] as Person;
                    
                    System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 选择的执行人员: {executor?.Name ?? "null"}");
                    
                    Person targetPerson = Session.MainGame.mainGameScreen.CurrentPerson;
                    Architecture targetArchitecture = Session.MainGame.mainGameScreen.CurrentTargetArchitecture;
                    Architecture sourceArchitecture = Session.MainGame.mainGameScreen.CurrentSourceArchitecture;
                    var operationType = Session.MainGame.mainGameScreen.CurrentOperationType;
                    
                    bool isConvinceOperation = (targetPerson != null && operationType == MainGameScreen.IntelligentOperationType.Convince);
                    bool isDestroyOperation = (targetArchitecture != null && sourceArchitecture != null && targetArchitecture != sourceArchitecture && operationType == MainGameScreen.IntelligentOperationType.Destroy);
                    bool isInstigateOperation = (targetArchitecture != null && sourceArchitecture != null && targetArchitecture != sourceArchitecture && operationType == MainGameScreen.IntelligentOperationType.Instigate);
                    
                    if (executor != null)
                    {
                        if (isConvinceOperation)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 开始执行说服: {executor.Name} → {targetPerson.Name}");
                            try
                            {
                                executor.GoForConvince(targetPerson);
                                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                                Session.MainGame.mainGameScreen.CurrentPerson = null;
                                Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.None;
                                Session.MainGame.mainGameScreen.CurrentSourceArchitecture = null;
                                Session.MainGame.mainGameScreen.CurrentTargetArchitecture = null;
                                this.CurrentGameObjects = null;
                                this.CurrentPersons = null;
                                return; 
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                        }
                        else if (isDestroyOperation)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 开始执行破坏: {executor.Name} → {targetArchitecture.Name}");
                            try
                            {
                                executor.GoForDestroy(targetArchitecture.Position);
                                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                                Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.None;
                                Session.MainGame.mainGameScreen.CurrentSourceArchitecture = null;
                                this.CurrentGameObjects = null;
                                this.CurrentPersons = null;
                                return;
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                        }
                        else if (isInstigateOperation)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 开始执行煽动: {executor.Name} → {targetArchitecture.Name}");
                            try
                            {
                                executor.GoForInstigate(targetArchitecture.Position);
                                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                                Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.None;
                                Session.MainGame.mainGameScreen.CurrentSourceArchitecture = null;
                                this.CurrentGameObjects = null;
                                this.CurrentPersons = null;
                                return;
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                        }
                        else if (operationType == MainGameScreen.IntelligentOperationType.Gossip && targetArchitecture != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 开始执行流言: {executor.Name} → {targetArchitecture.Name}");
                            try
                            {
                                executor.GoForGossip(targetArchitecture.Position);
                                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                            finally
                            {
                                Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.None;
                                Session.MainGame.mainGameScreen.CurrentSourceArchitecture = null;
                                this.CurrentGameObjects = null;
                                this.CurrentPersons = null;
                            }
                            return;
                        }
                        else if (operationType == MainGameScreen.IntelligentOperationType.JailBreak && targetArchitecture != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 开始执行劫牢: {executor.Name} → {targetArchitecture.Name}");
                            try
                            {
                                executor.GoForJailBreak(targetArchitecture.Position);
                                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                            finally
                            {
                                Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.None;
                                Session.MainGame.mainGameScreen.CurrentSourceArchitecture = null;
                                this.CurrentGameObjects = null;
                                this.CurrentPersons = null;
                            }
                            return;
                        }
                        else if (operationType == MainGameScreen.IntelligentOperationType.Assassinate && targetPerson != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 开始执行暗杀: {executor.Name} → {targetPerson.Name}");
                            try
                            {
                                // 关键：设置目标位置（目标人物所在建筑的位置）
                                if (targetPerson.BelongedArchitecture != null)
                                {
                                    executor.OutsideDestination = targetPerson.BelongedArchitecture.Position;
                                    System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 设置OutsideDestination: {executor.OutsideDestination}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("[FrameFunction_Architecture_AfterPersonManualHire] 目标人物无归属建筑，无法执行暗杀");
                                    return;
                                }
                                
                                executor.GoForAssassinate(targetPerson);
                                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                            }
                            catch (Exception ex) 
                            { 
                                System.Diagnostics.Debug.WriteLine($"[暗杀执行错误] {ex.Message}");
                                System.Diagnostics.Debug.WriteLine($"[暗杀执行堆栈] {ex.StackTrace}");
                            }
                            finally
                            {
                                Session.MainGame.mainGameScreen.CurrentPerson = null;
                                Session.MainGame.mainGameScreen.CurrentOperationType = MainGameScreen.IntelligentOperationType.None;
                                Session.MainGame.mainGameScreen.CurrentSourceArchitecture = null;
                                this.CurrentGameObjects = null;
                                this.CurrentPersons = null;
                            }
                            return;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine("[FrameFunction_Architecture_AfterPersonManualHire] 非智能系统操作，继续原始流程");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[FrameFunction_Architecture_AfterPersonManualHire] 没有选择执行人员");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[FrameFunction_Architecture_AfterPersonManualHire] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        private void FrameFunction_Architecture_AfterGetDestroyPerson() // 破坏
        {
            this.CurrentGameObjects = this.CurrentArchitecture.MovablePersons .GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                this.CurrentPersons = this.CurrentGameObjects.GetList();
                Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.DestroyPosition));
            }
        }

        private void FrameFunction_Architecture_AfterGetFacilityToBuild() // 建设设施
        {
            this.CurrentGameObjects = this.CurrentArchitecture.BuildableFacilityKindList.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                FacilityKind facilityKind = this.CurrentGameObjects[0] as FacilityKind;
                this.CurrentArchitecture.BeginToBuildAFacility(facilityKind);
            }
        }

        private void FrameFunction_Architecture_AfterGetFacilityToDemolish() // 拆除设施
        {
            this.CurrentGameObjects = this.CurrentArchitecture.Facilities.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                foreach (Facility facility in this.CurrentGameObjects)
                {
                    this.CurrentArchitecture.DemolishFacility(facility);
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetFriendlyDiplomaticRelation()
        {
            GameObjectList selectedList = this.CurrentArchitecture.ResetDiplomaticRelationList.GetSelectedList();
            if (selectedList != null)
            {
                foreach (DiplomaticRelationDisplay display in selectedList)
                {
                    Session.MainGame.mainGameScreen.xianshishijiantupian(Session.Current.Scenario.NeutralPerson, this.CurrentArchitecture.BelongedFaction.Leader.Name, "ResetDiplomaticRelation", "ResetDiplomaticRelation.jpg", "ResetDiplomaticRelation", display.FactionName, true);
                    this.CurrentArchitecture.BelongedFaction.Leader.DecreaseKarma(5);
                    display.Relation = 0;
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelation()
        {
            GameObjectList selectedList = this.CurrentArchitecture.EnhanceDiplomaticRelationList.GetSelectedList();

            if (selectedList != null && (selectedList.Count == 1))
            {
                this.CurrentDiplomaticRelationDisplay = selectedList[0] as DiplomaticRelationDisplay;
                Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.GetEnhanceDiplomaticRelationPerson, true, true, true, true, this.CurrentArchitecture.PersonsExcludeNvGuan, null, "外交人员", "Ability");
            }
        }

        private void FrameFunction_Architecture_AfterGetTruceDiplomaticRelation()
        {
            GameObjectList selectedList = this.CurrentArchitecture.TruceDiplomaticRelationList.GetSelectedList();

            if (selectedList != null && (selectedList.Count == 1))
            {
                this.CurrentDiplomaticRelationDisplay = selectedList[0] as DiplomaticRelationDisplay;
                Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.GetTruceDiplomaticRelationPerson, true, true, true, true, this.CurrentArchitecture.PersonsExcludeNvGuan, null, "外交人员", "Ability");
            }
        }

        private void FrameFunction_Architecture_AfterGetQuanXiangDiplomaticRelation() //劝降
        {
            GameObjectList selectedList = this.CurrentArchitecture.QuanXiangDiplomaticRelationList.GetSelectedList();

            if (selectedList != null && (selectedList.Count == 1))
            {
                this.CurrentDiplomaticRelationDisplay = selectedList[0] as DiplomaticRelationDisplay;
                Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.GetQuanXiangDiplomaticRelationPerson, false, true, true, false, this.CurrentArchitecture.PersonsExcludeNvGuan, null, "外交人员", "Ability");
            }
        }

        private void FrameFunction_Architecture_AfterGetQuanXiangDiplomaticRelationPerson() //劝降
        {
            GameObjectList selectedList = this.CurrentArchitecture.DiplomaticWorkingPersons.GetSelectedList();

            if (selectedList != null && (selectedList.Count == 1))
            {

                Person diplomaticperson = selectedList[0] as Person;
                if (this.CurrentArchitecture.Fund >= 50000)
                {
                    this.CurrentArchitecture.Fund -= 50000;
                    diplomaticperson.GoToQuanXiangDiplomatic(this.CurrentDiplomaticRelationDisplay);
                }    
                
            }
        }

        private void FrameFunction_Architecture_AfterGetAllyDiplomaticRelationTargetForAnalysis() // 智能结盟目标分析
        {
            System.Diagnostics.Debug.WriteLine("[智能结盟调试] 进入FrameFunction_Architecture_AfterGetAllyDiplomaticRelationTargetForAnalysis");
            
            try
            {
                GameObjectList selectedList = this.CurrentArchitecture.AllyDiplomaticRelationList.GetSelectedList();

                if (selectedList != null && selectedList.Count == 1)
                {
                    this.CurrentDiplomaticRelationDisplay = selectedList[0] as DiplomaticRelationDisplay;
                    
                    if (this.CurrentDiplomaticRelationDisplay != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[智能结盟调试] 选择了目标势力: {this.CurrentDiplomaticRelationDisplay.FactionName}");
                        
                        // 调用MainGameScreen中的结盟军师分析和对话系统
                        Session.MainGame.mainGameScreen.PerformAllyDiplomaticAnalysisAndRecommendation();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[智能结盟调试] CurrentDiplomaticRelationDisplay 为空");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[智能结盟调试] 未选择有效的外交关系");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[智能结盟调试] 异常: {ex.Message}");
            }
        }

        private void FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelationPerson() //亲善
        {
            GameObjectList selectedList = this.CurrentArchitecture.DiplomaticWorkingPersons.GetSelectedList();

            if (selectedList != null)
            {
                // 获取当前选择的代价
                Treasure cost = Session.MainGame.mainGameScreen.CurrentDiplomaticCost;

                foreach (Person diplomaticperson in selectedList)
                {
                    if (cost == null) // 金钱
                    {
                        if (this.CurrentArchitecture.Fund >= 10000)
                        {
                            this.CurrentArchitecture.Fund -= 10000;
                            diplomaticperson.DiplomaticGiftTreasure = null; // 确保清空
                            diplomaticperson.GoToDiplomatic(this.CurrentDiplomaticRelationDisplay);
                        }
                    }
                    else // 宝物
                    {
                        // 检查宝物是否仍在君主身上
                        if (this.CurrentArchitecture.BelongedFaction.Leader != null && 
                            this.CurrentArchitecture.BelongedFaction.Leader.Treasures.HasGameObject(cost))
                        {
                            // 将宝物暂时转移到执行武将身上（通过属性引用）
                            // 实际所有权转移在出发前做吗？
                            // Person.GoToDiplomatic -> ... -> DoEnhanceDiplomatic (执行时)
                            // DoEnhanceDiplomatic 会将 DiplomaticGiftTreasure 从 CurrentPerson 转移到 目标君主
                            // 但现在 Treasure 还属于 Faction Leader
                            // 我们应该先从 Leader 身上移除，或者标记？
                            
                            // 简单做法：从 Leader 身上移除，给 DiplomaticPerson
                            this.CurrentArchitecture.BelongedFaction.Leader.Treasures.Remove(cost);
                            cost.BelongedPerson = diplomaticperson; // 赋予执行人
                            diplomaticperson.Treasures.Add(cost); // 放入执行人行囊
                            
                            diplomaticperson.DiplomaticGiftTreasure = cost; // 设置标记
                            
                            diplomaticperson.GoToDiplomatic(this.CurrentDiplomaticRelationDisplay);
                            
                            // 此时宝物随人走
                            // 如果任务中途失败/返回，宝物应还在人身上，或者返回给君主？
                            // 现有逻辑：Person取消任务时，Attributes kept.
                        }
                    }
                }
            }
            // 清理
            Session.MainGame.mainGameScreen.CurrentDiplomaticCost = null;
        }

        private void FrameFunction_Architecture_AfterGetTruceDiplomaticRelationPerson()
        {
            GameObjectList selectedList = this.CurrentArchitecture.DiplomaticWorkingPersons.GetSelectedList();

            if (selectedList != null)
            {
                foreach (Person diplomaticperson in selectedList)
                {
                    if (this.CurrentArchitecture.Fund >= 50000)
                    {
                        // 🤖 使用AI命令系统安全执行停战外交
                        try
                        {
                            // 获取目标势力
                            Faction targetFaction = null;
                            if (this.CurrentDiplomaticRelationDisplay != null)
                            {
                                targetFaction = this.CurrentDiplomaticRelationDisplay.LinkedFaction1 == this.CurrentArchitecture.BelongedFaction 
                                    ? this.CurrentDiplomaticRelationDisplay.LinkedFaction2 
                                    : this.CurrentDiplomaticRelationDisplay.LinkedFaction1;
                            }

                            if (targetFaction != null)
                            {
                                // 通过AI命令系统执行外交行动
                                WorldOfTheThreeKingdoms.GameManager.AISystemIntegrator.RequestDiplomaticAction(
                                    this.CurrentArchitecture,
                                    targetFaction,
                                    "停战",
                                    diplomaticperson,
                                    "玩家停战外交"
                                );
                                
                                System.Diagnostics.Debug.WriteLine($"[ScreenManager] 停战外交命令已提交: {diplomaticperson.Name} -> {targetFaction.Name}");
                            }
                            else
                            {
                                // 回退到原有逻辑
                                this.CurrentArchitecture.Fund -= 50000;
                                diplomaticperson.GoToTruceDiplomatic(this.CurrentDiplomaticRelationDisplay);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ScreenManager] AI停战外交执行失败，回退到原有逻辑: {ex.Message}");
                            // 回退到原有逻辑
                            this.CurrentArchitecture.Fund -= 50000;
                            diplomaticperson.GoToTruceDiplomatic(this.CurrentDiplomaticRelationDisplay);
                        }
                    }
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetAllyDiplomaticRelationPerson()
        {
            GameObjectList selectedList = this.CurrentArchitecture.DiplomaticWorkingPersons.GetSelectedList();

            if (selectedList != null)
            {
                foreach (Person diplomaticperson in selectedList)
                {
                    if (this.CurrentArchitecture.Fund >= 20000)
                    {
                        this.CurrentArchitecture.Fund -= 20000;
                        diplomaticperson.GoToAllyDiplomatic(this.CurrentDiplomaticRelationDisplay);
                    }
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetDenounceDiplomaticRelation()
        {
            GameObjectList selectedList = this.CurrentArchitecture.DenounceDiplomaticRelationList.GetSelectedList();

            if (selectedList != null)
            {
                foreach (DiplomaticRelationDisplay display in selectedList)
                {
                    if (this.CurrentArchitecture.Fund >= 120000)
                    {
                        Faction toEncircle = display.LinkedFaction1 == this.CurrentArchitecture.BelongedFaction ? display.LinkedFaction2 : display.LinkedFaction1;
                        this.CurrentArchitecture.BelongedFaction.Encircle(this.CurrentArchitecture, toEncircle);
                    }
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetGossipPerson()
        {
            this.CurrentGameObjects = this.CurrentArchitecture.MovablePersons .GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                this.CurrentPersons = this.CurrentGameObjects.GetList();
                Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.GossipPosition));
            }
        }

        private void FrameFunction_Architecture_AfterGetJailBreakPerson()
        {
            this.CurrentGameObjects = this.CurrentArchitecture.MovablePersons .GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                this.CurrentPersons = this.CurrentGameObjects.GetList();
                Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.JailBreakPosition));
            }
        }

        private void FrameFunction_Architecture_AfterGetAssassinatePerson()
        {
            this.CurrentGameObjects = this.CurrentArchitecture.Persons.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                this.CurrentPersons = this.CurrentGameObjects.GetList();
                Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.AssassinatePosition));
            }
        }

        private void FrameFunction_Architecture_AfterGetAssassinatePersonTarget()
        {
            this.CurrentGameObjects = this.CurrentArchitecture.AssassinatablePersons((this.CurrentPersons[0] as Person).BelongedFaction).GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                foreach (Person person in this.CurrentPersons)
                {
                    person.GoForAssassinate(this.CurrentGameObjects[0] as Person);
                }
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        private void FrameFunction_Architecture_AfterGetInformationToStop()
        {
            this.CurrentGameObjects = this.CurrentArchitecture.Informations.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                foreach (Information i in this.CurrentGameObjects)
                {
                    i.Purify();
                    this.CurrentArchitecture.RemoveInformation(i);
                    Session.Current.Scenario.Informations.Remove(i);
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetOfficerType()
        {

            this.CurrentGameObjects = this.CurrentArchitecture.AvailGeneratorTypeList().GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {

                //this.CurrentGameObject = Session.Current.Scenario.GameCommonData.PlayerGeneratorTypes.GetSelectedList()[0] as PersonGeneratorType;
                PersonGeneratorType preferredType = this.CurrentArchitecture.AvailGeneratorTypeList().GetSelectedList()[0] as PersonGeneratorType;
                this.CurrentArchitecture.DoZhaoXian(preferredType);
                //this.CurrentArchitecture.DecreaseFund(preferredType.CostFund);
            }
        }

        private void FrameFunction_Architecture_AfterGetInformationKind()
        {
            Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Work, FrameFunction.GetInformationPerson, false, true, true, false, this.CurrentArchitecture.MovablePersons, null, "情报", "情报");
        }

        private void FrameFunction_Architecture_AfterGetInformationPerson() // 情报
        {
            this.CurrentGameObjects = this.CurrentArchitecture.MovablePersons .GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                this.CurrentPerson = this.CurrentGameObjects[0] as Person;
                this.CurrentPerson.CurrentInformationKind = Session.Current.Scenario.GameCommonData.AllInformationKinds.GetSelectedList()[0] as InformationKind;
                Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.InformationPosition));
            }
        }

        private void FrameFunction_Architecture_AfterGetInstigatePerson()
        {
            this.CurrentGameObjects = this.CurrentArchitecture.MovablePersons .GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                this.CurrentPersons = this.CurrentGameObjects.GetList();
                Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.InstigatePosition));
            }
        }

        private void FrameFunction_Architecture_AfterGetLevelUpMilitaryKind()
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.UpgradableMilitaryKindList.GetSelectedList();
                if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
                {
                    this.CurrentArchitecture.LevelUpMilitary(this.CurrentMilitary, this.CurrentGameObjects[0] as MilitaryKind);
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetLevelUpMilitaries()
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.LevelUpMilitaryList.GetSelectedList();
                if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
                {
                    this.CurrentMilitary = (Military) this.CurrentGameObjects[0];
                    Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.MilitaryKind, FrameFunction.GetLevelUpMiliaryKind, true, true, true, false, this.CurrentArchitecture.GetUpgradableMilitaryKindList(this.CurrentMilitary), null, "编队升级", "编队升级");
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetMergeMilitary()
        {
            GameObjectList selectedList = this.CurrentArchitecture.MergeMilitaryList.GetSelectedList();
            if ((selectedList != null) && (selectedList.Count == 1))
            {
                this.CurrentMilitary = selectedList[0] as Military;
                Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Military, FrameFunction.GetBeMergedMilitaries, false, true, true, false, this.CurrentArchitecture.GetBeMergedMilitaryList(this.CurrentMilitary), null, "选择编队", "");
            }
        }

        private void FrameFunction_Architecture_AfterSelectMarryablePerson()
        {
            GameObjectList selectedList = this.CurrentArchitecture.Persons.GetSelectedList();
            if ((selectedList != null) && (selectedList.Count == 1))
            {
                this.CurrentPerson = selectedList[0] as Person;
                Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.SelectMarryTo, false, true, true, false, this.CurrentPerson.MakeMarryable(true), null, "选择对象", "");
            }
        }

        private void FrameFunction_Architecture_AfterSelectMarryablePerson2()
        {
            GameObjectList selectedList = this.CurrentArchitecture.Persons.GetSelectedList();
            if ((selectedList != null) && (selectedList.Count == 1))
            {
                this.CurrentPerson = selectedList[0] as Person;
                Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.SelectMarryTo, false, true, true, false, this.CurrentPerson.MakeMarryable2(true), null, "选择纳妾对象", "");
            }
        }
        private void FrameFunction_Architecture_AfterSelectMarryTo()
        {
            GameObjectList selectedList = this.CurrentArchitecture.Persons.GetSelectedList();
            if ((selectedList != null) && (selectedList.Count == 2))
            {
                if (this.CurrentPerson == selectedList[0])
                {
                    this.CurrentPerson.Marry(selectedList[1] as Person, this.CurrentArchitecture.BelongedFaction.Leader);
                }
                else
                {
                    this.CurrentPerson.Marry(selectedList[0] as Person, this.CurrentArchitecture.BelongedFaction.Leader);
                }
            }
            this.CurrentArchitecture.Persons.ClearSelected();
        }

        private void FrameFunction_Architecture_AfterSelectTrainableChildren()
        {
            GameObjectList selectedList = this.CurrentArchitecture.BelongedFaction.Children.GetSelectedList();
            if ((selectedList != null) && (selectedList.Count >= 1))
            {
                this.CurrentPersons = selectedList;
                Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.TrainPolicy, FrameFunction.SelectTrainPolicy, false, true, true, false, (this.CurrentPersons[0] as Person).TrainPolicies(), null, "选择培育方针", "");
            }
            this.CurrentArchitecture.Persons.ClearSelected();
        }

        private void FrameFunction_Architecture_AfterSelectTrainPolicy()
        {
            GameObjectList selectedList = Session.Current.Scenario.GameCommonData.AllTrainPolicies.GetSelectedList();
            if ((selectedList != null) && (selectedList.Count == 1))
            {
                foreach (Person p in this.CurrentPersons)
                {
                    p.TrainPolicy = (TrainPolicy)selectedList[0];
                }
            }
            this.CurrentArchitecture.Persons.ClearSelected();
        }

        private void FrameFunction_Architecture_AfterGetNewCapital()
        {
            GameObjectList selectedList = this.CurrentArchitecture.ChangeCapitalArchitectureList.GetSelectedList();
            if ((selectedList != null) && (selectedList.Count == 1))
            {
                this.CurrentArchitecture.DecreaseFund(this.CurrentArchitecture.ChangeCapitalCost);
                this.CurrentArchitecture.BelongedFaction.ChangeCapital(selectedList[0] as Architecture);
            }
        }

        private void FrameFunction_Architecture_AfterGetNewMilitaryKind()
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.NewMilitaryKindList.GetSelectedList();
                if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
                {
                    this.CurrentArchitecture.CreateMilitary(this.CurrentGameObjects[0] as MilitaryKind);
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetOneArchitecture()
        {
            if (this.CurrentArchitecture != null)
            {
                GameObjectList selectedList = this.CurrentArchitecture.TransferArchitectureList.GetSelectedList();
                if ((selectedList != null) && (selectedList.Count == 1))
                {
                    foreach (Person person in this.CurrentGameObjects)
                    {
                        person.MoveToArchitecture(selectedList[0] as Architecture);
                        //this.CurrentArchitecture.RemovePerson(person);
                    }
                }
            }
        }

        public void FrameFunction_Architecture_AfterGetMoveCaptiveArchitectureBySelecting(Architecture architecture) //移动俘虏
        {
            if (architecture != null && this.CurrentPersons.Count > 0)
            {
                foreach (Captive captive in this.CurrentPersons)
                {
                    captive.CaptivePerson.MoveToArchitecture(architecture);
                }
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        public void FrameFunction_Architecture_AfterGetTransferMilitaryArchitectureBySelecting() //运输编队
        {
            if (this.CurrentArchitecture != null && this.CurrentMilitaries.Count > 0)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.BelongedFaction.ArchitecturesExcluding(this.CurrentArchitecture).GetSelectedList();
                if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
                {
                    Architecture targetArchitecture = this.CurrentGameObjects[0] as Architecture;

                    double distance = (double)Session.Current.Scenario.GetDistance(this.CurrentArchitecture.ArchitectureArea, targetArchitecture.ArchitectureArea);

                    foreach (Military military in this.CurrentMilitaries)
                    {
                        if (this.CurrentArchitecture.Fund >= military.TransferFundCost(distance) && 
                            this.CurrentArchitecture.Food >= military.TransferFoodCost(distance) &&
                            !this.CurrentArchitecture.IsSurrounded() && !targetArchitecture.IsSurrounded())
                        {
                            this.CurrentArchitecture.DecreaseFund(military.TransferFundCost(distance));
                            this.CurrentArchitecture.DecreaseFood(military.TransferFoodCost(distance));
                            military.StartingArchitecture = this.CurrentArchitecture;
                            military.TargetArchitecture = targetArchitecture;
                            //military.ArrivingDays = Math.Max(1, military.TransferDays(distance));
                            military.ArrivingDays = Math.Max(1, military.TransferDays(distance));
                            this.CurrentArchitecture.RemoveMilitary(military);
                            this.CurrentArchitecture.BelongedFaction.TransferingMilitaries.Add(military);
                            this.CurrentArchitecture.BelongedFaction.TransferingMilitaryCount++;
                        }
                    }
                }
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        public void FrameFunction_Architecture_AfterGetOneArchitectureBySelecting(Architecture architecture)
        {
            if (architecture != null && this.CurrentPersons.Count>0)
            {
                foreach (Person person in this.CurrentPersons)
                {
                    person.MoveToArchitecture(architecture);                    
                }
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }            
        }

        private void FrameFunction_Architecture_AfterGetRecruitmentMilitary() // 补充
        {
            GameObjectList selectedList = this.CurrentArchitecture.RecruitmentMilitaryList.GetSelectedList();
            if ((selectedList != null) && (selectedList.Count == 1))
            {
                this.CurrentMilitary = selectedList[0] as Military;
                Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Work, FrameFunction.GetRecruitmentPerson, false, true, true, false, this.CurrentArchitecture.PersonsExcludeNvGuan, null, "补充", "补充");
            }
        }

        private void FrameFunction_Faction_KillRelease_MoveCaptive() //俘虏可移动
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.Captives.GetSelectedList();
                if (this.CurrentGameObjects != null)
                {
                    //this.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Architecture, FrameFunction.GetOneArchitecture, false, true, true, false, this.CurrentArchitecture.GetTransferArchitectureList(), null, "目标", "");
                    //this.mainGameScreen.ShowMapViewSelector(false , this.CurrentArchitecture.GetTransferArchitectureList());
                    this.CurrentPersons = this.CurrentGameObjects.GetList();
                    Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.MoveCaptive));

                }
            }
        }

        private void FrameFunction_Architecture_AfterGetAutoCampaignMilitaries() //自动出征
        {
            if (this.CurrentArchitecture != null)
            {
                // Get selected Military list
                var selectedMilitaries = this.CurrentArchitecture.GetCampaignMilitaryList().GetSelectedList();
                if (selectedMilitaries != null && selectedMilitaries.Count > 0)
                {
                    this.CurrentMilitaries = selectedMilitaries.GetList();
                    this.CurrentMilitary = this.CurrentMilitaries[0] as Military;
                    
                    // FIX: Extract Person objects from Military leaders, NOT the Military objects themselves
                    this.CurrentGameObjects = new GameObjectList();
                    this.CurrentPerson = null;
                    
                    if (this.CurrentMilitary != null)
                    {
                        // Get the leader from the military
                        Person leader = null;
                        if (this.CurrentArchitecture.PersonsExcludeNvGuan.HasGameObject(this.CurrentMilitary.FollowedLeader))
                        {
                            leader = this.CurrentMilitary.FollowedLeader;
                        }
                        else if (this.CurrentArchitecture.PersonsExcludeNvGuan.HasGameObject(this.CurrentMilitary.Leader))
                        {
                            leader = this.CurrentMilitary.Leader;
                        }
                        else
                        {
                            // Fallback: get first available person from architecture
                            foreach (var obj in this.CurrentArchitecture.PersonsExcludeNvGuan.GetList())
                            {
                                if (obj is Person p)
                                {
                                    leader = p;
                                    break;
                                }
                            }
                        }
                        
                        if (leader != null)
                        {
                            this.CurrentPerson = leader;
                            this.CurrentGameObjects.Add(leader);
                            
                            // Add leader's preferred troop persons
                            foreach (Person p in leader.preferredTroopPersons)
                            {
                                if (this.CurrentArchitecture.PersonsExcludeNvGuan.HasGameObject(p) && !this.CurrentGameObjects.HasGameObject(p))
                                {
                                    this.CurrentGameObjects.Add(p);
                                }
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[AutoCampaign] Leader: {leader.Name}, Persons count: {this.CurrentGameObjects.Count}");
                            Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.ArchitectureAvailableContactArea));
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[AutoCampaign] ERROR: No valid person found for auto campaign");
                        }
                    }
                }
            }
        }


        private void FrameFunction_Architecture_AfterGetTransferMilitary() //运输编队
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.movableMilitaries.GetSelectedList();
                if (this.CurrentGameObjects != null)
                {

                    //this.CurrentArchitecture.RemoveMilitary(m);
                    this.CurrentMilitaries = this.CurrentGameObjects.GetList();
                    Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Architecture, FrameFunction.GetTransferArchitecture, false, true, true, false, this.CurrentArchitecture.BelongedFaction .ArchitecturesExcluding(this.CurrentArchitecture), null, "运兵", "运兵");
                    //this.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.MilitaryTransfer));



                }
            }
        }
                 

        private void FrameFunction_Architecture_AfterGetRecruitmentPerson() // 补充
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.PersonsExcludeNvGuan.GetSelectedList();
                if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
                {
                    this.CurrentPerson = this.CurrentGameObjects[0] as Person;
                    this.CurrentPerson.RecruitMilitary(this.CurrentMilitary);
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetRedeemCaptive() // 赎回俘虏
        {
            this.CurrentGameObjects = this.CurrentArchitecture.RedeemCaptiveList.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                (this.CurrentGameObjects[0] as Captive).SendRansom((this.CurrentGameObjects[0] as Captive).BelongedFaction.Capital, this.CurrentArchitecture);
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        private void FrameFunction_Architecture_AfterGetReleaseCaptive() // 释放俘虏
        {
            this.CurrentGameObjects = this.CurrentArchitecture.BelongedFaction.Captives.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                foreach (Captive captive in this.CurrentGameObjects)
                {
                    captive.SelfReleaseCaptive();
                }
            }
        }

        private void FrameFunction_Faction_PromoteNvGuan()
        {
            this.CurrentGameObjects = this.CurrentArchitecture.PromotableNvGuans.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                foreach (Person p in  this.CurrentGameObjects)
                {
                    p.PromoteFromNvGuan();
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetRewardPerson() // 奖赏
        {
            this.CurrentGameObjects = this.CurrentArchitecture.RewardPersonList.GetSelectedList();
            
            if (this.CurrentGameObjects.Count > 0)
            {
                int rewardCost = Session.Parameters.RewardPersonCost;
                int totalCost = this.CurrentGameObjects.Count * rewardCost;
                
                // 检查资金是否足够
                if (this.CurrentArchitecture.BelongedFaction.Fund < totalCost)
                {
                    // 资金不足，显示提示（可选）
                    return;
                }
                
                // 🎯 显示确认对话框
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.SetSimpleTextDialog(
                    Session.MainGame.mainGameScreen.Plugins.SimpleTextDialogPlugin);
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.ClearFunctions();
                
                // 设置确认回调：执行褒赏逻辑
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.AddYesFunction(
                    new GameDelegates.VoidFunction(() =>
                    {
                        // 执行褒赏
                        for (int i = 0; i < this.CurrentGameObjects.Count; i++)
                        {
                            Person person = (Person)this.CurrentGameObjects[i];
                            
                            // 扣除资金
                            this.CurrentArchitecture.DecreaseFund(rewardCost);
                            
                            // 执行褒赏
                            int increase = person.ReceiveReward(rewardCost);
                        }
                        
                        // 播放音效
                        Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                    }));
                
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                
                // 设置对话框文本（显示褒赏人数和总花费）
                // 🎯 临时方案：使用通用分支，后续可在XML中添加专用分支
                Person firstPerson = (Person)this.CurrentGameObjects[0];
                firstPerson.TextDestinationString = $"褒赏 {this.CurrentGameObjects.Count} 人，花费 {totalCost} 资金";
                Session.MainGame.mainGameScreen.Plugins.SimpleTextDialogPlugin.SetGameObjectBranch(
                    firstPerson, "RewardPerson");
                
                Session.MainGame.mainGameScreen.Plugins.ConfirmationDialogPlugin.IsShowing = true;
            }
        }

        private void FrameFunction_Architecture_AfterGetSearchPerson() // 搜索
        {
            this.CurrentGameObjects = this.CurrentArchitecture.PersonsExcludeNvGuan.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                foreach (Person person in this.CurrentGameObjects)
                {
                    person.shoudongjinxingsousuo();
                }
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        private void FrameFunction_Architecture_AfterGetSectionToDemolish()
        {
            this.CurrentGameObjects = this.CurrentArchitecture.BelongedFaction.Sections.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                foreach (Section section in this.CurrentGameObjects)
                {
                    this.CurrentArchitecture.BelongedFaction.RemoveSection(section);
                    Session.Current.Scenario.Sections.Remove(section);
                    Section anotherSection = this.CurrentArchitecture.BelongedFaction.GetAnotherSection(section);
                    if (anotherSection != null)
                    {
                        foreach (Architecture architecture in section.Architectures)
                        {
                            anotherSection.AddArchitecture(architecture);
                        }
                    }
                }
                foreach (Section section in this.CurrentArchitecture.BelongedFaction.Sections.GetList())
                {
                    if ((section.OrientationSection != null) && !this.CurrentArchitecture.BelongedFaction.Sections.HasGameObject(section.OrientationSection))
                    {
                        foreach (SectionAIDetail detail in Session.Current.Scenario.GameCommonData.AllSectionAIDetails.SectionAIDetails.Values)
                        {
                            if (detail.OrientationKind == SectionOrientationKind.无)
                            {
                                section.AIDetail = detail;
                                break;
                            }
                        }
                        section.OrientationSection = null;
                    }
                    section.RefreshSectionName();
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetSection()
        {
            // Get the selected section from the list
            Section selectedSection = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as Section;
            if (selectedSection != null && this.CurrentArchitecture != null && this.CurrentArchitecture.BelongedFaction != null)
            {
                // Open the MarshalSectionDialog for the selected section
                Session.MainGame.mainGameScreen.Plugins.MarshalSectionDialogPlugin.SetFaction(this.CurrentArchitecture.BelongedFaction);
                Session.MainGame.mainGameScreen.Plugins.MarshalSectionDialogPlugin.SetSection(selectedSection);
                Session.MainGame.mainGameScreen.Plugins.MarshalSectionDialogPlugin.SetMapPosition(ShowPosition.Center);
                Session.MainGame.mainGameScreen.Plugins.MarshalSectionDialogPlugin.IsShowing = true;
            }
        }

        private void FrameFunction_Architecture_AfterGetShortestNoWaterRouteway()
        {
            this.CurrentGameObjects = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItemList as GameObjectList;
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count > 0))
            {
                foreach (Architecture architecture in this.CurrentGameObjects)
                {
                    Routeway routeway = this.CurrentArchitecture.BuildShortestRouteway(architecture, true);
                    if (routeway != null)
                    {
                        routeway.Building = true;
                    }
                }
                Session.GlobalVariables.CurrentMapLayer = MapLayerKind.Routeway;
            }
        }

        private void FrameFunction_Architecture_AfterGetShortestRouteway()   //粮道最短
        {
            this.CurrentGameObjects = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItemList as GameObjectList;
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count > 0))
            {
                foreach (Architecture architecture in this.CurrentGameObjects)
                {
                    Routeway routeway = this.CurrentArchitecture.BuildShortestRouteway(architecture, false);
                    if (routeway != null)
                    {
                        routeway.Building = true;
                    }
                }
                Session.GlobalVariables.CurrentMapLayer = MapLayerKind.Routeway;
            }
        }

        /*
        private void FrameFunction_Architecture_AfterGetSpyPerson()
        {
            this.CurrentGameObjects = this.CurrentArchitecture.Persons.GetSelectedList();
            if (this.CurrentGameObjects != null)
            {
                this.CurrentPersons = this.CurrentGameObjects.GetList();
                this.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.SpyPosition));
            }
        }
        */

        private void FrameFunction_Architecture_AfterGetStudySkillPerson() // 修习技能
        {
            this.CurrentGameObjects = this.CurrentArchitecture.PersonStudySkillList.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count > 0))
            {
                foreach (Person person in this.CurrentGameObjects)
                {
                    person.GoForStudySkill();
                    person.ManualStudy = true;
                }
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        private void FrameFunction_Architecture_AfterGetStudyStunt() // 修习特技
        {
            this.CurrentGameObjects = this.CurrentPerson.StudyStuntList.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                this.CurrentPerson.GoForStudyStunt(this.CurrentGameObjects[0] as Stunt);
                this.CurrentPerson.ManualStudy = true;
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        private void FrameFunction_Architecture_AfterGetStudyStuntPerson() // 修习特技
        {
            this.CurrentGameObjects = this.CurrentArchitecture.PersonStudyStuntList.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                Person person = this.CurrentGameObjects[0] as Person;
                if (person != null)
                {
                    this.CurrentPerson = person;
                    Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Stunt, FrameFunction.GetStudyStunt, false, true, true, false, person.GetStudyStuntList(), null, "研习", "");
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetStudyTitle() // 修习称号
        {
            this.CurrentGameObjects = this.CurrentPerson.StudyTitleList.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                this.CurrentPerson.GoForStudyTitle(this.CurrentGameObjects[0] as Title);
                this.CurrentPerson.ManualStudy = true;
                Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        private void FrameFunction_Architecture_AfterGetStudyTitlePerson() // 修习称号
        {
            this.CurrentGameObjects = this.CurrentArchitecture.PersonStudyTitleList.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                Person person = this.CurrentGameObjects[0] as Person;
                if (person != null)
                {
                    this.CurrentPerson = person;
                    Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Title, FrameFunction.GetStudyTitle, false, true, true, false, person.GetStudyTitleList(), null, "研习", "");
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetAppointableTitle() // 任命官职
        {
            this.CurrentGameObjects = this.CurrentPerson.AppointableTitleList.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                this.CurrentPerson.AwardTitle(this.CurrentGameObjects[0] as Title);
                
               // this.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        private void FrameFunction_Architecture_AfterGetAppointPerson() // 任命官职
        {
            this.CurrentGameObjects = this.CurrentArchitecture.Kerenmingdeguanyuan.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                Person person = this.CurrentGameObjects[0] as Person;
                if (person != null)
                {
                    this.CurrentPerson = person;
                    Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Title, FrameFunction.GetAppointableTitle, false, true, true, false, person.GetAppointableTitleList(), null, "任命官职", "");
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetRecallablePerson() // 免除职位
        {
            this.CurrentGameObjects = this.CurrentArchitecture.RecallableOfficer.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                Person person = this.CurrentGameObjects[0] as Person;
                if (person != null)
                {
                    this.CurrentPerson = person;
                    Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Title, FrameFunction.GetRecallableTitle, false, true, true, true, person.RecallableTitleList(), null, "免除职位", "");
                }
            }
        }

        private void FrameFunction_Architecture_AfterGetRecallableTitle() // 免除职位
        {
            this.CurrentGameObjects = this.CurrentPerson.RecallableTitleList().GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count > 0))
            {
                foreach (Title t in this.CurrentGameObjects)
                {
                    this.CurrentPerson.RemoveTitle(t);
                }


                // this.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
            }
        }

        private void FrameFunction_Architecture_AfterGetTrainingMilitary()  //修改后未用
        {
            GameObjectList selectedList = this.CurrentArchitecture.TrainingMilitaryList.GetSelectedList();
            if ((selectedList != null) && (selectedList.Count == 1))
            {
                this.CurrentMilitary = selectedList[0] as Military;
                Session.MainGame.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Work, FrameFunction.GetTrainingPerson, false, true, true, false, this.CurrentArchitecture.PersonsExcludeNvGuan, null, "训练", "训练");
            }
        }

        private void FrameFunction_Architecture_PersonConvene() // 召唤
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.PersonConveneList.GetSelectedList();
                if (this.CurrentGameObjects != null)
                {
                    foreach (Person person in this.CurrentGameObjects)
                    {
                        person.MoveToArchitecture(this.CurrentArchitecture);
                    }
                }
            }
        }

        private void FrameFunction_Architecture_PersonTransfer()
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.MovablePersons.GetSelectedList();
                if (this.CurrentGameObjects != null)
                {
                    //this.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Architecture, FrameFunction.GetOneArchitecture, false, true, true, false, this.CurrentArchitecture.GetTransferArchitectureList(), null, "目标", "");
                    //this.mainGameScreen.ShowMapViewSelector(false , this.CurrentArchitecture.GetTransferArchitectureList());
                    this.CurrentPersons = this.CurrentGameObjects.GetList();
                    Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.WujiangDiaodong));

                }
            }
        }

        private void FrameFunction_Monarch_hougongTop_moveFeizi()
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.Feiziliebiao.GetSelectedList();
                if (this.CurrentGameObjects != null)
                {
                    //this.mainGameScreen.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Architecture, FrameFunction.GetOneArchitecture, false, true, true, false, this.CurrentArchitecture.GetTransferArchitectureList(), null, "目标", "");
                    //this.mainGameScreen.ShowMapViewSelector(false , this.CurrentArchitecture.GetTransferArchitectureList());
                    this.CurrentPersons = this.CurrentGameObjects.GetList();
                    Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.MoveFeizi));

                }
            }
        }

        private void FrameFunction_Monarch_hougongTop_releaseFeizi()
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.Feiziliebiao.GetSelectedList();
                if (this.CurrentGameObjects != null)
                {
                    foreach (GameObject o in this.CurrentGameObjects)
                    {
                        ((Person)o).feiziRelease();
                    }

                }
            }
        }

        /*
        private void FrameFunction_Faction_ZhaoXianBang_DengYong() //强制登用武将
        {
            this.CurrentPerson = this.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person ? (Person)this.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            {
                if (this.CurrentPerson != null)
                {
                    if (this.CurrentArchitecture.Fund > this.CurrentPerson.UntiredMerit)
                    {
                        this.CurrentArchitecture.DecreaseFund(CurrentPerson.UntiredMerit);
                        this.CurrentPerson.Status = PersonStatus.Normal;
                        if (this.CurrentPerson.Loyalty < 110)
                        {
                            this.CurrentPerson.Loyalty = 110;
                        }
                        this.CurrentArchitecture.DengYong(CurrentPerson);
                    }


                    
                }
                
                this.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.DengYongWujiang));
            }
        }
        */

        private void FrameFunction_Architecture_WorkingList()
        {
            if (this.CurrentArchitecture != null)
            {
                if (this.CurrentArchitectureWorkKind != ArchitectureWorkKind.无)
                {
                    foreach (Person person in this.CurrentArchitecture.Persons)
                    {
                        if (person.Selected)
                        {
                            person.WorkKind = this.CurrentArchitectureWorkKind;
                        }
                        else if (person.WorkKind == this.CurrentArchitectureWorkKind)
                        {
                            person.WorkKind = ArchitectureWorkKind.无;
                        }
                    }
                    Person extremePersonFromWorkingList = this.CurrentArchitecture.GetExtremePersonFromWorkingList(this.CurrentArchitectureWorkKind, true);
                    if (extremePersonFromWorkingList != null)
                    {
                        Session.MainGame.mainGameScreen.Plugins.PersonBubblePlugin.AddPerson(extremePersonFromWorkingList, this.CurrentArchitecture.Position, TextMessageKind.StartWork, "Work");
                    }
                }
                else
                {
                    foreach (Person person in this.CurrentArchitecture.Persons)
                    {
                        if (person.Selected)
                        {
                            person.WorkKind = ArchitectureWorkKind.无;
                            person.OldWorkKind = ArchitectureWorkKind.无;
                        }
                    }
                }
            }
        }

        private void FrameFunction_Troop_AfterGetAttackDefaultKind()
        {
            this.CurrentGameObjects = Session.Current.Scenario.GameCommonData.AllAttackDefaultKinds.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                (this.CurrentGameObjects[0] as AttackDefaultKind).Apply(this.CurrentTroop);
            }
        }

        private void FrameFunction_Troop_AfterGetAttackTargetKind()
        {
            this.CurrentGameObjects = Session.Current.Scenario.GameCommonData.AllAttackTargetKinds.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                (this.CurrentGameObjects[0] as AttackTargetKind).Apply(this.CurrentTroop);
            }
        }

        private void FrameFunction_Troop_AfterGetCastDefaultKind()
        {
            this.CurrentGameObjects = Session.Current.Scenario.GameCommonData.AllCastDefaultKinds.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                (this.CurrentGameObjects[0] as CastDefaultKind).Apply(this.CurrentTroop);
            }
        }

        private void FrameFunction_Troop_AfterGetCastTargetKind()
        {
            this.CurrentGameObjects = Session.Current.Scenario.GameCommonData.AllCastTargetKinds.GetSelectedList();
            if ((this.CurrentGameObjects != null) && (this.CurrentGameObjects.Count == 1))
            {
                (this.CurrentGameObjects[0] as CastTargetKind).Apply(this.CurrentTroop);
            }
        }

        public void HandleFrameFunction(FrameFunction function)
        {
            switch (function)
            {
                case FrameFunction.GetOneArchitecture:
                    this.FrameFunction_Architecture_AfterGetOneArchitecture();
                    break;

                case FrameFunction.Architecture_WorkingList:
                    this.FrameFunction_Architecture_WorkingList();
                    break;

                case FrameFunction.PersonTransfer:
                    this.FrameFunction_Architecture_PersonTransfer();
                    break;

                case FrameFunction.GetAutoCampaignMilitaries://自动出征
                    this.FrameFunction_Architecture_AfterGetAutoCampaignMilitaries();
                    break;

                case FrameFunction.GetTransferMilitary://运输编队
                    this.FrameFunction_Architecture_AfterGetTransferMilitary();
                    break;

                case FrameFunction.GetTransferArchitecture:
                    this.FrameFunction_Architecture_AfterGetTransferMilitaryArchitectureBySelecting();
                    break;

                case FrameFunction.PersonConvene:
                    this.FrameFunction_Architecture_PersonConvene();
                    break;

                case FrameFunction.GetConvinceSourcePerson:
                    this.FrameFunction_Architecture_AfterGetConvinceSourcePerson();
                    break;

                case FrameFunction.PersonManualHire:
                    this.FrameFunction_Architecture_AfterPersonManualHire();
                    break;

                case FrameFunction.GetConvinceDestinationPerson:
                    this.FrameFunction_Architecture_AfterGetConvinceDestinationPerson();
                    break;

                case FrameFunction.GetConvinceTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetConvinceTargetForAnalysis();
                    break;

                case FrameFunction.GetDestroyTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetDestroyTargetForAnalysis();
                    break;

                case FrameFunction.GetInstigateTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetInstigateTargetForAnalysis();
                    break;

                case FrameFunction.GetGossipTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetGossipTargetForAnalysis();
                    break;

                case FrameFunction.GetJailBreakTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetJailBreakTargetForAnalysis();
                    break;

                case FrameFunction.GetAssassinateTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetAssassinateTargetForAnalysis();
                    break;

                case FrameFunction.GetEnhanceDiplomaticRelationTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelationTargetForAnalysis();
                    break;

                case FrameFunction.GetEnhanceDiplomaticRelationCost:
                    this.FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelationCost();
                    break;

                case FrameFunction.GetTruceDiplomaticRelationTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetTruceDiplomaticRelationTargetForAnalysis();
                    break;

                case FrameFunction.GetInduceSurrenderTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetInduceSurrenderTargetForAnalysis();
                    break;

                case FrameFunction.GetInduceSurrenderPerson:
                    this.FrameFunction_Architecture_AfterGetInduceSurrenderPerson();
                    break;

                case FrameFunction.GetYearlyTalentRecommendation:
                    this.FrameFunction_Architecture_AfterGetYearlyTalentRecommendation();
                    break;

                case FrameFunction.GetRewardPerson:
                    this.FrameFunction_Architecture_AfterGetRewardPerson();
                    break;

                case FrameFunction.GetRedeemCaptive:
                    this.FrameFunction_Architecture_AfterGetRedeemCaptive();
                    break;

                case FrameFunction.GetReleaseCaptive:
                    this.FrameFunction_Architecture_AfterGetReleaseCaptive();
                    break;

                case FrameFunction.GetStudySkillPerson:
                    this.FrameFunction_Architecture_AfterGetStudySkillPerson();
                    break;

                case FrameFunction.GetStudyTitlePerson:
                    this.FrameFunction_Architecture_AfterGetStudyTitlePerson();
                    break;

                case FrameFunction.GetStudyTitle:
                    this.FrameFunction_Architecture_AfterGetStudyTitle();
                    break;

                case FrameFunction.GetAppointPerson://封官
                    this.FrameFunction_Architecture_AfterGetAppointPerson();
                    break;

                case FrameFunction.GetAppointableTitle: //封官
                    this.FrameFunction_Architecture_AfterGetAppointableTitle();
                    break;

                case FrameFunction.GetRecallablePerson://免官
                    this.FrameFunction_Architecture_AfterGetRecallablePerson();
                    break;

                case FrameFunction.GetRecallableTitle: //免官
                    this.FrameFunction_Architecture_AfterGetRecallableTitle();
                    break;

                case FrameFunction.GetStudyStuntPerson:
                    this.FrameFunction_Architecture_AfterGetStudyStuntPerson();
                    break;

                case FrameFunction.GetStudyStunt:
                    this.FrameFunction_Architecture_AfterGetStudyStunt();
                    break;

                case FrameFunction.GetNewMilitaryKind:
                    this.FrameFunction_Architecture_AfterGetNewMilitaryKind();
                    break;

                case FrameFunction.GetRecruitmentMilitary:
                    this.FrameFunction_Architecture_AfterGetRecruitmentMilitary();
                    break;

                case FrameFunction.GetRecruitmentPerson:
                    this.FrameFunction_Architecture_AfterGetRecruitmentPerson();
                    break;

                case FrameFunction.GetMergeMilitary:
                    this.FrameFunction_Architecture_AfterGetMergeMilitary();
                    break;

                case FrameFunction.GetBeMergedMilitaries:
                    this.FrameFunction_Architecture_AfterGetBeMergedMilitaries();
                    break;

                case FrameFunction.GetBeDisbandedMilitaries:
                    this.FrameFunction_Architecture_AfterGetBeDisbandedMilitaries();
                    break;

                case FrameFunction.GetLevelUpMilitaries:
                    this.FrameFunction_Architecture_AfterGetLevelUpMilitaries();
                    break;

                case FrameFunction.GetLevelUpMiliaryKind:
                    this.FrameFunction_Architecture_AfterGetLevelUpMilitaryKind();
                    break;

                case FrameFunction.SelectMarryablePerson:
                    this.FrameFunction_Architecture_AfterSelectMarryablePerson();
                    break;

                case FrameFunction.SelectMarryablePerson2:
                    this.FrameFunction_Architecture_AfterSelectMarryablePerson2();
                    break;

                case FrameFunction.SelectMarryTo:
                    this.FrameFunction_Architecture_AfterSelectMarryTo();
                    break;

                case FrameFunction.SelectTrainableChildren:
                    this.FrameFunction_Architecture_AfterSelectTrainableChildren();
                    break;

                case FrameFunction.SelectTrainPolicy:
                    this.FrameFunction_Architecture_AfterSelectTrainPolicy();
                    break;

                case FrameFunction.GetNewCapital:
                    this.FrameFunction_Architecture_AfterGetNewCapital();
                    break;

                case FrameFunction.GetEnhanceDiplomaticRelation:
                    this.FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelation();
                    break;

                case FrameFunction.GetEnhanceDiplomaticRelationPerson:
                    this.FrameFunction_Architecture_AfterGetEnhanceDiplomaticRelationPerson();
                    break;

                case FrameFunction.GetAllyDiplomaticRelationPerson:
                    this.FrameFunction_Architecture_AfterGetAllyDiplomaticRelationPerson();
                    break;

                case FrameFunction.GetFriendlyDiplomaticRelation:
                    this.FrameFunction_Architecture_AfterGetFriendlyDiplomaticRelation();
                    break;

                case FrameFunction.GetAllyDiplomaticRelationTargetForAnalysis:
                    this.FrameFunction_Architecture_AfterGetAllyDiplomaticRelationTargetForAnalysis();
                    break;

                case FrameFunction.GetTruceDiplomaticRelation:
                    this.FrameFunction_Architecture_AfterGetTruceDiplomaticRelation();
                    break;

                case FrameFunction.GetTruceDiplomaticRelationPerson:
                    this.FrameFunction_Architecture_AfterGetTruceDiplomaticRelationPerson();
                    break;

                case FrameFunction.GetDenounceDiplomaticRelation:
                    this.FrameFunction_Architecture_AfterGetDenounceDiplomaticRelation();
                    break;
                    
                case FrameFunction .GetQuanXiangDiplomaticRelation: //劝降
                    this.FrameFunction_Architecture_AfterGetQuanXiangDiplomaticRelation();
                    break;
         
                case FrameFunction .GetQuanXiangDiplomaticRelationPerson:
                    this.FrameFunction_Architecture_AfterGetQuanXiangDiplomaticRelationPerson();
                    break;

                case FrameFunction.GetAttackDefaultKind:
                    this.FrameFunction_Troop_AfterGetAttackDefaultKind();
                    break;

                case FrameFunction.GetAttackTargetKind:
                    this.FrameFunction_Troop_AfterGetAttackTargetKind();
                    break;

                case FrameFunction.GetCastDefaultKind:
                    this.FrameFunction_Troop_AfterGetCastDefaultKind();
                    break;

                case FrameFunction.GetCastTargetKind:
                    this.FrameFunction_Troop_AfterGetCastTargetKind();
                    break;

                case FrameFunction.GetInformationKind:
                    this.FrameFunction_Architecture_AfterGetInformationKind();
                    break;

                case FrameFunction.GetOfficerType:
                    this.FrameFunction_Architecture_AfterGetOfficerType();
                    break;

                case FrameFunction.GetInformationToStop:
                    this.FrameFunction_Architecture_AfterGetInformationToStop();
                    break;

                case FrameFunction.GetInformationPerson:
                    this.FrameFunction_Architecture_AfterGetInformationPerson();
                    break;
                    /*
                case FrameFunction.GetSpyPerson:
                    this.FrameFunction_Architecture_AfterGetSpyPerson();
                    break;
                     */

                case FrameFunction.GetDestroyPerson:
                    this.FrameFunction_Architecture_AfterGetDestroyPerson();
                    break;

                case FrameFunction.GetInstigatePerson:
                    this.FrameFunction_Architecture_AfterGetInstigatePerson();
                    break;

                case FrameFunction.GetGossipPerson:
                    this.FrameFunction_Architecture_AfterGetGossipPerson();
                    break;

                case FrameFunction.GetJailBreakPerson:
                    this.FrameFunction_Architecture_AfterGetJailBreakPerson();
                    break;

                case FrameFunction.GetAssassinatePerson:
                    this.FrameFunction_Architecture_AfterGetAssassinatePerson();
                    break;

                case FrameFunction.GetAssassinatePersonTarget:
                    this.FrameFunction_Architecture_AfterGetAssassinatePersonTarget();
                    break;

                case FrameFunction.GetSearchPerson:
                    this.FrameFunction_Architecture_AfterGetSearchPerson();
                    break;

                case FrameFunction.GetFacilityToBuild:
                    this.FrameFunction_Architecture_AfterGetFacilityToBuild();
                    break;

                case FrameFunction.GetFacilityToDemolish:
                    this.FrameFunction_Architecture_AfterGetFacilityToDemolish();
                    break;

                case FrameFunction.GetSectionToDemolish:
                    this.FrameFunction_Architecture_AfterGetSectionToDemolish();
                    break;

                case FrameFunction.GetSection:
                    this.FrameFunction_Architecture_AfterGetSection();
                    break;

                case FrameFunction.GetShortestRouteway:
                    this.FrameFunction_Architecture_AfterGetShortestRouteway();
                    break;

                case FrameFunction.GetShortestNoWaterRouteway:
                    this.FrameFunction_Architecture_AfterGetShortestNoWaterRouteway();
                    break;

                #region 宝物
                case FrameFunction.GetConfiscateTreasure:
                    this.FrameFunction_Architecture_AfterGetConfiscateTreasure();
                    break;
                case FrameFunction.GetAwardTreasure:
                    this.FrameFunction_Architecture_AfterGetAwardTreasure();
                    break;
                case FrameFunction.GetAwardTreasurePerson:
                    this.FrameFunction_Architecture_AfterGetAwardTreasurePerson();
                    break;
                case FrameFunction.GetSellTreasure:
                    this.FrameFunction_Architecture_AfterGetSellTreasure();
                    break;
                case FrameFunction.GetBuyTreasure:
                    this.FrameFunction_Architecture_AfterGetBuyTreasure();
                    break;
                #endregion

                case FrameFunction.xuanzemeinv :
                    this.FrameFunction_Architecture_Afterxuanzemeinv();
                    break;
                case FrameFunction.chongxingmeinv:
                    this.FrameFunction_Architecture_chongxingmeinv();
                    break;
                case FrameFunction.KillPerson:
                    this.FrameFunction_Architecture_KillPerson();
                    break;
                case FrameFunction.KillCaptive:
                    this.FrameFunction_Architecture_KillCaptive();
                    break;
                case FrameFunction.ReleaseSelfPerson:
                    this.FrameFunction_Architecture_ReleaseSelfPerson();
                    break;

                case FrameFunction.SelectPrince:
                    this.FrameFunction_Architecture_SelectPrince();
                    break;
                case FrameFunction.AppointMayor: //任命太守
                    this.FrameFunction_Architecture_AppointMayor();
                    break ;
                case FrameFunction.AppointAdvisor: //任命军师
                    this.FrameFunction_Faction_AppointAdvisor();
                    break;
                case FrameFunction.SelectLandLink:
                    this.FrameFunction_Architecture_SelectLandLink();
                    break;
                case FrameFunction.SelectWaterLink:
                    this.FrameFunction_Architecture_SelectWaterLink();
                    break;

                case FrameFunction.MoveFeizi:
                    this.FrameFunction_Monarch_hougongTop_moveFeizi();
                    break;

                case FrameFunction.ReleaseFeizi:
                    this.FrameFunction_Monarch_hougongTop_releaseFeizi();
                    break;

                case FrameFunction.MoveCaptive: //俘虏可移动
                    this.FrameFunction_Faction_KillRelease_MoveCaptive();
                    break;
                case FrameFunction.PromoteNvGuan:
                    this.FrameFunction_Faction_PromoteNvGuan();
                    break;

                // 编辑器相关功能
                case FrameFunction.GetEditArchitecture:
                    this.FrameFunction_Editor_AfterGetEditArchitecture();
                    break;

                case FrameFunction.GetEditTroop:
                    this.FrameFunction_Editor_AfterGetEditTroop();
                    break;

                case FrameFunction.GetEditFaction:
                    this.FrameFunction_Editor_AfterGetEditFaction();
                    break;

                case FrameFunction.GetEditPerson:
                    this.FrameFunction_Editor_AfterGetEditPerson();
                    break;

                case FrameFunction.GetEditMilitary:
                    this.FrameFunction_Editor_AfterGetEditMilitary();
                    break;

                case FrameFunction.GetEditTreasure:
                    // this.FrameFunction_Editor_AfterGetEditTreasure();
                    break;

                case FrameFunction.GetEditTitle:
                    // this.FrameFunction_Editor_AfterGetEditTitle();
                    break;

                case FrameFunction.GetEditSkill:
                // this.FrameFunction_Editor_AfterGetEditSkill();
                break;
            case FrameFunction.Editor_SelectInfluence:
                this.FrameFunction_Editor_SelectInfluence();
                break;


            }
            this.lastFrameFunction = function;
        }
        public void FrameFunction_Architecture_SelectLandLink()
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.ArchitectureListWithoutSelf().GetSelectedList();
                if (this.CurrentGameObjects != null)
                {

                    this.CurrentArchitecture.ResetLandLink(this.CurrentGameObjects.GetList());

                }
            }
        }

        public void FrameFunction_Architecture_SelectWaterLink()
        {
            if (this.CurrentArchitecture != null)
            {
                this.CurrentGameObjects = this.CurrentArchitecture.ArchitectureListWithoutSelf().GetSelectedList();
                if (this.CurrentGameObjects != null)
                {

                    this.CurrentArchitecture.ResetWaterLink(this.CurrentGameObjects.GetList());

                }
            }
        }

        private void FrameFunction_Architecture_SelectPrince()//立储的作用
        {
            this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person ? (Person)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (this.CurrentPerson != null)
            {
                this.CurrentArchitecture.BelongedFaction.PrinceID = this.CurrentPerson.ID;
                this.CurrentArchitecture.DecreaseFund(Session.Parameters.SelectPrinceCost);
                this.CurrentArchitecture.SelectPrince(this.CurrentPerson);
                //this.mainGameScreen.xianshishijiantupian(this.CurrentArchitecture.BelongedFaction.Leader, this.CurrentPerson.Name, "SelectPrince", "", "", true );
                
            }
        }

        private void FrameFunction_Architecture_AppointMayor()  //太守
        {
            this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person ? (Person)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (this.CurrentPerson != null)
            {
                this.CurrentArchitecture.MayorID = this.CurrentPerson.ID;
                this.CurrentArchitecture.MayorOnDutyDays = 0;
                this.CurrentArchitecture.AppointMayor(this.CurrentPerson);
               
            }
        }

        private void FrameFunction_Faction_AppointAdvisor()  //军师
        {
            System.Diagnostics.Debug.WriteLine("[AppointAdvisor] ========== 开始执行任命军师逻辑 ==========");
            
            this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person ? (Person)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 选中的人物: {this.CurrentPerson?.Name ?? "null"}");
            
            if (this.CurrentPerson != null)
            {
                // 获取目标势力
                Faction faction = this.CurrentFaction ?? 
                                 this.CurrentArchitecture?.BelongedFaction ?? 
                                 Session.Current.Scenario.CurrentPlayer;
                System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 目标势力: {faction?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 势力君主: {faction?.Leader?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 当前军师: {faction?.Advisor?.Name ?? "无"}");
                System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 当前军师ID: {faction?.AdvisorID ?? -999}");
                
                if (faction != null && faction.Leader != null)
                {
                    // 如果已有军师，先清除
                    if (faction.AdvisorID > 0 && faction.Advisor != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 当前军师: {faction.Advisor.Name}，将被替换");
                        faction.Advisor = null;  // 使用Advisor属性清空，会同时清空AdvisorID和缓存
                        System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 清空后军师: {faction.Advisor?.Name ?? "无"}");
                        System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 清空后军师ID: {faction.AdvisorID}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 使用AdvisorAppointmentSystem任命军师: {this.CurrentPerson.Name}");
                    
                    // 使用AdvisorAppointmentSystem，显示轮流对话
                    bool success = WorldOfTheThreeKingdoms.GameManager.AdvisorAppointmentSystem.TryAppointAdvisor(
                        faction.Leader, 
                        this.CurrentPerson, 
                        faction
                    );
                    
                    System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 任命结果: {(success ? "成功" : "失败")}");
                    System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 任命后军师: {faction.Advisor?.Name ?? "无"}");
                    System.Diagnostics.Debug.WriteLine($"[AppointAdvisor] 任命后军师ID: {faction.AdvisorID}");
                    
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine("[AppointAdvisor] ✅ 任命成功");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[AppointAdvisor] ❌ 任命被拒绝");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AppointAdvisor] ❌ 错误：目标势力或君主为空");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[AppointAdvisor] ❌ 错误：未选中任何人物");
            }
            
            System.Diagnostics.Debug.WriteLine("[AppointAdvisor] ========== 任命军师逻辑结束 ==========");
        }

        private void FrameFunction_Architecture_ReleaseSelfPerson()
        {
            this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person ? (Person)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (this.CurrentPerson != null)
            {
                Session.MainGame.mainGameScreen.xianshishijiantupian(this.CurrentPerson.BelongedFaction.Leader, this.CurrentPerson.Name, TextMessageKind.ReleaseSelfPerson, "ReleaseSelfPerson", "", "", false );
                this.CurrentPerson.BeLeaveToNoFaction();
            }
        }

        private void FrameFunction_Architecture_KillCaptive()
        {
            Captive captive = new Captive();
            captive = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as Captive;
            if (captive != null)
            {
                Person leader = captive.BelongedFaction.Leader;

                Session.MainGame.mainGameScreen.OnExecute(leader, captive.CaptivePerson);
                captive.CaptivePerson.execute(captive.BelongedFaction);
            }
        }

        private void FrameFunction_Architecture_KillPerson()
        {
            this.CurrentPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person ? (Person)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (this.CurrentPerson != null)
            {
                Session.MainGame.mainGameScreen.xianshishijiantupian(Session.Current.Scenario.NeutralPerson, this.CurrentPerson.BelongedFaction.Leader.Name, "KillSelfPerson", "chuzhan.jpg", "chuzhan", this.CurrentPerson.Name, true);
                Person leader = this.CurrentPerson.BelongedFaction.Leader;
                this.CurrentPerson.execute(this.CurrentPerson.BelongedFaction);
            }
        }

        public void Initialize()
        {

        }


        public void SetTroopsPosition(Point position)
        {
            MilitaryList templist = new MilitaryList();
            foreach(Military military in this.CurrentMilitaries)
            {
                Person leader=new Person();
                PersonList persons=new PersonList();
                if(this.CurrentArchitecture.PersonsExcludeNvGuan.Count==0 || this.CurrentArchitecture.GetAllAvailableArea(false).Area.Count==0)
                {
                    break;
                }
                else if (this.CurrentArchitecture.PersonsExcludeNvGuan.Count > 0 && this.CurrentArchitecture.GetAllAvailableArea(false).Area.Count > 0)
                {
                    if (this.CurrentArchitecture.PersonsExcludeNvGuan.HasGameObject(military.FollowedLeader))
                    {
                        leader = military.FollowedLeader;
                    }
                    else if (this.CurrentArchitecture.PersonsExcludeNvGuan.HasGameObject(military.Leader))
                    {
                        leader = military.Leader;
                    }
                    else
                    {
                        templist.Add(military);
                        continue;
                    }
                    persons.Add(leader);
                    foreach (Person p in leader.preferredTroopPersons)
                    {
                        if (this.CurrentArchitecture.PersonsExcludeNvGuan.HasGameObject(p) && !persons.HasGameObject(p))
                        {
                            persons.Add(p);
                        }
                    }
                    Point point = Session.Current.Scenario.GetClosestPoint(this.CurrentArchitecture.GetAllAvailableArea(false),position);

                    // 🔧 修复：传入-1触发自动粮食分配，或传入具体数值
                    int troopFood = this.CurrentArchitecture.Food > military.FoodMax ? military.FoodMax : -1;
                    this.CurrentTroop = this.CurrentArchitecture.CreateTroop(persons, leader, military, troopFood, point);
                    
                    // Skip if troop creation failed
                    if (this.CurrentTroop == null)
                    {
                        continue;
                    }
                    
                    this.CurrentTroop.zijin = this.CurrentArchitecture.Fund > military.zijinzuidazhi ? military.zijinzuidazhi : 0;
                    this.CurrentTroop.ManualControl = true;
                    this.CurrentArchitecture.DecreaseFund(this.CurrentTroop.zijin);
                    if ((this.CurrentArchitecture.DefensiveLegion == null) || (this.CurrentArchitecture.DefensiveLegion.Troops.Count == 0))
                    {
                        this.CurrentArchitecture.CreateDefensiveLegion();
                    }
                    this.CurrentArchitecture.DefensiveLegion.AddTroop(this.CurrentTroop);
                    Session.MainGame.mainGameScreen.Plugins.PersonBubblePlugin.AddPerson(leader, this.CurrentTroop.Position, TextMessageKind.StartCampaign, "Campaign");
                    //int minlength = 9999;
                    //foreach (Point point2 in this.CurrentArchitecture.GetAllAvailableArea(false).Area)
                    //{
                    //    if(Math.Abs(point2.X-position.X)+Math.Abs())
                    //}
                }
            }
            foreach (Military military in templist)
            {
                Person leader = new Person();
                PersonList persons = new PersonList();
                if (this.CurrentArchitecture.PersonsExcludeNvGuan.Count == 0 || this.CurrentArchitecture.GetAllAvailableArea(false).Area.Count == 0)
                {
                    break;
                }
                else if (this.CurrentArchitecture.PersonsExcludeNvGuan.Count > 0 && this.CurrentArchitecture.GetAllAvailableArea(false).Area.Count > 0)
                {
                    if (this.CurrentArchitecture.PersonsExcludeNvGuan.HasGameObject(military.FollowedLeader))
                    {
                        leader = military.FollowedLeader;
                    }
                    else if (this.CurrentArchitecture.PersonsExcludeNvGuan.HasGameObject(military.Leader))
                    {
                        leader = military.Leader;
                    }
                    else
                    {
                        leader=this.CurrentArchitecture.GetMaxFightingForcePerson();
                    }
                    persons.Add(leader);
                    Point point = Session.Current.Scenario.GetClosestPoint(this.CurrentArchitecture.GetAllAvailableArea(false), position);

                    this.CurrentTroop = this.CurrentArchitecture.CreateTroop(persons, leader, military, this.CurrentArchitecture.Food > military.FoodMax ? military.FoodMax : 0, point);
                    
                    // Skip if troop creation failed
                    if (this.CurrentTroop == null)
                    {
                        continue;
                    }
                    
                    this.CurrentTroop.zijin = this.CurrentArchitecture.Fund > military.zijinzuidazhi ? military.zijinzuidazhi : 0;
                    this.CurrentTroop.ManualControl = true;
                    this.CurrentArchitecture.DecreaseFund(this.CurrentTroop.zijin);
                    if ((this.CurrentArchitecture.DefensiveLegion == null) || (this.CurrentArchitecture.DefensiveLegion.Troops.Count == 0))
                    {
                        this.CurrentArchitecture.CreateDefensiveLegion();
                    }
                    this.CurrentArchitecture.DefensiveLegion.AddTroop(this.CurrentTroop);
                    Session.MainGame.mainGameScreen.Plugins.PersonBubblePlugin.AddPerson(leader, this.CurrentTroop.Position, TextMessageKind.StartCampaign, "Campaign");
                    //int minlength = 9999;
                    //foreach (Point point2 in this.CurrentArchitecture.GetAllAvailableArea(false).Area)
                    //{
                    //    if(Math.Abs(point2.X-position.X)+Math.Abs())
                    //}
                }
            }
        }

        public void SetCreatingTroopPosition(Point position)
        {
            // 🔥 修复：玩家手动创建部队，传入 playerManual=true
            this.CurrentTroop = this.CurrentArchitecture.CreateTroop(this.CurrentGameObjects, this.CurrentPerson, this.CurrentMilitary, this.CurrentNumber, position, assignedLegion: null, silent: false, playerManual: true);
            
            // If troop creation failed (e.g., no valid persons), abort gracefully
            if (this.CurrentTroop == null)
            {
                System.Diagnostics.Debug.WriteLine("[SetCreatingTroopPosition] Troop creation failed - CurrentTroop is null. Persons list may contain only Military objects.");
                return;
            }
            
            this.CurrentTroop.zijin = this.Currentzijin;
            // 🔥 修复：不再需要手动设置 ManualControl，Troop.Create 已经处理
            // this.CurrentTroop.ManualControl = true;
            this.CurrentArchitecture.DecreaseFund(this.CurrentTroop.zijin);
            
            // 🔥 修复：不再手动加入防守军团，Troop.Create 已经分配到玩家手动控制军团
            // if ((this.CurrentArchitecture.DefensiveLegion == null) || (this.CurrentArchitecture.DefensiveLegion.Troops.Count == 0))
            // {
            //     this.CurrentArchitecture.CreateDefensiveLegion();
            // }
            // this.CurrentArchitecture.DefensiveLegion.AddTroop(this.CurrentTroop);
            
            // this.CurrentArchitecture.PostCreateTroop(this.CurrentTroop, true);
            Session.MainGame.mainGameScreen.Plugins.PersonBubblePlugin.AddPerson(this.CurrentPerson, this.CurrentTroop.Position, TextMessageKind.StartCampaign, "Campaign");
            //this.mainGameScreen.Plugins.AirViewPlugin.ReloadTroopView();
        }

        public void ArchitectureExpand()
        {
            this.CurrentArchitecture.Expand();
        }

        #region 编辑器相关功能

        /// <summary>
        /// 处理选择城池进行编辑
        /// </summary>
        private void FrameFunction_Editor_AfterGetEditArchitecture()
        {
            var selectedArchitecture = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Architecture ? (Architecture)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (selectedArchitecture != null && Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin != null)
            {
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetEditTarget(selectedArchitecture);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetPosition(ShowPosition.Center);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.IsShowing = true;
            }
        }

        /// <summary>
        /// 处理选择部队进行编辑
        /// </summary>
        private void FrameFunction_Editor_AfterGetEditTroop()
        {
            var selectedTroop = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Troop ? (Troop)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (selectedTroop != null && Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin != null)
            {
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetEditTarget(selectedTroop);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetPosition(ShowPosition.Center);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.IsShowing = true;
            }
        }

        /// <summary>
        /// 处理选择势力进行编辑
        /// </summary>
        private void FrameFunction_Editor_AfterGetEditFaction()
        {
            var selectedFaction = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Faction ? (Faction)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (selectedFaction != null && Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin != null)
            {
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetEditTarget(selectedFaction);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetPosition(ShowPosition.Center);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.IsShowing = true;
            }
        }

        /// <summary>
        /// 处理选择武将进行编辑
        /// </summary>
        private void FrameFunction_Editor_AfterGetEditPerson()
        {
            var selectedPerson = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Person ? (Person)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (selectedPerson != null && Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin != null)
            {
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetEditTarget(selectedPerson);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetPosition(ShowPosition.Center);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.IsShowing = true;
            }
        }

        /// <summary>
        /// 处理选择编队进行编辑
        /// </summary>
        private void FrameFunction_Editor_AfterGetEditMilitary()
        {
            var selectedMilitary = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Military ? (Military)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (selectedMilitary != null && Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin != null)
            {
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetEditTarget(selectedMilitary);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.SetPosition(ShowPosition.Center);
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.IsShowing = true;
            }
        }

        /*
        /// <summary>
        /// 处理选择宝物编辑
        /// </summary>
        private void FrameFunction_Editor_AfterGetEditTreasure()
        {
            var selectedTreasure = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem is Treasure ? (Treasure)Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem : null;
            if (selectedTreasure != null && Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin != null)
            {
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.ShowGlobalTreasureEditMenu(selectedTreasure);
            }
        }

        /// <summary>
        /// 处理选择称号编辑
        /// </summary>
        private void FrameFunction_Editor_AfterGetEditTitle()
        {
            var selectedTitle = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as Title;
            if (selectedTitle != null && Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin != null)
            {
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.ShowGlobalTitleEditMenu(selectedTitle);
            }
        }

        /// <summary>
        /// 处理选择特技编辑
        /// </summary>
        private void FrameFunction_Editor_AfterGetEditSkill()
        {
            var selectedSkill = Session.MainGame.mainGameScreen.Plugins.TabListPlugin.SelectedItem as Skill;
            if (selectedSkill != null && Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin != null)
            {
                Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.ShowGlobalSkillEditMenu(selectedSkill);
            }
        }
        */

        private void FrameFunction_Editor_SelectInfluence()
        {
            if (Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin != null)
            {
                 var items = new System.Collections.Generic.List<object>();
                 var tabPlugin = Session.MainGame.mainGameScreen.Plugins.TabListPlugin;

                 // TabListPlugin.SelectedItemList is usually GameObjectList, which is not List<object> but is IEnumerable
                 if (tabPlugin.SelectedItemList is System.Collections.IEnumerable list)
                 {
                     foreach (var item in list)
                     {
                         items.Add(item);
                     }
                 }
                 // If SelectedItemList is null or empty, try single selection
                 if (items.Count == 0 && tabPlugin.SelectedItem != null)
                 {
                     items.Add(tabPlugin.SelectedItem);
                 }

                 Session.MainGame.mainGameScreen.Plugins.InGameEditorPlugin.FinishInfluenceSelection(items);
            }
        }

        #endregion

        
        
    }

 

}

