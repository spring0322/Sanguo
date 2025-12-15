using System.Runtime.Serialization;

namespace GameObjects
{
    [DataContract]
    public class TreasureCreationSetting : GameObject
    {
        [DataMember]
        public int[] EligibleInfluenceIDs
        {
            get;
            set;
        }

        [DataMember]
        public int TreasureGroup { get; set; }
        
        [DataMember]
        public int Cost { get; set; }
        
        [DataMember]
        public int[] PicIDs { get; set; }
        
    }
}
