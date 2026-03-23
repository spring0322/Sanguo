#nullable disable

using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Platforms;
using PluginServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Tools;
using WorldOfTheThreeKingdoms.GameScreens;
using WTTKGameManager = WorldOfTheThreeKingdoms.GameManager; // 使用别名避免冲突
using GlobalScreenManager = GameManager.ScreenManager; // 🔥 添加别名，避免字符串插值中的命名空间冲突
// using WorldOfTheThreeKingdoms.Helpers;

namespace WorldOfTheThreeKingdoms
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class MainGame : Game
    {
        // [NEW] 引入管理器
        private global::GameManager.ImGuiManager _imGuiManager;
        
        // 🔥 看门狗：监控主线程心跳，检测死锁/无限循环/未响应
        private global::WorldOfTheThreeKingdoms.Tools.GameWatchdog? _watchdog;

        //public static  ContentManager Content;   //原程序，没有new
        //public static new ContentManager Content;

        //public System.Windows.Forms.Form GameForm;

        //private GraphicsDeviceManager graphics;
        //private KeyboardState keyState;  //原程序，由于警告去掉

        public MainMenuScreen mainMenuScreen;

        public LoadingScreen loadingScreen;

        public MainGameScreen mainGameScreen;

        public float time = 0f;

        public bool beginApply = false;

#pragma warning disable CS0414 // The field 'MainGame.previousWindowHeight' is assigned but its value is never used
        private int previousWindowHeight = 720;
#pragma warning restore CS0414 // The field 'MainGame.previousWindowHeight' is assigned but its value is never used
#pragma warning disable CS0414 // The field 'MainGame.previousWindowWidth' is assigned but its value is never used
        private int previousWindowWidth = 0x438;
#pragma warning restore CS0414 // The field 'MainGame.previousWindowWidth' is assigned but its value is never used

        //public jiazaitishichuangkou jiazaitishi = new jiazaitishichuangkou();

        //public WindowsMediaPlayerClass Player = new WindowsMediaPlayerClass();

        //标识是否为全屏
#pragma warning disable CS0414 // The field 'MainGame.IsFullScreen' is assigned but its value is never used
        private bool IsFullScreen = false;
#pragma warning restore CS0414 // The field 'MainGame.IsFullScreen' is assigned but its value is never used

        public Matrix SpriteScale1, SpriteScale2;

        public bool disScale = false;

        public Rectangle fullScreenDestination;

        public SpriteBatch SpriteBatch;

        public bool? takePicture = null;
        RenderTarget2D screenshot;
        public string picture = "";

        public Vector2 errPos = Vector2.Zero;
        public string err = "";
        public string warn = "";
        public DateTime? lastWarnTime = null;
        public string view = "";
        
        public Texture2D renderLast = null;

        public bool isDebug = false;
        public bool loaded2 = false;
        public MainGame()
        {
            //第一步
            System.Console.WriteLine("MainGame constructor started...");
            Content.RootDirectory = "Content";
          
            Platform.MainGame = this;
            System.Console.WriteLine("Platform.MainGame set...");

            //if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            //{
            //    this.Window.IsBorderless = true;
            //}

            System.Console.WriteLine("Calling Platform.Current.PreparePhone()...");
            Platform.Current.PreparePhone();

            //獲取設置數據
            System.Console.WriteLine("Calling Setting.Init(false)...");
            Setting.Init(false);

            System.Console.WriteLine("Creating GlobalVariables...");
            // 🔥 技术性修复：安全创建GlobalVariables，避免初始化异常
            try
            {
                Session.globalVariablesBasic = new GlobalVariables();
                Session.globalVariablesBasic.InitialGlobalVariables();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGame] GlobalVariables初始化失败: {ex.Message}");
                // 创建最基本的GlobalVariables
                Session.globalVariablesBasic = new GlobalVariables();
            }

            System.Console.WriteLine("Creating Parameters...");
            // 🔥 技术性修复：安全创建Parameters，避免初始化异常
            try
            {
                Session.parametersBasic = new Parameters();
                Session.parametersBasic.InitializeGameParameters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGame] Parameters初始化失败: {ex.Message}");
                // 创建最基本的Parameters
                Session.parametersBasic = new Parameters();
            }

            //獲取設置數據
            System.Console.WriteLine("Calling Setting.Init(true)...");
            Setting.Init(true);

            System.Console.WriteLine("Calling Session.Init()...");
            Session.Init();

            //加速 TargetElapsedTime表示执行一帧所需要的时间
            IsFixedTimeStep = true;
            
            // 🔥 技术性修复：安全访问Setting.Current，避免ArgumentNullException
            int speedUp = 1;
            try
            {
                if (Setting.Current != null && Setting.Current.SpeedUp.HasValue)
                {
                    speedUp = Setting.Current.SpeedUp.Value;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainGame] Setting.Current或SpeedUp为null，使用默认值1");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGame] 获取SpeedUp失败: {ex.Message}，使用默认值1");
            }
            
            TargetElapsedTime = System.TimeSpan.FromMilliseconds(System.Math.Round(1000.0f / (60.0f * speedUp)));
            //TargetElapsedTime = new System.TimeSpan(0, 0, 0, 0, (int)System.Math.Round(1000.0f / (60.0f * Setting.Current.SpeedUp)));
            //TargetElapsedTime = System.TimeSpan.FromMilliseconds(16.666);

            //原本的分辨率默认值为720*1080，在其他大小的屏幕上会变形
            //将当前屏幕的分辨率作为默认值……
            //this.previousWindowWidth = WinHelper.GetSystemMetrics(WinHelper.SM_CXSCREEN);
            //this.previousWindowHeight = WinHelper.GetSystemMetrics(WinHelper.SM_CYSCREEN);
            //Platform.SetGraphicsWidthHeight(this.previousWindowWidth, this.previousWindowHeight);
            //this.graphics.PreferredBackBufferWidth = this.previousWindowWidth;
            //this.graphics.PreferredBackBufferHeight = this.previousWindowHeight;                      

            if (Platform.PlatFormType == PlatFormType.Win)  //Platform.PlatFormType == PlatFormType.UWP
            {
                DateTime buildDate = new FileInfo(Platform.Current.Location).LastWriteTime;
                base.Window.Title = "中华三国志---龍膽先行版" + buildDate.Year + "-" + buildDate.Month + "-" + buildDate.Day;
            }

            Platform.Current.SetMouseVisible(false);
            Platform.Current.SetBarStyle();
            Platform.Current.SetTimerDisabled(true);
            Platform.Current.ApplicationViewChanged();

            //System.Windows.Forms.Control control = System.Windows.Forms.Control.FromHandle(base.Window.Handle);
            //this.GameForm = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(this.Window.Handle);
            //this.GameForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;

            //this.GameForm = control as System.Windows.Forms.Form;
            //this.GameForm.KeyDown += new KeyEventHandler(this.GameForm_KeyDown);

            //int uFlags = 0x400;
            //IntPtr systemMenu = GetSystemMenu(base.Window.Handle, false);
            //int menuItemCount = GetMenuItemCount(systemMenu);
            //RemoveMenu(systemMenu, menuItemCount - 1, uFlags);
            //RemoveMenu(systemMenu, menuItemCount - 2, uFlags);

            //Plugin.Plugins.FindPlugins(AppDomain.CurrentDomain.BaseDirectory + "GameComponents");
            //Plugin.Plugins.FindPlugins(AppDomain.CurrentDomain.BaseDirectory + "GamePlugins");
            //this.mainGameScreen = new MainGameScreen(this);
            //base.Components.Add(this.mainGameScreen);
        }

        //private static bool AltComboPressed(KeyboardState state, Microsoft.Xna.Framework.Input.Keys key)
        //{
        //    return (state.IsKeyDown(key) && (state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) || state.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt)));
        //}

        //private void GameForm_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Alt && (e.KeyCode == System.Windows.Forms.Keys.F4))
        //    {
        //        e.Handled = true;
        //    }
        //}

        protected override void Initialize()
        {
            // 🔥 强制最大音量 (防止因为读取旧存档导致的静音)
            try 
            {
                Microsoft.Xna.Framework.Audio.SoundEffect.MasterVolume = 1.0f; 
                Microsoft.Xna.Framework.Media.MediaPlayer.Volume = 1.0f;
            }
            catch { /* 忽略音频设备不存在的异常 */ }

            //第二步
            //if (Platform.PlatFormType != PlatFormType.UWP)
            //{
                // 🔥 FIX: 注释掉第一次 ChangeDisplay，避免使用错误的 Viewport 尺寸
                // 第一次调用时 Viewport 还是默认值（800x480），会导致缩放矩阵错误
                // 改为在 base.Initialize() 之后使用实际尺寸
                System.Diagnostics.Debug.WriteLine($"[MainGame.Initialize] 跳过第一次 ChangeDisplay（Viewport 尚未就绪）");
                // Session.ChangeDisplay(true);  // 暂时注释
            //}

            //基本材質初始化
            Session.TextureRecs = TextureRecsManager.AllTextureRectangles();

            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            {
                Platform.Current.SetWindowAllowUserResizing(true);
                
            }

            //try
            //{
                this.mainMenuScreen = new MainMenuScreen();
            //}
            //catch (Exception ex)
            //{
            //    GameTools.SendErrMsg("MainMenuScreen", ex);
            //}

            //this.jiazaitishi.Close();
            ////全屏的判断放到初始化代码中
            //if (Session.GlobalVariables.FullScreen)
            //{
            //    this.ToggleFullScreen();
            //}

            this.Window.ClientSizeChanged += this.Window_ClientSizeChanged;

            base.Initialize();
            
            // 🔥 FIX: 在 base.Initialize() 之后，强制同步实际窗口尺寸
            // 问题根源：ChangeDisplay 使用 Session.Resolution（配置值），而不是实际 Viewport 尺寸
            // 解决方案：先更新 Session.Resolution 为实际尺寸，再重新应用显示设置
            int actualWidth = Platform.GraphicsDevice.Viewport.Width;
            int actualHeight = Platform.GraphicsDevice.Viewport.Height;
            
            System.Diagnostics.Debug.WriteLine($"[MainGame.Initialize] base.Initialize() 完成");
            System.Diagnostics.Debug.WriteLine($"[MainGame.Initialize] 实际Viewport尺寸: {actualWidth}x{actualHeight}");
            System.Diagnostics.Debug.WriteLine($"[MainGame.Initialize] 虚拟分辨率: {Session.Resolution}");
            System.Diagnostics.Debug.WriteLine($"[MainGame.Initialize] 当前SpriteScale1: {SpriteScale1}");
            
            // ✅ 修复：不要修改 Session.Resolution，它应该保持为固定的虚拟分辨率
            // Session.Resolution 已经在 Session.ChangeDisplay() 中根据宽高比正确设置为 1000*620 或 1024*768
            // 这里直接调用 ChangeDisplay，传入实际窗口尺寸进行缩放计算
            Session.ChangeDisplay(true);
            
            float scaleX = global::GameManager.ScreenManager.ScaleX;
            float scaleY = global::GameManager.ScreenManager.ScaleY;
            System.Diagnostics.Debug.WriteLine($"[MainGame.Initialize] 显示设置完成, SpriteScale1={SpriteScale1}, ScaleX={scaleX:F3}, ScaleY={scaleY:F3}");
            
            // [NEW] 初始化上帝模式 (在 base.Initialize 之后确保 GraphicsDevice 准备就绪)
            try {
                _imGuiManager = new global::GameManager.ImGuiManager(this);
                _imGuiManager.Initialize();
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[MainGame] ImGui Initialize failed: {ex.Message}");
            }
            
            // 🔥 初始化配置管理器（统一热重载机制）
            // 日期：2026-03-10
            WorldOfTheThreeKingdoms.GameLogic.Config.ConfigManagerCoordinator.Initialize();
            
            // 🔥 2026-03-13 修复：在 Initialize 最后设置窗口标题，确保不被覆盖
            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            {
                try
                {
                    DateTime buildDate = new FileInfo(Platform.Current.Location).LastWriteTime;
                    base.Window.Title = $"中华三国志---龍膽先行版 {buildDate:yyyy-MM-dd}";
                }
                catch (Exception ex)
                {
                    // 文件访问失败时使用默认标题（可能是权限问题或沙盒环境）
                    base.Window.Title = "中华三国志---龍膽先行版";
                    System.Diagnostics.Debug.WriteLine($"[MainGame.Initialize] 无法获取构建日期，使用默认标题: {ex.Message}");
                }
            }
            
            // 🔥 2026-03-15：启动看门狗，监控主线程心跳（检测死锁/无限循环/未响应）
            // 超时时间：30 秒（考虑大地图加载时间）
            try
            {
                _watchdog = new global::WorldOfTheThreeKingdoms.Tools.GameWatchdog(timeoutSeconds: 30);
                System.Diagnostics.Debug.WriteLine("[MainGame.Initialize] 看门狗已启动，超时阈值: 30 秒");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGame.Initialize] 看门狗启动失败: {ex.Message}");
                // 看门狗启动失败不影响游戏运行，只是失去超时保护
            }
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            // 限制窗口最小尺寸为 1280x720（UI 设计基准）
            int w = this.Window.ClientBounds.Width;
            int h = this.Window.ClientBounds.Height;
            if (w > 0 && h > 0 && (w < 1280 || h < 720))
            {
                Platform.SetGraphicsWidthHeight(Math.Max(w, 1280), Math.Max(h, 720));
                return;  // SetGraphicsWidthHeight 会再次触发本事件
            }

            if (mainGameScreen == null)
            {
                int width = Platform.GraphicsDevice.Viewport.Width;
                int height = Platform.GraphicsDevice.Viewport.Height;
                Session.ChangeStartDisplay(width, height);                
            }
            else
            {
                mainGameScreen.Window_ClientSizeChanged(sender, e);
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            //第三步
            
            // 🔥 FIX: 在 LoadContent 中再次确认缩放设置
            // 确保主菜单渲染前缩放矩阵已正确应用
            int actualWidth = Platform.GraphicsDevice.Viewport.Width;
            int actualHeight = Platform.GraphicsDevice.Viewport.Height;
            
            System.Diagnostics.Debug.WriteLine($"╔══════════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"║ [MainGame.LoadContent] 开始");
            System.Diagnostics.Debug.WriteLine($"║ Viewport尺寸: {actualWidth}x{actualHeight}");
            System.Diagnostics.Debug.WriteLine($"║ 当前SpriteScale1: {SpriteScale1}");
            System.Diagnostics.Debug.WriteLine($"║ 准备调用 Session.ChangeDisplay(true)...");
            
            // 🔥 修复：不要覆盖 Session.Resolution！
            // 日期：2026-03-20
            // 问题：Session.Resolution 应该保持为虚拟分辨率（如 1000*620 或 1024*768），而不是实际窗口尺寸
            // 原因：ChangeDisplay 会根据实际窗口尺寸和虚拟分辨率计算缩放矩阵
            // 如果将 Session.Resolution 设置为实际尺寸，会导致缩放矩阵计算错误（缩放比例变成 1.0）
            // Session.Resolution = $"{actualWidth}*{actualHeight}";  // ❌ 错误！
            Session.ChangeDisplay(true);
            
            System.Diagnostics.Debug.WriteLine($"║ Session.ChangeDisplay(true) 执行完成");
            System.Diagnostics.Debug.WriteLine($"║ 更新后SpriteScale1: {SpriteScale1}");
            System.Diagnostics.Debug.WriteLine($"║ 更新后SpriteScale2: {SpriteScale2}");
            System.Diagnostics.Debug.WriteLine($"║ InputManager.Scale1: {InputManager.Scale1}");
            System.Diagnostics.Debug.WriteLine($"║ InputManager.Scale2: {InputManager.Scale2}");
            System.Diagnostics.Debug.WriteLine($"╚══════════════════════════════════════════════════════════════");

            SpriteBatch = new SpriteBatch(Platform.GraphicsDevice);

            // [新增] 监听设备重置事件
            // 当显卡设备丢失或重置(如全屏切换、窗口大小剧变、锁定屏幕等)时触发
            // 这会导致所有与旧设备关联的资源失效(SpriteBatch, Texture2D等)
            Platform.GraphicsDevice.DeviceReset += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[MainGame] GraphicsDevice Reset Detected! New Device ID: {Platform.GraphicsDevice.GetHashCode()}");
                
                // [修复] 重置 TextManager 的静态资源，防止持有旧设备的引用导致 VertexBuffer 崩溃
                global::GameManager.TextManager.Reset(); 

                // [修复] 重置 CacheManager，清除旧设备的纹理资源
                global::GameManager.CacheManager.Reset();
                // 重新初始化 LRU 缓存 (使用新设备)
                global::GameManager.CacheManager.InitializeLRUCache(Platform.GraphicsDevice, 1024); 

                // [修复] 重新初始化头像管理器，更新 GraphicsDevice 引用
                try
                {
                    Texture2D defaultLoadingTexture = null;
                    try
                    {
                        defaultLoadingTexture = Platform.Current.LoadTexture(@"Content\Textures\Resources\Loading.png", false);
                    }
                    catch { }
                    WorldOfTheThreeKingdoms.GameGlobal.PortraitManager.Instance.Initialize(Platform.GraphicsDevice, @"Content\Textures\PersonPortrait", defaultLoadingTexture);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGame] PortraitManager Re-Initialize failed: {ex.Message}");
                }

                // [修复] 重建 SpriteBatch，确保其持有最新的 GraphicsDevice 引用
                try 
                {
                   if (SpriteBatch != null && !SpriteBatch.IsDisposed)
                   {
                        SpriteBatch.Dispose();
                   }
                   SpriteBatch = new SpriteBatch(Platform.GraphicsDevice);
                   System.Diagnostics.Debug.WriteLine("[MainGame] SpriteBatch reconstructed successfully.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGame] Error reconstructing SpriteBatch: {ex.Message}");
                }
            };



            // 初始化 LRU 纹理缓存系统
            // 根据系统配置设置显存预算：低端设备 512MB，中端 1GB，高端 2GB
            long memoryBudget = 1024; // 默认 1GB
            try
            {
                // 可以根据系统内存或显卡信息调整预算
                var totalMemory = GC.GetTotalMemory(false) / 1024 / 1024; // MB
                if (totalMemory < 2048) // 小于 2GB 系统内存
                    memoryBudget = 512;
                else if (totalMemory > 8192) // 大于 8GB 系统内存
                    memoryBudget = 2048;
                
                CacheManager.InitializeLRUCache(Platform.GraphicsDevice, memoryBudget);
                System.Diagnostics.Debug.WriteLine($"[MainGame] LRU缓存初始化完成，显存预算: {memoryBudget} MB");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGame] LRU缓存初始化失败: {ex.Message}");
            }


            Session.LoadContent(base.Content);

            // 初始化头像管理系统
            try
            {
                // 加载默认加载中纹理（可选，传null则返回null）
                Texture2D defaultLoadingTexture = null;
                try
                {
                    defaultLoadingTexture = Platform.Current.LoadTexture(@"Content\Textures\Resources\Loading.png", false);
                }
                catch { /* 没有默认纹理也没关系 */ }
                
                global::WorldOfTheThreeKingdoms.GameGlobal.PortraitManager.Instance.Initialize(
                    Platform.GraphicsDevice, 
                    @"Content\Textures\PersonPortrait",
                    defaultLoadingTexture
                );
                System.Diagnostics.Debug.WriteLine("[MainGame] 头像管理系统初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGame] 头像管理系统初始化失败: {ex.Message}");
            }

            // 初始化音频管理系统
            try
            {
                global::GameManager.AudioManager.Instance.Initialize(base.Content);
                System.Diagnostics.Debug.WriteLine("[MainGame] 音频管理系统初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGame] 音频管理系统初始化失败: {ex.Message}");
            }

            // 使用新的AudioManager播放开始音乐
            AudioManager.Instance?.PlayStartMusic();
            
        }

        public void ToggleFullScreen()
        {
            //bool full = Setting.Current.DisplayMode == "Full";
            //if (full)
            //{
            //    Setting.Current.DisplayMode = "Window";
            //    Platform.Current.SetFullScreen(false);
            //}
            //else
            //{
            //    Setting.Current.DisplayMode = "Full";
            //    Platform.Current.SetFullScreen(true);
            //}

            //Setting.Save();

            //Platform.GraphicsApplyChanges();

            //原来的代码会和单挑程序冲突
            /*
            if (this.graphics.GraphicsDevice.PresentationParameters.IsFullScreen)
            {
                this.graphics.PreferredBackBufferWidth = this.previousWindowWidth;
                this.graphics.PreferredBackBufferHeight = this.previousWindowHeight;
            }
            else
            {
                this.previousWindowWidth = this.graphics.GraphicsDevice.Viewport.Width;
                this.previousWindowHeight = this.graphics.GraphicsDevice.Viewport.Height;
                GraphicsAdapter adapter = this.graphics.GraphicsDevice.CreationParameters.Adapter;
                FullScreenHelper.FullScreen();
                this.graphics.PreferredBackBufferWidth = adapter.CurrentDisplayMode.Width;
                this.graphics.PreferredBackBufferHeight = adapter.CurrentDisplayMode.Height;
            }
            this.graphics.ToggleFullScreen();
            Session.GlobalVariables.FullScreen = this.graphics.GraphicsDevice.PresentationParameters.IsFullScreen;
             */

            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            {
                Platform.Current.SetFullScreen2(this.IsFullScreen);                

                this.IsFullScreen = !this.IsFullScreen;
            }

            ////修改后的全屏代码
            //if (this.IsFullScreen)
            //{
            //    WinHelper.RestoreFullScreen(this.GameForm.Handle);//传入窗体句柄
            //    this.IsFullScreen = false;

            //}
            //else
            //{
            //    WinHelper.FullScreen(this.GameForm.Handle);
            //    this.IsFullScreen = true;
            //}
        }

        private void TryToExit()
        {
            this.mainGameScreen.TryToExit();
        }

        protected override void Update(GameTime gameTime)
        {
            // 🔥 2026-03-15：性能检查点 - Release 模式下也能定位卡顿
            global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("MainGame.Update 开始");
            
            #if DEBUG
            var updateStartTime = DateTime.Now;
#endif

            // 🔥 2026-03-13：更新看门狗心跳（必须在 Update 最开始调用）
            _watchdog?.UpdateHeartbeat();

            global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("FrameworkDispatcher.Update 前");
            
            // 🔥 核心修复：必须每一帧调用这个，否则声音会卡在内存里播放不出来！
            // 这行代码是 MonoGame 音频系统的"心脏起搏器"
            Microsoft.Xna.Framework.FrameworkDispatcher.Update();

            //第四步
            // 🛡️ Watchdog for Device Lost Lockup
            // If device is lost but DeviceReset event isn't firing, force a check/reset attempts
            if (global::GameManager.CacheManager.IsDeviceLost)
            {
               if (Platform.GraphicsDevice != null && !Platform.GraphicsDevice.IsDisposed)
               {
                   // Try to detect if device is actually back
                   // Force a reset event 
                    System.Diagnostics.Debug.WriteLine("[MainGame] Watchdog: Device is marked lost but object seems valid. Attempting recovery...");
                    // Manually trigger reset logic
                    try {
                        global::GameManager.CacheManager.Reset();
                        global::GameManager.CacheManager.InitializeLRUCache(Platform.GraphicsDevice);
                    } catch {} 
               }
            }

            time += Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds);

            // 🔥 GPU 设备恢复尝试 - 如果设备丢失，尝试恢复
            CacheManager.TryRecoverDevice();

            // 更新性能监控 (暂时禁用)
            // PerformanceMonitor.Update(gameTime);

            // [新增] 确保音频管理器每帧更新
            if (global::GameManager.AudioManager.Instance != null)
            {
                global::GameManager.AudioManager.Instance.Update(gameTime);
            }

            // 🔥 更新配置热重载检查（必须在主线程）
            // 日期：2026-03-10
            // 性能：HOT PATH 优化，早期短路，< 0.1ms/帧
            WorldOfTheThreeKingdoms.GameLogic.Config.ConfigManagerCoordinator.Update();

            base.Update(gameTime);

            // [NEW] 更新 ImGui (处理 F12 和 输入状态)
            if (_imGuiManager != null) _imGuiManager.Update(gameTime);

            // 处理异步加载的头像纹理
            global::WorldOfTheThreeKingdoms.GameGlobal.PortraitManager.Instance.Update();

            // [NEW] 【核心防御】如果 ImGui 想要鼠标，就不要让游戏处理输入了！
            bool inputBlocked = _imGuiManager != null && _imGuiManager.WantsCapture;

            global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("输入处理前");
            
            if ((base.IsActive || Setting.Current.GlobalVariables.RunWhileNotFocused) && !inputBlocked)
            {
                if (Platform.Current.InputTextNow())
                {
                    return;
                }

                InputManager.Update(Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds));

                global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("InputManager.Update 完成");
                
                if (loadingScreen == null)
                {
                    if (mainGameScreen == null)
                    {
                        if (mainMenuScreen == null)
                        {

                        }
                        else
                        {
                            global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("mainMenuScreen.Update 前");
                            
                            mainMenuScreen.Update(gameTime);
                            
                            global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("mainMenuScreen.Update 完成");
                            
                        }
                    }
                    else
                    {
                        if (isDebug)
                        {
                            global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("mainGameScreen.Update 前 (Debug模式)");
                            
                            mainGameScreen.Update(gameTime);
                            
                            global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("mainGameScreen.Update 完成");
                            
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(err))
                            {
                                #if DEBUG
                                mainGameScreen.Update(gameTime);
        #else
                                try
                                {
                                    mainGameScreen.Update(gameTime);
                                }
                                catch (Exception ex)
                                {
                                    err = "不好意思，游戏运行出错，点击将返回主菜单，请考虑读取自动存档。\r\n" + ex.Message;
                                    WebTools.TakeWarnMsg("mainGameScreen.Update", "", ex);
                                }
        #endif
                                global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("mainGameScreen.Update 完成");
                            }
                            else
                            {
                                if (InputManager.IsPressed)
                                {
                                    err = "";

                                    //保存當前進度
                                    mainGameScreen.SaveGameAutoPosition();

                                    loadingScreen = new LoadingScreen("End", "");
                                    loadingScreen.LoadScreenEvent += (sender0, e0) =>
                                    {
                                        Platform.Sleep(1000);
                                    };
                                }
                            }
                        }
                    }
                }
                else
                {
                    global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("loadingScreen.Update 前");
                    
                    loadingScreen.Update(gameTime);
                    
                    global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("loadingScreen.Update 完成");
                    
                    // 🔥 核心修复：执行后台线程注册的主线程初始化任务
                    // 日期：2026-03-16
                    // 原因：LoadScreenEvent 在后台线程中注册 PendingMainThreadInitialization，
                    //       必须在主线程中执行，否则 mainGameScreen 永远为 null，导致回到主菜单
                    if (Session.PendingMainThreadInitialization != null)
                    {
                        global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("PendingMainThreadInitialization 执行前");
                        
                        try
                        {
                            var action = Session.PendingMainThreadInitialization;
                            Session.PendingMainThreadInitialization = null; // 清空，避免重复执行
                            
                            action.Invoke(); // 执行初始化（创建 mainGameScreen）
                            
                            // 🔥 关键：初始化完成后，清除 LoadingScreen
                            if (loadingScreen != null && loadingScreen.Mode != "Start")
                            {
                                loadingScreen = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainGame.Update] ❌ PendingMainThreadInitialization 执行失败: {ex}");
                            
                            // 🔥 Release 模式：写入文件日志
                            string logPath = System.IO.Path.Combine(
                                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                                "crash_log.txt");
                            try
                            {
                                System.IO.File.AppendAllText(logPath, 
                                    $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] PendingMainThreadInitialization 失败:\n{ex}\n\n---\n");
                            }
                            catch { }
                            
                            throw; // 重新抛出异常
                        }
                        
                        global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("PendingMainThreadInitialization 执行完成");
                    }
                }
            }

            global::WorldOfTheThreeKingdoms.Tools.PerformanceCheckpoint.Mark("MainGame.Update 结束");
            
        }

        protected override void Draw(GameTime gameTime)
        {
            try
            {
                //第五步

            if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.UWP || Platform.PlatFormType == PlatFormType.Android || Platform.PlatFormType == PlatFormType.Desktop)
            {
                if (takePicture == true)
                {
                    try
                    {
                        if (screenshot == null || screenshot.GraphicsDevice == null)
                        {
                            screenshot = new RenderTarget2D(Platform.GraphicsDevice, Platform.GraphicsDevice.Viewport.Width, Platform.GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None);
                        }
                        Platform.GraphicsDevice.SetRenderTarget(screenshot);
                    }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
                    catch (Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
                    {
                        //Log...
                    }
                    //takePicture = false;
                }
            }

            //var spriteMode = mainGameScreen == null ? SpriteSortMode.Deferred : SpriteSortMode.BackToFront;

            // [Crash Fix] Rendering Gate: Skip rendering if parallel AI is working
            // This prevents VertexBuffer NRE caused by resource contention
            if (Session.Current.IsWorking)
            {
                 return;
            }

            // 🔥 修复：GPU 设备恢复检查必须在 SpriteBatch.Begin() 之前
            // 原因：如果在 Begin() 之后 return，会导致 End() 未调用，下一帧抛出 InvalidOperationException
            if (!CacheManager.TryRecoverDevice())
            {
                // 如果设备无法恢复或正在重置中，跳过绘制
                return;
            }

            var spriteMode = mainGameScreen == null || loadingScreen != null ? SpriteSortMode.Deferred : SpriteSortMode.BackToFront;



            if (disScale) //Platform.PlatFormType == PlatForm.iOS && isRetina)
            {
                if (mainGameScreen == null || loadingScreen != null)
                {
                    // 必须使用 LinearClamp 或 AnisotropicClamp (各向异性过滤，效果最好)
                    SpriteBatch.Begin(spriteMode, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, SpriteScale1);
                }
                else
                {
                    // 必须使用 LinearClamp 或 AnisotropicClamp (各向异性过滤，效果最好)
                    SpriteBatch.Begin(spriteMode, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, SpriteScale2);
                }
            }
            else
            {
                if (mainGameScreen == null || loadingScreen != null)
                {
                    // 必须使用 LinearClamp 或 AnisotropicClamp (各向异性过滤，效果最好)
                    SpriteBatch.Begin(spriteMode, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, SpriteScale1);
                }
                else
                {
                    // 必须使用 LinearClamp 或 AnisotropicClamp (各向异性过滤，效果最好)
                    SpriteBatch.Begin(spriteMode, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, SpriteScale2);
                }
            }

            Platform.GraphicsDevice.Clear(Color.Transparent);
            //this.graphics.GraphicsDevice.Clear(Color.Transparent);

            if (loadingScreen == null)
            {
                if (mainGameScreen == null)
                {
                    if (mainMenuScreen == null)
                    {

                    }
                    else
                    {
                        mainMenuScreen.Draw(gameTime);
                    }
                }
                else
                {
                    if (isDebug)
                    {
                        mainGameScreen.Draw(gameTime);
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(err))
                        {
#if DEBUG
                            mainGameScreen.Draw(gameTime);
#else
                                try
                                {
                                    mainGameScreen.Draw(gameTime);
                                }
                                catch (Exception ex)
                                {
                                    err = "不好意思，游戏运行出错，点击将返回主菜单，请考虑读取自动存档。\r\n" + ex.Message;
                                    WebTools.TakeWarnMsg("mainGameScreen.Draw", "", ex);
                                }
#endif

                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(err))
                            {
                                CacheManager.DrawString(Session.Current.Font, err.SplitLineString(100), errPos, Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                            }

                            if (InputManager.IsPressed)
                            {
                                err = "";

                                //保存當前進度
                                mainGameScreen.SaveGameAutoPosition();

                                loadingScreen = new LoadingScreen("End", "");
                                loadingScreen.LoadScreenEvent += (sender0, e0) =>
                                {
                                    Platform.Sleep(1000);
                                };
                            }
                        }
                    }
                }
            }
            else
            {
                loadingScreen.Draw(gameTime);
            }

            //view = Platform.Current.MemoryUsage;
            //if (!String.IsNullOrEmpty(view))
            //{
            //    CacheManager.DrawString(Session.Current.Font, "view:" + view.SplitLineString(100), errPos, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            //}

            //if (!String.IsNullOrEmpty(warn) && lastWarnTime != null && (DateTime.Now - (DateTime)lastWarnTime).TotalSeconds < 20)
            //{
            //    CacheManager.DrawString(Session.Current.Font, "Warn:" + warn, errPos, Color.Red, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 1f);
            //}

            ////if (!String.IsNullOrEmpty(err))
            ////{
            ////CacheManager.DrawString(LightAncient, "Err:" + err.SplitLineString(100).ProcessStar(), errPos, Color.Red, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 1f);
            ////}

            if (renderLast != null)
            {
                // 防御式绘制：确保纹理未被释放
                if (!renderLast.IsDisposed)
                {
                    SpriteBatch.Draw(renderLast, Vector2.Zero, Color.White);
                }
            }

            // 绘制性能监控信息（在 SpriteBatch.End() 之前） (暂时禁用)
            // PerformanceMonitor.Draw(gameTime);

            // 为了防止底层 SharpDX 在 End 阶段因单个纹理异常而导致整个游戏崩溃，这里增加保护
            try
            {

                SpriteBatch.End();
            }
            catch (System.Exception ex)
            {
                // 记录错误并尝试继续运行，避免直接崩溃到桌面
                WebTools.TakeWarnMsg("SpriteBatch.End 出错", "", ex);
            }

            if (takePicture == true && String.IsNullOrEmpty(err))
            {
                takePicture = false;
                try
                {
                    if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.UWP || Platform.PlatFormType == PlatFormType.Android || Platform.PlatFormType == PlatFormType.Desktop)  // || Platform.PlatFormType == PlatForm.iOS)
                    {
                        if (screenshot != null)
                        {
                            byte[] shot = Platform.Current.ScreenShot(Platform.GraphicsDevice, screenshot);
                            //Season.GraphicsDevice.SetRenderTarget(null);

                            if (shot != null && shot.Length > 0)
                            {
                                var task = new PlatformTask(() => { });
                                task.OnStartFinish += (result) =>
                                {
                                    if (task.ParamArrayResultBytes != null && task.ParamArrayResultBytes.Length > 0)
                                    {
                                        Platform.Current.SaveUserFile(picture, task.ParamArrayResultBytes, null);
                                    }
                                };
                                Platform.Current.ResizeImageFile(shot, 800, 480, false, task);
                            }
                        }
                        //Task.Run(async () => await Season.Current.SaveUserFile(picture, shot));
                        //var task = new SeasonTask(() =>
                        //{
                        //    screenshot = new RenderTarget2D(Season.GraphicsDevice, 800, 480, false, SurfaceFormat.Color, DepthFormat.None);
                        //});
                    }
                }
                catch (Exception ex)
                {
                    WebTools.TakeWarnMsg("游戏界面截屏失败:", "takePicture：", ex);
                }
            }

            base.Draw(gameTime);
            
            // [NEW] 永远最后绘制 ImGui，保证它在最上层
            if (_imGuiManager != null) _imGuiManager.Draw(gameTime);
            }
            catch (ArgumentNullException argNullEx)
            {
                // 🔥 捕获 MonoGame 在设备丢失或资源销毁期间可能抛出的异常 (如 Texture为null调用 Draw)
                // 这种情况通常发生在设备恢复的过渡帧，忽略即可
                System.Diagnostics.Debug.WriteLine($"[MainGame.Draw] ArgumentNullException caught (Frame Skipped): {argNullEx.Message}");
#if DEBUG
                // 🔥 诊断：记录状态重置（仅 Debug 模式）
                System.Diagnostics.Debug.WriteLine($"[MainGame.Draw] 尝试重置 SpriteBatch 状态");
#endif
                // 尝试重置 SpriteBatch 状态，防止下一帧抛出 InvalidOperationException (Begin called twice)
                // 忽略 End() 抛出的任何异常 (如 "Begin not called")
                try { SpriteBatch.End(); } catch { }
            }
            catch (Exception ex)
            {
                // 捕获其他渲染异常，防止Crash
                System.Diagnostics.Debug.WriteLine($"[MainGame.Draw] Exception caught: {ex.Message}");
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[MainGame.Draw] StackTrace: {ex.StackTrace}");
                // 🔥 诊断：记录状态重置（仅 Debug 模式）
                System.Diagnostics.Debug.WriteLine($"[MainGame.Draw] 尝试重置 SpriteBatch 状态");
#endif
                // 尝试重置 SpriteBatch 状态
                try { SpriteBatch.End(); } catch { }

                if (ex.Message.Contains("DeviceRemoved") || ex.Message.Contains("device is lost"))
                {
                    global::GameManager.CacheManager.MarkDeviceLost(ex);
                }
            }
        }

        public void SaveGameWhenCrash(String _savePath)
        {
            // 🔥 防御性检查：崩溃可能发生在 mainGameScreen 初始化之前
            // 这不是 Band-Aid，而是崩溃上下文的必要保护
            if (this.mainGameScreen == null)
            {
                throw new InvalidOperationException("无法生成救援存档：游戏主屏幕尚未初始化（崩溃发生在游戏加载阶段）");
            }
            
            this.mainGameScreen.SaveGameWhenCrash(_savePath);
        }

        // 🔥 修复：临时存储InitializationFactionIDs，直到mainGameScreen创建
        private List<int> _tempInitializationFactionIDs = null;

        public List<int> InitializationFactionIDs
        {
            set
            {
                if (this.mainGameScreen != null)
                {
                    this.mainGameScreen.InitializationFactionIDs = value;
                }
                else
                {
                    // mainGameScreen还未创建，临时存储
                    this._tempInitializationFactionIDs = value;
                    System.Diagnostics.Debug.WriteLine($"[MainGame] 临时存储InitializationFactionIDs: [{string.Join(", ", value ?? new List<int>())}]");
                }
            }
        }

        /// <summary>
        /// 将临时存储的InitializationFactionIDs传递给新创建的mainGameScreen
        /// </summary>
        public void TransferTempInitializationFactionIDs(MainGameScreen mainGameScreen)
        {
            if (this._tempInitializationFactionIDs != null)
            {
                mainGameScreen.InitializationFactionIDs = this._tempInitializationFactionIDs;
                System.Diagnostics.Debug.WriteLine($"[MainGame] 传递临时存储的InitializationFactionIDs: [{string.Join(", ", this._tempInitializationFactionIDs)}]");
                this._tempInitializationFactionIDs = null; // 清理临时存储
            }
        }

        public string InitializationFileName
        {
            set
            {
                this.mainGameScreen.InitializationFileName = value;
            }
        }

        public bool LoadScenarioInInitialization
        {
            set
            {
                this.mainGameScreen.LoadScenarioInInitialization = value;
            }
        }
        
        //public void PlayMusic()
        //{
            //Player.currentPlaylist.clear();
            //WMPLib.IWMPMedia media;

            //string[] filePaths = Directory.GetFiles("GameMusic/Start/", "*.mp3");
            //Random rd = new Random();
            //int index = rd.Next(0, filePaths.Length);
            //string path = filePaths[index];

            //foreach (String s in filePaths)
            //{
            //    media = Player.newMedia(s);
            //    Player.currentPlaylist.appendItem(media);
            //}
            //media = Player.newMedia(path);
            //Player.currentPlaylist.appendItem(media);
            //Player.currentItem = media;
            //Player.play();
            //Player.settings.setMode("loop", true);
        //}

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            //第六步
            // TODO: Unload any non ContentManager content here
            // global::GameManager.TextManager.Reset(); // 注释掉：外部GameManager可能没有Reset方法
            global::GameManager.CacheManager.Reset();
            
            // 清理音频管理器
            try
            {
                global::GameManager.AudioManager.Instance?.Dispose();
                System.Diagnostics.Debug.WriteLine("[MainGame] AudioManager 已清理");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGame] 清理 AudioManager 时出错: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 清理音频管理器
                try
                {
                    global::GameManager.AudioManager.Instance?.Dispose();
                    System.Diagnostics.Debug.WriteLine("[MainGame] Dispose 中清理 AudioManager 完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGame] Dispose 中清理 AudioManager 出错: {ex.Message}");
                }
                
                // 🔥 2026-03-13：停止看门狗
                try
                {
                    _watchdog?.Dispose();
                    System.Diagnostics.Debug.WriteLine("[MainGame] Dispose 中停止看门狗完成");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGame] Dispose 中停止看门狗出错: {ex.Message}");
                }
            }
            
            Plugin.Plugins.ClosePlugins();
            base.Dispose(disposing);
        }

        //[DllImport("user32.dll")]
        //public static extern int GetMenuItemCount(IntPtr hMenu);
        //[DllImport("user32.dll")]
        //public static extern IntPtr GetSystemMenu(IntPtr hwnd, bool bRevert);
        //[DllImport("user32.dll")]
        //public static extern int RemoveMenu(IntPtr hMenu, int uPosition, int uFlags);

        //public void Processing()
        //{
        //    //formMainMenu menu = new formMainMenu
        //    //{
        //    //    mainGame = this
        //    //};

        //    if (menu.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //    {
        //        mainGame.jiazaitishi.Show();
        //    }
        //}
    }
}