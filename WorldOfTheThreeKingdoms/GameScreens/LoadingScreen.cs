using GameManager;
using GameObjects;
using GamePanels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public class LoadingScreen
    {
        public string Mode = "Start";  //End

        public string Scenario = "";

        string[] maps = null;

        string sound = "";

        string background = "";
        string tishi = "";
        float textPre = 0f;

        float elapsedTime = 0f;

        float pageTime = 0f;

        Vector2 backgroundScale = Microsoft.Xna.Framework.Vector2.One;  // new Vector2(1280f/1024f, 720/768f);

        int page = 1;

        bool pause = false;

        public bool IsLoading = false;
        public bool IsComplete = false;
        public event EventHandler LoadScreenEvent;
        
        // 🔥 诊断：静态标志，确保只记录一次（避免 Hot Path 文件 I/O）
        private static bool _updateLoggedOnce = false;
        private static bool _thresholdLoggedOnce = false;

        ButtonTexture btPre, btPlay, btPause, btNext, btLoad, btStart;

        public void ClearEvent()
        {
            LoadScreenEvent = null;
        }

        public LoadingScreen(string mode, string scenario)
        {
            Mode = mode;

            // 🔥 技术性修复：避免IndexOutOfRangeException
            var scenarioParts = scenario.NullToString().Split('-');
            Scenario = scenarioParts.Length > 0 ? scenarioParts[0] : scenario.NullToString();
            
            // 🔥 诊断：记录 LoadingScreen 构造参数
            string logPath = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "loading_screen_log.txt");
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen] 构造函数\n" +
                             $"  mode={mode}\n" +
                             $"  scenario={scenario}\n" +
                             $"  Scenario={Scenario}\n" +
                             $"  Mode={Mode}\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }
            
            if (Mode == "Start")
            {
                string baseDir = @"Content\Textures\Resources\ScenarioLoading\Maps\";

                var dirs = Platform.Current.GetMODDirectories(baseDir, true).NullToEmptyArray();

                // 🔥 诊断：记录找到的目录
                try
                {
                    string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen] GetMODDirectories 返回 {dirs.Length} 个目录\n";
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        msg += $"  [{i}] {dirs[i]}\n";
                    }
                    System.IO.File.AppendAllText(logPath, msg);
                }
                catch { }

                var dir = dirs.FirstOrDefault(di => di.Contains(Scenario));

                // 🔥 诊断：记录匹配结果
                try
                {
                    string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen] 查找包含 '{Scenario}' 的目录: {(dir == null ? "未找到" : dir)}\n";
                    System.IO.File.AppendAllText(logPath, msg);
                }
                catch { }

                if (dir == null)
                {
                    // 🔥 诊断：记录 Mode 被清空
                    try
                    {
                        string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen] ⚠️ 未找到地图目录，Mode 被设置为空字符串\n";
                        System.IO.File.AppendAllText(logPath, msg);
                    }
                    catch { }
                    Mode = "";
                }
                else
                {
                    maps = Platform.Current.GetMODFiles(dir + "/", true).NullToEmptyArray()
                        .Where(fi => fi.EndsWith(".dds") || fi.EndsWith(".png") || fi.EndsWith(".jpg"))
                        .NullToEmptyArray();  //.Select(fi => baseDir + Scenario + @"\" + fi).NullToEmptyArray();
                    
                    // 🔥 诊断：记录找到的地图文件
                    try
                    {
                        string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen] 找到 {maps.Length} 个地图文件\n";
                        System.IO.File.AppendAllText(logPath, msg);
                    }
                    catch { }
                }

                var soundDir = @"Content\Sound\Scenario\";

                var soundFiles = Platform.Current.GetMODFiles(soundDir, true).NullToEmptyArray();

                sound = soundFiles.FirstOrDefault(fi => fi.Contains(Scenario));

                if (!String.IsNullOrEmpty(sound))
                {
                    Platform.Current.PlayEffect(sound);
                }
            }
            
            // 🔥 诊断：记录构造函数完成状态
            try
            {
                string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen] 构造函数完成\n" +
                             $"  最终 Mode={Mode}\n" +
                             $"  maps.Length={maps?.Length ?? -1}\n";
                System.IO.File.AppendAllText(logPath, msg);
            }
            catch { }

            if (Mode == "Start")
            {
                btPre = new ButtonTexture(@"Content\Textures\Resources\Start\Play", "Pre", new Vector2(200 - 50, 640));
                btPre.OnButtonPress += (sender, e) =>
                {
                    if (page > 1)
                    {
                        page--;
                        pageTime = 0f;
                        if (Platform.IsMobilePlatForm)
                        {
                            InputManager.PoX = 0;
                            InputManager.PoY = 0;
                        }
                    }
                };

                btPlay = new ButtonTexture(@"Content\Textures\Resources\Start\Play", "Play", new Vector2(200 + 220, 640));
                btPlay.OnButtonPress += (sender, e) =>
                {
                    pause = false;
                };

                btPause = new ButtonTexture(@"Content\Textures\Resources\Start\Play", "Pause", new Vector2(200 + 220, 640));
                btPause.OnButtonPress += (sender, e) =>
                {
                    pause = true;
                };

                btNext = new ButtonTexture(@"Content\Textures\Resources\Start\Play", "Next", new Vector2(200 + 220 * 2, 640));
                btNext.OnButtonPress += (sender, e) =>
                {
                    if (page < maps.Length)
                    {
                        page++;
                        pageTime = 0f;
                        if (Platform.IsMobilePlatForm)
                        {
                            InputManager.PoX = 0;
                            InputManager.PoY = 0;
                        }
                    }
                };

                btLoad = new ButtonTexture(@"Content\Textures\Resources\Start\Play", "Load", new Vector2(200 + 220 * 3 + 100, 640));
                btLoad.Enable = false;

                btStart = new ButtonTexture(@"Content\Textures\Resources\Start\Play", "Start", new Vector2(200 + 220 * 3 + 100, 640));
                btStart.OnButtonPress += (sender, e) =>
                {
                    Session.MainGame.loadingScreen = null;
                };
            }
            else
            {
                var baseDir = @"Content\Textures\Resources\ScenarioLoading\";

                var pictures = Platform.Current.GetMODFiles(baseDir, true).NullToEmptyArray()
                    .Where(pi => pi.EndsWith(".dds") || pi.EndsWith(".png") || pi.EndsWith(".jpg"))
                    .NullToEmptyArray();  //.Select(fi => baseDir + fi).NullToEmptyArray();

                if (pictures.Length > 0)
                {
                    int ran = new Random().Next(1, pictures.Length);

                    //string ranStr = ran < 10 ? ("0" + ran) : ran.ToString();

                    background = pictures[ran-1];  // "Content/Textures/Resources/ScenarioLoading/" + ranStr + ".jpg";

                    tishi = new TiShiText().getRandomText();

                    var textLength = 22.4 * tishi.Trim().Length;

                    textPre = Convert.ToSingle((845 - textLength) / 2);
                }

            }


        }

        public void Load()
        {

        }

        public void Update(GameTime gameTime)
        {
            // 🔥 诊断：只记录第一次调用（避免 Hot Path 文件 I/O）
            if (!_updateLoggedOnce)
            {
                _updateLoggedOnce = true;
                string logPath = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "loading_screen_log.txt");
                try
                {
                    string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen.Update] 首次被调用\n" +
                                 $"  elapsedTime={elapsedTime:F3}\n" +
                                 $"  Mode={Mode}\n" +
                                 $"  IsLoading={IsLoading}\n" +
                                 $"  LoadScreenEvent == null: {LoadScreenEvent == null}\n";
                    System.IO.File.AppendAllText(logPath, msg);
                }
                catch { }
            }
            
            float seconds = Convert.ToSingle(gameTime.ElapsedGameTime.TotalSeconds);

            elapsedTime += seconds;

            if (Mode == "Start")
            {
                if (pause)
                {
                    btPlay.Visible = true;
                    btPause.Visible = false;
                }
                else
                {
                    btPlay.Visible = false;
                    btPause.Visible = true;

                    if (page < maps.Length)
                    {
                        pageTime += seconds;

                        if (pageTime >= Session.globalVariablesBasic.ScenarioMapPerTime)
                        {
                            page++;
                            pageTime -= Session.globalVariablesBasic.ScenarioMapPerTime;
                        }
                    }
                }

                if (IsComplete)
                {
                    btLoad.Visible = false;
                    btStart.Visible = true;
                }
                else
                {
                    btLoad.Visible = true;
                    btStart.Visible = false;
                }

                btPre.Enable = btNext.Enable = true;

                if (page <= 1)
                {
                    btPre.Enable = false;
                }

                if (page >= maps.Length)
                {
                    btNext.Enable = false;
                }

                btPre.Update();

                btPlay.Update();

                btPause.Update();

                btNext.Update();

                btLoad.Update();

                btStart.Update();
            }
            else
            {

            }

            if (elapsedTime >= 0.2f)
            {
                // 🔥 诊断：只记录第一次进入阈值分支（避免 Hot Path 文件 I/O）
                if (!_thresholdLoggedOnce)
                {
                    _thresholdLoggedOnce = true;
                    string logPath = System.IO.Path.Combine(
                        AppContext.BaseDirectory,
                        "loading_screen_log.txt");
                    try
                    {
                        string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen.Update] 首次进入 elapsedTime >= 0.2f 分支\n" +
                                     $"  IsLoading={IsLoading}\n" +
                                     $"  LoadScreenEvent == null: {LoadScreenEvent == null}\n";
                        System.IO.File.AppendAllText(logPath, msg);
                    }
                    catch { }
                }
                
                if (IsLoading)
                {

                }
                else
                {
                    if (LoadScreenEvent == null)
                    {
                        // 🔥 诊断：记录 LoadScreenEvent 为 null（只记录一次）
                        string logPath = System.IO.Path.Combine(
                            AppContext.BaseDirectory,
                            "loading_screen_log.txt");
                        try
                        {
                            string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen.Update] ⚠️ LoadScreenEvent 为 null，无法触发事件\n";
                            System.IO.File.AppendAllText(logPath, msg);
                        }
                        catch { }
                    }
                    else
                    {
                        // 🔥 诊断：记录准备触发事件（只记录一次）
                        string logPath = System.IO.Path.Combine(
                            AppContext.BaseDirectory,
                            "loading_screen_log.txt");
                        try
                        {
                            string msg = $"[{System.DateTime.Now:HH:mm:ss.fff}] [LoadingScreen.Update] ✅ 准备触发 LoadScreenEvent\n";
                            System.IO.File.AppendAllText(logPath, msg);
                        }
                        catch { }
                        
                        IsLoading = true;

                        if (Session.MainGame.mainGameScreen != null)
                        {
                            Session.MainGame.mainGameScreen.mainMapLayer.StopThreads();

                            Session.MainGame.mainGameScreen.DisposeMapTileMemory(false, true);

                            Session.MainGame.mainGameScreen.Dispose();

                            Session.MainGame.mainGameScreen = null;

                            GameScenario.ProcessCommonData(CommonData.Current);
                        }

                        // 🔥 关键修复：不要调用 Session.Current.Clear()！
                        // 日期：2026-03-15
                        // 原因：Session.StartScenario() 已经加载了剧本数据，包括 ScenarioMap
                        //       如果在这里调用 Clear()，会清空所有数据，导致 MapDimensions 被重置为 {X:0 Y:0}
                        //       LoadingScreen 的职责是显示加载画面，不应该清空剧本数据
                        // Session.Current.Clear(); // ❌ 错误：会清空剧本数据

                        CacheManager.Clear(CacheType.Live);
                        
                        // 🔥 性能优化：清理文件存在性缓存
                        Platform.ClearFileExistsCache();

                        GC.Collect();

#if DEBUG
                        try
                        {
                            LoadScreenEvent.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            throw new Exception("加載出錯：" + e);
                            //Program.PrintError(e);
                            //Environment.Exit(1);
                        }
                        ClearEvent();
                        IsComplete = true;
                        if (Mode == "Start")
                        {

                        }
                        else
                        {
                            Session.MainGame.loadingScreen = null;
                        }
#else
                        new Platforms.PlatformTask(() =>
                        {
                            try
                            {
                                LoadScreenEvent.Invoke(null, null);
                            }
                            catch (Exception e)
                            {
                                // 🔥 根本修复：Release 版本后台线程异常必须写文件，否则完全静默消失
                                // PlatformTask 用 Task.Run，异常不会传播到主线程
                                string logPath = System.IO.Path.Combine(
                                    AppContext.BaseDirectory,
                                    "crash_log.txt");
                                string msg = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] 加載出錯:\n{e}\n\nInnerException:\n{e.InnerException}\n\n---\n";
                                try
                                {
                                    System.IO.File.AppendAllText(logPath, msg);
                                }
                                catch
                                {
                                    // 写文件失败也要退出
                                }
                                // 强制退出，确保用户看到崩溃而不是卡死
                                System.Environment.Exit(1);
                            }

                            ClearEvent();
                            IsComplete = true;
                            if (Mode == "Start")
                            {

                            }
                            else
                            {
                                Session.MainGame.loadingScreen = null;
                            }
                        }).Start();
#endif
                    }
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            if (Mode == "Start")
            {
                // Calculate virtual dimensions that cover the entire physical screen
                float logicalWidth = Session.MainGame.GraphicsDevice.Viewport.Width / global::GameManager.ScreenManager.ScaleX;
                float logicalHeight = Session.MainGame.GraphicsDevice.Viewport.Height / global::GameManager.ScreenManager.ScaleY;
                int bgX = (int)((1280 - logicalWidth) / 2);
                int bgY = (int)((720 - logicalHeight) / 2);

                CacheManager.Draw(@"Content/Textures/Resources/Start/LoadingBack.jpg", new Rectangle(bgX, bgY, (int)logicalWidth, (int)logicalHeight), Color.White * 1f);

                if (maps.Length > 0)
                {
                    var map = maps[page - 1];

                    if (String.IsNullOrEmpty(map))
                    {

                    }
                    else
                    {
                        //@"Content/Textures/Resources/ScenarioLoading/Maps/" + Scenario
                        CacheManager.DrawAvatar(map, new Vector2(23 + 66, 5 + 44), Color.White * 1f, new Vector2(1106f / 2821f, 565f / 1587f));
                    }
                }


                CacheManager.DrawAvatar(@"Content/Textures/Resources/Start/LoadingBorder.png", new Vector2(23, 5), Color.White * 1f, 1f);

                btPre.Draw();

                btPlay.Draw();

                btPause.Draw();

                btNext.Draw();

                btLoad.Draw();

                btStart.Draw();

                if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop || Platform.PlatFormType == PlatFormType.UWP && !Platform.IsMobile)
                {
                    CacheManager.DrawAvatar(@"Content\Textures\Resources\MouseArrow\Normal.png", InputManager.Position, Color.White, 1f);
                }
            }
            else
            {
                float logicalWidth = Session.MainGame.GraphicsDevice.Viewport.Width / global::GameManager.ScreenManager.ScaleX;
                float logicalHeight = Session.MainGame.GraphicsDevice.Viewport.Height / global::GameManager.ScreenManager.ScaleY;
                int bgX = (int)((1280 - logicalWidth) / 2);
                int bgY = (int)((720 - logicalHeight) / 2);

                CacheManager.Draw(background, new Rectangle(bgX, bgY, (int)logicalWidth, (int)logicalHeight), Color.White * 1f);

                CacheManager.DrawAvatar(@"Content/Textures/Resources/ScenarioLoading/jindulan.png", new Vector2(215, 650), Color.White * 1f, 1f);

                CacheManager.DrawString(Session.Current.Font, tishi.Trim(), new Vector2(215 + textPre, 650) + new Vector2(10, 10), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }

        }

        public void ExitScreen()
        {

        }

    }
}
