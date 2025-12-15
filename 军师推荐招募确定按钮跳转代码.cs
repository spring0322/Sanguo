// 军师推荐招募系统 - 点击确定跳转到招募人物列表的完整代码
// 文件位置: WorldOfTheThreeKingdoms/GameManager/AdvisorAdviceEventSystem.cs

using System;
using System.Linq;
using GameObjects;
using WorldOfTheThreeKingdoms.GameScreens;

namespace WorldOfTheThreeKingdoms.GameManager
{
    public static class AdvisorAdviceEventSystem
    {
        /// <summary>
        /// 显示军师推荐招募界面 - 这是点击确定后跳转到人物列表的入口
        /// </summary>
        private static void ShowRecruitmentInterface(MainGameScreen gameScreen, Faction faction, GameObjectList recommendedPersons)
        {
            try
            {
                // 1. 清理当前界面状态
                if (gameScreen.Plugins.tupianwenziPlugin.IsShowing)
                {
                    gameScreen.Plugins.tupianwenziPlugin.IsShowing = false;
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 关闭当前对话框");
                }
                
                if (gameScreen.Plugins.ConfirmationDialogPlugin.IsShowing)
                {
                    gameScreen.Plugins.ConfirmationDialogPlugin.IsShowing = false;
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 关闭确认对话框");
                }
                
                // 2. 清理UndoneWork状态
                int cleanupCount = 0;
                while (gameScreen.PeekUndoneWork().Kind != UndoneWorkKind.None && cleanupCount < 10)
                {
                    gameScreen.PopUndoneWork();
                    cleanupCount++;
                    System.Diagnostics.Debug.WriteLine($"[军师谏言] 清理UndoneWork状态 ({cleanupCount})");
                }
                
                // 3. 设置单选模式
                if (gameScreen.Plugins.TabListPlugin != null)
                {
                    gameScreen.Plugins.TabListPlugin.SetSelectedItemMaxCount(1);
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 设置为单选模式");
                }
                
                // 4. 【核心代码】显示招募人物列表界面
                gameScreen.ShowTabListInFrame(
                    UndoneWorkKind.Frame,           // 工作类型：Frame界面
                    FrameKind.Person,               // 显示类型：人物列表
                    FrameFunction.PersonManualHire, // 功能：手动招募
                    false,                          // OKEnabled: 禁用默认确定按钮
                    true,                           // CancelEnabled: 启用取消按钮
                    true,                           // showCheckBox: 显示选择框
                    false,                          // multiselecting: 单选模式
                    recommendedPersons,             // 显示的人物列表（军师推荐的武将）
                    null,                           // 预选列表（无）
                    "军师推荐招募",                  // 界面标题
                    "Ability"                       // 排序方式：按能力排序
                );
                
                System.Diagnostics.Debug.WriteLine("[军师谏言] ShowTabListInFrame调用成功");
                
                // 5. 【关键】设置确认按钮的回调函数 - 这是点击确定后的处理逻辑
                if (gameScreen.Plugins.GameFramePlugin != null)
                {
                    gameScreen.Plugins.GameFramePlugin.SetOKFunction(new GameDelegates.VoidFunction(() => {
                        HandleRecruitmentConfirm(gameScreen, faction);
                    }));
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 设置招募确认回调");
                }
                
                System.Diagnostics.Debug.WriteLine("[军师谏言] 招募界面显示成功");
            }
            catch (Exception showEx)
            {
                System.Diagnostics.Debug.WriteLine($"[军师谏言] ShowTabListInFrame调用失败: {showEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[军师谏言] 堆栈跟踪: {showEx.StackTrace}");
                
                // 如果显示招募界面失败，回退到自动招募
                System.Diagnostics.Debug.WriteLine("[军师谏言] 回退到自动招募模式");
                
                // 自动招募能力最高的武将
                var bestPerson = recommendedPersons.GameObjects
                    .Cast<Person>()
                    .OrderByDescending(p => p.Command + p.Intelligence + p.Politics + p.Glamour)
                    .FirstOrDefault();
                    
                if (bestPerson != null)
                {
                    bool success = PerformRecruitment(bestPerson, faction);
                    ShowRecruitmentResult(gameScreen, faction, bestPerson, success, success ? "" : "自动招募失败");
                }
            }
        }

        /// <summary>
        /// 处理招募确认操作 - 点击确定按钮后的处理逻辑
        /// </summary>
        private static void HandleRecruitmentConfirm(MainGameScreen gameScreen, Faction faction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[军师谏言] 开始处理招募确认");
                
                // 1. 获取用户选中的武将
                Person selectedPerson = null;
                try
                {
                    selectedPerson = gameScreen.Plugins.TabListPlugin?.SelectedItem as Person;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[军师谏言] 获取选中武将时发生异常: {ex.Message}");
                }
                
                if (selectedPerson != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[军师谏言] 选中武将: {selectedPerson.Name}");
                    
                    // 2. 检查武将是否仍然在野
                    if (selectedPerson.BelongedFaction != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] 武将 {selectedPerson.Name} 已不在野，招募失败");
                        ShowRecruitmentResult(gameScreen, faction, selectedPerson, false, "该武将已被其他势力招募");
                        return;
                    }
                    
                    // 3. 执行招募操作
                    bool recruitSuccess = false;
                    try
                    {
                        recruitSuccess = PerformRecruitment(selectedPerson, faction);
                    }
                    catch (Exception recruitEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] 执行招募时发生异常: {recruitEx.Message}");
                        recruitSuccess = false;
                    }
                    
                    // 4. 显示招募结果
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] 准备显示招募结果: 成功={recruitSuccess}");
                        
                        if (recruitSuccess)
                        {
                            System.Diagnostics.Debug.WriteLine($"[军师谏言] 成功招募武将: {selectedPerson.Name}");
                            ShowRecruitmentResult(gameScreen, faction, selectedPerson, true, "");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[军师谏言] 招募武将失败: {selectedPerson.Name}");
                            ShowRecruitmentResult(gameScreen, faction, selectedPerson, false, "招募失败，该武将拒绝加入");
                        }
                    }
                    catch (Exception resultEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] 显示招募结果时发生异常: {resultEx.Message}");
                        
                        // 显示简单的错误消息
                        try
                        {
                            // 先关闭当前界面
                            if (gameScreen.PeekUndoneWork().Kind == UndoneWorkKind.Frame)
                            {
                                gameScreen.PopUndoneWork();
                            }
                            
                            gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                                faction.Advisor, faction.Advisor, 
                                $"军师 {faction.Advisor.Name}：\n\n主公，招募过程中遇到了困难，请检查结果。", 
                                "", "", "");
                            gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
                            gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine("[军师谏言] 连简单错误消息都无法显示");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[军师谏言] 未选择任何武将");
                    
                    // 显示提示信息
                    try
                    {
                        gameScreen.Plugins.tupianwenziPlugin.SetGameObjectBranch(
                            faction.Advisor, faction.Advisor, 
                            $"军师 {faction.Advisor.Name}：\n\n主公，请先选择要招募的武将。", 
                            "", "", "");
                        gameScreen.Plugins.tupianwenziPlugin.SetPosition(ShowPosition.Bottom, gameScreen);
                        gameScreen.Plugins.tupianwenziPlugin.IsShowing = true;
                    }
                    catch (Exception msgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[军师谏言] 显示提示信息失败: {msgEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[军师谏言] HandleRecruitmentConfirm失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[军师谏言] 异常堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// ShowTabListInFrame方法的参数说明
        /// </summary>
        /*
        gameScreen.ShowTabListInFrame(
            UndoneWorkKind undoneWork,      // 工作类型，通常是Frame
            FrameKind kind,                 // 显示的内容类型：Person=人物列表
            FrameFunction function,         // 功能类型：PersonManualHire=手动招募
            bool OKEnabled,                 // 是否启用确定按钮
            bool CancelEnabled,             // 是否启用取消按钮  
            bool showCheckBox,              // 是否显示选择框
            bool multiselecting,            // 是否允许多选
            GameObjectList gameObjectList,  // 要显示的对象列表
            GameObjectList selectedList,    // 预选的对象列表
            string title,                   // 界面标题
            string tabName                  // 标签页名称
        );
        */
    }
}

/*
调用流程说明：

1. 军师建议触发 → TriggerAdvisorAdvice()
2. 用户点击"确定，我要招募" → ShowRecruitmentInterface()
3. 显示招募人物列表界面 → ShowTabListInFrame()
4. 用户选择武将并点击确定 → HandleRecruitmentConfirm()
5. 执行招募操作 → PerformRecruitment()
6. 显示招募结果 → ShowRecruitmentResult()

关键参数：
- FrameFunction.PersonManualHire: 指定为手动招募功能
- 单选模式: multiselecting = false
- 启用选择框: showCheckBox = true
- 按能力排序: tabName = "Ability"
*/