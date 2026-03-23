using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using GameObjects.ArchitectureDetail.EventEffect;
using GameObjects.Conditions;
using GameObjects.Influences;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Platforms;
using WorldOfTheThreeKingdoms.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Tools;
using TroopDetailPlugin;
using WorldOfTheThreeKingdoms;
using WorldOfTheThreeKingdoms.GameScreens;


namespace GameManager
{

    public enum Difficulty
    {
        beginner,
        easy,
        normal,
        hard,
        veryhard,
        custom
    }

    public class Session
    {
        // 🔥 读写锁：用于保护游戏状态的并发访问
        // 日期：2026-03-16
        // 策略：NoRecursion（禁止锁重入，提升 AOT 性能并防止死锁）
        // 用法：
        //   - 读锁：AI 决策、异步寻路读取游戏状态
        //   - 写锁：部队移动、战斗结算修改游戏状态
        public static readonly ReaderWriterLockSlim WorkLock = 
            new(LockRecursionPolicy.NoRecursion);

        public Session() { }

        public static Session Current = new Session();

        public bool IsWorking = false;

        public static bool LargeContextMenu = false;

        // 🔥 修复：使用 volatile 字段确保多线程可见性
        // 问题：LoadScreenEvent 在后台线程触发，自动属性的私有字段不受 MemoryBarrier 保护
        // 解决：使用 volatile 字段 + 显式属性，确保跨线程可见
        private volatile GameScenario _scenario;
        public GameScenario Scenario 
        { 
            get => _scenario;
            set => _scenario = value;
        }

        // ====== 异步寻路系统：回合级地图快照 ======
        public GameObjects.AI.Pathfinding.MapSnapshot MapSnapshot { get; private set; }
        
        // ====== WEGO 引擎：Command Buffer 并发系统 ======
        // 日期：2026-03-16
        public WorldOfTheThreeKingdoms.GameManager.WegoEngine WegoEngine { get; internal set; }

        public void OnTurnStart()
        {
            if (this.Scenario != null)
            {
                this.MapSnapshot = new GameObjects.AI.Pathfinding.MapSnapshot(this.Scenario);
            }
        }

        // ====== 异步寻路系统：部队注册表 ======
        // 🔥 2026-03-16 AOT 修复：使用 ConcurrentDictionary 保证线程安全
        // 原因：WEGO 机制下多个 AI 部队可能同时注册/注销
        // 用于快速查找部队对象（仅新系统使用）
        // 注意：设为 internal 以便 MainGameScreen 访问
        internal readonly ConcurrentDictionary<Guid, GameObjects.Troop> _troopRegistry = new();

        public WorldOfTheThreeKingdoms.GameManager.TerritoryManager TerritoryManager { get; set; }

        public static Parameters parametersBasic = new Parameters();

        public static Parameters parametersTemp = new Parameters();

        public static Parameters Parameters
        {
            get
            {
                if (Session.Current.Scenario == null || Session.Current.Scenario.Parameters == null)
                {
                    return parametersTemp;
                }
                else
                {
                    return Session.Current.Scenario.Parameters;
                }
            }
        }

        public static GlobalVariables globalVariablesBasic = new GlobalVariables();

        public static GlobalVariables globalVariablesTemp = new GlobalVariables();

        public static GlobalVariables GlobalVariables
        {
            get
            {
                if (Session.Current.Scenario == null || Session.Current.Scenario.GlobalVariables == null)
                {
                    return globalVariablesTemp;
                }
                else
                {
                    return Session.Current.Scenario.GlobalVariables;
                }
            }
        }

        //public Parameters gameParameters = new Parameters();
        //public GlobalVariables globalVariables = new GlobalVariables();

        public static int ResolutionX
        {
            get
            {
                int resolutionX = 0;
                if (!String.IsNullOrEmpty(Resolution) && Resolution.Contains("*"))
                {
                    int.TryParse(Resolution.Split('*')[0].Trim(), out resolutionX);
                }
                return resolutionX;
            }
        }

        public static int ResolutionY
        {
            get
            {
                int resolutionY = 0;
                if (!String.IsNullOrEmpty(Resolution) && Resolution.Contains("*"))
                {
                    int.TryParse(Resolution.Split('*')[1].Trim(), out resolutionY);
                }
                return resolutionY;
            }
        }

        public static string Resolution
        {
            get
            {
                return Setting.Current != null ? Setting.Current.Resolution : "";
            }
            set
            {
                if (!String.IsNullOrEmpty(value) && value.Contains("*"))
                {
                    if (Setting.Current != null)
                    {
                        Setting.Current.Resolution = value;
                    }
                }
            }
        }

        public static MainGame MainGame
        {
            get
            {
                return (MainGame)Platform.MainGame;
            }
        }

        public static string RealResolution = "";

        /// <summary>
        /// 挂起的主线程初始化任务。
        /// 后台线程（LoadScreenEvent）将 Initialize() 打包至此，
        /// 由 MainGame.Update 在主线程安全执行。
        /// </summary>
        public static Action PendingMainThreadInitialization;

        public static Dictionary<string, TextureRecs> TextureRecs;

        public ContentManager Content;
        public ContentManager FontContent;
        public ContentManager MusicContent;
        public ContentManager SoundContent;

        SpriteFont fontE, fontL, fontS, fontT, font;

        public SpriteFont FontE
        {
            get
            {
                if (fontE == null)
                {
                    try
                    {
                        fontE = FontContent.Load<SpriteFont>("Font/FontE");
                    }
                    catch
                    {
                        try { fontE = FontContent.Load<SpriteFont>("Font/FontS"); } catch { }
                    }
                }
                return fontE;
            }
            set
            {
                fontE = value;
            }
        }

        public SpriteFont FontL
        {
            get
            {
                if (fontL == null)
                {
                    try
                    {
                        fontL = FontContent.Load<SpriteFont>("Font/FontL");
                    }
                    catch
                    {
                        try { fontL = FontContent.Load<SpriteFont>("Font/FontS"); } catch { }
                    }
                }
                return fontL;
            }
            set
            {
                fontL = value;
            }
        }

        public SpriteFont FontS
        {
            get
            {
                if (fontS == null)
                {
                    try
                    {
                        fontS = FontContent.Load<SpriteFont>("Font/FontS");
                    }
                    catch
                    {
                        //try { fontS = FontContent.Load<SpriteFont>("Font"); } catch { }
                    }
                }
                return fontS;
            }
            set
            {
                fontS = value;
            }
        }

        public SpriteFont FontT
        {
            get
            {
                if (fontT == null)
                {
                    try
                    {
                        fontT = FontContent.Load<SpriteFont>("Font/FontT");
                    }
                    catch
                    {
                        try { fontT = FontContent.Load<SpriteFont>("Font/FontS"); } catch { }
                    }
                }
                return fontT;
            }
            set
            {
                fontT = value;
            }
        }

        public SpriteFont Font
        {
            get
            {
                if (font == null && FontContent != null)
                {
                    try
                    {
                        // 优先尝试 Font/FontS 作为默认字体
                        font = FontContent.Load<SpriteFont>("Font/FontS");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Session] Load Font/FontS failed: {ex.Message}");
                        try
                        {
                            // 备用尝试 FontS
                            font = FontContent.Load<SpriteFont>("FontS");
                        }
                        catch
                        {
                             // 最后尝试调用 LoadFont(虽然它看起来不设置font字段)
                             try { Session.LoadFont(Setting.Current.Language); } catch { }
                        }
                    }
                }
                return font;
            }
            set
            {
                font = value;
            }
        }

        public SpriteBatch SpriteBatch
        {
            get
            {
                return MainGame.SpriteBatch;
            }
        }

        public void Clear()
        {
            if (Scenario != null)
            {
                Scenario.Clear();
                Scenario = null;
            }
        }

        /// <summary>
        /// 注册部队到注册表（仅新系统）
        /// 前置条件：troop 必须非 null（调用者保证）
        /// </summary>
        public void RegisterTroop(GameObjects.Troop troop)
        {
            // 不做空检查 - 如果 troop 为 null，让它崩溃以便发现调用者的 bug
            _troopRegistry[troop.Id] = troop;
            System.Diagnostics.Debug.WriteLine($"[Session] 注册部队: {troop.DisplayName} (ID: {troop.Id})");
        }

        /// <summary>
        /// 从注册表注销部队（仅新系统）
        /// 前置条件：troop 必须非 null（调用者保证）
        /// </summary>
        public void UnregisterTroop(GameObjects.Troop troop)
        {
            // 🔥 2026-03-16 AOT 修复：使用 ConcurrentDictionary.TryRemove
            // 不做空检查 - 如果 troop 为 null，让它崩溃以便发现调用者的 bug
            if (_troopRegistry.TryRemove(troop.Id, out _))
            {
                System.Diagnostics.Debug.WriteLine($"[Session] 注销部队: {troop.DisplayName} (ID: {troop.Id})");
            }
        }

        /// <summary>
        /// 尝试从注册表获取部队（仅新系统）
        /// </summary>
        /// <param name="troopId">部队 ID</param>
        /// <param name="troop">输出参数：找到的部队</param>
        /// <returns>是否找到部队</returns>
        public bool TryGetTroop(Guid troopId, out GameObjects.Troop troop)
        {
            return _troopRegistry.TryGetValue(troopId, out troop);
        }

        public static void Init()
        {
            #region 手機版采用跟PC同樣設置
            //if (Platform.PlatFormType == PlatFormType.Win)
            //{
            //    //此選項用於生成壓縮格式的劇本，以減小遊戲占用存儲空間
            //    bool BuildScenarioDataZip = false;

            //    if (BuildScenarioDataZip)
            //    {
            //        string comFile = Platform.Current.SolutionDir + @"Content\Data\Common\CommonData.json";
            //        string comFileCon = Platform.Current.ReadAllText(comFile);
            //        var common = Tools.SimpleSerializer.DeserializeJson<CommonData>(comFileCon);

            //        //var str = System.IO.File.ReadAllText(@"C:\Projects\InfluenceKind.xml");
            //        //var doc = new System.Xml.XmlDocument();
            //        //doc.LoadXml(str);

            //        //var childNodes = ((System.Xml.XmlLinkedNode)doc.FirstChild).NextSibling.ChildNodes;

            //        //foreach (System.Xml.XmlElement child in childNodes)
            //        //{
            //        //    var id = child["ID"];
            //        //    var type = child["Type"];
            //        //    var combat = child["Combat"];
            //        //    var pv = child["AIPersonValue"];
            //        //    var pvp = child["AIPersonValuePow"];

            //        //    var influ = common.AllInfluenceKinds.InfluenceKinds.FirstOrDefault(inf => inf.Key == int.Parse(id.InnerText));
            //        //    influ.Value.Type = (InfluenceType)Enum.Parse(typeof(InfluenceType), type.InnerText);
            //        //    influ.Value.Combat = combat.InnerText == "1";
            //        //    influ.Value.AIPersonValue = float.Parse(pv.InnerText);
            //        //    influ.Value.AIPersonValuePow = float.Parse(pvp.InnerText);
            //        //}

            //        string json = SimpleSerializer.SerializeJson<CommonData>(common, true);
            //        string comZipFile = Platform.Current.SolutionDir + @"Content\Data\CommonZip\CommonData.json";
            //        Platform.Current.WriteAllText(comZipFile, json);

            //        string dir = Platform.Current.SolutionDir + @"Content\Data\Scenario\";
            //        var sces = Platform.Current.GetFiles(dir);

            //        foreach (var sce in sces)
            //        {
            //            if (sce.Contains("Scenarios.json"))
            //            {
            //                continue;
            //            }

            //            string fileName = Platform.Current.GetFileNameFromPath(sce);
            //            string sceFileCon = Platform.Current.ReadAllText(dir + fileName);
            //            var scenario = Tools.SimpleSerializer.DeserializeJson<GameScenario>(sceFileCon, false);

            //            json = Tools.SimpleSerializer.SerializeJson<GameScenario>(scenario, true);
            //            string scenarioZipFile = Platform.Current.SolutionDir + @"Content\Data\ScenarioZip\" + fileName;
            //            Platform.Current.WriteAllText(scenarioZipFile, json);
            //        }
            //    }
            //}

            //bool zip = true;

            //if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            //{
            //    zip = false;
            //}
            #endregion

            CommonData.Init();

            //if (String.IsNullOrEmpty(Setting.Current.Difficulty))
            //{
            //    if (String.IsNullOrEmpty(Session.GlobalVariables.GameDifficulty))
            //    {
            //        Setting.Current.Difficulty = Difficulty.beginner.ToString();
            //    }
            //    else
            //    {
            //        Setting.Current.Difficulty = Session.GlobalVariables.GameDifficulty;
            //    }
            //}

            //if (String.IsNullOrEmpty(Setting.Current.BattleSpeed))
            //{
            //    Setting.Current.BattleSpeed = Setting.Current.GlobalVariables.FastBattleSpeed.ToString();
            //}

            Session.LoadFont(Setting.Current.Language);

            Platform.InitGraphicsDeviceManager();

            TouchPanel.EnabledGestures = GestureType.Tap | GestureType.DoubleTap | GestureType.FreeDrag | GestureType.Flick | GestureType.Pinch;
        }

        public static void LoadContent(ContentManager content)
        {
            Current.Content = content;
            //Current.Content.RootDirectory = "Content";

            Current.FontContent = new ContentManager(Current.Content.ServiceProvider, Current.Content.RootDirectory);
            //Current.FontContent.RootDirectory = "Content";

            Current.MusicContent = new ContentManager(Current.Content.ServiceProvider, Current.Content.RootDirectory);
            //Current.MusicContent.RootDirectory = "Content";

            Current.SoundContent = new ContentManager(Current.Content.ServiceProvider, Current.Content.RootDirectory);
            //Current.SoundContent.RootDirectory = "Content";

            //LoadFont(Setting.Current.Language);
        }

        public static void LoadFont(string language)
        {
            //Session.Current.FontContent.Unload();

            // [修改] 不直接设置为null，而是检查是否需要重新加载
            bool needReload = false;
            FontPair newFontPair;

            //此處兩種字體及大小可隨需要自由更改
            if (language == "cn" || language == "简体")
            {
                newFontPair = new FontPair()
                {
                    Name = @"Content\Font\FZLB_GBK.TTF",
                    Size = 30,
                    Style = "",
                    Width = 30,
                    Height = 32
                };
            }
            else
            {
                newFontPair = new FontPair()
                {
                    Name = @"Content\Font\JDFGY.TTF",
                    Size = 28,
                    Style = "",
                    Width = 28,
                    Height = 30
                };
            }

            // [新增] 检查是否需要重新加载字体
            if (CacheManager.FontPair.Name != newFontPair.Name || CacheManager.FontPair.Size != newFontPair.Size)
            {
                needReload = true;
                System.Diagnostics.Debug.WriteLine($"[Session] 字体配置改变，需要重新加载: {newFontPair.Name}");
            }

            CacheManager.FontPair = newFontPair;

            // [修改] 只有在需要时才重置字体
            if (needReload)
            {
                Session.Current.Font = null;
                System.Diagnostics.Debug.WriteLine($"[Session] 重置字体以便重新加载");
            }

            lock (CacheManager.CacheLock)
            {
                if (CacheManager.DicTexts != null)
                {
                    CacheManager.DicTexts.Clear();
                }
            }

        }

        public static void ChangeDisplay(bool setScale)
        {
            Platform.Current.SetFullScreen(Setting.Current.DisplayMode == "Full");

            Platform.Current.SetOrientations();

            float screenscalex1 = 1f;
            float screenscaley1 = 1f;

            float screenscalex2 = 1f;
            float screenscaley2 = 1f;

            int width = 0;
            int height = 0;

            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            {
                if (String.IsNullOrEmpty(Session.Resolution))
                {
                    Session.Resolution = Platform.PreferResolution;
                }

                width = int.Parse(Session.Resolution.Split('*')[0]);
                height = int.Parse(Session.Resolution.Split('*')[1]);
            }
            else if (Platform.PlatFormType == PlatFormType.Android || Platform.PlatFormType == PlatFormType.iOS || Platform.PlatFormType == PlatFormType.UWP)
            {
                width = Session.MainGame.fullScreenDestination.Width;
                height = Session.MainGame.fullScreenDestination.Height;
            }

            float slope = Convert.ToSingle(width) / Convert.ToSingle(height);
            if (Platform.PlatFormType == PlatFormType.Android || Platform.PlatFormType == PlatFormType.iOS || Platform.PlatFormType == PlatFormType.UWP)
            {
                if (slope >= 1.5)
                {
                    Session.Resolution = "1000*620";
                }
                else
                {
                    Session.Resolution = "1024*768";
                }
            }

            Session.RealResolution = Platform.PreferResolution = Session.Resolution;

            screenscalex1 = Convert.ToSingle(width) / 1280f;
            screenscaley1 = Convert.ToSingle(height) / 720f;

            screenscalex2 = Convert.ToSingle(width) / Session.ResolutionX;
            screenscaley2 = Convert.ToSingle(height) / Session.ResolutionY;

            InputManager.Scale1 = new Vector2(screenscalex1, screenscaley1);

            InputManager.ScaleDraw = new Vector2(1, 1);
            if (setScale)
            {
                InputManager.Scale2 = new Vector2(screenscalex2, screenscaley2);
                Session.MainGame.disScale = true;
            }
            else
            {
                Session.MainGame.disScale = false;
            }

            Session.MainGame.SpriteScale1 = Matrix.CreateScale(screenscalex1, screenscaley1, 1);
            Session.MainGame.SpriteScale2 = Matrix.CreateScale(screenscalex2, screenscaley2, 1);

            // 更新 ScreenManager 的缩放矩阵
            GameManager.ScreenManager.UpdateResolution(width, height);

            Platform.SetGraphicsWidthHeight(width, height);

            Platform.Current.ProcessViewChanged();

            Platform.GraphicsApplyChanges();

            InputManager.SWidth = Session.ResolutionX;
            InputManager.SHeight = Session.ResolutionY;
            InputManager.RealScale = new Vector2(Convert.ToSingle(MainGame.fullScreenDestination.Width) / ResolutionX, Convert.ToSingle(MainGame.fullScreenDestination.Height) / ResolutionY);
        }

        public static void ChangeStartDisplay(int width, int height)
        {
            float screenscalex1 = Convert.ToSingle(width) / 1280f;
            float screenscaley1 = Convert.ToSingle(height) / 720f;

            InputManager.Scale1 = new Vector2(screenscalex1, screenscaley1);
            Session.MainGame.SpriteScale1 = Matrix.CreateScale(screenscalex1, screenscaley1, 1);
            
            // 更新 ScreenManager 的缩放矩阵
            GameManager.ScreenManager.UpdateResolution(width, height);
        }

        public static void StartScenario(string scenarioName, bool fromScenario, string filename)
        {
            // 🔥 诊断：Release 模式使用文件日志
            string logPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                "start_scenario_log.txt");
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] 开始加载流程: {filename}\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }
            
            // PROFILING START
            // 按照用户要求，把整个加载流程（包括 UI 刷新）都包在计时器里
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // A. 配置读取规则 (只注册必要的转换器)
            // B. 读取地图/剧本文件
            // C. 【关键】打通数据管道：如果地图里没规则，就去读 CommonData.json
            // ---------------------------------------------------------
            // 🔥 2026-03-17: 根据文件类型调用正确的加载方法
            // - 剧本文件（.json）：使用 LoadScenario（未压缩）
            // - 存档文件（.sav.gz 或 .bin）：使用 LoadGame（压缩格式）
            // ---------------------------------------------------------
            WorldOfTheThreeKingdoms.Serialization.SerializationManager serializationManager = new();
            GameScenario scenario;
            
            if (fromScenario)
            {
                // 新开剧本：使用 LoadScenario（未压缩 JSON）
                scenario = serializationManager.LoadScenario(filename);
            }
            else
            {
                // 读取存档：使用 LoadGame（压缩格式 .sav.gz）
                scenario = serializationManager.LoadGame(filename);
            }

            // 🔥 诊断：Release 模式使用文件日志
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] ✅ SerializationManager.LoadScenario 完成\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }

            // 🔥 FIX: Check if scenario loading failed
            if (scenario == null)
            {
                try
                {
                    string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] ❌ 场景加载失败: {filename}\n";
                    System.IO.File.AppendAllText(logPath, msg);
                }
                catch { }
                throw new InvalidOperationException($"无法加载场景文件: {filename}。请检查文件是否存在且格式正确。");
            }

            // 🔥 2026-03-17 关键修复：先设置 Session.Current.Scenario，再调用 ProcessScenarioData
            // 原因：ProcessScenarioData 中的蜜月期初始化会访问 Person.Loyalty
            //       Loyalty 的 getter 依赖 Session.Current.Scenario.GameCommonData
            // 顺序：必须先赋值 Session.Current.Scenario，再调用 ProcessScenarioData
            Session.Current.Scenario = scenario;
            
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] ✅ Session.Current.Scenario 已赋值\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }

            // 🔥 2026-03-17 关键修复：调用 ProcessScenarioData 完成初始化
            // 原因：SerializationManager.LoadScenario 只负责反序列化和引用链接
            //       ProcessScenarioData 负责数据处理，包括蜜月期初始化
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] 准备调用 ProcessScenarioData (fromScenario={fromScenario})\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }
            
            scenario.ProcessScenarioData(fromScenario, editing: false);
            
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] ✅ ProcessScenarioData 完成\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }

            // 🔥 注意：LoadGame 已经确保 CurrentPlayer 不为 null（Fail Fast）
            // 如果 CurrentPlayer 为 null，LoadGame 会抛出异常，不会执行到这里

            // 重置计时器以测量下一阶段
            sw.Restart();

            // D. 完成初始化
            // Session.Current.Scenario 已在上面赋值

            // 🔥 诊断：确认 Scenario 已赋值
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] ✅ Session.Current.Scenario 已赋值（使用 volatile 字段）\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }

            // 🔥 FIX: Ensure object references are restored (AOT data support)
            // 注意: 不要在此处调用 AfterLoadSaveFile(null)，因为它需要 MainGameScreen 的 PluginList
            // AfterLoadSaveFile 会在 MainGameScreen.Initialize() 中被正确调用
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] 准备调用 EnsureObjectReferencesRestored\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }
            
            EnsureObjectReferencesRestored(scenario);
            
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] ✅ EnsureObjectReferencesRestored 完成\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }

            // 重置计时器，准备测量 UI 阶段 (注意：这中间会有 LoadingScreen 显示的等待时间)
            sw.Restart();

            Session.MainGame.loadingScreen = new LoadingScreen(fromScenario ? "Start" : "", scenarioName);
            
            // 🔥 诊断：确认 LoadingScreen 已创建（Release 模式使用文件日志）
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] LoadingScreen 已创建，准备注册 LoadScreenEvent\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }

            Session.MainGame.loadingScreen.LoadScreenEvent += (sender0, e0) =>
            {
                // 🔥 诊断：记录 LoadScreenEvent 被触发
                try
                {
                    string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadScreenEvent] ========== 事件被触发 ==========\n" +
                                 $"  Session.Current.Scenario == null: {Session.Current.Scenario == null}\n" +
                                 $"  闭包捕获的 scenario == null: {scenario == null}\n";
                    System.IO.File.AppendAllText(logPath, msg);
                }
                catch { }
                
                // 🔥 修复：确保 Session.Current.Scenario 已设置（使用闭包捕获的 scenario）
                // 如果 Session.Current.Scenario 为 null（线程可见性问题），使用闭包捕获的 scenario
                if (Session.Current.Scenario == null && scenario != null)
                {
                    try
                    {
                        string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadScreenEvent] ⚠️ 检测到线程可见性问题，使用闭包捕获的 scenario\n";
                        System.IO.File.AppendAllText(logPath, msg);
                    }
                    catch { }
                    Session.Current.Scenario = scenario;
                }
                
                // 🚨 核心修复：将整个初始化流程包裹在 try-catch 中，捕获后台线程的静默异常
                try
                {
                    // 进入 UI 构建阶段，重新开始计时 (忽略 LoadingScreen 的渲染等待时间，只关注实际构建耗时)
                    sw.Restart();

                    try
                    {
                        System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadScreenEvent] 准备实例化 MainGameScreen\n");
                    }
                    catch { }

                    var mainGameScreen = new MainGameScreen();
                    mainGameScreen.InitializationFileName = scenarioName;

                    // 🔥 修复：传递临时存储的InitializationFactionIDs
                    Session.MainGame.TransferTempInitializationFactionIDs(mainGameScreen);
                    mainGameScreen.LoadScenarioInInitialization = fromScenario;

                    System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadScreenEvent] ✅ 后台准备完成，打包 Initialize 至主线程\n");

                    // 🚨 关键：不在后台线程调用 Initialize()！
                    // 打包推给主线程，由 MainGame.Update 在主线程安全执行
                    Session.PendingMainThreadInitialization = () =>
                    {
                        System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [PendingInit] 主线程开始执行 Initialize()\n");

                        mainGameScreen.Initialize();

                        System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [PendingInit] ✅ Initialize() 完成，设置 mainGameScreen\n");

                        Session.MainGame.mainGameScreen = mainGameScreen;

                        if (mainGameScreen.cloudLayer != null)
                        {
                            mainGameScreen.cloudLayer.Start();
                        }

                        // AfterInit / AI Detail / 部队注册
                        // 🔥 ANTI-BAND-AID：Scenario 在此处必须非 null，不做防御性检查
                        try { Session.Current.Scenario.AfterInit(); }
                        catch (Exception ex)
                        {
                            System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [PendingInit] AfterInit异常: {ex}\n");
                        }

                        if (Session.Current.Scenario.Sections != null && CommonData.Current?.AllSectionAIDetails != null)
                        {
                            foreach (Section section in Session.Current.Scenario.Sections)
                            {
                                try { section.LoadAIDetail(CommonData.Current.AllSectionAIDetails); }
                                catch (Exception ex)
                                {
                                    System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [PendingInit] LoadAIDetail异常: {ex.Message}\n");
                                }
                            }
                        }

                        if (Session.Current.Scenario.Troops != null)
                        {
                            foreach (var troop in Session.Current.Scenario.Troops)
                            {
                                if (troop is GameObjects.Troop t)
                                    Session.Current.RegisterTroop(t);
                            }
                        }
                        
                        // 🔥 初始化 WegoEngine（2026-03-16）
                        // 日期：2026-03-16
                        // 位置：部队注册后，确保所有部队已在注册表中
                        try
                        {
                            Session.Current.WegoEngine = new WorldOfTheThreeKingdoms.GameManager.WegoEngine();
                            
                            // 将所有已注册的部队添加到 WegoEngine
                            foreach (var kvp in Session.Current._troopRegistry)
                            {
                                Session.Current.WegoEngine.RegisterTroop(kvp.Value);
                            }
                            
                            System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [PendingInit] ✅ WegoEngine 初始化完成，注册部队数: {Session.Current.WegoEngine.TroopCount}\n");
                        }
                        catch (Exception ex)
                        {
                            System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [PendingInit] ⚠️ WegoEngine 初始化失败: {ex.Message}\n");
                        }

                        System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [PendingInit] ✅ 所有初始化完成\n");
                    };

                    // 占位，保持原有 try-catch 结构闭合
                    try
                    {
                        System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadScreenEvent] ✅ PendingMainThreadInitialization 已注册\n");
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    // 🚨 核心抓捕代码：强行记录后台静默异常！
                    try
                    {
                        System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now:HH:mm:ss.fff}] ❌ [致命崩溃] 抓到后台异常：\n{ex.ToString()}\n");
                    }
                    catch { }
                    
                    // 重新抛出异常，让 LoadingScreen 的 catch 块处理
                    throw;
                }
            };
            
            // 🔥 诊断：确认 StartScenario 即将返回（Release 模式使用文件日志）
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [StartScenario] LoadScreenEvent 已注册，即将返回\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }
        }

        public static void PlayMusic(string category)
        {
            if (!Session.GlobalVariables.PlayMusic)
            {
                System.Diagnostics.Debug.WriteLine($"[Session.PlayMusic] 音乐已禁用，跳过播放");
                return;
            }
            
            // 使用新的AudioManager播放音乐
            AudioManager.Instance?.PlayMusic(category);
        }

        public static void StopSong()
        {
            Platform.Current.StopSong();
        }

        /// <summary>
        /// 🔥 BACKUP FIX: Ensure object references are restored for scenarios loaded through different paths
        /// </summary>
        private static void EnsureObjectReferencesRestored(GameScenario scenario)
        {
            if (scenario == null) return;

            try
            {
                // Quick check: if CurrentPlayer is null and we have factions, we need to restore references
                bool needsRestore = scenario.CurrentPlayer == null && scenario.Factions != null && scenario.Factions.Count > 0;
                
                // 🔥 AOT 兼容修复：使用模式匹配和直接属性访问，避免反射
                // 检查是否有势力的 Leader 为 null 但 LeaderID 有效（ID=0 是有效的阿会喃）
                if (!needsRestore && scenario.Factions != null)
                {
                    foreach (var f in scenario.Factions)
                    {
                        if (f is GameObjects.Faction faction && faction.Leader == null)
                        {
                            // 🔥 关键：ID >= 0 是有效的（ID=0 是阿会喃）
                            if (faction.LeaderID >= 0)
                            {
                                needsRestore = true;
                                System.Diagnostics.Debug.WriteLine($"[Session] 检测到势力 {faction.Name} 的 Leader 为 null 但 LeaderID={faction.LeaderID}，需要恢复引用");
                                break;
                            }
                        }
                    }
                }

                if (needsRestore)
                {
                    System.Diagnostics.Debug.WriteLine("[Session] 检测到需要恢复对象引用，调用备份修复方法");
                    
                    // Set basic references
                    Session.Current.Scenario = scenario;
                    
                    // 🔥 关键修复：强制从配置文件加载 Parameters 和 GlobalVariables
                    System.Diagnostics.Debug.WriteLine("[Session] 🔥 强制从配置文件加载配置...");
                    try
                    {
                        if (scenario.Parameters != null)
                        {
                            scenario.Parameters.InitializeGameParameters("Content/Data/GameParameters.xml");
                            System.Diagnostics.Debug.WriteLine($"[Session] ✅ Parameters 加载成功，ExpandConditions 数量: {scenario.Parameters.ExpandConditions?.Count ?? 0}");
                        }
                        
                        if (scenario.GlobalVariables != null)
                        {
                            scenario.GlobalVariables.InitialGlobalVariables("Content/Data/GlobalVariables.xml");
                            System.Diagnostics.Debug.WriteLine("[Session] ✅ GlobalVariables 加载成功");
                        }
                    }
                    catch (Exception configEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Session] ⚠️ 配置加载失败: {configEx.Message}");
                    }
                    
                    // Ensure CurrentPlayer is set
                    // 🔥 诊断：记录 CurrentPlayer 状态
                    System.Diagnostics.Debug.WriteLine($"[Session.EnsureObjectReferencesRestored] CurrentPlayer={scenario.CurrentPlayer?.Name}, CurrentPlayerID={scenario.CurrentPlayerID}, Factions.Count={scenario.Factions.Count}");
                    
                    if (scenario.CurrentPlayer == null && scenario.Factions.Count > 0)
                    {
                        // 🔥 关键修复：优先使用 CurrentPlayerID 查找玩家势力
                        // 日期：2026-03-16
                        // 原因：用户选择的势力 ID 存储在 CurrentPlayerID 中，必须使用它来查找
                        // 🔥 ID=0 是有效的（汉势力），必须使用 >= 0 判断
                        if (!string.IsNullOrEmpty(scenario.CurrentPlayerID) && int.TryParse(scenario.CurrentPlayerID, out int playerID) && playerID >= 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Session] 尝试从 CurrentPlayerID={playerID} 查找玩家势力...");
                            scenario.CurrentPlayer = scenario.Factions.GetGameObject(playerID) as GameObjects.Faction;
                            if (scenario.CurrentPlayer != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Session] ✅ 从 CurrentPlayerID={playerID} 恢复玩家势力: {scenario.CurrentPlayer.Name}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[Session] ⚠️ CurrentPlayerID={playerID} 对应的势力不存在，使用 Factions[0]");
                                scenario.CurrentPlayer = scenario.Factions[0] as GameObjects.Faction;
                                System.Diagnostics.Debug.WriteLine($"[Session] 回退到 Factions[0]: {scenario.CurrentPlayer?.Name}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Session] CurrentPlayerID 无效或为空，使用 Factions[0]");
                            scenario.CurrentPlayer = scenario.Factions[0] as GameObjects.Faction;
                            System.Diagnostics.Debug.WriteLine($"[Session] 备份设置当前玩家: {scenario.CurrentPlayer?.Name}");
                        }
                    }
                    else if (scenario.CurrentPlayer != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Session] CurrentPlayer 已设置: {scenario.CurrentPlayer.Name}，跳过恢复");
                    }
                    
                    // Set CurrentFaction
                    if (scenario.CurrentPlayer != null)
                    {
                        scenario.CurrentFaction = scenario.CurrentPlayer;
                        if (scenario.Factions is GameObjects.FactionListWithQueue factionQueue)
                        {
                            factionQueue.RunningFaction = scenario.CurrentPlayer;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Session] 对象引用已正确设置，无需备份修复");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Session] 备份对象引用修复失败: {ex.Message}");
                
                // Emergency fallback: ensure CurrentPlayer is set
                if (scenario.CurrentPlayer == null && scenario.Factions != null && scenario.Factions.Count > 0)
                {
                    // 🔥 关键修复：优先使用 CurrentPlayerID 查找玩家势力
                    // 🔥 ID=0 是有效的（汉势力），必须使用 >= 0 判断
                    if (!string.IsNullOrEmpty(scenario.CurrentPlayerID) && int.TryParse(scenario.CurrentPlayerID, out int playerID) && playerID >= 0)
                    {
                        scenario.CurrentPlayer = scenario.Factions.GetGameObject(playerID) as GameObjects.Faction;
                        if (scenario.CurrentPlayer != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Session] 紧急备份：从 CurrentPlayerID={playerID} 恢复玩家势力: {scenario.CurrentPlayer.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Session] ⚠️ 紧急备份：CurrentPlayerID={playerID} 对应的势力不存在，使用 Factions[0]");
                            scenario.CurrentPlayer = scenario.Factions[0] as GameObjects.Faction;
                        }
                    }
                    else
                    {
                        scenario.CurrentPlayer = scenario.Factions[0] as GameObjects.Faction;
                        System.Diagnostics.Debug.WriteLine($"[Session] 紧急备份设置当前玩家: {scenario.CurrentPlayer?.Name}");
                    }
                }
            }
        }

    }

    /// <summary>
    /// External Rule Loading Pipeline
    /// Loads CommonData.json and injects it into scenarios
    /// </summary>
    public static class ExternalRuleLoader
    {
        private static CommonData _cachedCommonData = null;
        private static bool _isLoading = false;

        /// <summary>
        /// Load CommonData from external file with legacy format support
        /// </summary>
        public static CommonData LoadExternalCommonData()
        {
            if (_cachedCommonData != null)
                return _cachedCommonData;

            if (_isLoading)
            {
                // Wait for loading to complete
                while (_isLoading)
                {
                    System.Threading.Thread.Sleep(10);
                }
                return _cachedCommonData;
            }

            _isLoading = true;

            try
            {
                string commonDataPath = @"Content\Data\Common\CommonData.json";
                
                // Check if file exists
                if (!Platform.Current.FileExists(commonDataPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[ExternalRuleLoader] CommonData.json not found at: {commonDataPath}");
                    _isLoading = false;
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[ExternalRuleLoader] Loading CommonData from: {commonDataPath}");

                // Create JsonSerializer with legacy converters


                // Read and deserialize the file
                string jsonContent = Platform.Current.ReadAllText(commonDataPath);
                _cachedCommonData = SimpleSerializer.DeserializeJson<CommonData>(jsonContent);

                if (_cachedCommonData != null)
                {
                    System.Diagnostics.Debug.WriteLine("[ExternalRuleLoader] CommonData loaded successfully");
                    
                    // Process the loaded data (same as in CommonData.Init)
                    GameScenario.ProcessCommonData(_cachedCommonData);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ExternalRuleLoader] Failed to deserialize CommonData");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExternalRuleLoader] Error loading CommonData: {ex.Message}");
                _cachedCommonData = null;
            }
            finally
            {
                _isLoading = false;
            }

            return _cachedCommonData;
        }

        /// <summary>
        /// Inject external CommonData into a scenario
        /// </summary>
        public static void InjectExternalRulesIntoScenario(GameScenario scenario)
        {
            if (scenario == null)
            {
                System.Diagnostics.Debug.WriteLine("[ExternalRuleLoader] Scenario is null, cannot inject rules");
                return;
            }

            var externalCommonData = LoadExternalCommonData();
            if (externalCommonData == null)
            {
                System.Diagnostics.Debug.WriteLine("[ExternalRuleLoader] External CommonData is null, cannot inject rules");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[ExternalRuleLoader] Injecting external rules into scenario");

            try
            {
                // Inject the external CommonData into the scenario
                scenario.GameCommonData = externalCommonData;

                // Also set it as the global CommonData if not already set
                if (CommonData.Current == null)
                {
                    CommonData.Current = externalCommonData;
                    CommonData.CurrentReady = true;
                    System.Diagnostics.Debug.WriteLine("[ExternalRuleLoader] Set external CommonData as global Current");
                }

                System.Diagnostics.Debug.WriteLine("[ExternalRuleLoader] External rules injection completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExternalRuleLoader] Error injecting external rules: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear cached data (for testing or reloading)
        /// </summary>
        public static void ClearCache()
        {
            _cachedCommonData = null;
            System.Diagnostics.Debug.WriteLine("[ExternalRuleLoader] Cache cleared");
        }
    }
}

//性能优化記錄
//Stopwatch stopwatch = new Stopwatch();
//stopwatch.Start();

//var conditionKinds = new ConditionKindTable();
//foreach (var conditionKind in CommonData.Current.AllConditionKinds.ConditionKinds)
//{
//    var con = new ConditionKind();
//    con.ID = conditionKind.Value.ID;
//    con.Name = conditionKind.Value.Name;
//    conditionKinds.AddConditionKind(con);
//}
//CommonData.Current.AllConditionKinds = conditionKinds;

//foreach (var condition in CommonData.Current.AllConditions.Conditions)
//{
//    var Kind = condition.Value.Kind;
//    var con = new ConditionKind();
//    con.ID = Kind.ID;
//    con.Name = Kind.Name;
//    condition.Value.Kind = con;
//}

//var influenceKinds = new InfluenceKindTable();
//foreach (var influenceKind in CommonData.Current.AllInfluenceKinds.InfluenceKinds)
//{
//    var inf = new InfluenceKind();
//    inf.ID = influenceKind.Value.ID;
//    inf.Name = influenceKind.Value.Name;
//    influenceKinds.AddInfluenceKind(inf);
//}
//CommonData.Current.AllInfluenceKinds = influenceKinds;

//foreach (var influence in CommonData.Current.AllInfluences.Influences)
//{
//    var kind = influence.Value.Kind;
//    var inf = new InfluenceKind();
//    inf.ID = kind.ID;
//    inf.Name = kind.Name;
//    influence.Value.Kind = inf;
//}

//var eventEffectKinds = new EventEffectKindTable();
//foreach (var eventEffectKind in CommonData.Current.AllEventEffectKinds.EventEffectKinds)
//{
//    var eve = new EventEffectKind();
//    eve.ID = eventEffectKind.Value.ID;
//    eve.Name = eventEffectKind.Value.Name;
//    eventEffectKinds.AddEventEffectKind(eve);
//}
//CommonData.Current.AllEventEffectKinds = eventEffectKinds;

//foreach (var eventEffect in CommonData.Current.AllEventEffects.EventEffects)
//{
//    var kind = eventEffect.Value.Kind;
//    var eve = new EventEffectKind();
//    eve.ID = kind.ID;
//    eve.Name = kind.Name;
//    eventEffect.Value.Kind = eve;
//}

//var troopEventEffectKinds = new GameObjects.TroopDetail.EventEffect.EventEffectKindTable();
//foreach (var eventEffectKind in CommonData.Current.AllTroopEventEffectKinds.EventEffectKinds)
//{
//    var eve = new GameObjects.TroopDetail.EventEffect.EventEffectKind();
//    eve.ID = eventEffectKind.Value.ID;
//    eve.Name = eventEffectKind.Value.Name;
//    troopEventEffectKinds.AddEventEffectKind(eve);
//}
//CommonData.Current.AllTroopEventEffectKinds = troopEventEffectKinds;

//foreach (var eventEffect in CommonData.Current.AllTroopEventEffects.EventEffects)
//{
//    var kind = eventEffect.Value.Kind;
//    var eve = new GameObjects.TroopDetail.EventEffect.EventEffectKind();
//    eve.ID = kind.ID;
//    eve.Name = kind.Name;
//    eventEffect.Value.Kind = eve;
//}

//CommonData.Current.ConditionKindList = CommonData.Current.AllConditionKinds.ConditionKinds.Select(co => co.Value).ToList();
//CommonData.Current.AllConditionKinds = null;
//CommonData.Current.ConditionList = CommonData.Current.AllConditions.Conditions.Select(co => co.Value).ToList();
//CommonData.Current.AllConditions = null;

//CommonData.Current.InfluenceKindList = CommonData.Current.AllInfluenceKinds.InfluenceKinds.Select(inf => inf.Value).ToList();
//CommonData.Current.AllInfluenceKinds = null;

//CommonData.Current.InfluenceList = CommonData.Current.AllInfluences.Influences.Select(inf => inf.Value).ToList();
//CommonData.Current.AllInfluences = null;

//string str = Tools.SimpleSerializer.SerializeJson<CommonData>(CommonData.Current);

//stopwatch.Stop();
