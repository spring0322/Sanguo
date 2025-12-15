// ===================================================================
// 说服按钮可用性检查优化方案
// 解决每次都要遍历所有城市的性能问题
// ===================================================================

namespace GameObjects
{
    public partial class Architecture : GameObject
    {
        // ===================================================================
        // 方案1：缓存机制 - 推荐方案
        // ===================================================================
        
        /// <summary>
        /// 缓存的可说服区域，避免重复计算
        /// </summary>
        private GameArea _cachedConvinceArea = null;
        
        /// <summary>
        /// 缓存是否有效的标记
        /// </summary>
        private bool _convinceAreaCacheValid = false;
        
        /// <summary>
        /// 获取可说服区域（带缓存优化）
        /// </summary>
        public GameArea GetConvincePersonArchitectureArea()
        {
            // 如果缓存有效，直接返回缓存结果
            if (_convinceAreaCacheValid && _cachedConvinceArea != null)
            {
                return _cachedConvinceArea;
            }
            
            // 重新计算并缓存
            _cachedConvinceArea = CalculateConvincePersonArchitectureArea();
            _convinceAreaCacheValid = true;
            
            return _cachedConvinceArea;
        }
        
        /// <summary>
        /// 实际计算可说服区域的方法
        /// </summary>
        private GameArea CalculateConvincePersonArchitectureArea()
        {
            GameArea area = new GameArea();
            
            foreach (Architecture architecture in Session.Current.Scenario.Architectures)
            {
                if (architecture.BelongedFaction == this.BelongedFaction)
                {
                    if (!architecture.HasCaptive() && !architecture.HasNoFactionPerson())
                    {
                        continue;
                    }
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
                else
                {
                    bool hasEnoughInformation = this.BelongedFaction.GetKnownAreaData(architecture.Position) >= InformationLevel.低;
                    
                    if ((!architecture.HasPerson() && !architecture.HasNoFactionPerson()) || !hasEnoughInformation)
                    {
                        continue;
                    }
                    foreach (Point point in architecture.ArchitectureArea.Area)
                    {
                        area.AddPoint(point);
                    }
                }
            }
            return area;
        }
        
        /// <summary>
        /// 使缓存失效 - 在相关状态改变时调用
        /// </summary>
        public void InvalidateConvinceAreaCache()
        {
            _convinceAreaCacheValid = false;
            _cachedConvinceArea = null;
        }
        
        /// <summary>
        /// 优化后的说服可用性检查
        /// </summary>
        public bool ConvincePersonAvail()
        {
            // 先检查简单条件，避免不必要的复杂计算
            if (!this.HasPerson() || this.Fund < this.ConvincePersonFund)
            {
                return false;
            }
            
            // 最后检查目标区域（可能触发缓存计算）
            return this.GetConvincePersonArchitectureArea().Count > 0;
        }
        
        // ===================================================================
        // 方案2：简化检查 - 最简单方案
        // ===================================================================
        
        /// <summary>
        /// 简化的说服可用性检查 - 不检查具体区域
        /// </summary>
        public bool ConvincePersonAvailSimple()
        {
            // 只检查基本条件：有人物 + 资金充足
            // 假设总是有可说服的目标（在实际执行时再检查）
            return this.HasPerson() && (this.Fund >= this.ConvincePersonFund);
        }
        
        // ===================================================================
        // 方案3：延迟检查 - 在实际需要时才检查
        // ===================================================================
        
        /// <summary>
        /// 延迟检查版本 - 只在用户真正要执行说服时才检查目标
        /// </summary>
        public bool ConvincePersonAvailLazy()
        {
            // UI显示时只检查基本条件
            return this.HasPerson() && (this.Fund >= this.ConvincePersonFund);
        }
        
        /// <summary>
        /// 在用户选择说服时才检查是否有实际目标
        /// </summary>
        public bool HasConvinceTargets()
        {
            return this.GetConvincePersonArchitectureArea().Count > 0;
        }
        
        // ===================================================================
        // 方案4：智能缓存 - 基于游戏状态的缓存管理
        // ===================================================================
        
        /// <summary>
        /// 全局缓存管理器
        /// </summary>
        public static class ConvinceAreaCacheManager
        {
            private static Dictionary<int, GameArea> _globalCache = new Dictionary<int, GameArea>();
            private static int _lastUpdateTurn = -1;
            
            /// <summary>
            /// 获取缓存的说服区域
            /// </summary>
            public static GameArea GetCachedArea(Architecture architecture)
            {
                // 如果回合变了，清空所有缓存
                if (Session.Current.Scenario.CurrentTurn != _lastUpdateTurn)
                {
                    _globalCache.Clear();
                    _lastUpdateTurn = Session.Current.Scenario.CurrentTurn;
                }
                
                // 检查是否有缓存
                if (_globalCache.ContainsKey(architecture.ID))
                {
                    return _globalCache[architecture.ID];
                }
                
                // 计算并缓存
                GameArea area = architecture.CalculateConvincePersonArchitectureArea();
                _globalCache[architecture.ID] = area;
                
                return area;
            }
            
            /// <summary>
            /// 清空特定建筑的缓存
            /// </summary>
            public static void InvalidateCache(int architectureId)
            {
                _globalCache.Remove(architectureId);
            }
            
            /// <summary>
            /// 清空所有缓存
            /// </summary>
            public static void ClearAllCache()
            {
                _globalCache.Clear();
            }
        }
        
        /// <summary>
        /// 使用全局缓存的说服区域获取
        /// </summary>
        public GameArea GetConvincePersonArchitectureAreaCached()
        {
            return ConvinceAreaCacheManager.GetCachedArea(this);
        }
        
        // ===================================================================
        // 方案5：预计算 - 在回合开始时预计算所有建筑的说服区域
        // ===================================================================
        
        /// <summary>
        /// 在回合开始时预计算所有说服区域
        /// </summary>
        public static void PreCalculateAllConvinceAreas()
        {
            foreach (Architecture arch in Session.Current.Scenario.Architectures)
            {
                if (arch.BelongedFaction != null && arch.BelongedFaction.IsAlive)
                {
                    // 预计算并缓存
                    arch.GetConvincePersonArchitectureArea();
                }
            }
        }
    }
}

// ===================================================================
// 缓存失效触发点 - 在这些事件发生时使缓存失效
// ===================================================================

namespace GameObjects
{
    public partial class Architecture
    {
        /// <summary>
        /// 当建筑状态改变时，使相关缓存失效
        /// </summary>
        private void OnArchitectureStateChanged()
        {
            // 使自己的缓存失效
            this.InvalidateConvinceAreaCache();
            
            // 如果是势力变更，需要使所有相关建筑的缓存失效
            if (this.BelongedFaction != null)
            {
                foreach (Architecture arch in this.BelongedFaction.Architectures)
                {
                    arch.InvalidateConvinceAreaCache();
                }
            }
        }
        
        /// <summary>
        /// 当人物状态改变时触发
        /// </summary>
        public void OnPersonStateChanged()
        {
            this.InvalidateConvinceAreaCache();
        }
        
        /// <summary>
        /// 当俘虏状态改变时触发
        /// </summary>
        public void OnCaptiveStateChanged()
        {
            this.InvalidateConvinceAreaCache();
        }
        
        /// <summary>
        /// 当在野人物状态改变时触发
        /// </summary>
        public void OnNoFactionPersonStateChanged()
        {
            this.InvalidateConvinceAreaCache();
        }
    }
    
    public partial class Faction
    {
        /// <summary>
        /// 当情报等级改变时，使相关缓存失效
        /// </summary>
        public void OnInformationLevelChanged(Point position)
        {
            foreach (Architecture arch in this.Architectures)
            {
                arch.InvalidateConvinceAreaCache();
            }
        }
    }
}

// ===================================================================
// 推荐的实现方案
// ===================================================================

namespace GameObjects
{
    public partial class Architecture
    {
        /// <summary>
        /// 推荐方案：结合简化检查和延迟验证
        /// </summary>
        public bool ConvincePersonAvailRecommended()
        {
            // 第一步：快速检查基本条件
            if (!this.HasPerson() || this.Fund < this.ConvincePersonFund)
            {
                return false;
            }
            
            // 第二步：简单的目标存在性检查（不遍历所有城市）
            return this.HasPotentialConvinceTargets();
        }
        
        /// <summary>
        /// 快速检查是否可能有说服目标（不遍历所有城市）
        /// </summary>
        private bool HasPotentialConvinceTargets()
        {
            // 检查己方是否有俘虏或在野人物
            foreach (Architecture arch in this.BelongedFaction.Architectures)
            {
                if (arch.HasCaptive() || arch.HasNoFactionPerson())
                {
                    return true;
                }
            }
            
            // 检查是否有已知的敌方建筑（简化检查）
            return this.BelongedFaction.KnownArchitectures.Count > this.BelongedFaction.Architectures.Count;
        }
        
        /// <summary>
        /// 在用户实际选择说服时才进行完整检查
        /// </summary>
        public bool ValidateConvinceTargetsOnExecution()
        {
            return this.GetConvincePersonArchitectureArea().Count > 0;
        }
    }
}

// ===================================================================
// 使用示例和性能对比
// ===================================================================

namespace GameManager
{
    public class ConvincePerformanceTest
    {
        /// <summary>
        /// 性能测试对比
        /// </summary>
        public static void TestPerformance()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 测试原始方法
            stopwatch.Restart();
            for (int i = 0; i < 100; i++)
            {
                foreach (Architecture arch in Session.Current.Scenario.Architectures)
                {
                    arch.ConvincePersonAvail(); // 原始方法
                }
            }
            long originalTime = stopwatch.ElapsedMilliseconds;
            
            // 测试优化方法
            stopwatch.Restart();
            for (int i = 0; i < 100; i++)
            {
                foreach (Architecture arch in Session.Current.Scenario.Architectures)
                {
                    arch.ConvincePersonAvailRecommended(); // 优化方法
                }
            }
            long optimizedTime = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine($"原始方法耗时: {originalTime}ms");
            Console.WriteLine($"优化方法耗时: {optimizedTime}ms");
            Console.WriteLine($"性能提升: {(double)originalTime / optimizedTime:F2}x");
        }
    }
}

// ===================================================================
// 总结和建议
// ===================================================================

/*
优化方案总结：

1. 【推荐】简化检查 + 延迟验证：
   - UI显示时只检查基本条件（有人物 + 资金充足）
   - 用户实际执行时才检查具体目标
   - 实现简单，性能提升明显

2. 【备选】缓存机制：
   - 缓存计算结果，避免重复遍历
   - 需要管理缓存失效时机
   - 适合频繁检查的场景

3. 【高级】全局缓存管理：
   - 回合级别的缓存管理
   - 内存使用更优化
   - 实现复杂度较高

性能提升预期：
- 简化检查：提升 80-90%（避免遍历所有城市）
- 缓存机制：提升 95%+（重复调用时）
- 延迟验证：用户体验更好（按钮响应更快）

建议实施步骤：
1. 先实施简化检查方案（最小改动）
2. 如果需要更高性能，再添加缓存机制
3. 在用户实际执行说服时进行完整验证
*/