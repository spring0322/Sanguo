using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using GameObjects.FactionDetail;
using GameObjects.PersonDetail;
using GameObjects.SectionDetail;
using GameObjects.TroopDetail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PluginInterface;
using WorldOfTheThreeKingdoms.GameLogic;
using WorldOfTheThreeKingdoms.GameScreens;
using WorldOfTheThreeKingdoms.GameScreens.ScreenLayers;
using WorldOfTheThreeKingdoms.Resources;
using GameManager;
using Platforms;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    partial class MainGameScreen : Screen
    {
        /*
        #region 军师双击菜单字段

        // 双击检测相关
        private DateTime _lastLeftClickTime = DateTime.MinValue;
        private Point _lastLeftClickPosition = Point.Zero;
        private const int DOUBLE_CLICK_THRESHOLD_MS = 400;
        private const int DOUBLE_CLICK_DISTANCE_THRESHOLD = 10;

        #endregion
        */

        public override void EarlyMouseLeftDown()
        {
            base.EarlyMouseLeftDown();
            this.scrollSpeedScale = this.scrollSpeedScaleSpeedy;
        }

        public override void EarlyMouseLeftUp()
        {
            base.EarlyMouseLeftUp();
            this.DrawingSelector = false;
            this.scrollSpeedScale = this.scrollSpeedScaleDefault;
        }

        public override void EarlyMouseMove()
        {
            base.EarlyMouseMove();
        }

        public override void EarlyMouseRightDown()
        {
            base.EarlyMouseRightDown();
            
            // 🎯 右键菜单必须在 Early 阶段处理，因为 Later 阶段鼠标状态已经更新
            if (!this.editMode)
            {
                this.ContextMenuRightClick();
            }
        }

        public override void EarlyMouseRightUp()
        {
            base.EarlyMouseRightUp();
        }

        public override void EarlyMouseScroll()
        {
            base.EarlyMouseScroll();
        }

        private void HandleLaterMouseEvent(GameTime gameTime)
        {
            try
            {
                if (base.EnableMouseEvent && base.EnableLaterMouseEvent)
                {
                    if (!StaticMethods.PointInViewport(new Point(InputManager.PoX, InputManager.PoY), base.viewportSize))
                    {
                        this.UpdateViewMove();
                    }
                    else
                    {
                        this.ResetCurrentStatus();
                        Faction routewayFaction = Session.Current.Scenario.IsObserverModeActive()
                            ? this.GetViewingFaction()
                            : Session.Current.Scenario.CurrentPlayer;
                        this.CurrentArchitecture = Session.Current.Scenario.GetArchitectureByPosition(this.position);
                        this.CurrentTroop = Session.Current.Scenario.GetTroopByPosition(this.position);
                        this.CurrentRouteway = Session.Current.Scenario.GetRoutewayByPositionAndFaction(this.position, routewayFaction);
                        this.HandleLaterMouseMove();
                        this.HandleLaterMouseScroll();
                        if (this.viewMove == ViewMove.Stop)
                        {
                            this.HandleLaterMouseLeftDown();
                            this.HandleLaterMouseLeftUp();
                            this.HandleLaterMouseRightDown();
                            this.HandleLaterMouseRightUp();
                            this.UpdateConmentText(gameTime);
                            this.UpdateSurvey(gameTime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleLaterMouseEvent] 异常: {ex}");
            }
        }

        private void HandleLaterMouseLeftDown()
        {
            // 首先处理军师双击菜单
            // HandleAdvisorDoubleClickMenu();
            
            if (InputManager.IsDown && this.viewMove == ViewMove.Stop)
            {
                if (this.editMode)
                {
                    int x = (InputManager.PoX - this.mainMapLayer.LeftEdge) / Session.Current.Scenario.ScenarioMap.TileWidth;
                    int y = (InputManager.PoY - this.mainMapLayer.TopEdge) / Session.Current.Scenario.ScenarioMap.TileHeight;
                    
                    // 添加边界检查
                    if (x >= 0 && y >= 0 && 
                        x < Session.Current.Scenario.ScenarioMap.MapDimensions.X && 
                        y < Session.Current.Scenario.ScenarioMap.MapDimensions.Y)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[TerrainEdit] 左键点击 - 位置({x},{y}), 地形类型: {this.ditukuaidezhi}");
                        Session.Current.Scenario.ScenarioMap.MapData[x, y] = this.ditukuaidezhi;
                        this.mainMapLayer.chongsheditukuaitupian(x, y);
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"[TerrainEdit] 左键点击越界 - 位置({x},{y}), 地图大小({Session.Current.Scenario.ScenarioMap.MapDimensions.X},{Session.Current.Scenario.ScenarioMap.MapDimensions.Y})");
                    }
                } 
                else if (Session.Current.Scenario.CurrentPlayer != null && 
                    this.PeekUndoneWork().Kind == UndoneWorkKind.None && Session.Current.Scenario.CurrentPlayer == Session.Current.Scenario.CurrentFaction)
                {
                    if (this.CurrentArchitecture == null && this.CurrentTroop == null && this.CurrentRouteway == null)
                    {
                        if (this.Plugins.youcelanPlugin.IsShowing && StaticMethods.PointInRectangle(this.MousePosition, this.Plugins.youcelanPlugin.FrameRectangle))
                        {
                        }
                        else
                        {
                            this.DrawingSelector = !this.Plugins.ContextMenuPlugin.IsShowing && !this.Plugins.RoutewayEditorPlugin.IsShowing;
                        }
                    }
                }
            }
        }

        private void HandleLaterMouseLeftUp()
        {
            if (InputManager.IsDownPre && InputManager.IsReleased && this.viewMove == ViewMove.Stop)
            {
                if (((Session.GlobalVariables.SkyEye || Session.Current.Scenario.NoCurrentPlayer) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(this.position)) && ((this.Plugins.ContextMenuPlugin != null) && (this.PeekUndoneWork().Kind == UndoneWorkKind.None)))
                {
                    this.SelectorStartPosition = base.MousePosition;
                    this.updateGameScreenByCurrentTarget();
                }
            }
        }

        private void HandleLaterMouseMove()
        {
            if (InputManager.IsPosChanged)
            {
                this.UpdateViewMove();
                if (this.editMode)
                {
                    if (InputManager.IsDown && (InputManager.NowMouse.RightButton != ButtonState.Pressed))
                    {
                        int x = (InputManager.PoX - this.mainMapLayer.LeftEdge) / Session.Current.Scenario.ScenarioMap.TileWidth;
                        int y = (InputManager.PoY - this.mainMapLayer.TopEdge) / Session.Current.Scenario.ScenarioMap.TileHeight;
                        
                        // 添加边界检查
                        if (x >= 0 && y >= 0 && 
                            x < Session.Current.Scenario.ScenarioMap.MapDimensions.X && 
                            y < Session.Current.Scenario.ScenarioMap.MapDimensions.Y)
                        {
                            if (Session.Current.Scenario.ScenarioMap.MapData[x, y] != this.ditukuaidezhi)
                            {
                                Session.Current.Scenario.ScenarioMap.MapData[x, y] = this.ditukuaidezhi;
                                this.mainMapLayer.chongsheditukuaitupian(x, y);
                                // System.Diagnostics.Debug.WriteLine($"[TerrainEdit] 拖拽绘制 - 位置({x},{y}), 地形类型: {this.ditukuaidezhi}");
                            }
                        }
                    }

                    if (InputManager.IsDown && (InputManager.NowMouse.RightButton == ButtonState.Pressed))
                    {
                        int x = (InputManager.PoX - this.mainMapLayer.LeftEdge) / Session.Current.Scenario.ScenarioMap.TileWidth;
                        int y = (InputManager.PoY - this.mainMapLayer.TopEdge) / Session.Current.Scenario.ScenarioMap.TileHeight;
                        
                        // 添加边界检查
                        if (x >= 0 && y >= 0 && 
                            x < Session.Current.Scenario.ScenarioMap.MapDimensions.X && 
                            y < Session.Current.Scenario.ScenarioMap.MapDimensions.Y)
                        {
                            if (Session.Current.Scenario.ScenarioMap.MapData[x, y] != this.ditukuaidezhi)
                            {
                                Session.Current.Scenario.ScenarioMap.MapData[x, y] = this.ditukuaidezhi;
                                this.mainMapLayer.chongsheditukuaitupian(x, y);
                                // System.Diagnostics.Debug.WriteLine($"[TerrainEdit] 右键拖拽绘制 - 位置({x},{y}), 地形类型: {this.ditukuaidezhi}");
                            }
                        }
                    }
                }
            }
        }

        private void HandleLaterMouseRightDown()
        {
            // ✅ 修复：只保留编辑模式的处理，非编辑模式的右键已在 EarlyMouseRightDown 处理
            if (this.editMode)
            {
                if ((InputManager.MouseStatePre.RightButton == ButtonState.Released) && (InputManager.NowMouse.RightButton == ButtonState.Pressed))
                {
                    int x = (InputManager.NowMouse.X - this.mainMapLayer.LeftEdge) / Session.Current.Scenario.ScenarioMap.TileWidth;
                    int y = (InputManager.NowMouse.Y - this.mainMapLayer.TopEdge) / Session.Current.Scenario.ScenarioMap.TileHeight;
                    
                    // 添加边界检查
                    if (x >= 0 && y >= 0 && 
                        x < Session.Current.Scenario.ScenarioMap.MapDimensions.X && 
                        y < Session.Current.Scenario.ScenarioMap.MapDimensions.Y)
                    {
                        Session.Current.Scenario.ScenarioMap.MapData[x, y] = this.ditukuaidezhi;
                        this.mainMapLayer.chongsheditukuaitupian(x, y);
                    }
                }
            }
            // ✅ 删除了 else 分支，避免重复调用 ContextMenuRightClick()
        }

        private void HandleLaterMouseRightUp()
        {
            if ((InputManager.MouseStatePre.RightButton == ButtonState.Pressed) && (InputManager.NowMouse.RightButton == ButtonState.Released))
            {
            }
        }

        private void HandleLaterMouseScroll()
        {
            if (this.currentKey == Keys.OemPlus || this.currentKey == Keys.OemMinus || (InputManager.NowMouse.ScrollWheelValue != InputManager.MouseStatePre.ScrollWheelValue && this.oldScrollWheelValue != InputManager.NowMouse.ScrollWheelValue))
            {
                float num = InputManager.NowMouse.ScrollWheelValue - this.oldScrollWheelValue;
                this.oldScrollWheelValue = InputManager.NowMouse.ScrollWheelValue;

                if (this.currentKey == Keys.OemPlus)
                {
                    num = 0.1f;
                }
                else if (this.currentKey == Keys.OemMinus)
                {
                    num = -0.1f;
                }

                if (num > 0f)
                {
                    num = 0.1f;
                    if (Session.MainGame.mainGameScreen.mainMapLayer.TileWidth == Session.Current.Scenario.ScenarioMap.TileWidthMax)
                    {
                        return;
                    }
                }
                if (num < 0f)
                {
                    num = -0.1f;
                    if (Session.MainGame.mainGameScreen.mainMapLayer.TileWidth == Session.Current.Scenario.ScenarioMap.TileWidthMin)
                    {
                        return;
                    }
                }

                ProcessScrollWheel(num);
            }

            if (InputManager.PinchMove != 0f)
            {
                ProcessScrollWheel(InputManager.PinchMove);
                if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
                {
                    InputManager.PinchMove = 0f;
                }
            }
        }

        private void ProcessScrollWheel(float num)
        {
            int tileWidthMax = (int)((1f + num) * this.mainMapLayer.TileWidth);
            if (tileWidthMax > Session.Current.Scenario.ScenarioMap.TileWidthMax)
            {
                tileWidthMax = Session.Current.Scenario.ScenarioMap.TileWidthMax;
            }
            else if (tileWidthMax < Session.Current.Scenario.ScenarioMap.TileWidthMin)
            {
                tileWidthMax = Session.Current.Scenario.ScenarioMap.TileWidthMin;
            }
            int tileHeightMax = (int)((1f + num) * this.mainMapLayer.TileHeight);
            if (tileHeightMax > Session.Current.Scenario.ScenarioMap.TileWidthMax)
            {
                tileHeightMax = Session.Current.Scenario.ScenarioMap.TileWidthMax;
            }
            else if (tileHeightMax < Session.Current.Scenario.ScenarioMap.TileWidthMin)
            {
                tileHeightMax = Session.Current.Scenario.ScenarioMap.TileWidthMin;
            }

            int tileWidth = this.mainMapLayer.TileWidth;
            int tileHeight = this.mainMapLayer.TileHeight;
            this.mainMapLayer.TileWidth = tileWidthMax;
            this.mainMapLayer.TileHeight = tileHeightMax;

            num = (((float)tileWidthMax) / ((float)tileWidth)) - 1f;
            int num4 = this.mainMapLayer.LeftEdge + ((int)(num * (Session.MainGame.mainGameScreen.mainMapLayer.LeftEdge - InputManager.PoX)));
            int num5 = this.mainMapLayer.TopEdge + ((int)(num * (Session.MainGame.mainGameScreen.mainMapLayer.TopEdge - InputManager.PoY)));
            if (((((this.viewportSize.X - num4) <= this.mainMapLayer.TotalTileWidth) && (num4 <= 0)) && ((this.viewportSize.Y - num5) <= this.mainMapLayer.TotalTileHeight)) && (num5 <= 0))
            {
                this.mainMapLayer.LeftEdge = num4;
                this.mainMapLayer.TopEdge = num5;
            }
            else
            {
                this.mainMapLayer.TileWidth = tileWidth;
                this.mainMapLayer.TileHeight = tileHeight;
            }
            this.ResetScreenEdge();
            this.mainMapLayer.ReCalculateTileDestination(this);
            Session.Current.Scenario.TroopAnimations.UpdateDirectionAnimations(Session.MainGame.mainGameScreen.mainMapLayer.TileWidth);
            this.Plugins.AirViewPlugin.ResetFrameSize(base.viewportSize, Session.MainGame.mainGameScreen.mainMapLayer.TotalMapSize);
            this.Plugins.AirViewPlugin.ResetFramePosition(base.viewportSize, Session.MainGame.mainGameScreen.mainMapLayer.LeftEdge, Session.MainGame.mainGameScreen.mainMapLayer.TopEdge, Session.MainGame.mainGameScreen.mainMapLayer.TotalMapSize);
        }

        /*
        #region 军师双击菜单处理

        /// <summary>
        /// 处理军师双击菜单的鼠标左键点击
        /// </summary>
        private void HandleAdvisorDoubleClickMenu()
        {
            // [新版本] 双击检测现在由 DoubleClickAdvisorSystem 自己处理
            // 这里只需要确保系统在运行即可
            try
            {
                // 让双击系统自己处理鼠标输入
                // DoubleClickAdvisorSystem.Instance.Update() 会在 DoubleClickIntegration.Update() 中被调用
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainGameScreen] 双击处理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否为双击
        /// </summary>
        private bool IsDoubleClick(DateTime currentTime, Point currentPosition)
        {
            // 检查时间间隔
            double timeDiff = (currentTime - _lastLeftClickTime).TotalMilliseconds;
            if (timeDiff > DOUBLE_CLICK_THRESHOLD_MS)
                return false;

            // 检查位置距离
            double distance = Math.Sqrt(
                Math.Pow(currentPosition.X - _lastLeftClickPosition.X, 2) +
                Math.Pow(currentPosition.Y - _lastLeftClickPosition.Y, 2)
            );
            
            return distance <= DOUBLE_CLICK_DISTANCE_THRESHOLD;
        }

        /// <summary>
        /// 检查是否为空白地形点击
        /// </summary>
        private bool IsEmptyTerrainClick(Point screenPosition)
        {
            try
            {
                // 1. 检查是否有建筑
                if (this.CurrentArchitecture != null)
                {
                    Console.WriteLine($"[MainGameScreen] 位置有建筑: {this.CurrentArchitecture.Name}");
                    return false;
                }

                // 2. 检查是否有部队
                if (this.CurrentTroop != null)
                {
                    Console.WriteLine($"[MainGameScreen] 位置有部队: {this.CurrentTroop.Name}");
                    return false;
                }

                // 3. 检查是否有路径
                if (this.CurrentRouteway != null)
                {
                    Console.WriteLine($"[MainGameScreen] 位置有路径");
                    return false;
                }

                // 4. 检查是否在UI元素上
                if (IsOverUIElement(screenPosition))
                {
                    Console.WriteLine($"[MainGameScreen] 位置在UI元素上");
                    return false;
                }

                Console.WriteLine($"[MainGameScreen] 位置是空白地形，可以显示军师菜单");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainGameScreen] 检查空白地形时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查是否在UI元素上
        /// </summary>
        private bool IsOverUIElement(Point screenPosition)
        {
            // 检查是否在各种插件UI上
            if (this.Plugins != null)
            {
                // 检查右键菜单
                if (this.Plugins.ContextMenuPlugin?.IsShowing == true)
                {
                    return true;
                }

                // 检查工具栏
                if (this.Plugins.ToolBarPlugin?.IsShowing == true)
                {
                    return true;
                }

                // 检查右侧面板
                if (this.Plugins.youcelanPlugin?.IsShowing == true &&
                    StaticMethods.PointInRectangle(screenPosition, this.Plugins.youcelanPlugin.FrameRectangle))
                {
                    return true;
                }

                // 检查其他可能的UI面板
                if (this.Plugins.TabListPlugin?.IsShowing == true ||
                    this.Plugins.GameFramePlugin?.IsShowing == true)
                {
                    return true;
                }
            }

            // 检查是否在屏幕边缘（可能是UI区域）
            if (screenPosition.Y > base.viewportSize.Y - 50) // 底部工具栏区域
            {
                return true;
            }

            if (screenPosition.X > base.viewportSize.X - 200) // 右侧面板区域
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 显示军师右键菜单（参考ContextMenuRightClick的实现）
        /// </summary>
        private void ShowAdvisorContextMenu()
        {
            Console.WriteLine("[MainGameScreen] ShowAdvisorContextMenu被调用");
            
            if ((this.Plugins.ContextMenuPlugin != null) && (this.PeekUndoneWork().Kind == UndoneWorkKind.None))
            {
                Console.WriteLine("[MainGameScreen] ContextMenuPlugin可用，UndoneWork为None");
                
                if (!this.Plugins.ContextMenuPlugin.IsShowing)
                {
                    Console.WriteLine("[MainGameScreen] 准备显示菜单");
                    
                    // 暂时使用现有的MapRightClick菜单类型进行测试
                    this.Plugins.ContextMenuPlugin.IsShowing = true;
                    this.Plugins.ContextMenuPlugin.SetCurrentGameObject(this);
                    this.Plugins.ContextMenuPlugin.SetMenuKindByName("MapRightClick");
                    this.Plugins.ContextMenuPlugin.Prepare(InputManager.PoX, InputManager.PoY, base.viewportSize);
                    this.bianduiLiebiaoBiaoji = "MapRightClick";
                    
                    Console.WriteLine("[MainGameScreen] 军师双击菜单已显示（使用MapRightClick类型）");
                }
                else
                {
                    Console.WriteLine("[MainGameScreen] ContextMenuPlugin已经在显示中");
                }
            }
            else
            {
                Console.WriteLine($"[MainGameScreen] 无法显示菜单 - ContextMenuPlugin: {this.Plugins.ContextMenuPlugin != null}, UndoneWork: {this.PeekUndoneWork().Kind}");
            }
        }

        #endregion
        */
    }
}
