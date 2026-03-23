using GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Tools;

namespace GameObjects.Conditions
{
    [DataContract]
    public class Condition : GameObject
    {
        [DataMember]
        public ConditionKind Kind;
        private string parameter;
        private string parameter2;

        /// <summary>
        /// AOT 修复：反序列化后自动替换 Kind 为子类实例
        /// 适用场景：
        /// 1. 新开剧本（JSON 无 $type 元数据）
        /// 2. 旧存档（JSON 无 $type 元数据）
        /// 不适用：新存档（ReferenceHandler.Preserve 已处理）
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // System.Text.Json 不支持 [OnDeserialized]
            // 实际修复在 CheckCondition 方法中按需执行
        }

        /// <summary>
        /// 确保 Kind 是具体的子类实例（而不是基类）
        /// 仅在首次检查时执行一次
        /// </summary>
        private void EnsureKindIsConcreteType()
        {
            if (Kind == null) return;

            // 如果 Kind 已经是子类，无需修复
            if (Kind.GetType() != typeof(ConditionKind)) return;

            try
            {
                // 🔥 数据源检查：CommonData 可能在加载过程中尚未初始化
                var commonData = CommonData.Current;
                if (commonData?.AllConditionKinds?.ConditionKinds == null)
                {
                    // ⚠️ 这不是错误：可能在数据加载过程中调用
                    // 跳过修复，使用原始的 Kind（虽然是基类，但不会崩溃）
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[Condition.EnsureKindIsConcreteType] ⏳ AllConditionKinds 未就绪，跳过修复，条件 {ID}");
                    #endif
                    return;
                }

                // 🔥 防御性编程：先获取 Kind.ID，捕获可能的异常
                int kindId;
                try
                {
                    kindId = Kind.ID;
                }
                catch (Exception ex)
                {
                    // 🚨 这是真正的错误：Kind 对象状态损坏
                    System.Diagnostics.Debug.WriteLine($"[Condition.EnsureKindIsConcreteType] 🚨 访问 Kind.ID 失败（对象损坏），条件 {ID}: {ex.GetType().Name} - {ex.Message}");
                    
                    // 无法修复，但不抛出异常（避免崩溃）
                    return;
                }

                // 🔥 从字典中查找具体类型
                if (commonData.AllConditionKinds.ConditionKinds.TryGetValue(kindId, out var concreteKind))
                {
                    Kind = concreteKind;
                    
                    // Kind = concreteKind;
                    Kind = concreteKind;
                }
                else
                {
                    // ⚠️ 字典中缺少该 ID，尝试 Factory 降级
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[Condition.EnsureKindIsConcreteType] ⚠️ 字典中未找到 Kind.ID={kindId}，尝试 Factory，条件 {ID}");
                    #endif

                    // 🔥 最后的降级：尝试使用 Factory 创建新实例
                    var factoryKind = ConditionKindFactory.CreateConditionKindByID(kindId);
                    if (factoryKind != null)
                    {
                        Kind = factoryKind;
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[Condition.EnsureKindIsConcreteType] ✅ 使用 Factory 创建 Kind.ID={kindId}，条件 {ID}");
                        #endif
                    }
                    else
                    {
                        // 🚨 Factory 也无法创建，记录错误但不崩溃
                        System.Diagnostics.Debug.WriteLine($"[Condition.EnsureKindIsConcreteType] 🚨 Factory 无法创建 Kind.ID={kindId}（可能缺少 case），条件 {ID}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 🔥 最外层捕获：记录详细信息但不重新抛出（避免崩溃）
                System.Diagnostics.Debug.WriteLine($"[Condition.EnsureKindIsConcreteType] 🚨 未预期的异常: 条件 {ID}, Kind类型={Kind?.GetType().Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"  异常类型: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"  异常消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"  堆栈跟踪: {ex.StackTrace}");
                
                // 不重新抛出，让游戏继续运行
            }
        }


        public Condition Clone()
        {
            return this.MemberwiseClone() as Condition;
        }

        public static bool CheckConditionList(ICollection<Condition> list, Architecture a, Event e = null)
        {
            // 🔥 关键修复：list 参数本身可能为 null
            // 日期：2026-03-16
            // 原因：数据加载时某些对象的 Conditions 集合未初始化
            if (list == null) return false;
            if (a == null) return false;
            bool flag = true;
            bool negate = false;
            foreach (Condition condition in list)
            {
                // 🔥 临时容错 + 日志记录
                // 日期：2026-03-16
                // TODO: 修复数据源后移除此容错逻辑
                // 原因：数据不完整，但游戏需要继续运行
                if (condition == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"⚠️ [CheckConditionList] Condition 集合中存在 null 元素，Architecture={a?.Name}(ID:{a?.ID})");
                    #endif
                    continue;
                }
                
                // 🔥 临时容错 + 日志记录
                // TODO: 修复数据源后移除此容错逻辑
                if (condition.Kind == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"⚠️ [CheckConditionList] Condition {condition.ID} 的 Kind 为 null，Architecture={a?.Name}(ID:{a?.ID})");
                    #endif
                    continue;
                }
                
                if (condition.Kind.ID == 996)
                {
                    negate = true;
                }
                else if (condition.Kind.ID == 997)
                {
                    if (flag) return true;
                    flag = true;
                }
                else
                {
                    if (negate)
                    {
                        if (e == null)
                        {
                            if (condition.CheckCondition(a))
                            {
                                flag = false;
                            }
                        }
                        else
                        {
                            if (condition.CheckCondition(a, e))
                            {
                                flag = false;
                            }
                        }
                    }
                    else
                    {
                        if (e == null)
                        {
                            if (!condition.CheckCondition(a))
                            {
                                flag = false;
                            }
                        }
                        else
                        {
                            if (!condition.CheckCondition(a, e))
                            {
                                flag = false;
                            }
                        }
                    }
                    negate = false;
                }
            }
            return flag;
        }

        public static bool CheckConditionList(ICollection<Condition> list, Person p, Event e = null)
        {
            // 🔥 关键修复：list 参数本身可能为 null
            if (list == null) return false;
            if (p == null) return false;
            bool flag = true;
            bool negate = false;
            foreach (Condition condition in list)
            {
                // 🔥 临时容错 + 日志记录
                // TODO: 修复数据源后移除此容错逻辑
                if (condition == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"⚠️ [CheckConditionList] Condition 集合中存在 null 元素，Person={p?.Name}(ID:{p?.ID})");
                    #endif
                    continue;
                }
                
                //why Kind is null sometimes?
                // 🔥 临时容错 + 日志记录
                // TODO: 修复数据源后移除此容错逻辑
                if (condition.Kind == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"⚠️ [CheckConditionList] Condition {condition.ID} 的 Kind 为 null，Person={p?.Name}(ID:{p?.ID})");
                    #endif
                    flag = false;
                    continue;
                }
                if (condition.Kind.ID == 996)
                {
                    negate = true;
                }
                else if (condition.Kind.ID == 997)
                {
                    if (flag) return true;
                    flag = true;
                }
                else
                {
                    if (negate)
                    {
                        if (e == null)
                        {
                            if (condition.CheckCondition(p))
                            {
                                flag = false;
                            }
                        }
                        else
                        {
                            if (condition.CheckCondition(p, e))
                            {
                                flag = false;
                            }
                        }
                    }
                    else
                    {
                        if (e == null)
                        {
                            if (!condition.CheckCondition(p))
                            {
                                flag = false;
                            }
                        }
                        else
                        {
                            if (!condition.CheckCondition(p, e))
                            {
                                flag = false;
                            }
                        }
                    }
                    negate = false;
                }
            }
            return flag;
        }

        public static bool CheckConditionList(ICollection<Condition> list, Faction f, Event e = null)
        {
            // 🔥 关键修复：list 参数本身可能为 null
            if (list == null) return false;
            if (f == null) return false;
            bool flag = true;
            bool negate = false;
            foreach (Condition condition in list)
            {
                // 🔥 临时容错 + 日志记录
                // TODO: 修复数据源后移除此容错逻辑
                if (condition == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"⚠️ [CheckConditionList] Condition 集合中存在 null 元素，Faction={f?.Name}(ID:{f?.ID})");
                    #endif
                    continue;
                }
                
                // 🔥 临时容错 + 日志记录
                // TODO: 修复数据源后移除此容错逻辑
                if (condition.Kind == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"⚠️ [CheckConditionList] Condition {condition.ID} 的 Kind 为 null，Faction={f?.Name}(ID:{f?.ID})");
                    #endif
                    continue;
                }
                
                if (condition.Kind.ID == 996)
                {
                    negate = true;
                }
                else if (condition.Kind.ID == 997)
                {
                    if (flag) return true;
                    flag = true;
                }
                else
                {
                    if (negate)
                    {
                        if (e == null)
                        {
                            if (condition.CheckCondition(f))
                            {
                                flag = false;
                            }
                        }
                        else
                        {
                            if (condition.CheckCondition(f, e))
                            {
                                flag = false;
                            }
                        }
                    }
                    else
                    {
                        if (e == null)
                        {
                            if (!condition.CheckCondition(f))
                            {
                                flag = false;
                            }
                        }
                        else
                        {
                            if (!condition.CheckCondition(f, e))
                            {
                                flag = false;
                            }
                        }
                    }
                    negate = false;
                }
            }
            return flag;
        }

        public static bool CheckConditionList(ICollection<Condition> list, Troop t, Event e = null)
        {
            // 🔥 关键修复：list 参数本身可能为 null
            if (list == null) return false;
            if (t == null) return false;
            bool flag = true;
            bool negate = false;
            foreach (Condition condition in list)
            {
                // 🔥 临时容错 + 日志记录
                // TODO: 修复数据源后移除此容错逻辑
                if (condition == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"⚠️ [CheckConditionList] Condition 集合中存在 null 元素，Troop={t?.Name}(ID:{t?.ID})");
                    #endif
                    continue;
                }
                
                // 🔥 临时容错 + 日志记录
                // TODO: 修复数据源后移除此容错逻辑
                if (condition.Kind == null)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"⚠️ [CheckConditionList] Condition {condition.ID} 的 Kind 为 null，Troop={t?.Name}(ID:{t?.ID})");
                    #endif
                    continue;
                }
                
                if (condition.Kind.ID == 996)
                {
                    negate = true;
                }
                else if (condition.Kind.ID == 997)
                {
                    if (flag) return true;
                    flag = true;
                }
                else
                {
                    if (negate)
                    {
                        if (e == null)
                        {
                            if (condition.CheckCondition(t))
                            {
                                flag = false;
                            }
                        }
                        else
                        {
                            if (condition.CheckCondition(t, e))
                            {
                                flag = false;
                            }
                        }
                    }
                    else
                    {
                        if (e == null)
                        {
                            if (!condition.CheckCondition(t))
                            {
                                flag = false;
                            }
                        }
                        else
                        {
                            if (!condition.CheckCondition(t, e))
                            {
                                flag = false;
                            }
                        }
                    }
                    negate = false;
                }
            }
            return flag;
        }

        public bool CheckCondition(Architecture architecture, Event e)
        {
            if (this.Kind == null) return false;
            EnsureKindIsConcreteType();
            this.Kind.InitializeParameter(this.Parameter);
            this.Kind.InitializeParameter2(this.Parameter2);
            return this.Kind.CheckConditionKind(architecture, e) || this.Kind.CheckConditionKind(architecture);
        }

        public bool CheckCondition(Faction faction, Event e)
        {
            if (this.Kind == null) return false;
            EnsureKindIsConcreteType();
            this.Kind.InitializeParameter(this.Parameter);
            this.Kind.InitializeParameter2(this.Parameter2);
            return this.Kind.CheckConditionKind(faction, e) || this.Kind.CheckConditionKind(faction);
        }

        public bool CheckCondition(Person person, Event e)
        {
            if (this.Kind == null) return false;
            EnsureKindIsConcreteType();
            this.Kind.InitializeParameter(this.Parameter);
            this.Kind.InitializeParameter2(this.Parameter2);
            return this.Kind.CheckConditionKind(person, e) || this.Kind.CheckConditionKind(person);
        }

        public bool CheckCondition(Troop troop, Event e)
        {
            if (this.Kind == null) return false;
            EnsureKindIsConcreteType();
            this.Kind.InitializeParameter(this.Parameter);
            this.Kind.InitializeParameter2(this.Parameter2);
            return this.Kind.CheckConditionKind(troop, e) || this.Kind.CheckConditionKind(troop);
        }

        public bool CheckCondition(Architecture architecture)
        {
            if (this.Kind == null) return false;
            EnsureKindIsConcreteType();
            this.Kind.InitializeParameter(this.Parameter);
            this.Kind.InitializeParameter2(this.Parameter2);
            return this.Kind.CheckConditionKind(architecture);
        }

        public bool CheckCondition(Faction faction)
        {
            if (this.Kind == null) return false;
            EnsureKindIsConcreteType();
            this.Kind.InitializeParameter(this.Parameter);
            this.Kind.InitializeParameter2(this.Parameter2);

            if (this.Kind.ID >= 2100 && this.Kind.ID < 3000)
            {
                foreach (Architecture a in faction.Architectures)
                {
                    if (this.Kind.CheckConditionKind(faction))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return this.Kind.CheckConditionKind(faction);
            }
        }

        public bool CheckCondition(Person person)
        {
            if (this.Kind == null) return false;
            EnsureKindIsConcreteType();
            this.Kind.InitializeParameter(this.Parameter);
            this.Kind.InitializeParameter2(this.Parameter2);
            return this.Kind.CheckConditionKind(person);
        }

        public bool CheckCondition(Troop troop)
        {
            if (this.Kind == null) return false;
            EnsureKindIsConcreteType();
            this.Kind.InitializeParameter(this.Parameter);
            this.Kind.InitializeParameter2(this.Parameter2);
            
            if (this.Kind.ID == 1010 || this.Kind.ID == 1240)
            {
                // string kindType = this.Kind.GetType().Name;
                // bool result = this.Kind.CheckConditionKind(troop);
                // System.Diagnostics.Debug.WriteLine($"[Condition.CheckCondition] 条件ID: {this.ID}, Kind.ID: {this.Kind.ID}, Kind类型: {kindType}, 结果: {result}");
                // return result;
            }
            
            return this.Kind.CheckConditionKind(troop);
        }
        [DataMember]
        public string Parameter
        {
            get
            {
                return this.parameter;
            }
            set
            {
                this.parameter = value;
            }
        }
        [DataMember]
        public string Parameter2
        {
            get
            {
                return this.parameter2;
            }
            set
            {
                this.parameter2 = value;
            }
        }

        public static List<string> LoadConditionWeightFromString(ConditionTable conditions, string str, out Dictionary<Condition, float> result)
        {
            result = new Dictionary<Condition, float>();

            str = str.NullToString("");

            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            Condition condition = null;
            List<string> errorMsg = new List<string>();
            try
            {
                for (int i = 0; i < strArray.Length; i += 2)
                {
                    if (conditions.Conditions.TryGetValue(int.Parse(strArray[i]), out condition))
                    {
                        result.Add(condition, float.Parse(strArray[i + 1]));
                    }
                    else
                    {
                        errorMsg.Add("条件ID" + int.Parse(strArray[i]) + "不存在");
                    }
                }
            }
            catch
            {
                errorMsg.Add("条件AI应为半型空格分隔的条件ID及数值相间");
            }
            return errorMsg;
        }
    }
}

