using GameObjects;
using GameObjects.Influences;
using System;


using System.Runtime.Serialization;namespace GameObjects.Influences.InfluenceKindPack
{

    [DataContract]public class InfluenceKind860 : InfluenceKind
    {
        [DataMember]
        private int id;

        public override void ApplyInfluenceKind(Troop troop)
        {
            if (troop != null && !troop.AllowedStrategems.Contains(id))
            {
                troop.AllowedStrategems.Add(id);
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[InfluenceKind860] 添加计略 ID={id} 到部队 {troop.DisplayName}");
                System.Diagnostics.Debug.WriteLine($"[InfluenceKind860]   当前计略列表: {string.Join(", ", troop.AllowedStrategems)}");
                #endif
            }
        }

        public override void PurifyInfluenceKind(Troop troop)
        {
            if (troop != null && troop.AllowedStrategems.Contains(id))
            {
                troop.AllowedStrategems.Remove(id);
            }
        }

        public override void InitializeParameter(string parameter)
        {
            try
            {
                this.id = int.Parse(parameter);
            }
            catch
            {
            }
        }
    }
}

