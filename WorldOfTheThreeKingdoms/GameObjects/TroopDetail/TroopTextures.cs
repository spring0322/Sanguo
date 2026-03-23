using GameManager;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using System;
using System.Runtime.InteropServices;
using WorldOfTheThreeKingdoms;



namespace GameObjects.TroopDetail
{

    [StructLayout(LayoutKind.Sequential)]
    public struct TroopTextures
    {
        public string MoveTextureFileName;
        private PlatformTexture moveTexture;
        public string AttackTextureFileName;
        private PlatformTexture attackTexture;
        public string BeAttackedTextureFileName;
        private PlatformTexture beAttackedTexture;
        public string CastTextureFileName;
        private PlatformTexture castTexture;
        public string BeCastedTextureFileName;
        private PlatformTexture beCastedTexture;

        public int TextureWidth
        {
            get
            {
                return 1280;  // Platform.IsMobilePlatForm ? 600 : 1280;
            }
        }

        public int TextureHeight
        {
            get
            {
                return 1024;  // Platform.IsMobilePlatForm ? 480 : 1024;
            }
        }

        public PlatformTexture MoveTexture
                {
                    get
                    {
                        if (this.moveTexture == null)
                        {
                            this.moveTexture = CacheManager.GetTempTexture(this.MoveTextureFileName);
                            
                            // 🔥 如果纹理加载失败（文件不存在/AI运行时/设备丢失），返回 null
                            // 调用方（Draw 代码）需要检查 null 并跳过绘制
                            if (this.moveTexture != null)
                            {
                                this.moveTexture.Width = TextureWidth;
                                this.moveTexture.Height = TextureHeight;
                            }
                        }
                        return this.moveTexture;  // 可能返回 null
                    }
                }

        public PlatformTexture AttackTexture
                {
                    get
                    {
                        if (this.attackTexture == null)
                        {
                            this.attackTexture = CacheManager.GetTempTexture(this.AttackTextureFileName);
                            
                            if (this.attackTexture != null)
                            {
                                this.attackTexture.Width = TextureWidth;
                                this.attackTexture.Height = TextureHeight;
                            }
                        }
                        return this.attackTexture;
                    }
                }

        public PlatformTexture BeAttackedTexture
                {
                    get
                    {
                        if (this.beAttackedTexture == null)
                        {
                            this.beAttackedTexture = CacheManager.GetTempTexture(this.BeAttackedTextureFileName);
                            
                            if (this.beAttackedTexture != null)
                            {
                                this.beAttackedTexture.Width = TextureWidth;
                                this.beAttackedTexture.Height = TextureHeight;
                            }
                        }
                        return this.beAttackedTexture;
                    }
                }

        public PlatformTexture CastTexture
                {
                    get
                    {
                        if (this.castTexture == null)
                        {
                            if (this.CastTextureFileName == this.AttackTextureFileName)
                            {
                                this.castTexture = this.AttackTexture;
                            }
                            else
                            {
                                this.castTexture = CacheManager.GetTempTexture(this.CastTextureFileName);
                            }

                            if (this.castTexture != null)
                            {
                                this.castTexture.Width = TextureWidth;
                                this.castTexture.Height = TextureHeight;
                            }
                        }
                        return this.castTexture;
                    }
                }

        public PlatformTexture BeCastedTexture
                {
                    get
                    {
                        if (this.beCastedTexture == null)
                        {
                            if (this.BeCastedTextureFileName == this.BeAttackedTextureFileName)
                            {
                                this.beCastedTexture = this.BeAttackedTexture;
                            }
                            else
                            {
                                this.beCastedTexture = CacheManager.GetTempTexture(this.BeCastedTextureFileName);
                            }

                            if (this.beCastedTexture != null)
                            {
                                this.beCastedTexture.Width = TextureWidth;
                                this.beCastedTexture.Height = TextureHeight;
                            }
                        }
                        return this.beCastedTexture;
                    }
                }

        //public void Dispose()
        //{
        //    if (this.moveTexture != null)
        //    {
        //        this.moveTexture.Dispose();
        //        this.moveTexture = null;
        //    }
        //    if (this.attackTexture != null)
        //    {
        //        this.attackTexture.Dispose();
        //        this.attackTexture = null;
        //    }
        //    if (this.beAttackedTexture != null)
        //    {
        //        this.beAttackedTexture.Dispose();
        //        this.beAttackedTexture = null;
        //    }
        //    if (this.castTexture != null)
        //    {
        //        this.castTexture.Dispose();
        //        this.castTexture = null;
        //    }
        //    if (this.beCastedTexture != null)
        //    {
        //        this.beCastedTexture.Dispose();
        //        this.beCastedTexture = null;
        //    }
        //}
    }
}

