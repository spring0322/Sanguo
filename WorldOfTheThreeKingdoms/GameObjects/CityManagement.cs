using System;
using System.Collections.Generic;
using System.Linq;

namespace GameObjects
{
    /// <summary>
    /// 点坐标结构
    /// </summary>
    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
        
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }

    /// <summary>
    /// 建筑类型枚举
    /// </summary>
    public enum BuildingType
    {
        None,           // 无建筑
        Market,         // 市场
        Farm,           // 农田
        Barracks,       // 兵营
        Wall,           // 城墙
        Academy,        // 学院
        Temple,         // 寺庙
        Workshop,       // 工坊
        Granary,        // 粮仓
        Fortress        // 要塞
    }

    /// <summary>
    /// 势力状态枚举
    /// </summary>
    public enum FactionState
    {
        Stable,         // 稳定
        Expansion,      // 扩张
        Defensive,      // 防御
        WarTime,        // 战时
        EconomicCrisis, // 经济危机
        Prosperous      // 繁荣
    }

    /// <summary>
    /// 城市类 - 扩展现有城市功能以支持建设AI
    /// </summary>
    public partial class City
    {
        // 建设相关属性
        public int FreeSlots { get; set; } = 3; // 可建设槽位
        public bool IsBuilding { get; set; } = false; // 是否正在建设
        public BuildingType CurrentConstruction { get; set; } = BuildingType.None; // 当前建设项目
        public int ConstructionProgress { get; set; } = 0; // 建设进度
        public int ConstructionTime { get; set; } = 0; // 建设所需时间
        
        // 城市特性
        public bool IsCapital { get; set; } = false; // 是否为首都
        public bool IsPortCity { get; set; } = false; // 是否为港口城市
        public bool IsBorderCity { get; set; } = false; // 是否为边境城市
        public bool HasRecentBattle { get; set; } = false; // 是否最近有战斗
        
        // 城市状态
        public int Population { get; set; } = 10000; // 人口
        public int MaxPopulation { get; set; } = 50000; // 最大人口
        public float Prosperity { get; set; } = 0.5f; // 繁荣度
        public float Security { get; set; } = 0.7f; // 治安
        public float Loyalty { get; set; } = 0.8f; // 忠诚度
        
        // 地理坐标
        public Point Coordinates { get; set; } = new Point(0, 0);
        
        // 建筑列表
        public List<BuildingType> Buildings { get; set; } = new List<BuildingType>();
        
        // 太守
        public Officer Prefect { get; set; }
        
        // 所属势力
        public Faction Owner { get; set; }
        
        // 城市名称
        public string Name { get; set; } = "未命名城市";

        /// <summary>
        /// 任命太守
        /// </summary>
        public void AppointPrefect(Officer officer)
        {
            if (officer == null) return;
            
            // 如果已有太守，先解除
            if (Prefect != null)
            {
                RemovePrefect();
            }
            
            Prefect = officer;
            officer.Location = this;
            
            System.Diagnostics.Debug.WriteLine($"[City] {officer.Name} 被任命为 {Name} 太守");
        }

        /// <summary>
        /// 解除太守职务
        /// </summary>
        public void RemovePrefect()
        {
            if (Prefect != null)
            {
                System.Diagnostics.Debug.WriteLine($"[City] {Prefect.Name} 被解除 {Name} 太守职务");
                Prefect.Location = null; // 或者设置为其他位置
                Prefect = null;
            }
        }

        /// <summary>
        /// 撤职太守（因忠诚度问题）
        /// </summary>
        public void DismissPrefect()
        {
            if (Prefect != null)
            {
                System.Diagnostics.Debug.WriteLine($"[City] {Prefect.Name} 因忠诚度问题被撤职，解除 {Name} 太守职务");
                Prefect.Location = null;
                Prefect = null;
            }
        }

        /// <summary>
        /// 开始建设
        /// </summary>
        public void StartConstruction(BuildingType buildingType)
        {
            if (IsBuilding || FreeSlots <= 0) return;
            
            CurrentConstruction = buildingType;
            IsBuilding = true;
            ConstructionProgress = 0;
            ConstructionTime = GetConstructionTime(buildingType);
            FreeSlots--;
            
            // 扣除建设费用
            int cost = GetBuildingCost(buildingType);
            if (Owner != null && Owner.Gold >= cost)
            {
                Owner.Gold -= cost;
            }
        }

        /// <summary>
        /// 更新建设进度
        /// </summary>
        public void UpdateConstruction()
        {
            if (!IsBuilding) return;
            
            ConstructionProgress++;
            
            if (ConstructionProgress >= ConstructionTime)
            {
                // 建设完成
                CompleteConstruction();
            }
        }

        /// <summary>
        /// 完成建设
        /// </summary>
        private void CompleteConstruction()
        {
            Buildings.Add(CurrentConstruction);
            ApplyBuildingEffects(CurrentConstruction);
            
            IsBuilding = false;
            CurrentConstruction = BuildingType.None;
            ConstructionProgress = 0;
            ConstructionTime = 0;
            
            System.Diagnostics.Debug.WriteLine($"[City] {Name} 完成建设：{CurrentConstruction}");
        }

        /// <summary>
        /// 应用建筑效果
        /// </summary>
        private void ApplyBuildingEffects(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.Market:
                    if (Owner != null) Owner.IncomeModifier += 0.1f;
                    Prosperity += 0.05f;
                    break;
                    
                case BuildingType.Farm:
                    if (Owner != null) Owner.FoodProduction += 100;
                    break;
                    
                case BuildingType.Barracks:
                    // 提升军队训练效率
                    break;
                    
                case BuildingType.Wall:
                    Security += 0.1f;
                    // 提升城市防御
                    break;
                    
                case BuildingType.Academy:
                    if (Owner != null) Owner.TechLevel += 5;
                    break;
                    
                case BuildingType.Temple:
                    Loyalty += 0.08f;
                    Security += 0.05f;
                    break;
                    
                case BuildingType.Workshop:
                    // 提升装备生产
                    break;
                    
                case BuildingType.Granary:
                    if (Owner != null) Owner.FoodStorage += 500;
                    break;
                    
                case BuildingType.Fortress:
                    Security += 0.15f;
                    // 大幅提升军事防御
                    break;
            }
        }

        /// <summary>
        /// 获取建设时间
        /// </summary>
        private int GetConstructionTime(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.Market: return 3;
                case BuildingType.Farm: return 4;
                case BuildingType.Barracks: return 5;
                case BuildingType.Wall: return 6;
                case BuildingType.Academy: return 8;
                case BuildingType.Temple: return 4;
                case BuildingType.Workshop: return 5;
                case BuildingType.Granary: return 3;
                case BuildingType.Fortress: return 10;
                default: return 5;
            }
        }

        /// <summary>
        /// 获取建筑成本
        /// </summary>
        private int GetBuildingCost(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.Market: return 1000;
                case BuildingType.Farm: return 800;
                case BuildingType.Barracks: return 1200;
                case BuildingType.Wall: return 1500;
                case BuildingType.Academy: return 2000;
                case BuildingType.Temple: return 900;
                case BuildingType.Workshop: return 1100;
                case BuildingType.Granary: return 700;
                case BuildingType.Fortress: return 2500;
                default: return 1000;
            }
        }

        /// <summary>
        /// 检查是否有特定建筑
        /// </summary>
        public bool HasBuilding(BuildingType buildingType)
        {
            return Buildings.Contains(buildingType);
        }

        /// <summary>
        /// 获取建筑数量
        /// </summary>
        public int GetBuildingCount(BuildingType buildingType)
        {
            return Buildings.Count(b => b == buildingType);
        }
    }

    /// <summary>
    /// 势力类 - 扩展现有势力功能
    /// </summary>
    public partial class Faction
    {
        // 经济状态
        public int Gold { get; set; } = 10000; // 金钱
        public int Food { get; set; } = 5000; // 粮食
        public int FoodProduction { get; set; } = 1000; // 粮食产量
        public int FoodStorage { get; set; } = 10000; // 粮食储存上限
        public float IncomeModifier { get; set; } = 1.0f; // 收入修正
        public float FiscalHealth { get; set; } = 0.7f; // 财政健康度 (0-1)
        
        // 军事状态
        public int TotalTroops { get; set; } = 1000; // 总兵力
        public int TechLevel { get; set; } = 30; // 科技等级
        
        // 势力状态
        public FactionState State { get; set; } = FactionState.Stable;
        
        // 城市列表
        public List<City> Cities { get; set; } = new List<City>();
        
        // 武将列表
        public List<Officer> Officers { get; set; } = new List<Officer>();
        
        // 人事管理器
        public PersonnelManager PersonnelManager { get; private set; }
        
        // 君主
        public Officer Ruler { get; set; }
        
        // 势力名称
        public string Name { get; set; } = "未命名势力";

        /// <summary>
        /// 初始化人事管理器
        /// </summary>
        public void InitializePersonnelManager()
        {
            PersonnelManager = new PersonnelManager(this);
        }

        /// <summary>
        /// 更新势力状态
        /// </summary>
        public void UpdateFactionState()
        {
            // 根据经济、军事、外交情况更新势力状态
            if (FiscalHealth < 0.3f)
            {
                State = FactionState.EconomicCrisis;
            }
            else if (HasActiveWars())
            {
                State = FactionState.WarTime;
            }
            else if (FiscalHealth > 0.8f && TotalTroops > GetAverageTroopCount())
            {
                State = FactionState.Prosperous;
            }
            else if (IsUnderThreat())
            {
                State = FactionState.Defensive;
            }
            else if (IsExpanding())
            {
                State = FactionState.Expansion;
            }
            else
            {
                State = FactionState.Stable;
            }
        }

        /// <summary>
        /// 是否有活跃战争
        /// </summary>
        private bool HasActiveWars()
        {
            // 简化实现
            return Cities.Any(c => c.HasRecentBattle);
        }

        /// <summary>
        /// 获取平均兵力
        /// </summary>
        private int GetAverageTroopCount()
        {
            // 简化实现
            return 1500;
        }

        /// <summary>
        /// 是否受到威胁
        /// </summary>
        private bool IsUnderThreat()
        {
            // 简化实现
            return Cities.Any(c => c.IsBorderCity && c.HasRecentBattle);
        }

        /// <summary>
        /// 是否在扩张
        /// </summary>
        private bool IsExpanding()
        {
            // 简化实现：最近是否占领了新城市
            return false;
        }
    }

    /// <summary>
    /// 武将类 - 扩展现有武将功能
    /// </summary>
    public partial class Officer
    {
        // 基础属性
        public int Politics { get; set; } = 50; // 政治
        public int Leadership { get; set; } = 50; // 统率
        public int Intelligence { get; set; } = 50; // 智力
        public int War { get; set; } = 50; // 武力
        public int Age { get; set; } = 30; // 年龄
        public int Health { get; set; } = 100; // 健康度
        
        // 忠诚度和稳定性相关属性
        public int Loyalty { get; set; } = 80; // 忠诚度 (0-100)
        public int Ambition { get; set; } = 50; // 野心 (0-100)
        public int Righteousness { get; set; } = 70; // 义理 (0-100)
        
        // 状态
        public OfficerState State { get; set; } = OfficerState.Active;
        public City Location { get; set; } // 当前所在城市
        
        // 性格特质 - 使用HashSet提高查询效率
        public HashSet<Trait> Traits { get; set; } = new HashSet<Trait>();
        
        // 个人经历
        public List<string> Experiences { get; set; } = new List<string>();
        
        // 关系网：Key=武将ID, Value=关系类型
        public Dictionary<int, RelationType> Relations { get; set; } = new Dictionary<int, RelationType>();
        
        // 武将ID
        public int ID { get; set; }
        
        // 武将姓名
        public string Name { get; set; } = "未命名武将";

        /// <summary>
        /// 检查是否有特定经历
        /// </summary>
        public bool HasExperience(string experience)
        {
            return Experiences.Contains(experience);
        }

        /// <summary>
        /// 添加经历
        /// </summary>
        public void AddExperience(string experience)
        {
            if (!Experiences.Contains(experience))
            {
                Experiences.Add(experience);
            }
        }

        /// <summary>
        /// 添加性格特质
        /// </summary>
        public void AddTrait(Trait trait)
        {
            Traits.Add(trait);
        }

        /// <summary>
        /// 检查是否有特定性格特质
        /// </summary>
        public bool HasTrait(Trait trait)
        {
            return Traits.Contains(trait);
        }

        /// <summary>
        /// 添加关系
        /// </summary>
        public void AddRelation(Officer officer, RelationType relationType)
        {
            if (officer != null && officer.ID != this.ID)
            {
                Relations[officer.ID] = relationType;
                
                // 双向关系：某些关系类型需要互相设置
                if (relationType == RelationType.Friend || 
                    relationType == RelationType.Hated || 
                    relationType == RelationType.SwornBrother ||
                    relationType == RelationType.Spouse)
                {
                    officer.Relations[this.ID] = relationType;
                }
            }
        }

        /// <summary>
        /// 获取与指定武将的关系
        /// </summary>
        public RelationType GetRelationWith(Officer officer)
        {
            if (officer == null || !Relations.ContainsKey(officer.ID))
                return RelationType.None;
            
            return Relations[officer.ID];
        }

        /// <summary>
        /// 检查是否厌恶某个武将
        /// </summary>
        public bool Dislikes(Officer officer)
        {
            return GetRelationWith(officer) == RelationType.Hated;
        }

        /// <summary>
        /// 检查是否喜欢某个武将
        /// </summary>
        public bool Likes(Officer officer)
        {
            var relation = GetRelationWith(officer);
            return relation == RelationType.Friend || 
                   relation == RelationType.ParentChild || 
                   relation == RelationType.SwornBrother ||
                   relation == RelationType.Spouse;
        }

        /// <summary>
        /// 判定是否死忠 (用于熔断逻辑)
        /// 配偶和义兄弟关系提供绝对忠诚，无视野心风险
        /// </summary>
        public bool IsSoulBoundTo(Officer target)
        {
            if (target == null || !Relations.ContainsKey(target.ID)) 
                return false;
            
            var relationType = Relations[target.ID];
            return relationType == RelationType.SwornBrother || relationType == RelationType.Spouse;
        }

        /// <summary>
        /// 移除关系
        /// </summary>
        public void RemoveRelation(Officer officer)
        {
            if (officer != null && Relations.ContainsKey(officer.ID))
            {
                Relations.Remove(officer.ID);
                
                // 同时移除对方的关系
                if (officer.Relations.ContainsKey(this.ID))
                {
                    officer.Relations.Remove(this.ID);
                }
            }
        }

        /// <summary>
        /// 获取所有指定类型的关系武将ID
        /// </summary>
        public List<int> GetRelatedOfficerIds(RelationType relationType)
        {
            return Relations.Where(r => r.Value == relationType).Select(r => r.Key).ToList();
        }
    }

    /// <summary>
    /// 性格特质枚举 - 影响建设偏好和行为模式
    /// </summary>
    public enum Trait
    {
        None,           // 无特殊性格
        Rash,           // 莽撞：喜欢造兵营，容易无视命令出击
        Greedy,         // 贪婪：喜欢造市场，容易私吞钱粮，忠诚度下降快
        Timid,          // 胆小：不喜欢出击，喜欢造城墙
        Fame,           // 名声：喜欢造这一类"好看"的建筑
        Loyal,          // 忠义：服从度极高
        Cautious,       // 谨慎 - 偏好防御建筑
        Scholar,        // 学者 - 偏好文化建筑
        Pragmatic,      // 实用主义 - 平衡发展
        Ambitious,      // 野心勃勃 - 快速扩张
        Conservative,   // 保守 - 稳健发展
        Rebellious      // 叛逆 - 容易叛变
    }

    /// <summary>
    /// 武将关系类型枚举
    /// </summary>
    public enum RelationType
    {
        None,           // 无关系
        Friend,         // 亲爱武将 (小幅加分)
        Hated,          // 厌恶武将 (绝对排斥/甚至拒绝同队)
        ParentChild,    // 亲子 (高加分)
        Spouse,         // 配偶 (绝对绑定，无视野心风险)
        SwornBrother    // 义兄弟 (绝对绑定，无视野心风险)
    }
}