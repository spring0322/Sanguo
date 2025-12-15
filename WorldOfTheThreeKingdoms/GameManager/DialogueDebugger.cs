using System;
using System.Text;
using GameObjects;
using GameGlobal;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 对话系统调试工具
    /// </summary>
    public static class DialogueDebugger
    {
        /// <summary>
        /// 测试对话匹配系统
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">军师</param>
        /// <returns>调试信息</returns>
        public static string TestDialogueMatching(Person leader, Person advisor)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("=== 对话匹配测试 ===");
            sb.AppendLine($"君主: {leader.Name} (ID:{leader.ID}, 性格:{leader.CharacterKindID})");
            sb.AppendLine($"军师: {advisor.Name} (ID:{advisor.ID}, 智力:{advisor.Intelligence}, 性格:{advisor.CharacterKindID})");
            sb.AppendLine($"      忠诚:{advisor.Loyalty}, 野心:{advisor.Ambition}, 年龄:{advisor.Age}");
            sb.AppendLine($"      统率:{advisor.Command}, 政治:{advisor.Politics}");
            sb.AppendLine();

            // 测试任命对话
            sb.AppendLine("--- 任命对话匹配 ---");
            var appointEntry = DialogueManager.GetAppointDialogue(leader, advisor, false);
            if (appointEntry != null)
            {
                sb.AppendLine($"匹配类型: {appointEntry.Type}");
                sb.AppendLine($"君主台词: {appointEntry.LeaderText}");
                sb.AppendLine($"军师台词: {appointEntry.AdvisorText}");
            }
            else
            {
                sb.AppendLine("未找到匹配的对话");
            }
            sb.AppendLine();

            // 测试拒绝对话
            sb.AppendLine("--- 拒绝对话匹配 ---");
            var refusalEntry = DialogueManager.GetAppointDialogue(leader, advisor, true);
            if (refusalEntry != null)
            {
                sb.AppendLine($"匹配类型: {refusalEntry.Type}");
                sb.AppendLine($"君主台词: {refusalEntry.LeaderText}");
                sb.AppendLine($"军师台词: {refusalEntry.AdvisorText}");
            }
            else
            {
                sb.AppendLine("未找到匹配的拒绝对话");
            }
            sb.AppendLine();

            // 测试罢免对话
            sb.AppendLine("--- 罢免对话匹配 ---");
            var recallEntry = DialogueManager.GetRecallDialogue(leader, advisor);
            if (recallEntry != null)
            {
                sb.AppendLine($"匹配类型: {recallEntry.Type}");
                sb.AppendLine($"君主台词: {recallEntry.LeaderText}");
                sb.AppendLine($"军师台词: {recallEntry.AdvisorText}");
            }
            else
            {
                sb.AppendLine("未找到匹配的对话");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取配置加载状态
        /// </summary>
        /// <returns>状态信息</returns>
        public static string GetConfigStatus()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("=== 对话配置状态 ===");
            sb.AppendLine(DialogueManager.GetConfigStats());
            sb.AppendLine();
            
            // 测试配置文件是否存在
            sb.AppendLine("--- 配置文件检查 ---");
            sb.AppendLine($"任命对话配置: {(System.IO.File.Exists("Content/Data/AppointmentDialogues.xml") ? "存在" : "缺失")}");
            sb.AppendLine($"罢免对话配置: {(System.IO.File.Exists("Content/Data/RecallDialogues.xml") ? "存在" : "缺失")}");
            
            return sb.ToString();
        }

        /// <summary>
        /// 重新加载配置并返回结果
        /// </summary>
        /// <returns>重载结果</returns>
        public static string ReloadConfigs()
        {
            try
            {
                DialogueManager.ReloadConfigs();
                return "✅ 配置重新加载成功\n" + GetConfigStatus();
            }
            catch (Exception ex)
            {
                return $"❌ 配置重新加载失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 测试特定条件的对话匹配
        /// </summary>
        /// <param name="leader">君主</param>
        /// <param name="advisor">军师</param>
        /// <returns>详细的匹配分析</returns>
        public static string AnalyzeDialogueMatching(Person leader, Person advisor)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("=== 详细对话匹配分析 ===");
            sb.AppendLine($"君主: {leader.Name} (性格:{GetCharacterName(leader.CharacterKindID)})");
            sb.AppendLine($"军师: {advisor.Name}");
            sb.AppendLine($"  - 智力: {advisor.Intelligence}");
            sb.AppendLine($"  - 忠诚: {advisor.Loyalty}");
            sb.AppendLine($"  - 野心: {advisor.Ambition} {(advisor.Ambition > 60 ? "(高野心)" : "(低野心)")}");
            sb.AppendLine($"  - 年龄: {advisor.Age}");
            sb.AppendLine($"  - 统率: {advisor.Command}");
            sb.AppendLine($"  - 政治: {advisor.Politics}");
            sb.AppendLine($"  - 性格: {GetCharacterName(advisor.CharacterKindID)}");
            sb.AppendLine();

            // 分析任命对话匹配
            sb.AppendLine("--- 任命对话匹配分析 ---");
            var appointEntry = DialogueManager.GetAppointDialogue(leader, advisor);
            if (appointEntry != null)
            {
                sb.AppendLine($"✅ 匹配成功 - 类型: {appointEntry.Type}");
                sb.AppendLine($"君主台词: {appointEntry.LeaderText}");
                sb.AppendLine($"军师台词: {appointEntry.AdvisorText}");
                
                // 分析匹配原因
                sb.AppendLine();
                sb.AppendLine("匹配条件分析:");
                AnalyzeMatchConditions(sb, appointEntry, leader, advisor);
            }
            else
            {
                sb.AppendLine("❌ 未找到匹配的对话");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 分析匹配条件
        /// </summary>
        private static void AnalyzeMatchConditions(StringBuilder sb, DialogueEntry entry, Person leader, Person advisor)
        {
            // 分析关系条件
            if (entry.Relation != RelationType.None)
            {
                int relationStatus = advisor.CheckRelation(leader);
                string relationText = relationStatus switch
                {
                    1 => "亲爱",
                    -1 => "厌恶", 
                    _ => "无特殊关系"
                };
                sb.AppendLine($"  💕 关系要求: {entry.Relation} (实际:{relationText}, 值:{advisor.GetRelation(leader)})");
            }

            if (entry.Type == DialogueType.Bond)
            {
                sb.AppendLine($"  🔗 专属羁绊匹配 (君主ID:{entry.LeaderID}, 军师ID:{entry.AdvisorID})");
            }
            else if (entry.Type == DialogueType.Personality)
            {
                if (entry.LeaderKind != -1)
                    sb.AppendLine($"  👑 君主性格匹配: {GetCharacterName(entry.LeaderKind)}");
                
                if (entry.AdvisorKind != -1)
                    sb.AppendLine($"  🎭 军师性格匹配: {GetCharacterName(entry.AdvisorKind)}");
                
                if (entry.MinIntelligence > 0 || entry.MaxIntelligence < 999)
                    sb.AppendLine($"  🧠 智力范围: {entry.MinIntelligence}-{entry.MaxIntelligence} (实际:{advisor.Intelligence})");
                
                if (entry.MinLoyalty > 0 || entry.MaxLoyalty < 100)
                    sb.AppendLine($"  ❤️ 忠诚范围: {entry.MinLoyalty}-{entry.MaxLoyalty} (实际:{advisor.Loyalty})");
                
                if (entry.HighAmbition)
                    sb.AppendLine($"  🔥 高野心要求 (实际:{advisor.Ambition})");
                
                if (entry.MinAge > 0 || entry.MaxAge < 999)
                    sb.AppendLine($"  📅 年龄范围: {entry.MinAge}-{entry.MaxAge} (实际:{advisor.Age})");
                
                if (entry.MinCommand > 0)
                    sb.AppendLine($"  ⚔️ 统率要求: >={entry.MinCommand} (实际:{advisor.Command})");
                
                if (entry.MinPolitics > 0)
                    sb.AppendLine($"  🏛️ 政治要求: >={entry.MinPolitics} (实际:{advisor.Politics})");
            }
            else
            {
                sb.AppendLine($"  📝 默认对话");
            }
        }

        /// <summary>
        /// 获取性格名称
        /// </summary>
        private static string GetCharacterName(int characterKind)
        {
            return characterKind switch
            {
                0 => "仁德",
                1 => "霸道",
                2 => "冷静",
                3 => "莽撞",
                4 => "狡诈",
                _ => "未知"
            };
        }

        /// <summary>
        /// 生成所有可能的对话组合测试
        /// </summary>
        /// <param name="leaders">君主列表</param>
        /// <param name="advisors">军师列表</param>
        /// <returns>测试报告</returns>
        public static string GenerateFullTestReport(PersonList leaders, PersonList advisors)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("=== 完整对话测试报告 ===");
            sb.AppendLine($"测试君主数量: {leaders.Count}");
            sb.AppendLine($"测试军师数量: {advisors.Count}");
            sb.AppendLine();

            int testCount = 0;
            int bondMatches = 0;
            int personalityMatches = 0;
            int defaultMatches = 0;

            foreach (Person leader in leaders.GetList())
            {
                foreach (Person advisor in advisors.GetList())
                {
                    if (leader == advisor) continue; // 跳过自己任命自己的情况
                    
                    testCount++;
                    var entry = DialogueManager.GetAppointDialogue(leader, advisor);
                    
                    if (entry != null)
                    {
                        switch (entry.Type)
                        {
                            case DialogueType.Bond:
                                bondMatches++;
                                sb.AppendLine($"🔗 羁绊匹配: {leader.Name} + {advisor.Name}");
                                break;
                            case DialogueType.Personality:
                                personalityMatches++;
                                break;
                            case DialogueType.Default:
                                defaultMatches++;
                                break;
                        }
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("--- 匹配统计 ---");
            sb.AppendLine($"总测试组合: {testCount}");
            sb.AppendLine($"专属羁绊匹配: {bondMatches}");
            sb.AppendLine($"性格匹配: {personalityMatches}");
            sb.AppendLine($"默认匹配: {defaultMatches}");
            sb.AppendLine($"匹配率: {((bondMatches + personalityMatches + defaultMatches) * 100.0 / testCount):F1}%");

            return sb.ToString();
        }

        /// <summary>
        /// 测试任命系统逻辑
        /// </summary>
        public static string TestAppointmentSystem(Person leader, Person candidate)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("=== 任命系统逻辑测试 ===");
            sb.AppendLine($"君主: {leader.Name}");
            sb.AppendLine($"候选军师: {candidate.Name}");
            sb.AppendLine();

            // 检查关系状态
            int relationStatus = candidate.CheckRelation(leader);
            string relationText = relationStatus switch
            {
                1 => "亲爱",
                -1 => "厌恶",
                _ => "普通"
            };
            sb.AppendLine($"关系状态: {relationText} (值: {relationStatus})");
            sb.AppendLine($"忠诚度: {candidate.Loyalty}");
            sb.AppendLine($"野心: {candidate.Ambition}");
            sb.AppendLine($"智力: {candidate.Intelligence}");
            sb.AppendLine();

            // 预测任命结果
            bool willRefuse = AdvisorAppointmentSystem.WillRefuseAppointment(leader, candidate);
            sb.AppendLine($"预测结果: {(willRefuse ? "会拒绝" : "会接受")}");
            
            if (willRefuse)
            {
                string reason = AdvisorAppointmentSystem.GetRefusalReason(leader, candidate);
                sb.AppendLine($"拒绝原因: {reason}");
            }
            sb.AppendLine();

            // 显示对应的对话
            var dialogue = DialogueManager.GetDialogue(leader, candidate, willRefuse);
            sb.AppendLine($"对话类型: {dialogue.Type}");
            sb.AppendLine($"君主台词: {dialogue.LeaderText}");
            sb.AppendLine($"军师台词: {dialogue.AdvisorText}");

            return sb.ToString();
        }
    }
}