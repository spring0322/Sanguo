using System;
using System.Collections.Generic;
//using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;  // 🔥 修复：使用完整的命名空间
using GameManager;
using GameObjects;
using GameObjects.Conditions;  // 🔥 新增：ConditionKind
using GameObjects.Events;  // 🔥 新增：ScenarioEvents
using GameObjects.ArchitectureDetail;
using WorldOfTheThreeKingdoms.GameManager;
using GameObjects.FactionDetail;
using GameObjects.PersonDetail;
using GameObjects.SectionDetail;
using GameObjects.TroopDetail;
using WorldOfTheThreeKingdoms.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PluginInterface;
using WorldOfTheThreeKingdoms.GameLogic;
using WorldOfTheThreeKingdoms.GameScreens;
using WorldOfTheThreeKingdoms.GameScreens.ScreenLayers;
using WorldOfTheThreeKingdoms.Resources;
using Platforms;
using System.Diagnostics;
using youcelanPlugin;

//using GameObjects.PersonDetail.PersonMessages;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    partial class MainGameScreen : Screen
    {
        public void Initialize()
        {
            // PROFILING
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // 🔥 诊断：记录 Session.Current.Scenario 的状态
            System.Diagnostics.Debug.WriteLine($"[MainGameScreen.Initialize] LoadScenarioInInitialization: {base.LoadScenarioInInitialization}");
            System.Diagnostics.Debug.WriteLine($"[MainGameScreen.Initialize] Session.Current.Scenario == null: {Session.Current.Scenario == null}");
            if (Session.Current.Scenario != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGameScreen.Initialize] Session.Current.Scenario.ScenarioTitle: {Session.Current.Scenario.ScenarioTitle}");
            }

            if (base.LoadScenarioInInitialization)
            {
                // 🔥 修复：避免重复加载剧本
                // Session.StartScenario 已经加载了剧本，不需要再次加载
                if (Session.Current.Scenario == null)
                {
                    System.Diagnostics.Debug.WriteLine("[MainGameScreen.Initialize] Session.Current.Scenario 为 null，开始加载剧本");
                    
                    //原ACCESS加載方式，用於將MDB轉為json
                    //this.LoadScenarioOld(base.InitializationFileName, base.InitializationFactionIDs);

                    // 🔥 性能诊断：记录场景加载耗时
                    var swLoadScenario = System.Diagnostics.Stopwatch.StartNew();
                    this.LoadScenario(base.InitializationFileName, base.InitializationFactionIDs, true, this);
                    swLoadScenario.Stop();
                    System.Diagnostics.Debug.WriteLine($"[性能诊断] LoadScenario (新游戏) 耗时: {swLoadScenario.ElapsedMilliseconds} ms");

                    if (Session.Current.Scenario == null)
                    {
                        System.Diagnostics.Debug.WriteLine("❌ 致命错误：剧本加载失败，Session.Current.Scenario 为 null");
                        throw new InvalidOperationException("剧本加载失败，无法继续初始化");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainGameScreen.Initialize] Session.Current.Scenario 已存在，跳过重复加载");
                }

                if (Setting.Current != null)
                {
                    Session.Current.Scenario.MOD = Setting.Current.MOD;
                }

                var globalVariables = Session.globalVariablesTemp;  //.globalVariablesBasic.Clone();

                var gameParameters = Session.parametersTemp;  //.parametersBasic.Clone();



                // 🔥 修复：新游戏时的设置优先级
                // 1. 剧本JSON中有GlobalVariables预设 → 使用剧本预设（剧本作者的特殊设定）
                // 2. 剧本JSON中无GlobalVariables → 使用主菜单设置（玩家选择）
                //
                // 实现方式：
                // - 如果剧本JSON中有GlobalVariables，直接使用剧本的GlobalVariables（已经从JSON加载+XML填充）
                // - 如果剧本JSON中无GlobalVariables，使用主菜单设置
                
                if (Session.Current.Scenario.IsGlobalVariablesFromJson)
                {
                    // 剧本JSON中有预设：直接使用剧本的GlobalVariables
                    // 这样可以保留剧本作者的所有特殊设定
                    globalVariables = Session.Current.Scenario.GlobalVariables;
                    

                }
                else
                {
                    // 剧本JSON中无预设：使用主菜单设置（globalVariables 已经从 globalVariablesTemp 克隆）

                }
                


                if (InitializationFactionIDs?.Count == 0 || InitializationFactionIDs == null)
                {

                    globalVariables.SkyEye = true;
                }
                else
                {

                    globalVariables.SkyEye = false;
                }

                Session.Current.Scenario.GlobalVariables = globalVariables;
                Session.Current.Scenario.Parameters = gameParameters;
                //以下修改是为了可以使剧本自带一些设置，这样可以使剧本作者能够预设一些特殊设定
                /*if (Session.Current.Scenario.GlobalVariables != null)
                {
                    System.Reflection.FieldInfo[] 非getset字段表 = typeof(GlobalVariables).GetFields((System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));
                    foreach (var v in 非getset字段表)
                    {
                        if (v.GetValue(Session.Current.Scenario.GlobalVariables) == null)
                        {
                            v.SetValue(Session.Current.Scenario.GlobalVariables, v.GetValue(globalVariables));
                        }
                    }
                }
                else
                {
                    Session.Current.Scenario.GlobalVariables = globalVariables;
                }

                if (Session.Current.Scenario.Parameters != null)
                {
                    System.Reflection.FieldInfo[] 非getset字段表 = typeof(Parameters).GetFields((System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));
                    foreach (var v in 非getset字段表)
                    {
                        if (v.GetValue(Session.Current.Scenario.Parameters) == null)
                        {
                            v.SetValue(Session.Current.Scenario.Parameters, v.GetValue(gameParameters));
                        }
                    }
                }
                else
                {
                    Session.Current.Scenario.Parameters = gameParameters;
                }*/

                // Session.Current.Scenario.GlobalVariables = globalVariables;
                // Session.Current.Scenario.Parameters = gameParameters;

                //this.mainMapLayer.jiazaibeijingtupian();
                
                // 🔥 诊断日志：调用InitializeScenarioPlayerFactions前

                
                Session.Current.Scenario.InitializeScenarioPlayerFactions(base.InitializationFactionIDs);
                
                // 🔥 诊断日志：调用InitializeScenarioPlayerFactions后


                if (Session.Current.Scenario.PlayerFactions.Count == 0)
                {
                    oldDialogShowTime = Setting.Current.GlobalVariables.DialogShowTime;
                    Setting.Current.GlobalVariables.DialogShowTime = 0;
                }
                else
                {
                    if (oldDialogShowTime >= 0)
                    {
                        Setting.Current.GlobalVariables.DialogShowTime = oldDialogShowTime;
                    }
                    else
                    {
                        //Setting.Current.GlobalVariables.DialogShowTime = Session.globalVariablesBasic.DialogShowTime;
                    }
                }

                if (Session.Current.Scenario.PlayerFactions.Count > 0)   //开始新游戏
                {
                    foreach (Faction faction in Session.Current.Scenario.PlayerFactions)
                    {
                        if (faction.FirstSection != null)
                        {
                            //faction.FirstSection.AIDetail = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(0, false, false, false, false, false)[0] as SectionAIDetail;
                            faction.FirstSection.AIDetail = Session.Current.Scenario.GameCommonData.AllSectionAIDetails.GetSectionAIDetailsByConditions(global::GameObjects.SectionOrientationKind.无, false, false, false, false, false)[0] as global::GameObjects.SectionDetail.SectionAIDetail;
                        }
                    }
                    foreach (Architecture jianzhu in Session.Current.Scenario.Architectures)
                    {
                        jianzhu.youzainan = false;
                        if (Session.Current.Scenario.IsPlayer(jianzhu.BelongedFaction))
                        {
                            jianzhu.AutoHiring = true;
                            jianzhu.AutoRewarding = true;
                        }
                    }
                    /*
                    foreach (Person wujiang in Session.Current.Scenario.Persons)
                    {
                        wujiang.huaiyun = false;
                        wujiang.faxianhuaiyun = false;
                        wujiang.huaiyuntianshu = -1;
                        wujiang.suoshurenwu = -1;
                    }*/

                    Session.Current.Scenario.CurrentPlayer = Session.Current.Scenario.PlayerFactions[0] as Faction;
                    
                    // 🔥 修复：同步设置 CurrentPlayerID
                    // 注意：不需要空检查，如果 CurrentPlayer 为 null 说明数据源有问题，应该让它崩溃
                    Session.Current.Scenario.CurrentPlayerID = Session.Current.Scenario.CurrentPlayer.ID.ToString();
                    System.Diagnostics.Debug.WriteLine($"[MGSStartLoad] 新游戏：设置 CurrentPlayer={Session.Current.Scenario.CurrentPlayer.Name}, CurrentPlayerID={Session.Current.Scenario.CurrentPlayerID}");
                }                
            }
            else  //从开始菜单读取游戏
            {
                this.LoadFileName = base.InitializationFileName;

                // 🔥 性能诊断：记录场景加载耗时
                var swLoadScenario = System.Diagnostics.Stopwatch.StartNew();
                this.LoadScenario(base.InitializationFileName, null, false, this);
                swLoadScenario.Stop();
                System.Diagnostics.Debug.WriteLine($"[性能诊断] LoadScenario (读档) 耗时: {swLoadScenario.ElapsedMilliseconds} ms");

                //this.Plugins.DateRunnerPlugin.Reset();
                //this.Plugins.GameRecordPlugin.Clear();
                //this.Plugins.GameRecordPlugin.RemoveDisableRects();
                //this.Plugins.AirViewPlugin.RemoveDisableRects();                

                //Session.Current.Scenario.EnableLoadAndSave = false;
                //string realPath = fileName.Substring(0, fileName.Length - 4) + ".mdb";
                //if (this.LoadFileName.EndsWith(".zhs"))
                //{
                //    FileEncryptor.DecryptFile(fileName, realPath, Session.GlobalVariables.cryptKey);
                //}
                //if (Session.GlobalVariables.EncryptSave)
                //{
                //    File.Delete(realPath);
                //}

                if (Session.Current.Scenario == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ 致命错误：读档后 Session.Current.Scenario 为 null");
                    throw new InvalidOperationException("读档失败，无法继续初始化");
                }
                Session.Current.Scenario.EnableLoadAndSave = true;
            }
            if ((Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop) && !Session.MainGame.loaded2)
            {
                Session.MainGame.loaded2 = true;
                //首次载入游戏界面结束后，绘制地图之前,改变窗口的位置和大小
                /*
                Session.MainGame.Window.Position = new Point(0, 0);
                Platform.SetGraphicsWidthHeight(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - 50, System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - 50);
                Platform.GraphicsApplyChanges();
                */
            }
            
            // ----------------------------------------------------
            // 🔥 性能诊断：界面初始化详细拆解
            // ----------------------------------------------------
            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            var swStep = System.Diagnostics.Stopwatch.StartNew();
            
            System.Diagnostics.Debug.WriteLine("[性能诊断] ========== 界面初始化开始 ==========");

            // ----------------------------------------------------
            // 步骤 1: UI插件初始化
            // ----------------------------------------------------
            swStep.Restart();
            if (this.Plugins != null)
            {
                this.Plugins.InitializePlugins(this);
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤1 - UI插件初始化: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 2: 建筑标题和旗帜初始化
            // ----------------------------------------------------
            swStep.Restart();
            this.chushihuajianzhubiaotiheqizi();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤2 - 建筑标题/旗帜: {swStep.ElapsedMilliseconds} ms");
            
            // ----------------------------------------------------
            // 步骤 3: 音频系统预热
            // ----------------------------------------------------
            swStep.Restart();
            if (Session.Current.SoundContent != null)
            {
               var sound = Session.Current.SoundContent.RootDirectory;
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤3 - 音频系统: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 4: 事件初始化
            // ----------------------------------------------------
            swStep.Restart();
            InitEvents();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤4 - 事件初始化: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 5: 剧本配置加载
            // ----------------------------------------------------
            swStep.Restart();
            if (base.LoadScenarioInInitialization)
            {
                Session.GlobalVariables.SaveToXml();
                Session.Parameters.SaveToXml();

                string str = @"Content\Data\Scenario\" + base.InitializationFileName + "GlobalVariables.xml";
                if (File.Exists(Environment.CurrentDirectory + "\\" + str))
                {
                    Session.Current.Scenario.GlobalVariables.InitialGlobalVariables(str);
                }
                string str2 = @"Content\Data\Scenario\" + base.InitializationFileName + "GameParameters.xml";
                if (File.Exists(Environment.CurrentDirectory + "\\" + str2))
                {
                    Session.Current.Scenario.Parameters.InitializeGameParameters(str2);
                }

                Session.Current.Scenario.AfterLoadGameScenario(this);
            }
            else
            {
                Session.Current.Scenario.AfterLoadSaveFile(this);
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤5 - 剧本配置加载: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 6: 地图图层初始化
            // ----------------------------------------------------
            swStep.Restart();
            this.mainMapLayer.Initialize();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤6 - 地图图层: {swStep.ElapsedMilliseconds} ms");
            
            // ----------------------------------------------------
            // 步骤 7: 其他图层初始化
            // ----------------------------------------------------
            swStep.Restart();
            this.architectureLayer.Initialize();
            this.mapVeilLayer.Initialize(this);
            this.selectingLayer.Initialize(this);
            this.tileAnimationLayer.Initialize();
            this.routewayLayer.Initialize();
            this.screenManager.Initialize();
            
            if (this.Textures != null)
            {
                var _ = this.Textures.qizitupian; 
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤7 - 其他图层: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 8: 部队图层初始化
            // ----------------------------------------------------
            swStep.Restart();
            this.troopLayer.Initialize();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤8 - 部队图层: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 9: 跳转到玩家势力
            // ----------------------------------------------------
            swStep.Restart();
            JumpToFaction();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤9 - 跳转势力: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 10: 显示右侧栏
            // ----------------------------------------------------
            swStep.Restart();
            if (Session.Current?.Scenario?.CurrentPlayer != null)
            {
                var currentPlayer = Session.Current.Scenario.CurrentPlayer;
                
                
                // 🔥 修复：使用 FirstSection.Architectures 直接显示第一个军区的城池
                // 日期：2026-02-16
                // 说明：右侧栏显示的是第一个军区（通常是手动控制的军区）的城池列表
                if (currentPlayer.FirstSection != null && currentPlayer.FirstSection.Architectures != null)
                {
                    
                    this.Showyoucelan(
                        UndoneWorkKind.None, 
                        FrameKind.Architecture, 
                        FrameFunction.Jump, 
                        false, true, false, false, 
                        currentPlayer.FirstSection.Architectures, 
                        null, "列表", ""  // ✅ 修改标题为"列表"
                    );
                    
                    
                    if (this.Plugins?.youcelanPlugin != null)
                    {
                        var tabListPlugin = this.Plugins.youcelanPlugin as youcelanPlugin.TabListPlugin;
                        if (tabListPlugin?.TabList is TabListInFrame tabList)
                        {
                            tabList.SetMouseEvent(this, true);
                        }
                    }
                }
                else
                {
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Initialize] ⚠️ Session.Current.Scenario.CurrentPlayer 为 null");
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤10 - 显示右侧栏: {swStep.ElapsedMilliseconds} ms");
            
            // ----------------------------------------------------
            // 步骤 11: 日期和季节设置
            // ----------------------------------------------------
            swStep.Restart();
            if (Session.Current?.Scenario?.Date != null)
            {
                Session.Current.Scenario.Date.SetSeason();
            }
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤11 - 日期设置: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 12: 四叉树和对话系统初始化
            // ----------------------------------------------------
            swStep.Restart();
            InitializeQuadtree();
            
            if (this.dialogueUI != null)
            {
                // 确保对话UI已初始化
            }
            
            // 🎯 初始化暴击图管理器
            _criticalHitImageManager = new WorldOfTheThreeKingdoms.GameObjects.Animations.CriticalHitImageManager();
            System.Diagnostics.Debug.WriteLine("[Initialize] 暴击图管理器初始化完成");
            
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤12 - 四叉树/对话系统/暴击图: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 13: 字体加载
            // ----------------------------------------------------
            swStep.Restart();
            var f1 = Session.Current.FontS;
            var f2 = Session.Current.FontT;
            var f3 = Session.Current.FontL;
            var f4 = Session.Current.FontE;
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤13 - 字体加载: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 14: 垃圾回收
            // ----------------------------------------------------
            swStep.Restart();
            GC.Collect(); 
            GC.WaitForPendingFinalizers(); 
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤14 - 垃圾回收: {swStep.ElapsedMilliseconds} ms");

            // ----------------------------------------------------
            // 步骤 15: 更新视口和UI布局
            // ----------------------------------------------------
            swStep.Restart();
            this.UpdateViewport();
            swStep.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 步骤15 - 更新视口和UI布局: {swStep.ElapsedMilliseconds} ms");

            swTotal.Stop();
            System.Diagnostics.Debug.WriteLine($"[性能诊断] ========== 界面初始化完成 ==========");
            System.Diagnostics.Debug.WriteLine($"[性能诊断] 总耗时: {swTotal.ElapsedMilliseconds} ms");
        }

        private void InitializeQuadtree()
        {
            try
            {
                // 🔥 防御性检查：确保必要对象存在
                if (Session.Current?.Scenario?.ScenarioMap == null)
                {
                    System.Diagnostics.Debug.WriteLine("[InitializeQuadtree] ⚠️ ScenarioMap 为 null，跳过四叉树初始化");
                    return;
                }
                
                // 计算地图边界
                Point mapSize = Session.Current.Scenario.ScenarioMap.MapDimensions;
                
                // 使用标准瓦片大小 (60x40 像素)
                int mapWidth = mapSize.X * 60;
                int mapHeight = mapSize.Y * 40;
                
                Rectangle mapBounds = new Rectangle(0, 0, mapWidth, mapHeight);
                _simpleQuadtree = new SimpleQuadtree(0, mapBounds);
                
                System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 简单四叉树初始化完成，边界: {mapBounds}");
                
                if (Session.Current?.Scenario?.Troops != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 部队总数: {Session.Current.Scenario.Troops.Count}");
                }
                
                // 初始化分层寻路系统
                try
                {
                    // 创建寻路管理器实例
                    _pathfindingManager = new PathfindingManager();
                    
                    // 预热寻路系统
                    _pathfindingManager.Prewarm();
                    
                    System.Diagnostics.Debug.WriteLine("[MainGameScreen] 分层寻路系统初始化完成");
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 分层寻路系统初始化失败: {ex.Message}");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainGameScreen] 四叉树初始化失败: {ex.Message}");
                _simpleQuadtree = null; // 使用回退渲染
            }
        }

        private void JumpToFaction()
        {
            if (base.LoadScenarioInInitialization)
            {
                if (Session.Current.Scenario.CurrentPlayer != null && 
                    Session.Current.Scenario.PlayerFactions.Count > 0)
                {
                    // 🔥 关键修复：设置 CurrentFaction，使 IsPlayerControlling() 返回 true
                    // 日期：2026-03-16
                    // 原因：新游戏流程中 CurrentFaction 未设置，导致无法点击建筑调出菜单
                    // 条件：CurrentFaction == CurrentPlayer 是 IsPlayerControlling() 的必要条件
                    Session.Current.Scenario.CurrentFaction = Session.Current.Scenario.CurrentPlayer;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[JumpToFaction] ✅ 设置 CurrentFaction = {Session.Current.Scenario.CurrentFaction.Name}");
                    System.Diagnostics.Debug.WriteLine($"[JumpToFaction] 验证 IsPlayerControlling() = {Session.Current.Scenario.IsPlayerControlling()}");
                    #endif
                    
                    Session.Current.Scenario.runScenarioStart(Session.Current.Scenario.CurrentPlayer.Capital, this);
                    this.JumpTo((Session.Current.Scenario.PlayerFactions[0] as Faction).Leader.Position);        //地图跳到玩家势力的首领处
                }
            }
        }

        private void chushihuajianzhubiaotiheqizi()
        {
            // 🔥 防御性检查：确保必要的对象已初始化
            if (Session.Current?.Scenario?.Architectures == null)
            {
                System.Diagnostics.Debug.WriteLine("[chushihuajianzhubiaotiheqizi] ⚠️ Architectures 为 null，跳过初始化");
                return;
            }
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[chushihuajianzhubiaotiheqizi] 开始初始化建筑标题和旗帜，共 {Session.Current.Scenario.Architectures.Count} 个建筑");
            int successCount = 0;
            int failCount = 0;
            #endif
            if (Session.Current == null || 
                Session.Current.Scenario == null || 
                Session.Current.Scenario.Architectures == null)
            {
                System.Diagnostics.Debug.WriteLine("[chushihuajianzhubiaotiheqizi] ⚠️ Session、Scenario或Architectures为null，跳过初始化");
                return;
            }
            
            // 🔥 优化：批量预加载建筑标题纹理
            // Cold Path - 初始化阶段，允许 LINQ，优先可读性
            #if DEBUG
            var swPreload = System.Diagnostics.Stopwatch.StartNew();
            #endif
            
            // 🔥 技术性修复：ArchitectureList 继承自 GameObjectList，需要使用 .GameObjects 属性访问内部 List
            List<string> captionTextures = [.. Session.Current.Scenario.Architectures.GameObjects
                .Cast<Architecture>()
                .Where(a => a.CaptionID > 0)
                .Select(a => $"Content/Textures/Resources/Architecture/Caption/{a.CaptionID}.png")
                .Distinct()];
            
            // 添加默认的 None.png
            captionTextures.Add("Content/Textures/Resources/Architecture/Caption/None.png");
            
            CacheManager.PreloadTextures(captionTextures, isTemp: true);
            
            #if DEBUG
            swPreload.Stop();
            System.Diagnostics.Debug.WriteLine($"[chushihuajianzhubiaotiheqizi] 批量预加载 {captionTextures.Count} 个唯一纹理，耗时: {swPreload.ElapsedMilliseconds} ms");
            #endif
            
            //System.Drawing.Font fontjianzhu = new System.Drawing.Font("华文中宋", 16f);
            Color colorjianzhu = new();
            colorjianzhu.PackedValue = uint.Parse("4294967040");

            //System.Drawing.Font font1 = new System.Drawing.Font("方正北魏楷书繁体", 30f);   //方正北魏楷书繁体
            //Microsoft.Xna.Framework.Color color1 = new Color(1f, 1f, 1f);

            //qizidezi = new FreeText(new System.Drawing.Font("方正北魏楷书繁体", 30f), new Color(1f, 1f, 1f));

            foreach (Architecture jianzhu in Session.Current.Scenario.Architectures)
            {
                // 🔥 防御性检查：跳过null的建筑
                if (jianzhu == null)
                {
                    continue;
                }
                
                //jianzhu.jianzhubiaoti = new FreeText(fontjianzhu, colorjianzhu);
                ///////jianzhu.jianzhubiaoti.DisplayOffset = new Point(0, -mainMapLayer.TileWidth / 2);
                //jianzhu.jianzhubiaoti.Text = jianzhu.Name;
                //jianzhu.jianzhubiaoti.Align = TextAlign.Left;
                jianzhu.jianzhuqizi = new qizi();
                //jianzhu.jianzhuqizi.qizidezi = new FreeText(font1, color1);

                try
                {
                    // 🔥 防御性检查：确保CaptionID有效
                    string captionPath = "Content/Textures/Resources/Architecture/Caption/";
                    if (jianzhu.CaptionID > 0)
                    {
                        captionPath += jianzhu.CaptionID + ".png";
                    }
                    else
                    {
                        captionPath += "None.png";
                    }
                    
                    /*
                    #if DEBUG
                    if (successCount < 3)  // 只打印前3个建筑
                    {
                        System.Diagnostics.Debug.WriteLine($"[chushihuajianzhubiaotiheqizi] 建筑 {jianzhu.Name}(ID:{jianzhu.ID}), CaptionID={jianzhu.CaptionID}, 路径={captionPath}");
                    }
                    #endif
                    */
                    
                    jianzhu.CaptionTexture = CacheManager.GetTempTexture(captionPath);
                    
                    // 🔥 防御性检查：确保CaptionTexture不为null
                    if (jianzhu.CaptionTexture != null)
                    {
                        jianzhu.CaptionTexture.Width = 120;
                        jianzhu.CaptionTexture.Height = 28;
                        #if DEBUG
                        successCount++;
                        #endif
                    }
                    else
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[chushihuajianzhubiaotiheqizi] ⚠️ 建筑 {jianzhu.Name} CaptionTexture 为 null");
                        failCount++;
                        #endif
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[chushihuajianzhubiaotiheqizi] ⚠️ 建筑 {jianzhu.Name} 加载Caption失败: {ex.Message}");
                    #if DEBUG
                    failCount++;
                    #endif
                    try
                    {
                        jianzhu.CaptionTexture = CacheManager.GetTempTexture("Content/Textures/Resources/Architecture/Caption/None.png");
                        if (jianzhu.CaptionTexture != null)
                        {
                            jianzhu.CaptionTexture.Width = 120;
                            jianzhu.CaptionTexture.Height = 28;
                        }
                    }
                    catch
                    {
                        // 如果连None.png都加载失败，就放弃
                        System.Diagnostics.Debug.WriteLine($"[chushihuajianzhubiaotiheqizi] ❌ 建筑 {jianzhu.Name} 连None.png都加载失败");
                    }
                }

                /*
                if (jianzhu.BelongedFaction != null)
                {
                    jianzhu.jianzhuqizi.qizidezi.Text = jianzhu.BelongedFaction.ToString().Substring(0, 1);
                }*/

                //this.qizidezi.Align = TextAlign.Middle;

                // jianzhu.dingdian cannot be null as it is a struct (Point)
                jianzhu.jianzhuqizi.qizipoint = new Point(jianzhu.dingdian.X, jianzhu.dingdian.Y - 1);
            }
            
            /*
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[chushihuajianzhubiaotiheqizi] ✅ 初始化完成：成功 {successCount} 个，失败 {failCount} 个");
            #endif
            */
        }

        public bool LoadAvail()
        {
            // 🔥 防御性检查
            if (Session.Current == null || Session.Current.Scenario == null)
            {
                return false;
            }
            return Session.Current.Scenario.LoadAvail();
        }

        public bool SaveAvail()
        {
            // 🔥 防御性检查
            if (Session.Current == null || Session.Current.Scenario == null)
            {
                return false;
            }
            return Session.Current.Scenario.SaveAvail();
        }

#pragma warning disable CS0108 // 'MainGameScreen.LoadContent()' hides inherited member 'Screen.LoadContent()'. Use the new keyword if hiding was intended.
        protected void LoadContent()
#pragma warning restore CS0108 // 'MainGameScreen.LoadContent()' hides inherited member 'Screen.LoadContent()'. Use the new keyword if hiding was intended.
        {
            base.LoadContent();
        }

        public override void LoadGame()   //从游戏里读取存档
        {
            this.Plugins.OptionDialogPlugin.SetStyle("SaveAndLoad");
            this.Plugins.OptionDialogPlugin.SetTitle("读取进度");
            this.Plugins.OptionDialogPlugin.Clear();

            var saves = GameScenario.LoadScenarioSaves();
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoadGame] 开始构建读档菜单");
            System.Diagnostics.Debug.WriteLine($"  存档列表数量: {saves.Count}");
            #endif
            
            for (int i = 0; i <= GameScenario.savemaxcounts; i++)
            {
                string ss = i < 10 ? "0" + i.ToString() : i.ToString();
                GameDelegates.VoidFunction voidFunction = delegate
                {
                    var sce = saves[int.Parse(ss)];

                    if (!String.IsNullOrEmpty(sce.Title))
                    {
                        mainMapLayer.StopThreads();
                        
                        // 🔥 优先尝试加载 .sav.gz 文件，如果不存在则回退到 .bin
                        string savGzPath = @"Save\Save" + sce.ID + ".sav.gz";
                        string binPath = @"Save\Save" + sce.ID + ".bin";
                        
                        string loadPath = File.Exists(savGzPath) ? savGzPath : binPath;
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[LoadGame] 加载存档: {loadPath}");
                        #endif
                        
                        Session.StartScenario(sce.Name, false, loadPath);
                    }
                };
                saves[i].ID = ss;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"  添加选项 {i}: Summary='{saves[i].Summary}'");
                #endif
                
                this.Plugins.OptionDialogPlugin.AddOption(saves[i].Summary, null, voidFunction);
            }

            this.Plugins.OptionDialogPlugin.EndAddOptions();
            this.Plugins.OptionDialogPlugin.ShowOptionDialog(ShowPosition.Center);
        }
        
        public override void ReloadScreenData()
        {
            //this.mainMapLayer.jiazaibeijingtupian();

            this.chushihuajianzhubiaotiheqizi();
            
            System.Diagnostics.Debug.WriteLine("[MGSStartLoad] ========== 读档完成，准备调用 gengxinyoucelan ==========");
            this.gengxinyoucelan();
            System.Diagnostics.Debug.WriteLine("[MGSStartLoad] ========== gengxinyoucelan 调用完成 ==========");
        }

        private void LoadGameFromPosition(string id)
        {
            var saves = GameScenario.LoadScenarioSaves();

            var sce = saves[int.Parse(id)];

            if (!String.IsNullOrEmpty(sce.Title))
            {
                mainMapLayer.StopThreads();
                
                // 🔥 优先尝试加载 .sav.gz 文件，如果不存在则回退到 .bin
                string savGzPath = @"Save\Save" + id + ".sav.gz";
                string binPath = @"Save\Save" + id + ".bin";
                
                string loadPath = File.Exists(savGzPath) ? savGzPath : binPath;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadGameFromPosition] 加载存档: {loadPath}");
                #endif
                
                Session.StartScenario(sce.Name, false, loadPath);
            }
        }

        private void LoadGameFromAutoPosition()
        {
            LoadGameFromPosition("00");
            //this.LoadFileName = "AutoSave" + this.SaveFileExtension;
            //Thread thread = new Thread(new ThreadStart(this.LoadGameFromDisk));
            //thread.Start();
            //thread.Join();
            //thread = null;
        }

        public static GameScenario LoadScenarioData(string scenarioName, bool fromScenario, MainGameScreen mainGameScreen, bool editing = false)
        {
            GameScenario scenario = null;

            Session.Current.IsWorking = true;

            //bool zip = true;

            //if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
            //{
            //    zip = false;
            //}

            // 🔥 统一使用 SerializationManager 加载剧本和存档
            // 日期：2026-03-16
            // 原因：剧本文件已转换为新格式，与存档格式一致
            WorldOfTheThreeKingdoms.Serialization.SerializationManager serializationManager = new();
            
            if (fromScenario)
            {
                // 剧本加载：直接使用 SerializationManager
                try
                {
                    System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
                    System.Diagnostics.Debug.WriteLine("║  [MGSStartLoad] 🔥 使用新的 SerializationManager.LoadScenario  ║");
                    System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
                    System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] 使用 SerializationManager 加载剧本: {scenarioName}");
                    scenario = serializationManager.LoadScenario(scenarioName);
                    
                    if (scenario != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 剧本加载成功");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ❌ 剧本加载失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] 堆栈: {ex.StackTrace}");
                    scenario = null;
                }
            }
            else
            {
                // 🔥 存档加载：支持新的 .sav.gz 格式和旧的 .bin 格式
                try
                {
                    
                    // 尝试加载 .sav.gz 文件
                    string savGzPath = scenarioName.EndsWith(".sav.gz") ? scenarioName : scenarioName.Replace(".bin", ".sav.gz");
                    
                    if (File.Exists(savGzPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] 使用 SerializationManager 加载 .sav.gz: {savGzPath}");
                        scenario = serializationManager.LoadGame(savGzPath);
                        
                        if (scenario != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ SerializationManager 加载成功");
                        }
                    }
                    // 如果 .sav.gz 不存在，尝试加载 .bin 文件
                    else if (File.Exists(scenarioName))
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] 使用 SerializationManager 加载 .bin: {scenarioName}");
                        scenario = serializationManager.LoadGame(scenarioName);
                        
                        if (scenario != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ SerializationManager 加载旧格式成功");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ⚠️ SerializationManager 加载失败:");
                    System.Diagnostics.Debug.WriteLine($"  文件路径: {scenarioName}");
                    System.Diagnostics.Debug.WriteLine($"  异常类型: {ex.GetType().FullName}");
                    System.Diagnostics.Debug.WriteLine($"  异常消息: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"  堆栈跟踪:\n{ex.StackTrace}");
                    
                    // 递归输出所有内部异常
                    var innerEx = ex.InnerException;
                    int depth = 1;
                    while (innerEx != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  内部异常 [{depth}]:");
                        System.Diagnostics.Debug.WriteLine($"    类型: {innerEx.GetType().FullName}");
                        System.Diagnostics.Debug.WriteLine($"    消息: {innerEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"    堆栈:\n{innerEx.StackTrace}");
                        innerEx = innerEx.InnerException;
                        depth++;
                    }
                    
                    // 🔥 不再回退到过时的 GameLoader
                    // GameLoader 不支持 .sav.gz 格式，会导致路径错误
                    System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ❌ 加载失败，不再尝试回退逻辑");
                    scenario = null;
                }
            }

            if (scenario == null)
            {
                // Deserialization failed completely
                Session.Current.IsWorking = false;
                return null;
            }

            Session.Current.IsWorking = false;

            // 🔥 修复：确保 LoadedFileName 使用正确的扩展名（.sav.gz）
            string loadedFileName = scenarioName;
            if (loadedFileName.EndsWith(".bin"))
            {
                loadedFileName = loadedFileName.Replace(".bin", ".sav.gz");
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] 转换 LoadedFileName: {scenarioName} → {loadedFileName}");
            }
            else if (!loadedFileName.EndsWith(".sav.gz") && !fromScenario)
            {
                // 如果是存档但没有扩展名，添加 .sav.gz
                loadedFileName += ".sav.gz";
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] 添加扩展名: {scenarioName} → {loadedFileName}");
            }
            
            scenario.LoadedFileName = loadedFileName;
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ LoadedFileName 设置为: {scenario.LoadedFileName}");

            scenario.UsingOwnCommonData = true;

            // 🔥 诊断：检查 AllEvents 状态（反序列化后）
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║  [LoadScenarioData] 检查 AllEvents 状态（反序列化后）      ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] AllEvents: {(scenario.AllEvents != null ? "存在" : "null")}");
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] AllEvents.Count: {scenario.AllEvents?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] AllEvents.GameObjects: {(scenario.AllEvents?.GameObjects != null ? "存在" : "null")}");
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] AllEvents.GameObjects.Count: {scenario.AllEvents?.GameObjects?.Count ?? 0}");
            
            if (scenario.AllEvents == null || scenario.AllEvents.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
                System.Diagnostics.Debug.WriteLine("║  ⚠️ 警告：AllEvents 为空！                                 ║");
                System.Diagnostics.Debug.WriteLine("║  可能原因：                                                ║");
                System.Diagnostics.Debug.WriteLine("║  1. 序列化时 AllEvents 就是空的                           ║");
                System.Diagnostics.Debug.WriteLine("║  2. 反序列化失败（GameObjectListConverter 问题）          ║");
                System.Diagnostics.Debug.WriteLine("║  3. JSON 格式不正确                                       ║");
                System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ AllEvents 包含 {scenario.AllEvents.Count} 个事件");
                
                // 输出前3个事件的信息
                int eventDebugCount = 0;
                foreach (GameObject gameObj in scenario.AllEvents)
                {
                    if (eventDebugCount >= 3) break;
                    
                    if (gameObj is Event e)
                    {
                        System.Diagnostics.Debug.WriteLine($"  事件 {eventDebugCount + 1}: ID={e.ID}, Name={e.Name}");
                        System.Diagnostics.Debug.WriteLine($"    PersonCondString: '{e.PersonCondString ?? "null"}'");
                        System.Diagnostics.Debug.WriteLine($"    architectureCondString: '{e.architectureCondString ?? "null"}'");
                        eventDebugCount++;
                    }
                }
            }

            if (scenario.GameCommonData == null)
            {
                scenario.GameCommonData = CommonData.Current;
                scenario.UsingOwnCommonData = false;
            }
            
            // 🔥 根本修复：无论哪个分支，都强制调用 ProcessCommonData
            // 日期：2026-02-17
            // 问题：读档后 Technique.Influences 为空，导致势力技巧效果不显示
            // 原因：scenario.GameCommonData 可能来自存档反序列化，未经过 ProcessCommonData 处理
            // 解决：强制调用 ProcessCommonData，该方法是幂等的，多次调用不会出错
            GameScenario.ProcessCommonData(scenario.GameCommonData);
            
            // 🔥 诊断：检查 ProcessCommonData 后的状态
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ProcessCommonData 完成");
            System.Diagnostics.Debug.WriteLine($"  - AllTechniques.Count: {scenario.GameCommonData.AllTechniques.Count}");
            if (scenario.GameCommonData.AllTechniques.Count > 0)
            {
                var firstTech = scenario.GameCommonData.AllTechniques.Techniques.Values.First();
                System.Diagnostics.Debug.WriteLine($"  - 第一个技巧: {firstTech.Name}");
                System.Diagnostics.Debug.WriteLine($"  - Influences.Count: {firstTech.Influences.Count}");
            }

            {
                // 所有建筑类型
                // 🔥 FIX: 修复 AllArchitectureKinds 未正确赋值的 bug
                if (scenario.GameCommonData.AllArchitectureKinds == null || 
                    scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds == null || 
                    scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ⚠️ AllArchitectureKinds 为空，从 CommonData 恢复");
                    
                    // 🔥 诊断：检查 CommonData.Current 状态
                    if (CommonData.Current == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ❌ CommonData.Current 为 null！");
                    }
                    else if (CommonData.Current.AllArchitectureKinds == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ❌ CommonData.Current.AllArchitectureKinds 为 null！");
                    }
                    else if (CommonData.Current.AllArchitectureKinds.ArchitectureKinds == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ❌ CommonData.Current.AllArchitectureKinds.ArchitectureKinds 字典为 null！");
                    }
                    else if (CommonData.Current.AllArchitectureKinds.ArchitectureKinds.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ❌ CommonData.Current.AllArchitectureKinds.ArchitectureKinds 字典为空！");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ CommonData.Current.AllArchitectureKinds 正常，包含 {CommonData.Current.AllArchitectureKinds.ArchitectureKinds.Count} 个建筑类型");
                    }
                    
                    scenario.GameCommonData.AllArchitectureKinds = CommonData.Current.AllArchitectureKinds;
                    scenario.UsingOwnCommonData = false;
                    System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 恢复 AllArchitectureKinds，包含 {scenario.GameCommonData.AllArchitectureKinds?.ArchitectureKinds?.Count ?? 0} 个建筑类型");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ AllArchitectureKinds 正常，包含 {scenario.GameCommonData.AllArchitectureKinds.ArchitectureKinds.Count} 个建筑类型");
                }
                if (scenario.GameCommonData.AllAttackDefaultKinds == null || scenario.GameCommonData.AllAttackDefaultKinds.Count == 0)
                {
                    scenario.GameCommonData.AllAttackDefaultKinds = CommonData.Current.AllAttackDefaultKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllAttackTargetKinds == null || scenario.GameCommonData.AllAttackTargetKinds.Count == 0)
                {
                    scenario.GameCommonData.AllAttackTargetKinds = CommonData.Current.AllAttackTargetKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllCastDefaultKinds == null || scenario.GameCommonData.AllCastDefaultKinds.Count == 0)
                {
                    scenario.GameCommonData.AllCastDefaultKinds = CommonData.Current.AllCastDefaultKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllCastTargetKinds == null || scenario.GameCommonData.AllCastTargetKinds.Count == 0)
                {
                    scenario.GameCommonData.AllCastTargetKinds = CommonData.Current.AllCastTargetKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllCharacterKinds == null || scenario.GameCommonData.AllCharacterKinds.Count == 0)
                {
                    scenario.GameCommonData.AllCharacterKinds = CommonData.Current.AllCharacterKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllColors == null || scenario.GameCommonData.AllColors.Count == 0)
                {
                    scenario.GameCommonData.AllColors = CommonData.Current.AllColors;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllCombatMethods == null || scenario.GameCommonData.AllCombatMethods.Count == 0)
                {
                    scenario.GameCommonData.AllCombatMethods = CommonData.Current.AllCombatMethods;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllConditionKinds == null || scenario.GameCommonData.AllConditionKinds.Count == 0)
                {
                    scenario.GameCommonData.AllConditionKinds = CommonData.Current.AllConditionKinds;
                    scenario.UsingOwnCommonData = false;
                }
                
                // 🔥 修复：读档后强制使用 CommonData.Current.AllConditions
                // 日期：2026-03-06
                // 原因：存档中的 AllConditions 的 Condition.Kind 是基类（Kind.ID=0），无法使用
                //       CommonData.Current.AllConditions 已经过 ProcessCommonData 处理，Kind 都是正确的子类
                //       Event 的条件对象从 AllConditions 引用，必须使用正确的版本
                // 注意：这不会丢失数据，因为 AllConditions 是静态的游戏规则数据，不是存档数据
                if (scenario.GameCommonData.AllConditions == null || scenario.GameCommonData.AllConditions.Count == 0)
                {
                    scenario.GameCommonData.AllConditions = CommonData.Current.AllConditions;
                    scenario.UsingOwnCommonData = false;
                    System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ✅ AllConditions 为空，从 CommonData 恢复");
                }
                else
                {
                    // 🔥 关键修复：即使 AllConditions 不为空，也要检查是否需要替换
                    // 日期：2026-03-06
                    // 原因：反序列化后 Condition.Kind 是基类 ConditionKind，但 ID 不是 0（ID 是正确的）
                    //       之前的检查条件 "Kind.GetType() == typeof(ConditionKind) && Kind.ID == 0" 太严格
                    //       导致无法检测到需要替换的情况
                    // 修复：只检查类型，不检查 ID
                    bool needsReplacement = false;
                    
                    // 使用枚举器而不是 LINQ（性能优化）
                    using (var enumerator = scenario.GameCommonData.AllConditions.Conditions.Values.GetEnumerator())
                    {
                        if (enumerator.MoveNext())
                        {
                            var firstCondition = enumerator.Current;
                            if (firstCondition?.Kind != null)
                            {
                                // 🔥 修复：只检查 Kind 是否是基类（不检查 ID）
                                // 原因：反序列化后 Kind.ID 是正确的值，不是 0
                                if (firstCondition.Kind.GetType() == typeof(ConditionKind))
                                {
                                    needsReplacement = true;
                                    System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ⚠️ AllConditions 中的 Condition.Kind 是基类，强制从 CommonData 恢复");
                                    System.Diagnostics.Debug.WriteLine($"  - 第一个条件: ID={firstCondition.ID}, Kind.ID={firstCondition.Kind.ID}, Kind类型={firstCondition.Kind.GetType().Name}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ AllConditions 正常，第一个条件 Kind 类型: {firstCondition.Kind.GetType().Name}, ID: {firstCondition.Kind.ID}");
                                }
                            }
                        }
                    }
                    
                    if (needsReplacement)
                    {
                        scenario.GameCommonData.AllConditions = CommonData.Current.AllConditions;
                        scenario.UsingOwnCommonData = false;
                        System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ✅ 已从 CommonData.Current 替换 AllConditions");
                    }
                }
                if (scenario.GameCommonData.AllFacilityKinds == null || scenario.GameCommonData.AllFacilityKinds.Count == 0)
                {
                    scenario.GameCommonData.AllFacilityKinds = CommonData.Current.AllFacilityKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.suoyouzainanzhonglei == null || scenario.GameCommonData.suoyouzainanzhonglei.Count == 0)
                {
                    scenario.GameCommonData.suoyouzainanzhonglei = CommonData.Current.suoyouzainanzhonglei;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.suoyouguanjuezhonglei == null || scenario.GameCommonData.suoyouguanjuezhonglei.Count == 0)
                {
                    scenario.GameCommonData.suoyouguanjuezhonglei = CommonData.Current.suoyouguanjuezhonglei;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllIdealTendencyKinds == null || scenario.GameCommonData.AllIdealTendencyKinds.Count == 0)
                {
                    scenario.GameCommonData.AllIdealTendencyKinds = CommonData.Current.AllIdealTendencyKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllInfluenceKinds == null || scenario.GameCommonData.AllInfluenceKinds.Count == 0)
                {
                    scenario.GameCommonData.AllInfluenceKinds = CommonData.Current.AllInfluenceKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllInfluences == null || scenario.GameCommonData.AllInfluences.Count == 0)
                {
                    scenario.GameCommonData.AllInfluences = CommonData.Current.AllInfluences;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllInformationKinds == null || scenario.GameCommonData.AllInformationKinds.Count == 0)
                {
                    scenario.GameCommonData.AllInformationKinds = CommonData.Current.AllInformationKinds;
                    scenario.UsingOwnCommonData = false;
                }
                // 🔥 技术性修复：增强 AllMilitaryKinds 初始化检查
                if (scenario.GameCommonData.AllMilitaryKinds == null || 
                    scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds == null || 
                    scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[MGSStartLoad] ⚠️ AllMilitaryKinds 需要从 CommonData 恢复");
                    
                    if (CommonData.Current?.AllMilitaryKinds != null)
                    {
                        scenario.GameCommonData.AllMilitaryKinds = CommonData.Current.AllMilitaryKinds;
                        scenario.UsingOwnCommonData = false;
                        System.Diagnostics.Debug.WriteLine("[MGSStartLoad] ✅ 从 CommonData.Current 恢复 AllMilitaryKinds");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[MGSStartLoad] ❌ CommonData.Current.AllMilitaryKinds 也为 null，创建空实例");
                        scenario.GameCommonData.AllMilitaryKinds = new MilitaryKindTable();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[MGSStartLoad] ✅ AllMilitaryKinds 正常，包含 {scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count} 个兵种");
                }
                if (scenario.GameCommonData.AllSectionAIDetails == null || scenario.GameCommonData.AllSectionAIDetails.Count == 0)
                {
                    scenario.GameCommonData.AllSectionAIDetails = CommonData.Current.AllSectionAIDetails;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllSkills == null || scenario.GameCommonData.AllSkills.Count == 0)
                {
                    scenario.GameCommonData.AllSkills = CommonData.Current.AllSkills;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllStratagems == null || scenario.GameCommonData.AllStratagems.Stratagems == null || scenario.GameCommonData.AllStratagems.Stratagems.Count == 0)
                {
                    scenario.GameCommonData.AllStratagems = CommonData.Current.AllStratagems;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllStunts == null || scenario.GameCommonData.AllStunts.Count == 0)
                {
                    scenario.GameCommonData.AllStunts = CommonData.Current.AllStunts;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTechniques == null || scenario.GameCommonData.AllTechniques.Count == 0)
                {
                    scenario.GameCommonData.AllTechniques = CommonData.Current.AllTechniques;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTerrainDetails == null || scenario.GameCommonData.AllTerrainDetails.Count == 0)
                {
                    scenario.GameCommonData.AllTerrainDetails = CommonData.Current.AllTerrainDetails;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTextMessages == null || scenario.GameCommonData.AllTextMessages.Count == 0)
                {
                    scenario.GameCommonData.AllTextMessages = CommonData.Current.AllTextMessages;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTileAnimations == null || scenario.GameCommonData.AllTileAnimations.Animations == null || scenario.GameCommonData.AllTileAnimations.Animations.Count == 0)
                {
                    scenario.GameCommonData.AllTileAnimations = CommonData.Current.AllTileAnimations;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTitles == null || scenario.GameCommonData.AllTitles.Count == 0)
                {
                    scenario.GameCommonData.AllTitles = CommonData.Current.AllTitles;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTitleKinds == null || scenario.GameCommonData.AllTitleKinds.Count == 0)
                {
                    scenario.GameCommonData.AllTitleKinds = CommonData.Current.AllTitleKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTroopAnimations == null || scenario.GameCommonData.AllTroopAnimations == null || scenario.GameCommonData.AllTroopAnimations.Animations.Count == 0)
                {
                    scenario.GameCommonData.AllTroopAnimations = CommonData.Current.AllTroopAnimations;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTroopEventEffectKinds == null || scenario.GameCommonData.AllTroopEventEffectKinds.Count == 0)
                {
                    scenario.GameCommonData.AllTroopEventEffectKinds = CommonData.Current.AllTroopEventEffectKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTroopEventEffects == null || scenario.GameCommonData.AllTroopEventEffects.Count == 0)
                {
                    scenario.GameCommonData.AllTroopEventEffects = CommonData.Current.AllTroopEventEffects;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllEventEffectKinds == null || scenario.GameCommonData.AllEventEffectKinds.Count == 0)
                {
                    scenario.GameCommonData.AllEventEffectKinds = CommonData.Current.AllEventEffectKinds;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllEventEffects == null || scenario.GameCommonData.AllEventEffects.Count == 0)
                {
                    scenario.GameCommonData.AllEventEffects = CommonData.Current.AllEventEffects;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllBiographyAdjectives == null || scenario.GameCommonData.AllBiographyAdjectives.Count == 0)
                {
                    scenario.GameCommonData.AllBiographyAdjectives = CommonData.Current.AllBiographyAdjectives;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.PersonGeneratorSetting == null)
                {
                    scenario.GameCommonData.PersonGeneratorSetting = CommonData.Current.PersonGeneratorSetting;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllPersonGeneratorTypes == null)
                {
                    scenario.GameCommonData.AllPersonGeneratorTypes = CommonData.Current.AllPersonGeneratorTypes;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTrainPolicies == null || scenario.GameCommonData.AllTrainPolicies.Count == 0)
                {
                    scenario.GameCommonData.AllTrainPolicies = CommonData.Current.AllTrainPolicies;
                    scenario.UsingOwnCommonData = false;
                }
                if (scenario.GameCommonData.AllTreasureCreationSettings == null || scenario.GameCommonData.AllTreasureCreationSettings.Count == 0)
                {
                    scenario.GameCommonData.AllTreasureCreationSettings = CommonData.Current.AllTreasureCreationSettings;
                    scenario.UsingOwnCommonData = false;
                }

            }
            
            // 🔥 NUCLEAR OPTION: Complete Initialization to Prevent ANY NullReferenceException
            // 🔥 关键修复：先检查 scenario 是否为 null，再设置 Session.Current.Scenario
            // 日期：2026-03-16
            // 原因：如果 scenario 为 null，不应该设置 Session.Current.Scenario
            //       否则会导致后续调用 LoadScenario 时误以为已经加载成功
            
            // 1. Deserialize with comprehensive error handling
            if (scenario == null)
            {
                System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ❌ Scenario deserialization failed completely");
                return null;
            }
            
            // 🔥 修复：只有在 scenario 不为 null 时才设置 Session.Current.Scenario
            Session.Current.Scenario = scenario;
            System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ✅ Session.Current.Scenario 已设置");

            // 2. Global Static Setup (CRITICAL)
            // GameScenario.Current = scenario; // This property may not exist, skip it
            if (scenario.GameTime == DateTime.MinValue.Ticks) 
                scenario.GameTime = (int)new DateTime(184, 1, 1).Ticks;

            // 3. Initialize ALL Potential Null Lists/Objects (The "Anti-Crash" Init)
            scenario.Factions ??= new FactionListWithQueue();
            scenario.Persons ??= new PersonList();
            scenario.Architectures ??= new ArchitectureList();
            scenario.Troops ??= new TroopListWithQueue();
            if (scenario.Legions == null) scenario.Legions = new LegionList();
            if (scenario.Regions == null) scenario.Regions = new RegionList();
            if (scenario.States == null) scenario.States = new StateList();
            if (scenario.Sections == null) scenario.Sections = new SectionList();

            // 4. Fix: Initialize Parameters to avoid math crashes in GameGo
            if (scenario.Parameters == null) 
            {
                scenario.Parameters = new WorldOfTheThreeKingdoms.GameGlobal.Parameters();
                System.Diagnostics.Debug.WriteLine("[LoadScenarioData] 🔧 初始化空Parameters对象");
            }
            
            // 🔥 修复：从XML文件加载Parameters配置
            try
            {
                scenario.Parameters.InitializeGameParameters("Content/Data/GameParameters.xml");
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 从GameParameters.xml加载配置，ExpandConditions包含{scenario.Parameters.ExpandConditions.Count}个条件");
                
                // 🔥 尝试加载场景特定配置（如果存在）
                if (fromScenario)
                {
                    string scenarioFileName = System.IO.Path.GetFileNameWithoutExtension(scenarioName);
                    // 从 "Content\Data\Scenario\XXX.json" 提取 "XXX"
                    if (scenarioFileName.Contains("\\"))
                    {
                        scenarioFileName = scenarioFileName.Substring(scenarioFileName.LastIndexOf("\\") + 1);
                    }
                    
                    string scenarioParams = $"Content/Data/Scenario/{scenarioFileName}GameParameters.xml";
                    if (System.IO.File.Exists(scenarioParams))
                    {
                        scenario.Parameters.InitializeGameParameters(scenarioParams);
                        System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 加载场景特定Parameters: {scenarioParams}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ❌ 加载GameParameters.xml失败: {ex.Message}");
            }

            // 5. Fix: Initialize GlobalVariables to avoid property access crashes
            // 🔥 修复：记录 JSON 中是否有 GlobalVariables（用于区分剧本预设和 XML 默认值）
            bool hasGlobalVariablesInJson = scenario.GlobalVariables != null;
            
            if (scenario.GlobalVariables == null) 
            {
                scenario.GlobalVariables = new WorldOfTheThreeKingdoms.GameGlobal.GlobalVariables();
                System.Diagnostics.Debug.WriteLine("[LoadScenarioData] 🔧 初始化空GlobalVariables对象");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ✅ 剧本包含GlobalVariables预设");
            }
            
            // 🔥 设置标记：用于后续判断是否应该使用剧本预设
            scenario.IsGlobalVariablesFromJson = hasGlobalVariablesInJson;
            
            // 🔥 注意：这里从 XML 加载的配置会被后续的主菜单设置覆盖（新游戏时）
            // 或者被存档中的完整状态覆盖（读档时）
            // 这里只是确保 GlobalVariables 对象的所有字段都有默认值
            try
            {
                scenario.GlobalVariables.InitialGlobalVariables("Content/Data/GlobalVariables.xml");
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 从GlobalVariables.xml加载默认配置");
                
                // 🔥 尝试加载场景特定配置（如果存在）
                if (fromScenario)
                {
                    string scenarioFileName = System.IO.Path.GetFileNameWithoutExtension(scenarioName);
                    // 从 "Content\Data\Scenario\XXX.json" 提取 "XXX"
                    if (scenarioFileName.Contains("\\"))
                    {
                        scenarioFileName = scenarioFileName.Substring(scenarioFileName.LastIndexOf("\\") + 1);
                    }
                    
                    string scenarioGlobalVars = $"Content/Data/Scenario/{scenarioFileName}GlobalVariables.xml";
                    if (System.IO.File.Exists(scenarioGlobalVars))
                    {
                        scenario.GlobalVariables.InitialGlobalVariables(scenarioGlobalVars);
                        System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 加载场景特定GlobalVariables: {scenarioGlobalVars}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ❌ 加载GlobalVariables.xml失败: {ex.Message}");
            }

            // 6. Build Maps for fast lookups
            var pMap = new Dictionary<int, Person>();
            var fMap = new Dictionary<int, Faction>();
            var aMap = new Dictionary<int, Architecture>();

            foreach (var p in scenario.Persons)
            {
                if (p is Person person && person.ID >= 0)
                    pMap[person.ID] = person;
            }

            foreach (var f in scenario.Factions)
            {
                if (f is Faction faction && faction.ID >= 0)
                    fMap[faction.ID] = faction;
            }

            foreach (var a in scenario.Architectures)
            {
                if (a is Architecture arch && arch.ID >= 0)
                    aMap[arch.ID] = arch;
            }

            // 8. Re-Link Objects using the robust method
            RestoreObjectReferences(scenario);
            
            // 🔥 注意：不在这里调用 RestoreObjectReferencesEnhanced
            // 因为此时 Faction.Architectures 列表还是空的（在 ProcessScenarioData 中才会填充）
            // RestoreObjectReferencesEnhanced 应该在 ProcessScenarioData 之后调用

            // 11. Force CurrentPlayer (Final Safety)
            if (scenario.CurrentPlayer == null && scenario.Factions.Count > 0)
            {
                scenario.CurrentPlayer = scenario.Factions[0] as Faction;
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] 🔧 强制设置当前玩家: {scenario.CurrentPlayer?.Name}");
            }

            System.Diagnostics.Debug.WriteLine("[LoadScenarioData] ✅ Nuclear Option 完整初始化完成!");

            // 🔥 最终修复：强制重新加载配置文件（确保配置不被存档数据覆盖）
            System.Diagnostics.Debug.WriteLine("[LoadScenarioData] 🔥 强制重新加载配置文件...");
            
            try
            {
                // 强制重新加载 Parameters
                scenario.Parameters.InitializeGameParameters("Content/Data/GameParameters.xml");
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 强制加载 Parameters，ExpandConditions 数量: {scenario.Parameters.ExpandConditions.Count}");
                
                // 场景特定配置（如果存在）
                if (fromScenario)
                {
                    string scenarioFileName = System.IO.Path.GetFileNameWithoutExtension(scenarioName);
                    if (scenarioFileName.Contains("\\"))
                    {
                        scenarioFileName = scenarioFileName.Substring(scenarioFileName.LastIndexOf("\\") + 1);
                    }
                    
                    string scenarioParams = $"Content/Data/Scenario/{scenarioFileName}GameParameters.xml";
                    if (System.IO.File.Exists(scenarioParams))
                    {
                        scenario.Parameters.InitializeGameParameters(scenarioParams);
                        System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 强制加载场景特定 Parameters");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ❌ 强制加载 Parameters 失败: {ex.Message}");
            }
            
            try
            {
                // 强制重新加载 GlobalVariables（确保所有字段都有值）
                scenario.GlobalVariables.InitialGlobalVariables("Content/Data/GlobalVariables.xml");
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 强制加载 GlobalVariables");
                
                // 场景特定配置（如果存在）
                if (fromScenario)
                {
                    string scenarioFileName = System.IO.Path.GetFileNameWithoutExtension(scenarioName);
                    if (scenarioFileName.Contains("\\"))
                    {
                        scenarioFileName = scenarioFileName.Substring(scenarioFileName.LastIndexOf("\\") + 1);
                    }
                    
                    string scenarioGlobalVars = $"Content/Data/Scenario/{scenarioFileName}GlobalVariables.xml";
                    if (System.IO.File.Exists(scenarioGlobalVars))
                    {
                        scenario.GlobalVariables.InitialGlobalVariables(scenarioGlobalVars);
                        System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ✅ 强制加载场景特定 GlobalVariables");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ❌ 强制加载 GlobalVariables 失败: {ex.Message}");
            }

            // 🔥 诊断：检查配置加载状态
            #if DEBUG
            try
            {
                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.WriteLine("🔍 开始诊断配置加载状态...");
                System.Diagnostics.Debug.WriteLine($"  - Parameters.ExpandConditions.Count: {scenario.Parameters?.ExpandConditions?.Count ?? -1}");
                
                if (scenario.Parameters?.ExpandConditions != null && scenario.Parameters.ExpandConditions.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  - ExpandConditions 内容: {string.Join(", ", scenario.Parameters.ExpandConditions)}");
                }
                
                // 检查 Faction.Capital
                int nullCapitalCount = 0;
                foreach (var factionObj in scenario.Factions)
                {
                    if (factionObj is Faction faction && faction.Capital == null)
                    {
                        nullCapitalCount++;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"  - Faction.Capital 为 null 的数量: {nullCapitalCount}");
                System.Diagnostics.Debug.WriteLine("");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] ⚠️ 诊断失败: {ex.Message}");
            }
            #endif

            // 初始化势力版图管理器
            if (Session.Current.TerritoryManager == null)
            {
                Session.Current.TerritoryManager = new TerritoryManager(
                    scenario.ScenarioMap.MapDimensions.X,
                    scenario.ScenarioMap.MapDimensions.Y
                );
            }

            // 🔥 关键调试：确认是否到达这里
            // 日期：2026-03-16
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");

            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] scenario: {(scenario != null ? "存在" : "null")}");
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] fromScenario: {fromScenario}");
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] editing: {editing}");
            System.Diagnostics.Debug.WriteLine($"[LoadScenarioData] scenario.Architectures.Count: {scenario?.Architectures?.Count ?? -1}");
            
            scenario.ProcessScenarioData(fromScenario, editing);
            


            // 🔥 在 ProcessScenarioData 之后修复 Faction.Capital
            // 此时 Faction.Architectures 列表已经填充完毕
            RestoreObjectReferencesEnhanced(scenario);

            return scenario;
        }



        public void LoadScenario(string filename, List<int> playerFactions, bool fromScenario, MainGameScreen mainGameScreen)
        {
            // 🔥 2026-03-17 修复：移除提前返回逻辑
            // 原因：提前返回会导致 ProcessScenarioData 不被调用，蜜月期初始化被跳过
            // ProcessScenarioData 是幂等的，可以多次调用，不会导致数据错误
            // 如果调用方担心重复加载，应该在调用前检查 Session.Current.Scenario 是否为 null
            
            System.Diagnostics.Debug.WriteLine($"[LoadScenario] 开始加载剧本: {filename}, fromScenario={fromScenario}");
            
            List<string> errorMsg = [];  // ✅ C# 12 集合表达式

            // 🔥 防止死循环：添加超时机制
            int waitCount = 0;
            while (CommonData.CurrentReady == false)
            {
                if (++waitCount > 100) // 10秒超时
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ CommonData 初始化超时！");
                    throw new TimeoutException("CommonData 初始化超时，无法加载剧本");
                }
                Platform.Sleep(100);
            }

            string scenarioName = "";

            if (fromScenario)
            {
                scenarioName = String.Format(@"Content\Data\Scenario\{0}.json", filename);
            }
            else
            {
                if (!Platform.Current.UserDirectoryExist("Save"))
                {
                    Platform.Current.UserDirectoryCreate("Save");
                }

                scenarioName = String.Format(@"Save\{0}.bin", filename);
            }

            LoadScenarioData(scenarioName, fromScenario, mainGameScreen, false);

            var scenario = Session.Current.Scenario;
            if (scenario == null) return;

            if (fromScenario)
            {
                scenario.PlayerList = playerFactions ?? new List<int>();
            }

            if (scenario != null && String.IsNullOrEmpty(scenario.CurrentPlayerID) && scenario.PlayerList != null && scenario.PlayerList.Count > 0)
            {
                scenario.CurrentPlayerID = scenario.PlayerList[0].ToString();
            }

            #if DEBUG
            // 🔥 诊断：检查 PlayerList 是否正确加载
            System.Diagnostics.Debug.WriteLine($"[LoadScenario] ========== PlayerList 诊断开始 ==========");
            System.Diagnostics.Debug.WriteLine($"[LoadScenario] PlayerList == null: {scenario.PlayerList == null}");
            if (scenario.PlayerList != null)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadScenario] PlayerList.Count: {scenario.PlayerList.Count}");
                System.Diagnostics.Debug.WriteLine($"[LoadScenario] PlayerList 内容: [{string.Join(", ", scenario.PlayerList)}]");
            }
            System.Diagnostics.Debug.WriteLine($"[LoadScenario] CurrentPlayerID: '{scenario.CurrentPlayerID ?? "null"}'");
            System.Diagnostics.Debug.WriteLine($"[LoadScenario] fromScenario: {fromScenario}");
            #endif
            
            if (scenario.PlayerList != null && scenario.PlayerList.Count > 0)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadScenario] 开始恢复 PlayerFactions，PlayerList.Count={scenario.PlayerList.Count}");
                #endif
                
                foreach (int i in scenario.PlayerList)
                {
                    var faction = scenario.Factions.GetGameObject(i);
                    scenario.PlayerFactions.Add(faction);
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[LoadScenario] 添加玩家势力: ID={i}, Name={faction?.Name ?? "null"}");
                    #endif
                }
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadScenario] PlayerFactions 初始化完成，共 {scenario.PlayerFactions.Count} 个玩家势力");
                #endif
            }
            else
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadScenario] ⚠️ PlayerList 为空或null，尝试从 CurrentPlayerID 恢复");
                #endif
                
                // 🔥 根本修复：如果 PlayerList 为空，尝试从 CurrentPlayerID 恢复
                if (!String.IsNullOrEmpty(scenario.CurrentPlayerID))
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[LoadScenario] CurrentPlayerID='{scenario.CurrentPlayerID}'，尝试解析...");
                    #endif
                    
                    if (int.TryParse(scenario.CurrentPlayerID, out int playerId))
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[LoadScenario] 解析成功，playerId={playerId}，查找势力...");
                        #endif
                        
                        var faction = scenario.Factions.GetGameObject(playerId);
                        if (faction != null)
                        {
                            scenario.PlayerFactions.Add(faction);
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[LoadScenario] ✅ 从 CurrentPlayerID 恢复玩家势力: ID={playerId}, Name={faction.Name}");
                            #endif
                        }
                        else
                        {
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[LoadScenario] ❌ 找不到 ID={playerId} 的势力！");
                            #endif
                        }
                    }
                    else
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[LoadScenario] ❌ CurrentPlayerID 解析失败！");
                        #endif
                    }
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[LoadScenario] ❌ CurrentPlayerID 为空或null！");
                    #endif
                }
            }
            
            if (!String.IsNullOrEmpty(scenario.CurrentPlayerID))
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadScenario] 设置 CurrentPlayer，CurrentPlayerID='{scenario.CurrentPlayerID}'");
                #endif
                
                scenario.CurrentPlayer = scenario.Factions.GetGameObject(int.Parse(scenario.CurrentPlayerID)) as Faction;
                scenario.CurrentFaction = scenario.CurrentPlayer;
                scenario.Factions.RunningFaction = scenario.CurrentPlayer;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadScenario] CurrentPlayer 设置为: {scenario.CurrentPlayer?.Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"[LoadScenario] IsPlayer(CurrentPlayer): {scenario.IsPlayer(scenario.CurrentPlayer)}");
                #endif
            }
            else
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoadScenario] ⚠️ CurrentPlayerID 为空，无法设置 CurrentPlayer");
                #endif
            }
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoadScenario] ========== PlayerList 诊断结束 ==========");
            System.Diagnostics.Debug.WriteLine($"[LoadScenario] 最终状态:");
            System.Diagnostics.Debug.WriteLine($"  - PlayerFactions.Count: {scenario.PlayerFactions.Count}");
            System.Diagnostics.Debug.WriteLine($"  - CurrentPlayer: {scenario.CurrentPlayer?.Name ?? "null"}");
            #endif

                
                // 🔥 修复：确保所有玩家势力获得正确的控制权状态
                if (scenario.PlayerFactions != null && scenario.PlayerFactions.Count > 0)
                {
                    foreach (Faction playerFaction in scenario.PlayerFactions)
                    {
                        if (playerFaction != null)
                        {
                            // 重置AI状态（所有玩家势力都不应该由AI控制）
                            playerFaction.AIFinished = false;
                            playerFaction.Passed = false;
                            
                            // 如果是当前玩家，设置控制权
                            if (playerFaction == scenario.CurrentPlayer)
                            {
                                playerFaction.Controlling = true;
                                playerFaction.StopToControl = true;
                                #if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[LoadScenario] ✅ 设置当前玩家势力 {playerFaction.Name} 获得控制权 (Controlling=true, StopToControl=true)");
                                #endif
                            }
                            else
                            {
                                // 其他玩家势力暂时不控制（等待轮到他们）
                                playerFaction.Controlling = false;
                                playerFaction.StopToControl = false;
                                #if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[LoadScenario] 设置玩家势力 {playerFaction.Name} 为待控制状态 (Controlling=false)");
                                #endif
                            }
                        }
                    }
                }
                
                // 🔥 额外检查：确保 CurrentFaction 和 RunningFaction 正确设置
                if (scenario.CurrentPlayer != null)
                {
                    scenario.CurrentFaction = scenario.CurrentPlayer;
                    scenario.Factions.RunningFaction = scenario.CurrentPlayer;
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[LoadScenario] ✅ CurrentFaction 和 RunningFaction 设置为: {scenario.CurrentPlayer.Name}");
                    System.Diagnostics.Debug.WriteLine($"[LoadScenario] 最终状态检查:");
                    System.Diagnostics.Debug.WriteLine($"  - CurrentPlayer.Controlling: {scenario.CurrentPlayer.Controlling}");
                    System.Diagnostics.Debug.WriteLine($"  - CurrentPlayer.Passed: {scenario.CurrentPlayer.Passed}");
                    System.Diagnostics.Debug.WriteLine($"  - CurrentPlayer.AIFinished: {scenario.CurrentPlayer.AIFinished}");
                    System.Diagnostics.Debug.WriteLine($"  - CurrentPlayer.StopToControl: {scenario.CurrentPlayer.StopToControl}");
                    System.Diagnostics.Debug.WriteLine($"  - IsPlayer(CurrentPlayer): {scenario.IsPlayer(scenario.CurrentPlayer)}");
                    #endif
                }

            if (scenario.PlayerList != null && scenario.PlayerList.Count == 0)
            {
                Session.Current.Scenario.ForceOptionsOnAutoplay();
            }

            //this.Clear();
            //this.Factions.LoadQueueFromString(reader["FactionQueue"].ToString()); 
        }
        
        public void InitEvents()
        {
            // 🔥 新事件系统：订阅 ScenarioEvents（AOT 兼容）
            ScenarioEvents.OnDayStarting += Scenario_OnDayStarting;
            ScenarioEvents.OnDayPassed += Scenario_OnDayPassed;
            ScenarioEvents.OnMonthStarting += Scenario_OnMonthStarting;
            ScenarioEvents.OnMonthPassed += Scenario_OnMonthPassed;
            ScenarioEvents.OnSeasonChanged += Scenario_OnSeasonChanged;
            ScenarioEvents.OnSeasonPassed += Scenario_OnSeasonPassed;
            ScenarioEvents.OnYearStarting += Scenario_OnYearStarting;
            ScenarioEvents.OnYearPassed += Scenario_OnYearPassed;
            ScenarioEvents.OnAfterScenarioLoaded += Scenario_OnAfterScenarioLoaded;
            ScenarioEvents.OnNewFactionCreated += Scenario_OnNewFactionCreated;
        }

        /// <summary>
        /// 剧本加载后处理完成事件处理器（新事件系统）
        /// </summary>
        private void Scenario_OnAfterScenarioLoaded(GameScenario scenario)
        {
            // 🔥 数据完整性：scenario 和 scenario.Date 必须存在
            // 如果为 null 说明游戏状态异常，应该崩溃而不是静默跳过
            scenario.Date.IsRunning = false;

            
            this.Textures.LoadTextures();

            // 🔥 修复：清空所有 Military.kind 缓存，强制重新从 AllMilitaryKinds 获取
            // 原因：反序列化后 Military.kind 可能缓存了旧的 MilitaryKind 对象引用
            // LoadTextures() 只更新 AllMilitaryKinds 中的对象，不会更新已缓存的引用
            // 日期：2026-02-25
            foreach (Military military in Session.Current.Scenario.Militaries.GetList())
            {
                military.ClearKindCache();
            }


            base.DefaultMouseArrowTexture = this.Textures.MouseArrowTextures[0];

            // 🔥 修复：读档后重新加载建筑标题和旗帜纹理

            this.chushihuajianzhubiaotiheqizi();

            // 🔥 修复：预加载所有兵种的 Move 纹理，避免首次渲染时的延迟
            // 必须在 chushihuajianzhubiaotiheqizi() 之后执行，避免被建筑纹理预加载阻塞
            // 日期：2026-02-25
            int preloadCount = 0;
            foreach (MilitaryKind kind in Session.Current.Scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Values)
            {
                var texture = kind.Textures.MoveTexture;
                if (texture != null)
                {
                    preloadCount++;
                }
            }
            System.Diagnostics.Debug.WriteLine($"[Scenario_OnAfterLoadScenario] 已预加载 {preloadCount} 个兵种的 Move 纹理");

            // 🎨 初始化水墨渲染器（2026-03-14）
            // 必须在 Scenario 加载完成后初始化
            // 🔥 逐步排查：测试创建渲染器（2026-03-14）
            try
            {
                System.Diagnostics.Debug.WriteLine("[水墨渲染器] 步骤1：开始加载纹理...");
                var noiseTexture = Session.Current.Content.Load<Texture2D>("XuanPaperNoise");
                System.Diagnostics.Debug.WriteLine("[水墨渲染器] 步骤1：✅ 纹理加载成功");
                
                System.Diagnostics.Debug.WriteLine("[水墨渲染器] 步骤2：开始加载着色器...");
                var inkBleedEffect = Session.Current.Content.Load<Effect>("InkBleed");
                System.Diagnostics.Debug.WriteLine("[水墨渲染器] 步骤2：✅ 着色器加载成功");
                
                int mapWidth = Session.Current.Scenario.ScenarioMap.MapDimensions.X;
                int mapHeight = Session.Current.Scenario.ScenarioMap.MapDimensions.Y;
                
                System.Diagnostics.Debug.WriteLine($"[水墨渲染器] 步骤3：开始创建渲染器，地图尺寸={mapWidth}×{mapHeight}...");
                _inkRenderer = new WorldOfTheThreeKingdoms.GameManager.InkBleedInfluenceRenderer(
                    Platform.GraphicsDevice,
                    mapWidth,
                    mapHeight,
                    inkBleedEffect,
                    noiseTexture);
                System.Diagnostics.Debug.WriteLine("[水墨渲染器] ✅ 渲染器创建成功，等待 InfluenceUpdateManager 初始化后调用 UpdateInfluenceMap");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[水墨渲染器] ❌ 初始化失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[水墨渲染器] 堆栈: {ex.StackTrace}");
            }

            if (Session.Current.Scenario.ScenarioMap != null)
            {
                this.mainMapLayer.PrepareMap();
                this.UpdateViewport();
                this.ResetScreenEdge();
                this.mainMapLayer.ReCalculateTileDestination(this);
                this.JumpTo(Session.Current.Scenario.ScenarioMap.JumpPosition);
            }
            if (this.Plugins.GameRecordPlugin.IsRecordShowing)
            {
                this.Plugins.GameRecordPlugin.AddDisableRects();
            }
            this.Plugins.AirViewPlugin.ResetMapPosition(this);
            this.Plugins.AirViewPlugin.ResetFramePosition(base.viewportSize, this.mainMapLayer.LeftEdge, this.mainMapLayer.TopEdge, this.mainMapLayer.TotalMapSize);
            if (Session.Current.Scenario.ScenarioMap.MapName != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MGSStartLoad] Calling ReloadAirView with MapName: {Session.Current.Scenario.ScenarioMap.MapName}");
                this.Plugins.AirViewPlugin.ReloadAirView(Session.Current.Scenario.ScenarioMap.MapName);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MGSStartLoad] MapName is null, calling ReloadAirView without parameter");
                this.Plugins.AirViewPlugin.ReloadAirView();
            }
            if (this.Plugins.AirViewPlugin.IsMapShowing)
            {
                this.Plugins.AirViewPlugin.AddDisableRects();
            }
        }

        /// <summary>
        /// 🔥 CRITICAL FIX: Restore Object References After JSON Deserialization
        /// System.Text.Json deserializes data but DOES NOT restore object references.
        /// This method maps IDs back to Objects to fix NullReferenceException in RunTheFactions.
        /// </summary>
        /// <summary>
        /// 🔥 COMPREHENSIVE FIX: Robust Dictionary-Based Object Re-Linking After JSON Deserialization
        /// This method handles Factions, Persons, Architectures, AND Troops to fix RunTheFactions crashes.
        /// </summary>
        private static void RestoreObjectReferences(GameScenario scenario)
        {
            if (scenario == null)
            {
                System.Diagnostics.Debug.WriteLine("[RestoreObjectReferences] ❌ Scenario is null, cannot restore references");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[RestoreObjectReferences] 🔧 开始全面重建对象引用...");

            try
            {
                // 1. Load Raw Data (already done by caller, but ensure global setup)
                Session.Current.Scenario = scenario;

                // 2. Global Setup
                scenario.GameTime = (scenario.GameTime == DateTime.MinValue.Ticks) ? (int)new DateTime(184, 1, 1).Ticks : scenario.GameTime;

                // 3. Initialize Lists to prevent NullReference
                scenario.Factions ??= new FactionListWithQueue();
                scenario.Persons ??= new PersonList();
                scenario.Architectures ??= new ArchitectureList();
                scenario.Troops ??= new TroopListWithQueue();

                // 4. Build Lookup Maps (Speed & Safety)
                var pMap = new Dictionary<int, Person>();
                var fMap = new Dictionary<int, Faction>();
                var aMap = new Dictionary<int, Architecture>();

                foreach (var p in scenario.Persons)
                {
                    if (p is Person person && person.ID >= 0)
                        pMap[person.ID] = person;
                }

                foreach (var f in scenario.Factions)
                {
                    if (f is Faction faction && faction.ID >= 0)
                        fMap[faction.ID] = faction;
                }

                foreach (var a in scenario.Architectures)
                {
                    if (a is Architecture arch && arch.ID >= 0)
                        aMap[arch.ID] = arch;
                }

                System.Diagnostics.Debug.WriteLine($"[RestoreObjectReferences] 构建查找表完成 - 人物:{pMap.Count}, 势力:{fMap.Count}, 建筑:{aMap.Count}");

                // 5. Re-Link PERSONS (Differs from Factions)
                foreach (var p in scenario.Persons)
                {
                    if (p is Person person)
                    {
                        SetPropertyValue(person, "Scenario", scenario);
                        if (HasProperty(person, "BelongedFactionID"))
                        {
                            var factionID = GetPropertyValue<int>(person, "BelongedFactionID");
                            if (factionID >= 0 && fMap.TryGetValue(factionID, out var f))
                                SetPropertyValue(person, "BelongedFaction", f);
                        }
                        if (HasProperty(person, "LocationArchitectureID"))
                        {
                            var locationID = GetPropertyValue<int>(person, "LocationArchitectureID");
                            if (locationID >= 0 && aMap.TryGetValue(locationID, out var a))
                                person.LocationArchitecture = a;
                        }
                        
                        // 🔥 重建 Treasures 列表 - 使用 TreasureIDs
                        if (person.TreasureIDs != null && person.TreasureIDs.Count > 0)
                        {
                            person.Treasures.Clear();
                            foreach (var treasureId in person.TreasureIDs)
                            {
                                var treasure = scenario.Treasures.GetGameObject(treasureId) as Treasure;
                                if (treasure != null)
                                {
                                    person.Treasures.Add(treasure);
                                }
                            }
                        }
                    }
                }

                // 6. Re-Link FACTIONS (Critical for RunTheFactions)
                foreach (var f in scenario.Factions)
                {
                    if (f is Faction faction)
                    {
                        SetPropertyValue(faction, "Scenario", scenario);
                        
                        // Fix Leader (AI Crash Source)
                        if (HasProperty(faction, "LeaderID"))
                        {
                            var leaderID = GetPropertyValue<int>(faction, "LeaderID");
                            if (leaderID >= 0 && pMap.TryGetValue(leaderID, out var leader))
                                faction.Leader = leader;
                        }
                        
                        // Fix Capital
                        if (HasProperty(faction, "CapitalID"))
                        {
                            var capitalID = GetPropertyValue<int>(faction, "CapitalID");
                            if (capitalID >= 0 && aMap.TryGetValue(capitalID, out var capital))
                                faction.Capital = capital;
                        }
                        
                        // Fix Architecture List (Optional but good) - Use ArchitectureList instead of List<Architecture>
                        var factionArchs = new ArchitectureList();
                        foreach (var a in scenario.Architectures)
                        {
                            if (a is Architecture arch && HasProperty(arch, "BelongedFactionID"))
                            {
                                var factionID = GetPropertyValue<int>(arch, "BelongedFactionID");
                                if (factionID == faction.ID)
                                {
                                    factionArchs.Add(arch);
                                }
                            }
                        }
                        faction.Architectures = factionArchs;
                    }
                }

                // 7. Re-Link ARCHITECTURES
                foreach (var a in scenario.Architectures)
                {
                    if (a is Architecture arch)
                    {
                        SetPropertyValue(arch, "Scenario", scenario);
                        if (HasProperty(arch, "BelongedFactionID"))
                        {
                            var factionID = GetPropertyValue<int>(arch, "BelongedFactionID");
                            if (factionID >= 0 && fMap.TryGetValue(factionID, out var f))
                                SetPropertyValue(arch, "BelongedFaction", f);
                        }
                        if (HasProperty(arch, "MayorID"))
                        {
                            var mayorID = GetPropertyValue<int>(arch, "MayorID");
                            if (mayorID >= 0 && pMap.TryGetValue(mayorID, out var p))
                                SetPropertyValue(arch, "Mayor", p);
                        }
                    }
                }

                // 8. Re-Link TROOPS (Often forgotten source of crashes)
                foreach (var t in scenario.Troops)
                {
                    if (t is Troop troop)
                    {
                        SetPropertyValue(troop, "Scenario", scenario);
                        if (HasProperty(troop, "LeaderID"))
                        {
                            var leaderID = GetPropertyValue<int>(troop, "LeaderID");
                            if (leaderID >= 0 && pMap.TryGetValue(leaderID, out var p))
                                SetPropertyValue(troop, "Leader", p);
                        }
                        if (HasProperty(troop, "BelongedFactionID"))
                        {
                            var factionID = GetPropertyValue<int>(troop, "BelongedFactionID");
                            if (factionID >= 0 && fMap.TryGetValue(factionID, out var f))
                                SetPropertyValue(troop, "BelongedFaction", f);
                        }
                    }
                }

                // 9. Force Player
                if (scenario.CurrentPlayer == null && scenario.Factions.Count > 0)
                    scenario.CurrentPlayer = scenario.Factions[0] as Faction;

                System.Diagnostics.Debug.WriteLine($"[RestoreObjectReferences] ✅ 全面对象引用重建完成!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestoreObjectReferences] ❌ 重建对象引用时发生异常: {ex.Message}");
                
                // Emergency fallback: ensure CurrentPlayer is set
                if (scenario.CurrentPlayer == null && scenario.Factions != null && scenario.Factions.Count > 0)
                {
                    scenario.CurrentPlayer = scenario.Factions[0] as Faction;
                    System.Diagnostics.Debug.WriteLine($"[RestoreObjectReferences] 紧急设置当前玩家: {scenario.CurrentPlayer?.Name}");
                }
            }
        }

        /// <summary>
        /// 🔥 增强版对象引用恢复 - 专门修复 Faction.Capital null 问题
        /// </summary>
        private static void RestoreObjectReferencesEnhanced(GameScenario scenario)
        {
            if (scenario == null)
            {
                // System.Diagnostics.Debug.WriteLine("[FactionCapitalFix] ❌ Scenario is null");
                return;
            }

            // System.Diagnostics.Debug.WriteLine("[FactionCapitalFix] 🔧 开始修复 Faction.Capital null 问题...");

            try
            {
                // 1. 确保集合不为空
                scenario.Factions ??= new FactionListWithQueue();
                scenario.Architectures ??= new ArchitectureList();
                scenario.Persons ??= new PersonList();
                scenario.Troops ??= new TroopListWithQueue();

                // 2. 构建查找表
                var architectureMap = new Dictionary<int, Architecture>();
                var personMap = new Dictionary<int, Person>();
                var factionMap = new Dictionary<int, Faction>();

                // 建筑查找表
                foreach (var arch in scenario.Architectures)
                {
                    if (arch is Architecture architecture && architecture.ID >= 0)
                    {
                        architectureMap[architecture.ID] = architecture;
                        // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 注册建筑: ID={architecture.ID}, Name={architecture.Name}");
                    }
                }

                // 人物查找表
                foreach (var person in scenario.Persons)
                {
                    if (person is Person p && p.ID >= 0)
                    {
                        personMap[p.ID] = p;
                    }
                }

                // 势力查找表
                foreach (var faction in scenario.Factions)
                {
                    if (faction is Faction f && f.ID >= 0)
                    {
                        factionMap[f.ID] = f;
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 查找表构建完成 - 建筑:{architectureMap.Count}, 人物:{personMap.Count}, 势力:{factionMap.Count}");

                // 3. 🔥 重点修复 Faction.Capital 引用
                int fixedCapitalCount = 0;
                int fixedLeaderCount = 0;

                foreach (var factionObj in scenario.Factions)
                {
                    if (factionObj is Faction faction)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 处理势力: {faction.Name} (ID={faction.ID}), CapitalID={faction.CapitalID}, Architectures.Count={faction.Architectures?.Count ?? 0}");

                        // 🔥 修复：不要清空已加载的建筑列表（ProcessScenarioData已正确加载）
                        if (faction.Architectures == null)
                            faction.Architectures = new ArchitectureList();
                        
                        // 只在建筑列表为空时才重新构建
                        if (faction.Architectures.Count == 0)
                        {
                            // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 势力 {faction.Name} 建筑列表为空，尝试重新构建");
                            
                            foreach (var archObj in scenario.Architectures)
                            {
                                if (archObj is Architecture arch && arch.BelongedFactionID == faction.ID)
                                {
                                    faction.Architectures.Add(arch);
                                    arch.BelongedFaction = faction;
                                }
                            }
                            
                            // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 重新构建完成，建筑数量: {faction.Architectures.Count}");
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 势力 {faction.Name} 已有 {faction.Architectures.Count} 个建筑，跳过重建");
                        }

                        // 修复 Capital 引用
                        if (faction.CapitalID >= 0)
                        {
                            if (architectureMap.TryGetValue(faction.CapitalID, out var capital))
                            {
                                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 找到首都建筑: {capital.Name} (ID={capital.ID})");
                                
                                // 🔥 方案1：尝试使用反射设置私有字段
                                var capitalField = typeof(Faction).GetField("capital", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                
                                if (capitalField != null)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 使用反射设置 capital 字段");
                                    capitalField.SetValue(faction, capital);
                                    
                                    // 同时设置 capitalID 字段
                                    var capitalIDField = typeof(Faction).GetField("capitalID", 
                                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    if (capitalIDField != null)
                                    {
                                        capitalIDField.SetValue(faction, capital.ID);
                                    }
                                    
                                    fixedCapitalCount++;
                                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ✅ 势力 {faction.Name} 首都已恢复: {capital.Name}");
                                }
                                else
                                {
                                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ⚠️ 无法找到 capital 字段，尝试使用属性");
                                    
                                    // 🔥 方案2：直接使用属性 setter
                                    try
                                    {
                                        faction.Capital = capital;
                                        fixedCapitalCount++;
                                        // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ✅ 通过属性设置势力 {faction.Name} 首都: {capital.Name}");
                                    }
                                    catch (Exception ex)
                                    {
                                        // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ❌ 设置属性失败: {ex.Message}");
                                    }
                                }
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ⚠️ 势力 {faction.Name} 的 CapitalID={faction.CapitalID} 找不到对应建筑");
                                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 建筑字典包含 {architectureMap.Count} 个建筑");
                                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 势力建筑列表包含 {faction.Architectures?.Count ?? 0} 个建筑");
                                
                                // 紧急修复：从势力建筑中选择一个作为首都
                                if (faction.Architectures != null && faction.Architectures.Count > 0)
                                {
                                    var newCapital = faction.Architectures[0] as Architecture;
                                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 选择建筑 {newCapital.Name} (ID={newCapital.ID}) 作为首都");
                                    
                                    try
                                    {
                                        faction.Capital = newCapital;
                                        faction.CapitalID = newCapital.ID;
                                        // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 🔧 紧急设置势力 {faction.Name} 首都为: {newCapital.Name}");
                                        fixedCapitalCount++;
                                    }
                                    catch (Exception ex)
                                    {
                                        // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ❌ 紧急设置失败: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ❌ 势力 {faction.Name} 没有任何建筑，无法设置首都");
                                }
                            }
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ⚠️ 势力 {faction.Name} 的 CapitalID 无效 ({faction.CapitalID})");
                            
                            // 紧急修复：自动选择首都
                            if (faction.Architectures != null && faction.Architectures.Count > 0)
                            {
                                var newCapital = faction.Architectures
                                    .Cast<Architecture>()
                                    .OrderByDescending(a => a.Population)
                                    .First();
                                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 自动选择建筑 {newCapital.Name} (ID={newCapital.ID}, 人口={newCapital.Population}) 作为首都");
                                
                                try
                                {
                                    faction.Capital = newCapital;
                                    faction.CapitalID = newCapital.ID;
                                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 🔧 自动设置势力 {faction.Name} 首都为: {newCapital.Name}");
                                    fixedCapitalCount++;
                                }
                                catch (Exception ex)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ❌ 自动设置失败: {ex.Message}");
                                }
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ❌ 势力 {faction.Name} 没有任何建筑，无法自动设置首都");
                            }
                        }

                        // 修复 Leader 引用
                        if (faction.LeaderID >= 0 && personMap.TryGetValue(faction.LeaderID, out var leader))
                        {
                            faction.Leader = leader;
                            fixedLeaderCount++;
                            // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ✅ 势力 {faction.Name} 领袖已恢复: {leader.Name}");
                        }

                        // 验证修复结果
                        if (faction.Capital == null)
                        {
                            // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ❌ 势力 {faction.Name} 的 Capital 仍然为 null！");
                            // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 详细信息:");
                            // System.Diagnostics.Debug.WriteLine($"  - CapitalID: {faction.CapitalID}");
                            // System.Diagnostics.Debug.WriteLine($"  - Architectures.Count: {faction.Architectures?.Count ?? 0}");
                            // System.Diagnostics.Debug.WriteLine($"  - 建筑字典中是否存在: {architectureMap.ContainsKey(faction.CapitalID)}");
                            
                            // 列出势力的所有建筑
                            if (faction.Architectures != null && faction.Architectures.Count > 0)
                            {
                                // System.Diagnostics.Debug.WriteLine($"  - 势力建筑列表:");
                                foreach (var archObj in faction.Architectures)
                                {
                                    if (archObj is Architecture a)
                                    {
                                        // System.Diagnostics.Debug.WriteLine($"    * {a.Name} (ID={a.ID}, BelongedFactionID={a.BelongedFactionID})");
                                    }
                                }
                            }
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ✅ 验证成功: 势力 {faction.Name} 的 Capital = {faction.Capital.Name}");
                        }
                    }
                }

                // 4. 修复建筑的势力引用
                foreach (var archObj in scenario.Architectures)
                {
                    if (archObj is Architecture arch)
                    {
                        if (arch.BelongedFactionID >= 0 && factionMap.TryGetValue(arch.BelongedFactionID, out var faction))
                        {
                            arch.BelongedFaction = faction;
                        }
                    }
                }

                // 5. 修复人物的势力和位置引用
                foreach (var personObj in scenario.Persons)
                {
                    if (personObj is Person person)
                    {
                        // 修复势力引用 - 通过设置 LocationArchitecture 来间接设置 BelongedFaction
                        if (person.BelongedFactionID >= 0 && factionMap.TryGetValue(person.BelongedFactionID, out var faction))
                        {
                            // Person.BelongedFaction 是只读的，通过 LocationArchitecture 间接设置
                            if (person.LocationArchitecture != null && person.LocationArchitecture.BelongedFaction != faction)
                            {
                                // 如果当前位置的势力不匹配，需要找到正确的建筑
                                var correctArch = architectureMap.Values.FirstOrDefault(a => a.BelongedFactionID == faction.ID);
                                if (correctArch != null)
                                {
                                    person.LocationArchitecture = correctArch;
                                }
                            }
                        }

                        // 修复位置引用
                        if (HasProperty(person, "TempLocationArchitectureID"))
                        {
                            var locationID = GetPropertyValue<int>(person, "TempLocationArchitectureID");
                            if (locationID >= 0 && architectureMap.TryGetValue(locationID, out var location))
                            {
                                person.LocationArchitecture = location;
                            }
                        }
                    }
                }

                // 6. 最终验证
                int nullCapitalCount = 0;
                int totalFactionCount = 0;
                foreach (var factionObj in scenario.Factions)
                {
                    if (factionObj is Faction faction)
                    {
                        totalFactionCount++;
                        if (faction.Capital == null)
                        {
                            nullCapitalCount++;
                            // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ❌ 验证失败: 势力 {faction.Name} 的 Capital 仍为 null");
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ✅ 修复完成统计:");
                // System.Diagnostics.Debug.WriteLine($"  - 总势力数: {totalFactionCount}");
                // System.Diagnostics.Debug.WriteLine($"  - 首都修复: {fixedCapitalCount}");
                // System.Diagnostics.Debug.WriteLine($"  - 领袖修复: {fixedLeaderCount}");
                // System.Diagnostics.Debug.WriteLine($"  - Capital 仍为 null: {nullCapitalCount}");

                if (nullCapitalCount > 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ⚠️ 仍有 {nullCapitalCount} 个势力的 Capital 为 null，需要进一步调查");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] ❌ 修复过程中发生异常: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"[FactionCapitalFix] 异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// <summary>
        /// Helper method to check if an object has a specific property
        /// 🔥 AOT 兼容：使用 IPropertyAccessor 接口替代反射
        /// 🔥 ANTI-BAND-AID：obj 为 null 说明数据加载错误，应该 Fail Fast
        /// 日期：2026-03-16
        /// </summary>
        private static bool HasProperty(object obj, string propertyName)
        {
            // 🔥 Fail Fast：obj 不应该为 null
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "读档时对象为 null，数据加载错误");
            
            // 🔥 使用接口而非反射
            if (obj is GameGlobal.IPropertyAccessor accessor)
            {
                return accessor.HasProperty(propertyName);
            }
            
            // 🔥 Fail Fast：对象未实现接口说明代码错误
            throw new InvalidOperationException($"对象 {obj.GetType().Name} 未实现 IPropertyAccessor 接口");
        }

        /// <summary>
        /// Helper method to get property value with multiple possible property names
        /// 🔥 AOT 兼容：使用 IPropertyAccessor 接口替代反射
        /// 🔥 ANTI-BAND-AID：异常应该抛出，不应该吞掉
        /// 日期：2026-03-16
        /// </summary>
        private static T GetPropertyValue<T>(object obj, params string[] propertyNames)
        {
            // 🔥 Fail Fast：obj 不应该为 null
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "读档时对象为 null，数据加载错误");

            // 🔥 Fail Fast：对象未实现接口说明代码错误
            if (obj is not GameGlobal.IPropertyAccessor accessor)
                throw new InvalidOperationException($"对象 {obj.GetType().Name} 未实现 IPropertyAccessor 接口");

            // 尝试所有可能的属性名
            foreach (var propName in propertyNames)
            {
                if (accessor.HasProperty(propName))
                {
                    var value = accessor.GetProperty(propName);
                    if (value != null)
                    {
                        // 类型转换
                        if (typeof(T) == typeof(int) && value is int intValue)
                            return (T)(object)intValue;
                        if (typeof(T) == typeof(object))
                            return (T)value;
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
            }
            
            // 所有属性名都不存在，返回默认值（这是正常情况，不是错误）
            return default(T);
        }

        /// <summary>
        /// Helper method to set property value
        /// 🔥 AOT 兼容：使用 IPropertyAccessor 接口替代反射
        /// 🔥 ANTI-BAND-AID：异常应该抛出，不应该吞掉
        /// 日期：2026-03-16
        /// </summary>
        private static void SetPropertyValue(object obj, string propertyName, object value)
        {
            // 🔥 Fail Fast：obj 不应该为 null
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "读档时对象为 null，数据加载错误");

            // 🔥 Fail Fast：对象未实现接口说明代码错误
            if (obj is not GameGlobal.IPropertyAccessor accessor)
                throw new InvalidOperationException($"对象 {obj.GetType().Name} 未实现 IPropertyAccessor 接口");

            // 🔥 不捕获异常：让异常向上传播，暴露数据错误
            accessor.SetProperty(propertyName, value);
        }
    }
}
