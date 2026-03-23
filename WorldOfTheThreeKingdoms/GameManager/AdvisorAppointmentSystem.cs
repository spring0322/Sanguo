using System;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms;
using PluginInterface;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 军师任命系统 - 处理任命逻辑和拒绝判定
    /// </summary>
    public static class AdvisorAppointmentSystem
    {
        /// <summary>
        /// 尝试任命军师
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="candidate">候选军师</param>
        /// <param name="faction">势力</param>
        /// <returns>是否任命成功</returns>
        public static bool TryAppointAdvisor(Person leader, Person candidate, Faction faction)
        {
            System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] ========== 开始任命流程 ==========");
            System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] 君主: {leader?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] 候选人: {candidate?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] 势力: {faction?.Name ?? "null"}");
            
            if (leader == null || candidate == null || faction == null)
            {
                System.Diagnostics.Debug.WriteLine("[TryAppointAdvisor] ❌ 参数为空，任命失败");
                return false;
            }

            bool isSuccess = true;

            // 1. 判定逻辑：是否拒绝？
            // 如果是厌恶关系，必定拒绝
            if (candidate.CheckRelation(leader) == -1)
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 厌恶 {leader.Name}，拒绝任命");
            }
            // 忠诚度极低也拒绝
            else if (candidate.Loyalty < 20)
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 忠诚度过低({candidate.Loyalty})，拒绝任命");
            }
            // 高野心 + 低智力的组合可能拒绝（觉得自己被低估）
            else if (candidate.Ambition > 80 && candidate.Intelligence < 70)
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 高野心但智力不足，拒绝任命");
            }
            // 性格严重不合也可能拒绝
            else if (IsPersonalityConflict(leader, candidate))
            {
                isSuccess = false;
                System.Diagnostics.Debug.WriteLine($"[任命拒绝] {candidate.Name} 与 {leader.Name} 性格不合，拒绝任命");
            }

            System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] 任命判定结果: {(isSuccess ? "接受" : "拒绝")}");

            // 2. 根据结果获取对话
            WorldOfTheThreeKingdoms.GameGlobal.DialogueEntry dialogue;
            if (isSuccess)

            {
                System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] 任命前 - 势力军师: {faction.Advisor?.Name ?? "无"}");
                System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] 任命前 - 势力军师ID: {faction.AdvisorID}");
                
                // 成功：获取成功任命对话
                dialogue = AdvisorAppointmentDialogueManager.GetAppointDialogue(leader, candidate, isRefusal: false);
                
                // 执行任命 - 修复：使用Advisor属性而不是直接设置AdvisorID
                faction.Advisor = candidate;  // 这会同时设置AdvisorID和清空缓存
                
                System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] 任命后 - 势力军师: {faction.Advisor?.Name ?? "无"}");
                System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] 任命后 - 势力军师ID: {faction.AdvisorID}");
                System.Diagnostics.Debug.WriteLine($"[任命成功] {leader.Name} 任命 {candidate.Name} 为军师");
            }
            else
            {
                // 失败：获取拒绝对话
                dialogue = AdvisorAppointmentDialogueManager.GetAppointDialogue(leader, candidate, isRefusal: true);
                System.Diagnostics.Debug.WriteLine($"[任命失败] {candidate.Name} 拒绝了 {leader.Name} 的任命");
            }

            // 3. 显示对话UI
            System.Diagnostics.Debug.WriteLine("[TryAppointAdvisor] 准备显示对话UI");
            ShowAppointmentDialogue(leader, candidate, dialogue, isSuccess);

            System.Diagnostics.Debug.WriteLine($"[TryAppointAdvisor] ========== 任命流程结束，结果: {(isSuccess ? "成功" : "失败")} ==========");
            return isSuccess;
        }

        /// <summary>
        /// 检查性格是否严重冲突
        /// </summary>
        private static bool IsPersonalityConflict(Person leader, Person candidate)
        {
            // 霸道君主 vs 仁德军师
            if (leader.Character?.ID == 1 && candidate.Character?.ID == 0)
                return true;
            
            // 狡诈君主 vs 仁德军师
            if (leader.Character?.ID == 4 && candidate.Character?.ID == 0)
                return true;
            
            // 莽撞君主 vs 冷静军师（高智力时）
            if (leader.Character?.ID == 3 && candidate.Character?.ID == 2 && candidate.Intelligence > 85)
                return true;

            return false;
        }

        /// <summary>
        /// 显示任命对话
        /// </summary>
        private static void ShowAppointmentDialogue(Person leader, Person candidate, WorldOfTheThreeKingdoms.GameGlobal.DialogueEntry dialogue, bool isSuccess)
        {

            var mainScreen = Session.MainGame?.mainGameScreen as WorldOfTheThreeKingdoms.GameScreens.MainGameScreen;
            if (mainScreen == null) return;

            if (isSuccess)
            {
                global::GameObjects.DialogueManager.ShowAppointDialogue(leader, candidate, mainScreen);
            }
            else
            {
                global::GameObjects.DialogueManager.ShowRefusalDialogue(leader, candidate, mainScreen);
            }
        }


        /// <summary>
        /// 检查候选人是否会拒绝任命
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="candidate">候选军师</param>
        /// <returns>true=会拒绝, false=会接受</returns>
        public static bool WillRefuseAppointment(Person leader, Person candidate)
        {
            if (leader == null || candidate == null) return true;

            // 厌恶关系必定拒绝
            if (candidate.CheckRelation(leader) == -1) return true;
            
            // 忠诚度极低拒绝
            if (candidate.Loyalty < 20) return true;
            
            // 高野心 + 低智力拒绝
            if (candidate.Ambition > 80 && candidate.Intelligence < 70) return true;
            
            // 性格严重冲突拒绝
            if (IsPersonalityConflict(leader, candidate)) return true;

            return false;
        }

        /// <summary>
        /// 获取拒绝原因描述
        /// </summary>
        public static string GetRefusalReason(Person leader, Person candidate)
        {
            if (leader == null || candidate == null) return "参数错误";

            if (candidate.CheckRelation(leader) == -1) return "厌恶关系";
            if (candidate.Loyalty < 20) return "忠诚度过低";
            if (candidate.Ambition > 80 && candidate.Intelligence < 70) return "高野心低智力";
            if (IsPersonalityConflict(leader, candidate)) return "性格冲突";

            return "无拒绝原因";
        }
    }
}