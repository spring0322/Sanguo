using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using GameObjects.MapDetail;
using GameObjects.TroopDetail;
using GameObjects.Animations;
using GameObjects.ArchitectureDetail;
using Platforms;
using GameManager;
using Tools;

namespace WorldOfTheThreeKingdoms.Resources
{

    public class GameTextures
    {
        //public Texture2D BackgroundMap;
        public PlatformTexture[] MapVeilTextures;
        public PlatformTexture[] MouseArrowTextures;
        public PlatformTexture[] RoutewayDirectionArrowTextures;
        public PlatformTexture[] RoutewayDirectionTailTextures;
        public PlatformTexture[] RoutewayTextures;
        public PlatformTexture SelectorTexture;
        public PlatformTexture[] TerrainTextures;
        public PlatformTexture[] TileFrameTextures;
        public PlatformTexture qizitupian;
        public PlatformTexture huangditupian;
        //public Texture2D jianzhubiaotibeijing;
        public PlatformTexture zidongcundangtupian;
        public Dictionary<int, PlatformTexture> mediumCityImg = new Dictionary<int, PlatformTexture>();
        public Dictionary<int, PlatformTexture> largeCityImg = new Dictionary<int, PlatformTexture>();
        public PlatformTexture[] guandetupian = new PlatformTexture[3];
        public PlatformTexture wanggetupian;
        public PlatformTexture EditModeGrid;
        public PlatformTexture LandConnect;
        public PlatformTexture WaterConnect;
        public PlatformTexture SingleConnect;

        public void LoadTextures()
        {
            Exception exception;
            string str;
            //this.BackgroundMap = CacheManager.GetTempTexture("Content/Textures/Resources/bg.jpg");

            try
            {
                this.MouseArrowTextures = new PlatformTexture[Enum.GetValues<MouseArrowKind>().Length];
                this.MouseArrowTextures[0] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/Normal.png");
                this.MouseArrowTextures[1] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/Left.png");
                this.MouseArrowTextures[2] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/Right.png");
                this.MouseArrowTextures[3] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/Top.png");
                this.MouseArrowTextures[4] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/Bottom.png");
                this.MouseArrowTextures[5] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/TopLeft.png");
                this.MouseArrowTextures[6] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/TopRight.png");
                this.MouseArrowTextures[7] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/BottomLeft.png");
                this.MouseArrowTextures[8] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/BottomRight.png");
                this.MouseArrowTextures[9] = CacheManager.GetTempTexture("Content/Textures/Resources/MouseArrow/Selecting.png");
            }
            catch (Exception exception1)
            {
                exception = exception1;
                throw new Exception("The MouseArrow Textures are not completely loaded.\n" + exception.ToString());
            }
            try
            {
                // 添加null检查
                if (Session.Current?.Scenario?.GameCommonData?.AllTerrainDetails?.TerrainDetails == null)
                {

                    return;
                }

                foreach (TerrainDetail detail in Session.Current.Scenario.GameCommonData.AllTerrainDetails.TerrainDetails.Values)
                {
                    detail.Textures = new TerrainTextures();
                    str = "Content/Textures/Resources/Terrain/" + detail.ID.ToString() + "/";
                    //if (Directory.Exists(str))
                    detail.Textures.BasicTextures.Clear();

                    foreach (string str2 in Platform.Current.GetFiles(str, false))   // Directory.GetFiles(str))
                    {
                        //FileInfo info = new FileInfo(str2);
                        if (str2.ToLower().EndsWith(".png") && str2.Contains("Basic"))
                        {
                            var platformTexture = new PlatformTexture()
                            {
                                Name = str2
                            };
                            detail.Textures.BasicTextures.Add(platformTexture);
                        }
                        /*    
                        else if (info.Extension.Equals(".png") && info.Name.Contains("TopLeftCorner"))
                        {
                            detail.Textures.TopLeftCornerTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("TopRightCorner"))
                        {
                            detail.Textures.TopRightCornerTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("BottomLeftCorner"))
                        {
                            detail.Textures.BottomLeftCornerTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("BottomRightCorner"))
                        {
                            detail.Textures.BottomRightCornerTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("Centre"))
                        {
                            detail.Textures.CentreTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("LeftEdge"))
                        {
                            detail.Textures.LeftEdgeTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("TopEdge"))
                        {
                            detail.Textures.TopEdgeTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("RightEdge"))
                        {
                            detail.Textures.RightEdgeTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("BottomEdge"))
                        {
                            detail.Textures.BottomEdgeTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("Left"))
                        {
                            detail.Textures.LeftTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("Top"))
                        {
                            detail.Textures.TopTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("Right"))
                        {
                            detail.Textures.RightTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        else if (info.Extension.Equals(".png") && info.Name.Contains("Bottom"))
                        {
                            detail.Textures.BottomTextures.Add(Platform.Current.LoadTexture(str2));
                        }
                        */
                    }

                }
            }
            catch (Exception exception2)
            {
                exception = exception2;
                throw new Exception("The Terrain Textures are not completely loaded.\n" + exception.ToString());
            }
            //try         //加载建筑
            //{
            //    foreach (ArchitectureKind kind in scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Values)
            //    {
            //        kind.Device = device;
            //    }
            //}
            //catch (Exception exception3)
            //{
            //    exception = exception3;
            //    throw new Exception("The Architecture Textures are not completely loaded.\n" + exception.ToString());
            //}
            try
            {
                // 💡 修复：避免直接 return 导致后续的 MapVeilTextures 等其他重要纹理被跳过加载
                if (Session.Current?.Scenario?.GameCommonData?.AllMilitaryKinds?.MilitaryKinds != null)
                {
                    foreach (MilitaryKind kind2 in Session.Current.Scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Values)
                    {
                        string originalId = kind2.ID.ToString();
                        string effectiveId = originalId; // 默认使用自身 ID

                        string troopDir = $"Content/Textures/Resources/Troop/{originalId}/";
                        
                        // 假设 Platform.Current.GetMODFiles 已经处理了 null 返回的情况，安全起见加上 NullToEmptyArray()
                        var files = Platform.Current.GetMODFiles(troopDir, false).NullToEmptyArray();

                        // 💡 修复 1：放弃脆弱的 Length == 1 判断，使用 LINQ 安全查找 sameX 文件，无视隐藏文件干扰
                        var sameFile = files.FirstOrDefault(f => f.Contains("same", StringComparison.OrdinalIgnoreCase));

                        if (!string.IsNullOrEmpty(sameFile))
                        {
                            // 💡 修复 2：使用跨平台的 Path 类安全提取文件名（如 "same1"），避免 Substring 越界崩溃
                            string fileName = Path.GetFileNameWithoutExtension(sameFile);
                            effectiveId = fileName.Replace("same", "", StringComparison.OrdinalIgnoreCase);

                            // 更新贴图目录路径到被复用的目标 ID
                            troopDir = $"Content/Textures/Resources/Troop/{effectiveId}/";

                        }

                        // 💡 修复 3：统一音效路径，并跟随 effectiveId 复用逻辑！同时将反斜杠替换为跨平台安全的正杠
                        string soundDir = $"Content/Sound/Troop/{effectiveId}/";

                        // 使用 C# 字符串内插，代码更易读
                        TroopTextures textures = new TroopTextures
                        {
                            MoveTextureFileName = $"{troopDir}Move.png",
                            AttackTextureFileName = $"{troopDir}Attack.png",
                            BeAttackedTextureFileName = $"{troopDir}BeAttacked.png",
                            CastTextureFileName = $"{troopDir}Cast.png",
                            BeCastedTextureFileName = $"{troopDir}BeCasted.png"
                        };
                        kind2.Textures = textures;
                        

                        
                        // 🔥 性能优化：预加载 Move 纹理，避免首次渲染时的延迟
                        // 只预加载 Move 纹理（最常用），其他纹理保持懒加载以节省内存
                        _ = textures.MoveTexture;

                        TroopSounds sounds = new TroopSounds
                        {
                            MovingSoundPath = $"{soundDir}Moving",
                            NormalAttackSoundPath = $"{soundDir}NormalAttack",
                            CriticalAttackSoundPath = $"{soundDir}CriticalAttack"
                        };
                        kind2.Sounds = sounds;
                    }
                }
                else
                {

                }
            }
            catch (Exception exception4)
            {
                exception = exception4;
                throw new Exception("The Troop Textures are not completely loaded.\n" + exception.ToString());
            }
            try
            {
                this.MapVeilTextures = new PlatformTexture[Enum.GetValues<MapVeilKind>().Length];
                this.MapVeilTextures[0] = CacheManager.GetTempTexture("Content/Textures/Resources/MapVeil/Gray.png");
            }
            catch (Exception exception5)
            {
                exception = exception5;
                throw new Exception("The MapVeil Textures are not completely loaded.\n" + exception.ToString());
            }
            try
            {
                this.TileFrameTextures = new PlatformTexture[Enum.GetValues<TileFrameKind>().Length];
                this.TileFrameTextures[0] = CacheManager.GetTempTexture("Content/Textures/Resources/TileFrame/White.png");
                this.TileFrameTextures[1] = CacheManager.GetTempTexture("Content/Textures/Resources/TileFrame/Black.png");
                this.TileFrameTextures[2] = CacheManager.GetTempTexture("Content/Textures/Resources/TileFrame/Red.png");
                this.TileFrameTextures[3] = CacheManager.GetTempTexture("Content/Textures/Resources/TileFrame/Blue.png");
                this.TileFrameTextures[4] = CacheManager.GetTempTexture("Content/Textures/Resources/TileFrame/Green.png");
                this.TileFrameTextures[5] = CacheManager.GetTempTexture("Content/Textures/Resources/TileFrame/Purple.png");
                this.TileFrameTextures[6] = CacheManager.GetTempTexture("Content/Textures/Resources/TileFrame/Yellow.png");
            }
            catch (Exception exception6)
            {
                exception = exception6;
                throw new Exception("The TileFrame Textures are not completely loaded.\n" + exception.ToString());
            }
            try
            {
                this.RoutewayTextures = new PlatformTexture[Enum.GetValues<RoutewayState>().Length];
                this.RoutewayTextures[0] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/Planning.png");
                this.RoutewayTextures[1] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/Active.png");
                this.RoutewayTextures[2] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/Inefficiency.png");
                this.RoutewayTextures[3] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/Building.png");
                this.RoutewayTextures[4] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/NoFood.png");
                this.RoutewayTextures[5] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/Hostile.png");
                this.RoutewayDirectionArrowTextures = new PlatformTexture[Enum.GetValues<SimpleDirection>().Length];
                this.RoutewayDirectionArrowTextures[0] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/DirectionArrowNone.png");
                this.RoutewayDirectionArrowTextures[1] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/DirectionArrowLeft.png");
                this.RoutewayDirectionArrowTextures[2] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/DirectionArrowUp.png");
                this.RoutewayDirectionArrowTextures[3] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/DirectionArrowRight.png");
                this.RoutewayDirectionArrowTextures[4] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/DirectionArrowDown.png");
                this.RoutewayDirectionTailTextures = new PlatformTexture[Enum.GetValues<SimpleDirection>().Length];
                this.RoutewayDirectionTailTextures[1] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/DirectionTailLeft.png");
                this.RoutewayDirectionTailTextures[2] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/DirectionTailUp.png");
                this.RoutewayDirectionTailTextures[3] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/DirectionTailRight.png");
                this.RoutewayDirectionTailTextures[4] = CacheManager.GetTempTexture("Content/Textures/Resources/Routeway/DirectionTailDown.png");
            }
            catch (Exception exception7)
            {
                exception = exception7;
                throw new Exception("The Routeway Textures are not completely loaded.\n" + exception.ToString());
            }
            //try
            //{
                // 添加null检查
                if (Session.Current?.Scenario?.GameCommonData?.AllTileAnimations?.Animations != null)
                {
                    foreach (Animation animation in Session.Current.Scenario.GameCommonData.AllTileAnimations.Animations.Values)
                    {
                        //animation.Device = device;
                        animation.TextureFileName = "Content/Textures/Resources/Effects/TileEffect/" + animation.Name + ".png";

                        //if (!Platform.Current.FileContentExists(animation.MaleSoundPath, ""))
                        //{
                        //    animation.MaleSoundPath = "Content/Sound/Animation/" + animation.Name;
                        //}

                        //if (!Platform.Current.FileContentExists(animation.FemaleSoundPath, ""))
                        //{
                        //    animation.FemaleSoundPath = "Content/Sound/Animation/" + animation.Name;
                        //}
                    }
                }
            //}
            //catch (Exception exception8)
            //{
            //    exception = exception8;
            //    throw new Exception("The TileAnimation Textures are not completely loaded.\n" + exception.ToString());
            //}
            try
            {
                // 添加null检查
                if (Session.Current?.Scenario?.GameCommonData?.NumberGenerator != null)
                {
                    Session.Current.Scenario.GameCommonData.NumberGenerator.TextureFileName = "Content/Textures/Resources/Effects/CombatNumber/CombatNumber.png";
                }
            }
            catch (Exception exception9)
            {
                exception = exception9;
                throw new Exception("The NumberGenerator Textures are not completely loaded.\n" + exception.ToString());
            }
            try
            {
                this.SelectorTexture = CacheManager.GetTempTexture("Content/Textures/Resources/Effects/Selector/Selector.png");
            }
            catch (Exception exception10)
            {
                exception = exception10;
                throw new Exception("The NumberGenerator Textures are not completely loaded.\n" + exception.ToString());
            }

            try
            {
                this.qizitupian = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/qizi.png");
            }
            catch (Exception exception11)
            {
                exception = exception11;
                throw new Exception("The qizi Textures are not completely loaded.\n" + exception.ToString());
            }

            try
            {
                this.huangditupian = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/huangdi.png");
            }
            catch (Exception exception11)
            {
                exception = exception11;
                throw new Exception("The huangdi Textures are not completely loaded.\n" + exception.ToString());
            }

            try
            {
                this.LandConnect = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/LandConnect.png");
                this.WaterConnect = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/WaterConnect.png");
                this.SingleConnect = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/SingleConnect.png");
            }
            catch (Exception exception12)
            {
                exception = exception12;
                throw new Exception("The ArchitectureConnect Textures are not completely loaded.\n" + exception.ToString());
            }

            //this.jianzhubiaotibeijing = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/jianzhubiaotibeijing.png");

            mediumCityImg.Clear();
            largeCityImg.Clear();
            string[] filePaths = Platform.Current.GetFiles("Content/Textures/Resources/Architecture/", false).NullToEmptyList().Where(fi => fi.EndsWith(".png")).NullToEmptyArray();
            foreach (String s in filePaths)
            {
                // 🔥 技术性修复：同时处理正斜杠和反斜杠，避免在Windows上解析失败
                int lastSlash = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
                int lastDot = s.LastIndexOf('.');
                
                if (lastDot <= lastSlash)
                {
                    continue; // 跳过格式不正确的文件名
                }
                
                string fileName = s.Substring(lastSlash + 1, lastDot - lastSlash - 1);
                // System.Diagnostics.Debug.WriteLine($"[CacheManager] Extracted fileName: {fileName}");
                if (fileName.IndexOf('-') < 0)
                {
                    continue;
                }
                
                int dashIndex = fileName.IndexOf('-');
                if (dashIndex <= 0 || dashIndex >= fileName.Length - 1)
                {
                    continue; // 跳过格式不正确的文件名
                }
                
                string archIdStr = fileName.Substring(0, dashIndex);
                string size = fileName.Substring(dashIndex + 1);

                int archId;
                if (int.TryParse(archIdStr, out archId) && (size.Equals("5") || size.Equals("13")))
                {
                    if (size.Equals("5"))
                    {
                        var loadedTex = Platform.Current.LoadTexture(s, false);
                        if (loadedTex != null)
                        {
                            var pt = new PlatformTexture(loadedTex) { Name = s };
                            mediumCityImg.Add(archId, pt);
                            // System.Diagnostics.Debug.WriteLine($"[GameTextures] Loaded medium city img: {s} -> ID:{archId}");
                        }
                        else
                        {

                        }
                    }
                    else
                    {
                        var loadedTex = Platform.Current.LoadTexture(s, false);
                        if (loadedTex != null)
                        {
                            var pt = new PlatformTexture(loadedTex) { Name = s };
                            largeCityImg.Add(archId, pt);
                            // System.Diagnostics.Debug.WriteLine($"[GameTextures] Loaded large city img: {s} -> ID:{archId}");
                        }
                        else
                        {

                        }
                    }
                }
                else
                {

                }
            }

            this.guandetupian[0] = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/hengguan3.png");
            this.guandetupian[1] = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/shuguan3.png");
            this.guandetupian[2] = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/shuguan5.png");
            this.wanggetupian = CacheManager.GetTempTexture("Content/Textures/Resources/TileFrame/wangge.png");
            this.EditModeGrid = CacheManager.GetTempTexture("Content/Textures/Resources/TileFrame/Blue.png");
            this.zidongcundangtupian = CacheManager.GetTempTexture("Content/Textures/Resources/Effects/zidongcundang.png");
        }
    }

}
