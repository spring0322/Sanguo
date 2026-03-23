using GameManager;
using GameObjects.ArchitectureDetail;
using GameObjects.PersonDetail;
using GameObjects.SectionDetail;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace GameObjects
{
    public partial class Section : GameObject
    {
        // --------------------------------------------------------------------------------
        // 📦 V8.4 全链路物流系统 (集成最终修正版)
        // 包含：总控入口、军区内分配、军区间调配、核心限流算法
        // 修正：彻底移除ID判断，改为基于农业/商业属性的功能判断，防止误判
        // --------------------------------------------------------------------------------

        /// <summary>
        /// 辅助属性：获取本军区下辖的所有建筑 (List形式)
        /// </summary>
        public List<Architecture> ArchitectureList
        {
            get
            {
                if (this.Architectures == null) return new List<Architecture>();
                return this.Architectures.Cast<Architecture>().ToList();
            }
        }

        /// <summary>
        /// 【总控】：执行军区的所有资源物流逻辑
        /// </summary>
        public void RunResourceLogistics()
        {
            // 1. 先进行军区内部平衡 (让钱粮下沉到城市)
            this.DistributeResourcesToCities(this.ArchitectureList);

            // 2. 再进行军区之间的调配 (前线缺粮找后方要)
            // 只有当本军区是"贫困"或"前线"状态时，才尝试向其他军区请求
            if (this.BelongedFaction != null)
            {
                this.ExchangeResourcesBetweenSections();
            }
        }

        /// <summary>
        /// 【链路A】：军区内部下沉
        /// </summary>
        private void DistributeResourcesToCities(List<Architecture> cities)
        {
            if (cities == null || cities.Count < 2) return;

            var suppliers = new List<Architecture>();
            var consumers = new List<Architecture>();
            var caps = new Dictionary<Architecture, (int fund, int food)>();

            // 1. 建立账本
            foreach (var arch in cities)
            {
                int capFund, capFood;
                GetResourceCaps(arch, out capFund, out capFood); // 获取严格上限
                caps[arch] = (capFund, capFood);

                // 需求方：低于 60% 才请求
                if (arch.Fund < capFund * 0.6f || arch.Food < capFood * 0.6f) consumers.Add(arch);

                // 供给方：智能判断门槛
                // 修正：使用属性判断是否为城市
                bool isCity = (arch.Kind != null && (arch.Kind.HasAgriculture || arch.Kind.HasCommerce));
                float threshold = isCity ? 1.1f : 2.0f; // 城市110%溢出可供，关口200%才供

                if (arch.Fund > capFund * threshold || arch.Food > capFood * threshold) suppliers.Add(arch);
            }

            // 2. 执行分配
            ExecuteLogisticsTransfer(consumers, suppliers, caps, "军区内");
        }

        /// <summary>
        /// 【链路B】：军区之间调配
        /// 修复痛点：防止前线军区按照 1900万 的需求向后方军区请求资源
        /// </summary>
        private void ExchangeResourcesBetweenSections()
        {
            var faction = this.BelongedFaction;
            if (faction == null || faction.Sections.Count < 2) return;

            // 1. 计算本军区 (Self) 的总体状态
            long selfFund = 0, selfFood = 0;
            long selfCapFund = 0, selfCapFood = 0;

            foreach (var arch in this.ArchitectureList)
            {
                selfFund += arch.Fund;
                selfFood += arch.Food;
                GetResourceCaps(arch, out int cFund, out int cFood);
                selfCapFund += cFund;
                selfCapFood += cFood;
            }

            // 判定：本军区是否缺资源？(低于总上限的 50%)
            bool needFund = selfFund < (selfCapFund * 0.5f);
            bool needFood = selfFood < (selfCapFood * 0.5f);

            if (!needFund && !needFood) return; // 不缺就不折腾

            // 2. 寻找供给军区
            foreach (Section otherSection in faction.Sections)
            {
                if (otherSection == this) continue;
                if (otherSection.ArchitectureList.Count == 0) continue;

                // 计算对方军区的富裕程度
                long otherFund = 0, otherFood = 0;
                long otherCapFund = 0, otherCapFood = 0;

                foreach (var arch in otherSection.ArchitectureList)
                {
                    otherFund += arch.Fund;
                    otherFood += arch.Food;
                    GetResourceCaps(arch, out int cFund, out int cFood);
                    otherCapFund += cFund;
                    otherCapFood += cFood;
                }

                // 判定：对方是否富裕？(高于总上限的 120%)
                bool otherRichFund = otherFund > (otherCapFund * 1.2f);
                bool otherRichFood = otherFood > (otherCapFood * 1.2f);

                if (!otherRichFund && !otherRichFood) continue;

                // 3. 执行调配：从对方最富裕的城市 -> 我方最缺粮的城市
                var supplierCity = otherSection.ArchitectureList.OrderByDescending(a => a.Food).FirstOrDefault();
                var consumerCity = this.ArchitectureList.OrderBy(a => a.Food).FirstOrDefault();

                if (supplierCity != null && consumerCity != null)
                {
                    // 构造临时列表调用通用传输逻辑
                    var tempConsumers = new List<Architecture> { consumerCity };
                    var tempSuppliers = new List<Architecture> { supplierCity };
                    var tempCaps = new Dictionary<Architecture, (int, int)>();

                    GetResourceCaps(consumerCity, out int ccFund, out int ccFood);
                    tempCaps[consumerCity] = (ccFund, ccFood);
                    GetResourceCaps(supplierCity, out int ssFund, out int ssFood);
                    tempCaps[supplierCity] = (ssFund, ssFood);

                    // 复用核心传输逻辑
                    ExecuteLogisticsTransfer(tempConsumers, tempSuppliers, tempCaps, "跨军区");
                }
            }
        }

        /// <summary>
        /// 【核心】：通用物流传输执行器 (含三大过滤器)
        /// </summary>
        private void ExecuteLogisticsTransfer(
            List<Architecture> consumers,
            List<Architecture> suppliers,
            Dictionary<Architecture, (int fund, int food)> caps,
            string logPrefix)
        {
            foreach (var consumer in consumers)
            {
                var (cCapFund, cCapFood) = caps[consumer];

                foreach (var supplier in suppliers)
                {
                    if (consumer == supplier) continue;

                    // 计算缺口与供给
                    int needFund = cCapFund - consumer.Fund;
                    int needFood = cCapFood - consumer.Food;

                    // 获取supplier的cap (防止跨军区时字典缺失)
                    int sCapFund, sCapFood;
                    if (caps.ContainsKey(supplier))
                    {
                        (sCapFund, sCapFood) = caps[supplier];
                    }
                    else
                    {
                        GetResourceCaps(supplier, out sCapFund, out sCapFood);
                    }

                    int giveFund = supplier.Fund - sCapFund;
                    int giveFood = supplier.Food - sCapFood;

                    // 理论交易额
                    int transferFund = Math.Min(Math.Max(0, needFund), Math.Max(0, giveFund));
                    int transferFood = Math.Min(Math.Max(0, needFood), Math.Max(0, giveFood));

                    // =========================================================
                    // 🛑 核心过滤器 (The Breakers)
                    // =========================================================

                    // Filter 1: 零值拦截 (拦截 99% 的垃圾日志)
                    if (transferFund <= 0 && transferFood <= 0) continue;

                    // Filter 2: 蚂蚁搬家拦截
                    bool isEmergency = consumer.Fund <= 0 || consumer.Food <= 0;
                    if (!isEmergency)
                    {
                        if (transferFund < 2000 && transferFood < 20000) continue;
                    }

                    // Filter 3: 关隘安全阀 (基于属性判断)
                    // 如果不具备农业/商业能力，视为关隘，强制限流
                    bool isConsumerCity = (consumer.Kind != null && (consumer.Kind.HasAgriculture || consumer.Kind.HasCommerce));
                    if (!isConsumerCity)
                    {
                        if (transferFood > 100000) transferFood = 100000;
                        if (transferFund > 10000) transferFund = 10000;
                    }

                    // 执行
                    supplier.Fund -= transferFund;
                    supplier.Food -= transferFood;
                    consumer.Fund += transferFund;
                    consumer.Food += transferFood;

#if DEBUG
                    if (transferFund > 5000 || transferFood > 50000)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{logPrefix}] {consumer.Name} 从 {supplier.Name} 调入: 金={transferFund}, 粮={transferFood}");
                    }
#endif
                    if (consumer.Fund >= cCapFund && consumer.Food >= cCapFood) break;
                }
            }
        }

        /// <summary>
        /// 【核心算法】：获取严格的资源上限
        /// </summary>
        private void GetResourceCaps(Architecture arch, out int idealFund, out int idealFood)
        {
            // 修正：使用属性判断是否为城市，不再依赖 ID==1
            bool isCity = (arch.Kind != null && (arch.Kind.HasAgriculture || arch.Kind.HasCommerce));

            if (!isCity)
            {
                // 关隘/港口：严格贫困线
                idealFund = 5000;
                // 粮草：强制封顶 20万
                idealFood = Math.Min(200000, Math.Max(30000, arch.MilitaryCount * 60));
            }
            else
            {
                // 城市：宽裕线
                idealFund = 10000 + (arch.Population / 10);
                // 粮草：300万封顶
                idealFood = Math.Min(3000000, 100000 + (arch.MilitaryCount * 360));
            }
        }

        /// <summary>
        /// 军备调配逻辑：在军区内城市间平衡军事力量
        /// </summary>
        /// <param name="cities">城市列表</param>
        private void ManageMilitaryLogistics(List<Architecture> cities)
        {
            // 计算军区内总军事力量和平均值
            int totalMilitary = cities.Sum(c => c.MilitaryCount);
            if (totalMilitary == 0) return;

            int averageMilitary = totalMilitary / cities.Count;
            int minThreshold = Math.Max(1000, averageMilitary / 2); // 最低1000兵力

            // 找出兵力不足的前线城市
            var frontlineCities = cities.Where(c => c.FrontLine && c.MilitaryCount < minThreshold).ToList();
            var rearCities = cities.Where(c => !c.FrontLine && c.MilitaryCount > averageMilitary).ToList();

            // 从后方城市向前线城市转移部分兵力
            foreach (var frontCity in frontlineCities)
            {
                int needed = minThreshold - frontCity.MilitaryCount;
                if (needed <= 0) continue;

                foreach (var rearCity in rearCities)
                {
                    int available = rearCity.MilitaryCount - averageMilitary;
                    if (available <= 0) continue;

                    int transfer = Math.Min(needed, available / 2); // 只转移一半，保持后方防御
                    if (transfer > 500) // 只有超过500才值得转移
                    {
                        // 这里可以实现具体的兵力转移逻辑
                        // 由于涉及Military对象的复杂操作，这里只做标记
                        // 实际实现可能需要调用现有的转移方法

#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[ManageMilitaryLogistics] 计划转移兵力 {transfer} 从 {rearCity.Name} 到 {frontCity.Name}");
#endif

                        needed -= transfer;
                        if (needed <= 0) break;
                    }
                }
            }
        }

        /// <summary>
        /// 军区级协同攻击：协调军区内城市的军事行动
        /// 限制：仅协调军区内城市，不涉及跨军区协调
        /// </summary>
        /// <param name="cities">军区内的城市列表</param>
        private void AICoordinatedAttacks(List<Architecture> cities)
        {
            if (cities == null || cities.Count <= 1) return;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Section.AICoordinatedAttacks] 军区 {this.Name} 开始协同攻击规划. 城市数:{cities.Count}");
#endif

            // 1. 识别共同威胁目标
            var commonTargets = IdentifyCommonTargets(cities);
            
            // 2. 协调防御：相邻城市互相支援
            CoordinateDefense(cities);
            
            // 3. 协调进攻：多城市联合攻击
            CoordinateOffense(cities, commonTargets);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Section.AICoordinatedAttacks] 军区 {this.Name} 协同攻击规划完成");
#endif
        }

        /// <summary>
        /// 识别军区内城市的共同威胁目标
        /// </summary>
        /// <param name="cities">军区内城市列表</param>
        /// <returns>共同威胁目标列表</returns>
        private List<Architecture> IdentifyCommonTargets(List<Architecture> cities)
        {
            var commonTargets = new List<Architecture>();
            var targetCounts = new Dictionary<Architecture, int>();

            // 统计每个敌方城市被多少个我方城市视为威胁
            // 简化实现：基于距离和敌对关系识别潜在目标
            foreach (var city in cities)
            {
                // 寻找附近的敌方城市作为潜在目标
                foreach (Architecture enemyCity in Session.Current.Scenario.Architectures)
                {
                    if (enemyCity.BelongedFaction != this.BelongedFaction && 
                        enemyCity.BelongedFaction != null &&
                        Session.Current.Scenario.GetDistance(city.ArchitectureArea, enemyCity.ArchitectureArea) <= 5)
                    {
                        if (!targetCounts.ContainsKey(enemyCity))
                            targetCounts[enemyCity] = 0;
                        targetCounts[enemyCity]++;
                    }
                }
            }

            // 选择被多个城市视为威胁的目标作为共同目标
            foreach (var kvp in targetCounts)
            {
                if (kvp.Value >= 2) // 至少被2个城市视为威胁
                {
                    commonTargets.Add(kvp.Key);
                }
            }

#if DEBUG
            if (commonTargets.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[IdentifyCommonTargets] 发现 {commonTargets.Count} 个共同威胁目标: {string.Join(", ", commonTargets.Select(t => t.Name))}");
            }
#endif

            return commonTargets;
        }

        /// <summary>
        /// 协调防御：相邻城市互相支援
        /// </summary>
        /// <param name="cities">军区内城市列表</param>
        private void CoordinateDefense(List<Architecture> cities)
        {
            // 找出受到威胁的城市（前线城市或最近被攻击的城市）
            var threatenedCities = cities.Where(c => c.FrontLine || c.RecentlyAttacked > 0).ToList();
            
            foreach (var threatenedCity in threatenedCities)
            {
                // 寻找可以支援的相邻城市
                var supportCities = cities.Where(c => 
                    c != threatenedCity && 
                    !c.FrontLine && 
                    c.MilitaryCount > 2000 && // 有足够兵力
                    IsAdjacent(c, threatenedCity)).ToList();

                foreach (var supportCity in supportCities)
                {
                    // 这里可以实现具体的支援逻辑
                    // 例如：派遣援军、资源支援等

#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[CoordinateDefense] {supportCity.Name} 准备支援受威胁 of {threatenedCity.Name}");
#endif
                }
            }
        }

        /// <summary>
        /// 协调进攻：多城市联合攻击共同目标
        /// </summary>
        /// <param name="cities">军区内城市列表</param>
        /// <param name="commonTargets">共同威胁目标列表</param>
        private void CoordinateOffense(List<Architecture> cities, List<Architecture> commonTargets)
        {
            foreach (var target in commonTargets)
            {
                // 找出可以攻击该目标的城市
                var attackerCities = cities.Where(c => 
                    c.MilitaryCount > 3000 && // 有足够攻击力
                    CanAttack(c, target)).ToList();

                if (attackerCities.Count >= 2) // 至少2个城市可以联合攻击
                {
                    // 协调攻击时机和策略
                    CoordinateAttackTiming(attackerCities, target);
                }
            }
        }

        /// <summary>
        /// 检查两个城市是否相邻（可以互相支援）
        /// </summary>
        /// <param name="city1">城市1</param>
        /// <param name="city2">城市2</param>
        /// <returns>是否相邻</returns>
        private bool IsAdjacent(Architecture city1, Architecture city2)
        {
            // 检查陆路连接
            if (city1.AILandLinks.HasGameObject(city2)) return true;
            
            // 检查水路连接
            if (city1.AIWaterLinks.HasGameObject(city2)) return true;
            
            return false;
        }

        /// <summary>
        /// 检查城市是否可以攻击目标
        /// </summary>
        /// <param name="city">攻击方城市</param>
        /// <param name="target">目标城市</param>
        /// <returns>是否可以攻击</returns>
        private bool CanAttack(Architecture city, Architecture target)
        {
            // 简化的攻击可能性检查
            // 实际实现可能需要考虑距离、路径、外交关系等
            return city.BelongedFaction != target.BelongedFaction && 
                   (IsAdjacent(city, target) || 
                    Session.Current.Scenario.GetDistance(city.ArchitectureArea, target.ArchitectureArea) <= 3); // 距离不超过3格
        }

        /// <summary>
        /// 协调攻击时机：确保多个城市同时或连续攻击
        /// </summary>
        /// <param name="attackers">攻击方城市列表</param>
        /// <param name="target">目标城市</param>
        private void CoordinateAttackTiming(List<Architecture> attackers, Architecture target)
        {
            // 这里可以实现具体的攻击协调逻辑
            // 例如：设置攻击优先级、协调攻击顺序等

#if DEBUG
            string attackerNames = string.Join(", ", attackers.Select(a => a.Name));
            System.Diagnostics.Debug.WriteLine($"[CoordinateAttackTiming] 协调联合攻击: {attackerNames} → {target.Name}");
#endif
        }
    }
}
