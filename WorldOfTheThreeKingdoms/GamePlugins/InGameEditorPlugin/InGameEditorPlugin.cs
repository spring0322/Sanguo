using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection; // 引入反射，用于查找上限属性
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input; 
using Microsoft.Xna.Framework.Graphics;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using GameObjects.TroopDetail;
using GameObjects.PersonDetail;
using GameObjects.FactionDetail;
using GameObjects.ArchitectureDetail;
using GameObjects.Influences;
// using GameObjects.MilitaryDetail; // Removed invalid reference

using PluginInterface;
using PluginInterface.BaseInterface;
using TabListPlugin;
using GameManager; 
using Platforms; 

namespace InGameEditorPlugin
{
    /// <summary>
    /// 游戏内超级编辑器 (Native Integration Version)
    /// </summary>
    public class InGameEditorPlugin : GameObject, IInGameEditor
    {
        private IGameContextMenu contextMenu;
        private InfluenceTable currentInfluenceEditTable;
        private Action currentInfluenceEditCallback;
        private bool isRemovingInfluence;
        private bool isMultiSelectingInfluence;
        private NumberInputerPlugin.NumberInputerPlugin numberInputer;
        private EditorFrame editorFrame;
        
        private bool isLoaded = false;
        private string pluginName = "InGameEditorPlugin";
        private string description = "In-Game Editor with Native UI";
        private string author = "Editor";
        private string version = "2.0";

        // 编辑器开关快捷键 - 已移除，只通过菜单激活
        // private Microsoft.Xna.Framework.Input.Keys ActivationKey = Microsoft.Xna.Framework.Input.Keys.F8; 

        // --- 核心：修改缓冲区 ---
        // Key: 属性名(用于显示), Value: 新数值
        // 我们用 Dictionary 来存储"待保存"的数据
        private Dictionary<string, object> _pendingChanges = new Dictionary<string, object>();
        // 当前正在编辑的对象
        private object _editingTarget;

        // 原始数值备份 - 用于验证失败时恢复
        private Dictionary<string, object> _originalValues = new Dictionary<string, object>(); 

        public string PluginName { get { return pluginName; } }
        public string Description { get { return description; } }
        public string Author { get { return author; } }
        public string Version { get { return version; } }

        public object Instance { get { return this; } }

        public void Dispose() { }

        public void SetGraphicsDevice() 
        { 
            // 加载XML配置
            this.LoadDataFromXMLDocument(@"Content\Data\Plugins\InGameEditorData.xml");
            
            // 确保EditorFrame被创建
            if (editorFrame == null)
            {
                editorFrame = new EditorFrame();
                editorFrame.Initialize();
                editorFrame.OnCancel += () => { 
                    // System.Diagnostics.Debug.WriteLine("[InGameEditor] OnCancel event triggered (SetGraphicsDevice)");
                    
                    // 取消时恢复原始数值
                    if (editorFrame?.EditTarget != null)
                    {
                        // System.Diagnostics.Debug.WriteLine("[InGameEditor] 取消编辑，恢复原始数值");
                        RestoreOriginalValues(editorFrame.EditTarget);
                    }
                    
                    this.IsShowing = false; 
                };
                editorFrame.OnSave += () => { 
                    // System.Diagnostics.Debug.WriteLine("[InGameEditor] OnSave event triggered (SetGraphicsDevice)");
                    
                    // 添加保存前验证
                    if (ValidateBeforeSave())
                    {
                        this.IsShowing = false;
                    }
                    // 如果验证失败，不关闭编辑器，让用户修正数据
                };
            }
            
            System.Diagnostics.Debug.WriteLine("[InGameEditor] SetGraphicsDevice completed");
        }

        public void Initialize(Screen screen) 
        { 
            Initialize(); 
        }

        public void SetScreen(Screen screen) 
        { 
            if (editorFrame != null)
            {
                editorFrame.SetScreen(screen);
            }
        }

        // --- IInGameEditor Interface Implementation ---

        public object EditTarget 
        { 
            get 
            {
                return editorFrame?.EditTarget;
            } 
        }

        public EditorType CurrentEditorType 
        { 
            get 
            {
                // Map internal type if necessary, or just return None if not strictly tracked here
                // We will rely on EditorFrame's state if we exposed it, but for now we can infer
                if (editorFrame == null) return EditorType.None;
                // Since EditorFrame doesn't expose CurrentEditorType directly as public Enum (maybe), 
                // we can just return None or add property to EditorFrame. 
                // For now, let's return None to satisfy interface
                return EditorType.None; 
            }
        }

        public void SetEditTarget(object target)
        {
            // System.Diagnostics.Debug.WriteLine($"[InGameEditor] SetEditTarget called with: {target?.GetType().Name ?? "null"}");
            
            if (editorFrame == null) 
            {
                // System.Diagnostics.Debug.WriteLine("[InGameEditor] EditorFrame is null, cannot set target");
                return;
            }

            // 备份原始数值
            BackupOriginalValues(target);
            
            // Try to infer type
            if (target is Person p) 
            {
                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] Setting Person target: {p.Name}");
                editorFrame.SetEditTarget(p, EditorType.Person);
            }
            else if (target is Architecture a) 
            {
                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] Setting Architecture target: {a.Name}");
                editorFrame.SetEditTarget(a, EditorType.Architecture);
            }
            else if (target is Faction f) 
            {
                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] Setting Faction target: {f.Name}");
                editorFrame.SetEditTarget(f, EditorType.Faction);
            }
            else if (target is Military m) 
            {
                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] Setting Military target: {m.Name}");
                editorFrame.SetEditTarget(m, EditorType.Military);
            }
            else if (target is Troop t) 
            {
                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] Setting Troop target: {t.Name}");
                editorFrame.SetEditTarget(t, EditorType.Troop);
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] Unknown target type: {target?.GetType().Name ?? "null"}");
                return;
            }
            
            // System.Diagnostics.Debug.WriteLine("[InGameEditor] Setting IsShowing = true");
            this.IsShowing = true;
        }

        public void SetPosition(ShowPosition showPosition)
        {
            // Editor handles its own position (centered usually)
        }

        public void SetNumberInputer(INumberInputer iNumberInputer)
        {
            this.numberInputer = iNumberInputer as NumberInputerPlugin.NumberInputerPlugin;
            if (editorFrame != null && this.numberInputer != null)
            {
                editorFrame.SetNumberInputer(this.numberInputer);
            }
        }

        public void SaveChanges()
        {
            // Trigger save on editor frame
            // Since EditorFrame doesn't expose public Save method directly (it has HandleSave via button),
            // We can just leave this empty or implement if needed. 
        }

        public void CancelChanges()
        {
             this.IsShowing = false;
        }

        // ----------------------------------------------
        
        public void LoadDataFromXMLDocument(string filename) 
        {
             if (editorFrame == null)
             {
                 editorFrame = new EditorFrame();
                 editorFrame.Initialize();
                 // 订阅事件
                 editorFrame.OnCancel += () => { 
                     // System.Diagnostics.Debug.WriteLine("[InGameEditor] OnCancel event triggered (LoadDataFromXMLDocument)");
                     
                     // 取消时恢复原始数值
                     if (editorFrame?.EditTarget != null)
                     {
                         // System.Diagnostics.Debug.WriteLine("[InGameEditor] 取消编辑，恢复原始数值");
                         RestoreOriginalValues(editorFrame.EditTarget);
                     }
                     
                     this.IsShowing = false; 
                 };
                 editorFrame.OnSave += () => { 
                     // System.Diagnostics.Debug.WriteLine("[InGameEditor] OnSave event triggered (LoadDataFromXMLDocument)");
                     
                     // 添加保存前验证
                     if (ValidateBeforeSave())
                     {
                         this.IsShowing = false;
                     }
                     // 如果验证失败，不关闭编辑器，让用户修正数据
                 };
             }

             try
             {
                 System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                 string xmlContent = Platform.Current.LoadText(filename);
                 doc.LoadXml(xmlContent);
                 
                 // 获取根节点
                 System.Xml.XmlNode root = doc.SelectSingleNode("InGameEditorData");
                 if (root != null)
                 {
                     editorFrame.LoadFromXML(root);
                 }
             }
             catch
             {
                 // Ignore errors, use defaults
             }
        }
        


        public void Draw() 
        { 
            // 绘制编辑器窗口
            if (editorFrame != null && editorFrame.IsShowing)
            {
                editorFrame.Draw();
            }

            // 绘制数字输入器 (必须确保在最上层)
            if (numberInputer != null && numberInputer.IsShowing)
            {
                numberInputer.Draw();
            }
        }

        public void Initialize()
        {
            // 初始化 EditorFrame
            if (editorFrame == null)
            {
                editorFrame = new EditorFrame();
                editorFrame.Initialize();
                editorFrame.OnCancel += () => { 
                    // System.Diagnostics.Debug.WriteLine("[InGameEditor] OnCancel event triggered (Initialize)");
                    
                    // 取消时恢复原始数值
                    if (editorFrame?.EditTarget != null)
                    {
                        // System.Diagnostics.Debug.WriteLine("[InGameEditor] 取消编辑，恢复原始数值");
                        RestoreOriginalValues(editorFrame.EditTarget);
                    }
                    
                    this.IsShowing = false; 
                };
                editorFrame.OnSave += () => { 
                    // System.Diagnostics.Debug.WriteLine("[InGameEditor] OnSave event triggered (Initialize)");
                    
                    // 添加保存前验证
                    if (ValidateBeforeSave())
                    {
                        this.IsShowing = false;
                    }
                    // 如果验证失败，不关闭编辑器，让用户修正数据
                };
            }

            // 获取游戏原生的 UI 插件
            if (Session.MainGame.mainGameScreen != null)
            {
                try {
                     this.contextMenu = Session.MainGame.mainGameScreen.Plugins.ContextMenuPlugin as ContextMenuPlugin.ContextMenuPlugin;
                     this.numberInputer = Session.MainGame.mainGameScreen.Plugins.NumberInputerPlugin as NumberInputerPlugin.NumberInputerPlugin;

                     // 配置 EditorFrame
                     if (this.numberInputer != null)
                     {
                         editorFrame.SetNumberInputer(this.numberInputer);
                     }
                     if (Session.MainGame.mainGameScreen != null)
                     {
                         editorFrame.SetScreen(Session.MainGame.mainGameScreen);
                     }

                     // 链接右键菜单中的“编辑数据”项 (ID 100 或 名称匹配)
                     if (this.contextMenu != null)
                     {
                         var concreteMenu = this.contextMenu as ContextMenuPlugin.ContextMenuPlugin;
                         if (concreteMenu != null)
                         {
                             bool hooked = false;
                             foreach (var kind in concreteMenu.ContextMenu.MenuKinds)
                         {
                             foreach (var item in kind.MenuItems)
                             {
                                 // 尝试匹配 ID 100 或名称 "EditData" / "数据编辑"
                                 if (item.ID == 100 || item.Name == "EditData" || item.DisplayName.Contains("数据编辑"))
                                 {
                                     item.SelectedAction = () => this.ActivateEditor();
                                     hooked = true;
                                 }
                             }
                         }
                         if (!hooked) System.Diagnostics.Debug.WriteLine("[InGameEditor] Editor will be activated through ContextMenuResult system");
                         }
                     }
                } catch { }

                if (this.contextMenu != null && this.numberInputer != null)
                {
                    isLoaded = true;
                }
            }
        }
        
        private bool isShowing = false;
        public bool IsShowing
        {
            get => isShowing;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[InGameEditor] IsShowing changing from {isShowing} to {value}");
                
                if (isShowing != value)
                {
                    isShowing = value;
                    if (Session.MainGame.mainGameScreen != null)
                    {
                        if (value)
                        {
                            System.Diagnostics.Debug.WriteLine("[InGameEditor] Pushing UndoneWork Dialog");
                            // Push UndoneWork to pause game logic/handle modal state
                            // We use UndoneWorkKind.Dialog to block underlying game input
                            Session.MainGame.mainGameScreen.PushUndoneWork(new UndoneWorkItem(UndoneWorkKind.Dialog, UndoneWorkSubKind.None));
                            
                            // Note: We DO NOT subscribe to Screen events here anymore because UndoneWork might suppress them.
                            // We will handle input explicitly in Update().
                            
                            if (editorFrame != null) 
                            {
                                System.Diagnostics.Debug.WriteLine("[InGameEditor] Setting EditorFrame.IsShowing = true");
                                editorFrame.IsShowing = true;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[InGameEditor] ERROR: EditorFrame is null!");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[InGameEditor] Popping UndoneWork Dialog");
                            // Pop UndoneWork - 安全检查
                            try
                            {
                                var undoneWork = Session.MainGame.mainGameScreen.PeekUndoneWork();
                                if (undoneWork.Kind == UndoneWorkKind.Dialog)
                                {
                                    Session.MainGame.mainGameScreen.PopUndoneWork();
                                    System.Diagnostics.Debug.WriteLine("[InGameEditor] UndoneWork Dialog popped successfully");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[InGameEditor] UndoneWork mismatch: {undoneWork.Kind}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[InGameEditor] Error popping UndoneWork: {ex.Message}");
                            }

                            if (editorFrame != null) 
                            {
                                System.Diagnostics.Debug.WriteLine("[InGameEditor] Setting EditorFrame.IsShowing = false");
                                editorFrame.IsShowing = false;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[InGameEditor] ERROR: Session.MainGame.mainGameScreen is null!");
                    }
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            if (!isLoaded || this.contextMenu == null || this.numberInputer == null) 
            {
                Initialize();
            }

            // 更新EditorFrame
            if (editorFrame != null)
            {
                editorFrame.Update(gameTime);
            }

            // Explicit Input Handling for EditorFrame when showing
            if (isShowing && editorFrame != null && editorFrame.IsShowing)
            {
                // 1. Handle keyboard (including shortcuts)
                editorFrame.HandleKeyboard();

                // 2. Handle Mouse Input Explicitly via InputManager
                Point mousePos = new Point(InputManager.PoX, InputManager.PoY);

                // Mouse Move
                editorFrame.HandleMouseMove(mousePos);

                // Mouse Click (Left Button Up)
                // Use InputManager.IsReleased for "Click" behavior
                if (InputManager.IsReleased) 
                {
                    System.Diagnostics.Debug.WriteLine($"[InGameEditor] Mouse click at ({mousePos.X}, {mousePos.Y})");
                    bool handled = editorFrame.HandleMouseClick(mousePos);
                    System.Diagnostics.Debug.WriteLine($"[InGameEditor] Mouse click handled: {handled}");
                }

                // Scroll Wheel
                int scrollDelta = InputManager.NowMouse.ScrollWheelValue - InputManager.MouseStatePre.ScrollWheelValue;
                if (scrollDelta != 0)
                {
                    editorFrame.HandleMouseScroll(scrollDelta);
                }

                // Right Click to close
                if (InputManager.NowMouse.RightButton == ButtonState.Pressed && 
                    InputManager.MouseStatePre.RightButton == ButtonState.Released)
                {
                    System.Diagnostics.Debug.WriteLine("[InGameEditor] Right click detected, closing editor");
                    this.IsShowing = false;
                }
            }

            // 快捷键已移除 - 只通过菜单激活缓冲区编辑器
            // if (InputManager.KeyBoardState.IsKeyDown(ActivationKey) && InputManager.KeyBoardStatePre.IsKeyUp(ActivationKey))
            // {
            //     System.Diagnostics.Debug.WriteLine("[InGameEditor] F8 key pressed, activating buffered editor");
            //     // 检查作弊模式是否开启
            //     if (!Session.GlobalVariables.EnableCheat)
            //     {
            //         ShowAlert("请先按 Ctrl+Shift+Z 开启作弊模式");
            //         return;
            //     }
            //     ActivateBufferedEditor();
            // }
        }

        /// <summary>
        /// 激活编辑器主入口
        /// </summary>
        private void ActivateEditor()
        {
            if (editorFrame == null) return;
            if (contextMenu == null) return;

            // 1. 检查 Scenario 是否加载
            if (Session.Current?.Scenario == null) 
            {
                System.Diagnostics.Debug.WriteLine("[InGameEditor] Scenario not loaded, cannot activate editor");
                return;
            }

            // 2. 暂停游戏时间推进，防止编辑时游戏逻辑继续运行导致数据混乱
            try
            {
                var dateRunner = Session.MainGame?.mainGameScreen?.Plugins?.DateRunnerPlugin;
                if (dateRunner != null)
                {
                    dateRunner.Pause();
                    // System.Diagnostics.Debug.WriteLine("[InGameEditor] Game paused for editing");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InGameEditor] Failed to pause game: {ex.Message}");
            }

            // 3. 获取目标对象
            var currentObj = contextMenu.CurrentGameObject;
            
            // 4. 备用获取方式
            if (currentObj == null)
            {
                Point mousePos = Session.MainGame.mainGameScreen.MousePosition;
                Point mapCoordinates = Session.MainGame.mainGameScreen.GetPositionByPoint(mousePos);
                
                // 边界检查
                if (mapCoordinates.X >= 0 && mapCoordinates.Y >= 0 &&
                    mapCoordinates.X < Session.Current.Scenario.ScenarioMap.MapDimensions.X &&
                    mapCoordinates.Y < Session.Current.Scenario.ScenarioMap.MapDimensions.Y)
                {
                    // 🔥 修复 NullReferenceException：添加 MapTileData 空检查
                    if (Session.Current.Scenario.MapTileData != null)
                    {
                        try
                        {
                            var tile = Session.Current.Scenario.MapTileData[mapCoordinates.X, mapCoordinates.Y];
                            currentObj = (object)tile.TileTroop ?? (object)tile.TileArchitecture;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[InGameEditor] MapTileData 访问异常: {ex.Message}");
                        }
                    }
                }
                
                if (currentObj == null)
                {
                    return; 
                }
            }

            // 5. 识别类型并打开编辑器
            if (currentObj is Person person)
            {
                editorFrame.SetEditTarget(person, EditorType.Person);
            }
            else if (currentObj is Architecture arch)
            {
                editorFrame.SetEditTarget(arch, EditorType.Architecture);
            }
            else if (currentObj is Troop troop)
            {
                editorFrame.SetEditTarget(troop, EditorType.Troop);
            }
            else if (currentObj is Faction faction)
            {
                editorFrame.SetEditTarget(faction, EditorType.Faction);
            }
            else if (currentObj is Military military)
            {
                editorFrame.SetEditTarget(military, EditorType.Military);
            }
            else 
            {
                return; // 不支持的类型
            }

            // 确保显示
            this.IsShowing = true;
        }

        #region 增强功能：验证与消息提示

        /// <summary>
        /// 显示简单的文本提示框 (利用 ContextMenu 模拟)
        /// </summary>
        private void ShowAlert(string message, Action onOk = null)
        {
            if (contextMenu == null) return;

            // 1. 清空当前菜单
            contextMenu.IsShowing = false;
            
            // 2. 使用游戏内对话框系统显示消息
            try
            {
                var tupianwenzi = Session.MainGame?.mainGameScreen?.Plugins?.tupianwenziPlugin;
                if (tupianwenzi != null)
                {
                    // 使用中立人物(系统)发言
                    var speaker = Session.Current?.Scenario?.NeutralPerson;
                    if (speaker == null) speaker = Session.Current?.Scenario?.CurrentPlayer?.Leader;

                    if (speaker != null)
                    {
                        tupianwenzi.SetGameObjectBranch(
                            speaker, 
                            null, 
                            $"【编辑器提示】{message}", 
                            "", "", "");
                        tupianwenzi.SetPosition(ShowPosition.Center, Session.MainGame.mainGameScreen);
                        tupianwenzi.IsShowing = true;
                    }
                }
                
                // 执行回调
                onOk?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InGameEditor] ShowAlert error: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示验证错误警告 (保持缓冲区数据，允许用户修正后重新保存)
        /// </summary>
        private void ShowValidationAlert(string message)
        {
            if (contextMenu == null) return;

            // 临时保存当前的菜单状态，显示警告后允许用户返回继续编辑
            contextMenu.ClearFunctions();
            contextMenu.AddMenu("⚠️ 数据验证错误", null);
            
            // 简单的换行处理
            if (message.Contains("\n"))
            {
                string[] lines = message.Split('\n');
                foreach (var line in lines) 
                {
                    if (!string.IsNullOrEmpty(line.Trim()))
                        contextMenu.AddMenu(line, null);
                }
            }
            else
            {
                contextMenu.AddMenu(message, null);
            }
            
            contextMenu.AddMenu("", null); // 空行分隔
            contextMenu.AddMenu("[ 知道了 ]", () => {
                // 关闭警告，关闭菜单
                contextMenu.IsShowing = false;
                // 注意：pendingChanges 依然保留，玩家可以重新打开编辑器修改正确的值
            });
            
            contextMenu.IsShowing = true;
        }

        /// <summary>
        /// 安全执行修改 (带验证逻辑)
        /// </summary>
        /// <param name="newValue">新值</param>
        /// <param name="validator">验证函数：返回 null 表示通过，返回字符串表示错误信息</param>
        /// <param name="action">实际执行修改的函数</param>
        /// <param name="correlation">关联更新函数 (可选)</param>
        private void SafeExecute(int newValue, Func<int, string> validator, Action<int> action, Action correlation = null)
        {
            // 1. 执行验证
            string errorMsg = validator?.Invoke(newValue);

            if (errorMsg != null)
            {
                // 2. 验证失败，显示警告
                ShowAlert(errorMsg, () => {
                    // 点击确定后，可以重新打开输入器让玩家重填，或者直接关闭
                    // 这里选择直接关闭，让玩家自己决定
                });
            }
            else
            {
                // 3. 验证通过，执行修改
                action?.Invoke(newValue);

                // 4. 执行自动关联 (如：改了统率，自动更新部队攻击力)
                correlation?.Invoke();
                
                // (可选) 播放成功音效
                // Session.MainGame.PlaySound("System_Ok");
            }
        }

        #endregion

        /// <summary>
        /// 呼出数字输入器 (增强版：带验证和范围保护)
        /// </summary>
        /// <param name="currentVal">当前值</param>
        /// <param name="maxVal">最大值</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="validator">验证逻辑 (可选)</param>
        /// <param name="correlation">关联逻辑 (可选)</param>
        private void OpenNumberInputer(int currentVal, int maxVal, Action<int> onConfirm, 
                                       Func<int, string> validator = null, Action correlation = null)
        {
            if (numberInputer == null) return;

            // --- 第一道锁：UI 限制 ---
            numberInputer.SetMax(maxVal);
            numberInputer.Number = currentVal;  // 使用 Number 属性设置初始值
            numberInputer.SetDepthOffset(-0.2f);

            // --- 第二道锁：逻辑强制截断 + 现有验证系统 ---
            numberInputer.SetEnterFunction(() => {
                // 1. 获取玩家输入的数字 (可能超出范围)
                int inputVal = numberInputer.Number;
                
                // 2. 强制执行范围检查 (Clamping) - 在验证之前进行
                bool wasModified = false;
                
                if (inputVal > maxVal) 
                {
                    inputVal = maxVal; // 超过上限强制设为上限
                    wasModified = true;
                }
                
                if (inputVal < 0) 
                {
                    inputVal = 0; // 防止负数
                    wasModified = true;
                }

                // 3. 如果数值被修正，记录日志
                if (wasModified)
                {
                    System.Diagnostics.Debug.WriteLine($"[Editor] 输入值被修正: 超范围 -> {inputVal} (范围: 0-{maxVal})");
                }
                
                // 4. 使用 SafeExecute 包装，保持现有验证系统
                SafeExecute(inputVal, validator, onConfirm, correlation);
            });

            if (contextMenu != null)
            {
                contextMenu.IsShowing = false;
            }
            numberInputer.IsShowing = true;
        }

        #region 保存前验证逻辑

        /// <summary>
        /// 备份原始数值
        /// </summary>
        private void BackupOriginalValues(object target)
        {
            _originalValues.Clear();
            
            if (target == null) return;

            try
            {
                if (target is Architecture arch)
                {
                    _originalValues["Fund"] = arch.Fund;
                    _originalValues["Food"] = arch.Food;
                    _originalValues["Population"] = arch.Population;
                    _originalValues["Morale"] = arch.Morale;
                    _originalValues["Endurance"] = arch.Endurance;
                    _originalValues["Agriculture"] = arch.Agriculture;
                    _originalValues["Commerce"] = arch.Commerce;
                    _originalValues["Technology"] = arch.Technology;
                    _originalValues["Domination"] = arch.Domination;
                }
                else if (target is Person person)
                {
                    _originalValues["Command"] = person.Command;
                    _originalValues["Strength"] = person.Strength;
                    _originalValues["Intelligence"] = person.Intelligence;
                    _originalValues["Politics"] = person.Politics;
                    _originalValues["Glamour"] = person.Glamour;
                }
                else if (target is Troop troop)
                {
                    _originalValues["Army"] = troop.Army.Quantity;
                    _originalValues["Morale"] = troop.Morale;
                    _originalValues["Food"] = troop.Food;
                }
                else if (target is Faction faction)
                {
                    _originalValues["Reputation"] = faction.Reputation;
                    _originalValues["TechniquePoint"] = faction.TechniquePoint;
                }

                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] 已备份 {_originalValues.Count} 个原始数值");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] 备份原始数值失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复原始数值
        /// </summary>
        private void RestoreOriginalValues(object target)
        {
            if (target == null || _originalValues.Count == 0) return;

            try
            {
                if (target is Architecture arch)
                {
                    if (_originalValues.ContainsKey("Fund")) arch.Fund = (int)_originalValues["Fund"];
                    if (_originalValues.ContainsKey("Food")) arch.Food = (int)_originalValues["Food"];
                    if (_originalValues.ContainsKey("Population")) arch.Population = (int)_originalValues["Population"];
                    if (_originalValues.ContainsKey("Morale")) arch.Morale = (int)_originalValues["Morale"];
                    if (_originalValues.ContainsKey("Endurance")) arch.Endurance = (int)_originalValues["Endurance"];
                    if (_originalValues.ContainsKey("Agriculture")) arch.Agriculture = (int)_originalValues["Agriculture"];
                    if (_originalValues.ContainsKey("Commerce")) arch.Commerce = (int)_originalValues["Commerce"];
                    if (_originalValues.ContainsKey("Technology")) arch.Technology = (int)_originalValues["Technology"];
                    if (_originalValues.ContainsKey("Domination")) arch.Domination = (int)_originalValues["Domination"];
                }
                else if (target is Person person)
                {
                    if (_originalValues.ContainsKey("Command")) person.Command = (int)_originalValues["Command"];
                    if (_originalValues.ContainsKey("Strength")) person.Strength = (int)_originalValues["Strength"];
                    if (_originalValues.ContainsKey("Intelligence")) person.Intelligence = (int)_originalValues["Intelligence"];
                    if (_originalValues.ContainsKey("Politics")) person.Politics = (int)_originalValues["Politics"];
                    if (_originalValues.ContainsKey("Glamour")) person.Glamour = (int)_originalValues["Glamour"];
                }
                else if (target is Troop troop)
                {
                    if (_originalValues.ContainsKey("Army")) troop.Army.Quantity = (int)_originalValues["Army"];
                    if (_originalValues.ContainsKey("Morale")) troop.Morale = (int)_originalValues["Morale"];
                    if (_originalValues.ContainsKey("Food")) troop.Food = (int)_originalValues["Food"];
                }
                else if (target is Faction faction)
                {
                    if (_originalValues.ContainsKey("Reputation")) faction.Reputation = (int)_originalValues["Reputation"];
                    if (_originalValues.ContainsKey("TechniquePoint")) faction.TechniquePoint = (int)_originalValues["TechniquePoint"];
                }

                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] 已恢复原始数值");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] 恢复原始数值失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存前验证 - 检查当前编辑对象的数值是否在合理范围内
        /// </summary>
        /// <returns>true表示验证通过可以保存，false表示验证失败需要修正</returns>
        private bool ValidateBeforeSave()
        {
            if (editorFrame?.EditTarget == null)
            {
                return true; // 没有编辑对象，允许保存
            }

            var target = editorFrame.EditTarget;
            // System.Diagnostics.Debug.WriteLine($"[InGameEditor] 开始验证保存: {target.GetType().Name}");

            bool isValid = false;

            // 根据对象类型进行不同的验证
            if (target is Architecture arch)
            {
                isValid = ValidateArchitecture(arch);
            }
            else if (target is Person person)
            {
                isValid = ValidatePerson(person);
            }
            else if (target is Troop troop)
            {
                isValid = ValidateTroop(troop);
            }
            else if (target is Faction faction)
            {
                isValid = ValidateFaction(faction);
            }
            else
            {
                isValid = true; // 其他类型默认通过
            }

            // 如果验证失败，恢复原始数值
            if (!isValid)
            {
                // System.Diagnostics.Debug.WriteLine("[InGameEditor] 验证失败，恢复原始数值");
                RestoreOriginalValues(target);
            }

            return isValid;
        }

        /// <summary>
        /// 验证建筑数据
        /// </summary>
        private bool ValidateArchitecture(Architecture arch)
        {
            // 获取上限值
            int maxAgri = GetTypeMax(arch, "Agriculture");
            int maxComm = GetTypeMax(arch, "Commerce");
            int maxTech = GetTypeMax(arch, "Technology");
            int maxDomi = GetTypeMax(arch, "Domination");
            int maxMorale = GetTypeMax(arch, "Morale");
            int maxEndurance = GetTypeMax(arch, "Endurance");

            // System.Diagnostics.Debug.WriteLine($"[InGameEditor] 建筑验证 - 耐久: 当前={arch.Endurance}, 上限={maxEndurance}");

            // 验证各项数值
            if (arch.Fund < 0)
            {
                ShowValidationAlert("保存失败！\n资金不能为负数！");
                return false;
            }

            if (arch.Food < 0)
            {
                ShowValidationAlert("保存失败！\n粮草不能为负数！");
                return false;
            }

            if (arch.Agriculture > maxAgri)
            {
                ShowValidationAlert($"保存失败！\n农业值({arch.Agriculture}) 不能超过上限({maxAgri})！");
                return false;
            }

            if (arch.Commerce > maxComm)
            {
                ShowValidationAlert($"保存失败！\n商业值({arch.Commerce}) 不能超过上限({maxComm})！");
                return false;
            }

            if (arch.Technology > maxTech)
            {
                ShowValidationAlert($"保存失败！\n技术值({arch.Technology}) 不能超过上限({maxTech})！");
                return false;
            }

            if (arch.Domination > maxDomi)
            {
                ShowValidationAlert($"保存失败！\n统治值({arch.Domination}) 不能超过上限({maxDomi})！");
                return false;
            }

            if (arch.Morale > maxMorale)
            {
                ShowValidationAlert($"保存失败！\n士气值({arch.Morale}) 不能超过上限({maxMorale})！");
                return false;
            }

            if (arch.Endurance > maxEndurance)
            {
                // System.Diagnostics.Debug.WriteLine($"[InGameEditor] 耐久验证失败: {arch.Endurance} > {maxEndurance}");
                ShowValidationAlert($"保存失败！\n耐久值({arch.Endurance}) 不能超过上限({maxEndurance})！");
                return false;
            }

            // System.Diagnostics.Debug.WriteLine("[InGameEditor] 建筑验证通过");
            return true;
        }

        /// <summary>
        /// 验证武将数据
        /// </summary>
        private bool ValidatePerson(Person person)
        {
            int statCap = 110; // 五维上限

            if (person.Command > statCap)
            {
                ShowValidationAlert($"保存失败！\n统率值({person.Command}) 不能超过上限({statCap})！");
                return false;
            }

            if (person.Strength > statCap)
            {
                ShowValidationAlert($"保存失败！\n武力值({person.Strength}) 不能超过上限({statCap})！");
                return false;
            }

            if (person.Intelligence > statCap)
            {
                ShowValidationAlert($"保存失败！\n智力值({person.Intelligence}) 不能超过上限({statCap})！");
                return false;
            }

            if (person.Politics > statCap)
            {
                ShowValidationAlert($"保存失败！\n政治值({person.Politics}) 不能超过上限({statCap})！");
                return false;
            }

            if (person.Glamour > statCap)
            {
                ShowValidationAlert($"保存失败！\n魅力值({person.Glamour}) 不能超过上限({statCap})！");
                return false;
            }

            // 检查负数
            if (person.Command < 0 || person.Strength < 0 || person.Intelligence < 0 || 
                person.Politics < 0 || person.Glamour < 0)
            {
                ShowValidationAlert("保存失败！\n武将属性不能为负数！");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证部队数据
        /// </summary>
        private bool ValidateTroop(Troop troop)
        {
            int maxArmy = GetCap(troop, "MaxArmy", 20000);

            if (troop.Army.Quantity > maxArmy)
            {
                ShowValidationAlert($"保存失败！\n兵力({troop.Army.Quantity}) 不能超过最大编制({maxArmy})！");
                return false;
            }

            if (troop.Morale > 120)
            {
                ShowValidationAlert($"保存失败！\n士气值({troop.Morale}) 不能超过上限(120)！");
                return false;
            }

            if (troop.Army.Quantity < 0 || troop.Morale < 0 || troop.Food < 0)
            {
                ShowValidationAlert("保存失败！\n部队属性不能为负数！");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证势力数据
        /// </summary>
        private bool ValidateFaction(Faction faction)
        {
            if (faction.Reputation > 30000)
            {
                ShowValidationAlert($"保存失败！\n声望值({faction.Reputation}) 不能超过上限(30000)！");
                return false;
            }

            if (faction.TechniquePoint > 100000)
            {
                ShowValidationAlert($"保存失败！\n技术点({faction.TechniquePoint}) 不能超过上限(100000)！");
                return false;
            }

            if (faction.Reputation < 0 || faction.TechniquePoint < 0)
            {
                ShowValidationAlert("保存失败！\n势力属性不能为负数！");
                return false;
            }

            return true;
        }

        #endregion

        #region 右键菜单编辑示例

        /// <summary>
        /// 构建武将编辑右键菜单 (示例)
        /// </summary>
        /// <param name="person">要编辑的武将</param>
        private void BuildPersonEditMenu(Person person)
        {
            if (contextMenu == null || person == null) return;

            contextMenu.ClearFunctions();

            // 修改统率
            contextMenu.AddMenu("修改统率", () => 
                OpenNumberInputer(
                    person.Command, 
                    Session.GlobalVariables.MaxAbility, 
                    (val) => person.Command = val,
                    null, // 无验证规则
                    // 关联更新：如果武将属于某个军团，刷新该军团的数据
                    () => {
                        if (person.LocationTroop != null)
                        {
                            // 重新计算部队战斗力
                            person.LocationTroop.RefreshAllData();
                        }
                    }
                )
            );

            // 修改武力
            contextMenu.AddMenu("修改武力", () => 
                OpenNumberInputer(
                    person.Strength, 
                    Session.GlobalVariables.MaxAbility, 
                    (val) => person.Strength = val,
                    null,
                    () => person.LocationTroop?.RefreshAllData()
                )
            );

            // 修改智力
            contextMenu.AddMenu("修改智力", () => 
                OpenNumberInputer(
                    person.Intelligence, 
                    Session.GlobalVariables.MaxAbility, 
                    (val) => person.Intelligence = val,
                    null,
                    () => person.LocationTroop?.RefreshAllData()
                )
            );

            // 修改政治
            contextMenu.AddMenu("修改政治", () => 
                OpenNumberInputer(
                    person.Politics, 
                    Session.GlobalVariables.MaxAbility, 
                    (val) => person.Politics = val
                )
            );

            // 修改魅力
            contextMenu.AddMenu("修改魅力", () => 
                OpenNumberInputer(
                    person.Glamour, 
                    Session.GlobalVariables.MaxAbility, 
                    (val) => person.Glamour = val
                )
            );

            // 注意: Person.Loyalty 是只读计算属性，无法直接修改
            // 可以通过修改 TempLoyaltyChange 字段来临时调整忠诚度

            contextMenu.IsShowing = true;
        }

        /// <summary>
        /// 构建势力编辑右键菜单 (示例)
        /// </summary>
        /// <param name="faction">要编辑的势力</param>
        private void BuildFactionEditMenu(Faction faction)
        {
            if (contextMenu == null || faction == null) return;

            contextMenu.ClearFunctions();

            // 注意: Faction.Fund 和 Faction.Food 是只读计算属性 (所有建筑资源总和)
            // 如需修改势力资源，应该修改各个 Architecture 的 Fund 和 Food

            // 修改技术点 (TechniquePoint 是可写的)
            contextMenu.AddMenu("修改技术点", () => 
                OpenNumberInputer(
                    faction.TechniquePoint, 
                    999999, 
                    (val) => faction.TechniquePoint = val
                )
            );

            contextMenu.IsShowing = true;
        }

        /// <summary>
        /// 准备菜单 (清空并设置标题)
        /// </summary>
        private void PrepareMenu(string title)
        {
            if (contextMenu == null) return;
            contextMenu.ClearFunctions();
            contextMenu.AddMenu(title, null); // 标题行，不可点击
        }

        /// <summary>
        /// 显示武将编辑主菜单
        /// </summary>
        /// <param name="person">要编辑的武将</param>
        public void ShowPersonEditMenu(Person person)
        {
            if (contextMenu == null || person == null) return;

            PrepareMenu($"[编辑武将] {person.Name}");

            // 基础属性编辑
            contextMenu.AddMenu("修改五维属性 >>", () => BuildPersonEditMenu(person));

            // 称号编辑
            contextMenu.AddMenu("编辑称号 >>", () => ShowTitleEditMenu(person));

            // 特技编辑
            contextMenu.AddMenu("编辑特技 >>", () => ShowSkillEditMenu(person));

            // 宝物编辑
            contextMenu.AddMenu("编辑宝物 >>", () => ShowTreasureEditMenu(person));

            // 分隔线
            contextMenu.AddMenu("────────", null);

            // 刷新状态
            contextMenu.AddMenu("🔄 刷新状态", () => ShowRefreshMenu(person));

            contextMenu.IsShowing = true;
        }

        /// <summary>
        /// 显示称号编辑菜单
        /// </summary>
        public void ShowTitleEditMenu(Person person)
        {
            if (contextMenu == null || person == null) return;

            PrepareMenu($"[编辑称号] {person.Name}");

            // 显示当前称号
            string titleNames = person.RealTitles.Count > 0 
                ? string.Join(", ", person.RealTitles.ConvertAll(t => t.Name)) 
                : "无";
            contextMenu.AddMenu($"当前称号: {titleNames}", null);
            contextMenu.AddMenu($"称号数量: {person.RealTitles.Count}", null);

            // 添加称号
            contextMenu.AddMenu("➕ 学习称号 (输入ID)", () => 
            {
                OpenNumberInputer(0, 9999, (titleId) => 
                {
                    // 检查称号是否已拥有
                    foreach (var t in person.RealTitles)
                    {
                        if (t.ID == titleId)
                        {
                            ShowAlert("该武将已经拥有此称号!");
                            return;
                        }
                    }

                    // 获取称号对象
                    var titleData = Session.Current.Scenario.GameCommonData.AllTitles.GetTitle(titleId);
                    if (titleData == null)
                    {
                        ShowAlert("无效的称号 ID !");
                        return;
                    }

                    // 添加称号
                    person.RealTitles.Add(titleData);

                    // 应用称号影响
                    titleData.Influences.ApplyInfluence(person, GameObjects.Influences.Applier.Title, titleId, false);

                    ShowAlert($"习得称号: {titleData.Name}");
                });
            });

            // 删除称号
            contextMenu.AddMenu("➖ 遗忘称号 (输入ID)", () => 
            {
                OpenNumberInputer(0, 9999, (titleId) => 
                {
                    Title titleToRemove = null;
                    foreach (var t in person.RealTitles)
                    {
                        if (t.ID == titleId)
                        {
                            titleToRemove = t;
                            break;
                        }
                    }

                    if (titleToRemove == null)
                    {
                        ShowAlert("武将未拥有此称号，无法删除!");
                        return;
                    }

                    // 移除称号影响
                    titleToRemove.Influences.PurifyInfluence(person, GameObjects.Influences.Applier.Title, titleId, false);

                    // 移除称号
                    person.RealTitles.Remove(titleToRemove);

                    ShowAlert($"遗忘称号: {titleToRemove.Name}");
                });
            });

            // 查看称号ID列表
            contextMenu.AddMenu("查看现有称号ID", () => {
                string msg = person.RealTitles.Count > 0 
                    ? string.Join(", ", person.RealTitles.ConvertAll(t => $"{t.ID}({t.Name})"))
                    : "无称号";
                ShowAlert(msg);
            });

            contextMenu.AddMenu("<< 返回", () => ShowPersonEditMenu(person));
            contextMenu.IsShowing = true;
        }

        /// <summary>
        /// 显示特技编辑菜单
        /// </summary>
        public void ShowSkillEditMenu(Person person)
        {
            if (contextMenu == null || person == null) return;

            PrepareMenu($"[编辑特技] {person.Name}");

            // 显示当前特技数量
            contextMenu.AddMenu($"现有特技数: {person.Skills.Count}", null);

            // 添加特技
            contextMenu.AddMenu("➕ 学习特技 (输入ID)", () => 
            {
                OpenNumberInputer(0, 9999, (skillId) => 
                {
                    // 检查是否已拥有
                    if (person.Skills.GetSkill(skillId) != null)
                    {
                        ShowAlert("该武将已经拥有此特技!");
                        return;
                    }

                    // 获取特技对象
                    var skillData = Session.Current.Scenario.GameCommonData.AllSkills.GetSkill(skillId);
                    if (skillData == null)
                    {
                        ShowAlert("无效的特技 ID !");
                        return;
                    }

                    // 添加特技
                    person.Skills.AddSkill(skillData);

                    // 应用特技影响
                    skillData.Influences.ApplyInfluence(person, GameObjects.Influences.Applier.Skill, skillId, false);

                    ShowAlert($"习得特技: {skillData.Name}");
                });
            });

            // 删除特技
            contextMenu.AddMenu("➖ 遗忘特技 (输入ID)", () => 
            {
                OpenNumberInputer(0, 9999, (skillId) => 
                {
                    var skillToRemove = person.Skills.GetSkill(skillId);
                    if (skillToRemove == null)
                    {
                        ShowAlert("武将未拥有此特技，无法删除!");
                        return;
                    }

                    // 移除特技影响
                    skillToRemove.Influences.PurifyInfluence(person, GameObjects.Influences.Applier.Skill, skillId, false);

                    // 注意: SkillTable 没有 Remove 方法，需要直接操作 Dictionary
                    person.Skills.Skills.Remove(skillId);

                    ShowAlert($"遗忘特技: {skillToRemove.Name}");
                });
            });
            
            // 查看当前特技ID
            contextMenu.AddMenu("查看现有特技ID", () => {
                var skills = person.Skills.Skills.Values;
                string msg = skills.Count > 0 
                    ? string.Join(", ", new List<Skill>(skills).ConvertAll(s => $"{s.ID}({s.Name})"))
                    : "无特技";
                ShowAlert(msg);
            });

            contextMenu.AddMenu("<< 返回", () => ShowPersonEditMenu(person));
            contextMenu.IsShowing = true;
        }

        /// <summary>
        /// 显示宝物编辑菜单
        /// </summary>
        public void ShowTreasureEditMenu(Person person)
        {
            if (contextMenu == null || person == null) return;

            PrepareMenu($"[编辑宝物] {person.Name}");

            // 显示当前宝物
            string treasureNames = person.Treasures.Count > 0 
                ? string.Join(", ", new List<Treasure>(person.Treasures.GameObjects.ConvertAll(t => (Treasure)t)).ConvertAll(t => t.Name)) 
                : "无";
            contextMenu.AddMenu($"当前宝物: {treasureNames}", null);
            contextMenu.AddMenu($"宝物数量: {person.Treasures.Count}", null);

            // 添加宝物
            contextMenu.AddMenu("➕ 获得宝物 (输入ID)", () => 
            {
                OpenNumberInputer(0, 9999, (treasureId) => 
                {
                    // 检查宝物是否已拥有
                    foreach (Treasure t in person.Treasures)
                    {
                        if (t.ID == treasureId)
                        {
                            ShowAlert("该武将已经拥有此宝物!");
                            return;
                        }
                    }

                    // 获取宝物对象
                    var treasureData = (Session.Current.Scenario.Treasures.GetGameObject(treasureId) is Treasure ? (Treasure)Session.Current.Scenario.Treasures.GetGameObject(treasureId) : null);
                    if (treasureData == null)
                    {
                        ShowAlert("无效的宝物 ID !");
                        return;
                    }

                    // 检查宝物是否被其他人持有
                    if (treasureData.BelongedPerson != null)
                    {
                        ShowAlert($"此宝物已被 {treasureData.BelongedPerson.Name} 持有!");
                        return;
                    }

                    // 使用游戏内置方法添加宝物 (自动处理影响)
                    person.ReceiveTreasure(treasureData);
                    treasureData.Available = true;

                    ShowAlert($"获得宝物: {treasureData.Name}");
                });
            });

            // 移除宝物
            contextMenu.AddMenu("➖ 失去宝物 (输入ID)", () => 
            {
                OpenNumberInputer(0, 9999, (treasureId) => 
                {
                    Treasure treasureToRemove = null;
                    foreach (Treasure t in person.Treasures)
                    {
                        if (t.ID == treasureId)
                        {
                            treasureToRemove = t;
                            break;
                        }
                    }

                    if (treasureToRemove == null)
                    {
                        ShowAlert("武将未持有此宝物，无法移除!");
                        return;
                    }

                    // 使用游戏内置方法移除宝物 (自动处理影响)
                    person.LoseTreasure(treasureToRemove);

                    ShowAlert($"失去宝物: {treasureToRemove.Name}");
                });
            });

            // 查看宝物ID列表
            contextMenu.AddMenu("查看现有宝物ID", () => {
                string msg = person.Treasures.Count > 0 
                    ? string.Join(", ", new List<Treasure>(person.Treasures.GameObjects.ConvertAll(t => (Treasure)t)).ConvertAll(t => t.Name))
                    : "无宝物";
                ShowAlert(msg);
            });

            // 分隔线
            contextMenu.AddMenu("────────", null);

            // 修改宝物影响
            // contextMenu.AddMenu("🔧 修改宝物影响 >>", () => ShowTreasureInfluenceSelectMenu(person));

            contextMenu.AddMenu("<< 返回", () => ShowPersonEditMenu(person));
            contextMenu.IsShowing = true;
        }

        /*
        /// <summary>
        /// 选择要修改影响的宝物
        /// </summary>
        private void ShowTreasureInfluenceSelectMenu(Person person)
        {
            if (contextMenu == null || person == null) return;

            PrepareMenu($"[选择宝物] {person.Name}");

            if (person.Treasures.Count == 0)
            {
                contextMenu.AddMenu("该武将没有宝物", null);
            }
            else
            {
                foreach (Treasure t in person.Treasures)
                {
                    var treasure = t; // 闭包捕获
                    contextMenu.AddMenu($"{treasure.Name} (ID:{treasure.ID})", () => ShowInfluenceEditMenu(treasure.Influences, treasure.Name, () => ShowTreasureInfluenceSelectMenu(person)));
                }
            }

            contextMenu.AddMenu("<< 返回", () => ShowTreasureEditMenu(person));
            contextMenu.IsShowing = true;
        }
        */

        /// <summary>
        /// 通用影响编辑菜单
        /// </summary>
        private void ShowInfluenceEditMenu(InfluenceTable table, string title, Action backAction)
        {
            if (contextMenu == null || table == null) return;

            PrepareMenu($"[编辑影响] {title}");

            contextMenu.AddMenu($"现有影响数: {table.Count}", null);

            contextMenu.AddMenu("➕ 添加影响 (列表选择)", () => StartSelectInfluence(table, false, () => ShowInfluenceEditMenu(table, title, backAction)));
            contextMenu.AddMenu("➖ 删除影响 (批量删除)", () => StartSelectInfluence(table, true, () => ShowInfluenceEditMenu(table, title, backAction)));
            contextMenu.AddMenu("", null);

            // 显示当前影响
            contextMenu.AddMenu("<< 返回", () => {
                if(backAction != null) backAction();
                else contextMenu.IsShowing = false;
            });
            contextMenu.IsShowing = true;
        }



        /// <summary>
        /// 强制刷新人物状态 - 重新应用所有影响
        /// </summary>
        /// <param name="person">要刷新的武将</param>
        private void RefreshPersonStats(Person person)
        {
            if (person == null) return;

            try
            {
                // 1. 清除所有当前影响
                person.PurifySkills(false);
                person.PurifyTitles(false);
                person.PurifyAllTreasures(false);

                // 2. 重新应用所有影响
                person.ApplySkills(false);
                person.ApplyTitles(false);
                person.ApplyAllTreasures(false);

                // 3. 如果武将在部队中，刷新部队数据
                if (person.LocationTroop != null)
                {
                    person.LocationTroop.RefreshAllData();
                }

                ShowAlert($"已刷新 {person.Name} 的所有状态!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InGameEditor] RefreshPersonStats error: {ex.Message}");
                ShowAlert("刷新状态时发生错误!");
            }
        }

        /// <summary>
        /// 显示刷新状态确认菜单
        /// </summary>
        private void ShowRefreshMenu(Person person)
        {
            if (contextMenu == null || person == null) return;

            PrepareMenu($"[刷新状态] {person.Name}");

            contextMenu.AddMenu("⚠️ 此操作将:", null);
            contextMenu.AddMenu("  1. 清除所有特技/称号/宝物影响", null);
            contextMenu.AddMenu("  2. 重新应用所有影响", null);
            contextMenu.AddMenu("", null);

            contextMenu.AddMenu("✅ 确定刷新", () => {
                RefreshPersonStats(person);
            });

            contextMenu.AddMenu("<< 取消", () => ShowPersonEditMenu(person));
            contextMenu.IsShowing = true;
        }

        /// <summary>
        /// 全局宝物编辑菜单 (接口实现 - 已禁用)
        /// </summary>
        public void ShowGlobalTreasureEditMenu(Treasure treasure)
        {
            // 功能已禁用
        }

        /// <summary>
        /// 全局称号编辑菜单 (接口实现 - 已禁用)
        /// </summary>
        public void ShowGlobalTitleEditMenu(Title title)
        {
            // 功能已禁用
        }

        /// <summary>
        /// 全局特技编辑菜单 (接口实现 - 已禁用)
        /// </summary>
        public void ShowGlobalSkillEditMenu(Skill skill)
        {
            // 功能已禁用
        }

        /// <summary>
        /// 开始选择影响 (Add/Remove)
        /// </summary>
        private void StartSelectInfluence(InfluenceTable table, bool removeMode, Action callback)
        {
            this.currentInfluenceEditTable = table;
            this.currentInfluenceEditCallback = callback;
            this.isRemovingInfluence = removeMode;

            GameObjectList list = null;
            if (removeMode)
            {
                list = table.GetInfluenceList();
            }
            else
            {
                list = Session.Current.Scenario.GameCommonData.AllInfluences.GetInfluenceList();
            }

            string title = removeMode ? "选择要删除的影响" : "选择要添加的影响";
            
            // Ensure Influence ListKind exists
            var tabListPlugin = Session.MainGame.mainGameScreen.Plugins.TabListPlugin as TabListPlugin.TabListPlugin;
            if (tabListPlugin != null)
            {
               tabListPlugin.EnsureInfluenceListKind();
            }

            // 调用 MainGameScreen 的 ShowTabListInFrame
            Session.MainGame.mainGameScreen.ShowTabListInFrame(
                WorldOfTheThreeKingdoms.GameGlobal.UndoneWorkKind.Frame, 
                WorldOfTheThreeKingdoms.GameGlobal.FrameKind.Influence, 
                WorldOfTheThreeKingdoms.GameGlobal.FrameFunction.Editor_SelectInfluence, 
                true, true, true, true, 
                list, null, title, "");
        }

        public void FinishInfluenceSelection(System.Collections.Generic.List<object> items)
        {
            if (currentInfluenceEditTable == null) return;

            int count = 0;
            if (this.isRemovingInfluence)
            {
                foreach (var item in items)
                {
                    if (item is Influence inf)
                    {
                        if (currentInfluenceEditTable.Influences.Remove(inf.ID))
                        {
                            count++;
                        }
                    }
                }
                ShowAlert($"已删除 {count} 个影响");
            }
            else
            {
                foreach (var item in items)
                {
                    if (item is Influence inf)
                    {
                         // 添加时可能需要 Clone
                         var newInf = inf.Clone();
                         // 必须重新初始化以清空应用列表引用，否则会共享状态
                         newInf.Init();
                         if (currentInfluenceEditTable.AddInfluence(newInf))
                         {
                             count++;
                         }
                    }
                }
                ShowAlert($"已添加 {count} 个影响");
            }

            // 完成后调用回调刷新菜单
            if (currentInfluenceEditCallback != null)
            {
                currentInfluenceEditCallback();
            }
            else
            {
                // 如果没有回调，至少恢复上下文菜单显示
            }
        }

        #endregion

        #region 缓冲区编辑器 - 新增功能

        /// <summary>
        /// 激活缓冲区编辑器主入口
        /// </summary>
        private void ActivateBufferedEditor()
        {
            if (contextMenu == null) return;

            // 每次打开编辑器，清空缓冲区
            _pendingChanges.Clear();
            _editingTarget = null;

            var currentObj = contextMenu.CurrentGameObject;
            
            // 尝试获取鼠标位置的地图坐标 (用于地形编辑)
            Point mousePos = Session.MainGame.mainGameScreen.MousePosition;
            Point mapCoordinates = Session.MainGame.mainGameScreen.GetPositionByPoint(mousePos);

            if (currentObj == null)
            {
                ShowTerrainEditMenu(mapCoordinates);
                return;
            }

            _editingTarget = currentObj;

            if (currentObj is Person person) ShowPersonBufferedEditMenu(person);
            else if (currentObj is Architecture arch) ShowArchitectureBufferedEditMenu(arch);
            else if (currentObj is Troop troop) ShowTroopBufferedEditMenu(troop);
            else if (currentObj is Faction faction) ShowFactionBufferedEditMenu(faction);
        }

        // ========================================================================
        // 核心辅助方法：处理缓冲与显示
        // ========================================================================
        /// <summary>
        /// 获取显示文本。如果有待保存的修改，显示 "原值 -> 新值"
        /// </summary>
        private string GetDisplayText<T>(string label, T currentValue, string key)
        {
            if (_pendingChanges.ContainsKey(key))
            {
                // ⚠️ 格式：[待保存] 属性: 原值 -> 新值
                return $"[待保存] {label}: {currentValue} -> {_pendingChanges[key]}";
            }
            return $"{label}: {currentValue}";
        }

        /// <summary>
        /// 添加数值编辑项（带缓冲、带上限）
        /// </summary>
        private void AddIntEditItem(string label, string key, int currentValue, int maxValue, Action refreshMenu)
        {
            // 如果缓冲区有值，优先使用缓冲区的值作为"当前值"来打开输入器
            int startValue = _pendingChanges.ContainsKey(key) ? (int)_pendingChanges[key] : currentValue;
            string menuText = GetDisplayText(label, currentValue, key);

            contextMenu.AddMenu(menuText, () => {
                // 打开输入器，传入特定的上限 (maxValue)
                OpenBufferedNumberInputer(startValue, maxValue, (newValue) => {
                    // 1. 不直接修改对象，而是存入缓冲区
                    _pendingChanges[key] = newValue;
                    // 2. 重新打开菜单以刷新显示
                    refreshMenu(); 
                });
            });
        }

        /// <summary>
        /// 添加保存和取消按钮
        /// </summary>
        private void AddSaveControl(Action applyChanges, Action refreshMenu)
        {
            if (_pendingChanges.Count > 0)
            {
                contextMenu.AddMenu("-----------------------", null);
                contextMenu.AddMenu("💾 保存修改 (Apply)", () => {
                    // 1. 执行实际修改
                    applyChanges();
                    // 2. 清空缓冲
                    _pendingChanges.Clear();
                    // 3. 提示成功
                    ShowAlert("保存成功!"); 
                    // 4. 刷新菜单看到最终结果
                    refreshMenu();
                });
                contextMenu.AddMenu("❌ 放弃修改 (Cancel)", () => {
                    _pendingChanges.Clear();
                    refreshMenu();
                });
            }
        }

        // ========================================================================
        // 1. 城市编辑 (Architecture) - 演示属性上限补丁
        // ========================================================================
        private void ShowArchitectureBufferedEditMenu(Architecture arch)
        {
            PrepareBufferedMenu($"[编辑城市] {arch.Name}");

            // --- 智能获取上限 ---
            // 尝试通过反射或已知属性获取上限，如果获取不到则用默认值
            int maxAgri = GetTypeMax(arch, "Agriculture");
            int maxComm = GetTypeMax(arch, "Commerce");
            int maxTech = GetTypeMax(arch, "Technology");
            int maxDomi = GetTypeMax(arch, "Domination");
            int maxMorale = GetTypeMax(arch, "Morale");
            int maxEndurance = GetTypeMax(arch, "Endurance");
            
            // 调试信息：输出实际获取到的上限值
            // System.Diagnostics.Debug.WriteLine($"[BufferedEditor] 城市 {arch.Name} 耐久上限: {maxEndurance}");
            // System.Diagnostics.Debug.WriteLine($"[BufferedEditor] 当前耐久值: {arch.Endurance}");
            
            // 资金粮草通常上限很高
            int maxFund = 9999999; 
            int maxFood = 9999999;

            // --- 添加编辑项 (只存缓冲) ---
            // 资源
            AddIntEditItem("资金", "Fund", arch.Fund, maxFund, () => ShowArchitectureBufferedEditMenu(arch));
            AddIntEditItem("粮草", "Food", arch.Food, maxFood, () => ShowArchitectureBufferedEditMenu(arch));
            AddIntEditItem("人口", "Population", arch.Population, 999999, () => ShowArchitectureBufferedEditMenu(arch));

            // 军事
            AddIntEditItem("士气", "Morale", arch.Morale, maxMorale, () => ShowArchitectureBufferedEditMenu(arch));
            AddIntEditItem("耐久", "Endurance", arch.Endurance, maxEndurance, () => ShowArchitectureBufferedEditMenu(arch));

            // 内政
            AddIntEditItem("农业", "Agriculture", arch.Agriculture, maxAgri, () => ShowArchitectureBufferedEditMenu(arch));
            AddIntEditItem("商业", "Commerce", arch.Commerce, maxComm, () => ShowArchitectureBufferedEditMenu(arch));
            AddIntEditItem("技术", "Technology", arch.Technology, maxTech, () => ShowArchitectureBufferedEditMenu(arch));
            AddIntEditItem("统治", "Domination", arch.Domination, maxDomi, () => ShowArchitectureBufferedEditMenu(arch));

            // --- 核心修改：带验证的保存逻辑 ---
            AddSaveControl(() => {
                // =========================================================
                // 1. 预读取所有"拟修改"的数据 (Proposed Values)
                // =========================================================
                int newFund = GetProposedInt("Fund", arch.Fund);
                int newFood = GetProposedInt("Food", arch.Food);
                int newPop = GetProposedInt("Population", arch.Population);
                int newMorale = GetProposedInt("Morale", arch.Morale);
                int newEndurance = GetProposedInt("Endurance", arch.Endurance);
                int newAgri = GetProposedInt("Agriculture", arch.Agriculture);
                int newComm = GetProposedInt("Commerce", arch.Commerce);
                int newTech = GetProposedInt("Technology", arch.Technology);
                int newDomi = GetProposedInt("Domination", arch.Domination);

                // 调试信息：输出验证过程
                // System.Diagnostics.Debug.WriteLine($"[BufferedEditor] 验证开始 - 耐久: 当前={arch.Endurance}, 新值={newEndurance}, 上限={maxEndurance}");

                // =========================================================
                // 2. 执行逻辑校验 (Validation Rules)
                // =========================================================
                
                // 规则 A: 资金和粮草不能为负数
                if (newFund < 0)
                {
                    ShowValidationAlert("保存失败！\n资金不能为负数！");
                    return; // ⛔ 阻断保存
                }
                
                if (newFood < 0)
                {
                    ShowValidationAlert("保存失败！\n粮草不能为负数！");
                    return; // ⛔ 阻断保存
                }

                // 规则 B: 内政值不能超过硬性上限
                if (newAgri > maxAgri)
                {
                    ShowValidationAlert($"保存失败！\n农业值({newAgri}) 不能超过上限({maxAgri})！");
                    return; // ⛔ 阻断保存
                }
                
                if (newComm > maxComm)
                {
                    ShowValidationAlert($"保存失败！\n商业值({newComm}) 不能超过上限({maxComm})！");
                    return; // ⛔ 阻断保存
                }
                
                if (newTech > maxTech)
                {
                    ShowValidationAlert($"保存失败！\n技术值({newTech}) 不能超过上限({maxTech})！");
                    return; // ⛔ 阻断保存
                }
                
                if (newDomi > maxDomi)
                {
                    ShowValidationAlert($"保存失败！\n统治值({newDomi}) 不能超过上限({maxDomi})！");
                    return; // ⛔ 阻断保存
                }

                // 规则 C: 士气和耐久不能超过上限
                if (newMorale > maxMorale)
                {
                    ShowValidationAlert($"保存失败！\n士气值({newMorale}) 不能超过上限({maxMorale})！");
                    return; // ⛔ 阻断保存
                }
                
                if (newEndurance > maxEndurance)
                {
                    // System.Diagnostics.Debug.WriteLine($"[BufferedEditor] 耐久验证失败: {newEndurance} > {maxEndurance}");
                    ShowValidationAlert($"保存失败！\n耐久值({newEndurance}) 不能超过上限({maxEndurance})！");
                    return; // ⛔ 阻断保存
                }

                // =========================================================
                // 3. 校验通过，执行写入 (Commit)
                // =========================================================
                // System.Diagnostics.Debug.WriteLine($"[BufferedEditor] 验证通过，开始保存");
                if (_pendingChanges.ContainsKey("Fund")) arch.Fund = (int)_pendingChanges["Fund"];
                if (_pendingChanges.ContainsKey("Food")) arch.Food = (int)_pendingChanges["Food"];
                if (_pendingChanges.ContainsKey("Population")) arch.Population = (int)_pendingChanges["Population"];
                if (_pendingChanges.ContainsKey("Morale")) arch.Morale = (int)_pendingChanges["Morale"];
                if (_pendingChanges.ContainsKey("Endurance")) arch.Endurance = (int)_pendingChanges["Endurance"];
                if (_pendingChanges.ContainsKey("Agriculture")) arch.Agriculture = (int)_pendingChanges["Agriculture"];
                if (_pendingChanges.ContainsKey("Commerce")) arch.Commerce = (int)_pendingChanges["Commerce"];
                if (_pendingChanges.ContainsKey("Technology")) arch.Technology = (int)_pendingChanges["Technology"];
                if (_pendingChanges.ContainsKey("Domination")) arch.Domination = (int)_pendingChanges["Domination"];
            }, () => ShowArchitectureBufferedEditMenu(arch));

            contextMenu.IsShowing = true;
        }

        // ========================================================================
        // 2. 人员编辑 (Person)
        // ========================================================================
        private void ShowPersonBufferedEditMenu(Person person)
        {
            PrepareBufferedMenu($"[编辑武将] {person.Name}");

            // 五维上限通常是 100 或者 120 (根据剧本设定)
            int statCap = 110; 

            AddIntEditItem("统率", "Command", person.Command, statCap, () => ShowPersonBufferedEditMenu(person));
            AddIntEditItem("武力", "Strength", person.Strength, statCap, () => ShowPersonBufferedEditMenu(person));
            AddIntEditItem("智力", "Intelligence", person.Intelligence, statCap, () => ShowPersonBufferedEditMenu(person));
            AddIntEditItem("政治", "Politics", person.Politics, statCap, () => ShowPersonBufferedEditMenu(person));
            AddIntEditItem("魅力", "Glamour", person.Glamour, statCap, () => ShowPersonBufferedEditMenu(person));

            // 文本和开关类不需要缓冲，可以直接改 (因为没有"半成品"状态)
            // 或者你也可以把 Name 加入缓冲，这里演示直接修改
            contextMenu.AddMenu($"粘贴姓名 (当前:{person.Name})", () => {
                string text = BufferedClipboardUtils.GetText();
                if (!string.IsNullOrEmpty(text)) { 
                    // Person.Name 是只读的，无法直接修改
                    ShowAlert("武将姓名无法修改 (只读属性)");
                }
            });

            AddSaveControl(() => {
                // =========================================================
                // 1. 预读取所有"拟修改"的数据 (Proposed Values)
                // =========================================================
                int newCommand = GetProposedInt("Command", person.Command);
                int newStrength = GetProposedInt("Strength", person.Strength);
                int newIntelligence = GetProposedInt("Intelligence", person.Intelligence);
                int newPolitics = GetProposedInt("Politics", person.Politics);
                int newGlamour = GetProposedInt("Glamour", person.Glamour);

                // =========================================================
                // 2. 执行逻辑校验 (Validation Rules)
                // =========================================================
                
                // 规则 A: 五维属性不能超过上限
                if (newCommand > statCap)
                {
                    ShowValidationAlert($"保存失败！\n统率值({newCommand}) 不能超过上限({statCap})！");
                    return; // ⛔ 阻断保存
                }
                
                if (newStrength > statCap)
                {
                    ShowValidationAlert($"保存失败！\n武力值({newStrength}) 不能超过上限({statCap})！");
                    return; // ⛔ 阻断保存
                }
                
                if (newIntelligence > statCap)
                {
                    ShowValidationAlert($"保存失败！\n智力值({newIntelligence}) 不能超过上限({statCap})！");
                    return; // ⛔ 阻断保存
                }
                
                if (newPolitics > statCap)
                {
                    ShowValidationAlert($"保存失败！\n政治值({newPolitics}) 不能超过上限({statCap})！");
                    return; // ⛔ 阻断保存
                }
                
                if (newGlamour > statCap)
                {
                    ShowValidationAlert($"保存失败！\n魅力值({newGlamour}) 不能超过上限({statCap})！");
                    return; // ⛔ 阻断保存
                }

                // 规则 B: 所有属性不能为负数
                if (newCommand < 0 || newStrength < 0 || newIntelligence < 0 || newPolitics < 0 || newGlamour < 0)
                {
                    ShowValidationAlert("保存失败！\n武将属性不能为负数！");
                    return; // ⛔ 阻断保存
                }

                // =========================================================
                // 3. 校验通过，执行写入 (Commit)
                // =========================================================
                if (_pendingChanges.ContainsKey("Command")) person.Command = (int)_pendingChanges["Command"];
                if (_pendingChanges.ContainsKey("Strength")) person.Strength = (int)_pendingChanges["Strength"];
                if (_pendingChanges.ContainsKey("Intelligence")) person.Intelligence = (int)_pendingChanges["Intelligence"];
                if (_pendingChanges.ContainsKey("Politics")) person.Politics = (int)_pendingChanges["Politics"];
                if (_pendingChanges.ContainsKey("Glamour")) person.Glamour = (int)_pendingChanges["Glamour"];
            }, () => ShowPersonBufferedEditMenu(person));

            contextMenu.IsShowing = true;
        }

        // ========================================================================
        // 3. 其他类型的简单实现
        // ========================================================================
        private void ShowTroopBufferedEditMenu(Troop troop)
        {
            PrepareBufferedMenu($"[编辑部队] {troop.DisplayName}");

            // 假设 Troop.MaxArmy 是它的最大编制
            int maxArmy = GetCap(troop, "MaxArmy", 20000); 

            AddIntEditItem("兵力", "Army", troop.Army.Quantity, maxArmy, () => ShowTroopBufferedEditMenu(troop));
            AddIntEditItem("士气", "Morale", troop.Morale, 120, () => ShowTroopBufferedEditMenu(troop));
            AddIntEditItem("粮草", "Food", troop.Food, 50000, () => ShowTroopBufferedEditMenu(troop));

            AddSaveControl(() => {
                // =========================================================
                // 1. 预读取所有"拟修改"的数据 (Proposed Values)
                // =========================================================
                int newArmy = GetProposedInt("Army", troop.Army.Quantity);
                int newMorale = GetProposedInt("Morale", troop.Morale);
                int newFood = GetProposedInt("Food", troop.Food);

                // =========================================================
                // 2. 执行逻辑校验 (Validation Rules)
                // =========================================================
                
                // 规则 A: 兵力不能超过最大编制
                if (newArmy > maxArmy)
                {
                    ShowValidationAlert($"保存失败！\n兵力({newArmy}) 不能超过最大编制({maxArmy})！");
                    return; // ⛔ 阻断保存
                }
                
                // 规则 B: 士气不能超过上限
                if (newMorale > 120)
                {
                    ShowValidationAlert($"保存失败！\n士气值({newMorale}) 不能超过上限(120)！");
                    return; // ⛔ 阻断保存
                }

                // 规则 C: 所有数值不能为负数
                if (newArmy < 0 || newMorale < 0 || newFood < 0)
                {
                    ShowValidationAlert("保存失败！\n部队属性不能为负数！");
                    return; // ⛔ 阻断保存
                }

                // =========================================================
                // 3. 校验通过，执行写入 (Commit)
                // =========================================================
                if (_pendingChanges.ContainsKey("Army")) troop.Army.Quantity = (int)_pendingChanges["Army"];
                if (_pendingChanges.ContainsKey("Morale")) troop.Morale = (int)_pendingChanges["Morale"];
                if (_pendingChanges.ContainsKey("Food")) troop.Food = (int)_pendingChanges["Food"];
            }, () => ShowTroopBufferedEditMenu(troop));

            contextMenu.IsShowing = true;
        }

        private void ShowFactionBufferedEditMenu(Faction faction)
        {
            PrepareBufferedMenu($"[编辑势力] {faction.Name}");

            AddIntEditItem("声望", "Reputation", faction.Reputation, 30000, () => ShowFactionBufferedEditMenu(faction));
            AddIntEditItem("技术点", "TechniquePoint", faction.TechniquePoint, 100000, () => ShowFactionBufferedEditMenu(faction));

            AddSaveControl(() => {
                // =========================================================
                // 1. 预读取所有"拟修改"的数据 (Proposed Values)
                // =========================================================
                int newReputation = GetProposedInt("Reputation", faction.Reputation);
                int newTechniquePoint = GetProposedInt("TechniquePoint", faction.TechniquePoint);

                // =========================================================
                // 2. 执行逻辑校验 (Validation Rules)
                // =========================================================
                
                // 规则 A: 声望不能超过上限
                if (newReputation > 30000)
                {
                    ShowValidationAlert($"保存失败！\n声望值({newReputation}) 不能超过上限(30000)！");
                    return; // ⛔ 阻断保存
                }
                
                // 规则 B: 技术点不能超过上限
                if (newTechniquePoint > 100000)
                {
                    ShowValidationAlert($"保存失败！\n技术点({newTechniquePoint}) 不能超过上限(100000)！");
                    return; // ⛔ 阻断保存
                }

                // 规则 C: 所有数值不能为负数
                if (newReputation < 0 || newTechniquePoint < 0)
                {
                    ShowValidationAlert("保存失败！\n势力属性不能为负数！");
                    return; // ⛔ 阻断保存
                }

                // =========================================================
                // 3. 校验通过，执行写入 (Commit)
                // =========================================================
                if (_pendingChanges.ContainsKey("Reputation")) faction.Reputation = (int)_pendingChanges["Reputation"];
                if (_pendingChanges.ContainsKey("TechniquePoint")) faction.TechniquePoint = (int)_pendingChanges["TechniquePoint"];
            }, () => ShowFactionBufferedEditMenu(faction));

            contextMenu.IsShowing = true;
        }

        private void ShowTerrainEditMenu(Point mapCoordinates)
        {
            int current = Session.Current.Scenario.ScenarioMap.MapData[mapCoordinates.X, mapCoordinates.Y];
            PrepareBufferedMenu($"[编辑地形] ({mapCoordinates.X}, {mapCoordinates.Y})");

            AddIntEditItem("地形ID", "Terrain", current, 99, () => ShowTerrainEditMenu(mapCoordinates));

            AddSaveControl(() => {
                // =========================================================
                // 1. 预读取所有"拟修改"的数据 (Proposed Values)
                // =========================================================
                int newTerrain = GetProposedInt("Terrain", current);

                // =========================================================
                // 2. 执行逻辑校验 (Validation Rules)
                // =========================================================
                
                // 规则 A: 地形ID不能超过上限
                if (newTerrain > 99)
                {
                    ShowValidationAlert($"保存失败！\n地形ID({newTerrain}) 不能超过上限(99)！");
                    return; // ⛔ 阻断保存
                }

                // 规则 B: 地形ID不能为负数
                if (newTerrain < 0)
                {
                    ShowValidationAlert("保存失败！\n地形ID不能为负数！");
                    return; // ⛔ 阻断保存
                }

                // =========================================================
                // 3. 校验通过，执行写入 (Commit)
                // =========================================================
                if (_pendingChanges.ContainsKey("Terrain")) 
                    Session.Current.Scenario.ScenarioMap.MapData[mapCoordinates.X, mapCoordinates.Y] = (int)_pendingChanges["Terrain"];
            }, () => ShowTerrainEditMenu(mapCoordinates));

            contextMenu.IsShowing = true;
        }

        // ========================================================================
        // 通用工具
        // ========================================================================
        private void PrepareBufferedMenu(string title)
        {
            if (contextMenu == null) return;
            contextMenu.IsShowing = false;
            contextMenu.ClearFunctions();
            contextMenu.AddMenu(title, null);
        }

        /// <summary>
        /// 获取拟修改的数值 (如果有待保存的修改则返回修改值，否则返回当前游戏原值)
        /// </summary>
        private int GetProposedInt(string key, int currentValue)
        {
            if (_pendingChanges.ContainsKey(key))
            {
                return (int)_pendingChanges[key];
            }
            return currentValue;
        }

        /// <summary>
        /// 呼出数字输入器 (增强版：双重上限保护)
        /// </summary>
        private void OpenBufferedNumberInputer(int val, int max, Action<int> onConfirm)
        {
            if (numberInputer == null) return;

            // --- 第一道锁：UI 限制 ---
            // 告诉输入器最大值和最小值，影响UI显示和基础验证
            numberInputer.SetMax(max);
            numberInputer.Number = val;  // 设置初始值
            numberInputer.SetDepthOffset(-0.2f);

            // --- 第二道锁：逻辑强制截断 (核心保护) ---
            numberInputer.SetEnterFunction(() => {
                // 1. 获取玩家输入的数字 (可能超出范围)
                int inputVal = numberInputer.Number;

                // 2. 强制执行范围检查 (Clamping)
                bool wasModified = false;
                
                if (inputVal > max) 
                {
                    inputVal = max; // 超过上限强制设为上限
                    wasModified = true;
                }
                
                if (inputVal < 0) 
                {
                    inputVal = 0; // 防止负数
                    wasModified = true;
                }

                // 3. 如果数值被修正，可选择提示用户
                if (wasModified)
                {
                    // 可以在这里添加提示，但为了不打断流程，暂时注释
                    // ShowAlert($"输入已修正为合法范围: {inputVal}");
                    // System.Diagnostics.Debug.WriteLine($"[BufferedEditor] 输入值被修正: 原值可能超范围 -> {inputVal} (范围: 0-{max})");
                }

                // 4. 将修正后的合法数值传给回调
                onConfirm(inputVal);
            });

            contextMenu.IsShowing = false;
            numberInputer.IsShowing = true;
        }

        /// <summary>
        /// 智能获取对象的上限属性 (反射)
        /// 如果找不到属性，则返回 defaultCap
        /// </summary>
        private int GetCap(object target, string propertyName, int defaultCap)
        {
            try
            {
                PropertyInfo pi = target.GetType().GetProperty(propertyName);
                if (pi != null && pi.PropertyType == typeof(int))
                {
                    return (int)pi.GetValue(target);
                }
            }
            catch { }
            return defaultCap;
        }

        /// <summary>
        /// 获取属性类型的最大值 (防止溢出)
        /// </summary>
        private int GetTypeMax(object target, string propertyName)
        {
            try
            {
                PropertyInfo pi = target.GetType().GetProperty(propertyName);
                if (pi != null)
                {
                    if (pi.PropertyType == typeof(int)) return int.MaxValue;
                    if (pi.PropertyType == typeof(short)) return (int)short.MaxValue;
                    if (pi.PropertyType == typeof(ushort)) return (int)ushort.MaxValue;
                    if (pi.PropertyType == typeof(byte)) return (int)byte.MaxValue;
                    if (pi.PropertyType == typeof(sbyte)) return (int)sbyte.MaxValue;
                }
            }
            catch { }
            return int.MaxValue; // 默认返回Int最大值
        }

        #endregion

        #region 公共接口方法 - 供菜单调用

        /// <summary>
        /// 为建筑激活缓冲区编辑器
        /// </summary>
        public void ActivateBufferedEditorForArchitecture(Architecture arch)
        {
            if (!Session.GlobalVariables.EnableCheat)
            {
                ShowAlert("请先按 Ctrl+Shift+Z 开启作弊模式");
                return;
            }

            _pendingChanges.Clear();
            _editingTarget = arch;
            ShowArchitectureBufferedEditMenu(arch);
        }

        /// <summary>
        /// 为部队激活缓冲区编辑器
        /// </summary>
        public void ActivateBufferedEditorForTroop(Troop troop)
        {
            if (!Session.GlobalVariables.EnableCheat)
            {
                ShowAlert("请先按 Ctrl+Shift+Z 开启作弊模式");
                return;
            }

            _pendingChanges.Clear();
            _editingTarget = troop;
            ShowTroopBufferedEditMenu(troop);
        }

        /// <summary>
        /// 为势力激活缓冲区编辑器
        /// </summary>
        public void ActivateBufferedEditorForFaction(Faction faction)
        {
            if (!Session.GlobalVariables.EnableCheat)
            {
                ShowAlert("请先按 Ctrl+Shift+Z 开启作弊模式");
                return;
            }

            _pendingChanges.Clear();
            _editingTarget = faction;
            ShowFactionBufferedEditMenu(faction);
        }

        /// <summary>
        /// 为武将激活缓冲区编辑器
        /// </summary>
        public void ActivateBufferedEditorForPerson(Person person)
        {
            if (!Session.GlobalVariables.EnableCheat)
            {
                ShowAlert("请先按 Ctrl+Shift+Z 开启作弊模式");
                return;
            }

            _pendingChanges.Clear();
            _editingTarget = person;
            ShowPersonBufferedEditMenu(person);
        }

        /// <summary>
        /// 为地形激活缓冲区编辑器
        /// </summary>
        public void ActivateBufferedEditorForTerrain(Point mapCoordinates)
        {
            if (!Session.GlobalVariables.EnableCheat)
            {
                ShowAlert("请先按 Ctrl+Shift+Z 开启作弊模式");
                return;
            }

            _pendingChanges.Clear();
            _editingTarget = null;
            ShowTerrainEditMenu(mapCoordinates);
        }

        #endregion
    }

    /// <summary>
    /// 缓冲区编辑器专用剪贴板工具
    /// </summary>
    public static class BufferedClipboardUtils
    {
        public static string GetText()
        {
            // Note: System.Windows.Forms.Clipboard is not available in .NET 8 cross-platform
            // This would need to be replaced with a cross-platform clipboard library
            // For now, return empty string as placeholder
            
            // TODO: Implement cross-platform clipboard access using:
            // - TextCopy NuGet package
            // - Or platform-specific clipboard APIs
            
            return "";
        }
    }
}

