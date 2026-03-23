using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using PluginInterface;
using PluginInterface.BaseInterface;
using System;
//using System.Drawing;
using System.Xml;
using WorldOfTheThreeKingdoms;

namespace MarshalSectionDialogPlugin
{

    public class MarshalSectionDialogPlugin : GameObject, IMarshalSectionDialog, IBasePlugin, IPluginXML, IPluginGraphics
    {
        private string author = "clip_on";
        private const string DataPath = @"Content\Textures\GameComponents\MarshalSectionDialog\Data\";
        private string description = "编组军区对话框";
        
        private MarshalSectionDialog marshalSectionDialog = new MarshalSectionDialog();
        private const string Path = @"Content\Textures\GameComponents\MarshalSectionDialog\";
        private string pluginName = "MarshalSectionDialogPlugin";
        private string version = "1.0.0";
        private const string XMLFilename = "MarshalSectionDialogData.xml";

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (this.IsShowing)
            {
                this.marshalSectionDialog.Draw();
            }
        }

        public void Initialize(Screen screen)
        {
        }

        public void LoadDataFromXMLDocument(string filename)
        {
            XmlDocument document = new XmlDocument();
            string xml = Platform.Current.LoadText(filename);
            document.LoadXml(xml);
            XmlNode nextSibling = document.FirstChild.NextSibling;
            XmlNode node = nextSibling.ChildNodes.Item(0);
            this.marshalSectionDialog.BackgroundTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.marshalSectionDialog.BackgroundSize.X = int.Parse(node.Attributes.GetNamedItem("Width").Value);
            this.marshalSectionDialog.BackgroundSize.Y = int.Parse(node.Attributes.GetNamedItem("Height").Value);
            node = nextSibling.ChildNodes.Item(1);
            for (int i = 0; i < node.ChildNodes.Count; i += 2)
            {
                Font font;
                Microsoft.Xna.Framework.Color color;
                LabelText item = new LabelText();
                XmlNode node3 = node.ChildNodes.Item(i);
                StaticMethods.LoadFontAndColorFromXMLNode(node3, out font, out color);
                item.Label = new FreeText(font, color);
                item.Label.Position = StaticMethods.LoadRectangleFromXMLNode(node3);
                item.Label.Align = (TextAlign) Enum.Parse(typeof(TextAlign), node3.Attributes.GetNamedItem("Align").Value);
                item.Label.Text = node3.Attributes.GetNamedItem("Label").Value;
                node3 = node.ChildNodes.Item(i + 1);
                StaticMethods.LoadFontAndColorFromXMLNode(node3, out font, out color);
                item.Text = new FreeText(font, color);
                item.Text.Position = StaticMethods.LoadRectangleFromXMLNode(node3);
                item.Text.Align = (TextAlign) Enum.Parse(typeof(TextAlign), node3.Attributes.GetNamedItem("Align").Value);
                item.PropertyName = node3.Attributes.GetNamedItem("PropertyName").Value;
                this.marshalSectionDialog.LabelTexts.Add(item);
            }
            node = nextSibling.ChildNodes.Item(2);
            this.marshalSectionDialog.OKButtonTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.marshalSectionDialog.OKButtonSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("Selected").Value);
            this.marshalSectionDialog.OKButtonDisabledTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("Disabled").Value);
            this.marshalSectionDialog.OKButtonPosition = StaticMethods.LoadRectangleFromXMLNode(node);
            this.marshalSectionDialog.OKButtonDisplayTexture = this.marshalSectionDialog.OKButtonDisabledTexture;
            node = nextSibling.ChildNodes.Item(3);
            this.marshalSectionDialog.CancelButtonTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.marshalSectionDialog.CancelButtonSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("Selected").Value);
            this.marshalSectionDialog.CancelButtonDisabledTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("Disabled").Value);
            this.marshalSectionDialog.CancelButtonPosition = StaticMethods.LoadRectangleFromXMLNode(node);
            this.marshalSectionDialog.CancelButtonDisplayTexture = this.marshalSectionDialog.CancelButtonTexture;
            node = nextSibling.ChildNodes.Item(4);
            this.marshalSectionDialog.ArchitectureListButtonTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.marshalSectionDialog.ArchitectureListButtonSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("Selected").Value);
            this.marshalSectionDialog.ArchitectureListButtonPosition = StaticMethods.LoadRectangleFromXMLNode(node);
            this.marshalSectionDialog.ArchitectureListButtonDisplayTexture = this.marshalSectionDialog.ArchitectureListButtonTexture;
            
            // 初始化部队列表按钮（复用城池列表按钮的纹理，位置在其右侧）
            this.marshalSectionDialog.TroopListButtonTexture = this.marshalSectionDialog.ArchitectureListButtonTexture;
            this.marshalSectionDialog.TroopListButtonSelectedTexture = this.marshalSectionDialog.ArchitectureListButtonSelectedTexture;
            var archButtonPos = this.marshalSectionDialog.ArchitectureListButtonPosition;
            this.marshalSectionDialog.TroopListButtonPosition = new Rectangle(
                archButtonPos.X + archButtonPos.Width + 5, 
                archButtonPos.Y, 
                archButtonPos.Width, 
                archButtonPos.Height
            );
            this.marshalSectionDialog.TroopListButtonDisplayTexture = this.marshalSectionDialog.TroopListButtonTexture;
            
            node = nextSibling.ChildNodes.Item(5);
            this.marshalSectionDialog.AIDetailButtonTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.marshalSectionDialog.AIDetailButtonSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("Selected").Value);
            this.marshalSectionDialog.AIDetailButtonPosition = StaticMethods.LoadRectangleFromXMLNode(node);
            this.marshalSectionDialog.AIDetailButtonDisplayTexture = this.marshalSectionDialog.AIDetailButtonTexture;
            node = nextSibling.ChildNodes.Item(6);
            this.marshalSectionDialog.OrientationButtonTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.marshalSectionDialog.OrientationButtonSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("Selected").Value);
            this.marshalSectionDialog.OrientationButtonDisabledTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("Disabled").Value);
            this.marshalSectionDialog.OrientationButtonPosition = StaticMethods.LoadRectangleFromXMLNode(node);
            this.marshalSectionDialog.OrientationButtonDisplayTexture = this.marshalSectionDialog.OrientationButtonDisabledTexture;
            
            // 加载军团长按钮 (如果存在)
            if (nextSibling.ChildNodes.Count > 7)
            {
                node = nextSibling.ChildNodes.Item(7);
                this.marshalSectionDialog.SectionLeaderButtonTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("FileName").Value);
                this.marshalSectionDialog.SectionLeaderButtonSelectedTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\MarshalSectionDialog\Data\" + node.Attributes.GetNamedItem("Selected").Value);
                this.marshalSectionDialog.SectionLeaderButtonPosition = StaticMethods.LoadRectangleFromXMLNode(node);
            }
            else
            {
                // 回退: 使用委任目标按钮位置，向下偏移
                this.marshalSectionDialog.SectionLeaderButtonTexture = this.marshalSectionDialog.AIDetailButtonTexture;
                this.marshalSectionDialog.SectionLeaderButtonSelectedTexture = this.marshalSectionDialog.AIDetailButtonSelectedTexture;
                var pos = this.marshalSectionDialog.OrientationButtonPosition;
                this.marshalSectionDialog.SectionLeaderButtonPosition = new Rectangle(pos.X + pos.Width + 5, pos.Y, pos.Width, pos.Height);
            }
            this.marshalSectionDialog.SectionLeaderButtonDisplayTexture = this.marshalSectionDialog.SectionLeaderButtonTexture;
            
            // 初始化军团长按钮文字为“选择都督”
            if (this.marshalSectionDialog.LabelTexts.Count > 0)
            {
                var baseLabel = this.marshalSectionDialog.LabelTexts[2].Label; // 尝试使用“委任模式”标签的字体
                this.marshalSectionDialog.SectionLeaderButtonText = new FreeText(baseLabel.Builder, Color.White);
                this.marshalSectionDialog.SectionLeaderButtonText.Align = TextAlign.Middle;
                this.marshalSectionDialog.SectionLeaderButtonText.Text = "选择都督";
                this.marshalSectionDialog.SectionLeaderButtonText.Position = this.marshalSectionDialog.SectionLeaderButtonPosition;
            }

            // 初始化都督显示 (对齐第一列，与确定按钮同高，名称紫色，上下对齐)
            if (this.marshalSectionDialog.LabelTexts.Count > 0)
            {
                var refLabel = this.marshalSectionDialog.LabelTexts[0].Label;
                var okPos = this.marshalSectionDialog.OKButtonPosition;
                
                // 纵向居中于确定按钮，确保两者 Y 坐标一致以实现上下对齐
                int vOffset = okPos.Y + (okPos.Height - 35) / 2;

                // 都督标签 (红色)
                this.marshalSectionDialog.ViceroyLabel = new FreeText(new Font(refLabel.Builder.Name, refLabel.Builder.Size * 1.6f, refLabel.Builder.Style), Color.Red);
                this.marshalSectionDialog.ViceroyLabel.Align = TextAlign.Left;
                this.marshalSectionDialog.ViceroyLabel.Text = "都督";
                this.marshalSectionDialog.ViceroyLabel.Position = new Rectangle(refLabel.Position.X, vOffset, 80, 40);

                // 都督姓名 (紫色, 上下对齐使用相同 vOffset)
                this.marshalSectionDialog.ViceroyName = new FreeText(new Font(refLabel.Builder.Name, refLabel.Builder.Size * 1.4f, refLabel.Builder.Style), Color.MediumPurple);
                this.marshalSectionDialog.ViceroyName.Align = TextAlign.Left;
                this.marshalSectionDialog.ViceroyName.Text = "----";
                this.marshalSectionDialog.ViceroyName.Position = new Rectangle(refLabel.Position.X + 60, vOffset, 300, 40);
            }

        }

        public void SetFaction(object faction)
        {
            this.marshalSectionDialog.SetFaction((faction is Faction ? (Faction)faction : null));
        }

        public void SetGameFrame(IGameFrame iGameFrame)
        {
            this.marshalSectionDialog.GameFramePlugin = iGameFrame;
        }

        public void SetGraphicsDevice()
        {
            this.LoadDataFromXMLDocument(@"Content\Data\Plugins\MarshalSectionDialogData.xml");
        }

        public void SetMapPosition(ShowPosition showPosition)
        {
            this.marshalSectionDialog.SetDisplayOffset(showPosition);
        }

        public void SetScreen(Screen screen)
        {
            this.marshalSectionDialog.Initialize();
        }

        public void SetSection(object section)
        {
            this.marshalSectionDialog.SetSection(section as Section);
        }

        public void SetTabList(ITabList iTabList)
        {
            this.marshalSectionDialog.TabListPlugin = iTabList;
        }

        public void Update(GameTime gameTime)
        {
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
                return this.marshalSectionDialog.IsShowing;
            }
            set
            {
                this.marshalSectionDialog.IsShowing = value;
            }
        }

        public string PluginName
        {
            get
            {
                return this.pluginName;
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


