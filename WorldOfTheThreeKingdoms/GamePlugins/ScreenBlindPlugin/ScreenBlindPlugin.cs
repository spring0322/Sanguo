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
using WorldOfTheThreeKingdoms.GameScreens;

namespace ScreenBlindPlugin
{

    public class ScreenBlindPlugin : GameObject, IScreenBlind, IBasePlugin, IPluginXML, IPluginGraphics
    {
        private string author = "clip_on";
        private const string DataPath = @"Content\Textures\GameComponents\ScreenBlind\Data\";
        private string description = "屏幕窗帘";
        
        private const string Path = @"Content\Textures\GameComponents\ScreenBlind\";
        private string pluginName = "ScreenBlindPlugin";
        private ScreenBlind screenBlind = new ScreenBlind();
        private string version = "1.0.0";
        private const string XMLFilename = "ScreenBlindData.xml";

        public bool IsShowing
        {
            get
            {
                return screenBlind.IsShowing;
            }
            set
            {
                screenBlind.IsShowing = value;
            }
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            if (this.screenBlind.IsShowing)
            {
                this.screenBlind.Draw();
            }
        }

        public void Initialize(Screen screen)
        {
        }

        public void LoadDataFromXMLDocument(string filename)
        {
            Microsoft.Xna.Framework.Color color;
            Font font;
            XmlDocument document = new XmlDocument();
            string xml = Platform.Current.LoadText(filename);
            
            // 🔥 ANTI-BAND-AID：Fail Fast - 文件不存在或内容为空时立即报错
            if (string.IsNullOrEmpty(xml))
            {
                throw new InvalidOperationException(
                    $"❌ ScreenBlindPlugin 数据文件加载失败：{filename}\n   文件不存在或内容为空。请检查文件路径是否正确。");
            }
            
            document.LoadXml(xml);
            XmlNode nextSibling = document.FirstChild.NextSibling;
            XmlNode node = nextSibling.ChildNodes.Item(0);
            this.screenBlind.BackgroundTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ScreenBlind\Data\" + node.Attributes.GetNamedItem("FileName").Value);
            this.screenBlind.BackgroundClient = StaticMethods.LoadRectangleFromXMLNode(node);
            node = nextSibling.ChildNodes.Item(1);
            this.screenBlind.SpringTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ScreenBlind\Data\" + node.Attributes.GetNamedItem("Spring").Value);
            this.screenBlind.SummerTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ScreenBlind\Data\" + node.Attributes.GetNamedItem("Summer").Value);
            this.screenBlind.AutumnTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ScreenBlind\Data\" + node.Attributes.GetNamedItem("Autumn").Value);
            this.screenBlind.WinterTexture = CacheManager.GetTempTexture(@"Content\Textures\GameComponents\ScreenBlind\Data\" + node.Attributes.GetNamedItem("Winter").Value);
            this.screenBlind.SeasonTexture = this.screenBlind.SpringTexture;
            node = nextSibling.ChildNodes.Item(2);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.DateText = new FreeText(font, color);
            this.screenBlind.DateText.Position = StaticMethods.LoadRectangleFromXMLNode(node);
            this.screenBlind.DateText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.DateText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);
            node = nextSibling.ChildNodes.Item(3);
            this.screenBlind.FactionClient = StaticMethods.LoadRectangleFromXMLNode(node);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.FactionText = new FreeText(font, color);
            this.screenBlind.FactionText.Position = StaticMethods.LoadRectangleFromXMLNode(node);
            this.screenBlind.FactionText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.FactionText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);
            node = nextSibling.ChildNodes.Item(4);
            this.screenBlind.SeasonClient = StaticMethods.LoadRectangleFromXMLNode(node);

            node = nextSibling.ChildNodes.Item(5);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.FactionTechText = new FreeText(font, color);
            var rect5 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect5.Y -= 5; // Fix alignment (Adjusted to -5)
            this.screenBlind.FactionTechText.Position = rect5;
            this.screenBlind.FactionTechText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.FactionTechText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            //阿柒:新增势力信息XML设置
            node = nextSibling.ChildNodes.Item(6);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.LeaderNameText = new FreeText(font, color);
            var rect6 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect6.Y -= 5;
            this.screenBlind.LeaderNameText.Position = rect6;
            this.screenBlind.LeaderNameText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.LeaderNameText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(7);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.PrinceNameText = new FreeText(font, color);
            var rect7 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect7.Y -= 5;
            this.screenBlind.PrinceNameText.Position = rect7;
            this.screenBlind.PrinceNameText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.PrinceNameText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(8);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.CounsellorNameText = new FreeText(font, color);
            var rect8 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect8.Y -= 5;
            this.screenBlind.CounsellorNameText.Position = rect8;
            this.screenBlind.CounsellorNameText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.CounsellorNameText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(9);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.PersonCountText = new FreeText(font, color);
            var rect9 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect9.Y -= 5;
            this.screenBlind.PersonCountText.Position = rect9;
            this.screenBlind.PersonCountText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.PersonCountText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(10);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.PopulationText = new FreeText(font, color);
            var rect10 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect10.Y -= 5;
            this.screenBlind.PopulationText.Position = rect10;
            this.screenBlind.PopulationText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.PopulationText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(11);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.ArmyText = new FreeText(font, color);
            var rect11 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect11.Y -= 5;
            this.screenBlind.ArmyText.Position = rect11;
            this.screenBlind.ArmyText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.ArmyText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(12);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.FundText = new FreeText(font, color);
            var rect12 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect12.Y -= 5;
            this.screenBlind.FundText.Position = rect12;
            this.screenBlind.FundText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.FundText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(13);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.FoodText = new FreeText(font, color);
            var rect13 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect13.Y -= 5;
            this.screenBlind.FoodText.Position = rect13;
            this.screenBlind.FoodText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.FoodText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(14);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.CapitalNameText = new FreeText(font, color);
            var rect14 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect14.Y -= 5;
            this.screenBlind.CapitalNameText.Position = rect14;
            this.screenBlind.CapitalNameText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.CapitalNameText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(15);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.guanjuezifuchuanText = new FreeText(font, color);
            var rect15 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect15.Y -= 5;
            this.screenBlind.guanjuezifuchuanText.Position = rect15;
            this.screenBlind.guanjuezifuchuanText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.guanjuezifuchuanText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(16);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.chaotinggongxianduText = new FreeText(font, color);
            var rect16 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect16.Y -= 5;
            this.screenBlind.chaotinggongxianduText.Position = rect16;
            this.screenBlind.chaotinggongxianduText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.chaotinggongxianduText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(17);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.CityCountText = new FreeText(font, color);
            var rect17 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect17.Y -= 5;
            this.screenBlind.CityCountText.Position = rect17;
            this.screenBlind.CityCountText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.CityCountText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(18);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.FiveTigerText = new FreeText(font, color);
            var rect18 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect18.Y -= 5;
            this.screenBlind.FiveTigerText.Position = rect18;
            this.screenBlind.FiveTigerText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.FiveTigerText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);

            node = nextSibling.ChildNodes.Item(19);
            StaticMethods.LoadFontAndColorFromXMLNode(node, out font, out color);
            this.screenBlind.GovernorNameText = new FreeText(font, color);
            var rect19 = StaticMethods.LoadRectangleFromXMLNode(node);
            rect19.Y -= 5;
            this.screenBlind.GovernorNameText.Position = rect19;
            this.screenBlind.GovernorNameText.Align = Enum.Parse<TextAlign>(node.Attributes.GetNamedItem("Align").Value);
            this.screenBlind.GovernorNameText.DisplayOffset = new Microsoft.Xna.Framework.Point(0, 0);
        }

        public void SetGraphicsDevice()
        {
            this.LoadDataFromXMLDocument(@"Content\Data\Plugins\ScreenBlindData.xml");
        }

        public void SetRealViewportSize(Point realViewportSize)
        {
            // 🎯 设计目标：随窗口缩放，占左上角 2/3 宽度
            // 右侧预留 1/3 空间给其他插件
            
            // 重新从 XML 加载原始尺寸（基于 1030 宽度的设计）
            this.LoadDataFromXMLDocument(@"Content\Data\Plugins\ScreenBlindData.xml");
            
            // 🔥 关键：计算缩放比例
            // 原始设计宽度：1030px
            // 目标宽度：窗口宽度的 2/3
            const float designWidthRatio = 2f / 3f;
            const float baseDesignWidth = 1030f;
            
            float targetWidth = realViewportSize.X * designWidthRatio;
            float scaleX = targetWidth / baseDesignWidth;
            
            // 局部方法：按比例缩放 Rectangle
            Rectangle ScaleRect(Rectangle rect)
            {
                return new(
                    (int)(rect.X * scaleX), 
                    rect.Y, 
                    (int)(rect.Width * scaleX), 
                    rect.Height
                );
            }
            
            // 局部方法：按比例缩放 FreeText 的位置
            // 🔥 ANTI-BAND-AID：不检查 null，如果字段未初始化应该 Fail Fast
            void ScaleFreeText(FreeText text)
            {
                text.Position = ScaleRect(text.Position);
            }

            // 🔥 修复：背景也需要缩放，保持在 (0, 0) 位置
            this.screenBlind.BackgroundClient = new(
                0, 
                0, 
                (int)targetWidth, 
                this.screenBlind.BackgroundClient.Height
            );

            // 缩放所有子控件
            this.screenBlind.SeasonClient = ScaleRect(this.screenBlind.SeasonClient);
            ScaleFreeText(this.screenBlind.DateText);
            ScaleFreeText(this.screenBlind.FactionText);
            ScaleFreeText(this.screenBlind.FactionTechText);
            ScaleFreeText(this.screenBlind.LeaderNameText);
            ScaleFreeText(this.screenBlind.PrinceNameText);
            ScaleFreeText(this.screenBlind.CounsellorNameText);
            ScaleFreeText(this.screenBlind.PersonCountText);
            ScaleFreeText(this.screenBlind.PopulationText);
            ScaleFreeText(this.screenBlind.ArmyText);
            ScaleFreeText(this.screenBlind.FundText);
            ScaleFreeText(this.screenBlind.FoodText);
            ScaleFreeText(this.screenBlind.CapitalNameText);
            ScaleFreeText(this.screenBlind.guanjuezifuchuanText);
            ScaleFreeText(this.screenBlind.chaotinggongxianduText);
            ScaleFreeText(this.screenBlind.CityCountText);
            ScaleFreeText(this.screenBlind.FiveTigerText);
            ScaleFreeText(this.screenBlind.GovernorNameText);
        }

        public void SetScreen(Screen screen)
        {
            this.screenBlind.Initialize(screen as MainGameScreen);
        }

        public void Update(GameTime gameTime)
        {
            if (this.screenBlind.IsShowing)
            {
                this.screenBlind.Update();
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

