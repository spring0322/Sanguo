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
using System.Diagnostics;
using GameManager;

//using GameObjects.PersonDetail.PersonMessages;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    partial class MainGameScreen : Screen
    {

        private void HandleKey(GameTime gameTime)
        {
            if (this.currentKey != Keys.None)
            {
                //if (!base.KeyState.IsKeyUp(this.currentKey))
                if (!InputManager.KeyBoardState.IsKeyUp(this.currentKey))
                {
                    return;
                }
                this.currentKey = Keys.None;
            }
            
            // 编辑模式下的数字键处理（优先处理，不需要CurrentPlayer条件）
            // 支持主键盘数字键(D1-D0)和数字键盘(NumPad1-NumPad0)
            if (this.editMode)
            {
                if (InputManager.KeyBoardState.IsKeyDown(Keys.D1) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad1))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D1) ? Keys.D1 : Keys.NumPad1;
                    this.ditukuaidezhi = 1;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 1");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D2) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad2))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D2) ? Keys.D2 : Keys.NumPad2;
                    this.ditukuaidezhi = 2;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 2");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D3) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad3))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D3) ? Keys.D3 : Keys.NumPad3;
                    this.ditukuaidezhi = 3;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 3");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D4) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad4))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D4) ? Keys.D4 : Keys.NumPad4;
                    this.ditukuaidezhi = 4;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 4");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D5) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad5))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D5) ? Keys.D5 : Keys.NumPad5;
                    this.ditukuaidezhi = 5;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 5");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D6) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad6))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D6) ? Keys.D6 : Keys.NumPad6;
                    this.ditukuaidezhi = 6;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 6");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D7) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad7))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D7) ? Keys.D7 : Keys.NumPad7;
                    this.ditukuaidezhi = 7;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 7");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D8) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad8))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D8) ? Keys.D8 : Keys.NumPad8;
                    this.ditukuaidezhi = 8;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 8");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D9) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad9))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D9) ? Keys.D9 : Keys.NumPad9;
                    this.ditukuaidezhi = 9;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 9");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D0) || InputManager.KeyBoardState.IsKeyDown(Keys.NumPad0))
                {
                    this.currentKey = InputManager.KeyBoardState.IsKeyDown(Keys.D0) ? Keys.D0 : Keys.NumPad0;
                    this.ditukuaidezhi = 10;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 选择地形类型: 10");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.T))
                {
                    this.currentKey = Keys.T;
                    this.mainMapLayer.xianshidituxiaokuai = !this.mainMapLayer.xianshidituxiaokuai;
                    // System.Diagnostics.Debug.WriteLine($"[TerrainEdit] 切换地形显示: {this.mainMapLayer.xianshidituxiaokuai}");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.LeftAlt) && InputManager.KeyBoardState.IsKeyDown(Keys.F) && Session.GlobalVariables.EnableCheat)
                {
                    this.currentKey = Keys.F;
                    this.editMode = false;
                    this.mainMapLayer.xianshidituxiaokuai = false;
                    this.Plugins.youcelanPlugin.IsShowing = true;
                    this.mapEdited = true;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 退出地形编辑模式");
                }
                // 在编辑模式下，跳过其他按键处理
                return;
            }

            if (Session.Current?.Scenario == null)
            {
                return;
            }

            var currentPlayer = Session.Current.Scenario.CurrentPlayer;
            if (currentPlayer == null && !Session.Current.Scenario.IsObserverModeActive())
            {
                return;
            }
            
            // 🛡️ 新增：防弹检查
            // 如果剧本还没加载好，或者当前没有玩家，直接跳过按键处理，不要崩！
            if (currentPlayer == null && !Session.Current.Scenario.IsObserverModeActive()) 
            {
                return; // 直接不再往下执行，等待下一帧数据加载好
            }
            
            // 原来的逻辑放在下面
            if (currentPlayer == null && Session.Current.Scenario.IsObserverModeActive())
            {
                if (InputManager.KeyBoardState.IsKeyDown(Keys.D1))
                {
                    this.currentKey = Keys.D1;
                    this.DateGo(1);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D2))
                {
                    this.currentKey = Keys.D2;
                    this.DateGo(2);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D3))
                {
                    this.currentKey = Keys.D3;
                    this.DateGo(3);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D4))
                {
                    this.currentKey = Keys.D4;
                    this.DateGo(4);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D5))
                {
                    this.currentKey = Keys.D5;
                    this.DateGo(5);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D6))
                {
                    this.currentKey = Keys.D6;
                    this.DateGo(6);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D7))
                {
                    this.currentKey = Keys.D7;
                    this.DateGo(7);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D8))
                {
                    this.currentKey = Keys.D8;
                    this.DateGo(8);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D9))
                {
                    this.currentKey = Keys.D9;
                    this.DateGo(9);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D0))
                {
                    this.currentKey = Keys.D0;
                    this.DateGo(10);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.F1))
                {
                    this.currentKey = Keys.F1;
                    this.DateGo(30);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.F2))
                {
                    this.currentKey = Keys.F2;
                    this.DateGo(60);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.F3))
                {
                    this.currentKey = Keys.F3;
                    this.DateGo(90);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.F5))
                {
                    this.currentKey = Keys.F5;
                    this.DateGo(-999);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.Q))
                {
                    this.currentKey = Keys.Q;
                    Session.GlobalVariables.ShowGrid = !Session.GlobalVariables.ShowGrid;
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.LeftAlt) && InputManager.KeyBoardState.IsKeyDown(Keys.C) && Session.GlobalVariables.EnableCheat)
                {
                    this.currentKey = Keys.C;
                    changeFaction();
                }
                return;
            }

            if (currentPlayer != null && currentPlayer.Controlling)
            {
                if (InputManager.KeyBoardState.IsKeyDown(Keys.D1))
                {
                    this.currentKey = Keys.D1;
                    this.DateGo(1);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D2))
                {
                    this.currentKey = Keys.D2;
                    this.DateGo(2);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D3))
                {
                    this.currentKey = Keys.D3;
                    this.DateGo(3);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D4))
                {
                    this.currentKey = Keys.D4;
                    this.DateGo(4);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D5))
                {
                    this.currentKey = Keys.D5;
                    this.DateGo(5);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D6))
                {
                    this.currentKey = Keys.D6;
                    this.DateGo(6);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D7))
                {
                    this.currentKey = Keys.D7;
                    this.DateGo(7);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D8))
                {
                    this.currentKey = Keys.D8;
                    this.DateGo(8);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D9))
                {
                    this.currentKey = Keys.D9;
                    this.DateGo(9);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.D0))
                {
                    this.currentKey = Keys.D0;
                    this.DateGo(10);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.F1))
                {
                    this.currentKey = Keys.F1;
                    this.DateGo(30);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.F2))
                {
                    this.currentKey = Keys.F2;
                    this.DateGo(60);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.F3))
                {
                    this.currentKey = Keys.F3;
                    this.DateGo(90);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.F5))
                {
                    this.currentKey = Keys.F5;
                    this.DateGo(-999);
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.Q))
                {
                    this.currentKey = Keys.Q;
                    Session.GlobalVariables.ShowGrid = !Session.GlobalVariables.ShowGrid;
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.LeftAlt) && InputManager.KeyBoardState.IsKeyDown(Keys.C) && Session.GlobalVariables.EnableCheat)
                {
                    this.currentKey = Keys.C;
                    changeFaction();
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.LeftAlt) && InputManager.KeyBoardState.IsKeyDown(Keys.E) && Session.GlobalVariables.EnableCheat)
                {
                    this.currentKey = Keys.E;
                    this.editMode = true;
                    this.mainMapLayer.xianshidituxiaokuai = true;
                    this.Plugins.youcelanPlugin.IsShowing = false;
                    this.mapEdited = true;
                    // System.Diagnostics.Debug.WriteLine("[TerrainEdit] 进入地形编辑模式");
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.T))
                {
                    this.currentKey = Keys.T;
                    this.ShowArchitectureConnectedLine = !this.ShowArchitectureConnectedLine;
                }
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.L))
                {
                    this.currentKey = Keys.L;
                    this.ShowArchitectureConnectedLine = !this.ShowArchitectureConnectedLine;
                }
                // 🗺️ F12 切换势力范围渲染（2026-03-11）
                else if (InputManager.KeyBoardState.IsKeyDown(Keys.F12))
                {
                    this.currentKey = Keys.F12;
                    _influenceRenderer?.Toggle();
                    if (_inkRenderer != null)
                    {
                        _inkRenderer.IsEnabled = !_inkRenderer.IsEnabled;
                    }
                }
                // F12 快捷键已移除 - 只通过菜单激活编辑器
                // else if (InputManager.IsKeyPressed(Keys.F12) && Session.GlobalVariables.EnableCheat)
                // {
                //     this.currentKey = Keys.F12;
                //     if (this.Plugins.InGameEditorPlugin != null)
                //     {
                //         if (this.Plugins.InGameEditorPlugin.IsShowing)
                //         {
                //             // 如果已显示，则关闭
                //             this.Plugins.InGameEditorPlugin.IsShowing = false;
                //         }
                //         else
                //         {
                //             // 打开编辑器，优先编辑当前选中的城池
                //             if (this.CurrentArchitecture != null)
                //             {
                //                 this.Plugins.InGameEditorPlugin.SetEditTarget(this.CurrentArchitecture);
                //                 this.Plugins.InGameEditorPlugin.SetPosition(ShowPosition.Center);
                //                 this.Plugins.InGameEditorPlugin.IsShowing = true;
                //             }
                //             else if (this.CurrentTroop != null)
                //             {
                //                 this.Plugins.InGameEditorPlugin.SetEditTarget(this.CurrentTroop);
                //                 this.Plugins.InGameEditorPlugin.SetPosition(ShowPosition.Center);
                //                 this.Plugins.InGameEditorPlugin.IsShowing = true;
                //             }
                //         }
                //     }
                // }
            }
            if (InputManager.KeyBoardState.IsKeyDown(Keys.Space))
            {

                this.currentKey = Keys.Space;
                if (!this.editMode)
                {
                    this.Plugins.DateRunnerPlugin.Run();
                }
            }
            else if (InputManager.KeyBoardState.IsKeyDown(Keys.W))
            {
                this.currentKey = Keys.W;
            }
            else if (InputManager.KeyBoardState.IsKeyDown(Keys.A))
            {
                this.currentKey = Keys.A;
            }
            else if (InputManager.KeyBoardState.IsKeyDown(Keys.S))
            {
                this.currentKey = Keys.S;
            }
            else if (InputManager.KeyBoardState.IsKeyDown(Keys.D))
            {
                this.currentKey = Keys.D;
            }
            else if (InputManager.KeyBoardState.IsKeyDown(Keys.OemPlus) || InputManager.KeyBoardState.IsKeyDown(Keys.Add))
            {
                this.currentKey = Keys.OemPlus;
            }
            else if (InputManager.KeyBoardState.IsKeyDown(Keys.OemMinus) || InputManager.KeyBoardState.IsKeyDown(Keys.Subtract))
            {
                this.currentKey = Keys.OemMinus;
            }
            
            // 启用作弊模式: Ctrl+Shift+Z
            // 使用 IsKeyDown + currentKey 锁闭机制，比 IsKeyPressed 更可靠
            if ((InputManager.KeyBoardState.IsKeyDown(Keys.LeftControl) || InputManager.KeyBoardState.IsKeyDown(Keys.RightControl)) &&
                (InputManager.KeyBoardState.IsKeyDown(Keys.LeftShift) || InputManager.KeyBoardState.IsKeyDown(Keys.RightShift)) &&
                InputManager.KeyBoardState.IsKeyDown(Keys.Z))
            {
                // 锁定当前按键，防止重复触发，直到 Z 键释放
                this.currentKey = Keys.Z;

                // 总是执行，确保用户能看到提示 (无论之前是否开启)
                if (true)
                {
                    Session.GlobalVariables.EnableCheat = !Session.GlobalVariables.EnableCheat; // Toggle status
                    
                    string statusMsg = Session.GlobalVariables.EnableCheat ? "开启" : "关闭";
                    // 强制控制台输出
                    Console.WriteLine($"[MGSKeys] CHEAT MODE {statusMsg}");
                    System.Diagnostics.Debug.WriteLine($"[MGSKeys] 作弊模式已{statusMsg} (Ctrl+Shift+Z)");
                    
                    // 显示提示消息
                    if (this.Plugins?.tupianwenziPlugin != null)
                    {
                        // 尝试使用中立人物(系统)发言，否则使用当前玩家君主
                        var speaker = Session.Current?.Scenario?.NeutralPerson;
                        if (speaker == null) speaker = Session.Current?.Scenario?.CurrentPlayer?.Leader;

                        if (speaker != null)
                        {
                            // 简化文本，避免特殊字符和换行导致的乱码
                            this.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                                speaker, 
                                null, 
                                $"【系统提示】作弊模式已{statusMsg}。", 
                                "", "", "");
                            this.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Center, this);
                            this.Plugins.tupianwenziPlugin.IsShowing = true;
                        }
                    }
                }
            }
        }



    }
}
