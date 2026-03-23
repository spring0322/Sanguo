using WorldOfTheThreeKingdoms.GameGlobal;  // 🔥 用于 GenerateUIAccessor 特性
using GameObjects;
using System;
using System.Runtime.Serialization;

namespace GameObjects.PersonDetail
{
    [DataContract]
    [GenerateUIAccessor]  // 🔥 2026-03-03 修复：添加源生成器特性，支持 UI 列表显示
    public class CharacterKind : GameObject
    {
        private int challengeChance;
        private int controversyChance;
        private float intelligenceRate;
        private int listenToAdvisorChance;

        public void Init()
        {
            generationChance = new int[10];
        }

        [DataMember]
        public int ChallengeChance
        {
            get
            {
                return this.challengeChance;
            }
            set
            {
                this.challengeChance = value;
            }
        }

        [DataMember]
        public int ControversyChance
        {
            get
            {
                return this.controversyChance;
            }
            set
            {
                this.controversyChance = value;
            }
        }

        [DataMember]
        public float IntelligenceRate
        {
            get
            {
                return this.intelligenceRate;
            }
            set
            {
                this.intelligenceRate = value;
            }
        }

        private int[] generationChance = new int[10];

        [DataMember]
        public int[] GenerationChance
        {
            get
            {
                return generationChance;
            }
            set
            {
                generationChance = value;
            }
        }

        /// <summary>
        /// 纳谏倾向 (0-100)
        /// 100 = 言听计从 (如刘备对诸葛亮)
        /// 70 = 较易听从 (如曹操对荀彧)
        /// 50 = 普通
        /// 30 = 较难说服 (如孙权的独立性)
        /// 10 = 刚愎自用 (如袁绍)
        /// </summary>
        [DataMember]
        public int ListenToAdvisorChance
        {
            get
            {
                return this.listenToAdvisorChance;
            }
            set
            {
                this.listenToAdvisorChance = value;
            }
        }
    }
}

