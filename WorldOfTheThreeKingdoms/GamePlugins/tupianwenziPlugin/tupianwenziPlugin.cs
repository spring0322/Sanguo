using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using GameObjects.PersonDetail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PluginInterface;
using PluginInterface.BaseInterface;
using System;
////using System.Drawing;
using System.Xml;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using WorldOfTheThreeKingdoms;
using Platforms;
using GameManager;
using Tools;

namespace tupianwenziPlugin
{

    public class tupianwenziPlugin : GameObject, Itupianwenzi, IBasePlugin, IPluginXML, IPluginGraphics
    {
        private string author = "clip_on";
        private const string DataPath = @"Content\Textures\GameComponents\tupianwenzi\Data\";
        private string description = "图片文字插件";
        
        private const string Path = @"Content\Textures\GameComponents\tupianwenzi\";
        public tupianwenzilei tupianwenzi = new tupianwenzilei();
        private string pluginName = "tupianwenziPlugin";
        private string version = "1.0.0";
        private const string XMLFilename = "tupianwenziData.xml";

        public void Close(Screen screen)
        {
            this.tupianwenzi.Close(screen);
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (this.tupianwenzi.IsShowing)
            {
                this.tupianwenzi.Draw();
            }
        }

        public void Initialize(Screen screen)
        {
            this.tupianwenzi.iPersonTextDialog = this;
        }

        public void LoadDataFromXMLDocument(string filename)
        {
            Microsoft.Xna.Framework.Color color;
            Font font;
            XmlDocument document = new XmlDocument();
            string xml = Platform.Current.LoadText(filename);document.LoadXml(xml);
            XmlNode nextSibling = document.FirstChild.NextSibling;
            XmlNode node = nextSibling.ChildNodes.Item(0);
            this.tupianwenzi.BackgroundTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\tupianwenzi\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.tupianwenzi.BackgroundSize.X = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            this.tupianwenzi.BackgroundSize.Y = int.Parse(node.Attributes.GetNamedItem("Height").Value);
            node = nextSibling.ChildNodes.Item(1);
            this.tupianwenzi.PortraitClient = StaticMethods.LoadRectangleFromXMLNode(node);
            node = nextSibling.ChildNodes.Item(2);
            this.tupianwenzi.ClientPosition = StaticMethods.LoadRectangleFromXMLNode(node);
            this.tupianwenzi.RichText.ClientWidth = this.tupianwenzi.ClientPosition.Width;
            this.tupianwenzi.RichText.ClientHeight = this.tupianwenzi.ClientPosition.Height;
            this.tupianwenzi.RichText.RowMargin = int.Parse(node.Attributes.GetNamedItem("RowMargin").Value);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);

            this.tupianwenzi.RichText.Builder = font;
            //this.tupianwenzi.RichText.Builder.SetFreeTextBuilder(font);

            this.tupianwenzi.RichText.DefaultColor = color;
            this.tupianwenzi.BuildingRichText.ClientWidth = this.tupianwenzi.RichText.ClientWidth;
            this.tupianwenzi.BuildingRichText.ClientHeight = this.tupianwenzi.RichText.ClientHeight;
            this.tupianwenzi.BuildingRichText.RowMargin = this.tupianwenzi.RichText.RowMargin;

            this.tupianwenzi.BuildingRichText.Builder = font;
            //this.tupianwenzi.BuildingRichText.Builder.SetFreeTextBuilder(font);

            this.tupianwenzi.BuildingRichText.DefaultColor = color;
            node = nextSibling.ChildNodes.Item(3);
            this.tupianwenzi.FirstPageButtonTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\tupianwenzi\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.tupianwenzi.FirstPageButtonSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\tupianwenzi\Data\" + node.Attributes.GetNamedItem("Selected").Value);
            this.tupianwenzi.FirstPageButtonDisabledTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\tupianwenzi\Data\" + node.Attributes.GetNamedItem("Disabled").Value);
            this.tupianwenzi.FirstPageButtonPosition = StaticMethods.LoadRectangleFromXMLNode(node);
            node = nextSibling.ChildNodes.Item(4);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.tupianwenzi.NameText = new FreeText(font, color);
            this.tupianwenzi.NameText.Position = StaticMethods.LoadRectangleFromXMLNode(node);
            this.tupianwenzi.NameText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            //node = nextSibling.ChildNodes.Item(5);
            //this.tupianwenzi.ShowingSeconds = int.Parse(node.Attributes.GetNamedItem("Time").Value);
            //this.tupianwenzi.ShowingSeconds = Session.GlobalVariables.DialogShowTime;
            this.tupianwenzi.TextTree.LoadFromXmlFile(@"Content\Data\Plugins\tupianwenziTextTree.xml");

        }

        public void SetCloseFunction(GameDelegates.VoidFunction closeFunction)
        {
            this.tupianwenzi.CloseFunction += closeFunction;
        }

        public void SetConfirmationDialog(IConfirmationDialog iConfirmationDialog, GameDelegates.VoidFunction yesFunction, GameDelegates.VoidFunction noFunction)
        {
            this.tupianwenzi.iConfirmationDialog = iConfirmationDialog;
            this.tupianwenzi.YesFunction = yesFunction;
            this.tupianwenzi.NoFunction = noFunction;
            this.tupianwenzi.HasConfirmationDialog = true;
        }

        public void SetContextMenu(IGameContextMenu iContextMenu)
        {
            this.tupianwenzi.iContextMenu = iContextMenu;
        }

        public void SetGameObjectBranch(object person, object gameObject, string branchName)
        {
            SetGameObjectBranch(person, gameObject, branchName, "", "");
        }

        public void SetGameObjectBranch(object person, object gameObject, Enum kind, string branchName)
        {
            SetGameObjectBranch(person, gameObject, kind, branchName, "", "");
        }

        public void SetGameObjectBranch(object person, object gameObject, Enum kind, string branchName, string tupian, string shengyin)
        {
            GameObject p = (GameObject) person;
            TextMessageKind k = (TextMessageKind) kind;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[SetGameObjectBranch-Enum] person类型={person?.GetType().Name}, person.ID={(person as GameObject)?.ID}, person.Name={(person as GameObject)?.Name}");
            System.Diagnostics.Debug.WriteLine($"[SetGameObjectBranch-Enum] kind={kind}, branchName={branchName}");
            #endif

            List<String> msg = Session.Current.Scenario.GameCommonData.AllTextMessages.GetTextMessage(p.ID, k);
            if (msg.Count > 0)
            {
                SetGameObjectBranch(p, null, msg[GameObject.Random(msg.Count)], tupian, shengyin);
            }
            else
            {
                SetGameObjectBranch(p, gameObject, branchName, tupian, shengyin);
            }
        }

        public void SetGameObjectBranch(object person, object gameObject, string branchName, string tupian, string shengyin ,string TryToShowString="")
        {
            string shijianshengyin;
            PlatformTexture shijiantupian;
            Microsoft.Xna.Framework.Rectangle shijiantupianjuxing;

            if (!(Session.Current.Scenario.SkyEyeSimpleNotification(gameObject as GameObject) && Session.GlobalVariables.SkyEye))
            {

                this.tupianwenzi.SetGameObjectBranch(person as GameObject, gameObject as GameObject, branchName, TryToShowString );

                if (shengyin != "")
                {
                    shijianshengyin = @"Content\Sound\Yinxiao\" + shengyin;
                }
                else
                {
                    shijianshengyin = null;

                }

                if (branchName == "chongxing")
                {
                    try
                    {
                        string[] files = Platform.Current.GetFiles(@"Content\Textures\GameComponents\tupianwenzi\Data\meinvtupian\" + tupian + "\\", false).NullToEmptyArray();
                        string suijitupianwenjianming = files[GameObject.Random(files.Length)];
                        shijiantupian = CacheManager.GetTempTexture(suijitupianwenjianming);
                    }
                    catch
                    {
                        try
                        {
                            string[] files = Platform.Current.GetFiles(@"Content\Textures\GameComponents\tupianwenzi\Data\meinvtupian\", false).NullToEmptyArray();

                            string suijitupianwenjianming = files[GameObject.Random(files.Length)];
                            shijiantupian = CacheManager.GetTempTexture(suijitupianwenjianming);
                        }
                        catch
                        {
                            shijiantupian = null;
                        }

                    }
                    // 🔧 修复：使用智能比例计算，保持图片原始比例
                    shijiantupianjuxing = CalculateImageRectangle(shijiantupian, tupian, ImageType.Beauty);

                }
                else if (branchName == "renwusiwang")
                {
                    //shijiantupian = ((this.tupianwenzi.screen.Scenario.Persons.GetGameObject(Convert.ToInt32(tupian))) as Person).Portrait ;
                    //shijiantupianjuxing = new Microsoft.Xna.Framework.Rectangle(0, 0, 240, 240);

                    //shijiantupian = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\tupianwenzi\Data\tupian\" + "renwusiwang.jpg");
                    // 🔧 修复：使用智能比例计算，保持图片原始比例
                    shijiantupianjuxing = CalculateImageRectangle(null, "renwusiwang", ImageType.PersonDeath);
                    shijiantupian = null;
                }
                else
                {
                    if (!String.IsNullOrEmpty(tupian))
                    {
                        try
                        {
                            // 尝试加载指定的图片文件
                            string imagePath = @"Content\Textures\GameComponents\tupianwenzi\Data\tupian\" + tupian;
                            
                            // 如果文件名不包含扩展名，尝试添加.jpg扩展名
                            if (!tupian.Contains("."))
                            {
                                imagePath += ".jpg";
                            }
                            
                            shijiantupian = CacheManager.GetTempTexture(imagePath);
                            
                            System.Diagnostics.Debug.WriteLine($"[tupianwenziPlugin] 成功加载图片: {imagePath}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[tupianwenziPlugin] 无法加载图片: {tupian}, 错误: {ex.Message}");
                            
                            // 如果加载失败，尝试使用默认图片或设为null
                            try
                            {
                                // 尝试使用一个通用的默认图片
                                shijiantupian = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\tupianwenzi\Data\tupian\caocao.jpg");
                                System.Diagnostics.Debug.WriteLine($"[tupianwenziPlugin] 使用默认图片: caocao.jpg");
                            }
                            catch
                            {
                                // 如果连默认图片都加载不了，就设为null
                                shijiantupian = null;
                                System.Diagnostics.Debug.WriteLine($"[tupianwenziPlugin] 无法加载任何图片，设为null");
                            }
                        }
                    }
                    else
                    {
                        shijiantupian = null;
                    }

                    // 🔧 修复：使用智能比例计算，保持图片原始比例
                    shijiantupianjuxing = CalculateImageRectangle(shijiantupian, tupian, ImageType.Event);

                }

                this.tupianwenzi.shijiantupianduilie.Enqueue(shijiantupian);
                this.tupianwenzi.juxingduilie.Enqueue(shijiantupianjuxing);
                this.tupianwenzi.shijianshengyinduilie.Enqueue(shijianshengyin);
            }

        }

        /// <summary>
        /// 🔧 修复：根据图片实际尺寸计算保持比例的显示矩形
        /// </summary>
        private Microsoft.Xna.Framework.Rectangle CalculateImageRectangle(PlatformTexture texture, string imageName, ImageType imageType)
        {
            try
            {
                if (texture != null)
                {
                    // 获取原始图片尺寸
                    int originalWidth = texture.Width;
                    int originalHeight = texture.Height;

                    // 根据图片类型设置不同的最大尺寸限制
                    int maxWidth, maxHeight;
                    switch (imageType)
                    {
                        case ImageType.Beauty:
                            maxWidth = 300;
                            maxHeight = 400;
                            break;
                        case ImageType.PersonDeath:
                            maxWidth = 250;
                            maxHeight = 250;
                            break;
                        case ImageType.Event:
                        default:
                            maxWidth = 600;
                            maxHeight = 400;
                            break;
                    }

                    // 计算保持比例的新尺寸
                    var newSize = CalculateProportionalSize(originalWidth, originalHeight, maxWidth, maxHeight);
                    
                    System.Diagnostics.Debug.WriteLine($"[tupianwenziPlugin] 图片 {imageName} 原始尺寸: {originalWidth}x{originalHeight}, 调整后: {newSize.Width}x{newSize.Height}");
                    
                    return new Microsoft.Xna.Framework.Rectangle(0, 0, newSize.Width, newSize.Height);
                }
                else
                {
                    // 如果纹理为空，返回默认矩形
                    return GetDefaultRectangle(imageType);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[tupianwenziPlugin] 计算图片比例失败: {ex.Message}");
                return GetDefaultRectangle(imageType);
            }
        }

        /// <summary>
        /// 计算保持比例的新尺寸
        /// </summary>
        private (int Width, int Height) CalculateProportionalSize(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            if (originalWidth <= 0 || originalHeight <= 0)
            {
                return (maxWidth, maxHeight);
            }

            // 计算宽高比
            float aspectRatio = (float)originalWidth / originalHeight;

            int newWidth = originalWidth;
            int newHeight = originalHeight;

            // 如果超过最大尺寸，按比例缩小
            if (originalWidth > maxWidth || originalHeight > maxHeight)
            {
                // 按高度限制计算
                if (originalHeight > maxHeight)
                {
                    newHeight = maxHeight;
                    newWidth = (int)(maxHeight * aspectRatio);
                }

                // 如果宽度仍然超限，按宽度重新计算
                if (newWidth > maxWidth)
                {
                    newWidth = maxWidth;
                    newHeight = (int)(maxWidth / aspectRatio);
                }
            }

            // 确保不小于最小尺寸
            const int MIN_SIZE = 150;
            if (newWidth < MIN_SIZE)
            {
                newWidth = MIN_SIZE;
                newHeight = (int)(MIN_SIZE / aspectRatio);
            }

            if (newHeight < MIN_SIZE)
            {
                newHeight = MIN_SIZE;
                newWidth = (int)(MIN_SIZE * aspectRatio);
            }

            return (newWidth, newHeight);
        }

        /// <summary>
        /// 获取默认矩形（当无法计算时使用）
        /// </summary>
        private Microsoft.Xna.Framework.Rectangle GetDefaultRectangle(ImageType imageType)
        {
            switch (imageType)
            {
                case ImageType.Beauty:
                    return new Microsoft.Xna.Framework.Rectangle(0, 0, 286, 400);
                case ImageType.PersonDeath:
                    return new Microsoft.Xna.Framework.Rectangle(0, 0, 240, 240);
                case ImageType.Event:
                default:
                    return new Microsoft.Xna.Framework.Rectangle(0, 0, 512, 384);
            }
        }

        /// <summary>
        /// 图片类型枚举
        /// </summary>
        private enum ImageType
        {
            Event,      // 普通事件图片
            Beauty,     // 美女图片
            PersonDeath // 人物死亡图片
        }

        public void SetGraphicsDevice()
        {
            this.LoadDataFromXMLDocument(@"Content\Data\Plugins\tupianwenziData.xml");
        }

        public void SetPosition(ShowPosition showPosition, Screen screen)
        {
            this.tupianwenzi.SetPosition(showPosition, screen);
        }

        public void SetScreen(Screen screen)
        {
            this.tupianwenzi.Initialize();
        }

        public void Update(GameTime gameTime)
        {
            if (this.tupianwenzi.IsShowing)
            {
                this.tupianwenzi.Update();
            }
        }

        public string Author
        {
            get
            {
                return this.author;
            }
        }

        public string Description
        {
            get
            {
                return this.description;
            }
        }

        public object Instance
        {
            get
            {
                return this;
            }
        }

        public bool IsShowing
        {
            get
            {
                return this.tupianwenzi.IsShowing;
            }
            set
            {
                this.tupianwenzi.IsShowing = value;
            }
        }

        public string PluginName
        {
            get
            {
                return this.pluginName;
            }
        }

        public FreeRichText RichText
        {
            get
            {
                return this.tupianwenzi.RichText;
            }
        }

        public string Version
        {
            get
            {
                return this.version;
            }
        }
    }
}

