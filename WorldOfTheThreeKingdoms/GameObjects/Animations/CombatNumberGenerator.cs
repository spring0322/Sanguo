using GameManager;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using System;
using System.Runtime.Serialization;
using WorldOfTheThreeKingdoms;



namespace GameObjects.Animations
{
    [DataContract]
    public class CombatNumberGenerator
    {
        [DataMember]
        public int DigitHeight = 20;
        [DataMember]
        public int DigitWidth = 12;
        private PlatformTexture texture;
        [DataMember]
        public string TextureFileName;

        public Rectangle GetCurrentArrowRectangle(CombatNumberKind kind, CombatNumberDirection direction)
        {
            // ✅ 修复：纹理行索引 = kind索引 * 2 + direction索引
            int row = (int)kind * 2 + (int)direction;
            return new Rectangle(this.DigitWidth * (10 + (int)direction), this.DigitHeight * row, this.DigitWidth, this.DigitHeight);
        }

        public Rectangle GetCurrentDigitRectangle(CombatNumberKind kind, CombatNumberDirection direction, int digit)
        {
            // ✅ 修复：纹理行索引 = kind索引 * 2 + direction索引
            int row = (int)kind * 2 + (int)direction;
            return new Rectangle(this.DigitWidth * digit, this.DigitHeight * row, this.DigitWidth, this.DigitHeight);
        }

        public PlatformTexture Texture
        {
            get
            {
                if (this.texture == null)
                {
                    this.texture = CacheManager.GetTempTexture(this.TextureFileName);
                    this.DigitWidth = 144 / 12;  // this.texture.Width / 12;
                    // ✅ AOT 修复：使用编译时常量替代反射
                    // CombatNumberKind 有 5 个枚举值：人数、士气、战意、资金、粮草
                    const int CombatNumberKindCount = 5;
                    this.DigitHeight = (200 / CombatNumberKindCount) / 2;   //this.texture.Height
                }
                return this.texture;
            }
        }
    }
}

