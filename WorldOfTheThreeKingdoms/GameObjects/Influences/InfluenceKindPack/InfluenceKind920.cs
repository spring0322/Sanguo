using GameObjects;
using GameObjects.Influences;
using GameObjects.Conditions;
using GameManager;
using System;
using System.Runtime.Serialization;

namespace GameObjects.Influences.InfluenceKindPack
{
    [DataContract]
    public class InfluenceKind920 : InfluenceKind
    {
        [DataMember]
        private int number = -1;

        public override void ApplyInfluenceKind(Person person)
        {
            if (person.BelongedFaction != null)
            {
                Condition c = Session.Current.Scenario.GameCommonData.AllConditions.GetCondition(this.number);
                if (c != null)
                {
                    // Apply condition logic here
                }
            }
        }

        public override void InitializeParameter(string parameter)
        {
            try
            {
                this.number = int.Parse(parameter);
            }
            catch
            {
            }
        }

        public override bool IsVaild(Person person)
        {
            return person.BelongedFaction != null;
        }
    }
}
