using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using GameObjects;
using GameGlobal;
using WorldOfTheThreeKingdoms.GameManager; // 引入 PathfindingManager 等辅助类

namespace GameManager
{
    /// <summary>
    /// 地形类型枚举 - 寻路系统专用的简化地形分类
    /// 🎯 设计原则：寻路系统只需要知道这些基础信息，不需要复杂的游戏逻辑
    /// </summary>
    public enum TerrainType 
    { 
        Plain,      // 平原 - 标准移动
        Forest,     // 森林 - 较慢移动
        Mountain,   // 山地 - 很慢移动或不可通行
        River,      // 河流 - 需要特殊单位或桥梁
        City,       // 城市 - 可能有特殊规则
        Wall        // 城墙 - 通常不可通行
    }
    
    /// <summary>
    /// 地图信息提供者接口 - 寻路系统的清洁抽象层
    /// 🎯 核心设计理念：
    /// 1. 接口隔离原则 - 只暴露寻路需要的最小信息
    /// 2. 依赖倒置原则 - 寻路系统不依赖具体的游戏世界实现
    /// 3. 单一职责原则 - 专注于提供寻路相关的地图信息
    /// </summary>
    public interface IMapInfoProvider
    {
        /// <summary>
        /// 获取基础地形类型 - 用于移动消耗计算
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <returns>简化的地形类型</returns>
        TerrainType GetTerrain(int x, int y);
        
        /// <summary>
        /// 检查是否有单位占据 - 用于碰撞检测
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <returns>true 表示有单位占据</returns>
        bool IsUnitAt(int x, int y);
        
        /// <summary>
        /// 检查某点是否在敌方控制区 (ZOC) - 用于战术路径规划
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <param name="factionId">寻路单位所属的势力 ID</param>
        /// <returns>true 表示在敌方控制区内</returns>
        bool IsInEnemyZOC(int x, int y, int factionId);
        
        /// <summary>
        /// 获取地图边界信息 - 用于边界检查
        /// </summary>
        /// <returns>地图的宽度和高度</returns>
        (int width, int height) GetMapBounds();
        
        /// <summary>
        /// 检查坐标是否在地图范围内
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <returns>true 表示在地图范围内</returns>
        bool IsInBounds(int x, int y);
    }
    /// <summary>
    /// 优化的单位移动系统 - 基于分层寻路和动态重计算
    /// 🎯 核心特点：
    /// 1. 分层路径管理：宏观路径 + 局部路径
    /// 2. 动态障碍检测：实时检测路径阻塞
    /// 3. 智能重计算：局部重算 vs 全局重算
    /// 4. 高性能优化：O(1)障碍检测，最小化寻路开销
    /// </summary>
    public class UnitMovement
    {
        // ==========================================
        // 1. 路径管理系统
        // ==========================================
        
        /// <summary>
        /// 宏观路径：只存大区块的中心点
        /// </summary>
        private Queue<Point> _chunkPath = new Queue<Point>();
        
        /// <summary>
        /// 当前详细路径：从当前位置到下一个区块中心的具体路径
        /// </summary>
        private List<Point> _currentDetailPath = new List<Point>();
        
        /// <summary>
        /// 当前路径索引
        /// </summary>
        private int _pathIndex = 0;
        
        /// <summary>
        /// 当前目标：下一个区块的入口，或者最终目标
        /// </summary>
        private Vector2 _currentTarget;
        
        /// <summary>
        /// 当前正在前往的大格子中心点
        /// </summary>
        private Point _currentHighLevelTarget;
        
        /// <summary>
        /// 最终目标位置
        /// </summary>
        private Vector2 _finalDestination;
        
        /// <summary>
        /// 所属单位
        /// </summary>
        private Troop _owner;
        
        /// <summary>
        /// 单位类型 - 用于地形适应性计算
        /// </summary>
        public UnitType UnitType { get; set; } = UnitType.步兵;
        
        /// <summary>
        /// 单位势力 - 用于 ZOC 计算
        /// </summary>
        public Faction UnitFaction => _owner?.BelongedFaction;
        
        // ==========================================
        // 2. 状态管理
        // ==========================================
        
        /// <summary>
        /// 移动状态
        /// </summary>
        public enum MovementState
        {
            Idle,           // 空闲
            Moving,         // 正常移动
            Recalculating,  // 重新计算路径中
            Blocked,        // 被阻挡
            Arrived         // 已到达
        }
        
        public MovementState State { get; private set; } = MovementState.Idle;
        
        /// <summary>
        /// 移动速度（每秒移动的格子数）
        /// </summary>
        public float MovementSpeed { get; set; } = 2.0f;
        
        /// <summary>
        /// 路径重计算冷却时间（防止频繁重算）
        /// </summary>
        private float _recalculationCooldown = 0f;
        private const float RECALCULATION_INTERVAL = 1.0f; // 1秒
        
        /// <summary>
        /// 连续阻塞计数（用于判断是否需要全局重算）
        /// </summary>
        private int _consecutiveBlockCount = 0;
        private const int MAX_CONSECUTIVE_BLOCKS = 3;
        
        // ==========================================
        // 3. 性能优化
        // ==========================================
        
        /// <summary>
        /// 障碍检测缓存（避免重复检测同一位置）
        /// </summary>
        private Dictionary<Point, bool> _obstacleCache = new Dictionary<Point, bool>();
        private float _obstacleCacheTime = 0f;
        private const float OBSTACLE_CACHE_DURATION = 2.0f; // 2秒缓存
        
        /// <summary>
        /// 路径重用：如果目标相近，尝试重用部分路径
        /// </summary>
        private Vector2 _lastDestination;
        private List<Point> _lastPath;
        
        // ==========================================
        // 帧率优化系统 - 分离视觉和逻辑更新
        // ==========================================
        
        /// <summary>
        /// 逻辑更新计时器
        /// </summary>
        private double _logicTimer;
        
        /// <summary>
        /// 逻辑更新间隔 - 100ms 更新一次逻辑足够了，也就是每秒 10 帧
        /// </summary>
        private const double LOGIC_INTERVAL = 0.1;
        
        /// <summary>
        /// 视觉位置 - 用于平滑移动插值
        /// </summary>
        private Vector2 _visualPosition;
        
        /// <summary>
        /// 逻辑位置 - 实际的游戏逻辑位置
        /// </summary>
        private Vector2 _logicPosition;
        
        /// <summary>
        /// 移动插值速度
        /// </summary>
        private float _interpolationSpeed = 8.0f;
        
        public UnitMovement(Troop owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            
            // 自动根据部队的军事类型确定单位类型
            UnitType = DetermineUnitType(owner);
            
            // 初始化位置系统
            Vector2 initialPos = new Vector2(owner.Position.X, owner.Position.Y);
            _logicPosition = initialPos;
            _visualPosition = initialPos;
            _logicTimer = 0;
        }
        
        /// <summary>
        /// 根据部队的军事类型自动确定单位类型
        /// </summary>
        /// <param name="troop">部队</param>
        /// <returns>单位类型</returns>
        private UnitType DetermineUnitType(Troop troop)
        {
            if (troop?.Army?.Kind == null)
                return UnitType.步兵; // 默认为步兵
            
            // 根据军事类型的名称或属性判断
            var militaryKind = troop.Army.Kind;
            
            // 这里需要根据实际的 MilitaryKind 属性来判断
            // 以下是示例逻辑，需要根据实际游戏数据调整
            
            try
            {
                // 检查是否是水军
                if (militaryKind.ToString().Contains("水军") || 
                    militaryKind.ToString().Contains("舰") ||
                    militaryKind.ToString().Contains("船"))
                {
                    return UnitType.水军;
                }
                
                // 检查是否是骑兵
                if (militaryKind.ToString().Contains("骑兵") || 
                    militaryKind.ToString().Contains("骑") ||
                    militaryKind.ToString().Contains("马"))
                {
                    return UnitType.骑兵;
                }
                
                // 检查是否是弓兵
                if (militaryKind.ToString().Contains("弓兵") || 
                    militaryKind.ToString().Contains("弩") ||
                    militaryKind.ToString().Contains("射"))
                {
                    return UnitType.弓兵;
                }
                
                // 检查是否是攻城器械
                if (militaryKind.ToString().Contains("器械") || 
                    militaryKind.ToString().Contains("投石") ||
                    militaryKind.ToString().Contains("冲车"))
                {
                    return UnitType.攻城器械;
                }
                
                // 默认为步兵
                return UnitType.步兵;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UnitMovement] 确定单位类型时出错: {ex.Message}");
                return UnitType.步兵; // 出错时默认为步兵
            }
        }
        
        // ==========================================
        // 4. 主要接口
        // ==========================================
        
        /// <summary>
        /// 开始移动到目标位置
        /// </summary>
        /// <param name="destination">目标位置</param>
        /// <returns>是否成功开始移动</returns>
        public bool MoveTo(Vector2 destination)
        {
            try
            {
                _finalDestination = destination;
                Point start = new Point(_owner.Position.X, _owner.Position.Y);
                Point end = new Point((int)destination.X, (int)destination.Y);
                
                // 检查是否可以重用之前的路径
                if (CanReusePath(destination))
                {
                    System.Diagnostics.Debug.WriteLine($"[UnitMovement] 重用路径: {_owner.ID}");
                    State = MovementState.Moving;
                    
                    // ✅ 安全记录事件：重用路径
                    LogMovementEvent("重用路径", start, end, 0);
                    return true;
                }
                
                // 请求新的分层路径
                var path = PathfindingManager.Instance?.FindPath(start, end);
                if (path == null || path.Count == 0)
                {
                    State = MovementState.Blocked;
                    
                    // ✅ 安全记录事件：路径规划失败
                    LogMovementEvent("路径规划失败", start, end, -1);
                    return false;
                }
                
                // 将路径分解为宏观路径和当前详细路径
                DecomposePath(path);
                
                State = MovementState.Moving;
                _lastDestination = destination;
                _lastPath = new List<Point>(path);
                
                // ✅ 安全记录事件：开始移动
                LogMovementEvent("开始移动", start, end, path.Count);
                
                System.Diagnostics.Debug.WriteLine($"[UnitMovement] 开始移动: {_owner.ID}, 路径长度: {path.Count}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UnitMovement] 移动失败: {ex.Message}");
                State = MovementState.Blocked;
                return false;
            }
        }
        
        /// <summary>
        /// ✅ 安全的移动事件记录方法
        /// 正确：创建池对象 → 填充数据 → UI复制数据 → 返回池对象
        /// </summary>
        private void LogMovementEvent(string eventType, Point fromPos, Point toPos, int cost)
        {
            // 1. 从池中获取事件对象
            var eventData = MovementEventPool.Get();
            
            try
            {
                // 2. 填充事件数据
                eventData.UnitId = _owner.ID;
                eventData.UnitType = UnitType.ToString();
                eventData.FromPosition = fromPos;
                eventData.ToPosition = toPos;
                eventData.MovementCost = cost;
                eventData.InEnemyZOC = IsInEnemyZoneOfControl();
                eventData.TerrainType = GetTerrainName(toPos);
                eventData.EventType = eventType;
                eventData.EventTime = DateTime.Now;
                eventData.FactionName = UnitFaction?.Name ?? "无势力";
                
                // 3. ✅ 正确：UI 系统复制数据，不持有池对象引用
                MovementLogSystem.Instance?.LogMovementEvent(eventData);
            }
            finally
            {
                // 4. 重要：确保对象最终返回池子
                MovementEventPool.Return(eventData);
            }
        }
        
        /// <summary>
        /// 获取地形名称
        /// </summary>
        private string GetTerrainName(Point position)
        {
            if (Session.Current?.Scenario == null) return "未知";
            
            try
            {
                var terrain = Session.Current.Scenario.GetTerrainKindByPosition(position);
                return terrain.ToString();
            }
            catch
            {
                return "未知";
            }
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        public void Stop()
        {
            Point currentPos = new Point(_owner.Position.X, _owner.Position.Y);
            
            State = MovementState.Idle;
            _chunkPath.Clear();
            _currentDetailPath.Clear();
            _pathIndex = 0;
            _consecutiveBlockCount = 0;
            
            // ✅ 安全记录事件：停止移动
            LogMovementEvent("停止移动", currentPos, currentPos, 0);
            
            System.Diagnostics.Debug.WriteLine($"[UnitMovement] 停止移动: {_owner.ID}");
        }
        
        /// <summary>
        /// 🚀 优化的更新系统 - 分离视觉和逻辑更新频率
        /// 1. 渲染相关（平滑移动插值、动画、特效） -> 每帧都跑，保证画面丝滑
        /// 2. 核心逻辑（数值、AI、寻路请求） -> 降频运行，节省 CPU
        /// </summary>
        /// <param name="gameTime">游戏时间</param>
        public void Update(GameTime gameTime)
        {
            double deltaTime = gameTime.ElapsedGameTime.TotalSeconds;
            
            // 1. 渲染相关（平滑移动插值、动画、特效） -> 每帧都跑，保证画面丝滑
            UpdateVisuals(deltaTime);
            
            // 2. 核心逻辑（数值、AI、寻路请求） -> 降频运行
            _logicTimer += deltaTime;
            if (_logicTimer >= LOGIC_INTERVAL)
            {
                UpdateGameplayLogic();
                _logicTimer = 0;
            }
        }
        
        /// <summary>
        /// 🎨 视觉更新 - 每帧运行，保证画面流畅
        /// 包括：位置插值、动画播放、特效更新、UI 状态同步
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        private void UpdateVisuals(double deltaTime)
        {
            if (State == MovementState.Idle)
                return;
            
            float dt = (float)deltaTime;
            
            // 平滑移动插值 - 让视觉位置平滑地跟随逻辑位置
            if (_logicPosition != _visualPosition)
            {
                Vector2 direction = _logicPosition - _visualPosition;
                float distance = direction.Length();
                
                if (distance > 0.01f) // 避免抖动
                {
                    direction.Normalize();
                    float moveDistance = _interpolationSpeed * dt;
                    
                    if (moveDistance >= distance)
                    {
                        // 直接到达目标位置
                        _visualPosition = _logicPosition;
                    }
                    else
                    {
                        // 平滑移动
                        _visualPosition += direction * moveDistance;
                    }
                }
            }
            
            // 更新单位的视觉位置（用于渲染）
            // 注意：这里不直接修改 _owner.Position，因为那是逻辑位置
            // 渲染系统应该使用 GetVisualPosition() 来获取平滑的视觉位置
            
            // 可以在这里添加其他视觉效果：
            // - 移动动画播放
            // - 粒子特效更新
            // - 音效位置更新
            // - UI 状态指示器更新
        }
        
        /// <summary>
        /// 🧠 游戏逻辑更新 - 降频运行（每秒 10 次），节省 CPU
        /// 包括：路径计算、障碍检测、AI 决策、状态转换
        /// </summary>
        private void UpdateGameplayLogic()
        {
            if (State != MovementState.Moving)
                return;
            
            // 更新计时器
            _recalculationCooldown -= (float)LOGIC_INTERVAL;
            _obstacleCacheTime += (float)LOGIC_INTERVAL;
            
            // 清理过期的障碍缓存
            if (_obstacleCacheTime >= OBSTACLE_CACHE_DURATION)
            {
                _obstacleCache.Clear();
                _obstacleCacheTime = 0f;
            }
            
            try
            {
                // 1. 检查前方是否有动态障碍 (比如临时修的箭塔)
                if (IsNextStepBlocked())
                {
                    // 触发方案：局部重计算
                    // 只计算：当前位置 -> _currentTarget 的短路径
                    // 距离很短，A* 消耗极低
                    OnPathBlocked();
                }
                else
                {
                    // 正常移动 (可以使用简单的 Steering 来避让友军，不需寻路)
                    MoveTowardsLogic(_currentTarget);
                }
                
                // 2. 检查是否到达当前目标
                CheckTargetReached();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UnitMovement] 逻辑更新错误: {ex.Message}");
                State = MovementState.Blocked;
            }
        }
        
        /// <summary>
        /// 获取当前的视觉位置 - 用于渲染系统
        /// </summary>
        /// <returns>平滑插值后的视觉位置</returns>
        public Vector2 GetVisualPosition()
        {
            return _visualPosition;
        }
        
        /// <summary>
        /// 获取当前的逻辑位置 - 用于游戏逻辑
        /// </summary>
        /// <returns>实际的逻辑位置</returns>
        public Vector2 GetLogicPosition()
        {
            return _logicPosition;
        }
        
        /// <summary>
        /// 设置插值速度
        /// </summary>
        /// <param name="speed">插值速度（越大越快）</param>
        public void SetInterpolationSpeed(float speed)
        {
            _interpolationSpeed = Math.Max(0.1f, speed);
        }
        
        // ==========================================
        // 5. 路径管理
        // ==========================================
        
        /// <summary>
        /// 将完整路径分解为宏观路径和当前详细路径
        /// </summary>
        private void DecomposePath(List<Point> fullPath)
        {
            _chunkPath.Clear();
            _currentDetailPath.Clear();
            
            if (fullPath == null || fullPath.Count == 0)
                return;
            
            // 简化实现：将路径按固定间隔分段
            const int CHUNK_SIZE = 10;
            
            for (int i = 0; i < fullPath.Count; i += CHUNK_SIZE)
            {
                _chunkPath.Enqueue(fullPath[i]);
            }
            
            // 确保终点在队列中
            if (fullPath.Count > 0)
            {
                Point lastPoint = fullPath[fullPath.Count - 1];
                if (_chunkPath.Count == 0 || !_chunkPath.ToArray()[_chunkPath.Count - 1].Equals(lastPoint))
                {
                    _chunkPath.Enqueue(lastPoint);
                }
            }
            
            // 设置当前详细路径为前往第一个区块
            SetNextChunkTarget();
        }
        
        /// <summary>
        /// 设置下一个区块目标
        /// </summary>
        private void SetNextChunkTarget()
        {
            if (_chunkPath.Count > 0)
            {
                _currentHighLevelTarget = _chunkPath.Dequeue();
                _currentTarget = new Vector2(_currentHighLevelTarget.X, _currentHighLevelTarget.Y);
                
                // 计算到当前目标的详细路径
                RecalculateLocalPath();
            }
            else
            {
                // 所有区块都已完成，到达最终目标
                State = MovementState.Arrived;
            }
        }
        
        /// <summary>
        /// 检查是否可以重用之前的路径
        /// </summary>
        private bool CanReusePath(Vector2 destination)
        {
            if (_lastPath == null || _lastPath.Count == 0)
                return false;
            
            // 如果目标位置变化不大，且路径仍然有效
            float distanceChange = Vector2.Distance(destination, _lastDestination);
            if (distanceChange < 5.0f && _currentDetailPath.Count > _pathIndex)
            {
                // 检查当前路径是否仍然畅通
                Point currentPos = new Point(_owner.Position.X, _owner.Position.Y);
                Point targetPos = new Point((int)destination.X, (int)destination.Y);
                
                // 简单检查：如果起点和终点都没有被阻挡，认为可以重用
                return !IsBlocked(currentPos) && !IsBlocked(targetPos);
            }
            
            return false;
        }
        
        // ==========================================
        // 6. 动态障碍检测和路径重计算
        // ==========================================
        
        /// <summary>
        /// 检查下一步是否被阻挡
        /// </summary>
        private bool IsNextStepBlocked()
        {
            if (_currentDetailPath.Count == 0 || _pathIndex >= _currentDetailPath.Count - 1)
                return false;
            
            Point nextStep = _currentDetailPath[_pathIndex + 1];
            return IsBlocked(nextStep); // 这是一个 O(1) 的查表
        }
        
        /// <summary>
        /// 高效的障碍检测 - 使用 SLG 移动消耗系统
        /// </summary>
        private bool IsBlocked(Point position)
        {
            // 先检查缓存
            if (_obstacleCache.TryGetValue(position, out bool cachedResult))
            {
                return cachedResult;
            }
            
            // 使用新的 SLG 移动消耗系统
            Point currentPos = new Point(_owner.Position.X, _owner.Position.Y);
            int movementCost = World.GetMovementCost(currentPos, position, UnitType, UnitFaction);
            
            // -1 表示不可通行，其他值表示可以通行（只是消耗不同）
            bool isBlocked = (movementCost == -1);
            
            // 缓存结果
            _obstacleCache[position] = isBlocked;
            
            return isBlocked;
        }
        
        /// <summary>
        /// 获取移动到指定位置的消耗 - SLG 核心功能
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <returns>移动消耗，-1表示不可通行</returns>
        public int GetMovementCost(Point position)
        {
            Point currentPos = new Point(_owner.Position.X, _owner.Position.Y);
            return World.GetMovementCost(currentPos, position, UnitType, UnitFaction);
        }
        
        /// <summary>
        /// 检查是否在敌军控制区内
        /// </summary>
        /// <returns>true表示在敌军控制区内</returns>
        public bool IsInEnemyZoneOfControl()
        {
            Point currentPos = new Point(_owner.Position.X, _owner.Position.Y);
            return World.HasEnemyZoneOfControl(currentPos, UnitFaction);
        }
        
        /// <summary>
        /// 路径被阻挡时的处理
        /// </summary>
        private void OnPathBlocked()
        {
            _consecutiveBlockCount++;
            
            // 冷却时间内不重复计算
            if (_recalculationCooldown > 0)
                return;
            
            _recalculationCooldown = RECALCULATION_INTERVAL;
            
            if (_consecutiveBlockCount <= MAX_CONSECUTIVE_BLOCKS)
            {
                // 策略 A: 简单的局部重算 (Local Repath)
                // 尝试重新寻路到当前的短期目标 (比如下一个大格子的中心)
                // 而不是重新计算到地图终点的几千格长的路径
                RecalculateLocalPath();
            }
            else
            {
                // 策略 B: 严重阻塞 (Global Repath)
                // 说明这个大格子彻底堵死了，或者去下一个大区的路口堵死了
                // 这时才需要请求 HighLevelFinder 重新规划宏观路线
                RequestNewHighLevelPath();
            }
        }
        
        /// <summary>
        /// 局部路径重计算 - 只计算到当前目标的短路径
        /// </summary>
        private void RecalculateLocalPath()
        {
            try
            {
                Point currentPos = new Point(_owner.Position.X, _owner.Position.Y);
                Point targetPos = new Point((int)_currentTarget.X, (int)_currentTarget.Y);
                
                var newLocalPath = PathfindingManager.Instance?.FindPath(currentPos, targetPos);
                if (newLocalPath != null && newLocalPath.Count > 0)
                {
                    // 找到了绕行的路，替换当前路径
                    _currentDetailPath = newLocalPath;
                    _pathIndex = 0;
                    _consecutiveBlockCount = 0; // 重置阻塞计数
                    
                    // ✅ 安全记录事件：局部重算成功
                    LogMovementEvent("局部重算成功", currentPos, targetPos, newLocalPath.Count);
                    
                    System.Diagnostics.Debug.WriteLine($"[UnitMovement] 局部重算成功: {_owner.ID}, 新路径长度: {newLocalPath.Count}");
                }
                else
                {
                    // 局部重算失败，可能需要全局重算
                    // ✅ 安全记录事件：局部重算失败
                    LogMovementEvent("局部重算失败", currentPos, targetPos, -1);
                    
                    System.Diagnostics.Debug.WriteLine($"[UnitMovement] 局部重算失败: {_owner.ID}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UnitMovement] 局部重算错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 全局路径重计算 - 重新规划整个路线
        /// </summary>
        private void RequestNewHighLevelPath()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UnitMovement] 请求全局重算: {_owner.ID}");
                
                // 重新计算到最终目标的完整路径
                bool success = MoveTo(_finalDestination);
                if (success)
                {
                    _consecutiveBlockCount = 0; // 重置阻塞计数
                }
                else
                {
                    State = MovementState.Blocked;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UnitMovement] 全局重算错误: {ex.Message}");
                State = MovementState.Blocked;
            }
        }
        
        // ==========================================
        // 7. 移动执行
        // ==========================================
        
        /// <summary>
        /// 逻辑移动 - 更新逻辑位置（降频运行）
        /// </summary>
        private void MoveTowardsLogic(Vector2 target)
        {
            Vector2 currentPos = new Vector2(_owner.Position.X, _owner.Position.Y);
            _logicPosition = currentPos; // 同步逻辑位置
            
            Vector2 direction = target - currentPos;
            
            if (direction.Length() < 0.1f)
            {
                // 已经很接近目标
                return;
            }
            
            direction.Normalize();
            // 使用逻辑更新间隔计算移动距离
            Vector2 movement = direction * MovementSpeed * (float)LOGIC_INTERVAL;
            
            // 检查移动是否会撞到障碍物
            Vector2 newPos = currentPos + movement;
            Point newPosInt = new Point((int)newPos.X, (int)newPos.Y);
            
            if (!IsBlocked(newPosInt))
            {
                // 更新单位的逻辑位置
                _owner.Position = new Point((int)newPos.X, (int)newPos.Y);
                _logicPosition = newPos;
                
                // 如果这是第一次移动，初始化视觉位置
                if (_visualPosition == Vector2.Zero)
                {
                    _visualPosition = newPos;
                }
            }
            else
            {
                // 移动被阻挡，触发重计算
                OnPathBlocked();
            }
        }
        
        /// <summary>
        /// 兼容性方法 - 保持原有接口
        /// </summary>
        private void MoveTowards(Vector2 target, float deltaTime)
        {
            // 这个方法现在只是为了兼容性，实际逻辑在 MoveTowardsLogic 中
            // 可以在这里添加一些每帧的视觉效果处理
        }
        
        /// <summary>
        /// 检查是否到达当前目标
        /// </summary>
        private void CheckTargetReached()
        {
            Vector2 currentPos = new Vector2(_owner.Position.X, _owner.Position.Y);
            float distanceToTarget = Vector2.Distance(currentPos, _currentTarget);
            
            if (distanceToTarget < 1.0f) // 到达阈值
            {
                Point currentPosInt = new Point(_owner.Position.X, _owner.Position.Y);
                Point targetPosInt = new Point((int)_currentTarget.X, (int)_currentTarget.Y);
                
                // 到达当前目标，设置下一个目标
                if (_chunkPath.Count > 0)
                {
                    // ✅ 安全记录事件：到达中间目标
                    LogMovementEvent("到达中间目标", currentPosInt, targetPosInt, 0);
                    SetNextChunkTarget();
                }
                else
                {
                    // 到达最终目标
                    State = MovementState.Arrived;
                    
                    // ✅ 安全记录事件：到达最终目标
                    LogMovementEvent("到达最终目标", currentPosInt, targetPosInt, 0);
                    
                    System.Diagnostics.Debug.WriteLine($"[UnitMovement] 到达目标: {_owner.ID}");
                }
            }
        }
        
        // ==========================================
        // 8. 调试和监控
        // ==========================================
        
        /// <summary>
        /// 获取当前移动状态信息 - 包含 SLG 战术信息
        /// ✅ 安全模式：返回数据副本，不暴露内部对象引用
        /// </summary>
        public string GetStatusInfo()
        {
            Point currentPos = new Point(_owner.Position.X, _owner.Position.Y);
            bool inZOC = IsInEnemyZoneOfControl();
            string zocStatus = inZOC ? "敌军控制区" : "安全区域";
            
            return $"单位{_owner.ID}({UnitType}): 状态={State}, 目标={_currentTarget}, 路径长度={_currentDetailPath.Count}, 索引={_pathIndex}, 阻塞次数={_consecutiveBlockCount}, 位置={currentPos}, 战术状态={zocStatus}";
        }
        
        /// <summary>
        /// 获取 UI 安全的移动状态数据 - 值类型副本
        /// ✅ 正确：UI 只存储这个结构体，不持有 UnitMovement 对象引用
        /// </summary>
        public MovementStatusData GetSafeStatusData()
        {
            Point currentPos = new Point(_owner.Position.X, _owner.Position.Y);
            bool inZOC = IsInEnemyZoneOfControl();
            
            return new MovementStatusData
            {
                UnitId = _owner.ID,
                UnitType = UnitType.ToString(),
                State = State.ToString(),
                CurrentPosition = currentPos,
                TargetPosition = new Point((int)_currentTarget.X, (int)_currentTarget.Y),
                PathLength = _currentDetailPath.Count,
                PathIndex = _pathIndex,
                BlockCount = _consecutiveBlockCount,
                InEnemyZOC = inZOC,
                MovementSpeed = MovementSpeed,
                FactionName = UnitFaction?.Name ?? "无势力",
                Timestamp = DateTime.Now
            };
        }
        
        /// <summary>
        /// 获取详细的战术状态信息
        /// </summary>
        public string GetTacticalInfo()
        {
            Point currentPos = new Point(_owner.Position.X, _owner.Position.Y);
            bool inZOC = IsInEnemyZoneOfControl();
            int currentTerrainCost = World.GetMovementCost(currentPos, currentPos, UnitType, UnitFaction);
            
            return $"单位{_owner.ID} 战术信息:\n" +
                   $"  兵种: {UnitType}\n" +
                   $"  势力: {UnitFaction?.Name ?? "无"}\n" +
                   $"  当前位置: {currentPos}\n" +
                   $"  地形消耗: {currentTerrainCost}\n" +
                   $"  敌军控制区: {(inZOC ? "是" : "否")}\n" +
                   $"  移动状态: {State}";
        }
        
        /// <summary>
        /// 获取当前路径（用于调试显示）
        /// </summary>
        public List<Point> GetCurrentPath()
        {
            return new List<Point>(_currentDetailPath);
        }
        
        /// <summary>
        /// 获取剩余路径长度
        /// </summary>
        public int GetRemainingPathLength()
        {
            return Math.Max(0, _currentDetailPath.Count - _pathIndex) + _chunkPath.Count * 10; // 估算
        }
    }
    
    // ==========================================
    // 9. 辅助类和接口
    // ==========================================
    
    /// <summary>
    /// 游戏世界地图信息提供者 - IMapInfoProvider 的具体实现
    /// 🎯 适配器模式：将复杂的游戏世界适配为简洁的寻路接口
    /// </summary>
    public class GameWorldMapProvider : IMapInfoProvider
    {
        private static GameWorldMapProvider _instance;
        public static GameWorldMapProvider Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameWorldMapProvider();
                return _instance;
            }
        }
        
        /// <summary>
        /// 获取简化的地形类型 - 将复杂的游戏地形映射为寻路需要的基础类型
        /// </summary>
        public TerrainType GetTerrain(int x, int y)
        {
            if (Session.Current?.Scenario == null)
                return TerrainType.Plain;
            
            try
            {
                var gameTerrainKind = Session.Current.Scenario.GetTerrainKindByPosition(new Point(x, y));
                
                // 将游戏的复杂地形类型映射为寻路系统需要的简化类型
                switch (gameTerrainKind)
                {
                    case GameGlobal.TerrainKind.平原:
                        return TerrainType.Plain;
                    case GameGlobal.TerrainKind.草原:
                        return TerrainType.Plain;
                    case GameGlobal.TerrainKind.森林:
                        return TerrainType.Forest;
                    case GameGlobal.TerrainKind.山地:
                        return TerrainType.Mountain;
                    case GameGlobal.TerrainKind.峻岭:
                        return TerrainType.Mountain;
                    case GameGlobal.TerrainKind.水域:
                        return TerrainType.River;
                    case GameGlobal.TerrainKind.湿地:
                        return TerrainType.Forest; // 湿地按森林处理
                    case GameGlobal.TerrainKind.荒地:
                        return TerrainType.Plain;  // 荒地按平原处理
                    case GameGlobal.TerrainKind.沙漠:
                        return TerrainType.Plain;  // 沙漠按平原处理
                    case GameGlobal.TerrainKind.栈道:
                        return TerrainType.Wall;   // 栈道按城墙处理
                    default:
                        return TerrainType.Plain;
                }
            }
            catch
            {
                return TerrainType.Plain;
            }
        }
        
        /// <summary>
        /// 检查是否有单位占据该位置
        /// </summary>
        public bool IsUnitAt(int x, int y)
        {
            if (Session.Current?.Scenario?.Troops == null)
                return false;
            
            Point position = new Point(x, y);
            
            foreach (Troop troop in Session.Current.Scenario.Troops)
            {
                if (troop.Position.Equals(position) && !troop.Destroyed)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查是否在敌方控制区 (ZOC)
        /// </summary>
        public bool IsInEnemyZOC(int x, int y, int factionId)
        {
            if (Session.Current?.Scenario?.Troops == null)
                return false;
            
            Point position = new Point(x, y);
            
            // 检查周围8格是否有敌军
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // 跳过自己
                    
                    Point checkPos = new Point(x + dx, y + dy);
                    
                    foreach (Troop troop in Session.Current.Scenario.Troops)
                    {
                        if (troop.Position.Equals(checkPos) && 
                            troop.BelongedFaction?.ID != factionId &&
                            !troop.Destroyed)
                        {
                            return true; // 发现敌军，在控制区内
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取地图边界
        /// </summary>
        public (int width, int height) GetMapBounds()
        {
            if (Session.Current?.Scenario != null)
            {
                // 假设地图有固定大小，实际应该从 Scenario 获取
                return (1000, 1000); // 需要根据实际游戏调整
            }
            
            return (100, 100); // 默认大小
        }
        
        /// <summary>
        /// 检查坐标是否在地图范围内
        /// </summary>
        public bool IsInBounds(int x, int y)
        {
            var (width, height) = GetMapBounds();
            return x >= 0 && y >= 0 && x < width && y < height;
        }
    }
    
    /// <summary>
    /// 世界系统接口 - SLG 游戏的核心地图和战术系统
    /// 🔄 重构说明：逐步迁移到使用 IMapInfoProvider 接口
    /// </summary>
    public static class World
    {
        /// <summary>
        /// 地图信息提供者 - 使用依赖注入模式
        /// </summary>
        public static IMapInfoProvider MapProvider { get; set; }
        
        /// <summary>
        /// 静态构造函数 - 初始化默认的地图提供者
        /// </summary>
        static World()
        {
            MapProvider = GameWorldMapProvider.Instance;
        }
        /// <summary>
        /// 检查指定位置是否被阻挡 - 应该是 O(1) 的查表操作
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>true表示被阻挡</returns>
        public static bool IsBlocked(Point position)
        {
            // 简化实现：检查是否超出边界
            if (position.X < 0 || position.Y < 0 || 
                position.X >= 1000 || position.Y >= 1000)
            {
                return true;
            }
            
            // 检查地形障碍
            if (Session.Current?.Scenario != null)
            {
                var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(position);
                return terrainKind == GameGlobal.TerrainKind.山地; // 山地视为障碍
            }
            
            return false;
        }
        
        /// <summary>
        /// SLG 核心：基于兵种和地形计算移动消耗
        /// 调整前：只看是否阻挡 if (grid[x, y].IsBlocked) continue;
        /// 调整后：基于兵种和地形计算消耗 (SLG核心逻辑)
        /// </summary>
        /// <param name="from">起始位置</param>
        /// <param name="to">目标位置</param>
        /// <param name="unitType">单位类型</param>
        /// <param name="unitFaction">单位势力</param>
        /// <returns>移动消耗，-1表示不可通行</returns>
        public static int GetMovementCost(Point from, Point to, UnitType unitType, Faction unitFaction)
        {
            // 1. 基础地形消耗 (平原=1, 森林=2, 山地=5)
            int terrainCost = MapData.GetTerrainCost(to, unitType);
            
            // 2. 无法通行的情况 (比如骑兵不能上城墙，或者没有船不能下水)
            if (terrainCost == int.MaxValue) return -1; // -1 代表不通
            
            // 3. ZOC (控制区) 判定 - 这是 SLG 的灵魂
            // 如果目标格子周围有敌军，消耗剧增 (防止直接绕过防线)
            if (HasEnemyZoneOfControl(to, unitFaction))
            {
                terrainCost += 5; // 惩罚值
            }
            
            return terrainCost;
        }
        
        /// <summary>
        /// 检查指定位置是否在敌军控制区内 (Zone of Control - ZOC)
        /// </summary>
        /// <param name="position">检查位置</param>
        /// <param name="unitFaction">单位势力</param>
        /// <returns>true表示在敌军控制区内</returns>
        public static bool HasEnemyZoneOfControl(Point position, Faction unitFaction)
        {
            if (Session.Current?.Scenario?.Troops == null || unitFaction == null)
                return false;
            
            // 检查周围8格是否有敌军
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // 跳过自己
                    
                    Point checkPos = new Point(position.X + dx, position.Y + dy);
                    
                    foreach (Troop troop in Session.Current.Scenario.Troops)
                    {
                        if (troop.Position.Equals(checkPos) && 
                            troop.BelongedFaction != unitFaction &&
                            !troop.Destroyed)
                        {
                            return true; // 发现敌军，在控制区内
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查指定位置是否有其他单位 - 用于避让
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="excludeUnit">排除的单位（自己）</param>
        /// <returns>true表示有其他单位</returns>
        public static bool HasOtherUnit(Point position, Troop excludeUnit)
        {
            if (Session.Current?.Scenario?.Troops == null)
                return false;
            
            foreach (Troop troop in Session.Current.Scenario.Troops)
            {
                if (troop != excludeUnit && troop.Position.Equals(position))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// 单位类型枚举 - 用于地形适应性计算
    /// </summary>
    public enum UnitType
    {
        步兵,    // 步兵 - 通用性强
        骑兵,    // 骑兵 - 平原快，山地慢
        弓兵,    // 弓兵 - 类似步兵
        水军,    // 水军 - 只能在水上
        攻城器械  // 攻城器械 - 移动缓慢
    }
    
    /// <summary>
    /// UI 安全的移动状态数据结构 - 值类型副本
    /// ✅ 正确：UI 系统存储这个结构体，不持有对象引用
    /// </summary>
    public struct MovementStatusData
    {
        public int UnitId { get; set; }
        public string UnitType { get; set; }
        public string State { get; set; }
        public Point CurrentPosition { get; set; }
        public Point TargetPosition { get; set; }
        public int PathLength { get; set; }
        public int PathIndex { get; set; }
        public int BlockCount { get; set; }
        public bool InEnemyZOC { get; set; }
        public float MovementSpeed { get; set; }
        public string FactionName { get; set; }
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 格式化为显示文本
        /// </summary>
        public string ToDisplayText()
        {
            string zocStatus = InEnemyZOC ? "⚠️控制区" : "✅安全";
            return $"{UnitType} {UnitId}: {State} → {TargetPosition} ({PathIndex}/{PathLength}) {zocStatus}";
        }
    }
    
    /// <summary>
    /// 移动事件数据 - 池化对象
    /// ⚠️ 注意：这是池化对象，UI 不应直接存储此对象的引用
    /// </summary>
    public class MovementEventData
    {
        public int UnitId { get; set; }
        public string UnitType { get; set; }
        public Point FromPosition { get; set; }
        public Point ToPosition { get; set; }
        public int MovementCost { get; set; }
        public bool InEnemyZOC { get; set; }
        public string TerrainType { get; set; }
        public string EventType { get; set; } // "开始移动", "到达目标", "路径阻塞", "重新计算"
        public DateTime EventTime { get; set; }
        public string FactionName { get; set; }
        
        public void Reset()
        {
            UnitId = 0;
            UnitType = null;
            FromPosition = Point.Zero;
            ToPosition = Point.Zero;
            MovementCost = 0;
            InEnemyZOC = false;
            TerrainType = null;
            EventType = null;
            EventTime = DateTime.MinValue;
            FactionName = null;
        }
    }
    
    /// <summary>
    /// UI 安全的移动日志数据 - 值类型副本
    /// ✅ 正确：UI 日志系统存储这个结构体
    /// </summary>
    public struct MovementLogData
    {
        public int UnitId { get; set; }
        public string UnitType { get; set; }
        public Point FromPosition { get; set; }
        public Point ToPosition { get; set; }
        public int MovementCost { get; set; }
        public bool InEnemyZOC { get; set; }
        public string TerrainType { get; set; }
        public string EventType { get; set; }
        public DateTime EventTime { get; set; }
        public string FactionName { get; set; }
        public string FormattedText { get; set; }
        
        /// <summary>
        /// 创建格式化文本
        /// </summary>
        public static MovementLogData CreateFromEvent(MovementEventData eventData)
        {
            string zocStatus = eventData.InEnemyZOC ? "⚠️控制区" : "✅安全";
            string formattedText = $"[{eventData.EventTime:HH:mm:ss}] {eventData.UnitType} {eventData.UnitId} {eventData.EventType}: {eventData.FromPosition}→{eventData.ToPosition} (消耗:{eventData.MovementCost}, {zocStatus})";
            
            return new MovementLogData
            {
                UnitId = eventData.UnitId,
                UnitType = eventData.UnitType,
                FromPosition = eventData.FromPosition,
                ToPosition = eventData.ToPosition,
                MovementCost = eventData.MovementCost,
                InEnemyZOC = eventData.InEnemyZOC,
                TerrainType = eventData.TerrainType,
                EventType = eventData.EventType,
                EventTime = eventData.EventTime,
                FactionName = eventData.FactionName,
                FormattedText = formattedText
            };
        }
    }
    
    /// <summary>
    /// 地图数据接口 - 提供地形和移动消耗信息
    /// </summary>
    public static class MapData
    {
        /// <summary>
        /// 根据地形和单位类型计算移动消耗
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="unitType">单位类型</param>
        /// <returns>移动消耗，int.MaxValue表示不可通行</returns>
        public static int GetTerrainCost(Point position, UnitType unitType)
        {
            if (Session.Current?.Scenario == null)
                return 1; // 默认消耗
            
            var terrainKind = Session.Current.Scenario.GetTerrainKindByPosition(position);
            
            // 根据地形和兵种计算消耗
            switch (terrainKind)
            {
                case GameGlobal.TerrainKind.平原:
                    switch (unitType)
                    {
                        case UnitType.骑兵: return 1;      // 骑兵在平原最快
                        case UnitType.步兵: return 1;      // 步兵标准
                        case UnitType.弓兵: return 1;      // 弓兵标准
                        case UnitType.攻城器械: return 3;   // 攻城器械慢
                        case UnitType.水军: return int.MaxValue; // 水军不能上陆
                    }
                    break;
                    
                case GameGlobal.TerrainKind.森林:
                    switch (unitType)
                    {
                        case UnitType.骑兵: return 3;      // 骑兵在森林受限
                        case UnitType.步兵: return 2;      // 步兵适中
                        case UnitType.弓兵: return 2;      // 弓兵适中
                        case UnitType.攻城器械: return 5;   // 攻城器械很慢
                        case UnitType.水军: return int.MaxValue; // 水军不能上陆
                    }
                    break;
                    
                case GameGlobal.TerrainKind.山地:
                    switch (unitType)
                    {
                        case UnitType.骑兵: return 5;      // 骑兵在山地很慢
                        case UnitType.步兵: return 3;      // 步兵较慢
                        case UnitType.弓兵: return 3;      // 弓兵较慢
                        case UnitType.攻城器械: return int.MaxValue; // 攻城器械不能上山
                        case UnitType.水军: return int.MaxValue; // 水军不能上陆
                    }
                    break;
                    
                case GameGlobal.TerrainKind.水域:
                    switch (unitType)
                    {
                        case UnitType.水军: return 1;      // 水军在水上正常
                        default: return int.MaxValue;      // 其他兵种不能下水
                    }
                    
                case GameGlobal.TerrainKind.栈道:
                    switch (unitType)
                    {
                        case UnitType.步兵: return 2;      // 步兵可以走栈道
                        case UnitType.弓兵: return 2;      // 弓兵可以走栈道
                        case UnitType.骑兵: return 4;      // 骑兵在栈道较慢
                        default: return int.MaxValue;      // 攻城器械不能走栈道
                    }
                    
                case GameGlobal.TerrainKind.峻岭:
                    switch (unitType)
                    {
                        case UnitType.步兵: return 4;      // 步兵在峻岭很慢
                        case UnitType.弓兵: return 4;      // 弓兵在峻岭很慢
                        default: return int.MaxValue;      // 其他兵种不能通过峻岭
                    }
                    
                case GameGlobal.TerrainKind.湿地:
                    switch (unitType)
                    {
                        case UnitType.步兵: return 3;      // 步兵在湿地较慢
                        case UnitType.弓兵: return 3;      // 弓兵在湿地较慢
                        case UnitType.骑兵: return 4;      // 骑兵在湿地很慢
                        case UnitType.攻城器械: return int.MaxValue; // 攻城器械不能过湿地
                        case UnitType.水军: return int.MaxValue; // 水军不能上陆
                    }
                    break;
                    
                case GameGlobal.TerrainKind.草原:
                    switch (unitType)
                    {
                        case UnitType.骑兵: return 1;      // 骑兵在草原最快
                        case UnitType.步兵: return 1;      // 步兵标准
                        case UnitType.弓兵: return 1;      // 弓兵标准
                        case UnitType.攻城器械: return 2;   // 攻城器械较慢
                        case UnitType.水军: return int.MaxValue; // 水军不能上陆
                    }
                    break;
                    
                case GameGlobal.TerrainKind.荒地:
                    switch (unitType)
                    {
                        case UnitType.步兵: return 2;      // 步兵在荒地较慢
                        case UnitType.弓兵: return 2;      // 弓兵在荒地较慢
                        case UnitType.骑兵: return 3;      // 骑兵在荒地慢
                        case UnitType.攻城器械: return 4;   // 攻城器械很慢
                        case UnitType.水军: return int.MaxValue; // 水军不能上陆
                    }
                    break;
                    
                case GameGlobal.TerrainKind.沙漠:
                    switch (unitType)
                    {
                        case UnitType.步兵: return 3;      // 步兵在沙漠慢
                        case UnitType.弓兵: return 3;      // 弓兵在沙漠慢
                        case UnitType.骑兵: return 2;      // 骑兵在沙漠适中
                        case UnitType.攻城器械: return int.MaxValue; // 攻城器械不能过沙漠
                        case UnitType.水军: return int.MaxValue; // 水军不能上陆
                    }
                    break;
                    
                default:
                    return 1; // 默认消耗
            }
            
            return 1; // 默认消耗
        }
        
        /// <summary>
        /// 检查地形是否适合指定兵种
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="unitType">单位类型</param>
        /// <returns>true表示适合</returns>
        public static bool IsTerrainSuitable(Point position, UnitType unitType)
        {
            return GetTerrainCost(position, unitType) != int.MaxValue;
        }
    }
    
    /// <summary>
    /// 移动事件池 - 安全的对象池管理
    /// </summary>
    public static class MovementEventPool
    {
        private static readonly Stack<MovementEventData> _pool = new Stack<MovementEventData>();
        private static int _totalCreated = 0;
        private static int _totalReturned = 0;
        
        public static MovementEventData Get()
        {
            if (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                obj.Reset();
                return obj;
            }
            
            _totalCreated++;
            return new MovementEventData();
        }
        
        public static void Return(MovementEventData eventData)
        {
            if (eventData != null)
            {
                eventData.Reset();
                _pool.Push(eventData);
                _totalReturned++;
            }
        }
        
        public static string GetStats()
        {
            return $"移动事件池: 可用={_pool.Count}, 已创建={_totalCreated}, 已回收={_totalReturned}";
        }
    }
    
    /// <summary>
    /// 移动日志系统 - UI 安全的日志管理
    /// </summary>
    public class MovementLogSystem
    {
        private readonly List<MovementLogData> _logs = new List<MovementLogData>();
        private const int MAX_LOG_ENTRIES = 500;
        
        public static MovementLogSystem Instance { get; private set; }
        
        static MovementLogSystem()
        {
            Instance = new MovementLogSystem();
        }
        
        /// <summary>
        /// ✅ 正确：添加移动事件日志（安全模式）
        /// </summary>
        public void LogMovementEvent(MovementEventData eventData)
        {
            // 创建 UI 安全的数据副本
            var logEntry = MovementLogData.CreateFromEvent(eventData);
            
            _logs.Add(logEntry);
            
            // 清理旧日志
            if (_logs.Count > MAX_LOG_ENTRIES)
            {
                _logs.RemoveAt(0);
            }
            
            // 输出调试信息
            System.Diagnostics.Debug.WriteLine($"[MovementLog] {logEntry.FormattedText}");
        }
        
        /// <summary>
        /// 获取最近的移动日志
        /// </summary>
        public List<MovementLogData> GetRecentLogs(int count = 20)
        {
            int startIndex = Math.Max(0, _logs.Count - count);
            return _logs.GetRange(startIndex, _logs.Count - startIndex);
        }
        
        /// <summary>
        /// 获取指定单位的移动日志
        /// </summary>
        public List<MovementLogData> GetUnitLogs(int unitId, int count = 10)
        {
            var unitLogs = _logs.Where(log => log.UnitId == unitId).ToList();
            int startIndex = Math.Max(0, unitLogs.Count - count);
            return unitLogs.GetRange(startIndex, unitLogs.Count - startIndex);
        }
        
        /// <summary>
        /// 清理日志
        /// </summary>
        public void ClearLogs()
        {
            _logs.Clear();
        }
        
        /// <summary>
        /// 获取日志统计
        /// </summary>
        public string GetLogStats()
        {
            var eventTypes = _logs.GroupBy(log => log.EventType)
                                 .ToDictionary(g => g.Key, g => g.Count());
            
            string stats = $"移动日志统计: 总数={_logs.Count}";
            foreach (var kvp in eventTypes)
            {
                stats += $", {kvp.Key}={kvp.Value}";
            }
            
            return stats;
        }
    }
    
    /// <summary>
    /// 移动管理器 - 统一管理所有单位的移动
    /// </summary>
    public class MovementManager
    {
        private Dictionary<int, UnitMovement> _unitMovements = new Dictionary<int, UnitMovement>();
        
        public static MovementManager Instance { get; private set; }
        
        public MovementManager()
        {
            Instance = this;
        }
        
        /// <summary>
        /// 为单位创建移动组件
        /// </summary>
        public UnitMovement CreateMovement(Troop troop)
        {
            if (_unitMovements.ContainsKey(troop.ID))
            {
                return _unitMovements[troop.ID];
            }
            
            var movement = new UnitMovement(troop);
            _unitMovements[troop.ID] = movement;
            return movement;
        }
        
        /// <summary>
        /// 获取单位的移动组件
        /// </summary>
        public UnitMovement GetMovement(int troopId)
        {
            _unitMovements.TryGetValue(troopId, out var movement);
            return movement;
        }
        
        /// <summary>
        /// 移除单位的移动组件
        /// </summary>
        public void RemoveMovement(int troopId)
        {
            _unitMovements.Remove(troopId);
        }
        
        /// <summary>
        /// 🚀 优化的批量更新 - 分离视觉和逻辑更新
        /// </summary>
        public void UpdateAll(GameTime gameTime)
        {
            foreach (var movement in _unitMovements.Values)
            {
                movement.Update(gameTime);
            }
        }
        
        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public string GetPerformanceStats()
        {
            int movingUnits = 0;
            int idleUnits = 0;
            int blockedUnits = 0;
            int arrivedUnits = 0;
            
            foreach (var movement in _unitMovements.Values)
            {
                switch (movement.State)
                {
                    case UnitMovement.MovementState.Moving:
                        movingUnits++;
                        break;
                    case UnitMovement.MovementState.Idle:
                        idleUnits++;
                        break;
                    case UnitMovement.MovementState.Blocked:
                        blockedUnits++;
                        break;
                    case UnitMovement.MovementState.Arrived:
                        arrivedUnits++;
                        break;
                }
            }
            
            return $"移动系统性能: 总单位={_unitMovements.Count}, 移动中={movingUnits}, 空闲={idleUnits}, 阻塞={blockedUnits}, 已到达={arrivedUnits}";
        }
        
        /// <summary>
        /// 获取移动统计信息
        /// </summary>
        public string GetStats()
        {
            int moving = 0, idle = 0, blocked = 0, arrived = 0;
            
            foreach (var movement in _unitMovements.Values)
            {
                switch (movement.State)
                {
                    case UnitMovement.MovementState.Moving:
                        moving++;
                        break;
                    case UnitMovement.MovementState.Idle:
                        idle++;
                        break;
                    case UnitMovement.MovementState.Blocked:
                        blocked++;
                        break;
                    case UnitMovement.MovementState.Arrived:
                        arrived++;
                        break;
                }
            }
            
            return $"移动单位统计: 总数={_unitMovements.Count}, 移动中={moving}, 空闲={idle}, 阻塞={blocked}, 已到达={arrived}";
        }
    }
}