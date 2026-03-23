using GameFreeText;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Xml;

namespace InGameEditorPlugin
{
    /// <summary>
    /// 编辑器框架 - 管理编辑字段的显示和交互
    /// </summary>
    public class EditorFrame
    {
        // UI State
        public bool IsShowing { get; set; }
        private Screen screen;
        private INumberInputer numberInputer;

        // Layout
        private Rectangle backgroundRect;
        private Rectangle clientRect;
        private Point displayOffset;
        
        // Textures
        private PlatformTexture backgroundTexture;

        // Editor Fields
        private List<EditorField> fields = [];
        private EditorField selectedField;
        private EditorField hoveredField;
        
        // Scrolling
        private int scrollOffset = 0;
        private int maxScrollOffset = 0;
        private const int ScrollStep = 28;
        private Rectangle scrollbarRect;
        private Rectangle scrollThumbRect;
        private bool isDraggingScrollbar = false;

        // Target
        private object editTarget;
        public object EditTarget { get => editTarget; }
        private EditorType editorType;

        // Title
        private string title = "数据编辑器";

        // Buttons
        private Rectangle saveButtonRect;
        private Rectangle cancelButtonRect;
        private bool saveButtonHover = false;
        private bool cancelButtonHover = false;
        
        // Events
        public event Action OnSave;
        public event Action OnCancel;
        
        // Clipboard - 复制粘贴支持
        private static Dictionary<string, object> clipboard = new Dictionary<string, object>();
        private static EditorType clipboardType = EditorType.None;
        private string statusMessage = "";
        private float statusMessageTimer = 0f;
        
        // 私有字体缓存 - 用于在 Session.Current.Font 失败时使用备用
        private SpriteFont cachedFont = null;
        private bool triedLoadingFont = false;

        /// <summary>
        /// 获取可用的字体 (带多个备用选项)
        /// </summary>
        private SpriteFont GetFont()
        {
            // 首先尝试使用 Session.Current.Font
            if (Session.Current?.Font != null)
            {
                return Session.Current.Font;
            }
            
            // 如果已有缓存的字体，直接使用
            if (cachedFont != null)
            {
                return cachedFont;
            }
            
            // 如果已经尝试过加载但失败了，不要再试，防止卡顿
            if (triedLoadingFont)
            {
                return null;
            }
            
            triedLoadingFont = true;
            
            // 尝试其他字体源
            try
            {
                // Session 的其他字体属性 (FontS, FontL, FontE, FontT)
                if (Session.Current?.FontS != null) { cachedFont = Session.Current.FontS; return cachedFont; }
                if (Session.Current?.FontL != null) { cachedFont = Session.Current.FontL; return cachedFont; }
                if (Session.Current?.FontE != null) { cachedFont = Session.Current.FontE; return cachedFont; }
                if (Session.Current?.FontT != null) { cachedFont = Session.Current.FontT; return cachedFont; }
                
                // 直接从 FontContent 加载
                if (Session.Current?.FontContent != null)
                {
                    try { cachedFont = Session.Current.FontContent.Load<SpriteFont>("Font/FontS"); return cachedFont; } catch { }
                    try { cachedFont = Session.Current.FontContent.Load<SpriteFont>("FontS"); return cachedFont; } catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditorFrame] GetFont exception: {ex.Message}");
            }
            
            return null;
        }

        public void Initialize()
        {
            fields = new List<EditorField>();

            // 增大编辑器尺寸以容纳更多内容
            int width = 450;  // 从350增加到450
            int height = 600; // 从450增加到600
            backgroundRect = new Rectangle(100, 100, width, height);

            clientRect = new Rectangle(
                backgroundRect.X + padding,
                backgroundRect.Y + 40,
                backgroundRect.Width - padding * 2,
                backgroundRect.Height - 40 - 50
            );

            saveButtonRect = new Rectangle(
                backgroundRect.X + padding,
                backgroundRect.Bottom - 40,
                80,
                30
            );

            cancelButtonRect = new Rectangle(
                backgroundRect.Right - padding - 80,
                backgroundRect.Bottom - 40,
                80,
                30
            );
            
            // 不加载背景纹理，使用纯色背景
            backgroundTexture = null;
            
            // 创建备用纹理 (1x1 白色)
            try 
            {
                if (Session.MainGame.GraphicsDevice != null) 
                {
                    fallbackTexture = new Texture2D(Session.MainGame.GraphicsDevice, 1, 1);
                    fallbackTexture.SetData(new Color[] { Color.White });
                }
            }
            catch { }
        }

        public void UseDefaultConfiguration()
        {
            // Use default texture-based rendering
        }

        private Color backgroundColor = new Color(30, 30, 40, 220);
        private Color borderColor = new Color(100, 100, 120);
        private Color titleColor = Color.Gold;
        private Color fieldBackgroundColor = new Color(40, 45, 55);
        private Color fieldSelectedColor = new Color(60, 80, 100);
        private Color labelColor = Color.White;
        private Color valueColor = Color.LightGray;
        private Color valueSelectedColor = Color.Yellow;
        private Color saveButtonColor = new Color(30, 80, 30);
        private Color saveButtonHoverColor = new Color(50, 120, 50);
        private Color cancelButtonColor = new Color(80, 30, 30);
        private Color cancelButtonHoverColor = new Color(120, 50, 50);
        private Color saveButtonCurrentColor;
        private Color cancelButtonCurrentColor;

        private int frameWidth = 450;  // 从350增加到450
        private int frameHeight = 600; // 从450增加到600
        private int padding = 15;      // 从10增加到15，增加内边距
        private int fieldHeight = 32;  // 从30增加到32，增加字段高度
        private int labelWidth = 120;  // 从100增加到120
        private int valueWidth = 150;  // 从120增加到150
        private float fontSize = 1.0f;

        public void LoadFromXML(XmlNode root)
                {
                    if (root == null) return;

                    var totalSw = System.Diagnostics.Stopwatch.StartNew();

                    try 
                    {
                        // 1. Frame
                        var frameSw = System.Diagnostics.Stopwatch.StartNew();
                        XmlNode frameNode = root.SelectSingleNode("Frame");
                        if (frameNode != null)
                        {
                            if (frameNode.Attributes["Width"] != null) frameWidth = int.Parse(frameNode.Attributes["Width"].Value);
                            if (frameNode.Attributes["Height"] != null) frameHeight = int.Parse(frameNode.Attributes["Height"].Value);
                            if (frameNode.Attributes["Padding"] != null) padding = int.Parse(frameNode.Attributes["Padding"].Value);

                            XmlNode bgNode = frameNode.SelectSingleNode("Background");
                            if (bgNode != null)
                            {
                                if (bgNode.Attributes["Color"] != null) backgroundColor = StaticMethods.LoadColor(bgNode.Attributes["Color"].Value);
                                if (bgNode.Attributes["BorderColor"] != null) borderColor = StaticMethods.LoadColor(bgNode.Attributes["BorderColor"].Value);
                            }

                            XmlNode titleNode = frameNode.SelectSingleNode("Title");
                            if (titleNode != null)
                            {
                                if (titleNode.Attributes["Color"] != null) titleColor = StaticMethods.LoadColor(titleNode.Attributes["Color"].Value);
                                // FontSize handling if needed
                            }
                        }
                        frameSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[EditorFrame.LoadFromXML] Frame 解析耗时: {frameSw.ElapsedMilliseconds} ms");

                        // 2. Fields
                        var fieldsSw = System.Diagnostics.Stopwatch.StartNew();
                        XmlNode fieldsNode = root.SelectSingleNode("Fields");
                        if (fieldsNode != null)
                        {
                            if (fieldsNode.Attributes["FieldHeight"] != null) fieldHeight = int.Parse(fieldsNode.Attributes["FieldHeight"].Value);
                            if (fieldsNode.Attributes["LabelWidth"] != null) labelWidth = int.Parse(fieldsNode.Attributes["LabelWidth"].Value);
                            if (fieldsNode.Attributes["ValueWidth"] != null) valueWidth = int.Parse(fieldsNode.Attributes["ValueWidth"].Value);

                            XmlNode fieldBgNode = fieldsNode.SelectSingleNode("FieldBackground");
                            if (fieldBgNode != null)
                            {
                                if (fieldBgNode.Attributes["Color"] != null) fieldBackgroundColor = StaticMethods.LoadColor(fieldBgNode.Attributes["Color"].Value);
                                if (fieldBgNode.Attributes["SelectedColor"] != null) fieldSelectedColor = StaticMethods.LoadColor(fieldBgNode.Attributes["SelectedColor"].Value);
                            }

                            XmlNode labelNode = fieldsNode.SelectSingleNode("Label");
                            if (labelNode != null && labelNode.Attributes["Color"] != null) labelColor = StaticMethods.LoadColor(labelNode.Attributes["Color"].Value);

                            XmlNode valueNode = fieldsNode.SelectSingleNode("Value");
                            if (valueNode != null)
                            {
                                if (valueNode.Attributes["Color"] != null) valueColor = StaticMethods.LoadColor(valueNode.Attributes["Color"].Value);
                                if (valueNode.Attributes["SelectedColor"] != null) valueSelectedColor = StaticMethods.LoadColor(valueNode.Attributes["SelectedColor"].Value);
                            }
                        }
                        fieldsSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[EditorFrame.LoadFromXML] Fields 解析耗时: {fieldsSw.ElapsedMilliseconds} ms");

                        // 3. Buttons
                        var buttonsSw = System.Diagnostics.Stopwatch.StartNew();
                        XmlNode btnNode = root.SelectSingleNode("Buttons");
                        if (btnNode != null)
                        {
                            XmlNode saveNode = btnNode.SelectSingleNode("Save");
                            if (saveNode != null)
                            {
                                 if (saveNode.Attributes["Color"] != null) saveButtonColor = StaticMethods.LoadColor(saveNode.Attributes["Color"].Value);
                                 if (saveNode.Attributes["HoverColor"] != null) saveButtonHoverColor = StaticMethods.LoadColor(saveNode.Attributes["HoverColor"].Value);
                            }

                            XmlNode cancelNode = btnNode.SelectSingleNode("Cancel");
                            if (cancelNode != null)
                            {
                                 if (cancelNode.Attributes["Color"] != null) cancelButtonColor = StaticMethods.LoadColor(cancelNode.Attributes["Color"].Value);
                                 if (cancelNode.Attributes["HoverColor"] != null) cancelButtonHoverColor = StaticMethods.LoadColor(cancelNode.Attributes["HoverColor"].Value);
                            }
                        }
                        buttonsSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[EditorFrame.LoadFromXML] Buttons 解析耗时: {buttonsSw.ElapsedMilliseconds} ms");

                        // 4. Layout Recalculation
                        var layoutSw = System.Diagnostics.Stopwatch.StartNew();
                        RecalculateLayout();
                        layoutSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[EditorFrame.LoadFromXML] RecalculateLayout 耗时: {layoutSw.ElapsedMilliseconds} ms");

                        // Set initial button colors
                        saveButtonCurrentColor = saveButtonColor;
                        cancelButtonCurrentColor = cancelButtonColor;

                        totalSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[EditorFrame.LoadFromXML] ===== 总耗时: {totalSw.ElapsedMilliseconds} ms =====");
                    }
                    catch (Exception ex)
                    {
                        totalSw.Stop();
                        System.Diagnostics.Debug.WriteLine($"[EditorFrame.LoadFromXML] ❌ 异常退出，总耗时: {totalSw.ElapsedMilliseconds} ms");
                        System.Diagnostics.Debug.WriteLine($"[EditorFrame.LoadFromXML] 异常类型: {ex.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"[EditorFrame.LoadFromXML] 异常消息: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[EditorFrame.LoadFromXML] 堆栈跟踪: {ex.StackTrace}");
                        // Fallback to defaults on error
                    }
                }


        public void SetScreen(Screen screen)
        {
            this.screen = screen;
            RecalculateLayout();
        }

        public void SetPosition(ShowPosition showPosition)
        {
            RecalculateLayout();
        }

        public void SetNumberInputer(INumberInputer iNumberInputer)
        {
            this.numberInputer = iNumberInputer;
        }

        public void SetEditTarget(object target, EditorType type)
        {
            editTarget = target;
            editorType = type;
            fields.Clear();

            switch (type)
            {
                case EditorType.Person:
                    SetupPersonEditor((target is Person ? (Person)target : null));
                    break;
                case EditorType.Architecture:
                    SetupArchitectureEditor((target is Architecture ? (Architecture)target : null));
                    break;
                case EditorType.Faction:
                    SetupFactionEditor((target is Faction ? (Faction)target : null));
                    break;
                case EditorType.Military:
                    SetupMilitaryEditor((target is Military ? (Military)target : null));
                    break;
                case EditorType.Troop:
                    SetupTroopEditor((target is Troop ? (Troop)target : null));
                    break;
            }

            scrollOffset = 0;
        }

        #region Editor Setup by Type

        private void SetupPersonEditor(Person person)
        {
            if (person == null) return;
            title = $"武将编辑 - {person.Name}";

            // 基本信息
            fields.Add(EditorField.ReadOnly("姓名", "Name", () => person.Name));
            fields.Add(EditorField.ReadOnly("字", "CalledName", () => person.CalledName));
            fields.Add(EditorField.ReadOnly("年龄", "Age", () => person.Age));
            
            // 五维属性 (可编辑)
            fields.Add(new EditorField("统率", "Command", () => person.Command, v => person.Command = (int)v, EditorFieldType.Number, 0, 100));
            fields.Add(new EditorField("武力", "Strength", () => person.Strength, v => person.Strength = (int)v, EditorFieldType.Number, 0, 100));
            fields.Add(new EditorField("智力", "Intelligence", () => person.Intelligence, v => person.Intelligence = (int)v, EditorFieldType.Number, 0, 100));
            fields.Add(new EditorField("政治", "Politics", () => person.Politics, v => person.Politics = (int)v, EditorFieldType.Number, 0, 100));
            fields.Add(new EditorField("魅力", "Glamour", () => person.Glamour, v => person.Glamour = (int)v, EditorFieldType.Number, 0, 100));
            
            // 状态属性 (只读)
            fields.Add(EditorField.ReadOnly("忠诚", "Loyalty", () => person.Loyalty));
            fields.Add(EditorField.ReadOnly("功勋", "Merit", () => person.Merit));
            fields.Add(EditorField.ReadOnly("内政", "InternalWork", () => person.WorkKind.ToString()));
        }

        private void SetupArchitectureEditor(Architecture arch)
        {
            if (arch == null) return;
            title = $"城池编辑 - {arch.Name}";

            // 资源 (可编辑)
            fields.Add(new EditorField("资金", "Fund", () => arch.Fund, v => arch.Fund = (int)v, EditorFieldType.Number, 0, 99999999));
            fields.Add(new EditorField("粮草", "Food", () => arch.Food, v => arch.Food = (int)v, EditorFieldType.Number, 0, 99999999));
            fields.Add(new EditorField("人口", "Population", () => arch.Population, v => arch.Population = (int)v, EditorFieldType.Number, 0, 99999999));
            
            // 发展度 (可编辑)
            fields.Add(new EditorField("农业", "Agriculture", () => arch.Agriculture, v => arch.Agriculture = (int)v, EditorFieldType.Number, 0, 9999));
            fields.Add(new EditorField("商业", "Commerce", () => arch.Commerce, v => arch.Commerce = (int)v, EditorFieldType.Number, 0, 9999));
            fields.Add(new EditorField("技术", "Technology", () => arch.Technology, v => arch.Technology = (int)v, EditorFieldType.Number, 0, 9999));
            fields.Add(new EditorField("统治", "Domination", () => arch.Domination, v => arch.Domination = (int)v, EditorFieldType.Number, 0, 100));
            
            // 状态 (可编辑)
            fields.Add(new EditorField("民心", "Morale", () => arch.Morale, v => arch.Morale = (int)v, EditorFieldType.Number, 0, 100));
            fields.Add(new EditorField("耐久", "Endurance", () => arch.Endurance, v => arch.Endurance = (int)v, EditorFieldType.Number, 0, 999999));
            
            // 军事人口 (可编辑)
            fields.Add(new EditorField("兵源", "MilitaryPopulation", () => arch.MilitaryPopulation, v => arch.MilitaryPopulation = (int)v, EditorFieldType.Number, 0, 9999999));
            
            // 上限信息 (只读)
            fields.Add(EditorField.ReadOnly("资金上限", "FundCeiling", () => arch.FundCeiling));
            fields.Add(EditorField.ReadOnly("粮草上限", "FoodCeiling", () => arch.FoodCeiling));
            fields.Add(EditorField.ReadOnly("农业上限", "AgricultureCeiling", () => arch.AgricultureCeiling));
            fields.Add(EditorField.ReadOnly("商业上限", "CommerceCeiling", () => arch.CommerceCeiling));
            fields.Add(EditorField.ReadOnly("技术上限", "TechnologyCeiling", () => arch.TechnologyCeiling));
        }

        private void SetupFactionEditor(Faction faction)
        {
            if (faction == null) return;
            title = $"势力编辑 - {faction.Name}";

            // 基本信息 (只读)
            fields.Add(EditorField.ReadOnly("势力名", "Name", () => faction.Name));
            fields.Add(EditorField.ReadOnly("君主", "Leader", () => faction.Leader?.Name ?? "无"));
            fields.Add(EditorField.ReadOnly("军师", "Advisor", () => faction.Advisor?.Name ?? "无"));
            
            // 资源 (可编辑)
            fields.Add(new EditorField("声望", "Reputation", () => faction.Reputation, v => faction.Reputation = (int)v, EditorFieldType.Number, 0, 9999999));
            fields.Add(new EditorField("技巧点", "TechniquePoint", () => faction.TechniquePoint, v => faction.TechniquePoint = (int)v, EditorFieldType.Number, 0, 9999999));
            
            // 统计信息 (只读)
            fields.Add(EditorField.ReadOnly("城池数", "ArchitectureCount", () => faction.ArchitectureCount));
            fields.Add(EditorField.ReadOnly("武将数", "PersonCount", () => faction.PersonCount));
            fields.Add(EditorField.ReadOnly("总军队", "MilitaryCount", () => faction.MilitaryCount));
            fields.Add(EditorField.ReadOnly("部队数", "TroopCount", () => faction.TroopCount));
            fields.Add(EditorField.ReadOnly("总人口", "Population", () => faction.Population));
        }

        private void SetupMilitaryEditor(Military military)
        {
            if (military == null) return;
            title = $"编队编辑 - {military.Name}";

            // 基本信息 (只读)
            fields.Add(EditorField.ReadOnly("名称", "Name", () => military.Name));
            fields.Add(EditorField.ReadOnly("兵种", "Kind", () => military.Kind?.Name ?? "未知"));
            fields.Add(EditorField.ReadOnly("将领", "Leader", () => military.Leader?.Name ?? "无"));
            
            // 兵力 (可编辑)
            fields.Add(new EditorField("兵力", "Quantity", () => military.Quantity, v => military.Quantity = (int)v, EditorFieldType.Number, 0, 999999));
            fields.Add(new EditorField("士气", "Morale", () => military.Morale, v => military.Morale = (int)v, EditorFieldType.Number, 0, 100));
            fields.Add(new EditorField("战意", "Combativity", () => military.Combativity, v => military.Combativity = (int)v, EditorFieldType.Number, 0, 200));
            fields.Add(new EditorField("经验", "Experience", () => military.Experience, v => military.Experience = (int)v, EditorFieldType.Number, 0, 999999));
            
            // 上限信息 (只读)
            fields.Add(EditorField.ReadOnly("兵力上限", "MaxQuantity", () => military.Kind?.MaxScale ?? 0));
        }

        private void SetupTroopEditor(Troop troop)
        {
            if (troop == null) return;
            title = $"部队编辑 - {troop.Name}";

            // 基本信息 (只读)
            fields.Add(EditorField.ReadOnly("名称", "Name", () => troop.Name));
            fields.Add(EditorField.ReadOnly("主将", "Leader", () => troop.Leader?.Name ?? "无"));
            fields.Add(EditorField.ReadOnly("兵种", "Kind", () => troop.Army?.Kind?.Name ?? "未知"));
            
            // 战斗属性 (可编辑)
            fields.Add(new EditorField("兵力", "Quantity", () => troop.Quantity, v => { if (troop.Army != null) troop.Army.Quantity = (int)v; }, EditorFieldType.Number, 0, 999999));
            fields.Add(new EditorField("士气", "Morale", () => troop.Morale, v => troop.Morale = (int)v, EditorFieldType.Number, 0, 100));
            fields.Add(new EditorField("战意", "Combativity", () => troop.Combativity, v => troop.Combativity = (int)v, EditorFieldType.Number, 0, 200));
            
            // 资源 (可编辑)
            fields.Add(new EditorField("粮草", "Food", () => troop.Food, v => troop.Food = (int)v, EditorFieldType.Number, 0, 9999999));
            fields.Add(new EditorField("资金", "Fund", () => troop.Fund, v => troop.Fund = (int)v, EditorFieldType.Number, 0, 9999999));
            
            // 状态信息 (只读)
            fields.Add(EditorField.ReadOnly("战斗力", "FightingForce", () => troop.FightingForce));
            fields.Add(EditorField.ReadOnly("移动力", "Movability", () => troop.Movability));
            fields.Add(EditorField.ReadOnly("自动", "Auto", () => troop.Auto ? "是" : "否"));
        }

        #endregion

        private void RecalculateLayout()
        {
            if (screen == null) return;

            int screenWidth = screen.viewportSize.X;
            int screenHeight = screen.viewportSize.Y;

            // 将编辑器定位到右上角，留出一些边距
            int margin = 20;
            backgroundRect = new Rectangle(
                screenWidth - frameWidth - margin,  // 右上角X位置
                margin,                             // 右上角Y位置
                frameWidth,
                frameHeight
            );

            RecalculateClientRect();
        }

        private void RecalculateClientRect()
        {
            clientRect = new Rectangle(
                backgroundRect.X + padding,
                backgroundRect.Y + 40,
                backgroundRect.Width - padding * 2,
                backgroundRect.Height - 40 - 50
            );

            saveButtonRect = new Rectangle(
                backgroundRect.X + padding,
                backgroundRect.Bottom - 40,
                80,
                30
            );

            cancelButtonRect = new Rectangle(
                backgroundRect.Right - padding - 80,
                backgroundRect.Bottom - 40,
                80,
                30
            );
        }

        public void Show()
        {
            IsShowing = true;
            scrollOffset = 0;
            hoveredField = null;
            RecalculateLayout();
            CalculateMaxScroll();
        }

        public void Hide()
        {
            IsShowing = false;
            selectedField = null;
            hoveredField = null;
            isDraggingScrollbar = false;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsShowing) return;

            // Calculate max scroll based on content
            CalculateMaxScroll();
        }

        private void CalculateMaxScroll()
        {
            int totalContentHeight = fields.Count * fieldHeight;
            int visibleHeight = clientRect.Height;
            maxScrollOffset = Math.Max(0, totalContentHeight - visibleHeight);
            
            // Clamp scroll offset
            scrollOffset = Math.Min(scrollOffset, maxScrollOffset);
            scrollOffset = Math.Max(0, scrollOffset);
            
            // Calculate scrollbar thumb position
            if (maxScrollOffset > 0)
            {
                int scrollbarHeight = clientRect.Height - 20;
                int thumbHeight = Math.Max(30, (int)(scrollbarHeight * ((float)visibleHeight / totalContentHeight)));
                int thumbY = (int)(scrollbarHeight * ((float)scrollOffset / (totalContentHeight)));
                
                scrollThumbRect = new Rectangle(
                    scrollbarRect.X + 2,
                    scrollbarRect.Y + thumbY + 10,
                    scrollbarRect.Width - 4,
                    thumbHeight
                );
            }
        }

        public void HandleMouseScroll(int scrollValue)
        {
            if (!IsShowing) return;
            
            if (scrollValue > 0)
            {
                scrollOffset = Math.Max(0, scrollOffset - ScrollStep);
            }
            else if (scrollValue < 0)
            {
                scrollOffset = Math.Min(maxScrollOffset, scrollOffset + ScrollStep);
            }
        }

        public void HandleMouseMove(Point position)
        {
            if (!IsShowing) return;
            
            // Update button hover states
            saveButtonHover = saveButtonRect.Contains(position);
            cancelButtonHover = cancelButtonRect.Contains(position);
            
            // Update field hover
            int fieldIndex = GetFieldIndexAtPosition(position);
            hoveredField = (fieldIndex >= 0 && fieldIndex < fields.Count) ? fields[fieldIndex] : null;
        }

        public bool HandleMouseClick(Point position)
        {
            if (!IsShowing) return false;
            
            // System.Diagnostics.Debug.WriteLine($"[EditorFrame] HandleMouseClick at ({position.X}, {position.Y})");
            // System.Diagnostics.Debug.WriteLine($"[EditorFrame] Save button rect: {saveButtonRect}");
            // System.Diagnostics.Debug.WriteLine($"[EditorFrame] Cancel button rect: {cancelButtonRect}");
            
            // Check save button
            if (saveButtonRect.Contains(position))
            {
                // System.Diagnostics.Debug.WriteLine("[EditorFrame] Save button clicked!");
                // System.Diagnostics.Debug.WriteLine($"[EditorFrame] OnSave event is null: {OnSave == null}");
                OnSave?.Invoke();
                return true;
            }
            
            // Check cancel button
            if (cancelButtonRect.Contains(position))
            {
                // System.Diagnostics.Debug.WriteLine("[EditorFrame] Cancel button clicked!");
                // System.Diagnostics.Debug.WriteLine($"[EditorFrame] OnCancel event is null: {OnCancel == null}");
                OnCancel?.Invoke();
                return true;
            }
            
            // Check if clicked on a field
            int fieldIndex = GetFieldIndexAtPosition(position);
            if (fieldIndex >= 0 && fieldIndex < fields.Count)
            {
                // System.Diagnostics.Debug.WriteLine($"[EditorFrame] Field {fieldIndex} clicked: {fields[fieldIndex].DisplayName}");
                selectedField = fields[fieldIndex];
                OnFieldClicked(selectedField);
                return true;
            }
            
            // System.Diagnostics.Debug.WriteLine("[EditorFrame] Click not handled");
            return false;
        }

        private int GetFieldIndexAtPosition(Point pos)
        {
            if (!clientRect.Contains(pos)) return -1;

            int relativeY = pos.Y - clientRect.Y + scrollOffset;
            int index = relativeY / fieldHeight;

            if (index >= 0 && index < fields.Count)
            {
                return index;
            }
            return -1;
        }

        private void OnFieldClicked(EditorField field)
        {
            if (field == null) return;

            if (field.FieldType == EditorFieldType.Number && numberInputer != null)
            {
                // Use NumberInputer for numeric fields
                numberInputer.SetMax(field.MaxValue);
                numberInputer.SetEnterFunction(() =>
                {
                    field.SetValue(numberInputer.Number);
                });
                numberInputer.SetMapPosition(ShowPosition.Center);
                numberInputer.IsShowing = true;
            }
        }

        #region Copy/Paste 复制粘贴功能

        /// <summary>
        /// 处理键盘输入 (Ctrl+C/V)
        /// </summary>
        public void HandleKeyboard()
        {
            if (!IsShowing) return;
            
            bool ctrlDown = InputManager.KeyBoardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                           InputManager.KeyBoardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);
            
            if (ctrlDown)
            {
                // Ctrl+C: 复制
                if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.C))
                {
                    CopyAllFields();
                }
                // Ctrl+V: 粘贴
                else if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.V))
                {
                    PasteAllFields();
                }
            }
            
            // 更新状态消息计时器
            if (statusMessageTimer > 0)
            {
                statusMessageTimer -= 0.016f; // 约60fps
                if (statusMessageTimer <= 0)
                {
                    statusMessage = "";
                }
            }
        }

        /// <summary>
        /// 复制所有可编辑字段的值到剪贴板
        /// </summary>
        public void CopyAllFields()
        {
            clipboard.Clear();
            clipboardType = editorType;
            
            int copiedCount = 0;
            foreach (var field in fields)
            {
                if (field.FieldType != EditorFieldType.ReadOnly)
                {
                    var value = field.GetValue();
                    if (value != null)
                    {
                        clipboard[field.PropertyName] = value;
                        copiedCount++;
                    }
                }
            }
            
            ShowStatusMessage($"已复制 {copiedCount} 个字段");
        }

        /// <summary>
        /// 复制单个选中字段的值
        /// </summary>
        public void CopySingleField()
        {
            if (selectedField == null || selectedField.FieldType == EditorFieldType.ReadOnly)
            {
                ShowStatusMessage("无法复制只读字段");
                return;
            }
            
            clipboard.Clear();
            clipboardType = editorType;
            
            var value = selectedField.GetValue();
            if (value != null)
            {
                clipboard[selectedField.PropertyName] = value;
                ShowStatusMessage($"已复制: {selectedField.DisplayName} = {value}");
            }
        }

        /// <summary>
        /// 粘贴剪贴板中的值到当前对象
        /// </summary>
        public void PasteAllFields()
        {
            if (clipboard.Count == 0)
            {
                ShowStatusMessage("剪贴板为空");
                return;
            }
            
            if (clipboardType != editorType)
            {
                ShowStatusMessage($"类型不匹配: 剪贴板={clipboardType}, 当前={editorType}");
                return;
            }
            
            int pastedCount = 0;
            foreach (var field in fields)
            {
                if (field.FieldType != EditorFieldType.ReadOnly && clipboard.ContainsKey(field.PropertyName))
                {
                    try
                    {
                        field.SetValue(clipboard[field.PropertyName]);
                        pastedCount++;
                    }
                    catch
                    {
                        // 忽略类型转换错误
                    }
                }
            }
            
            ShowStatusMessage($"已粘贴 {pastedCount} 个字段");
        }

        /// <summary>
        /// 显示状态消息
        /// </summary>
        private void ShowStatusMessage(string message)
        {
            statusMessage = message;
            statusMessageTimer = 2.0f; // 显示2秒
        }

        #endregion

        public void Draw()
        {
            if (!IsShowing) 
            {
                // System.Diagnostics.Debug.WriteLine("[EditorFrame] Draw skipped: IsShowing is false");
                return;
            }
            if (screen == null) 
            {
                // System.Diagnostics.Debug.WriteLine("[EditorFrame] Draw skipped: Screen is null");
                return;
            }

            // ★★★ 修复：自动检测并纠正无效的布局 ★★★
            // 如果X坐标小于0（说明初始化时屏幕宽度可能为0），或者背景框宽度不对，强制重算
            if (backgroundRect.X < 0 || backgroundRect.Width != frameWidth)
            {
                // System.Diagnostics.Debug.WriteLine($"[EditorFrame] 检测到无效布局 (Rect:{backgroundRect}, Screen:{screen.viewportSize.X}x{screen.viewportSize.Y})，强制重算布局");
                RecalculateLayout();
                // 重算后再次打印以确认
                //  System.Diagnostics.Debug.WriteLine($"[EditorFrame] 重算后布局: {backgroundRect}");
            }

            // System.Diagnostics.Debug.WriteLine($"[EditorFrame] Viewport: {screen.viewportSize}, Frame Size: {frameWidth}x{frameHeight}");
            // System.Diagnostics.Debug.WriteLine($"[EditorFrame] BackgroundRect: {backgroundRect}");
            
            // 允许在没有字体的情况下仍然绘制背景和按钮
            SpriteFont useFont = GetFont();
            bool hasFont = useFont != null;

            try
            {
                // Draw background
                DrawBackground();

                // Draw title (only if font available)
                if (hasFont) DrawTitle();

                // Draw fields (only if font available)
                if (hasFont) DrawFields();
                
                // Draw scrollbar if needed
                if (maxScrollOffset > 0)
                {
                    DrawScrollbar();
                }

                // Draw buttons
                DrawButtons();
            }
            catch (System.Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[EditorFrame] Draw exception: {ex.Message}");
            }
        }

        private Texture2D fallbackTexture;

        private void DrawBackground()
        {
            // 使用系统白色纹理绘制背景
            try 
            {
                PlatformTexture whiteTexture = null;
                
                if (Session.MainGame.mainGameScreen != null && 
                    Session.MainGame.mainGameScreen.Textures != null && 
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures != null &&
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 0)
                {
                     whiteTexture = Session.MainGame.mainGameScreen.Textures.TileFrameTextures[0];
                }

                // 如果没有找到系统纹理，使用备用纹理
                if (whiteTexture == null && fallbackTexture != null)
                {
                    // 检查SpriteBatch是否可用
                    if (Session.Current?.SpriteBatch == null)
                    {
                        // System.Diagnostics.Debug.WriteLine("[EditorFrame] ⚠️ SpriteBatch is null, cannot draw with fallback");
                        return;
                    }
                    
                    // 使用原生MonoGame Draw
                    Session.Current.SpriteBatch.Draw(fallbackTexture, backgroundRect, null, backgroundColor, 0f, Vector2.Zero, SpriteEffects.None, 0.2f);
                    
                    // 边框
                    Color border = borderColor;
                    int borderThickness = 3;
                    Session.Current.SpriteBatch.Draw(fallbackTexture, new Rectangle(backgroundRect.X, backgroundRect.Y, backgroundRect.Width, borderThickness), null, border, 0f, Vector2.Zero, SpriteEffects.None, 0.19f);
                    Session.Current.SpriteBatch.Draw(fallbackTexture, new Rectangle(backgroundRect.X, backgroundRect.Bottom - borderThickness, backgroundRect.Width, borderThickness), null, border, 0f, Vector2.Zero, SpriteEffects.None, 0.19f);
                    Session.Current.SpriteBatch.Draw(fallbackTexture, new Rectangle(backgroundRect.X, backgroundRect.Y, borderThickness, backgroundRect.Height), null, border, 0f, Vector2.Zero, SpriteEffects.None, 0.19f);
                    Session.Current.SpriteBatch.Draw(fallbackTexture, new Rectangle(backgroundRect.Right - borderThickness, backgroundRect.Y, borderThickness, backgroundRect.Height), null, border, 0f, Vector2.Zero, SpriteEffects.None, 0.19f);
                    
                    // 标题栏
                    Session.Current.SpriteBatch.Draw(fallbackTexture, new Rectangle(backgroundRect.X + borderThickness, backgroundRect.Y + borderThickness, 
                                                             backgroundRect.Width - borderThickness * 2, 35), null, new Color(50, 50, 70, 200), 0f, Vector2.Zero, SpriteEffects.None, 0.18f);
                                                             
                    // System.Diagnostics.Debug.WriteLine("[EditorFrame] ✓ Drew background with fallback texture");
                    return; // 处理完毕
                }

                if (whiteTexture != null)
                {
                        // 绘制主背景
                        CacheManager.Draw(whiteTexture, backgroundRect, null, backgroundColor, 0f, Vector2.Zero, SpriteEffects.None, 0.2f);
                        
                        // 绘制边框
                        Color border = borderColor;
                        int borderThickness = 3; // 增加边框厚度
                        
                        // 上边框
                        CacheManager.Draw(whiteTexture, new Rectangle(backgroundRect.X, backgroundRect.Y, backgroundRect.Width, borderThickness), null, border, 0f, Vector2.Zero, SpriteEffects.None, 0.19f);
                        // 下边框
                        CacheManager.Draw(whiteTexture, new Rectangle(backgroundRect.X, backgroundRect.Bottom - borderThickness, backgroundRect.Width, borderThickness), null, border, 0f, Vector2.Zero, SpriteEffects.None, 0.19f);
                        // 左边框
                        CacheManager.Draw(whiteTexture, new Rectangle(backgroundRect.X, backgroundRect.Y, borderThickness, backgroundRect.Height), null, border, 0f, Vector2.Zero, SpriteEffects.None, 0.19f);
                        // 右边框
                        CacheManager.Draw(whiteTexture, new Rectangle(backgroundRect.Right - borderThickness, backgroundRect.Y, borderThickness, backgroundRect.Height), null, border, 0f, Vector2.Zero, SpriteEffects.None, 0.19f);
                        
                        // 绘制标题栏背景
                        Rectangle titleBarRect = new Rectangle(backgroundRect.X + borderThickness, backgroundRect.Y + borderThickness, 
                                                             backgroundRect.Width - borderThickness * 2, 35);
                        CacheManager.Draw(whiteTexture, titleBarRect, null, new Color(50, 50, 70, 200), 0f, Vector2.Zero, SpriteEffects.None, 0.18f);
                        

                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("[EditorFrame] ⚠️ DrawBackground skipped: both whiteTexture and fallbackTexture are null");
                }
            }
            catch (Exception ex) 
            {
                // System.Diagnostics.Debug.WriteLine($"[EditorFrame] ❌ DrawBackground failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void DrawTitle()
        {
            var font = GetFont();
            if (font == null) return;
            
            Vector2 titlePos = new Vector2(backgroundRect.X + padding, backgroundRect.Y + 10);
            CacheManager.DrawString(font, title, titlePos, Color.Gold, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.19f);
            
            // Draw field count info
            string countInfo = $"({fields.Count}项)";
            Vector2 countPos = new Vector2(backgroundRect.Right - padding - 60, backgroundRect.Y + 12);
            CacheManager.DrawString(font, countInfo, countPos, Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0.19f);
        }

        private void DrawFields()
        {
            var font = GetFont();
            if (font == null) return;
            
            int yOffset = 0;
            int index = 0;
            foreach (var field in fields)
            {
                int fieldY = clientRect.Y + yOffset - scrollOffset;

                // Only draw visible fields
                if (fieldY + fieldHeight > clientRect.Y && fieldY < clientRect.Bottom)
                {
                    Rectangle fieldRect = new Rectangle(clientRect.X, fieldY, clientRect.Width - 15, fieldHeight - 2);
                    DrawField(field, fieldRect, index % 2 == 0);
                }

                yOffset += fieldHeight;
                index++;
            }
        }

        private void DrawScrollbar()
        {
            if (Session.Current?.Font == null) return;
            
            // Draw scroll indicator
            int totalItems = fields.Count;
            int visibleStart = scrollOffset / fieldHeight;
            int visibleEnd = Math.Min(totalItems, visibleStart + (clientRect.Height / fieldHeight) + 1);
            
            string scrollInfo = $"▲▼ {visibleStart + 1}-{visibleEnd}/{totalItems}";
            Vector2 scrollInfoPos = new Vector2(clientRect.Right - 80, clientRect.Bottom + 5);
            CacheManager.DrawString(Session.Current.Font, scrollInfo, scrollInfoPos, Color.Gray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0.17f);
        }

        private void DrawField(EditorField field, Rectangle rect, bool alternate)
        {
            bool isSelected = field == selectedField;
            bool isHovered = field == hoveredField;
            bool isReadOnly = field.FieldType == EditorFieldType.ReadOnly;

            // 绘制字段背景
            try 
            {
                PlatformTexture whiteTexture = null;
                if (Session.MainGame.mainGameScreen != null && 
                    Session.MainGame.mainGameScreen.Textures != null && 
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures != null &&
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 0)
                {
                    whiteTexture = Session.MainGame.mainGameScreen.Textures.TileFrameTextures[0];
                }

                Color fieldBg;
                if (isSelected)
                    fieldBg = fieldSelectedColor;
                else if (isHovered && !isReadOnly)
                    fieldBg = new Color(50, 60, 80, 150);
                else if (alternate)
                    fieldBg = new Color(45, 50, 60, 100);
                else
                    fieldBg = fieldBackgroundColor;

                if (whiteTexture != null)
                {
                    CacheManager.Draw(whiteTexture, rect, null, fieldBg, 0f, Vector2.Zero, SpriteEffects.None, 0.17f);
                }
                else if (fallbackTexture != null)
                {
                    Session.Current.SpriteBatch.Draw(fallbackTexture, rect, null, fieldBg, 0f, Vector2.Zero, SpriteEffects.None, 0.17f);
                }
            }
            catch { }

            // Determine label color based on state
            Color labelColor;
            if (isSelected)
                labelColor = Color.Yellow;
            else if (isHovered && !isReadOnly)
                labelColor = Color.LightCyan;
            else if (isReadOnly)
                labelColor = Color.Gray;
            else
                labelColor = Color.White;

            // Label with icon for editable fields
            string labelPrefix = isReadOnly ? "  " : "▸ ";
            Vector2 labelPos = new Vector2(rect.X + 5, rect.Y + 6);
            CacheManager.DrawString(GetFont(), labelPrefix + field.DisplayName, labelPos, labelColor, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0.16f);

            // Value
            string valueStr = field.GetValue()?.ToString() ?? "";
            Vector2 valuePos = new Vector2(rect.X + labelWidth + 20, rect.Y + 6);
            
            Color valueColor;
            if (isSelected)
                valueColor = Color.Yellow;
            else if (isHovered && !isReadOnly)
                valueColor = Color.Cyan;
            else if (isReadOnly)
                valueColor = Color.DarkGray;
            else
                valueColor = Color.LightGreen;
                
            CacheManager.DrawString(GetFont(), valueStr, valuePos, valueColor, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0.16f);
        }

        private void DrawButtons()
        {
            var font = GetFont();
            if (font == null) return;
            
            // 绘制按钮背景
            try 
            {
                PlatformTexture whiteTexture = null;
                if (Session.MainGame.mainGameScreen != null && 
                    Session.MainGame.mainGameScreen.Textures != null && 
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures != null &&
                    Session.MainGame.mainGameScreen.Textures.TileFrameTextures.Length > 0)
                {
                    whiteTexture = Session.MainGame.mainGameScreen.Textures.TileFrameTextures[0];
                }

                if (whiteTexture != null)
                {
                    // 保存按钮背景
                    Color saveBg = saveButtonHover ? saveButtonHoverColor : saveButtonColor;
                    CacheManager.Draw(whiteTexture, saveButtonRect, null, saveBg, 0f, Vector2.Zero, SpriteEffects.None, 0.16f);
                    
                    // 取消按钮背景
                    Color cancelBg = cancelButtonHover ? cancelButtonHoverColor : cancelButtonColor;
                    CacheManager.Draw(whiteTexture, cancelButtonRect, null, cancelBg, 0f, Vector2.Zero, SpriteEffects.None, 0.16f);
                }
                else if (fallbackTexture != null)
                {
                     // 保存按钮背景
                    Color saveBg = saveButtonHover ? saveButtonHoverColor : saveButtonColor;
                    Session.Current.SpriteBatch.Draw(fallbackTexture, saveButtonRect, null, saveBg, 0f, Vector2.Zero, SpriteEffects.None, 0.16f);
                    
                    // 取消按钮背景
                    Color cancelBg = cancelButtonHover ? cancelButtonHoverColor : cancelButtonColor;
                    Session.Current.SpriteBatch.Draw(fallbackTexture, cancelButtonRect, null, cancelBg, 0f, Vector2.Zero, SpriteEffects.None, 0.16f);
                }
            }
            catch { }
            
            // Save button text
            Vector2 saveTextPos = new Vector2(saveButtonRect.X + 25, saveButtonRect.Y + 8);
            Color saveColor = saveButtonHover ? Color.White : Color.LightGray;
            CacheManager.DrawString(font, "保存", saveTextPos, saveColor, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0.15f);

            // Cancel button text
            Vector2 cancelTextPos = new Vector2(cancelButtonRect.X + 25, cancelButtonRect.Y + 8);
            Color cancelColor = cancelButtonHover ? Color.White : Color.LightGray;
            CacheManager.DrawString(font, "取消", cancelTextPos, cancelColor, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0.15f);
            
            // Draw status message or copy/paste hint
            if (!string.IsNullOrEmpty(statusMessage))
            {
                Vector2 statusPos = new Vector2(backgroundRect.X + padding, saveButtonRect.Y - 25);
                CacheManager.DrawString(font, statusMessage, statusPos, Color.Cyan, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0.15f);
            }
            else
            {
                // Show copy/paste hint
                Vector2 hintPos = new Vector2(backgroundRect.X + padding, saveButtonRect.Y - 25);
                CacheManager.DrawString(font, "Ctrl+C复制 Ctrl+V粘贴", hintPos, Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0.15f);
            }
        }

        // Expose button rects for plugin to handle clicks
        public Rectangle SaveButtonRect => saveButtonRect;
        public Rectangle CancelButtonRect => cancelButtonRect;
    }
}

