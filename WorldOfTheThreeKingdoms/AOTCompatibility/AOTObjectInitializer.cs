using GameObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WorldOfTheThreeKingdoms.AOTCompatibility
{
    /// <summary>
    /// AOT兼容的对象初始化器
    /// 提供编译时对象初始化，替换反射依赖的初始化逻辑
    /// </summary>
    public static class AOTObjectInitializer
    {
        private static readonly Dictionary<Type, Action<GameObject>> _initializers;
        private static readonly AOTCompatibilityAdapter _adapter;

        static AOTObjectInitializer()
        {
            _initializers = new Dictionary<Type, Action<GameObject>>();
            _adapter = new AOTCompatibilityAdapter();
            
            RegisterDefaultInitializers();
        }

        /// <summary>
        /// 注册默认的初始化器
        /// </summary>
        private static void RegisterDefaultInitializers()
        {
            Debug.WriteLine("[AOTObjectInitializer] 注册默认初始化器");

            _initializers[typeof(Faction)] = InitializeFaction;
            _initializers[typeof(Person)] = InitializePerson;
            _initializers[typeof(Architecture)] = InitializeArchitecture;
            _initializers[typeof(Troop)] = InitializeTroop;
            _initializers[typeof(Legion)] = InitializeLegion;

            Debug.WriteLine($"[AOTObjectInitializer] 已注册 {_initializers.Count} 个初始化器");
        }

        /// <summary>
        /// 初始化游戏对象
        /// </summary>
        /// <param name="obj">要初始化的对象</param>
        public static void Initialize(GameObject obj)
        {
            if (obj == null)
            {
                Debug.WriteLine("[AOTObjectInitializer] 对象为null，跳过初始化");
                return;
            }

            try
            {
                var type = obj.GetType();
                
                // 使用专用初始化器
                if (_initializers.TryGetValue(type, out var initializer))
                {
                    initializer(obj);
                    Debug.WriteLine($"[AOTObjectInitializer] 使用专用初始化器完成: {type.Name}[{obj.ID}]");
                    return;
                }

                // 使用适配器初始化
                _adapter.InitializeObject(obj);
                Debug.WriteLine($"[AOTObjectInitializer] 使用适配器初始化完成: {type.Name}[{obj.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectInitializer] 初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步初始化游戏对象
        /// </summary>
        /// <param name="obj">要初始化的对象</param>
        public static async Task InitializeAsync(GameObject obj)
        {
            await Task.Run(() => Initialize(obj));
        }

        /// <summary>
        /// 批量初始化游戏对象
        /// </summary>
        /// <param name="objects">要初始化的对象列表</param>
        public static void InitializeBatch(List<GameObject> objects)
        {
            if (objects == null || objects.Count == 0)
            {
                Debug.WriteLine("[AOTObjectInitializer] 对象列表为空，跳过批量初始化");
                return;
            }

            try
            {
                int successCount = 0;
                foreach (var obj in objects)
                {
                    try
                    {
                        Initialize(obj);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[AOTObjectInitializer] 批量初始化单个对象失败: {ex.Message}");
                    }
                }

                Debug.WriteLine($"[AOTObjectInitializer] 批量初始化完成: {successCount}/{objects.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectInitializer] 批量初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步批量初始化游戏对象
        /// </summary>
        /// <param name="objects">要初始化的对象列表</param>
        public static async Task InitializeBatchAsync(List<GameObject> objects)
        {
            await Task.Run(() => InitializeBatch(objects));
        }

        /// <summary>
        /// 初始化Faction对象
        /// </summary>
        private static void InitializeFaction(GameObject obj)
        {
            if (obj is not Faction faction) return;

            try
            {
                // 基础属性初始化
                // if (string.IsNullOrEmpty(faction.Name))
                //     faction.Name = $"势力{faction.ID}";

                if (faction.ColorIndex <= 0)
                    faction.ColorIndex = 1;

                // 资源初始化
                // if (faction.BaseMoney <= 0)
                //     faction.BaseMoney = 1000;

                // if (faction.BaseFood <= 0)
                //     faction.BaseFood = 500;

                if (faction.TechniquePoint <= 0)
                    faction.TechniquePoint = 100;

                if (faction.Reputation <= 0)
                    faction.Reputation = 50;

                // 状态初始化
                faction.Passed = false;

                Debug.WriteLine($"[AOTObjectInitializer] Faction初始化完成: {faction.Name}[{faction.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectInitializer] Faction初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化Person对象
        /// </summary>
        private static void InitializePerson(GameObject obj)
        {
            if (obj is not Person person) return;

            try
            {
                // 基础属性初始化
                // if (string.IsNullOrEmpty(person.Name))
                //     person.Name = $"人物{person.ID}";

                if (string.IsNullOrEmpty(person.SurName))
                    person.SurName = "未知";

                if (string.IsNullOrEmpty(person.GivenName))
                    person.GivenName = "人物";

                // 状态初始化
                person.Alive = true;
                person.Available = true;

                // 能力值初始化
                if (person.Command <= 0) person.Command = 50;
                if (person.Strength <= 0) person.Strength = 50;
                if (person.Intelligence <= 0) person.Intelligence = 50;
                if (person.Politics <= 0) person.Politics = 50;
                if (person.Glamour <= 0) person.Glamour = 50;
                // if (person.Loyalty <= 0) person.Loyalty = 80;
                if (person.Ambition <= 0) person.Ambition = 50;

                Debug.WriteLine($"[AOTObjectInitializer] Person初始化完成: {person.Name}[{person.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectInitializer] Person初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化Architecture对象
        /// </summary>
        private static void InitializeArchitecture(GameObject obj)
        {
            if (obj is not Architecture architecture) return;

            try
            {
                // 基础属性初始化
                // if (string.IsNullOrEmpty(architecture.Name))
                //     architecture.Name = $"城市{architecture.ID}";

                // 资源初始化
                if (architecture.Population <= 0) architecture.Population = 10000;
                if (architecture.Fund <= 0) architecture.Fund = 5000;
                if (architecture.Food <= 0) architecture.Food = 3000;
                if (architecture.Morale <= 0) architecture.Morale = 80;
                if (architecture.Endurance <= 0) architecture.Endurance = 1000;
                // if (architecture.Defense <= 0) architecture.Defense = 500;

                // 发展度初始化
                if (architecture.Agriculture <= 0) architecture.Agriculture = 50;
                if (architecture.Commerce <= 0) architecture.Commerce = 50;
                if (architecture.Technology <= 0) architecture.Technology = 50;
                // if (architecture.Recruit <= 0) architecture.Recruit = 50;

                Debug.WriteLine($"[AOTObjectInitializer] Architecture初始化完成: {architecture.Name}[{architecture.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectInitializer] Architecture初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化Troop对象
        /// </summary>
        private static void InitializeTroop(GameObject obj)
        {
            if (obj is not Troop troop) return;

            try
            {
                // 基础属性初始化
                // if (string.IsNullOrEmpty(troop.Name))
                //     troop.Name = $"部队{troop.ID}";

                // 部队属性初始化
                if (troop.Quantity <= 0) troop.Quantity = 1000;
                if (troop.Morale <= 0) troop.Morale = 80;
                if (troop.Combativity <= 0) troop.Combativity = 80;
                // if (troop.Experience <= 0) troop.Experience = 50;
                if (troop.Food <= 0) troop.Food = 500;
                if (troop.Fund <= 0) troop.Fund = 200;
                // if (troop.Will <= 0) troop.Will = 80;

                // 状态初始化
                // troop.AutoRun = false;
                // troop.Controllable = true;
                troop.Destroyed = false;

                Debug.WriteLine($"[AOTObjectInitializer] Troop初始化完成: {troop.Name}[{troop.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectInitializer] Troop初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化Legion对象
        /// </summary>
        private static void InitializeLegion(GameObject obj)
        {
            if (obj is not Legion legion) return;

            try
            {
                // 基础属性初始化
                // if (string.IsNullOrEmpty(legion.Name))
                //     legion.Name = $"军团{legion.ID}";

                // 🔥 重构：使用新的Kind+Mission系统
                // 日期：2026-03-09
                legion.Kind = LegionKind.AI;
                legion.Mission = LegionMission.Attack;

                Debug.WriteLine($"[AOTObjectInitializer] Legion初始化完成: {legion.Name}[{legion.ID}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AOTObjectInitializer] Legion初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册自定义初始化器
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="initializer">初始化器</param>
        public static void RegisterInitializer(Type type, Action<GameObject> initializer)
        {
            if (type != null && initializer != null)
            {
                _initializers[type] = initializer;
                Debug.WriteLine($"[AOTObjectInitializer] 注册自定义初始化器: {type.Name}");
            }
        }

        /// <summary>
        /// 检查是否支持指定类型的初始化
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>是否支持</returns>
        public static bool IsSupported(Type type)
        {
            return type != null && (_initializers.ContainsKey(type) || _adapter.IsAOTCompatible(type));
        }

        /// <summary>
        /// 获取支持的类型列表
        /// </summary>
        /// <returns>支持的类型列表</returns>
        public static List<Type> GetSupportedTypes()
        {
            return new List<Type>(_initializers.Keys);
        }

        /// <summary>
        /// 获取初始化器统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public static string GetStats()
        {
            return $"AOT对象初始化器: 支持 {_initializers.Count} 种类型的对象初始化";
        }
    }
}