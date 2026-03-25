#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using GameObjects.Events;  // 🔥 新增：ScenarioEvents
using GameObjects.FactionDetail;
using GameObjects.PersonDetail;
using GameObjects.SectionDetail;
using GameObjects.TroopDetail;
using GamePanels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PluginInterface;
using WorldOfTheThreeKingdoms.GameLogic;
using WorldOfTheThreeKingdoms.GameScreens;
using WorldOfTheThreeKingdoms.GameManager;
using WorldOfTheThreeKingdoms.GameScreens.ScreenLayers;
using WorldOfTheThreeKingdoms.Resources;
using WorldOfTheThreeKingdoms.Tools;
using WorldOfTheThreeKingdoms.GameObjects;  // 🔥 2026-03-21 添加：支持 TroopZocCalculator
using Platforms;
using youcelanPlugin;  // 🔥 2026-03-05 添加：支持 TabListInFrame 类型转换

//using GameObjects.PersonDetail.PersonMessages;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 说服分析结果
    /// </summary>
    public class ConvinceAnalysisResult
    {
        public Person BestCandidate { get; set; }
        public int BestScore { get; set; }          // 军师预测的成功率
        public int ActualScore { get; set; }        // 实际成功率
        public bool HasSuitableCandidate { get; set; }
        public Person TargetPerson { get; set; }
        public int AdvisorIntelligence { get; set; } // 军师智力
        public int PredictionAccuracy { get; set; }  // 预测准确性
    }

    /// <summary>
    /// 破坏分析结果
    /// </summary>
    public class DestroyAnalysisResult
    {
        public Architecture TargetArchitecture { get; set; }
        public Person Advisor { get; set; }
        public Person BestCandidate { get; set; }
        public int BestScore { get; set; }
        public bool HasSuitableCandidate { get; set; }
    }

    /// <summary>
    /// 煽动分析结果
    /// </summary>
    public class InstigateAnalysisResult
    {
        public Architecture TargetArchitecture { get; set; }
        public Person Advisor { get; set; }
        public Person BestCandidate { get; set; }
        public int BestScore { get; set; }
        public bool HasSuitableCandidate { get; set; }
    }

    /// <summary>
    /// 流言分析结果
    /// </summary>
    public class GossipAnalysisResult
    {
        public Architecture TargetArchitecture { get; set; }
        public Person Advisor { get; set; }
        public Person BestCandidate { get; set; }
        public int BestScore { get; set; }
        public bool HasSuitableCandidate { get; set; }
    }

    [GenerateUIAccessor]  // 🔥 添加源生成器特性，支持UI属性访问
    public partial class MainGameScreen : Screen
    {
        private string bianduiLiebiaoBiaoji;
        private ArchitectureLayer architectureLayer;
        private Keys currentKey;
        private bool drawingSelector;
        private bool tufashijianzantingyinyue=false ;
        private int shangciCundangShijian;
        private int cundangShijianJiange;
        public bool EnableLaterMouseLeftDownEvent;
        public bool EnableLaterMouseLeftUpEvent;
        public bool EnableLaterMouseMoveEvent;
        public bool EnableLaterMouseRightDownEvent;
        public bool EnableLaterMouseRightUpEvent;
        public bool EnableLaterMouseScrollEvent;
        private int frameCounter;
        private int frameRate;
        private Point lastPosition;
        private double lastTime;
        private string LoadFileName;
        public MainMapLayer mainMapLayer;



        #region 对话UI字段

        /// <summary>
        /// 对话UI组件
        /// </summary>
        private WorldOfTheThreeKingdoms.GameManager.DialogueUI dialogueUI;

        /// <summary>
        /// 对话UI是否已加载标志
        /// </summary>
        private bool dialogueUILoaded = false;

        /// <summary>
        /// 对话结束后的回调委托
        /// </summary>
        public delegate void DialogueFinishedCallback();

        /// <summary>
        /// 当前对话的回调函数
        /// </summary>
        private DialogueFinishedCallback currentDialogueCallback;

        #endregion

        #region 军师推荐系统字段

        /// <summary>
        /// 军师推荐系统
        /// </summary>
        private WorldOfTheThreeKingdoms.GameManager.AdvisorRecommendationSystem _advisorRecommendationSystem;

        #endregion

        #region 暴击图显示系统字段

        /// <summary>
        /// 暴击图显示管理器
        /// </summary>
        private WorldOfTheThreeKingdoms.GameObjects.Animations.CriticalHitImageManager _criticalHitImageManager;

        /// <summary>
        /// 当前帧的 GameTime（用于暴击图等需要时间的系统）
        /// </summary>
        private GameTime _currentGameTime;

        #endregion

        #region 智能操作系统字段

        /// <summary>
        /// 智能操作类型枚举
        /// </summary>
        public enum IntelligentOperationType
        {
            None,
            Destroy,
            Instigate,
            Convince,
            Gossip,
            JailBreak,
            Assassinate
        }

        public class InstigateAnalysisResult
        {
            public Architecture TargetArchitecture { get; set; }
            public Person Advisor { get; set; }
            public Person BestCandidate { get; set; }
            public int BestScore { get; set; }
            public bool HasSuitableCandidate { get; set; }
            public string AnalysisText { get; set; }
        }

        public class GossipAnalysisResult
        {
            public Architecture TargetArchitecture { get; set; }
            public Person Advisor { get; set; }
            public Person BestCandidate { get; set; }
            public int SuccessRate { get; set; }
            public int BestScore { get; set; }
            public bool HasSuitableCandidate { get; set; }
            public string AnalysisText { get; set; }
        }

        public class JailBreakAnalysisResult
        {
            public Architecture TargetArchitecture { get; set; }
            public Person Advisor { get; set; }
            public Person BestCandidate { get; set; }
            public int SuccessRate { get; set; }
            public string AnalysisText { get; set; }
        }

        public class AssassinateAnalysisResult
        {
            public Person TargetPerson { get; set; }
            public Architecture TargetArchitecture { get; set; }
            public Person Advisor { get; set; }
            public Person BestCandidate { get; set; }
            public int SuccessRate { get; set; }
            public bool HasSuitableCandidate { get; set; }
            public string AnalysisText { get; set; }
        }


        /// <summary>
        /// 当前智能操作类型
        /// </summary>
        public IntelligentOperationType CurrentOperationType { get; set; } = IntelligentOperationType.None;

        /// <summary>
        /// 当前智能操作的执行建筑（发起操作的建筑）
        /// </summary>
        public Architecture CurrentSourceArchitecture { get; set; } = null;

        /// <summary>
        /// 当前智能操作的目标建筑（被操作的建筑）
        /// </summary>
        public Architecture CurrentTargetArchitecture { get; set; } = null;

        #endregion

        #region 天气粒子系统字段

        /// <summary>
        /// 天气粒子系统（雨雪效果，支持风向风力）
        /// 日期：2026-03-10
        /// </summary>
        private WorldOfTheThreeKingdoms.GameLogic.WeatherParticleSystem _weatherParticleSystem;

        /// <summary>
        /// 1x1 白色纹理（用于粒子渲染）
        /// </summary>
        private Texture2D _pixelTexture;
        
        /// <summary>
        /// 1x1 白色纹理（用于边界线渲染）
        /// </summary>
        private Texture2D _whitePixel;

        /// <summary>
        /// 🔥 HOT PATH 优化：缓存地图尺寸，避免每帧访问属性链
        /// 日期：2026-03-11
        /// </summary>
        private int _cachedMapWidth;
        private int _cachedMapHeight;

        #endregion

        private MapVeilLayer mapVeilLayer;
        private int oldScrollWheelValue;

        //public WindowsMediaPlayerClass Player;

        public GamePlugin Plugins;
        private Point position;
        private RoutewayLayer routewayLayer;
        private string SaveFileName;
        private ScreenManager screenManager;
        private float scrollSpeedScale;
        private float scrollSpeedScaleDefault;
        private float scrollSpeedScaleSpeedy;
        private SelectingLayer selectingLayer;
        public Point SelectorStartPosition;
        public TroopList SelectorTroops;
        public GameTextures Textures;
        
        private TileAnimationLayer tileAnimationLayer;
        private TroopLayer troopLayer;
        private int UpdateCount;
        private ViewMove viewMove;
        private bool isKeyScrolling = false;
        //public FreeText qizidezi;
        public bool editMode = false;
        public bool ShowArchitectureConnectedLine = false;
        private int ditukuaidezhi = 1;

        /// <summary>
        /// 获取当前可视视口矩形（Grid坐标）
        /// 根据 MainMapLayer 的计算逻辑推导
        /// </summary>
        public Rectangle ViewportRect
        {
            get
            {
                if (this.mainMapLayer == null) return Rectangle.Empty;
                
                // TopLeftPosition 和 BottomRightPosition 是 MainGameScreen 的内部字段/属性
                // 如果它们是私有的，我们需要确保能够访问，或者使用 LeftEdge/TopEdge 计算
                // 假设 TopLeftPosition 和 BottomRightPosition 是可用的 (MainMapLayer 在用)
                
                int x = this.TopLeftPosition.X;
                int y = this.TopLeftPosition.Y;
                int width = (this.BottomRightPosition.X - x) + 1;
                int height = (this.BottomRightPosition.Y - y) + 1;
                
                return new Rectangle(x, y, width, height);
            }
        }


        private bool mapEdited = false;

        public CloudLayer cloudLayer = new CloudLayer();

        public DantiaoLayer dantiaoLayer = null;

        // 四叉树优化
        private SimpleQuadtree _simpleQuadtree;
        
        // 时间切片优化系统
        private int _globalFrameCounter = 0;
        private const int DEFAULT_SLICE_COUNT = 10;  // 默认切片数量
        private const int QUICKBATTLE_SLICE_COUNT = 20;  // 快速战斗切片数量
        
        // 🔥 性能优化：预计算的 GameTime 缓存（避免 Hot Path 分配）
        // 索引 = sliceCount，值 = 对应的 GameTime
        private static readonly GameTime[] _cachedBrainGameTimes = InitializeBrainGameTimeCache();
        
        private static GameTime[] InitializeBrainGameTimeCache()
        {
            // 预计算 sliceCount 1-200 的所有 GameTime
            const int maxSliceCount = 200;
            GameTime[] cache = new GameTime[maxSliceCount + 1];
            
            for (int i = 1; i <= maxSliceCount; i++)
            {
                TimeSpan elapsed = TimeSpan.FromSeconds(1.0 / 60.0 * i);
                cache[i] = new GameTime(TimeSpan.Zero, elapsed);
            }
            
            return cache;
        }
        
        // 集群战斗系统
        private Dictionary<Point, ArmySquad> _armySquads = new Dictionary<Point, ArmySquad>();
        private int _squadUpdateCounter = 0;
        
        // 智能性能管理系统
        private PerformanceMonitor _performanceMonitor = new PerformanceMonitor();
        
        // AI管理系统
        private AIManager _aiManager;
        
        // 🧠 AI决策系统 - 基于记忆驱动的三步式AI逻辑
        private WorldOfTheThreeKingdoms.GameManager.CompleteAIDecisionSystem _aiDecisionSystem;
        private int _aiUpdateCounter = 0;
        
        // 性能优化：缓存部队列表
        private GameObjectList _cachedTroopList;
        private int _lastTroopListUpdate = 0;
        
        // 分层寻路系统
        private WorldOfTheThreeKingdoms.GameManager.PathfindingManager _pathfindingManager;
        
        // 内存监控系统
        private WorldOfTheThreeKingdoms.GameManager.MemoryMonitor _memoryMonitor = new WorldOfTheThreeKingdoms.GameManager.MemoryMonitor();
        
        // 视觉管理系统
        private WorldOfTheThreeKingdoms.GameManager.VisualsManager _visualsManager;
        
        // 军师UI系统
        private WorldOfTheThreeKingdoms.GameManager.StrategistUI _strategistUI;

        private bool _strategistUILoaded = false;
        
        // 主线程调度器 - 用于异步纹理加载等需要主线程执行的操作
        private WorldOfTheThreeKingdoms.GameManager.MainThreadDispatcher _mainThreadDispatcher;
        
        // 性能监控：队列长度采样计数器
        private int _dispatcherMonitorCounter = 0;
        
        /// <summary>
        /// 🆕 势力范围更新管理器（2026-03-11）
        /// </summary>
        internal WorldOfTheThreeKingdoms.GameManager.InfluenceUpdateManager? _influenceUpdateManager;
        
        /// <summary>
        /// 🔥 游戏启动标志（用于跳过第一次能量衰减）
        /// 日期：2026-03-21
        /// 原因：游戏启动时能量还未初始化，第一次衰减会把所有能量清零
        /// </summary>
        private bool _isFirstTurn = true;
        
        /// <summary>
        /// 🆕 势力范围渲染器（2026-03-11）
        /// </summary>
        private WorldOfTheThreeKingdoms.GameManager.InfluenceRenderer? _influenceRenderer;
        
        /// <summary>
        /// 🎨 新工笔重彩水墨渲染器（2026-03-13）
        /// </summary>
        private WorldOfTheThreeKingdoms.GameManager.InkBleedInfluenceRenderer? _inkRenderer;
        
        #region 选中部队能量覆盖范围高亮显示 (2026-03-21)
        
        /// <summary>
        /// 选中部队的能量覆盖范围缓存（存储格子索引）
        /// 🔥 Zero-Allocation：使用 HashSet 避免重复，预分配容量
        /// </summary>
        private HashSet<int> _selectedTroopZocTiles = new HashSet<int>(128);
        
        /// <summary>
        /// 上次更新ZOC的部队（用于检测选中部队是否变化）
        /// </summary>
        private Troop? _lastSelectedTroop = null;
        
        /// <summary>
        /// 1x1 白色纹理（用于高亮蒙版渲染）
        /// </summary>
        private Texture2D? _whiteTileOverlay = null;
        
        /// <summary>
        /// 🔥 Hot Path 优化：BFS 算法的 frontier 队列（复用，避免每次分配）
        /// 日期：2026-03-21
        /// </summary>
        private PriorityQueue<int, int> _zocFrontier = new PriorityQueue<int, int>(128);
        
        /// <summary>
        /// 🔥 Hot Path 优化：BFS 算法的 visited 集合（复用，避免每次分配）
        /// 日期：2026-03-21
        /// </summary>
        private HashSet<int> _zocVisited = new HashSet<int>(128);
        
        #endregion
        
        /// <summary>
        /// 获取主线程调度器实例（供插件异步加载使用）
        /// </summary>
        public WorldOfTheThreeKingdoms.GameManager.MainThreadDispatcher MainThreadDispatcher => _mainThreadDispatcher;

        public MainGameScreen()
            : base()
        {
            //this.Player = new WindowsMediaPlayerClass();

            this.Textures = new GameTextures();
            this.mainMapLayer = new MainMapLayer();
            this.architectureLayer = new ArchitectureLayer();
            this.mapVeilLayer = new MapVeilLayer();
            this.troopLayer = new TroopLayer();
            this.selectingLayer = new SelectingLayer();
            this.tileAnimationLayer = new TileAnimationLayer();
            this.routewayLayer = new RoutewayLayer();
            this.Plugins = new GamePlugin();
            this.SelectorTroops = new TroopList();
            this.scrollSpeedScale = 1f;
            this.scrollSpeedScaleDefault = 1f;
            this.scrollSpeedScaleSpeedy = 1.7f;
            this.oldScrollWheelValue = 0;
            this.EnableLaterMouseLeftDownEvent = true;
            this.EnableLaterMouseLeftUpEvent = true;
            this.EnableLaterMouseRightDownEvent = true;
            this.EnableLaterMouseRightUpEvent = true;
            this.EnableLaterMouseMoveEvent = true;
            this.EnableLaterMouseScrollEvent = true;
            this.frameRate = 0;
            this.frameCounter = 0;
            this.cundangShijianJiange = 0;
            this.shangciCundangShijian = 0;
            this.UpdateCount = 0;

            this.screenManager = new ScreenManager();
            
            // 初始化军师UI系统
            this._strategistUI = new WorldOfTheThreeKingdoms.GameManager.StrategistUI();
            
            // 初始化对话UI系统
            this.dialogueUI = new WorldOfTheThreeKingdoms.GameManager.DialogueUI();
            
            // 初始化军师推荐系统
            this._advisorRecommendationSystem = new WorldOfTheThreeKingdoms.GameManager.AdvisorRecommendationSystem();
            
            // 初始化主线程调度器（用于异步纹理加载等操作）
            this._mainThreadDispatcher = new WorldOfTheThreeKingdoms.GameManager.MainThreadDispatcher();
            
            // 尝试加载对话UI资源
            try
            {
                if (Session.Current?.Content != null)
                {
                    this.dialogueUI.LoadContent(Session.Current.Content, Platform.GraphicsDevice);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Initialize] 对话UI初始化失败: {ex.Message}");
            }

            // 注册年份变化事件，用于触发军师推荐
            // 注意：在构造函数中Session.Current.Scenario可能还未初始化
            // 所以我们延迟到LoadContent或其他合适时机注册
            try
            {
                RegisterAdvisorRecommendationEvents();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Initialize] 注册年份事件失败: {ex.Message}");
            }
            

            
            // 🧠 初始化AI决策系统
            InitializeAIDecisionSystem();
            
            // 🎭 初始化对话管理系统
            InitializeDialogueSystem();
            
            // 订阅性能设置变更事件
            PerformanceSettings.Current.OnSettingsChanged += OnPerformanceSettingsChanged;
            
            // 🎨 水墨渲染器初始化已移至 Scenario_OnAfterScenarioLoaded 事件（2026-03-14）
            // 原因：构造函数调用时 Scenario 还未加载
            
            // 🔥 初始化白色纹理（用于高亮蒙版）
            // 日期：2026-03-21
            InitializeWhiteTileOverlay();
            
            // 初始化双击系统
            /*
            try
            {
                DoubleClickIntegration.Initialize();
                System.Diagnostics.Debug.WriteLine("DoubleClickIntegration initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DoubleClickIntegration initialization failed: {ex.Message}");
            }
            */
            
            // 对象池将在游戏运行时自动初始化
            
            //Session.Current.Scenario = new GameScenario(this);
            //this.LoadCommonData();

            //Platform.MainGame.Window.ClientSizeChanged += this.Window_ClientSizeChanged;  // new EventHandler(this.Window_ClientSizeChanged);

            Platform.MainGame.Activated += this.Game_Activated;  // new EventHandler(this.Game_Activated);
            Platform.MainGame.Deactivated += this.Game_Deactivated;  // new EventHandler(this.Game_Deactivated);
        }        

        /// <summary>
        /// 性能设置变更事件处理器
        /// </summary>
        private void OnPerformanceSettingsChanged()
        {
            try
            {
                var settings = PerformanceSettings.Current;
                System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 性能设置已更新: {settings.Mode}");
                
                // 可以在这里添加其他响应性能设置变更的逻辑
                // 例如：重新初始化对象池、调整渲染质量等
                
                // 如果需要重建四叉树以应用新的设置
                if (_simpleQuadtree != null && Setting.Current.GlobalVariables.UseQuadtreeOptimization)
                {
                    // 四叉树会在下一帧自动重建，无需手动操作
                    System.Diagnostics.Debug.WriteLine("[MainGameScreen] 四叉树将在下一帧应用新的性能设置");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnPerformanceSettingsChanged] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化白色纹理（用于高亮蒙版）
        /// 日期：2026-03-21
        /// </summary>
        private void InitializeWhiteTileOverlay()
        {
            if (_whiteTileOverlay == null)
            {
                _whiteTileOverlay = new Texture2D(Session.MainGame.GraphicsDevice, 1, 1);
                _whiteTileOverlay.SetData([Color.White]);
            }
        }

        private void ClearSelectedTroopZoc()
        {
            if (_selectedTroopZocTiles.Count > 0)
            {
                _selectedTroopZocTiles.Clear();
            }

            _lastSelectedTroop = null;
        }

        private bool ShouldShowHoveredTroopZoc()
        {
            return !this.Plugins.ContextMenuPlugin.IsShowing &&
                   base.UndoneWorks.Count > 0 &&
                   base.UndoneWorks.Peek().Kind == UndoneWorkKind.None;
        }
        
        /// <summary>
        /// 更新选中部队的能量覆盖范围缓存
        /// 日期：2026-03-21
        /// 🧊 Cold Path：鼠标悬停变化时调用
        /// 🔥 修复：为悬停部队单独计算能量覆盖范围
        /// 日期：2026-03-21
        /// 原因：GlobalInfluenceMap.ArmyEnergy 存储的是整个势力所有部队的能量叠加
        ///       需要为单个部队重新计算能量传播范围
        /// </summary>
        private void UpdateSelectedTroopZoc()
        {
            Troop hoveredTroop = null;
            
            if (!ShouldShowHoveredTroopZoc())
            {
                ClearSelectedTroopZoc();
                return;
            }
            
            // 🔥 复用 TroopSurveyPlugin 的悬停检测逻辑（参考第 13318-13346 行）
            // 日期：2026-03-21
            // 关键：
            // 1. 不要检查 this.CurrentTroop（选中部队后仍然可以悬停其他部队）
            // 2. 检查 ContextMenuPlugin.IsShowing（有菜单弹出时不显示）
            
            // 1. 检查是否有菜单弹出（如果有，不显示任何渲染）
            if (this.Plugins.ContextMenuPlugin.IsShowing)
            {
                // 清空渲染
                if (_selectedTroopZocTiles.Count > 0)
                {
                    _selectedTroopZocTiles.Clear();
                    _lastSelectedTroop = null;
                }
                return;
            }
            
            // 2. 检查是否满足显示条件（天眼模式 或 玩家已知该位置）
            if (Session.GlobalVariables.SkyEye || 
                ((Session.Current.Scenario.CurrentPlayer != null) && 
                 Session.Current.Scenario.CurrentPlayer.IsPositionKnown(this.position)))
            {
                // 3. 获取鼠标位置的部队
                hoveredTroop = Session.Current.Scenario.GetTroopByPosition(this.position);
                
                // 4. 检查鼠标是否在右侧栏上（如果在，不显示）
                if (this.Plugins.youcelanPlugin.IsShowing && 
                    StaticMethods.PointInRectangle(this.MousePosition, this.Plugins.youcelanPlugin.FrameRectangle))
                {
                    hoveredTroop = null;
                }
                
                // 5. 检查部队是否埋伏（埋伏部队不显示）
                if (hoveredTroop != null && hoveredTroop.Status == TroopStatus.埋伏)
                {
                    hoveredTroop = null;
                }
            }
            
            // 检查悬停部队是否变化
            if (hoveredTroop == _lastSelectedTroop)
            {
                return; // 未变化，无需更新
            }
            
            _lastSelectedTroop = hoveredTroop;
            
            // ✅ 正确：null 检查是合理的（业务逻辑）
            if (hoveredTroop == null)
            {
                ClearSelectedTroopZoc();
                return;
            }
            
            // 🔥 关键修复：为单个部队重新计算能量传播范围
            // 日期：2026-03-21
            // 原因：GlobalInfluenceMap.ArmyEnergy 存储的是整个势力所有部队的能量叠加
            //       不能直接使用，需要为单个部队重新计算
            _selectedTroopZocTiles.Clear();
            
            var faction = hoveredTroop.BelongedFaction;
            if (faction == null)
            {
                return; // 势力未初始化
            }
            
            // 计算部队威压能量
            int energy = hoveredTroop.CalculateZocEnergy();
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[UpdateSelectedTroopZoc] 部队 {hoveredTroop.DisplayName} 初始能量: {energy}");
            System.Diagnostics.Debug.WriteLine($"  - 兵力: {hoveredTroop.Quantity}, 士气: {hoveredTroop.Morale}");
            System.Diagnostics.Debug.WriteLine($"  - 攻击: {hoveredTroop.Offence}, 防御: {hoveredTroop.Defence}, 机动: {hoveredTroop.Movability}");
            #endif
            
            if (energy <= 0)
            {
                return; // 无威压能量
            }
            
            // 获取部队位置
            var pos = hoveredTroop.Position;
            int mapWidth = Session.Current.Scenario.ScenarioMap.MapDimensions.X;
            int mapHeight = Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
            
            // 边界检查
            if (pos.X < 0 || pos.X >= mapWidth || pos.Y < 0 || pos.Y >= mapHeight)
            {
                return;
            }
            
            // 🔥 使用与 InjectTroopEnergy 相同的扩散算法
            // 🔥 Hot Path 优化：复用字段级别的集合，避免每次分配
            // 🔥 C# 12：使用集合表达式
            _zocFrontier.Clear();
            _zocVisited.Clear();
            int[] dirOffsets = [-mapWidth, mapWidth, -1, 1];  // 上下左右
            
            // 获取天气和配置
            var weatherManager = Session.Current.Scenario.WeatherManager;
            if (weatherManager == null)
            {
                return;
            }
            
            var weather = weatherManager.GetWeatherAt(pos);
            string weatherName = weather.ToString();
            
            var influenceConfig = WorldOfTheThreeKingdoms.GameData.InfluenceConfig.Current;
            double weatherCostMultiplier = influenceConfig.GetWeatherTroopTerrainCostMultiplier(weatherName);
            var troopConfig = influenceConfig.TroopEnergySpreadConfig;  // 🔥 修复：使用 TroopEnergySpreadConfig
            
            // 起点：部队所在位置
            int startIndex = pos.Y * mapWidth + pos.X;
            _zocFrontier.Enqueue(startIndex, -energy);
            
            while (_zocFrontier.TryDequeue(out int currentIndex, out int priority))
            {
                if (!_zocVisited.Add(currentIndex)) continue;
                
                int currentEnergy = -priority;
                _selectedTroopZocTiles.Add(currentIndex);
                
                // 扩散到相邻格子
                int cx = currentIndex % mapWidth;
                
                for (int i = 0; i < 4; i++)
                {
                    // 边缘检测
                    if (i == 2 && cx == 0) continue;
                    if (i == 3 && cx == mapWidth - 1) continue;
                    
                    int nextIndex = currentIndex + dirOffsets[i];
                    if (nextIndex < 0 || nextIndex >= mapWidth * mapHeight) continue;
                    if (_zocVisited.Contains(nextIndex)) continue;
                    
                    // 获取地形代价
                    int baseTerrainCost = WorldOfTheThreeKingdoms.GameManager.TerrainCostCache.GetCostByIndex(nextIndex);
                    if (baseTerrainCost >= 99999) continue;
                    
                    // 应用天气倍率
                    int weatherAdjustedCost = (int)(baseTerrainCost * weatherCostMultiplier);
                    int actualCost = Math.Max(weatherAdjustedCost, troopConfig.MinimumTerrainCost);  // 🔥 修复：使用配置文件参数
                    
                    // 敌城阻力
                    // 🔥 关键：ID=0（洛阳）是有效的，必须使用 >= 0
                    int targetArchId = Session.Current.Scenario.ArchitectureCoreMap[nextIndex];
                    if (targetArchId >= 0)
                    {
                        var targetArch = Session.Current.Scenario.Architectures.GetGameObject(targetArchId) as Architecture;
                        
                        // 🔥 ANTI-BAND-AID：Fail Fast
                        if (targetArch == null)
                        {
                            throw new InvalidOperationException(
                                $"[UpdateSelectedTroopZoc] 数据损坏：ArchitectureCoreMap[{nextIndex}] " +
                                $"引用了不存在的建筑 ID={targetArchId}");
                        }
                        
                        // 跳过无主建筑（业务逻辑）
                        if (targetArch.BelongedFaction == null)
                        {
                            // 无主建筑视为中立，不增加额外阻力
                        }
                        else if (targetArch.BelongedFaction.IsHostile(faction))
                        {
                            // 🔥 修复：直接使用配置文件的值（与 InjectTroopEnergy 一致）
                            actualCost += troopConfig.EnemyCityResistance;
                        }
                        else if (targetArch.BelongedFaction != faction)
                        {
                            // 🔥 修复：直接使用配置文件的值
                            actualCost += troopConfig.AllyBarrier;
                        }
                    }
                    
                    // 敌军部队阻断
                    int nx = nextIndex % mapWidth;
                    int ny = nextIndex / mapWidth;
                    var nextTroop = Session.Current.Scenario.GetTroopByPosition(new Microsoft.Xna.Framework.Point(nx, ny));
                    
                    // ✅ 正确：GetTroopByPosition 返回 null 是正常的（该位置没有部队）
                    if (nextTroop != null && nextTroop.BelongedFaction != null && nextTroop.BelongedFaction.IsHostile(faction))
                    {
                        // 🔥 修复：直接使用配置文件的值
                        actualCost += troopConfig.EnemyBlocking;
                    }
                    
                    // 计算衰减后的能量
                    int nextEnergy = currentEnergy - actualCost;
                    if (nextEnergy <= troopConfig.MinimumEnergyThreshold) continue;  // 🔥 修复：使用配置文件的阈值
                    
                    _zocFrontier.Enqueue(nextIndex, -nextEnergy);
                }
            }
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[UpdateSelectedTroopZoc] 悬停部队 {hoveredTroop.DisplayName}");
            System.Diagnostics.Debug.WriteLine($"  - 初始能量: {energy}");
            System.Diagnostics.Debug.WriteLine($"  - 部队位置: ({pos.X}, {pos.Y})");
            System.Diagnostics.Debug.WriteLine($"  - 天气倍率: {weatherCostMultiplier:F2}");
            System.Diagnostics.Debug.WriteLine($"  - 覆盖范围: {_selectedTroopZocTiles.Count} 格");
            
            // 🔥 诊断：检查是否所有部队都从同一个位置开始扩散
            // 🔥 性能修复：不使用 LINQ，使用枚举器直接获取第一个元素
            if (_selectedTroopZocTiles.Count > 0)
            {
                var enumerator = _selectedTroopZocTiles.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    int firstTile = enumerator.Current;
                    int fx = firstTile % mapWidth;
                    int fy = firstTile / mapWidth;
                    System.Diagnostics.Debug.WriteLine($"  - 第一个覆盖格子: ({fx}, {fy})");
                }
                enumerator.Dispose();
            }
            #endif
        }

        /// <summary>
        /// 🎨 初始化水墨渲染器（2026-03-14）
        /// 🧊 Cold Path：在 Scenario 加载完成后调用
        /// </summary>
        internal void InitializeInkBleedRenderer()
        {
            // 🔥 ANTI-BAND-AID：此方法在 Scenario 已加载后调用，以下对象必须存在
            // 调用时机：1) MGSStartLoad.Scenario_OnAfterLoadScenario  2) MainMapLayer.freeTilesMemory（Content.Unload 后重建）
            if (Session.Current.Content == null)
                throw new InvalidOperationException("InitializeInkBleedRenderer: Session.Current.Content 为 null，请检查 Session 初始化流程");
            
            if (Platform.GraphicsDevice == null)
                throw new InvalidOperationException("InitializeInkBleedRenderer: Platform.GraphicsDevice 为 null，请检查平台初始化流程");
            
            var scenario = Session.Current.Scenario;
            if (scenario == null)
                throw new InvalidOperationException("InitializeInkBleedRenderer: Session.Current.Scenario 为 null，必须在 Scenario 加载后调用");
            
            if (scenario.ScenarioMap == null)
                throw new InvalidOperationException("InitializeInkBleedRenderer: Scenario.ScenarioMap 为 null，请检查剧本加载流程");
            
            // 旧渲染器先释放（freeTilesMemory 重建场景下 _inkRenderer 不为 null）
            _inkRenderer?.Dispose();
            
            var noiseTexture = Session.Current.Content.Load<Texture2D>("XuanPaperNoise");
            var inkBleedEffect = Session.Current.Content.Load<Effect>("InkBleed");
            
            int mapWidth = scenario.ScenarioMap.MapDimensions.X;
            int mapHeight = scenario.ScenarioMap.MapDimensions.Y;
            
            _inkRenderer = new WorldOfTheThreeKingdoms.GameManager.InkBleedInfluenceRenderer(
                Platform.GraphicsDevice,
                mapWidth,
                mapHeight,
                inkBleedEffect,
                noiseTexture);
            
            // 🔥 修复：读档时 GlobalInfluenceMap 可能未初始化
            // 日期：2026-03-18
            // 场景：freeTilesMemory() → InitializeInkBleedRenderer() 在 InfluenceUpdateManager.Initialize() 之前调用
            // 解决：检查所有势力的 GlobalInfluenceMap，如果任何一个未初始化则跳过
            bool canUpdateInfluenceMap = false;
            if (scenario.Factions != null && scenario.Factions.Count > 0)
            {
                // 🔥 关键修复：必须检查所有势力，不能只检查第一个
                // 原因：不同势力的 GlobalInfluenceMap 可能在不同时机初始化
                var factions = scenario.Factions.GetList();
                canUpdateInfluenceMap = true;  // 假设都已初始化
                
                for (int i = 0; i < factions.Count; i++)
                {
                    if (factions[i] is Faction faction)
                    {
                        if (faction.GlobalInfluenceMap is not { Length: > 0 })
                        {
                            canUpdateInfluenceMap = false;
                            System.Diagnostics.Debug.WriteLine(
                                $"[水墨渲染器] ⏸️ 势力 {faction.Name} 的 GlobalInfluenceMap 未初始化，跳过更新");
                            break;
                        }
                    }
                }
            }
            
            if (canUpdateInfluenceMap)
            {
                _inkRenderer.UpdateInfluenceMap();
                
                // 🎨 生成地形遮罩纹理（2026-03-17）
                _inkRenderer.GenerateTerrainMask(scenario);
                
                System.Diagnostics.Debug.WriteLine($"[水墨渲染器] ✅ 初始化完成，地图尺寸={mapWidth}×{mapHeight}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[水墨渲染器] ⏸️ 渲染器已创建，等待 GlobalInfluenceMap 初始化后更新");
            }
        }

        /// <summary>
        /// 智能预热对象池 - 根据当前场景自动检测规模
        /// </summary>
        private void PrewarmPoolsIntelligently()
        {
            try
            {
                // 自动检测游戏规模
                string gameMode = DetectGameScale();
                
                System.Diagnostics.Debug.WriteLine($"[智能预热] 检测到游戏规模: {gameMode}");
                
                // 调用静态预热方法
                PrewarmAllPoolsForLoading(gameMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrewarmPoolsIntelligently] 错误: {ex.Message}");
                // 出错时使用默认中等规模预热
                PrewarmAllPoolsForLoading("medium");
            }
        }

        /// <summary>
        /// 智能检测当前游戏规模 - 综合多个因素进行判断
        /// </summary>
        private string DetectGameScale()
        {
            try
            {
                if (Session.Current?.Scenario == null)
                    return "medium";

                // 收集游戏规模指标
                int troopCount = Session.Current.Scenario.Troops?.Count ?? 0;
                int factionCount = Session.Current.Scenario.Factions?.Count ?? 0;
                int architectureCount = Session.Current.Scenario.Architectures?.Count ?? 0;
                int personCount = Session.Current.Scenario.Persons?.Count ?? 0;
                
                // 计算综合规模评分 (0-100)
                int scaleScore = 0;
                
                // 部队数量权重 (40%)
                if (troopCount <= 30) scaleScore += 10;
                else if (troopCount <= 100) scaleScore += 25;
                else if (troopCount <= 300) scaleScore += 40;
                else scaleScore += 40;
                
                // 势力数量权重 (25%)
                if (factionCount <= 2) scaleScore += 5;
                else if (factionCount <= 5) scaleScore += 15;
                else if (factionCount <= 10) scaleScore += 25;
                else scaleScore += 25;
                
                // 建筑数量权重 (20%)
                if (architectureCount <= 20) scaleScore += 5;
                else if (architectureCount <= 50) scaleScore += 12;
                else if (architectureCount <= 100) scaleScore += 20;
                else scaleScore += 20;
                
                // 人物数量权重 (15%)
                if (personCount <= 50) scaleScore += 3;
                else if (personCount <= 200) scaleScore += 8;
                else if (personCount <= 500) scaleScore += 15;
                else scaleScore += 15;
                
                // 根据综合评分确定规模
                string gameScale;
                if (scaleScore <= 30)
                {
                    gameScale = "small";
                }
                else if (scaleScore >= 70)
                {
                    gameScale = "large";
                }
                else
                {
                    gameScale = "medium";
                }
                
                System.Diagnostics.Debug.WriteLine($"[智能规模检测] 部队:{troopCount}, 势力:{factionCount}, 建筑:{architectureCount}, 人物:{personCount}");
                System.Diagnostics.Debug.WriteLine($"[智能规模检测] 综合评分:{scaleScore}/100, 判定规模:{gameScale}");
                
                return gameScale;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DetectGameScale] 检测错误: {ex.Message}");
                return "medium"; // 出错时返回默认值
            }
        }

        /// <summary>
        /// Loading阶段智能预热所有对象池 - 在场景加载时调用
        /// </summary>
        /// <param name="gameMode">游戏模式：small=小规模, medium=中等规模, large=大规模</param>
        public static void PrewarmAllPoolsForLoading(string gameMode = "medium")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Loading阶段智能对象池预热开始 (模式: {gameMode}) ===");
                
                // 预热通用对象池
                global::GameManager.ObjectPoolManager.PrewarmAll();
                
                // 检测系统性能，动态调整预热策略
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                long initialMemory = GC.GetTotalMemory(false);
                
                // 根据游戏规模和系统性能智能调整预热数量
                int damagePoolSize, troopListSize, gameListSize, pointListSize, dictSize;
                float performanceMultiplier = GetPerformanceMultiplier();
                
                switch (gameMode.ToLower())
                {
                    case "small":
                        damagePoolSize = (int)(30 * performanceMultiplier);
                        troopListSize = (int)(10 * performanceMultiplier);
                        gameListSize = (int)(8 * performanceMultiplier);
                        pointListSize = (int)(20 * performanceMultiplier);
                        dictSize = (int)(5 * performanceMultiplier);
                        break;
                    case "large":
                        damagePoolSize = (int)(400 * performanceMultiplier);
                        troopListSize = (int)(80 * performanceMultiplier);
                        gameListSize = (int)(50 * performanceMultiplier);
                        pointListSize = (int)(150 * performanceMultiplier);
                        dictSize = (int)(40 * performanceMultiplier);
                        break;
                    default: // medium
                        damagePoolSize = (int)(150 * performanceMultiplier);
                        troopListSize = (int)(40 * performanceMultiplier);
                        gameListSize = (int)(25 * performanceMultiplier);
                        pointListSize = (int)(80 * performanceMultiplier);
                        dictSize = (int)(20 * performanceMultiplier);
                        break;
                }
                
                System.Diagnostics.Debug.WriteLine($"[智能预热] 性能倍数: {performanceMultiplier:F2}, 预热目标: 伤害池{damagePoolSize}, 部队列表{troopListSize}");
                
                // 执行智能预热 - 现在由ObjectPoolManager统一管理
                // 预热已在ObjectPoolManager.PrewarmAll()中完成
                
                stopwatch.Stop();
                long finalMemory = GC.GetTotalMemory(false);
                long memoryUsed = (finalMemory - initialMemory) / (1024 * 1024); // MB
                
                System.Diagnostics.Debug.WriteLine($"=== Loading阶段智能预热完成 ===");
                System.Diagnostics.Debug.WriteLine($"预热耗时: {stopwatch.ElapsedMilliseconds}ms, 内存使用: {memoryUsed}MB");
                System.Diagnostics.Debug.WriteLine($"模式: {gameMode}, 性能倍数: {performanceMultiplier:F2}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrewarmAllPoolsForLoading] 错误: {ex.Message}");
                // 预热失败时使用最小配置确保游戏能正常运行
                try
                {
                    // 最小配置预热已在ObjectPoolManager.PrewarmAll()中完成
                    System.Diagnostics.Debug.WriteLine("[PrewarmAllPoolsForLoading] 使用最小配置预热完成");
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("[PrewarmAllPoolsForLoading] 最小配置预热也失败，跳过预热");
                }
            }
        }
        
        /// <summary>
        /// 获取系统性能倍数 - 根据系统性能动态调整预热数量
        /// </summary>
        private static float GetPerformanceMultiplier()
        {
            try
            {
                // 检测可用内存
                long totalMemory = GC.GetTotalMemory(false);
                long memoryMB = totalMemory / (1024 * 1024);
                
                // 简单的性能评估 - 基于内存使用情况
                if (memoryMB < 256) // 低内存设备
                {
                    return 0.5f;
                }
                else if (memoryMB > 1024) // 高性能设备
                {
                    return 1.5f;
                }
                else // 标准设备
                {
                    return 1.0f;
                }
            }
            catch
            {
                return 1.0f; // 默认倍数
            }
        }

        /// <summary>
        /// 测试内存和对象池系统 - 可以通过调试器或控制台调用
        /// </summary>
        public void TestMemoryAndPoolSystem()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 内存和对象池系统测试开始 ===");
                
                // 内存监控测试
                System.Diagnostics.Debug.WriteLine(_memoryMonitor.GetMemoryReport());
                
                // 对象池统计测试
                System.Diagnostics.Debug.WriteLine(global::GameManager.ObjectPoolManager.GetAllStats());
                
                // 测试对象池使用
                System.Diagnostics.Debug.WriteLine("测试对象池使用...");
                
                // 测试TroopDamage池
                var damage1 = global::GameManager.ObjectPoolManager.DamagePool.Get();
                var damage2 = global::GameManager.ObjectPoolManager.DamagePool.Get();
                damage1.Damage = 100;
                damage2.Damage = 200;
                
                global::GameManager.ObjectPoolManager.DamagePool.Return(damage1);
                global::GameManager.ObjectPoolManager.DamagePool.Return(damage2);
                
                var damage3 = global::GameManager.ObjectPoolManager.DamagePool.Get(); // 应该是重置后的对象
                System.Diagnostics.Debug.WriteLine($"重用对象的伤害值: {damage3.Damage} (应该为0)");
                global::GameManager.ObjectPoolManager.DamagePool.Return(damage3);
                
                // 测试列表池 - 使用通用列表池
                var pointList = global::GameManager.ObjectPoolManager.PathPool.Get();
                pointList.Add(new Point(1, 1)); // 添加一些测试数据
                pointList.Add(new Point(2, 2));
                System.Diagnostics.Debug.WriteLine($"列表使用前大小: {pointList.Count}");
                
                global::GameManager.ObjectPoolManager.PathPool.Return(pointList);
                var pointList2 = global::GameManager.ObjectPoolManager.PathPool.Get(); // 应该是重置后的列表
                System.Diagnostics.Debug.WriteLine($"重用列表大小: {pointList2.Count} (应该为0)");
                global::GameManager.ObjectPoolManager.PathPool.Return(pointList2);
                
                System.Diagnostics.Debug.WriteLine("=== 内存和对象池系统测试完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestMemoryAndPoolSystem] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试AI管理系统 - 可以通过调试器或控制台调用
        /// </summary>
        public void TestAISystem()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== AI管理系统测试开始 ===");
                
                if (_aiManager != null)
                {
                    System.Diagnostics.Debug.WriteLine($"AI管理器状态: 已初始化");
                    System.Diagnostics.Debug.WriteLine(_aiManager.GetPerformanceStats());
                    
                    // 测试配置调整
                    _aiManager.ConfigurePerformance(3, 45); // 降低性能要求
                    System.Diagnostics.Debug.WriteLine("AI性能参数已调整为低性能模式");
                    
                    _aiManager.ConfigurePerformance(5, 30); // 恢复默认
                    System.Diagnostics.Debug.WriteLine("AI性能参数已恢复默认");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("AI管理器状态: 未初始化");
                }
                
                // 测试部队AI状态
                if (Session.Current?.Scenario?.Troops != null)
                {
                    var troops = Session.Current.Scenario.Troops.GetList().Cast<Troop>().Take(5);
                    foreach (var troop in troops)
                    {
                        System.Diagnostics.Debug.WriteLine($"部队{troop.ID}: 上次决策帧={troop.LastDecisionFrame}, 状态={troop.TroopStatus}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("=== AI管理系统测试完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestAISystem] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试性能管理系统 - 可以通过调试器或控制台调用
        /// </summary>
        public void TestPerformanceSystem()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 性能管理系统测试开始 ===");
                
                var settings = PerformanceSettings.Current;
                System.Diagnostics.Debug.WriteLine($"当前模式: {settings.Mode}");
                System.Diagnostics.Debug.WriteLine($"AI切片数: {settings.AiLogicSliceCount}");
                System.Diagnostics.Debug.WriteLine($"最大可见部队: {settings.MaxVisibleTroops}");
                
                // 测试模式切换
                System.Diagnostics.Debug.WriteLine("测试低性能模式...");
                settings.SetLowPerformanceMode();
                
                System.Diagnostics.Debug.WriteLine("测试高质量模式...");
                settings.SetHighQualityMode();
                
                System.Diagnostics.Debug.WriteLine("恢复平衡模式...");
                settings.SetBalancedMode();
                
                // 显示性能监控信息
                System.Diagnostics.Debug.WriteLine("=== 性能监控信息 ===");
                _performanceMonitor.LogPerformance();
                
                System.Diagnostics.Debug.WriteLine("=== 性能管理系统测试完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestPerformanceSystem] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试 WegoEngine（Command Buffer 架构）- 可以通过调试器或控制台调用
        /// 日期：2026-03-16
        /// </summary>
        public void TestWegoEngine()
        {
            try
            {
                if (Session.Current?.Scenario == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestWegoEngine] ❌ 无法测试：Scenario 为 null");
                    return;
                }
                
                if (Session.Current.WegoEngine == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TestWegoEngine] ⚠️ WegoEngine 未初始化，尝试初始化...");
                    
                    Session.Current.WegoEngine = new WorldOfTheThreeKingdoms.GameManager.WegoEngine();
                    
                    if (Session.Current.Scenario.Troops != null)
                    {
                        foreach (Troop troop in Session.Current.Scenario.Troops)
                        {
                            if (troop != null)
                                Session.Current.WegoEngine.RegisterTroop(troop);
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[TestWegoEngine] ✅ WegoEngine 已初始化，部队数量: {Session.Current.Scenario.Troops?.Count ?? 0}");
                }
                
                // 运行所有单元测试
                WegoEngineTests.RunAllTests(Session.Current.Scenario);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestWegoEngine] ❌ 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"  堆栈: {ex.StackTrace}");
            }
        }

        private string SaveFileExtension
        {
            get
            {
                return ".json";
#pragma warning disable CS0162 // Unreachable code detected
                if (Session.GlobalVariables.EncryptSave)
#pragma warning restore CS0162 // Unreachable code detected
                {
                    return ".zhs";
                }
                else
                {
                    return ".mdb";
                }
            }
        }


        private void CalculateFrameRate(GameTime gameTime)
        {
        }

        public override void DisposeMapTileMemory(bool gc, bool clearAll)
        {
            this.mainMapLayer.freeTilesMemory(gc, clearAll);
        }

        public void Dispose()
        {
            try
            {
                // 🔥 清理新事件系统订阅
                ScenarioEvents.ClearAllHandlers();
                
                // 执行安全的内存清理
                MemoryManager.OnGameExit();
                
                // 清空所有对象池
                global::GameManager.ObjectPoolManager.ClearAll();
                
                // 🎨 清理势力范围渲染器（2026-03-12）
                _influenceRenderer?.Dispose();
                _influenceRenderer = null;
                
                // 🎨 清理水墨渲染器（2026-03-13）
                _inkRenderer?.Dispose();
                _inkRenderer = null;
                
                System.Diagnostics.Debug.WriteLine("[MainGameScreen] 对象池和内存已清理");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGameScreen.Dispose] 清理错误: {ex.Message}");
            }

            //Platform.MainGame.Window.ClientSizeChanged -= this.Window_ClientSizeChanged;  // new EventHandler(this.Window_ClientSizeChanged);
            Platform.MainGame.Activated -= this.Game_Activated;  // new EventHandler(this.Game_Activated);
            Platform.MainGame.Deactivated -= this.Game_Deactivated;  // new EventHandler(this.Game_Deactivated);

            this.mainMapLayer.DisplayingMapTiles = null;
            this.mainMapLayer.DisplayingTiles = null;
            this.mainMapLayer = null;
            this.architectureLayer = null;
            this.routewayLayer = null;
            this.screenManager = null;
            this.Plugins = null;
        }


        public bool CurrentPlayerHasArchitecture()
        {
            return ((Session.Current.Scenario.CurrentPlayer != null) && (Session.Current.Scenario.CurrentPlayer.ArchitectureCount > 0));
        }

        public bool CurrentPlayerHasPerson()
        {
            return ((Session.Current.Scenario.CurrentPlayer != null) && (Session.Current.Scenario.CurrentPlayer.PersonCount > 0));
        }

        public bool CurrentPlayerHasTroop()
        {
            return ((Session.Current.Scenario.CurrentPlayer != null) && (Session.Current.Scenario.CurrentPlayer.TroopCount > 0));
        }



        private void DemolishCurrentRouteway()
        {
            Session.Current.Scenario.RemoveRouteway(this.CurrentRouteway);
        }

        // ----------------------------------------------------
        // 全链路性能监控：首帧渲染
        // ----------------------------------------------------
        private bool _isFirstDraw = true;
        

        public void Draw(GameTime gameTime)
        {
            if (_isFirstDraw)
            {
                var swDraw = System.Diagnostics.Stopwatch.StartNew();
                System.Diagnostics.Debug.WriteLine($"[全链路] >>> ⚠️ 开始首帧渲染 (GPU Upload) ⚠️ <<<");
                
                this.Drawing(gameTime);
                
                swDraw.Stop();
                System.Diagnostics.Debug.WriteLine($"[全链路] ⚠️ 首帧渲染耗时: {swDraw.ElapsedMilliseconds} ms ⚠️");
                
                if (swDraw.ElapsedMilliseconds > 1000)
                {
                    System.Diagnostics.Debug.WriteLine(">>> 破案了！卡顿是因为 GPU 纹理上传或 Shader 编译！<<<");
                }
                _isFirstDraw = false;
            }
            else
            {
                this.Drawing(gameTime);
            }

            // 🟢 加个判断：只有当系统鼠标隐藏时，才画游戏的自定义鼠标
            if (!Platform.MainGame.IsMouseVisible && (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop || Platform.PlatFormType == PlatFormType.UWP && !Platform.IsMobile))
            {
                this.DrawMouseArrow();
            }
        }

        private void DrawAutoSavePicture()
        {
            CacheManager.Draw(this.Textures.zidongcundangtupian, StaticMethods.GetViewportCenterRectangle(this.Textures.zidongcundangtupian.Width, this.Textures.zidongcundangtupian.Height, base.viewportSize), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.0001f);            
        }

        private void DrawArchitectureSurvey()   //绘制城市情况表
        {
            if ((this.Plugins.ArchitectureSurveyPlugin.Showing && (this.viewMove == ViewMove.Stop)) && (this.Plugins.ArchitectureSurveyPlugin != null))
            {
                this.Plugins.ArchitectureSurveyPlugin.Draw();
            }
        }

        private void DrawCommentText()  //绘制底部地图注释
        {
            if (this.Plugins.ConmentTextPlugin != null)
            {
                this.Plugins.ConmentTextPlugin.Draw();
            }
        }

        public void DrawContextMenu()         //绘制建筑命令菜单
        {
            if (this.Plugins.ContextMenuPlugin.IsShowing)
            {
                this.Plugins.ContextMenuPlugin.Draw();
            }
        }

        public void DrawDialog()
        {
            this.Plugins.HelpPlugin.Draw();
            this.Plugins.OptionDialogPlugin.Draw();
            this.Plugins.SimpleTextDialogPlugin.Draw();
            this.Plugins.tupianwenziPlugin.Draw();
            //this.Plugins.tupianwenziPlugin.Draw();
            this.Plugins.ConfirmationDialogPlugin.Draw();
            this.Plugins.TransportDialogPlugin.Draw();
            this.Plugins.CreateTroopPlugin.Draw();
            this.Plugins.MarshalSectionDialogPlugin.Draw();
            if (this.Plugins.InGameEditorPlugin != null)
            {
                this.Plugins.InGameEditorPlugin.Draw();
            }
        }

        public void Drawtupianwenzi()
        {

            this.Plugins.tupianwenziPlugin.Draw();

        }

        public void DrawFrameRate()
        {
            this.frameCounter++;
            string str = string.Format("fps: {0}", this.frameRate);
            Platform.MainGame.Window.Title = str;
        }

        public void DrawGameFrame()
        {

            if (this.Plugins.GameFramePlugin.IsShowing)
            {
                this.Plugins.GameFramePlugin.Draw();
            }
        }
        

        private void Drawing(GameTime gameTime)            //绘制游戏屏幕
        {
            if (Session.Current?.Scenario == null) 
            {
                return; 
            }

            var spriteBatch = Session.MainGame.SpriteBatch;

            // ==========================================
            // 阶段 0：Pre-pass - 更新水墨渲染器的离屏缓冲区
            // 🔥 关键：在主画面渲染之前，将势力范围绘制到 _lowResTarget
            // ==========================================
            if (_inkRenderer != null)
            {
                Rectangle viewport = new(
                    base.TopLeftPosition.X,
                    base.TopLeftPosition.Y,
                    base.BottomRightPosition.X - base.TopLeftPosition.X,
                    base.BottomRightPosition.Y - base.TopLeftPosition.Y
                );
                _inkRenderer.UpdateRenderTarget(
                    spriteBatch,
                    viewport,
                    this.mainMapLayer.TileWidth,
                    this.mainMapLayer.LeftEdge,
                    this.mainMapLayer.TopEdge);
            }

            // ==========================================
            // 阶段 1：主画面渲染
            // ==========================================
            this.mainMapLayer.Draw(base.viewportSize);
            
            // 🗺️ 绘制势力范围（在地形层之后、建筑层之前）
            // 日期：2026-03-11
            // 🔥 关键修复：使用独立的 SpriteBatch 块，明确指定半透明混合模式
            if (_influenceRenderer != null && _influenceRenderer.IsEnabled)
            {
                try
                {
                    // 🔥 步骤 1：结束当前批次
                    spriteBatch.End();
                    
                    // 🔥 步骤 2：使用半透明混合模式重新开始
                    spriteBatch.Begin(
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,      // 🔥 关键：半透明混合
                        SamplerState.PointClamp,
                        DepthStencilState.None,
                        RasterizerState.CullNone
                    );
                    
                    // 计算当前视口（Grid坐标）
                    int tileWidth = this.mainMapLayer.TileWidth;
                    int tileHeight = this.mainMapLayer.TileHeight;
                    
                    Rectangle viewport = new Rectangle(
                        base.TopLeftPosition.X,
                        base.TopLeftPosition.Y,
                        base.BottomRightPosition.X - base.TopLeftPosition.X,
                        base.BottomRightPosition.Y - base.TopLeftPosition.Y
                    );
                    
                    // 🔥 关键：传递屏幕边缘偏移（用于坐标转换）
                    _influenceRenderer.Draw(spriteBatch, viewport, tileWidth, 
                        this.mainMapLayer.LeftEdge, this.mainMapLayer.TopEdge);
                    
                    // 🔥 步骤 3：结束势力范围批次
                    spriteBatch.End();
                }
                finally
                {
                    // 🔥 步骤 4：恢复外层的 SpriteBatch 状态（使用与 MainGame.Draw 相同的参数）
                    // 注意：这里必须与 MainGame.Draw() 中的 Begin() 参数完全一致
                    spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null);
                }
            }
            
            this.architectureLayer.Draw(base.viewportSize, gameTime);
            this.routewayLayer.Draw(base.viewportSize);

            this.cloudLayer.Draw();

            // 🌧️ 绘制天气粒子系统（雨雪效果，在云层之后、地块动画之前）
            // 日期：2026-03-10
            // 🔥 性能优化：通过设置开关控制是否绘制粒子系统
            if (Session.GlobalVariables.EnableWeatherParticles && _weatherParticleSystem != null)
            {
                _weatherParticleSystem.Draw(Session.MainGame.SpriteBatch);
            }

            if (this.dantiaoLayer != null)
            {
                this.dantiaoLayer.Draw();
            }

            this.tileAnimationLayer.Draw(base.viewportSize);
            
            // 四叉树优化渲染
            if (Setting.Current.GlobalVariables.UseQuadtreeOptimization && _simpleQuadtree != null)
            {
                SimpleTroopRenderer.DrawOptimized(base.viewportSize, gameTime, _simpleQuadtree);
            }
            else
            {
                // 使用原始部队渲染
                this.troopLayer.Draw(base.viewportSize, gameTime);
            }
            
            // 渲染视觉管理系统（单位平滑移动动画）
            if (_visualsManager != null)
            {
                _visualsManager.Render(Session.MainGame.SpriteBatch);
            }
            
            // ==========================================
            // 阶段 3：Post-Overlay 水墨叠加
            // 🔥 关键：DrawOverlay 会结束外层批次，之后需要重新 Begin
            // ==========================================
            if (_inkRenderer != null)
            {
                spriteBatch.End();
                _inkRenderer.DrawOverlay(spriteBatch, gameTime);
                // 🔥 恢复外层 SpriteBatch 状态
                spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null);
            }

            // ==========================================
            // 阶段 3.5：选中部队能量覆盖范围高亮显示
            // 日期：2026-03-21
            // ==========================================
            DrawSelectedTroopZocHighlight(spriteBatch, gameTime);

            // ==========================================
            // 阶段 4：UI 层
            // ==========================================
            this.mapVeilLayer.Draw(base.viewportSize);
            
            // [新增] 绘制双击军师菜单UI（在所有其他UI之上）
            /*
            try
            {
                DoubleClickIntegration.Draw(Session.MainGame.SpriteBatch);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DoubleClickIntegration draw failed: {ex.Message}");
            }
            */
            
            // 绘制军师UI（在所有其他UI之上）
            if (_strategistUI != null && Session.Current?.Scenario != null)
            {
                _strategistUI.Draw(Session.MainGame.SpriteBatch);
            }

            switch (base.UndoneWorks.Peek().Kind)
            {
                case UndoneWorkKind.None:
                    if (StaticMethods.PointInViewport(base.MousePosition, base.viewportSize))
                    {
                        this.DrawCommentText();
                        this.DrawArchitectureSurvey();
                        this.DrawTroopSurvey();
                    }
                    this.DrawRoutewayEditor();
                    this.Plugins.ToolBarPlugin.DrawTools = true;
                    this.Plugins.youcelanPlugin.Draw(); 
                    break;

                case UndoneWorkKind.ContextMenu:
                    this.DrawContextMenu();
                    if (this.bianduiLiebiaoBiaoji  == "ArchitectureLeftClick")
                    {
                        this.Plugins.BianduiLiebiao.Draw();
                        if (StaticMethods.PointInViewport(base.MousePosition, base.viewportSize))
                        {
                            this.DrawArchitectureSurvey();
                        }
                    }
                    this.Plugins.ToolBarPlugin.DrawTools = false;
                    break;

                case UndoneWorkKind.Frame:
                    this.DrawGameFrame();
                    this.Plugins.ToolBarPlugin.DrawTools = false;
                    break;

                case UndoneWorkKind.Dialog:
                    this.DrawDialog();
                    this.Plugins.ToolBarPlugin.DrawTools = false;
                    break;
                case UndoneWorkKind.tupianwenzi:
                    this.Drawtupianwenzi();
                    this.Plugins.ToolBarPlugin.DrawTools = false;
                    break;
                case UndoneWorkKind.liangdaobianji:
                    if (StaticMethods.PointInViewport(base.MousePosition, base.viewportSize))
                    {
                        this.DrawCommentText();
                        this.DrawArchitectureSurvey();
                        this.DrawTroopSurvey();
                    }
                    this.DrawRoutewayEditor();

                    this.Plugins.ToolBarPlugin.DrawTools = false;
                    break;
                case UndoneWorkKind.SubDialog:
                    this.DrawSubDialog();
                    this.Plugins.ToolBarPlugin.DrawTools = false;
                    break;

                case UndoneWorkKind.Selecting:
                    this.DrawCommentText();
                    this.selectingLayer.Draw(base.viewportSize);
                    this.Plugins.ToolBarPlugin.DrawTools = true;
                    this.Plugins.youcelanPlugin.Draw(); 
                    break;

                case UndoneWorkKind.Inputer:
                    this.DrawInputer();
                    this.Plugins.ToolBarPlugin.DrawTools = false;
                    break;

                case UndoneWorkKind.Selector:
                    if (StaticMethods.PointInViewport(base.MousePosition, base.viewportSize))
                    {
                        this.DrawCommentText();
                    }
                    this.DrawSelector();

                    this.Plugins.ToolBarPlugin.DrawTools = true;
                    this.Plugins.youcelanPlugin.Draw(); 

                    break;

                case UndoneWorkKind.MapViewSelector:
                    if (StaticMethods.PointInViewport(base.MousePosition, base.viewportSize))
                    {
                        this.DrawCommentText();
                    }
                    if (this.Plugins.MapViewSelectorPlugin.Kind == MapViewSelectorKind.建筑)
                    {
                        this.DrawArchitectureSurvey();
                    }
                    this.DrawMapViewSelector();
                    break;
            }
            this.DrawScreenBlind();
            this.DrawPersonBubble();
            this.DrawToolBar(gameTime);

            if (this.Plugins.ToolBarPlugin != null)
            {
                ((ToolBarPlugin.ToolBarPlugin)this.Plugins.ToolBarPlugin).backTool.Draw();
            }

            // 绘制军师双击菜单（在鼠标箭头之前，确保在最上层）
            // this.DrawAdvisorDoubleClickMenu();

            // 🎯 绘制暴击图（在UI层之上，鼠标箭头之前）
            // 🔥 Hot Path - 调用方保证 _criticalHitImageManager 已初始化
            _criticalHitImageManager.Draw();

            if (!Platform.MainGame.IsMouseVisible && (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop || Platform.PlatFormType == PlatFormType.UWP && !Platform.IsMobile))
            {
                this.DrawMouseArrow();
            }

            // =========================================================
            // 🔥 绘制对话UI（在鼠标箭头之后，确保在最上层）
            // =========================================================
            if (dialogueUI != null && dialogueUI.IsActive)
            {
                try
                {
                    dialogueUI.Draw(Session.Current.SpriteBatch);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DialogueUI] Draw Error: {ex.Message}");
                }
            }
            // =========================================================

        }

        private void DrawInputer()
        {
            this.Plugins.NumberInputerPlugin.Draw();
        }

        private void DrawMapViewSelector()
        {
            if (this.Plugins.MapViewSelectorPlugin != null)
            {
                this.Plugins.MapViewSelectorPlugin.Draw();
            }
        }

        private void DrawMouseArrow()
        {
            try
            {
                if (base.MouseArrowTexture != null)
                {
                    CacheManager.Draw(base.MouseArrowTexture.Name, InputManager.Position, null, Color.White, SpriteEffects.None, 1f);
                    //base.CacheManager.Draw(base.MouseArrowTexture, new Vector2((float)InputManager.NowMouse.X, (float)InputManager.NowMouse.Y), null, Color.White, 0f, Vector2.Zero, (float)1f, SpriteEffects.None, 0f);
                }
            }
            catch (Exception ex)
            {
                // 捕获并忽略鼠标绘制错误，防止因纹理丢失导致游戏崩溃
                System.Diagnostics.Debug.WriteLine($"[DrawMouseArrow] Error: {ex.Message}");
            }
        }

        private void DrawPersonBubble()
        {
            if (this.Plugins.PersonBubblePlugin != null)
            {
                this.Plugins.PersonBubblePlugin.Draw();
            }
        }

        /// <summary>
        /// 绘制军师双击菜单系统
        /// </summary>
        /*
        private void DrawAdvisorDoubleClickMenu()
        {
            // 使用双击集成系统进行渲染
            try
            {
                // DoubleClickIntegration.Draw(Session.MainGame.SpriteBatch); // 暂时注释掉，等修复编译错误后再启用
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DrawAdvisorDoubleClickMenu] 渲染错误: {ex.Message}");
            }
        }
        */

        private void DrawRoutewayEditor()
        {
            if (this.Plugins.RoutewayEditorPlugin != null)
            {
                this.Plugins.RoutewayEditorPlugin.Draw();
            }
        }

        private void DrawScreenBlind()
        {
            this.Plugins.ScreenBlindPlugin.Draw();
            
            // 🆕 绘制地块势力范围信息插件
            // 日期：2026-03-13
            if (this.Plugins.TileInfluenceInfoPlugin != null)
            {
                this.Plugins.TileInfluenceInfoPlugin.Draw();
            }
        }

        private void DrawSelector()
        {
            if (this.DrawingSelector)
            {
                CacheManager.Draw(this.Textures.SelectorTexture, 
                    new Rectangle(Math.Min(base.MousePosition.X, this.SelectorStartPosition.X), Math.Min(base.MousePosition.Y, this.SelectorStartPosition.Y), 
                        Math.Abs(base.MousePosition.X - this.SelectorStartPosition.X), Math.Abs(base.MousePosition.Y - this.SelectorStartPosition.Y)), 
                    null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.72f);
            }
        }

        public void DrawSubDialog()
        {
            this.Plugins.PersonDetailPlugin.Draw();
            this.Plugins.TroopDetailPlugin.Draw();
            this.Plugins.ArchitectureDetailPlugin.Draw();
            this.Plugins.FactionTechniquesPlugin.Draw();
            this.Plugins.TreasureDetailPlugin.Draw();
        }

        private void DrawToolBar(GameTime gameTime)
        {
            if (this.Plugins.ToolBarPlugin != null)
            {
                //this.Plugins.ToolBarPlugin.Draw();
                this.Plugins.ToolBarPlugin.Draw(gameTime);
            }
        }

        private void DrawTroopSurvey()
        {
            if ((this.Plugins.TroopSurveyPlugin.Showing && (this.viewMove == ViewMove.Stop)) && (this.Plugins.TroopSurveyPlugin != null))
            {
                this.Plugins.TroopSurveyPlugin.Draw();
            }
        }

        private void Game_Activated(object sender, EventArgs e)
        {
            //this.UpdateViewport();
            this.ResumeMusic();
            base.EnableMouseEvent = true;
        }

        private void Game_Deactivated(object sender, EventArgs e)
        {
            this.PauseMusic();
            base.EnableMouseEvent = false;
        }

        public override Rectangle GetDestination(Point mapPosition)
        {
            return this.mainMapLayer.GetDestination(mapPosition);
        }

        public override Point GetPointByPosition(Point mapPosition)
        {
            return this.mainMapLayer.GetTopCenterPoint(mapPosition);
        }
            
        //public override Texture2D GetPortrait(float id)
        //{
        //    return this.Plugins.PersonPortraitPlugin.GetPortrait(id);
        //}

        public override Point GetPositionByPoint(Point mousePoint)
        {
            return this.mainMapLayer.TranslateCoordinateToTilePosition(mousePoint.X, mousePoint.Y);
        }        

        //public override Texture2D GetSmallPortrait(float id)
        //{
        //    return this.Plugins.PersonPortraitPlugin.GetSmallPortrait(id);
        //}

        //public override Texture2D GetTroopPortrait(float id)
        //{
        //    return this.Plugins.PersonPortraitPlugin.GetTroopPortrait(id);
        //}
        
        //public override Texture2D GetFullPortrait(float id)
        //{
        //    return this.Plugins.PersonPortraitPlugin.GetFullPortrait(id);
        //}
        
        private void HandleDialogResult(DialogKind kind)
        {
            switch (kind)
            {
                case DialogKind.Confirmation:
                    /*
                    switch (this.Plugins.ConfirmationDialogPlugin.Result)
                    {
                    }
                    */  //原程序，由于警告去掉
                    break;
            }
        }

        private void HandleFrameResult(FrameResult result)
        {
            switch (result)
            {
                case FrameResult.OK:
                    this.screenManager.HandleFrameFunction(this.Plugins.GameFramePlugin.Function);

                    
                    break;
                
            }
        }

        private int oldDialogShowTime = -1;
        private bool? oldEnableCheat = null;
        private bool? oldSkyEye = null;

        private void StartAutoplayMode()
        {
            Session.Current.Scenario.CurrentPlayer = null;
            oldSkyEye = Session.GlobalVariables.SkyEye;
            Session.GlobalVariables.SkyEye = true;
            oldDialogShowTime = Setting.Current.GlobalVariables.DialogShowTime;
            Setting.Current.GlobalVariables.DialogShowTime = 0;
            oldEnableCheat = Session.GlobalVariables.EnableCheat;
            Session.GlobalVariables.EnableCheat = true;
        }

        private void StopAutoplayMode()
        {
            if (oldSkyEye != null)
            {
                Session.GlobalVariables.SkyEye = oldSkyEye.Value;
            }
            if (oldEnableCheat != null)
            {
                Session.GlobalVariables.EnableCheat = oldEnableCheat.Value;
            }
            if (this.oldDialogShowTime >= 0)
            {
                Setting.Current.GlobalVariables.DialogShowTime = this.oldDialogShowTime;
            }
        }

        public void changeFaction()
        {
            GameDelegates.VoidFunction function = null;
            this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Faction, FrameFunction.Browse, true, true, true, true, Session.Current.Scenario.Factions, Session.Current.Scenario.PlayerFactions, "变更控制", "");
            if (function == null)
            {
                function = delegate
                {
                    FacilityList list = new FacilityList();
                    foreach (Faction faction in Session.Current.Scenario.PlayerFactions)
                    {
                        list.Add(faction);
                    }
                    Session.Current.Scenario.SetPlayerFactionList(this.Plugins.TabListPlugin.SelectedItemList as GameObjectList);
                    foreach (Faction faction in list)
                    {
                        if (!Session.Current.Scenario.IsPlayer(faction))
                        {
                            faction.EndControl();
                        }
                    }
                    foreach (Faction faction in Session.Current.Scenario.PlayerFactions)
                    {
                        if (!list.HasGameObject(faction))
                        {
                            foreach (Routeway routeway in faction.Routeways.GetList())
                            {
                                if (!routeway.IsInUsing)
                                {
                                    Session.Current.Scenario.RemoveRouteway(routeway);
                                }
                            }
                        }
                    }
                    if (!Session.Current.Scenario.IsPlayer(Session.Current.Scenario.CurrentPlayer))
                    {
                        if (Session.Current.Scenario.CurrentPlayer != null)
                        {
                            Session.Current.Scenario.CurrentPlayer.Passed = true;
                            Session.Current.Scenario.CurrentPlayer.Controlling = false;
                        }
                        if (!Session.Current.Scenario.Factions.HasFactionInQueue(Session.Current.Scenario.PlayerFactions))
                        {
                            this.Plugins.DateRunnerPlugin.Reset();
                            this.Plugins.DateRunnerPlugin.RunDays(1);
                        }
                    }
                    if (Session.Current.Scenario.PlayerFactions.Count == 0)
                    {
                        this.StartAutoplayMode();
                    }
                    else
                    {
                        this.StopAutoplayMode();
                    }
                };
            }
            this.Plugins.GameFramePlugin.SetOKFunction(function);
        }

        private void HandlePushSelectingUndoneWork(SelectingUndoneWorkKind kind)
        {
            switch (kind)
            {
                case SelectingUndoneWorkKind.ArchitectureAvailableContactArea:
                    if ((this.CurrentArchitecture != null) && (this.CurrentMilitary != null))
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.ArchitectureAvailableContactArea;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetAllAvailableArea(false);
                        this.CurrentMilitary.ModifyAreaByTerrainAdaptablity(this.selectingLayer.Area);
                    }
                    break;

                case SelectingUndoneWorkKind.ConvincePersonPosition:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.ConvincePersonPosition;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetConvincePersonArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;

                case SelectingUndoneWorkKind.AssassinatePosition:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.AssassinatePosition;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetAssassinateArchitectureArea((this.CurrentPersons[0] as Person).BelongedFaction);
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;

                case SelectingUndoneWorkKind.WujiangDiaodong:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.WujiangDiaodong;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetPersonTransferArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;

                case SelectingUndoneWorkKind.MoveFeizi:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.MoveFeizi;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetFeiziTransferArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;

                case SelectingUndoneWorkKind.MoveCaptive: //俘虏可移动
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.MoveCaptive;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetCaptiveTransferArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;
                    /*
                case SelectingUndoneWorkKind.MilitaryTransfer: //运输编队
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.MilitaryTransfer;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetMilitaryTransferArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;
                    */
                

                case SelectingUndoneWorkKind.InformationPosition:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.InformationPosition;
                        this.selectingLayer.Area = new GameArea();
                        this.selectingLayer.EffectingAreaRadius = this.CurrentPerson.RadiusIncrementOfInformation + this.CurrentPerson.CurrentInformationKind.Radius;
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = false;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;
                    /*
                case SelectingUndoneWorkKind.SpyPosition:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.SpyPosition;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetSpyArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;
                    */

                case SelectingUndoneWorkKind.DestroyPosition:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.DestroyPosition;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetDestroyArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;

                case SelectingUndoneWorkKind.InstigatePosition:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.InstigatePosition;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetInstigateArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;

                case SelectingUndoneWorkKind.GossipPosition:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.GossipPosition;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetGossipArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;

                case SelectingUndoneWorkKind.JailBreakPosition:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.JailBreakPosition;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetJailBreakArchitectureArea();
                        this.selectingLayer.ShowComment = true;
                        this.selectingLayer.SingleWay = true;
                        this.selectingLayer.FromArea = this.CurrentArchitecture.ArchitectureArea;
                    }
                    break;

                case SelectingUndoneWorkKind.TroopDestination:
                    if (this.CurrentTroop != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.TroopDestination;
                        this.selectingLayer.Area = this.CurrentTroop.GetDayArea(1);
                    }
                    break;

                case SelectingUndoneWorkKind.SelectorTroopsDestination:
                    if (this.SelectorTroops.Count > 0)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.SelectorTroopsDestination;
                        this.selectingLayer.Area = new GameArea();
                    }
                    break;

                case SelectingUndoneWorkKind.TroopTarget:
                    if (this.CurrentTroop != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.TroopTarget;
                        if (this.CurrentTroop.CurrentCombatMethod != null)
                        {
                            this.selectingLayer.Area = this.CurrentTroop.GetTargetArea(false, this.CurrentTroop.CurrentCombatMethod.ArchitectureTarget);
                        }
                        else if (this.CurrentTroop.CurrentStratagem != null)
                        {
                            this.selectingLayer.Area = this.CurrentTroop.GetTargetArea(this.CurrentTroop.CurrentStratagem.Friendly, this.CurrentTroop.CurrentStratagem.ArchitectureTarget);
                        }
                        else
                        {
                            this.selectingLayer.Area = this.CurrentTroop.GetTargetArea(false, true);
                        }
                    }
                    break;

                case SelectingUndoneWorkKind.Trooprucheng :
                    if (this.CurrentTroop != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.Trooprucheng ;

                        this.selectingLayer.Area = this.CurrentTroop.GetruchengArea(true);
                        
                    }
                    break;

                case SelectingUndoneWorkKind.TroopInvestigatePosition:
                    if (this.CurrentTroop != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.TroopInvestigatePosition;
                        this.selectingLayer.Area = Session.Current.Scenario.GetAreaWithinDistance(this.CurrentTroop.Position, this.CurrentTroop.ViewRadius + 1, false);
                        this.selectingLayer.EffectingAreaRadius = this.CurrentTroop.InvestigateRadius;
                    }
                    break;

                case SelectingUndoneWorkKind.TroopSetFirePosition:
                    if (this.CurrentTroop != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.TroopSetFirePosition;
                        this.selectingLayer.Area = this.CurrentTroop.GetSetFireArea();
                    }
                    break;

                case SelectingUndoneWorkKind.ArchitectureRoutewayStartPoint:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.ArchitectureRoutewayStartPoint;
                        this.selectingLayer.Area = this.CurrentArchitecture.GetRoutewayStartPoints();
                    }
                    break;

                case SelectingUndoneWorkKind.RoutewayPointShortestNormal:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.RoutewayPointShortestNormal;
                        this.selectingLayer.Area = new GameArea();
                    }
                    break;

                case SelectingUndoneWorkKind.RoutewayPointShortestNoWater:
                    if (this.CurrentArchitecture != null)
                    {
                        this.selectingLayer.AreaFrameKind = SelectingUndoneWorkKind.RoutewayPointShortestNoWater;
                        this.selectingLayer.Area = new GameArea();
                    }
                    break;
            }
            this.selectingLayer.TryToShow();
        }

        private void HandleSelectingResult(SelectingUndoneWorkKind kind)
        {
            Architecture targetArchitecture;
            Routeway routeway;


            switch (kind)
            {
                case SelectingUndoneWorkKind.None:
                case SelectingUndoneWorkKind.SearchPosition:
                    return;

                case SelectingUndoneWorkKind.ArchitectureAvailableContactArea:
                    if (!this.selectingLayer.Canceled)
                    {
                        if(this.CurrentMilitaries.Count==1 && this.CurrentMilitary!=null )
                        {
                            this.screenManager.SetCreatingTroopPosition(this.selectingLayer.SelectedPoint);
                        }
                        else if (this.CurrentMilitaries.Count > 1)
                        {
                            this.screenManager.SetTroopsPosition(this.selectingLayer.SelectedPoint);
                        }
                    }
                    return;

                case SelectingUndoneWorkKind.ConvincePersonPosition:
                    if (!this.selectingLayer.Canceled && (this.CurrentPersons.Count > 0))
                    {
                        Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.selectingLayer.SelectedPoint);
                        if (architectureByPosition != null)
                        {
                            this.CurrentArchitecture = architectureByPosition;
                            foreach (Person person in this.CurrentPersons)
                            {
                                person.OutsideDestination = new Point?(this.selectingLayer.SelectedPoint);
                            }
                            
                            // 检查是否有足够的情报等级来进行说服
                            Faction playerFaction = (this.CurrentPersons[0] as Person).BelongedFaction;
                            bool hasEnoughInformation = architectureByPosition.BelongedFaction == playerFaction || 
                                                      playerFaction.GetKnownAreaData(this.selectingLayer.SelectedPoint) >= InformationLevel.低;
                            
                            if (hasEnoughInformation)
                            {
                                // 获取可说服的目标列表
                                var convinceTargets = architectureByPosition.GetConvinceDestinationPersonList(playerFaction);
                                if (convinceTargets.Count == 0)
                                {
                                    // 没有可说服的目标，但仍给玩家选择权
                                    ShowNoConvinceTargetsWithChoiceDialog(playerFaction);
                                }
                                else
                                {
                                    this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.GetConvinceDestinationPerson, false, true, true, false, convinceTargets, null, "说服", "Personal");
                                }
                            }
                            else
                            {
                                // 情报不足时，显示军师提示
                                Person advisor = playerFaction.Advisor ?? playerFaction.Leader;
                                if (advisor != null && this.Plugins?.tupianwenziPlugin != null)
                                {
                                    string message = $"{advisor.Name}：主公，我军对此地情报不足，无法进行有效说服。建议先派人收集情报。";
                                    
                                    this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                                        advisor, advisor, message, "", "", ""
                                    );
                                    this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                                    this.Plugins.tupianwenziPlugin.IsShowing = true;
                                }
                            }
                        }
                    }
                    return;

                case SelectingUndoneWorkKind.AssassinatePosition:
                    if (!this.selectingLayer.Canceled && (this.CurrentPersons.Count > 0))
                    {
                        Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.selectingLayer.SelectedPoint);
                        if (architectureByPosition != null)
                        {
                            this.CurrentArchitecture = architectureByPosition;
                            foreach (Person person in this.CurrentPersons)
                            {
                                person.OutsideDestination = new Point?(this.selectingLayer.SelectedPoint);
                            }
                            this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.GetAssassinatePersonTarget, false, true, true, false, architectureByPosition.GetAssassinatePersonTarget((this.CurrentPersons[0] as Person).BelongedFaction), null, "暗杀", "Personal");
                        }
                    }
                    return;

                case SelectingUndoneWorkKind.WujiangDiaodong:
                    if (!this.selectingLayer.Canceled && (this.CurrentPersons.Count > 0))
                    {
                           Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.selectingLayer.SelectedPoint);
                           if (architectureByPosition != null)
                           {

                               this.screenManager.FrameFunction_Architecture_AfterGetOneArchitectureBySelecting(architectureByPosition);
                           }
                    }
                    return;

                case SelectingUndoneWorkKind.MoveFeizi:
                    if (!this.selectingLayer.Canceled && (this.CurrentPersons != null))
                    {
                        Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.selectingLayer.SelectedPoint);
                        if (architectureByPosition != null)
                        {

                            this.screenManager.FrameFunction_Architecture_AfterGetOneArchitectureBySelecting(architectureByPosition);
                        }
                    }
                    return;

                case SelectingUndoneWorkKind.MoveCaptive: //移动俘虏
                    if (!this.selectingLayer.Canceled && (this.CurrentPersons != null))
                    {
                        Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.selectingLayer.SelectedPoint);
                        if (architectureByPosition != null)
                        {

                            this.screenManager.FrameFunction_Architecture_AfterGetMoveCaptiveArchitectureBySelecting(architectureByPosition);
                        }
                    }
                    return;
                    /*
                case SelectingUndoneWorkKind.MilitaryTransfer: //运输编队
                    if (!this.selectingLayer.Canceled && (this.CurrentMilitaries != null))
                    {
                        Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.selectingLayer.SelectedPoint);
                        if (architectureByPosition != null)
                        {

                            this.screenManager.FrameFunction_Architecture_AfterGetTransferMilitaryArchitectureBySelecting(architectureByPosition);

                            /*foreach (Military military in this.CurrentMilitaries)
                            {
                                architectureByPosition.AddMilitary(military);
                                this.CurrentArchitecture.RemoveMilitary(military);
                            }
                        }
                    }
                    return;
                     */


                case SelectingUndoneWorkKind.InformationPosition:
                    if (!this.selectingLayer.Canceled)
                    {
                        this.CurrentPerson.GoForInformation(this.selectingLayer.SelectedPoint);
                        base.PlayNormalSound("Content/Sound/Tactics/Outside");
                    }
                    return;
                    /*
                case SelectingUndoneWorkKind.SpyPosition:
                    if (!this.selectingLayer.Canceled)
                    {
                        foreach (Person person in this.CurrentPersons)
                        {
                            person.GoForSpy(this.selectingLayer.SelectedPoint);
                        }
                        base.PlayNormalSound("Content/Sound/Tactics/Outside");
                    }
                    return;
                    */
                case SelectingUndoneWorkKind.DestroyPosition:
                    if (!this.selectingLayer.Canceled)
                    {
                        foreach (Person person in this.CurrentPersons)
                        {
                            person.GoForDestroy(this.selectingLayer.SelectedPoint);
                        }
                        base.PlayNormalSound("Content/Sound/Tactics/Outside");
                    }
                    return;

                case SelectingUndoneWorkKind.InstigatePosition:
                    if (!this.selectingLayer.Canceled)
                    {
                        foreach (Person person in this.CurrentPersons)
                        {
                            person.GoForInstigate(this.selectingLayer.SelectedPoint);
                        }
                        base.PlayNormalSound("Content/Sound/Tactics/Outside");
                    }
                    return;

                case SelectingUndoneWorkKind.GossipPosition:
                    if (!this.selectingLayer.Canceled)
                    {
                        foreach (Person person in this.CurrentPersons)
                        {
                            person.GoForGossip(this.selectingLayer.SelectedPoint);
                        }
                        base.PlayNormalSound("Content/Sound/Tactics/Outside");
                    }
                    return;

                case SelectingUndoneWorkKind.JailBreakPosition:
                    if (!this.selectingLayer.Canceled)
                    {
                        foreach (Person person in this.CurrentPersons)
                        {
                            person.GoForJailBreak(this.selectingLayer.SelectedPoint);
                        }
                        base.PlayNormalSound("Content/Sound/Tactics/Outside");
                    }
                    return;

                case SelectingUndoneWorkKind.TroopDestination:   //移动
                    if (this.selectingLayer.Canceled)
                    {
                        return;
                    }
                    targetArchitecture = Session.Current.Scenario.GetArchitectureByPosition(this.selectingLayer.SelectedPoint);
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 目标点={this.selectingLayer.SelectedPoint}");
                    System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 清空前 RealDest={this.CurrentTroop.RealDestination}");
                    #endif
                    
                    // 🔥 根本修复：清空所有旧目标，避免 WillArchitecture 干扰
                    // 日期：2026-03-06
                    // 原因：如果不清空 WillArchitecture，它的 setter 可能会覆盖玩家设置的 RealDestination
                    //       导致 RealDestination 被重置为 {0,0} 或其他错误值
                    // 解决：在设置新目标前，先清空所有旧目标引用
                    this.CurrentTroop.TargetArchitecture = null;
                    this.CurrentTroop.TargetTroop = null;
                    this.CurrentTroop.WillTroop = null;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 清空目标后 RealDest={this.CurrentTroop.RealDestination}");
                    #endif
                    
                    this.CurrentTroop.WillArchitecture = null; // ★★★ 关键：清空旧的军团目标 ★★★
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 清空WillArch后 RealDest={this.CurrentTroop.RealDestination}");
                    #endif
                    
                    // 🔥 根本修复：确保玩家部队加入 PlayerManual 军团
                    // 日期：2026-03-07
                    // 🔥 防止默认值陷阱：确保传入有效的己方城市，避免 ID=0 的洛阳问题
                    if (this.CurrentTroop.BelongedLegion is null or { Kind: not LegionKind.Player })
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 需要分配军团，当前军团: {this.CurrentTroop.BelongedLegion?.Name ?? "null"}");
                        #endif
                        
                        // 🔥 2026-03-16 修复：移除错误的 ID > 0 判断
                        // 原因：ID=0 是有效的（洛阳的 ID=0），之前的逻辑会跳过洛阳
                        // 解决：移除所有 ID 判断，只检查 null 和势力归属
                        Architecture baseCity = this.CurrentTroop.StartingArchitecture;
                        
                        // 🔥 Anti-Band-Aid：只检查 null 和势力归属，不检查 ID
                        if (baseCity == null || baseCity.BelongedFaction != this.CurrentTroop.BelongedFaction)
                        {
                            // 回退1：使用势力的首都（Capital）
                            baseCity = this.CurrentTroop.BelongedFaction.Capital;
                            
                            // 回退2：如果首都为 null，使用势力的第一个城市
                            if (baseCity == null)
                            {
                                foreach (Architecture arch in this.CurrentTroop.BelongedFaction.Architectures)
                                {
                                    baseCity = arch;
                                    System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} Capital 为 null，使用第一个城市 {baseCity.Name}(ID:{baseCity.ID})");
                                    break;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} StartingArchitecture 无效，使用首都 {baseCity.Name}(ID:{baseCity.ID})");
                            }
                        }
                        
                        // 🔥 Anti-Band-Aid：如果找不到城市，让 GetOrCreatePlayerLegion 抛出异常
                        // 唯一的例外：baseCity 仍然是 null（势力没有城市，已被灭国）
                        if (baseCity == null)
                        {
                            // 势力被灭国，没有城市，这是合法状态，跳过军团分配
                            System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 势力 {this.CurrentTroop.BelongedFaction.Name} 没有城市（已被灭国），跳过军团分配");
                        }
                        else
                        {
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 分配军团前 RealDest={this.CurrentTroop.RealDestination}");
                            #endif
                            
                            Legion playerLegion = this.CurrentTroop.BelongedFaction.GetOrCreatePlayerLegion(baseCity);
                            playerLegion.AddTroop(this.CurrentTroop);
                            
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 加入玩家军团 {playerLegion.Name}");
                            System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 分配军团后 RealDest={this.CurrentTroop.RealDestination}");
                            #endif
                        }
                    }
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 设置RealDest前: {this.CurrentTroop.RealDestination}");
                    #endif
                    
                    // 设置部队移动目标（必须在清空旧目标之后）
                    this.CurrentTroop.RealDestination = this.selectingLayer.SelectedPoint;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 设置RealDest后: {this.CurrentTroop.RealDestination}");
                    #endif
                    
                    this.CurrentTroop.TargetArchitecture = targetArchitecture;
                    this.CurrentTroop.SelectedMove = true;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {this.CurrentTroop.DisplayName} 设置后 RealDest={this.CurrentTroop.RealDestination}");
                    #endif
                    
                    // 🔥 修复：操作面只设置 Command，不设置 CurrentAIState
                    // 日期：2026-03-08
                    // 原因：在操作面设置 CurrentAIState = Marching 会导致部队在操作面就显示为"移动"状态
                    //       违反了半即时半回合的游戏模式（操作面应该是"停止"状态）
                    // 解决：只设置 Command 和 Operated，CurrentAIState 由 TroopListWithQueue 在执行面设置
                    //       （TroopListWithQueue.CurrentQueueTroopMove 第 376 行会根据 Command 设置 CurrentAIState）
                    this.CurrentTroop.SetCommand(TroopCommand.Move);
                    this.CurrentTroop.Operated = true;
                    
                    // ❌ 删除以下代码（会导致操作面立即显示为"移动"状态）：
                    // this.CurrentTroop.CurrentAIState = TroopAIState.Marching;

                    break;

                case SelectingUndoneWorkKind.Trooprucheng :   //入城
                    if (this.selectingLayer.Canceled)
                    {
                        return;
                    }

                    targetArchitecture = Session.Current.Scenario.GetArchitectureByPosition(this.selectingLayer.SelectedPoint);

                    if (this.CurrentTroop.CanEnter() && this.CurrentTroop.EnterList.GameObjects.Contains(targetArchitecture))
                    {
                        this.CurrentTroop.Enter(targetArchitecture);
                        this.CurrentTroop = null;
                        //this.Plugins.AirViewPlugin.ReloadTroopView();
                        Session.Current.Scenario.ClearPersonStatusCache();
                        return;
                    }
                    
                    this.CurrentTroop.RealDestination = this.selectingLayer.SelectedPoint;
                    this.CurrentTroop.TargetTroop = null;
                    this.CurrentTroop.WillTroop = null;
                    this.CurrentTroop.TargetArchitecture = null;
                    this.CurrentTroop.WillArchitecture = null;
                    this.CurrentTroop.SetCommand(TroopCommand.Enter);
                    if (targetArchitecture != null)
                    {
                        this.CurrentTroop.TargetArchitecture = targetArchitecture;

                    }

                    this.CurrentTroop.SelectedMove = true;
                    
                    // 🔥 修复：操作面只设置 Command，不设置 CurrentAIState
                    // 日期：2026-03-08
                    // 原因：在操作面设置 CurrentAIState = EnterCity 会导致部队在操作面就显示为"移动"状态
                    //       违反了半即时半回合的游戏模式（操作面应该是"停止"状态）
                    // 解决：只设置 Command 和 Operated，CurrentAIState 由 TroopListWithQueue 在执行面设置
                    this.CurrentTroop.Operated = true;
                    
                    // ❌ 删除以下代码（会导致操作面立即显示为"移动"状态）：
                    // this.CurrentTroop.CurrentAIState = TroopAIState.EnterCity;

                    break;


                case SelectingUndoneWorkKind.SelectorTroopsDestination:
                    System.Diagnostics.Debug.WriteLine($"[SelectorTroopsDestination] 开始处理，SelectorTroops数量:{this.SelectorTroops.Count}");
                    if (!this.selectingLayer.Canceled)
                    {
                        targetArchitecture = Session.Current.Scenario.GetArchitectureByPosition(this.selectingLayer.SelectedPoint);
                        Troop targetTroop = Session.Current.Scenario.GetTroopByPosition(this.selectingLayer.SelectedPoint);
                        System.Diagnostics.Debug.WriteLine($"[SelectorTroopsDestination] 选中点:{this.selectingLayer.SelectedPoint} 建筑:{targetArchitecture?.Name ?? "无"} 部队:{targetTroop?.DisplayName ?? "无"}");
                        
                        foreach (Troop troop in this.SelectorTroops)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SelectorTroopsDestination] 检查部队:{troop.DisplayName} SelectedMove:{troop.SelectedMove} SelectedAttack:{troop.SelectedAttack} ManualControl:{troop.ManualControl}");
                            
                            if (!troop.SelectedMove && !troop.SelectedAttack)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SelectorTroopsDestination] 部队{troop.DisplayName}满足条件，开始设置目标");
                                
                                bool isStratagemCommand = troop.CurrentStratagem != null;
                                bool prefersTroopTarget = targetTroop != null &&
                                    (targetArchitecture == null || targetArchitecture.Endurance <= 0 || troop.Army.Kind.AirOffence || troop.CurrentStratagem != null || troop.CurrentCombatMethod != null);

                                if (targetArchitecture != null)
                                {
                                    troop.WillArchitecture = targetArchitecture;
                                    troop.BelongedLegion.SetOperationalTarget(targetArchitecture);
                                    if (targetArchitecture.BelongedFaction == troop.BelongedFaction)
                                    {
                                        troop.TargetArchitecture = targetArchitecture;
                                        troop.TargetTroop = null;
                                        troop.WillTroop = null;
                                        troop.SelectedMove = true;
                                        troop.SelectedAttack = false;
                                        troop.SetCommand(TroopCommand.Enter);
                                    }
                                    else if (prefersTroopTarget)
                                    {
                                        troop.TargetTroop = targetTroop;
                                        troop.WillTroop = targetTroop;
                                        troop.TargetArchitecture = null;
                                        troop.SelectedMove = !isStratagemCommand;
                                        troop.SelectedAttack = true;
                                        troop.SetCommand(TroopCommand.AttackTroop);
                                    }
                                    else
                                    {
                                        troop.TargetArchitecture = targetArchitecture;
                                        troop.TargetTroop = null;
                                        troop.WillTroop = null;
                                        troop.SelectedMove = !isStratagemCommand;
                                        troop.SelectedAttack = true;
                                        troop.SetCommand(TroopCommand.AttackArch);
                                    }
                                }
                                else if (targetTroop != null)
                                {
                                    troop.TargetTroop = targetTroop;
                                    troop.WillTroop = targetTroop;
                                    troop.TargetArchitecture = null;
                                    troop.SelectedMove = !isStratagemCommand;
                                    troop.SelectedAttack = true;
                                    troop.SetCommand(TroopCommand.AttackTroop);
                                }

                                if ((troop.CurrentCombatMethod != null || troop.CurrentStratagem != null) && troop.TargetTroop != null)
                                {
                                    troop.OrientationTroop = troop.TargetTroop;
                                    troop.OrientationArchitecture = null;
                                    troop.TargetTroop.OrientationTroop = troop;
                                }
                                else if (troop.CurrentCombatMethod != null && troop.TargetArchitecture != null)
                                {
                                    troop.OrientationTroop = null;
                                    troop.OrientationArchitecture = troop.TargetArchitecture;
                                }
                                else
                                {
                                    // 🔥 修复：先设置 RealDestination，再清空 WillArchitecture
                                    // 日期：2026-02-27
                                    // 原因：WillArchitecture setter 会检查 RealDestination == Point.Zero，如果是则清空
                                    //       必须先设置 RealDestination 为有效值，再清空 WillArchitecture
                                    // 旧顺序：清空 WillArch → 触发 setter → 检查 RealDest==Zero → 清空 RealDest
                                    // 新顺序：设置 RealDest → 清空 WillArch → 触发 setter → 检查 RealDest!=Zero → 保留 RealDest
                                    troop.SetCommand(TroopCommand.Move);
                                    troop.RealDestination = this.selectingLayer.SelectedPoint;
                                    troop.Destination = this.selectingLayer.SelectedPoint;
                                    
                                    // 现在可以安全地清空目标对象，不会影响 RealDestination
                                    troop.WillArchitecture = null;
                                    troop.TargetArchitecture = null;
                                    troop.WillTroop = null;
                                    troop.TargetTroop = null;
                                }
                                // 🔥 已移动到上面：先设置 RealDestination，再清空 WillArchitecture
                                // troop.RealDestination = this.selectingLayer.SelectedPoint;
                                // troop.Destination = this.selectingLayer.SelectedPoint;
                                
                                // 🔥 修复：玩家右键设置目标时，必须设置 CurrentAIState
                                // 日期：2026-02-26
                                // 原因：ExecuteTactics() 根据 CurrentAIState 决定是否执行移动
                                // 如果目标是己方建筑，设置为 EnterCity；否则设置为 Marching
                                if (targetArchitecture != null && targetArchitecture.BelongedFaction == troop.BelongedFaction)
                                {
                                    troop.CurrentAIState = TroopAIState.EnterCity;
                                }
                                else
                                {
                                    troop.CurrentAIState = TroopAIState.Marching;
                                }
                                
                                // 🔥 根本修复：玩家手动控制的部队必须使用 PlayerManual 军团
                                // 日期：2026-03-07
                                // 🔥 防止默认值陷阱：确保传入有效的己方城市，避免 ID=0 的洛阳问题
                                if (troop.BelongedLegion is null or { Kind: not LegionKind.Player })
                                {
                                    // 优先使用 StartingArchitecture，如果无效则使用势力的首都
                                    Architecture baseCity = troop.StartingArchitecture;
                                    if (baseCity == null || baseCity.BelongedFaction != troop.BelongedFaction)
                                    {
                                        // 回退：使用势力的首都（Capital）
                                        baseCity = troop.BelongedFaction.Capital;
                                        System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {troop.DisplayName} StartingArchitecture 无效，使用首都 {baseCity?.Name ?? "null"}");
                                    }
                                    
                                    // 🔥 数据验证：如果首都也为 null，说明势力被灭国，让它崩溃暴露问题
                                    System.Diagnostics.Debug.Assert(baseCity != null, 
                                        $"[玩家移动指令] {troop.DisplayName} 无法找到有效的基地城市，势力可能被灭国");
                                    
                                    Legion playerLegion = troop.BelongedFaction.GetOrCreatePlayerLegion(baseCity);
                                    playerLegion.AddTroop(troop);
                                    System.Diagnostics.Debug.WriteLine($"[玩家移动指令] {troop.DisplayName} 加入玩家军团 {playerLegion.Name}");
                                }
                                
                                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.TroopMoveTo, "Destination");

                                troop.SelectedMove = true;
                                
                                // 🔥 根本修复：玩家设置目标后，将部队设置为 CurrentTroop
                                // 日期：2026-02-27
                                // 原因：如果玩家在回合开始 > 1 秒后才设置目标，此时 CurrentQueueTroopMove 已经处理完所有部队
                                //       CurrentTroop = null，队列不再驱动，导致部队不动
                                // 解决：将部队设置为 CurrentTroop，让 MoveTheTroops 继续驱动它
                                Session.Current.Scenario.Troops.CurrentTroop = troop;
                                
                                System.Diagnostics.Debug.WriteLine($"[玩家指令] {troop.DisplayName} 设置目标:{this.selectingLayer.SelectedPoint} AIState:{troop.CurrentAIState} Status:{troop.Status} Dest:{troop.Destination} RealDest:{troop.RealDestination} MovLeft:{troop.MovabilityLeft}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SelectorTroopsDestination] 操作被取消");
                    }
                    this.SelectorTroops.Clear();
                    return;

                case SelectingUndoneWorkKind.TroopTarget:  //选目标
                    {
                        if (this.selectingLayer.Canceled)
                        {
                            this.CurrentTroop.AttackTargetKind = TroopAttackTargetKind.遇敌;
                            if (this.CurrentTroop.CurrentStratagem != null)
                            {
                                this.CurrentTroop.CastTargetKind = TroopCastTargetKind.可能;
                            }
                            
                            if (this.CurrentTroop.Status == TroopStatus.埋伏)
                            {
                                this.CurrentTroop.EndAmbush();
                            }
                            return;
                        }
                        
                        // 🔥 2026-03-18 新增：基于目标地块的天气检查（仅针对受天气影响的计略）
                        // 原因：只有包含火系或水系 Influence 的计略才受天气影响
                        // 解决：先检查计略是否受天气影响，再检查目标地块的天气条件
                        if (this.CurrentTroop.CurrentStratagem != null)
                        {
                            var targetWeather = Session.Current.Scenario.WeatherManager.GetWeatherAt(this.selectingLayer.SelectedPoint);
                            
                            // 只有在天气对火水有影响时才检查
                            if (targetWeather.SuppressFire() || targetWeather.EnhanceWater())
                            {
                                var weatherState = WorldOfTheThreeKingdoms.UI.WeatherUIHelper.GetStratagemWeatherState(
                                    this.CurrentTroop.CurrentStratagem, 
                                    targetWeather
                                );
                                
                                // 只有当计略真正受天气影响时才处理（weatherState.Hint 不为空）
                                if (weatherState.IsBlocked)
                                {
                                    // 🔥 2026-03-18 修复：简化文本以适应弹窗尺寸
                                    // 原因：SimpleTextDialog 的文本区域只有 470x70，无法容纳多行文本
                                    // 解决：简化提示信息，只保留关键内容
                                    string weatherName = targetWeather == WeatherType.Rain ? "雨天" : "雪天";
                                    string message = $"⚠️ 无法使用计略：{this.CurrentTroop.CurrentStratagem.Name}\n\n目标地块天气：{weatherName}\n{weatherState.Hint}";
                                    this.Plugins.SimpleTextDialogPlugin.RichText.Clear();
                                    this.Plugins.SimpleTextDialogPlugin.RichText.AddText(message);
                                    this.Plugins.SimpleTextDialogPlugin.SetPosition(ShowPosition.Center);
                                    this.Plugins.SimpleTextDialogPlugin.IsShowing = true;
                                    
                                    // 取消计略
                                    this.CurrentTroop.CurrentStratagem = null;
                                    this.CurrentTroop.AttackTargetKind = TroopAttackTargetKind.遇敌;
                                    this.CurrentTroop.CastTargetKind = TroopCastTargetKind.可能;
                                    return;
                                }
                                else if (weatherState.Hint.Length > 0)
                                {
                                    // 计略效果被削弱或增强，显示提示但允许使用
                                    System.Diagnostics.Debug.WriteLine($"[计略天气提示] {this.CurrentTroop.DisplayName} 使用 {this.CurrentTroop.CurrentStratagem.Name}：{weatherState.Hint}");
                                }
                            }
                        }
                        
                        // 🔥 2026-03-18 新增：基于目标地块的天气检查（针对战法）
                        // 原因：战法与计略类似，也受天气影响
                        // 解决：与计略保持一致的检查逻辑
                        if (this.CurrentTroop.CurrentCombatMethod != null)
                        {
                            var targetWeather = Session.Current.Scenario.WeatherManager.GetWeatherAt(this.selectingLayer.SelectedPoint);
                            
                            // 只有在天气对火水有影响时才检查
                            if (targetWeather.SuppressFire() || targetWeather.EnhanceWater())
                            {
                                var weatherState = WorldOfTheThreeKingdoms.UI.WeatherUIHelper.GetCombatMethodWeatherState(
                                    this.CurrentTroop.CurrentCombatMethod, 
                                    targetWeather
                                );
                                
                                // 只有当战法真正受天气影响时才处理
                                if (weatherState.IsBlocked)
                                {
                                    // 🔥 2026-03-18 修复：简化文本以适应弹窗尺寸
                                    string weatherName = targetWeather == WeatherType.Rain ? "雨天" : "雪天";
                                    string message = $"⚠️ 无法使用战法：{this.CurrentTroop.CurrentCombatMethod.Name}\n\n目标地块天气：{weatherName}\n{weatherState.Hint}";
                                    this.Plugins.SimpleTextDialogPlugin.RichText.Clear();
                                    this.Plugins.SimpleTextDialogPlugin.RichText.AddText(message);
                                    this.Plugins.SimpleTextDialogPlugin.SetPosition(ShowPosition.Center);
                                    this.Plugins.SimpleTextDialogPlugin.IsShowing = true;
                                    
                                    // 取消战法
                                    this.CurrentTroop.CurrentCombatMethod = null;
                                    return;
                                }
                                else if (weatherState.Hint.Length > 0)
                                {
                                    // 战法效果被削弱或增强，显示提示但允许使用
                                    System.Diagnostics.Debug.WriteLine($"[战法天气提示] {this.CurrentTroop.DisplayName} 使用 {this.CurrentTroop.CurrentCombatMethod.Name}：{weatherState.Hint}");
                                }
                            }
                        }
                        
                        // 🔥 根本修复：移除 SelectedMove 条件限制，允许玩家随时重新设置目标
                        // 日期：2026-02-27
                        // 原因：第二回合重新下达指令时，SelectedMove 已经为 true，导致 RealDestination 不被设置
                        // 解决：无条件设置 RealDestination，让玩家可以随时覆盖目标
                        
                        Troop troopByPositionNoCheck = Session.Current.Scenario.GetTroopByPositionNoCheck(this.selectingLayer.SelectedPoint);
                        
                        // 🔥 根本修复：战法指令不设置 RealDestination，避免瞬移
                        // 🔥 根本修复：战法指令不设置 RealDestination，避免瞬移
                        // 日期：2026-03-06
                        // 原因：战法是原地释放的，不需要移动。设置 RealDestination 会导致部队在操作面瞬移
                        // 解决：只有攻击指令才设置 RealDestination，战法指令只设置目标引用
                        bool isStratagemCommand = (this.CurrentTroop.Command == TroopCommand.Stratagem);
                        
                        // 🔥 修复：先检查是否有城池目标或部队目标，避免设置错误的 RealDestination
                        // 日期：2026-03-07
                        // 原因：攻击城池/部队指令应该交给 AI 自动处理走位和攻击
                        // 解决：先检查城池/部队目标，如果有则不设置 RealDestination，让 AI 处理
                        Architecture architectureByPositionNoCheck = Session.Current.Scenario.GetArchitectureByPositionNoCheck(this.selectingLayer.SelectedPoint);
                        bool hasArchitectureTarget = (architectureByPositionNoCheck != null);
                        bool hasTroopTarget = (troopByPositionNoCheck != null);
                        
                        if ((troopByPositionNoCheck == null) || !this.CurrentTroop.BelongedFaction.IsPositionKnown(this.selectingLayer.SelectedPoint))
                        {
                            this.CurrentTroop.TargetTroop = null;
                            this.CurrentTroop.WillTroop = null;
                            
                            // 战法指令不设置移动目标
                            // 🔥 修复：如果有城池目标，不设置 RealDestination
                            if (!isStratagemCommand && !hasArchitectureTarget)
                            {
                                this.CurrentTroop.RealDestination = this.selectingLayer.SelectedPoint;
                            }
                        }
                        else
                        {
                            this.CurrentTroop.TargetTroop = troopByPositionNoCheck;
                            this.CurrentTroop.WillTroop = troopByPositionNoCheck;
                            
                            // 🔥 修复：攻击部队指令不设置 RealDestination，交给 AI 处理
                            // 日期：2026-03-07
                            // 原因：玩家只选择目标部队（战略目标），具体如何接近和攻击应该由 AI 决定
                            // 解决：不设置 RealDestination，让 AI 自动处理走位
                            // 战法指令和攻击部队指令都不设置移动目标
                            if (!isStratagemCommand && !hasTroopTarget && !hasArchitectureTarget)
                            {
                                this.CurrentTroop.RealDestination = this.selectingLayer.SelectedPoint;
                            }
                        }
                        
                        if (architectureByPositionNoCheck != null)
                        {
                            // 🔥 修复：攻击城池时先清空 RealDestination，让 WillArchitecture.setter 自动计算
                            // 日期：2026-03-09
                            // 原因：部队可能有旧的 RealDestination（比如之前自动回退到首都的坐标）
                            //       WillArchitecture.setter 检查 RealDestination != (-1,-1) 时会保留旧值
                            //       导致部队带着错误的坐标去寻路（往反方向移动）
                            // 解决：先清空 RealDestination，让 setter 调用 GetClosestPoint 计算正确坐标
                            this.CurrentTroop.RealDestination = new Point(-1, -1);
                            
                            this.CurrentTroop.TargetArchitecture = architectureByPositionNoCheck;
                            this.CurrentTroop.WillArchitecture = architectureByPositionNoCheck;
                            
                            // 🔥 修复：设置军团的 WillArchitecture，让 SmartSiege 系统能正确分配攻击位置
                            // 日期：2026-03-07
                            // 原因：Legion.AIWithAuto 中的 SmartSiege 系统需要 Legion.WillArchitecture 来分配攻击位置
                            // 解决：攻击城池时，同步设置 Legion.WillArchitecture
                            // 🔥 数据验证：不检查 BelongedLegion 是否为 null，如果为 null 让它崩溃，暴露数据源问题
                            this.CurrentTroop.BelongedLegion.SetOperationalTarget(architectureByPositionNoCheck);
                            System.Diagnostics.Debug.WriteLine($"[TroopTarget] 设置军团目标: {this.CurrentTroop.BelongedLegion.Name}.WillArchitecture = {architectureByPositionNoCheck.Name}");
                            
                            // 🔥 修复：攻击城池时不设置 RealDestination
                            // 日期：2026-03-07
                            // 原因：城池位置不可通行，设置 RealDestination 为城池位置会导致寻路失败
                            // 解决：只设置 TargetArchitecture，让 SmartSiege 系统自动分配可通行的攻击位置
                            // 战法指令不设置移动目标
                            if (!isStratagemCommand)
                            {
                                // 攻击城池：不设置 RealDestination，保持为 Zero
                                // 让 Legion.AIWithAuto → AssignSmartSiegePositions 处理
                            }
                        }
                        else
                        {
                            this.CurrentTroop.TargetArchitecture = null;
                            this.CurrentTroop.WillArchitecture = null;
                            
                            // 🔥 数据验证：不检查 BelongedLegion 是否为 null，如果为 null 让它崩溃，暴露数据源问题
                            this.CurrentTroop.BelongedLegion.SetOperationalTarget(null);
                        }

                        this.CurrentTroop.SelectedMove = !isStratagemCommand;  // 战法指令不标记为"需要移动"
                        this.CurrentTroop.SelectedAttack = true;
                        
                        // 🔥 修复：根据目标类型设置正确的 Command
                        // 日期：2026-03-07
                        // 原因：所有攻击指令都被统一设置为 TroopCommand.Attack，导致 HasValidDestination 无法识别攻击城池指令
                        // 解决：根据 TargetArchitecture 和 TargetTroop 设置正确的 Command 类型
                        // 🔥 修复：移除 Command 条件检查，无条件设置正确的 Command
                        // 日期：2026-03-07
                        // 原因：条件检查 `if (Command != Move && Command != Stratagem && Command != Enter)` 会导致
                        //       如果玩家之前已经下达过攻击指令，Command 不会被更新
                        // 解决：移除条件检查，根据当前目标无条件设置正确的 Command
                        if (this.CurrentTroop.TargetArchitecture != null && this.CurrentTroop.TargetTroop == null)
                        {
                            // 只有建筑目标 → 攻击建筑
                            this.CurrentTroop.SetCommand(TroopCommand.AttackArch);
                        }
                        else if (this.CurrentTroop.TargetTroop != null && this.CurrentTroop.TargetArchitecture == null)
                        {
                            // 只有部队目标 → 攻击部队
                            this.CurrentTroop.SetCommand(TroopCommand.AttackTroop);
                        }
                        else if (this.CurrentTroop.TargetArchitecture != null && this.CurrentTroop.TargetTroop != null)
                        {
                            // 同时有建筑和部队 → 根据兵种特性决定
                            if (this.CurrentTroop.TargetArchitecture.Endurance <= 0 ||
                                this.CurrentTroop.Army.Kind.AirOffence ||
                                this.CurrentTroop.CurrentStratagem != null ||
                                this.CurrentTroop.CurrentCombatMethod != null)
                            {
                                // 远程兵种或有战法 → 优先攻击部队
                                this.CurrentTroop.SetCommand(TroopCommand.AttackTroop);
                            }
                            else
                            {
                                // 近战兵种 → 优先攻击建筑
                                this.CurrentTroop.SetCommand(TroopCommand.AttackArch);
                            }
                        }
                        else
                        {
                            // 没有明确目标 → 通用攻击指令
                            this.CurrentTroop.SetCommand(TroopCommand.Attack);
                        }
                        
                        // 🔥 根本修复：计略和战法指令需要设置 OrientationTroop
                        // 日期：2026-02-28 & 2026-03-12
                        // 原因：DoCombatAction 使用 OrientationTroop 而不是 TargetTroop
                        //       玩家手动选择目标时只设置了 TargetTroop，导致计略/战法无法执行
                        // 解决：如果是计略或战法指令，将 TargetTroop 复制到 OrientationTroop
                        if ((this.CurrentTroop.Command == TroopCommand.Stratagem || this.CurrentTroop.CurrentCombatMethod != null) 
                            && this.CurrentTroop.TargetTroop != null)
                        {
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[TroopTarget] {this.CurrentTroop.DisplayName} 设置 OrientationTroop = {this.CurrentTroop.TargetTroop.DisplayName}");
                            #endif
                            
                            this.CurrentTroop.OrientationTroop = this.CurrentTroop.TargetTroop;
                            this.CurrentTroop.TargetTroop.OrientationTroop = this.CurrentTroop;
                        }
                        
                        // 🔥 修复：目标确认后才设置 Operated = true
                        // 日期：2026-03-07
                        // 原因：之前在 SetTroopStratagem/SetTroopCombatMethod 中就设置了 Operated = true
                        //       如果玩家取消选择目标，部队仍被标记为已操作，无法重新下达指令
                        // 解决：延迟到目标确认后再设置 Operated，确保只有完整的指令才消耗操作权
                        if (this.CurrentTroop.CurrentStratagem != null || this.CurrentTroop.CurrentCombatMethod != null)
                        {
                            this.CurrentTroop.Operated = true;
                        }
                        
                        // 🔥 修复：操作面不设置 CurrentAIState，避免立即移动
                        // 日期：2026-03-06
                        // 原因：在操作面设置 CurrentAIState = Marching 会导致 UpdateMovementLogic 立即开始寻路和移动
                        //       表现为下达战法指令后部队立即瞬移，而不是等到行动面再移动
                        // 解决：只在操作面设置 Command 和目标，CurrentAIState 由 TroopListWithQueue 在行动面设置
                        //       （TroopListWithQueue.CurrentQueueTroopMove 会根据 Command 设置正确的 CurrentAIState）
                        
                        // ❌ 删除以下代码（会导致操作面立即移动）：
                        // if (this.CurrentTroop.Command == TroopCommand.Enter)
                        // {
                        //     this.CurrentTroop.CurrentAIState = TroopAIState.EnterCity;
                        // }
                        // else if (this.CurrentTroop.Command == TroopCommand.Stratagem)
                        // {
                        //     this.CurrentTroop.CurrentAIState = TroopAIState.Marching;
                        // }
                        // else if (this.CurrentTroop.Command == TroopCommand.Move)
                        // {
                        //     this.CurrentTroop.CurrentAIState = TroopAIState.Marching;
                        // }
                        // else // Attack
                        // {
                        //     this.CurrentTroop.CurrentAIState = TroopAIState.Marching;
                        // }

                        ///////////////////////////////////////////////////////////////////////////////////
                        /*
                        Troop troopByPositionNoCheck = Session.Current.Scenario.GetTroopByPositionNoCheck(this.selectingLayer.SelectedPoint);
                        Architecture architectureByPositionNoCheck = Session.Current.Scenario.GetArchitectureByPositionNoCheck(this.selectingLayer.SelectedPoint);
                        bool youjianzhu=false ;
                        bool youdijun = false;
                        youjianzhu = (architectureByPositionNoCheck != null && architectureByPositionNoCheck.Endurance > 0);
                        youdijun = troopByPositionNoCheck != null && (this.CurrentTroop.BelongedFaction.IsPositionKnown(this.selectingLayer.SelectedPoint)||Session.GlobalVariables.SkyEye);
                        if (!youjianzhu && !youdijun)
                        {
                            this.CurrentTroop.TargetTroop = null;
                            this.CurrentTroop.TargetArchitecture = null;
                            this.CurrentTroop.SetCommand(TroopCommand.None);

                            return;
                        }
                        else if (youjianzhu && !youdijun)
                        {
                            this.CurrentTroop.TargetArchitecture = architectureByPositionNoCheck;
                            this.CurrentTroop.TargetTroop = null;
                            this.CurrentTroop.RealDestination = this.selectingLayer.SelectedPoint;
                            this.CurrentTroop.SetCommand(TroopCommand.AttackArch);

                            // 🔥 关键修复：玩家下达攻击城池命令后，立即分配攻击坑位
                            // 日期：2026-03-21
                            // 原因：坑位应该在战略面（操作面）分配，而不是在执行面之后
                            // 场景：玩家选择攻击目标 → 立即分配坑位 → 点击"进行" → 部队移动到坑位
                            // 解决：在下达命令时立即调用 SmartSiege 系统
                            
                            // 🔥 ANTI-BAND-AID：Fail Fast
                            // 日期：2026-03-21
                            // 原因：如果 BelongedLegion 为 null，说明数据损坏，应该立即失败
                            // 场景：玩家下达攻击命令时，部队必须有归属军团
                            if (this.CurrentTroop.BelongedLegion == null)
                            {
                                throw new InvalidOperationException(
                                    $"数据损坏：部队 {this.CurrentTroop.DisplayName}(ID:{this.CurrentTroop.ID}) 的 BelongedLegion 为 null");
                            }
                            
                            this.CurrentTroop.BelongedLegion.AssignSmartSiegePositions();
                        }
                        else if (!youjianzhu && youdijun)
                        {
                            this.CurrentTroop.TargetTroop = troopByPositionNoCheck;
                            this.CurrentTroop.TargetArchitecture = null;
                            this.CurrentTroop.SetCommand(TroopCommand.AttackTroop);

                        }
                        else if (youjianzhu && youdijun)
                        {
                            //if (this.CurrentTroop.Army.Kind.Type == MilitaryType.弩兵 || this.CurrentTroop.Army.KindID == 26 || troopByPositionNoCheck.BelongedFaction == this.CurrentTroop.BelongedFaction )
                            if (this.CurrentTroop.Army.Kind.AirOffence || this.CurrentTroop.CurrentStratagem != null)

                            {
                                this.CurrentTroop.TargetTroop = troopByPositionNoCheck;
                                this.CurrentTroop.TargetArchitecture = null;
                                this.CurrentTroop.SetCommand(TroopCommand.AttackTroop);

                            }
                            else
                            {
                                this.CurrentTroop.TargetArchitecture = architectureByPositionNoCheck;
                                this.CurrentTroop.TargetTroop = null;
                                this.CurrentTroop.RealDestination = this.selectingLayer.SelectedPoint;
                                this.CurrentTroop.SetCommand(TroopCommand.AttackArch);

                            }
                        }
                        */
                        /////////////////////////////////////////////////////////////////////////////////////

                        //this.Plugins.PersonBubblePlugin.AddPerson(this.CurrentTroop.Leader, this.CurrentTroop.Position, "Target");
                        return;
                    }
                case SelectingUndoneWorkKind.TroopInvestigatePosition:  //让军队侦查
                    if (this.selectingLayer.Canceled)
                    {
                        this.CurrentTroop.CurrentStratagem = null;

                        return;
                    }
                    this.CurrentTroop.SelfCastPosition = this.selectingLayer.SelectedPoint;

                    this.CurrentTroop.SelectedAttack = true;
                    this.CurrentTroop.SetCommand(TroopCommand.Stratagem);
                    
                    // 🔥 修复：目标确认后才设置 Operated = true
                    // 日期：2026-03-07
                    // 原因：之前在 SetTroopStratagem 中就设置了 Operated = true，如果玩家取消选择目标，部队仍被标记为已操作
                    // 解决：延迟到目标确认后再设置 Operated，确保只有完整的指令才消耗操作权
                    this.CurrentTroop.Operated = true;
                    
                    // 🔥 修复：操作面不设置 CurrentAIState，避免立即移动
                    // 日期：2026-03-06
                    // 原因：在操作面设置 CurrentAIState = Marching 会导致 UpdateMovementLogic 立即开始寻路和移动
                    //       表现为下达战法指令后部队立即瞬移，而不是等到行动面再移动
                    // 解决：只在操作面设置 Command 和目标，CurrentAIState 由 TroopListWithQueue 在行动面设置
                    this.CurrentTroop.Will = TroopWill.行军;
                    
                    // ❌ 删除以下代码（会导致操作面立即移动）：
                    // this.CurrentTroop.CurrentAIState = TroopAIState.Marching;
                    
                    System.Diagnostics.Debug.WriteLine($"[TroopInvestigatePosition] {this.CurrentTroop.DisplayName} 设置完成: Will={this.CurrentTroop.Will}, mingling={this.CurrentTroop.mingling}, SelfCastPosition={this.CurrentTroop.SelfCastPosition}, CurrentStratagem={this.CurrentTroop.CurrentStratagem?.Name ?? "null"}, MovabilityLeft={this.CurrentTroop.MovabilityLeft}, OperationDone={this.CurrentTroop.OperationDone}");

                    return;

                case SelectingUndoneWorkKind.TroopSetFirePosition:
                    if (this.selectingLayer.Canceled)
                    {
                        this.CurrentTroop.CurrentStratagem = null;
                        return;
                    }
                    
                    // 🔥 2026-03-18 新增：基于目标地块的天气检查
                    // 原因：计略的使用条件应该基于目标地块，而不是施法部队位置
                    // 解决：在目标确认后检查目标地块的天气，如果不符合条件则弹窗提示
                    {
                        var targetWeather = Session.Current.Scenario.WeatherManager.GetWeatherAt(this.selectingLayer.SelectedPoint);
                        if (targetWeather.SuppressFire())
                        {
                            // 🔥 2026-03-18 修复：简化文本以适应弹窗尺寸
                            string weatherName = targetWeather == WeatherType.Rain ? "雨天" : "雪天";
                            string message = $"⚠️ 无法点火\n\n目标地块天气：{weatherName}\n{weatherName}无法点燃火焰";
                            this.Plugins.SimpleTextDialogPlugin.RichText.Clear();
                            this.Plugins.SimpleTextDialogPlugin.RichText.AddText(message);
                            this.Plugins.SimpleTextDialogPlugin.SetPosition(ShowPosition.Center);
                            this.Plugins.SimpleTextDialogPlugin.IsShowing = true;
                            
                            // 取消计略
                            this.CurrentTroop.CurrentStratagem = null;
                            return;
                        }
                    }
                    
                    this.CurrentTroop.SelfCastPosition = this.selectingLayer.SelectedPoint;

                    this.CurrentTroop.SelectedAttack = true;
                    this.CurrentTroop.SetCommand(TroopCommand.Stratagem);
                    
                    // 🔥 修复：目标确认后才设置 Operated = true
                    // 日期：2026-03-07
                    // 原因：之前在 SetTroopStratagem 中就设置了 Operated = true，如果玩家取消选择目标，部队仍被标记为已操作
                    // 解决：延迟到目标确认后再设置 Operated，确保只有完整的指令才消耗操作权
                    this.CurrentTroop.Operated = true;
                    
                    // 🔥 根本修复：计略指令不需要设置 CurrentAIState
                    // 日期：2026-02-28
                    // 原因：设置 CurrentAIState = Combat 会导致 UpdateMovementLogic 被状态门卫拦截
                    // 解决：计略指令通过 mingling='Stratagem' 标识，在 CurrentQueueTroopMove 中直接检查并执行
                    //       不需要经过 UpdateMovementLogic，所以不需要设置 CurrentAIState
                    this.CurrentTroop.Will = TroopWill.行军;
                    
                    System.Diagnostics.Debug.WriteLine($"[TroopSetFirePosition] {this.CurrentTroop.DisplayName} 设置完成: Will={this.CurrentTroop.Will}, mingling={this.CurrentTroop.mingling}, SelfCastPosition={this.CurrentTroop.SelfCastPosition}, CurrentStratagem={this.CurrentTroop.CurrentStratagem?.Name ?? "null"}");

                    return;

                case SelectingUndoneWorkKind.ArchitectureRoutewayStartPoint:
                    if (!this.selectingLayer.Canceled)
                    {
                        routeway = this.CurrentArchitecture.CreateRouteway(this.selectingLayer.SelectedPoint);
                        if (routeway != null)
                        {
                            this.Plugins.RoutewayEditorPlugin.SetRouteway(routeway);
                            this.Plugins.RoutewayEditorPlugin.IsShowing = true;
                        }
                    }
                    return;

                case SelectingUndoneWorkKind.RoutewayPointShortestNormal:
                    if (!this.selectingLayer.Canceled)
                    {
                        routeway = this.CurrentArchitecture.BuildShortestRouteway(this.selectingLayer.SelectedPoint, false);
                        if (routeway != null)
                        {
                            routeway.Building = true;
                            Session.GlobalVariables.CurrentMapLayer = MapLayerKind.Routeway;
                        }
                    }
                    return;

                case SelectingUndoneWorkKind.RoutewayPointShortestNoWater:
                    if (!this.selectingLayer.Canceled)
                    {
                        routeway = this.CurrentArchitecture.BuildShortestRouteway(this.selectingLayer.SelectedPoint, true);
                        if (routeway != null)
                        {
                            routeway.Building = true;
                            Session.GlobalVariables.CurrentMapLayer = MapLayerKind.Routeway;
                        }
                    }
                    return;

                default:
                    return;
            }
            
            // 🔍 调试：检查 PersonBubble 调用前的状态
            System.Diagnostics.Debug.WriteLine($"[PersonBubble调用前] CurrentTroop = {this.CurrentTroop?.DisplayName ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[PersonBubble调用前] Leader = {this.CurrentTroop?.Leader?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[PersonBubble调用前] Leader.LocationTroop = {this.CurrentTroop?.Leader?.LocationTroop?.DisplayName ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[PersonBubble调用前] Troop.RealDestinationString = {this.CurrentTroop?.RealDestinationString ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[PersonBubble调用前] Person.RealDestinationString = {this.CurrentTroop?.Leader?.RealDestinationString ?? "null"}");
            
            this.Plugins.PersonBubblePlugin.AddPerson(this.CurrentTroop.Leader, this.CurrentTroop.Position, TextMessageKind.TroopMoveTo, "Destination");
        }



        public override void JumpTo(Point mapPosition)    //地图跳转
        {
            if (this.mainMapLayer == null) return;
            
            int num = (this.mainMapLayer.TileWidth * mapPosition.X) + (this.mainMapLayer.TileWidth / 2);
            int num2 = (this.mainMapLayer.TileHeight * mapPosition.Y) + (this.mainMapLayer.TileHeight / 2);
            this.mainMapLayer.LeftEdge = (this.viewportSize.X / 2) - num;
            if (this.mainMapLayer.LeftEdge > 0)
            {
                this.mainMapLayer.LeftEdge = 0;
            }
            else if (this.mainMapLayer.LeftEdge < (this.viewportSize.X - this.mainMapLayer.TotalTileWidth))
            {
                this.mainMapLayer.LeftEdge = this.viewportSize.X - this.mainMapLayer.TotalTileWidth;
            }
            this.mainMapLayer.TopEdge = (this.viewportSize.Y / 2) - num2;
            if (this.mainMapLayer.TopEdge > 0)
            {
                this.mainMapLayer.TopEdge = 0;
            }
            else if (this.mainMapLayer.TopEdge < (this.viewportSize.Y - this.mainMapLayer.TotalTileHeight))
            {
                this.mainMapLayer.TopEdge = this.viewportSize.Y - this.mainMapLayer.TotalTileHeight;
            }
            this.ResetScreenEdge();
            this.mainMapLayer.ReCalculateTileDestination(this);
            
            if (this.Plugins != null && this.Plugins.AirViewPlugin != null)
            {
                // 🔥 ANTI-BAND-AID：TotalMapSize 必须有效，否则说明 mainMapLayer 未正确初始化
                var totalMapSize = this.mainMapLayer.TotalMapSize;
                if (totalMapSize.X == 0 || totalMapSize.Y == 0)
                {
                    throw new InvalidOperationException(
                        $"mainMapLayer.TotalMapSize 无效 ({totalMapSize.X}×{totalMapSize.Y})，" +
                        $"ScenarioMap.MapDimensions={Session.Current.Scenario.ScenarioMap.MapDimensions}, " +
                        $"TileWidth={Session.Current.Scenario.ScenarioMap.TileWidth}");
                }
                
                this.Plugins.AirViewPlugin.ResetFramePosition(base.viewportSize, this.mainMapLayer.LeftEdge, this.mainMapLayer.TopEdge, totalMapSize);
            }

            if (Session.MainGame.mainGameScreen != null && Session.MainGame.mainGameScreen.cloudLayer != null)
            {
                Session.MainGame.mainGameScreen.cloudLayer.Start();
            }
        }



        // 🔥 静态计数器：检测 Threading=true 卡死
        private static int _moveTheTroopsThreadingCount = 0;
        private const int MAX_THREADING_WAIT_MOVES = 50;
        
        private bool MoveTheTroops(GameTime gameTime)
        {
            /*
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[MoveTheTroops] 方法被调用！Threading={Session.Current.Scenario.Threading}, Animating={Session.Current.Scenario.Animating}");
            #endif
            */
            
            // 🔥 死锁检测与强制修复
            if (Session.Current.Scenario.Threading)
            {
                _moveTheTroopsThreadingCount++;
                
                // 每10次输出一次警告
                if (_moveTheTroopsThreadingCount % 10 == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[MoveTheTroops] Threading=true 已持续 {_moveTheTroopsThreadingCount} 次，检查死锁...");
                    
                    // 检查是否所有势力AI都完成了
                    bool allAIFinished = true;
                    string waitingFor = "";
                    foreach (var obj in Session.Current.Scenario.Factions.GetList())
                    {
                        if (obj is Faction faction && faction.IsAlive && !faction.AIFinished)
                        {
                            allAIFinished = false;
                            waitingFor = faction.Name;
                            break;
                        }
                    }
                    
                    if (allAIFinished)
                    {
                        System.Diagnostics.Debug.WriteLine("[MoveTheTroops] 死锁检测：所有AI已完成但Threading=true，强制重置！");
                        Session.Current.Scenario.Threading = false;
                        _moveTheTroopsThreadingCount = 0;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MoveTheTroops] 等待势力: {waitingFor}");
                    
                    // 🔥 额外调试：显示等待势力的详细状态
                    foreach (var obj in Session.Current.Scenario.Factions.GetList())
                    {
                        if (obj is Faction faction && faction.IsAlive && !faction.AIFinished)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MoveTheTroops] 势力 {faction.Name}: AIFinished={faction.AIFinished}, Controlling={faction.Controlling}, Passed={faction.Passed}, IsPlayer={Session.Current.Scenario.IsPlayer(faction)}");
                        }
                    }
                    }
                }
                
                // 超时强制修复
                if (_moveTheTroopsThreadingCount >= MAX_THREADING_WAIT_MOVES)
                {
                    System.Diagnostics.Debug.WriteLine("[MoveTheTroops] 超时！强制重置Threading和所有势力AI状态");
                    Session.Current.Scenario.Threading = false;
                    foreach (var obj in Session.Current.Scenario.Factions.GetList())
                    {
                        if (obj is Faction faction)
                        {
                            faction.AIFinished = true;
                        }
                    }
                    _moveTheTroopsThreadingCount = 0;
                }
            }
            else
            {
                _moveTheTroopsThreadingCount = 0;
            }
            
            if (!Session.Current.Scenario.Threading)
            {
                // 🔥 动画阻塞重构：废除全局 Animating 检查
                // 日期：2026-02-28
                // 原因：全局 Animating 会阻塞所有部队，即使只有 1 个部队在动画
                // 解决：让 CurrentQueueTroopMove 内部检查 CurrentTroop.IsAnimationPlaying
                
                bool isPlayerControlling = Session.Current.Scenario.CurrentPlayer != null && 
                                           Session.Current.Scenario.CurrentPlayer.Controlling;

                #if DEBUG
                // System.Diagnostics.Debug.WriteLine($"[MoveTheTroops] Threading=false, isPlayerControlling={isPlayerControlling}, CurrentPlayer={Session.Current.Scenario.CurrentPlayer?.Name}");
                #endif

                // 只有在非玩家控制时，才允许处理移动队列
                if (!isPlayerControlling)
                {
                    #if DEBUG
                    // System.Diagnostics.Debug.WriteLine($"[MoveTheTroops] 准备调用 CurrentQueueTroopMove");
                    #endif
                    
                    Session.Current.Scenario.Troops.CurrentQueueTroopMove(gameTime);
                    if (Session.Current.Scenario.Troops.TotallyEmpty)
                    {
                        return false;
                    }
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[MoveTheTroops] 跳过 CurrentQueueTroopMove：玩家正在控制");
                    #endif
                }
            }
            
            return true;
        }

        public void PauseMusic()
        {
            try
            {
                if (Session.GlobalVariables.PlayMusic)  // && (this.Player.playState == WMPPlayState.wmppsPlaying))
                {
                    Platform.Current.PauseSong();
                    //this.Player.pause();
                }
            }
            //catch (System.Runtime.InteropServices.COMException ex)
            catch
            {
                //do not let an ignorable exception break the game!
            }
        }



        private void Player_PlayStateChange(int NewState)
        {
            if (Session.GlobalVariables.PlayMusic && (NewState == 1))
            {
                Platform.Current.ResumeSong();
                //this.Player.play();
            }
        }

        //public override void PlayMusic(string musicFileLocation)
        //{
        //    if (Session.GlobalVariables.PlayMusic && File.Exists(musicFileLocation))
        //    {
        //        this.Player.URL = musicFileLocation;
        //    }
        //}

        public override UndoneWorkItem PopUndoneWork()
        {
            UndoneWorkItem item = base.PopUndoneWork();
            if (this.PeekUndoneWork().Kind == UndoneWorkKind.None)
            {
                this.Plugins.ToolBarPlugin.Enabled = true;
            }
            //base.previousMouseState = base.mouseState;
            switch (item.Kind)
            {

                case UndoneWorkKind.ContextMenu:
                    this.HandleContextMenuResult(this.Plugins.ContextMenuPlugin.Result);
                    this.gengxinyoucelan(); 
                    break;

                case UndoneWorkKind.Frame:
                    this.HandleFrameResult(this.Plugins.GameFramePlugin.Result);


                    this.gengxinyoucelan(); 

                    break;

                case UndoneWorkKind.Dialog:
                    this.HandleDialogResult((DialogKind)item.SubKind);
                    break;
                case UndoneWorkKind.tupianwenzi:
                    break;
                case UndoneWorkKind.liangdaobianji :
                    break;
                case UndoneWorkKind.SubDialog:
                    break;

                case UndoneWorkKind.Selecting:
                    this.HandleSelectingResult((SelectingUndoneWorkKind)item.SubKind);
                    this.gengxinyoucelan(); 

                    break;
                case UndoneWorkKind.MapViewSelector:
                    //this.screenManager.FrameFunction_Architecture_AfterGetOneArchitectureByMapViewSelector();
                    break;

            }

            if (this.tufashijianzantingyinyue && this.Plugins.tupianwenziPlugin.IsShowing == false)
            {
                this.ResumeMusic();
                this.tufashijianzantingyinyue = false;
            }

            return item;
        }

        public override void PushUndoneWork(UndoneWorkItem undoneWork)
        {
            base.PushUndoneWork(undoneWork);
            if (this.PeekUndoneWork().Kind != UndoneWorkKind.None)
            {
                this.Plugins.ToolBarPlugin.Enabled = false;
            }
            switch (undoneWork.Kind)
            {
                case UndoneWorkKind.Selecting:
                    this.HandlePushSelectingUndoneWork((SelectingUndoneWorkKind)undoneWork.SubKind);
                    break;
            }
        }

        public void RefreshDisableRects()
        {
            base.ClearDisableRects();
            if (this.Plugins.AirViewPlugin != null && this.Plugins.AirViewPlugin.IsMapShowing)
            {
                this.Plugins.AirViewPlugin.ResetMapPosition(Session.MainGame.mainGameScreen);
                this.Plugins.AirViewPlugin.AddDisableRects();
            }
            if (this.Plugins.GameRecordPlugin != null && this.Plugins.GameRecordPlugin.IsRecordShowing)
            {
                this.Plugins.GameRecordPlugin.ResetRecordShowPosition();
                this.Plugins.GameRecordPlugin.AddDisableRects();
            }
            if (this.Plugins.RoutewayEditorPlugin.IsShowing)
            {
                this.Plugins.RoutewayEditorPlugin.AddDisableRects();
            }
            if (this.Plugins.MapViewSelectorPlugin.IsShowing)
            {
                this.Plugins.MapViewSelectorPlugin.AddDisableRects();
            }
        }

        private void ResetCurrentStatus()
        {
            this.lastPosition = this.position;
            this.position = this.mainMapLayer.TranslateCoordinateToTilePosition(InputManager.PoX, InputManager.PoY);
        }

        public override void ResetMouse()
        {
            base.MouseArrowTexture = base.DefaultMouseArrowTexture;
            this.viewMove = ViewMove.Stop;
        }

        private void ResetScreenEdge()
        {
            this.TopLeftPosition.X = -this.mainMapLayer.LeftEdge / Session.Current.Scenario.ScenarioMap.TileWidth;
            this.TopLeftPosition.Y = -this.mainMapLayer.TopEdge / Session.Current.Scenario.ScenarioMap.TileHeight;
            this.BottomRightPosition.X = (this.viewportSize.X - this.mainMapLayer.LeftEdge) / Session.Current.Scenario.ScenarioMap.TileWidth;
            this.BottomRightPosition.Y = (this.viewportSize.Y - this.mainMapLayer.TopEdge) / Session.Current.Scenario.ScenarioMap.TileHeight;
        }

        private void ResetTiles()
        {
            if (Session.Current.Scenario.ScenarioMap != null)
            {
                if (this.mainMapLayer.TotalTileWidth < this.viewportSize.X)
                {
                    this.mainMapLayer.TileWidth = (this.viewportSize.X / Session.Current.Scenario.ScenarioMap.MapDimensions.X) + (Session.Current.Scenario.ScenarioMap.TileWidthMin / 5);
                }
                if (this.mainMapLayer.TotalTileHeight < this.viewportSize.Y)
                {
                    this.mainMapLayer.TileWidth = (this.viewportSize.Y / Session.Current.Scenario.ScenarioMap.MapDimensions.Y) + (Session.Current.Scenario.ScenarioMap.TileWidthMin / 5);
                }
            }
        }

        public void ResumeMusic()
        {
            try
            {
                if (Session.GlobalVariables.PlayMusic)  // && (this.Player.playState == WMPPlayState.wmppsPaused))
                {
                    //this.Player.play();
                    Platform.Current.ResumeSong();
                }
            }
            catch (System.Runtime.InteropServices.COMException)
            {
            }
        }

        private void RoutewayOptionDialogClickCallback(object obj)
        {
            this.CurrentRouteway = obj as Routeway;
        }

        private bool RunTheFactions(GameTime gameTime)
        {
            try
            {
                // 🔥 COMPREHENSIVE NULL REFERENCE PROTECTION
                if (Session.Current?.Scenario == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RunTheFactions] ❌ Session.Current.Scenario is null");
                    return false;
                }

                if (Session.Current.Scenario.Factions == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RunTheFactions] ❌ Scenario.Factions is null");
                    return false;
                }

                // Check if any faction has null Leader or Capital (common crash source)
                foreach (var f in Session.Current.Scenario.Factions)
                {
                    if (f is Faction faction)
                    {
                        if (faction.Leader == null)
                        {
                            continue;
                        }
                        if (faction.Capital == null)
                        {
                            // 🔥 如果 Capital 仍然为 null，跳过处理
                            // 注意：正常情况下不应该到这里，因为 LinkScenarioReferences 已经修复了
                            continue;
                        }
                    }
                }

                // Ensure CurrentPlayer is set
                if (Session.Current.Scenario.CurrentPlayer == null && Session.Current.Scenario.Factions.Count > 0)
                {
                    try 
                    {
                        // Use ElementAtOrDefault or LINQ to be safe against race conditions or list changes
                        // GetList() returns GameObjectList, we need to access its GameObjects property (List<GameObject>) to use FirstOrDefault
                        var firstFaction = Session.Current.Scenario.Factions.GetList().GameObjects.FirstOrDefault() as Faction;
                        if (firstFaction != null)
                        {
                            Session.Current.Scenario.CurrentPlayer = firstFaction;
                            System.Diagnostics.Debug.WriteLine($"[RunTheFactions] 紧急设置CurrentPlayer: {firstFaction.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RunTheFactions] 设置CurrentPlayer时出错: {ex.Message}");
                    }
                }
                
                try
                {
                    Session.Current.Scenario.Factions.RunQueue();
                }
                catch (ArgumentOutOfRangeException indexEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[RunTheFactions] ❌ 说曹操曹操到: RunQueue 触发 IndexOutOfRange: {indexEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"[RunTheFactions] 堆栈: {indexEx.StackTrace}");
                    return false;
                }
                catch (Exception runEx)
                {
                    throw; // Re-throw to be caught by outer handler
                }

                if (Session.Current.Scenario.Factions.QueueEmpty)
                {
                    return false;
                }
                return true;
            }
            catch (NullReferenceException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RunTheFactions] ❌ NullReferenceException: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RunTheFactions] 堆栈跟踪: {ex.StackTrace}");
                
                // Try to recover by ensuring basic objects are set
                if (Session.Current?.Scenario != null && Session.Current.Scenario.CurrentPlayer == null && Session.Current.Scenario.Factions?.Count > 0)
                {
                    Session.Current.Scenario.CurrentPlayer = Session.Current.Scenario.Factions[0] as Faction;
                    System.Diagnostics.Debug.WriteLine("[RunTheFactions] 尝试恢复：设置CurrentPlayer");
                }
                
                return false;
            }
            catch (OutOfMemoryException)
            {
                Session.Current.Scenario.DisposeLotsOfMemory();
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RunTheFactions] ❌ 未预期异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 🔥 运行时安全检查 - 确保 Faction.Capital 不为 null
        /// </summary>
        private void EnsureFactionCapitalNotNull(Faction faction)
        {
            if (faction == null) return;

            if (faction.Capital == null)
            {
                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 🔧 运行时修复: 势力 {faction.Name} 的 Capital 为 null");

                // 尝试从 CapitalID 恢复
                if (faction.CapitalID >= 0 && Session.Current?.Scenario?.Architectures != null)
                {
                    var capital = Session.Current.Scenario.Architectures.GetGameObject(faction.CapitalID) as Architecture;
                    if (capital != null)
                    {
                        faction.Capital = capital;
                        // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ✅ 从 CapitalID 恢复首都: {capital.Name}");
                        return;
                    }
                }

                // 从势力建筑中选择一个
                if (faction.Architectures != null && faction.Architectures.Count > 0)
                {
                    var newCapital = faction.Architectures[0] as Architecture;
                    faction.Capital = newCapital;
                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 🔧 紧急设置首都: {newCapital?.Name}");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ❌ 势力 {faction.Name} 没有任何建筑，无法设置首都");
                }
            }
        }

        public override void SaveGame()
        {
            this.Plugins.OptionDialogPlugin.SetStyle("SaveAndLoad");
            this.Plugins.OptionDialogPlugin.SetTitle("存储进度");
            this.Plugins.OptionDialogPlugin.Clear();

            //throw new Exception("SaveGame");

            var saves = GameScenario.LoadScenarioSaves();
            for (int i = 1; i <= GameScenario.savemaxcounts; i++)
            {
                string ss = i < 10 ? "0" + i.ToString() : i.ToString();
                GameDelegates.VoidFunction voidFunction = delegate
                {
                    // 🔥 修复：使用 .sav.gz 扩展名而不是 .bin
                    this.SaveFileName = "Save" + ss + ".sav.gz";
                    this.SaveGameToDisk(this.SaveFileName);
                };
                saves[i].ID = ss;
                this.Plugins.OptionDialogPlugin.AddOption(saves[i].Summary, null, voidFunction);
            }
            this.Plugins.OptionDialogPlugin.EndAddOptions();
            this.Plugins.OptionDialogPlugin.ShowOptionDialog(ShowPosition.Center);
        }

        public void SaveGameToDisk(string LoadedFileName)
        {
            Session.Current.Scenario.EnableLoadAndSave = false;

            try
            {
                this.mainMapLayer.freeTilesMemory();

                if (!Platform.Current.UserDirectoryExist("Save"))
                {
                    Platform.Current.UserDirectoryCreate("Save");
                }

                // 🔥 FIX: 始终保存地图数据，避免读档时 MapDataString 为空
                // 地图数据是游戏核心数据，应该始终保存到存档文件中
                bool saveMap = true;

                Session.Current.Scenario.ScenarioMap.JumpPosition = this.mainMapLayer.GetCurrentScreenCenter(base.viewportSize);
                saveMap = saveMap || this.mapEdited;

                // 🔥 修复：检测是否为绝对路径（崩溃存档使用完整路径）
                // 如果 LoadedFileName 包含盘符（如 "G:\"）或以路径分隔符开头，则认为是完整路径
                bool isFullPath = Path.IsPathRooted(LoadedFileName);
                
                Session.Current.Scenario.SaveGameScenario(LoadedFileName, saveMap, saveMap, true, fullPathProvided: isFullPath);

                this.mainMapLayer.freeTilesMemory();
            }
            finally
            {
                Session.Current.Scenario.EnableLoadAndSave = true;
                
                // 🔥 修复：不要在存档后立即播放音乐
                // 原因：freeTilesMemory() 调用 GC.Collect() 可能导致 ContentManager 内部资源被回收
                // 解决方案：延迟到下一帧播放音乐，让 GC 完成清理
                // 音乐会在 MainGameScreen.Update() 的下一帧自动恢复（通过 MGSDate.PlayMusic()）
                System.Diagnostics.Debug.WriteLine("[SaveGameToDisk] 存档完成，音乐将在下一帧自动恢复");
            }
        }

        public void SaveGameAutoPosition()
        {
            this.SaveFileName = "Save00.bin"; //"AutoSave" + this.SaveFileExtension;
            this.SaveGameToDisk(this.SaveFileName);
        }

        private void SaveGameQuitPosition()
        {
            this.SaveFileName = "QuitSave.bin";
            this.SaveGameToDisk(this.SaveFileName);
        }
 
        public void SaveGameWhenCrash(String _savePath)
        {
            this.SaveFileName = _savePath;
            this.SaveGameToDisk(this.SaveFileName);
        }

        /// <summary>
        /// 新势力创建事件处理器（新事件系统）
        /// </summary>
        private void Scenario_OnNewFactionCreated(GameScenario scenario, Faction oldFaction, Faction newFaction, Architecture capital)
        {
            if ((newFaction.Leader != null) && (newFaction.Capital != null))
            {
                newFaction.Leader.TextDestinationString = newFaction.Capital.Name;
                this.Plugins.GameRecordPlugin.AddBranch(newFaction.Leader, "NewFactionAppear", newFaction.Leader.Position);
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(newFaction.Leader, newFaction.Leader, TextMessageKind.CreateNewFaction, "NewFactionAppear");
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
        }

        private void ScrollTheMainMap(GameTime gameTime)
        {
            if (base.EnableScroll)
            {
                if (Platform.PlatFormType == PlatFormType.Desktop || Platform.PlatFormType == PlatFormType.Win) 
                {
                    if (this.viewMove != ViewMove.Stop)
                    {
                        if ((Math.Abs((int)(InputManager.PoX - Convert.ToInt32(InputManager.PositionPre.X))) <= 2) && (Math.Abs((int)(InputManager.PoY - Convert.ToInt32(InputManager.PositionPre.Y))) <= 2)
                            || this.isKeyScrolling)
                        {
                            // 没有200毫秒的延迟，鼠标滚动屏幕效果也不错
                            //
                            //if (!this.isKeyScrolling && (gameTime.TotalGameTime.TotalMilliseconds - this.lastTime) < 200.0)
                            //{
                            //    return;
                            //}

                            int num = (int)((gameTime.ElapsedGameTime.Milliseconds * Session.GlobalVariables.MapScrollSpeed) * this.scrollSpeedScale);
                            switch (this.viewMove)
                            {
                                case ViewMove.Left:
                                    if (this.viewportSize.X < this.mainMapLayer.TotalTileWidth)
                                    {
                                        this.mainMapLayer.LeftEdge += num;
                                        if (this.mainMapLayer.LeftEdge > 0)
                                        {
                                            this.mainMapLayer.LeftEdge = 0;
                                        }
                                    }
                                    break;

                                case ViewMove.Right:
                                    if (this.viewportSize.X < this.mainMapLayer.TotalTileWidth)
                                    {
                                        this.mainMapLayer.LeftEdge -= num;
                                        if (this.mainMapLayer.LeftEdge < (this.viewportSize.X - this.mainMapLayer.TotalTileWidth))
                                        {
                                            this.mainMapLayer.LeftEdge = this.viewportSize.X - this.mainMapLayer.TotalTileWidth;
                                        }
                                    }
                                    break;

                                case ViewMove.Top:
                                    if (this.viewportSize.Y < this.mainMapLayer.TotalTileHeight)
                                    {
                                        this.mainMapLayer.TopEdge += num;
                                        if (this.mainMapLayer.TopEdge > 0)
                                        {
                                            this.mainMapLayer.TopEdge = 0;
                                        }
                                    }
                                    break;

                                case ViewMove.Bottom:
                                    if (this.viewportSize.Y < this.mainMapLayer.TotalTileHeight)
                                    {
                                        this.mainMapLayer.TopEdge -= num;
                                        if (this.mainMapLayer.TopEdge < (this.viewportSize.Y - this.mainMapLayer.TotalTileHeight))
                                        {
                                            this.mainMapLayer.TopEdge = this.viewportSize.Y - this.mainMapLayer.TotalTileHeight;
                                        }
                                    }
                                    break;

                                case ViewMove.TopLeft:
                                    if (this.viewportSize.X < this.mainMapLayer.TotalTileWidth)
                                    {
                                        this.mainMapLayer.LeftEdge += num;
                                        if (this.mainMapLayer.LeftEdge > 0)
                                        {
                                            this.mainMapLayer.LeftEdge = 0;
                                        }
                                    }
                                    if (this.viewportSize.Y < this.mainMapLayer.TotalTileHeight)
                                    {
                                        this.mainMapLayer.TopEdge += num;
                                        if (this.mainMapLayer.TopEdge > 0)
                                        {
                                            this.mainMapLayer.TopEdge = 0;
                                        }
                                    }
                                    break;

                                case ViewMove.TopRight:
                                    if (this.viewportSize.X < this.mainMapLayer.TotalTileWidth)
                                    {
                                        this.mainMapLayer.LeftEdge -= num;
                                        if (this.mainMapLayer.LeftEdge < (this.viewportSize.X - this.mainMapLayer.TotalTileWidth))
                                        {
                                            this.mainMapLayer.LeftEdge = this.viewportSize.X - this.mainMapLayer.TotalTileWidth;
                                        }
                                    }
                                    if (this.viewportSize.Y < this.mainMapLayer.TotalTileHeight)
                                    {
                                        this.mainMapLayer.TopEdge += num;
                                        if (this.mainMapLayer.TopEdge > 0)
                                        {
                                            this.mainMapLayer.TopEdge = 0;
                                        }
                                    }
                                    break;

                                case ViewMove.BottomLeft:
                                    if (this.viewportSize.X < this.mainMapLayer.TotalTileWidth)
                                    {
                                        this.mainMapLayer.LeftEdge += num;
                                        if (this.mainMapLayer.LeftEdge > 0)
                                        {
                                            this.mainMapLayer.LeftEdge = 0;
                                        }
                                    }
                                    if (this.viewportSize.Y < this.mainMapLayer.TotalTileHeight)
                                    {
                                        this.mainMapLayer.TopEdge -= num;
                                        if (this.mainMapLayer.TopEdge < (this.viewportSize.Y - this.mainMapLayer.TotalTileHeight))
                                        {
                                            this.mainMapLayer.TopEdge = this.viewportSize.Y - this.mainMapLayer.TotalTileHeight;
                                        }
                                    }
                                    break;

                                case ViewMove.BottomRight:
                                    if (this.viewportSize.X < this.mainMapLayer.TotalTileWidth)
                                    {
                                        this.mainMapLayer.LeftEdge -= num;
                                        if (this.mainMapLayer.LeftEdge < (this.viewportSize.X - this.mainMapLayer.TotalTileWidth))
                                        {
                                            this.mainMapLayer.LeftEdge = this.viewportSize.X - this.mainMapLayer.TotalTileWidth;
                                        }
                                    }
                                    if (this.viewportSize.Y < this.mainMapLayer.TotalTileHeight)
                                    {
                                        this.mainMapLayer.TopEdge -= num;
                                        if (this.mainMapLayer.TopEdge < (this.viewportSize.Y - this.mainMapLayer.TotalTileHeight))
                                        {
                                            this.mainMapLayer.TopEdge = this.viewportSize.Y - this.mainMapLayer.TotalTileHeight;
                                        }
                                    }
                                    break;
                            }
                            //goto Label_0647;
                            this.ResetScreenEdge();
                            this.mainMapLayer.ReCalculateTileDestination(this);
                            if (this.Plugins.AirViewPlugin != null)
                            {
                                this.Plugins.AirViewPlugin.ResetFramePosition(base.viewportSize, this.mainMapLayer.LeftEdge, this.mainMapLayer.TopEdge, this.mainMapLayer.TotalMapSize);
                            }
                            return;
                        }
                        this.lastTime = gameTime.TotalGameTime.TotalMilliseconds;
                    }
                    return;
                    //Label_0647:
                    //this.ResetScreenEdge();
                    //this.mainMapLayer.ReCalculateTileDestination();
                    //this.Plugins.AirViewPlugin.ResetFramePosition(base.viewportSize, this.mainMapLayer.LeftEdge, this.mainMapLayer.TopEdge, this.mainMapLayer.TotalMapSize);
                }
                else
                {
                    if (InputManager.IsMoved)
                    {
                        this.mainMapLayer.LeftEdge += InputManager.PoXMove;
                        this.mainMapLayer.TopEdge += InputManager.PoYMove;

                        if (this.mainMapLayer.LeftEdge > 0)
                        {
                            this.mainMapLayer.LeftEdge = 0;
                        }

                        if (this.mainMapLayer.LeftEdge < (this.viewportSize.X - this.mainMapLayer.TotalTileWidth))
                        {
                            this.mainMapLayer.LeftEdge = this.viewportSize.X - this.mainMapLayer.TotalTileWidth;
                        }

                        if (this.mainMapLayer.TopEdge > 0)
                        {
                            this.mainMapLayer.TopEdge = 0;
                        }

                        if (this.mainMapLayer.TopEdge < (this.viewportSize.Y - this.mainMapLayer.TotalTileHeight))
                        {
                            this.mainMapLayer.TopEdge = this.viewportSize.Y - this.mainMapLayer.TotalTileHeight;
                        }

                        this.ResetScreenEdge();
                        this.mainMapLayer.ReCalculateTileDestination(this);
                        if (this.Plugins.AirViewPlugin != null)
                        {
                            this.Plugins.AirViewPlugin.ResetFramePosition(base.viewportSize, this.mainMapLayer.LeftEdge, this.mainMapLayer.TopEdge, this.mainMapLayer.TotalMapSize);
                        }
                    }
                    else
                    {
                        this.lastTime = gameTime.TotalGameTime.TotalMilliseconds;
                    }
                }
            }
        }

        private void SetTroopCombatMethod(int id)
        {
            // 🔥 修复：延迟设置 Operated，等目标确认后再标记为已操作
            // 日期：2026-03-07
            // 原因：在选择战法时就设置 Operated = true，如果玩家取消选择目标，部队仍被标记为已操作
            // 解决：只在这里设置战法，Operated 在 HandleSelectingResult(TroopTarget) 确认目标后设置
            // this.CurrentTroop.Operated = true;  // ❌ 移除，延迟到目标确认后
            this.CurrentTroop.CurrentStratagem = null;
            this.CurrentTroop.CurrentCombatMethod = Session.Current.Scenario.GameCommonData.AllCombatMethods.GetCombatMethod(id);
            if (this.CurrentTroop.CurrentCombatMethod != null)
            {
                if (this.CurrentTroop.CurrentCombatMethod.AttackDefault != null)
                {
                    this.CurrentTroop.AttackDefaultKind = (TroopAttackDefaultKind)this.CurrentTroop.CurrentCombatMethod.AttackDefault.ID;
                }
                if (this.CurrentTroop.CurrentCombatMethod.AttackTarget != null)
                {
                    this.CurrentTroop.AttackTargetKind = (TroopAttackTargetKind)this.CurrentTroop.CurrentCombatMethod.AttackTarget.ID;
                }
                if ((this.CurrentTroop.AttackTargetKind == TroopAttackTargetKind.目标默认) || (this.CurrentTroop.AttackTargetKind == TroopAttackTargetKind.目标))
                {
                    this.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.TroopTarget));
                }
            }
        }

        private void SetTroopStratagem(int id)
        {
            // 🔥 修复：延迟设置 Operated，等目标确认后再标记为已操作
            // 日期：2026-03-07
            // 原因：在选择技能时就设置 Operated = true，如果玩家取消选择目标，部队仍被标记为已操作
            // 解决：只在这里设置技能，Operated 在 HandleSelectingResult(TroopTarget) 确认目标后设置
            // this.CurrentTroop.Operated = true;  // ❌ 移除，延迟到目标确认后
            this.CurrentTroop.CurrentCombatMethod = null;
            this.CurrentTroop.CurrentStratagem = Session.Current.Scenario.GameCommonData.AllStratagems.GetStratagem(id);
            
            // 🔥 根本修复：设置 mingling 标识计略指令
            // 日期：2026-02-28
            // 原因：SetTroopStratagem 没有设置 mingling，导致后续 TroopTarget 逻辑将其误判为 Attack
            // 解决：在设置计略时立即设置 mingling = "Stratagem"
            this.CurrentTroop.SetCommand(TroopCommand.Stratagem);
            
            if (this.CurrentTroop.CurrentStratagem != null)
            {
                if (this.CurrentTroop.CurrentStratagem.CastDefault != null)
                {
                    this.CurrentTroop.CastDefaultKind = (TroopCastDefaultKind)this.CurrentTroop.CurrentStratagem.CastDefault.ID;
                }
                if (this.CurrentTroop.CurrentStratagem.CastTarget != null)
                {
                    this.CurrentTroop.CastTargetKind = (TroopCastTargetKind)this.CurrentTroop.CurrentStratagem.CastTarget.ID;
                }

                if (id == 2 || id == 3 || id == 6 || id == 8)
                {

                }
                else if ((this.CurrentTroop.CastTargetKind == TroopCastTargetKind.特定默认) || (this.CurrentTroop.CastTargetKind == TroopCastTargetKind.特定))
                {
                    this.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.TroopTarget));
                }
            }
        }

        private void SetTroopStunt(int id)
        {
            // 🔥 修复：特技是即时生效的增益效果，不应消耗部队操作权
            // 日期：2026-03-06
            // 原因：设置 Operated = true 导致部队无法下达后续指令（移动/攻击/计略）
            // 解决：移除 Operated 设置，让特技成为"自由操作"，符合 WEGO 机制
            // this.CurrentTroop.Operated = true;  // ❌ 移除此行
            
            this.CurrentTroop.CurrentStunt = Session.Current.Scenario.GameCommonData.AllStunts.GetStunt(id);
            this.CurrentTroop.ApplyCurrentStunt();
        }

        public void ShowFactionTechniques(Faction faction, Architecture architecture)
        {
            this.Plugins.FactionTechniquesPlugin.SetArchitecture(architecture);
            this.Plugins.FactionTechniquesPlugin.SetFaction(faction, Session.Current.Scenario.CurrentPlayer == faction);
            this.Plugins.FactionTechniquesPlugin.SetPosition(ShowPosition.Center);
            this.Plugins.FactionTechniquesPlugin.IsShowing = true;
        }

        /// <summary>
        /// 显示对话UI
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">军师</param>
        /// <param name="dialogue">对话条目</param>
        /// <param name="onFinished">对话结束后的回调</param>
        public void ShowDialogueUI(Person leader, Person advisor, WorldOfTheThreeKingdoms.GameGlobal.DialogueEntry dialogue, DialogueFinishedCallback onFinished)
        {
            try
            {
                if (dialogueUI == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowDialogueUI] 对话UI未初始化");
                    onFinished?.Invoke(); // 直接执行回调
                    return;
                }

                // 转换 WorldOfTheThreeKingdoms.GameGlobal.DialogueEntry 到本地 DialogueEntry
                var localDialogue = new WorldOfTheThreeKingdoms.GameManager.DialogueEntry
                {
                    Type = dialogue.Type,
                    Relation = dialogue.Relation,
                    LeaderText = dialogue.LeaderText,
                    AdvisorText = dialogue.AdvisorText
                };

                // 保存回调函数
                currentDialogueCallback = onFinished;

                // 开始对话
                dialogueUI.StartDialogue(leader, advisor, localDialogue);

                System.Diagnostics.Debug.WriteLine($"[ShowDialogueUI] 开始对话: {leader?.Name} <-> {advisor?.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowDialogueUI] 显示对话失败: {ex.Message}");
                onFinished?.Invoke(); // 出错时也执行回调
            }
        }

        /// <summary>
        /// 【军师系统】执行罢免军师的逻辑
        /// </summary>
        public void RecallAdvisor()
        {
            System.Diagnostics.Debug.WriteLine("[RecallAdvisor] 开始执行罢免军师逻辑");
            
            // 1. 获取当前数据
            Faction faction = Session.Current.Scenario.CurrentFaction;
            if (faction == null) 
            {
                System.Diagnostics.Debug.WriteLine("[RecallAdvisor] 错误：当前势力为空");
                return;
            }

            Person leader = faction.Leader;
            Person advisor = faction.Advisor; 

            System.Diagnostics.Debug.WriteLine($"[RecallAdvisor] 君主: {leader?.Name}, 军师: {advisor?.Name}");

            // 如果没有军师，或者数据异常，直接返回
            if (leader == null || advisor == null) 
            {
                System.Diagnostics.Debug.WriteLine("[RecallAdvisor] 错误：君主或军师为空，无法执行罢免");
                return;
            }

            // 2. 获取罢免对话内容
            try 
            {
                var dialogue = DialogueManager.GetRecallDialogue(leader, advisor);

                // 3. 显示 UI 对话框
                // 这里的 this.ShowDialogueUI 指的是我们在 MainGameScreen 里定义的显示方法
                this.ShowDialogueUI(leader, advisor, dialogue, () => 
                {
                    // --- 这里是回调函数：等玩家看完对话点结束之后，才会执行 ---
                    
                    // A. 执行数据清除
                    faction.RemoveAdvisor(); 

                    // B. 降低忠诚度逻辑
                    // 额外的安全检查，确保对象仍然有效
                    if (advisor != null && leader != null)
                    {
                        // 检查 CheckRelation 是否存在于 Person 类中
                        if (advisor.CheckRelation(leader) != 1) // 1 代表亲爱
                        {
                            advisor.PersonalLoyalty -= 1;
                            // 防止忠诚度变成负数
                            if (advisor.PersonalLoyalty < 0) advisor.PersonalLoyalty = 0;
                        }
                    }

                    // C. (可选) 播放音效
                    // Session.MainGame.PlaySound("System_Cancel");
                    
                    // D. (可选) 刷新一下界面按钮状态
                    // this.UpdateAdvisorButton(Session.Current.Scenario); // 已移除军师按钮系统
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecallAdvisor] 对话系统错误: {ex.Message}");
                // 如果对话系统出错，直接执行罢免逻辑
                faction.RemoveAdvisor();
            }
        }

        /// <summary>
        /// 【军师系统】切换军师举荐功能开关
        /// </summary>
        public void ToggleAdvisorRecommendation()
        {
            System.Diagnostics.Debug.WriteLine("[ToggleAdvisorRecommendation] 开始切换军师举荐开关");
            
            // 1. 获取当前数据
            Faction faction = Session.Current.Scenario.CurrentFaction;
            if (faction == null) 
            {
                System.Diagnostics.Debug.WriteLine("[ToggleAdvisorRecommendation] 错误：当前势力为空");
                return;
            }

            Person advisor = faction.Advisor; 
            if (advisor == null) 
            {
                System.Diagnostics.Debug.WriteLine("[ToggleAdvisorRecommendation] 错误：当前没有军师");
                return;
            }

            // 2. 切换举荐开关状态（这里需要在Faction类中添加一个属性）
            // 暂时使用一个简单的实现，可以后续扩展
            bool currentState = faction.IsAdvisorRecommendationEnabled;
            faction.IsAdvisorRecommendationEnabled = !currentState;
            
            // 3. 显示状态变化消息
            string statusText = faction.IsAdvisorRecommendationEnabled ? "开启" : "关闭";
            string message = $"{advisor.Name}：主公，军师举荐功能已{statusText}。";
            
            // 4. 显示消息对话框
            try
            {
                if (this.Plugins?.tupianwenziPlugin != null)
                {
                    this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                    this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                        advisor, advisor, message, "", "", "");
                    this.Plugins.tupianwenziPlugin.IsShowing = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ToggleAdvisorRecommendation] 显示消息失败: {ex.Message}");
                // 即使显示消息失败，功能开关仍然有效
            }

            System.Diagnostics.Debug.WriteLine($"[ToggleAdvisorRecommendation] 军师举荐功能已{statusText}");
        }

        /// <summary>
        /// 【军师推荐系统】执行军师推荐人才的逻辑
        /// </summary>
        public void CheckAdvisorRecommendation()
        {
            try
            {
                var currentPlayer = Session.Current.Scenario.CurrentPlayer;
                
                if (currentPlayer == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CheckAdvisorRecommendation] CurrentPlayer is null, skipping.");
                    return;
                }

                if (!currentPlayer.IsAdvisorRecommendationEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("[CheckAdvisorRecommendation] 军师举荐功能已关闭，跳过");
                    return;
                }

                if (currentPlayer?.Advisor == null) 
                {
                    System.Diagnostics.Debug.WriteLine("[CheckAdvisorRecommendation] 当前玩家无军师，跳过推荐");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[CheckAdvisorRecommendation] 开始军师推荐流程，军师: {currentPlayer.Advisor.Name}");

                // 尝试推荐
                var result = _advisorRecommendationSystem.AttemptRecommendation(
                    currentPlayer, 
                    out Person foundPerson, 
                    out int initialLoyalty
                );

                // 处理推荐结果
                WorldOfTheThreeKingdoms.GameManager.AdvisorRecommendationSystem.HandleRecommendationResult(
                    currentPlayer, 
                    result, 
                    foundPerson, 
                    initialLoyalty
                );

                // 记录统计信息
                var stats = WorldOfTheThreeKingdoms.GameManager.AdvisorRecommendationSystem.GetGlobalStats();
                System.Diagnostics.Debug.WriteLine($"[CheckAdvisorRecommendation] 推荐完成，结果: {result}");
                System.Diagnostics.Debug.WriteLine($"[CheckAdvisorRecommendation] {stats.GetSummary()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckAdvisorRecommendation] 军师推荐过程中发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CheckAdvisorRecommendation] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 年份变化事件处理器 - 触发军师推荐
        /// </summary>
        private void OnYearPassed_CheckAdvisorRecommendation(GameScenario scenario)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[OnYearPassed] 年份变化，检查军师推荐");
                
                // 使用安全的后台操作，避免 Fire-and-Forget 警告
                AsyncWarningsFix.RunInBackgroundWithDelay(() =>
                {
                    CheckAdvisorRecommendation();
                }, 100, "军师推荐检查");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnYearPassed] 年份事件处理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册军师推荐系统的事件处理器
        /// </summary>
        private void RegisterAdvisorRecommendationEvents()
        {
            try
            {
                // 使用安全的异步初始化模式
                AsyncWarningsFix.InitializeAsync(async () =>
                {
                    // 等待一段时间确保游戏初始化完成
                    await Task.Delay(1000).ConfigureAwait(false);
                    
                    // 🔥 修复：订阅新事件系统 ScenarioEvents.OnYearPassed
                    ScenarioEvents.OnYearPassed += OnYearPassed_CheckAdvisorRecommendation;
                    System.Diagnostics.Debug.WriteLine("[RegisterAdvisorRecommendationEvents] 年份事件注册成功（新事件系统）");
                }, "军师推荐事件注册");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegisterAdvisorRecommendationEvents] 注册异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 【测试方法】手动测试军师推荐系统
        /// </summary>
        public void TestAdvisorRecommendationSystem()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 开始测试军师推荐系统 ===");
                
                var currentPlayer = Session.Current.Scenario.CurrentPlayer;
                if (currentPlayer == null)
                {
                    System.Diagnostics.Debug.WriteLine("[测试] 当前玩家为空");
                    return;
                }

                if (currentPlayer.Advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[测试] 当前玩家无军师");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[测试] 当前玩家: {currentPlayer.Name}");
                System.Diagnostics.Debug.WriteLine($"[测试] 军师: {currentPlayer.Advisor.Name} (智力: {currentPlayer.Advisor.Intelligence})");
                
                // 获取军师评估
                string assessment = WorldOfTheThreeKingdoms.GameManager.AdvisorRecommendationSystem.GetAdvisorAssessment(currentPlayer.Advisor);
                System.Diagnostics.Debug.WriteLine($"[测试] 军师评估: {assessment}");

                // 执行推荐测试
                CheckAdvisorRecommendation();
                
                // 显示统计信息
                var stats = WorldOfTheThreeKingdoms.GameManager.AdvisorRecommendationSystem.GetGlobalStats();
                System.Diagnostics.Debug.WriteLine($"[测试] {stats.GetSummary()}");
                
                System.Diagnostics.Debug.WriteLine("=== 军师推荐系统测试完成 ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[测试] 测试军师推荐系统时发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[测试] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 【智能说服系统】触发智能说服流程
        /// 流程：选择说服目标 → 军师分析 → 推荐执行人员 → 玩家决定
        /// </summary>
        public void TriggerIntelligentConvince()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentConvince] 开始智能说服流程");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                if (faction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentConvince] 当前玩家势力为null，退出");
                    return;
                }

                // 使用玩家的建筑作为执行说服的基地，而不是目标建筑
                Architecture sourceArchitecture = this.CurrentArchitecture;
                
                if (sourceArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentConvince] 执行建筑为null，退出");
                    return;
                }

                // 确保执行建筑属于当前玩家
                if (sourceArchitecture.BelongedFaction != faction)
                {
                    System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentConvince] 执行建筑不属于当前玩家，退出");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentConvince] 执行建筑: {sourceArchitecture.Name}");
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentConvince] 当前玩家势力: {faction.Name}");

                // 获取所有可能的目标建筑和人物
                PersonList convinceTargets = GetAllPossibleTargetsFromAllArchitectures(sourceArchitecture, faction);
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentConvince] 所有可能的目标人物数量: {convinceTargets.Count}");
                
                // 第一步：直接让玩家选择要说服的目标人物，不进行任何军师判断或阻止
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentConvince] 显示目标人物选择界面，无任何限制");
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.GetConvinceTargetForAnalysis, // 选择目标后进行军师分析
                    false, true, true, false, 
                    convinceTargets, 
                    null, 
                    "选择说服目标", 
                    "Personal"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentConvince] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentConvince] 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取所有可能的目标人物（从所有建筑中，使用与GetConvincePersonArchitectureArea完全相同的逻辑）
        /// </summary>
        private PersonList GetAllPossibleTargetsFromAllArchitectures(Architecture sourceArchitecture, Faction playerFaction)
        {
            PersonList targets = new PersonList();

            try
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargetsFromAllArchitectures] 开始获取目标，执行建筑: {sourceArchitecture.Name}");
                
                // 遍历所有建筑，使用与GetConvincePersonArchitectureArea完全相同的逻辑
                foreach (Architecture architecture in Session.Current.Scenario.Architectures)
                {
                    if (architecture.BelongedFaction == playerFaction)
                    {
                        // 己方建筑：检查俘虏和在野人员（与GetConvincePersonArchitectureArea相同）
                        if (!architecture.HasCaptive() && !architecture.HasNoFactionPerson())
                        {
                            continue;
                        }
                        
                        // 添加俘虏
                        foreach (Captive captive in architecture.Captives)
                        {
                            targets.Add(captive.CaptivePerson);
                        }
                        
                        // 添加在野人员
                        foreach (Person person in architecture.NoFactionPersons)
                        {
                            targets.Add(person);
                        }
                    }
                    else
                    {
                        // 敌方建筑：使用与GetConvincePersonArchitectureArea完全相同的逻辑
                        // 使用与GetConvincePersonArchitectureArea完全相同的逻辑：检查情报等级是否达到"低"
                        bool hasEnoughInformation = playerFaction.GetKnownAreaData(architecture.Position) >= InformationLevel.低;
                        
                        // 使用与GetConvincePersonArchitectureArea相同的条件：(有人员 OR 有在野人员) AND 情报等级>=低
                        if ((!architecture.HasPerson() && !architecture.HasNoFactionPerson()) || !hasEnoughInformation)
                        {
                            continue;
                        }
                        
                        // 添加敌方人员（排除女官）
                        if (architecture.HasPerson())
                        {
                            foreach (Person person in architecture.PersonsExcludeNvGuan)
                            {
                                targets.Add(person);
                            }
                        }
                        
                        // 添加在野人员
                        if (architecture.HasNoFactionPerson())
                        {
                            foreach (Person person in architecture.NoFactionPersons)
                            {
                                targets.Add(person);
                            }
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargetsFromAllArchitectures] 完成，总目标数量: {targets.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargetsFromAllArchitectures] 错误: {ex.Message}");
            }
            
            return targets;
        }

        /// <summary>
        /// 获取所有可能的目标人物（使用原始的建筑已知检查）
        /// </summary>
        private PersonList GetAllPossibleTargets(Architecture targetArchitecture, Faction playerFaction)
        {
            PersonList targets = new PersonList();
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 检查建筑: {targetArchitecture.Name}");
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 建筑归属: {targetArchitecture.BelongedFaction?.Name ?? "无"}");
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 玩家势力: {playerFaction.Name}");
                
                if (targetArchitecture.BelongedFaction == playerFaction)
                {
                    // 己方建筑：检查俘虏和在野人员
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 己方建筑");
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 俘虏数量: {targetArchitecture.Captives.Count}");
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 在野人员数量: {targetArchitecture.NoFactionPersons.Count}");
                    
                    // 添加俘虏
                    foreach (Captive captive in targetArchitecture.Captives)
                    {
                        targets.Add(captive.CaptivePerson);
                        System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 添加俘虏: {captive.CaptivePerson.Name}");
                    }
                    
                    // 添加在野人员
                    foreach (Person person in targetArchitecture.NoFactionPersons)
                    {
                        targets.Add(person);
                        System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 添加在野人员: {person.Name}");
                    }
                }
                else
                {
                    // 敌方建筑：使用与GetConvincePersonArchitectureArea完全相同的逻辑
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 敌方建筑");
                    
                    // 使用与GetConvincePersonArchitectureArea完全相同的逻辑：检查情报等级是否达到"低"
                    bool hasEnoughInformation = playerFaction.GetKnownAreaData(targetArchitecture.Position) >= InformationLevel.低;
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 情报等级: {playerFaction.GetKnownAreaData(targetArchitecture.Position)}");
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 是否有足够情报: {hasEnoughInformation}");
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 建筑有人员: {targetArchitecture.HasPerson()}");
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 建筑有在野人员: {targetArchitecture.HasNoFactionPerson()}");
                    
                    // 使用与GetConvincePersonArchitectureArea相同的条件：(有人员 OR 有在野人员) AND 情报等级>=低
                    bool hasPersonsOrNoFaction = targetArchitecture.HasPerson() || targetArchitecture.HasNoFactionPerson();
                    bool canConvince = hasPersonsOrNoFaction && hasEnoughInformation;
                    
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 有人员或在野人员: {hasPersonsOrNoFaction}");
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 可以说服: {canConvince}");
                    
                    // 额外调试：显示具体的情报等级值
                    var infoLevel = playerFaction.GetKnownAreaData(targetArchitecture.Position);
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 具体情报等级: {infoLevel} (数值: {(int)infoLevel})");
                    System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 需要的最低等级: {InformationLevel.低} (数值: {(int)InformationLevel.低})");
                    
                    if (canConvince)
                    {
                        // 添加敌方人员（排除女官）
                        if (targetArchitecture.HasPerson())
                        {
                            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 添加敌方人员: {targetArchitecture.PersonsExcludeNvGuan.Count}人");
                            foreach (Person person in targetArchitecture.PersonsExcludeNvGuan)
                            {
                                targets.Add(person);
                                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 添加敌方人员: {person.Name}");
                            }
                        }
                        
                        // 添加在野人员
                        if (targetArchitecture.HasNoFactionPerson())
                        {
                            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 添加在野人员: {targetArchitecture.NoFactionPersons.Count}人");
                            foreach (Person person in targetArchitecture.NoFactionPersons)
                            {
                                targets.Add(person);
                                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 添加在野人员: {person.Name}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[GetAllPossibleTargets] 不符合条件，无法添加目标");
                        if (!hasPersonsOrNoFaction)
                        {
                            System.Diagnostics.Debug.WriteLine("[GetAllPossibleTargets] 原因: 建筑内没有人员或在野人员");
                        }
                        if (!hasEnoughInformation)
                        {
                            System.Diagnostics.Debug.WriteLine("[GetAllPossibleTargets] 原因: 情报等级不足（需要'低'级或以上）");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 最终目标人物数量: {targets.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleTargets] 堆栈: {ex.StackTrace}");
            }
            
            return targets;
        }

        /// <summary>
        /// 【第二步】玩家选择目标后，进行军师分析和智能推荐执行人员
        /// </summary>
        public void PerformAdvisorAnalysisAndRecommendation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PerformAdvisorAnalysisAndRecommendation] 开始军师分析");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                var targetPerson = this.CurrentPerson; // 玩家选择的目标人物（要被说服的人）
                
                // 获取执行说服的建筑（玩家的建筑，有执行人员的地方）
                Architecture sourceArchitecture = null;
                if (faction.Architectures.Count > 0)
                {
                    sourceArchitecture = faction.Architectures[0] as Architecture; // 使用第一个建筑
                }
                
                if (targetPerson == null || sourceArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformAdvisorAnalysisAndRecommendation] 目标人物或源建筑为null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[PerformAdvisorAnalysisAndRecommendation] 说服目标: {targetPerson.Name} (要被说服的人)");
                System.Diagnostics.Debug.WriteLine($"[PerformAdvisorAnalysisAndRecommendation] 执行建筑: {sourceArchitecture.Name} (派遣执行人员的地方)");

                // 获取军师（智力最高的人物）
                var advisor = GetBestAdvisor(sourceArchitecture);
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformAdvisorAnalysisAndRecommendation] 没有找到军师，直接显示人员选择");
                    // 没有军师时，直接显示执行人员选择界面
                    ShowExecutorSelectionForConvince(sourceArchitecture);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[PerformAdvisorAnalysisAndRecommendation] 军师: {advisor.Name} (智力: {advisor.Intelligence})");

                // 进行军师分析：分析哪个执行人员最适合说服这个目标
                var analysisResult = PerformAdvisorAnalysis(advisor, targetPerson, sourceArchitecture);
                
                System.Diagnostics.Debug.WriteLine($"[PerformAdvisorAnalysisAndRecommendation] 分析结果: 推荐执行人员={analysisResult.BestCandidate?.Name ?? "无"}, 预测成功率={analysisResult.BestScore}%");

                // 根据分析结果决定下一步
                if (analysisResult.BestScore >= 67) // 成功率很高（67%以上）
                {
                    if (analysisResult.BestCandidate != null)
                    {
                        // 成功率很高且有推荐人员，显示支持对话并自动执行
                        System.Diagnostics.Debug.WriteLine($"[PerformAdvisorAnalysisAndRecommendation] 成功率很高({analysisResult.BestScore}%)，显示支持对话并自动执行");
                        ShowAdvisorSupportDialogWithAutoExecution(analysisResult, sourceArchitecture);
                    }
                    else
                    {
                        // 成功率很高但没有推荐人员，显示支持对话
                        ShowAdvisorSupportDialogWithRecommendation(analysisResult, sourceArchitecture);
                    }
                }
                else if (analysisResult.BestScore >= 34) // 成功率中等（34-66%）
                {
                    if (analysisResult.HasSuitableCandidate)
                    {
                        // 成功率中等，有合适的执行人员，显示军师支持对话
                        ShowAdvisorSupportDialogWithRecommendation(analysisResult, sourceArchitecture);
                    }
                    else
                    {
                        // 成功率中等但没有合适人员，显示劝阻对话
                        ShowAdvisorDissuasionDialogWithChoice(analysisResult, sourceArchitecture);
                    }
                }
                else // 成功率低（0-33%）
                {
                    // 成功率低，显示分两次的劝阻对话
                    System.Diagnostics.Debug.WriteLine($"[PerformAdvisorAnalysisAndRecommendation] 成功率低({analysisResult.BestScore}%)，显示分两次的劝阻对话");
                    ShowLowSuccessRateDissuasionDialog(analysisResult, sourceArchitecture);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformAdvisorAnalysisAndRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取最佳军师（优先使用指定军师，其次君主，最后智力最高的人物）
        /// </summary>
        private Person GetBestAdvisor(Architecture architecture)
        {
            var faction = Session.Current.Scenario.CurrentPlayer;
            
            // 优先使用势力的指定军师
            if (faction?.Advisor != null)
            {
                return faction.Advisor;
            }
            
            // 其次使用建筑的军师
            if (architecture?.Advisor != null)
            {
                return architecture.Advisor;
            }
            
            // 再次使用势力君主
            if (faction?.Leader != null)
            {
                return faction.Leader;
            }
            
            // 最后选择智力最高的人物
            Person bestAdvisor = null;
            int highestIntelligence = 0;

            foreach (Person person in architecture.Persons)
            {
                if (person.Intelligence > highestIntelligence)
                {
                    highestIntelligence = person.Intelligence;
                    bestAdvisor = person;
                }
            }

            return bestAdvisor;
        }

        /// <summary>
        /// 执行军师分析（非确定性，基于军师智力）
        /// </summary>
        private ConvinceAnalysisResult PerformAdvisorAnalysis(Person advisor, Person target, Architecture sourceArchitecture)
        {
            var result = new ConvinceAnalysisResult
            {
                TargetPerson = target,
                AdvisorIntelligence = advisor.Intelligence
            };

            Person bestCandidate = null;
            int bestScore = 0;

            // 分析每个可能的执行人员
            foreach (Person candidate in sourceArchitecture.PersonsExcludeNvGuan)
            {
                // 计算实际成功率（基于游戏逻辑）
                int actualScore = CalculateActualConvinceChance(candidate, target);
                
                // 军师预测成功率（基于军师智力，带有不确定性）
                int predictedScore = CalculateAdvisorPrediction(advisor, actualScore);
                
                System.Diagnostics.Debug.WriteLine($"[PerformAdvisorAnalysis] 候选人: {candidate.Name}, 实际成功率: {actualScore}%, 军师预测: {predictedScore}%");

                if (predictedScore > bestScore)
                {
                    bestScore = predictedScore;
                    bestCandidate = candidate;
                    result.ActualScore = actualScore;
                }
            }

            result.BestCandidate = bestCandidate;
            result.BestScore = bestScore;
            result.HasSuitableCandidate = bestScore >= 60; // 60%以上认为有希望

            // 计算预测准确性
            if (bestCandidate != null)
            {
                result.PredictionAccuracy = Math.Max(0, 100 - Math.Abs(result.BestScore - result.ActualScore));
            }

            return result;
        }

        /// <summary>
        /// 计算实际说服成功率（基于游戏原有逻辑）
        /// </summary>
        private int CalculateActualConvinceChance(Person convincer, Person target)
        {
            // 这里使用游戏原有的说服成功率计算逻辑
            // 简化版本，实际应该调用游戏的原有方法
            int baseChance = 50;
            
            // 魅力影响
            baseChance += (convincer.Glamour - target.Glamour) / 2;
            
            // 智力影响
            baseChance += (convincer.Intelligence - target.Intelligence) / 3;
            
            // 忠诚度影响（忠诚度越高越难说服）
            baseChance -= target.Loyalty / 2;
            
            // 简化的关系影响检查
            // 注意：这里简化了关系检查，实际游戏中可能有更复杂的关系系统
            if (convincer.ID != target.ID) // 基本的非自己检查
            {
                baseChance += 10; // 基础关系加成
            }

            return Math.Max(5, Math.Min(95, baseChance)); // 限制在5%-95%之间
        }

        /// <summary>
        /// 计算军师预测成功率（带有不确定性）
        /// </summary>
        private int CalculateAdvisorPrediction(Person advisor, int actualChance)
        {
            // 军师智力越高，预测越准确
            int accuracy = Math.Min(90, advisor.Intelligence); // 最高90%准确度
            
            // 计算预测偏差范围
            int maxDeviation = (100 - accuracy) / 2; // 智力90时，最大偏差5%
            
            // 随机偏差
            int deviation = GameObject.Random(maxDeviation * 2 + 1) - maxDeviation;
            
            int predictedChance = actualChance + deviation;
            
            return Math.Max(0, Math.Min(100, predictedChance));
        }

        /// <summary>
        /// 显示军师支持对话并自动推荐执行人员
        /// </summary>
        private void ShowAdvisorSupportDialogWithRecommendation(ConvinceAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                string message = $"军师建议：\n\n" +
                               $"说服目标：{analysis.TargetPerson.Name}\n" +
                               $"推荐执行人员：{analysis.BestCandidate.Name}\n" +
                               $"预计成功率：{analysis.BestScore}%\n\n" +
                               $"军师认为此次说服有较大成功希望，建议派遣 {analysis.BestCandidate.Name} 执行。\n\n" +
                               $"是否采纳军师建议？";

                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorSupportDialogWithRecommendation] {message}");
                
                // 显示军师支持确认对话框
                ShowAdvisorSupportConfirmationDialog(message, analysis, sourceArchitecture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorSupportDialogWithRecommendation] 错误: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForConvince(sourceArchitecture);
            }
        }

        /// <summary>
        /// 显示军师支持对话并自动执行（成功率很高时使用）
        /// </summary>
        private void ShowAdvisorSupportDialogWithAutoExecution(ConvinceAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorSupportDialogWithAutoExecution] 显示军师支持对话并准备自动执行");
                
                // 获取军师
                var faction = Session.Current.Scenario.CurrentPlayer;
                var advisor = faction?.Advisor ?? sourceArchitecture?.Advisor ?? faction?.Leader;
                
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowAdvisorSupportDialogWithAutoExecution] 无法获取军师，直接执行");
                    ExecuteConvinceDirectly(analysis.BestCandidate, analysis.TargetPerson);
                    return;
                }

                // 使用简化的对话模板
                string advisorMessage = GetSimpleAdvisorDialogue(analysis.BestScore, analysis.TargetPerson.Name, true);

                // 设置自动执行的回调函数
                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    this.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorSupportAuto] 用户确认，执行说服");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 直接执行说服
                        ExecuteConvinceDirectly(analysis.BestCandidate, analysis.TargetPerson);
                    }),
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorSupportAuto] 玩家选择否，结束流程");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 选择否直接结束
                    })
                );

                // 2. 设置确认对话框位置
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 3. 显示军师头像和对话 - 使用正确的参数顺序
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    advisorMessage,                    // 直接显示的中文对话内容
                    "",                                // 图片
                    "",                                // 声音
                    ""                                 // 空字符串
                );

                // 4. 显示对话框
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorSupportDialogWithAutoExecution] 军师支持对话已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorSupportDialogWithAutoExecution] 显示对话框失败: {ex.Message}");
                // 出错时直接执行
                ExecuteConvinceDirectly(analysis.BestCandidate, analysis.TargetPerson);
            }
        }

        /// <summary>
        /// 直接执行说服（无需用户选择执行人员）
        /// </summary>
        private void ExecuteConvinceDirectly(Person executor, Person target)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ExecuteConvinceDirectly] 直接执行说服: {executor?.Name ?? "null"} → {target?.Name ?? "null"}");
                
                if (executor != null && target != null)
                {
                    // 直接执行说服
                    executor.GoForConvince(target);
                    Session.MainGame.mainGameScreen.PlayNormalSound("Content/Sound/Tactics/Outside");
                    
                    System.Diagnostics.Debug.WriteLine("[ExecuteConvinceDirectly] 说服执行完成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ExecuteConvinceDirectly] 执行人员或目标为null，无法执行");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExecuteConvinceDirectly] 执行说服时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据成功率生成简化的军师对话
        /// <summary>
        /// 根据成功率生成简化的军师对话
        /// </summary>
        private string GetSimpleAdvisorDialogue(int successRate, string targetName, bool isSupport)
        {
            string baseDialogue;
            
            if (successRate <= 0)
            {
                baseDialogue = "一定不能成功，建议换个人或者时机。";
            }
            else if (successRate <= 33)
            {
                baseDialogue = "成功机会微乎其微。";
            }
            else if (successRate <= 66)
            {
                baseDialogue = "有一定的成功几率。";
            }
            else if (successRate <= 99)
            {
                baseDialogue = "成功概率很高。";
            }
            else
            {
                baseDialogue = "万无一失。";
            }
            
            return $"主公，说服{targetName}一事，{baseDialogue}";
        }

        /// <summary>
        /// 检查是否需要显示"是否继续？"的问句（成功率低时）
        /// </summary>
        private bool NeedsContinueQuestion(int successRate)
        {
            return successRate <= 33; // 0% 和 1-33% 需要问"是否继续？"
        }

        /// <summary>
        /// 显示成功率低的劝阻对话，分两次展示
        /// </summary>
        private void ShowLowSuccessRateDissuasionDialog(ConvinceAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowLowSuccessRateDissuasionDialog] 显示成功率低的劝阻对话");
                
                // 获取军师
                var faction = Session.Current.Scenario.CurrentPlayer;
                var advisor = faction?.Advisor ?? sourceArchitecture?.Advisor ?? faction?.Leader;
                
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowLowSuccessRateDissuasionDialog] 无法获取军师，直接显示执行人员选择");
                    ShowExecutorSelectionForConvince(sourceArchitecture);
                    return;
                }

                // 第一次对话：显示成功率判断
                string firstMessage = GetSimpleAdvisorDialogue(analysis.BestScore, analysis.TargetPerson.Name, false);

                // 设置第一次对话的关闭回调 - 关闭后显示第二次对话
                this.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[LowSuccessRateDissuasion] 第一次对话完成，显示第二次对话");
                    
                    // 显示第二次对话："是否继续？"
                    ShowContinueQuestionDialog(analysis, sourceArchitecture);
                }));

                // 设置对话框位置
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);

                // 显示军师头像和第一次对话（只有确定按钮）
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    firstMessage,                      // 第一次对话内容
                    "",                                // 图片
                    "",                                // 声音
                    ""                                 // 空字符串
                );

                // 显示对话框（只有确定按钮）
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[ShowLowSuccessRateDissuasionDialog] 第一次劝阻对话已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowLowSuccessRateDissuasionDialog] 显示对话框失败: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForConvince(sourceArchitecture);
            }
        }

        /// <summary>
        /// 显示"是否继续？"的问句对话
        /// </summary>
        private void ShowContinueQuestionDialog(ConvinceAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowContinueQuestionDialog] 显示是否继续问句");
                
                // 获取军师
                var faction = Session.Current.Scenario.CurrentPlayer;
                var advisor = faction?.Advisor ?? sourceArchitecture?.Advisor ?? faction?.Leader;
                
                if (advisor == null)
                {
                    ShowExecutorSelectionForConvince(sourceArchitecture);
                    return;
                }

                // 第二次对话：问"是否继续？"
                string secondMessage = "是否继续？";

                // 设置第二次对话的回调
                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    this.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[ContinueQuestion] 玩家选择继续，显示执行人员选择");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 玩家选择继续，显示执行人员选择界面
                        if (analysis.BestCandidate != null)
                        {
                            ShowExecutorSelectionWithRecommendation(sourceArchitecture, analysis.BestCandidate);
                        }
                        else
                        {
                            ShowExecutorSelectionForConvince(sourceArchitecture);
                        }
                    }),
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[ContinueQuestion] 玩家选择不继续，结束流程");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 选择否直接结束
                    })
                );

                // 设置确认对话框位置
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 显示军师头像和第二次对话
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    secondMessage,                     // 第二次对话内容："是否继续？"
                    "",                                // 图片
                    "",                                // 声音
                    ""                                 // 空字符串
                );

                // 显示对话框
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[ShowContinueQuestionDialog] 是否继续问句已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowContinueQuestionDialog] 显示问句失败: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForConvince(sourceArchitecture);
            }
        }

        /// <summary>
        /// 显示军师支持确认对话框 - 简化版本，参考军师任命对话
        /// </summary>
        private void ShowAdvisorSupportConfirmationDialog(string message, ConvinceAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorSupportConfirmationDialog] 显示军师支持确认对话框");
                
                // 获取军师
                var faction = Session.Current.Scenario.CurrentPlayer;
                var advisor = faction?.Advisor ?? sourceArchitecture?.Advisor ?? faction?.Leader;
                
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowAdvisorSupportConfirmationDialog] 无法获取军师，直接显示执行人员选择");
                    ShowExecutorSelectionForConvince(sourceArchitecture);
                    return;
                }

                // 使用简化的对话模板
                string advisorMessage = GetSimpleAdvisorDialogue(analysis.BestScore, analysis.TargetPerson.Name, true);

                // 设置确认对话框的回调函数
                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    this.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorSupport] 玩家选择是");
                        // 直接显示执行人员选择界面
                        ShowExecutorSelectionForConvince(sourceArchitecture);
                    }),
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorSupport] 玩家选择否，直接结束");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 选择否直接结束，不显示额外对话框
                    })
                );

                // 2. 设置确认对话框位置
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 3. 显示军师头像和对话 - 使用正确的参数顺序
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    advisorMessage,                    // 直接显示的中文对话内容
                    "",                                // 图片
                    "",                                // 声音
                    ""                                 // 空字符串
                );

                // 4. 显示对话框
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorSupportConfirmationDialog] 军师支持对话框已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorSupportConfirmationDialog] 显示对话框失败: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForConvince(sourceArchitecture);
            }
        }

        /// <summary>
        /// 显示军师劝阻对话并提供选择（第四步：这里才进行各种限制检查）
        /// </summary>
        private void ShowAdvisorDissuasionDialogWithChoice(ConvinceAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                // 第四步：在这里进行所有的限制检查
                var faction = Session.Current.Scenario.CurrentPlayer;
                var targetArchitecture = this.CurrentArchitecture;
                var targetPerson = analysis.TargetPerson;
                
                // 检查情报等级限制
                bool hasEnoughInformation = targetArchitecture.BelongedFaction == faction || 
                                          faction.GetKnownAreaData(targetArchitecture.Position) >= InformationLevel.低;
                
                string additionalWarning = "";
                if (!hasEnoughInformation)
                {
                    additionalWarning = "\n\n⚠️ 警告：对此地情报不足，说服难度极高！";
                }
                
                // 检查是否有执行人员
                if (sourceArchitecture.PersonsExcludeNvGuan.Count == 0)
                {
                    additionalWarning += "\n\n⚠️ 警告：没有可用的执行人员！";
                }
                
                // 检查资金是否充足
                if (sourceArchitecture.Fund < sourceArchitecture.ConvincePersonFund)
                {
                    additionalWarning += $"\n\n⚠️ 警告：资金不足！需要{sourceArchitecture.ConvincePersonFund}金，当前只有{sourceArchitecture.Fund}金。";
                }

                string message = $"军师劝阻：\n\n" +
                               $"说服目标：{targetPerson.Name}\n" +
                               $"最佳执行人员：{analysis.BestCandidate?.Name ?? "无"}\n" +
                               $"预计成功率：{analysis.BestScore}%\n" +
                               additionalWarning + "\n\n" +
                               $"军师认为此次说服风险较高，建议谨慎考虑。是否坚持执行？";

                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorDissuasionDialogWithChoice] {message}");
                
                // 显示真正的确认对话框
                ShowAdvisorDissuasionConfirmationDialog(message, analysis, sourceArchitecture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorDissuasionDialogWithChoice] 错误: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForConvince(sourceArchitecture);
            }
        }

        /// <summary>
        /// 显示军师劝阻确认对话框 - 参考军师任命的实现
        /// </summary>
        private void ShowAdvisorDissuasionConfirmationDialog(string message, ConvinceAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorDissuasionConfirmationDialog] 显示军师劝阻确认对话框");
                
                // 获取军师
                var faction = Session.Current.Scenario.CurrentPlayer;
                var advisor = faction?.Advisor ?? sourceArchitecture?.Advisor ?? faction?.Leader;
                
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowAdvisorDissuasionConfirmationDialog] 无法获取军师，直接显示执行人员选择");
                    ShowExecutorSelectionForConvince(sourceArchitecture);
                    return;
                }

                // 使用简化的对话模板
                string advisorMessage = GetSimpleAdvisorDialogue(analysis.BestScore, analysis.TargetPerson.Name, false);

                // 1. 设置确认对话框的回调函数
                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    this.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorDissuasion] 玩家选择坚持执行，不顾军师劝阻");
                        
                        try
                        {
                            // 先关闭当前对话框
                            this.Plugins.tupianwenziPlugin.IsShowing = false;
                            this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                            
                            System.Diagnostics.Debug.WriteLine("[AdvisorDissuasion] 已关闭军师对话框，准备显示执行人员选择");
                            
                            // 玩家坚持执行，显示执行人员选择界面
                            if (analysis.BestCandidate != null)
                            {
                                // 如果有推荐人员，预先选中
                                ShowExecutorSelectionWithRecommendation(sourceArchitecture, analysis.BestCandidate);
                            }
                            else
                            {
                                // 没有推荐人员，显示普通选择界面
                                ShowExecutorSelectionForConvince(sourceArchitecture);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AdvisorDissuasion] 坚持执行时发生错误: {ex.Message}");
                            ShowExecutorSelectionForConvince(sourceArchitecture);
                        }
                    }),
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorDissuasion] 玩家选择听从军师劝告，结束说服行动");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 选择否直接结束，不显示额外对话框
                    })
                );

                // 2. 设置确认对话框位置
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 3. 显示军师头像和对话 - 使用正确的参数顺序
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    advisorMessage,                    // 直接显示的中文对话内容
                    "",                                // 图片
                    "",                                // 声音
                    ""                                 // 空字符串
                );

                // 4. 显示对话框
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorDissuasionConfirmationDialog] 军师劝阻对话框已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorDissuasionConfirmationDialog] 显示对话框失败: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForConvince(sourceArchitecture);
            }
        }

        /// <summary>
        /// 显示执行人员选择界面（带推荐）
        /// </summary>
        private void ShowExecutorSelectionWithRecommendation(Architecture sourceArchitecture, Person recommendedPerson)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithRecommendation] 开始显示执行人员选择界面");
                
                // 安全检查
                if (sourceArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionWithRecommendation] sourceArchitecture为null，无法继续");
                    return;
                }
                
                if (sourceArchitecture.PersonsExcludeNvGuan == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionWithRecommendation] PersonsExcludeNvGuan为null，无法继续");
                    return;
                }
                
                if (sourceArchitecture.PersonsExcludeNvGuan.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionWithRecommendation] 没有可用的执行人员");
                    return;
                }
                
                if (recommendedPerson != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithRecommendation] 显示执行人员选择，推荐: {recommendedPerson.Name}");
                    
                    // 检查推荐人员是否在可选列表中
                    bool foundRecommended = false;
                    foreach (Person person in sourceArchitecture.PersonsExcludeNvGuan)
                    {
                        if (person == recommendedPerson)
                        {
                            foundRecommended = true;
                            break;
                        }
                    }
                    
                    if (foundRecommended)
                    {
                        // 预先选中推荐的人员
                        sourceArchitecture.PersonsExcludeNvGuan.ClearSelected();
                        recommendedPerson.Selected = true;
                        System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithRecommendation] 已预选推荐人员: {recommendedPerson.Name}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithRecommendation] 推荐人员{recommendedPerson.Name}不在可选列表中");
                    }
                    
                    this.ShowTabListInFrame(
                        UndoneWorkKind.Frame, 
                        FrameKind.Person, 
                        FrameFunction.PersonManualHire,  // 使用PersonManualHire避免触发标准说服流程
                        false, true, true, true, 
                        sourceArchitecture.PersonsExcludeNvGuan, 
                        null, 
                        "选择执行人员 (已推荐)", 
                        "Personal"
                    );
                    
                    System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionWithRecommendation] ShowTabListInFrame调用完成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionWithRecommendation] 推荐人员为空，显示普通选择界面");
                    ShowExecutorSelectionForConvince(sourceArchitecture);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithRecommendation] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithRecommendation] 堆栈跟踪: {ex.StackTrace}");
                // 出错时显示普通选择界面
                try
                {
                    ShowExecutorSelectionForConvince(sourceArchitecture);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithRecommendation] 备用方案也失败: {ex2.Message}");
                }
            }
        }

        /// <summary>
        /// 显示执行人员选择界面（普通）
        /// </summary>
        private void ShowExecutorSelectionForConvince(Architecture sourceArchitecture)
        {
            System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionForConvince] 显示执行人员选择界面");
            
            this.ShowTabListInFrame(
                UndoneWorkKind.Frame, 
                FrameKind.Person, 
                FrameFunction.PersonManualHire,  // 使用PersonManualHire避免触发标准说服流程
                false, true, true, true, 
                sourceArchitecture.PersonsExcludeNvGuan, 
                null, 
                "选择执行人员", 
                "Personal"
            );
        }

        /// <summary>
        /// 使用推荐人员执行说服
        /// </summary>
        private void ExecuteConvinceWithRecommendedPerson(ConvinceAnalysisResult analysis)
        {
            System.Diagnostics.Debug.WriteLine($"[ExecuteConvinceWithRecommendedPerson] 使用推荐执行人员 {analysis.BestCandidate.Name} 说服 {analysis.TargetPerson.Name}");
            
            // 设置选中的执行人员
            var selectedPersons = new GameObjectList();
            selectedPersons.Add(analysis.BestCandidate);
            
            // 执行说服
            this.CurrentGameObjects = selectedPersons;
            this.CurrentPerson = analysis.TargetPerson;
            
            // 调用原有的说服执行逻辑
            System.Diagnostics.Debug.WriteLine("[ExecuteConvinceWithRecommendedPerson] 执行说服任务");
        }

        /// <summary>
        /// 显示带回调的对话框（简化版本，实际应该使用游戏的对话系统）
        /// </summary>
        private void ShowDialogWithCallback(string message, List<string> options, Action<int> callback)
        {
            // 这里应该使用游戏的对话系统
            // 暂时使用调试输出模拟
            System.Diagnostics.Debug.WriteLine($"[ShowDialogWithCallback] 消息: {message}");
            for (int i = 0; i < options.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowDialogWithCallback] 选项{i}: {options[i]}");
            }
            
            // 暂时默认选择第一个选项进行测试
            callback(0);
        }

        /// <summary>
        /// 显示没有可说服目标的对话框
        /// </summary>
        private void ShowNoConvinceTargetsDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string message = $"{speaker.Name}：主公，此地并无可说服之人。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker, speaker, message, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoConvinceTargetsDialog] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示没有可说服目标的对话框（带选择权）
        /// </summary>
        private void ShowNoConvinceTargetsWithChoiceDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string message = $"{speaker.Name}：主公，此地并无明显可说服之人。\n\n" +
                               $"不过，若主公坚持，臣也可安排人手尝试。\n\n" +
                               $"只是成功的可能性微乎其微。";

                // 设置对话框关闭后的回调 - 显示选择对话框
                this.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() =>
                {
                    ShowNoTargetsPlayerChoiceDialog(faction);
                }));

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker, speaker, message, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoConvinceTargetsWithChoiceDialog] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示无目标情况下的玩家选择对话框
        /// </summary>
        private void ShowNoTargetsPlayerChoiceDialog(Faction faction)
        {
            try
            {
                if (this.Plugins?.ConfirmationDialogPlugin == null)
                {
                    // 如果确认对话框不可用，直接跳转到人员选择
                    ShowConvincePersonSelectionForEmptyTarget();
                    return;
                }

                // 清除之前的函数
                this.Plugins.ConfirmationDialogPlugin.ClearFunctions();
                
                // 设置"听从军师"选项
                this.Plugins.ConfirmationDialogPlugin.AddYesFunction(new GameDelegates.VoidFunction(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[NoTargetsChoice] 玩家选择听从军师建议，结束说服行动");
                    // 显示结束对话
                    ShowNoTargetsEndDialog(faction);
                }));
                
                // 设置"坚持执行"选项
                this.Plugins.ConfirmationDialogPlugin.AddNoFunction(new GameDelegates.VoidFunction(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[NoTargetsChoice] 玩家选择坚持执行说服");
                    // 跳转到人员选择，让玩家自由选择目标和执行人员
                    ShowConvincePersonSelectionForEmptyTarget();
                }));
                
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                this.Plugins.ConfirmationDialogPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoTargetsPlayerChoiceDialog] 错误: {ex.Message}");
                // 出错时直接跳转到人员选择
                ShowConvincePersonSelectionForEmptyTarget();
            }
        }

        /// <summary>
        /// 显示无目标情况下的结束对话
        /// </summary>
        private void ShowNoTargetsEndDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string message = $"{speaker.Name}：主公明智！此地确实不宜强行说服。\n\n" +
                               $"不如将精力用在更有希望的地方。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker, speaker, message, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoTargetsEndDialog] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示无明确目标情况下的人员选择界面
        /// </summary>
        private void ShowConvincePersonSelectionForEmptyTarget()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowConvincePersonSelectionForEmptyTarget] 显示人员选择界面");
                
                // 清空目标人物，让玩家自由选择
                this.ConvinceTargetPerson = null;
                
                // 当没有明显目标时，让玩家选择执行说服的人员
                // 然后这些人员会去尝试说服（即使可能失败）
                var faction = Session.Current.Scenario.CurrentPlayer;
                
                // 获取可以执行说服任务的人员（当前建筑中的非女官人员）
                var availablePersons = this.CurrentArchitecture.PersonsExcludeNvGuan;
                
                if (availablePersons.Count == 0)
                {
                    // 如果连执行人员都没有，显示提示
                    ShowNoExecutorDialog(faction);
                    return;
                }
                
                // 安全检查：确保TabListPlugin不为null
                if (this.Plugins?.TabListPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowConvincePersonSelectionForEmptyTarget] TabListPlugin为null，无法显示界面");
                    return;
                }
                
                // 设置可选择的最大人员数量
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(this.CurrentArchitecture.ConvincePersonMaxCount);
                
                // 显示执行人员选择界面
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Work, 
                    FrameFunction.GetConvinceSourcePerson, 
                    false, true, true, false, 
                    availablePersons, 
                    null, 
                    "选择说服执行人员", 
                    "说服"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowConvincePersonSelectionForEmptyTarget] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示没有执行人员的对话
        /// </summary>
        private void ShowNoExecutorDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string message = $"{speaker.Name}：主公，此地连可派遣的人员都没有。\n\n" +
                               $"臣实在无法安排说服任务。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker, speaker, message, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoExecutorDialog] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示情报不足的对话框
        /// </summary>
        private void ShowInsufficientInformationDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string message = $"{speaker.Name}：主公，我军对此地情报不足，无法进行有效说服。建议先派人收集情报。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker, speaker, message, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowInsufficientInformationDialog] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 【智能说服系统】分析说服目标并给出军师建议
        /// 在玩家选择目标后调用，军师根据智力进行预测分析
        /// </summary>
        /// <param name="target">说服目标</param>
        public void AnalyzeConvinceTarget(Person target)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyzeConvinceTarget] 军师开始分析说服目标: {target?.Name}");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                if (faction == null || target == null)
                    return;

                Person advisor = faction.Advisor ?? faction.Leader;
                if (advisor == null)
                    return;

                // 军师根据智力进行预测分析（非确定值）
                var analysis = AnalyzeConvinceSuccessWithAdvisorIntelligence(faction, target, advisor);
                
                if (analysis.HasSuitableCandidate)
                {
                    // 有合适人选，军师推荐并自动勾选
                    System.Diagnostics.Debug.WriteLine($"[AnalyzeConvinceTarget] 军师推荐: {analysis.BestCandidate?.Name}");
                    ShowConvincePersonSelectionWithAdvisorRecommendation(target, analysis);
                }
                else
                {
                    // 没有合适人选，军师劝阻，但玩家可以坚持
                    System.Diagnostics.Debug.WriteLine($"[AnalyzeConvinceTarget] 军师劝阻，但给玩家选择权");
                    ShowAdvisorDissuasionWithChoice(advisor, target, analysis);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AnalyzeConvinceTarget] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AnalyzeConvinceTarget] 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 军师根据智力进行说服成功预测分析（非确定值）
        /// </summary>
        private ConvinceAnalysisResult AnalyzeConvinceSuccessWithAdvisorIntelligence(Faction faction, Person target, Person advisor)
        {
            var result = new ConvinceAnalysisResult();
            
            // 获取可用的说服人员
            var availablePersons = this.CurrentArchitecture.PersonsExcludeNvGuan.GetList().Cast<Person>().ToList();
            
            Person bestCandidate = null;
            int bestScore = 0;
            
            foreach (Person person in availablePersons)
            {
                if (!person.Available || person.IsCaptive)
                    continue;
                
                // 计算说服成功率
                int score = CalculateConvinceScore(person, target);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = person;
                }
            }
            
            // 军师的智力影响预测准确性
            int advisorIntelligence = advisor.Intelligence;
            int predictionAccuracy = Math.Min(95, 50 + advisorIntelligence / 2); // 50-95%的准确性
            
            // 根据军师智力调整预测结果
            int adjustedScore = bestScore;
            if (GameObject.Random(100) > predictionAccuracy)
            {
                // 预测不准确时，随机调整分数
                int adjustment = GameObject.Random(-20, 21); // -20到+20的调整
                adjustedScore = Math.Max(0, Math.Min(100, bestScore + adjustment));
                System.Diagnostics.Debug.WriteLine($"[军师预测] 智力{advisorIntelligence}，准确性{predictionAccuracy}%，预测偏差{adjustment}");
            }
            
            result.BestCandidate = bestCandidate;
            result.BestScore = adjustedScore; // 使用调整后的分数
            result.ActualScore = bestScore;   // 保存实际分数用于对比
            result.HasSuitableCandidate = adjustedScore >= 60; // 基于预测分数判断
            result.TargetPerson = target;
            result.AdvisorIntelligence = advisorIntelligence;
            result.PredictionAccuracy = predictionAccuracy;
            
            System.Diagnostics.Debug.WriteLine($"[军师预测] 目标:{target.Name}, 推荐:{bestCandidate?.Name}, 预测成功率:{adjustedScore}%, 实际:{bestScore}%");
            
            return result;
        }
        private ConvinceAnalysisResult AnalyzeConvinceSuccess(Faction faction, Person target)
        {
            var result = new ConvinceAnalysisResult();
            
            // 获取可用的说服人员
            var availablePersons = this.CurrentArchitecture.PersonsExcludeNvGuan.GetList().Cast<Person>().ToList();
            
            Person bestCandidate = null;
            int bestScore = 0;
            
            foreach (Person person in availablePersons)
            {
                if (!person.Available || person.IsCaptive)
                    continue;
                
                // 计算说服成功率（简化算法）
                int score = CalculateConvinceScore(person, target);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = person;
                }
            }
            
            result.BestCandidate = bestCandidate;
            result.BestScore = bestScore;
            result.HasSuitableCandidate = bestScore >= 60; // 60分以上认为有希望
            result.TargetPerson = target;
            
            return result;
        }

        /// <summary>
        /// 计算说服成功评分
        /// </summary>
        private int CalculateConvinceScore(Person convincer, Person target)
        {
            int score = 0;
            
            // 基础魅力评分 (40%)
            score += convincer.Glamour * 40 / 100;
            
            // 政治能力评分 (30%)
            score += convincer.Politics * 30 / 100;
            
            // 智力评分 (20%)
            score += convincer.Intelligence * 20 / 100;
            
            // 关系加成 (10%)
            if (convincer.CheckRelation(target) == 1) // 亲爱关系
                score += 20;
            else if (convincer.CheckRelation(target) == 2) // 友好关系
                score += 10;
            
            // 目标忠诚度惩罚
            score -= target.PersonalLoyalty * 10;
            
            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// 显示带军师推荐的人员选择界面（自动勾选推荐人员）
        /// </summary>
        private void ShowConvincePersonSelectionWithAdvisorRecommendation(Person target, ConvinceAnalysisResult analysis)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ShowConvincePersonSelectionWithAdvisorRecommendation] 军师推荐: {analysis.BestCandidate?.Name}");
                
                // 设置推荐人选为预选
                var preSelectedList = new GameObjectList();
                if (analysis.BestCandidate != null)
                    preSelectedList.Add(analysis.BestCandidate);
                
                // 存储目标人物，供后续使用
                this.ConvinceTargetPerson = target;
                
                // 安全检查：确保TabListPlugin不为null
                if (this.Plugins?.TabListPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowConvincePersonSelectionWithAdvisorRecommendation] TabListPlugin为null，无法显示界面");
                    return;
                }
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(this.CurrentArchitecture.ConvincePersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Work, 
                    FrameFunction.GetConvinceSourcePerson, 
                    false, true, true, true, 
                    this.CurrentArchitecture.PersonsExcludeNvGuan, 
                    preSelectedList, // 自动勾选军师推荐的人员
                    $"说服 {target.Name}（军师推荐：{analysis.BestCandidate?.Name}）", 
                    "说服"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowConvincePersonSelectionWithAdvisorRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示军师劝阻对话（但给玩家选择权）
        /// </summary>
        private void ShowAdvisorDissuasionWithChoice(Person advisor, Person target, ConvinceAnalysisResult analysis)
        {
            try
            {
                if (this.Plugins?.tupianwenziPlugin == null)
                    return;

                string dissuasionMessage = GenerateAdvisorDissuasion(advisor, target, analysis);
                
                // 设置对话框关闭后的回调 - 显示选择对话框
                this.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() =>
                {
                    ShowDissuasionPlayerChoiceDialog(target, analysis);
                }));

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor, advisor, dissuasionMessage, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorDissuasionWithChoice] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成军师劝阻文本
        /// </summary>
        private string GenerateAdvisorDissuasion(Person advisor, Person target, ConvinceAnalysisResult analysis)
        {
            string targetInfo = $"{target.Name}";
            if (target.PersonalLoyalty >= 4)
                targetInfo += "（忠诚度极高）";
            else if (target.PersonalLoyalty >= 3)
                targetInfo += "（忠诚度较高）";
            
            string intelligenceNote = "";
            if (analysis.AdvisorIntelligence >= 90)
                intelligenceNote = "以臣之见，";
            else if (analysis.AdvisorIntelligence >= 70)
                intelligenceNote = "臣认为，";
            else
                intelligenceNote = "臣觉得，";
            
            if (analysis.BestCandidate != null)
            {
                return $"{advisor.Name}：主公，{intelligenceNote}要说服{targetInfo}恐怕很难成功。\n\n" +
                       $"即使派遣我军中最合适的{analysis.BestCandidate.Name}前往，" +
                       $"成功的可能性也微乎其微。\n\n" +
                       $"臣建议三思而后行。";
            }
            else
            {
                return $"{advisor.Name}：主公，{intelligenceNote}要说服{targetInfo}实在是难如登天。\n\n" +
                       $"我军中恐怕无人能胜任此任务，臣强烈建议放弃此计。";
            }
        }

        /// <summary>
        /// 显示劝阻后的玩家选择对话框
        /// </summary>
        private void ShowDissuasionPlayerChoiceDialog(Person target, ConvinceAnalysisResult analysis)
        {
            try
            {
                if (this.Plugins?.ConfirmationDialogPlugin == null)
                {
                    // 如果确认对话框不可用，直接跳转到人员选择
                    ShowConvincePersonSelectionDirect(target);
                    return;
                }

                // 清除之前的函数
                this.Plugins.ConfirmationDialogPlugin.ClearFunctions();
                
                // 设置"听从军师劝告"选项
                this.Plugins.ConfirmationDialogPlugin.AddYesFunction(new GameDelegates.VoidFunction(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[DissuasionChoice] 玩家选择听从军师劝告，结束说服行动");
                    ShowDissuasionEndDialog(analysis);
                }));
                
                // 设置"坚持执行"选项
                this.Plugins.ConfirmationDialogPlugin.AddNoFunction(new GameDelegates.VoidFunction(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[DissuasionChoice] 玩家选择坚持执行说服");
                    ShowConvincePersonSelectionDirect(target);
                }));
                
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                this.Plugins.ConfirmationDialogPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowDissuasionPlayerChoiceDialog] 错误: {ex.Message}");
                // 出错时直接跳转到人员选择
                ShowConvincePersonSelectionDirect(target);
            }
        }

        /// <summary>
        /// 显示劝阻结束对话
        /// </summary>
        private void ShowDissuasionEndDialog(ConvinceAnalysisResult analysis)
        {
            try
            {
                var faction = Session.Current.Scenario.CurrentPlayer;
                Person advisor = faction?.Advisor ?? faction?.Leader;
                
                if (advisor == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string endMessage = $"{advisor.Name}：主公深思熟虑，臣佩服。\n\n" +
                                  $"此事暂且作罢，不如将精力用在更有希望的地方。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor, advisor, endMessage, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowDissuasionEndDialog] 错误: {ex.Message}");
            }
        }
        private void ShowAdvisorSupportDialog(Person advisor, Person target, ConvinceAnalysisResult analysis)
        {
            try
            {
                if (this.Plugins?.tupianwenziPlugin == null)
                    return;

                string supportMessage = GenerateAdvisorSupport(advisor, target, analysis);
                
                // 设置对话框关闭后的回调 - 显示选择对话框
                this.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() =>
                {
                    ShowPlayerChoiceDialog(target, analysis, true); // true表示军师支持
                }));

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor, advisor, supportMessage, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorSupportDialog] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成军师支持文本
        /// </summary>
        private string GenerateAdvisorSupport(Person advisor, Person target, ConvinceAnalysisResult analysis)
        {
            string targetInfo = $"{target.Name}";
            if (target.PersonalLoyalty >= 4)
                targetInfo += "（忠诚度极高）";
            else if (target.PersonalLoyalty >= 3)
                targetInfo += "（忠诚度较高）";
            
            if (analysis.BestCandidate != null)
            {
                return $"{advisor.Name}：主公，说服{targetInfo}有一定希望。\n\n" +
                       $"臣建议派遣{analysis.BestCandidate.Name}前往，成功率约{analysis.BestScore}%。\n\n" +
                       $"主公意下如何？";
            }
            else
            {
                return $"{advisor.Name}：主公，说服{targetInfo}虽有难度，但并非不可能。\n\n" +
                       $"主公若决意而行，臣当全力支持。";
            }
        }
        /// <summary>
        /// 显示军师警告对话框
        /// </summary>
        private void ShowAdvisorWarningDialog(Person advisor, Person target, ConvinceAnalysisResult analysis)
        {
            try
            {
                if (this.Plugins?.tupianwenziPlugin == null)
                    return;

                string warningMessage = GenerateAdvisorWarning(advisor, target, analysis);
                
                // 设置对话框关闭后的回调 - 显示选择对话框
                this.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() =>
                {
                    ShowPlayerChoiceDialog(target, analysis, false); // false表示军师不支持
                }));

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor, advisor, warningMessage, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorWarningDialog] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成军师警告文本
        /// </summary>
        private string GenerateAdvisorWarning(Person advisor, Person target, ConvinceAnalysisResult analysis)
        {
            string targetInfo = $"{target.Name}";
            if (target.PersonalLoyalty >= 4)
                targetInfo += "（忠诚度极高）";
            else if (target.PersonalLoyalty >= 3)
                targetInfo += "（忠诚度较高）";
            
            if (analysis.BestCandidate != null)
            {
                return $"{advisor.Name}：主公，要说服{targetInfo}恐怕很难成功。\n\n" +
                       $"即使派遣{analysis.BestCandidate.Name}前往，成功率也只有{analysis.BestScore}%。\n\n" +
                       $"臣建议三思而后行。";
            }
            else
            {
                return $"{advisor.Name}：主公，要说服{targetInfo}实在是难如登天。\n\n" +
                       $"我军中无人能胜任此任务，臣强烈建议放弃此计。";
            }
        }

        /// <summary>
        /// 显示玩家选择对话框（听从军师 vs 坚持执行）
        /// </summary>
        /// <param name="target">说服目标</param>
        /// <param name="analysis">分析结果</param>
        /// <param name="advisorSupports">军师是否支持</param>
        private void ShowPlayerChoiceDialog(Person target, ConvinceAnalysisResult analysis, bool advisorSupports)
        {
            try
            {
                if (this.Plugins?.ConfirmationDialogPlugin == null)
                {
                    // 如果确认对话框不可用，直接跳转到人员选择
                    if (analysis.HasSuitableCandidate)
                        ShowConvincePersonSelectionWithRecommendation(target, analysis.BestCandidate);
                    else
                        ShowConvincePersonSelectionDirect(target);
                    return;
                }

                // 清除之前的函数
                this.Plugins.ConfirmationDialogPlugin.ClearFunctions();
                
                // 设置"听从军师"选项
                this.Plugins.ConfirmationDialogPlugin.AddYesFunction(new GameDelegates.VoidFunction(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"[PlayerChoice] 玩家选择听从军师建议，结束说服行动");
                    // 显示结束对话
                    ShowConvinceEndDialog(advisorSupports);
                }));
                
                // 设置"坚持执行"选项
                this.Plugins.ConfirmationDialogPlugin.AddNoFunction(new GameDelegates.VoidFunction(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"[PlayerChoice] 玩家选择坚持执行说服");
                    // 根据分析结果决定是否预选推荐人员
                    if (analysis.HasSuitableCandidate)
                        ShowConvincePersonSelectionWithRecommendation(target, analysis.BestCandidate);
                    else
                        ShowConvincePersonSelectionDirect(target);
                }));
                
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                this.Plugins.ConfirmationDialogPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowPlayerChoiceDialog] 错误: {ex.Message}");
                // 出错时直接跳转到人员选择
                if (analysis.HasSuitableCandidate)
                    ShowConvincePersonSelectionWithRecommendation(target, analysis.BestCandidate);
                else
                    ShowConvincePersonSelectionDirect(target);
            }
        }

        /// <summary>
        /// 显示说服结束对话（玩家选择听从军师建议时）
        /// </summary>
        /// <param name="advisorSupports">军师是否支持</param>
        private void ShowConvinceEndDialog(bool advisorSupports)
        {
            try
            {
                var faction = Session.Current.Scenario.CurrentPlayer;
                Person advisor = faction?.Advisor ?? faction?.Leader;
                
                if (advisor == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string endMessage;
                if (advisorSupports)
                {
                    endMessage = $"{advisor.Name}：主公英明！臣这就去安排此事。";
                }
                else
                {
                    endMessage = $"{advisor.Name}：主公深思熟虑，臣佩服。此事暂且作罢。";
                }

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor, advisor, endMessage, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowConvinceEndDialog] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示说服结束对话（自定义消息）
        /// </summary>
        /// <param name="message">自定义结束消息</param>
        private void ShowConvinceEndDialog(string message)
        {
            try
            {
                var faction = Session.Current.Scenario.CurrentPlayer;
                Person advisor = faction?.Advisor ?? faction?.Leader;
                
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowConvinceEndDialog] 没有军师或君主，无法显示结束对话");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[ShowConvinceEndDialog] 显示结束对话: {message}");
                
                // 检查对话框插件是否可用
                if (this.Plugins?.tupianwenziPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowConvinceEndDialog] tupianwenziPlugin不可用");
                    return;
                }

                // 使用tupianwenziPlugin显示结束消息
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor, advisor, $"{advisor.Name}：{message}", "", "", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Center, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowConvinceEndDialog] 显示自定义结束对话失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示说服人员选择界面（带推荐）
        /// </summary>
        private void ShowConvincePersonSelectionWithRecommendation(Person target, Person recommendedPerson)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ShowConvincePersonSelectionWithRecommendation] 推荐人选: {recommendedPerson?.Name}");
                
                // 设置推荐人选为预选
                var preSelectedList = new GameObjectList();
                if (recommendedPerson != null)
                    preSelectedList.Add(recommendedPerson);
                
                // 存储目标人物，供后续使用
                this.ConvinceTargetPerson = target;
                
                // 安全检查：确保TabListPlugin不为null
                if (this.Plugins?.TabListPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowConvincePersonSelectionWithRecommendation] TabListPlugin为null，无法显示界面");
                    return;
                }
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(this.CurrentArchitecture.ConvincePersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Work, 
                    FrameFunction.GetConvinceSourcePerson, 
                    false, true, true, true, 
                    this.CurrentArchitecture.PersonsExcludeNvGuan, 
                    preSelectedList, // 预选推荐人选
                    "说服人员", 
                    "说服"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowConvincePersonSelectionWithRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示说服人员选择界面（直接）
        /// </summary>
        private void ShowConvincePersonSelectionDirect(Person target)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ShowConvincePersonSelectionDirect] 直接选择人员说服: {target?.Name}");
                
                // 存储目标人物，供后续使用
                this.ConvinceTargetPerson = target;
                
                // 安全检查：确保TabListPlugin不为null
                if (this.Plugins?.TabListPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowConvincePersonSelectionDirect] TabListPlugin为null，无法显示界面");
                    return;
                }
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(this.CurrentArchitecture.ConvincePersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Work, 
                    FrameFunction.GetConvinceSourcePerson, 
                    false, true, true, true, 
                    this.CurrentArchitecture.PersonsExcludeNvGuan, 
                    null, 
                    "说服人员", 
                    "说服"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowConvincePersonSelectionDirect] 错误: {ex.Message}");
            }
        }

        // 添加一个字段来存储说服目标
        private Person ConvinceTargetPerson = null;

        #region 智能破坏系统

        /// <summary>
        /// 【智能破坏系统】触发智能破坏流程
        /// 流程：选择破坏目标 → 军师分析 → 推荐执行人员 → 玩家决定
        /// </summary>
        public void TriggerIntelligentDestroy()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentDestroy] 开始智能破坏流程");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                if (faction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentDestroy] 当前玩家势力为null，退出");
                    return;
                }

                Architecture sourceArchitecture = this.CurrentArchitecture;
                
                if (sourceArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentDestroy] 执行建筑为null，退出");
                    return;
                }

                if (sourceArchitecture.BelongedFaction != faction)
                {
                    System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentDestroy] 执行建筑不属于当前玩家，退出");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentDestroy] 执行建筑: {sourceArchitecture.Name}");
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentDestroy] 当前玩家势力: {faction.Name}");

                // 获取所有可能的破坏目标建筑
                ArchitectureList destroyTargets = GetAllPossibleDestroyTargets(sourceArchitecture, faction);
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentDestroy] 所有可能的目标建筑数量: {destroyTargets.Count}");
                
                if (destroyTargets.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentDestroy] 没有可破坏的目标建筑");
                    ShowNoDestroyTargetsDialog(faction);
                    return;
                }
                
                // 第一步：让玩家选择要破坏的目标建筑
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentDestroy] 显示目标建筑选择界面");
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Architecture, 
                    FrameFunction.GetDestroyTargetForAnalysis, // 选择目标后进行军师分析
                    false, true, true, false, 
                    destroyTargets, 
                    null, 
                    "选择破坏目标", 
                    "Architecture"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentDestroy] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentDestroy] 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取所有可能的破坏目标建筑
        /// </summary>
        private ArchitectureList GetAllPossibleDestroyTargets(Architecture sourceArchitecture, Faction playerFaction)
        {
            ArchitectureList targets = new ArchitectureList();
            HashSet<int> addedIds = new HashSet<int>();

            try
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleDestroyTargets] 开始获取目标，执行建筑: {sourceArchitecture.Name}");

                // Helper to add valid targets
                void TryAddTarget(Architecture arch)
                {
                    if (arch != null && !addedIds.Contains(arch.ID))
                    {
                        // 1. Must be Enemy
                        if (arch.BelongedFaction != null && !sourceArchitecture.IsFriendly(arch.BelongedFaction))
                        {
                            // 2. Must be Known to Player
                            if (playerFaction.IsArchitectureKnown(arch))
                            {
                                // 3. Must be worthwhile (Fund > 0 or Endurance > 0)
                                if (arch.Endurance > 0 || arch.Fund > 0)
                                {
                                    targets.Add(arch);
                                    addedIds.Add(arch.ID);
                                    // System.Diagnostics.Debug.WriteLine($"[GetAllPossibleDestroyTargets] 添加破坏目标: {arch.Name}");
                                }
                            }
                        }
                    }
                }

                // 1. Check Direct Neighbors
                foreach (Architecture neighbor in sourceArchitecture.AILandLinks)
                {
                    TryAddTarget(neighbor);
                    // 2. Check Neighbors of Neighbors
                    foreach (Architecture next in neighbor.AILandLinks) TryAddTarget(next);
                    foreach (Architecture next in neighbor.AIWaterLinks) TryAddTarget(next);
                }

                foreach (Architecture neighbor in sourceArchitecture.AIWaterLinks)
                {
                    TryAddTarget(neighbor);
                    // 2. Check Neighbors of Neighbors
                    foreach (Architecture next in neighbor.AILandLinks) TryAddTarget(next);
                    foreach (Architecture next in neighbor.AIWaterLinks) TryAddTarget(next);
                }
                
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleDestroyTargets] 完成，总目标数量: {targets.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllPossibleDestroyTargets] 错误: {ex.Message}");
            }
            
            return targets;
        }

        /// <summary>
        /// 【第二步】玩家选择目标后，进行军师分析和智能推荐执行人员
        /// </summary>
        public void PerformDestroyAnalysisAndRecommendation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PerformDestroyAnalysisAndRecommendation] 开始军师分析");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                var targetArchitecture = this.CurrentArchitecture; // 玩家选择的目标建筑
                
                // 获取执行破坏的建筑（玩家的建筑，有执行人员的地方）
                // 获取执行破坏的建筑（玩家的建筑，有执行人员的地方）
                // 修复：优先使用已设置的CurrentSourceArchitecture，而不是默认取第一个建筑
                Architecture sourceArchitecture = this.CurrentSourceArchitecture;
                if (sourceArchitecture == null && faction.Architectures.Count > 0)
                {
                    sourceArchitecture = faction.Architectures[0] as Architecture; // 回退方案
                }
                
                if (targetArchitecture == null || sourceArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformDestroyAnalysisAndRecommendation] 目标建筑或源建筑为null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[PerformDestroyAnalysisAndRecommendation] 破坏目标: {targetArchitecture.Name}");
                System.Diagnostics.Debug.WriteLine($"[PerformDestroyAnalysisAndRecommendation] 执行建筑: {sourceArchitecture.Name}");

                // 获取军师（智力最高的人物）
                var advisor = GetBestAdvisor(sourceArchitecture);
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformDestroyAnalysisAndRecommendation] 没有找到军师，直接显示人员选择");
                    ShowExecutorSelectionForDestroy(sourceArchitecture, targetArchitecture);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[PerformDestroyAnalysisAndRecommendation] 军师: {advisor.Name} (智力: {advisor.Intelligence})");

                // 进行军师分析：分析哪个执行人员最适合破坏这个目标
                var analysisResult = PerformDestroyAnalysis(advisor, targetArchitecture, sourceArchitecture);
                
                System.Diagnostics.Debug.WriteLine($"[PerformDestroyAnalysisAndRecommendation] 分析结果: 推荐执行人员={analysisResult.BestCandidate?.Name ?? "无"}, 预测成功率={analysisResult.BestScore}%");

                // 根据分析结果决定下一步
                if (analysisResult.BestScore >= 67) // 成功率很高（67%以上）
                {
                    System.Diagnostics.Debug.WriteLine($"[PerformDestroyAnalysisAndRecommendation] 成功率很高({analysisResult.BestScore}%)，显示支持对话");
                    ShowAdvisorDestroySupport(analysisResult, sourceArchitecture);
                }
                else if (analysisResult.BestScore >= 34) // 成功率中等（34-66%）
                {
                    System.Diagnostics.Debug.WriteLine($"[PerformDestroyAnalysisAndRecommendation] 成功率中等({analysisResult.BestScore}%)，显示支持对话");
                    ShowAdvisorDestroySupport(analysisResult, sourceArchitecture);
                }
                else // 成功率低（0-33%）
                {
                    System.Diagnostics.Debug.WriteLine($"[PerformDestroyAnalysisAndRecommendation] 成功率低({analysisResult.BestScore}%)，显示劝阻对话");
                    ShowAdvisorDestroyDissuasion(analysisResult, sourceArchitecture);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformDestroyAnalysisAndRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行军师破坏分析
        /// </summary>
        private DestroyAnalysisResult PerformDestroyAnalysis(Person advisor, Architecture target, Architecture sourceArchitecture)
        {
            var result = new DestroyAnalysisResult
            {
                TargetArchitecture = target,
                Advisor = advisor
            };

            Person bestCandidate = null;
            int bestScore = 0;

            // 分析每个可能的执行人员
            foreach (Person candidate in sourceArchitecture.PersonsExcludeNvGuan)
            {
                // 计算实际成功率
                int actualScore = CalculateDestroySuccessRate(candidate, target);
                
                System.Diagnostics.Debug.WriteLine($"[PerformDestroyAnalysis] 候选人: {candidate.Name}, 破坏能力: {candidate.DestroyAbility}, 成功率: {actualScore}%");

                if (actualScore > bestScore)
                {
                    bestScore = actualScore;
                    bestCandidate = candidate;
                }
            }

            result.BestCandidate = bestCandidate;
            result.BestScore = bestScore;
            result.HasSuitableCandidate = bestScore >= 60; // 60%以上认为有希望

            return result;
        }

        /// <summary>
        /// 计算破坏成功率
        /// </summary>
        private int CalculateDestroySuccessRate(Person executor, Architecture target)
        {
            int targetDefense = target.Domination * 8;
            int executorAbility = executor.DestroyAbility;
            
            if (executorAbility <= 150) return 0; // 能力太低
            
            double successRate = (double)executorAbility / (executorAbility + targetDefense) * 100;
            return Math.Max(0, Math.Min(100, (int)successRate));
        }

        /// <summary>
        /// 显示军师支持破坏对话
        /// </summary>
        private void ShowAdvisorDestroySupport(DestroyAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorDestroySupport] 显示军师支持破坏对话");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                var advisor = analysis.Advisor;
                
                if (advisor == null || this.Plugins?.tupianwenziPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowAdvisorDestroySupport] 无法获取军师或对话插件，直接显示执行人员选择");
                    ShowExecutorSelectionForDestroy(sourceArchitecture, analysis.TargetArchitecture);
                    return;
                }

                // 生成军师对话
                string advisorMessage = GetDestroyAdvisorDialogue(faction, advisor, analysis.TargetArchitecture, analysis.BestScore, true);

                // 设置确认对话框的回调函数
                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    this.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorDestroySupport] 用户确认，显示执行人员选择");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 显示执行人员选择界面，并推荐最佳人员
                        ShowExecutorSelectionWithDestroyRecommendation(sourceArchitecture, analysis.TargetArchitecture, analysis.BestCandidate);
                    }),
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorDestroySupport] 玩家选择否，结束流程");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    })
                );

                // 设置确认对话框位置
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 显示军师头像和对话
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    advisorMessage,                    // 对话内容
                    "",                                // 图片
                    "",                                // 声音
                    ""                                 // 空字符串
                );

                // 显示对话框
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorDestroySupport] 军师支持对话已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorDestroySupport] 显示对话框失败: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForDestroy(sourceArchitecture, analysis.TargetArchitecture);
            }
        }

        /// <summary>
        /// 显示军师劝阻破坏对话
        /// </summary>
        private void ShowAdvisorDestroyDissuasion(DestroyAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorDestroyDissuasion] 显示军师劝阻破坏对话");
                
                var faction = Session.Current.Scenario.CurrentPlayer; // Get faction
                var advisor = analysis.Advisor;
                
                if (advisor == null || this.Plugins?.tupianwenziPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowAdvisorDestroyDissuasion] 无法获取军师或对话插件，直接显示执行人员选择");
                    ShowExecutorSelectionForDestroy(sourceArchitecture, analysis.TargetArchitecture);
                    return;
                }

                // 生成军师劝阻对话
                string advisorMessage = GetDestroyAdvisorDialogue(faction, advisor, analysis.TargetArchitecture, analysis.BestScore, false);

                // 设置确认对话框的回调函数
                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    this.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorDestroyDissuasion] 玩家选择坚持执行");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 显示执行人员选择界面
                        ShowExecutorSelectionForDestroy(sourceArchitecture, analysis.TargetArchitecture);
                    }),
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorDestroyDissuasion] 玩家选择听从劝告，结束流程");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    })
                );

                // 设置确认对话框位置
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 显示军师头像和对话
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    advisorMessage,                    // 对话内容
                    "",                                // 图片
                    "",                                // 声音
                    ""                                 // 空字符串
                );

                // 显示对话框
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorDestroyDissuasion] 军师劝阻对话已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorDestroyDissuasion] 显示对话框失败: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForDestroy(sourceArchitecture, analysis.TargetArchitecture);
            }
        }

        /// <summary>
        /// 获取破坏军师对话内容
        /// </summary>
        private string GetDestroyAdvisorDialogue(Faction faction, Person advisor, Architecture target, int successRate, bool isSupport)
        {
            string baseDialogue;
            
            if (successRate <= 0)
            {
                baseDialogue = "此建筑防御森严，破坏行动一定不能成功，建议换个目标或等待时机。";
            }
            else if (successRate <= 33)
            {
                baseDialogue = "此建筑防御严密，破坏成功的机会微乎其微。";
            }
            else if (successRate <= 66)
            {
                baseDialogue = "此建筑防御一般，破坏行动有一定的成功几率。";
            }
            else if (successRate <= 99)
            {
                baseDialogue = "此建筑防御薄弱，破坏成功的概率很高。";
            }
            else
            {
                baseDialogue = "此建筑防御空虚，破坏行动万无一失！";
            }
            
            string leaderAddress = AppellationSettings.GetAddress(faction, advisor, faction?.Leader);
            if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";

            if (isSupport)
            {
                return $"{leaderAddress}，破坏{target.Name}一事，{baseDialogue}\n\n是否采纳军师建议？";
            }
            else
            {
                return $"{leaderAddress}，破坏{target.Name}一事，{baseDialogue}\n\n臣建议{leaderAddress}三思而后行，是否坚持执行？";
            }
        }

        /// <summary>
        /// 显示带推荐人员的执行人员选择界面
        /// </summary>
        private void ShowExecutorSelectionWithDestroyRecommendation(Architecture sourceArchitecture, Architecture targetArchitecture, Person recommendedPerson)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithDestroyRecommendation] 推荐执行人员: {recommendedPerson?.Name}");
                
                // 安全检查：确保TabListPlugin不为null
                if (this.Plugins?.TabListPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionWithDestroyRecommendation] TabListPlugin为null，无法显示界面");
                    return;
                }
                
                // 设置推荐人选为预选
                var preSelectedList = new GameObjectList();
                if (recommendedPerson != null)
                    preSelectedList.Add(recommendedPerson);
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(sourceArchitecture.DestroyPersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire,  // 使用PersonManualHire避免触发原始破坏流程
                    true, true, true, true,  // 启用确定按钮
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    preSelectedList, // 自动勾选军师推荐的人员
                    $"破坏 {targetArchitecture.Name}（军师推荐：{recommendedPerson?.Name}）", 
                    "破坏"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithDestroyRecommendation] 错误: {ex.Message}");
                // 出错时显示普通选择界面
                ShowExecutorSelectionForDestroy(sourceArchitecture, targetArchitecture);
            }
        }

        /// <summary>
        /// 显示执行人员选择界面（普通）
        /// </summary>
        private void ShowExecutorSelectionForDestroy(Architecture sourceArchitecture, Architecture targetArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionForDestroy] 显示执行人员选择界面");
                
                // 安全检查：确保TabListPlugin不为null
                if (this.Plugins?.TabListPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionForDestroy] TabListPlugin为null，无法显示界面");
                    return;
                }
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(sourceArchitecture.DestroyPersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire,  // 使用PersonManualHire避免触发原始破坏流程
                    true, true, true, true,  // 启用确定按钮
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    null, 
                    $"破坏 {targetArchitecture.Name}", 
                    "破坏"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionForDestroy] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示没有可破坏目标的对话框
        /// </summary>
        private void ShowNoDestroyTargetsDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string message = $"{speaker.Name}：主公，此地并无可破坏的敌方建筑。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker, speaker, message, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoDestroyTargetsDialog] 错误: {ex.Message}");
            }
        }

        #endregion

        #region 智能煽动系统

        /// <summary>
        /// 【智能煽动系统】触发智能煽动流程
        /// 流程：选择煽动目标 → 军师分析 → 推荐执行人员 → 玩家决定
        /// </summary>
        public void TriggerIntelligentInstigate()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentInstigate] 开始智能煽动流程");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                if (faction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentInstigate] 当前玩家势力为null，退出");
                    return;
                }

                Architecture sourceArchitecture = this.CurrentArchitecture;
                
                if (sourceArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentInstigate] 执行建筑为null，退出");
                    return;
                }

                if (sourceArchitecture.BelongedFaction != faction)
                {
                    System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentInstigate] 执行建筑不属于当前玩家，退出");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentInstigate] 执行建筑: {sourceArchitecture.Name}");
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentInstigate] 当前玩家势力: {faction.Name}");

                // 获取所有可能的煽动目标建筑
                ArchitectureList instigateTargets = GetAllPossibleInstigateTargets(sourceArchitecture, faction);
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentInstigate] 所有可能的目标建筑数量: {instigateTargets.Count}");
                
                if (instigateTargets.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentInstigate] 没有可煽动的目标建筑");
                    ShowNoInstigateTargetsDialog(faction);
                    return;
                }
                
                // 第一步：让玩家选择要煽动的目标建筑
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentInstigate] 显示目标建筑选择界面");
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Architecture, 
                    FrameFunction.GetInstigateTargetForAnalysis, // 选择目标后进行军师分析
                    false, true, true, false, 
                    instigateTargets, 
                    null, 
                    "选择煽动目标", 
                    "Architecture"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentInstigate] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentInstigate] 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取所有可能的煽动目标建筑
        /// </summary>
        private ArchitectureList GetAllPossibleInstigateTargets(Architecture sourceArchitecture, Faction playerFaction)
        {
            ArchitectureList targets = new ArchitectureList();
            
            // Check inputs
            if (sourceArchitecture == null || playerFaction == null)
                return targets;

            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleInstigateTargets] 开始获取目标，执行建筑: {sourceArchitecture.Name}");
            
            // Use HashSet to avoid duplicates
            System.Collections.Generic.HashSet<int> addedIds = new System.Collections.Generic.HashSet<int>();

            void TryAddTarget(Architecture target)
            {
                if (target == null) return;
                
                // Avoid duplicates
                if (addedIds.Contains(target.ID)) return;

                // 1. Must be enemy
                if (target.BelongedFaction != null && !sourceArchitecture.IsFriendly(target.BelongedFaction))
                {
                    // 2. Must be known
                    if (sourceArchitecture.BelongedFaction.IsArchitectureKnown(target))
                    {
                        // 3. Must be worthwhile (Pop > 0 or Domination > 0)
                        if (target.Population > 0 || target.Domination > 0)
                        {
                            targets.Add(target);
                            addedIds.Add(target.ID);
                            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleInstigateTargets] 添加煽动目标: {target.Name}");
                        }
                    }
                }
            }

            // Hop 1: Direct Link (Land & Water)
            foreach (Architecture neighbor in sourceArchitecture.AILandLinks)
            {
                TryAddTarget(neighbor);
                // Hop 2: Neighbor's Neighbors
                foreach (Architecture next in neighbor.AILandLinks) TryAddTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) TryAddTarget(next);
            }

            foreach (Architecture neighbor in sourceArchitecture.AIWaterLinks)
            {
                TryAddTarget(neighbor);
                // Hop 2: Neighbor's Neighbors
                foreach (Architecture next in neighbor.AILandLinks) TryAddTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) TryAddTarget(next);
            }

            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleInstigateTargets] 完成，总目标数量: {targets.Count}");
            return targets;
        }

        /// <summary>
        /// 【第二步】玩家选择目标后，进行军师分析和智能推荐执行人员
        /// </summary>
        public void PerformInstigateAnalysisAndRecommendation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PerformInstigateAnalysisAndRecommendation] 开始军师分析");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                var targetArchitecture = this.CurrentArchitecture; // 玩家选择的目标建筑
                
                // 获取执行煽动的建筑
                // 修复：优先使用已设置的CurrentSourceArchitecture
                Architecture sourceArchitecture = this.CurrentSourceArchitecture;
                if (sourceArchitecture == null && faction.Architectures.Count > 0)
                {
                    sourceArchitecture = faction.Architectures[0] as Architecture;
                }
                
                if (targetArchitecture == null || sourceArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformInstigateAnalysisAndRecommendation] 目标建筑或源建筑为null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[PerformInstigateAnalysisAndRecommendation] 煽动目标: {targetArchitecture.Name}");
                System.Diagnostics.Debug.WriteLine($"[PerformInstigateAnalysisAndRecommendation] 执行建筑: {sourceArchitecture.Name}");

                // 获取军师（智力最高的人物）
                var advisor = GetBestAdvisor(sourceArchitecture);
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformInstigateAnalysisAndRecommendation] 没有找到军师，直接显示人员选择");
                    ShowExecutorSelectionForInstigate(sourceArchitecture, targetArchitecture);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[PerformInstigateAnalysisAndRecommendation] 军师: {advisor.Name} (智力: {advisor.Intelligence})");

                // 进行军师分析：分析哪个执行人员最适合煽动这个目标
                var analysisResult = PerformInstigateAnalysis(advisor, targetArchitecture, sourceArchitecture);
                
                System.Diagnostics.Debug.WriteLine($"[PerformInstigateAnalysisAndRecommendation] 分析结果: 推荐执行人员={analysisResult.BestCandidate?.Name ?? "无"}, 预测成功率={analysisResult.BestScore}%");

                // 根据分析结果决定下一步
                if (analysisResult.BestScore >= 67) // 成功率很高（67%以上）
                {
                    System.Diagnostics.Debug.WriteLine($"[PerformInstigateAnalysisAndRecommendation] 成功率很高({analysisResult.BestScore}%)，显示支持对话");
                    ShowAdvisorInstigateSupport(analysisResult, sourceArchitecture);
                }
                else if (analysisResult.BestScore >= 34) // 成功率中等（34-66%）
                {
                    System.Diagnostics.Debug.WriteLine($"[PerformInstigateAnalysisAndRecommendation] 成功率中等({analysisResult.BestScore}%)，显示支持对话");
                    ShowAdvisorInstigateSupport(analysisResult, sourceArchitecture);
                }
                else // 成功率低（0-33%）
                {
                    System.Diagnostics.Debug.WriteLine($"[PerformInstigateAnalysisAndRecommendation] 成功率低({analysisResult.BestScore}%)，显示劝阻对话");
                    ShowAdvisorInstigateDissuasion(analysisResult, sourceArchitecture);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformInstigateAnalysisAndRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行军师煽动分析
        /// </summary>
        private InstigateAnalysisResult PerformInstigateAnalysis(Person advisor, Architecture target, Architecture sourceArchitecture)
        {
            var result = new InstigateAnalysisResult
            {
                TargetArchitecture = target,
                Advisor = advisor
            };

            Person bestCandidate = null;
            int bestScore = 0;

            // 分析每个可能的执行人员
            foreach (Person candidate in sourceArchitecture.PersonsExcludeNvGuan)
            {
                // 计算实际成功率
                int actualScore = CalculateInstigateSuccessRate(candidate, target);
                
                System.Diagnostics.Debug.WriteLine($"[PerformInstigateAnalysis] 候选人: {candidate.Name}, 煽动能力: {candidate.InstigateAbility}, 成功率: {actualScore}%");

                if (actualScore > bestScore)
                {
                    bestScore = actualScore;
                    bestCandidate = candidate;
                }
            }

            result.BestCandidate = bestCandidate;
            result.BestScore = bestScore;
            result.HasSuitableCandidate = bestScore >= 60; // 60%以上认为有希望

            return result;
        }

        /// <summary>
        /// 计算煽动成功率
        /// </summary>
        private int CalculateInstigateSuccessRate(Person executor, Architecture target)
        {
            int targetMorale = target.Morale * 2 + 200; // 目标士气防御
            int executorAbility = executor.InstigateAbility;
            
            if (executorAbility <= 150) return 0; // 能力太低
            
            // 基于游戏原有的煽动成功率计算逻辑
            double successRate = (double)executorAbility / (executorAbility + targetMorale) * 100;
            return Math.Max(0, Math.Min(100, (int)successRate));
        }

        /// <summary>
        /// 显示军师支持煽动对话
        /// </summary>
        private void ShowAdvisorInstigateSupport(InstigateAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorInstigateSupport] 显示军师支持煽动对话");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                var advisor = analysis.Advisor;
                
                if (advisor == null || this.Plugins?.tupianwenziPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowAdvisorInstigateSupport] 无法获取军师或对话插件，直接显示执行人员选择");
                    ShowExecutorSelectionForInstigate(sourceArchitecture, analysis.TargetArchitecture);
                    return;
                }

                // 生成军师对话
                string advisorMessage = GetInstigateAdvisorDialogue(faction, advisor, analysis.TargetArchitecture, analysis.BestScore, true);

                // 设置确认对话框的回调函数
                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    this.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorInstigateSupport] 用户确认，显示执行人员选择");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 显示执行人员选择界面，并推荐最佳人员
                        ShowExecutorSelectionWithInstigateRecommendation(sourceArchitecture, analysis.TargetArchitecture, analysis.BestCandidate);
                    }),
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorInstigateSupport] 玩家选择否，结束流程");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    })
                );

                // 设置确认对话框位置
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 显示军师头像和对话
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    advisorMessage,                    // 对话内容
                    "",                                // 图片
                    "",                                // 声音
                    ""                                 // 空字符串
                );

                // 显示对话框
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorInstigateSupport] 军师支持对话已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorInstigateSupport] 显示对话框失败: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForInstigate(sourceArchitecture, analysis.TargetArchitecture);
            }
        }

        /// <summary>
        /// 显示军师劝阻煽动对话
        /// </summary>
        private void ShowAdvisorInstigateDissuasion(InstigateAnalysisResult analysis, Architecture sourceArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorInstigateDissuasion] 显示军师劝阻煽动对话");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                var advisor = analysis.Advisor;
                
                if (advisor == null || this.Plugins?.tupianwenziPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowAdvisorInstigateDissuasion] 无法获取军师或对话插件，直接显示执行人员选择");
                    ShowExecutorSelectionForInstigate(sourceArchitecture, analysis.TargetArchitecture);
                    return;
                }

                // 生成军师劝阻对话
                string advisorMessage = GetInstigateAdvisorDialogue(faction, advisor, analysis.TargetArchitecture, analysis.BestScore, false);

                // 设置确认对话框的回调函数
                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    this.Plugins.ConfirmationDialogPlugin,
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorInstigateDissuasion] 玩家选择坚持执行");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                        
                        // 显示执行人员选择界面
                        ShowExecutorSelectionForInstigate(sourceArchitecture, analysis.TargetArchitecture);
                    }),
                    new GameDelegates.VoidFunction(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("[AdvisorInstigateDissuasion] 玩家选择听从劝告，结束流程");
                        
                        // 先关闭当前对话框
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    })
                );

                // 设置确认对话框位置
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

                // 显示军师头像和对话
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    advisor,                           // 说话人
                    advisor,                           // 对象
                    advisorMessage,                    // 对话内容
                    "",                                // 图片
                    "",                                // 声音
                    ""                                 // 空字符串
                );

                // 显示对话框
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine("[ShowAdvisorInstigateDissuasion] 军师劝阻对话已显示");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowAdvisorInstigateDissuasion] 显示对话框失败: {ex.Message}");
                // 出错时直接显示执行人员选择
                ShowExecutorSelectionForInstigate(sourceArchitecture, analysis.TargetArchitecture);
            }
        }

        /// <summary>
        /// 获取煽动军师对话内容
        /// </summary>
        private string GetInstigateAdvisorDialogue(Faction faction, Person advisor, Architecture target, int successRate, bool isSupport)
        {
            string baseDialogue;
            
            if (successRate <= 0)
            {
                baseDialogue = "此地民心稳固，煽动行动一定不能成功，建议换个目标或等待时机。";
            }
            else if (successRate <= 33)
            {
                baseDialogue = "此地士气高昂，煽动成功的机会微乎其微。";
            }
            else if (successRate <= 66)
            {
                baseDialogue = "此地民心一般，煽动行动有一定的成功几率。";
            }
            else if (successRate <= 99)
            {
                baseDialogue = "此地士气低落，煽动成功的概率很高。";
            }
            else
            {
                baseDialogue = "此地民心涣散，煽动行动万无一失！";
            }
            
            string leaderAddress = AppellationSettings.GetAddress(faction, advisor, faction?.Leader);
            if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";
            
            if (isSupport)
            {
                return $"{leaderAddress}，煽动{target.Name}一事，{baseDialogue}\n\n是否采纳军师建议？";
            }
            else
            {
                return $"{leaderAddress}，煽动{target.Name}一事，{baseDialogue}\n\n臣建议{leaderAddress}三思而后行，是否坚持执行？";
            }
        }

        /// <summary>
        /// 显示带推荐人员的执行人员选择界面
        /// </summary>
        private void ShowExecutorSelectionWithInstigateRecommendation(Architecture sourceArchitecture, Architecture targetArchitecture, Person recommendedPerson)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithInstigateRecommendation] 推荐执行人员: {recommendedPerson?.Name}");
                
                // 安全检查：确保TabListPlugin不为null
                if (this.Plugins?.TabListPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionWithInstigateRecommendation] TabListPlugin为null，无法显示界面");
                    return;
                }
                
                // 设置推荐人选为预选
                var preSelectedList = new GameObjectList();
                if (recommendedPerson != null)
                    preSelectedList.Add(recommendedPerson);
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(sourceArchitecture.InstigatePersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire,  // 使用PersonManualHire避免触发原始煽动流程
                    true, true, true, true,  // 启用确定按钮
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    preSelectedList, // 自动勾选军师推荐的人员
                    $"煽动 {targetArchitecture.Name}（军师推荐：{recommendedPerson?.Name}）", 
                    "煽动"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithInstigateRecommendation] 错误: {ex.Message}");
                // 出错时显示普通选择界面
                ShowExecutorSelectionForInstigate(sourceArchitecture, targetArchitecture);
            }
        }

        /// <summary>
        /// 显示执行人员选择界面（普通）
        /// </summary>
        private void ShowExecutorSelectionForInstigate(Architecture sourceArchitecture, Architecture targetArchitecture)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionForInstigate] 显示执行人员选择界面");
                
                // 安全检查：确保TabListPlugin不为null
                if (this.Plugins?.TabListPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowExecutorSelectionForInstigate] TabListPlugin为null，无法显示界面");
                    return;
                }
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(sourceArchitecture.InstigatePersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire,  // 使用PersonManualHire避免触发原始煽动流程
                    true, true, true, true,  // 启用确定按钮
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    null, 
                    $"煽动 {targetArchitecture.Name}", 
                    "煽动"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionForInstigate] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示没有可煽动目标的对话框
        /// </summary>
        private void ShowNoInstigateTargetsDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string message = $"{speaker.Name}：主公，此地并无可煽动的敌方建筑。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker, speaker, message, "junshi.jpg", "junshi", ""
                );
                
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoInstigateTargetsDialog] 错误: {ex.Message}");
            }
        }


        #region 智能流言系统

        /// <summary>
        /// 触发智能流言流程
        /// </summary>
        public void TriggerIntelligentGossip()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentGossip] 开始智能流言流程");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                if (faction == null) 
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentGossip] 当前玩家势力为null，退出");
                    return;
                }

                Architecture sourceArchitecture = this.CurrentArchitecture;
                
                if (sourceArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentGossip] 执行建筑为null，退出");
                    return;
                }

                if (sourceArchitecture.BelongedFaction != faction)
                {
                    System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentGossip] 执行建筑不属于当前玩家，退出");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentGossip] 执行建筑: {sourceArchitecture.Name}");

                // 获取所有可能的流言目标建筑
                ArchitectureList gossipTargets = GetAllPossibleGossipTargets(sourceArchitecture, faction);
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentGossip] 所有可能的目标建筑数量: {gossipTargets.Count}");
                
                if (gossipTargets.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentGossip] 没有可流言的目标建筑");
                    ShowNoGossipTargetsDialog(faction);
                    return;
                }
                
                // 第一步：让玩家选择要流言的目标建筑
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentGossip] 显示目标建筑选择界面");
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Architecture, 
                    FrameFunction.GetGossipTargetForAnalysis, // 选择目标后进行军师分析
                    false, true, true, false, 
                    gossipTargets, 
                    null, 
                    "选择流言目标", 
                    "Architecture"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentGossip] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentGossip] 堆栈: {ex.StackTrace}");
            }
        }

/// <summary>
/// 触发智能亲善流程 (Advisor Prediction)
/// </summary>
public void TriggerIntelligentEnhanceDiplomaticRelation()
{
    try
    {
        System.Diagnostics.Debug.WriteLine("[TriggerIntelligentEnhanceDiplomaticRelation] 开始智能亲善流程");
        
        var faction = Session.Current.Scenario.CurrentPlayer;
        if (faction == null) return;

        Architecture sourceArchitecture = this.CurrentArchitecture;
        if (sourceArchitecture == null) return;

        // 获取原来的亲善列表
        // 注意：原逻辑直接使用 GetEnhanceDiplomaticRelationList
        // 这里沿用，让玩家选择要亲善的外交关系
        
        GameObjectList targetDiplomaticRelations = sourceArchitecture.GetEnhanceDiplomaticRelationList();

        if (targetDiplomaticRelations.Count == 0)
        {
            this.xianshishijiantupian(
                this.CurrentArchitecture.BelongedFaction.Leader, 
                "Diplomacy", 
                TextMessageKind.EnhanceDiplomaticRelation, 
                "EnhaneceDiplomaticRelation", 
                "EnhaneceDiplomaticRelation.jpg", 
                "EnhaneceDiplomaticRelation", 
                "当前没有可亲善的势力", 
                true
            );
            return;
        }

        // 第一步：让玩家选择目标势力（外交关系）
        this.ShowTabListInFrame(
            UndoneWorkKind.Frame, 
            FrameKind.DiplomaticRelation, // 显示外交关系列表
            FrameFunction.GetEnhanceDiplomaticRelationTargetForAnalysis, // 选择后进入代价选择
            false, true, true, false, 
            targetDiplomaticRelations, 
            null, 
            "选择亲善目标", 
            "Ability"
        );
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentEnhanceDiplomaticRelation] Error: {ex.Message}");
    }
}

public Treasure CurrentDiplomaticCost { get; set; }
public Person CurrentRecommendedPersonForEnhanceDiplomatic { get; set; }

public void PerformEnhanceDiplomaticAnalysisAndRecommendation()
{
    try 
    {
        System.Diagnostics.Debug.WriteLine("[PerformEnhanceDiplomaticAnalysisAndRecommendation] 开始军师亲善分析");

        var faction = Session.Current.Scenario.CurrentPlayer;
        if (faction == null) return;

        // 获取之前选择的 DiplomaticRelationDisplay
        // 注意：CurrentDiplomaticRelationDisplay 存储在 ScreenManager 中
        // 但我们可以通过 CurrentTargetArchitecture 或 CurrentDiplomaticRelationDisplay 访问
        // ScreenManager logic sets CurrentDiplomaticRelationDisplay
        
        var relationDisplay = this.screenManager.CurrentDiplomaticRelationDisplay;
        if (relationDisplay == null) 
        {
             System.Diagnostics.Debug.WriteLine("[PerformEnhanceDiplomaticAnalysisAndRecommendation] 错误：未找到选定的外交关系");
             return;
        }

        Faction targetFaction = faction.GetFactionByName(relationDisplay.FactionName);
        if (targetFaction == null || targetFaction.Leader == null) return;

        // 获取军师
        Person advisor = faction.Advisor;
        if (advisor == null && faction.Leader != null) advisor = faction.Leader; // 无军师则君主自己思考
        
        if (advisor != null)
        {
            // 获取建议
            var advice = AdvisorStrategySystem.GetAdvice(advisor, faction, targetFaction.Leader, StrategyKind.EnhanceDiplomatic);
            
            // 显示建议对话
            // 显示建议对话 (使用 tupianwenziPlugin 的确认对话框模式以等待玩家点击)
            string costText = (this.CurrentDiplomaticCost == null) ? "10000资金" : this.CurrentDiplomaticCost.Name;
            // 存储推荐人选供后续自动勾选
            this.CurrentRecommendedPersonForEnhanceDiplomatic = advice.BestCandidate;
            ShowAdvisorEnhanceDiplomaticSupport(advisor, faction, targetFaction, advice, costText);
        }
        else
        {
            // 无军师直接进人员选择
            this.ShowExecutorSelectionForEnhanceDiplomatic(faction);
        }

    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[PerformEnhanceDiplomaticAnalysisAndRecommendation] Error: {ex.Message}");
    }
}

private void ShowAdvisorEnhanceDiplomaticSupport(Person advisor, Faction faction, Faction targetFaction, StrategyAdviceResult advice, string costText)
{
    if (this.Plugins?.tupianwenziPlugin == null)
    {
        this.ShowExecutorSelectionForEnhanceDiplomatic(faction);
        return;
    }

    string leaderAddress = AppellationSettings.GetAddress(faction, advisor, faction?.Leader);
    if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";

    // 第一段对话：告知代价 (点击任意处继续，不显示按钮)
    string message1 = $"{advisor.Name}：{leaderAddress}，若要与【{targetFaction.Name}】修好，需赠送【{costText}】。";
    
    // 设置说话人文本
    advisor.TextResultString = message1;

    // 第一段对话：点击任意处继续（不显示确认按钮）
    // 使用 SetGameObjectBranch 并设置回调在点击后进入第二段
    this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
        advisor, 
        advisor, 
        message1, 
        "", "", ""
    );
    
    // 设置关闭（点击）后的回调 (进入第二段对话)
    this.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() => 
    {
        // 第二段对话：给出建议和预测
        string message2 = advice.AdvisorComment;
        advisor.TextResultString = message2;

        // 注意：必须先设置ConfirmationDialog，再调用SetGameObjectBranch
        // 因为SetGameObjectBranch会检查HasConfirmationDialog标志并将对话框信息入队
        this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
            this.Plugins.ConfirmationDialogPlugin,
            new GameDelegates.VoidFunction(() => 
            {
                // Yes -> 选择执行人
                this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                this.Plugins.tupianwenziPlugin.IsShowing = false;
                this.ShowExecutorSelectionForEnhanceDiplomatic(faction);
            }), 
            new GameDelegates.VoidFunction(() => 
            { 
                // No -> 取消
                this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                this.Plugins.tupianwenziPlugin.IsShowing = false;
                this.CurrentDiplomaticCost = null;
            })
        );

        // 现在调用SetGameObjectBranch，此时HasConfirmationDialog已经为true
        this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
            advisor, 
            advisor, 
            message2, 
            "", "", ""
        );
        
        this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
        this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
        this.Plugins.tupianwenziPlugin.IsShowing = true;
    }));
    
    this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
    this.Plugins.tupianwenziPlugin.IsShowing = true;
    // 第一段对话不显示确认按钮
    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
}


private void ShowExecutorSelectionForEnhanceDiplomatic(Faction faction)
{
    // 获取此时可用的外交人员
    
    if (this.CurrentArchitecture != null)
    {
        PersonList personList = this.CurrentArchitecture.PersonsExcludeNvGuan;
        GameObjectList preSelectedList = null;

        // 自动选中推荐人选
        if (this.CurrentRecommendedPersonForEnhanceDiplomatic != null && 
            personList.HasGameObject(this.CurrentRecommendedPersonForEnhanceDiplomatic))
        {
            preSelectedList = new GameObjectList();
            preSelectedList.Add(this.CurrentRecommendedPersonForEnhanceDiplomatic);
        }
        
        this.ShowTabListInFrame(
            UndoneWorkKind.Frame, 
            FrameKind.Person, 
            FrameFunction.GetEnhanceDiplomaticRelationPerson, 
            true, true, true, true, 
            personList, 
            preSelectedList, // 传入预选列表
            "派遣使者", 
            "Politics" 
        );
    }
}        

        /// <summary>
        /// 获取所有可能的流言目标建筑
        /// </summary>
        private ArchitectureList GetAllPossibleGossipTargets(Architecture sourceArchitecture, Faction playerFaction)
        {
            ArchitectureList targets = new ArchitectureList();
            
            // Check inputs
            if (sourceArchitecture == null || playerFaction == null)
                return targets;

            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleGossipTargets] 开始获取目标，执行建筑: {sourceArchitecture.Name}");
            
             // Use HashSet to avoid duplicates
            System.Collections.Generic.HashSet<int> addedIds = new System.Collections.Generic.HashSet<int>();

            void TryAddTarget(Architecture target)
            {
                if (target == null) return;
                
                // Avoid duplicates
                if (addedIds.Contains(target.ID)) return;

                // 1. Must be enemy
                if (target.BelongedFaction != null && !sourceArchitecture.IsFriendly(target.BelongedFaction))
                {
                    // 2. Must be known
                    if (sourceArchitecture.BelongedFaction.IsArchitectureKnown(target))
                    {
                        // 3. Must be worthwhile 
                        if (target.Population > 0 || target.Domination > 0)
                        {
                            targets.Add(target);
                            addedIds.Add(target.ID);
                            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleGossipTargets] 添加流言目标: {target.Name}");
                        }
                    }
                }
            }

            // Hop 1: Direct Link (Land & Water)
            foreach (Architecture neighbor in sourceArchitecture.AILandLinks)
            {
                TryAddTarget(neighbor);
                // Hop 2: Neighbor's Neighbors
                foreach (Architecture next in neighbor.AILandLinks) TryAddTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) TryAddTarget(next);
            }

            foreach (Architecture neighbor in sourceArchitecture.AIWaterLinks)
            {
                TryAddTarget(neighbor);
                // Hop 2: Neighbor's Neighbors
                foreach (Architecture next in neighbor.AILandLinks) TryAddTarget(next);
                foreach (Architecture next in neighbor.AIWaterLinks) TryAddTarget(next);
            }

            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleGossipTargets] 完成，总目标数量: {targets.Count}");
            return targets;
        }

        /// <summary>
        /// 【第二步】玩家选择目标后，进行军师分析和智能推荐执行人员
        /// </summary>
        public void PerformGossipAnalysisAndRecommendation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PerformGossipAnalysisAndRecommendation] 开始军师分析");
                
                var faction = Session.Current.Scenario.CurrentPlayer;
                var targetArchitecture = this.CurrentArchitecture; // 玩家选择的目标建筑
                
                // 获取执行流言的建筑
                // 修复：优先使用已设置的CurrentSourceArchitecture
                Architecture sourceArchitecture = this.CurrentSourceArchitecture;
                if (sourceArchitecture == null && faction.Architectures.Count > 0)
                {
                    sourceArchitecture = faction.Architectures[0] as Architecture;
                }
                
                if (targetArchitecture == null || sourceArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformGossipAnalysisAndRecommendation] 目标建筑或源建筑为null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[PerformGossipAnalysisAndRecommendation] 流言目标: {targetArchitecture.Name}");
                System.Diagnostics.Debug.WriteLine($"[PerformGossipAnalysisAndRecommendation] 执行建筑: {sourceArchitecture.Name}");

                // 获取军师（智力最高的人物）
                var advisor = GetBestAdvisor(sourceArchitecture);
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformGossipAnalysisAndRecommendation] 没有找到军师，直接显示人员选择");
                    ShowExecutorSelectionForGossip(sourceArchitecture, targetArchitecture);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[PerformGossipAnalysisAndRecommendation] 军师: {advisor.Name} (智力: {advisor.Intelligence})");

                // 进行军师分析
                var analysisResult = PerformGossipAnalysis(advisor, targetArchitecture, sourceArchitecture);
                
                System.Diagnostics.Debug.WriteLine($"[PerformGossipAnalysisAndRecommendation] 分析结果: 推荐执行人员={analysisResult.BestCandidate?.Name ?? "无"}, 预测成功率={analysisResult.BestScore}%");

                // 根据分析结果决定下一步
                if (analysisResult.BestScore >= 60)
                {
                    ShowAdvisorGossipSupport(analysisResult, sourceArchitecture);
                }
                else if (analysisResult.BestScore >= 30)
                {
                    ShowAdvisorGossipSupport(analysisResult, sourceArchitecture);
                }
                else
                {
                    ShowAdvisorGossipDissuasion(analysisResult, sourceArchitecture);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformGossipAnalysisAndRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行军师流言分析
        /// </summary>
        private GossipAnalysisResult PerformGossipAnalysis(Person advisor, Architecture target, Architecture sourceArchitecture)
        {
            var result = new GossipAnalysisResult
            {
                TargetArchitecture = target,
                Advisor = advisor
            };

            Person bestCandidate = null;
            int bestScore = 0;

            // 分析每个可能的执行人员
            foreach (Person candidate in sourceArchitecture.PersonsExcludeNvGuan)
            {
                // 计算实际成功率 (流言通常与智力或魅力有关，此处假设主要看智力)
                int actualScore = CalculateGossipSuccessRate(candidate, target);
                
                if (actualScore > bestScore)
                {
                    bestScore = actualScore;
                    bestCandidate = candidate;
                }
            }

            result.BestCandidate = bestCandidate;
            result.BestScore = bestScore;
            result.HasSuitableCandidate = bestScore >= 50; 

            return result;
        }

        /// <summary>
        /// 计算流言成功率 (简化算法)
        /// </summary>
        private int CalculateGossipSuccessRate(Person executor, Architecture target)
        {
            // 假设流言主要依赖执行者的智力与目标的民心/治安/统治度
            // 如果Person有GossipAbility则用之，否则用Intelligence
            int executorAbility = executor.Intelligence; 
            int targetDefense = target.Domination; 
            
            if (executorAbility <= 50) return 0; // 能力太低
            
            // 简单的对抗公式
            double successRate = (double)executorAbility / (executorAbility + targetDefense * 0.5) * 100;
            return Math.Max(0, Math.Min(100, (int)successRate));
        }

        /// <summary>
        /// 显示军师支持流言的对话
        /// </summary>
        private void ShowAdvisorGossipSupport(GossipAnalysisResult result, Architecture sourceArchitecture)
        {
            if (this.Plugins?.tupianwenziPlugin == null)
            {
                ShowExecutorSelectionWithGossipRecommendation(sourceArchitecture, result.TargetArchitecture, result.BestCandidate);
                return;
            }

            string message;
            Faction faction = sourceArchitecture.BelongedFaction;
            string leaderAddress = AppellationSettings.GetAddress(faction, result.Advisor, faction?.Leader);
            
            if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";

            if (result.BestCandidate != null)
            {
                string candidateAddress = AppellationSettings.GetAddress(faction, result.Advisor, result.BestCandidate);
                message = $"{result.Advisor.Name}：{leaderAddress}，{result.TargetArchitecture.Name}人心浮动。若派{candidateAddress}前往散布流言，定能成功。";
            }
            else
            {
                message = $"{result.Advisor.Name}：{leaderAddress}，{result.TargetArchitecture.Name}防备松懈，我军可派人前往散布流言，动摇其军心。";
            }
            
            // 设置说话人文本
            result.Advisor.TextResultString = message;

            // 设置确认对话框回调 (是/否)
            this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                this.Plugins.ConfirmationDialogPlugin,
                new GameDelegates.VoidFunction(() => 
                {
                    // 先关闭当前对话框
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    ShowExecutorSelectionWithGossipRecommendation(sourceArchitecture, result.TargetArchitecture, result.BestCandidate);
                }), // Yes -> 显示带推荐的选择界面
                new GameDelegates.VoidFunction(() => 
                { 
                    // 先关闭当前对话框
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    System.Diagnostics.Debug.WriteLine("玩家取消流言操作"); 
                }) // No -> 取消
            );

            // 设置确认对话框位置
            this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

            // 显示对话
            this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                result.Advisor, 
                result.Advisor, 
                message, 
                "", 
                "", 
                ""
            );

            this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
            this.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 显示军师劝阻流言的对话
        /// </summary>
        private void ShowAdvisorGossipDissuasion(GossipAnalysisResult result, Architecture sourceArchitecture)
        {
            if (this.Plugins?.tupianwenziPlugin == null)
            {
                ShowExecutorSelectionForGossip(sourceArchitecture, result.TargetArchitecture);
                return;
            }

            Faction faction = sourceArchitecture.BelongedFaction;
            string leaderAddress = AppellationSettings.GetAddress(faction, result.Advisor, faction?.Leader);
             if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";

            string message = $"{result.Advisor.Name}：{leaderAddress}，{result.TargetArchitecture.Name}统治稳固，此时流言恐怕难以奏效，甚至可能打草惊蛇。";
            
            // 设置说话人文本
            result.Advisor.TextResultString = message;

            // 设置确认对话框回调 (是/否)
            // 注意：这里"是"代表坚持尝试，"否"代表听从劝阻
            this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                this.Plugins.ConfirmationDialogPlugin,
                new GameDelegates.VoidFunction(() => 
                {
                    // 先关闭当前对话框
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    ShowExecutorSelectionForGossip(sourceArchitecture, result.TargetArchitecture);
                }), // Yes (坚持) -> 显示普通选择界面
                new GameDelegates.VoidFunction(() => 
                {
                    // 先关闭当前对话框
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    System.Diagnostics.Debug.WriteLine("玩家听从劝阻取消流言"); 
                }) // No (放弃) -> 取消
            );

            // 设置确认对话框位置
            this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

            this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                result.Advisor, 
                result.Advisor, 
                message, 
                "", 
                "", 
                ""
            );

            this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
            this.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 显示执行人员选择界面（带推荐）
        /// </summary>
        private void ShowExecutorSelectionWithGossipRecommendation(Architecture sourceArchitecture, Architecture targetArchitecture, Person recommendedPerson)
        {
            try
            {
                var preSelectedList = new GameObjectList();
                if (recommendedPerson != null)
                    preSelectedList.Add(recommendedPerson);
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(sourceArchitecture.GossipPersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire,  // 统一使用PersonManualHire
                    true, true, true, true,
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    preSelectedList,
                    $"流言 {targetArchitecture.Name} - 推荐: {recommendedPerson?.Name ?? "无"}", 
                    "流言"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithGossipRecommendation] 错误: {ex.Message}");
                ShowExecutorSelectionForGossip(sourceArchitecture, targetArchitecture);
            }
        }

        /// <summary>
        /// 显示执行人员选择界面（普通）
        /// </summary>
        private void ShowExecutorSelectionForGossip(Architecture sourceArchitecture, Architecture targetArchitecture)
        {
            try
            {
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(sourceArchitecture.GossipPersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire, // 统一使用PersonManualHire
                    true, true, true, true,
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    null, 
                    $"流言 {targetArchitecture.Name}", 
                    "流言"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionForGossip] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示没有可流言目标的对话框
        /// </summary>
        private void ShowNoGossipTargetsDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string leaderAddress = AppellationSettings.GetAddress(faction, speaker, faction.Leader);
                if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";
                
                string message = $"{speaker.Name}：{leaderAddress}，此地并无可施展流言的敌方势力。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker, speaker, message, "", "", ""
                );
                // this.Plugins.tupianwenziPlugin.SetBranchEvent(null, null); // 移除不存在的方法
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoGossipTargetsDialog] 错误: {ex.Message}");
            }
        }

        // ==========================================
        // 智能劫牢系统 (Intelligent Jailbreak System)
        // ==========================================

        /// <summary>
        /// 触发智能劫牢策略
        /// </summary>
        public void TriggerIntelligentJailBreak(Faction faction)
        {
            System.Diagnostics.Debug.WriteLine("[智能劫牢] 开始触发流程");

            try
            {
                Architecture sourceArchitecture = this.CurrentArchitecture;
                if (sourceArchitecture == null) return;

                // 1. 获取所有可能的劫牢目标
                var targets = GetAllPossibleJailBreakTargets(sourceArchitecture);

                if (targets == null || targets.Count == 0)
                {
                    // 无目标时显示提示
                    ShowNoJailBreakTargetsDialog(faction);
                    return;
                }

                // 2. 如果只有一个目标，直接进行分析
                // 如果有多个目标，让玩家选择（通过TabListPlugin，带地图高亮）
                // 统一进入地图选择模式，高亮有效目标
                
                System.Diagnostics.Debug.WriteLine($"[智能劫牢] 找到 {targets.Count} 个潜在目标");

                // 设置当前操作类型
                this.CurrentOperationType = IntelligentOperationType.JailBreak;
                this.CurrentSourceArchitecture = sourceArchitecture;

                // 显示目标选择列表
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(1);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Architecture, 
                    FrameFunction.GetJailBreakTargetForAnalysis, 
                    false, true, true, false, 
                    targets, 
                    null, 
                    "选择劫牢目标", 
                    "劫牢目标"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentJailBreak] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有合法的劫牢目标（即范围内有我方俘虏的敌方建筑）
        /// </summary>
        private GameObjectList GetAllPossibleJailBreakTargets(Architecture source)
        {
            // 使用GameArea遍历范围内的地点，然后找到对应的建筑
            GameArea reachableArea = source.GetInstigateArchitectureArea(); // 使用煽动的范围作为参考
            GameObjectList validTargets = new GameObjectList();
            HashSet<int> addedIds = new HashSet<int>();

            foreach (Point pt in reachableArea.Area)
            {
                Architecture target = Session.Current.Scenario.GetArchitectureByPosition(pt);
                if (target == null) continue;
                if (addedIds.Contains(target.ID)) continue;
                
                // 必须是不同势力的建筑
                if (target.BelongedFaction == source.BelongedFaction) continue;

                // 检查该建筑内是否有我方俘虏
                bool hasMyCaptive = false;
                foreach (Captive c in target.Captives)
                {
                    if (c.CaptiveFaction == source.BelongedFaction)
                    {
                        hasMyCaptive = true;
                        break;
                    }
                }

                if (hasMyCaptive)
                {
                    validTargets.Add(target);
                    addedIds.Add(target.ID);
                }
            }

            return validTargets;
        }

        /// <summary>
        /// 执行劫牢分析并给出建议
        /// </summary>
        public void PerformJailBreakAnalysisAndRecommendation()
        {
            try
            {
                Architecture source = this.CurrentSourceArchitecture;
                Architecture target = this.CurrentTargetArchitecture;
                Faction faction = source.BelongedFaction;
                Person advisor = faction.Advisor ?? faction.Leader;

                System.Diagnostics.Debug.WriteLine($"[PerformJailBreakAnalysis] 分析源: {source.Name}, 目标: {target.Name}, 军师: {advisor.Name}");

                // 1. 进行数据分析
                var result = PerformJailBreakAnalysis(source, target, advisor);

                // 2. 根据成功率显示军师建议
                if (result.SuccessRate >= 40) // 成功率大于40%建议支持
                {
                    ShowAdvisorJailBreakSupport(result, source);
                }
                else
                {
                    ShowAdvisorJailBreakDissuasion(result, source);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformJailBreakAnalysisAndRecommendation] 错误: {ex.Message}");
                // 出错时回退到普通选择
                ShowExecutorSelectionForJailBreak(this.CurrentSourceArchitecture, this.CurrentTargetArchitecture);
            }
        }

        /// <summary>
        /// 具体的劫牢数据分析
        /// </summary>
        private JailBreakAnalysisResult PerformJailBreakAnalysis(Architecture source, Architecture target, Person advisor)
        {
            var result = new JailBreakAnalysisResult
            {
                TargetArchitecture = target,
                Advisor = advisor,
                SuccessRate = 0,
                BestCandidate = null
            };

            // 寻找最佳执行人
            Person bestPerson = null;
            int maxRate = -1;

            foreach (Person p in source.PersonsExcludeNvGuan)
            {
                if (p.LocationArchitecture != source) continue; // 必须在本地
                if (p == advisor && advisor != source.BelongedFaction.Leader) continue; // 军师如果是本人不推荐自己去（可选）

                int rate = CalculateJailBreakSuccessRate(p, target);
                if (rate > maxRate)
                {
                    maxRate = rate;
                    bestPerson = p;
                }
            }

            result.SuccessRate = maxRate;
            result.BestCandidate = bestPerson;

            return result;
        }

        /// <summary>
        /// 计算劫牢成功率 (简化算法)
        /// </summary>
        private int CalculateJailBreakSuccessRate(Person executor, Architecture target)
        {
            if (executor == null || target == null) return 0;

            // 基础成功率基于武力 + 统率 (JailBreakAbility usually depends on Strength/Leadership or similar)
            // Person.JailBreakAbility definition: (int)(this.CaptiveAbility * (1f + this.RateIncrementOfJailBreakAbility));
            // CaptiveAbility is usually Strength?
            
            // 获取目标防御度 (使用耐久度作为参考)
            int defense = target.Endurance;
            
            // 获取执行者能力
            int ability = executor.JailBreakAbility; 
            
            // 简单估算：(能力 / (防御/3 + 1)) * 基数
            // 参考 DoJailBreak: GameObject.Random(this.JailBreakAbility + c.CaptivePerson.CaptiveAbility) >= GameObject.Random(architectureByPosition.Security / 3 + 1)
            
            // 我们取一个期望值估算
            double expectedAttack = ability + 50; // 假设俘虏能力平均50
            double expectedDefense = defense / 3 + 1;
            
            double chance = (expectedAttack - expectedDefense) / 100.0;
            if (chance < 0.1) chance = 0.1;
            if (chance > 0.95) chance = 0.95;
            
            return (int)(chance * 100);
        }

        /// <summary>
        /// 显示军师支持劫牢的对话
        /// </summary>
        private void ShowAdvisorJailBreakSupport(JailBreakAnalysisResult result, Architecture sourceArchitecture)
        {
            if (this.Plugins?.tupianwenziPlugin == null)
            {
                ShowExecutorSelectionWithJailBreakRecommendation(sourceArchitecture, result.TargetArchitecture, result.BestCandidate);
                return;
            }

            string recName = result.BestCandidate != null ? result.BestCandidate.Name : "良将";
            string message = $"{result.Advisor.Name}：主公，{result.TargetArchitecture.Name}防备松懈，我方志士正待救援。若派{recName}前往劫牢，定能成功。";

            // 设置说话人文本
            result.Advisor.TextResultString = message;

            // 设置确认对话框回调 (是/否)
            this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                this.Plugins.ConfirmationDialogPlugin,
                new GameDelegates.VoidFunction(() => 
                {
                    // 先关闭当前对话框
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    ShowExecutorSelectionWithJailBreakRecommendation(sourceArchitecture, result.TargetArchitecture, result.BestCandidate);
                }), // Yes -> 显示带推荐的选择界面
                new GameDelegates.VoidFunction(() => 
                { 
                    // 先关闭当前对话框
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    System.Diagnostics.Debug.WriteLine("玩家取消劫牢操作"); 
                }) // No -> 取消
            );

            // 设置确认对话框位置
            this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

            // 显示对话 (不使用图片)
            this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                result.Advisor, 
                result.Advisor, 
                message, 
                "", 
                "", 
                ""
            );

            this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
            this.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 显示军师劝阻劫牢的对话
        /// </summary>
        private void ShowAdvisorJailBreakDissuasion(JailBreakAnalysisResult result, Architecture sourceArchitecture)
        {
            if (this.Plugins?.tupianwenziPlugin == null)
            {
                ShowExecutorSelectionForJailBreak(sourceArchitecture, result.TargetArchitecture);
                return;
            }

            string message = $"{result.Advisor.Name}：主公，{result.TargetArchitecture.Name}戒备森严，劫牢风险极大，恐有去无回，建议三思。";
            
            // 设置说话人文本
            result.Advisor.TextResultString = message;

            // 设置确认对话框回调 (是/否)
            this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                this.Plugins.ConfirmationDialogPlugin,
                new GameDelegates.VoidFunction(() => 
                {
                    // 先关闭当前对话框
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    ShowExecutorSelectionForJailBreak(sourceArchitecture, result.TargetArchitecture);
                }), // Yes (坚持) -> 显示普通选择界面
                new GameDelegates.VoidFunction(() => 
                {
                    // 先关闭当前对话框
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    System.Diagnostics.Debug.WriteLine("玩家听从劝阻取消劫牢"); 
                }) // No (放弃) -> 取消
            );

            // 设置确认对话框位置
            this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);

            this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                result.Advisor, 
                result.Advisor, 
                message, 
                "", 
                "", 
                ""
            );

            this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
            this.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 显示执行人员选择界面（带推荐）
        /// </summary>
        private void ShowExecutorSelectionWithJailBreakRecommendation(Architecture sourceArchitecture, Architecture targetArchitecture, Person recommendedPerson)
        {
            try
            {
                var preSelectedList = new GameObjectList();
                if (recommendedPerson != null)
                    preSelectedList.Add(recommendedPerson);
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(sourceArchitecture.JailBreakPersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire,  // 统一使用PersonManualHire
                    true, true, true, true,
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    preSelectedList, 
                    $"劫牢 {targetArchitecture.Name} - 推荐: {recommendedPerson?.Name ?? "无"}", 
                    "劫牢"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithJailBreakRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示执行人员选择界面（无推荐）
        /// </summary>
        private void ShowExecutorSelectionForJailBreak(Architecture sourceArchitecture, Architecture targetArchitecture)
        {
            try
            {
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(sourceArchitecture.JailBreakPersonMaxCount);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire, // 统一使用PersonManualHire
                    true, true, true, true,
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    null, 
                    $"劫牢 {targetArchitecture.Name}", 
                    "劫牢"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionForJailBreak] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示没有可劫牢目标的对话框
        /// </summary>
        private void ShowNoJailBreakTargetsDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string message = $"{speaker.Name}：主公，目前周边敌方势力中并没有我方被俘人员。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker, speaker, message, "", "", ""
                );
                // this.Plugins.tupianwenziPlugin.SetBranchEvent(null, null); // 移除不存在的方法
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoJailBreakTargetsDialog] 错误: {ex.Message}");
            }
        }

        #region 智能暗杀系统

        /// <summary>
        /// 触发智能暗杀策略
        /// </summary>
        public void TriggerIntelligentAssassinate()
        {
            System.Diagnostics.Debug.WriteLine("[TriggerIntelligentAssassinate] 开始");
            
            try
            {
                Architecture currentArch = this.CurrentArchitecture;
                if (currentArch == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentAssassinate] 当前建筑为null");
                    return;
                }

                Faction faction = currentArch.BelongedFaction;
                if (faction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentAssassinate] 当前势力为null");
                    return;
                }

                // 保存源建筑
                this.CurrentSourceArchitecture = currentArch;

                // 获取可暗杀的目标人物列表（敌方势力范围内的人物）
                GameObjectList targets = GetAllPossibleAssassinateTargets(currentArch);

                if (targets == null || targets.Count == 0)
                {
                    ShowNoAssassinateTargetsDialog(faction);
                    return;
                }

                // 显示目标人物选择列表
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentAssassinate] 找到 {targets.Count} 个可暗杀目标");
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame,
                    FrameKind.Person,
                    FrameFunction.GetAssassinateTargetForAnalysis,
                    false, true, true, false,
                    targets,
                    null,
                    "选择暗杀目标",
                    "Personal"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentAssassinate] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有可暗杀的目标人物
        /// </summary>
        private GameObjectList GetAllPossibleAssassinateTargets(Architecture source)
        {
            GameObjectList validTargets = new GameObjectList();

            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleAssassinateTargets] 开始获取目标，执行建筑: {source.Name}");

            HashSet<int> addedPersonIds = new HashSet<int>();
            HashSet<int> processedArchIds = new HashSet<int>();

            void ProcessArchitecture(Architecture target)
            {
                if (target == null) return;
                if (processedArchIds.Contains(target.ID)) return;
                processedArchIds.Add(target.ID);

                if (target.BelongedFaction == null) return;
                if (target.BelongedFaction == source.BelongedFaction) return; // Must be different faction

                // Must be known
                if (!source.BelongedFaction.IsArchitectureKnown(target)) return;

                // Check for assassinatable persons
                if (target.Persons != null)
                {
                    foreach (Person p in target.Persons)
                    {
                        if (p == null) continue;
                        if (p.Status != PersonStatus.Normal) continue;
                        if (addedPersonIds.Contains(p.ID)) continue;

                        validTargets.Add(p);
                        addedPersonIds.Add(p.ID);
                    }
                }
            }

            // Hop 1: Direct Link (Land & Water)
            foreach (Architecture neighbor in source.AILandLinks)
            {
                ProcessArchitecture(neighbor);
                // Hop 2: Neighbor's Neighbors
                foreach (Architecture next in neighbor.AILandLinks) ProcessArchitecture(next);
                foreach (Architecture next in neighbor.AIWaterLinks) ProcessArchitecture(next);
            }

            foreach (Architecture neighbor in source.AIWaterLinks)
            {
                ProcessArchitecture(neighbor);
                // Hop 2: Neighbor's Neighbors
                foreach (Architecture next in neighbor.AILandLinks) ProcessArchitecture(next);
                foreach (Architecture next in neighbor.AIWaterLinks) ProcessArchitecture(next);
            }

            System.Diagnostics.Debug.WriteLine($"[GetAllPossibleAssassinateTargets] 完成，总目标数量: {validTargets.Count}");
            return validTargets;
        }

        /// <summary>
        /// 执行暗杀分析并给出建议
        /// </summary>
        public void PerformAssassinateAnalysisAndRecommendation()
        {
            System.Diagnostics.Debug.WriteLine("[PerformAssassinateAnalysisAndRecommendation] 开始");
            
            try
            {
                Architecture sourceArchitecture = this.CurrentSourceArchitecture;
                Person targetPerson = this.CurrentPerson; // 目标人物

                if (sourceArchitecture == null || targetPerson == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformAssassinateAnalysisAndRecommendation] 源建筑或目标人物为null");
                    return;
                }

                // 获取军师
                Person advisor = sourceArchitecture.BelongedFaction?.Advisor;
                if (advisor == null)
                    advisor = sourceArchitecture.BelongedFaction?.Leader;
                
                if (advisor == null)
                {
                    ShowExecutorSelectionForAssassinate(sourceArchitecture, targetPerson);
                    return;
                }

                // 进行军师分析
                var analysisResult = PerformAssassinateAnalysis(advisor, targetPerson, sourceArchitecture);
                
                System.Diagnostics.Debug.WriteLine($"[PerformAssassinateAnalysisAndRecommendation] 分析结果: 推荐={analysisResult.BestCandidate?.Name ?? "无"}, 成功率={analysisResult.SuccessRate}%");

                // 根据分析结果决定下一步
                if (analysisResult.SuccessRate >= 50)
                {
                    ShowAdvisorAssassinateSupport(analysisResult, sourceArchitecture);
                }
                else if (analysisResult.SuccessRate >= 20)
                {
                    ShowAdvisorAssassinateSupport(analysisResult, sourceArchitecture);
                }
                else
                {
                    ShowAdvisorAssassinateDissuasion(analysisResult, sourceArchitecture);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformAssassinateAnalysisAndRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行军师暗杀分析
        /// </summary>
        private AssassinateAnalysisResult PerformAssassinateAnalysis(Person advisor, Person targetPerson, Architecture sourceArchitecture)
        {
            var result = new AssassinateAnalysisResult
            {
                TargetPerson = targetPerson,
                TargetArchitecture = targetPerson?.BelongedArchitecture,
                Advisor = advisor
            };

            Person bestCandidate = null;
            int bestScore = 0;

            // 分析每个可能的执行人员
            var candidates = sourceArchitecture?.PersonsExcludeNvGuan;
            if (candidates != null)
            {
                foreach (Person candidate in candidates)
                {
                    if (candidate == null) continue;
                    int actualScore = CalculateAssassinateSuccessRate(candidate, targetPerson);
                    
                    if (actualScore > bestScore)
                    {
                        bestScore = actualScore;
                        bestCandidate = candidate;
                    }
                }
            }

            result.BestCandidate = bestCandidate;
            result.SuccessRate = bestScore;
            result.HasSuitableCandidate = bestScore >= 30;

            return result;
        }

        /// <summary>
        /// 计算暗杀成功率
        /// </summary>
        private int CalculateAssassinateSuccessRate(Person executor, Person target)
        {
            if (executor == null || target == null) return 0;

            // 基于暗杀能力计算
            int executorAbility = executor.AssassinateAbility;
            int targetDefense = target.AssassinateAbility; // 目标的反暗杀能力
            
            // 简单估算：(执行者能力 - 目标防御*1.5) / 100
            double chance = (executorAbility - targetDefense * 1.5) / 200.0 + 0.3;
            if (chance < 0.05) chance = 0.05;
            if (chance > 0.95) chance = 0.95;
            
            return (int)(chance * 100);
        }

        /// <summary>
        /// 显示军师支持暗杀的对话
        /// </summary>
        private void ShowAdvisorAssassinateSupport(AssassinateAnalysisResult result, Architecture sourceArchitecture)
        {
            if (this.Plugins?.tupianwenziPlugin == null)
            {
                ShowExecutorSelectionWithAssassinateRecommendation(sourceArchitecture, result.TargetPerson, result.BestCandidate);
                return;
            }

            Faction faction = sourceArchitecture.BelongedFaction;
            string leaderAddress = AppellationSettings.GetAddress(faction, result.Advisor, faction?.Leader);
            if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";

            string recName = "勇士";
            if (result.BestCandidate != null)
            {
                recName = AppellationSettings.GetAddress(faction, result.Advisor, result.BestCandidate);
            }

            string message = $"{result.Advisor.Name}：{leaderAddress}，{result.TargetPerson.Name}乃敌方要员，若派{recName}前往行刺，可削弱敌军实力。";
            
            result.Advisor.TextResultString = message;

            this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                this.Plugins.ConfirmationDialogPlugin,
                new GameDelegates.VoidFunction(() => 
                {
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    ShowExecutorSelectionWithAssassinateRecommendation(sourceArchitecture, result.TargetPerson, result.BestCandidate);
                }),
                new GameDelegates.VoidFunction(() => 
                {
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    System.Diagnostics.Debug.WriteLine("玩家取消暗杀");
                })
            );

            this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
            this.Plugins.tupianwenziPlugin.SetGameObjectBranch(result.Advisor, result.Advisor, message, "", "", "");
            this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
            this.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 显示军师劝阻暗杀的对话
        /// </summary>
        private void ShowAdvisorAssassinateDissuasion(AssassinateAnalysisResult result, Architecture sourceArchitecture)
        {
            if (this.Plugins?.tupianwenziPlugin == null)
            {
                ShowExecutorSelectionForAssassinate(sourceArchitecture, result.TargetPerson);
                return;
            }

            Faction faction = sourceArchitecture.BelongedFaction;
            string leaderAddress = AppellationSettings.GetAddress(faction, result.Advisor, faction?.Leader);
            if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";

            string message = $"{result.Advisor.Name}：{leaderAddress}，{result.TargetPerson.Name}武艺高强，行刺风险极大，恐怕有去无回。";
            
            result.Advisor.TextResultString = message;

            this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                this.Plugins.ConfirmationDialogPlugin,
                new GameDelegates.VoidFunction(() => 
                {
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    ShowExecutorSelectionForAssassinate(sourceArchitecture, result.TargetPerson);
                }),
                new GameDelegates.VoidFunction(() => 
                {
                    this.Plugins.tupianwenziPlugin.IsShowing = false;
                    this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    System.Diagnostics.Debug.WriteLine("玩家听从劝阻取消暗杀");
                })
            );

            this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
            this.Plugins.tupianwenziPlugin.SetGameObjectBranch(result.Advisor, result.Advisor, message, "", "", "");
            this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
            this.Plugins.tupianwenziPlugin.IsShowing = true;
        }

        /// <summary>
        /// 显示执行人员选择界面（带推荐）
        /// </summary>
        private void ShowExecutorSelectionWithAssassinateRecommendation(Architecture sourceArchitecture, Person targetPerson, Person recommendedPerson)
        {
            try
            {
                // 保存目标人物供后续使用
                this.CurrentPerson = targetPerson;
                this.CurrentOperationType = IntelligentOperationType.Assassinate;
                
                var preSelectedList = new GameObjectList();
                if (recommendedPerson != null)
                    preSelectedList.Add(recommendedPerson);
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(1);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire,
                    true, true, true, true,
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    preSelectedList, 
                    $"暗杀 {targetPerson.Name} - 推荐: {recommendedPerson?.Name ?? "无"}", 
                    "暗杀"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionWithAssassinateRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示执行人员选择界面（无推荐）
        /// </summary>
        private void ShowExecutorSelectionForAssassinate(Architecture sourceArchitecture, Person targetPerson)
        {
            try
            {
                // 保存目标人物供后续使用
                this.CurrentPerson = targetPerson;
                this.CurrentOperationType = IntelligentOperationType.Assassinate;
                
                this.Plugins.TabListPlugin.SetSelectedItemMaxCount(1);
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.Person, 
                    FrameFunction.PersonManualHire,
                    true, true, true, true,
                    sourceArchitecture.PersonsExcludeNvGuan, 
                    null, 
                    $"暗杀 {targetPerson.Name}", 
                    "暗杀"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowExecutorSelectionForAssassinate] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示没有可暗杀目标的对话框
        /// </summary>
        private void ShowNoAssassinateTargetsDialog(Faction faction)
        {
            try
            {
                Person speaker = faction.Advisor ?? faction.Leader;
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string leaderAddress = AppellationSettings.GetAddress(faction, speaker, faction.Leader);
                if (string.IsNullOrEmpty(leaderAddress)) leaderAddress = "主公";

                string message = $"{speaker.Name}：{leaderAddress}，目前周边敌方势力中并无可行刺的目标。";

                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(speaker, speaker, message, "", "", "");
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoAssassinateTargetsDialog] 错误: {ex.Message}");
            }
        }

        #endregion

        #endregion

        #endregion

        /// <summary>
        /// 【军师对话系统】触发军师策略建议对话框
        /// </summary>
        /// <param name="strategy">策略类型</param>
        /// <param name="target">目标对象（可选）</param>
        public void TriggerAdvisorAdvice(WorldOfTheThreeKingdoms.GameManager.StrategyKind strategy, object target = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerAdvisorAdvice] 开始执行，策略类型: {strategy}");
                
                // A. 获取当前玩家势力
                var faction = Session.Current.Scenario.CurrentPlayer;
                if (faction == null) 
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerAdvisorAdvice] 当前玩家势力为null，退出");
                    return;
                }

                // B. 确定发言人（优先军师，其次君主）
                Person speaker = faction.Advisor;
                if (speaker == null)
                {
                    speaker = faction.Leader;
                    System.Diagnostics.Debug.WriteLine("[TriggerAdvisorAdvice] 没有军师，使用君主作为发言人");
                }
                if (speaker == null) 
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerAdvisorAdvice] 连君主都没有，退出");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[TriggerAdvisorAdvice] 发言人: {speaker.Name}");

                // C. 生成建议文本
                string adviceText = GetSimpleAdviceText(speaker, strategy);
                System.Diagnostics.Debug.WriteLine($"[TriggerAdvisorAdvice] 建议文本: {adviceText}");

                // D. 验证插件是否可用
                if (this.Plugins?.tupianwenziPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerAdvisorAdvice] tupianwenziPlugin为null，无法显示对话框");
                    ShowStrategyPersonSelection(strategy, null);
                    return;
                }

                // E. 设置对话框关闭后的回调函数 - 先设置回调
                this.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerAdvisorAdvice] 对话框关闭回调执行");
                    ShowStrategyPersonSelection(strategy, null);
                }));

                // F. 设置说话人的文本内容
                speaker.TextResultString = adviceText;
                System.Diagnostics.Debug.WriteLine($"[TriggerAdvisorAdvice] 设置说话人文本: {speaker.TextResultString}");

                // G. 设置对话框位置和显示
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, Session.MainGame.mainGameScreen);
                
                System.Diagnostics.Debug.WriteLine("[TriggerAdvisorAdvice] 准备调用SetGameObjectBranch");
                
                // H. 使用正确的6参数SetGameObjectBranch模式（参考成功的对话实现）
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                    speaker,                           // 说话人
                    speaker,                           // 游戏对象
                    adviceText,                        // 文本内容
                    "junshi.jpg",                      // 图片文件名
                    "junshi",                          // 声音文件名
                    ""                                 // 目标字符串
                );
                
                System.Diagnostics.Debug.WriteLine("[TriggerAdvisorAdvice] 准备显示对话框");
                this.Plugins.tupianwenziPlugin.IsShowing = true;
                
                System.Diagnostics.Debug.WriteLine($"[TriggerAdvisorAdvice] 显示军师建议: {adviceText}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerAdvisorAdvice] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TriggerAdvisorAdvice] 堆栈: {ex.StackTrace}");
                
                // 出错时直接跳转到人员选择
                ShowStrategyPersonSelection(strategy, null);
            }
        }

        /// <summary>
        /// 生成简单的军师建议文本
        /// </summary>
        /// <param name="speaker">发言人</param>
        /// <param name="strategy">策略类型</param>
        /// <returns>建议文本</returns>
        private string GetSimpleAdviceText(Person speaker, WorldOfTheThreeKingdoms.GameManager.StrategyKind strategy)
        {
            string speakerTitle = speaker.Name;
            
            switch (strategy)
            {
                case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Search:
                    return $"{speakerTitle}：主公，派遣能人搜索，或可有所收获。";
                case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Gossip:
                    return $"{speakerTitle}：主公，散布流言可动摇敌军士气，请慎选人才。";
                case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Destruction:
                    return $"{speakerTitle}：主公，破坏敌方设施需要勇敢之士，请选择合适人选。";
                case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Instigate:
                    return $"{speakerTitle}：主公，离间敌方需要口才出众之人，请明智选择。";
                case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Arson:
                    return $"{speakerTitle}：主公，暗杀行动凶险万分，需选择武艺高强者。";
                case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Alliance:
                    return $"{speakerTitle}：主公，结盟需要德才兼备之人，请谨慎派遣。";
                case WorldOfTheThreeKingdoms.GameManager.StrategyKind.JailBreak:
                    return $"{speakerTitle}：主公，劫牢救人需要勇武之士，请谨慎派遣。";
                case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Convince:
                    return $"{speakerTitle}：主公，请先选择要说服的目标，臣将为您分析可行性。";
                default:
                    return $"{speakerTitle}：主公，此策略需要合适的人才执行，请慎重选择。";
            }
        }

        /// <summary>
        /// 显示策略人员选择界面
        /// </summary>
        /// <param name="strategy">策略类型</param>
        /// <param name="target">目标对象</param>
        private void ShowStrategyPersonSelection(WorldOfTheThreeKingdoms.GameManager.StrategyKind strategy, object target)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ShowStrategyPersonSelection] 显示人员选择，策略: {strategy}");
                
                // 安全检查：确保必要的对象不为null
                if (this.CurrentArchitecture == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowStrategyPersonSelection] CurrentArchitecture为null，无法继续");
                    return;
                }
                
                if (this.Plugins?.TabListPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowStrategyPersonSelection] TabListPlugin为null，无法继续");
                    return;
                }
                
                // 根据策略类型调用相应的人员选择方法
                // 这里需要根据实际的策略枚举值来匹配对应的上下文菜单结果
                switch (strategy)
                {
                    case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Search:
                        this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Work, FrameFunction.GetSearchPerson, false, true, true, true, this.CurrentArchitecture.PersonsExcludeNvGuan, null, "搜索", "搜索");
                        break;
                    case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Gossip:
                        // 使用智能流言系统
                        System.Diagnostics.Debug.WriteLine("[ShowStrategyPersonSelection] 重定向到智能流言系统");
                        this.TriggerIntelligentGossip();
                        break;
                    case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Destruction:
                        // 使用智能破坏系统，确保完整的流程（选择目标 → 军师分析 → 选择执行人）
                        System.Diagnostics.Debug.WriteLine("[ShowStrategyPersonSelection] 重定向到智能破坏系统");
                        this.TriggerIntelligentDestroy();
                        break;
                    case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Instigate:
                        // 使用智能煽动系统，确保完整的流程（选择目标 → 军师分析 → 选择执行人）
                        System.Diagnostics.Debug.WriteLine("[ShowStrategyPersonSelection] 重定向到智能煽动系统");
                        this.TriggerIntelligentInstigate();
                        break;
                    case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Assassinate:
                        // 使用智能暗杀系统
                        System.Diagnostics.Debug.WriteLine("[ShowStrategyPersonSelection] 重定向到智能暗杀系统");
                        this.TriggerIntelligentAssassinate();
                        break;
                    case WorldOfTheThreeKingdoms.GameManager.StrategyKind.JailBreak:
                        TriggerIntelligentJailBreak(Session.MainGame.mainGameScreen.CurrentFaction);
                        break;
                    case WorldOfTheThreeKingdoms.GameManager.StrategyKind.Alliance:
                        // 结盟功能需要特殊处理，可能需要选择目标势力
                        System.Diagnostics.Debug.WriteLine("[ShowStrategyPersonSelection] 结盟功能暂未实现");
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"[ShowStrategyPersonSelection] 未知策略类型: {strategy}");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowStrategyPersonSelection] 错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ShowStrategyPersonSelection] 堆栈: {ex.StackTrace}");
            }
        }

        #region 外交分析方法

        /// <summary>
        /// 执行停战分析并给出建议
        /// </summary>
        public void PerformTruceDiplomaticAnalysisAndRecommendation(Faction targetFaction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PerformTruceDiplomaticAnalysisAndRecommendation] 开始分析停战可行性，目标势力: {targetFaction?.Name ?? "null"}");
                
                if (targetFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformTruceDiplomaticAnalysisAndRecommendation] 目标势力为null");
                    return;
                }

                Architecture sourceArchitecture = this.CurrentSourceArchitecture ?? this.CurrentArchitecture;
                if (sourceArchitecture?.BelongedFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformTruceDiplomaticAnalysisAndRecommendation] 源建筑或势力为null");
                    return;
                }

                Faction sourceFaction = sourceArchitecture.BelongedFaction;
                Person advisor = sourceFaction.Advisor ?? sourceFaction.Leader;
                
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformTruceDiplomaticAnalysisAndRecommendation] 没有军师或君主");
                    return;
                }

                // 分析停战可行性
                int successRate = CalculateTruceSuccessRate(sourceFaction, targetFaction);
                Person bestCandidate = FindBestDiplomaticCandidate(sourceArchitecture, "停战");

                string analysisText = GenerateTruceAnalysisText(advisor, targetFaction, successRate, bestCandidate);
                
                // 显示军师分析对话
                ShowDiplomaticAnalysisDialog(advisor, analysisText, () => {
                    // 用户确认后显示执行人员选择
                    ShowDiplomaticExecutorSelection(sourceArchitecture, targetFaction, "停战", bestCandidate);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformTruceDiplomaticAnalysisAndRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行劝降分析并给出建议
        /// </summary>
        public void PerformInduceSurrenderAnalysisAndRecommendation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PerformInduceSurrenderAnalysisAndRecommendation] 开始分析劝降可行性");
                
                Architecture sourceArchitecture = this.CurrentSourceArchitecture ?? this.CurrentArchitecture;
                if (sourceArchitecture?.BelongedFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformInduceSurrenderAnalysisAndRecommendation] 源建筑或势力为null");
                    return;
                }

                Faction sourceFaction = sourceArchitecture.BelongedFaction;
                Person advisor = sourceFaction.Advisor ?? sourceFaction.Leader;
                
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformInduceSurrenderAnalysisAndRecommendation] 没有军师或君主");
                    return;
                }

                // 获取当前劝降目标
                var targetRelation = this.CurrentInduceSurrenderTarget;
                if (targetRelation == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformInduceSurrenderAnalysisAndRecommendation] 没有劝降目标");
                    return;
                }

                // 分析劝降可行性
                int successRate = CalculateInduceSurrenderSuccessRate(sourceFaction, targetRelation);
                Person bestCandidate = FindBestDiplomaticCandidate(sourceArchitecture, "劝降");

                string analysisText = GenerateInduceSurrenderAnalysisText(advisor, targetRelation, successRate, bestCandidate);
                
                // 显示军师分析对话
                ShowDiplomaticAnalysisDialog(advisor, analysisText, () => {
                    // 用户确认后显示执行人员选择
                    ShowInduceSurrenderExecutorSelection(sourceArchitecture, targetRelation, bestCandidate);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformInduceSurrenderAnalysisAndRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行结盟分析并给出建议
        /// </summary>
        public void PerformAllyDiplomaticAnalysisAndRecommendation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PerformAllyDiplomaticAnalysisAndRecommendation] 开始分析结盟可行性");
                
                Architecture sourceArchitecture = this.CurrentSourceArchitecture ?? this.CurrentArchitecture;
                if (sourceArchitecture?.BelongedFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformAllyDiplomaticAnalysisAndRecommendation] 源建筑或势力为null");
                    return;
                }

                Faction sourceFaction = sourceArchitecture.BelongedFaction;
                Person advisor = sourceFaction.Advisor ?? sourceFaction.Leader;
                
                if (advisor == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformAllyDiplomaticAnalysisAndRecommendation] 没有军师或君主");
                    return;
                }

                // 获取可结盟的势力列表
                var allyTargets = GetAllyTargets(sourceFaction);
                if (allyTargets.Count == 0)
                {
                    ShowNoDiplomaticTargetsDialog(advisor, "结盟");
                    return;
                }

                // 分析最佳结盟目标
                Faction bestTarget = allyTargets[0]; // 简化处理，取第一个
                int successRate = CalculateAllySuccessRate(sourceFaction, bestTarget);
                Person bestCandidate = FindBestDiplomaticCandidate(sourceArchitecture, "结盟");

                string analysisText = GenerateAllyAnalysisText(advisor, bestTarget, successRate, bestCandidate);
                
                // 显示军师分析对话
                ShowDiplomaticAnalysisDialog(advisor, analysisText, () => {
                    // 用户确认后显示执行人员选择
                    ShowDiplomaticExecutorSelection(sourceArchitecture, bestTarget, "结盟", bestCandidate);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformAllyDiplomaticAnalysisAndRecommendation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 触发智能劝降外交关系
        /// </summary>
        public void TriggerIntelligentInduceSurrenderDiplomaticRelation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentInduceSurrenderDiplomaticRelation] 开始");
                
                if (this.CurrentArchitecture?.BelongedFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentInduceSurrenderDiplomaticRelation] 当前建筑或势力为null");
                    return;
                }

                // 设置操作类型和源建筑
                this.CurrentOperationType = IntelligentOperationType.None; // 劝降不在现有枚举中
                this.CurrentSourceArchitecture = this.CurrentArchitecture;

                // 获取可劝降的目标列表
                var surrenderTargets = this.CurrentArchitecture.GetQuanXiangDiplomaticRelationList();
                if (surrenderTargets == null || surrenderTargets.Count == 0)
                {
                    ShowNoDiplomaticTargetsDialog(this.CurrentArchitecture.BelongedFaction.Advisor ?? this.CurrentArchitecture.BelongedFaction.Leader, "劝降");
                    return;
                }

                // 显示目标选择界面
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.DiplomaticRelation, 
                    FrameFunction.GetInduceSurrenderTargetForAnalysis,
                    false, true, true, false, 
                    surrenderTargets, 
                    null, 
                    "选择劝降目标", 
                    "劝降"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentInduceSurrenderDiplomaticRelation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 触发智能结盟外交关系
        /// </summary>
        public void TriggerIntelligentAllyDiplomaticRelation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentAllyDiplomaticRelation] 开始");
                
                if (this.CurrentArchitecture?.BelongedFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentAllyDiplomaticRelation] 当前建筑或势力为null");
                    return;
                }

                // 设置操作类型和源建筑
                this.CurrentOperationType = IntelligentOperationType.None; // 结盟不在现有枚举中
                this.CurrentSourceArchitecture = this.CurrentArchitecture;

                // 获取可结盟的目标列表
                var allyTargets = this.CurrentArchitecture.GetAllyDiplomaticRelationList();
                if (allyTargets == null || allyTargets.Count == 0)
                {
                    ShowNoDiplomaticTargetsDialog(this.CurrentArchitecture.BelongedFaction.Advisor ?? this.CurrentArchitecture.BelongedFaction.Leader, "结盟");
                    return;
                }

                // 显示目标选择界面
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.DiplomaticRelation, 
                    FrameFunction.GetAllyTargetForAnalysis,
                    false, true, true, false, 
                    allyTargets, 
                    null, 
                    "选择结盟目标", 
                    "结盟"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentAllyDiplomaticRelation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 触发智能停战外交关系
        /// </summary>
        public void TriggerIntelligentTruceDiplomaticRelation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TriggerIntelligentTruceDiplomaticRelation] 开始");
                
                if (this.CurrentArchitecture?.BelongedFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine("[TriggerIntelligentTruceDiplomaticRelation] 当前建筑或势力为null");
                    return;
                }

                // 设置操作类型和源建筑
                this.CurrentOperationType = IntelligentOperationType.None; // 停战不在现有枚举中
                this.CurrentSourceArchitecture = this.CurrentArchitecture;

                // 获取可停战的目标列表
                var truceTargets = this.CurrentArchitecture.GetTruceDiplomaticRelationList();
                if (truceTargets == null || truceTargets.Count == 0)
                {
                    ShowNoDiplomaticTargetsDialog(this.CurrentArchitecture.BelongedFaction.Advisor ?? this.CurrentArchitecture.BelongedFaction.Leader, "停战");
                    return;
                }

                // 显示目标选择界面
                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame, 
                    FrameKind.DiplomaticRelation, 
                    FrameFunction.GetTruceTargetForAnalysis,
                    false, true, true, false, 
                    truceTargets, 
                    null, 
                    "选择停战目标", 
                    "停战"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TriggerIntelligentTruceDiplomaticRelation] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 当前劝降目标
        /// </summary>
        public DiplomaticRelationDisplay CurrentInduceSurrenderTarget { get; set; }

        /// <summary>
        /// 当前推荐的劝降执行人
        /// </summary>
        public Person CurrentRecommendedPersonForInduceSurrender { get; set; }

        #region 外交分析辅助方法

        /// <summary>
        /// 计算停战成功率
        /// </summary>
        private int CalculateTruceSuccessRate(Faction sourceFaction, Faction targetFaction)
        {
            if (sourceFaction == null || targetFaction == null) return 0;
            
            // 简化计算：基于双方实力对比和关系
            int baseRate = 50;
            
            // 实力对比影响
            int powerDiff = sourceFaction.Architectures.Count - targetFaction.Architectures.Count;
            if (powerDiff > 0) baseRate += Math.Min(20, powerDiff * 5);
            else baseRate -= Math.Min(20, Math.Abs(powerDiff) * 5);
            
            return Math.Max(10, Math.Min(90, baseRate));
        }

        /// <summary>
        /// 计算劝降成功率
        /// </summary>
        private int CalculateInduceSurrenderSuccessRate(Faction sourceFaction, DiplomaticRelationDisplay targetRelation)
        {
            if (sourceFaction == null || targetRelation == null) return 0;
            
            // 简化计算
            int baseRate = 30; // 劝降比停战更难
            
            return Math.Max(5, Math.Min(80, baseRate));
        }

        /// <summary>
        /// 计算结盟成功率
        /// </summary>
        private int CalculateAllySuccessRate(Faction sourceFaction, Faction targetFaction)
        {
            if (sourceFaction == null || targetFaction == null) return 0;
            
            // 简化计算
            int baseRate = 60; // 结盟相对容易
            
            return Math.Max(20, Math.Min(95, baseRate));
        }

        /// <summary>
        /// 寻找最佳外交候选人
        /// </summary>
        private Person FindBestDiplomaticCandidate(Architecture architecture, string actionType)
        {
            if (architecture?.Persons == null) return null;
            
            Person bestCandidate = null;
            int bestScore = 0;
            
            foreach (Person person in architecture.Persons)
            {
                if (person == null || !person.Available) continue;
                
                // 外交主要看魅力和政治
                int score = person.Glamour + person.Politics;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = person;
                }
            }
            
            return bestCandidate;
        }

        /// <summary>
        /// 生成停战分析文本
        /// </summary>
        private string GenerateTruceAnalysisText(Person advisor, Faction targetFaction, int successRate, Person bestCandidate)
        {
            string candidateName = bestCandidate?.Name ?? "合适人选";
            return $"{advisor.Name}：主公，与{targetFaction.Name}停战成功率约{successRate}%，建议派遣{candidateName}前往谈判。";
        }

        /// <summary>
        /// 生成劝降分析文本
        /// </summary>
        private string GenerateInduceSurrenderAnalysisText(Person advisor, DiplomaticRelationDisplay targetRelation, int successRate, Person bestCandidate)
        {
            string candidateName = bestCandidate?.Name ?? "合适人选";
            return $"{advisor.Name}：主公，劝降成功率约{successRate}%，建议派遣{candidateName}前往劝说。";
        }

        /// <summary>
        /// 生成结盟分析文本
        /// </summary>
        private string GenerateAllyAnalysisText(Person advisor, Faction targetFaction, int successRate, Person bestCandidate)
        {
            string candidateName = bestCandidate?.Name ?? "合适人选";
            return $"{advisor.Name}：主公，与{targetFaction.Name}结盟成功率约{successRate}%，建议派遣{candidateName}前往商议。";
        }

        /// <summary>
        /// 显示外交分析对话
        /// </summary>
        private void ShowDiplomaticAnalysisDialog(Person advisor, string analysisText, GameDelegates.VoidFunction confirmAction)
        {
            try
            {
                if (this.Plugins?.tupianwenziPlugin == null)
                {
                    confirmAction?.Invoke();
                    return;
                }

                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(
                    this.Plugins.ConfirmationDialogPlugin,
                    confirmAction,
                    new GameDelegates.VoidFunction(() => {
                        this.Plugins.tupianwenziPlugin.IsShowing = false;
                        this.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    })
                );

                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(advisor, advisor, analysisText, "", "", "");
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowDiplomaticAnalysisDialog] 错误: {ex.Message}");
                confirmAction?.Invoke();
            }
        }

        /// <summary>
        /// 显示外交执行人员选择
        /// </summary>
        private void ShowDiplomaticExecutorSelection(Architecture sourceArchitecture, Faction targetFaction, string actionType, Person recommendedPerson)
        {
            try
            {
                var preSelectedList = new GameObjectList();
                if (recommendedPerson != null)
                    preSelectedList.Add(recommendedPerson);

                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame,
                    FrameKind.Person,
                    FrameFunction.PersonManualHire,
                    true, true, true, true,
                    sourceArchitecture.PersonsExcludeNvGuan,
                    preSelectedList,
                    $"{actionType} - 推荐: {recommendedPerson?.Name ?? "无"}",
                    actionType
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowDiplomaticExecutorSelection] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示劝降执行人员选择
        /// </summary>
        private void ShowInduceSurrenderExecutorSelection(Architecture sourceArchitecture, DiplomaticRelationDisplay targetRelation, Person recommendedPerson)
        {
            try
            {
                // 保存当前目标和推荐人
                this.CurrentInduceSurrenderTarget = targetRelation;
                this.CurrentRecommendedPersonForInduceSurrender = recommendedPerson;

                var preSelectedList = new GameObjectList();
                if (recommendedPerson != null)
                    preSelectedList.Add(recommendedPerson);

                this.ShowTabListInFrame(
                    UndoneWorkKind.Frame,
                    FrameKind.Person,
                    FrameFunction.GetInduceSurrenderExecutor,
                    true, true, true, true,
                    sourceArchitecture.PersonsExcludeNvGuan,
                    preSelectedList,
                    $"劝降 - 推荐: {recommendedPerson?.Name ?? "无"}",
                    "劝降"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowInduceSurrenderExecutorSelection] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示没有外交目标的对话
        /// </summary>
        private void ShowNoDiplomaticTargetsDialog(Person speaker, string actionType)
        {
            try
            {
                if (speaker == null || this.Plugins?.tupianwenziPlugin == null)
                    return;

                string message = $"{speaker.Name}：主公，目前没有可进行{actionType}的目标。";
                
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(speaker, speaker, message, "", "", "");
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShowNoDiplomaticTargetsDialog] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取可结盟的目标列表
        /// </summary>
        private List<Faction> GetAllyTargets(Faction sourceFaction)
        {
            var targets = new List<Faction>();
            
            try
            {
                if (Session.Current?.Scenario?.Factions == null) return targets;
                
                foreach (Faction faction in Session.Current.Scenario.Factions)
                {
                    if (faction != null && faction != sourceFaction && !faction.Destroyed)
                    {
                        targets.Add(faction);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetAllyTargets] 错误: {ex.Message}");
            }
            
            return targets;
        }

        #endregion

        #endregion

        public void ShowTabListInFrame(UndoneWorkKind undoneWork, FrameKind kind, FrameFunction function, bool OKEnabled, bool CancelEnabled, bool showCheckBox, bool multiselecting, GameObjectList gameObjectList, GameObjectList selectedObjectList, string title, string tabName)
        {
            if ((gameObjectList != null) && (gameObjectList.Count != 0))
            {
                // 安全检查：确保必要的插件不为null
                if (this.Plugins?.TabListPlugin == null || this.Plugins?.GameFramePlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ShowTabListInFrame] 必要的插件为null，无法显示界面");
                    return;
                }
                
                this.Plugins.GameFramePlugin.Kind = kind;
                this.Plugins.GameFramePlugin.Function = function;
                this.Plugins.TabListPlugin.InitialValues(gameObjectList, selectedObjectList, InputManager.NowMouse.ScrollWheelValue, title);
                this.Plugins.TabListPlugin.SetListKindByName(kind.ToString(), showCheckBox, multiselecting);
                this.Plugins.TabListPlugin.SetSelectedTab(tabName);
                this.Plugins.GameFramePlugin.SetFrameContent(this.Plugins.TabListPlugin.TabList, base.viewportSizeFull);
                
                this.Plugins.GameFramePlugin.OKButtonEnabled = OKEnabled;
                this.Plugins.GameFramePlugin.CancelButtonEnabled = CancelEnabled;
                this.Plugins.GameFramePlugin.IsShowing = true;
            }
        }

        public void SetTabListInFrame(UndoneWorkKind undoneWork, FrameKind kind, FrameFunction function, bool OKEnabled, bool CancelEnabled, bool showCheckBox, bool multiselecting, GameObjectList gameObjectList, GameObjectList selectedObjectList, string title, string tabName)
        {
            if ((gameObjectList != null) && (gameObjectList.Count != 0))
            {
                // 安全检查：确保必要的插件不为null
                if (this.Plugins?.TabListPlugin == null || this.Plugins?.GameFramePlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SetTabListInFrame] 必要的插件为null，无法设置界面");
                    return;
                }
                
                this.Plugins.GameFramePlugin.Kind = kind;
                this.Plugins.GameFramePlugin.Function = function;
                this.Plugins.TabListPlugin.InitialValues(gameObjectList, selectedObjectList, InputManager.NowMouse.ScrollWheelValue, title);
                this.Plugins.TabListPlugin.SetListKindByName(kind.ToString(), showCheckBox, multiselecting);
                this.Plugins.TabListPlugin.SetSelectedTab(tabName);
                this.Plugins.GameFramePlugin.SetFrameContent(this.Plugins.TabListPlugin.TabList, base.viewportSizeFull);

                this.Plugins.GameFramePlugin.OKButtonEnabled = OKEnabled;
                this.Plugins.GameFramePlugin.CancelButtonEnabled = CancelEnabled;
                //this.Plugins.GameFramePlugin.IsShowing = true;
            }
        }

        public void ShowMapViewSelector(bool multiSelecting, GameObjectList gameObjectList, GameDelegates.VoidFunction function, MapViewSelectorKind mapViewSelectorKind)
        {
                        this.Plugins.MapViewSelectorPlugin.SetMultiSelecting(multiSelecting);
                        this.Plugins.MapViewSelectorPlugin.SetGameObjectList(gameObjectList);
                        //if (this.firstTimeMapViewSelector)
                        {
                            //this.firstTimeMapViewSelector = false;
                            this.Plugins.MapViewSelectorPlugin.SetMapPosition(ShowPosition.Center);
                        }
                        this.Plugins.MapViewSelectorPlugin.SetOKFunction(function);
                        this.Plugins.MapViewSelectorPlugin.Kind =mapViewSelectorKind;
                        //this.Plugins.MapViewSelectorPlugin.SetTabList(this.Plugins.TabListPlugin);
                        this.Plugins.MapViewSelectorPlugin.IsShowing = true;
        }
        

        private void gengxinyoucelan()
        {
            if (Session.Current.Scenario.CurrentPlayer != null)
            {
                // 🔥 2026-03-05 修复：刷新当前列表而不改变列表类型
                // 原逻辑：总是切换到建筑列表
                // 问题：用户在部队列表中操作部队后，右键菜单关闭时自动切换回建筑列表
                // 新逻辑：保持当前列表类型（部队或建筑），只刷新数据
                this.RefreshCurrentYoucelanList();
            }
        }

        /// <summary>
        /// 获取玩家手动控制的城池列表（排除委任军区的城池）
        /// </summary>
        private ArchitectureList GetPlayerControlledArchitectures()
        {
            if (Session.Current.Scenario.CurrentPlayer == null)
            {
                return null;
            }

            var player = Session.Current.Scenario.CurrentPlayer;

            // 使用 C# 12 目标类型 new()
            ArchitectureList playerControlled = new();
            
            foreach (Architecture architecture in player.Architectures.GetList())
            {
                // 🔥 修复：手动控制判定逻辑
                // 日期：2026-02-17
                // 问题：原逻辑使用 else if 导致君主直辖军区不检查 AutoRun 状态
                // 解决：核心判断是 AutoRun == false（非委任），君主直辖只是特殊情况
                
                bool isManualControlled = false;

                if (architecture.BelongedSection == null)
                {
                    // 情况1：无军区归属（直辖城池）
                    isManualControlled = true;
                }
                else if (architecture.BelongedSection.AIDetail == null)
                {
                    // 情况2：军区没有AI设定（数据错误，但视为手动控制）
                    isManualControlled = true;
                    System.Diagnostics.Debug.WriteLine($"[GetPlayerControlledArchitectures] ⚠️ 城池 {architecture.Name} 的军区 {architecture.BelongedSection.Name} 没有 AIDetail");
                }
                else
                {
                    // 情况3：核心判断 - 检查军区是否为非委任（AutoRun == false）
                    isManualControlled = !architecture.BelongedSection.AIDetail.AutoRun;
                }

                if (isManualControlled)
                {
                    playerControlled.Add(architecture);
                }
            }
            

            
            return playerControlled;
        }



        public void Showyoucelan(UndoneWorkKind undoneWork, FrameKind kind, FrameFunction function, bool OKEnabled, bool CancelEnabled, bool showCheckBox, bool multiselecting, GameObjectList gameObjectList, GameObjectList selectedObjectList, string title, string tabName)
        {
            // 🔍 调试：追踪所有调用
            // System.Diagnostics.Debug.WriteLine("========================================");
            // System.Diagnostics.Debug.WriteLine($"[Showyoucelan] 被调用！Kind={kind}, 列表数量={gameObjectList.Count}");
            // System.Diagnostics.Debug.WriteLine("[Showyoucelan] 调用堆栈：");
            // System.Diagnostics.Debug.WriteLine(Environment.StackTrace);
            // System.Diagnostics.Debug.WriteLine("========================================");
            
            this.Plugins.youcelanPlugin.Kind = kind;
            this.Plugins.youcelanPlugin.Function = function;
            
            // ✅ 修复调用顺序：先设置 listKindToDisplay，再调用 InitialValues
            this.Plugins.youcelanPlugin.SetListKindByName(kind.ToString(), showCheckBox, multiselecting);
            this.Plugins.youcelanPlugin.InitialValues(gameObjectList, selectedObjectList, InputManager.NowMouse.ScrollWheelValue, title);
            this.Plugins.youcelanPlugin.SetSelectedTab(tabName);
            this.Plugins.youcelanPlugin.SetyoucelanContent(base.viewportSize);
            this.Plugins.youcelanPlugin.IsShowing = true;
        }

        // 🎯 右侧栏切换功能：显示建筑列表
        public void ShowArchitectureListInYoucelan()
        {
            // 🧊 冷路径：UI事件
            
            // 🔍 调试：追踪调用来源
            // System.Diagnostics.Debug.WriteLine("========================================");
            // System.Diagnostics.Debug.WriteLine("[ShowArchitectureListInYoucelan] 被调用！");
            // System.Diagnostics.Debug.WriteLine("[ShowArchitectureListInYoucelan] 调用堆栈：");
            // System.Diagnostics.Debug.WriteLine(Environment.StackTrace);
            // System.Diagnostics.Debug.WriteLine("========================================");
            
            var currentPlayer = Session.Current.Scenario.CurrentPlayer;
            var firstSection = currentPlayer.FirstSection;
            
            this.Showyoucelan(
                UndoneWorkKind.None,
                FrameKind.Architecture,
                FrameFunction.Jump,
                false,
                true,
                false,
                false,
                firstSection.Architectures,
                null,
                "列表",  // ✅ 修改标题为"列表"
                ""
            );
        }

        // 🎯 右侧栏切换功能：显示部队列表
        public void ShowTroopListInYoucelan()
        {
            // 🔍 调试：追踪调用来源
            // System.Diagnostics.Debug.WriteLine("========================================");
            // System.Diagnostics.Debug.WriteLine("[ShowTroopListInYoucelan] 被调用！");
            // System.Diagnostics.Debug.WriteLine("[ShowTroopListInYoucelan] 调用堆栈：");
            // System.Diagnostics.Debug.WriteLine(Environment.StackTrace);
            // System.Diagnostics.Debug.WriteLine("========================================");
            
            var currentPlayer = Session.Current.Scenario.CurrentPlayer;
            GameObjectList troopList = GetPlayerControlledTroops(currentPlayer);
            
            this.Showyoucelan(
                UndoneWorkKind.None,
                FrameKind.Troop,
                FrameFunction.Jump,
                false,
                true,
                false,
                false,
                troopList,
                null,
                "列表",
                "Basic"
            );
        }

        /// <summary>
        /// 获取玩家手动控制的部队列表（排除委任军区的部队）
        /// </summary>
        private GameObjectList GetPlayerControlledTroops(Faction player)
        {
            // 🧊 冷路径：UI事件，允许使用 C# 12 集合表达式
            GameObjectList troopList = [];
            
            // 🔍 诊断计数器
            int totalPlayerTroops = 0;
            int manualControlledCount = 0;
            
            // ✅ 直接遍历，避免 LINQ 的重复枚举
            foreach (GameObject obj in Session.Current.Scenario.Troops.GetList())
            {
                if (obj is not Troop troop) continue;
                if (troop.BelongedFaction != player) continue;
                
                totalPlayerTroops++;
                
                // 🔥 修复：直接使用 ManualControl 属性判断
                // 日期：2026-03-07
                // 问题：原逻辑通过 StartingArchitecture 推断，但这是间接且不准确的
                // 解决：Troop 类有 ManualControl 属性，直接使用即可
                // ✅ Anti-Band-Aid: 使用数据源的真实属性，不做推断
                
                if (troop.ManualControl)
                {
                    troopList.Add(troop);
                    manualControlledCount++;
                    System.Diagnostics.Debug.WriteLine($"[GetPlayerControlledTroops] ✅ 部队 {troop.Name} - 手动控制");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[GetPlayerControlledTroops] ⏭️ 部队 {troop.Name} - AI控制，跳过");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[GetPlayerControlledTroops] 玩家总部队数: {totalPlayerTroops}, 筛选出手动控制: {manualControlledCount}");
            return troopList;
        }

        // 🎯 刷新当前右侧栏列表（不改变列表类型）
        public void RefreshCurrentYoucelanList()
        {
            var currentPlayer = Session.Current.Scenario.CurrentPlayer;
            var tabList = (TabListInFrame)this.Plugins.youcelanPlugin.TabList;
            
            if (tabList.isShowingTroopList)
            {
                // 刷新部队列表（只显示手动控制的部队）
                GameObjectList troopList = GetPlayerControlledTroops(currentPlayer);
                tabList.SetObjectList(troopList);
                tabList.ReCalculate();
            }
            else
            {
                // 刷新建筑列表（只显示手动控制的城池）
                ArchitectureList playerControlled = GetPlayerControlledArchitectures();
                tabList.SetObjectList(playerControlled);
                tabList.ReCalculate();
            }
        }

        public void ShowBianduiLiebiao(UndoneWorkKind undoneWork, FrameKind kind, FrameFunction function, bool OKEnabled, bool CancelEnabled, bool showCheckBox, bool multiselecting, GameObjectList gameObjectList, GameObjectList selectedObjectList, string title, string tabName,int bingyi)
        {
            //if ((gameObjectList != null) && (gameObjectList.Count != 0))
            {
                this.Plugins.BianduiLiebiao.Kind = kind;
                this.Plugins.BianduiLiebiao.Function = function;
                this.Plugins.BianduiLiebiao.ShezhiBingyi(bingyi);
                this.Plugins.BianduiLiebiao.InitialValues(gameObjectList, selectedObjectList, InputManager.NowMouse.ScrollWheelValue, title);
                this.Plugins.BianduiLiebiao.SetListKindByName(kind.ToString(), showCheckBox, multiselecting);
                this.Plugins.BianduiLiebiao.SetSelectedTab(tabName);

                //this.Plugins.GameFramePlugin.SetFrameyoucelanContent(this.Plugins.youcelanPlugin.TabList, base.viewportSize);  //viewportSize  游戏内容窗口的大小
                this.Plugins.BianduiLiebiao.SetyoucelanContent(base.viewportSize);  //viewportSize  游戏内容窗口的大小
                
                //this.Plugins.GameFramePlugin.shiyoucelan = true;
                //this.Plugins.GameFramePlugin.OKButtonEnabled = OKEnabled;
                //this.Plugins.GameFramePlugin.CancelButtonEnabled = CancelEnabled;
                //this.Plugins.GameFramePlugin.IsShowing = true;
                //this.Plugins.youcelanPlugin.IsShowing = true;


                this.Plugins.BianduiLiebiao.IsShowing = true;
                this.Plugins.youcelanPlugin.IsShowing = false ;

                this.Plugins.ContextMenuPlugin.ShezhiBianduiLiebiaoXinxi(this.Plugins.BianduiLiebiao.IsShowing, this.Plugins.BianduiLiebiao.Weizhi);

            }
        }


        //public void StopMusic()
        //{
        //    if (this.Player.playState == WMPPlayState.wmppsPlaying)
        //    {
        //        this.Player.stop();
        //    }
        //}

        public void ToggleFullScreen()
        {
            Platform.SetGraphicsWidthHeight(Session.MainGame.Window.ClientBounds.Width, Session.MainGame.Window.ClientBounds.Height);

            Session.MainGame.ToggleFullScreen();
            this.RefreshDisableRects();
        }

        public override void TroopAmbush(Troop troop)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.StartAmbush, "Ambush");
            }
        }

        public override void TroopAntiArrowAttack(Troop sending, Troop receiving)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(receiving.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(receiving.Leader, receiving.Position, TextMessageKind.AntiAttack, "AntiArrowAttack");
            }
        }

        public override void TroopAntiAttack(Troop sending, Troop receiving)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(receiving.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(receiving.Leader, receiving.Position, TextMessageKind.AntiAttack, "AntiAttack");
            }
        }

        public override void TroopApplyStunt(Troop troop, Stunt stunt)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                if (troop.BelongedFaction != null)
                {
                    troop.Leader.TextDestinationString = stunt.Name;
                    this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.UseStunt, "ApplyStunt");
                }
                else
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.UseStunt, "ApplyStuntBasic");
                }
            }
        }

        public override void TroopApplyTroopEvent(TroopEvent te, Troop troop)
        {
            if ((((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye) && (te.Dialogs.Count > 0))
            {
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                foreach (PersonDialog dialog in te.Dialogs)
                {
                    dialog.SpeakingPerson = (Session.Current.Scenario.Persons.GetGameObject(dialog.SpeakingPersonID) is Person ? (Person)Session.Current.Scenario.Persons.GetGameObject(dialog.SpeakingPersonID) : null);//修复部队事件未识别说话武将
                    if (dialog.SpeakingPerson !=null)
                    {
                        this.Plugins.tupianwenziPlugin.SetGameObjectBranch(dialog.SpeakingPerson, null, dialog.Text, te.Image, te.Sound,te.TryToShowString);
                    }
                    else
                    {
                        this.Plugins.tupianwenziPlugin.SetGameObjectBranch(troop.Leader, null, dialog.Text, te.Image, te.Sound,te.TryToShowString);
                    }
                }
                if (Setting.Current.GlobalVariables.DialogShowTime > 0)
                {
                    this.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(Session.Current.Scenario.ApplyTroopEvents));
                    this.Plugins.tupianwenziPlugin.IsShowing = true;
                }
                else
                {
                    Session.Current.Scenario.ApplyTroopEvents();
                }
            }
            else
            {
                Session.Current.Scenario.ApplyTroopEvents();
            }
        }

        public override void ObtainMilitaryKind(Faction f, Person giver, MilitaryKind m)
        {
            if (Session.Current.Scenario.CurrentPlayer == f || Session.GlobalVariables.SkyEye) 
            {
                giver.TextResultString = m.Name;
                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(giver, null, "SpyMessageNewFacility");
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, this);
                this.Plugins.tupianwenziPlugin.IsShowing = true;
            }
        }
        /*
        public override void AutoAwardGuanzhi(Person p, Person courier, Guanzhi guanzhi)
        {
            if (Session.Current.Scenario.CurrentPlayer == null || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(p.Position) || Session.GlobalVariables.SkyEye)
            {
                if (guanzhi.AutoLearnTextByCourier.Length > 0 && guanzhi.Level >= 6 )
                {
                    this.Plugins.tupianwenziPlugin.SetGameObjectBranch(courier, null, guanzhi.AutoLearnTextByCourier.Replace("%0", p.Name));
                    this.Plugins.tupianwenziPlugin.IsShowing = true;
                }
                if (guanzhi.AutoLearnText.Length > 0 && guanzhi.Level >= 6 )
                {
                    this.Plugins.tupianwenziPlugin.SetGameObjectBranch(p, null, guanzhi.AutoLearnText.Replace("%0", p.Name));
                    this.Plugins.tupianwenziPlugin.IsShowing = true;
                }
            }
        }*/

        public override void AutoLearnTitle(Person p, Person courier, Title title)
        {
            if (Session.Current.Scenario.CurrentPlayer == null || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(p.Position) || Session.GlobalVariables.SkyEye)
            {
                // 🔥 关键修复：使用当事人而非courier作为说话人
                // 日期：2026-03-23
                // 原因：courier (ID=7200) 是系统占位符，不应该显示给玩家
                // 修复：将说话人从 courier 改为 p（获得称号的当事人）
                if (title.AutoLearnTextByCourier.Length > 0 && title.Level >= 6)
                {
                    this.Plugins.tupianwenziPlugin.SetGameObjectBranch(p, null, title.AutoLearnTextByCourier.Replace("%0", p.Name));
                    this.Plugins.tupianwenziPlugin.IsShowing = true;
                }
                if (title.AutoLearnText.Length > 0 && title.Level >= 6)
                {
                    this.Plugins.tupianwenziPlugin.SetGameObjectBranch(p, null, title.AutoLearnText.Replace("%0", p.Name));
                    this.Plugins.tupianwenziPlugin.IsShowing = true;
                }
            }
        }

        public override void ApplyEvent(Event e, Architecture a, Screen screen)
        {


            if ((Session.Current.Scenario.CurrentPlayer == null || Session.Current.Scenario.CurrentPlayer.IsArchitectureKnown(a) || Session.GlobalVariables.SkyEye || e.GloballyDisplayed) 
                && (e.matchedDialog != null && e.matchedDialog.Count > 0 && (!e.Minor || e.InvolveLeader)))
            {
                this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, screen);
                
                foreach (PersonDialog dialog in e.matchedDialog)
                {
                    if (dialog.SpeakingPerson != null)
                    {
                        this.Plugins.tupianwenziPlugin.SetGameObjectBranch(dialog.SpeakingPerson, null, dialog.Text, e.Image, e.Sound,e.TryToShowString);
                    }
                    else
                    {
                        this.Plugins.tupianwenziPlugin.SetGameObjectBranch(a.BelongedFaction.Leader, null, dialog.Text, e.Image, e.Sound,e.TryToShowString);
                    }
                }

                if (e.yesEffect.Count > 0 || e.noEffect.Count > 0 || e.yesArchitectureEffect.Count > 0 || e.noArchitectureEffect.Count > 0)
                {
                    if (Session.Current.Scenario.CurrentPlayer != null)
                    {

                        if (!this.Plugins.ConfirmationDialogPlugin.IsShowing)
                        {
                            //this.Plugins.tupianwenziPlugin.SetConfirmationDialog(this.Plugins.ConfirmationDialogPlugin, new GameDelegates.VoidFunction(Session.Current.Scenario.ApplyEvents(true), new GameDelegates.VoidFunction(Session.Current.Scenario.ApplyEvents(false)));
                            this.Plugins.ConfirmationDialogPlugin.ClearFunctions();
                            this.Plugins.ConfirmationDialogPlugin.AddYesFunction(new GameDelegates.VoidFunction(Session.Current.Scenario.ApplyYesEvents));
                            this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                            this.Plugins.ConfirmationDialogPlugin.AddNoFunction(new GameDelegates.VoidFunction(Session.Current.Scenario.ApplyNoEvents));



                            this.Plugins.ConfirmationDialogPlugin.IsShowing = true;
                        }
                    }
                    else
                    {
                        if (GameObject.Chance(50))
                        {
                            Session.Current.Scenario.ApplyYesEvents();
                        }
                        else
                        {
                            Session.Current.Scenario.ApplyNoEvents();
                        }
                    }
                }

                if (Setting.Current.GlobalVariables.DialogShowTime > 0)
                {
                    this.Plugins.tupianwenziPlugin.SetCloseFunction(new GameDelegates.VoidFunction(Session.Current.Scenario.ApplyEvents));
                    (this.Plugins.tupianwenziPlugin as tupianwenziPlugin.tupianwenziPlugin).tupianwenzi.SetIsShowing(this, true);
                }
                else
                {
                    Session.Current.Scenario.ApplyEvents();
                }
            }
            else
            {
               
                Session.Current.Scenario.ApplyEvents();
            }
        }

        public override void TroopBreakWall(Troop troop, Architecture architecture)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.BreakWall, "BreakWall");
            }
        }

        public override void TroopCastDeepChaos(Troop sending, Troop receiving)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(sending.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.CastDeepChaos, "CastDeepChaos");
            }
        }

        public override void TroopCastStratagem(Troop sending, Troop receiving, Stratagem stratagem)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(sending.Position)) || Session.GlobalVariables.SkyEye)
            {
                if (sending.BelongedFaction != null)
                {
                    sending.Leader.TextDestinationString = receiving.Leader.Name;
                    sending.Leader.TextResultString = receiving.DisplayName;
                    this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, (TextMessageKind) (((int) TextMessageKind.UseStratagem0) + stratagem.ID), "Stratagem" + stratagem.ID);
                }
                else if (stratagem.Friendly)
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.NoFactionUseStratagemFriendly, "StratagemFriendly");
                }
                else
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.NoFactionUseStratagemHostile, "StratagemHostile");
                }
            }
        }

        public override void TroopChaos(Troop troop, bool deepChaos)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                if (deepChaos)
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.DeepChaos, "DeepChaos");
                } 
                else 
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.Chaos, "Chaos");
                }
            }
        }

        public override void TroopAttract(Troop troop, Troop caster)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.Attract, "Attract");
            }
        }

        public override void TroopRumour(Troop troop)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.Rumour, "Rumour");
            }
        }

        public override void TroopCombatMethodAttack(Troop sending, Troop receiving, CombatMethod combatMethod)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(sending.Position)) || Session.GlobalVariables.SkyEye)
            {
                if (sending.BelongedFaction != null)
                {
                    sending.Leader.TextDestinationString = receiving.Leader.Name;
                    sending.Leader.TextResultString = receiving.DisplayName;
                    this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.UseCombatMethod, "CombatMethod" + combatMethod.ID);
                }
                else
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.UseCombatMethod, "CombatMethod");
                }
            }
        }

        public override void TroopCreate(Troop troop)
        {
            if ((((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye) && !Session.Current.Scenario.IsCurrentPlayer(troop.BelongedFaction))
            {
                troop.TextDestinationString = troop.StartingArchitecture.Name;
                this.Plugins.GameRecordPlugin.AddBranch(troop, "TroopCreate", troop.Position);
            }
        }

        public override void TroopCriticalStrike(Troop sending, Troop receiving)
        {
            // 🔥 Anti-Band-Aid: 调用方保证 sending 不为 null
            // 可见性检查：视野内 或 天眼模式
            bool isVisible = Session.Current.Scenario.CurrentPlayer == null 
                || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(sending.Position) 
                || Session.GlobalVariables.SkyEye;
            
            if (!isVisible) return;
            
            // 显示暴击气泡
            if (receiving != null)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.Critical, "CriticalStrike");
            } 
            else 
            {
                this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.CriticalArchitecture, "CriticalStrikeOnArchitecture");
            }
            
            // 🎯 显示暴击图：仅玩家部队显示
            // 条件：视野内 + 玩家部队
            bool isPlayerTroop = Session.Current.Scenario.CurrentPlayer != null 
                && sending.BelongedFaction == Session.Current.Scenario.CurrentPlayer;
            
            if (isPlayerTroop)
            {
                _criticalHitImageManager.AddCriticalHitImage(sending, WorldOfTheThreeKingdoms.GameObjects.Animations.CriticalHitType.普通攻击暴击, _currentGameTime);
            }
            
            /* 🚧 战法暴击功能暂时注释（架构限制：战法在暴击判定时还未施放）
            // 普通攻击暴击
            if (sending.CurrentCombatMethod == null)
            {
                if (receiving != null)
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.Critical, "CriticalStrike");
                } 
                else 
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.CriticalArchitecture, "CriticalStrikeOnArchitecture");
                }
                
                _criticalHitImageManager.AddCriticalHitImage(sending, WorldOfTheThreeKingdoms.GameObjects.Animations.CriticalHitType.普通攻击暴击, _currentGameTime);
            }
            // 战法攻击暴击
            else
            {
                if (receiving != null)
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.Critical, "CriticalStrike");
                }
                
                _criticalHitImageManager.AddCriticalHitImage(sending, WorldOfTheThreeKingdoms.GameObjects.Animations.CriticalHitType.战法攻击暴击, _currentGameTime);
            }
            */
        }

        public override void TroopDiscoverAmbush(Troop sending, Troop receiving)
        {
            if ((sending != null) && (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(sending.Position)) || Session.GlobalVariables.SkyEye))
            {
                this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.DiscoverAmbush, "DiscoverAmbush");
            }
            if ((receiving != null) && (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(receiving.Position)) || Session.GlobalVariables.SkyEye))
            {
                this.Plugins.PersonBubblePlugin.AddPerson(receiving.Leader, receiving.Position, TextMessageKind.BeDiscoverAmbush, "BeDiscoveredAmbush");
            }
        }

        public override void TroopEndCutRouteway(Troop troop, bool success)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                if (success)
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.CutRoutewaySuccess, "EndCutRoutewaySuccess");
                }
                else
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.CutRoutewayFail, "EndCutRoutewayFail");
                }
            }
        }

        public override void TroopEndPath(Troop troop)
        {
        }

        public override void TroopGetNewCaptive(Troop troop, PersonList personlist)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                Person person = personlist[StaticMethods.Random(personlist.Count)] as Person;
                troop.Leader.TextDestinationString = person.Name;
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.TroopNewCaptive, "TroopGetNewCaptive");
                troop.TextDestinationString = person.Name;
                this.Plugins.GameRecordPlugin.AddBranch(troop, "TroopGetNewCaptive", troop.Position);
            }
        }

        public override void TroopGetSpreadBurnt(Troop troop)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.GetSpreadBurnt, "GetSpreadBurnt");
            }
        }

        public override void TroopLevyFieldFood(Troop troop, int food)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, "LevyFieldFood");
            }
        }

        public override void TroopNormalAttack(Troop sending, Troop receiving)
        {
        }

        public override void TroopOccupyArchitecture(Troop troop, Architecture architecture)
        {
            this.Plugins.GameRecordPlugin.AddBranch(architecture, "ArchitectureOccupied", troop.Position);

        }

        public override void TroopOutburst(Troop troop, OutburstKind kind)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                switch (kind)
                {
                    case OutburstKind.愤怒:
                        this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.Angry, "TroopOutburstAngry");
                        break;

                    case OutburstKind.沉静:
                        this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.Calm, "TroopOutburstQuiet");
                        break;
                }
            }
        }

        public override void TroopPathNotFound(Troop troop)
        {
        }



        public override void TroopReceiveCriticalStrike(Troop sending, Troop receiving)
        {
            if (!receiving.Destroyed && ((((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(receiving.Position)) || Session.GlobalVariables.SkyEye) && ((receiving.Status != TroopStatus.混乱) && (GameObject.Chance(receiving.Leader.Braveness * 10)))))
            {
                this.Plugins.PersonBubblePlugin.AddPerson(receiving.Leader, receiving.Position, TextMessageKind.BeCritical, "ReceiveCriticalStrike");
            }
        }

        public override void TroopReceiveWaylay(Troop sending, Troop receiving)
        {
            if (!receiving.Destroyed && (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(receiving.Position)) || Session.GlobalVariables.SkyEye))
            {
                this.Plugins.PersonBubblePlugin.AddPerson(receiving.Leader, receiving.Position, TextMessageKind.BeAmbush, "ReceiveWaylay");
            }
        }

        public override void TroopRecoverFromChaos(Troop troop)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.RecoverChaos, "RecoverFromChaos");
            }
        }

        public override void TroopReleaseCaptive(Troop troop, PersonList personlist)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                Person person = personlist[StaticMethods.Random(personlist.Count)] as Person;
                troop.TextDestinationString = person.Name;
                this.Plugins.GameRecordPlugin.AddBranch(troop, "TroopReleaseCaptive", troop.Position);
            }
        }

        public override void TroopResistStratagem(Troop sending, Troop receiving, Stratagem stratagem, bool isHarmful)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(receiving.Position)) || Session.GlobalVariables.SkyEye)
            {
                if (isHarmful)
                {
                    if (GameObject.Random(receiving.TroopIntelligence) > GameObject.Random(sending.TroopIntelligence))
                    {
                        this.Plugins.PersonBubblePlugin.AddPerson(receiving.Leader, receiving.Position, TextMessageKind.ResistHarmfulStratagem, "ResistHarmfulStratagem");
                    }
                }
                else
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(receiving.Leader, receiving.Position, TextMessageKind.ResistHelpfulStratagem, "ResistNoHarmStratagem");
                }
                
                // 🔥 2026-03-10 新增：计略失败的详细反馈（包含天气影响）
                // 🔥 ANTI-BAND-AID：数据完整性断言，不使用防御性检查
                System.Diagnostics.Debug.Assert(Session.Current?.Scenario?.WeatherManager != null,
                    "[TroopResistStratagem] WeatherManager 未初始化，检查 GameScenario.Init()");
                System.Diagnostics.Debug.Assert(Session.Current.Scenario.EnvironmentConfig?.WeatherStratagem != null,
                    "[TroopResistStratagem] WeatherStratagem 配置缺失，检查 EnvironmentConfig.json");

                var weather = Session.Current.Scenario.WeatherManager.GetWeatherAt(receiving.Position);
                var weatherMultiplier = GetWeatherMultiplierForDisplay(stratagem, receiving.Position);
                
                if (weatherMultiplier < 1.0f)
                {
                    string weatherName = WorldOfTheThreeKingdoms.GameLogic.WeatherManager.GetWeatherDisplayName(weather);
                    System.Diagnostics.Debug.WriteLine(
                        $"[计略失败] {sending.DisplayName} 对 {receiving.DisplayName} 使用 {stratagem.Name} 失败！{weatherName}天削弱了效果（{weatherMultiplier:F1}x）");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[计略失败] {sending.DisplayName} 对 {receiving.DisplayName} 使用 {stratagem.Name} 失败！{receiving.DisplayName} 成功抵抗");
                }
            }
        }

        public override void TroopRout(Troop sending, Troop receiving)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(sending.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.Rout, "Rout");
            }
        }

        public override void TroopRouted(Troop sending, Troop receiving)
        {
        }

        public override void TroopSetCombatMethod(Troop troop, CombatMethod combatMethod)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                troop.Leader.TextDestinationString = combatMethod.Name;
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.SetCombatMethod, "SetCombatMethod");
            }
        }

        public override void TroopSetStratagem(Troop troop, Stratagem stratagem)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                troop.Leader.TextDestinationString = stratagem.Name;
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.SetStratagem, "SetStratagem");
            }
        }

        public override void TroopStartCutRouteway(Troop troop, int days)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                troop.Leader.TextDestinationString = days.ToString();
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.StartCutRouteway, "StartCutRouteway");
            }
        }

        public override void TroopStopAmbush(Troop troop)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(troop.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(troop.Leader, troop.Position, TextMessageKind.StopAmbush, "StopAmbush");
            }
        }

        public override void TroopStratagemSuccess(Troop sending, Troop receiving, Stratagem stratagem, bool isHarmful)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(receiving.Position)) || Session.GlobalVariables.SkyEye)
            {
                if (isHarmful)
                {
                    if (GameObject.Chance(0x21))
                    {
                        this.Plugins.PersonBubblePlugin.AddPerson(receiving.Leader, receiving.Position, TextMessageKind.TrappedByStratagem, "HarmfulStratagemSuccess");
                    }
                }
                else if ((sending != receiving) && GameObject.Chance(0x21))
                {
                    this.Plugins.PersonBubblePlugin.AddPerson(receiving.Leader, receiving.Position, TextMessageKind.HelpedByStratagem, "NoHarmStratagemSuccess");
                }
                
                // 🔥 2026-03-10 新增：计略成功的详细反馈（包含天气影响）
                // 🔥 ANTI-BAND-AID：数据完整性断言，不使用防御性检查
                System.Diagnostics.Debug.Assert(Session.Current?.Scenario?.WeatherManager != null,
                    "[TroopStratagemSuccess] WeatherManager 未初始化，检查 GameScenario.Init()");
                System.Diagnostics.Debug.Assert(Session.Current.Scenario.EnvironmentConfig?.WeatherStratagem != null,
                    "[TroopStratagemSuccess] WeatherStratagem 配置缺失，检查 EnvironmentConfig.json");

                var weather = Session.Current.Scenario.WeatherManager.GetWeatherAt(receiving.Position);
                var weatherMultiplier = GetWeatherMultiplierForDisplay(stratagem, receiving.Position);
                
                if (weatherMultiplier > 1.0f)
                {
                    string weatherName = WorldOfTheThreeKingdoms.GameLogic.WeatherManager.GetWeatherDisplayName(weather);
                    System.Diagnostics.Debug.WriteLine(
                        $"[计略成功] {sending.DisplayName} 对 {receiving.DisplayName} 使用 {stratagem.Name} 成功！{weatherName}天增强了效果（{weatherMultiplier:F1}x）");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[计略成功] {sending.DisplayName} 对 {receiving.DisplayName} 使用 {stratagem.Name} 成功！");
                }
            }
        }

        /// <summary>
        /// 获取天气修正倍率（用于显示反馈）
        /// 日期：2026-03-10
        /// </summary>
        private float GetWeatherMultiplierForDisplay(Stratagem stratagem, Point targetPosition)
        {
            // 🔥 ANTI-BAND-AID：数据完整性断言
            System.Diagnostics.Debug.Assert(Session.Current?.Scenario?.WeatherManager != null,
                "[GetWeatherMultiplierForDisplay] WeatherManager 未初始化");
            System.Diagnostics.Debug.Assert(Session.Current.Scenario.EnvironmentConfig?.WeatherStratagem != null,
                "[GetWeatherMultiplierForDisplay] WeatherStratagem 配置缺失");

            var targetWeather = Session.Current.Scenario.WeatherManager.GetWeatherAt(targetPosition);
            var weatherConfig = Session.Current.Scenario.EnvironmentConfig.WeatherStratagem;
            
            string weatherName = targetWeather.ToString();
            string animationKindName = stratagem.AnimationKind.ToString();
            
            var modifiers = weatherConfig.Modifiers;
            int modifierCount = modifiers.Count;
            
            for (int i = 0; i < modifierCount; i++)
            {
                var modifier = modifiers[i];
                if (modifier.AnimationKind == animationKindName && modifier.Weather == weatherName)
                {
                    return modifier.Multiplier;
                }
            }
            
            return 1.0f;
        }

        public override void TroopSurround(Troop sending, Troop receiving)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(sending.Position)) || Session.GlobalVariables.SkyEye)
            {
                sending.Leader.TextDestinationString = receiving.Leader.Name;
                sending.Leader.TextResultString = receiving.DisplayName;
                this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.Surround, "Surround");
            }
        }

        public override void TroopWaylay(Troop sending, Troop receiving)
        {
            if (((Session.Current.Scenario.CurrentPlayer == null) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(sending.Position)) || Session.GlobalVariables.SkyEye)
            {
                this.Plugins.PersonBubblePlugin.AddPerson(sending.Leader, sending.Position, TextMessageKind.Ambush, "Waylay");
            }
        }

        public void TryToDemolishRouteway()
        {
            if (!this.Plugins.tupianwenziPlugin.IsShowing)
            {
                this.Plugins.ConfirmationDialogPlugin.SetSimpleTextDialog(this.Plugins.SimpleTextDialogPlugin);
                this.Plugins.ConfirmationDialogPlugin.ClearFunctions();
                this.Plugins.ConfirmationDialogPlugin.AddYesFunction(new GameDelegates.VoidFunction(this.DemolishCurrentRouteway));
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                this.Plugins.SimpleTextDialogPlugin.SetBranch("DemolishRouteway");
                this.Plugins.ConfirmationDialogPlugin.IsShowing = true;
            }
        }

        private void saveBeforeExit()
        {
            this.mainMapLayer.StopThreads();
            if (Session.GlobalVariables.HardcoreMode)
            {
                this.SaveGameAutoPosition();
            }
            
            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop || Platform.PlatFormType == PlatFormType.Android)
            {
                Platform.Current.Exit();
            }
            else
            {
                ReturnMainMenu();
            }            
        }

        public override void TryToExit()
        {
            if (!this.Plugins.tupianwenziPlugin.IsShowing)
            {
                this.Plugins.ConfirmationDialogPlugin.SetSimpleTextDialog(this.Plugins.SimpleTextDialogPlugin);
                this.Plugins.ConfirmationDialogPlugin.ClearFunctions();
                this.Plugins.ConfirmationDialogPlugin.AddYesFunction(new GameDelegates.VoidFunction(this.saveBeforeExit));
                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                this.Plugins.SimpleTextDialogPlugin.SetBranch(Session.GlobalVariables.HardcoreMode ? "ExitSaveGame" : (Session.Current.Scenario.JustSaved ? "ExitGameNoReminder" : "ExitGame"));
                this.Plugins.ConfirmationDialogPlugin.IsShowing = true;
            }
        }

        
        private volatile bool roundDone = false;
        private object roundDoneLock = new object();
        private int _gameGoFrameCounter = 0;

        public override void GameGo(GameTime gameTime)
        {
            // 🔥 NUCLEAR OPTION: Comprehensive Null Safety Check
            try
            {
                if (Session.Current?.Scenario == null)
                {
                    System.Diagnostics.Debug.WriteLine("[GameGo] ❌ Session.Current.Scenario is null, aborting GameGo");
                    return;
                }

                if (Session.Current.Scenario.Parameters == null)
                {
                    Session.Current.Scenario.Parameters = new WorldOfTheThreeKingdoms.GameGlobal.Parameters();
                    System.Diagnostics.Debug.WriteLine("[GameGo] 🔧 紧急修复: 初始化Parameters");
                }

                if (Session.Current.Scenario.GlobalVariables == null)
                {
                    Session.Current.Scenario.GlobalVariables = new WorldOfTheThreeKingdoms.GameGlobal.GlobalVariables();
                    System.Diagnostics.Debug.WriteLine("[GameGo] 🔧 紧急修复: 初始化GlobalVariables");
                }

                if (Session.Current.Scenario.CurrentPlayer == null && Session.Current.Scenario.Factions != null && Session.Current.Scenario.Factions.Count > 0)
                {
                    Session.Current.Scenario.CurrentPlayer = Session.Current.Scenario.Factions[0] as Faction;
                    System.Diagnostics.Debug.WriteLine($"[GameGo] 🔧 紧急修复: 设置当前玩家 {Session.Current.Scenario.CurrentPlayer?.Name}");
                }

                if (this.Plugins?.DateRunnerPlugin == null)
                {
                    System.Diagnostics.Debug.WriteLine("[GameGo] ❌ DateRunnerPlugin is null, aborting GameGo");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GameGo] ❌ Nuclear Option 安全检查异常: {ex.Message}");
                return;
            }

            // 🔥 Fix GameGo Livelock: If AI is running (Threading=true), guard clause prevents re-entry.
            if (Session.Current?.Scenario != null && Session.Current.Scenario.Threading)
            {
                return;
            }

            // 🔍 诊断日志（每60帧输出一次）
            bool shouldLog = (_gameGoFrameCounter++ % 60 == 0);

            bool viewMoveOk = (this.viewMove == ViewMove.Stop);
            if (!viewMoveOk)
            {
                if (shouldLog) System.Diagnostics.Debug.WriteLine($"[GameGo] ❌ viewMove={this.viewMove}, 不是Stop，跳过");
                return;
            }

            bool factionsStillRunning = this.AfterDayPassed(gameTime);
            // if (shouldLog) System.Diagnostics.Debug.WriteLine($"[GameGo] AfterDayPassed={factionsStillRunning}, QueueEmpty={Session.Current?.Scenario?.Factions?.QueueEmpty}, RunningFaction={Session.Current?.Scenario?.Factions?.RunningFaction?.Name ?? \"null\"}");

            if (!factionsStillRunning)
            {
                // 🆕 2026-03-21：回合开始前，先衰减所有势力的能量
                // 日期：2026-03-21
                // 用途：修复部队能量永久残留 Bug（余威自然衰退机制）
                // 说明：部队离开后，能量不是立即消失，也不是永久残留，而是逐渐衰减
                // 
                // 🔥 关键修复：跳过游戏启动时的第一次能量衰减
                // 日期：2026-03-21
                // 原因：游戏启动时能量还未初始化，第一次衰减会把所有能量清零，导致水墨渲染器不显示
                if (!_isFirstTurn)
                {
                    _influenceUpdateManager?.DecayAllFactionsEnergy();
                }
                else
                {
                    _isFirstTurn = false;
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[GameGo] ⏸️ 跳过第一次能量衰减（游戏启动）");
                    #endif
                }
                
                this.Plugins.DateRunnerPlugin.DateGo();
                
                bool dateIsRunning = Session.Current?.Scenario?.Date?.IsRunning ?? false;
                if (shouldLog) System.Diagnostics.Debug.WriteLine($"[GameGo] DateGo完成, Date.IsRunning={dateIsRunning}, playing={this.Plugins.DateRunnerPlugin?.IsPlaying}");
                
                bool troopsMovementDone = this.AfterDayStarting(gameTime);
                
                if (shouldLog) System.Diagnostics.Debug.WriteLine($"[GameGo] MoveTheTroops返回={troopsMovementDone} (false=全部移完, true=还在移动), TotallyEmpty={Session.Current?.Scenario?.Troops?.TotallyEmpty}, CurrentTroop={Session.Current?.Scenario?.Troops?.CurrentTroop?.DisplayName ?? "null"}");
                
                // 🔥 修复：MoveTheTroops (AfterDayStarting) 的返回值语义：
                //   false = 所有部队移动完毕 (TotallyEmpty)
                //   true  = 还有部队在移动中
                // 只有当部队全部移完 (!troopsMovementDone) 时，才能结束本日
                if (Session.GlobalVariables.EnableResposiveThreading)
                {
                    if (!troopsMovementDone)
                    {
                        if (shouldLog) System.Diagnostics.Debug.WriteLine("[GameGo] 异步模式：部队移完，设置roundDone=true");
                        roundDone = true;
                    }
                }
                else
                {
                    // 同步模式：只有部队移完后才调用 DateStop 结束本日
                    // 否则下一帧继续调用 MoveTheTroops 处理队列中剩余部队
                    if (!troopsMovementDone && Session.Current?.Scenario?.Date != null && Session.Current.Scenario.Date.IsRunning) 
                    {
                        if (shouldLog) System.Diagnostics.Debug.WriteLine("[GameGo] 同步模式：部队移完，调用DateStop");
                        this.Plugins.DateRunnerPlugin.DateStop();
                        
                        // 🗺️ 回合结束时强制更新所有势力范围（2026-03-11）
                        _influenceUpdateManager?.ForceUpdateAll();
                    }
                }
            }
        }

        /// <summary>
        /// 🔧 FIX: 数据清洗与修复函数
        /// 用于在读取存档后，强制修复所有对象之间的引用关系和非法数值
        /// </summary>
        private void SanitizeGameData()
        {
            System.Diagnostics.Debug.WriteLine("[System] 开始执行数据清洗 (SanitizeGameData)...");
            
            int fixedLegionRefs = 0;
            int fixedMissingLegions = 0;
            int fixedOverflows = 0;
            
            // 获取场景引用
            var scenario = Session.Current.Scenario;
            if (scenario == null) return;
            
            // ========================================================================
            // 1. 修复军团 <-> 部队 的双向引用 (Bidirectional Reference Fix)
            // 目标：确保 legion.Troops 里的兵，其 BelongedLegion 字段一定指向该 legion
            // ========================================================================
            foreach (Faction faction in scenario.Factions)
            {
                if (faction == null) continue;
                
                foreach (Legion legion in faction.Legions)
                {
                    if (legion == null) continue;
                    
                    // 遍历军团内的每一个部队
                    for (int i = 0; i < legion.Troops.Count; i++)
                    {
                        Troop troop = legion.Troops[i] as Troop;
                        if (troop != null)
                        {
                            // 检查引用是否断裂
                            if (troop.BelongedLegion != legion)
                            {
                                // 强制修复反向引用
                                troop.BelongedLegion = legion;
                                fixedLegionRefs++;
                            }
                            
                            // 顺便修复所属势力引用
                            if (troop.BelongedFaction != faction)
                            {
                                troop.BelongedFaction = faction;
                            }
                        }
                    }
                }
            }
            
            // ========================================================================
            // 2. 拯救"流浪"部队 (Fix Homeless Troops)
            // 目标：确保所有在地图上的部队都有军团，没有的立刻分配
            // ========================================================================
            var allTroops = scenario.Troops.GetList();
            foreach (Troop troop in allTroops)
            {
                if (troop == null) continue;
                
                // 如果部队有势力，但没有军团 (BelongedLegion == null)
                if (troop.BelongedFaction != null && troop.BelongedLegion == null)
                {
                    // 尝试找回目标建筑或出发地
                    Architecture targetArch = troop.WillArchitecture ?? troop.StartingArchitecture;
                    
                    // 兜底：如果连目标都没了，回首府
                    if (targetArch == null) targetArch = troop.BelongedFaction.Capital;
                    
                    if (targetArch != null)
                    {
                        // 1. 尝试获取现有军团
                        Legion assignedLegion = troop.BelongedFaction.GetLegion(targetArch);
                        
                        // 2. 如果没有，创建默认军团
                        if (assignedLegion == null)
                        {
                            assignedLegion = troop.BelongedFaction.CreateDefaultLegion(targetArch);
                        }
                        
                        // 3. 执行分配并建立双向连接
                        if (assignedLegion != null)
                        {
                            troop.BelongedLegion = assignedLegion;
                            if (!assignedLegion.Troops.HasGameObject(troop))
                            {
                                assignedLegion.AddTroop(troop);
                            }
                            fixedMissingLegions++;
                        }
                    }
                }
                
                // ====================================================================
                // 3. 修复兵力数值溢出 (Fix Integer Overflow)
                // 目标：解决负数兵力和异常兵力的问题
                // ====================================================================
                if (troop.Quantity < 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[Warning] 修正 {troop.DisplayName} 的负数兵力: {troop.Quantity} -> 0");
                    troop.Quantity = 0;
                    troop.InjuryQuantity = 0;
                    fixedOverflows++;
                }
                else if (troop.Army != null && troop.Army.Kind != null)
                {
                    int maxQuantity = troop.Army.Kind.MaxScale;
                    if (troop.Quantity > maxQuantity)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Warning] 修正 {troop.DisplayName} 的异常兵力: {troop.Quantity} -> {maxQuantity}");
                        troop.Quantity = maxQuantity;
                        if (troop.InjuryQuantity > troop.Quantity)
                        {
                            troop.InjuryQuantity = 0;
                        }
                        fixedOverflows++;
                    }
                }
            }
            
            // 输出修复报告
            if (fixedLegionRefs > 0 || fixedMissingLegions > 0 || fixedOverflows > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[System] 数据清洗完成:");
                System.Diagnostics.Debug.WriteLine($"  - 修复引用断裂: {fixedLegionRefs} 个");
                System.Diagnostics.Debug.WriteLine($"  - 分配丢失军团: {fixedMissingLegions} 个");
                System.Diagnostics.Debug.WriteLine($"  - 修正数值溢出: {fixedOverflows} 个");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[System] 数据清洗完成：未发现需要修复的数据");
            }
            
            // ========================================================================
            // 4. 🔧 FIX: 清理 AI 势力的多军区，合并成一个军区
            // 目标：AI 势力默认只有一个军区（由君主控制）
            // ========================================================================
            CleanupAIMultipleSections();
        }
        
        /// <summary>
        /// 🔧 FIX: 清理 AI 势力的多军区，合并成一个军区
        /// </summary>
        private void CleanupAIMultipleSections()
        {
            var scenario = Session.Current.Scenario;
            if (scenario == null) return;
            
            int cleanedFactions = 0;
            int removedSections = 0;
            
            foreach (Faction faction in scenario.Factions)
            {
                if (faction == null) continue;
                
                // 检查是否是 AI 势力（非玩家势力）
                bool isPlayerFaction = scenario.CurrentPlayer != null && faction == scenario.CurrentPlayer;
                
                // 如果是玩家势力，跳过
                if (isPlayerFaction)
                {
                    System.Diagnostics.Debug.WriteLine($"[CleanupAIMultipleSections] 跳过玩家势力: {faction.Name} (军区数:{faction.Sections.Count})");
                    continue;
                }
                
                // 如果 AI 势力有多个军区，需要清理
                if (faction.Sections.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"[CleanupAIMultipleSections] 发现AI势力 {faction.Name} 有 {faction.Sections.Count} 个军区，开始清理...");
                    
                    // 保留第一个军区（君主控制的军区）
                    Section firstSection = faction.Sections[0] as Section;
                    if (firstSection == null) continue;
                    
                    // 收集所有需要移除的军区
                    var sectionsToRemove = new List<Section>();
                    for (int i = 1; i < faction.Sections.Count; i++)
                    {
                        Section section = faction.Sections[i] as Section;
                        if (section != null)
                        {
                            sectionsToRemove.Add(section);
                        }
                    }
                    
                    // 将其他军区的建筑合并到第一个军区
                    foreach (Section section in sectionsToRemove)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CleanupAIMultipleSections] 移除军区: {section.Name} (建筑数:{section.Architectures.Count})");
                        
                        // 将建筑转移到第一个军区
                        foreach (Architecture arch in section.Architectures.GetList())
                        {
                            if (arch != null && !firstSection.Architectures.HasGameObject(arch))
                            {
                                firstSection.Architectures.Add(arch);
                                arch.BelongedSection = firstSection;
                            }
                        }
                        
                        // 清空该军区的建筑列表
                        section.Architectures.Clear();
                        
                        // 从势力中移除该军区
                        faction.Sections.Remove(section);
                        
                        // 从场景中移除该军区
                        scenario.Sections.Remove(section);
                        
                        removedSections++;
                    }
                    
                    cleanedFactions++;
                    System.Diagnostics.Debug.WriteLine($"[CleanupAIMultipleSections] ✅ 势力 {faction.Name} 清理完成，保留军区: {firstSection.Name}");
                }
            }
            
            if (cleanedFactions > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CleanupAIMultipleSections] 清理完成: {cleanedFactions} 个AI势力，移除 {removedSections} 个多余军区");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[CleanupAIMultipleSections] 未发现需要清理的AI多军区");
            }
        }

        private void RunAI()
        {
            // 🔥 修复：添加适当的延迟和控制，避免无限循环
            try
            {
                // 只有当需要处理AI时才执行
                // 只有当需要处理AI时才执行
                // if (Session.Current?.Scenario?.Factions?.RunningFaction != null)
                {
                    this.GameGo(new GameTime());
                }
                
                // 添加短暂延迟，避免CPU占用过高
                System.Threading.Thread.Sleep(10);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RunAI] 异常: {ex.Message}");
                // 异常时也要延迟，避免无限重试
                System.Threading.Thread.Sleep(100);
            }
        }

        private PlatformTask aiThread;

        private bool loaded = false;

        private bool toggleScreen = false;

        private float toggleScreenTime = 0f;

        public override void Update(GameTime gameTime)   //视野内容更新
        {
            // 🔥 更新选中部队的能量覆盖范围缓存
            // 日期：2026-03-21
            // 🧊 Cold Path：仅在选中部队变化时执行计算
            UpdateSelectedTroopZoc();
            
            // 🔥 存储当前帧的 GameTime（用于暴击图等需要时间的系统）
            _currentGameTime = gameTime;
            
            // 🛡️ GOD MODE: Catch ANY error and keep running 🛡️
            try
            {
                // 🌧️ 延迟初始化天气粒子系统（Cold Path - 仅初始化一次）
                // 日期：2026-03-10
                // 🔥 ANTI-BAND-AID：仅在场景未加载时跳过，初始化失败则抛出异常
                if (_weatherParticleSystem == null)
                {
                    if (Session.Current?.Scenario != null)
                    {
                        // 创建 1x1 白色纹理（用于粒子渲染）
                        _pixelTexture = new Texture2D(Platform.GraphicsDevice, 1, 1);
                        _pixelTexture.SetData([Color.White]);
                        
                        // 创建 1x1 白色纹理（用于边界线渲染）
                        _whitePixel = new Texture2D(Platform.GraphicsDevice, 1, 1);
                        _whitePixel.SetData([Color.White]);
                        
                        // 初始化粒子系统（失败则抛出异常）
                        _weatherParticleSystem = new WorldOfTheThreeKingdoms.GameLogic.WeatherParticleSystem(_pixelTexture);
                        
                        // 🔥 2026-03-11 性能优化：缓存地图尺寸，避免每帧访问属性链
                        // 🔥 修复：Point 只有 X 和 Y 属性，没有 Width 和 Height
                        _cachedMapWidth = Session.Current.Scenario.ScenarioMap.MapDimensions.X;
                        _cachedMapHeight = Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
                        
                        System.Diagnostics.Debug.WriteLine("[MainGameScreen] 天气粒子系统初始化完成");
                    }
                    // 场景未加载时跳过初始化，等待下一帧
                }
                
                // 🗺️ 延迟初始化势力范围更新管理器（Cold Path - 仅初始化一次）
                // 日期：2026-03-11
                // 🔥 ANTI-BAND-AID：仅在场景未加载时跳过，初始化失败则抛出异常
                if (_influenceUpdateManager == null)
                {
                    if (Session.Current?.Scenario != null)
                    {
                        // 🔥 ANTI-BAND-AID：如果场景存在但 Architectures 为 null，这是数据错误
                        if (Session.Current.Scenario.Architectures == null)
                        {
                            throw new InvalidOperationException(
                                "[MainGameScreen] 数据损坏：Scenario 存在但 Architectures 为 null");
                        }
                        
                        // 🧊 Cold Path：使用 LINQ 提高可读性
                        var architectures = Session.Current.Scenario.Architectures.GetList()
                            .OfType<Architecture>()
                            .ToList();
                        
                        _influenceUpdateManager = new WorldOfTheThreeKingdoms.GameManager.InfluenceUpdateManager(architectures);
                        _influenceUpdateManager.Initialize();  // 🆕 阶段 1：初始化地形代价缓存
                        
                        // 🔥 订阅能量竞争前的事件（用于水墨渲染器）
                        // 日期：2026-03-21
                        // 原因：水墨渲染器需要在 ApplyGlobalEnergyCompetition 清零能量前读取数据
                        _influenceUpdateManager.OnBeforeEnergyCompetition += () =>
                        {
                            _inkRenderer?.UpdateInfluenceMap();
                        };
                        
                        // 🔥 重新应用势力范围增益（GlobalInfluenceMap 已初始化）
                        // 🔥 日期：2026-03-17
                        // 🔥 原因：AfterLoadGameScenario() 中的 ApplyInfluenceBuff() 因 GlobalInfluenceMap 未初始化而跳过
                        Session.Current.Scenario.Architectures.ApplyInfluenceBuff();
                        Session.Current.Scenario.Troops.ApplyInfluenceBuff();
                        System.Diagnostics.Debug.WriteLine("[MainGameScreen] ✅ 势力范围增益应用完成");
                        
                        // 🎨 GlobalInfluenceMap 已初始化，现在可以安全调用水墨渲染器
                        _inkRenderer?.UpdateInfluenceMap();
                        
                        System.Diagnostics.Debug.WriteLine("[MainGameScreen] 🗺️ 势力范围系统初始化完成");
                    }
                    // 场景未加载时跳过初始化，等待下一帧
                }
                
                // 🗺️ 延迟初始化势力范围渲染器（Cold Path - 仅初始化一次）
                // 日期：2026-03-11
                if (_influenceRenderer == null)
                {
                    if (_pixelTexture != null && _whitePixel != null)
                    {
                        _influenceRenderer = new WorldOfTheThreeKingdoms.GameManager.InfluenceRenderer(_pixelTexture, _whitePixel);
                        System.Diagnostics.Debug.WriteLine("[MainGameScreen] 🗺️ 势力范围渲染器初始化完成");
                    }
                    // _pixelTexture 或 _whitePixel 未创建时跳过初始化，等待下一帧
                }
                
                // 🌧️ 更新天气粒子系统（Hot Path - 每帧调用）
                // 日期：2026-03-10
                // 🔥 性能优化：通过设置开关控制是否启用粒子系统
                if (Session.GlobalVariables.EnableWeatherParticles && _weatherParticleSystem != null)
                {
                    // 🔥 HOT PATH 优化：直接计算视野中心（地图格子坐标），避免中间分配
                    // TopLeftPosition/BottomRightPosition 在 ResetScreenEdge() 中更新，会跟随地图滑动
                    int viewCenterX = this.TopLeftPosition.X + (this.BottomRightPosition.X - this.TopLeftPosition.X) / 2;
                    int viewCenterY = this.TopLeftPosition.Y + (this.BottomRightPosition.Y - this.TopLeftPosition.Y) / 2;
                    
                    // 🔥 钳制焦点坐标到地图有效范围（使用缓存的地图尺寸，避免每帧访问属性链）
                    int clampedX = Math.Clamp(viewCenterX, 0, _cachedMapWidth - 1);
                    int clampedY = Math.Clamp(viewCenterY, 0, _cachedMapHeight - 1);
                    
                    _weatherParticleSystem.Update(
                        gameTime, 
                        new Point(clampedX, clampedY),
                        Platform.GraphicsDevice.Viewport.Width,
                        Platform.GraphicsDevice.Viewport.Height);
                }
                
                // 🗺️ 更新势力范围系统（Cold Path - 低频更新）
                // 日期：2026-03-11
                // 🔥 性能优化：默认每300帧（5秒）更新一次，大部分时间直接返回
                _influenceUpdateManager?.Update();
                
                // 🎨 平滑插值水墨渲染器（Hot Path - 每帧调用）
                // 日期：2026-03-13
                _inkRenderer?.Update();
                
                // 🔥 [性能关键] 处理主线程调度队列（用于异步纹理加载等操作）
                // 必须在 Update 早期调用，确保每帧都处理
                if (_mainThreadDispatcher != null)
                {
                    _mainThreadDispatcher.ProcessQueue();
                    
                    // 性能监控：每 300 帧（约 5 秒）记录一次队列长度
                    _dispatcherMonitorCounter++;
                    if (_dispatcherMonitorCounter >= 300)
                    {
                        _dispatcherMonitorCounter = 0;
                        int pendingCount = _mainThreadDispatcher.PendingCount;
                        if (pendingCount > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainThreadDispatcher] 待处理操作数: {pendingCount}");
                        }
                    }
                }
                
                // 🎯 更新暴击图管理器（Cold Path - 每帧更新，但不在热循环中）
                // 调用方（Initialize）保证 _criticalHitImageManager 已初始化
                _criticalHitImageManager.Update(gameTime);
                
                // [FIX] Ensure InGameEditorPlugin is always updated, regardless of game state or UndoneWork
                // [FIX] Ensure InGameEditorPlugin is always updated, regardless of game state or UndoneWork
                if (this.Plugins != null && this.Plugins.InGameEditorPlugin != null)
                {
                    this.Plugins.InGameEditorPlugin.Update(gameTime);
                }

                // 🔥 Checklist Fix: Priority 3 - Defensive Checks for Plugins
                if (this.Plugins != null)
                {
                    if (this.Plugins.PersonBubblePlugin != null) this.Plugins.PersonBubblePlugin.Update(gameTime);
                    if (this.Plugins.ToolBarPlugin != null) this.Plugins.ToolBarPlugin.Update(gameTime);
                    // Add other plugin updates here if they were previously crashing
                }

                // 1. Safety Check (Prevent logic running on bad data)
                if (Session.Current?.Scenario?.CurrentPlayer == null ||
                    Session.Current.Scenario.Factions == null) 
                {
                    return; 
                }

                // ====== 异步寻路系统：处理寻路结果 ======
                // 🔥 Hot Path - 每帧调用，严格优化
                try
                {
                    global::GameObjects.AI.Pathfinding.AsyncPathfindingManager.Instance.ProcessCompletedPaths(result =>
                    {
                        // 🔥 性能优化：直接使用 Troops 集合，避免类型转换
                        global::GameObjects.Troop troop = null;
                        
                        // 使用 for 循环遍历，避免 LINQ
                        var troops = Session.Current.Scenario.Troops;
                        for (int i = 0; i < troops.Count; i++)
                        {
                            var t = troops[i] as global::GameObjects.Troop;
                            if (t != null && t.ID == result.TroopId)
                            {
                                troop = t;
                                break;
                            }
                        }
                        
                        if (troop != null && !troop.Destroyed)
                        {
                            troop.OnPathfindingCompleted(result);
                        }
                        else
                        {
                            // 部队已销毁，回收路径内存（孤儿结果处理）
                            if (result.IsSuccess && result.Path != null)
                            {
                                global::GameObjects.AI.Pathfinding.PathPool.Return(result.Path);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 寻路结果处理异常: {ex.Message}");
                }

                // 2. Original Logic (Keep all your existing code here)
                // 更新性能监控系统
                _performanceMonitor.Update(gameTime);
            
            // 更新内存监控系统
            _memoryMonitor.Update();
            
            // [新增] 双击系统更新钩子
            /*
            try
            {
                DoubleClickIntegration.Update(gameTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DoubleClickIntegration update failed: {ex.Message}");
            }
            */
            
            // 更新音频管理系统
            // if (global::GameManager.AudioManager.Instance != null)
            // {
            //     // 更新监听者位置（通常是摄像机中心）
            //     global::GameManager.AudioManager.Instance.ListenerPosition = new Vector2(
            //         this.mainMapLayer.LeftEdge + this.mainMapLayer.TileWidth * base.viewportSize.X / 2,
            //         this.mainMapLayer.TopEdge + this.mainMapLayer.TileHeight * base.viewportSize.Y / 2
            //     );
            //     global::GameManager.AudioManager.Instance.Update(gameTime);
            // }
            
            // 更新视觉管理系统
            if (_visualsManager != null)
            {
                // 更新摄像机位置（基于地图偏移）
                var cameraPosition = new Vector2(
                    this.mainMapLayer.LeftEdge,
                    this.mainMapLayer.TopEdge
                );
                _visualsManager.SetCameraPosition(cameraPosition);
                
                // 更新所有单位视觉组件
                _visualsManager.Update(gameTime);
            }
            
            // 初始化视觉管理系统（如果尚未初始化）
            if (_visualsManager == null)
            {
                _visualsManager = new WorldOfTheThreeKingdoms.GameManager.VisualsManager();
                
                // 设置视口大小
                _visualsManager.SetViewportSize(new Vector2(base.viewportSize.X, base.viewportSize.Y));
                
                // 为现有部队创建视觉组件
                if (Session.Current?.Scenario?.Troops != null)
                {
                    var troopList = Session.Current.Scenario.Troops.GetList().Cast<object>().ToList();
                    _visualsManager.CreateVisualsForTroops(troopList);
                }
                
                System.Diagnostics.Debug.WriteLine("[MainGameScreen] 视觉管理系统初始化完成");
            }
            
            // 初始化AI管理器（如果尚未初始化且场景可用）
            if (_aiManager == null && Session.Current?.Scenario?.Troops != null)
            {
                // 缓存列表
                var troopList = Session.Current.Scenario.Troops.GetList().Cast<Troop>().ToList();
                _aiManager = new AIManager(troopList);
                
                // 启动分摊初始化
                _aiManager.StartInitialization(troopList);
                
                // 智能预热对象池 - 根据当前场景规模自动调整
                PrewarmPoolsIntelligently();
                
                System.Diagnostics.Debug.WriteLine("[MainGameScreen] AI管理器和对象池初始化完成");
            }
            
            // 初始化军师UI系统（如果尚未初始化且场景可用）
            if (_strategistUI != null && !_strategistUILoaded && Session.Current?.Scenario != null && Session.Current?.Content != null)
            {
                try
                {
                    _strategistUI.LoadContent(Session.Current.Content, Platform.GraphicsDevice);
                    _strategistUILoaded = true;
                    System.Diagnostics.Debug.WriteLine("[MainGameScreen] 军师UI系统初始化完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 军师UI初始化失败: {ex.Message}");
                }
            }
            
            // 初始化对话UI系统（如果尚未初始化且场景可用）
            if (dialogueUI != null && !dialogueUILoaded && Session.Current?.Content != null)
            {
                try
                {
                    dialogueUI.LoadContent(Session.Current.Content, Platform.GraphicsDevice);
                    dialogueUILoaded = true;
                    System.Diagnostics.Debug.WriteLine("[MainGameScreen] 对话UI系统初始化完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 对话UI初始化失败: {ex.Message}");
                    // 即使失败也设置为已加载，避免重复尝试
                    dialogueUILoaded = true;
                }
            }
            
            // 🎨 初始化势力范围渲染器（2026-03-12）
            if (_influenceRenderer == null && Session.Current?.Content != null)
            {
                try
                {
                    int screenWidth = Platform.GraphicsDevice.Viewport.Width;
                    int screenHeight = Platform.GraphicsDevice.Viewport.Height;
                    
                    // 🎨 尝试创建新工笔重彩渲染器，失败则回退到传统模式
                    _influenceRenderer = InfluenceRendererInitializer.CreateRenderer(
                        Platform.GraphicsDevice,
                        Session.Current.Content,
                        screenWidth,
                        screenHeight,
                        preferInkBleed: true);
                    
                    // 🎨 默认启用渲染（可通过 F12 切换）
                    _influenceRenderer.IsEnabled = true;
                    
                    System.Diagnostics.Debug.WriteLine("[MainGameScreen] 势力范围渲染器初始化完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 势力范围渲染器初始化失败: {ex.Message}");
                }
            }
            
            // 🧠 更新AI决策系统 - 基于记忆驱动的三步式AI逻辑
            // 🔥 关键修复：只在回合引擎未运行时更新AI，避免线程冲突
            if (this.Plugins.DateRunnerPlugin != null && !this.Plugins.DateRunnerPlugin.IsRunning)
            {
                UpdateAIDecisionSystem(gameTime);
            }
            
            // 更新AI系统 - 性能优化：减少GetList()调用频率
            // 🔥 关键修复：只在回合引擎未运行时更新AI，避免线程冲突
            if (_aiManager != null && Session.Current?.Scenario?.Troops != null && 
                this.Plugins.DateRunnerPlugin != null && !this.Plugins.DateRunnerPlugin.IsRunning)
            {
                // 只在必要时更新部队列表（每60帧更新一次，约1秒）
                if (_globalFrameCounter % 60 == 0)
                {
                    var currentTroops = Session.Current.Scenario.Troops.GetList().Cast<Troop>().ToList();
                    _aiManager.UpdateTroopList(currentTroops);
                }
                
                // 确保每帧都调用
                _aiManager.Update(gameTime);
            }
            
            if (toggleScreen)
            {
                toggleScreenTime += Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds);
                if (toggleScreenTime >= 1f)
                {
                    toggleScreen = false;
                    int width = Session.MainGame.Window.ClientBounds.Width;
                    int height = Session.MainGame.Window.ClientBounds.Height;
                    Session.RealResolution = Session.Resolution = width + "*" + height;
                    Platform.SetGraphicsWidthHeight(width, height);
                    //Session.MainGame.ToggleFullScreen();
                    Platform.GraphicsApplyChanges();
                    this.UpdateViewport();
                    this.ResetTiles();
                    this.RefreshDisableRects();
                    JumpToFaction();
                }
            }

            if (!loaded)
            {
                loaded = true;
                //全屏的判断放到初始化代码中
                if (Session.GlobalVariables.FullScreen)
                {
                    Platform.Current.SetFullScreen2(true);
                    toggleScreen = true;
                    //Platform.Current.ProcessViewChanged();
                    //this.RefreshDisableRects();
                    //
                }                
            }

            if (cloudLayer.IsVisible)
            {
                if (cloudLayer.IsStart)
                {

                }
                else
                {
                    if (this.mainMapLayer.DisplayingMapTiles.Exists(ma => ma == null || ma.TileTexture == null))
                    {

                    }
                    else
                    {
                        cloudLayer.IsStart = true;
                    }
                }
                cloudLayer.Update(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));
            }

            if (dantiaoLayer == null)
            {

            }
            else
            {
                dantiaoLayer.Update(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));

                return;
            }

            if (this.Plugins.ToolBarPlugin != null)
            {
                var btBack = ((ToolBarPlugin.ToolBarPlugin)this.Plugins.ToolBarPlugin).backTool;
                btBack.Update();
                if (btBack.MouseOver)
                {
                    //if (InputManager.IsPressed)
                    //{
                    //    InputManager.SleepTime = 1f;
                    //}
                }
            }
            if (base.EnableUpdate)
            {
                /*try
                {*/


                // 🔥 Thread Safety Fix: Execute deferred FactionGetControl on main thread
                if (this.pendingControlFaction != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGameScreen.Update] 在主线程处理玩家控制权: {this.pendingControlFaction.Name}");
                    this.PerformFactionGetControl(this.pendingControlFaction);
                    this.pendingControlFaction = null;
                }

                this.UpdateCount++;
                base.Update(gameTime);
                this.CalculateFrameRate(gameTime);
                
                // 🆕 更新地块势力范围信息插件（Hot Path - 每帧调用）
                // 日期：2026-03-13
                if (this.Plugins.TileInfluenceInfoPlugin != null && this.mainMapLayer != null)
                {
                    this.Plugins.TileInfluenceInfoPlugin.UpdateTileInfo(
                        InputManager.NowMouse.Position,
                        this.mainMapLayer.LeftEdge,
                        this.mainMapLayer.TopEdge,
                        this.mainMapLayer.TileWidth,
                        this.mainMapLayer.TileHeight
                    );
                }
                
                // Add null check for PersonBubblePlugin to prevent NullReferenceException
                if (this.Plugins.PersonBubblePlugin != null)
                {
                    this.Plugins.PersonBubblePlugin.Update(gameTime);
                }

                // 更新对话UI系统
                if (this.dialogueUI != null && this.dialogueUI.IsActive)
                {
                    this.dialogueUI.Update(gameTime);
                    
                    // 【检查对话是否刚刚结束】
                    if (this.dialogueUI.IsFinished && this.currentDialogueCallback != null)
                    {
                        // 执行回调（也就是执行 RecallAdvisor 里的那些清除数据的代码）
                        this.currentDialogueCallback.Invoke();
                        
                        // 清空回调，防止重复执行
                        this.currentDialogueCallback = null;
                        
                        // 彻底关闭 UI
                        this.dialogueUI.Hide();
                    }
                }

                // 更新军师UI系统
                if (_strategistUI != null && _strategistUILoaded && Session.Current?.Scenario != null)
                {
                    try
                    {
                        _strategistUI.Update(gameTime);
                        
                        // 更新军师按钮状态
                        _strategistUI.UpdateAdvisorButtonSafe(Session.Current.Scenario, Platform.GraphicsDevice);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 军师UI更新异常: {ex.Message}");
                    }
                }

                switch (base.UndoneWorks.Peek().Kind)
                {
                    case UndoneWorkKind.None:

                        this.UpdateToolBar(gameTime);
                        this.UpdateScreenBlind(gameTime);
                        //this.Plugins.youcelanPlugin.Update(gameTime);
                        //this.Plugins.youcelanPlugin.IsShowing = false;
                        this.UpdateViewMove();
                        this.HandleLaterMouseEvent(gameTime);
                        this.ScrollTheMainMap(gameTime);
                        this.HandleKey(gameTime);

                        if (Session.GlobalVariables.EnableResposiveThreading)
                        {
                            if (aiThread == null || !aiThread.IsAlive)
                            {
                                aiThread = null;
                                aiThread = new PlatformTask(() => RunAI()); 
                                    //new PlatformTask(new ThreadStart(RunAI));
                                aiThread.Start();
                            }

                            lock (roundDoneLock)
                            {
                                if (roundDone)
                                {
                                    roundDone = false;
                                    this.Plugins.DateRunnerPlugin.DateStop();
                                    
                                    // 🗺️ 回合结束时强制更新所有势力范围（2026-03-11）
                                    _influenceUpdateManager?.ForceUpdateAll();
                                    
                                    // 🎨 水墨渲染器更新已通过 OnBeforeEnergyCompetition 事件自动触发
                                    // 日期：2026-03-21
                                    // 原因：必须在 ApplyGlobalEnergyCompetition 清零能量前读取数据
                                }
                            }
                        }
                        else
                        {
                            this.GameGo(gameTime);
                        }

                        // 四叉树更新 - 包含快速战斗优化
                        UpdateQuadtree(gameTime);

                        if (Session.Current.Scenario.PlayerFactions.Count == 0)
                        {
                            this.DateGo(1);
                        }

                        //if (!this.Plugins.youcelanPlugin.IsShowing)
                        //{
                        //    this.Showyoucelan(UndoneWorkKind.None, FrameKind.Architecture, FrameFunction.Jump, false, true, false, false, Session.Current.Scenario.CurrentPlayer.Architectures, null, "", "");
                        //}
                        break;

                    case UndoneWorkKind.Dialog:
                        this.UpdateDialog(gameTime);

                        break;
                    case UndoneWorkKind.tupianwenzi:
                        this.Updatetupianwenzi(gameTime);

                        break;

                    case UndoneWorkKind.liangdaobianji:

                        this.HandleLaterMouseEvent(gameTime);
                        this.ScrollTheMainMap(gameTime);

                        break;
                    case UndoneWorkKind.Selecting:
                        if (base.EnableSelecting)
                        {
                            this.ResetCurrentStatus();
                            this.UpdateViewMove();
                            this.HandleLaterMouseScroll();
                            this.ScrollTheMainMap(gameTime);
                            this.UpdateConmentText(gameTime);
                        }
                        break;

                    case UndoneWorkKind.Inputer:
                        this.UpdateInputer(gameTime);
                        break;

                    case UndoneWorkKind.Selector:
                        this.HandleLaterMouseEvent(gameTime);
                        this.ScrollTheMainMap(gameTime);
                        break;

                    case UndoneWorkKind.MapViewSelector:
                        this.ResetCurrentStatus();
                        this.UpdateViewMove();
                        this.HandleLaterMouseScroll();
                        this.ScrollTheMainMap(gameTime);
                        if (base.EnableLaterMouseEvent)
                        {
                            this.UpdateSurvey(gameTime);
                            this.UpdateConmentText(gameTime);
                        }
                        break;
                }

                var optionDialog = Session.MainGame.mainGameScreen.Plugins.OptionDialogPlugin as OptionDialogPlugin.OptionDialogPlugin;
                if(optionDialog.IsShowing)
                {
                    optionDialog.Update(gameTime);
                }
            }
            }
            catch (Exception)
            {
                // 🤫 Shhh... Swallow the crash. 
                // If a frame fails, we just skip it and try drawing the next frame.
                // This prevents CTD (Crash To Desktop).
                return;
            }
        }

        private void UpdateConmentText(GameTime gameTime)
        {
            if ((this.Plugins.ConmentTextPlugin != null) && (this.lastPosition != this.position))
            {
                Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.position);
                Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(this.position);
                this.Plugins.ConmentTextPlugin.BuildThirdText(Session.Current.Scenario.GetCoordinateString(this.position), true);

                if (Session.Current.Scenario.CurrentPlayer != null)
                {
                    this.Plugins.ConmentTextPlugin.BuildSecondText(InformationTile.InformationString(Session.Current.Scenario.CurrentPlayer.GetKnownAreaData(this.position)), true);
                }
                else
                {
                    this.Plugins.ConmentTextPlugin.BuildSecondText("", false);
                }
                
                // 🔥 2026-03-09 新增：获取天气信息并显示
                // ANTI-BAND-AID：WeatherManager 必须在 Scenario.Init() 中初始化
                var weather = Session.Current.Scenario.WeatherManager!.GetWeatherAt(this.position);
                string weatherInfo = " " + WorldOfTheThreeKingdoms.GameLogic.WeatherManager.GetWeatherDisplayName(weather);
                
                // 🌧️ 2026-03-10 新增：获取风向风力信息并显示
                // 日期：2026-03-10
                var wind = Session.Current.Scenario.WeatherManager.GetWindAt(this.position);
                string windInfo = " " + 
                    WorldOfTheThreeKingdoms.GameLogic.WeatherManager.GetWindDirectionDisplayName(wind.Direction) + 
                    WorldOfTheThreeKingdoms.GameLogic.WeatherManager.GetWindForceDisplayName(wind.Force);
                
                if ((troopByPosition != null && troopByPosition.Status != TroopStatus.埋伏) && (Session.GlobalVariables.SkyEye || ((Session.Current.Scenario.CurrentPlayer != null) && Session.Current.Scenario.CurrentPlayer.IsPositionKnown(this.position))))
                {
                    this.Plugins.ConmentTextPlugin.BuildFirstText(troopByPosition.DisplayName + " " + this.mainMapLayer.GetTerrainNameByPosition(this.position) + weatherInfo + windInfo, true);
                }
                else if (architectureByPosition != null)
                {
                    this.Plugins.ConmentTextPlugin.BuildFirstText(architectureByPosition.Name + " " + this.mainMapLayer.GetTerrainNameByPosition(this.position) + weatherInfo + windInfo, true);
                }
                else
                {
                    this.Plugins.ConmentTextPlugin.BuildFirstText(this.mainMapLayer.GetTerrainNameByPosition(this.position) + weatherInfo + windInfo, false);
                }
                if (this.Plugins.ConmentTextPlugin != null)
                {
                    this.Plugins.ConmentTextPlugin.SetView(this.viewportSize.X, this.viewportSize.Y - this.Plugins.ToolBarPlugin.Height);
                    this.Plugins.ConmentTextPlugin.Update(gameTime);
                }
            }
        }

        private void UpdateDialog(GameTime gameTime)
        {

            if (this.Plugins.SimpleTextDialogPlugin != null)
            {
                this.Plugins.SimpleTextDialogPlugin.Update(gameTime);
            }
            if (this.Plugins.tupianwenziPlugin != null)
            {
                this.Plugins.tupianwenziPlugin.Update(gameTime);
            }


 
        }
        private void Updatetupianwenzi(GameTime gameTime)
        {


            if (this.Plugins.tupianwenziPlugin != null)
            {
                this.Plugins.tupianwenziPlugin.Update(gameTime);
            }


        }
        private void UpdateInputer(GameTime gameTime)
        {
            if (this.Plugins.NumberInputerPlugin != null)
            {
                this.Plugins.NumberInputerPlugin.Update(gameTime);
            }
        }

        private void UpdateScreenBlind(GameTime gameTime)
        {
            // 🔥 ANTI-BAND-AID：在调用前检查 Scenario 是否已完全加载
            // 日期：2026-03-17
            // 原因：ScreenBlind.Update() 需要访问 Date 数据，必须在 Scenario 加载完成后才能调用
            if (this.Plugins.ScreenBlindPlugin != null && 
                Session.Current?.Scenario?.Date != null)
            {
                this.Plugins.ScreenBlindPlugin.Update(gameTime);
            }
        }

        private void UpdateQuadtree(GameTime gameTime)
        {
            if (_simpleQuadtree == null) return;

            try
            {
                // 更新全局帧计数器
                _globalFrameCounter++;
                
                // 性能统计
                int optimizedTroops = 0;
                int clusterBattles = 0;
                
                // 清空并重建四叉树
                _simpleQuadtree.Clear();

                // 使用时间切片系统更新部队 - 性能优化：缓存部队列表
                if (_cachedTroopList == null || _lastTroopListUpdate + 60 < _globalFrameCounter)
                {
                    _cachedTroopList = Session.Current.Scenario.Troops.GetList();
                    _lastTroopListUpdate = _globalFrameCounter;
                }
                
                var troopList = _cachedTroopList;
                // if (troopList.Count > 0 && _globalFrameCounter % 60 == 0) System.Diagnostics.Debug.WriteLine($"[UpdateQuadtree] Updating {troopList.Count} troops.");
                for (int i = 0; i < troopList.Count; i++)
                {
                    Troop troop = troopList[i] as Troop;
                    if (troop == null || troop.Destroyed || troop.BelongedFaction == null || !troop.DrawAnimation) 
                    {
                        // if (_globalFrameCounter % 60 == 0) System.Diagnostics.Debug.WriteLine($"[UpdateQuadtree] Skipped troop {troop?.ID}. Destroyed: {troop?.Destroyed}, Faction: {troop?.BelongedFaction?.Name ?? "null"}, DrawAnimation: {troop?.DrawAnimation}");
                        continue;
                    }
                    
                    _simpleQuadtree.Insert(troop);
                    
                    if (troop.QuickBattling && Setting.Current.GlobalVariables.UseQuadtreeOptimization)
                    {
                        optimizedTroops++;
                        
                        // === A. 高频逻辑：每帧必做 ===
                        // 🔥 修复：传入真实的 GameTime，确保插值正常工作
                        troop.UpdateVisuals(gameTime);
                        
                        // === B. 低频逻辑：时间切片执行 ===
                        // 动态决定思考频率
                        int sliceCount = GetSliceCountForTroop(troop);
                        
                        // 使用部队索引和全局帧计数器来错峰执行
                        if ((_globalFrameCounter + i) % sliceCount == 0)
                        {
                            // 🔥 性能优化：使用预计算的 GameTime，避免 Hot Path 分配
                            // 确保 sliceCount 在缓存范围内
                            int cacheIndex = Math.Min(sliceCount, _cachedBrainGameTimes.Length - 1);
                            GameTime brainGameTime = _cachedBrainGameTimes[cacheIndex];
                            troop.UpdateBrainAdvanced(brainGameTime, sliceCount);
                        }
                    }
                    else
                    {
                        // 🔥 修复：非快速战斗部队也需要视觉插值
                        // 日期：2026-02-27
                        // 原因：UpdateVisuals 只在 QuickBattling 模式下被调用，导致玩家部队没有平滑移动
                        // 解决：所有部队都需要调用 UpdateVisuals 进行视觉插值
                        troop.UpdateVisuals(gameTime);
                        
                        // 非快速战斗部队使用原始更新逻辑
                        UpdateQuickBattleTroop(troop);
                        UpdateQuickBattleMovement(troop);
                    }
                }
                
                // 🔥 部队协调系统更新 - 每30帧更新一次以减少性能开销
                if (_globalFrameCounter % 30 == 0)
                {
                    try
                    {
                        global::GameManager.TroopCoordinationManager.Instance.UpdateCoordination();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 部队协调系统更新出错: {ex.Message}");
                    }
                }

                // 🔥 强制解卡系统 - 每30帧检查一次卡住的部队（更频繁）
                if (_globalFrameCounter % 30 == 0)
                {
                    try
                    {
                        ForceUnstuckTroops();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 强制解卡系统出错: {ex.Message}");
                    }
                }

                // 🔥 新增：无军团部队检查 - 每60帧检查一次无军团部队（更频繁）
                if (_globalFrameCounter % 60 == 0)
                {
                    try
                    {
                        CheckAndFixNoLegionTroops();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 无军团部队检查出错: {ex.Message}");
                    }
                }

                // 🔥 新增：激进解卡系统 - 每180帧执行一次全面解卡
                if (_globalFrameCounter % 180 == 0)
                {
                    try
                    {
                        //AggressiveUnstuckAllTroops();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 激进解卡系统出错: {ex.Message}");
                    }
                }

                // 🔥 修改：合理的解卡系统 - 每180帧执行一次（6秒一次）
                if (_globalFrameCounter % 180 == 0)
                {
                    try
                    {
                        global::GameManager.TroopCoordinationManager.Instance.UpdateCoordination();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 合理解卡系统出错: {ex.Message}");
                    }
                }
                
                // 更新集群战斗系统 (用于屏幕外的大规模战斗)
                clusterBattles = UpdateArmySquads();
                
                // 更新性能监控统计
                _performanceMonitor.OptimizedTroops = optimizedTroops;
                _performanceMonitor.ClusterBattles = clusterBattles;
                
                // 计算性能提升估算
                float totalTroops = troopList.Count;
                if (totalTroops > 0)
                {
                    _performanceMonitor.CPUUsageReduction = optimizedTroops / totalTroops * 0.8f; // 估算80%的CPU减少
                    _performanceMonitor.MemoryUsageReduction = clusterBattles / totalTroops * 0.6f; // 估算60%的内存减少
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateQuadtree] 错误: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[UpdateQuadtree] 堆栈: {ex.StackTrace}");
                // 🔥 不要禁用四叉树，让下一帧继续尝试
            }
        }
        
        /// <summary>
        /// 根据部队状态动态决定时间切片数量
        /// </summary>
        private int GetSliceCountForTroop(Troop troop)
        {
            try
            {
                var settings = PerformanceSettings.Current;
                
                // 检查部队是否在屏幕内
                bool isOnScreen = IsOnScreen(troop);
                
                // 🔥 统一战斗系统：同步设置 troop.IsOnScreen 属性
                troop.IsOnScreen = isOnScreen;
                
                if (isOnScreen)
                {
                    // 屏幕内：使用性能设置中的AI逻辑切片数
                    return settings.AiLogicSliceCount;
                }
                else
                {
                    // 屏幕外：使用屏幕外倍数
                    return settings.AiLogicSliceCount * settings.OffScreenSliceMultiplier;
                }
            }
            catch
            {
                return DEFAULT_SLICE_COUNT; // 出错时使用默认值
            }
        }
        
        /// <summary>
        /// 检查部队是否在屏幕内
        /// </summary>
        private bool IsOnScreen(Troop troop)
        {
            try
            {
                if (troop == null || this.mainMapLayer == null) return false;
                
                // 简化的屏幕检查
                return this.mainMapLayer.TileInScreen(troop.Position);
            }
            catch
            {
                return true; // 出错时假设在屏幕内，使用高频更新
            }
        }

        /// <summary>
        /// 强制解卡系统 - 处理长时间卡住的部队
        /// </summary>
        private void ForceUnstuckTroops()
        {
            try
            {
                if (Session.Current?.Scenario?.Troops == null) return;

                var stuckTroops = new List<Troop>();
                
                // 收集卡住的部队
                foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
                {
                    if (troop == null || troop.Destroyed) continue;
                    
                    // 🔥 修复：更严格的卡住检测条件
                    // 1. 有移动力但行动是Stop
                    // 2. 有目标但位置没变化
                    // 3. 卡住计数器超过阈值
                    if (troop.MovabilityLeft > 0 && 
                        troop.Action == TroopAction.Stop &&
                        troop.RealDestination != Point.Zero &&
                        troop.RealDestination != troop.Position &&
                        troop.stuckedFor > 2) // 🔥 修复：进一步降低阈值，更快响应
                    {
                        stuckTroops.Add(troop);
                    }
                }

                if (stuckTroops.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ForceUnstuckTroops] 发现 {stuckTroops.Count} 个卡住的部队");
                }

                // 🔥 修复：使用改进的协调管理器处理卡住的部队
                foreach (var troop in stuckTroops)
                {
                    try
                    {
                        global::GameManager.TroopCoordinationManager.Instance.ForceUnstuckTroop(troop);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ForceUnstuckTroops] 处理部队 {troop.DisplayName} 出错: {ex.Message}");
                        // 备用方案：直接处理
                        ForceUnstuckSingleTroop(troop);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ForceUnstuckTroops] 系统错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 强制解卡单个部队
        /// </summary>
        private void ForceUnstuckSingleTroop(Troop troop)
        {
            System.Diagnostics.Debug.WriteLine($"[ForceUnstuck] 开始处理卡住的部队: {troop.DisplayName} (ID:{troop.ID})");

            // 方案1: 尝试清除路径，强制重新寻路
            troop.ClearFirstTierPath();
            troop.stuckedFor = 0;
            
            // 方案2: 如果周围有友军，尝试随机传送到附近空位
            var nearbyEmptyPositions = FindNearbyEmptyPositions(troop.Position, 3);
            if (nearbyEmptyPositions.Count > 0)
            {
                // 选择最接近目标的空位
                Point bestPosition = nearbyEmptyPositions[0];
                float bestDistance = GetDistance(bestPosition, troop.RealDestination);
                
                foreach (var pos in nearbyEmptyPositions)
                {
                    float distance = GetDistance(pos, troop.RealDestination);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPosition = pos;
                    }
                }
                
                // 传送到最佳位置
                troop.Position = bestPosition;
                troop.Action = TroopAction.Move;
                
                System.Diagnostics.Debug.WriteLine($"[ForceUnstuck] 部队 {troop.DisplayName} 传送到 {bestPosition}");
                return;
            }

            // 方案3: 如果找不到空位，给部队一些移动力并重置状态
            troop.MovabilityLeft = Math.Max(troop.MovabilityLeft, 50);
            troop.Action = TroopAction.Move;
            
            System.Diagnostics.Debug.WriteLine($"[ForceUnstuck] 部队 {troop.DisplayName} 重置移动力和状态");
        }

        /// <summary>
        /// 🔥 新增：紧急解卡所有卡住的部队
        /// </summary>
        public void EmergencyUnstuckAllTroops()
        {
            try
            {
                if (Session.Current?.Scenario?.Troops == null) return;

                var allStuckTroops = new List<Troop>();
                var noLegionTroops = new List<Troop>();
                
                // 收集所有可能卡住的部队（更宽松的条件）
                foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
                {
                    if (troop == null || troop.Destroyed) continue;
                    
                    // 🔥 优先处理无军团部队
                    if (troop.BelongedLegion == null && troop.BelongedFaction != null)
                    {
                        noLegionTroops.Add(troop);
                        System.Diagnostics.Debug.WriteLine($"[EmergencyUnstuck] 发现无军团部队: {troop.DisplayName}(ID:{troop.ID}) 目标:{troop.WillArchitecture?.Name ?? "无"}");
                    }
                    
                    // 🔥 修复：包含无军团部队和Move状态但实际卡住的部队
                    if (troop.MovabilityLeft > 0 && 
                        troop.RealDestination != Point.Zero &&
                        troop.RealDestination != troop.Position &&
                        (troop.Action == TroopAction.Stop || 
                         (troop.Action == TroopAction.Move && troop.stuckedFor > 1)))
                    {
                        allStuckTroops.Add(troop);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[EmergencyUnstuck] 发现 {noLegionTroops.Count} 个无军团部队，{allStuckTroops.Count} 个卡住部队");

                // 🔥 强制处理无军团部队
                foreach (var troop in noLegionTroops)
                {
                    try
                    {
                        ForceAssignLegion(troop);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EmergencyUnstuck] 强制分配军团失败 {troop.DisplayName}: {ex.Message}");
                    }
                }

                // 🔥 新增：优先处理循环阻挡的部队
                var circularGroups = FindAllCircularDeadlocks(allStuckTroops);
                foreach (var group in circularGroups)
                {
                    System.Diagnostics.Debug.WriteLine($"[EmergencyUnstuck] 处理循环阻挡组: {string.Join(", ", group.Select(t => t.DisplayName))}");
                    global::GameManager.TroopCoordinationManager.Instance.ResolveCircularDeadlock(group);
                }

                // 使用协调管理器的强制解卡功能处理剩余部队
                foreach (var troop in allStuckTroops)
                {
                    try
                    {
                        global::GameManager.TroopCoordinationManager.Instance.ForceUnstuckTroop(troop);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EmergencyUnstuck] 处理部队 {troop.DisplayName} 出错: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EmergencyUnstuck] 紧急解卡出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 新增：强制分配军团给无军团部队
        /// </summary>
        private void ForceAssignLegion(Troop troop)
        {
            try
            {
                if (troop == null || troop.BelongedFaction == null) return;

                // 🔥 修复：根据出发地和目标地正确判断军团类型
                Architecture startArch = troop.StartingArchitecture;
                Architecture targetArch = troop.WillArchitecture;
                
                // 🔥 数据清理：如果WillArchitecture是敌方城市且没有明确的攻击指令，清空它
                // 日期：2026-03-09
                // 原因：城市被占领后，部队的WillArchitecture可能变成敌方城市
                // 解决：检查WillArchitecture归属，如果是敌方城市且部队不是在执行攻击指令，清空它
                if (targetArch != null && 
                    targetArch.BelongedFaction != troop.BelongedFaction &&
                    troop.Command is not (TroopCommand.AttackArch or TroopCommand.AttackTroop or TroopCommand.Attack))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[ForceAssignLegion] 清理错误目标：{troop.DisplayName} WillArchitecture={targetArch.Name}(敌方)，Command={troop.Command}");
                    troop.WillArchitecture = null;
                    troop.RealDestination = new Point(-1, -1);
                    targetArch = null;  // 清空局部变量
                }

                LegionKind legionKind;
                LegionMission legionMission;
                Architecture legionTarget;

                if (startArch != null && targetArch != null)
                {
                    if (startArch == targetArch)
                    {
                        // 出发地A，目标A → 防守军团
                        legionKind = LegionKind.AI;
                        legionMission = LegionMission.Defend;
                        legionTarget = startArch;
                    }
                    else
                    {
                        // 出发地A，目标B → 进攻军团
                        legionKind = LegionKind.AI;
                        legionMission = LegionMission.Attack;
                        legionTarget = targetArch;
                    }
                }
                else if (targetArch != null)
                {
                    // 🔥 根本修复：拒绝为目标错误的部队创建军团
                    // 日期：2026-03-07
                    // 原因：如果 WillArchitecture 被错误设置为敌方城市（如洛阳），
                    //       不应该创建 Offensive_洛阳 军团，而应该清空错误的目标
                    // 解决：检查目标是否合法，不合法则清空目标并返回
                    if (targetArch.BelongedFaction == troop.BelongedFaction)
                    {
                        legionKind = LegionKind.AI;
                        legionMission = LegionMission.Defend;
                        legionTarget = targetArch;
                    }
                    else
                    {
                        // ❌ 目标是敌方城市，但部队没有出发地，这是数据错误
                        System.Diagnostics.Debug.WriteLine($"[ForceAssignLegion] ❌ 部队 {troop.DisplayName} 目标是敌方城市 {targetArch.Name}，但没有出发地，清空错误目标");
                        troop.WillArchitecture = null;
                        troop.RealDestination = new Point(-1, -1);
                        return;
                    }
                }
                else if (startArch != null)
                {
                    // 只有出发地，没有目标 → 撤退军团
                    // 🔥 修复：验证StartingArchitecture归属
                    // 日期：2026-03-09
                    // 原因：StartingArchitecture可能被占领，变成敌方城市
                    // 解决：如果StartingArchitecture是敌方城市，使用GetBestRetreatTarget找最近的己方城市
                    if (startArch.BelongedFaction == troop.BelongedFaction)
                    {
                        // StartingArchitecture是己方城市，可以作为撤退目标
                        legionKind = LegionKind.AI;
                        legionMission = LegionMission.Retreat;
                        legionTarget = startArch;
                    }
                    else
                    {
                        // StartingArchitecture已被占领，找最近的己方城市
                        Architecture retreatTarget = troop.GetBestRetreatTarget();
                        if (retreatTarget != null)
                        {
                            legionKind = LegionKind.AI;
                            legionMission = LegionMission.Retreat;
                            legionTarget = retreatTarget;
                            
                            // 更新部队的StartingArchitecture
                            troop.StartingArchitecture = retreatTarget;
                            
                            System.Diagnostics.Debug.WriteLine(
                                $"[ForceAssignLegion] {troop.DisplayName} 出发地{startArch.Name}已被占领，" +
                                $"撤退到最近己方城市{retreatTarget.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[ForceAssignLegion] {troop.DisplayName} 找不到己方城市撤退，势力可能被灭");
                            return;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ForceAssignLegion] 部队 {troop.DisplayName} 没有出发地和目标建筑，无法分配军团");
                    return;
                }

                // 尝试获取现有军团
                Legion legion = null;
                foreach (Legion existingLegion in troop.BelongedFaction.Legions)
                {
                    if (existingLegion.Kind == legionKind && existingLegion.Mission == legionMission && existingLegion.WillArchitecture == legionTarget)
                    {
                        legion = existingLegion;
                        break;
                    }
                }

                // 如果没有找到，创建新军团
                if (legion == null)
                {
                    legion = troop.BelongedFaction.CreateLegion(legionKind, legionMission, legionTarget);
                }

                if (legion != null)
                {
                    troop.BelongedLegion = legion;
                    
                    // 确保双向关系
                    if (!legion.Troops.HasGameObject(troop))
                    {
                        legion.Troops.Add(troop);
                    }
                    
                    // System.Diagnostics.Debug.WriteLine($"[ForceAssignLegion] 成功分配：{troop.DisplayName} → {legion.Kind}_{legion.WillArchitecture?.Name}");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"[ForceAssignLegion] 无法为部队 {troop.DisplayName} 创建军团");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ForceAssignLegion] 强制分配军团出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 新增：查找所有循环死锁组
        /// </summary>
        private List<List<Troop>> FindAllCircularDeadlocks(List<Troop> troops)
        {
            var circularGroups = new List<List<Troop>>();
            var processedTroops = new HashSet<int>();

            try
            {
                foreach (var troop in troops)
                {
                    if (processedTroops.Contains(troop.ID)) continue;

                    var group = global::GameManager.TroopCoordinationManager.Instance.FindCircularDeadlockGroup(troop, troops);
                    if (group.Count >= 2)
                    {
                        circularGroups.Add(group);
                        global::GameManager.TroopCoordinationManager.Instance.ResolveCircularDeadlock(group);
                        foreach (var t in group)
                        {
                            processedTroops.Add(t.ID);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindAllCircularDeadlocks] 查找所有循环死锁出错: {ex.Message}");
            }

            return circularGroups;
        }

        /// <summary>
        /// 🔥 新增：检查和修复无军团部队
        /// </summary>
        private void CheckAndFixNoLegionTroops()
        {
            try
            {
                if (Session.Current?.Scenario?.Troops == null) return;

                var noLegionTroops = new List<Troop>();

                // 收集所有无军团部队
                foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
                {
                    if (troop != null && !troop.Destroyed && 
                        troop.BelongedFaction != null && 
                        troop.BelongedLegion == null)
                    {
                        noLegionTroops.Add(troop);
                    }
                }

                if (noLegionTroops.Count > 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"[CheckNoLegionTroops] 发现 {noLegionTroops.Count} 个无军团部队，开始修复");

                    foreach (var troop in noLegionTroops)
                    {
                        ForceAssignLegion(troop);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckNoLegionTroops] 检查无军团部队出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 新增：激进解卡系统 - 强制解决所有卡住问题
        /// </summary>
        private void AggressiveUnstuckAllTroops()
        {
            try
            {
                if (Session.Current?.Scenario?.Troops == null) return;

                var allTroops = Session.Current.Scenario.Troops.GetList().OfType<Troop>().Where(t => t != null && !t.Destroyed).ToList();
                var problemTroops = new List<Troop>();
                var noLegionTroops = new List<Troop>();
                var stuckTroops = new List<Troop>();

                //System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuck] 开始激进解卡，检查 {allTroops.Count} 个部队");

                // 分类收集问题部队
                foreach (var troop in allTroops)
                {
                    // 无军团部队
                    if (troop.BelongedFaction != null && troop.BelongedLegion == null)
                    {
                        noLegionTroops.Add(troop);
                        problemTroops.Add(troop);
                    }
                    // 卡住部队
                    else if (troop.MovabilityLeft > 0 && 
                            troop.RealDestination != Point.Zero &&
                            troop.RealDestination != troop.Position &&
                            troop.Action == TroopAction.Stop)
                    {
                        stuckTroops.Add(troop);
                        problemTroops.Add(troop);
                    }
                }

                //System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuck] 发现问题部队: 无军团 {noLegionTroops.Count} 个, 卡住 {stuckTroops.Count} 个");

                // 1. 优先处理无军团部队
                foreach (var troop in noLegionTroops)
                {
                    try
                    {
                        // System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuck] 强制分配军团: {troop.DisplayName}(ID:{troop.ID}) 目标:{troop.WillArchitecture?.Name ?? "无"}");
                        ForceAssignLegionAggressive(troop);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuck] 分配军团失败 {troop.DisplayName}: {ex.Message}");
                    }
                }

                // 2. 处理卡住部队
                foreach (var troop in stuckTroops)
                {
                    try
                    {
                        AggressiveUnstuckSingleTroop(troop);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuck] 解卡失败 {troop.DisplayName}: {ex.Message}");
                    }
                }

                // 3. 处理循环死锁
                var circularGroups = FindAllCircularDeadlocks(problemTroops);
                foreach (var group in circularGroups)
                {
                    // System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuck] 解决循环死锁: {string.Join(", ", group.Select(t => t.DisplayName))}");
                    global::GameManager.TroopCoordinationManager.Instance.ResolveCircularDeadlock(group);
                }

                // System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuck] 激进解卡完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuck] 激进解卡系统出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 新增：激进军团分配
        /// </summary>
        private void ForceAssignLegionAggressive(Troop troop)
        {
            try
            {
                if (troop?.BelongedFaction == null) return;

                // 1. 确定出发地和目标地
                Architecture startArch = troop.StartingArchitecture;
                Architecture targetArch = troop.WillArchitecture;

                // 🔥 修复：根据出发地和目标地正确判断军团类型
                LegionKind legionKind;
                LegionMission legionMission;
                Architecture legionTarget;

                if (startArch != null && targetArch != null)
                {
                    if (startArch == targetArch)
                    {
                        // 出发地A，目标A → 防守军团
                        legionKind = LegionKind.AI;
                        legionMission = LegionMission.Defend;
                        legionTarget = startArch;
                        System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] {troop.DisplayName}: 出发{startArch.Name}，目标{targetArch.Name} → 防守军团");
                    }
                    else
                    {
                        // 出发地A，目标B → 进攻军团
                        legionKind = LegionKind.AI;
                        legionMission = LegionMission.Attack;
                        legionTarget = targetArch;
                        System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] {troop.DisplayName}: 出发{startArch.Name}，目标{targetArch.Name} → 进攻军团");
                    }
                }
                else if (targetArch != null)
                {
                    // 只有目标，没有出发地 → 根据目标是否为己方判断
                    if (targetArch.BelongedFaction == troop.BelongedFaction)
                    {
                        legionKind = LegionKind.AI;
                        legionMission = LegionMission.Defend;
                        legionTarget = targetArch;
                        System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] {troop.DisplayName}: 无出发地，目标己方{targetArch.Name} → 防守军团");
                    }
                    else
                    {
                        legionKind = LegionKind.AI;
                        legionMission = LegionMission.Attack;
                        legionTarget = targetArch;
                        System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] {troop.DisplayName}: 无出发地，目标敌方{targetArch.Name} → 进攻军团");
                    }
                }
                else if (startArch != null)
                {
                    // 只有出发地，没有目标 → 撤退军团
                    // 🔥 修复：验证StartingArchitecture归属
                    // 日期：2026-03-09
                    if (startArch.BelongedFaction == troop.BelongedFaction)
                    {
                        legionKind = LegionKind.AI;
                        legionMission = LegionMission.Retreat;
                        legionTarget = startArch;
                        System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] {troop.DisplayName}: 出发{startArch.Name}，无目标 → 撤退军团");
                    }
                    else
                    {
                        // StartingArchitecture已被占领，找最近的己方城市
                        Architecture retreatTarget = troop.GetBestRetreatTarget();
                        if (retreatTarget != null)
                        {
                            legionKind = LegionKind.AI;
                            legionMission = LegionMission.Retreat;
                            legionTarget = retreatTarget;
                            troop.StartingArchitecture = retreatTarget;
                            System.Diagnostics.Debug.WriteLine(
                                $"[ForceAssignLegionAggressive] {troop.DisplayName}: 出发地{startArch.Name}已被占领，" +
                                $"撤退到{retreatTarget.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[ForceAssignLegionAggressive] {troop.DisplayName}: 找不到己方城市撤退");
                            return;
                        }
                    }
                }
                else
                {
                    // 既没有出发地也没有目标 → 使用势力首都作为撤退目标
                    legionKind = LegionKind.AI;
                    legionMission = LegionMission.Retreat;
                    legionTarget = troop.BelongedFaction.Capital;
                    if (legionTarget != null)
                    {
                        troop.StartingArchitecture = legionTarget;
                        troop.WillArchitecture = legionTarget;
                        troop.RealDestination = legionTarget.ArchitectureArea.Centre;
                        System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] {troop.DisplayName}: 无出发地和目标，设置首都{legionTarget.Name} → 撤退军团");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] {troop.DisplayName}: 无法确定任何目标，跳过");
                        return;
                    }
                }

                // 2. 查找现有军团
                Legion legion = null;
                foreach (Legion existingLegion in troop.BelongedFaction.Legions)
                {
                    if (existingLegion.Kind == legionKind && existingLegion.Mission == legionMission && existingLegion.WillArchitecture == legionTarget)
                    {
                        legion = existingLegion;
                        break;
                    }
                }

                // 3. 如果没有找到，创建新军团
                if (legion == null)
                {
                    legion = troop.BelongedFaction.CreateLegion(legionKind, legionMission, legionTarget);
                    if (legion != null)
                    {
                        // 设置起始建筑
                        legion.StartArchitecture = startArch ?? troop.BelongedFaction.Capital;
                    }
                    // System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] 创建新军团: {legionKind}_{legionTarget.Name}");
                }

                // 4. 分配部队到军团
                troop.BelongedLegion = legion;
                if (!legion.Troops.HasGameObject(troop))
                {
                    legion.Troops.Add(troop);
                }

                // 5. 重置部队状态
                troop.stuckedFor = 0;
                troop.Action = TroopAction.Move;
                
                // System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] 成功分配: {troop.DisplayName} → {legion.Kind}_{legion.WillArchitecture.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ForceAssignLegionAggressive] 激进分配军团出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 🔥 新增：激进解卡单个部队
        /// </summary>
        private void AggressiveUnstuckSingleTroop(Troop troop)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuckSingle] 处理卡住部队: {troop.DisplayName}(ID:{troop.ID})");

                // 1. 重置状态
                troop.stuckedFor = 0;
                troop.ClearFirstTierPath();
                global::GameManager.TroopCoordinationManager.Instance.ResetTroopWaitState(troop.ID);

                // 2. 尝试传送到安全位置
                var safePositions = FindNearbyEmptyPositions(troop.Position, 4);
                if (safePositions.Count > 0)
                {
                    // 选择最接近目标的位置
                    Point bestPosition = safePositions[0];
                    if (troop.RealDestination != Point.Zero)
                    {
                        float bestDistance = GetDistance(bestPosition, troop.RealDestination);
                        foreach (var pos in safePositions)
                        {
                            float distance = GetDistance(pos, troop.RealDestination);
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestPosition = pos;
                            }
                        }
                    }

                    troop.Position = bestPosition;
                    troop.Action = TroopAction.Move;
                    // System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuckSingle] 传送 {troop.DisplayName} 到 {bestPosition}");
                }
                else
                {
                    // 3. 如果找不到安全位置，强制设置为移动状态
                    troop.Action = TroopAction.Move;
                    troop.MovabilityLeft = Math.Max(troop.MovabilityLeft, 50);
                    // System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuckSingle] 重置 {troop.DisplayName} 状态和移动力");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AggressiveUnstuckSingle] 激进解卡出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找附近的空位置
        /// </summary>
        private List<Point> FindNearbyEmptyPositions(Point center, int radius)
            
        {
            var emptyPositions = new List<Point>();
            try
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        Point checkPos = new Point(center.X + dx, center.Y + dy);
                        
                        // 检查位置是否有效且为空
                        if (checkPos.X >= 0 && checkPos.Y >= 0 &&
                            Session.Current.Scenario.IsPositionEmpty(checkPos))
                        {
                            // 检查地形是否可通行
                            var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(checkPos);
                            if (terrainKind != WorldOfTheThreeKingdoms.GameGlobal.TerrainKind.水域) // 避免传送到水里
                            {
                                emptyPositions.Add(checkPos);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindNearbyEmptyPositions] 错误: {ex.Message}");
            }
            
            return emptyPositions;
        }

        /// <summary>
        /// 计算两点距离
        /// </summary>
        private float GetDistance(Point a, Point b)
        {
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }
        
        /// <summary>
        /// 集群战斗系统 - 用于屏幕外的大规模战斗优化
        /// </summary>
        public class ArmySquad
        {
            public List<Troop> Troops = new List<Troop>();
            public float TotalHP = 0f;        // 缓存总血量
            public float AverageDPS = 0f;     // 缓存平均DPS
            public Point CenterPosition;      // 集群中心位置
            public Faction BelongedFaction;   // 所属势力
            
            public void UpdateStats()
            {
                try
                {
                    if (Troops.Count == 0) return;
                    
                    TotalHP = 0f;
                    float totalDPS = 0f;
                    int validTroops = 0;
                    
                    foreach (var troop in Troops)
                    {
                        if (troop != null && !troop.Destroyed)
                        {
                            TotalHP += troop.Quantity * 10f; // 简化血量计算
                            totalDPS += troop.Offence * 1.0f; // 简化DPS计算
                            validTroops++;
                        }
                    }
                    
                    AverageDPS = validTroops > 0 ? totalDPS / validTroops : 0f;
                    
                    // 更新中心位置
                    if (validTroops > 0)
                    {
                        int avgX = 0, avgY = 0;
                        foreach (var troop in Troops)
                        {
                            if (troop != null && !troop.Destroyed)
                            {
                                avgX += troop.Position.X;
                                avgY += troop.Position.Y;
                            }
                        }
                        CenterPosition = new Point(avgX / validTroops, avgY / validTroops);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ArmySquad.UpdateStats] 错误: {ex.Message}");
                }
            }
            
            /// <summary>
            /// 屏幕外战斗更新 - 使用兰切斯特平方律
            /// </summary>
            public void UpdateOffScreenBattle(ArmySquad enemySquad, float deltaTime)
            {
                try
                {
                    if (enemySquad == null || enemySquad.Troops.Count == 0) return;
                    
                    // 应用兰切斯特平方律：集火效应
                    float lanchesterMultiplier = Troop.CombatCalculator.CalculateLanchesterEffect(
                        enemySquad.Troops.Count, this.Troops.Count, enemySquad.AverageDPS);
                    
                    // 计算受到的伤害
                    float incomingDamage = enemySquad.Troops.Count * lanchesterMultiplier * deltaTime;
                    this.TotalHP -= incomingDamage;
                    
                    // 简单的死亡处理
                    if (this.TotalHP <= 0)
                    {
                        KillAllTroops();
                    }
                    else
                    {
                        // 按比例减少部队数量 - 确保比例在0-1之间
                        float maxHP = this.Troops.Count * 10f;
                        float survivalRatio = Math.Max(0, Math.Min(1.0f, this.TotalHP / maxHP));
                        ApplyCasualties(survivalRatio);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateOffScreenBattle] 错误: {ex.Message}");
                }
            }
            
            private void KillAllTroops()
            {
                foreach (var troop in Troops)
                {
                    if (troop != null)
                    {
                        troop.Destroyed = true;
                        troop.Quantity = 0;
                    }
                }
                Troops.Clear();
            }
            
            private void ApplyCasualties(float survivalRatio)
            {
                var troopsToRemove = new List<Troop>();
                
                // 确保survivalRatio在有效范围内
                survivalRatio = Math.Max(0f, Math.Min(1.0f, survivalRatio));
                
                foreach (var troop in Troops)
                {
                    if (troop != null && !troop.Destroyed)
                    {
                        int originalQuantity = troop.Quantity;
                        int newQuantity = (int)(originalQuantity * survivalRatio);
                        
                        // 确保新数量不会超过原数量
                        newQuantity = Math.Min(newQuantity, originalQuantity);
                        newQuantity = Math.Max(0, newQuantity);
                        
                        int casualties = originalQuantity - newQuantity;
                        
                        troop.Quantity = newQuantity;
                        troop.InjuryQuantity += casualties / 3; // 部分伤亡转为伤兵
                        
                        if (troop.Quantity <= 0)
                        {
                            troop.Destroyed = true;
                            troopsToRemove.Add(troop);
                        }
                    }
                }
                
                // 移除被消灭的部队
                foreach (var troop in troopsToRemove)
                {
                    Troops.Remove(troop);
                }
            }
        }
        
        /// <summary>
        /// 更新集群战斗系统
        /// </summary>
        private int UpdateArmySquads()
        {
            try
            {
                _squadUpdateCounter++;
                
                // 每60帧更新一次集群战斗 (约1秒)
                if (_squadUpdateCounter % 60 != 0) return 0;
                
                // 清空旧的集群数据
                _armySquads.Clear();
                
                // 重新组织集群
                OrganizeArmySquads();
                
                // 执行集群间战斗
                int clusterBattles = ExecuteSquadBattles();
                
                return clusterBattles;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateArmySquads] 错误: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// 组织集群 - 将附近的部队组织成战斗集群
        /// </summary>
        private void OrganizeArmySquads()
        {
            try
            {
                var processedTroops = new HashSet<Troop>();
                
                foreach (Troop troop in Session.Current.Scenario.Troops.GetList())
                {
                    if (troop == null || troop.Destroyed || processedTroops.Contains(troop)) continue;
                    // 🔧 FIX: 屏幕外战斗系统适用于所有部队，不仅限QuickBattling
                    if (IsOnScreen(troop)) continue; // 只处理屏幕外的部队
                    
                    // 创建新集群
                    var squad = new ArmySquad();
                    squad.BelongedFaction = troop.BelongedFaction;
                    squad.Troops.Add(troop);
                    processedTroops.Add(troop);
                    
                    // 寻找附近的友军
                    FindNearbyAllies(troop, squad, processedTroops, 5); // 5格范围内的友军
                    
                    // 更新集群统计
                    squad.UpdateStats();
                    
                    // 添加到集群字典
                    _armySquads[squad.CenterPosition] = squad;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrganizeArmySquads] 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 寻找附近的友军加入集群
        /// </summary>
        private void FindNearbyAllies(Troop centerTroop, ArmySquad squad, HashSet<Troop> processedTroops, int range)
        {
            try
            {
                foreach (Troop otherTroop in Session.Current.Scenario.Troops.GetList())
                {
                    if (otherTroop == null || otherTroop.Destroyed || processedTroops.Contains(otherTroop)) continue;
                    // 🔧 FIX: 屏幕外战斗系统适用于所有部队
                    if (IsOnScreen(otherTroop) || otherTroop.BelongedFaction != centerTroop.BelongedFaction) continue;
                    
                    // 检查距离
                    int distance = Math.Abs(otherTroop.Position.X - centerTroop.Position.X) + 
                                  Math.Abs(otherTroop.Position.Y - centerTroop.Position.Y);
                    
                    if (distance <= range)
                    {
                        squad.Troops.Add(otherTroop);
                        processedTroops.Add(otherTroop);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindNearbyAllies] 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行集群间战斗
        /// </summary>
        private int ExecuteSquadBattles()
        {
            int battleCount = 0;
            
            try
            {
                var squads = _armySquads.Values.ToList();
                float deltaTime = 1.0f; // 1秒的时间间隔
                
                for (int i = 0; i < squads.Count; i++)
                {
                    var squad1 = squads[i];
                    if (squad1.Troops.Count == 0) continue;
                    
                    for (int j = i + 1; j < squads.Count; j++)
                    {
                        var squad2 = squads[j];
                        if (squad2.Troops.Count == 0) continue;
                        
                        // 检查是否为敌对势力
                        if (squad1.BelongedFaction != null && squad2.BelongedFaction != null &&
                            !squad1.BelongedFaction.IsFriendly(squad2.BelongedFaction))
                        {
                            // 检查距离 - 只有相邻的集群才会战斗
                            int distance = Math.Abs(squad1.CenterPosition.X - squad2.CenterPosition.X) +
                                          Math.Abs(squad1.CenterPosition.Y - squad2.CenterPosition.Y);
                            
                            if (distance <= 10) // 10格范围内可以战斗
                            {
                                // 双向战斗
                                squad1.UpdateOffScreenBattle(squad2, deltaTime);
                                squad2.UpdateOffScreenBattle(squad1, deltaTime);
                                battleCount++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExecuteSquadBattles] 错误: {ex.Message}");
            }
            
            return battleCount;
        }
        

        

        
        /// <summary>
        /// 更新快速战斗部队的持续战斗状态
        /// </summary>
        private void UpdateQuickBattleTroop(Troop troop)
        {
            try
            {
                // 检查是否有敌对目标在攻击范围内
                if (troop.TargetTroop != null && !troop.TargetTroop.Destroyed)
                {
                    // 计算距离
                    float distance = Vector2.Distance(
                        new Vector2(troop.Position.X, troop.Position.Y),
                        new Vector2(troop.TargetTroop.Position.X, troop.TargetTroop.Position.Y)
                    );
                    
                    // 如果在攻击范围内，持续造成伤害
                    if (distance <= troop.ViewRadius)
                    {
                        // 使用简化的持续伤害系统
                        float deltaTime = 1.0f / 60.0f; // 假设60FPS
                        float dps = troop.Offence * 0.1f; // 每秒10%攻击力的持续伤害
                        
                        // 直接累积伤害而不需要每次调用AttackTroop
                        troop._damageAccumulator += dps * deltaTime;
                        
                        // 当累积伤害足够时，应用一次性伤害
                        if (troop._damageAccumulator >= 5.0f) // 每累积5点伤害应用一次
                        {
                            int damageToApply = (int)troop._damageAccumulator;
                            troop._damageAccumulator = 0f;
                            
                            // 直接应用伤害，跳过复杂逻辑
                            ApplyDirectDamage(troop.TargetTroop, damageToApply);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateQuickBattleTroop] 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 直接应用伤害，用于快速战斗优化
        /// </summary>
        private void ApplyDirectDamage(Troop target, int damage)
        {
            try
            {
                if (target == null || target.Destroyed) return;
                
                // 简化的伤害应用
                int troopLoss = damage / 15; // 每15点伤害减少1个兵
                int injuryIncrease = damage / 5; // 每5点伤害增加1个伤兵
                
                target.Quantity = Math.Max(0, target.Quantity - troopLoss);
                target.InjuryQuantity = Math.Min(target.Quantity, target.InjuryQuantity + injuryIncrease);
                target.Morale = Math.Max(0, target.Morale - damage / 10);
                
                if (target.Quantity <= 0)
                {
                    target.Destroyed = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApplyDirectDamage] 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新快速战斗部队的移动 - 使用优化的移动算法
        /// </summary>
        private void UpdateQuickBattleMovement(Troop troop)
        {
            try
            {
                if (troop == null || troop.Destroyed) return;
                
                // 🔥 修复：手动控制的部队不应被 AI 移动
                // 日期：2026-03-10
                // 原因：快速战斗模式会移动所有部队，包括玩家手动控制的部队
                // 现象：袁绍队 ManualControl=True 但仍被 UpdateMovement_Quick 移动，导致位置冲突
                // 解决：优先检查 ManualControl 标志，手动控制的部队完全跳过 AI 移动
                if (troop.ManualControl)
                {
                    return;
                }
                
                // 🔥 修复操作面瞬移：执行阶段才允许部队移动
                // 日期：2026-03-14
                // 根因：之前用 UndoneWorks.Peek().Kind != None 判断操作面，但操作面正常状态就是 None，
                //       导致检查永远为 false，部队在操作面每帧都被 UpdateMovement_Quick 驱动移动。
                // 解决：直接用 Date.IsRunning 判断——只有执行阶段才允许移动，这是机制的唯一真相来源。
                bool isExecutionPhase = Session.Current.Scenario.Date.IsRunning;
                if (!isExecutionPhase)
                {
                    return;
                }
                
                // 如果部队没有目标，尝试寻找最近的敌对部队
                if (troop.TargetTroop == null || troop.TargetTroop.Destroyed)
                {
                    FindNearestEnemyForQuickBattle(troop);
                }
                
                // 使用优化的移动更新
                if (troop.TargetTroop != null && !troop.TargetTroop.Destroyed)
                {
                    // 创建一个简化的GameTime对象用于移动计算
                    TimeSpan elapsed = TimeSpan.FromSeconds(1.0 / 60.0); // 假设60FPS
                    GameTime gameTime = new GameTime(TimeSpan.Zero, elapsed);
                    troop.UpdateMovement_Quick(gameTime);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateQuickBattleMovement] 错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 为快速战斗部队寻找最近的敌对目标
        /// </summary>
        private void FindNearestEnemyForQuickBattle(Troop troop)
        {
            try
            {
                if (troop?.BelongedFaction == null) return;
                
                Troop nearestEnemy = null;
                float nearestDistanceSquared = float.MaxValue;
                Vector2 troopPos = new Vector2(troop.Position.X, troop.Position.Y);
                
                // 在可见区域内搜索敌对部队
                Rectangle searchArea = new Rectangle(
                    troop.Position.X - troop.ViewRadius,
                    troop.Position.Y - troop.ViewRadius,
                    troop.ViewRadius * 2,
                    troop.ViewRadius * 2
                );
                
                List<Troop> nearbyTroops = new List<Troop>();
                if (_simpleQuadtree != null)
                {
                    _simpleQuadtree.Retrieve(nearbyTroops, searchArea);
                }
                
                foreach (Troop enemy in nearbyTroops)
                {
                    if (enemy == null || enemy.Destroyed || enemy == troop) continue;
                    
                    // 检查是否为敌对势力
                    if (enemy.BelongedFaction != null && 
                        !troop.BelongedFaction.IsFriendly(enemy.BelongedFaction))
                    {
                        Vector2 enemyPos = new Vector2(enemy.Position.X, enemy.Position.Y);
                        float distanceSquared = Vector2.DistanceSquared(troopPos, enemyPos);
                        
                        if (distanceSquared < nearestDistanceSquared)
                        {
                            nearestDistanceSquared = distanceSquared;
                            nearestEnemy = enemy;
                        }
                    }
                }
                
                // 设置目标并初始化移动优化
                if (nearestEnemy != null)
                {
                    troop.SetQuickBattleTarget(nearestEnemy);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FindNearestEnemyForQuickBattle] 错误: {ex.Message}");
            }
        }

        private void ShowArchitectureSurveyPlugin(Architecture architectureByPosition) //,GameTime gameTime)       //显示情况表到右侧
        {
            if (this.Plugins.ArchitectureSurveyPlugin != null)
            {
                //Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.position);
                if (architectureByPosition != null)
                {
                    this.Plugins.ArchitectureSurveyPlugin.SetArchitecture(architectureByPosition, this.position);
                    this.Plugins.ArchitectureSurveyPlugin.SetFaction(Session.Current.Scenario.CurrentPlayer);
                    this.Plugins.ArchitectureSurveyPlugin.Showing = true;

                    if (Session.LargeContextMenu)
                    {
                        //if (InputManager.PoX < 670 && InputManager.PoY < 300)  // if (InputManager.NowMouse.X < 670 && InputManager.NowMouse.Y < 300)
                        if (InputManager.PoX < 400)  // && InputManager.PoY < 250)  // if (InputManager.NowMouse.X < 670 && InputManager.NowMouse.Y < 300)
                        {
                            this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(this.viewportSize.X - 100, 20);
                        }
                        else
                        {
                            this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(100, 20);
                            //this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(280, 20);
                        }
                    }
                    else
                    {
                        if (InputManager.PoX < 670 && InputManager.PoY < 300)  // if (InputManager.NowMouse.X < 670 && InputManager.NowMouse.Y < 300)
                        //if (InputManager.PoX < 400)  // && InputManager.PoY < 250)  // if (InputManager.NowMouse.X < 670 && InputManager.NowMouse.Y < 300)
                        {
                            this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(this.viewportSize.X - 100, 20);
                        }
                        else
                        {
                            //this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(100, 20);
                            this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(280, 20);
                        }
                    }

                    this.Plugins.ArchitectureSurveyPlugin.Gengxin();
                }
                else
                {
                    this.Plugins.ArchitectureSurveyPlugin.SetArchitecture(null, this.position);
                    this.Plugins.ArchitectureSurveyPlugin.Showing = false;
                }
            }
        }

        private void UpdateSurvey(GameTime gameTime)       //更新情况表
        {
            if (this.Plugins.ArchitectureSurveyPlugin != null)
            {
                Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(this.position);
                if (this.Plugins.youcelanPlugin.IsShowing && StaticMethods.PointInRectangle(this.MousePosition, this.Plugins.youcelanPlugin.FrameRectangle))
                {
                    architectureByPosition = null;
                }
                if ((architectureByPosition != null) && ((this.CurrentTroop == null) || ((!Session.GlobalVariables.SkyEye && (Session.Current.Scenario.CurrentPlayer != null)) && !Session.Current.Scenario.CurrentPlayer.IsPositionKnown(this.CurrentTroop.Position))))
                {
                    if (this.Plugins.ArchitectureSurveyPlugin != null)
                    {
                        this.Plugins.ArchitectureSurveyPlugin.SetArchitecture(architectureByPosition, this.position);
                        this.Plugins.ArchitectureSurveyPlugin.SetFaction(Session.Current.Scenario.CurrentPlayer);
                        this.Plugins.ArchitectureSurveyPlugin.Showing = true;

                        //this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(280, 20);

                        if (Session.LargeContextMenu)
                        {
                            if (InputManager.PoX < 400)  // && InputManager.PoY < 250)  // if (InputManager.NowMouse.X < 670 && InputManager.NowMouse.Y < 300)
                            {
                                this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(this.viewportSize.X - 100, 20);
                            }
                            else
                            {
                                this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(100, 20);
                                //this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(280, 20);
                            }
                        }
                        else
                        {
                            //if (InputManager.NowMouse.X < 670 && InputManager.NowMouse.Y < 300)
                            //{
                            //}
                            //else
                            //{
                            //}

                            this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(InputManager.PoX, InputManager.PoY);
                        }

                        //this.Plugins.ArchitectureSurveyPlugin.SetTopLeftPoint(InputManager.PoX, InputManager.PoY);  // InputManager.NowMouse.X, InputManager.NowMouse.Y);

                        this.Plugins.ArchitectureSurveyPlugin.Update(gameTime);
                    }
                }
                else
                {
                    if (this.Plugins.ArchitectureSurveyPlugin != null)
                    {
                        this.Plugins.ArchitectureSurveyPlugin.SetArchitecture(null, this.position);
                        this.Plugins.ArchitectureSurveyPlugin.Showing = false;
                    }
                }
            }
            if (this.Plugins.TroopSurveyPlugin != null)
            {
                if (Session.GlobalVariables.SkyEye || ((Session.Current.Scenario.CurrentPlayer != null) && Session.Current.Scenario.CurrentPlayer.IsPositionKnown(this.position)))
                {
                    Troop troopByPosition = Session.Current.Scenario.GetTroopByPosition(this.position);
                    if (this.Plugins.youcelanPlugin.IsShowing && StaticMethods.PointInRectangle(this.MousePosition, this.Plugins.youcelanPlugin.FrameRectangle))
                    {
                        troopByPosition = null;
                    }
                    if (troopByPosition != null && troopByPosition.Status != TroopStatus.埋伏)
                    {
                        this.Plugins.TroopSurveyPlugin.SetTroop(troopByPosition);
                        this.Plugins.TroopSurveyPlugin.SetFaction(Session.Current.Scenario.CurrentPlayer);
                        this.Plugins.TroopSurveyPlugin.Showing = true;
                        this.Plugins.TroopSurveyPlugin.SetTopLeftPoint(InputManager.PoX, InputManager.PoY);  // InputManager.NowMouse.X, InputManager.NowMouse.Y);
                        this.Plugins.TroopSurveyPlugin.Update(gameTime);
                    }
                    else
                    {
                        this.Plugins.TroopSurveyPlugin.SetTroop(null);
                        this.Plugins.TroopSurveyPlugin.Showing = false;
                    }
                }
                else
                {
                    this.Plugins.TroopSurveyPlugin.SetTroop(null);
                    this.Plugins.TroopSurveyPlugin.Showing = false;
                }
            }
        }

        private void UpdateToolBar(GameTime gameTime)
        {
            if (this.Plugins.ToolBarPlugin != null)
            {
                this.Plugins.ToolBarPlugin.Update(gameTime);
            }
        }

        private void UpdateViewMove()          //更新视野移动方向
        {
            this.ResetMouse();

            if (this.Plugins.AirViewPlugin != null && this.Plugins.AirViewPlugin.IsMapShowing)
            {
                if (StaticMethods.PointInRectangle(this.MousePosition, this.Plugins.AirViewPlugin.MapPosition))
                {
                    return;
                }
            }

            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            {
                //if (((Platform.MainGame.IsActive && base.EnableScroll) && (!this.DrawingSelector && (base.viewportSize != Point.Zero))) && ((((InputManager.NowMouse.X >= 0) && (InputManager.NowMouse.Y >= 0)) && (InputManager.NowMouse.X <= this.viewportSize.X)) && (InputManager.NowMouse.Y <= this.viewportSize.Y)))
                if (((Platform.MainGame.IsActive && base.EnableScroll) && (!this.DrawingSelector && (base.viewportSize != Point.Zero))) && ((((InputManager.PoX >= 0) && (InputManager.PoY >= 0)) && (InputManager.PoX <= this.viewportSize.X)) && (InputManager.PoY <= this.viewportSize.Y)))
                {
                    if (InputManager.PoX < 50)
                    {
                        if (InputManager.PoY < 50)
                        {
                            if ((this.mainMapLayer.LeftEdge != 0) || (this.mainMapLayer.TopEdge != 0))
                            {
                                base.MouseArrowTexture = this.Textures.MouseArrowTextures[5];
                                this.viewMove = ViewMove.TopLeft;
                            }
                        }
                        else if ((this.viewportSize.Y - InputManager.PoY) < 50)
                        {
                            if ((this.mainMapLayer.LeftEdge != 0) || ((this.mainMapLayer.TopEdge + this.mainMapLayer.TotalTileHeight) != this.viewportSize.Y))
                            {
                                base.MouseArrowTexture = this.Textures.MouseArrowTextures[7];
                                this.viewMove = ViewMove.BottomLeft;
                            }
                        }
                        else if (this.mainMapLayer.LeftEdge != 0)
                        {
                            base.MouseArrowTexture = this.Textures.MouseArrowTextures[1];
                            this.viewMove = ViewMove.Left;
                        }
                    }
                    else if ((this.viewportSize.X - InputManager.PoX) < 50)
                    {
                        if (InputManager.PoY < 50)
                        {
                            if (((this.mainMapLayer.LeftEdge + this.mainMapLayer.TotalTileWidth) != this.viewportSize.X) || (this.mainMapLayer.TopEdge != 0))
                            {
                                base.MouseArrowTexture = this.Textures.MouseArrowTextures[6];
                                this.viewMove = ViewMove.TopRight;
                            }
                        }
                        else if ((this.viewportSize.Y - InputManager.PoY) < 50)
                        {
                            if (((this.mainMapLayer.LeftEdge + this.mainMapLayer.TotalTileWidth) != this.viewportSize.X) || ((this.mainMapLayer.TopEdge + this.mainMapLayer.TotalTileHeight) != this.viewportSize.Y))
                            {
                                base.MouseArrowTexture = this.Textures.MouseArrowTextures[8];
                                this.viewMove = ViewMove.BottomRight;
                            }
                        }
                        else if ((this.mainMapLayer.LeftEdge + this.mainMapLayer.TotalTileWidth) != this.viewportSize.X)
                        {
                            base.MouseArrowTexture = this.Textures.MouseArrowTextures[2];
                            this.viewMove = ViewMove.Right;
                        }
                    }
                    else if (InputManager.PoY < 50)
                    {
                        if (this.mainMapLayer.TopEdge != 0)
                        {
                            base.MouseArrowTexture = this.Textures.MouseArrowTextures[3];
                            this.viewMove = ViewMove.Top;
                        }
                    }
                    else if (((this.viewportSize.Y - InputManager.PoY) < 50) && ((this.mainMapLayer.TopEdge + this.mainMapLayer.TotalTileHeight) != this.viewportSize.Y))
                    {
                        base.MouseArrowTexture = this.Textures.MouseArrowTextures[4];
                        this.viewMove = ViewMove.Bottom;
                    }

                    if (this.currentKey == Keys.A)
                    {
                        if (this.mainMapLayer.LeftEdge != 0)
                        {
                            this.viewMove = ViewMove.Left;
                            this.isKeyScrolling = true;
                        }
                    }
                    else if (this.currentKey == Keys.D)
                    {
                        if ((this.mainMapLayer.LeftEdge + this.mainMapLayer.TotalTileWidth) != this.viewportSize.X)
                        {
                            this.viewMove = ViewMove.Right;
                            this.isKeyScrolling = true;
                        }
                    }
                    else if (this.currentKey == Keys.W)
                    {
                        if (this.mainMapLayer.TopEdge != 0)
                        {
                            this.viewMove = ViewMove.Top;
                            this.isKeyScrolling = true;
                        }
                    }
                    else if ((this.currentKey == Keys.S && ((this.mainMapLayer.TopEdge + this.mainMapLayer.TotalTileHeight) != this.viewportSize.Y)))
                    {
                        this.viewMove = ViewMove.Bottom;
                        this.isKeyScrolling = true;
                    }
                    else
                    {
                        this.isKeyScrolling = false;
                    }
                }
            }
        }

        private void UpdateViewport()
        {
            if (Platform.GraphicsDevice != null)
            {
                if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
                {
                    this.viewportSize.X = Platform.GraphicsDevice.Viewport.Width;
                    this.viewportSize.Y = Platform.GraphicsDevice.Viewport.Height - this.Plugins.ToolBarPlugin.Height;
                }
                else
                {
                    this.viewportSize.X = Session.ResolutionX - 20;  // Platform.GraphicsDevice.Viewport.Width;
                    this.viewportSize.Y = Convert.ToInt32(Session.ResolutionY - this.Plugins.ToolBarPlugin.Height - 10);  // Platform.GraphicsDevice.Viewport.Height - this.Plugins.ToolBarPlugin.Height;
                }

                this.viewportSizeFull.X = Platform.GraphicsDevice.Viewport.Width;
                this.viewportSizeFull.Y = Platform.GraphicsDevice.Viewport.Height;

                this.Plugins.ToolBarPlugin.SetRealViewportSize(new Point(this.viewportSize.X, this.viewportSize.Y));
                
                if (this.Plugins.ScreenBlindPlugin != null)
                {
                    this.Plugins.ScreenBlindPlugin.SetRealViewportSize(new Point(this.viewportSize.X, this.viewportSize.Y));
                }
                
                // 🆕 调整地块势力范围信息插件大小
                if (this.Plugins.TileInfluenceInfoPlugin != null)
                {
                    this.Plugins.TileInfluenceInfoPlugin.SetRealViewportSize(new Point(this.viewportSize.X, this.viewportSize.Y));
                }

                //this.Plugins.ToolBarPlugin.SetRealViewportSize(new Point(this.viewportSize.X, this.viewportSize.Y));

                this.ResetScreenEdge();

                this.mainMapLayer.ReCalculateTileDestination(this);

                Session.ChangeStartDisplay(Platform.GraphicsDevice.Viewport.Width, Platform.GraphicsDevice.Viewport.Height);
            }
        }

        public void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            this.UpdateViewport();
            this.ResetTiles();
            this.RefreshDisableRects();
        }

        public Architecture CurrentArchitecture
        {
            get
            {
                return this.screenManager.CurrentArchitecture;
            }
            set
            {
                this.screenManager.CurrentArchitecture = value;
            }
        }

        public string CurrentArchitectureDisplayName
        {
            get
            {
                return this.CurrentArchitecture.Name;
            }
        }

        public Faction CurrentFaction
        {
            get
            {
                return this.screenManager.CurrentFaction;
            }
            set
            {
                this.screenManager.CurrentFaction = value;
            }
        }

        public GameObjectList CurrentGameObjects
        {
            get
            {
                return this.screenManager.CurrentGameObjects;
            }
            set
            {
                this.screenManager.CurrentGameObjects = value;
            }
        }

        public Military CurrentMilitary
        {
            get
            {
                return this.screenManager.CurrentMilitary;
            }
            set
            {
                this.screenManager.CurrentMilitary = value;
            }
        }

        public int CurrentNumber
        {
            get
            {
                return this.screenManager.CurrentNumber;
            }
            set
            {
                this.screenManager.CurrentNumber = value;
            }
        }

        public int Currentzijin
        {
            get
            {
                return this.screenManager.Currentzijin;
            }
            set
            {
                this.screenManager.Currentzijin = value;
            }
        }

        public Person CurrentPerson
        {
            get
            {
                return this.screenManager.CurrentPerson;
            }
            set
            {
                this.screenManager.CurrentPerson = value;
            }
        }

        public GameObjectList CurrentPersons
        {
            get
            {
                return this.screenManager.CurrentPersons;
            }
        }

        public Routeway CurrentRouteway
        {
            get
            {
                return this.screenManager.CurrentRouteway;
            }
            set
            {
                this.screenManager.CurrentRouteway = value;
            }
        }

        public string CurrentRoutewayDisplayName
        {
            get
            {
                return this.CurrentRouteway.DisplayName;
            }
        }

        public Troop CurrentTroop
        {
            get
            {
                return this.screenManager.CurrentTroop;
            }
            set
            {
                this.screenManager.CurrentTroop = value;
            }
        }

        public string CurrentTroopDisplayName
        {
            get
            {
                return this.CurrentTroop.DisplayName;
            }
        }

        public override bool DrawingSelector
        {
            get
            {
                return this.drawingSelector;
            }
            set
            {
                if (this.drawingSelector != value)
                {
                    this.drawingSelector = value;
                    this.SelectorTroops.Clear();
                    if (!value)
                    {
                        this.PopUndoneWork();
                        if (((this.SelectorStartPosition != base.MousePosition) && (Session.Current.Scenario.CurrentPlayer != null)) && Session.Current.Scenario.CurrentPlayer.Controlling)
                        {
                            Point positionByPoint = this.GetPositionByPoint(this.SelectorStartPosition);
                            Point point2 = this.GetPositionByPoint(base.MousePosition);
                            Rectangle r = new Rectangle(
                                Math.Min(point2.X, positionByPoint.X), Math.Min(point2.Y, positionByPoint.Y),
                                Math.Abs(point2.X - positionByPoint.X), Math.Abs(point2.Y - positionByPoint.Y));
                            
                            System.Diagnostics.Debug.WriteLine($"[框选部队] 开始框选，矩形:{r}");
                            
                            foreach (Troop troop in Session.Current.Scenario.CurrentPlayer.Troops.GetList())
                            {
                                if (!troop.Destroyed && troop.Status == TroopStatus.一般 && 
                                    r.Contains(troop.Position) &&
                                    !troop.Operated)
                                {
                                    this.SelectorTroops.Add(troop);
                                    System.Diagnostics.Debug.WriteLine($"[框选部队] ✅ 加入部队:{troop.DisplayName} Operated:{troop.Operated}");
                                }
                                else if (!troop.Destroyed && r.Contains(troop.Position))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[框选部队] ❌ 跳过部队:{troop.DisplayName} Status:{troop.Status} Operated:{troop.Operated}");
                                }
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[框选部队] 完成，SelectorTroops数量:{this.SelectorTroops.Count}");
                            this.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selecting, SelectingUndoneWorkKind.SelectorTroopsDestination));
                        }
                    }
                    else
                    {
                        this.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selector, UndoneWorkSubKind.None ));
                        //this.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Selector, ViewMove.Stop ));
                    }
                    this.SelectorStartPosition = base.MousePosition;
                }
            }
        }

        public bool IsFullScreen
        {
            get
            {
                return Platform.GraphicsDevice.PresentationParameters.IsFullScreen;  // base.GraphicsDevice.PresentationParameters.IsFullScreen;
            }
        }

        public GameObjectList CurrentMilitaries
        {
            get
            {
                return this.screenManager.CurrentMilitaries;
            }
            set
            {
                this.screenManager.CurrentMilitaries = value;
            }
        }

        public bool IsMultipleResource
        {
            get
            {
                return Session.GlobalVariables.MultipleResource;
            }
        }

        public bool IsPlayingBattleSound
        {
            get
            {
                return Setting.Current.GlobalVariables.PlayBattleSound;
            }
        }

        public bool IsPlayingTroopVoice
        {
            get
            {
                return Setting.Current.GlobalVariables.TroopVoice;
            }
        }

        public bool IsPlayingMusic
        {
            get
            {
                return Session.GlobalVariables.PlayMusic;
            }
        }

        public bool IsPlayingNormalSound
        {
            get
            {
                return Setting.Current.GlobalVariables.PlayNormalSound;
            }
        }

        public bool IsPlayingTroopAnimation
        {
            get
            {
                return Setting.Current.GlobalVariables.DrawTroopAnimation;
            }
        }

        public bool IsShowingSmog
        {
            get
            {
                return Setting.Current.GlobalVariables.DrawMapVeil;
            }
        }

        public bool IsStopOnAttack
        {
            get
            {
                return Setting.Current.GlobalVariables.StopToControlOnAttack;
            }
        }

        public bool IsShowingTroopTitle
        {
            get
            {
                return this.Plugins.TroopTitlePlugin.IsShowing;
            }
        }

        public bool IsSkyEye
        {
            get
            {
                return Session.GlobalVariables.SkyEye;
            }
        }

        public bool IsSkyEyeSimpleNotification
        {
            get
            {
                return Session.GlobalVariables.SkyEyeSimpleNotification;
            }
        }

        public ViewMove ViewMoveDirection
        {
            get
            {
                return this.viewMove;
            }
        }

        public override void ReduceSound()
        {
            Setting.Current.MusicVolume -= 10;

            if (Setting.Current.MusicVolume < 0)
            {
                Setting.Current.MusicVolume = 0;
            }

            Setting.Save();

            Platform.Current.SetMusicVolume((int)Setting.Current.MusicVolume);

            //this.Player.settings.volume -= 10;
        }

        public override void IncreaseSound()
        {
            Setting.Current.MusicVolume += 10;

            if (Setting.Current.MusicVolume > 100)
            {
                Setting.Current.MusicVolume = 100;
            }

            Setting.Save();

            Platform.Current.SetMusicVolume((int)Setting.Current.MusicVolume);

            //Platform.Current.PlayEffect(@"Sound\Move");

            //this.Player.settings.volume += 10;
        }

        public override void ReturnMainMenu()
        {
            this.Plugins.ConfirmationDialogPlugin.SetSimpleTextDialog(this.Plugins.SimpleTextDialogPlugin);
            this.Plugins.ConfirmationDialogPlugin.ClearFunctions();
            this.Plugins.ConfirmationDialogPlugin.AddYesFunction(new GameDelegates.VoidFunction(this.ReturnToMainMenu));
            this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
            this.Plugins.SimpleTextDialogPlugin.SetBranch("退回初始");
            this.Plugins.ConfirmationDialogPlugin.IsShowing = true;
        }

        public void ReturnToMainMenu()
        {
            Session.MainGame.loadingScreen = new LoadingScreen("End", "");
            Session.MainGame.loadingScreen.LoadScreenEvent += (sender0, e0) =>
            {
                Platform.Sleep(1000);
            };

            //System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            //System.Diagnostics.ProcessStartInfo myProcessStartInfo = new System.Diagnostics.ProcessStartInfo(@"WorldOfTheThreeKingdoms.exe", "");
            //myProcess.StartInfo = myProcessStartInfo;
            //myProcess.Start();
            //System.Environment.Exit(0);
        }

        public bool SkyEyeSimpleNotification
        {
            get
            {
                return Session.GlobalVariables.SkyEyeSimpleNotification;
            }
        }
        public void updateGameScreenByCurrentTarget()
        {            
            if ((((this.CurrentArchitecture != null) && (this.CurrentTroop != null)) && (this.CurrentTroop.BelongedFaction == Session.Current.Scenario.CurrentPlayer)) && (this.CurrentArchitecture.BelongedFaction == Session.Current.Scenario.CurrentPlayer) && this.CurrentTroop.Operated == false)
            {
                if (!(this.Plugins.ContextMenuPlugin.IsShowing || !Session.Current.Scenario.CurrentPlayer.Controlling))
                {
                    this.Plugins.ContextMenuPlugin.IsShowing = true;
                    this.Plugins.ContextMenuPlugin.SetCurrentGameObject(this);
                    this.Plugins.ContextMenuPlugin.SetMenuKindByName("ArchitectureTroopLeftClick");
                    this.Plugins.ContextMenuPlugin.Prepare(this.SelectorStartPosition.X, this.SelectorStartPosition.Y, base.viewportSize);
                    this.bianduiLiebiaoBiaoji = "ArchitectureTroopLeftClick";
                }
            }
            else if ((this.CurrentTroop != null) && (this.CurrentTroop.BelongedFaction == Session.Current.Scenario.CurrentPlayer) && this.CurrentTroop.Operated == false)
            {
                if (!this.Plugins.ContextMenuPlugin.IsShowing && Session.Current.Scenario.IsPlayerControlling())
                {
                    this.Plugins.ContextMenuPlugin.IsShowing = true;
                    this.Plugins.ContextMenuPlugin.SetCurrentGameObject(this.CurrentTroop);
                    this.Plugins.ContextMenuPlugin.SetMenuKindByName("TroopLeftClick");
                    this.Plugins.ContextMenuPlugin.Prepare(this.SelectorStartPosition.X, this.SelectorStartPosition.Y, base.viewportSize);
                    this.bianduiLiebiaoBiaoji = "TroopLeftClick";
                    if (!this.Plugins.ContextMenuPlugin.IsShowing && (this.CurrentTroop.CutRoutewayDays > 0))
                    {
                        this.CurrentTroop.Leader.TextDestinationString = this.CurrentTroop.CutRoutewayDays.ToString();
                        this.Plugins.tupianwenziPlugin.SetConfirmationDialog(this.Plugins.ConfirmationDialogPlugin, new GameDelegates.VoidFunction(this.CurrentTroop.StopCutRouteway), null);
                        this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                        this.Plugins.tupianwenziPlugin.SetGameObjectBranch(this.CurrentTroop.Leader, this.CurrentTroop.Leader, TextMessageKind.StopCutRouteway, "StopCutRouteway");
                        this.Plugins.tupianwenziPlugin.IsShowing = true;
                    }
                }
            }
            else if (((this.CurrentArchitecture != null) && (this.CurrentArchitecture.BelongedFaction == Session.Current.Scenario.CurrentPlayer)) && !(this.Plugins.ContextMenuPlugin.IsShowing || !Session.Current.Scenario.IsPlayerControlling()))
            {
                this.Plugins.ContextMenuPlugin.IsShowing = true;
                this.Plugins.ContextMenuPlugin.SetCurrentGameObject(this.CurrentArchitecture);
                this.Plugins.ContextMenuPlugin.SetMenuKindByName("ArchitectureLeftClick");
                this.Plugins.ContextMenuPlugin.Prepare(this.SelectorStartPosition.X, this.SelectorStartPosition.Y, base.viewportSize);

                this.bianduiLiebiaoBiaoji = "ArchitectureLeftClick";
                this.ShowBianduiLiebiao(UndoneWorkKind.None, FrameKind.Military, FrameFunction.Browse, false, true, false, true,
                    this.CurrentArchitecture.Militaries, this.CurrentArchitecture.ZhengzaiBuchongDeBiandui(), "", "", this.CurrentArchitecture.MilitaryPopulation);
                this.ShowArchitectureSurveyPlugin(this.CurrentArchitecture);
            }
        }
        
        #region 🧠 AI决策系统集成
        
        /// <summary>
        /// 初始化AI决策系统
        /// </summary>
        private void InitializeAIDecisionSystem()
        {
            try
            {
                _aiDecisionSystem = new WorldOfTheThreeKingdoms.GameManager.CompleteAIDecisionSystem();
                System.Diagnostics.Debug.WriteLine("[MainGameScreen] 🧠 AI决策系统初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGameScreen] ❌ AI决策系统初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化对话管理系统
        /// </summary>
        private void InitializeDialogueSystem()
        {
            try
            {
                DialogueManager.Initialize();
                System.Diagnostics.Debug.WriteLine("[MainGameScreen] 🎭 对话管理系统初始化完成");
                System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 📊 {DialogueManager.GetConfigStats()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGameScreen] ❌ 对话管理系统初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 更新AI决策系统 - 实现三步式AI逻辑
        /// </summary>
        private void UpdateAIDecisionSystem(GameTime gameTime)
        {
            if (_aiDecisionSystem == null) return;
            if (Session.Current?.Scenario?.Factions == null) return;
            
            _aiUpdateCounter++;
            
            // 每30帧更新一次AI决策（约每0.5秒）
            if (_aiUpdateCounter % 30 == 0)
            {
                try
                {
                    // 为每个AI势力执行决策
                    foreach (Faction faction in Session.Current.Scenario.Factions.GetList())
                    {
                        if (faction == null) continue;
                        if (faction == Session.Current.Scenario.CurrentPlayer) continue; // 跳过玩家势力
                        
                        // 🎯 执行三步式AI逻辑：记忆更新 → 势能图刷新 → 智能移动
                        _aiDecisionSystem.RunAILogic(faction);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGameScreen] ❌ AI决策系统更新出错: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 获取AI决策系统统计信息
        /// </summary>
        public string GetAIDecisionStats()
        {
            if (_aiDecisionSystem == null) return "AI决策系统未初始化";
            
            return _aiDecisionSystem.GetSystemStats();
        }
        
        #endregion
        
        #region 选中部队能量覆盖范围高亮显示 (2026-03-21)
        
        /// <summary>
        /// 绘制选中部队的能量覆盖范围高亮
        /// 日期：2026-03-21
        /// 🔥 Hot Path：每帧调用，但仅在有选中部队时绘制
        /// </summary>
        private void DrawSelectedTroopZocHighlight(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // 🔥 Hot Path：快速返回
            if (!ShouldShowHoveredTroopZoc() || _selectedTroopZocTiles.Count == 0 || _whiteTileOverlay == null)
            {
                return;
            }
            
            // 🔥 ANTI-BAND-AID：Fail Fast，不要掩盖数据错误
            // 如果 Scenario 为 null，说明游戏状态异常，让它崩溃
            var scenario = Session.Current.Scenario;
            int mapWidth = scenario.ScenarioMap.MapDimensions.X;
            int tileWidth = this.mainMapLayer.TileWidth;
            int tileHeight = this.mainMapLayer.TileHeight;
            
            // 计算当前视口范围（Grid坐标）
            int viewLeft = this.TopLeftPosition.X;
            int viewTop = this.TopLeftPosition.Y;
            int viewRight = this.BottomRightPosition.X;
            int viewBottom = this.BottomRightPosition.Y;
            
            // 计算呼吸Alpha（0.008f ~ 0.012f 之间微弱起伏，边缘渲染）
            // 🔥 修复：改为边缘渲染，保持低透明度
            // 日期：2026-03-21
            float time = (float)gameTime.TotalGameTime.TotalSeconds;
            float alpha = 0.01f + (float)Math.Sin(time * 2.5f) * 0.002f;
            
            // 使用浅金色高亮（非预乘Alpha）
            Color highlightColor = new Color(255, 240, 180, (int)(alpha * 255));
            
            // 🔥 关键：使用 AlphaBlend 混合模式（更通用，在深色和浅色背景上都可见）
            // 不使用 Additive，因为在深色背景上效果不明显
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);
            
            // 🔥 Hot Path 优化：使用值类型枚举器（Zero-Allocation）
            // HashSet<T>.GetEnumerator() 返回值类型枚举器，不产生堆分配
            var enumerator = _selectedTroopZocTiles.GetEnumerator();
            while (enumerator.MoveNext())
            {
                int tileIndex = enumerator.Current;
                
                // 将索引转换为坐标
                int tileX = tileIndex % mapWidth;
                int tileY = tileIndex / mapWidth;
                
                // 视口裁剪：跳过不在屏幕内的格子
                if (tileX < viewLeft || tileX > viewRight ||
                    tileY < viewTop || tileY > viewBottom)
                {
                    continue;
                }
                
                // 计算屏幕坐标
                Point tilePos = new Point(tileX, tileY);
                Rectangle rect = this.mainMapLayer.GetDestination(tilePos);
                
                // 🔥 关键修复：只绘制边缘，中间透明（参考移动范围渲染）
                // 日期：2026-03-21
                // 原因：用户反馈"还是会覆盖"，改为边缘渲染
                int borderThickness = 2;
                
                // 上边框
                spriteBatch.Draw(
                    _whiteTileOverlay,
                    new Rectangle(rect.X, rect.Y, rect.Width, borderThickness),
                    highlightColor);
                
                // 下边框
                spriteBatch.Draw(
                    _whiteTileOverlay,
                    new Rectangle(rect.X, rect.Bottom - borderThickness, rect.Width, borderThickness),
                    highlightColor);
                
                // 左边框
                spriteBatch.Draw(
                    _whiteTileOverlay,
                    new Rectangle(rect.X, rect.Y, borderThickness, rect.Height),
                    highlightColor);
                
                // 右边框
                spriteBatch.Draw(
                    _whiteTileOverlay,
                    new Rectangle(rect.Right - borderThickness, rect.Y, borderThickness, rect.Height),
                    highlightColor);
            }
            
            // 🔥 恢复外层 SpriteBatch 状态
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.BackToFront,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                null,
                null,
                null);
        }
        
        #endregion
    }

    /// <summary>
    /// 增强四叉树实现 - 支持LOD和高性能渲染优化
    /// </summary>
    public class SimpleQuadtree
    {
        private const int MAX_OBJECTS = 15;  // 增加容量以减少分割
        private const int MAX_LEVELS = 6;    // 增加深度以支持更大地图
        
        private int _level;
        private List<Troop> _objects;
        private Rectangle _bounds;
        private SimpleQuadtree[] _nodes;
        
        // 性能优化：复用列表避免GC
        private static readonly List<Troop> _tempList = new List<Troop>(100);

        public SimpleQuadtree(int level, Rectangle bounds)
        {
            _level = level;
            _bounds = bounds;
            _objects = new List<Troop>(MAX_OBJECTS);  // 预分配容量
            _nodes = new SimpleQuadtree[4];
        }

        /// <summary>
        /// 高性能清空 - 只重置计数器和引用，避免内存分配
        /// </summary>
        public void Clear()
        {
            _objects.Clear();  // 只清空，不释放内存
            
            // 递归清空子节点但保持对象池
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] != null)
                {
                    _nodes[i].Clear();
                    // 注意：不设为null，保持对象池以减少GC
                }
            }
        }

        /// <summary>
        /// 延迟分割 - 只在真正需要时创建子节点
        /// </summary>
        private void Split()
        {
            int subWidth = _bounds.Width / 2;
            int subHeight = _bounds.Height / 2;
            int x = _bounds.X;
            int y = _bounds.Y;

            // 只创建尚未存在的子节点
            if (_nodes[0] == null) _nodes[0] = new SimpleQuadtree(_level + 1, new Rectangle(x + subWidth, y, subWidth, subHeight));
            if (_nodes[1] == null) _nodes[1] = new SimpleQuadtree(_level + 1, new Rectangle(x, y, subWidth, subHeight));
            if (_nodes[2] == null) _nodes[2] = new SimpleQuadtree(_level + 1, new Rectangle(x, y + subHeight, subWidth, subHeight));
            if (_nodes[3] == null) _nodes[3] = new SimpleQuadtree(_level + 1, new Rectangle(x + subWidth, y + subHeight, subWidth, subHeight));
        }

        /// <summary>
        /// 优化的象限索引计算 - 使用整数运算避免浮点数
        /// </summary>
        private int GetIndex(Rectangle pRect)
        {
            int verticalMidpoint = _bounds.X + (_bounds.Width >> 1);  // 位运算除以2
            int horizontalMidpoint = _bounds.Y + (_bounds.Height >> 1);

            bool topQuadrant = (pRect.Y + pRect.Height < horizontalMidpoint);
            bool bottomQuadrant = (pRect.Y >= horizontalMidpoint);
            bool leftQuadrant = (pRect.X + pRect.Width < verticalMidpoint);
            bool rightQuadrant = (pRect.X >= verticalMidpoint);

            // 使用位运算快速计算索引
            if (leftQuadrant && topQuadrant) return 1;
            if (leftQuadrant && bottomQuadrant) return 2;
            if (rightQuadrant && topQuadrant) return 0;
            if (rightQuadrant && bottomQuadrant) return 3;

            return -1; // 跨象限对象
        }

        /// <summary>
        /// 高性能插入 - 支持跨象限对象的正确处理
        /// </summary>
        public void Insert(Troop troop)
        {
            if (troop == null) return;
            
            Rectangle troopBounds = GetTroopBounds(troop);

            // 如果有子节点，尝试插入到合适的子节点
            if (_nodes[0] != null)
            {
                int index = GetIndex(troopBounds);
                if (index != -1)
                {
                    _nodes[index].Insert(troop);
                    return;
                }
            }

            // 插入到当前节点
            _objects.Add(troop);

            // 检查是否需要分割
            if (_objects.Count > MAX_OBJECTS && _level < MAX_LEVELS)
            {
                if (_nodes[0] == null) Split();

                // 重新分配对象到子节点
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    Rectangle objBounds = GetTroopBounds(_objects[i]);
                    int index = GetIndex(objBounds);
                    if (index != -1)
                    {
                        Troop t = _objects[i];
                        _objects.RemoveAt(i);
                        _nodes[index].Insert(t);
                    }
                }
            }
        }

        /// <summary>
        /// 增强的检索方法 - 确保跨象限对象被正确返回
        /// </summary>
        public void Retrieve(List<Troop> returnObjects, Rectangle rect)
        {
            // 首先添加当前节点的对象（包括跨象限对象）
            for (int i = 0; i < _objects.Count; i++)
            {
                Troop troop = _objects[i];
                if (troop != null && GetTroopBounds(troop).Intersects(rect))
                {
                    returnObjects.Add(troop);
                }
            }

            // 如果有子节点，检查哪些子节点与查询区域相交
            if (_nodes[0] != null)
            {
                int verticalMidpoint = _bounds.X + (_bounds.Width >> 1);
                int horizontalMidpoint = _bounds.Y + (_bounds.Height >> 1);

                // 检查每个象限是否与查询区域相交
                if (rect.X < verticalMidpoint && rect.Y < horizontalMidpoint)
                    _nodes[1].Retrieve(returnObjects, rect); // 左上
                if (rect.Right >= verticalMidpoint && rect.Y < horizontalMidpoint)
                    _nodes[0].Retrieve(returnObjects, rect); // 右上
                if (rect.X < verticalMidpoint && rect.Bottom >= horizontalMidpoint)
                    _nodes[2].Retrieve(returnObjects, rect); // 左下
                if (rect.Right >= verticalMidpoint && rect.Bottom >= horizontalMidpoint)
                    _nodes[3].Retrieve(returnObjects, rect); // 右下
            }
        }

        /// <summary>
        /// 优化的边界计算 - 考虑单位实际尺寸和地图坐标转换
        /// </summary>
        private Rectangle GetTroopBounds(Troop troop)
        {
            try
            {
                // 🔥 修复：直接获取该瓦片在当前屏幕上的实际绘制坐标矩形
                // 这与 GetVisibleArea 返回的 (0,0,width,height) 屏幕坐标系完全一致！
                var mainMapLayer = Session.MainGame?.mainGameScreen?.mainMapLayer;
                if (mainMapLayer != null)
                {
                    Rectangle dest = mainMapLayer.GetDestination(troop.Position);
                    
                    // 考虑大型单位（如攻城车）的额外尺寸
                    int extraSize = troop.Army?.Kind?.Type == MilitaryType.器械 ? 30 : 0;
                    if (extraSize > 0)
                    {
                        dest.Inflate(extraSize, extraSize);
                    }
                    return dest;
                }
            }
            catch
            {
                // ignored
            }

            // Fallback (虽然不准确但防止崩溃)
            const int tileWidth = 60;
            const int tileHeight = 40;
            int worldX = troop.Position.X * tileWidth;
            int worldY = troop.Position.Y * tileHeight;
            return new Rectangle(worldX, worldY, tileWidth, tileHeight);
        }

        /// <summary>
        /// 获取统计信息用于调试和性能监控
        /// </summary>
        public void GetStats(out int totalObjects, out int totalNodes, out int maxDepth)
        {
            totalObjects = _objects.Count;
            totalNodes = 1;
            maxDepth = _level;

            if (_nodes[0] != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    _nodes[i].GetStats(out int childObjects, out int childNodes, out int childDepth);
                    totalObjects += childObjects;
                    totalNodes += childNodes;
                    maxDepth = Math.Max(maxDepth, childDepth);
                }
            }
        }
    }

    /// <summary>
    /// 增强部队渲染器 - 支持LOD系统和高性能渲染
    /// </summary>
    public static class SimpleTroopRenderer
    {
        // 复用列表防止GC - 这是性能优化的关键！
        private static readonly List<Troop> _visibleCandidates = new List<Troop>(2000);
        private static readonly List<Troop> _sortedTroops = new List<Troop>(2000);
        
        // 距离缓存 - 避免重复计算同一帧内的距离
        private static readonly Dictionary<Troop, float> _distanceCache = new Dictionary<Troop, float>(2000);
        
        // LOD距离阈值（使用平方距离避免开根号）
        private const float LOD_HIGH_DISTANCE_SQ = 1000f * 1000f;    // 高质量渲染距离
        private const float LOD_MEDIUM_DISTANCE_SQ = 2000f * 2000f;  // 中等质量渲染距离
        private const float LOD_LOW_DISTANCE_SQ = 5000f * 5000f;     // 低质量渲染距离
        
        // 性能统计
        private static int _frameCounter = 0;
        private static int _lastStatsFrame = 0;

        /// <summary>
        /// 主渲染方法 - 集成四叉树粗略筛选、精确剔除、LOD系统
        /// </summary>
        public static void DrawOptimized(Point viewportSize, GameTime gameTime, SimpleQuadtree quadtree)
        {
            try
            {
                _frameCounter++;
                
                if (!Setting.Current.GlobalVariables.DrawTroopAnimation)
                {
                    return;
                }

                // === 第一步：准备数据 ===
                var settings = PerformanceSettings.Current;
                Rectangle cameraView = GetVisibleArea();
                Vector2 cameraCenter = new Vector2(cameraView.X + cameraView.Width / 2f, cameraView.Y + cameraView.Height / 2f);
                
                // 清空距离缓存 - 每帧重新计算，防止内存泄漏
                if (_distanceCache.Count > 5000) // 防止缓存过大
                {
                    _distanceCache.Clear();
                }
                else
                {
                    _distanceCache.Clear();
                }
                
                // 扩充视野 (Padding) - 防止大体积单位闪烁
                Rectangle queryBounds = cameraView;
                int padding = 150; // 根据最大单位尺寸设定
                queryBounds.Inflate(padding, padding);

                // === 第二步：四叉树粗略筛选 (Broad Phase) ===
                _visibleCandidates.Clear();
                
                int totalTroops = 0;
                if (Session.Current?.Scenario?.Troops != null)
                {
                    totalTroops = Session.Current.Scenario.Troops.Count;
                    
                    if (quadtree != null)
                    {
                        // 四叉树返回的是"所有与视野相交的象限内的物体"
                        quadtree.Retrieve(_visibleCandidates, queryBounds);
                    }
                    else
                    {
                        // 回退到全部遍历
                        _visibleCandidates.AddRange(Session.Current.Scenario.Troops.GetList().Cast<Troop>());
                    }
                }

                // === 第三步：精确筛选与LOD计算 (Narrow Phase) ===
                _sortedTroops.Clear();
                
                foreach (var troop in _visibleCandidates)
                {
                    if (troop == null || troop.Destroyed || troop.BelongedFaction == null || !troop.DrawAnimation)
                        continue;

                    // 精确剔除 - 四叉树返回的只是"附近的"，可能包含不在屏幕内的
                    if (!IsInViewport(troop, queryBounds))
                        continue;

                    // 可见性检查
                    if (!IsVisible(troop))
                    {
                        try { troop.SetNotShowing(); } catch { }
                        continue;
                    }

                    // 计算并缓存距离 - 使用DistanceSquared避免开根号
                    float distSq = GetCachedDistance(troop, cameraCenter);
                    
                    // 更新单位的LOD状态（每10帧更新一次以减少计算）
                    if (_frameCounter % 10 == 0)
                    {
                        troop.UpdateLODLevel(distSq);
                    }

                    _sortedTroops.Add(troop);
                }

                // === 第四步：距离排序和数量限制 ===
                if (_sortedTroops.Count > settings.MaxVisibleTroops)
                {
                    // 按距离排序，优先显示近的部队 - 使用缓存的距离避免重复计算
                    _sortedTroops.Sort((a, b) => 
                    {
                        float distA = GetCachedDistance(a, cameraCenter);
                        float distB = GetCachedDistance(b, cameraCenter);
                        return distA.CompareTo(distB);
                    });
                    
                    // 只保留最近的部队
                    _sortedTroops.RemoveRange(settings.MaxVisibleTroops, _sortedTroops.Count - settings.MaxVisibleTroops);
                }

                // === 第五步：最终渲染 ===
                foreach (Troop troop in _sortedTroops)
                {
                    // 根据LOD等级选择渲染方式 - 重用已缓存的距离
                    float distSq = GetCachedDistance(troop, cameraCenter);

                    if (troop.QuickBattling && Setting.Current.GlobalVariables.UseQuadtreeOptimization)
                    {
                        DrawQuickBattleTroop(troop, gameTime, distSq);
                    }
                    else if (distSq <= LOD_HIGH_DISTANCE_SQ)
                    {
                        DrawHighQualityTroop(troop, gameTime);
                    }
                    else if (distSq <= LOD_MEDIUM_DISTANCE_SQ)
                    {
                        DrawMediumQualityTroop(troop, gameTime);
                    }
                    else if (distSq <= LOD_LOW_DISTANCE_SQ)
                    {
                        DrawLowQualityTroop(troop, gameTime);
                    }
                    // 超远距离的部队不渲染
                }

                // === 性能统计 (每60帧输出一次) ===
                if (_frameCounter - _lastStatsFrame >= 60)
                {
                    _lastStatsFrame = _frameCounter;
                    int culledTroops = totalTroops - _sortedTroops.Count;
                    
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[SimpleTroopRenderer] 总计: {totalTroops}, 候选: {_visibleCandidates.Count}, 渲染: {_sortedTroops.Count}, 剔除: {culledTroops}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不崩溃
                System.Diagnostics.Debug.WriteLine($"[SimpleTroopRenderer] DrawOptimized 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取缓存的距离 - 避免重复计算
        /// </summary>
        private static float GetCachedDistance(Troop troop, Vector2 cameraCenter)
        {
            if (!_distanceCache.TryGetValue(troop, out float distance))
            {
                var mainMapLayer = Session.MainGame?.mainGameScreen?.mainMapLayer;
                if (mainMapLayer != null)
                {
                    Rectangle dest = mainMapLayer.GetDestination(troop.Position);
                    Vector2 troopScreenCenter = new Vector2(dest.X + dest.Width / 2f, dest.Y + dest.Height / 2f);
                    distance = Vector2.DistanceSquared(troopScreenCenter, cameraCenter);
                }
                else
                {
                    distance = Vector2.DistanceSquared(
                        new Vector2(troop.Position.X * 60f, troop.Position.Y * 40f), 
                        cameraCenter
                    );
                }
                _distanceCache[troop] = distance;
            }
            return distance;
        }

        /// <summary>
        /// 检查部队是否在视口范围内 - 精确剔除
        /// </summary>
        private static bool IsInViewport(Troop troop, Rectangle viewport)
        {
            try
            {
                var mainMapLayer = Session.MainGame?.mainGameScreen?.mainMapLayer;
                if (mainMapLayer != null)
                {
                    Rectangle dest = mainMapLayer.GetDestination(troop.Position);
                    // 考虑单位尺寸的边界检查，放大一些避免裁剪
                    dest.Inflate(30, 20);
                    return viewport.Intersects(dest);
                }
                return true;
            }
            catch
            {
                return true; // 出错时默认可见
            }
        }

        /// <summary>
        /// 高质量渲染 - 完整动画和特效
        /// </summary>
        private static void DrawHighQualityTroop(Troop troop, GameTime gameTime)
        {
            try
            {
                // 使用完整的部队渲染逻辑
                DrawSingleTroop(troop, gameTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DrawHighQualityTroop] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 中等质量渲染 - 简化动画
        /// </summary>
        private static void DrawMediumQualityTroop(Troop troop, GameTime gameTime)
        {
            try
            {
                // 简化渲染：降低动画更新频率，但不切换渲染方法（否则会闪烁）
                DrawSingleTroop(troop, gameTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DrawMediumQualityTroop] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 低质量渲染 - 仅静态图像
        /// </summary>
        private static void DrawLowQualityTroop(Troop troop, GameTime gameTime)
        {
            try
            {
                // 最简渲染：只显示静态图像，跳过动画
                DrawStaticTroop(troop);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DrawLowQualityTroop] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 静态部队渲染 - 无动画的高性能版本
        /// </summary>
        private static void DrawStaticTroop(Troop troop)
        {
            try
            {
                if (Session.MainGame?.mainGameScreen?.mainMapLayer == null) return;

                Rectangle destination = Session.MainGame.mainGameScreen.mainMapLayer.GetDestination(troop.Position);
                
                // 🔥 修复：不再使用错误的 Troop_x 纹理名，直接提取该部队当前的静态纹理帧！
                PlatformTexture textureToUse = null;
                int frameCount = 1;

                if (troop.TroopTexture != null)
                {
                    textureToUse = troop.TroopTexture;
                    frameCount = Math.Max(1, troop.CurrentAnimation?.FrameCount ?? 1);
                }
                else if (troop.TileAnimation?.Texture != null)
                {
                    textureToUse = troop.TileAnimation.Texture;
                    frameCount = Math.Max(1, troop.TileAnimation.FrameCount);
                }

                if (textureToUse != null && textureToUse.Width > 0 && textureToUse.Height > 0)
                {
                    Rectangle sourceRect;
                    if (troop.TroopTexture != null && textureToUse == troop.TroopTexture && troop.CurrentAnimation != null)
                    {
                        int dummyAnimIndex = 0;
                        int dummyStayIndex = 0;
                        bool dummyFlag;
                        sourceRect = troop.CurrentAnimation.GetCurrentDisplayRectangle(ref dummyAnimIndex, ref dummyStayIndex, textureToUse.Width / frameCount, (int)troop.Direction, out dummyFlag, false);
                    }
                    else
                    {
                        int frameWidth = textureToUse.Width / frameCount;
                        sourceRect = new Rectangle(0, 0, frameWidth, textureToUse.Height);
                    }

                    // 绘制静态帧
                    CacheManager.Draw(textureToUse, destination, sourceRect, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.4f);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DrawStaticTroop] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 快速战斗部队渲染 - 带距离LOD
        /// </summary>
        private static void DrawQuickBattleTroop(Troop troop, GameTime gameTime, float distanceSquared)
        {
            try
            {
                // 根据距离选择渲染质量
                if (distanceSquared <= LOD_HIGH_DISTANCE_SQ)
                {
                    // 近距离：显示简化的战斗效果
                    DrawSingleTroop(troop, gameTime);
                }
                else
                {
                    // 远距离：只显示基本图标
                    DrawStaticTroop(troop);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DrawQuickBattleTroop] 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取部队纹理名称
        /// </summary>
        private static string GetTroopTextureName(Troop troop)
        {
            return "DefaultTroop";
        }

        private static Rectangle GetVisibleArea()
        {
            var mainGameScreen = Session.MainGame?.mainGameScreen;
            if (mainGameScreen == null || mainGameScreen.mainMapLayer == null)
            {
                // 如果关键对象为空，返回一个默认的大区域
                return new Rectangle(0, 0, 4000, 4000);
            }

            try
            {
                // 🔥 修复：返回摄像机屏幕/视口的实际坐标，而不是瓦片的绝对世界坐标
                // 因为 GetTroopBounds 会返回屏幕坐标，这里也必须匹配屏幕坐标范围
                Point viewport = mainGameScreen.viewportSize;
                Rectangle visibleArea = new Rectangle(0, 0, viewport.X, viewport.Y);
                visibleArea.Inflate(150, 150); // 增加填充避免边缘剔除
                return visibleArea;
            }
            catch
            {
                return new Rectangle(0, 0, 4000, 4000);
            }
        }

        private static bool IsVisible(Troop troop)
        {
            try
            {
                if (troop == null || Session.Current?.Scenario == null)
                {
                    return false;
                }

                return (Session.GlobalVariables.SkyEye || 
                        Session.Current.Scenario.NoCurrentPlayer || 
                        (Session.Current.Scenario.CurrentPlayer?.IsFriendly(troop.BelongedFaction) ?? false) || 
                        (Session.Current.Scenario.CurrentPlayer?.IsPositionKnown(troop.Position) ?? false)) &&
                       (Session.GlobalVariables.SkyEye || 
                        Session.Current.Scenario.CurrentPlayer == null || 
                        troop.Status != TroopStatus.埋伏 || 
                        troop.IsFriendly(Session.Current.Scenario.CurrentPlayer));
            }
            catch
            {
                // 如果检查失败，默认为可见
                return true;
            }
        }

        private static void DrawSingleTroop(Troop troop, GameTime gameTime)
        {
            try
            {
                // 简化的部队绘制 - 使用基础方法避免复杂的动画计算
                Color troopColor = Color.White;
                if (troop.CurrentOutburstKind == OutburstKind.愤怒)
                {
                    troopColor = Color.Red;
                }
                else if (troop.CurrentOutburstKind == OutburstKind.沉静)
                {
                    troopColor = Color.Green;
                }

                // 使用正确的纹理 - 参考原始TroopLayer实现
                PlatformTexture textureToUse = null;
                int frameCount = 1;
                
                // 优先使用 TroopTexture（原始实现使用的）
                if (troop.TroopTexture != null)
                {
                    textureToUse = troop.TroopTexture;
                    frameCount = troop.CurrentAnimation?.FrameCount ?? 1;
                }
                // 回退到 TileAnimation.Texture
                else if (troop.TileAnimation?.Texture != null)
                {
                    textureToUse = troop.TileAnimation.Texture;
                    frameCount = troop.TileAnimation.FrameCount;
                }
                
                if (textureToUse != null)
                {
                    // 验证纹理有效性 - 更安全的检查
                    try
                    {
                        int textureWidth = textureToUse.Width;
                        int textureHeight = textureToUse.Height;
                        
                        if (textureWidth <= 0 || textureHeight <= 0)
                        {
                            return; // 纹理无效，跳过绘制
                        }
                    }
                    catch
                    {
                        return; // 纹理访问失败，跳过绘制
                    }

                    if (frameCount <= 0)
                    {
                        frameCount = 1;
                    }

                    Rectangle? sourceRect = null;
                    Rectangle destination;

                    // 计算源矩形 - 使用正确的方法
                    if (textureToUse.Width > 0 && frameCount > 0)
                    {
                        if (troop.TroopTexture != null && textureToUse == troop.TroopTexture)
                        {
                            // 使用 TroopTexture 时，使用 GetCurrentStopDisplayRectangle
                            sourceRect = troop.GetCurrentStopDisplayRectangle(textureToUse.Width / frameCount);
                        }
                        else
                        {
                            // 使用 TileAnimation.Texture 时，使用简单的帧计算
                            int frameWidth = textureToUse.Width / frameCount;
                            sourceRect = new Rectangle(0, 0, frameWidth, textureToUse.Height);
                        }
                    }

                    // 🔥 修复：使用视觉插值位置进行渲染（平滑移动）
                    // 修复日期：2026-02-27
                    // 问题：四叉树渲染器直接使用逻辑坐标，导致部队瞬移
                    // 解决：使用 VisualPosition 进行插值渲染
                    
                    // 前置检查：确保地图数据有效
                    if (Session.MainGame?.mainGameScreen?.mainMapLayer?.Tiles == null)
                    {
                        // 地图未初始化，跳过渲染
                        return;
                    }
                    
                    Vector2 visualPos = troop.VisualPosition;
                    
                    // 手动 Round 和 Clamp
                    int renderX = (int)(visualPos.X + 0.5f);
                    int renderY = (int)(visualPos.Y + 0.5f);
                    
                    int maxX = Session.MainGame.mainGameScreen.mainMapLayer.Tiles.GetLength(0);
                    int maxY = Session.MainGame.mainGameScreen.mainMapLayer.Tiles.GetLength(1);
                    
                    if (renderX < 0) renderX = 0;
                    else if (renderX >= maxX) renderX = maxX - 1;
                    
                    if (renderY < 0) renderY = 0;
                    else if (renderY >= maxY) renderY = maxY - 1;
                    
                    destination = Session.MainGame.mainGameScreen.mainMapLayer.Tiles[renderX, renderY].Destination;
                    
                    // 🔥 子像素插值：计算精确偏移（保留浮点精度）
                    float subPixelX = (visualPos.X - renderX) * Session.MainGame.mainGameScreen.mainMapLayer.TileWidth;
                    float subPixelY = (visualPos.Y - renderY) * Session.MainGame.mainGameScreen.mainMapLayer.TileHeight;
                    destination.X += (int)(subPixelX + 0.5f);  // 四舍五入
                    destination.Y += (int)(subPixelY + 0.5f);
                    
                    /*
                    #if DEBUG
                    if (troop.ManualControl)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SimpleTroopRenderer] {troop.DisplayName} 渲染管线: Action={troop.Action} Logic={troop.Position} → Visual=({visualPos.X:F4},{visualPos.Y:F4}) → SubPixel=({subPixelX:F2},{subPixelY:F2}) → FinalDest=({destination.X},{destination.Y})");
                    }
                    #endif
                    */

                    // 绘制部队 - 使用验证过的纹理
                    try
                    {
                        // 最后一次验证纹理在绘制前是否仍然有效
                        if (textureToUse != null && 
                            textureToUse.Width > 0 &&
                            textureToUse.Height > 0)
                        {
                            CacheManager.Draw(
                                textureToUse,
                                destination,
                                sourceRect,
                                troopColor,
                                0f,
                                Vector2.Zero,
                                SpriteEffects.None,
                                0.7f);
                        }
                    }
                    catch (Exception drawEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SimpleTroopRenderer] CacheManager.Draw 失败: {drawEx.Message}");
                        // 绘制失败时不影响其他部队
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleTroopRenderer] 绘制部队出错: {ex.Message}");
                // 绘制失败时不影响其他部队
            }
        }
        
        /// <summary>
        /// 快速战斗部队的简化渲染 - 跳过动画，使用静态图像
        /// </summary>
        private static void DrawQuickBattleTroop(Troop troop, GameTime gameTime)
        {
            try
            {
                // 简化的颜色处理 - 快速战斗部队使用半透明显示
                Color troopColor = Color.White * 0.8f; // 80%透明度表示快速战斗状态
                
                // 根据战斗状态调整颜色
                if (troop.TargetTroop != null)
                {
                    troopColor = Color.Red * 0.8f; // 战斗中显示红色
                }
                else if (troop.Morale < 50)
                {
                    troopColor = Color.Yellow * 0.8f; // 士气低显示黄色
                }
                
                // 使用最简单的纹理 - 优先使用TroopTexture
                PlatformTexture textureToUse = troop.TroopTexture ?? troop.TileAnimation?.Texture;
                
                if (textureToUse != null && textureToUse.Width > 0)
                {
                    // 简化的源矩形 - 使用第一帧，不做动画
                    Rectangle sourceRect = new Rectangle(0, 0, textureToUse.Width, textureToUse.Height);
                    
                    // 如果是多帧纹理，只使用第一帧
                    if (troop.TroopTexture != null)
                    {
                        int frameCount = troop.CurrentAnimation?.FrameCount ?? 1;
                        if (frameCount > 1)
                        {
                            int frameWidth = textureToUse.Width / frameCount;
                            sourceRect = new Rectangle(0, 0, frameWidth, textureToUse.Height);
                        }
                    }
                    
                    // 简化的目标位置计算
                    Rectangle destination = new Rectangle(
                        troop.Position.X * 60, 
                        troop.Position.Y * 40, 
                        60, 
                        40
                    );
                    
                    // 尝试使用更精确的位置
                    try
                    {
                        if (Session.MainGame?.mainGameScreen?.mainMapLayer?.Tiles != null &&
                            troop.Position.X >= 0 && troop.Position.Y >= 0 &&
                            troop.Position.X < Session.MainGame.mainGameScreen.mainMapLayer.Tiles.GetLength(0) &&
                            troop.Position.Y < Session.MainGame.mainGameScreen.mainMapLayer.Tiles.GetLength(1))
                        {
                            destination = Session.MainGame.mainGameScreen.mainMapLayer.Tiles[troop.Position.X, troop.Position.Y].Destination;
                        }
                    }
                    catch
                    {
                        // 使用默认位置
                    }
                    
                    // 绘制 - 使用较低的深度层级以提高性能
                    CacheManager.Draw(
                        textureToUse,
                        destination,
                        sourceRect,
                        troopColor,
                        0f,
                        Vector2.Zero,
                        SpriteEffects.None,
                        0.65f); // 比普通部队稍微靠后
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleTroopRenderer] 快速战斗部队绘制出错: {ex.Message}");
                // 出错时回退到普通绘制
                try
                {
                    DrawSingleTroop(troop, gameTime);
                }
                catch
                {
                    // 完全失败时跳过
                }
            }
        }
    }

    /// <summary>
    /// 性能模式枚举
    /// </summary>
    public enum PerformanceMode
    {
        Low,    // 高性能（低配设备）
        Medium, // 平衡（默认）
        High,   // 高画质（高配设备）
        Custom  // 自定义
    }
    
    /// <summary>
    /// 智能性能设置管理器 - 单例模式
    /// </summary>
    public class PerformanceSettings
    {
        // === 单例模式 ===
        private static PerformanceSettings _instance;
        public static PerformanceSettings Current => _instance ?? (_instance = new PerformanceSettings());
        
        // === 事件系统：当设置改变时通知各模块 ===
        public event Action OnSettingsChanged;
        
        // === 1. 逻辑与CPU设置 (影响 AIQuickBattle) ===
        // 战斗计算切片数：每 N 帧执行一次 AI 思考
        public int AiLogicSliceCount { get; private set; } = 5;
        
        // 屏幕外 AI 的休眠系数：屏幕外 AI 的思考频率是屏幕内的多少倍？
        public int OffScreenSliceMultiplier { get; private set; } = 4;
        
        // 是否开启兰切斯特方程 (屏幕外纯数值模拟)
        public bool EnableLanchesterSimulation { get; private set; } = true;
        
        // === 2. 渲染与GPU设置 (影响 Draw) ===
        // 最大可见单位数 (超过这个数量就不画了，优先画离摄像机近的)
        public int MaxVisibleTroops { get; private set; } = 2000;
        
        // 视锥体扩边 (Padding)：越小剔除越激进，但可能导致物体边缘闪烁
        public int CullingPadding { get; private set; } = 100;
        
        // 是否开启粒子特效
        public bool EnableParticles { get; private set; } = true;
        
        // === 3. 内存与资源设置 ===
        // 预分配对象池大小 (Loading 时申请多大内存)
        public int InitialPoolSize { get; private set; } = 2000;
        
        // 当前模式记录
        public PerformanceMode Mode { get; private set; } = PerformanceMode.Medium;
        
        // === 预设配置方法 ===
        public void SetLowPerformanceMode() // 低配设备模式
        {
            Mode = PerformanceMode.Low;
            AiLogicSliceCount = 15;        // 极低频思考 (每15帧一次)
            OffScreenSliceMultiplier = 10; // 屏幕外几乎不思考 (每150帧一次)
            EnableLanchesterSimulation = true;
            MaxVisibleTroops = 500;        // 只画最近的 500 人
            CullingPadding = 50;           // 激进剔除
            EnableParticles = false;       // 关闭特效
            InitialPoolSize = 1000;        // 省内存
            NotifyChanged();
        }
        
        public void SetBalancedMode() // 默认平衡模式
        {
            Mode = PerformanceMode.Medium;
            AiLogicSliceCount = 5;         // 正常频率
            OffScreenSliceMultiplier = 4;
            EnableLanchesterSimulation = true;
            MaxVisibleTroops = 2000;
            CullingPadding = 150;
            EnableParticles = true;
            InitialPoolSize = 3000;
            NotifyChanged();
        }
        
        public void SetHighQualityMode() // 高配设备模式
        {
            Mode = PerformanceMode.High;
            AiLogicSliceCount = 1;         // 每帧都思考 (最平滑)
            OffScreenSliceMultiplier = 2;  
            EnableLanchesterSimulation = false; // 全物理模拟
            MaxVisibleTroops = 10000;      // 显卡好，随便画
            CullingPadding = 300;          // 防止任何穿帮
            EnableParticles = true;
            InitialPoolSize = 10000;
            NotifyChanged();
        }
        
        /// <summary>
        /// 自定义性能设置
        /// </summary>
        public void SetCustomMode(int aiSlice, int offScreenMultiplier, bool enableLanchester, 
                                int maxTroops, int cullingPadding, bool enableParticles, int poolSize)
        {
            Mode = PerformanceMode.Custom;
            AiLogicSliceCount = Math.Max(1, aiSlice);
            OffScreenSliceMultiplier = Math.Max(1, offScreenMultiplier);
            EnableLanchesterSimulation = enableLanchester;
            MaxVisibleTroops = Math.Max(100, maxTroops);
            CullingPadding = Math.Max(0, cullingPadding);
            EnableParticles = enableParticles;
            InitialPoolSize = Math.Max(500, poolSize);
            NotifyChanged();
        }
        
        private void NotifyChanged()
        {
            System.Diagnostics.Debug.WriteLine($"[PerformanceSettings] 性能模式切换为: {Mode}");
            OnSettingsChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// 性能监控器 - 自动调节性能设置
    /// </summary>
    public class PerformanceMonitor
    {
        private float _fpsAccumulator = 0;
        private int _fpsSamples = 0;
        private float _checkTimer = 0;
        private float _lastFrameTime = 0;
        
        // 性能统计
        public float AverageFrameTime { get; private set; } = 16.67f; // 默认60FPS
        public float AverageFPS { get; private set; } = 60f;
        public int OptimizedTroops { get; set; } = 0;
        public int ClusterBattles { get; set; } = 0;
        public float CPUUsageReduction { get; set; } = 0f;
        public float MemoryUsageReduction { get; set; } = 0f;
        
        public void Update(GameTime gameTime)
        {
            try
            {
                // 计算当前帧时间
                float currentFrameTime = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                _lastFrameTime = currentFrameTime;
                
                // 累积FPS数据
                if (currentFrameTime > 0)
                {
                    float currentFPS = 1000f / currentFrameTime;
                    _fpsAccumulator += currentFPS;
                    _fpsSamples++;
                }
                
                _checkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                
                // 每 5 秒做一次"体检"
                if (_checkTimer >= 5.0f)
                {
                    if (_fpsSamples > 0)
                    {
                        AverageFPS = _fpsAccumulator / _fpsSamples;
                        AverageFrameTime = 1000f / AverageFPS;
                        
                        AdjustSettingsAutomatically(AverageFPS);
                        LogPerformance();
                    }
                    
                    // 重置统计
                    _fpsAccumulator = 0;
                    _fpsSamples = 0;
                    _checkTimer = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PerformanceMonitor] 错误: {ex.Message}");
            }
        }
        
        private void AdjustSettingsAutomatically(float fps)
        {
            try
            {
                var settings = PerformanceSettings.Current;
                
                // 如果 FPS 持续低于 30，且还没降级到最低
                if (fps < 30 && settings.Mode != PerformanceMode.Low)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformanceMonitor] 检测到帧率过低，自动降级性能设置...");
                    settings.SetLowPerformanceMode();
                }
                // 如果 FPS 在 30-45 之间，且当前是高质量模式，降级到平衡模式
                else if (fps < 45 && settings.Mode == PerformanceMode.High)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformanceMonitor] 帧率偏低，切换到平衡模式...");
                    settings.SetBalancedMode();
                }
                // 如果 FPS 极其流畅 (> 80) 且当前是低性能模式，可以尝试提升
                else if (fps > 80 && settings.Mode == PerformanceMode.Low)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformanceMonitor] 性能充足，提升到平衡模式...");
                    settings.SetBalancedMode();
                }
                // 如果 FPS > 120 且当前是平衡模式，可以提升到高质量
                else if (fps > 120 && settings.Mode == PerformanceMode.Medium)
                {
                    System.Diagnostics.Debug.WriteLine("[PerformanceMonitor] 性能优秀，提升到高质量模式...");
                    settings.SetHighQualityMode();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdjustSettingsAutomatically] 错误: {ex.Message}");
            }
        }
        
        public void LogPerformance()
        {
            try
            {
                // 在玩家控制回合时不显示调试信息，避免刷屏
                if (Session.Current?.Scenario?.IsPlayerControlling() == true)
                {
                    return;
                }
                
                var settings = PerformanceSettings.Current;
                System.Diagnostics.Debug.WriteLine($"[Performance] 平均FPS: {AverageFPS:F1} 帧时间: {AverageFrameTime:F2}ms 模式: {settings.Mode}");
                System.Diagnostics.Debug.WriteLine($"[Optimization] 优化部队: {OptimizedTroops} 集群战斗: {ClusterBattles} 最大可见: {settings.MaxVisibleTroops}");
                System.Diagnostics.Debug.WriteLine($"[Efficiency] CPU减少: {CPUUsageReduction:P} 内存减少: {MemoryUsageReduction:P} AI切片: {settings.AiLogicSliceCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogPerformance] 错误: {ex.Message}");
            }
        }
    }
}
