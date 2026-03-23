using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using WorldOfTheThreeKingdoms;



namespace GameObjects.Animations
{
    [DataContract]
    public class Animation : GameObject
    {
        private bool back;
        private int frameCount;

        private int stayCount;
        private PlatformTexture texture;
        [DataMember]
        public string TextureFileName;
        [DataMember]
        public int TextureWidth;
        [DataMember]
        public int TextureHeight;

        public Rectangle GetCurrentDisplayRectangle(ref int frameIndex, ref int stayIndex, int width, int row, out bool EndLoop, bool hold)
        {
            EndLoop = false;
            if (!hold)
            {
                stayIndex++;
                if (stayIndex >= this.StayCount * Setting.Current.GlobalVariables.TroopMoveSpeed / 4)
                {
                    stayIndex = 0;
                    frameIndex++;
                    if (frameIndex >= (this.FrameCount - 1))
                    {
                        EndLoop = true;
                    }
                }
            }
            // 修复：确保宽度至少为1像素，避免绘制区域无效错误
            int safeWidth = Math.Max(1, width);
            return new Rectangle(safeWidth * frameIndex, safeWidth * row, safeWidth, safeWidth);
        }
        [DataMember]
        public bool Back
        {
            get
            {
                return this.back;
            }
            set
            {
                this.back = value;
            }
        }
        [DataMember]
        public int FrameCount
        {
            get
            {
                return this.frameCount;
            }
            set
            {
                this.frameCount = value;
            }
        }
        [DataMember]
        public int StayCount
        {
            get
            {
                return this.stayCount;
            }
            set
            {
                this.stayCount = value;
            }
        }

        //public void disposeTexture()
        //{
        //    if (this.texture != null)
        //    {
        //        this.texture.Dispose();
        //        this.texture = null;
        //    }
        //}

        public PlatformTexture Texture
        {
            get
            {
                // 🔥 关键修复：检查纹理是否已被释放
                // 日期：2026-03-13
                // 问题：战法动画纹理被 CacheManager.Clear 释放后，this.texture 仍持有已释放的引用
                // 原因：延迟加载只检查 null，不检查 IsDisposed
                // 解决：如果纹理已释放，重新加载
                // 🔥 ANTI-BAND-AID：不添加空检查，让 GetTempTexture 返回 null 时崩溃
                // 如果崩溃，说明 TextureFileName 错误或纹理文件缺失，需要追溯数据源
                if (this.texture == null || this.texture.Texture.IsDisposed)
                {
                    this.texture = CacheManager.GetTempTexture(this.TextureFileName);
                    // 🔥 直接访问，不检查 null
                    this.texture.Width = this.TextureWidth;
                    this.texture.Height = this.TextureHeight;
                }
                return this.texture;
            }
        }
    }
}

