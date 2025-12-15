// ===================================================================
// 军师按钮状态管理完整代码 - UI交互优化
// ===================================================================

// ScreenManager.cs 中的完整实现
namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class ScreenManager
    {
        /// <summary>
        /// 检查任命军师按钮是否应该启用
        /// </summary>
        public bool IsAppointAdvisorButtonEnabled()
        {
            if (this.CurrentFaction == null) return false;

            // 检查基础条件
            if (this.CurrentFaction.Leader == null || this.CurrentFaction.Leader.BelongedCaptive != null)
            {
                return false;
            }

            // 检查是否有选中角色
            if (this.CurrentPerson == null) return false;

            // 检查选中角色是否满足军师条件
            return this.CurrentPerson.Alive && 
                   this.CurrentPerson.Available && 
                   this.CurrentPerson.BelongedCaptive == null &&
                   this.CurrentPerson.LocationTroop == null &&
                   this.CurrentPerson.BelongedFaction == this.CurrentFaction &&
                   this.CurrentPerson != this.CurrentFaction.Leader &&
                   this.CurrentPerson.Intelligence >= 70;
        }

        /// <summary>
        /// 检查罢免军师按钮是否应该启用
        /// </summary>
        public bool IsRecallAdvisorButtonEnabled()
        {
            if (this.CurrentFaction == null) return false;
            
            // 检查基础条件
            if (this.CurrentFaction.Leader == null || this.CurrentFaction.Leader.BelongedCaptive != null)
            {
                return false;
            }

            // 必须有当前军师才能罢免
            return this.CurrentFaction.Advisor != null;
        }

        /// <summary>
        /// 检查选中的人物是否已经是当前军师
        /// </summary>
        public bool IsSelectedPersonCurrentAdvisor()
        {
            if (this.CurrentPerson == null || this.CurrentFaction == null) return false;
            
            return this.CurrentFaction.Advisor != null && 
                   this.CurrentFaction.Advisor.ID == this.CurrentPerson.ID;
        }

        /// <summary>
        /// 获取军师按钮的显示文本
        /// </summary>
        public string GetAdvisorButtonText()
        {
            if (this.CurrentFaction == null) return "任命军师";

            if (IsSelectedPersonCurrentAdvisor())
            {
                return "已是军师";
            }
            else if (this.CurrentFaction.Advisor != null)
            {
                return "重新任命军师";
            }
            else
            {
                return "任命军师";
            }
        }

        /// <summary>
        /// 获取军师按钮禁用的原因
        /// </summary>
        private string GetAdvisorButtonDisabledReason()
        {
            if (this.CurrentFaction == null) return "没有当前势力";
            if (this.CurrentFaction.Leader == null) return "没有君主";
            if (this.CurrentFaction.Leader.BelongedCaptive != null) return "君主被俘虏";
            if (this.CurrentPerson == null) return "请选择角色";
            
            // 检查选中角色的具体问题
            if (!this.CurrentPerson.Alive) return "角色已死亡";
            if (!this.CurrentPerson.Available) return "角色不可用";
            if (this.CurrentPerson.BelongedCaptive != null) return "角色被俘虏";
            if (this.CurrentPerson.LocationTroop != null) return "角色在部队中";
            if (this.CurrentPerson.BelongedFaction != this.CurrentFaction) return "角色不属于当前势力";
            if (this.CurrentPerson == this.CurrentFaction.Leader) return "不能任命君主为军师";
            if (this.CurrentPerson.Intelligence < 70) return $"智力不足70 (当前:{this.CurrentPerson.Intelligence})";
            
            // 如果选中的是当前军师
            if (IsSelectedPersonCurrentAdvisor()) return "该角色已是当前军师";

            return "可以任命军师";
        }

        /// <summary>
        /// 获取罢免军师按钮禁用的原因
        /// </summary>
        private string GetRecallAdvisorButtonDisabledReason()
        {
            if (this.CurrentFaction == null) return "没有当前势力";
            if (this.CurrentFaction.Leader == null) return "没有君主";
            if (this.CurrentFaction.Leader.BelongedCaptive != null) return "君主被俘虏";
            if (this.CurrentFaction.Advisor == null) return "当前没有军师";

            return "可以罢免军师";
        }

        /// <summary>
        /// 更新军师相关按钮状态
        /// </summary>
        private void UpdateAdvisorButtons()
        {
            // 更新任命军师按钮
            UpdateAdvisorButton();
            
            // 更新罢免军师按钮
            UpdateRecallAdvisorButton();
            
            // 更新军师信息显示
            UpdateAdvisorInfoDisplay();
        }

        /// <summary>
        /// 更新任命军师按钮
        /// </summary>
        private void UpdateAdvisorButton()
        {
            var button = FindControl<Button>("AppointAdvisor_Button");
            if (button != null)
            {
                bool enabled = IsAppointAdvisorButtonEnabled();
                button.Enabled = enabled;
                button.Text = GetAdvisorButtonText();
                
                // 设置提示信息
                string reason = GetAdvisorButtonDisabledReason();
                button.ToolTip = enabled ? "点击任命选中角色为军师" : reason;
                
                // 根据状态设置按钮样式
                if (IsSelectedPersonCurrentAdvisor())
                {
                    button.BackColor = Color.LightGray;  // 已是军师时显示灰色
                    button.Enabled = false;
                }
                else if (enabled)
                {
                    button.BackColor = Color.LightGreen; // 可任命时显示绿色
                }
                else
                {
                    button.BackColor = Color.LightCoral; // 不可任命时显示红色
                }
            }
        }

        /// <summary>
        /// 更新罢免军师按钮
        /// </summary>
        private void UpdateRecallAdvisorButton()
        {
            var button = FindControl<Button>("RecallAdvisor_Button");
            if (button != null)
            {
                bool enabled = IsRecallAdvisorButtonEnabled();
                button.Enabled = enabled;
                button.Text = "罢免军师";
                
                // 设置提示信息
                string reason = GetRecallAdvisorButtonDisabledReason();
                button.ToolTip = enabled ? $"点击罢免当前军师 {this.CurrentFaction?.AdvisorName}" : reason;
                
                // 根据状态设置按钮样式
                button.BackColor = enabled ? Color.Orange : Color.LightGray;
            }
        }

        /// <summary>
        /// 更新军师信息显示
        /// </summary>
        private void UpdateAdvisorInfoDisplay()
        {
            var infoLabel = FindControl<Label>("AdvisorInfo_Label");
            if (infoLabel != null && this.CurrentFaction != null)
            {
                if (this.CurrentFaction.Advisor != null)
                {
                    var advisor = this.CurrentFaction.Advisor;
                    infoLabel.Text = $"当前军师: {advisor.Name} (智力:{advisor.Intelligence})";
                    infoLabel.ForeColor = Color.DarkGreen;
                }
                else
                {
                    infoLabel.Text = "当前没有军师";
                    infoLabel.ForeColor = Color.DarkRed;
                }
            }

            // 更新候选人信息
            UpdateCandidateInfo();
        }

        /// <summary>
        /// 更新候选人信息显示
        /// </summary>
        private void UpdateCandidateInfo()
        {
            var candidateLabel = FindControl<Label>("CandidateInfo_Label");
            if (candidateLabel != null && this.CurrentPerson != null && this.CurrentFaction != null)
            {
                if (IsSelectedPersonCurrentAdvisor())
                {
                    candidateLabel.Text = $"{this.CurrentPerson.Name} 是当前军师";
                    candidateLabel.ForeColor = Color.Blue;
                }
                else if (IsAppointAdvisorButtonEnabled())
                {
                    var currentAdvisor = this.CurrentFaction.Advisor;
                    if (currentAdvisor != null)
                    {
                        int intelligenceDiff = this.CurrentPerson.Intelligence - currentAdvisor.Intelligence;
                        string comparison = intelligenceDiff > 0 ? $"(+{intelligenceDiff})" : 
                                          intelligenceDiff < 0 ? $"({intelligenceDiff})" : "(相同)";
                        candidateLabel.Text = $"{this.CurrentPerson.Name} 智力:{this.CurrentPerson.Intelligence} {comparison}";
                        candidateLabel.ForeColor = intelligenceDiff > 0 ? Color.Green : 
                                                 intelligenceDiff < 0 ? Color.Red : Color.Black;
                    }
                    else
                    {
                        candidateLabel.Text = $"{this.CurrentPerson.Name} 智力:{this.CurrentPerson.Intelligence} (合格)";
                        candidateLabel.ForeColor = Color.Green;
                    }
                }
                else
                {
                    string reason = GetAdvisorButtonDisabledReason();
                    candidateLabel.Text = $"{this.CurrentPerson.Name}: {reason}";
                    candidateLabel.ForeColor = Color.Red;
                }
            }
            else if (candidateLabel != null)
            {
                candidateLabel.Text = "请选择角色";
                candidateLabel.ForeColor = Color.Gray;
            }
        }

        /// <summary>
        /// 处理人物选择变化事件
        /// </summary>
        private void OnPersonSelectionChanged(Person selectedPerson)
        {
            this.CurrentPerson = selectedPerson;
            
            // 更新所有军师相关的UI
            UpdateAdvisorButtons();
        }

        /// <summary>
        /// 处理势力变化事件
        /// </summary>
        private void OnFactionChanged(Faction newFaction)
        {
            this.CurrentFaction = newFaction;
            this.CurrentPerson = null; // 清空选中人物
            
            // 更新所有军师相关的UI
            UpdateAdvisorButtons();
        }

        /// <summary>
        /// 执行任命军师操作
        /// </summary>
        private void ExecuteAppointAdvisor()
        {
            if (!IsAppointAdvisorButtonEnabled())
            {
                string reason = GetAdvisorButtonDisabledReason();
                ShowMessage($"无法任命军师: {reason}");
                return;
            }

            // 确认对话框
            if (this.CurrentFaction.Advisor != null)
            {
                string message = $"确定要将军师从 {this.CurrentFaction.AdvisorName} 更换为 {this.CurrentPerson.Name} 吗？";
                if (ShowConfirmDialog(message))
                {
                    PerformAdvisorAppointment();
                }
            }
            else
            {
                string message = $"确定要任命 {this.CurrentPerson.Name} 为军师吗？";
                if (ShowConfirmDialog(message))
                {
                    PerformAdvisorAppointment();
                }
            }
        }

        /// <summary>
        /// 执行罢免军师操作
        /// </summary>
        private void ExecuteRecallAdvisor()
        {
            if (!IsRecallAdvisorButtonEnabled())
            {
                string reason = GetRecallAdvisorButtonDisabledReason();
                ShowMessage($"无法罢免军师: {reason}");
                return;
            }

            string message = $"确定要罢免军师 {this.CurrentFaction.AdvisorName} 吗？";
            if (ShowConfirmDialog(message))
            {
                // 执行罢免
                this.CurrentFaction.AdvisorID = -1;
                this.CurrentFaction.Advisor = null;
                
                ShowMessage($"已罢免军师 {this.CurrentFaction.AdvisorName}");
                
                // 更新UI
                UpdateAdvisorButtons();
            }
        }

        /// <summary>
        /// 执行军师任命
        /// </summary>
        private void PerformAdvisorAppointment()
        {
            try
            {
                string formerAdvisorName = this.CurrentFaction.AdvisorName;
                
                // 执行任命
                this.CurrentFaction.AdvisorID = this.CurrentPerson.ID;
                this.CurrentFaction.AppointAdvisor(this.CurrentPerson);
                
                // 显示成功消息
                if (!string.IsNullOrEmpty(formerAdvisorName))
                {
                    ShowMessage($"已将军师从 {formerAdvisorName} 更换为 {this.CurrentPerson.Name}");
                }
                else
                {
                    ShowMessage($"已任命 {this.CurrentPerson.Name} 为军师");
                }
                
                // 更新UI
                UpdateAdvisorButtons();
            }
            catch (Exception ex)
            {
                ShowMessage($"任命军师失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示军师候选人列表
        /// </summary>
        private void ShowAdvisorCandidateList()
        {
            if (this.CurrentFaction == null) return;

            PersonList candidates = this.CurrentFaction.GetAdvisorCandidates(false); // 包括当前军师
            
            if (candidates.Count == 0)
            {
                ShowMessage("没有合适的军师候选人");
                return;
            }

            // 显示候选人选择界面
            this.ShowTabListInFrame(
                UndoneWorkKind.Frame, 
                FrameKind.Person, 
                FrameFunction.AppointAdvisor, 
                false, true, true, false, 
                candidates, 
                null, 
                GetAdvisorButtonText(), 
                ""
            );
        }

        // 辅助方法
        private T FindControl<T>(string name) where T : Control
        {
            // 实现控件查找逻辑
            // 这里需要根据实际的UI框架实现
            return null;
        }

        private void ShowMessage(string message)
        {
            // 显示消息的实现
            Console.WriteLine(message);
        }

        private bool ShowConfirmDialog(string message)
        {
            // 显示确认对话框的实现
            Console.WriteLine($"确认: {message}");
            return true; // 简化实现，实际应该显示对话框
        }
    }
}

// ===================================================================
// UI事件处理示例
// ===================================================================

namespace WorldOfTheThreeKingdoms.GameScreens
{
    public partial class ScreenManager
    {
        /// <summary>
        /// 初始化军师相关的UI事件
        /// </summary>
        private void InitializeAdvisorUI()
        {
            // 任命军师按钮事件
            var appointButton = FindControl<Button>("AppointAdvisor_Button");
            if (appointButton != null)
            {
                appointButton.Click += (sender, e) => ExecuteAppointAdvisor();
            }

            // 罢免军师按钮事件
            var recallButton = FindControl<Button>("RecallAdvisor_Button");
            if (recallButton != null)
            {
                recallButton.Click += (sender, e) => ExecuteRecallAdvisor();
            }

            // 候选人列表按钮事件
            var candidateButton = FindControl<Button>("ShowCandidates_Button");
            if (candidateButton != null)
            {
                candidateButton.Click += (sender, e) => ShowAdvisorCandidateList();
            }

            // 人物列表选择事件
            var personList = FindControl<ListBox>("PersonList");
            if (personList != null)
            {
                personList.SelectedIndexChanged += (sender, e) =>
                {
                    if (personList.SelectedItem is Person selectedPerson)
                    {
                        OnPersonSelectionChanged(selectedPerson);
                    }
                };
            }
        }

        /// <summary>
        /// 定期更新UI状态
        /// </summary>
        public void UpdateUI()
        {
            // 在游戏主循环中调用，确保UI状态实时更新
            UpdateAdvisorButtons();
        }
    }
}

// ===================================================================
// 使用示例
// ===================================================================

public class AdvisorUIExample
{
    public static void DemonstrateAdvisorUI()
    {
        var screenManager = new ScreenManager();
        
        // 模拟选择不同的人物
        Console.WriteLine("=== 军师UI状态测试 ===");
        
        // 测试1: 没有选中人物
        screenManager.CurrentPerson = null;
        Console.WriteLine($"没有选中人物 - 按钮启用: {screenManager.IsAppointAdvisorButtonEnabled()}");
        Console.WriteLine($"禁用原因: {screenManager.GetAdvisorButtonDisabledReason()}");
        
        // 测试2: 选中合格候选人
        // Person candidate = GetQualifiedCandidate();
        // screenManager.CurrentPerson = candidate;
        // Console.WriteLine($"选中合格候选人 - 按钮启用: {screenManager.IsAppointAdvisorButtonEnabled()}");
        // Console.WriteLine($"按钮文本: {screenManager.GetAdvisorButtonText()}");
        
        // 测试3: 选中当前军师
        // screenManager.CurrentPerson = screenManager.CurrentFaction.Advisor;
        // Console.WriteLine($"选中当前军师 - 按钮启用: {screenManager.IsAppointAdvisorButtonEnabled()}");
        // Console.WriteLine($"按钮文本: {screenManager.GetAdvisorButtonText()}");
    }
}