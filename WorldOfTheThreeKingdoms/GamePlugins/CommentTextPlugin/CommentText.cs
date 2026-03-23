using GameFreeText;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace CommentTextPlugin
{

    internal class CommentText
    {
        public bool DecorateFirst;
        public bool DecorateSecond;
        public bool DecorateThird;
        private bool enableUpdate;
        public FreeText FirstText;
        public string FirstTextDecorationLeft;
        public string FirstTextDecorationRight;
        public Rectangle Position;
        public FreeText SecondText;
        public string SecondTextDecorationLeft;
        public string SecondTextDecorationRight;
        public FreeText ThirdText;
        public string ThirdTextDecorationLeft;
        public string ThirdTextDecorationRight;

        public string BuildFirstString(string text)
        {
            if (this.DecorateFirst)
            {
                this.FirstText.Text = this.FirstTextDecorationLeft + text + this.FirstTextDecorationRight;
            }
            else
            {
                this.FirstText.Text = text;
            }
            this.enableUpdate = true;
            return this.FirstText.Text;
        }

        public string BuildSecondString(string text)
        {
            if (this.DecorateSecond)
            {
                this.SecondText.Text = this.SecondTextDecorationLeft + text +"视野"+ this.SecondTextDecorationRight;
            }
            else
            {
                this.SecondText.Text = text;
            }
            this.enableUpdate = true;
            return this.SecondText.Text;
        }

        public string BuildThirdString(string text)
        {
            if (this.DecorateThird)
            {
                this.ThirdText.Text = this.ThirdTextDecorationLeft + text + this.ThirdTextDecorationRight;
            }
            else
            {
                this.ThirdText.Text = text;
            }
            this.enableUpdate = true;
            return this.ThirdText.Text;
        }

        public void Draw()
        {
            // 🔥 HOT PATH：移除所有诊断代码
            // CommentText 显示的是地形信息（峻岭、坐标等），不是游戏事件消息
            // 用户反馈的重复显示问题不在这里
            
            this.FirstText.Draw(0.05f);
            this.SecondText.Draw(0.05f);
            this.ThirdText.Draw(0.05f);
        }

        public void Update()
        {
            if (this.enableUpdate)
            {
                int x = this.Position.X + ((((this.Position.Width - this.FirstText.Width) - this.SecondText.Width) - this.ThirdText.Width) / 2);
                this.FirstText.Position = new Rectangle(x, this.Position.Y, this.Position.Width, this.Position.Height);
                this.SecondText.Position = new Rectangle(x + this.FirstText.Width, this.Position.Y, this.Position.Width, this.Position.Height);
                this.ThirdText.Position = new Rectangle((x + this.FirstText.Width) + this.SecondText.Width, this.Position.Y, this.Position.Width, this.Position.Height);
                this.enableUpdate = false;
            }
        }
    }
}

