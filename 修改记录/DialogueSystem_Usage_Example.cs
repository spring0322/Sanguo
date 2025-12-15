// ===================================================================
// 对话系统使用示例 - 增强版
// ===================================================================
// 
// 新增功能：
// 1. 状态管理系统 (Hidden/Showing/Finished)
// 2. 打字机效果 (逐字显示文本)
// 3. 点击跳过打字效果
// 4. 更完善的Hide()方法
// 5. 调试和控制方法
// 
// ===================================================================

using GameGlobal;
using GameObjects;

namespace WorldOfTheThreeKingdoms.Examples
{
    /// <summary>
    /// 对话系统使用示例
    /// </summary>
    public class DialogueSystemUsageExample
    {
        /// <summary>
        /// 示例：罢免军师时显示对话（增强版）
        /// </summary>
        public void RecallAdvisorWithDialogue(MainGameScreen screen, Person leader, Person advisor)
        {
            // 1. 加载罢免对话配置
            var recallConfig = DialogueConfigManager.LoadRecallDialogues();
            
            // 2. 查找最佳匹配的对话条目
            var dialogue = DialogueConfigManager.FindBestMatch(recallConfig, leader, advisor);
            
            if (dialogue != null)
            {
                // 3. 显示对话UI，并设置回调函数
                screen.ShowDialogueUI(leader, advisor, dialogue, () => 
                {
                    // --- 这里是回调函数：等玩家看完对话点结束之后，才会执行 ---
                    
                    // 执行实际的罢免逻辑
                    var faction = leader.BelongedFaction;
                    if (faction != null)
                    {
                        // 清除军师职位
                        faction.AdvisorID = -1;
                        
                        // 降低军师忠诚度（因为被罢免）
                        if (advisor != null)
                        {
                            advisor.Loyalty = Math.Max(0, advisor.Loyalty - 10);
                        }
                        
                        // 记录日志
                        System.Diagnostics.Debug.WriteLine($"[罢免军师] {leader.Name} 罢免了军师 {advisor.Name}");
                        
                        // 可以在这里添加其他游戏逻辑，比如：
                        // - 更新UI状态
                        // - 触发事件
                        // - 保存游戏状态等
                    }
                });
            }
            else
            {
                // 没有找到合适的对话，直接执行罢免逻辑（无对话版本）
                System.Diagnostics.Debug.WriteLine("[罢免军师] 未找到合适的对话，直接执行罢免");
                // 执行罢免逻辑...
            }
        }

        /// <summary>
        /// 【新增】示例：高级对话控制
        /// </summary>
        public void AdvancedDialogueControl(MainGameScreen screen)
        {
            // 获取对话UI实例（假设可以通过某种方式访问）
            var dialogueUI = screen.dialogueUI; // 这需要将字段设为public或添加属性
            
            if (dialogueUI != null)
            {
                // 检查对话状态
                System.Diagnostics.Debug.WriteLine($"对话状态: {dialogueUI.GetStateInfo()}");
                
                // 跳过打字机效果
                if (dialogueUI.IsActive)
                {
                    dialogueUI.SkipTypewriter();
                }
                
                // 在特殊情况下强制完成对话
                // dialogueUI.ForceFinish();
            }
        }

        /// <summary>
        /// 示例：任命军师时显示对话
        /// </summary>
        public void AppointAdvisorWithDialogue(MainGameScreen screen, Person leader, Person advisor)
        {
            // 1. 加载任命对话配置
            var appointmentConfig = DialogueConfigManager.LoadAppointmentDialogues();
            
            // 2. 查找最佳匹配的对话条目
            var dialogue = DialogueConfigManager.FindBestMatch(appointmentConfig, leader, advisor);
            
            if (dialogue != null)
            {
                // 3. 显示对话UI，并设置回调函数
                screen.ShowDialogueUI(leader, advisor, dialogue, () => 
                {
                    // --- 任命完成后的回调逻辑 ---
                    
                    var faction = leader.BelongedFaction;
                    if (faction != null)
                    {
                        // 设置新军师
                        faction.AdvisorID = advisor.ID;
                        
                        // 提高军师忠诚度（因为被任命）
                        advisor.Loyalty = Math.Min(100, advisor.Loyalty + 15);
                        
                        // 记录日志
                        System.Diagnostics.Debug.WriteLine($"[任命军师] {leader.Name} 任命了 {advisor.Name} 为军师");
                    }
                });
            }
            else
            {
                // 没有找到合适的对话，直接执行任命逻辑
                System.Diagnostics.Debug.WriteLine("[任命军师] 未找到合适的对话，直接执行任命");
                // 执行任命逻辑...
            }
        }

        /// <summary>
        /// 示例：创建自定义对话条目
        /// </summary>
        public DialogueEntry CreateCustomDialogue()
        {
            return new DialogueEntry
            {
                Type = DialogueType.Recall,
                Relation = RelationType.Love,
                LeaderText = "爱卿多年来辅佐有功，但朕决定让你休息一段时间。",
                AdvisorText = "臣明白主公的苦心，愿意交出军师之职，但仍会为主公效力。"
            };
        }

        /// <summary>
        /// 示例：批量加载和测试对话配置
        /// </summary>
        public void TestDialogueConfigurations()
        {
            try
            {
                // 测试任命对话配置
                var appointmentConfig = DialogueConfigManager.LoadAppointmentDialogues();
                System.Diagnostics.Debug.WriteLine($"任命对话配置加载成功，共 {appointmentConfig.Entries.Count} 条");

                // 测试罢免对话配置
                var recallConfig = DialogueConfigManager.LoadRecallDialogues();
                System.Diagnostics.Debug.WriteLine($"罢免对话配置加载成功，共 {recallConfig.Entries.Count} 条");

                // 测试通用加载方法
                DialogueConfigManager.LoadConfig("Content/Data/AppointmentDialogues.xml");
                var cachedEntries = DialogueConfigManager.CachedEntries;
                System.Diagnostics.Debug.WriteLine($"通用加载方法测试成功，缓存 {cachedEntries.Count} 条对话");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"对话配置测试失败: {ex.Message}");
            }
        }
    }
}

// ===================================================================
// XML配置文件示例
// ===================================================================

/*
AppointmentDialogues.xml 示例：

<?xml version="1.0" encoding="utf-8"?>
<AppointmentDialogues>
  <!-- 羁绊对话：刘备任命诸葛亮 -->
  <Entry Type="Bond" LeaderID="1" AdvisorID="100">
    <LeaderText>孔明先生，请您出山辅佐我匡扶汉室！</LeaderText>
    <AdvisorText>既然主公如此诚心，亮愿鞠躬尽瘁，死而后已！</AdvisorText>
  </Entry>
  
  <!-- 性格匹配：仁德君主任命高智力军师 -->
  <Entry Type="Personality" LeaderKind="1" MinIntelligence="90">
    <LeaderText>先生才华横溢，正是我军所需的军师之才。</LeaderText>
    <AdvisorText>承蒙主公看重，在下定当竭尽所能！</AdvisorText>
  </Entry>
  
  <!-- 关系匹配：任命亲爱的人为军师 -->
  <Entry Type="Personality" Relation="Love">
    <LeaderText>你我情同手足，军师之职非你莫属！</LeaderText>
    <AdvisorText>主公如此信任，我必不负所托！</AdvisorText>
  </Entry>
  
  <!-- 默认对话 -->
  <Entry Type="Default">
    <LeaderText>请您担任我军军师一职。</LeaderText>
    <AdvisorText>遵命，主公。</AdvisorText>
  </Entry>
</AppointmentDialogues>

RecallDialogues.xml 示例：

<?xml version="1.0" encoding="utf-8"?>
<RecallDialogues>
  <!-- 罢免关系好的军师 -->
  <Entry Type="Recall" Relation="Love">
    <LeaderText>爱卿辛苦了，先休息一段时间吧。</LeaderText>
    <AdvisorText>臣明白主公的苦心，愿意交出军师之职。</AdvisorText>
  </Entry>
  
  <!-- 罢免智力低的军师 -->
  <Entry Type="Recall" MaxIntelligence="60">
    <LeaderText>军师之职责任重大，恐怕需要更换人选。</LeaderText>
    <AdvisorText>臣才疏学浅，确实不堪重任。</AdvisorText>
  </Entry>
  
  <!-- 默认罢免对话 -->
  <Entry Type="Recall">
    <LeaderText>军师之职暂时由他人担任。</LeaderText>
    <AdvisorText>臣遵命。</AdvisorText>
  </Entry>
</RecallDialogues>
*/