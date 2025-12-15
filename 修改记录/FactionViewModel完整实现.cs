// ===================================================================
// FactionViewModel 完整实现 - MVVM模式军师管理
// ===================================================================

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GameObjects;
using GameManager;

namespace WorldOfTheThreeKingdoms.ViewModels
{
    public class FactionViewModel : INotifyPropertyChanged
    {
        #region 私有字段
        private Person _selectedPerson;
        private Faction _currentFaction;
        private bool _isAdvisorDialogOpen;
        private string _statusMessage;
        #endregion

        #region 属性

        /// <summary>
        /// 当前势力
        /// </summary>
        public Faction CurrentFaction
        {
            get => _currentFaction ?? Session.Current?.Scenario?.CurrentFaction;
            set
            {
                if (_currentFaction != value)
                {
                    _currentFaction = value;
                    OnPropertyChanged();
                    RefreshAllProperties();
                }
            }
        }

        /// <summary>
        /// 当前选中的人物
        /// </summary>
        public Person SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                if (_selectedPerson != value)
                {
                    _selectedPerson = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAppointAdvisorButtonEnabled));
                    OnPropertyChanged(nameof(IsRecallAdvisorButtonEnabled));
                    OnPropertyChanged(nameof(AdvisorButtonText));
                    OnPropertyChanged(nameof(AdvisorButtonTooltip));
                    OnPropertyChanged(nameof(SelectedPersonInfo));
                    OnPropertyChanged(nameof(IsSelectedPersonCurrentAdvisor));
                    
                    // 刷新命令状态
                    (AppointAdvisorCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RecallAdvisorCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 军师候选人列表
        /// </summary>
        public ObservableCollection<Person> AdvisorCandidates
        {
            get
            {
                var candidates = new ObservableCollection<Person>();
                if (CurrentFaction != null)
                {
                    var candidateList = CurrentFaction.GetAdvisorCandidates(false); // 包括当前军师
                    foreach (Person person in candidateList)
                    {
                        candidates.Add(person);
                    }
                }
                return candidates;
            }
        }

        /// <summary>
        /// 当前军师
        /// </summary>
        public Person CurrentAdvisor => CurrentFaction?.Advisor;

        /// <summary>
        /// 当前军师名称
        /// </summary>
        public string CurrentAdvisorName => CurrentAdvisor?.Name ?? "无";

        /// <summary>
        /// 当前军师信息
        /// </summary>
        public string CurrentAdvisorInfo
        {
            get
            {
                if (CurrentAdvisor != null)
                {
                    return $"当前军师: {CurrentAdvisor.Name} (智力:{CurrentAdvisor.Intelligence})";
                }
                return "当前没有军师";
            }
        }

        /// <summary>
        /// 选中人物信息
        /// </summary>
        public string SelectedPersonInfo
        {
            get
            {
                if (SelectedPerson == null) return "请选择角色";

                if (IsSelectedPersonCurrentAdvisor)
                {
                    return $"{SelectedPerson.Name} 是当前军师";
                }

                if (CurrentAdvisor != null)
                {
                    int intelligenceDiff = SelectedPerson.Intelligence - CurrentAdvisor.Intelligence;
                    string comparison = intelligenceDiff > 0 ? $"(+{intelligenceDiff})" : 
                                      intelligenceDiff < 0 ? $"({intelligenceDiff})" : "(相同)";
                    return $"{SelectedPerson.Name} 智力:{SelectedPerson.Intelligence} {comparison}";
                }

                return $"{SelectedPerson.Name} 智力:{SelectedPerson.Intelligence}";
            }
        }

        /// <summary>
        /// 选中的人物是否是当前军师
        /// </summary>
        public bool IsSelectedPersonCurrentAdvisor
        {
            get
            {
                return SelectedPerson != null && CurrentAdvisor != null && 
                       SelectedPerson.ID == CurrentAdvisor.ID;
            }
        }

        /// <summary>
        /// 任命军师按钮是否启用
        /// </summary>
        public bool IsAppointAdvisorButtonEnabled
        {
            get
            {
                if (CurrentFaction == null || SelectedPerson == null) return false;

                // 检查基础条件
                if (CurrentFaction.Leader == null || CurrentFaction.Leader.BelongedCaptive != null)
                    return false;

                // 如果选中的是当前军师，不允许重复任命
                if (IsSelectedPersonCurrentAdvisor) return false;

                // 检查选中角色是否满足军师条件
                return SelectedPerson.Alive && 
                       SelectedPerson.Available && 
                       SelectedPerson.BelongedCaptive == null &&
                       SelectedPerson.LocationTroop == null &&
                       SelectedPerson.BelongedFaction == CurrentFaction &&
                       SelectedPerson != CurrentFaction.Leader &&
                       SelectedPerson.Intelligence >= 70;
            }
        }

        /// <summary>
        /// 罢免军师按钮是否启用
        /// </summary>
        public bool IsRecallAdvisorButtonEnabled
        {
            get
            {
                if (CurrentFaction == null) return false;
                
                // 检查基础条件
                if (CurrentFaction.Leader == null || CurrentFaction.Leader.BelongedCaptive != null)
                    return false;

                // 必须有当前军师才能罢免
                return CurrentFaction.Advisor != null;
            }
        }

        /// <summary>
        /// 军师按钮文本
        /// </summary>
        public string AdvisorButtonText
        {
            get
            {
                if (SelectedPerson == null) return "任命军师";

                if (IsSelectedPersonCurrentAdvisor)
                    return "已是军师";
                else if (CurrentFaction?.Advisor != null)
                    return "重新任命军师";
                else
                    return "任命军师";
            }
        }

        /// <summary>
        /// 军师按钮提示信息
        /// </summary>
        public string AdvisorButtonTooltip
        {
            get
            {
                if (IsAppointAdvisorButtonEnabled)
                {
                    if (CurrentFaction?.Advisor != null)
                        return $"将军师从 {CurrentAdvisorName} 更换为 {SelectedPerson?.Name}";
                    else
                        return $"任命 {SelectedPerson?.Name} 为军师";
                }

                return GetAdvisorButtonDisabledReason();
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 军师对话框是否打开
        /// </summary>
        public bool IsAdvisorDialogOpen
        {
            get => _isAdvisorDialogOpen;
            set
            {
                if (_isAdvisorDialogOpen != value)
                {
                    _isAdvisorDialogOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region 命令

        /// <summary>
        /// 任命军师命令
        /// </summary>
        public ICommand AppointAdvisorCommand { get; set; }

        /// <summary>
        /// 罢免军师命令
        /// </summary>
        public ICommand RecallAdvisorCommand { get; set; }

        /// <summary>
        /// 显示候选人列表命令
        /// </summary>
        public ICommand ShowCandidatesCommand { get; set; }

        /// <summary>
        /// 刷新数据命令
        /// </summary>
        public ICommand RefreshCommand { get; set; }

        #endregion

        #region 构造函数

        public FactionViewModel()
        {
            InitializeCommands();
            RefreshAllProperties();
        }

        private void InitializeCommands()
        {
            AppointAdvisorCommand = new RelayCommand(
                execute: () => ExecuteAppointAdvisor(),
                canExecute: () => IsAppointAdvisorButtonEnabled
            );

            RecallAdvisorCommand = new RelayCommand(
                execute: () => ExecuteRecallAdvisor(),
                canExecute: () => IsRecallAdvisorButtonEnabled
            );

            ShowCandidatesCommand = new RelayCommand(
                execute: () => ExecuteShowCandidates(),
                canExecute: () => CurrentFaction != null && AdvisorCandidates.Count > 0
            );

            RefreshCommand = new RelayCommand(
                execute: () => RefreshAllProperties(),
                canExecute: () => true
            );
        }

        #endregion

        #region 命令执行方法

        /// <summary>
        /// 执行任命军师
        /// </summary>
        private void ExecuteAppointAdvisor()
        {
            if (!IsAppointAdvisorButtonEnabled)
            {
                StatusMessage = $"无法任命军师: {GetAdvisorButtonDisabledReason()}";
                return;
            }

            try
            {
                string formerAdvisorName = CurrentAdvisorName;
                
                // 执行任命
                CurrentFaction.AdvisorID = SelectedPerson.ID;
                CurrentFaction.AppointAdvisor(SelectedPerson);
                
                // 显示成功消息
                if (CurrentFaction.Advisor != null && formerAdvisorName != "无")
                {
                    StatusMessage = $"已将军师从 {formerAdvisorName} 更换为 {SelectedPerson.Name}";
                }
                else
                {
                    StatusMessage = $"已任命 {SelectedPerson.Name} 为军师";
                }
                
                // 刷新UI
                RefreshAllProperties();
            }
            catch (Exception ex)
            {
                StatusMessage = $"任命军师失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 执行罢免军师
        /// </summary>
        private void ExecuteRecallAdvisor()
        {
            if (!IsRecallAdvisorButtonEnabled)
            {
                StatusMessage = "无法罢免军师: 当前没有军师";
                return;
            }

            try
            {
                string formerAdvisorName = CurrentAdvisorName;
                
                // 执行罢免
                CurrentFaction.AdvisorID = -1;
                CurrentFaction.Advisor = null;
                
                StatusMessage = $"已罢免军师 {formerAdvisorName}";
                
                // 刷新UI
                RefreshAllProperties();
            }
            catch (Exception ex)
            {
                StatusMessage = $"罢免军师失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 显示候选人列表
        /// </summary>
        private void ExecuteShowCandidates()
        {
            IsAdvisorDialogOpen = true;
            OnPropertyChanged(nameof(AdvisorCandidates));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取军师按钮禁用的原因
        /// </summary>
        private string GetAdvisorButtonDisabledReason()
        {
            if (CurrentFaction == null) return "没有当前势力";
            if (CurrentFaction.Leader == null) return "没有君主";
            if (CurrentFaction.Leader.BelongedCaptive != null) return "君主被俘虏";
            if (SelectedPerson == null) return "请选择角色";
            
            // 检查选中角色的具体问题
            if (!SelectedPerson.Alive) return "角色已死亡";
            if (!SelectedPerson.Available) return "角色不可用";
            if (SelectedPerson.BelongedCaptive != null) return "角色被俘虏";
            if (SelectedPerson.LocationTroop != null) return "角色在部队中";
            if (SelectedPerson.BelongedFaction != CurrentFaction) return "角色不属于当前势力";
            if (SelectedPerson == CurrentFaction.Leader) return "不能任命君主为军师";
            if (SelectedPerson.Intelligence < 70) return $"智力不足70 (当前:{SelectedPerson.Intelligence})";
            
            // 如果选中的是当前军师
            if (IsSelectedPersonCurrentAdvisor) return "该角色已是当前军师";

            return "可以任命军师";
        }

        /// <summary>
        /// 刷新所有属性
        /// </summary>
        private void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(CurrentFaction));
            OnPropertyChanged(nameof(CurrentAdvisor));
            OnPropertyChanged(nameof(CurrentAdvisorName));
            OnPropertyChanged(nameof(CurrentAdvisorInfo));
            OnPropertyChanged(nameof(AdvisorCandidates));
            OnPropertyChanged(nameof(IsAppointAdvisorButtonEnabled));
            OnPropertyChanged(nameof(IsRecallAdvisorButtonEnabled));
            OnPropertyChanged(nameof(AdvisorButtonText));
            OnPropertyChanged(nameof(AdvisorButtonTooltip));
            OnPropertyChanged(nameof(SelectedPersonInfo));
            OnPropertyChanged(nameof(IsSelectedPersonCurrentAdvisor));
            
            // 刷新命令状态
            (AppointAdvisorCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RecallAdvisorCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ShowCandidatesCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 获取推荐的军师候选人
        /// </summary>
        public Person GetRecommendedAdvisor()
        {
            if (CurrentFaction == null) return null;

            var candidates = CurrentFaction.GetAdvisorCandidates(true); // 排除当前军师
            
            if (candidates.Count > 0)
            {
                // 返回智力最高的候选人
                Person best = candidates[0];
                foreach (Person p in candidates)
                {
                    if (p.Intelligence > best.Intelligence)
                    {
                        best = p;
                    }
                }
                return best;
            }
            
            return null;
        }

        /// <summary>
        /// 检查是否有更好的军师候选人
        /// </summary>
        public bool HasBetterAdvisorCandidate()
        {
            if (CurrentAdvisor == null) return AdvisorCandidates.Count > 0;

            var recommended = GetRecommendedAdvisor();
            return recommended != null && recommended.Intelligence > CurrentAdvisor.Intelligence;
        }

        #endregion

        #region INotifyPropertyChanged 实现

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region RelayCommand 实现

    /// <summary>
    /// 简单的命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion
}

// ===================================================================
// XAML 绑定示例
// ===================================================================

/*
<UserControl x:Class="WorldOfTheThreeKingdoms.Views.FactionView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 当前军师信息 -->
        <TextBlock Grid.Row="0" 
                   Text="{Binding CurrentAdvisorInfo}" 
                   FontWeight="Bold" 
                   Margin="5"/>

        <!-- 选中人物信息 -->
        <TextBlock Grid.Row="1" 
                   Text="{Binding SelectedPersonInfo}" 
                   Margin="5"/>

        <!-- 候选人列表 -->
        <ListBox Grid.Row="2" 
                 ItemsSource="{Binding AdvisorCandidates}"
                 SelectedItem="{Binding SelectedPerson}"
                 DisplayMemberPath="Name"
                 Margin="5"/>

        <!-- 按钮区域 -->
        <StackPanel Grid.Row="3" 
                    Orientation="Horizontal" 
                    HorizontalAlignment="Center" 
                    Margin="5">
            
            <Button Content="{Binding AdvisorButtonText}"
                    Command="{Binding AppointAdvisorCommand}"
                    IsEnabled="{Binding IsAppointAdvisorButtonEnabled}"
                    ToolTip="{Binding AdvisorButtonTooltip}"
                    Margin="5"
                    Padding="10,5"/>

            <Button Content="罢免军师"
                    Command="{Binding RecallAdvisorCommand}"
                    IsEnabled="{Binding IsRecallAdvisorButtonEnabled}"
                    Margin="5"
                    Padding="10,5"/>

            <Button Content="刷新"
                    Command="{Binding RefreshCommand}"
                    Margin="5"
                    Padding="10,5"/>
        </StackPanel>

        <!-- 状态消息 -->
        <TextBlock Grid.Row="4" 
                   Text="{Binding StatusMessage}" 
                   HorizontalAlignment="Center"
                   Margin="5"
                   Foreground="Blue"/>
    </Grid>
</UserControl>
*/

// ===================================================================
// 使用示例
// ===================================================================

public class FactionViewModelExample
{
    public static void DemonstrateViewModel()
    {
        var viewModel = new FactionViewModel();
        
        Console.WriteLine("=== FactionViewModel 测试 ===");
        
        // 测试当前军师信息
        Console.WriteLine($"当前军师: {viewModel.CurrentAdvisorInfo}");
        
        // 测试候选人列表
        Console.WriteLine($"候选人数量: {viewModel.AdvisorCandidates.Count}");
        
        // 模拟选择候选人
        if (viewModel.AdvisorCandidates.Count > 0)
        {
            viewModel.SelectedPerson = viewModel.AdvisorCandidates[0];
            Console.WriteLine($"选中: {viewModel.SelectedPersonInfo}");
            Console.WriteLine($"按钮文本: {viewModel.AdvisorButtonText}");
            Console.WriteLine($"按钮启用: {viewModel.IsAppointAdvisorButtonEnabled}");
            Console.WriteLine($"提示信息: {viewModel.AdvisorButtonTooltip}");
        }
        
        // 测试推荐候选人
        var recommended = viewModel.GetRecommendedAdvisor();
        if (recommended != null)
        {
            Console.WriteLine($"推荐候选人: {recommended.Name} (智力: {recommended.Intelligence})");
        }
    }
}