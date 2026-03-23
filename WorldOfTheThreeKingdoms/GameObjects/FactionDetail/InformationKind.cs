using WorldOfTheThreeKingdoms.GameGlobal;
using GameObjects;
using System;
using System.Runtime.Serialization;

namespace GameObjects.FactionDetail
{
    // 🔥 2026-03-03 修复：添加源生成器特性，让 UI 能访问属性
    [DataContract]
    [GenerateUIAccessor]
    public class InformationKind : GameObject
    {
        // 🔥 AOT 序列化修复：添加私有无参构造函数
        public InformationKind() { }
        
        private int costFund;
        private InformationLevel level;
        private bool oblique;
        private int radius;

        // 🔥 2026-03-03 修复：确保Name在首次访问时生成
        // 策略：不使用 new 隐藏属性，而是在setter中自动生成Name
        // 这样源生成器访问 GameObject.Name 时能获取到正确的值
        private void EnsureNameGenerated()
        {
            // 如果已经有Name，不重新生成
            if (!string.IsNullOrEmpty(base.name))
                return;

            // 根据等级生成名称（使用实际的枚举值）
            string levelName = this.Level switch
            {
                InformationLevel.未知 => "未知",
                InformationLevel.无 => "无",
                InformationLevel.低 => "低级",
                InformationLevel.中 => "中级",
                InformationLevel.高 => "高级",
                InformationLevel.全 => "全域",
                _ => "未知"
            };

            string obliqueText = this.Oblique ? "斜向" : "正向";
            base.name = $"{levelName}情报({obliqueText},半径{this.Radius})";
        }

        public bool Avail(Architecture a)
        {
            return (a.Fund >= this.costFund);
        }
        [DataMember]
        public int CostFund
        {
            get
            {
                return this.costFund;
            }
            set
            {
                this.costFund = value;
            }
        }

        public int FightingWeighing
        {
            get
            {
                return ((((this.Radius) *(int)  this.Level) * 100) / this.CostFund);
            }
        }
        [DataMember]
        public InformationLevel Level
        {
            get
            {
                return this.level;
            }
            set
            {
                this.level = value;
                EnsureNameGenerated(); // 🔥 Level设置后生成Name
            }
        }
        [DataMember]
        public bool Oblique
        {
            get
            {
                return this.oblique;
            }
            set
            {
                this.oblique = value;
                EnsureNameGenerated(); // 🔥 Oblique设置后生成Name
            }
        }

        public string ObliqueString
        {
            get
            {
                EnsureNameGenerated(); // 🔥 访问前确保Name已生成
                return (this.Oblique ? "○" : "×");
            }
        }
        [DataMember]
        public int Radius
        {
            get
            {
                return this.radius;
            }
            set
            {
                this.radius = value;
                EnsureNameGenerated(); // 🔥 Radius设置后生成Name
            }
        }
    }
}

