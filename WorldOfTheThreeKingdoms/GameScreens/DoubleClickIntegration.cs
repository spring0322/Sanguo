using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameManager;
using GameObjects;
// [修复] 引入缺失的命名空间，通常这些枚举在这里
using GameGlobal; 
using WorldOfTheThreeKingdoms.GameScreens; // 引用 AdvisorSystem 所在的层级

namespace WorldOfTheThreeKingdoms.GameScreens
{
    // 定义简单的菜单项结构
    public class AdvisorMenuItem
    {
        public string Title { get; set; }
        public Action OnClick { get; set; }
    }

    /// <summary>
    /// 双击系统集成 (Controller)
    /// </summary>
    public class DoubleClickIntegration
    {
        private static bool _isInitialized = false;
        // [修复] 保存当前帧的 GameTime 供回调使用
        private static GameTime _currentGameTime; 

        public static void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                DoubleClickMenuManager.Instance.Initialize();
                // [修复] 修正类名为 DoubleClickAdvisorSystem (之前可能有笔误)
                DoubleClickAdvisorSystem.Instance.OnDoubleClickDetected += HandleDoubleClickDetected;
                _isInitialized = true;
                Console.WriteLine("✅ 双击军师系统已就绪");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 初始化失败: {ex.Message}");
            }
        }

        public static void Update(GameTime gameTime)
        {
            if (!_isInitialized) return;

            try
            {
                _currentGameTime = gameTime;
                // [修复] 使用正确的类名
                DoubleClickAdvisorSystem.Instance.Update();
                // [修复] 传递 gameTime
                DoubleClickMenuManager.Instance.Update(gameTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[控制器] Update 异常: {ex.Message}");
            }
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (!_isInitialized) return;

            try
            {
                DoubleClickMenuManager.Instance.Draw(spriteBatch);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[控制器] Draw 异常: {ex.Message}");
            }
        }

        private static void HandleDoubleClickDetected(ClickedObjectType type, object targetObj, Point position)
        {
            try
            {
                Console.WriteLine($"[控制器] 收到双击信号: {type}");

                List<AdvisorMenuItem> menuItems = new List<AdvisorMenuItem>();

                if (type == ClickedObjectType.EmptyTerrain)
                {
                    menuItems = GenerateMapMenu();
                }
                else if (type == ClickedObjectType.Architecture)
                {
                    menuItems = GenerateArchitectureMenu(targetObj as Architecture);
                }
                else if (type == ClickedObjectType.Troop)
                {
                    menuItems = GenerateTroopMenu(targetObj as Troop);
                }

                if (menuItems.Count > 0)
                {
                    DoubleClickMenuManager.Instance.ShowMenu(menuItems, position, _currentGameTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[控制器] HandleDoubleClickDetected 异常: {ex.Message}");
            }
        }

        // --- 菜单生成逻辑 ---
        private static List<AdvisorMenuItem> GenerateArchitectureMenu(Architecture arch)
        {
            var list = new List<AdvisorMenuItem>();
            if (arch == null) return list;

            list.Add(new AdvisorMenuItem 
            { 
                Title = "自动内政", 
                OnClick = () => { 
                    Console.WriteLine("执行自动内政..."); 
                } 
            });

            // 添加军师任命选项
            list.Add(new AdvisorMenuItem 
            { 
                Title = "军师任命", 
                OnClick = () => { 
                    Console.WriteLine("点击了军师任命");
                    HandleStrategistAppointment();
                } 
            });

            return list;
        }

        private static List<AdvisorMenuItem> GenerateTroopMenu(Troop troop)
        {
            var list = new List<AdvisorMenuItem>();
            if (troop == null) return list;

            list.Add(new AdvisorMenuItem 
            { 
                Title = "一键补给", 
                OnClick = () => { Console.WriteLine("执行补给..."); } 
            });

            return list;
        }

        private static List<AdvisorMenuItem> GenerateMapMenu()
        {
            var list = new List<AdvisorMenuItem>();

            list.Add(new AdvisorMenuItem 
            { 
                Title = "势力情报", 
                OnClick = () => { 
                    Console.WriteLine("点击了势力情报");
                    try
                    {
                        var mainScreen = Session.MainGame.mainGameScreen;
                        if (mainScreen != null && Session.Current.Scenario?.CurrentPlayer != null)
                        {
                            mainScreen.ShowTabListInFrame(
                                UndoneWorkKind.Frame, 
                                FrameKind.Faction, 
                                FrameFunction.Browse, 
                                false, true, false, false, 
                                Session.Current.Scenario.CurrentPlayer.GetGameObjectList(), 
                                null, "势力信息", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"显示势力信息失败: {ex.Message}");
                    }
                } 
            });

            return list;
        }
    }
}