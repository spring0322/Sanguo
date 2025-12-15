using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using GameObjects;
using GameGlobal;
using GameManager;
using WorldOfTheThreeKingdoms.Resources;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 对话条目 - 本地定义，避免与GameGlobal冲突
    /// </summary>
    public class DialogueEntry
    {
        public GameGlobal.DialogueType Type { get; set; }
        public GameGlobal.RelationType Relation { get; set; }
        public string LeaderText { get; set; }
        public string AdvisorText { get; set; }
    }

    /// <summary>
    /// 头像尺寸枚举
    /// </summary>
    public enum PortraitSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// 纹理管理器（临时实现）
    /// </summary>
    public static class TextureManager
    {
        public static Texture2D GetPortraitTexture(int pictureIndex, PortraitSize size)
        {
            try
            {
                // 使用现有的 TextureManager 获取头像
                return WorldOfTheThreeKingdoms.GameManager.TextureManager.GetPortraitTexture(pictureIndex, size);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 对话UI系统 - 用于显示军师任命/罢免对话
    /// </summary>
    public class DialogueUI
    {
        /// <summary>
        /// 对话状态枚举
        /// </summary>
        public enum State
        {
            Hidden,         // 隐藏状态
            LeaderTalking,  // 君主说话中
            WaitClick1,     // 等待点击（君主说完）
            AdvisorTalking, // 军师说话中
            WaitClick2,     // 等待点击（军师说完）
            Finished        // 播放完毕，等待回调处理
        }

        private SpriteFont font;
        private Texture2D pixelTexture;
        private Texture2D backgroundTexture;
        
        // 对话状态管理
        private State currentState = State.Hidden;
        
        /// <summary>
        /// 当前对话状态（公共访问）
        /// </summary>
        public State CurrentState => currentState;
        
        // 对话内容
        private Person leader;
        private Person advisor;
        private DialogueEntry currentDialogue;
        
        // 兼容性字段
        private Person currentLeader => leader;
        private Person currentAdvisor => advisor;
        
        // 文本显示系统
        private string currentText = "";    // 当前显示的文本
        private string targetText = "";     // 目标完整文本
        private float textTimer = 0f;       // 打字机效果计时器
        private const float TextSpeed = 0.05f; // 打字速度（秒/字符）
        
        /// <summary>
        /// 当前显示的文本（公共访问）
        /// </summary>
        public string CurrentText => currentText;
        
        /// <summary>
        /// 目标完整文本（公共访问）
        /// </summary>
        public string TargetText => targetText;
        
        // 震动效果系统
        private Vector2 shakeOffset = Vector2.Zero;  // 震动偏移量
        private float shakeTimer = 0f;               // 震动计时器
        private float shakeDuration = 0f;            // 震动持续时间
        private float shakeIntensity = 0f;           // 震动强度
        
        // UI布局
        private const int DialogueWidth = 600;
        private const int DialogueHeight = 300;
        private Vector2 dialoguePosition;
        private Rectangle dialogRect;
        
        // 新增属性支持改进的Draw方法
        public SpriteFont TextFont => font; // 可能为null，Draw方法需要检查
        public Texture2D BackgroundTexture => backgroundTexture ?? pixelTexture;
        
        // 鼠标状态
        private MouseState prevMouse;
        private bool waitingForClick = false;

        /// <summary>
        /// 对话是否处于活动状态
        /// </summary>
        public bool IsActive => currentState != State.Hidden && currentState != State.Finished;

        /// <summary>
        /// 【新增】外部只读属性：判断对话是否刚刚播放完毕
        /// 用于 MainGameScreen 检测并触发回调
        /// </summary>
        public bool IsFinished
        {
            get { return currentState == State.Finished; }
        }

        public DialogueUI()
        {
        }

        /// <summary>
        /// 加载内容资源
        /// </summary>
        public void LoadContent(ContentManager content, GraphicsDevice device)
        {
            // 加载字体 - 改进的容错处理
            try 
            { 
                font = content.Load<SpriteFont>("FontS"); 
                System.Diagnostics.Debug.WriteLine("[DialogueUI] 成功加载字体: FontS");
            }
            catch (Exception ex1)
            { 
                System.Diagnostics.Debug.WriteLine($"[DialogueUI] 加载FontS失败: {ex1.Message}");
                try 
                { 
                    font = content.Load<SpriteFont>("Fonts/FontS"); 
                    System.Diagnostics.Debug.WriteLine("[DialogueUI] 成功加载字体: Fonts/FontS");
                } 
                catch (Exception ex2)
                { 
                    System.Diagnostics.Debug.WriteLine($"[DialogueUI] 加载Fonts/FontS失败: {ex2.Message}");
                    try
                    {
                        // 尝试加载其他可能的字体
                        font = content.Load<SpriteFont>("Font");
                        System.Diagnostics.Debug.WriteLine("[DialogueUI] 成功加载字体: Font");
                    }
                    catch (Exception ex3)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DialogueUI] 加载Font失败: {ex3.Message}");
                        // 字体加载完全失败，但不抛出异常，使用null字体
                        font = null;
                        System.Diagnostics.Debug.WriteLine("[DialogueUI] 警告: 所有字体加载失败，对话UI将无法正常显示文本");
                    }
                } 
            }

            // 创建像素纹理
            CreatePixelTexture(device);

            // 设置对话框位置（根据你的截图，对话框在右侧）
            dialoguePosition = new Vector2(
                device.Viewport.Width - DialogueWidth - 50, // 右侧留50像素边距
                device.Viewport.Height - DialogueHeight - 50 // 底部留50像素边距
            );
            
            // 设置对话框矩形
            dialogRect = new Rectangle(
                (int)dialoguePosition.X,
                (int)dialoguePosition.Y,
                DialogueWidth,
                DialogueHeight
            );
            
            System.Diagnostics.Debug.WriteLine("[DialogueUI] LoadContent完成");
        }

        /// <summary>
        /// 安全创建像素纹理
        /// </summary>
        private void CreatePixelTexture(GraphicsDevice device)
        {
            if (pixelTexture == null || pixelTexture.IsDisposed || pixelTexture.GraphicsDevice != device)
            {
                try 
                {
                    pixelTexture = new Texture2D(device, 1, 1);
                    pixelTexture.SetData(new[] { Color.White });
                }
                catch 
                {
                    // 纹理创建失败，记录但不崩溃
                    System.Diagnostics.Debug.WriteLine("[DialogueUI] 像素纹理创建失败");
                }
            }
        }

        /// <summary>
        /// 开始对话
        /// </summary>
        public void StartDialogue(Person leader, Person advisor, DialogueEntry dialogue)
        {
            this.leader = leader;
            this.advisor = advisor;
            currentDialogue = dialogue;
            
            waitingForClick = false;
            
            // 使用新的状态切换方法开始君主说话
            SwitchState(State.LeaderTalking);
            
            System.Diagnostics.Debug.WriteLine($"[DialogueUI] 开始对话: {leader?.Name} -> {advisor?.Name}");
        }

        /// <summary>
        /// 必须确保每次切换状态时，currentText 被重置
        /// </summary>
        private void SwitchState(State newState)
        {
            currentState = newState;
            
            // ===== 关键修复 1: 彻底重置文本 =====
            currentText = "";   // 清空屏幕上正在显示的字
            targetText = "";    // 清空目标文本
            textTimer = 0f;
            shakeTimer = 0f;
            shakeOffset = Vector2.Zero;
            
            switch (newState)
            {
                case State.LeaderTalking:
                    // ===== 关键修复 2: 只加载君主的文本 =====
                    // 格式化：「xxxxxx」
                    targetText = "「" + (currentDialogue?.LeaderText ?? "") + "」";
                    waitingForClick = false;
                    
                    // 触发君主震动效果
                    if (currentDialogue != null && currentDialogue.Type == GameGlobal.DialogueType.Refusal && currentDialogue.Relation == GameGlobal.RelationType.Hate)
                    {
                        TriggerShake(0.3f, 5.0f);
                    }
                    else
                    {
                        TriggerShake(0.2f, 1.5f); // 默认轻微震动
                    }
                    break;
                    
                case State.AdvisorTalking:
                    // ===== 关键修复 3: 只加载军师的文本 =====
                    // 此时屏幕上旧的君主文本已经被上面 currentText="" 清掉了
                    targetText = "「" + (currentDialogue?.AdvisorText ?? "") + "」";
                    waitingForClick = false;
                    
                    // 触发军师震动效果
                    if (advisor != null && currentDialogue != null)
                    {
                        if (advisor.Intelligence < 50)
                        {
                            TriggerShake(0.5f, 3.0f);
                        }
                        else if (currentDialogue.Type == GameGlobal.DialogueType.Refusal && currentDialogue.Relation == GameGlobal.RelationType.Hate)
                        {
                            TriggerShake(1.0f, 10.0f);
                        }
                        else if (currentDialogue.Type == GameGlobal.DialogueType.Bond)
                        {
                            TriggerShake(0.4f, 2.0f);
                        }
                        else if (advisor.Intelligence >= 80)
                        {
                            TriggerShake(0.1f, 1.0f);
                        }
                        else
                        {
                            TriggerShake(0.3f, 2.0f);
                        }
                    }
                    break;
                    
                case State.WaitClick1:
                case State.WaitClick2:
                    waitingForClick = true;
                    break;
                    
                case State.Finished:
                    targetText = "";
                    waitingForClick = false;
                    StopShake();
                    System.Diagnostics.Debug.WriteLine("[DialogueUI] 对话结束，等待回调执行");
                    break;
                    
                case State.Hidden:
                    targetText = "";
                    waitingForClick = false;
                    StopShake();
                    break;
            }
        }

        /// <summary>
        /// 更新对话UI
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (currentState == State.Hidden) return;

            MouseState mouse = Mouse.GetState();
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            bool clicked = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
            
            // 更新震动效果
            UpdateShake(deltaTime);

            // 根据当前状态处理逻辑
            switch (currentState)
            {
                case State.LeaderTalking:
                    // 君主说话中 - 打字机效果
                    if (currentText.Length < targetText.Length)
                    {
                        textTimer += deltaTime;
                        if (textTimer >= TextSpeed)
                        {
                            textTimer = 0f;
                            currentText = targetText.Substring(0, currentText.Length + 1);
                        }
                        
                        // 检查鼠标点击以跳过打字效果
                        if (clicked)
                        {
                            currentText = targetText; // 立即显示全部文本
                        }
                    }
                    else
                    {
                        // 君主说完了，等待点击
                        SwitchState(State.WaitClick1);
                    }
                    break;
                    
                case State.WaitClick1:
                    // 等待点击继续到军师说话
                    if (clicked)
                    {
                        // 开始军师说话
                        SwitchState(State.AdvisorTalking);
                    }
                    break;
                    
                case State.AdvisorTalking:
                    // 军师说话中 - 打字机效果
                    if (currentText.Length < targetText.Length)
                    {
                        textTimer += deltaTime;
                        if (textTimer >= TextSpeed)
                        {
                            textTimer = 0f;
                            currentText = targetText.Substring(0, currentText.Length + 1);
                        }
                        
                        // 检查鼠标点击以跳过打字效果
                        if (clicked)
                        {
                            currentText = targetText; // 立即显示全部文本
                        }
                    }
                    else
                    {
                        // 军师说完了，等待点击
                        SwitchState(State.WaitClick2);
                    }
                    break;
                    
                case State.WaitClick2:
                    // 等待点击结束对话
                    if (clicked)
                    {
                        SwitchState(State.Finished);
                    }
                    break;
                    
                case State.Finished:
                    // 在这个状态下等待 MainGameScreen 检测并执行回调
                    // 回调执行完后会调用 Hide() 方法
                    break;
            }

            prevMouse = mouse;
        }

        /// <summary>
        /// 开始震动效果
        /// </summary>
        /// <param name="duration">震动持续时间（秒）</param>
        /// <param name="intensity">震动强度（像素）</param>
        private void StartShake(float duration, float intensity)
        {
            shakeDuration = duration;
            shakeTimer = 0f;
            shakeIntensity = intensity;
        }

        /// <summary>
        /// 停止震动效果
        /// </summary>
        private void StopShake()
        {
            shakeDuration = 0f;
            shakeTimer = 0f;
            shakeIntensity = 0f;
            shakeOffset = Vector2.Zero;
        }

        /// <summary>
        /// 更新震动效果
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateShake(float deltaTime)
        {
            if (shakeDuration <= 0f)
            {
                shakeOffset = Vector2.Zero;
                return;
            }

            shakeTimer += deltaTime;
            
            if (shakeTimer >= shakeDuration)
            {
                // 震动结束
                StopShake();
                return;
            }

            // 计算震动强度衰减（随时间减弱）
            float progress = shakeTimer / shakeDuration;
            float currentIntensity = shakeIntensity * (1f - progress);

            // 生成随机震动偏移
            Random random = new Random();
            float offsetX = (float)(random.NextDouble() * 2.0 - 1.0) * currentIntensity;
            float offsetY = (float)(random.NextDouble() * 2.0 - 1.0) * currentIntensity;
            
            shakeOffset = new Vector2(offsetX, offsetY);
        }

        /// <summary>
        /// 绘制对话UI
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            try
            {
                // 如果字体未加载，跳过绘制文本部分
                if (TextFont == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DialogueUI] 字体未加载，跳过文本绘制");
                    return;
                }

                spriteBatch.Begin();

                // ===== 1. 确定当前主角是谁 =====
                Person currentSpeaker = null;
                bool isAdvisor = false;
                
                if (currentState == State.LeaderTalking || currentState == State.WaitClick1)
                {
                    currentSpeaker = this.leader;
                    isAdvisor = false;
                }
                else if (currentState == State.AdvisorTalking || currentState == State.WaitClick2)
                {
                    currentSpeaker = this.advisor;
                    isAdvisor = true;
                }

                // 如果状态不对（比如 Finished），直接不画
                if (currentSpeaker == null) return;

                // ===== 2. 绘制头像 (只画当前主角) =====
                // 加上震动偏移量
                Rectangle portraitRect = new Rectangle(
                    0 + (int)shakeOffset.X, 
                    300 + (int)shakeOffset.Y, 
                    240, 360);
                
                // 获取人物头像纹理
                Texture2D portraitTexture = null;
                try
                {
                    // 使用现有的 TextureManager 获取头像
                    portraitTexture = WorldOfTheThreeKingdoms.GameManager.TextureManager.GetPortraitTexture(currentSpeaker.ID, PortraitSize.Medium);
                }
                catch
                {
                    // 如果获取失败，使用默认纹理
                    portraitTexture = pixelTexture;
                }
                
                if (portraitTexture != null)
                {
                    // 如果是军师，可以选择水平翻转 (FlipHorizontally) 让他面向左
                    SpriteEffects effects = isAdvisor ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    spriteBatch.Draw(portraitTexture, portraitRect, null, Color.White, 0f, Vector2.Zero, effects, 0f);
                }

                // ===== 3. 绘制对话框 =====
                spriteBatch.Draw(BackgroundTexture, dialogRect, Color.White);

                // ===== 4. 绘制名字 (独立绘制，不混在文本里) =====
                Vector2 namePos = new Vector2(dialogRect.X + 20, dialogRect.Y + 15);
                spriteBatch.DrawString(TextFont, currentSpeaker.Name, namePos, Color.Gold);

                // ===== 5. 绘制内容 (只绘制 currentText) =====
                // 确保这里只画 currentText，不要画 dialogue.LeaderText + dialogue.AdvisorText
                Vector2 textPos = new Vector2(dialogRect.X + 30, dialogRect.Y + 45);
                string wrappedText = WrapText(TextFont, currentText, DialogueWidth - 60);
                spriteBatch.DrawString(TextFont, wrappedText, textPos, Color.White);

                // ===== 6. 绘制提示文字 =====
                string hint = "";
                if (currentText.Length < targetText.Length)
                {
                    hint = "点击跳过打字效果...";
                }
                else if (waitingForClick)
                {
                    hint = "点击任意位置继续...";
                }

                if (!string.IsNullOrEmpty(hint))
                {
                    Vector2 hintSize = TextFont.MeasureString(hint);
                    Vector2 hintPos = new Vector2(dialogRect.X + DialogueWidth - hintSize.X - 20, dialogRect.Y + DialogueHeight - 30);
                    spriteBatch.DrawString(TextFont, hint, hintPos, Color.Yellow);
                }

                spriteBatch.End();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DialogueUI] 绘制异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 【新增】强制关闭/隐藏对话框
        /// 当回调执行完毕后调用，重置状态以便下次使用
        /// </summary>
        public void Hide()
        {
            // 使用统一的状态切换方法
            SwitchState(State.Hidden);
            
            // 清理对话数据
            leader = null;
            advisor = null;
            currentDialogue = null;
            
            // (可选) 如果有正在播放的打字音效，在这里停止
            // SoundManager.StopSE();
            
            System.Diagnostics.Debug.WriteLine("[DialogueUI] 对话UI已隐藏");
        }

        /// <summary>
        /// 获取对话标题
        /// </summary>
        private string GetDialogueTitle()
        {
            if (currentDialogue?.Type == GameGlobal.DialogueType.Recall)
            {
                return "罢免军师";
            }
            else if (currentDialogue?.Type == GameGlobal.DialogueType.Bond)
            {
                return "特殊羁绊";
            }
            else if (currentDialogue?.Type == GameGlobal.DialogueType.Personality)
            {
                return "性格匹配";
            }
            else
            {
                return "任命军师";
            }
        }

        /// <summary>
        /// 【新增】跳过打字机效果，立即显示全部文本
        /// </summary>
        public void SkipTypewriter()
        {
            if ((currentState == State.LeaderTalking || currentState == State.AdvisorTalking) && currentText.Length < targetText.Length)
            {
                currentText = targetText;
                textTimer = 0f;
                System.Diagnostics.Debug.WriteLine("[DialogueUI] 跳过打字机效果");
            }
        }

        /// <summary>
        /// 【新增】强制完成对话（用于调试或特殊情况）
        /// </summary>
        public void ForceFinish()
        {
            if (currentState == State.LeaderTalking || currentState == State.AdvisorTalking || 
                currentState == State.WaitClick1 || currentState == State.WaitClick2)
            {
                SwitchState(State.Finished);
                System.Diagnostics.Debug.WriteLine("[DialogueUI] 强制完成对话");
            }
        }

        /// <summary>
        /// 【新增】获取当前状态信息（用于调试）
        /// </summary>
        public string GetStateInfo()
        {
            return $"State: {currentState}, Text: {currentText.Length}/{targetText.Length}, WaitingClick: {waitingForClick}";
        }

        /// <summary>
        /// 【新增】触发震动效果（供外部调用）
        /// </summary>
        /// <param name="duration">震动持续时间（秒）</param>
        /// <param name="intensity">震动强度（像素）</param>
        public void TriggerShake(float duration = 0.5f, float intensity = 3.0f)
        {
            StartShake(duration, intensity);
        }

        /// <summary>
        /// 绘制边框
        /// </summary>
        private void DrawBorder(SpriteBatch sb, Texture2D tex, Rectangle r, int thickness, Color color)
        {
            // 上边框
            sb.Draw(tex, new Rectangle(r.X, r.Y, r.Width, thickness), color);
            // 下边框
            sb.Draw(tex, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
            // 左边框
            sb.Draw(tex, new Rectangle(r.X, r.Y, thickness, r.Height), color);
            // 右边框
            sb.Draw(tex, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
        }

        /// <summary>
        /// 文本换行
        /// </summary>
        private string WrapText(SpriteFont font, string text, float maxLineWidth)
        {
            if (string.IsNullOrEmpty(text) || font == null) return "";

            string result = "";
            float currentLineWidth = 0f;
            string[] words = text.Split(' ');

            foreach (string word in words)
            {
                Vector2 wordSize = font.MeasureString(word + " ");
                
                if (currentLineWidth + wordSize.X > maxLineWidth)
                {
                    result += "\n";
                    currentLineWidth = 0f;
                }
                
                result += word + " ";
                currentLineWidth += wordSize.X;
            }

            return result.TrimEnd();
        }

        /// <summary>
        /// 处理点击事件（兼容性方法）
        /// </summary>
        public void HandleClick()
        {
            if (!IsActive) return;

            // 情况 A: 还在打字中 -> 瞬间显示全句
            if (currentState == State.LeaderTalking || currentState == State.AdvisorTalking)
            {
                currentText = targetText; // 直接显示完整
                
                // 切换到"等待点击"状态
                if (currentState == State.LeaderTalking) 
                    SwitchState(State.WaitClick1);
                else 
                    SwitchState(State.WaitClick2);
                return;
            }

            // 情况 B: 君主说完了 (WaitClick1) -> 切换到 军师说话
            if (currentState == State.WaitClick1)
            {
                // ▼▼▼ 这里是"切镜"的关键 ▼▼▼
                SwitchState(State.AdvisorTalking); 
                return;
            }

            // 情况 C: 军师说完了 (WaitClick2) -> 结束对话
            if (currentState == State.WaitClick2)
            {
                SwitchState(State.Finished);
                return;
            }
        }
    }
}