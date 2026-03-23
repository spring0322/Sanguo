using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;
using GameManager;

namespace tupianwenziPlugin
{

    public class tupianwenzilei
    {
        internal Point BackgroundSize;
        internal PlatformTexture BackgroundTexture;
        internal Queue<PlatformTexture> shijiantupianduilie = new Queue<PlatformTexture>();
        internal Queue<Rectangle> juxingduilie = new Queue<Rectangle>();
        internal Queue<string> shijianshengyinduilie = new Queue<string>();


        internal Rectangle shijiantupianjuxing;
        internal PlatformTexture shijiantupian;
        internal string shijianshengyin;


        internal FreeRichText BuildingRichText = new FreeRichText();
        internal Rectangle ClientPosition;
        private Keys currentKey;
        private Point DisplayOffset;
        internal Queue<GameObjectAndBranchName> DisplayQueue = new Queue<GameObjectAndBranchName>();
        internal PlatformTexture FirstPageButtonDisabledTexture;
        private PlatformTexture FirstPageButtonDisplayTexture;
        internal Rectangle FirstPageButtonPosition;
        internal PlatformTexture FirstPageButtonSelectedTexture;
        internal PlatformTexture FirstPageButtonTexture;
        private bool firstShowing;
        internal bool HasConfirmationDialog = false;
        internal IConfirmationDialog iConfirmationDialog;
        internal IGameContextMenu iContextMenu;
        internal Itupianwenzi iPersonTextDialog;
        private bool isShowing;
        private bool diyigeshengyin=false;
        internal FreeText NameText;
        internal GameDelegates.VoidFunction NoFunction;
        internal Rectangle PortraitClient;
        internal FreeRichText RichText = new FreeRichText();
        
        internal Person SpeakingPerson;
        private DateTime startShowingTime;
        internal GameObjectTextTree TextTree = new GameObjectTextTree();
        internal GameDelegates.VoidFunction YesFunction;

        internal event GameDelegates.VoidFunction CloseFunction;
        internal string TryToShowString = "";

        // 🚀 性能优化：添加文本计算缓存，避免每帧重复计算
        private string _cachedTryToShowString = "";
        private string _cachedWrappedText = "";
        private string[] _cachedTextLines = null;
        private int _cachedDialogWidth = 0;
        private int _cachedDialogHeight = 0;
        private Rectangle _cachedDialogRect = Rectangle.Empty;
        private Vector2 _cachedTextPosition = Vector2.Zero;
        private float _cachedEffectScale = 0.8f;
        
        // 🔧 修复：添加人物缓存，检测人物变化
        private Person _cachedSpeakingPerson = null;

        internal void Close(Screen screen)
        {
            if (this.DequeueAndDisplay(screen))
            {
                this.IsShowing = false;
            }
        }


        private bool DequeueAndDisplay(Screen screen)
        {


            if (this.DisplayQueue.Count > 0)
            {
                this.startShowingTime = DateTime.Now;
                GameObjectAndBranchName name = this.DisplayQueue.Dequeue();
                this.shijiantupian = this.shijiantupianduilie.Dequeue();
                this.shijiantupianjuxing = this.juxingduilie.Dequeue();
                this.SetPosition(ShowPosition.Bottom, screen);
                this.shijianshengyin = this.shijianshengyinduilie.Dequeue();
                // 🔧 修复：检测人物变化，清除相关缓存
                var newSpeakingPerson = name.person;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DequeueAndDisplay] 说话人={newSpeakingPerson?.Name ?? "null"}(ID:{newSpeakingPerson?.ID ?? -1}), 分支={name.branchName}");
                #endif
                
                if (_cachedSpeakingPerson != newSpeakingPerson)
                {
                    _cachedSpeakingPerson = newSpeakingPerson;
                    // 清除所有缓存，确保头像正确显示
                    ClearAllCache();
                }
                
                this.SpeakingPerson = newSpeakingPerson;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"========== [DequeueAndDisplay] 显示弹窗 ==========");
                System.Diagnostics.Debug.WriteLine($"队列长度={DisplayQueue.Count}");
                System.Diagnostics.Debug.WriteLine($"说话人={newSpeakingPerson?.Name ?? "null"}(ID:{newSpeakingPerson?.ID ?? -1})");
                System.Diagnostics.Debug.WriteLine($"分支={name.branchName}");
                System.Diagnostics.Debug.WriteLine($"========== [DequeueAndDisplay] 结束 ==========\n");
                #endif
                
                this.NameText.Text = name.person?.Name ?? "未知";
                if (name.TryToShowString != null && name.TryToShowString.Length > 1)
                {
                    this.TryToShowString = name.TryToShowString;
                }
                else this.TryToShowString = "";
                
                // 🚀 计算文本长度，决定动态缩放比例以防止溢出
                float baseSize = 15f; // From XML default
                float newRichTextSize = baseSize;
                _cachedEffectScale = 0.8f;
                int totalLen = 0;
                if (name.texts != null)
                {
                    foreach (var tx in name.texts)
                    {
                        if (tx != null && tx.Text != null) totalLen += tx.Text.Length;
                    }
                }
                int effectLen = this.TryToShowString != null ? this.TryToShowString.Length : 0;
                if (effectLen > 0)
                {
                     if (totalLen + effectLen > 160) { newRichTextSize = baseSize * 0.65f; _cachedEffectScale = 0.60f; }
                     else if (totalLen + effectLen > 110) { newRichTextSize = baseSize * 0.75f; _cachedEffectScale = 0.65f; }
                     else if (totalLen + effectLen > 70) { newRichTextSize = baseSize * 0.85f; _cachedEffectScale = 0.75f; }
                     else if (totalLen + effectLen > 40) { newRichTextSize = baseSize * 0.95f; _cachedEffectScale = 0.8f; }
                }
                this.RichText.Builder.Size = newRichTextSize;
                
                // 🚀 性能优化：清除文本缓存，确保下次绘制时重新计算
                _cachedTryToShowString = "";
                if(TryToShowString != null && TryToShowString.Length > 1)
                {
                    // 强制在有效果时仍然在下方居中显示
                    this.SetPosition(ShowPosition.Bottom, screen);
                }
                this.RichText.Clear();
                if (this.diyigeshengyin)
                {
                    screen.PlayNormalSound(this.shijianshengyin);
                    this.diyigeshengyin = false;
                }

                this.RichText.Texts = name.texts;
                this.RichText.ResortTexts();
                if (name.iConfirmationDialog != null)
                {
                    this.iConfirmationDialog = name.iConfirmationDialog;
                    this.iConfirmationDialog.SetPersonTextDialog(this.iPersonTextDialog);
                    this.iConfirmationDialog.AddYesFunction(name.YesFunction);
                    this.iConfirmationDialog.AddNoFunction(name.NoFunction);
                    this.iConfirmationDialog.IsShowing = true;
                }
                else
                {
                    this.iConfirmationDialog = null;
                    this.YesFunction = null;
                    this.NoFunction = null;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 🔧 修复：清除所有缓存的方法
        /// </summary>
        private void ClearAllCache()
        {
            _cachedTryToShowString = "";
            _cachedWrappedText = "";
            _cachedTextLines = null;
            _cachedDialogWidth = 0;
            _cachedDialogHeight = 0;
            _cachedDialogRect = Rectangle.Empty;
            _cachedTextPosition = Vector2.Zero;
        }

        internal void Draw()
        {
            // 🔧 修复：添加人物有效性检查
            if (this.SpeakingPerson != null)
            {
                // 🔧 性能优化：删除每帧执行的调试输出，避免卡顿
                Rectangle? sourceRectangle = null;

                // 🔧 修复：在绘制头像前检查人物状态，添加异常处理
                try
                {
                    // 即使人物死亡也要显示头像，但要确保PictureIndex有效
                    if (this.SpeakingPerson.PictureIndex >= 0)
                    {
                        CacheManager.DrawZhsanAvatar(this.SpeakingPerson, this.PortraitDisplayPosition, 0.201f);
                    }
                    else
                    {
                        // 如果PictureIndex无效，使用默认头像
                        System.Diagnostics.Debug.WriteLine($"[tupianwenzi] 警告：人物 {this.SpeakingPerson.Name} 的PictureIndex无效: {this.SpeakingPerson.PictureIndex}");
                        CacheManager.DrawZhsanAvatar(0, this.PortraitDisplayPosition, 0.201f); // 使用默认头像
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[tupianwenzi] 绘制头像失败: {this.SpeakingPerson?.Name ?? "未知"}, 错误: {ex.Message}");
                    // 绘制失败时使用默认头像
                    try
                    {
                        CacheManager.DrawZhsanAvatar(0, this.PortraitDisplayPosition, 0.201f);
                    }
                    catch
                    {
                        // 如果连默认头像都绘制失败，就跳过头像绘制
                    }
                }

                sourceRectangle = null;
                CacheManager.Draw(this.BackgroundTexture, this.BackgroundDisplayPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.2f);
                if (this.shijiantupian != null)
                {
                    if (this.shijiantupianjuxing.Width == 240 && this.shijiantupianjuxing.Height == 240)//如果是人物死亡图片的话
                    {
                        CacheManager.Draw(this.shijiantupian, this.shijiantupianjuxing, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.202f);
                    }
                    else
                    {
                        CacheManager.Draw(this.shijiantupian, this.shijiantupianjuxing, sourceRectangle, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.202f);
                    }
                }
                
                this.NameText.Draw(0.1999f);
                CacheManager.Draw(this.FirstPageButtonDisplayTexture, this.FirstPageButtonDisplayPosition, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.199f);
                if(TryToShowString!=null && TryToShowString.Length>1)
                {
                    // 🚀 性能优化：只有当文本内容改变时才重新计算
                    if (_cachedTryToShowString != TryToShowString)
                    {
                        _cachedTryToShowString = TryToShowString;
                        
                        // 动态计算最大可显示宽度（放在名字和头像的右侧区域内的固定宽域，原为540，统一到450以匹配主对话框宽度）
                        var maxTextWidth = 450; 
                        
                        // 计算文本所需的尺寸（使用CacheManager.AutoWrap以确保测量与绘制字体一致）
                        _cachedWrappedText = CacheManager.AutoWrap(Session.Current.Font, TryToShowString, maxTextWidth, _cachedEffectScale); 
                        
                        // 调整位置：与对话文本RichText左对齐，但在其正下方
                        var textX = this.DisplayOffset.X + this.ClientPosition.X; 
                        
                        // 根据对话实际高度，加上间距。保底高度40避免太短重叠
                        var dialogHeight = this.RichText.RealHeight;
                        if (dialogHeight < 40) dialogHeight = 40;
                        var textY = this.DisplayOffset.Y + this.ClientPosition.Y + dialogHeight + 10;
                            
                        _cachedTextPosition = new Vector2(textX, textY);
                    }
                    
                    // 绘制阴影 (黑色偏移1像素)
                    CacheManager.DrawString(Session.Current.Font, _cachedWrappedText, _cachedTextPosition + new Vector2(1,1), 
                        Color.Black, 0f, Vector2.Zero, _cachedEffectScale, SpriteEffects.None, 0.19991f);
                        
                    // 绘制正文 (金黄色效果以示区分)
                    CacheManager.DrawString(Session.Current.Font, _cachedWrappedText, _cachedTextPosition, 
                        new Color(255, 235, 130), 0f, Vector2.Zero, _cachedEffectScale, SpriteEffects.None, 0.1999f);
                }
                // RichText 始终绘制（部队事件需要上面RichText对话 + 下方TryToShowString效果同时显示）
                this.RichText.Draw(0.1999f);
            }
        }

        internal void Initialize()
        {
            
        }

        private void screen_OnMouseLeftDown(Point position)
        {

        }

        private void screen_OnMouseLeftUp(Point position)
        {
            if (StaticMethods.PointInRectangle(position, this.FirstPageButtonDisplayPosition))
            {
                //this.RichText.FirstPage();
                //this.FirstPageButtonDisplayTexture = this.FirstPageButtonDisabledTexture;
            }
            else if ((this.RichText.CurrentPageIndex >= (this.RichText.PageCount - 1)) && ((this.iConfirmationDialog == null) || !this.iConfirmationDialog.IsShowing))
            {
                if (!this.firstShowing)
                {
                    this.Close(Session.MainGame.mainGameScreen);
                }
                this.firstShowing = false;
            }
            else
            {
                this.RichText.NextPage();
                this.startShowingTime = DateTime.Now;
                if (this.RichText.CurrentPageIndex > 0)
                {
                    this.FirstPageButtonDisplayTexture = this.FirstPageButtonTexture;
                }
            }
        }

        private void screen_OnMouseMove(Point position, bool leftDown)
        {
            if (this.RichText.CurrentPageIndex > 0)
            {
                if (StaticMethods.PointInRectangle(position, this.FirstPageButtonDisplayPosition))
                {
                    this.FirstPageButtonDisplayTexture = this.FirstPageButtonSelectedTexture;
                }
                else
                {
                    this.FirstPageButtonDisplayTexture = this.FirstPageButtonTexture;
                }
            }
        }

        internal void SetGameObjectBranch(GameObject gongfang, GameObject gameObject, string branchName,string TryToShowString = "")
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SetGameObjectBranch] gongfang类型={gongfang?.GetType().Name}, Name={(gongfang as Person)?.Name ?? "非Person"}(ID:{(gongfang as Person)?.ID ?? -1})");
            System.Diagnostics.Debug.WriteLine($"[SetGameObjectBranch] gameObject类型={gameObject?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[SetGameObjectBranch] branchName={branchName}");
            #endif

            this.BuildingRichText.Clear();
            if (gameObject != null)
            {
                GameObjectTextBranch a = this.TextTree.GetBranch(branchName);
                if (a != null)
                {
                    this.BuildingRichText.AddGameObjectTextBranch(gameObject, a);
                }
                else
                {
                    this.BuildingRichText.AddText(branchName);
                }
            }
            else
            {
                GameObjectTextBranch a = this.TextTree.GetBranch(branchName);
                if (a != null)
                {
                    this.BuildingRichText.AddGameObjectTextBranch(gongfang, a);
                }
                else
                {
                    this.BuildingRichText.AddText(branchName);
                }
            }
            if (DisplayQueue.Count > Session.GlobalVariables.MaxTupianwenzi)
            {
                this.DisplayQueue.Dequeue();
                this.shijiantupian = this.shijiantupianduilie.Dequeue();
                this.shijiantupianjuxing = this.juxingduilie.Dequeue();
                this.shijianshengyin = this.shijianshengyinduilie.Dequeue();
            }
            if (this.HasConfirmationDialog)
            {
                this.DisplayQueue.Enqueue(new GameObjectAndBranchName(gongfang, this.BuildingRichText.Texts, branchName, this.iConfirmationDialog, this.YesFunction, this.NoFunction,TryToShowString));
                this.YesFunction = null;
                this.NoFunction = null;
            }
            else
            {
                this.DisplayQueue.Enqueue(new GameObjectAndBranchName(gongfang, this.BuildingRichText.Texts, branchName, null, null, null, TryToShowString));
            }
            this.HasConfirmationDialog = false;
        }

        internal void SetPosition(ShowPosition showPosition, Screen screen)
        {
            Rectangle rectDes = new Rectangle(0, 0, screen.viewportSize.X, screen.viewportSize.Y);
            Rectangle rect = new Rectangle(0, 0, this.BackgroundSize.X, this.BackgroundSize.Y);
            switch (showPosition)
            {
                case ShowPosition.Center:
                    rect = StaticMethods.GetCenterRectangle(rectDes, rect);
                    break;

                case ShowPosition.Top:
                    rect = StaticMethods.GetTopRectangle(rectDes, rect);
                    break;

                case ShowPosition.Left:
                    rect = StaticMethods.GetLeftRectangle(rectDes, rect);
                    break;

                case ShowPosition.Right:
                    rect = StaticMethods.GetRightRectangle(rectDes, rect);
                    break;

                case ShowPosition.Bottom:
                    rect = StaticMethods.GetBottomRectangle(rectDes, rect);
                    break;

                case ShowPosition.TopLeft:
                    rect = StaticMethods.GetTopLeftRectangle(rectDes, rect);
                    break;

                case ShowPosition.TopRight:
                    rect = StaticMethods.GetTopRightRectangle(rectDes, rect);
                    break;

                case ShowPosition.BottomLeft:
                    rect = StaticMethods.GetBottomLeftRectangle(rectDes, rect);
                    break;

                case ShowPosition.BottomRight:
                    rect = StaticMethods.GetBottomRightRectangle(rectDes, rect);
                    break;
            }
            this.DisplayOffset = new Point(rect.X, rect.Y);
            this.RichText.DisplayOffset = new Point(rect.X + this.ClientPosition.X, rect.Y + this.ClientPosition.Y);
            this.NameText.DisplayOffset = this.DisplayOffset;

            // 把图片调整到中央偏下的位置，对话框上面
            this.shijiantupianjuxing.X = rectDes.Left + (rectDes.Width - this.shijiantupianjuxing.Width) / 2;
            this.shijiantupianjuxing.Y = rect.Y - this.shijiantupianjuxing.Height - 10;
            if (this.shijiantupianjuxing.Y < 10) 
            {
                this.shijiantupianjuxing.Y = 10;
            }
        }

        internal void Update()
        {
            if ((this.iConfirmationDialog == null) || !this.iConfirmationDialog.IsShowing)
            {
                if (this.currentKey != Keys.None)
                {
                    //if (!Session.MainGame.mainGameScreen.KeyState.IsKeyUp(this.currentKey))
                    if (!InputManager.KeyBoardState.IsKeyUp(this.currentKey))
                    {
                        return;
                    }
                    this.currentKey = Keys.None;
                }
                //if (Session.MainGame.mainGameScreen.KeyState.IsKeyDown(Keys.Enter))
                if (InputManager.KeyBoardState.IsKeyDown(Keys.Enter))
                {
                    this.currentKey = Keys.Enter;
                    this.Close(Session.MainGame.mainGameScreen);
                }
                TimeSpan span = (TimeSpan) (DateTime.Now - this.startShowingTime);
                if (span.Milliseconds >= 300)
                {
                    this.firstShowing = false;
                }
                if (span.Seconds >= Setting.Current.GlobalVariables.DialogShowTime)
                {
                    this.Close(Session.MainGame.mainGameScreen);
                }
            }
        }

        private Rectangle BackgroundDisplayPosition
        {
            get
            {
                return new Rectangle(this.DisplayOffset.X, this.DisplayOffset.Y, this.BackgroundSize.X, this.BackgroundSize.Y);
            }
        }

        private Rectangle FirstPageButtonDisplayPosition
        {
            get
            {
                return new Rectangle(this.FirstPageButtonPosition.X + this.DisplayOffset.X, this.FirstPageButtonPosition.Y + this.DisplayOffset.Y, this.FirstPageButtonPosition.Width, this.FirstPageButtonPosition.Height);
            }
        }

        internal bool IsShowing
        {
            get
            {
                return this.isShowing;
            }
            set
            {
                SetIsShowing(Session.MainGame.mainGameScreen, value);
            }
        }

        public void SetIsShowing(Screen screen, bool value)
        {
            if (this.isShowing != value && Setting.Current.GlobalVariables.DialogShowTime > 0)
            {
                this.isShowing = value;
                if (value)
                {
                    if ((this.iContextMenu != null) && this.iContextMenu.IsShowing)
                    {
                        this.iContextMenu.IsShowing = false;
                    }
                    this.firstShowing = true;
                    screen.IsHolding = true;
                    this.diyigeshengyin = true;
                    screen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.tupianwenzi, UndoneWorkSubKind.None));
                    screen.OnMouseLeftDown += new Screen.MouseLeftDown(this.screen_OnMouseLeftDown);
                    screen.OnMouseLeftUp += new Screen.MouseLeftUp(this.screen_OnMouseLeftUp);
                    screen.OnMouseMove += new Screen.MouseMove(this.screen_OnMouseMove);
                    this.FirstPageButtonDisplayTexture = this.FirstPageButtonDisabledTexture;
                    screen.EnableLaterMouseEvent = false;
                    this.Close(screen);
                }
                else
                {
                    screen.IsHolding = false;
                    if (screen.PopUndoneWork().Kind != UndoneWorkKind.tupianwenzi)
                    {
                        //throw new Exception("The UndoneWork is not a tupianwenzi.");
                    }
                    screen.OnMouseLeftDown -= new Screen.MouseLeftDown(this.screen_OnMouseLeftDown);
                    screen.OnMouseLeftUp -= new Screen.MouseLeftUp(this.screen_OnMouseLeftUp);
                    screen.OnMouseMove -= new Screen.MouseMove(this.screen_OnMouseMove);
                    screen.EnableLaterMouseEvent = true;

                    this.iConfirmationDialog = null;
                    if (this.CloseFunction != null)
                    {
                        this.CloseFunction();
                        this.CloseFunction = null;
                    }
                }
            }
        }

        private Rectangle PortraitDisplayPosition
        {
            get
            {
                return new Rectangle(this.PortraitClient.X + this.DisplayOffset.X, this.PortraitClient.Y + this.DisplayOffset.Y, this.PortraitClient.Width, this.PortraitClient.Height);
            }
        }


        //private Texture2D CaiseTupianZhuanchengHeibai(GraphicsDevice device, Texture2D YuanTupian)
        //{


        //    int Height = YuanTupian.Height;
        //    int Width = YuanTupian.Width;
        //    Texture2D XinTupian = new Texture2D(device, Width, Height);


        //    Color[] pixel = GetColorDataFromTexture(YuanTupian);
        //    int XiangsuGeshu = YuanTupian.Width * YuanTupian.Height;
        //    for (int i = 0; i < XiangsuGeshu; i++)

        //    {

        //        int r, g, b, Result = 0;
        //        r = pixel[i].R;
        //        g = pixel[i].G;
        //        b = pixel[i].B;
        //        //实例程序以加权平均值法产生黑白图像       
        //        int iType = 2;
        //        switch (iType)
        //        {
        //            case 0://平均值法           
        //                Result = ((r + g + b) / 3);
        //                break;
        //            case 1://最大值法                 
        //                Result = r > g ? r : g;
        //                Result = Result > b ? Result : b;
        //                break;
        //            case 2://加权平均值法             
        //                Result = ((int)(0.7 * r) + (int)(0.2 * g) + (int)(0.1 * b));
        //                break;
        //        }
        //        pixel[i] = new Color(Result, Result, Result);

        //        XinTupian.SetData<Color>(pixel);
        //    }

        //    return XinTupian;
        //}

        private Color[] GetColorDataFromTexture(Texture2D texture)
        {
            Color[] colors = new Color[texture.Width * texture.Height];
            texture.GetData<Color>(colors);
            return colors;
        }



    }
}

