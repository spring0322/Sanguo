using System;
using System.IO;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using ImGuiNET;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Monogame.Imgui.Renderer; 
using WorldOfTheThreeKingdoms; 
using GameObjects;
using WorldOfTheThreeKingdoms.GameScreens;
using Tools;
using WorldOfTheThreeKingdoms.Tools;
using GameObjects.PersonDetail;


namespace GameManager
{
    public class ImGuiManager
    {
        private Game _game;
        private ImGuiRenderer _imGuiRenderer;
        private bool _showGui = false; 
        private object _inspectTarget = null; 
        private bool _lastF11Down = false;

        private Architecture _cachedArchitecture; 
        private int _archListIndex = 0;           
        private string[] _archNamesCache;         
        private bool _needRefreshArchList = true; 
        private int _selectedKindIndex = 0; 

        // Pathfinding Debugger Fields
        private bool _showRouteways = false;
        private bool _showTroopPathNodes = true;
        private int _testStartID = 0;
        private int _testEndID = 0;
        private string _pathFindLog = "等待测试...";
        private List<System.Numerics.Vector2[]> _cachedLines = new List<System.Numerics.Vector2[]>();
        private float _refreshTimer = 0f;
        private string _testAudioName = "Theme";

        // 🎨 文字颜色与阴影调试字段 (放在类级别，避免每次重置)
        private System.Numerics.Vector4 _textColor = new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f); // 默认纯白
        private System.Numerics.Vector4 _shadowColor = new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.5f); // 默认半透黑
        private float _shadowOffset = 1.0f; // 阴影偏移量
        
        public bool WantsCapture { get; private set; }

        public ImGuiManager(Game game)
        {
            _game = game;
        }

        // 🎨 公共访问器：获取当前调试的文字颜色设置
        public System.Numerics.Vector4 GetDebugTextColor() => _textColor;
        public System.Numerics.Vector4 GetDebugShadowColor() => _shadowColor;
        public float GetDebugShadowOffset() => _shadowOffset;

        // 🎨 辅助方法：在 ImGui 中绘制带阴影的文字
        public void DrawTextWithShadow(string text)
        {
            if (_shadowOffset > 0)
            {
                var cursorPos = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new System.Numerics.Vector2(cursorPos.X + _shadowOffset, cursorPos.Y + _shadowOffset));
                ImGui.TextColored(_shadowColor, text);
                ImGui.SetCursorPos(cursorPos);
            }
            ImGui.TextColored(_textColor, text);
        }

        public void Initialize()
        {
            if (_imGuiRenderer == null)
            {
                _imGuiRenderer = new ImGuiRenderer(_game);
            }

            _game.IsMouseVisible = false; 
            var io = ImGui.GetIO();
            io.Fonts.Clear(); 

            string fontPath = ResolveImGuiFontPath();

            try 
            {
                if (!string.IsNullOrEmpty(fontPath))
                {
                    io.Fonts.AddFontFromFileTTF(fontPath, 24.0f, null, io.Fonts.GetGlyphRangesChineseFull());
                }
                else
                {
                    io.Fonts.AddFontDefault();
                }
            }
            catch 
            {
                io.Fonts.AddFontDefault();
            }

            _imGuiRenderer.RebuildFontAtlas(); 
        }

        private static string ResolveImGuiFontPath()
        {
            string fontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            if (string.IsNullOrEmpty(fontsDirectory))
            {
                return string.Empty;
            }

            string yaheiPath = Path.Combine(fontsDirectory, "msyh.ttc");
            if (File.Exists(yaheiPath))
            {
                return yaheiPath;
            }

            string simheiPath = Path.Combine(fontsDirectory, "simhei.ttf");
            if (File.Exists(simheiPath))
            {
                return simheiPath;
            }

            return string.Empty;
        }

        public void Update(GameTime gameTime)
        {
            // 1. F11开关逻辑 (带消抖保护)
            var ks = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            if (ks.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F11) && !_lastF11Down)
            {
                _showGui = !_showGui;
                _lastF11Down = true;
            }
            else if (ks.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.F11))
            {
                _lastF11Down = false;
            }

            // 2. 鼠标控制逻辑 (核心修改：上帝模式智能切换)
            if (_showGui)
            {
                var io = ImGui.GetIO();
                WantsCapture = io.WantCaptureMouse || io.WantCaptureKeyboard;

                // 🔥 智能切换：
                // 如果 ImGui 想要鼠标（悬停在面板上），就显示系统鼠标。
                // 否则（鼠标在地图上），就隐藏系统鼠标，让游戏显示自己的鼠标。
                _game.IsMouseVisible = WantsCapture;
            }
            else
            {
                WantsCapture = false;
                // 关闭上帝模式时，永远隐藏系统鼠标 (完全交给游戏原本的 UI 绘制)
                _game.IsMouseVisible = false;
            }
        }

        public void Draw(GameTime gameTime)
        {
            if (!_showGui) return;
            _imGuiRenderer.BeforeLayout(_game, gameTime);
            
            if (_showRouteways) DrawRoutewayOverlay();
            
            DrawGodModeWindow();
            _imGuiRenderer.AfterLayout();
        }

        private void DrawGodModeWindow()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 500), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("Zhsan Debugger", ref _showGui))
            {
                if (ImGui.BeginTabBar("DebugTabs"))
                {
                    if (ImGui.BeginTabItem("General"))
                    {
                        DrawGeneralTab();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("AI Inspector"))
                    {
                        DrawAIDebugger();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Object Inspector"))
                    {
                        DrawInspectorTab();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Military"))
                    {
                        DrawMilitaryManager();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Pathfinding"))
                    {
                        DrawPathfindingDebugger();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Input & Camera"))
                    {
                        DrawInputDebugger();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Time Control"))
                    {
                        DrawTimeManipulator();
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
            }
            ImGui.End();
        }

        private void DrawGeneralTab()
        {
            ImGui.Text($"FPS: {1 / _game.TargetElapsedTime.TotalSeconds:0.00}");
            ImGui.Separator();
            if (ImGui.Button("Add 10000 Gold to Player"))
            {
                var playerFaction = Session.Current?.Scenario?.CurrentPlayer;
                if (playerFaction != null && playerFaction.Architectures.Count > 0)
                {
                    (playerFaction.Architectures[0] as Architecture).Fund += 10000;
                }
            }

            ImGui.Separator();

            // 🎨 文字颜色与阴影调试面板
            if (ImGui.CollapsingHeader("调试：文字颜色与阴影"))
            {
                ImGui.TextColored(new System.Numerics.Vector4(0, 1, 1, 1), "实时调整文字渲染效果");
                ImGui.Separator();

                // 颜色选择器
                ImGui.ColorEdit4("字体颜色", ref _textColor);
                ImGui.ColorEdit4("阴影颜色", ref _shadowColor);
                ImGui.SliderFloat("阴影偏移", ref _shadowOffset, 0.0f, 3.0f);

                ImGui.Separator();

                // 显示当前配置的代码（方便复制）
                ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "当前配置代码（可直接复制）:");
                ImGui.TextWrapped($"textColor = new Vector4({_textColor.X:F2}f, {_textColor.Y:F2}f, {_textColor.Z:F2}f, {_textColor.W:F2}f);");
                ImGui.TextWrapped($"shadowColor = new Vector4({_shadowColor.X:F2}f, {_shadowColor.Y:F2}f, {_shadowColor.Z:F2}f, {_shadowColor.W:F2}f);");
                ImGui.TextWrapped($"shadowOffset = {_shadowOffset:F2}f;");

                ImGui.Separator();

                // 实时预览效果
                ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), "实时预览:");
                
                // 绘制阴影（如果偏移量大于0）
                if (_shadowOffset > 0)
                {
                    var cursorPos = ImGui.GetCursorPos();
                    ImGui.SetCursorPos(new System.Numerics.Vector2(cursorPos.X + _shadowOffset, cursorPos.Y + _shadowOffset));
                    ImGui.TextColored(_shadowColor, "示例文字 Example Text 测试123");
                    ImGui.SetCursorPos(cursorPos); // 归位
                }
                
                // 绘制本体文字
                ImGui.TextColored(_textColor, "示例文字 Example Text 测试123");

                ImGui.Separator();

                // 快速预设
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0, 1), "快速预设:");
                if (ImGui.Button("纯白 (默认)"))
                {
                    _textColor = new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                    _shadowColor = new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.5f);
                    _shadowOffset = 1.0f;
                }
                ImGui.SameLine();
                if (ImGui.Button("金色"))
                {
                    _textColor = new System.Numerics.Vector4(1.0f, 0.84f, 0.0f, 1.0f);
                    _shadowColor = new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.7f);
                    _shadowOffset = 1.5f;
                }
                ImGui.SameLine();
                if (ImGui.Button("红色"))
                {
                    _textColor = new System.Numerics.Vector4(1.0f, 0.2f, 0.2f, 1.0f);
                    _shadowColor = new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.6f);
                    _shadowOffset = 1.2f;
                }
                
                if (ImGui.Button("青色"))
                {
                    _textColor = new System.Numerics.Vector4(0.0f, 1.0f, 1.0f, 1.0f);
                    _shadowColor = new System.Numerics.Vector4(0.0f, 0.0f, 0.5f, 0.8f);
                    _shadowOffset = 1.0f;
                }
                ImGui.SameLine();
                if (ImGui.Button("无阴影"))
                {
                    _shadowOffset = 0.0f;
                }
            }
        }

        private void DrawInspectorTab()
        {
            if (Session.Current?.Scenario?.CurrentPlayer != null) 
            {
                var player = Session.Current.Scenario.CurrentPlayer;
                if (ImGui.Button("Inspect Faction")) _inspectTarget = player;
                ImGui.SameLine();
                if (ImGui.Button("Inspect Leader")) _inspectTarget = player.Leader;
            }

            if (_inspectTarget != null)
            {
                ImGui.Separator();
                if (ImGui.Button("Close Inspector")) _inspectTarget = null;
#if DEBUG
                else DrawObjectInspector(_inspectTarget);
#else
                else ImGui.Text("Inspector unavailable in Release AOT build.");
#endif
            }
        }

#if DEBUG
        private void DrawObjectInspector(object obj)
        {
            if (obj == null) return;
            Type type = obj.GetType();
            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), $"Type: {type.Name}");
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                if (field.FieldType == typeof(int))
                {
                    int val = (int)value;
                    if (ImGui.DragInt(field.Name, ref val)) field.SetValue(obj, val);
                }
                else if (field.FieldType == typeof(bool))
                {
                    bool val = (bool)value;
                    if (ImGui.Checkbox(field.Name, ref val)) field.SetValue(obj, val);
                }
                else
                {
                    ImGui.Text($"{field.Name}: {value?.ToString() ?? "null"}");
                }
            }
        }
#endif

        private void DrawMilitaryManager()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "未加载剧本！");
                return;
            }

            // ========================================================
            // 🧠 核心逻辑 1：自动锁定 & 同步 (解决鼠标移开消失的问题)
            // ========================================================
            var gameHoverArch = Session.MainGame?.mainGameScreen?.CurrentArchitecture;
            if (gameHoverArch != null)
            {
                _cachedArchitecture = gameHoverArch;
            }

            // ========================================================
            // 📋 核心逻辑 2：城池下拉列表 (通过列表选择)
            // ========================================================
            if (_needRefreshArchList || _archNamesCache == null || _archNamesCache.Length != scenario.Architectures.Count)
            {
                var archList = new System.Collections.Generic.List<string>();
                foreach (Architecture archObj in scenario.Architectures)
                {
                    archList.Add($"{archObj.Name} (ID:{archObj.ID})");
                }
                _archNamesCache = archList.ToArray();
                _needRefreshArchList = false;
            }

            if (_cachedArchitecture != null)
            {
                for (int i = 0; i < scenario.Architectures.Count; i++)
                {
                    if (scenario.Architectures[i] == _cachedArchitecture)
                    {
                        _archListIndex = i;
                        break;
                    }
                }
            }

            ImGui.Text("选择操作目标：");
            if (ImGui.Combo("##SelectArch", ref _archListIndex, _archNamesCache, _archNamesCache.Length))
            {
                if (_archListIndex >= 0 && _archListIndex < scenario.Architectures.Count)
                {
                    _cachedArchitecture = scenario.Architectures[_archListIndex] as Architecture;
                }
            }

            ImGui.Separator();

            if (_cachedArchitecture == null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "请先在地图上划过任意城池，或在上方列表选择！");
                return;
            }

            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), $"当前锁定: {_cachedArchitecture.Name} (ID: {_cachedArchitecture.ID})");
            
            var arch = _cachedArchitecture; 

            if (ImGui.CollapsingHeader("管理现有编队", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (arch.Militaries.Count == 0)
                {
                    ImGui.TextDisabled("城内空空如也");
                }
                else
                {
                    var milList = new System.Collections.Generic.List<Military>();
                    foreach (Military m in arch.Militaries) milList.Add(m);
                    
                    for (int i = 0; i < milList.Count; i++)
                    {
                        var military = milList[i];
                        string kindName = military.Kind?.Name ?? "未知兵种";

                        if (ImGui.TreeNode($"队{i+1}: {kindName} ({military.Quantity}人)###Mil{i}"))
                        {
                            int qty = military.Quantity;
                            if (ImGui.DragInt("人数", ref qty, 100, 0, 200000)) military.Quantity = qty;

                            int morale = military.Morale;
                            if (ImGui.SliderInt("士气", ref morale, 0, 200)) military.Morale = morale;

                            int comb = military.Combativity;
                            if (ImGui.SliderInt("战意", ref comb, 0, 100)) military.Combativity = comb;

                            if (ImGui.Button("解散/删除"))
                            {
                                arch.RemoveMilitary(military);
                            }
                            ImGui.TreePop();
                        }
                    }
                }
            }

            if (ImGui.CollapsingHeader("招募新编队"))
            {
                var allKinds = scenario.GameCommonData.AllMilitaryKinds;
                var kindList = new System.Collections.Generic.List<string>();
                var kindIds = new System.Collections.Generic.List<int>();
                foreach (var kv in allKinds.MilitaryKinds)
                {
                    kindList.Add($"{kv.Value.Name}");
                    kindIds.Add(kv.Key);
                }
                var kindNames = kindList.ToArray();

                if (kindNames.Length > 0)
                {
                    ImGui.Combo("选择兵种类型", ref _selectedKindIndex, kindNames, kindNames.Length);

                    if (ImGui.Button("立即生成并加入城池"))
                    {
                        try 
                        {
                            int selectedKindID = kindIds[_selectedKindIndex];
                            var kind = allKinds.GetMilitaryKind(selectedKindID);

                            var newMilitary = new Military();
                            
                            // 🔧 修复：必须先分配ID，再添加到集合
                            newMilitary.ID = Session.Current.Scenario.Militaries.GetFreeGameObjectID();
                            
                            newMilitary.KindID = selectedKindID;
                            newMilitary.Kind = kind;
                            
                            // 🔧 修复：设置名称
                            if (kind.RecruitLimit == 1)
                            {
                                newMilitary.Name = kind.Name;
                            }
                            else
                            {
                                newMilitary.Name = kind.Name + "队";
                            }
                            
                            newMilitary.Quantity = 5000;
                            newMilitary.Morale = 100;
                            newMilitary.Combativity = 100;
                            newMilitary.BelongedArchitecture = arch;
                            newMilitary.BelongedFaction = arch.BelongedFaction;
                            
                            // 🔧 修复：添加到全局集合
                            Session.Current.Scenario.Militaries.AddMilitary(newMilitary);
                            arch.AddMilitary(newMilitary);
                            
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[ImGuiManager] 创建Military成功:");
                            System.Diagnostics.Debug.WriteLine($"  ID: {newMilitary.ID}");
                            System.Diagnostics.Debug.WriteLine($"  Name: {newMilitary.Name}");
                            System.Diagnostics.Debug.WriteLine($"  Kind: {kind.Name}");
                            #endif
                        }
                        catch (System.Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ImGuiManager] 创建Military失败: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                        }
                    }
                }
            }
        }

        private string _lastAIError = "无异常";
        private string _lastAILog = "等待指令...";

        private void DrawAIDebugger()
        {
            if (ImGui.Button("Clear Log")) _lastAILog = "";
            ImGui.SameLine();
            if (ImGui.Button("Clear Error")) _lastAIError = "无异常";
            
            ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), $"Last Error: {_lastAIError}");
            ImGui.BeginChild("AILogScroll", new System.Numerics.Vector2(0, 100), true);
            ImGui.TextWrapped($"Log: {_lastAILog}");
            ImGui.EndChild();
            ImGui.Separator();

            var scenario = Session.Current?.Scenario;
            if (scenario == null) return;

            foreach (var f in scenario.Factions)
            {
                var faction = (f is Faction ? (Faction)f : null);
                if (faction == null) continue;

                var color = faction.FactionColor;
                ImGui.PushStyleColor(ImGuiCol.Header, new System.Numerics.Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 0.4f));
                bool nodeOpen = ImGui.TreeNode($"{faction.Name} (ID: {faction.ID})###Fact{faction.ID}");
                ImGui.PopStyleColor();

                if (nodeOpen)
                {
                    DrawAIDeepDiagnostic(faction);
                    ImGui.TreePop();
                }
            }
        }

        private void DrawAIDeepDiagnostic(Faction faction)
        {
            ImGui.Text($"资金: {faction.Fund} | 粮草: {faction.Food} | 科技: {faction.TechniquePoint}");
            
            // 显示外交现状
            if (ImGui.TreeNode($"Diplomacy Status###Dipl{faction.ID}"))
            {
                var scenario = Session.Current.Scenario;
                foreach (var f2 in scenario.Factions.Cast<Faction>())
                {
                    if (f2 == faction) continue;
                    var rel = scenario.DiplomaticRelations.GetDiplomaticRelation(faction.ID, f2.ID);
                    if (rel != null)
                    {
                        ImGui.Text($"{f2.Name}: 友好度 {rel.Relation}, 休战 {rel.Truce}天");
                    }
                }
                ImGui.TreePop();
            }

            ImGui.Separator();
            ImGui.TextColored(new System.Numerics.Vector4(1, 0, 1, 1), $"=== {faction.Name} AI 深度体检 ===");

            // 1. 检查战略目标 (从 Section 中获取)
            var target = faction.Sections.Cast<Section>().FirstOrDefault(s => s.OrientationArchitecture != null)?.OrientationArchitecture;
            if (target == null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "❌ 致命: AI 没有战略目标 (Target is null)");
                ImGui.Text("可能原因: 周围无敌对势力 / 距离太远 / 寻路中断");
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(0, 1, 1, 1), $"✅ 战略目标: {target.Name}");
            }

            // 2. 检查兵源有效性 (关键！)
            int validMilitaries = 0;
            int invalidMilitaries = 0;
            long totalGarrison = 0;
            
            foreach (Architecture arch in faction.Architectures)
            {
                foreach (Military mil in arch.Militaries)
                {
                    totalGarrison += mil.Quantity;
                    // 检查双向引用
                    bool linkOK = (mil.BelongedArchitecture == arch) && (mil.BelongedFaction == faction);
                    
                    if (linkOK) validMilitaries += mil.Quantity;
                    else 
                    {
                        invalidMilitaries += mil.Quantity;
                        // 红色警告
                        ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), 
                            $"⚠️ 幽灵部队发现于 {arch.Name}: {mil.Kind?.Name ?? "未知"}");
                        ImGui.SameLine();
                        ImGui.TextDisabled($"(Fact:{mil.BelongedFaction?.Name ?? "null"}, Arch:{mil.BelongedArchitecture?.Name ?? "null"})");
                    }
                }
            }
            
            ImGui.Text($"城内有效兵力: {validMilitaries}");
            if (invalidMilitaries > 0) ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), $"无效兵力: {invalidMilitaries} (AI 无法指挥)");

            // 3. 检查武将可用性
            int availableGenerals = 0;
            foreach (Person p in faction.Persons)
            {
                // 检查武将是否空闲 (只有空闲武将能带兵)
                if (p.Status == PersonStatus.Normal && p.LocationArchitecture != null)
                {
                    availableGenerals++;
                }
            }
            ImGui.Text($"可用武将: {availableGenerals} / {faction.Persons.Count}");
            if (availableGenerals == 0) ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "⚠️ 警告: 没有空闲武将带兵！");

            ImGui.Text($"出征军团 (Legions): {faction.Legions.Count}");
            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), $"城内守军 (Garrison): {totalGarrison}");

            if (ImGui.Button("强制设置目标 -> 孙坚 (示例)"))
            {
                var enemy = Session.Current.Scenario.Factions.Cast<Faction>().FirstOrDefault(f => f.Name.Contains("孙坚"));
                if (enemy != null && enemy.Architectures.Count > 0)
                {
                    var targetArch = enemy.Architectures[0] as Architecture;
                    foreach (Section section in faction.Sections)
                    {
                        section.OrientationArchitecture = targetArch;
                        section.OrientationArchitectureID = targetArch.ID;
                    }
                    _lastAILog = $"已强制设置 {faction.Name} 的战略目标为: {targetArch.Name}";
                }
            }

            ImGui.Separator();
            DrawStrategicRadar(faction);
        }

        private void DrawStrategicRadar(Faction faction)
        {
            if (ImGui.TreeNode($"Architectures ({faction.Architectures.Count})###ArchList{faction.ID}"))
            {
                foreach (Architecture arch in faction.Architectures)
                {
                    string label = $"{arch.Name} {(arch.FrontLine ? "[Front]" : "")} (P:{arch.Persons.Count}/M:{arch.Militaries.Count})";
                    if (ImGui.TreeNode($"{label}###Arch{arch.ID}"))
                    {
                        DrawAIAttackSimulation(arch);
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }
        }

        private void DrawAIAttackSimulation(Architecture arch)
        {
            ImGui.BulletText($"Fund: {arch.Fund} / Food: {arch.Food}");
            ImGui.BulletText($"Defense: {arch.Endurance}"); // / Security: {arch.Security}");
            
            if (ImGui.Button("Diagnostic AI Neighbors"))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"--- AI Thinking for {arch.Name} ---");
                try 
                {
                    foreach (var link in arch.AILandLinks)
                    {
                        var target = (link is Architecture ? (Architecture)link : null);
                        if (target == null) continue;
                        
                        bool isEnemy = !target.IsFriendly(arch.BelongedFaction);
                        sb.AppendLine($" Neighbor: {target.Name} ({(isEnemy ? "Enemy" : "Friend")})");
                        if (isEnemy)
                        {
                            sb.AppendLine($"   - Defense: {target.Endurance}, Garrison: {target.Militaries.Count} units");
                        }
                    }
                } 
                catch (Exception ex) 
                { 
                    _lastAIError = ex.Message; 
                    sb.AppendLine($" ERROR: {ex.Message}");
                }
                _lastAILog = sb.ToString();
            }
        }

        private void DrawPathfindingDebugger()
        {
            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 1, 1), "寻路系统诊断 (Pathfinding Diagnostic)");
            ImGui.Separator();

            if (ImGui.Checkbox("显示所有通航/道路连接 (Routeway Overlay)", ref _showRouteways))
            {
                if (_showRouteways) CacheRouteways();
            }
            if (_showRouteways && ImGui.Button("强制刷新缓存")) CacheRouteways();

            ImGui.Separator();

            var selectorTroops = Session.MainGame?.mainGameScreen?.SelectorTroops;
            var troop = (selectorTroops != null && selectorTroops.Count > 0) ? selectorTroops[0] as Troop : null;
            
            if (troop == null)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "请先在游戏里选中一个部队(Troop)！");
            }
            else
            {
                ImGui.Text($"选中部队: {troop.DisplayName} (ID:{troop.ID})");
                ImGui.Text($"当前坐标: {troop.Position}");
                ImGui.Text($"目标坐标: {troop.Destination}");
                
                if (ImGui.TreeNode("路径详情 (Tier Paths)"))
                {
                    DrawPathNodes("First Tier", troop.FirstTierPath);
                    DrawPathNodes("Second Tier", troop.SecondTierPath);
                    DrawPathNodes("Third Tier", troop.ThirdTierPath);
                    ImGui.TreePop();
                }

                if ((troop.FirstTierPath == null || troop.FirstTierPath.Count == 0) && troop.Position != troop.Destination)
                {
                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "⚠️ 警告：有远方目标但路径为空！(可能卡死)");
                }
            }

            ImGui.Separator();
            ImGui.Text("A* 算法实验室");
            ImGui.InputInt("起点 ID (Arch/Node)", ref _testStartID);
            ImGui.InputInt("终点 ID (Arch/Node)", ref _testEndID);

            if (ImGui.Button("运行 A* 测试"))
            {
                RunAStarTest(_testStartID, _testEndID);
            }
            ImGui.TextWrapped($"测试日志: {_pathFindLog}");
        }

        private void DrawPathNodes(string label, IReadOnlyList<Point> path)
        {
            if (path == null)
            {
                ImGui.Text($"{label}: null");
                return;
            }
            if (ImGui.TreeNode($"{label} ({path.Count} nodes)###{label}"))
            {
                for (int i = 0; i < Math.Min(path.Count, 50); i++)
                {
                    ImGui.Text($"  [{i}] {path[i]}");
                }
                if (path.Count > 50) ImGui.Text("  ... 更多省略");
                ImGui.TreePop();
            }
        }

        private void CacheRouteways()
        {
            _cachedLines.Clear();
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return;

            foreach (var r in scenario.Routeways)
            {
                Routeway route = r as Routeway;
                if (route == null || route.RoutePoints == null) continue;

                var nodes = route.RoutePoints;
                if (nodes.Count < 2) continue;

                var prev = nodes.First;
                var curr = prev.Next;
                while (curr != null)
                {
                    // 只有在已激活部分的路径才画实线，或者全部画但不同颜色？
                    // 这里我们画出全部定义的路径
                    _cachedLines.Add(new System.Numerics.Vector2[] { 
                        new System.Numerics.Vector2(prev.Value.Position.X, prev.Value.Position.Y), 
                        new System.Numerics.Vector2(curr.Value.Position.X, curr.Value.Position.Y) 
                    });
                    prev = curr;
                    curr = curr.Next;
                }
            }
        }

        private void DrawRoutewayOverlay()
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var mml = Session.MainGame?.mainGameScreen?.mainMapLayer;
            if (mml == null) return;

            var matrix = GameManager.ScreenManager.ScaleMatrix;
            
            // 获取地图当前的偏移和瓦片大小
            int tileW = mml.TileWidth;
            int tileH = mml.TileHeight;
            int leftEdge = mml.LeftEdge;
            int topEdge = mml.TopEdge;

            foreach (var line in _cachedLines)
            {
                // 转换逻辑：TilePos -> WorldPixPos -> ScreenPixPos
                float x1 = (line[0].X * tileW + leftEdge + tileW / 2) * matrix.M11;
                float y1 = (line[0].Y * tileH + topEdge + tileH / 2) * matrix.M22;
                float x2 = (line[1].X * tileW + leftEdge + tileW / 2) * matrix.M11;
                float y2 = (line[1].Y * tileH + topEdge + tileH / 2) * matrix.M22;

                drawList.AddLine(
                    new System.Numerics.Vector2(x1, y1), 
                    new System.Numerics.Vector2(x2, y2), 
                    ImGui.GetColorU32(new System.Numerics.Vector4(0, 1, 0, 0.6f)), 
                    3.0f);
            }
        }

        private void RunAStarTest(int startId, int endId)
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null) return;

            Architecture start = scenario.Architectures.GetGameObject(startId) as Architecture;
            Architecture end = scenario.Architectures.GetGameObject(endId) as Architecture;

            if (start == null || end == null)
            {
                _pathFindLog = $"错误: 找不到 ID ({startId} 或 {endId}) 对应的城市";
                return;
            }

            try
            {
                var pathfinder = new GameObjects.TroopDetail.TierPathFinder();
                var kind = scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKind(1);
                if (kind == null && scenario.GameCommonData.AllMilitaryKinds.MilitaryKinds.Count > 0)
                {
                    kind = scenario.GameCommonData.AllMilitaryKinds.GetMilitaryKindList()[0] as GameObjects.TroopDetail.MilitaryKind;
                }
                
                if (kind == null)
                {
                    _pathFindLog = "错误: 数据库中没有定义任何兵种 (MilitaryKind)";
                    return;
                }
                
                _pathFindLog = $"正在计算 {start.Name} -> {end.Name} ...";
                bool success = pathfinder.GetPath(start.Position, end.Position, kind);
                
                if (success)
                {
                    _pathFindLog = $"成功! 发现路径。";
                }
                else
                {
                    _pathFindLog = "失败: A* 未能连接两个点 (可能是孤岛)";
                }
            }
            catch (Exception ex)
            {
                _pathFindLog = $"运行错误: {ex.Message}";
            }
        }
        private void DrawInputDebugger()
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "坐标系统检查 (Coordinate Transformation)");
            ImGui.Separator();

            var mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            Vector2 rawPos = new Vector2(mouseState.X, mouseState.Y);
            
            // 调用 ScreenManager
            Vector2 worldPos = GameManager.ScreenManager.InputToWorld(rawPos);

            ImGui.Text($"Raw Screen Pos: {rawPos}");
            ImGui.Text($"Game World Pos: {worldPos}");

            if (rawPos == worldPos && rawPos != Vector2.Zero)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "❌ 警告: 坐标未转换！ScaleMatrix 可能是 Identity。");
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), "✅ 转换矩阵已生效");
            }

            ImGui.Separator();
            ImGui.Text("Viewport & Resolution");
            var vp = _game.GraphicsDevice.Viewport;
            ImGui.Text($"Viewport: {vp.Width}x{vp.Height}");
            ImGui.Text($"Virtual: {GameManager.ScreenManager.VirtualWidth}x{GameManager.ScreenManager.VirtualHeight}");
            ImGui.Text($"Scale X: {GameManager.ScreenManager.ScaleX:F2}");
            ImGui.Text($"Scale Y: {GameManager.ScreenManager.ScaleY:F2}");
        }

        private void DrawTimeManipulator()
        {
            var scenario = Session.Current?.Scenario;
            if (scenario == null)
            {
                ImGui.Text("请先加载剧本！");
                return;
            }

            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0.5f, 1), $"当前日期: {scenario.Date.ToString()}");
            ImGui.Text($"季节: {scenario.Date.Season}");
            
            ImGui.Separator();

            if (ImGui.Button("下一天 (+1 Day)"))
            {
                // scenario.Date.Go() 内部处理了进位和季节切换
                scenario.Date.Go();
                _lastAILog = $"[Time] 时间推进到: {scenario.Date}";
            }

            if (ImGui.Button("快速推进 (+30 Days)"))
            {
                scenario.Date.Go(30);
                _lastAILog = $"[Time] 跳过30天";
            }
            
            ImGui.SameLine();
            if (ImGui.Button("快速推进 (+1 Year)"))
            {
                scenario.Date.Go(360);
                _lastAILog = $"[Time] 跳过一年";
            }

            ImGui.Separator();
            if (ImGui.Button("重置日期 -> 184年1月1日"))
            {
                scenario.Date.LoadDateData(184, 1, 1);
            }
        }
    }
}
