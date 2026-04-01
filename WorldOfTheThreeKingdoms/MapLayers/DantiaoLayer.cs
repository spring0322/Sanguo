using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.Json; // 需要 .NET 8 环境
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms;
using Microsoft.Xna.Framework.Graphics;
using GameManager;
using GamePanels;
using Tools;
using Platforms;
using WorldOfTheThreeKingdoms.Serialization.SystemTextJson;
using WorldOfTheThreeKingdoms.GameObjects.Duel;

namespace WorldOfTheThreeKingdoms.GameScreens.ScreenLayers
{
    // --- 新增：战术枚举 ---
    public enum DuelTactic
    {
        Normal,     // 平衡
        Aggressive, // 重视进攻 (攻+50%, 防-30%)
        Defensive   // 重视防御 (攻-40%, 防+50%)
    }

    // --- 新增：配置数据结构 ---
    public class DantiaoConfigData
    {
        public string DefaultStyleLeft { get; set; } = "General01A";
        public string DefaultStyleRight { get; set; } = "General01B";
        
        // 🔥 C# 12: 使用集合表达式
        public Dictionary<string, string> SpecialGenerals { get; set; } = [];
    }

    // --- 新增：配置管理器 ---
    public static class DantiaoConfigManager
    {
        public static DantiaoConfigData Config;

        public static void LoadConfig()
        {
            string path = ResolveConfigPath();
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    string jsonString = File.ReadAllText(path);
                    
                    // 🔥 2026-03-09 AOT 序列化修复：使用 GameJsonContext
                    // Cold Path - 初始化阶段，可读性优先
                    var options = WorldOfTheThreeKingdoms.Serialization.GameJsonContext.GetDefaultOptions();
                    Config = JsonSerializer.Deserialize(
                        jsonString,
                        JsonTypeInfoHelper.Resolve<DantiaoConfigData>(options));
                    if (Config == null)
                    {
                        throw new InvalidDataException("[LoadConfig] DantiaoConfigData 反序列化返回 null");
                    }
                    
                    // ⚠️ 数据完整性断言：配置文件必须有效
                    // 如果反序列化失败（返回 null），说明 JSON 格式错误或类型未注册
                    // 应该在开发期通过断言发现问题，而不是静默回退
                    System.Diagnostics.Debug.Assert(Config != null, 
                        "[LoadConfig] DantiaoConfigData 反序列化失败，检查 JSON 格式和类型注册");
                    
                    System.Diagnostics.Debug.WriteLine($"[单挑] 成功加载配置文件: {path}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[单挑] 找不到配置文件，使用默认值: {path}");
                    Config = new DantiaoConfigData(); // 文件不存在则使用默认值
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[单挑] 加载配置文件失败: {ex.Message}");
                Config = new DantiaoConfigData(); // 读取出错兜底
            }
        }

        public static string GetStyle(int personId, bool isLeft)
        {
            if (Config == null) LoadConfig();

            // 1. 优先查找专属皮肤 (ID转String)
            if (Config.SpecialGenerals != null && Config.SpecialGenerals.ContainsKey(personId.ToString()))
            {
                string style = Config.SpecialGenerals[personId.ToString()];
                if (!string.IsNullOrEmpty(style) && style.StartsWith("General_", StringComparison.Ordinal))
                {
                    style = style["General_".Length..];
                }
                return style;
            }

            // 2. 返回默认皮肤
            return isLeft ? Config.DefaultStyleLeft : Config.DefaultStyleRight;
        }
        public static void EnsureStyleTextureRecs(string style, string fallbackStyle)
        {
            if (string.IsNullOrEmpty(style) || Session.TextureRecs == null)
            {
                return;
            }

            string probeKey = @"Content\Textures\Resources\Dantiao\" + style + "#WalkLeft";
            if (Session.TextureRecs.ContainsKey(probeKey))
            {
                return;
            }

            string fallbackKey = @"Content\Textures\Resources\Dantiao\" + fallbackStyle + "#WalkLeft";
            if (!Session.TextureRecs.ContainsKey(fallbackKey))
            {
                return;
            }

            string assetPath = @"Content\Textures\Resources\Dantiao\" + style;
            Texture2D texture = Platform.Current.LoadTexture(assetPath + ".png", false);
            if (texture == null)
            {
                return;
            }

            string[] names = ["WalkLeft", "WalkRight", "AttackLeft", "AttackRight", "Failure"];
            TextureRecs fallbackRec = Session.TextureRecs[fallbackKey];
            int fallbackFrameWidth = fallbackRec.Recs[0].Width;
            int fallbackFrameHeight = fallbackRec.Recs[0].Height;
            int columns = Math.Max(1, fallbackRec.Width / fallbackFrameWidth);
            int rows = Math.Max(1, fallbackRec.Height / fallbackFrameHeight);
            int frameWidth = Math.Max(1, texture.Width / columns);
            int frameHeight = Math.Max(1, texture.Height / rows);
            int repeat = fallbackRec.Recs.Length;
            Rectangle[][] generatedRecs = new Rectangle[names.Length][];

            for (int index = 0; index < names.Length; index++)
            {
                generatedRecs[index] = TextureRecsManager.FindOneTexRectangles(index, repeat, frameWidth, texture.Width, frameHeight);
            }

            Rectangle trimRect = FindSharedOpaqueTrim(texture, generatedRecs, 8);
            if (trimRect.Width > 0 && trimRect.Height > 0 && (trimRect.Width < frameWidth || trimRect.Height < frameHeight))
            {
                for (int index = 0; index < generatedRecs.Length; index++)
                {
                    generatedRecs[index] = ApplyTrim(generatedRecs[index], trimRect);
                }
            }

            for (int index = 0; index < names.Length; index++)
            {
                string key = assetPath + "#" + names[index];
                if (Session.TextureRecs.ContainsKey(key))
                {
                    continue;
                }

                Session.TextureRecs.Add(key, new TextureRecs()
                {
                    Width = texture.Width,
                    Height = texture.Height,
                    CacheType = fallbackRec.CacheType,
                    Ext = "png",
                    Recs = generatedRecs[index]
                });
            }
        }

        public static float GetStyleScale(string style, string fallbackStyle)
        {
            if (string.IsNullOrEmpty(style) || Session.TextureRecs == null)
            {
                return 1f;
            }

            string styleKey = @"Content\Textures\Resources\Dantiao\" + style + "#WalkLeft";
            string fallbackKey = @"Content\Textures\Resources\Dantiao\" + fallbackStyle + "#WalkLeft";
            if (!Session.TextureRecs.ContainsKey(styleKey) || !Session.TextureRecs.ContainsKey(fallbackKey))
            {
                return 1f;
            }

            TextureRecs styleRec = Session.TextureRecs[styleKey];
            TextureRecs fallbackRec = Session.TextureRecs[fallbackKey];
            if (styleRec.Recs == null || styleRec.Recs.Length == 0 || fallbackRec.Recs == null || fallbackRec.Recs.Length == 0)
            {
                return 1f;
            }

            int fallbackFrameWidth = fallbackRec.Recs[0].Width;
            int fallbackFrameHeight = fallbackRec.Recs[0].Height;
            int columns = Math.Max(1, fallbackRec.Width / fallbackFrameWidth);
            int rows = Math.Max(1, fallbackRec.Height / fallbackFrameHeight);
            int styleFrameWidth = Math.Max(1, styleRec.Width / columns);
            int styleFrameHeight = Math.Max(1, styleRec.Height / rows);
            if (styleFrameWidth <= 0 || styleFrameHeight <= 0 || fallbackFrameWidth <= 0 || fallbackFrameHeight <= 0)
            {
                return 1f;
            }

            float scaleX = (float)fallbackFrameWidth / styleFrameWidth;
            float scaleY = (float)fallbackFrameHeight / styleFrameHeight;
            return Math.Min(scaleX, scaleY);
        }

        static Rectangle FindSharedOpaqueTrim(Texture2D texture, Rectangle[][] rectangleGroups, byte minAlpha)
        {
            if (texture == null || rectangleGroups == null || rectangleGroups.Length == 0)
            {
                return Rectangle.Empty;
            }

            Color[] pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);

            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;
            bool found = false;

            for (int groupIndex = 0; groupIndex < rectangleGroups.Length; groupIndex++)
            {
                Rectangle[] recs = rectangleGroups[groupIndex];
                if (recs == null)
                {
                    continue;
                }

                for (int recIndex = 0; recIndex < recs.Length; recIndex++)
                {
                    Rectangle rec = recs[recIndex];
                    if (TryFindOpaqueBounds(pixels, texture.Width, rec, minAlpha, out Rectangle bounds))
                    {
                        left = Math.Min(left, bounds.X);
                        top = Math.Min(top, bounds.Y);
                        right = Math.Max(right, bounds.Right);
                        bottom = Math.Max(bottom, bounds.Bottom);
                        found = true;
                    }
                }
            }

            if (!found)
            {
                return Rectangle.Empty;
            }

            return new Rectangle(left, top, right - left, bottom - top);
        }

        static bool TryFindOpaqueBounds(Color[] pixels, int textureWidth, Rectangle rec, byte minAlpha, out Rectangle bounds)
        {
            int left = rec.Width;
            int top = rec.Height;
            int right = -1;
            int bottom = -1;

            for (int y = 0; y < rec.Height; y++)
            {
                int rowStart = (rec.Y + y) * textureWidth + rec.X;
                for (int x = 0; x < rec.Width; x++)
                {
                    if (pixels[rowStart + x].A > minAlpha)
                    {
                        if (x < left) left = x;
                        if (y < top) top = y;
                        if (x > right) right = x;
                        if (y > bottom) bottom = y;
                    }
                }
            }

            if (right < left || bottom < top)
            {
                bounds = Rectangle.Empty;
                return false;
            }

            bounds = new Rectangle(left, top, right - left + 1, bottom - top + 1);
            return true;
        }

        static Rectangle[] ApplyTrim(Rectangle[] recs, Rectangle trimRect)
        {
            Rectangle[] trimmed = new Rectangle[recs.Length];
            for (int i = 0; i < recs.Length; i++)
            {
                Rectangle rec = recs[i];
                trimmed[i] = new Rectangle(rec.X + trimRect.X, rec.Y + trimRect.Y, trimRect.Width, trimRect.Height);
            }
            return trimmed;
        }

        static string ResolveConfigPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            [
                Path.Combine("Content", "Textures", "Resources", "Dantiao", "DantiaoConfig.json"),
                Path.Combine("Content", "DantiaoConfig.json"),
                Path.Combine(baseDirectory, "Content", "Textures", "Resources", "Dantiao", "DantiaoConfig.json"),
                Path.Combine(baseDirectory, "Content", "DantiaoConfig.json")
            ];

            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }
    }

    public class General
    {
        public Person Person { get; set; }

        public int Force { get; set; }

        public float Life { get; set; }

        public float Skill { get; set; }

        public Vector2 Position { get; set; }

        public Vector2 LandPosition { get; set; }

        public int LimiteWidth = 0;

        public bool LimiteOrder = true;

        AnimatedTexture atCurrent = null;

        public string Style { get; set; }

        public string Status { get; set; }

        Dictionary<string, AnimatedTexture> atGeneralStatus = new Dictionary<string, AnimatedTexture>();
        private readonly DuelRandom duelRandom;

        // 修改：不再需要 hardcode 的 styles 数组，改为动态加载
        string[] statusNames = new string[] { "WalkLeft", "WalkRight", "AttackLeft", "AttackRight", "Failure" };

        public string Direction = "";  //Up Down Left Right

        public float Duration = 0f;

        public float Delay = 0f;

        public Vector2 StartPos;

        public float ActionTime = 0f;

        public float Speed = 20f;

        public float SpeedExt = 0f;

        public float SpeedPlus = 0f;

        public float SpeedNow = 0f;

        public float SayTimeTotal = 0f;

        public float SayTime = 0f;

        public bool SayDisappear = true;

        public string SayWords = "";

        // --- 新增逻辑属性 ---
        public DuelTactic CurrentTactic { get; set; } = DuelTactic.Normal;
        public bool IsPlayerControlled { get; set; } = false; // 是否由玩家控制
        private float aiThinkTimer = 0f; // AI思考计时器

        public bool IsPaused
        {
            get
            {
                return atCurrent == null ? true : atCurrent.Paused;
            }
        }
        
        // 修改构造函数：增加 isLeftSide 和 isPlayer 参数
        public General(Person p, bool isLeftSide, bool isPlayer, DuelRandom duelRandom)
        {
            Person = p;
            IsPlayerControlled = isPlayer;
            if (duelRandom == null)
            {
                throw new ArgumentNullException(nameof(duelRandom));
            }
            this.duelRandom = duelRandom;

            // 1. 动态获取兵模名称
            Style = DantiaoConfigManager.GetStyle(((GameObject)p).ID, isLeftSide);
            string defaultStyle = isLeftSide ? DantiaoConfigManager.Config.DefaultStyleLeft : DantiaoConfigManager.Config.DefaultStyleRight;
            DantiaoConfigManager.EnsureStyleTextureRecs(Style, defaultStyle);
            float styleScale = DantiaoConfigManager.GetStyleScale(Style, defaultStyle);

            foreach (var gen in statusNames)
            {
                try 
                {
                    // 验证TextureRecs中是否存在该配置
                    string textureKey = @"Content\Textures\Resources\Dantiao\" + Style + "#" + gen;
                    if (Session.TextureRecs != null && Session.TextureRecs.ContainsKey(textureKey))
                    {
                        // 动态加载选定的 Style
                        var genStatus = new AnimatedTexture(@"Content\Textures\Resources\Dantiao\" + Style, gen, "", true, 5)
                        {
                            Depth = DantiaoLayer.depth - 0.035f,
                            Scale = styleScale
                        };

                        atGeneralStatus.Add(Style + "-" + gen, genStatus);
                        System.Diagnostics.Debug.WriteLine($"[单挑] 成功加载纹理: {textureKey}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[单挑警告] TextureRecs中缺失配置: {textureKey}");
                        
                        // 尝试使用默认配置
                        if (Style != defaultStyle)
                        {
                            string fallbackKey = @"Content\Textures\Resources\Dantiao\" + defaultStyle + "#" + gen;
                            if (Session.TextureRecs != null && Session.TextureRecs.ContainsKey(fallbackKey))
                            {
                                var genStatus = new AnimatedTexture(@"Content\Textures\Resources\Dantiao\" + defaultStyle, gen, "", true, 5)
                                {
                                    Depth = DantiaoLayer.depth - 0.035f
                                };
                                atGeneralStatus.Add(Style + "-" + gen, genStatus);
                                System.Diagnostics.Debug.WriteLine($"[单挑] 使用默认纹理: {fallbackKey}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 容错处理：如果文件缺失，记录错误但不中断
                    System.Diagnostics.Debug.WriteLine($"[单挑错误] 加载纹理失败 {Style}-{gen}: {ex.Message}");
                }
            }
            
            // ⚠️ 数据完整性断言：必须至少加载一些纹理
            // 如果纹理全部加载失败，说明资源文件缺失或配置错误
            System.Diagnostics.Debug.Assert(atGeneralStatus.Count > 0, 
                $"[单挑] 武将 {Person.Name} 没有加载任何纹理，检查 TextureRecs 配置和资源文件");
            
            if (atGeneralStatus.Count == 0)
            {
                // 🔥 根本性修复：如果纹理加载失败，抛出异常阻止单挑启动
                // 不应该让程序在没有纹理的情况下继续运行
                throw new InvalidOperationException(
                    $"[单挑] 武将 {Person.Name} (Style: {Style}) 纹理加载失败，无法启动单挑。" +
                    $"请检查：1) TextureRecs 是否包含单挑纹理配置 2) 资源文件是否存在");
            }
        }

        // --- 新增：获取攻击修正 ---
        public float GetAttackModifier()
        {
            switch (CurrentTactic)
            {
                case DuelTactic.Aggressive: return 1.5f;
                case DuelTactic.Defensive: return 0.6f;
                default: return 1.0f;
            }
        }

        // --- 新增：获取防御修正 ---
        public float GetDefenseModifier()
        {
            switch (CurrentTactic)
            {
                case DuelTactic.Aggressive: return 1.3f; // 破绽大，受伤多
                case DuelTactic.Defensive: return 0.5f;  // 受伤减半
                default: return 1.0f;
            }
        }

        // --- 新增：简单的 AI 思考逻辑 ---
        public void UpdateAI(float gameTime)
        {
            if (IsPlayerControlled) return;

            aiThinkTimer += gameTime;
            // 每 2.5 秒思考一次
            if (aiThinkTimer > 2.5f)
            {
                aiThinkTimer = 0f;
                
                // 简单的状态机
                if (Life < 30)
                {
                    // 血少时 70% 概率龟缩防御
                    CurrentTactic = duelRandom.Next(0, 10) < 7 ? DuelTactic.Defensive : DuelTactic.Normal;
                }
                else
                {
                    // 随机切换
                    int rand = duelRandom.Next(0, 10);
                    if (rand < 3) CurrentTactic = DuelTactic.Defensive;
                    else if (rand < 6) CurrentTactic = DuelTactic.Aggressive;
                    else CurrentTactic = DuelTactic.Normal;
                }
            }
        }

        public void ChangeStatus(string style, string status)
        {
            // 注意：虽然传入了 style 参数，但在本修改版中，我们主要依赖内部的 Style 属性
            // 但为了兼容旧调用，我们更新 Status
            Status = status;

            if (atGeneralStatus.ContainsKey(Style + "-" + Status))
            {
                atCurrent = atGeneralStatus[Style + "-" + Status];
                atCurrent.Reset();
            }
        }

        public void ChangeWalkToAttack()
        {
            Status = Status.Replace("Walk", "Attack");

            ChangeStatus(Style, Status);
        }

        public void ChangeAttackToWalk()
        {
            Status = Status.Replace("Attack", "Walk");

            ChangeStatus(Style, Status);
        }

        public void ChangeStatusDirection()
        {
            if (Status.Contains("Left"))
            {
                Status = Status.Replace("Left", "Right");
                Direction = "Right";
            }
            else if (Status.Contains("Right"))
            {
                Status = Status.Replace("Right", "Left");
                Direction = "Left";
            }

            ChangeStatus(Style, Status);
        }

        public void Pause()
        {
            Direction = "";
            if (atCurrent != null) atCurrent.Paused = true;
        }

        public void Start()
        {
            StartPos = LandPosition;
            ActionTime = 0f;
            if (atCurrent != null)
            {
                atCurrent.Paused = false;
                atCurrent.ChangeFrame(Convert.ToInt32(5 * (Speed + SpeedExt + SpeedPlus) / Speed));
            }
        }

        public void ChangePosition(Vector2 landPos)
        {
            LandPosition = landPos;
        }

        public void Update(float gameTime, Vector2 screenPos, bool updateAi = true)
        {
            // 每一帧更新 AI
            if (updateAi)
            {
                UpdateAI(gameTime);
            }

            if (atCurrent == null)
            {

            }
            else
            {
                if (SayTime > 0f)
                {
                    SayTime -= gameTime;

                    if (SayTime < 0f)
                    {
                        SayTime = 0f;
                        //SayWords = "";
                    }
                }

                if (Delay > 0)
                {
                    Delay -= gameTime;

                    if (Delay < 0)
                    {
                        Delay = 0;
                    }

                }

                if (Duration > 0)
                {
                    Duration -= gameTime;

                    if (Duration <= 0)
                    {
                        Duration = 0;

                        Direction = "";

                        if (Status.Contains("Walk"))
                        {
                            Pause();
                        }
                    }
                }

                if (Delay == 0)
                {

                    ActionTime += gameTime;

                    if (String.IsNullOrEmpty(Direction))
                    {

                    }
                    else
                    {
                        SpeedNow = Speed + SpeedExt + ActionTime * SpeedPlus;

                        if (SpeedNow <= 0)
                        {
                            SpeedNow = 0;
                        }

                        //位移s＝Vot + at²/ 2

                        var moveDis = (Speed + SpeedExt) * ActionTime + SpeedPlus * ActionTime * ActionTime / 2;

                        //ActionTime * realSpeed;

                        Vector2 movePos = Vector2.Zero;

                        if (Direction == "Left")
                        {
                            movePos = new Vector2(-moveDis, 0);

                            if (StartPos.X + movePos.X - DantiaoLayer.basePos.X > 0)
                            {
                                LandPosition = StartPos + movePos;
                            }
                        }
                        else if (Direction == "Right")
                        {
                            movePos = new Vector2(moveDis, 0);

                            if (StartPos.X + movePos.X - DantiaoLayer.basePos.X < 2200 - 120)
                            {
                                LandPosition = StartPos + movePos;
                            }
                        }
                        else if (Direction == "Up")
                        {
                            movePos = new Vector2(0, -moveDis);

                            if (StartPos.Y + movePos.Y - DantiaoLayer.basePos.Y > 150)
                            {
                                LandPosition = StartPos + movePos;
                            }
                        }
                        else if (Direction == "Down")
                        {
                            movePos = new Vector2(0, moveDis);

                            if (StartPos.Y + movePos.Y - DantiaoLayer.basePos.Y < 450)
                            {
                                LandPosition = StartPos + movePos;
                            }
                        }


                    }
                }

                Position = LandPosition - screenPos;

                atCurrent.Position = Position;

                atCurrent.LimiteWidth = LimiteWidth;

                atCurrent.LimiteOrder = LimiteOrder;

                atCurrent.UpdateFrame(gameTime);
            }
        }

        public void Draw()
        {
            if (atCurrent == null)
            {

            }
            else
            {
                atCurrent.DrawFrame(null);
            }
            
            // --- 新增：绘制头顶战术状态 ---
            string statusText = "";
            Color statusColor = Color.White;
            
            switch (CurrentTactic)
            {
                case DuelTactic.Aggressive: statusText = "攻"; statusColor = Color.Red; break;
                case DuelTactic.Defensive: statusText = "守"; statusColor = Color.Blue; break;
                // Normal 不显示
            }

            if (!string.IsNullOrEmpty(statusText))
            {
                CacheManager.DrawString(null, statusText, Position + new Vector2(50, -30), statusColor, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, DantiaoLayer.depth - 0.05f);
            }
        }

    }

    public class DantiaoLayer : IDuelView
    {
        float totalTime = 0f;

        float elapsedTime = 0f;

        float fightTime = 0f;
        private readonly DuelRandom duelRandom;
        // Visual effects must not consume duelRandom sequence.
        private readonly Random visualRandom = new Random();

        Vector2 scale = Vector2.One;

        public float Alpha = 0f;

        public static float depth = 0.1f;

        public bool IsVisible = true;

        public bool IsStart = true;

        public static Vector2 basePos = Vector2.Zero;

        Vector2 cloudPos = new Vector2(15, 10);

        Rectangle cloudRec = new Rectangle(0, 0, 1000 - 22, 222);

        Vector2 treePos = new Vector2(15, 150);

        Rectangle treeRec = new Rectangle(0, 0, 1000 - 22, 80);

        Vector2 landPos = new Vector2(15, 225);

        Rectangle landRec = new Rectangle(0, 0, 1000 - 22, 530);

        Vector2 general1Pos = Vector2.Zero;

        Vector2 general2Pos = Vector2.Zero;

        Vector2 screenPosPre = Vector2.Zero;

        Vector2 screenPos = Vector2.Zero;

        public int round = 0;

        int Speed = 1;

        ButtonTexture btnStory, btnPagePre, btnPageNext, btnSpeed, btnSpeedUp, btnSpeedDown;

        // --- 新增：指令按钮 ---
        ButtonTexture btnAtk, btnDef, btnNrm;

        General genLeft, genRight, playerGeneral;
        Vector2 leftPlaybackHomePosition = Vector2.Zero;
        Vector2 rightPlaybackHomePosition = Vector2.Zero;

        int moveDistance = 0;

        float moveTime = 0f;

        public string Stage = "Cloud";

        public int Result = 0;

        string Title = "";

        bool ViewExit = false;

        public TroopDamage damage = null;
        
        public Action OnDemoExit { get; set; }
        public bool UseExternalLifecycle { get; set; }
        public event Action<int, int, Person, Person> Completed;
        public event Action PresentationCompleted;
        public event Action<DuelCommand> CommandInput;
        bool controllerDrivenMode = false;
        bool commandWindowVisible = false;
        int commandTicksRemaining = 0;
        DuelStage controllerStage = DuelStage.Intro;
        DuelOutcome controllerOutcome = DuelOutcome.None;
        DuelPlaybackScript activePlaybackScript = null;
        int activeBeatIndex = -1;
        float activeBeatRemaining = 0f;
        bool presentationCompletionRaised = false;
        float resultElapsedTime = 0f;
        
        // --- 新增：演示模式开关 ---
        public bool IsDemoMode = false;
        public int Round => round;
        public DuelStage CurrentStage => controllerDrivenMode ? controllerStage : DuelResolver.ResolveStageFromLegacy(Stage);
        public DuelOutcome CurrentOutcome => controllerDrivenMode ? controllerOutcome : DuelResolver.ResolveOutcomeFromLegacyResult(Result);

        static bool IsPlayerControlledSide(Person person)
        {
            if (person == null || person.BelongedFaction == null || Session.Current?.Scenario == null)
            {
                return false;
            }

            if (Session.Current.Scenario.CurrentPlayer != null)
            {
                return Session.Current.Scenario.IsCurrentPlayer(person.BelongedFaction);
            }

            return Session.Current.Scenario.IsPlayer(person.BelongedFaction);
        }

        void ApplyPlayerTactic(DuelTactic tactic)
        {
            if (!commandWindowVisible)
            {
                return;
            }

            DuelCommandType commandType = tactic switch
            {
                DuelTactic.Aggressive => DuelCommandType.Aggressive,
                DuelTactic.Defensive => DuelCommandType.Defensive,
                _ => DuelCommandType.Normal
            };

            CommandInput?.Invoke(new DuelCommand(commandType));
        }

        static Vector2 GetCenteredBasePosition()
        {
            float logicalWidth = global::GameManager.ScreenManager.VirtualWidth;
            float logicalHeight = global::GameManager.ScreenManager.VirtualHeight;

            if (Platform.GraphicsDevice != null)
            {
                float scaleX = global::GameManager.ScreenManager.ScaleX > 0f ? global::GameManager.ScreenManager.ScaleX : 1f;
                float scaleY = global::GameManager.ScreenManager.ScaleY > 0f ? global::GameManager.ScreenManager.ScaleY : 1f;
                logicalWidth = Platform.GraphicsDevice.Viewport.Width / scaleX;
                logicalHeight = Platform.GraphicsDevice.Viewport.Height / scaleY;
            }
            else if (Session.ResolutionX > 0 && Session.ResolutionY > 0)
            {
                logicalWidth = Session.ResolutionX;
                logicalHeight = Session.ResolutionY;
            }

            return new Vector2((logicalWidth - 1000f) / 2f, (logicalHeight - 620f) / 2f);
        }

        // 修改构造函数，增加 demoMode 默认参数
        public DantiaoLayer(Person left, Person right, bool demoMode = false, int? seed = null)
        {
            duelRandom = new DuelRandom(seed ?? Environment.TickCount);
            IsDemoMode = demoMode;
            bool leftPlayerControlled = !IsDemoMode && IsPlayerControlledSide(left);
            bool rightPlayerControlled = IsDemoMode || (!leftPlayerControlled && IsPlayerControlledSide(right));

            //scale = new Vector2(Convert.ToSingle(Session.ResolutionX) / 800f, Convert.ToSingle(Session.ResolutionY) / 480f);

            basePos = GetCenteredBasePosition();

            btnStory = new ButtonTexture(@"Content\Textures\Resources\Dantiao\Story", "Story", basePos + new Vector2(25, 545))
            {
                Visible = false
            };

            btnStory.OnButtonPress += (sender, e) =>
            {
                btnStory.Selected = !btnStory.Selected;
            };

            btnPagePre = new ButtonTexture(@"Content\Textures\Resources\Dantiao\Page", "Left", basePos + new Vector2(30 + 110, 555));

            btnPagePre.OnButtonPress += (sender, e) =>
            {

            };

            btnPageNext = new ButtonTexture(@"Content\Textures\Resources\Dantiao\Page", "Right", basePos + new Vector2(38 + 228, 555));

            btnPageNext.OnButtonPress += (sender, e) =>
            {

            };

            btnSpeed = new ButtonTexture(@"Content\Textures\Resources\Start\Setting", "Setting", basePos + new Vector2(750, 30))
            {
                Scale = 0.5f,
                Visible = false
            };

            btnSpeed.OnButtonPress += (sender, e) =>
            {

            };

            btnSpeedDown = new ButtonTexture(@"Content\Textures\Resources\Dantiao\Page", "Left", basePos + new Vector2(750 + 110, 35))
            {
                Enable = false,
                Visible = false
            };

            btnSpeedDown.OnButtonPress += (sender, e) =>
            {
                Speed--;
                btnSpeedUp.Enable = true;
                if (Speed == 1)
                {
                    btnSpeedDown.Enable = false;
                }
            };

            btnSpeedUp = new ButtonTexture(@"Content\Textures\Resources\Dantiao\Page", "Right", basePos + new Vector2(750 + 170, 35))
            {
                Visible = false
            };

            btnSpeedUp.OnButtonPress += (sender, e) =>
            {
                Speed++;
                btnSpeedDown.Enable = true;
                if (Speed == 5)
                {
                    btnSpeedUp.Enable = false;
                }
            };
            
            // --- 初始化指令按钮 (仅在非演示模式下显示) ---
            Vector2 cmdPos = basePos + new Vector2(350, 550);
            
            // 🔥 2026-03-09 修复：添加异常处理，避免按钮纹理缺失导致崩溃
            try
            {
                // 临时使用 "Page" 按钮资源代替，你可以替换为 Content\Textures\Resources\Dantiao\Attack 等
                // 修复：使用通用的 Button 纹理，避免 KeyNotFound 或 IndexOutOfRange 导致的崩溃
                // Button 纹理大小为 100x42 (每帧)，Scale 0.5f 后为 50x21，间隔 60px 刚好合适
                bool enablePlayerCommand = leftPlayerControlled || rightPlayerControlled;
                btnAtk = new ButtonTexture(@"Content\Textures\Resources\Dantiao\Button", "Button", cmdPos) { Visible = enablePlayerCommand, Scale = 0.5f }; 
                btnAtk.OnButtonPress += (s, e) => ApplyPlayerTactic(DuelTactic.Aggressive);

                btnNrm = new ButtonTexture(@"Content\Textures\Resources\Dantiao\Button", "Button", cmdPos + new Vector2(60, 0)) { Visible = enablePlayerCommand, Scale = 0.5f };
                btnNrm.OnButtonPress += (s, e) => ApplyPlayerTactic(DuelTactic.Normal);

                btnDef = new ButtonTexture(@"Content\Textures\Resources\Dantiao\Button", "Button", cmdPos + new Vector2(120, 0)) { Visible = enablePlayerCommand, Scale = 0.5f };
                btnDef.OnButtonPress += (s, e) => ApplyPlayerTactic(DuelTactic.Defensive);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[单挑警告] 指令按钮纹理加载失败: {ex.Message}，将禁用玩家控制");
                // 如果按钮纹理加载失败，强制进入演示模式
                IsDemoMode = true;
                leftPlayerControlled = false;
                rightPlayerControlled = false;
                btnAtk = null;
                btnNrm = null;
                btnDef = null;
            }

            // --- 加载武将 ---
            DantiaoConfigManager.LoadConfig();

            // genLeft: 左侧位置 (true), 如果是 DemoMode 则为 AI (isPlayer=false)，否则为玩家 (isPlayer=true)
            genLeft = new General(left, true, leftPlayerControlled, duelRandom)
            {
                Force = left.ChallengeStrength,
                Life = 100,
                Skill = 100
            };

            genLeft.ChangeStatus(genLeft.Style, "WalkRight");

            genLeft.ChangePosition(basePos + new Vector2(500-100, 250));
            leftPlaybackHomePosition = genLeft.LandPosition;

            genLeft.Pause();

            // genRight: 右侧位置 (false), 永远是 AI (isPlayer=false)
            genRight = new General(right, false, rightPlayerControlled, duelRandom)
            {
                Force = right.ChallengeStrength,
                Life = 100,
                Skill = 100
            };

            genRight.ChangeStatus(genRight.Style, "WalkLeft");

            genRight.ChangePosition(basePos + new Vector2(2200-1000+500-80, 250));
            rightPlaybackHomePosition = genRight.LandPosition;

            genRight.Pause();
            playerGeneral = leftPlayerControlled ? genLeft : (rightPlayerControlled ? genRight : null);

            AudioManager.Instance?.PlayCombatMusic();
            System.Diagnostics.Debug.WriteLine($"[单挑] DantiaoLayer 初始化完成: {left.Name} vs {right.Name}");
        }

        public void Start()
        {
            elapsedTime = 0f;
            totalTime = 0f;
            IsVisible = true;
            IsStart = true;
            controllerDrivenMode = UseExternalLifecycle;
            commandWindowVisible = false;
            commandTicksRemaining = 0;
            controllerStage = DuelStage.Intro;
            controllerOutcome = DuelOutcome.None;
            activePlaybackScript = null;
            activeBeatIndex = -1;
            activeBeatRemaining = 0f;
            presentationCompletionRaised = false;
            resultElapsedTime = 0f;
            if (controllerDrivenMode)
            {
                Stage = "Start";
                Result = 0;
                Title = string.Empty;
                Alpha = 0f;
                SetCommandButtonsVisible(false);
                genLeft.ChangeStatus(genLeft.Style, "WalkRight");
                genRight.ChangeStatus(genRight.Style, "WalkLeft");
                genLeft.Pause();
                genRight.Pause();
                SetScreenPos();
            }
        }

        public void SyncState(DuelSessionState sessionState)
        {
            if (sessionState == null)
            {
                throw new ArgumentNullException(nameof(sessionState));
            }

            round = sessionState.Round;
            controllerStage = sessionState.Stage;
            controllerOutcome = sessionState.Outcome;
            genLeft.Life = sessionState.LeftSide.CurrentLife;
            genRight.Life = sessionState.RightSide.CurrentLife;
            genLeft.CurrentTactic = ResolveTactic(sessionState.LeftSide.CommandLocked ? sessionState.LeftSide.LockedCommand : sessionState.LeftSide.LastCommand);
            genRight.CurrentTactic = ResolveTactic(sessionState.RightSide.CommandLocked ? sessionState.RightSide.LockedCommand : sessionState.RightSide.LastCommand);
        }

        public void ShowCommandWindow(int remainingTicks)
        {
            controllerStage = DuelStage.Command;
            commandWindowVisible = true;
            commandTicksRemaining = remainingTicks;
            presentationCompletionRaised = false;
            SetCommandButtonsVisible(playerGeneral != null);
        }

        public void UpdateCommandWindow(int remainingTicks)
        {
            commandTicksRemaining = remainingTicks;
        }

        public void HideCommandWindow()
        {
            commandWindowVisible = false;
            SetCommandButtonsVisible(false);
        }

        public void PlayScript(DuelPlaybackScript script)
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            controllerStage = DuelStage.Playback;
            controllerOutcome = script.Outcome;
            activePlaybackScript = script;
            activeBeatIndex = -1;
            activeBeatRemaining = 0f;
            presentationCompletionRaised = false;
            commandWindowVisible = false;
            SetCommandButtonsVisible(false);
            PreparePlaybackPose();
        }

        public void ShowResult(DuelOutcome outcome, string title)
        {
            controllerStage = DuelStage.Result;
            controllerOutcome = outcome;
            Result = DuelResolver.ResolveLegacyResult(outcome);
            Title = title ?? string.Empty;
            presentationCompletionRaised = false;
            resultElapsedTime = 0f;
            commandWindowVisible = false;
            SetCommandButtonsVisible(false);
            ApplyResultPose(outcome);
        }

        public void UpdateView(float gameTime)
        {
            Update(gameTime);
        }

        static DuelTactic ResolveTactic(in DuelCommand command)
        {
            return command.CommandType switch
            {
                DuelCommandType.Aggressive => DuelTactic.Aggressive,
                DuelCommandType.Defensive => DuelTactic.Defensive,
                _ => DuelTactic.Normal
            };
        }

        void SetCommandButtonsVisible(bool visible)
        {
            if (btnAtk != null)
            {
                btnAtk.Visible = visible;
            }

            if (btnNrm != null)
            {
                btnNrm.Visible = visible;
            }

            if (btnDef != null)
            {
                btnDef.Visible = visible;
            }
        }

        void RaisePresentationCompleted()
        {
            if (presentationCompletionRaised)
            {
                return;
            }

            presentationCompletionRaised = true;
            PresentationCompleted?.Invoke();
        }

        void PreparePlaybackPose()
        {
            ResetPlaybackGeneral(genLeft, leftPlaybackHomePosition, "WalkRight");
            ResetPlaybackGeneral(genRight, rightPlaybackHomePosition, "WalkLeft");
            SetScreenPos();
        }

        void ApplyResultPose(DuelOutcome outcome)
        {
            switch (outcome)
            {
                case DuelOutcome.LeftWin:
                case DuelOutcome.RightDead:
                    genRight.ChangeStatus(genRight.Style, "Failure");
                    genRight.Pause();
                    break;
                case DuelOutcome.RightWin:
                case DuelOutcome.LeftDead:
                    genLeft.ChangeStatus(genLeft.Style, "Failure");
                    genLeft.Pause();
                    break;
            }
        }

        void ResetPlaybackGeneral(General general, Vector2 homePosition, string status)
        {
            general.SpeedExt = 0f;
            general.SpeedPlus = 0f;
            general.SpeedNow = 0f;
            general.Duration = 0f;
            general.Delay = 0f;
            general.ActionTime = 0f;
            general.ChangePosition(homePosition);
            general.StartPos = homePosition;
            general.ChangeStatus(general.Style, status);
            general.Pause();
        }

        void PlayPlaybackAttack(General general, Vector2 homePosition, string attackStatus, string direction, float beatDuration)
        {
            general.SpeedExt = 42f;
            general.SpeedPlus = -36f;
            general.SpeedNow = 0f;
            general.Delay = 0f;
            general.ActionTime = 0f;
            general.Duration = Math.Max(0.12f, beatDuration);
            general.ChangePosition(homePosition);
            general.StartPos = homePosition;
            general.ChangeStatus(general.Style, attackStatus);
            general.Direction = direction;
            general.Start();
        }

        void AdvancePlaybackBeat(float gameTime)
        {
            if (activePlaybackScript == null)
            {
                RaisePresentationCompleted();
                return;
            }

            if (activeBeatRemaining > 0f)
            {
                activeBeatRemaining -= gameTime;
                if (activeBeatRemaining > 0f)
                {
                    return;
                }

                PreparePlaybackPose();
            }

            activeBeatIndex++;
            if (activeBeatIndex >= activePlaybackScript.Beats.Count)
            {
                activePlaybackScript = null;
                RaisePresentationCompleted();
                return;
            }

            DuelPlaybackBeat beat = activePlaybackScript.Beats[activeBeatIndex];
            activeBeatRemaining = beat.Duration;
            ApplyPlaybackBeat(beat);
        }

        void ApplyPlaybackBeat(in DuelPlaybackBeat beat)
        {
            switch (beat.Clip)
            {
                case "left_attack":
                    PlayPlaybackAttack(genLeft, leftPlaybackHomePosition, "AttackRight", "Right", beat.Duration);
                    Platform.Current.PlayEffect(@"Content\Sound\Dantiao\NormalAttack");
                    break;
                case "right_attack":
                    PlayPlaybackAttack(genRight, rightPlaybackHomePosition, "AttackLeft", "Left", beat.Duration);
                    Platform.Current.PlayEffect(@"Content\Sound\Dantiao\NormalAttack");
                    break;
                case "finish":
                    ApplyResultPose(controllerOutcome);
                    break;
                default:
                    PreparePlaybackPose();
                    break;
            }

            SetScreenPos();
        }

        void UpdateSharedPresentation(float gameTime, bool updateAi)
        {
            btnStory.Update();

            if (btnStory.Selected)
            {
                btnPagePre.Update();
                btnPageNext.Update();
            }

            btnSpeed.Update();
            btnSpeedUp.Update();
            btnSpeedDown.Update();

            if (commandWindowVisible && playerGeneral != null && btnAtk != null && btnDef != null && btnNrm != null)
            {
                btnAtk.Update();
                btnDef.Update();
                btnNrm.Update();
            }

            landRec.Height = 620 - 10 - Convert.ToInt32(landPos.Y);
            cloudRec.X = Convert.ToInt32(screenPos.X * 0.2f);
            treeRec.X = Convert.ToInt32(screenPos.X * 0.5f);
            landRec.X = Convert.ToInt32(screenPos.X);
            genLeft.Update(gameTime, screenPos, updateAi);
            genRight.Update(gameTime, screenPos, updateAi);
        }

        void UpdateControllerDriven(float gameTime)
        {
            if (!(IsVisible && IsStart))
            {
                return;
            }

            totalTime += gameTime;
            elapsedTime += gameTime;
            SetScreenPos();

            switch (controllerStage)
            {
                case DuelStage.Intro:
                    Alpha = Math.Min(1f, elapsedTime / 0.45f);
                    if (elapsedTime >= 0.45f)
                    {
                        RaisePresentationCompleted();
                    }
                    break;
                case DuelStage.Command:
                    Alpha = 1f;
                    break;
                case DuelStage.Playback:
                    Alpha = 1f;
                    AdvancePlaybackBeat(gameTime);
                    break;
                case DuelStage.Result:
                    Alpha = 1f;
                    resultElapsedTime += gameTime;
                    if (resultElapsedTime >= 0.25f && InputManager.IsDown)
                    {
                        RaisePresentationCompleted();
                    }
                    break;
                case DuelStage.Exit:
                    Alpha = Math.Max(0f, Alpha - gameTime);
                    break;
            }

            UpdateSharedPresentation(gameTime, false);
        }

        public void SetScreenPos()
        {
            screenPos.X = (genLeft.LandPosition.X + genRight.LandPosition.X) / 2 + 64 - 500 - basePos.X;
            if (screenPos.X < 0)
            {
                screenPos.X = 0;
            }
            else if (screenPos.X > 2200 - 1000)
            {
                screenPos.X = 2200 - 1000;
            }
            screenPos.Y = (genLeft.LandPosition.Y + genRight.LandPosition.Y) / 2 + 64 - 350 - basePos.Y;
            if (screenPos.Y < 0)
            {
                screenPos.Y = 0;
            }
            //else if (screenPos.Y > 300)
            //{
            //    screenPos.Y = 300;
            //}
        }

        public void Update(float gameTime)
        {
            if (controllerDrivenMode)
            {
                UpdateControllerDriven(gameTime);
                return;
            }

            if (IsVisible && IsStart)
            {
                totalTime += gameTime;

                elapsedTime += gameTime;

                if (Stage == "Cloud")
                {
                    if (IsDemoMode)
                    {
                        Alpha = 1f;
                        Stage = "Start";
                        elapsedTime = 2f;
                    }
                    else if (elapsedTime <= 1f)
                    {
                        Alpha = 0f;
                    }
                    else
                    {
                        Stage = "Start";
                        elapsedTime = 0f;
                    }
                }
                else
                {
                    if (totalTime >= 120f && Stage != "Over" && Stage != "OverOut")
                    {
                        Stage = "Over";
                        genLeft.SayDisappear = false;
                        genRight.SayDisappear = false;

                        Result = -1;
                        genLeft.SayWords = "棋逢对手！";
                        genLeft.SayTime = 2f;
                        genRight.SayWords = "痛快痛快！";
                        genRight.SayTime = 2f;

                        Title = "双方平局";
                    }

                    if (Stage == "Start")
                    {
                        if (elapsedTime <= 2f)
                        {
                            Alpha = elapsedTime / 2f;
                        }
                        else
                        {
                            Alpha = 1f;
                            Stage = "Gen1Move";
                            elapsedTime = 0f;
                            Platform.Current.PlayEffect(@"Content\Sound\Dantiao\Moving");
                        }
                    }
                    else if (Stage == "Gen1Move")
                    {
                        genLeft.SpeedExt = 30;
                        genLeft.Direction = "Right";
                        genLeft.Start();
                        Stage = "Gen1Moving";
                        elapsedTime = 0f;
                        screenPosPre = screenPos;
                    }
                    else if (Stage == "Gen1Moving")
                    {
                        screenPos = screenPosPre + (genLeft.LandPosition - genLeft.StartPos);

                        if (elapsedTime >= 2f)
                        {
                            Stage = "Gen1Speak";
                            elapsedTime = 0f;
                        }
                    }
                    else if (Stage == "Gen1Speak")
                    {
                        genLeft.Pause();
                        genLeft.SayTimeTotal = 2f;
                        genLeft.SayTime = 2f;
                        genLeft.SayWords = $"吾乃{genLeft.Person.Name}，哪个敢来应战？";

                        if (genLeft.Person.Name == "曹操")
                        {
                            genLeft.SayWords = $"吾乃{genLeft.Person.Name}，将军别来无恙？";
                        }

                        Stage = "Gen1Speaking";
                        elapsedTime = 0f;
                    }
                    else if (Stage == "Gen1Speaking")
                    {
                        if (elapsedTime >= 2f)
                        {
                            Stage = "Gen1Run";
                            elapsedTime = 0f;
                        }
                    }
                    else if (Stage == "Gen1Run")
                    {
                        genLeft.SpeedExt = 15;
                        genLeft.SpeedPlus = 10;
                        genLeft.Direction = "Right";
                        genLeft.Start();
                        Stage = "Gen1Running";
                        elapsedTime = 0f;
                        screenPosPre = screenPos;
                        Platform.Current.PlayEffect(@"Content\Sound\Dantiao\Moving");
                    }
                    else if (Stage == "Gen1Running")
                    {
                        if (elapsedTime <= 2f)
                        {
                            screenPos = screenPosPre + new Vector2(genRight.LandPosition.X + 64 - 530 - basePos.X - screenPosPre.X, 0) * elapsedTime / 2f;
                        }
                        else if (elapsedTime >= 3f)
                        {
                            Stage = "Gen2Speak";
                        }
                    }
                    else if (Stage == "Gen2Speak")
                    {
                        genRight.SayTimeTotal = 2f;
                        genRight.SayTime = 2f;
                        genRight.SayWords = $"上将{genRight.Person.Name}在此，贼将休得猖狂！";
                        Stage = "Gen2Speaking";
                        elapsedTime = 0f;
                    }
                    else if (Stage == "Gen2Speaking")
                    {
                        if (elapsedTime >= 2f)
                        {
                            Stage = "Gen2Run";
                            elapsedTime = 0f;
                        }
                    }
                    else if (Stage == "Gen2Run")
                    {
                        genRight.SpeedExt = 45;
                        genRight.SpeedPlus = 25;
                        genRight.Direction = "Left";
                        genRight.Start();
                        Stage = "Gen2Running";
                        elapsedTime = 0f;
                        screenPosPre = screenPos;
                        Platform.Current.PlayEffect(@"Content\Sound\Dantiao\Moving");
                    }
                    else if (Stage == "Gen2Running")
                    {
                        screenPos = new Vector2(genRight.LandPosition.X + 64 - 530 - basePos.X, screenPos.Y);

                        var centerLeft = genLeft.LandPosition + new Vector2(64, 64);

                        var centerRight = genRight.LandPosition + new Vector2(64, 64);

                        var distance = Math.Sqrt((centerRight.X - centerLeft.X) * (centerRight.X - centerLeft.X) + (centerRight.Y - centerLeft.Y));

                        if (distance <= 100)
                        {
                            Stage = "FightRush";
                        }
                    }
                    else if (Stage == "WaitRush")
                    {
                        SetScreenPos();

                        var centerLeft = genLeft.LandPosition + new Vector2(64, 64);

                        var centerRight = genRight.LandPosition + new Vector2(64, 64);

                        if (Math.Abs(genLeft.LandPosition.Y - genRight.LandPosition.Y) <= 10f)
                        {
                            var distance = Math.Sqrt((centerRight.X - centerLeft.X) * (centerRight.X - centerLeft.X) + (centerRight.Y - centerLeft.Y));

                            if (distance <= 100)
                            {
                                if (genLeft.Duration == 0 && genRight.Duration == 0)
                                {
                                    Stage = "FightRush";
                                }
                            }
                        }
                    }
                    else if (Stage == "FightRush")
                    {
                        SetScreenPos();

                        round++;

                        genLeft.ChangeWalkToAttack();

                        genLeft.Start();

                        genRight.ChangeWalkToAttack();

                        genRight.Start();

                        elapsedTime = 0f;

                        if (!genLeft.IsPaused && !genRight.IsPaused && (genLeft.SpeedNow > 68 || genRight.SpeedNow > 68))
                        {
                            //速度超過一定值，則對沖過去，開始減速
                            Stage = "FightRun";

                            Platform.Current.PlayEffect(@"Content\Sound\Dantiao\Moving");
                        }
                        else
                        {
                            //速度不到一定值，則開始對打
                            genLeft.Direction = "";
                            genRight.Direction = "";

                            Stage = "Fighting";
                        }
                    }
                    else if (Stage == "FightRun")
                    {
                        SetScreenPos();

                        //交匯情況下的武力傷害
                        Fight(gameTime, true);

                        if (elapsedTime > 0.4f)
                        {
                            fightTime = 0f;

                            genLeft.ChangeAttackToWalk();

                            genLeft.SpeedPlus = -duelRandom.Next(3, 7);

                            genLeft.Start();

                            genRight.ChangeAttackToWalk();

                            genRight.SpeedPlus = -duelRandom.Next(3, 7);

                            genRight.Start();

                            Stage = "FightRunStop";
                        }
                    }
                    else if (Stage == "FightRunStop")
                    {
                        SetScreenPos();

                        if (genLeft.SpeedNow <= 20 || genRight.SpeedNow <= 20)
                        {
                            genLeft.Pause();
                            genRight.Pause();

                            elapsedTime = 0f;

                            Stage = "FightRunBack";
                        }
                    }
                    else if (Stage == "FightRunBack")
                    {
                        SetScreenPos();

                        if (elapsedTime > 0.3f)
                        {
                            genLeft.ChangeStatusDirection();

                            genLeft.SpeedPlus = duelRandom.Next(1, 8);

                            genRight.ChangeStatusDirection();

                            genRight.SpeedPlus = duelRandom.Next(1, 8);

                            genLeft.Start();

                            genRight.Start();

                            Stage = "WaitRush";

                            Platform.Current.PlayEffect(@"Content\Sound\Dantiao\Moving");
                        }
                    }
                    else if (Stage == "Fighting")
                    {
                        //對打情況下的傷害
                        Fight(gameTime, false);

                        if (elapsedTime >= 5f)
                        {
                            General gen1, gen2;

                            var ran = duelRandom.Next(0, 10);

                            if (ran == 0 || ran == 2 || ran == 4 || ran == 6 || ran == 8)
                            {
                                gen1 = genLeft;
                                gen2 = genRight;
                            }
                            else
                            {
                                gen1 = genRight;
                                gen2 = genLeft;
                            }

                            if (0 <= ran && ran <= 2)
                            {
                                //保持不動

                            }
                            else if (3 <= ran && ran <= 5)
                            {
                                //後退、跟進
                                if (gen1.LandPosition.X <= gen2.LandPosition.X)
                                {
                                    if (gen1.LandPosition.X - basePos.X > 60)
                                    {
                                        gen1.Direction = "Left";
                                        gen1.ChangeStatus(gen1.Style, "WalkRight");
                                        gen1.Duration = 2f;

                                        gen2.Direction = "Left";
                                        gen2.ChangeStatus(gen2.Style, "WalkLeft");
                                        gen2.Delay = 1f;

                                        gen1.Start();

                                        gen2.Start();

                                        Stage = "WaitRush";
                                    }
                                }
                                else
                                {
                                    if (gen1.LandPosition.X - basePos.X < 2200 - 200)
                                    {
                                        gen1.Direction = "Right";
                                        gen1.ChangeStatus(gen1.Style, "WalkLeft");
                                        gen1.Duration = 2f;

                                        gen2.Direction = "Right";
                                        gen2.ChangeStatus(gen2.Style, "WalkRight");
                                        gen2.Delay = 1f;

                                        gen1.Start();

                                        gen2.Start();

                                        Stage = "WaitRush";
                                    }
                                }

                            }
                            else if (6 <= ran && ran <= 7)
                            {
                                if (gen1.LandPosition.Y - basePos.Y > 200 && gen2.LandPosition.Y - basePos.Y > 150)
                                {
                                    //向上
                                    gen1.Direction = "Up";
                                    gen1.ChangeAttackToWalk();
                                    gen1.Duration = 2f;

                                    gen2.Direction = "Up";
                                    gen2.ChangeAttackToWalk();
                                    gen2.Delay = 1f;

                                    gen1.Start();

                                    gen2.Start();

                                    Stage = "WaitRush";

                                    Platform.Current.PlayEffect(@"Content\Sound\Dantiao\Moving");
                                }
                            }
                            else if (8 <= ran && ran <= 10)
                            {
                                if (gen1.LandPosition.Y - basePos.Y < 460 && gen2.LandPosition.Y - basePos.Y < 460)
                                {
                                    //向下
                                    gen1.Direction = "Down";
                                    gen1.ChangeAttackToWalk();
                                    gen1.Duration = 2f;

                                    gen2.Direction = "Down";
                                    gen2.ChangeAttackToWalk();
                                    gen2.Delay = 1f;

                                    gen1.Start();

                                    gen2.Start();

                                    Stage = "WaitRush";


                                    Platform.Current.PlayEffect(@"Content\Sound\Dantiao\Moving");
                                }
                            }

                            elapsedTime = 0f;
                        }
                    }
                    else if (Stage == "Over")
                    {
                        if (InputManager.IsDown)
                        {
                            Stage = "OverOut";
                            elapsedTime = 0f;
                        }
                    }
                    else if (Stage == "OverOut")
                    {
                        if (elapsedTime >= 1f)
                        {
                            if (UseExternalLifecycle)
                            {
                                IsStart = false;
                                IsVisible = false;
                                Completed?.Invoke(Result, round, genLeft?.Person, genRight?.Person);
                                return;
                            }

                            if (damage == null)
                            {
                                if (OnDemoExit != null)
                                {
                                    OnDemoExit();
                                }
                                else
                                {
                                    Session.MainGame.mainGameScreen.dantiaoLayer = null;
                                    Session.MainGame.mainGameScreen.ReturnToMainMenu();
                                }
                            }
                            else
                            {
                                Session.MainGame.mainGameScreen.dantiaoLayer = null;
                                Session.MainGame.mainGameScreen.cloudLayer.IsStart = false;
                                Session.MainGame.mainGameScreen.cloudLayer.IsVisible = false;
                                Session.MainGame.mainGameScreen.cloudLayer.Reverse = false;

                                damage.ChallengeHappened = true;

                                damage.ChallengeStarted = false;

                                damage.ChallengeResult = Result;
                                damage.ChallengeSourcePerson = genLeft?.Person;
                                damage.ChallengeDestinationPerson = genRight?.Person;

                                Session.MainGame.mainGameScreen.EnableUpdate = true;
                            }

                        }
                        Alpha = 1 - elapsedTime;
                    }

                    UpdateSharedPresentation(gameTime, true);

                    // --- 更新按钮 ---
                    
                    // 更新指令按钮（非演示模式）


                    landPos = new Vector2(15, 225 - screenPos.Y);

                    if (String.IsNullOrEmpty(Title))
                    {

                    }
                    else
                    {
                        // 🔥 技术性修复：避免IndexOutOfRangeException
                        var timeParts = totalTime.ToString().Split(new string[] { "." }, StringSplitOptions.None);
                        var timeString = timeParts.Length > 0 ? timeParts[0] : "0";
                        var time = float.Parse("0." + timeString);

                        if (time <= 0.5f)
                        {
                            ViewExit = true;
                        }
                        else
                        {
                            ViewExit = false;
                        }

                    }

                }

            }
        }

        public void Fight(float gameTime, bool rush)
        {
            fightTime += gameTime;

            if (fightTime >= 0.8f || rush && fightTime >= 0.3f)
            {
                fightTime -= (rush ? 0.3f : 0.8f);

                Platform.Current.PlayEffect(@"Content\Sound\Dantiao\NormalAttack");
                
                // --- 修改后的伤害计算 (带战术修正) ---
                
                // 1. 基础伤害 (随机部分)
                float rawDmgLeft = Convert.ToSingle(duelRandom.Next(0, genLeft.Force / 5) * 8) / 10f;
                float rawDmgRight = Convert.ToSingle(duelRandom.Next(0, genRight.Force / 5) * 8) / 10f;
                
                // 2. 最终伤害 = 基础 * 攻击者战术修正 * 防御者战术修正
                float finalDmgLeft = rawDmgLeft * genLeft.GetAttackModifier() * genRight.GetDefenseModifier();
                float finalDmgRight = rawDmgRight * genRight.GetAttackModifier() * genLeft.GetDefenseModifier();

                genRight.Life -= finalDmgLeft;
                genLeft.Life -= finalDmgRight;

                if (genLeft.Life <= 0 || genRight.Life <= 0)
                {
                    Stage = "Over";
                    if (genLeft.Life <= 0 && genRight.Life <= 0)
                    {
                        Result = -1;
                        genLeft.SayWords = "棋逢对手！";
                        genLeft.SayTime = 2f;
                        genRight.SayWords = "痛快痛快！";
                        genRight.SayTime = 2f;

                        genLeft.SayDisappear = false;
                        genRight.SayDisappear = false;

                        Title = "双方平局";
                    }
                    else if (genRight.Life <= 0)
                    {
                        Result = 1;

                        genRight.ChangeStatus("General01B", "Failure");

                        genLeft.SayWords = "谁敢再战！";
                        genLeft.SayTime = 2f;
                        genLeft.SayDisappear = false;

                        Title = genLeft.Person.Name + "获胜！";
                    }
                    else if (genLeft.Life <= 0)
                    {
                        Result = 2;

                        genLeft.ChangeStatus("General01A", "Failure");

                        genRight.SayWords = "不过如此！";
                        genRight.SayTime = 2f;
                        genRight.SayDisappear = false;

                        Title = genRight.Person.Name + "获胜！";
                    }

                }
            }
        }

        public void Draw()
        {
            if (IsVisible && Stage != "Cloud")
            {

                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\Cloud.png", basePos + cloudPos, cloudRec, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.01f);

                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\Tree.png", basePos + treePos, treeRec, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.02f);

                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\Land.png", basePos + landPos, landRec, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.03f);

                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\Avatar.png", basePos + new Vector2(15 + 10, 10 + 10), null, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.04f);

                CacheManager.DrawZhsanAvatar(genLeft.Person, new Rectangle(new Point(Convert.ToInt32(basePos.X + 15 + 10), Convert.ToInt32(basePos.Y + 10 + 10)), new Point(150, 150)), depth - 0.035f, PortraitSize.Medium, Color.White * Alpha);

                CacheManager.DrawString(null, genLeft.Person.Name, basePos + new Vector2(15 + 10 + 10, 10 + 10 + 10), Color.Red * Alpha, 0f, Vector2.Zero, scale.X * 0.8f, SpriteEffects.None, depth - 0.045f);

                CacheManager.DrawString(null, "武力：" + genLeft.Force, basePos + new Vector2(25, 175), Color.DarkRed * Alpha, 0f, Vector2.Zero, scale.X * 0.8f, SpriteEffects.None, depth - 0.036f);

                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\SwordBlackLeft.png", basePos + new Vector2(210, 25), null, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.04f);

                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\SwordBlackLeft.png", basePos + new Vector2(210, 55), null, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.04f);

                // 修复：确保技能条宽度至少为1像素
                int skillBarWidth = Math.Max(1, Convert.ToInt32(288 * Convert.ToSingle(genLeft.Skill) / 100f));
                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\SwordCyanLeft.png", basePos + new Vector2(210, 25), new Rectangle(0, 0, skillBarWidth, 25), Color.White * Alpha, SpriteEffects.None, scale, depth - 0.045f);

                // 修复：确保生命条宽度至少为1像素
                int lifeBarWidth = Math.Max(1, Convert.ToInt32(288 * Convert.ToSingle(genLeft.Life) / 100f));
                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\SwordRedLeft.png", basePos + new Vector2(210, 55), new Rectangle(0, 0, lifeBarWidth, 25), Color.White * Alpha, SpriteEffects.None, scale, depth - 0.045f);



                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\Avatar.png", basePos + new Vector2(1000 - 15 - 15 - 150, 620 - 20 - 15 - 150), null, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.04f);

                CacheManager.DrawZhsanAvatar(genRight.Person, new Rectangle(new Point(Convert.ToInt32(basePos.X + 1000 - 15 - 15 - 150), Convert.ToInt32(basePos.Y + 620 - 20 - 15 - 150)), new Point(150, 150)), depth - 0.035f, PortraitSize.Medium, Color.White * Alpha);

                CacheManager.DrawString(null, genRight.Person.Name, basePos + new Vector2(1000 - 15 - 15 - 140, 620 - 20 - 15 - 140), Color.Red * Alpha, 0f, Vector2.Zero, scale.X * 0.8f, SpriteEffects.None, depth - 0.045f);

                CacheManager.DrawString(null, "武力：" + genRight.Force, basePos + new Vector2(1000 - 140, 620 - 218), Color.DarkRed * Alpha, 0f, Vector2.Zero, scale.X * 0.8f, SpriteEffects.None, depth - 0.036f);

                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\SwordBlackRight.png", basePos + new Vector2(510, 520), null, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.04f);

                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\SwordBlackRight.png", basePos + new Vector2(510, 550), null, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.04f);

                // 修复：确保右边技能条宽度至少为1像素
                int rightSkillBarWidth = Math.Max(1, Convert.ToInt32(288 * Convert.ToSingle(genRight.Skill) / 100));
                int rightSkillBarOffset = Convert.ToInt32(288 * (1 - Convert.ToSingle(genRight.Skill) / 100f));
                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\SwordCyanRight.png", basePos + new Vector2(510 + rightSkillBarOffset, 520), new Rectangle(rightSkillBarOffset, 0, rightSkillBarWidth, 25), Color.White * Alpha, SpriteEffects.None, scale, depth - 0.045f);

                // 修复：确保右边生命条宽度至少为1像素
                int rightLifeBarWidth = Math.Max(1, Convert.ToInt32(288 * Convert.ToSingle(genRight.Life) / 100));
                int rightLifeBarOffset = Convert.ToInt32(288 * (1 - Convert.ToSingle(genRight.Life) / 100f));
                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\SwordRedRight.png", basePos + new Vector2(510 + rightLifeBarOffset, 550), new Rectangle(rightLifeBarOffset, 0, rightLifeBarWidth, 25), Color.White * Alpha, SpriteEffects.None, scale, depth - 0.045f);

                CacheManager.DrawString(null, "第 " + round + " 回合", basePos + new Vector2(550, 35), Color.Red * Alpha, 0f, Vector2.Zero, scale.X, SpriteEffects.None, depth - 0.036f);

                btnStory.Draw();

                if (btnStory.Selected)
                {
                    btnPagePre.Draw();

                    btnPageNext.Draw();

                    CacheManager.DrawString(null, "1/2 回合", basePos + new Vector2(35 + 138, 557), Color.Blue * Alpha, 0f, Vector2.Zero, scale.X * 0.7f, SpriteEffects.None, depth - 0.036f);
                }

                btnSpeed.Draw();

                //CacheManager.DrawString(null, "X" + Speed, basePos + new Vector2(750 + 50, 35), Color.Black * Alpha, 0f, Vector2.Zero, scale.X, SpriteEffects.None, depth - 0.036f);

                btnSpeedDown.Draw();

                btnSpeedUp.Draw();
                
                // 绘制指令按钮（非演示模式）
                if (commandWindowVisible && playerGeneral != null && btnAtk != null && btnNrm != null && btnDef != null)
                {
                    btnAtk.Draw();
                    btnNrm.Draw();
                    btnDef.Draw();

                    // 绘制按钮文字
                    Vector2 cmdTextBase = basePos + new Vector2(350, 550);
                    CacheManager.DrawString(null, $"Tick {commandTicksRemaining}", cmdTextBase + new Vector2(200, 2), Color.DarkGoldenrod, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, depth - 0.06f);
                    CacheManager.DrawString(null, "攻", cmdTextBase + new Vector2(15, 2), Color.Red, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, depth - 0.06f);
                    CacheManager.DrawString(null, "普", cmdTextBase + new Vector2(60 + 15, 2), Color.Black, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, depth - 0.06f);
                    CacheManager.DrawString(null, "守", cmdTextBase + new Vector2(120 + 15, 2), Color.Blue, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, depth - 0.06f);
                }
                else if (playerGeneral == null)
                {
                    // 演示模式提示
                    CacheManager.DrawString(null, "演示模式 - AI托管中", basePos + new Vector2(400, 550), Color.Yellow * Alpha, 0f, Vector2.Zero, scale.X, SpriteEffects.None, depth - 0.045f);
                }

                if (basePos.X - 128 + 30 <= genLeft.Position.X && genLeft.Position.X < basePos.X + 1000 - 70)
                {
                    if (basePos.X > genLeft.Position.X)
                    {
                        genLeft.LimiteWidth = Convert.ToInt32(128 - (basePos.X - genLeft.Position.X));

                        genLeft.LimiteOrder = true;
                    }
                    else if (genLeft.Position.X > basePos.X + 1000 - 128)
                    {
                        genLeft.LimiteWidth = Convert.ToInt32(1000 - (genLeft.Position.X - basePos.X));

                        genLeft.LimiteOrder = false;
                    }
                    else
                    {
                        genLeft.LimiteWidth = 0;
                    }

                    genLeft.Draw();
                }

                if (basePos.X - 128 + 30 <= genRight.Position.X && genRight.Position.X < basePos.X + 1000 - 65)
                {
                    if (basePos.X > genRight.Position.X)
                    {
                        genRight.LimiteWidth = Convert.ToInt32(128 - (basePos.X - genRight.Position.X));

                        genRight.LimiteOrder = true;
                    }
                    else if (genRight.Position.X > basePos.X + 1000 - 128)
                    {
                        genRight.LimiteWidth = Convert.ToInt32(1000 - (genRight.Position.X - basePos.X));

                        genRight.LimiteOrder = false;
                    }
                    else
                    {
                        genRight.LimiteWidth = 0;
                    }

                    genRight.Draw();
                }

                if (genLeft.SayTime > 0f || !genLeft.SayDisappear)
                {
                    CacheManager.Draw(@"Content\Textures\Resources\Dantiao\StarLeft.png", basePos + new Vector2(185, 82), null, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.035f);

                    CacheManager.DrawString(null, genLeft.SayWords.WordsSubString(Convert.ToInt32((1 - genLeft.SayTime / genLeft.SayTimeTotal) * genLeft.SayWords.Length), 0).SplitLineString(12), basePos + new Vector2(185 + 45, 82 + 38), Color.Black * Alpha, 0f, Vector2.Zero, scale.X * 0.8f, SpriteEffects.None, depth - 0.036f);
                }

                if (genRight.SayTime > 0f || !genRight.SayDisappear)
                {
                    CacheManager.Draw(@"Content\Textures\Resources\Dantiao\StarRight.png", basePos + new Vector2(470, 405), null, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.035f);

                    CacheManager.DrawString(null, genRight.SayWords.WordsSubString(Convert.ToInt32((1 - genRight.SayTime / genRight.SayTimeTotal) * genRight.SayWords.Length), 0).SplitLineString(12), basePos + new Vector2(470 + 35, 405 + 38), Color.Black * Alpha, 0f, Vector2.Zero, scale.X * 0.8f, SpriteEffects.None, depth - 0.036f);
                }

                if (String.IsNullOrEmpty(Title))
                {
                    
                }
                else
                {
                    CacheManager.DrawString(null, Title, basePos + new Vector2(350, 150), Color.Red * Alpha, 0f, Vector2.Zero, scale.X * 3f, SpriteEffects.None, depth - 0.036f);
                }

                if (ViewExit)
                {
                    string exitWords = "点击任意处以退出。";

                    CacheManager.DrawString(null, exitWords, basePos + new Vector2(300, 450), Color.Black * Alpha, 0f, Vector2.Zero, scale.X * 1.5f, SpriteEffects.None, depth - 0.036f);
                }

                CacheManager.Draw(@"Content\Textures\Resources\Dantiao\Ground.png", basePos, null, Color.White * Alpha, SpriteEffects.None, scale, depth - 0.09f);

            }
        }
    }

}
