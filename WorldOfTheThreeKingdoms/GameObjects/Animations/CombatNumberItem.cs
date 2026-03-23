using GameManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.Serialization;

namespace GameObjects.Animations
{
    [DataContract]
    public class CombatNumberItem
    {
        [DataMember]
        public CombatNumberKind Kind;

        [DataMember]
        public int Number;

        [DataMember]
        public Point Position;

        public void DrawLeft(CombatNumberGenerator generator, Point start, float scale)
        {
            int x = start.X;
            var rec = new Rectangle?(generator.GetCurrentArrowRectangle(this.Kind, CombatNumberDirection.上));
            CacheManager.Draw(generator.Texture, new Vector2((float)x, start.Y - (generator.DigitHeight * scale)), rec, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.01f);
            int number = this.Number;

            Span<int> digits = stackalloc int[10];
            int digitCount = 0;

            if (number == 0)
            {
                digits[0] = 0;
                digitCount = 1;
            }
            else
            {
                while (number > 0)
                {
                    digits[digitCount++] = number % 10;
                    number /= 10;
                }

                for (int i = 0; i < digitCount / 2; i++)
                {
                    (digits[i], digits[digitCount - 1 - i]) = (digits[digitCount - 1 - i], digits[i]);
                }
            }

            for (int i = 0; i < digitCount; i++)
            {
                x += (int)(generator.DigitWidth * scale);
                CacheManager.Draw(generator.Texture, new Vector2((float)x, start.Y - (generator.DigitHeight * scale)), new Rectangle?(generator.GetCurrentDigitRectangle(this.Kind, CombatNumberDirection.上, digits[i])), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.01f);
            }
        }

        public void DrawRight(CombatNumberGenerator generator, Point start, float scale)
        {
            int num = start.X - generator.DigitWidth;
            var rec = new Rectangle?(generator.GetCurrentArrowRectangle(this.Kind, CombatNumberDirection.下));
            CacheManager.Draw(generator.Texture, new Vector2((float)num, (float)start.Y), rec, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.01f);
            int number = this.Number;
            int renshuYanseXuhao = 0;
            float renshuFangdaBeishu = 1f;

            if (this.Kind == CombatNumberKind.人数)
            {
                if (this.Number < 1000)
                {
                    renshuYanseXuhao = 0;
                    renshuFangdaBeishu = 1.5f;
                }
                else if (this.Number >= 1000 && this.Number < 3000)
                {
                    renshuYanseXuhao = 1;
                    renshuFangdaBeishu = 1.8f;
                }
                else if (this.Number >= 3000 && this.Number < 5000)
                {
                    renshuYanseXuhao = 2;
                    renshuFangdaBeishu = 2.1f;
                }
                else
                {
                    renshuYanseXuhao = 3;
                    renshuFangdaBeishu = 2.5f;
                }
            }

            do
            {
                if (this.Kind != CombatNumberKind.人数)
                {
                    num -= (int)(generator.DigitWidth * scale);
                    var rec0 = new Rectangle?(generator.GetCurrentDigitRectangle(this.Kind, CombatNumberDirection.下, number % 10));
                    CacheManager.Draw(generator.Texture, new Vector2((float)num, (float)start.Y), rec0, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.01f);
                }
                else
                {
                    num -= (int)(generator.DigitWidth * scale * renshuFangdaBeishu);
                    var rec0 = new Rectangle?(generator.GetCurrentDigitRectangle((CombatNumberKind)renshuYanseXuhao, CombatNumberDirection.下, number % 10));
                    CacheManager.Draw(generator.Texture, new Vector2((float)num, (float)start.Y), rec0, Color.White, 0f, Vector2.Zero, renshuFangdaBeishu, SpriteEffects.None, 0.01f);
                }

                number /= 10;
            } while (number > 0);
        }
    }
}
