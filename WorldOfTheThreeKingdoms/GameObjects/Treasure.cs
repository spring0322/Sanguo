using WorldOfTheThreeKingdoms.GameGlobal;  // 🔥 添加：用于 GenerateUIAccessor 特性
using GameManager;
using GameObjects.Influences;
using Microsoft.Xna.Framework.Graphics;
using Platforms;
using System;
using System.Runtime.Serialization;
using WorldOfTheThreeKingdoms;



namespace GameObjects
{
    [DataContract]
    [GenerateUIAccessor]  // 🔥 添加源生成器特性，支持宝物列表和右键菜单访问
    public partial class Treasure : GameObject
    {
        private int appearYear;
        private bool available;

        [DataMember]
        public int BelongedPersonIDString { get; set; }

        public Person BelongedPerson;
        private string description;

        [DataMember]
        public int HidePlaceIDString { get; set; }

        public Architecture HidePlace;

        [DataMember]
        public string InfluencesString { get; set; }
        
        [DataMember] 
        public int Durability { get; set; }

        public InfluenceTable Influences = new InfluenceTable();
        private int pic;
        private PlatformTexture picture;
        private int worth;

        public void Init()
        {
            Influences = new InfluenceTable();
        }

        [DataMember]
        public int TreasureGroup
        {
            get;
            set;
        }

        [DataMember]
        public int AppearYear
        {
            get
            {
                return this.appearYear;
            }
            set
            {
                this.appearYear = value;
            }
        }

        [DataMember]
        public bool Available
        {
            get
            {
                return this.available;
            }
            set
            {
                this.available = value;
            }
        }

        public string BelongedPersonString
        {
            get
            {
                return ((this.BelongedPerson != null) ? this.BelongedPerson.Name : "----");
            }
        }

        [DataMember]
        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
            }
        }

        public string HidePlaceString
        {
            get
            {
                return ((this.HidePlace != null) ? this.HidePlace.Name : "----");
            }
        }

        public string InfluenceString
        {
            get
            {
                string str = "";
                foreach (Influence influence in this.Influences.Influences.Values)
                {
                    str = str + "•" + influence.Description;
                }
                return str;
            }
        }

        [DataMember]
        public int Pic
        {
            get
            {
                return this.pic;
            }
            set
            {
                this.pic = value;
            }
        }

        //public void disposeTexture()
        //{
        //    if (this.picture != null)
        //    {
        //        this.picture.Dispose();
        //        this.picture = null;
        //    }
        //}

        public PlatformTexture Picture
        {
            get
            {
                if (this.picture == null)
                {
                    try
                    {
                        this.picture = CacheManager.GetTempTexture("Content/Textures/Resources/Treasure/" + this.Pic.ToString() + ".jpg");
                    }
                    catch
                    {
                        this.picture = null;
                    }
                }
                return this.picture;
            }
        }

        [DataMember]
        public int Worth
        {
            get
            {
                return this.worth;
            }
            set
            {
                this.worth = value;
            }
        }

        /// <summary>
        /// 出售宝物获得的资金（用于UI显示）
        /// 🔥 2026-03-03 新增：宝物交易系统
        /// </summary>
        public int SellTreasureFund
        {
            get
            {
                return this.Worth * 1000;
            }
        }

        /// <summary>
        /// 购买宝物需要的资金（用于UI显示）
        /// 🔥 2026-03-03 新增：宝物交易系统，加价20%
        /// </summary>
        public int BuyTreasureFund
        {
            get
            {
                return (int)(this.Worth * 1.2f * 1000);
            }
        }
    }
}

