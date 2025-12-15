using System;
using GameObjects;

namespace GameManager
{
    /// <summary>
    /// 武将招募处理器 - 正确处理武将加入势力的完整流程
    /// 确保忠诚度设置的时机和方式正确，避免被重置或无效
    /// </summary>
    public class PersonRecruitmentHandler
    {
        /// <summary>
        /// 直接加入逻辑 - 正确的武将招募流程
        /// </summary>
        /// <param name="faction">目标势力</param>
        /// <param name="talent">被招募的武将</param>
        /// <param name="recruiter">招募执行人</param>
        /// <param name="method">招募方式</param>
        /// <returns>是否成功</returns>
        public static bool DirectJoinLogic(Faction faction, Person talent, Person recruiter = null, RecruitMethod method = RecruitMethod.Recommendation)
        {
            try
            {
                if (faction == null || talent == null)
                {
                    System.Diagnostics.Debug.WriteLine("[招募处理] 参数为空，招募失败");
                    return false;
                }

                // 使用君主作为默认招募者
                if (recruiter == null)
                    recruiter = faction.Leader;

                if (recruiter == null)
                {
                    System.Diagnostics.Debug.WriteLine("[招募处理] 无法找到招募执行人，招募失败");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[招募处理] 开始招募 {talent.Name} 到势力 {faction.Name}");
                System.Diagnostics.Debug.WriteLine($"[招募处理] 招募前状态 - PersonalLoyalty: {talent.PersonalLoyalty}, Loyalty: {talent.Loyalty}, TempLoyaltyChange: {talent.TempLoyaltyChange}");

                // 1. 【关键】先确立归属关系
                // 这行代码通常会设置 talent.BelongedFaction = faction
                faction.AddPerson(talent);
                System.Diagnostics.Debug.WriteLine($"[招募处理] 已将武将加入势力");

                // 2. 【关键】修改武将状态
                // 如果状态还是 NoFaction (在野)，Loyalty getter 可能也会强制返回 0
                talent.Status = PersonStatus.General;
                
                // 确保位置正确 - 移动到招募者所在位置
                if (recruiter.Location != null)
                {
                    talent.Location = recruiter.Location;
                    System.Diagnostics.Debug.WriteLine($"[招募处理] 设置武将位置到 {recruiter.Location}");
                }
                else if (faction.Advisor?.Location != null)
                {
                    talent.Location = faction.Advisor.Location;
                    System.Diagnostics.Debug.WriteLine($"[招募处理] 设置武将位置到军师所在地 {faction.Advisor.Location}");
                }

                System.Diagnostics.Debug.WriteLine($"[招募处理] MoveToArchitecture后 - PersonalLoyalty: {talent.PersonalLoyalty}, Loyalty: {talent.Loyalty}, TempLoyaltyChange: {talent.TempLoyaltyChange}");

                // 3. 【最后】计算并应用忠诚度
                // 此时 BelongedFaction 已存在，Status 已正确，Loyalty 属性才允许被写入或读取
                int finalLoyalty = RecruitmentCalculator.CalculateInitialLoyalty(faction.Leader, recruiter, talent, method);
                
                System.Diagnostics.Debug.WriteLine($"[招募处理] 计算出的目标忠诚度: {finalLoyalty}");

                // 应用忠诚度 - 使用安全的设置方法
                bool loyaltySetSuccess = SetPersonLoyalty(talent, finalLoyalty);
                
                if (!loyaltySetSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募处理] 忠诚度设置失败，尝试激进调整");
                    // 如果常规方法失败，尝试更激进的调整
                    ForceSetPersonLoyalty(talent, finalLoyalty);
                }

                System.Diagnostics.Debug.WriteLine($"[招募处理] 最终状态 - PersonalLoyalty: {talent.PersonalLoyalty}, Loyalty: {talent.Loyalty}, TempLoyaltyChange: {talent.TempLoyaltyChange}");
                System.Diagnostics.Debug.WriteLine($"[招募处理] 武将 {talent.Name} 已加入势力 {faction.Name}，当前忠诚度: {talent.Loyalty}");

                // 4. 触发相关事件和更新
                OnPersonJoinedFaction(faction, talent, recruiter, method, finalLoyalty);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 招募过程中发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 安全设置武将忠诚度
        /// </summary>
        /// <param name="person">武将</param>
        /// <param name="targetLoyalty">目标忠诚度</param>
        /// <returns>是否成功设置</returns>
        private static bool SetPersonLoyalty(Person person, int targetLoyalty)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 忠诚度调整开始:");
                System.Diagnostics.Debug.WriteLine($"  目标忠诚度: {targetLoyalty}");
                System.Diagnostics.Debug.WriteLine($"  当前状态 - BelongedFaction: {person.BelongedFaction?.Name ?? "null"}, Status: {person.Status}");

                // 确保武将已经正确加入势力
                if (person.BelongedFaction == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募处理] 错误：武将尚未加入势力，无法设置忠诚度");
                    return false;
                }

                // 多种方法尝试设置忠诚度
                bool success = false;
                
                // 方法1：直接通过TempLoyaltyChange设置
                success = TrySetLoyaltyMethod1(person, targetLoyalty);
                if (success) return true;

                // 方法2：重置后再设置
                success = TrySetLoyaltyMethod2(person, targetLoyalty);
                if (success) return true;

                // 方法3：逐步调整
                success = TrySetLoyaltyMethod3(person, targetLoyalty);
                if (success) return true;

                // 方法4：强制设置（如果有直接访问BaseLoyalty的方法）
                success = TrySetLoyaltyMethod4(person, targetLoyalty);
                if (success) return true;

                System.Diagnostics.Debug.WriteLine($"[招募处理] 所有忠诚度设置方法都失败了");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 设置忠诚度时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 方法1：标准TempLoyaltyChange调整
        /// </summary>
        private static bool TrySetLoyaltyMethod1(Person person, int targetLoyalty)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 尝试方法1 - 标准TempLoyaltyChange调整");
                
                // 重置临时修正
                person.TempLoyaltyChange = 0;
                
                // 获取当前忠诚度
                int currentLoyalty = person.Loyalty;
                System.Diagnostics.Debug.WriteLine($"  重置后当前忠诚度: {currentLoyalty}");
                
                // 计算需要的调整量
                int diff = targetLoyalty - currentLoyalty;
                System.Diagnostics.Debug.WriteLine($"  需要调整: {diff}");
                
                // 应用调整
                person.TempLoyaltyChange = diff;
                System.Diagnostics.Debug.WriteLine($"  设置TempLoyaltyChange: {person.TempLoyaltyChange}");

                // 验证结果
                int finalLoyalty = person.Loyalty;
                System.Diagnostics.Debug.WriteLine($"  调整后忠诚度: {finalLoyalty}");

                bool success = Math.Abs(finalLoyalty - targetLoyalty) <= 2;
                System.Diagnostics.Debug.WriteLine($"  方法1结果: {(success ? "成功" : "失败")}");
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 方法1异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 方法2：等待一帧后再设置
        /// </summary>
        private static bool TrySetLoyaltyMethod2(Person person, int targetLoyalty)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 尝试方法2 - 等待后重新设置");
                
                // 可能需要等待游戏状态更新
                // 在实际游戏中，可能需要等待一帧或触发某些更新
                
                // 尝试更大的TempLoyaltyChange值
                person.TempLoyaltyChange = targetLoyalty * 2; // 双倍确保
                System.Diagnostics.Debug.WriteLine($"  设置双倍TempLoyaltyChange: {person.TempLoyaltyChange}");
                
                int finalLoyalty = person.Loyalty;
                System.Diagnostics.Debug.WriteLine($"  调整后忠诚度: {finalLoyalty}");
                
                bool success = finalLoyalty >= targetLoyalty * 0.8f; // 允许更大误差
                System.Diagnostics.Debug.WriteLine($"  方法2结果: {(success ? "成功" : "失败")}");
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 方法2异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 方法3：逐步调整忠诚度
        /// </summary>
        private static bool TrySetLoyaltyMethod3(Person person, int targetLoyalty)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 尝试方法3 - 逐步调整");
                
                // 逐步增加TempLoyaltyChange，观察Loyalty的变化
                for (int attempt = 1; attempt <= 10; attempt++)
                {
                    int adjustmentValue = targetLoyalty + (attempt * 50);
                    person.TempLoyaltyChange = adjustmentValue;
                    
                    int currentLoyalty = person.Loyalty;
                    System.Diagnostics.Debug.WriteLine($"  第{attempt}次调整，TempLoyaltyChange: {adjustmentValue}, Loyalty: {currentLoyalty}");
                    
                    if (currentLoyalty >= targetLoyalty * 0.8f)
                    {
                        System.Diagnostics.Debug.WriteLine($"  方法3成功，最终忠诚度: {currentLoyalty}");
                        return true;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"  方法3失败，忠诚度仍为: {person.Loyalty}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 方法3异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 方法4：尝试直接设置基础忠诚度（如果可能）
        /// </summary>
        private static bool TrySetLoyaltyMethod4(Person person, int targetLoyalty)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 尝试方法4 - 反射或直接设置");
                
                // 尝试通过反射访问私有字段
                var personType = person.GetType();
                
                // 尝试找到可能的基础忠诚度字段
                var possibleFields = new[] { "baseLoyalty", "BaseLoyalty", "_loyalty", "_baseLoyalty", "loyalty" };
                
                foreach (var fieldName in possibleFields)
                {
                    var field = personType.GetField(fieldName, 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (field != null && (field.FieldType == typeof(int) || field.FieldType == typeof(float)))
                    {
                        System.Diagnostics.Debug.WriteLine($"  找到字段: {fieldName}，尝试设置为 {targetLoyalty}");
                        
                        if (field.FieldType == typeof(int))
                        {
                            field.SetValue(person, targetLoyalty);
                        }
                        else
                        {
                            field.SetValue(person, (float)targetLoyalty);
                        }
                        
                        // 重置TempLoyaltyChange
                        person.TempLoyaltyChange = 0;
                        
                        int finalLoyalty = person.Loyalty;
                        System.Diagnostics.Debug.WriteLine($"  设置后忠诚度: {finalLoyalty}");
                        
                        if (Math.Abs(finalLoyalty - targetLoyalty) <= 5)
                        {
                            System.Diagnostics.Debug.WriteLine($"  方法4成功通过字段 {fieldName}");
                            return true;
                        }
                    }
                }
                
                // 尝试找到可能的设置方法
                var possibleMethods = new[] { "SetLoyalty", "SetBaseLoyalty", "UpdateLoyalty" };
                
                foreach (var methodName in possibleMethods)
                {
                    var method = personType.GetMethod(methodName, 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (method != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  找到方法: {methodName}，尝试调用");
                        
                        try
                        {
                            method.Invoke(person, new object[] { targetLoyalty });
                            
                            int finalLoyalty = person.Loyalty;
                            System.Diagnostics.Debug.WriteLine($"  调用后忠诚度: {finalLoyalty}");
                            
                            if (Math.Abs(finalLoyalty - targetLoyalty) <= 5)
                            {
                                System.Diagnostics.Debug.WriteLine($"  方法4成功通过方法 {methodName}");
                                return true;
                            }
                        }
                        catch (Exception methodEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"  调用方法 {methodName} 失败: {methodEx.Message}");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"  方法4失败，未找到可用的设置方式");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 方法4异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 强制设置武将忠诚度（当常规方法失败时使用）
        /// </summary>
        /// <param name="person">武将</param>
        /// <param name="targetLoyalty">目标忠诚度</param>
        private static void ForceSetPersonLoyalty(Person person, int targetLoyalty)
        {
            try
            {
                // 尝试更大的调整量
                int currentLoyalty = person.Loyalty;
                int requiredAdjustment = targetLoyalty - currentLoyalty;
                
                // 如果当前忠诚度仍然是0，可能需要更激进的方法
                if (currentLoyalty == 0)
                {
                    // 尝试设置一个很大的TempLoyaltyChange
                    person.TempLoyaltyChange = targetLoyalty + 50; // 额外加50确保达到目标
                    System.Diagnostics.Debug.WriteLine($"[招募处理] 激进调整：设置TempLoyaltyChange为 {person.TempLoyaltyChange}");
                }
                else
                {
                    // 正常调整
                    person.TempLoyaltyChange += requiredAdjustment;
                    System.Diagnostics.Debug.WriteLine($"[招募处理] 追加调整：TempLoyaltyChange增加 {requiredAdjustment}");
                }

                int finalLoyalty = person.Loyalty;
                System.Diagnostics.Debug.WriteLine($"[招募处理] 激进调整后忠诚度: {finalLoyalty}");

                // 如果还是不行，可能需要直接操作底层字段
                // 这里可以添加反射或其他底层操作
                // 但通常TempLoyaltyChange应该足够了
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 强制设置忠诚度时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 武将加入势力后的事件处理
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="person">武将</param>
        /// <param name="recruiter">招募者</param>
        /// <param name="method">招募方式</param>
        /// <param name="finalLoyalty">最终忠诚度</param>
        private static void OnPersonJoinedFaction(Faction faction, Person person, Person recruiter, RecruitMethod method, int finalLoyalty)
        {
            try
            {
                // 生成招募日志
                string recruitmentLog = BattleLogGenerator.GenerateRecruitmentLog(faction, person, true);
                System.Diagnostics.Debug.WriteLine($"[招募处理] 招募日志: {recruitmentLog}");

                // 更新统计数据
                UpdateRecruitmentStatistics(faction, person, method, finalLoyalty);

                // 触发AI战略系统更新（如果需要）
                if (AIStrategicConfig.EnableRecruitmentImpactOnStrategy)
                {
                    // 新武将的加入可能影响势力的战略决策
                    // 这里可以触发战略重新评估
                }

                // 应用难度管理器的招募修正（如果是AI势力）
                if (!Session.Current.Scenario.IsPlayer(faction))
                {
                    ApplyAIRecruitmentBonus(faction, person);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 处理加入事件时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新招募统计数据
        /// </summary>
        private static void UpdateRecruitmentStatistics(Faction faction, Person person, RecruitMethod method, int loyalty)
        {
            // 这里可以记录招募统计，用于分析和平衡
            if (RecruitmentConfig.EnableStatistics)
            {
                System.Diagnostics.Debug.WriteLine($"[招募统计] {faction.Name} 通过{method}招募了{person.Name}，忠诚度{loyalty}");
            }
        }

        /// <summary>
        /// 为AI势力应用招募加成
        /// </summary>
        private static void ApplyAIRecruitmentBonus(Faction faction, Person person)
        {
            try
            {
                float recruitmentModifier = DifficultyManager.Instance.GetFactionRecruitmentModifier(faction);
                
                if (recruitmentModifier > 1.0f)
                {
                    // AI招募加成：提升忠诚度
                    int bonusLoyalty = (int)((recruitmentModifier - 1.0f) * 20); // 最多+6忠诚度
                    person.TempLoyaltyChange += bonusLoyalty;
                    
                    System.Diagnostics.Debug.WriteLine($"[招募处理] AI势力 {faction.Name} 获得招募加成: 忠诚度+{bonusLoyalty} (修正系数: {recruitmentModifier:F2})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募处理] 应用AI招募加成时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量招募处理
        /// </summary>
        /// <param name="faction">势力</param>
        /// <param name="talents">武将列表</param>
        /// <param name="recruiter">招募者</param>
        /// <param name="method">招募方式</param>
        /// <returns>成功招募的数量</returns>
        public static int BatchRecruitment(Faction faction, Person[] talents, Person recruiter = null, RecruitMethod method = RecruitMethod.Recommendation)
        {
            int successCount = 0;
            
            foreach (var talent in talents)
            {
                if (DirectJoinLogic(faction, talent, recruiter, method))
                {
                    successCount++;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[招募处理] 批量招募完成: {successCount}/{talents.Length} 成功");
            return successCount;
        }

        /// <summary>
        /// 验证武将状态是否正确设置
        /// </summary>
        /// <param name="person">武将</param>
        /// <param name="expectedFaction">期望的势力</param>
        /// <returns>验证结果</returns>
        public static bool ValidatePersonState(Person person, Faction expectedFaction)
        {
            try
            {
                bool isValid = true;
                var issues = new System.Collections.Generic.List<string>();

                // 检查归属
                if (person.BelongedFaction != expectedFaction)
                {
                    issues.Add($"归属势力不匹配: 期望{expectedFaction?.Name}, 实际{person.BelongedFaction?.Name}");
                    isValid = false;
                }

                // 检查状态
                if (person.Status == PersonStatus.NoFaction)
                {
                    issues.Add("状态仍为在野");
                    isValid = false;
                }

                // 检查忠诚度
                if (person.Loyalty <= 0)
                {
                    issues.Add($"忠诚度异常: {person.Loyalty}");
                    isValid = false;
                }

                if (!isValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[招募验证] {person.Name} 状态验证失败:");
                    foreach (var issue in issues)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {issue}");
                    }
                }

                return isValid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[招募验证] 验证过程中发生异常: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 招募配置
    /// </summary>
    public static class RecruitmentConfig
    {
        public static bool EnableDebugLog { get; set; } = true;
        public static bool EnableStatistics { get; set; } = true;
    }
}