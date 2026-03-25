using WorldOfTheThreeKingdoms.GameGlobal;  // 🔥 添加：用于 GenerateUIAccessor 特性
using GameManager;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;
using GameObjects.SectionDetail;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace GameObjects
{
    [DataContract]
    [GenerateUIAccessor]  // 🔥 添加源生成器特性，支持军区列表和右键菜单访问
    public partial class Section : GameObject
    {
        private SectionAIDetail aiDetail;

        // 军团长/都督相关
        private Person sectionLeader;

        // 添加一个计时器或帧计数器
        [DataMember]
        private int _aiCooldownCounter = 0;
        private const int AI_EXECUTION_INTERVAL = 2; // 恢复到2回合正常间隔
        private const int AI_URGENT_INTERVAL = 1;    // 紧急事件1回合快速响应
        
        // ==========================================
        // AI 控制器：负责管理时间、事件和规模过滤
        // ==========================================
        [DataMember]
        private bool _isDirty = false;        // 脏标记：是否有突发事件
        [DataMember]
        private bool _isInitialized = false;  // 是否已初始化（错峰）
        [DataMember]
        private int _urgentCooldown = 0;      // 紧急事件冷却
        
        // 新建军区优先调配
        [DataMember]
        private int _creationTurn = -1;       // 军区建立回合数
        private const int NEW_SECTION_PRIORITY_TURNS = 3; // 新建军区优先期：3回合

        [DataMember]
        public int BelongedFactionID { get; set; } = -1;

        // 🔥 修复：AIDetail 的 ID 字段
        // 日期：2026-02-13
        // 历史：原名为 AIDetailIDString（命名不当，实际是 int），现在统一为 AIDetailID
        // 为了向后兼容旧存档，保留 AIDetailIDString 作为别名
        [DataMember]
        public int AIDetailID { get; set; } = -1;

        // 向后兼容：旧存档使用 AIDetailIDString
        [DataMember]
        [JsonInclude]
        [JsonPropertyName("AIDetailIDString")]
        public int AIDetailIDString_Legacy
        {
            get => AIDetailID;
            set => AIDetailID = value;
        }

        /// <summary>
        /// 军团长/都督/提督
        /// 他的性格将决定整个军区的 AI 风格
        /// </summary>
        public Person SectionLeader
        {
            get { return sectionLeader; }
            set
            {
                sectionLeader = value;
                SectionLeaderID = value?.ID ?? -1;
            }
        }

        /// <summary>
        /// 军团长名称字符串（用于UI显示）
        /// </summary>
        public string SectionLeaderString
        {
            get
            {
                return sectionLeader?.Name ?? "----";
            }
        }

        // Removed obsolete ArchitecturesString - use ArchitectureIDs instead
        // 🔥 但是需要在反序列化时处理旧格式的剧本文件
        [DataMember]
        [JsonInclude]
        [JsonPropertyName("ArchitecturesString")]
        public string ArchitecturesString_Legacy
        {
            get => null; // 不再使用
            set
            {
                // 🔥 修复：从旧格式的ArchitecturesString转换为ArchitectureIDs
                if (!string.IsNullOrWhiteSpace(value))
                {
                    ArchitectureIDs = new List<int>();
                    var ids = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var idStr in ids)
                    {
                        if (int.TryParse(idStr, out int id))
                        {
                            ArchitectureIDs.Add(id);
                        }
                    }
                    // 延迟日志输出，避免Name为null
                    // System.Diagnostics.Debug.WriteLine($"[Section] {Name}(ID:{ID}) 从 ArchitecturesString 转换了 {ArchitectureIDs.Count} 个建筑ID");
                }
            }
        }

        [DataMember]
        [JsonInclude]
        public List<int> ArchitectureIDs { get; set; } = new List<int>();

        public ArchitectureList Architectures = new ArchitectureList();
        public Faction BelongedFaction;
        public Faction OrientationFaction;
        [DataMember]
        public int OrientationFactionID;
        public Section OrientationSection;
        [DataMember]
        public int OrientationSectionID;
        public State OrientationState;
        [DataMember]
        public int OrientationStateID;
        public Architecture OrientationArchitecture;
        [DataMember]
        public int OrientationArchitectureID;

        public void Init()
        {
            Architectures = new ArchitectureList();

        }

        public void EnsureSectionArchitecture()
        {
            foreach (Architecture a in Session.Current.Scenario.Architectures)
            {
                if (a.BelongedSection == this && !this.Architectures.GameObjects.Contains(a))
                {
                    this.Architectures.Add(a);
                }
            }
        }

        public void AddArchitecture(Architecture architecture)
        {
            this.Architectures.Add(architecture);
            architecture.BelongedSection = this;
        }

        public void AI(GameTime gameTime)
        {
            // 🔍 调试输出：记录军区AI调用（已关闭，输出太多）
            /*
            System.Diagnostics.Debug.WriteLine($"");
            System.Diagnostics.Debug.WriteLine($"🏰 ═══════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"🏰 [军区AI触发] 军区: {this.Name} (ID:{this.ID})");
            System.Diagnostics.Debug.WriteLine($"🏰 所属势力: {this.BelongedFaction?.Name ?? "无"}");
            System.Diagnostics.Debug.WriteLine($"🏰 管辖城市: {this.Architectures?.Count ?? 0}个");
            System.Diagnostics.Debug.WriteLine($"🏰 AutoRun: {this.AIDetail?.AutoRun ?? false}");
            System.Diagnostics.Debug.WriteLine($"🏰 ═══════════════════════════════════════════════════════════");
            */
            
#if DEBUG
            // System.Diagnostics.Debug.WriteLine($"[Section.AI] 军区AI入口: {this.Name} (ID:{this.ID})");
#endif
            try
            {
                // 1. 基础检查
                // 🔥 修复：使用属性而不是字段，确保自动初始化
                if (this.AIDetail == null || this.BelongedFaction == null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[Section.AI] ABORT {this.Name} - AIDetail或Faction为null");
#endif
                    return;
                }

                // 2. 【关键修复】：回合控制检查
                // 军区AI应该跟随玩家回合节奏，只有在玩家结束回合时才执行
                if (Session.Current.Scenario.IsPlayer(this.BelongedFaction))
                {
                    // 玩家势力的军区：只有在玩家点击"结束回合"时才执行
                    if (!this.BelongedFaction.Passed)
                    {
                        // 🔥 修复：玩家未结束回合时，不要递减冷却计数器
                        // 日期：2026-03-23
                        // 原因：冷却计数器应该只在 ShouldExecuteAI() 中递减
                        //       如果在这里递减，会导致每次玩家点击"进行"时计数器都被递减
                        //       结果：玩家结束回合时，计数器已经是0，导致每回合都执行AI
                        // 解决：直接返回，不递减计数器
                        return;
                    }
                }
                else
                {
                    // AI势力的军区：跟随AI势力的回合节奏
                    // AI势力通常在Faction.Run()中处理，这里可以正常执行
                    System.Diagnostics.Debug.WriteLine($"🏰 [AI势力] {this.Name} - 无需检查Passed状态");
                }

                // 3. 【AI控制器】：智能调度检查
                if (!ShouldExecuteAI())
                {
                    System.Diagnostics.Debug.WriteLine($"🏰 [跳过] {this.Name} - ShouldExecuteAI() 返回 false（时间控制）");
                    return;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🏰 [通过] {this.Name} - ShouldExecuteAI() 返回 true");
                }

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[Section.AI] {this.Name} 开始执行军区级AI逻辑. AutoRun={this.AIDetail.AutoRun}");
#endif

                // 确保 Architectures 列表是最新的
                this.EnsureSectionArchitecture();

                if (this.AIDetail.AutoRun)
                {
                    System.Diagnostics.Debug.WriteLine($"🏰 [执行] {this.Name} - AutoRun=true，开始执行军区AI");
                    // 执行AI逻辑
                    RunSectionAI();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"🏰 [跳过] {this.Name} - AutoRun=false，不执行军区AI");
                }

                // 5. 执行其他轻量级AI逻辑 (比如任命军团长等)
                AILightweight();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[🔴 Section.AI ERROR] 军区: {this.Name} (ID:{this.ID}, 势力:{this.BelongedFaction?.Name})\n异常: {ex.Message}\n堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 【AI控制器】：智能调度判断
        /// 结合时间间隔、事件驱动、规模过滤和新建军区优先调配
        /// </summary>
        private bool ShouldExecuteAI()
        {
            // 初始化：错峰出行 (防止开局卡顿)
            if (!_isInitialized)
            {
                InitializeAI();
            }

            // 【新增】：新建军区优先调配检查
            bool isNewSection = IsNewlyCreatedSection();
            
            // 判定是否需要执行 AI（先判断，再递减计数器）
            bool timeUp = _aiCooldownCounter <= 0;  // 正常时间到了
            bool urgentAndReady = _isDirty && (_urgentCooldown <= 0);  // 紧急事件且冷却结束
            bool newSectionPriority = isNewSection && timeUp;  // 🔥 修复：新建军区也要遵守冷却时间

            if (timeUp || urgentAndReady)
            {
                // 重置计时器和标记
                if (timeUp)
                {
                    // 🔥 修复：新建军区使用更短的间隔（1回合），普通军区使用正常间隔（2回合）
                    _aiCooldownCounter = isNewSection ? 1 : AI_EXECUTION_INTERVAL;
                }
                
                if (urgentAndReady)
                {
                    _urgentCooldown = AI_URGENT_INTERVAL; // 紧急事件1回合冷却
                    _isDirty = false; // 清除脏标记
                }

#if DEBUG
                string reason = urgentAndReady ? "紧急事件" : 
                               newSectionPriority ? "新建军区优先调配" : 
                               "定时执行";
                System.Diagnostics.Debug.WriteLine($"[Section.AI] {this.Name} 触发AI执行 - 原因: {reason}");
#endif
                return true;
            }

            // 🔥 修复：只有在不执行AI时才递减计数器
            _aiCooldownCounter--;
            _urgentCooldown--;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Section.AI] COOLDOWN {this.Name} - 正常:{_aiCooldownCounter}回合, 紧急:{_urgentCooldown}回合, 脏标记:{_isDirty}, 新建:{isNewSection}");
#endif
            return false;
        }

        /// <summary>
        /// 【AI控制器】：错峰初始化
        /// 给每个军区一个随机初始偏移，防止同时执行造成卡顿
        /// </summary>
        private void InitializeAI()
        {
            // 记录军区建立回合（如果还没有记录的话）
            if (_creationTurn == -1 && Session.Current?.Scenario != null)
            {
                // 使用DaySince转换为回合数（假设每回合30天）
                _creationTurn = Session.Current.Scenario.DaySince / 30;
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[Section.AI] {this.Name} 记录建立回合: 第{_creationTurn}回合 (DaySince:{Session.Current.Scenario.DaySince})");
#endif
            }

            // 给每个军区一个随机初始偏移
            // 但新建军区不需要偏移，立即可以执行
            if (IsNewlyCreatedSection())
            {
                _aiCooldownCounter = 0; // 新建军区立即执行
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[Section.AI] {this.Name} 新建军区，立即执行AI");
#endif
            }
            else
            {
                _aiCooldownCounter = GameObject.Random(0, AI_EXECUTION_INTERVAL);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[Section.AI] {this.Name} 初始化AI控制器 - 初始偏移: {_aiCooldownCounter}回合");
#endif
            }
            
            _urgentCooldown = 0;
            _isInitialized = true;
        }

        /// <summary>
        /// 【新建军区检查】：判断是否为新建立的军区（3回合内）
        /// </summary>
        private bool IsNewlyCreatedSection()
        {
            if (_creationTurn == -1 || Session.Current?.Scenario == null)
            {
                return false;
            }

            // 计算军区建立至今的回合数
            int currentTurn = Session.Current.Scenario.DaySince / 30; // 假设每回合30天
            int turnsSinceCreation = currentTurn - _creationTurn;

#if DEBUG
            if (turnsSinceCreation <= NEW_SECTION_PRIORITY_TURNS)
            {
                System.Diagnostics.Debug.WriteLine($"[Section.AI] {this.Name} 新建军区检查: 建立{turnsSinceCreation}回合，仍在优先期内");
            }
#endif

            return turnsSinceCreation <= NEW_SECTION_PRIORITY_TURNS;
        }

        /// <summary>
        /// 【事件驱动接口】：外部调用通知紧急事件
        /// 当发生丢城、武将变动、前线告急时调用此方法
        /// </summary>
        public void NotifyUrgentEvent()
        {
            _isDirty = true;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Section.AI] {this.Name} 收到紧急事件通知");
#endif
        }
        
        // 🔥 修复：添加AI冷却状态访问器（用于序列化）
        // 日期：2026-03-21
        // 原因：SaveDataPhase 需要访问 private 字段来保存AI冷却状态
        // 解决：提供只读访问器方法
        internal int GetAiCooldownCounter() => _aiCooldownCounter;
        internal bool GetIsDirty() => _isDirty;
        internal bool GetIsInitialized() => _isInitialized;
        internal int GetUrgentCooldown() => _urgentCooldown;
        internal int GetCreationTurn() => _creationTurn;
        
        // 🔥 修复：添加AI冷却状态设置器（用于反序列化）
        // 日期：2026-03-21
        // 原因：LoadDataPhase 需要设置 private 字段来恢复AI冷却状态
        // 解决：提供内部设置器方法（同时支持外部调用）
        internal void SetAiCooldownCounter(int value) => _aiCooldownCounter = value;
        internal void SetIsDirty(bool value) => _isDirty = value;
        internal void SetIsInitialized(bool value) => _isInitialized = value;
        internal void SetUrgentCooldown(int value) => _urgentCooldown = value;
        
        /// <summary>
        /// 设置军区建立回合（用于序列化和创建军区时调用）
        /// </summary>
        internal void SetCreationTurn(int value)
        {
            _creationTurn = value;
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Section.AI] {this.Name} 设置建立回合: 第{_creationTurn}回合");
#endif
        }

        /// <summary>
        /// 【核心逻辑】：执行军区AI，包含规模过滤
        /// </summary>
        private void RunSectionAI()
        {
            var myCities = this.Architectures.Cast<Architecture>().ToList();
            int cityCount = myCities.Count;

#if DEBUG
            // 🚨 异常追踪：记录AI执行前的资源状态
            var resourcesBefore = myCities.ToDictionary(c => c.Name, c => new { Fund = c.Fund, Food = c.Food });
            int totalFundBefore = myCities.Sum(c => c.Fund);
            int totalFoodBefore = myCities.Sum(c => c.Food);
            
            System.Diagnostics.Debug.WriteLine($"[🔍 Section.RunSectionAI] {this.Name} 管辖城市数: {cityCount}");
            System.Diagnostics.Debug.WriteLine($"[🔍 Section.RunSectionAI] AI执行前总资源: 资金={totalFundBefore:N0}, 粮食={totalFoodBefore:N0}");
#endif

            // 【规模过滤】: 如果只有 1 座城，使用简化逻辑
            if (cityCount <= 1)
            {
                RunSimpleDefense(myCities);
                return;
            }

            // --- 多城军区：执行完整逻辑 ---
            try
            {
                // 🔥 NOTE: 资源下沉已移至 RunResourceLogistics() 统一执行，避免重复调用
                
#if DEBUG
                // 🚨 检查AI执行前的状态
                int fundBeforePersonnel = myCities.Sum(c => c.Fund);
                int foodBeforePersonnel = myCities.Sum(c => c.Food);
                System.Diagnostics.Debug.WriteLine($"[🔍 Section.RunSectionAI] 开始执行AI: 资金={fundBeforePersonnel:N0}, 粮食={foodBeforePersonnel:N0}");
#endif
                
                // B. 借用全局大脑 (复用 Faction 代码)
                System.Diagnostics.Debug.WriteLine($"🏰 [军区人员调配] {this.Name} 开始调用势力级人员调配");
                System.Diagnostics.Debug.WriteLine($"🏰 调配范围: {myCities.Count}个城市 - {string.Join(", ", myCities.Select(c => c.Name))}");
                
                this.BelongedFaction.RunPersonnel(myCities); // 局部调人
                
                System.Diagnostics.Debug.WriteLine($"🏰 [军区人员调配] {this.Name} 人员调配完成");
                
#if DEBUG
                int fundAfterPersonnel = myCities.Sum(c => c.Fund);
                int foodAfterPersonnel = myCities.Sum(c => c.Food);
                System.Diagnostics.Debug.WriteLine($"[🔍 Section.RunSectionAI] RunPersonnel后: 资金={fundAfterPersonnel:N0}, 粮食={foodAfterPersonnel:N0}");
#endif
                
                this.BelongedFaction.RunDomestic(myCities);  // 局部内政
                
#if DEBUG
                int fundAfterDomestic = myCities.Sum(c => c.Fund);
                int foodAfterDomestic = myCities.Sum(c => c.Food);
                System.Diagnostics.Debug.WriteLine($"[🔍 Section.RunSectionAI] RunDomestic后: 资金={fundAfterDomestic:N0}, 粮食={foodAfterDomestic:N0}");
#endif
                
                this.BelongedFaction.RunMilitary(myCities);  // 局部出兵
                
#if DEBUG
                int fundAfterMilitary = myCities.Sum(c => c.Fund);
                int foodAfterMilitary = myCities.Sum(c => c.Food);
                System.Diagnostics.Debug.WriteLine($"[🔍 Section.RunSectionAI] RunMilitary后: 资金={fundAfterMilitary:N0}, 粮食={foodAfterMilitary:N0}");
#endif
                
                // C. 军区级县令任命：委任军区应该自动任命县令

                
                this.BelongedFaction.AutoAppointMayor(myCities); // 军区内县令任命
                

                
                // D. 军区级增强功能
                this.RunResourceLogistics();              // V8.4 全链路物流
                this.ManageMilitaryLogistics(myCities);   // 军备调配 (保持独立)
                this.AICoordinatedAttacks(myCities);         // 协同攻击

#if DEBUG
                // 🚨 最终检查：对比AI执行前后的资源变化
                int totalFundAfter = myCities.Sum(c => c.Fund);
                int totalFoodAfter = myCities.Sum(c => c.Food);
                
                System.Diagnostics.Debug.WriteLine($"[🔍 Section.RunSectionAI] AI执行完成: 资金 {totalFundBefore:N0}→{totalFundAfter:N0} 粮食 {totalFoodBefore:N0}→{totalFoodAfter:N0}");
                
                // 检查每个城市的资源变化
                foreach (var city in myCities)
                {
                    var before = resourcesBefore[city.Name];
                    if (city.Fund == 0 && before.Fund > 100000)
                    {
                        System.Diagnostics.Debug.WriteLine($"[🔴 CRITICAL ERROR] {city.Name} 资金异常归零! {before.Fund:N0} → 0");
                    }
                    if (city.Food == 0 && before.Food > 100000)
                    {
                        System.Diagnostics.Debug.WriteLine($"[🔴 CRITICAL ERROR] {city.Name} 粮食异常归零! {before.Food:N0} → 0");
                    }
                    if (Math.Abs(city.Fund - before.Fund) > 1000000 || Math.Abs(city.Food - before.Food) > 1000000)
                    {
                        System.Diagnostics.Debug.WriteLine($"[🔴 CRITICAL ERROR] {city.Name} 资源异常变化! 资金:{before.Fund:N0}→{city.Fund:N0} 粮食:{before.Food:N0}→{city.Food:N0}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[Section.RunSectionAI] {this.Name} 完整军区AI执行完成");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[🔴 Section.RunSectionAI ERROR] 军区: {this.Name}\n异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 【简化逻辑】：单城模式，只执行基础防御
        /// </summary>
        private void RunSimpleDefense(List<Architecture> cities)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Section.RunSimpleDefense] {this.Name} 执行单城简化逻辑");
#endif

            try
            {
                // 单城模式只跑最简单的检查
                this.DistributeResourcesToCities(cities);    // 确保有资源
                this.BelongedFaction.RunMilitary(cities);    // 基础军事防御
                
                // 跳过复杂的人员调配和物资调配，因为单城不需要
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[🔴 Section.RunSimpleDefense ERROR] 军区: {this.Name}\n异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 轻量级AI逻辑，每回合都执行
        /// 包括军团长任命、基础管理等不耗时的操作
        /// </summary>
        public void AILightweight()
        {
            System.Diagnostics.Debug.WriteLine($"[Diagnostic] Section.AILightweight EXECUTION for {this.Name}"); 
#if DEBUG
            if (SectionAIHelper.EnableDebugOutput)
            {
                System.Diagnostics.Debug.WriteLine("[Section.AILightweight] 军区 " + this.Name + " 执行轻量级AI");
            }
#endif
            
            // 检查并自动任命军团长（如果没有的话）
            if (this.SectionLeader == null)
            {
                AutoAppointLeader();
            }
            
            // 其他轻量级逻辑可以在这里添加
            // 例如：检查城市状态、基础资源管理等
        }

        /// <summary>
        /// 手动任命军团长
        /// </summary>
        /// <param name="person">候选武将</param>
        /// <returns>是否任命成功</returns>
        public bool AppointLeader(Person person)
        {
            if (person == null) return false;

            // 校验 1: 武将必须属于本势力
            if (person.BelongedFaction != this.BelongedFaction) return false;

            // 校验 2: 武将所在的城市必须属于本军区
            if (person.LocationArchitecture == null || 
                !this.Architectures.HasGameObject(person.LocationArchitecture))
            {
                return false;
            }

            this.SectionLeader = person;
            
            // 触发赴任补丁
            EnsureLeaderInJurisdiction(person);
            
            return true;
        }

        /// <summary>
        /// 自动推举/补位逻辑
        /// 当军团长战死、被俘或新建军区时，自动选出一个能力最强的
        /// </summary>
        public void AutoSelectLeader()
        {
            // 如果当前军团长还健在且属于本军区，就不换人
            if (this.sectionLeader != null &&
                this.sectionLeader.BelongedFaction == this.BelongedFaction &&
                this.sectionLeader.LocationArchitecture != null &&
                this.Architectures.HasGameObject(this.sectionLeader.LocationArchitecture))
            {
                return;
            }

            // 寻找最佳人选：统率(Command)最高，或者官职/功绩(Merit)最高
            Person bestCandidate = null;
            int maxScore = -1;

            foreach (Architecture arch in this.Architectures)
            {
                foreach (Person p in arch.Persons)
                {
                    // 排除俘虏、未出仕等情况
                    if (p.Status != PersonStatus.Normal) continue;

                    // 评分标准：优先看统率 (Command) + 政治 (Politics) 综合
                    // 你也可以改为只看功绩 (Merit)
                    int score = p.Command + p.Politics;

                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestCandidate = p;
                    }
                }
            }

            if (bestCandidate != null)
            {
                this.sectionLeader = bestCandidate;
                
                // 触发赴任补丁
                EnsureLeaderInJurisdiction(bestCandidate);
            }
        }

        /// <summary>
        /// 补丁：任命成功后，强制将军团长移动到军区治所（一般是军区内最大的城市）
        /// </summary>
        private void EnsureLeaderInJurisdiction(Person leader)
        {
            if (leader == null) return;

            // 如果人已经在军区内的某个城市里了，就不折腾了
            if (leader.LocationArchitecture != null && this.Architectures.HasGameObject(leader.LocationArchitecture))
            {
                return;
            }

            // 寻找治所：军区内人口最多
            Architecture capital = null;
            int maxPop = -1;
            foreach (Architecture a in this.Architectures)
            {
                if (a.Population > maxPop)
                {
                    maxPop = a.Population;
                    capital = a;
                }
            }

            if (capital != null)
            {
                // 移动逻辑
                leader.LocationArchitecture = capital;
            }
        }

        /// <summary>
        /// 从存档恢复军团长引用（在场景加载时调用）
        /// </summary>
        public void RestoreSectionLeader()
        {
            // 🔧 FIX: AI 势力的军区不应该有都督，只有玩家势力的军区才有都督
            bool isPlayerFaction = this.BelongedFaction != null && 
                                   Session.Current.Scenario.CurrentPlayer != null && 
                                   this.BelongedFaction == Session.Current.Scenario.CurrentPlayer;
            
            // 如果不是玩家势力，直接跳过
            if (!isPlayerFaction)
            {
                // 清空 AI 势力军区的都督
                this.sectionLeader = null;
                this.SectionLeaderID = -1;
                System.Diagnostics.Debug.WriteLine($"[RestoreSectionLeader] ⚠️ 跳过AI势力军区: {this.Name} (势力:{this.BelongedFaction?.Name})");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[RestoreSectionLeader] 玩家军区:{this.Name}, LeaderID:{this.SectionLeaderID}");
            
            // 🔧 FIX: 如果 LeaderID 无效（0 或负数），清空都督
            if (this.SectionLeaderID <= 0)
            {
                this.sectionLeader = null;
                this.SectionLeaderID = -1;
                System.Diagnostics.Debug.WriteLine($"[RestoreSectionLeader] ⚠️ LeaderID无效({this.SectionLeaderID})，清空都督");
                return;
            }
            
            // 尝试获取都督
            Person leader = Session.Current.Scenario.Persons.GetGameObject(this.SectionLeaderID) as Person;
            
            if (leader == null)
            {
                // 找不到这个人，清空都督
                this.sectionLeader = null;
                this.SectionLeaderID = -1;
                System.Diagnostics.Debug.WriteLine($"[RestoreSectionLeader] ❌ 找不到ID={this.SectionLeaderID}的Person，清空都督");
                return;
            }
            
            // 🔧 FIX: 验证都督是否属于该势力
            if (leader.BelongedFaction != this.BelongedFaction)
            {
                // 都督不属于该势力，清空都督
                this.sectionLeader = null;
                this.SectionLeaderID = -1;
                System.Diagnostics.Debug.WriteLine($"[RestoreSectionLeader] ❌ 都督 {leader.Name} 不属于势力 {this.BelongedFaction?.Name}（属于:{leader.BelongedFaction?.Name}），清空都督");
                return;
            }
            
            // 验证通过，恢复都督
            this.sectionLeader = leader;
            System.Diagnostics.Debug.WriteLine($"[RestoreSectionLeader] ✅ 成功恢复都督: {leader.Name}");
        }

        // 优化后的自动任命入口
        public void AutoAppointLeader()
        {
            if (this.AIDetail != null && !this.AIDetail.AutoRun && this.SectionLeader != null) return;

            // 1. 获取所有候选人（排除君主）
            var candidates = new List<Person>();
            foreach (Architecture arch in this.Architectures)
            {
                foreach (Person p in arch.Persons)
                {
                    if (p.Status == PersonStatus.Normal && 
                        p.Alive && 
                        (this.BelongedFaction == null || p != this.BelongedFaction.Leader))
                    {
                        candidates.Add(p);
                    }
                }
            }

            if (candidates.Count == 0) return;

            Person bestCandidate = null;
            float maxScore = -9999f;

            // 2. 遍历打分
            foreach (var p in candidates)
            {
                // 计算匹配度
                float score = CalculateLeaderSuitability(p);

                // 增加一点随机扰动(0~5分)，模拟君主喜好，避免每次数值完全一样太死板
                score += (float)GameObject.Random(5);

                if (score > maxScore)
                {
                    maxScore = score;
                    bestCandidate = p;
                }
            }

            // 3. 换人判断：为了维持稳定，新人的分数必须显著高于现任(比如高20%)才能换人
            if (this.SectionLeader == null)
            {
                this.SectionLeader = bestCandidate;

                // 触发赴任补丁
                EnsureLeaderInJurisdiction(bestCandidate);
            }
            else if (bestCandidate != null && bestCandidate != this.SectionLeader)
            {
                float currentScore = CalculateLeaderSuitability(this.SectionLeader);
                // 稳定性阈值：新人必须比旧人强 20% 以上，或者旧人忠诚度太低
                if (maxScore > currentScore * 1.2f || this.SectionLeader.PersonalLoyalty < 80)
                {
                    this.SectionLeader = bestCandidate;

                    // 触发赴任补丁
                    EnsureLeaderInJurisdiction(bestCandidate);
                }
            }
        }

        /// <summary>
        /// 核心优化：计算武将与当前军团方针的"契合度"
        /// </summary>
        private float CalculateLeaderSuitability(Person p)
        {
            float score = 0;

            // --- A. 基础素质 (Base Capability) ---
            // 无论干什么，能力强、地位高总是好的
            score += (p.Command + p.Politics + p.Intelligence + p.Glamour) / 4.0f;
            score += p.Merit / 200.0f; // 资历分
            score += p.PersonalLoyalty; // 忠诚分（防止叛变）

            // --- B. 战略方针匹配 (Mission Fit) ---
            if (this.AIDetail != null)
            {
                switch (this.AIDetail.OrientationKind)
                {
                    case SectionOrientationKind.势力:      // 攻略势力 (全面战争)
                    case SectionOrientationKind.建筑:      // 攻略据点 (局部进攻)
                    case SectionOrientationKind.州域:      // 攻略州域 (区域压制)

                        // 进攻型任务：看重统率、武力
                        score += p.Command * 1.5f;
                        score += p.Strength * 0.5f;
                        break;

                    case SectionOrientationKind.军区:      // 支援军区 (后勤/协防)

                        // 支援任务：看重智力(不中埋伏)、统率(行军速度/防御)
                        score += p.Intelligence * 1.2f;
                        score += p.Command * 1.0f;
                        break;

                    case SectionOrientationKind.无:         // 委任统治 (种田/发育)
                    default:

                        // 内政任务：看重政治、魅力
                        score += p.Politics * 1.5f;
                        score += p.Glamour * 1.0f;
                        break;
                }
            }
            else
            {
                // 默认：内政任务
                score += p.Politics * 1.5f;
                score += p.Glamour * 1.0f;
            }

            // --- C. 相性匹配 (Chemistry) ---
            // 如果有君主对象，判断与君主的相性 (Ideal 是 0-150 的圆环值)
            // 距离越近，分越高
            if (this.BelongedFaction != null && this.BelongedFaction.Leader != null)
            {
                int diff = Math.Abs(p.Ideal - this.BelongedFaction.Leader.Ideal);
                if (diff > 75) diff = 150 - diff; // 环形处理
                // 相性差越小，分数加成越高 (最多加20分)
                score += (75 - diff) / 3.75f;
            }

            return score;
        }


        public void AIIntraTransfer()
        {
            if (this.Architectures.Count > 1)
            {
                this.BelongedFaction.AITransferPlanning(this.Architectures);
            }
        }

        public void AIInterTransfer()
        {
            if (this.OrientationSection != null &&
                (this.AIDetail.AllowFoodTransfer || this.AIDetail.AllowFundTransfer || this.AIDetail.AllowMilitaryTransfer))
            {
                this.BelongedFaction.AllocationTransfer(this.Architectures, this.OrientationSection.Architectures,
                    (this.AIDetail.AllowFoodTransfer || this.AIDetail.AllowFundTransfer), false, this.AIDetail.AllowMilitaryTransfer);
                if (GameObject.Chance(10))
                {
                    this.BelongedFaction.FullTransfer(this.Architectures, this.OrientationSection.Architectures,
                        (this.AIDetail.AllowFoodTransfer || this.AIDetail.AllowFundTransfer), false, this.AIDetail.AllowMilitaryTransfer);
                }
            }
        }

        public int GetFrontScale()
        {
            if (this.ArchitectureCount == 0)
            {
                return 0;
            }
            int num = 0;
            foreach (Architecture architecture in this.Architectures)
            {
                if (architecture.FrontLine)
                {
                    num++;
                }
            }
            return ((num * 100) / this.ArchitectureCount);
        }

         // 逻辑已搬迁至 AI_Section_Resource_Logistics_Integrated.cs


        public int GetHostileScale()
        {
            if (this.ArchitectureCount == 0)
            {
                return 0;
            }
            int num = 0;
            foreach (Architecture architecture in this.Architectures)
            {
                if (architecture.HostileLine)
                {
                    num++;
                }
            }
            return ((num * 100) / this.ArchitectureCount);
        }

        public ArchitectureList GetOtherArchitectureList(Architecture architecture)
        {
            ArchitectureList list = new ArchitectureList();
            foreach (Architecture architecture2 in this.Architectures)
            {
                if (architecture2 != architecture)
                {
                    list.Add(architecture2);
                }
            }
            return list;
        }

        public bool HasArchitecture(Architecture a)
        {
            return this.Architectures.HasGameObject(a);
        }

        public List<string> LoadArchitecturesFromString(ArchitectureList architectures, string dataString)
        {
            List<string> errorMsg = [];
            char[] separator = [' ', '\n', '\r', '\t'];
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            this.Architectures.Clear();
            try
            {
                foreach (string str in strArray)
                {
                    // 🔥 2026-03-16 修复：使用 TryParse 而不是 Parse，避免 FormatException
                    // 原因：字符串末尾可能有空格或其他无效字符
                    if (!int.TryParse(str, out int archId))
                    {
                        errorMsg.Add($"无效的建筑ID: '{str}'");
                        continue;
                    }
                    
                    Architecture gameObject = architectures.GetGameObject(archId) as Architecture;
                    if (gameObject != null)
                    {
                        this.AddArchitecture(gameObject);
                    }
                    else
                    {
                        errorMsg.Add("建筑ID" + str + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("建筑列表应为半型空格分隔的建筑ID");
            }
            if (this.ArchitectureCount == 0)
            {
                errorMsg.Add("没有建筑");
            }
            return errorMsg;
        }

        public void RefreshSectionName()
        {
            if (this.ArchitectureCount > 0)
            {
                if (this.ArchitectureCount > 1)
                {
                    this.Architectures.PropertyName = "Population";
                    this.Architectures.IsNumber = true;
                    this.Architectures.ReSort();
                }
                base.Name = this.Architectures[0].Name + "军区";
            }
            else
            {
                base.Name = "----";
            }
        }

        public void RemoveArchitecture(Architecture architecture)
        {
            this.Architectures.Remove(architecture);

            // 🔥 关键修复：在军区重建过程中，不要重新分配，直接设为null
            // 让上层的重建逻辑来处理分配
            if (architecture.BelongedFaction != null)
            {
                // 检查是否在军区重建过程中（通过检查势力是否有其他可用军区）
                var availableSections = new List<Section>();
                foreach (GameObject obj in architecture.BelongedFaction.Sections.GetList())
                {
                    if (obj is Section s && s != this && s.AIDetail != null)
                    {
                        availableSections.Add(s);
                    }
                }
                
                if (availableSections.Count > 0)
                {
                    // 有其他可用军区，重新分配到第一个
                    Section newSection = availableSections[0];
                    architecture.BelongedSection = newSection;
                    if (!newSection.Architectures.HasGameObject(architecture))
                    {
                        newSection.Architectures.Add(architecture);
                    }
                    // System.Diagnostics.Debug.WriteLine($"[Section.RemoveArchitecture] 建筑{architecture.Name}从军区{this.Name}移除后重新分配到{newSection.Name}");
                }
                else
                {
                    // 没有其他可用军区，可能在重建过程中，设为null让重建逻辑处理
                    architecture.BelongedSection = null;
                    // System.Diagnostics.Debug.WriteLine($"[Section.RemoveArchitecture] 建筑{architecture.Name}从军区{this.Name}移除，等待重新分配");
                }
            }
            else
            {
                // 建筑没有势力，设为null
                architecture.BelongedSection = null;
                // System.Diagnostics.Debug.WriteLine($"[Section.RemoveArchitecture] 建筑{architecture.Name}没有势力，设为null");
            }
        }

        // [已移除] AutoManagePersonnel 及其辅助方法
        // 逻辑已升级并迁移至 Faction.AdvancedPersonnelTransfer
        // Section.AI() 现在统一调用 BelongedFaction.RunPersonnel()
        
        [DataMember]
        public int SectionLeaderID { get; set; } = -1;

        public void LoadAIDetail(SectionAIDetailTable table)
        {
            if (table == null) return;

            // 🔥 修复：使用 AIDetailID 而不是 Section.ID 来获取 AI 配置
            var detail = table.GetSectionAIDetail(this.AIDetailID);
            
            // 检查获取到的详情是否有效（AutoRun是否开启？）
            // 如果详情为空，或者详情虽然存在但并非自动运行（且ID看起来像默认值），则尝试寻找更有用的AI
            bool isValid = detail != null;
            if (isValid && !detail.AutoRun)
            {
                 // 如果加载的是ID=1（通常为默认），但它是Manual的
                 // 启发式：
                 // 1. 如果是势力的首个军区（中央政府），通常保持手动（Manual）。
                 // 2. 如果是次级军区（Military Districts），且是默认配置，说明是BUG导致的未激活，强制修正为Auto。
                 if (this.AIDetailID <= 1) 
                 {
                     bool isMainSection = (this.BelongedFaction != null && this.BelongedFaction.FirstSection == this);
                     if (!isMainSection)
                     {
                         // 次级军区不应该处于默认的手动状态
                         isValid = false; 
                     }
                 }
            }

            if (!isValid)
            {
                // 1. 尝试查找默认值 "军区委任-无" 且 AutoRun=true
                // 参数顺序：OrientationKind, AutoRun, ValueOffensive, AllowOffensive, AllowMilitaryTransfer, ValueRecruitment
                // 参考 MarshalSectionDialog 中的默认设定：(无, true, false, true, true, false)
                var list = table.GetSectionAIDetailsByConditions(SectionOrientationKind.无, true, false, true, true, false);
                if (list.Count > 0)
                {
                    detail = list[0] as SectionAIDetail;
                }
                // 2. 如果还找不到，退而求其次找任意一个 AutoRun=true 的
                else
                {
                    foreach(var d in table.SectionAIDetails.Values)
                    {
                        if (d.AutoRun)
                        {
                            detail = d;
                            break;
                        }
                    }
                }
                
                // 3. 实在不行，拿表中第一个
                if (detail == null && table.SectionAIDetails.Count > 0)
                {
                     foreach(var d in table.SectionAIDetails.Values)
                     {
                         detail = d;
                         break;
                     }
                }
            }

            if (detail != null)
            {
                this.aiDetail = detail;
                // 🔥 关键修复：同步设置 AIDetailID
                this.AIDetailID = detail.ID;
            }
            else
            {
                if (Session.GlobalVariables.EnableAIDebugLog)
                {
                    System.Diagnostics.Debug.WriteLine($"[Section.LoadAIDetail] CRITICAL: Section {this.Name} failed to load ANY AIDetail.");
                }
            }
        }

        // 在场景加载完成，且 SectionAIDetailTable 已经反序列化之后调用
        public static void LinkSectionAIDetails()
        {
            if (Session.Current.Scenario.GameCommonData == null || Session.Current.Scenario.GameCommonData.AllSectionAIDetails == null) return;

            var sectionTable = Session.Current.Scenario.GameCommonData.AllSectionAIDetails;
            var sections = Session.Current.Scenario.Sections.GetList();
            
            foreach (Section sect in sections)
            {
                sect.LoadAIDetail(sectionTable);
                
                // 【修复】：从存档恢复军团长引用
                sect.RestoreSectionLeader();
            }
        }

        public SectionAIDetail AIDetail
        {
            get
            {
                // 🔥 ANTI-BAND-AID：不掩盖数据错误
                // 如果 aiDetail 为 null，说明数据加载失败，应该让问题暴露
                // 调用方应该在使用前确保 AIDetail 已正确加载
                return this.aiDetail;
            }
            set
            {
                this.aiDetail = value;
                // 🔥 关键修复：同步设置 AIDetailID
                // 日期：2026-03-17
                // 原因：读档时 LinkReferencesPhase 需要通过 AIDetailID 恢复 AIDetail 引用
                // 修复：无论 value 是否为 null，都要同步设置 AIDetailID
                this.AIDetailID = value?.ID ?? -1;
            }
        }

        public string AIDetailString
        {
            get
            {
                // 🔥 修复：使用属性而不是字段
                return this.AIDetail == null ? "————" : this.AIDetail.Name;
            }
        }

        public int ArchitectureCount
        {
            get
            {
                return this.Architectures.Count;
            }
        }

        public int ArchitectureScale
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.AreaCount;
                }
                return num;
            }
        }

        public int Army
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.ArmyQuantity;
                }
                foreach (Troop troop in this.BelongedFaction.Troops)
                {
                    if (!(troop.Destroyed || !this.HasArchitecture(troop.StartingArchitecture)))
                    {
                        num += troop.Quantity;
                    }
                }
                return num;
            }
        }

        public int ArmyScale
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.ArmyScale;
                }
                foreach (Troop troop in this.BelongedFaction.Troops)
                {
                    if (!(troop.Destroyed || !this.HasArchitecture(troop.StartingArchitecture)))
                    {
                        num += troop.Army.Scales;
                    }
                }
                return num;
            }
        }

        public string FactionString
        {
            get
            {
                return this.BelongedFaction.Name;
            }
        }

        public int Food
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.Food;
                }
                return num;
            }
        }

        public int Fund
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.Fund;
                }
                return num;
            }
        }

        public Architecture MaxPopulationArchitecture
        {
            get
            {
                if (this.ArchitectureCount == 0)
                {
                    return null;
                }
                if (this.ArchitectureCount != 1)
                {
                    this.Architectures.PropertyName = "Population";
                    this.Architectures.IsNumber = true;
                    this.Architectures.ReSort();
                }
                return (this.Architectures[0] as Architecture);
            }
        }

        public int MilitaryCount
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.MilitaryCount;
                }
                return num + this.TroopCount;
            }
        }

        public string OrientationString
        {
            get
            {
                if (this.AIDetail != null)
                {
                    switch (this.AIDetail.OrientationKind)
                    {
                        case SectionOrientationKind.无:
                            return "----";

                        case SectionOrientationKind.军区:
                            return ((this.OrientationSection != null) ? this.OrientationSection.Name : "----");

                        case SectionOrientationKind.势力:
                            return ((this.OrientationFaction != null) ? this.OrientationFaction.Name : "----");

                        case SectionOrientationKind.州域:
                            return ((this.OrientationState != null) ? this.OrientationState.Name : "----");

                        case SectionOrientationKind.建筑:
                            return ((this.OrientationArchitecture != null) ? this.OrientationArchitecture.Name : "----");
                    }
                }
                return "----";
            }
        }

        public PersonList Persons
        {
            get
            {
                PersonList result = new PersonList();
                foreach (Architecture architecture in this.Architectures)
                {
                    foreach (Person p in architecture.Persons)
                    {
                        result.Add(p);
                    }
                }
                foreach (Troop troop in this.BelongedFaction.Troops)
                {
                    if (troop.StartingArchitecture.BelongedSection == this)
                    {
                        foreach (Person p in troop.Persons)
                        {
                            result.Add(p);
                        }
                    }
                }
                return result;
            }
        }

        public int PersonCount
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.GetAllPersons().Count;
                }
                foreach (Troop troop in this.BelongedFaction.Troops)
                {
                    if (troop.StartingArchitecture.BelongedSection == this)
                    {
                        num += troop.PersonCount;
                    }
                }
                return num;
            }
        }

        public int Population
        {
            get
            {
                int num = 0;
                foreach (Architecture architecture in this.Architectures)
                {
                    num += architecture.Population;
                }
                return num;
            }
        }

        public int TroopCount
        {
            get
            {
                int num = 0;
                foreach (Troop troop in this.BelongedFaction.Troops)
                {
                    if (!troop.Destroyed && troop.StartingArchitecture.BelongedSection == this)
                    {
                        num++;
                    }
                }
                return num;
            }
        }

         // 逻辑已搬迁至 AI_Section_Resource_Logistics.cs
    }
}



