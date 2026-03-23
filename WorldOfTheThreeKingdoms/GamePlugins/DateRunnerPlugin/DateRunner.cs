using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using GamePanels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using WorldOfTheThreeKingdoms.GameScreens;

namespace DateRunnerPlugin
{

    public class DateRunner : Tool
    {
        internal PlatformTexture BackgroundTexture;
        internal GameDate Date;
        private int daysLeftBackUp;

        public Font DaysLeftTextBuilder = new Font();
        //internal FreeTextBuilder DaysLeftTextBuilder = new FreeTextBuilder();

        internal Color DaysLeftTextColor;
        internal Rectangle DaysLeftTextPosition;
        //private Texture2D DaysLeftTextTexture;
        internal int DaysToGo = 1;
        internal Rectangle DaysToGoFirstDigitTextPosition;
        //private Texture2D DaysToGoFirstDigitTextTexture;
        internal Rectangle DaysToGoSecondDigitTextPosition;
        //private Texture2D DaysToGoSecondDigitTextTexture;

        public Font DaysToGoTextBuilder = new Font();
        //internal FreeTextBuilder DaysToGoTextBuilder = new FreeTextBuilder();

        internal Color DaysToGoTextColor;
        private PlatformTexture FirstDigitLowerArrowDisplayTexture;
        internal Rectangle FirstDigitLowerArrowPosition;
        private PlatformTexture FirstDigitUpperArrowDisplayTexture;
        internal Rectangle FirstDigitUpperArrowPosition;
        internal PlatformTexture LowerArrowSelectedTexture;
        internal PlatformTexture LowerArrowTexture;
        private const int MaxDay = 0x63;
        internal PlatformTexture PauseSelectedTexture;
        internal PlatformTexture PauseTexture;
        private PlatformTexture PlayDisplayTexture;
        public bool playing = false;
        internal Rectangle PlayPosition;
        internal PlatformTexture PlaySelectedTexture;
        internal PlatformTexture PlayTexture;
#pragma warning disable CS0414 // The field 'DateRunner.runLastDay' is assigned but its value is never used
        private bool runLastDay = false;
#pragma warning restore CS0414 // The field 'DateRunner.runLastDay' is assigned but its value is never used
        
        private PlatformTexture SecondDigitLowerArrowDisplayTexture;
        internal Rectangle SecondDigitLowerArrowPosition;
        private PlatformTexture SecondDigitUpperArrowDisplayTexture;
        internal Rectangle SecondDigitUpperArrowPosition;
        private PlatformTexture StopDisplayTexture;
        internal Rectangle StopPosition;
        internal PlatformTexture StopSelectedTexture;
        internal PlatformTexture StopTexture;
        internal bool Updated = false;
        internal PlatformTexture UpperArrowSelectedTexture;
        internal PlatformTexture UpperArrowTexture;
        internal bool yizhiyunxing = false;

        private const int MAX_DAY = 99;

        ButtonTexture btChangeDays = null;

        public DateRunner()
        {
            btChangeDays = new ButtonTexture(@"Content\Textures\Resources\Start\Setting", "Setting", null);
            btChangeDays.Scale = 0.8f;
            btChangeDays.OnButtonPress += (sender, e) =>
            {
                GameDelegates.VoidFunction function = null;

                Session.MainGame.mainGameScreen.Plugins.NumberInputerPlugin.SetMax(MAX_DAY);
                Session.MainGame.mainGameScreen.Plugins.NumberInputerPlugin.SetMapPosition(ShowPosition.Center);
                Session.MainGame.mainGameScreen.Plugins.NumberInputerPlugin.SetDepthOffset(-0.01f);
                if (function == null)
                {
                    function = delegate
                    {
                        var number = Session.MainGame.mainGameScreen.Plugins.NumberInputerPlugin.Number;
                        if (number < 1)
                        {
                            number = 1;
                        }
                        DaysToGo = number;
                        this.Updated = false;
                    };
                }
                Session.MainGame.mainGameScreen.Plugins.NumberInputerPlugin.SetEnterFunction(function);
                Session.MainGame.mainGameScreen.Plugins.NumberInputerPlugin.IsShowing = true;
            };
        }

        private static DateTime _lastDateGoLogTime = DateTime.MinValue;
        
        internal void DateGo()
        {
            // 每5秒输出一次DateGo状态
            if ((DateTime.Now - _lastDateGoLogTime).TotalSeconds > 5)
            {
                _lastDateGoLogTime = DateTime.Now;
                _lastDateGoLogTime = DateTime.Now;
            }
            
            if (this.playing && (this.DaysLeft > 0))
            {
                // 🔥 Fix: Remove manual setting of IsRunning=true. It conflicts with Date.StartRunning() check.
                // if (!this.Date.IsRunning) ...

                // 🔥 Fix: If Player hasn't passed while DateRunner is active, we should AUTO-PASS them.
                // The user complained that the runner stops automatically. This means they want to "Skip" their turn during the run.
                var player = Session.Current.Scenario.CurrentPlayer;
                if (player != null && !player.Passed)
                {
                     player.Passed = true;
                     // 🔥 修复：不要在这里设置 Controlling = false
                     // 原因：Faction.Run() 需要看到 Controlling=true 才能走玩家路径
                     // 在 Passed=true 时，Faction.Run() 会自行将 Controlling 设为 false 并返回 true
                     // 如果提前设置 Controlling=false，Faction.Run() 会跌入 AI 路径并永远卡死
                     player.AIFinished = true; 
                }

                // 🔥 修复：只有当日期尚未开始运行时，才调用 DateStartRunning
                // 原因：Date.StartRunning() 内的死锁修复会在 Threading=false 时重置 IsRunning
                //       导致 OnDayStarting → DayStartingEvent → BuildQueue 每帧重复执行
                //       BuildQueue 会清空所有正在移动的部队队列，使部队永远无法移动
                if (!this.Date.IsRunning)
                {
                    this.DateStartRunning();
                }
            }
            else
            {

            }
        }

        private bool DateStartRunning()
        {
            // System.Diagnostics.Debug.WriteLine($"[DateStartRunning] 调用 Date.StartRunning(), Date.IsRunning={this.Date.IsRunning}");
            if (this.Date.StartRunning())
            {
                // System.Diagnostics.Debug.WriteLine("[DateStartRunning] Date.StartRunning() 返回 true");
                return true;
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine("[DateStartRunning] Date.StartRunning() 返回 false");
                return false;
            }
        }

        internal void DateStop()
        {
            // 渲染资源保护
            if (this.PlayDisplayTexture == null) return;

            // 状态检查：如果没在跑，直接退出
            if (!this.Date.IsRunning && !this.playing) return;

            // 尝试停止
            // 注意：不再做复杂的 while 循环
            bool ended = this.Date.EndRunning();

            if (ended)
            {
                if (!Session.GlobalVariables.EnableResposiveThreading)
                {
                    if (this.yizhiyunxing) this.DaysLeft = MAX_DAY;
                    this.Date.Go();
                    
                    if (this.DaysLeft <= 0)
                    {
                        this.DaysLeft = 0;
                        this.DaysLeft = 0;
                        this.playing = false;
                        this.ResetPlayDisplayTexture();
                    }
                }
                this.Updated = false;
            }
            else
            {
                // [死锁熔断]
                // 如果 EndRunning 失败，直接强制认为日期已停止运行。
                // 这会防止 GameGo 无限调用 DateStop，从而防止 GPU 崩溃。
                if (this.playing && this.DaysLeft == 0)
                {
                    // 只要卡住，就强行关闭 Threading，不讲道理
                    if (Session.Current.Scenario != null) 
                    {
                        Session.Current.Scenario.Threading = false; 
                    }
                    this.Date.IsRunning = false;
                }
            }
        }

        private string DaysLeftString()
        {
            int daysLeftBackUp = this.daysLeftBackUp;
            if (daysLeftBackUp == 0)
            {
                daysLeftBackUp = this.DaysLeft;
            }
            if (daysLeftBackUp > 9)
            {
                return daysLeftBackUp.ToString();
            }
            return ("0" + daysLeftBackUp.ToString());
        }

        public override void Draw()
        {
            // [核弹级修复] 暴力异常捕获
            // 当显卡重置时，任何绘图操作都会抛出 SharpDXException 或 NullReference。
            // 我们直接捕获所有异常并忽略，确保这一帧能混过去，不让程序崩溃。
            try
            {
                // 1. 基础判空（防止 ArgumentNullException）
                if (this.PlayDisplayTexture == null || this.StopDisplayTexture == null || 
                    Session.Current == null || Session.Current.Font == null)
                {
                    return;
                }

                // 2. 深度判空（防止资源已释放引发的 InvalidOperation）
                // 移除：MonoGame 的 Texture2D 在设备丢失时 IsDisposed 会变 true，但 PlatformTexture 没有此属性
                // if (this.PlayDisplayTexture.XNATexture != null && this.PlayDisplayTexture.XNATexture.IsDisposed) return;

                // --- 以下是你原有的绘制逻辑 ---
                
                Rectangle? sourceRectangle = null;
                // 绘制播放/暂停按钮
                CacheManager.Draw(this.PlayDisplayTexture, this.PlayDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.099f);
                CacheManager.Draw(this.StopDisplayTexture, this.StopDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.099f);

                // 绘制数字 (加上 try-catch 后这里安全了)
                var first = ((this.DaysToGo / 10) % 10).ToString();
                var left = this.DaysLeftString();
                var leftPos = new Vector2(this.DaysLeftTextDisplayPosition.X, this.DaysLeftTextDisplayPosition.Y);
                
                // 使用原有的 DrawString 逻辑
                CacheManager.DrawString(Session.Current.Font, left, leftPos, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.0999f);
                CacheManager.DrawString(Session.Current.Font, first + (this.DaysToGo % 10).ToString(), leftPos + new Vector2(34, 0), Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.08f);

                // 按钮保护
                if (btChangeDays != null)
                {
                     btChangeDays.Draw();
                }
            }
            catch (Exception)
            {
                // 吃掉所有异常。
                // 如果显卡炸了，这里会不断捕获异常，直到显卡恢复。
                // 绝对不要在这里 throw，否则程序直接退出。
            }
        }

        public override void DrawBackground(Rectangle Position)
        {
            CacheManager.Draw(this.BackgroundTexture, Position, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.09999f);
        }

        internal void Initialize(Screen screen)
        {            
            screen.OnMouseLeftDown += new Screen.MouseLeftDown(this.screen_OnMouseLeftDown);
            screen.OnMouseMove += new Screen.MouseMove(this.screen_OnMouseMove);
        }

        public void Pause()
        {
            if (((this.DaysLeft > 1) && this.playing) && (this.daysLeftBackUp == 0))
            {
                System.Diagnostics.Debug.WriteLine($"[DateRunner.Pause] 暂停执行: DaysLeft={this.DaysLeft} -> 1, daysLeftBackUp=0 -> {this.DaysLeft - 1}");
                this.daysLeftBackUp = this.DaysLeft - 1;
                this.DaysLeft = 1;
                this.playing = false; // 🔥 Fix: Set playing to false when paused
                this.ResetPlayDisplayTexture(); // 🔥 Fix: Use proper texture reset method
                this.Updated = false;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DateRunner.Pause] 暂停条件不满足: DaysLeft={this.DaysLeft}, playing={this.playing}, daysLeftBackUp={this.daysLeftBackUp}");
            }
        }

        public void Reset()
        {
            this.playing = false;
            this.DaysLeft = 0;
            this.daysLeftBackUp = 0;
            this.PlayDisplayTexture = this.PlayTexture;
            this.Updated = false;
        }

        internal void ResetDisplayTextures()
        {
            this.FirstDigitUpperArrowDisplayTexture = this.UpperArrowTexture;
            this.FirstDigitLowerArrowDisplayTexture = this.LowerArrowTexture;
            this.SecondDigitUpperArrowDisplayTexture = this.UpperArrowTexture;
            this.SecondDigitLowerArrowDisplayTexture = this.LowerArrowTexture;
            this.ResetPlayDisplayTexture();
            this.StopDisplayTexture = this.StopTexture;
        }

        private void ResetPlayDisplayTexture()
        {
            if (this.playing)
            {
                this.PlayDisplayTexture = this.PauseTexture;
            }
            else
            {
                this.PlayDisplayTexture = this.PlayTexture;
            }
        }

        public void Run()
        {
            var scenario = Session.Current.Scenario;
            var player = scenario.CurrentPlayer;

            if ((((this.DaysLeft == 0) && (this.daysLeftBackUp == 0)) && (this.DaysToGo > 0)) && !this.playing)
            {
                System.Diagnostics.Debug.WriteLine($"[DateRunner] 检查自动结束条件: DaysLeft={this.DaysLeft}, daysLeftBackUp={this.daysLeftBackUp}, DaysToGo={this.DaysToGo}, playing={this.playing}");
                if (scenario.CurrentFaction == player && player.Controlling)
                {
                    System.Diagnostics.Debug.WriteLine($"[DateRunner] 自动结束玩家回合 (条件1): 玩家={player.Name}，开始执行{this.DaysToGo}回合");
                    if (player != null)
                    {
                        // 🔥 修复：只设置 Passed 和 AIFinished，不设置 Controlling=false
                        // 原因：Faction.Run() 需要 Controlling=true 才能走玩家路径
                        // 如果提前 Controlling=false，Faction.Run() 跌入 AI 路径永远返回 false
                        // RunningFaction 卡死 → AfterDayPassed 永远返回 true → DateGo/MoveTheTroops 永远不执行
                        player.Passed = true;
                        player.AIFinished = true; 
                    }
                    if (scenario.IsLastPlayer(player))
                    {
                        System.Diagnostics.Debug.WriteLine($"[DateRunner] 开始执行RunDays({this.DaysToGo})");
                        this.RunDays(this.DaysToGo);
                    }
                }
            }
            else if (((this.DaysLeft > 1) && this.playing) && (this.daysLeftBackUp == 0))
            {
                // 暂停逻辑保持不变，但增加 Updated 标志
                this.yizhiyunxing = false;
                this.daysLeftBackUp = this.DaysLeft - 1;
                this.DaysLeft = 1;
                this.PlayDisplayTexture = this.PlaySelectedTexture;
                this.Updated = false;
            }
            else if ((((this.daysLeftBackUp > 0) && !this.playing) && (this.DaysLeft == 0)) && ((Session.Current.Scenario.CurrentFaction == Session.Current.Scenario.CurrentPlayer) && Session.Current.Scenario.CurrentPlayer.Controlling))
            {
                System.Diagnostics.Debug.WriteLine($"[DateRunner] 自动结束玩家回合: DaysLeft={this.DaysLeft}, daysLeftBackUp={this.daysLeftBackUp}, playing={this.playing}");
                if (Session.Current.Scenario.CurrentPlayer != null)
                {
                    Session.Current.Scenario.CurrentPlayer.Passed = true;
                    Session.Current.Scenario.CurrentPlayer.Controlling = false;
                    Session.Current.Scenario.CurrentPlayer.AIFinished = true; // Apply optimization here too
                }
                
                // if (Session.Current.Scenario.CurrentPlayer != null)
                // {
                //     Session.Current.Scenario.CurrentPlayer.Passed = true;
                //     Session.Current.Scenario.CurrentPlayer.Controlling = false;
                // }
                if (Session.Current.Scenario.IsLastPlayer(Session.Current.Scenario.CurrentPlayer))
                {
                    this.RunDays(0);
                }
            }
        }

        // 抽取出来的强制结束方法，确保状态清理干净
        private void ForcePlayerEndTurn()
        {
            var p = Session.Current.Scenario.CurrentPlayer;
            if (p != null)
            {
                p.Passed = true;
                p.Controlling = false;
                p.AIFinished = true; // 关键：确保 AI 状态也被标记完成
                // System.Diagnostics.Debug.WriteLine($"[DateRunner] 强制清除玩家 {p.Name} 的控制状态");
            }
        }

        internal void RunDays(int Days)
        {
            if (Days == -999)
            {
                this.yizhiyunxing = true;
            }
            this.playing = true;
            this.DaysLeft += Days + this.daysLeftBackUp;
            this.daysLeftBackUp = 0;
            if (this.DaysLeft > MAX_DAY || this.yizhiyunxing)
            {
                this.DaysLeft = MAX_DAY;
            }
            this.Updated = false;
            if (this.PlayDisplayTexture == this.PlayTexture)
            {
                this.PlayDisplayTexture = this.PauseTexture;
            }
            else if (this.PlayDisplayTexture == this.PlaySelectedTexture)
            {
                this.PlayDisplayTexture = this.PauseSelectedTexture;
            }
        }

        private void screen_OnMouseLeftDown(Point position)
        {
            if (base.Enabled)
            {
                if (StaticMethods.PointInRectangle(position, this.PlayDisplayPosition))
                {
                    Platforms.Platform.Sleep(500);
                    
                    // 🔥 Fix: Check if currently running and should pause instead of always calling Run()
                    if (this.playing && this.DaysLeft > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DateRunner] Play按钮点击 - 当前正在运行，执行暂停: playing={this.playing}, DaysLeft={this.DaysLeft}");
                        this.Pause();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DateRunner] Play按钮点击 - 当前未运行，执行开始: playing={this.playing}, DaysLeft={this.DaysLeft}");
                        this.Run();
                    }
                }
                else if (StaticMethods.PointInRectangle(position, this.StopDisplayPosition))
                {
                    this.yizhiyunxing = false;

                    this.Stop();
                }
                //else if (StaticMethods.PointInRectangle(position, this.FirstDigitUpperArrowDisplayPosition))
                //{
                //    if (this.DaysToGo <= 0x63)
                //    {
                //        this.DaysToGo += 10;
                //        if (this.DaysToGo > 0x63)
                //        {
                //            this.DaysToGo = 0x63;
                //        }
                //        this.Updated = false;
                //    }
                //}
                //else if (StaticMethods.PointInRectangle(position, this.FirstDigitLowerArrowDisplayPosition))
                //{
                //    if (this.DaysToGo > 1)
                //    {
                //        this.DaysToGo -= 10;
                //        if (this.DaysToGo < 1)
                //        {
                //            this.DaysToGo = 1;
                //        }
                //        this.Updated = false;
                //    }
                //}
                //else if (StaticMethods.PointInRectangle(position, this.SecondDigitUpperArrowDisplayPosition))
                //{
                //    if (this.DaysToGo < 0x63)
                //    {
                //        this.DaysToGo++;
                //        this.Updated = false;
                //    }
                //}
                //else if (StaticMethods.PointInRectangle(position, this.SecondDigitLowerArrowDisplayPosition) && (this.DaysToGo > 1))
                //{
                //    this.DaysToGo--;
                //    this.Updated = false;
                //}
            }
        }

        private void screen_OnMouseMove(Point position, bool leftDown)
        {
            if (base.Enabled)
            {
                if (StaticMethods.PointInRectangle(position, this.PlayDisplayPosition))
                {
                    if (this.PlayDisplayTexture == this.PlayTexture)
                    {
                        this.PlayDisplayTexture = this.PlaySelectedTexture;
                    }
                    else if (this.PlayDisplayTexture == this.PauseTexture)
                    {
                        this.PlayDisplayTexture = this.PauseSelectedTexture;
                    }
                    this.StopDisplayTexture = this.StopTexture;
                }
                else if (StaticMethods.PointInRectangle(position, this.StopDisplayPosition))
                {
                    this.StopDisplayTexture = this.StopSelectedTexture;
                    this.ResetPlayDisplayTexture();
                }
                //else if (StaticMethods.PointInRectangle(position, this.FirstDigitUpperArrowDisplayPosition))
                //{
                //    this.FirstDigitUpperArrowDisplayTexture = this.UpperArrowSelectedTexture;
                //}
                //else if (StaticMethods.PointInRectangle(position, this.FirstDigitLowerArrowDisplayPosition))
                //{
                //    this.FirstDigitLowerArrowDisplayTexture = this.LowerArrowSelectedTexture;
                //}
                //else if (StaticMethods.PointInRectangle(position, this.SecondDigitUpperArrowDisplayPosition))
                //{
                //    this.SecondDigitUpperArrowDisplayTexture = this.UpperArrowSelectedTexture;
                //}
                //else if (StaticMethods.PointInRectangle(position, this.SecondDigitLowerArrowDisplayPosition))
                //{
                //    this.SecondDigitLowerArrowDisplayTexture = this.LowerArrowSelectedTexture;
                //}
                else
                {
                    this.ResetDisplayTextures();
                }
            }
        }

        public void Stop()
        {
            if ((this.playing && (this.DaysLeft > 1)) && (this.daysLeftBackUp == 0))
            {
                this.DaysLeft = 1;
                this.PlayDisplayTexture = this.PlayTexture;
                this.Updated = false;
            }
            else if (((!this.playing && (this.daysLeftBackUp > 0)) && (this.DaysLeft == 0)) && ((Session.Current.Scenario.CurrentFaction == Session.Current.Scenario.CurrentPlayer) && Session.Current.Scenario.CurrentPlayer.Controlling))
            {
                if (Session.Current.Scenario.CurrentPlayer.Passed)
                {
                    Session.Current.Scenario.CurrentPlayer.Passed = false;
                }
                this.daysLeftBackUp = 0;

                this.PlayDisplayTexture = this.PlayTexture;
                this.Updated = false;
            }
        }

        public override void Update()
        {
            if (!this.Updated)
            {
                //int num = this.DaysToGo % 10;
                //this.DaysToGoSecondDigitTextTexture = this.DaysToGoTextBuilder.CreateTextTexture(num.ToString());
                //this.DaysToGoFirstDigitTextTexture = this.DaysToGoTextBuilder.CreateTextTexture(((this.DaysToGo / 10) % 10).ToString());
                //this.DaysLeftTextTexture = this.DaysLeftTextBuilder.CreateTextTexture(this.DaysLeftString());
                this.Updated = true;
            }

            var pos = new Vector2(this.DaysToGoFirstDigitTextDisplayPosition.X - 5, this.DaysToGoFirstDigitTextDisplayPosition.Y - 15);

            btChangeDays.Position = pos;

            btChangeDays.Update();
        }

        internal int DaysLeft
        {
            get
            {
                return this.Date.DaysLeft;
            }
            set
            {
                this.Date.DaysLeft = value;
            }
        }

        private Rectangle DaysLeftTextDisplayPosition
        {
            get
            {
                return new Rectangle(this.DaysLeftTextPosition.X + this.DisplayOffset.X, this.DaysLeftTextPosition.Y + this.DisplayOffset.Y, this.DaysLeftTextPosition.Width, this.DaysLeftTextPosition.Height);
            }
        }

        private Rectangle DaysToGoFirstDigitTextDisplayPosition
        {
            get
            {
                return new Rectangle(this.DaysToGoFirstDigitTextPosition.X + this.DisplayOffset.X, this.DaysToGoFirstDigitTextPosition.Y + this.DisplayOffset.Y, this.DaysToGoFirstDigitTextPosition.Width, this.DaysToGoFirstDigitTextPosition.Height);
            }
        }

        private Rectangle DaysToGoSecondDigitTextDisplayPosition
        {
            get
            {
                return new Rectangle(this.DaysToGoSecondDigitTextPosition.X + this.DisplayOffset.X, this.DaysToGoSecondDigitTextPosition.Y + this.DisplayOffset.Y, this.DaysToGoSecondDigitTextPosition.Width, this.DaysToGoSecondDigitTextPosition.Height);
            }
        }

        private Rectangle FirstDigitLowerArrowDisplayPosition
        {
            get
            {
                return new Rectangle(this.FirstDigitLowerArrowPosition.X + this.DisplayOffset.X, this.FirstDigitLowerArrowPosition.Y + this.DisplayOffset.Y, this.FirstDigitLowerArrowPosition.Width, this.FirstDigitLowerArrowPosition.Height);
            }
        }

        private Rectangle FirstDigitUpperArrowDisplayPosition
        {
            get
            {
                return new Rectangle(this.FirstDigitUpperArrowPosition.X + this.DisplayOffset.X, this.FirstDigitUpperArrowPosition.Y + this.DisplayOffset.Y, this.FirstDigitUpperArrowPosition.Width, this.FirstDigitUpperArrowPosition.Height);
            }
        }

        private Rectangle PlayDisplayPosition
        {
            get
            {
                return new Rectangle(this.PlayPosition.X + this.DisplayOffset.X, this.PlayPosition.Y + this.DisplayOffset.Y, this.PlayPosition.Width, this.PlayPosition.Height);
            }
        }

        private Rectangle SecondDigitLowerArrowDisplayPosition
        {
            get
            {
                return new Rectangle(this.SecondDigitLowerArrowPosition.X + this.DisplayOffset.X, this.SecondDigitLowerArrowPosition.Y + this.DisplayOffset.Y, this.SecondDigitLowerArrowPosition.Width, this.SecondDigitLowerArrowPosition.Height);
            }
        }

        private Rectangle SecondDigitUpperArrowDisplayPosition
        {
            get
            {
                return new Rectangle(this.SecondDigitUpperArrowPosition.X + this.DisplayOffset.X, this.SecondDigitUpperArrowPosition.Y + this.DisplayOffset.Y, this.SecondDigitUpperArrowPosition.Width, this.SecondDigitUpperArrowPosition.Height);
            }
        }

        private Rectangle StopDisplayPosition
        {
            get
            {
                return new Rectangle(this.StopPosition.X + this.DisplayOffset.X, this.StopPosition.Y + this.DisplayOffset.Y, this.StopPosition.Width, this.StopPosition.Height);
            }
        }
    }
}

