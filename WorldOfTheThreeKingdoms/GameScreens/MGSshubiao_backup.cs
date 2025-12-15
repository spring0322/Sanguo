using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GameFreeText;
using GameGlobal;
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

//using GameObjects.PersonDetail.PersonMessages;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    partial class MainGameScreen : Screen
    {
        #region 军师双击菜单字段

        // 双击检测相关
        private DateTime _lastLeftClickTime = DateTime.MinValue;
        private Point _lastLeftClickPosition = Point.Zero;
        private const int DOUBLE_CLICK_THRESHOLD_MS = 400;
        private const int DOUBLE_CLICK_DISTANCE_THRESHOLD = 10;

        #endregion

        public override void EarlyMouseLeftDown()
        {
            base.EarlyMouseLeftDown();

            this.scrollSpeedScale = this.scrollSpeedScaleSpeedy;
            /*if (!Session.Current.Scenario.LoadAndSaveAvail())
            {
                Session.GlobalVariables.FastBattleSpeed = InputManager.NowMouse.LeftButton == ButtonState.Pressed;
            }*/
        }

        public override void EarlyMouseLeftUp()
        {
            base.EarlyMouseLeftUp();
            this.DrawingSelector = false;
            this.scrollSpeedScale = this.scrollSpeedScaleDefault;
            //GlobalVariables.FastBattleSpeed = false;
        }

        public override void EarlyMouseMove()
        {
            base.EarlyMouseMove();
        }

        public override void EarlyMouseRightDown()
        {
            base.EarlyMouseRightDown();
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
            if (base.EnableMouseEvent && base.EnableLaterMouseEvent)
            {
                if (!StaticMethods.PointInViewport(new Point(InputManager.PoX, InputManager.PoY), base.viewportSize))
                {
                    this.UpdateViewMove();
                }
                else
                {
                    this.ResetCurrentStatus();
                    this.CurrentArchitecture = Session.Current.Scenario.GetArchitectureByPosition(this.position);
                    this.CurrentTroop = Session.Current.Scenario.GetTroopByPosition(this.position);
                    this.CurrentRouteway = Session.Current.Scenario.GetRoutewayByPositionAndFaction(this.position, Session.Current.Scenario.CurrentPlayer);
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

        //private void HandleLaterMouseEventOld(GameTime gameTime)
        //{
        //    if (base.EnableMouseEvent && base.EnableLaterMouseEvent)
        //    {
        //        if (!StaticMethods.PointInViewport(new Point(InputManager.NowMouse.X, InputManager.NowMouse.Y), base.viewportSize))
        //        {
        //            this.UpdateViewMove();
        //        }
        //        else
        //        {
        //            this.ResetCurrentStatus();
        //            this.CurrentArchitecture = Session.Current.Scenario.GetArchitectureByPosition(this.position);
        //            this.CurrentTroop = Session.Current.Scenario.GetTroopByPosition(this.position);
        //            this.CurrentRouteway = Session.Current.Scenario.GetRoutewayByPositionAndFaction(this.position, Session.Current.Scenario.CurrentPlayer);
        //            this.HandleLaterMouseMove();
        //            this.HandleLaterMouseScroll();
        //            if (this.viewMove == ViewMove.Stop)
        //            {
        //                this.HandleLaterMouseLeftDown();
        //                this.HandleLaterMouseLeftUp();
        //                this.HandleLaterMouseRightDown();
        //                this.HandleLaterMouseRightUp();
        //                this.UpdateConmentText(gameTime);
        //                this.UpdateSurvey(gameTime);
        //            }
        //        }
        //    }
        //}

        private void HandleLaterMouseLeftDown()
        {
            // 首先处理军师双击菜单
            HandleAdvisorDoubleClickMenu();
            
            //if (((this.previousMouseState.LeftButton == ButtonState.Released) && (InputManager.NowMouse.LeftButton == ButtonState.Pressed)) && (this.viewMove == ViewMove.Stop))
            if (InputManager.IsDown && this.viewMove == ViewMove.Stop)
            {
                if (this.editMode)
                {
                    int x = (InputManager.PoX - this.mainMapLayer.LeftEdge) / Session.Current.Scenario.ScenarioMap.TileWidth;
                    int y = (InputManager.PoY - this.mainMapLayer.TopEdge) / Session.Current.Scenario.ScenarioMap.TileHeight;
                    Session.Current.Scenario.ScenarioMap.MapData[x, y] = this.ditukuaidezhi;
                    this.mainMapLayer.chongsheditukuaitupian(x, y);
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
            /*
            if (Session.Current.Scenario.CurrentPlayer == null) return;
            if (((this.previousMouseState.LeftButton == ButtonState.Released) && (InputManager.NowMouse.LeftButton == ButtonState.Pressed)) && (this.viewMove == ViewMove.Stop))
            {
                if (((Session.GlobalVariables.SkyEye || Session.Current.Scenario.NoCurrentPlayer) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(this.position)) && ((this.Plugins.ContextMenuPlugin != null) && (this.PeekUndoneWork().Kind == UndoneWorkKind.None)))
                {
                    if ((((this.CurrentArchitecture != null) && (this.CurrentTroop != null)) && (this.CurrentTroop.BelongedFaction == Session.Current.Scenario.CurrentPlayer)) && (this.CurrentArchitecture.BelongedFaction == Session.Current.Scenario.CurrentPlayer) && this.CurrentTroop.Operated == false)
                    {
                        if (!(this.Plugins.ContextMenuPlugin.IsShowing || !Session.Current.Scenario.CurrentPlayer.Controlling))
                        {
                            this.Plugins.ContextMenuPlugin.IsShowing = true;
                            this.Plugins.ContextMenuPlugin.SetCurrentGameObject(this);
                            this.Plugins.ContextMenuPlugin.SetMenuKindByName("ArchitectureTroopLeftClick");
                            this.Plugins.ContextMenuPlugin.Prepare(InputManager.NowMouse.X, InputManager.NowMouse.Y, base.viewportSize);
                            this.bianduiLiebiaoBiaoji = "ArchitectureTroopLeftClick";
                        }
                    }
                    else if ((this.CurrentTroop != null) && (this.CurrentTroop.BelongedFaction == Session.Current.Scenario.CurrentPlayer) && this.CurrentTroop.Operated == false )
                    {
                        if (!this.Plugins.ContextMenuPlugin.IsShowing && Session.Current.Scenario.IsPlayerControlling())
                        {
                            this.Plugins.ContextMenuPlugin.IsShowing = true;
                            this.Plugins.ContextMenuPlugin.SetCurrentGameObject(this.CurrentTroop);
                            this.Plugins.ContextMenuPlugin.SetMenuKindByName("TroopLeftClick");
                            this.Plugins.ContextMenuPlugin.Prepare(InputManager.NowMouse.X, InputManager.NowMouse.Y, base.viewportSize);
                            this.bianduiLiebiaoBiaoji="TroopLeftClick";
                            if (!this.Plugins.ContextMenuPlugin.IsShowing && (this.CurrentTroop.CutRoutewayDays > 0))
                            {
                                this.CurrentTroop.Leader.TextDestinationString = this.CurrentTroop.CutRoutewayDays.ToString();
                                this.Plugins.tupianwenziPlugin.SetConfirmationDialog(this.Plugins.ConfirmationDialogPlugin, new GameDelegates.VoidFunction(this.CurrentTroop.StopCutRouteway), null);
                                this.Plugins.ConfirmationDialogPlugin.SetPosition(ShowPosition.Center);
                                this.Plugins.tupianwenziPlugin.SetGameObjectBranch(this.CurrentTroop.Leader, this.CurrentTroop.Leader, "StopCutRouteway");
                                this.Plugins.tupianwenziPlugin.IsShowing = true;
                            }
                        }
                    }
                    else if (((this.CurrentArchitecture != null) && (this.CurrentArchitecture.BelongedFaction == Session.Current.Scenario.CurrentPlayer)) && !(this.Plugins.ContextMenuPlugin.IsShowing || !Session.Current.Scenario.IsPlayerControlling()))
                    {
                        this.Plugins.ContextMenuPlugin.IsShowing = true;
                        this.Plugins.ContextMenuPlugin.SetCurrentGameObject(this.CurrentArchitecture);
                        this.Plugins.ContextMenuPlugin.SetMenuKindByName("ArchitectureLeftClick");
                        this.Plugins.ContextMenuPlugin.Prepare(InputManager.NowMouse.X, InputManager.NowMouse.Y, base.viewportSize);
                        
                        this.bianduiLiebiaoBiaoji = "ArchitectureLeftClick";
                        this.ShowBianduiLiebiao(UndoneWorkKind.None, FrameKind.Military, FrameFunction.Browse , false, true, false ,true,
                            this.CurrentArchitecture.Militaries, this.CurrentArchitecture.ZhengzaiBuchongDeBiandui(), "", "", this.CurrentArchitecture.MilitaryPopulation);
                        this.ShowArchitectureSurveyPlugin(this.CurrentArchitecture);
                    }
                }
            }
            */
        }

        private void HandleLaterMouseLeftUp()
        {
            if (Session.Current.Scenario.CurrentPlayer == null||this.editMode) return;

            if (this.Plugins.youcelanPlugin.IsShowing && StaticMethods.PointInRectangle(this.MousePosition, this.Plugins.youcelanPlugin.FrameRectangle))
            {
                return;
            }

            //if ((this.previousMouseState.LeftButton == ButtonState.Pressed) && (InputManager.NowMouse.LeftButton == ButtonState.Released) && (this.viewMove == ViewMove.Stop))
            if (InputManager.IsDownPre && InputManager.IsReleased && this.viewMove == ViewMove.Stop)
            {
                if (((Session.GlobalVariables.SkyEye || Session.Current.Scenario.NoCurrentPlayer) || Session.Current.Scenario.CurrentPlayer.IsPositionKnown(this.position)) && ((this.Plugins.ContextMenuPlugin != null) && (this.PeekUndoneWork().Kind == UndoneWorkKind.None)))
                {
                    // Assign the current mouse position so that selectorStartPosition can cache the selected target's accurate coordinates
                    this.SelectorStartPosition = base.MousePosition;
                    this.updateGameScreenByCurrentTarget();
                }
            }
        }

        private void HandleLaterMouseMove()
        {
            /*
            if ((InputManager.NowMouse.X != this.previousMouseState.X) || (InputManager.NowMouse.Y != this.previousMouseState.Y))
            {
                this.UpdateViewMove();
                if ((InputManager.NowMouse.LeftButton != ButtonState.Pressed) && (InputManager.NowMouse.RightButton != ButtonState.Pressed))
                {
                }
            }
            */

            if (InputManager.IsPosChanged)
            {
                this.UpdateViewMove();
                if (this.editMode)
                {
                    if (InputManager.IsDown && (InputManager.NowMouse.RightButton != ButtonState.Pressed))
                    {
                        int x = (InputManager.PoX - this.mainMapLayer.LeftEdge) / Session.Current.Scenario.ScenarioMap.TileWidth;
                        int y = (InputManager.PoY - this.mainMapLayer.TopEdge) / Session.Current.Scenario.ScenarioMap.TileHeight;
                        if (Session.Current.Scenario.ScenarioMap.MapData[x, y] != this.ditukuaidezhi)
                        {
                            Session.Current.Scenario.ScenarioMap.MapData[x, y] = this.ditukuaidezhi;
                            this.mainMapLayer.chongsheditukuaitupian(x, y);

                        }
                    }
                    if (InputManager.IsDown && (InputManager.NowMouse.RightButton == ButtonState.Pressed))
                    {
                        int x = (InputManager.PoX - this.mainMapLayer.LeftEdge) / Session.Current.Scenario.ScenarioMap.TileWidth;
                        int y = (InputManager.PoY - this.mainMapLayer.TopEdge) / Session.Current.Scenario.ScenarioMap.TileHeight;
                        if (Session.Current.Scenario.ScenarioMap.MapData[x, y] != 0)
                        {
                            Session.Current.Scenario.ScenarioMap.MapData[x, y] = 0;
                            this.mainMapLayer.chongsheditukuaitupian(x, y);

                        }
                    }
                }
            }


        }

        private void HandleLaterMouseRightDown()
        {
            if (this.editMode)
            {
                if ((InputManager.MouseStatePre.RightButton == ButtonState.Released) && (InputManager.NowMouse.RightButton == ButtonState.Pressed))
                {
                    int x = (InputManager.NowMouse.X - this.mainMapLayer.LeftEdge) / Session.Current.Scenario.ScenarioMap.TileWidth;
                    int y = (InputManager.NowMouse.Y - this.mainMapLayer.TopEdge) / Session.Current.Scenario.ScenarioMap.TileHeight;
                    Session.Current.Scenario.ScenarioMap.MapData[x, y] = 0;
                    this.mainMapLayer.chongsheditukuaitupian(x, y);
                }
            }
            else
            {

                GameDelegates.VoidFunction optionFunction = null;
                if ((InputManager.MouseStatePre.RightButton == ButtonState.Released) && (InputManager.NowMouse.RightButton == ButtonState.Pressed))
                {
                    if ((this.Plugins.OptionDialogPlugin != null) && (Session.GlobalVariables.CurrentMapLayer == MapLayerKind.Routeway))
                    {
                        List<Routeway> routewaysByPositionAndFaction = Session.Current.Scenario.GetRoutewaysByPositionAndFaction(this.position, Session.Current.Scenario.CurrentPlayer);
                        List<Routeway> list2 = new List<Routeway>();
                        foreach (Routeway routeway in routewaysByPositionAndFaction)
                        {
                            if ((routeway.StartArchitecture == null) || ((((routeway.DestinationArchitecture != null) && routeway.StartArchitecture.BelongedSection.AIDetail.AutoRun) && !routeway.Building) && (routeway.LastActivePointIndex < 0)))
                            {
                                list2.Add(routeway);
                            }
                        }
                        foreach (Routeway routeway in list2)
                        {
                            routewaysByPositionAndFaction.Remove(routeway);
                        }
                        if ((routewaysByPositionAndFaction.Count > 1) && !Session.Current.Scenario.PositionIsTroop(this.position))
                        {
                            this.Plugins.OptionDialogPlugin.SetStyle("Small");
                            this.Plugins.OptionDialogPlugin.SetTitle("粮道");
                            this.Plugins.OptionDialogPlugin.Clear();
                            this.Plugins.OptionDialogPlugin.SetReturnObjectFunction(new GameDelegates.ObjectFunction(this.RoutewayOptionDialogClickCallback));
                            foreach (Routeway routeway in routewaysByPositionAndFaction)
                            {
                                if (optionFunction == null)
                                {
                                    optionFunction = delegate
                                    {
                                        this.ContextMenuRightClick();
                                    };
                                }
                                this.Plugins.OptionDialogPlugin.AddOption(routeway.DisplayName, routeway, optionFunction);
                            }
                            this.Plugins.OptionDialogPlugin.EndAddOptions();
                            this.Plugins.OptionDialogPlugin.ShowOptionDialog(ShowPosition.Mouse);
                            return;
                        }
                    }
                    this.ContextMenuRightClick();
                }
            }
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
            int num4 = this.mainMapLayer.LeftEdge + ((int)(num * (Session.MainGame.mainGameScreen.mainMapLayer.LeftEdge - InputManager.PoX)));  // InputManager.NowMouse.X)));
            int num5 = this.mainMapLayer.TopEdge + ((int)(num * (Session.MainGame.mainGameScreen.mainMapLayer.TopEdge - InputManager.PoY)));  // InputManager.NowMouse.Y)));
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

        #region 军师双击菜单处理

        /// <summary>
        /// 处理军师双击菜单的鼠标左键点击
        /// </summary>
        private void HandleAdvisorDoubleClickMenu()
        {
            // 检测左键按下
            if (InputManager.IsDown && this.viewMove == ViewMove.Stop)
            {
                DateTime currentTime = DateTime.Now;
                Point currentPosition = new Point(InputManager.PoX, InputManager.PoY);
                
                // 检查是否为双击
                if (IsDoubleClick(currentTime, currentPosition))
                {
                    // 检查是否点击在空白地形上
                    if (IsEmptyTerrainClick(currentPosition))
                    {
                        Console.WriteLine($"[MainGameScreen] 检测到双击空白地形: {currentPosition}");
                        ShowAdvisorContextMenu();
                    }
                }
                
                // 更新最后点击信息
                _lastLeftClickTime = currentTime;
                _lastLeftClickPosition = currentPosition;
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
        /// 显示军师右键菜单（参考ContextMenuRightClick的实现）
        /// </summary>
        private void ShowAdvisorContextMenu()
        {
            if ((this.Plugins.ContextMenuPlugin != null) && (this.PeekUndoneWork().Kind == UndoneWorkKind.None))
            {
                if (!this.Plugins.ContextMenuPlugin.IsShowing)
                {
                    this.Plugins.ContextMenuPlugin.IsShowing = true;
                    this.Plugins.ContextMenuPlugin.SetCurrentGameObject(this);
                    this.Plugins.ContextMenuPlugin.SetMenuKindByName("AdvisorDoubleClick");
                    this.Plugins.ContextMenuPlugin.Prepare(InputManager.PoX, InputManager.PoY, base.viewportSize);
                    this.bianduiLiebiaoBiaoji = "AdvisorDoubleClick";
                    
                    Console.WriteLine("[MainGameScreen] 显示军师双击菜单");
                }
            }
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

                // 5. 检查地图位置是否有效
                var mapPosition = this.GetPositionByPoint(screenPosition);
                if (!IsValidMapPosition(mapPosition))
                {
                    Console.WriteLine($"[MainGameScreen] 地图位置无效");
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
        /// 检查地图位置是否有效
        /// </summary>
        private bool IsValidMapPosition(Point mapPosition)
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return false;

            // 检查是否在地图范围内
            if (mapPosition.X < 0 || mapPosition.Y < 0 ||
                mapPosition.X >= scenario.ScenarioMap.MapDimensions.X ||
                mapPosition.Y >= scenario.ScenarioMap.MapDimensions.Y)
            {
                return false;
            }

            // 所有在地图范围内的地形都允许双击
            return true;
        }







        #endregion


    }

}
    {
        #region 私有字段
        
        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastClickPosition = Point.Zero;
        private const int DOUBLE_CLICK_THRESHOLD_MS = 500; // 双击时间阈值
        private const int DOUBLE_CLICK_DISTANCE_THRESHOLD = 5; // 双击位置阈值
        
        private bool _isMenuVisible = false;
        private List<AdvisorMenuItem> _currentMenuItems;
        
        // 菜单显示相关
        private Rectangle _menuBounds;
        private int _selectedItemIndex = -1;
        private Point _menuPosition;
        
        #endregion

        #region 构造函数

        public AdvisorEmptyTerrainMenuManager()
        {
            _currentMenuItems = new List<AdvisorMenuItem>();
        }

        #endregion

        #region 双击处理

        /// <summary>
        /// 处理在空白地形上的双击（MainGameScreen版本）
        /// </summary>
        public void HandleDoubleClickOnEmptyTerrain(Point clickPosition, Faction currentFaction)
        {
            Console.WriteLine($"[军师菜单] 在空白地形双击: {clickPosition}");

            if (currentFaction == null)
            {
                Console.WriteLine("[军师菜单] 没有当前势力");
                return;
            }

            // 构建军师菜单
            BuildAdvisorMenu(currentFaction);

            // 显示菜单
            if (_currentMenuItems.Count > 0)
            {
                ShowMenu(clickPosition, $"军师管理 - {currentFaction.Name}");
            }
            else
            {
                Console.WriteLine("[军师菜单] 没有可用的菜单项");
            }
        }

        /// <summary>
        /// 构建军师菜单
        /// </summary>
        private void BuildAdvisorMenu(Faction faction)
        {
            _currentMenuItems.Clear();

            // 1. 任命/重新任命军师
            if (faction.AppointAdvisorAvail())
            {
                if (faction.Advisor != null)
                {
                    _currentMenuItems.Add(new AdvisorMenuItem
                    {
                        ID = "ReappointAdvisor",
                        Text = "重新任命军师",
                        Description = $"当前: {faction.AdvisorName}",
                        Action = () => ShowAdvisorCandidateList(faction),
                        IsEnabled = true,
                        Icon = "🔄"
                    });
                }
                else
                {
                    _currentMenuItems.Add(new AdvisorMenuItem
                    {
                        ID = "AppointAdvisor",
                        Text = "任命军师",
                        Description = "选择智力出众的人物",
                        Action = () => ShowAdvisorCandidateList(faction),
                        IsEnabled = true,
                        Icon = "👑"
                    });
                }
            }

            // 2. 罢免军师
            if (faction.Advisor != null)
            {
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "RecallAdvisor",
                    Text = "罢免军师",
                    Description = $"解除 {faction.AdvisorName}",
                    Action = () => RecallAdvisor(faction),
                    IsEnabled = true,
                    Icon = "❌"
                });

                // 分隔线
                _currentMenuItems.Add(new AdvisorMenuItem { IsSeparator = true });

                // 3. 军师建议
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "AdvisorAdvice",
                    Text = "军师建议",
                    Description = "听取战略建议",
                    Action = () => ShowAdvisorAdvice(faction),
                    IsEnabled = true,
                    Icon = "💡"
                });

                // 4. 军师信息
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "AdvisorInfo",
                    Text = "军师信息",
                    Description = "查看详细信息",
                    Action = () => ShowAdvisorInfo(faction),
                    IsEnabled = true,
                    Icon = "ℹ️"
                });
            }

            // 5. 如果没有军师且没有候选人，显示提示
            if (_currentMenuItems.Count == 0)
            {
                _currentMenuItems.Add(new AdvisorMenuItem
                {
                    ID = "NoOptions",
                    Text = "暂无可用选项",
                    Description = "没有合适的军师候选人",
                    Action = null,
                    IsEnabled = false,
                    Icon = "⚠️"
                });
            }
        }

        #endregion

        #region 菜单操作

        /// <summary>
        /// 显示军师候选人列表
        /// </summary>
        private void ShowAdvisorCandidateList(Faction faction)
        {
            var candidates = faction.AdvisorCandicate;
            if (candidates.Count == 0)
            {
                ShowMessage("没有合适的军师候选人");
                return;
            }

            // 使用MainGameScreen的现有UI系统
            var mainGameScreen = Session.MainGame?.mainGameScreen as MainGameScreen;
            if (mainGameScreen != null)
            {
                mainGameScreen.ShowTabListInFrame(
                    UndoneWorkKind.Frame,
                    FrameKind.Person,
                    FrameFunction.AppointAdvisor,
                    false, true, true, false,
                    candidates,
                    null,
                    faction.Advisor != null ? "重新任命军师" : "任命军师",
                    ""
                );
            }

            HideMenu();
        }

        /// <summary>
        /// 罢免军师
        /// </summary>
        private void RecallAdvisor(Faction faction)
        {
            if (faction.Advisor == null)
            {
                ShowMessage("当前没有军师");
                return;
            }

            string advisorName = faction.AdvisorName;
            
            // 执行罢免
            faction.AdvisorID = -1;
            faction.Advisor = null;
            
            ShowMessage($"已罢免军师 {advisorName}");
            HideMenu();
        }

        /// <summary>
        /// 显示军师建议
        /// </summary>
        private void ShowAdvisorAdvice(Faction faction)
        {
            if (faction.Advisor == null)
            {
                ShowMessage("当前没有军师");
                return;
            }

            // 这里可以集成你的军师建议系统
            ShowMessage($"军师 {faction.AdvisorName} 的建议：\n\n" +
                       "1. 加强内政建设\n" +
                       "2. 招募更多人才\n" +
                       "3. 与邻国保持友好关系");
            
            HideMenu();
        }

        /// <summary>
        /// 显示军师信息
        /// </summary>
        private void ShowAdvisorInfo(Faction faction)
        {
            if (faction.Advisor == null)
            {
                ShowMessage("当前没有军师");
                return;
            }

            var advisor = faction.Advisor;
            string info = $"军师信息：\n\n" +
                         $"姓名: {advisor.Name}\n" +
                         $"智力: {advisor.Intelligence}\n" +
                         $"政治: {advisor.Politics}\n" +
                         $"魅力: {advisor.Charm}\n" +
                         $"所在地: {advisor.LocationArchitecture?.Name ?? "未知"}";

            ShowMessage(info);
            HideMenu();
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        private void ShowMessage(string message)
        {
            Console.WriteLine($"[军师系统] {message}");
            // 这里可以集成实际的消息显示系统
        }

        #endregion

        #region 菜单显示和交互

        /// <summary>
        /// 显示菜单
        /// </summary>
        private void ShowMenu(Point position, string title)
        {
            if (_currentMenuItems.Count == 0) return;

            // 计算菜单大小
            int menuWidth = 220;
            int itemHeight = 30;
            int titleHeight = 35;
            int menuHeight = _currentMenuItems.Count * itemHeight + titleHeight + 10;

            // 调整菜单位置，确保不超出屏幕
            var screenBounds = new Rectangle(0, 0, 1920, 1080); // 根据实际屏幕大小调整
            int x = Math.Min(position.X, screenBounds.Width - menuWidth);
            int y = Math.Min(position.Y, screenBounds.Height - menuHeight);

            // 确保菜单不会太靠近边缘
            x = Math.Max(10, x);
            y = Math.Max(10, y);

            _menuBounds = new Rectangle(x, y, menuWidth, menuHeight);
            _menuPosition = new Point(x, y);
            _isMenuVisible = true;
            _selectedItemIndex = -1;

            Console.WriteLine($"[军师菜单] 显示菜单: {title}, 位置: ({x}, {y}), 项目数: {_currentMenuItems.Count}");
        }

        /// <summary>
        /// 隐藏菜单
        /// </summary>
        public void HideMenu()
        {
            if (_isMenuVisible)
            {
                _isMenuVisible = false;
                _selectedItemIndex = -1;
                Console.WriteLine("[军师菜单] 隐藏菜单");
            }
        }

        /// <summary>
        /// 处理菜单鼠标移动
        /// </summary>
        public void HandleMenuMouseMove(Point mousePosition)
        {
            if (!_isMenuVisible) return;

            // 检查鼠标是否在菜单内
            if (!_menuBounds.Contains(mousePosition))
            {
                _selectedItemIndex = -1;
                return;
            }

            // 计算鼠标在哪个菜单项上
            int titleHeight = 35;
            int relativeY = mousePosition.Y - _menuBounds.Y - titleHeight;
            int itemIndex = relativeY / 30; // 每项高度30

            if (itemIndex >= 0 && itemIndex < _currentMenuItems.Count)
            {
                var item = _currentMenuItems[itemIndex];
                if (!item.IsSeparator && item.IsEnabled)
                {
                    _selectedItemIndex = itemIndex;
                }
                else
                {
                    _selectedItemIndex = -1;
                }
            }
            else
            {
                _selectedItemIndex = -1;
            }
        }

        /// <summary>
        /// 处理菜单点击
        /// </summary>
        public void HandleMenuClick(Point mousePosition)
        {
            if (!_isMenuVisible) return;

            // 检查是否点击在菜单内
            if (!_menuBounds.Contains(mousePosition))
            {
                HideMenu();
                return;
            }

            // 执行选中的菜单项
            if (_selectedItemIndex >= 0 && _selectedItemIndex < _currentMenuItems.Count)
            {
                var selectedItem = _currentMenuItems[_selectedItemIndex];
                if (!selectedItem.IsSeparator && selectedItem.IsEnabled && selectedItem.Action != null)
                {
                    Console.WriteLine($"[军师菜单] 执行菜单项: {selectedItem.Text}");
                    selectedItem.Action.Invoke();
                }
            }
        }

        #endregion

        #region 属性

        /// <summary>
        /// 菜单是否可见
        /// </summary>
        public bool IsMenuVisible => _isMenuVisible;

        /// <summary>
        /// 当前菜单边界
        /// </summary>
        public Rectangle MenuBounds => _menuBounds;

        /// <summary>
        /// 当前菜单项
        /// </summary>
        public IReadOnlyList<AdvisorMenuItem> CurrentMenuItems => _currentMenuItems.AsReadOnly();

        /// <summary>
        /// 选中的菜单项索引
        /// </summary>
        public int SelectedItemIndex => _selectedItemIndex;

        #endregion
    }

    /// <summary>
    /// 军师菜单项
    /// </summary>
    public class AdvisorMenuItem
    {
        public string ID { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsSeparator { get; set; } = false;
        public Action Action { get; set; }
    }

    #endregion

    #region 简单双击处理器

    /// <summary>
    /// 简单双击处理器
    /// </summary>
    public class SimpleDoubleClickHandler
    {
        private static SimpleDoubleClickHandler _instance;
        public static SimpleDoubleClickHandler Instance => _instance ??= new SimpleDoubleClickHandler();

        // 双击时间阈值（毫秒）
        private const int DOUBLE_CLICK_INTERVAL = 400;
        
        // 双击位置容差（像素）
        private const int DOUBLE_CLICK_TOLERANCE = 10;
        
        // 记录上一次点击的时间和位置
        private DateTime _lastClickTime = DateTime.MinValue;
        private Point? _lastClickPosition;
        
        // 事件
        public event Action<Point> OnDoubleClick;
        public event Action<Point> OnLeftClick;

        private SimpleDoubleClickHandler()
        {
        }

        /// <summary>
        /// 处理鼠标左键点击
        /// </summary>
        public void HandleLeftClick(Point clickPosition)
        {
            DateTime currentTime = DateTime.Now;
            
            // 触发单击事件
            OnLeftClick?.Invoke(clickPosition);
            
            // 检查是否是双击
            if (_lastClickPosition.HasValue)
            {
                var timeElapsed = (currentTime - _lastClickTime).TotalMilliseconds;
                var positionDelta = Math.Sqrt(
                    Math.Pow(clickPosition.X - _lastClickPosition.Value.X, 2) +
                    Math.Pow(clickPosition.Y - _lastClickPosition.Value.Y, 2)
                );
                
                // 判断是否为双击：时间间隔小且位置相近
                if (timeElapsed < DOUBLE_CLICK_INTERVAL && positionDelta < DOUBLE_CLICK_TOLERANCE)
                {
                    // 触发双击事件
                    OnDoubleClick?.Invoke(clickPosition);
                    
                    // 重置双击检测
                    ResetDoubleClickDetection();
                    return;
                }
            }
            
            // 记录这次点击
            _lastClickPosition = clickPosition;
            _lastClickTime = currentTime;
        }

        /// <summary>
        /// 重置双击检测
        /// </summary>
        private void ResetDoubleClickDetection()
        {
            _lastClickPosition = null;
            _lastClickTime = DateTime.MinValue;
        }
    }

    #endregion
}
