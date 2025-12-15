// ===================================================================
// 军师按钮系统 - 包含头像更新和调试诊断功能
// ===================================================================

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameObjects;
using GameManager;
using WorldOfTheThreeKingdoms.Resources;
using Platforms;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class MainGameScreen
    {
        #region 军师按钮字段
        
        /// <summary>
        /// 调试模式开关 - 设为true启用详细调试
        /// </summary>
        private const bool DEBUG_MODE = true;
        
        /// <summary>
        /// 军师按钮图像纹理
        /// </summary>
        private Texture2D AdvisorButtonImage;
        
        /// <summary>
        /// 上次更新的军师ID，用于防抖
        /// </summary>
        private int lastAdvisorId = -2;
        
        /// <summary>
        /// 军师按钮UI控件
        /// </summary>
        private PlatformButton advisorBtn;
        
        #endregion

        #region 军师按钮头像更新

        /// <summary>
        /// 更新军师按钮显示
        /// </summary>
        public void UpdateAdvisorButton(GameObjects.GameScenario scenario)
        {
            var currentFaction = Session.Current.CurrentPlayerFaction;
            
            // 如果连势力都没了（比如游戏刚开始初始化或者灭亡），那确实应该隐藏
            if (currentFaction == null) 
            {
                this.advisorBtn.Visible = false;
                return;
            }

            this.advisorBtn.Visible = true; // 确保按钮可见

            // 1. 定义显示样式的默认值（假设无军师状态）
            string displayText = "任命";  // 中文提示，引导玩家点击
            Color backgroundColor = Color.DarkSlateGray; // 深灰色，表示空缺但可点击
            Color borderColor = Color.Gray;
            bool hasAdvisor = false;

            // 2. 获取实际军师数据
            int advisorId = currentFaction.AdvisorID;
            var advisor = scenario.Persons.GetGameObject(advisorId) as Person;
            if (advisor != null)
            {
                hasAdvisor = true;
                displayText = advisor.Name; // 显示名字，如"荀彧"
                
                // 使用势力颜色作为底色，增强归属感
                backgroundColor = currentFaction.Color; 
                // 如果底色太亮（比如白色势力），文字可能看不清，这里可以做一个简单的亮度变暗处理
                // backgroundColor = new Color(backgroundColor.R * 0.8f, backgroundColor.G * 0.8f, backgroundColor.B * 0.8f);
                
                // 检测是否有谏言，改变边框颜色
                bool hasSuggestion = CheckAdvisorHasSuggestion(advisorId);
                borderColor = hasSuggestion ? Color.Gold : Color.Black; 
            }

            // --- 以下是绘制逻辑 (RenderTarget) ---
            int width = 120; 
            int height = 60;
            GraphicsDevice device = Platform.GraphicsDevice;

            // 注意：在频繁调用的Update中，尽量不要每次都 new RenderTarget
            // 但在按钮更新频率不高的情况下（只有任命变更时），这里直接 new 问题不大
            // 极致优化可以把 canvas 缓存为类成员变量
            RenderTarget2D canvas = new RenderTarget2D(device, width, height);
            var previousTargets = device.GetRenderTargets();
            device.SetRenderTarget(canvas);
            device.Clear(Color.Transparent); 

            SpriteBatch sb = Session.Current.SpriteBatch; 
            if (sb == null) sb = new SpriteBatch(device); 
            sb.Begin();

            // A. 画 1x1 像素点作为画笔
            Texture2D pixel = new Texture2D(device, 1, 1);
            pixel.SetData(new[] { Color.White });

            // B. 画背景
            sb.Draw(pixel, new Rectangle(0, 0, width, height), backgroundColor);

            // C. 画边框 (如果是"任命"状态，可以画一个虚线感或者简单的亮色边框表示可交互)
            int borderSize = hasAdvisor ? 3 : 2;
            // 如果没军师，边框用白色显眼一点；有军师则用黑色或金色
            Color finalBorderColor = hasAdvisor ? borderColor : Color.LightGray;
            sb.Draw(pixel, new Rectangle(0, 0, width, borderSize), finalBorderColor); // Top
            sb.Draw(pixel, new Rectangle(0, height - borderSize, width, borderSize), finalBorderColor); // Bottom
            sb.Draw(pixel, new Rectangle(0, 0, borderSize, height), finalBorderColor); // Left
            sb.Draw(pixel, new Rectangle(width - borderSize, 0, borderSize, height), finalBorderColor); // Right

            // D. 画文字
            SpriteFont font = Session.Current.Font;
            if (font != null)
            {
                Vector2 textSize = font.MeasureString(displayText);
                Vector2 position = new Vector2((width - textSize.X) / 2, (height - textSize.Y) / 2);
                
                // 没军师的时候，字可以稍微透明一点，或者用黄色提示
                Color textColor = hasAdvisor ? Color.White : Color.Yellow;
                sb.DrawString(font, displayText, position, textColor);
            }

            sb.End();
            device.SetRenderTargets(previousTargets);
            pixel.Dispose();

            // 资源清理与赋值
            if (this.AdvisorButtonImage != null && !this.AdvisorButtonImage.IsDisposed)
            {
                this.AdvisorButtonImage.Dispose();
            }
            this.AdvisorButtonImage = canvas;

            // 强制 UI 刷新
            string uniqueName = $"AdvisorBtn_State_{advisorId}_{DateTime.Now.Ticks}";
            this.advisorBtn.NormalTexture = new PlatformTexture()
            {
                Name = uniqueName,
                Width = width,
                Height = height
            };
            CacheManager.TextureTempDics[uniqueName] = this.AdvisorButtonImage;
        }

        /// <summary>
        /// 检查军师是否有谏言
        /// </summary>
        private bool CheckAdvisorHasSuggestion(int advisorId)
        {
            // TODO: 实现检查军师是否有谏言的逻辑
            // 这里可以检查军师的建议队列、计略可用性等
            return false; // 暂时返回false
        }

        /// <summary>
        /// 军师按钮点击事件处理
        /// </summary>
        private void OnAdvisorButtonClick(object sender, EventArgs e)
        {
            var faction = Session.Current.CurrentPlayerFaction;
            if (faction.AdvisorID == -1) // 假设 -1 代表无军师
            {
                // 场景 A：无军师 -> 打开人员列表，筛选智力高的人，让玩家选择
                // Menus.OpenAppointAdvisorMenu(); 
                
                // 暂时使用现有的菜单系统
                this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.AppointAdvisor, 
                    false, true, true, false, faction.AdvisorCandicate, null, "任命军师", "");
            }
            else
            {
                // 场景 B：有军师 -> 打开军师计策面板
                // 包含：听取谏言、发动计略、查看军师状态等
                // Menus.OpenAdvisorStrategyMenu();
                
                // 暂时显示军师详细信息
                var advisor = Session.Current.Scenario.Persons.GetGameObject(faction.AdvisorID) as Person;
                if (advisor != null)
                {
                    this.ShowTabListInFrame(UndoneWorkKind.Frame, FrameKind.Person, FrameFunction.Browse, 
                        false, true, false, false, advisor.GetGameObjectList(), null, "军师信息", "");
                }
            }
        }

        /// <summary>
        /// 辅助方法：填充纯色
        /// </summary>
        private void FillColor(Color[] data, Color color)
        {
            for (int i = 0; i < data.Length; i++) 
                data[i] = color;
        }

        #endregion

        #region 调试诊断功能

        /// <summary>
        /// 输出简化的军师状态报告
        /// </summary>
        public void QuickAdvisorStatusReport()
        {
            var faction = Session.Current?.Scenario?.CurrentFaction;
            
            Console.WriteLine("=== 军师状态报告 ===");
            Console.WriteLine($"势力: {faction?.Name ?? "无"}");
            Console.WriteLine($"君主: {faction?.Leader?.Name ?? "无"}");
            Console.WriteLine($"军师: {faction?.AdvisorName ?? "无"}");
            Console.WriteLine($"候选人: {faction?.AdvisorCandicate?.Count ?? 0}");
            Console.WriteLine($"可任命军师: {faction?.AppointAdvisorAvail() ?? false}");
            Console.WriteLine($"可罢免军师: {faction?.RecallAdvisorAvail() ?? false}");
            Console.WriteLine("==================");
        }

        /// <summary>
        /// 检查军师按钮是否应该显示
        /// </summary>
        public bool ShouldShowAdvisorButton()
        {
            var faction = Session.Current?.Scenario?.CurrentFaction;
            if (faction == null) return false;
            
            return faction.AppointAdvisorAvail();
        }

        /// <summary>
        /// 检查罢免军师按钮是否应该显示
        /// </summary>
        public bool ShouldShowRecallButton()
        {
            var faction = Session.Current?.Scenario?.CurrentFaction;
            if (faction == null) return false;
            
            return faction.RecallAdvisorAvail();
        }

        #endregion

        #region 完整诊断方法

        /// <summary>
        /// 执行完整的军师按钮诊断
        /// </summary>
        private AdvisorButtonDiagnostics PerformCompleteDiagnostics()
        {
            var diagnostics = new AdvisorButtonDiagnostics();
            
            try
            {
                // 检查当前势力
                var currentFaction = this.CurrentFaction ?? Session.Current?.Scenario?.CurrentFaction;
                diagnostics.HasCurrentFaction = currentFaction != null;
                
                if (!diagnostics.HasCurrentFaction)
                {
                    diagnostics.PrimaryIssue = "没有当前势力";
                    diagnostics.TooltipText = "错误: 没有当前势力";
                    return diagnostics;
                }

                // 检查君主
                diagnostics.HasLeader = currentFaction.Leader != null;
                diagnostics.LeaderName = currentFaction.Leader?.Name ?? "无";
                diagnostics.LeaderCaptured = currentFaction.Leader?.BelongedCaptive != null;
                diagnostics.IsPlayerFaction = Session.Current?.Scenario?.IsPlayer(currentFaction) ?? false;

                if (!diagnostics.HasLeader)
                {
                    diagnostics.PrimaryIssue = "没有君主";
                    diagnostics.TooltipText = "错误: 势力没有君主";
                    return diagnostics;
                }

                if (diagnostics.LeaderCaptured)
                {
                    diagnostics.PrimaryIssue = "君主被俘虏";
                    diagnostics.TooltipText = "错误: 君主被俘虏，无法任命军师";
                    return diagnostics;
                }

                // 检查当前军师
                diagnostics.HasCurrentAdvisor = currentFaction.Advisor != null;
                diagnostics.CurrentAdvisorName = currentFaction.AdvisorName;
                diagnostics.CurrentAdvisorID = currentFaction.AdvisorID;

                // 统计人员信息
                diagnostics.TotalPersonCount = currentFaction.Persons?.Count ?? 0;
                
                // 分析候选人
                AnalyzeCandidates(currentFaction, diagnostics);
                
                // 获取原始的AppointAdvisorAvail结果
                bool originalAvail = currentFaction.AppointAdvisorAvail();
                
                // 决定按钮状态
                diagnostics.ShouldShowButton = originalAvail;
                
                // 设置按钮文本和提示
                if (diagnostics.HasCurrentAdvisor)
                {
                    diagnostics.ButtonText = "重新任命军师";
                    if (diagnostics.PlayerCandidateCount > 0)
                    {
                        diagnostics.TooltipText = $"当前军师: {diagnostics.CurrentAdvisorName}，点击重新任命";
                    }
                    else
                    {
                        diagnostics.TooltipText = "没有合适的候选人可以重新任命";
                        diagnostics.PrimaryIssue = "没有合适的候选人";
                    }
                }
                else
                {
                    diagnostics.ButtonText = "任命军师";
                    if (diagnostics.PlayerCandidateCount > 0)
                    {
                        diagnostics.TooltipText = $"选择 {diagnostics.PlayerCandidateCount} 个候选人中的一个任命为军师";
                    }
                    else
                    {
                        diagnostics.TooltipText = "没有合适的候选人可以任命为军师";
                        diagnostics.PrimaryIssue = "没有合适的候选人";
                    }
                }
                
            }
            catch (Exception ex)
            {
                diagnostics.PrimaryIssue = $"诊断异常: {ex.Message}";
                diagnostics.TooltipText = $"诊断错误: {ex.Message}";
            }
            
            return diagnostics;
        }

        /// <summary>
        /// 分析候选人情况
        /// </summary>
        private void AnalyzeCandidates(Faction faction, AdvisorButtonDiagnostics diagnostics)
        {
            try
            {
                // 获取候选人列表
                var playerCandidates = faction.AdvisorCandicate;
                var aiCandidates = faction.AIAdvisorCandicate;
                
                diagnostics.PlayerCandidateCount = playerCandidates?.Count ?? 0;
                diagnostics.AICandidateCount = aiCandidates?.Count ?? 0;
                
                // 分析所有人员，找出为什么不合格
                if (faction.Persons != null)
                {
                    foreach (Person person in faction.Persons)
                    {
                        var personDiag = new PersonDiagnostic
                        {
                            Name = person.Name,
                            Intelligence = person.Intelligence
                        };
                        
                        // 检查各项条件
                        var issues = new System.Collections.Generic.List<string>();
                        
                        if (person == faction.Leader)
                            issues.Add("是君主");
                        else if (person == faction.Advisor)
                            issues.Add("是当前军师");
                        else if (!person.Available)
                            issues.Add("不可用");
                        else if (!person.Alive)
                            issues.Add("已死亡");
                        else if (person.BelongedCaptive != null)
                            issues.Add("被俘虏");
                        else if (person.LocationTroop != null)
                            issues.Add("在部队中");
                        else if (person.Intelligence < 70)
                            issues.Add($"智力不足({person.Intelligence}<70)");
                        else
                        {
                            issues.Add("合格");
                            personDiag.IsQualified = true;
                            diagnostics.QualifiedPersonCount++;
                        }
                        
                        personDiag.Status = string.Join(", ", issues);
                        diagnostics.CandidateDetails.Add(personDiag);
                    }
                }
                
                // 按智力排序
                diagnostics.CandidateDetails = diagnostics.CandidateDetails
                    .OrderByDescending(p => p.Intelligence)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[错误] 分析候选人失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行罢免军师按钮诊断
        /// </summary>
        private AdvisorButtonDiagnostics PerformRecallDiagnostics()
        {
            var diagnostics = new AdvisorButtonDiagnostics();
            
            var currentFaction = this.CurrentFaction ?? Session.Current?.Scenario?.CurrentFaction;
            diagnostics.HasCurrentFaction = currentFaction != null;
            
            if (diagnostics.HasCurrentFaction)
            {
                diagnostics.HasCurrentAdvisor = currentFaction.Advisor != null;
                diagnostics.CurrentAdvisorName = currentFaction.AdvisorName;
                diagnostics.ShouldShowButton = diagnostics.HasCurrentAdvisor;
                
                if (diagnostics.HasCurrentAdvisor)
                {
                    diagnostics.TooltipText = $"罢免当前军师: {diagnostics.CurrentAdvisorName}";
                }
                else
                {
                    diagnostics.TooltipText = "当前没有军师可以罢免";
                    diagnostics.PrimaryIssue = "没有当前军师";
                }
            }
            else
            {
                diagnostics.TooltipText = "没有当前势力";
                diagnostics.PrimaryIssue = "没有当前势力";
            }
            
            return diagnostics;
        }

        #endregion

        #region UI样式设置

        /// <summary>
        /// 根据诊断结果设置按钮样式
        /// </summary>
        private void SetButtonStyle(Button button, AdvisorButtonDiagnostics diagnostics)
        {
            try
            {
                if (FORCE_SHOW_BUTTON)
                {
                    // 调试模式：使用特殊颜色
                    button.Background = System.Windows.Media.Brushes.Yellow;
                    button.Foreground = System.Windows.Media.Brushes.Black;
                }
                else if (diagnostics.ShouldShowButton)
                {
                    if (diagnostics.HasCurrentAdvisor)
                    {
                        // 重新任命：橙色
                        button.Background = System.Windows.Media.Brushes.Orange;
                        button.Foreground = System.Windows.Media.Brushes.White;
                    }
                    else
                    {
                        // 首次任命：绿色
                        button.Background = System.Windows.Media.Brushes.Green;
                        button.Foreground = System.Windows.Media.Brushes.White;
                    }
                }
                else
                {
                    // 不可用：灰色
                    button.Background = System.Windows.Media.Brushes.Gray;
                    button.Foreground = System.Windows.Media.Brushes.DarkGray;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[警告] 设置按钮样式失败: {ex.Message}");
            }
        }

        #endregion

        #region 手动调试方法

        /// <summary>
        /// 手动触发诊断（可以绑定到按钮或快捷键）
        /// </summary>
        public void ManualDiagnose()
        {
            try
            {
                Console.WriteLine("\n=== 手动诊断开始 ===");
                
                var diagnostics = PerformCompleteDiagnostics();
                Console.WriteLine(diagnostics.GetFullReport());
                
                var recallDiagnostics = PerformRecallDiagnostics();
                Console.WriteLine("【罢免按钮诊断】");
                Console.WriteLine($"应显示: {recallDiagnostics.ShouldShowButton}");
                Console.WriteLine($"提示: {recallDiagnostics.TooltipText}");
                
                Console.WriteLine("=== 手动诊断结束 ===\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[错误] 手动诊断失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 输出简化的状态报告
        /// </summary>
        public void QuickStatusReport()
        {
            var faction = this.CurrentFaction ?? Session.Current?.Scenario?.CurrentFaction;
            
            Console.WriteLine("=== 快速状态报告 ===");
            Console.WriteLine($"势力: {faction?.Name ?? "无"}");
            Console.WriteLine($"君主: {faction?.Leader?.Name ?? "无"}");
            Console.WriteLine($"军师: {faction?.AdvisorName ?? "无"}");
            Console.WriteLine($"候选人: {faction?.AdvisorCandicate?.Count ?? 0}");
            Console.WriteLine($"AppointAdvisorAvail: {faction?.AppointAdvisorAvail() ?? false}");
            Console.WriteLine("==================");
        }

        #endregion
    }
}

// ===================================================================
// XAML 绑定示例
// ===================================================================

/*
<!-- 在XAML中绑定调试事件 -->
<Button Name="AppointAdvisorButton" 
        Content="任命军师"
        Loaded="OnAdvisorButtonLoaded"
        Click="OnAppointAdvisorClick"/>

<Button Name="RecallAdvisorButton" 
        Content="罢免军师"
        Loaded="OnRecallAdvisorButtonLoaded"
        Click="OnRecallAdvisorClick"/>

<!-- 调试按钮 -->
<Button Content="手动诊断" 
        Click="OnManualDiagnoseClick"
        Visibility="{Binding IsDebugMode, Converter={StaticResource BoolToVisibilityConverter}}"/>
*/

// ===================================================================
// 使用示例
// ===================================================================

public class DiagnosticsExample
{
    public static void TestDiagnostics()
    {
        var view = new FactionView();
        
        // 执行快速状态报告
        view.QuickStatusReport();
        
        // 执行完整诊断
        view.ManualDiagnose();
        
        // 可以在游戏运行时随时调用这些方法进行调试
    }
}