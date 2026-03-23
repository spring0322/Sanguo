using GameManager;
using GameObjects;
using GameObjects.PersonDetail;
using System;


using System.Runtime.Serialization;namespace GameObjects.ArchitectureDetail.EventEffect
{


    [DataContract]public class EventEffect270 : EventEffectKind
    {
        private int preferredType;

        public override void ApplyEffectKind(Person person, Event e)
        {
            if (person.BelongedFaction != null && person.LocationArchitecture != null && person.BelongedCaptive == null)
            {
                // 🔥 修复：防御性类型检查，避免索引访问时的 InvalidCastException
                var allTypes = Session.Current.Scenario.GameCommonData.AllPersonGeneratorTypes;
                
                if (preferredType < 0 || preferredType >= allTypes.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"[EventEffect270] ❌ 无效的 preferredType 索引: {preferredType}");
                    return;
                }

                GameObject obj = allTypes[preferredType];
                
                if (obj is PersonGeneratorType type)
                {
                    person.LocationArchitecture.GenerateOfficer(type, true);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[EventEffect270] ⚠️ 数据污染：索引 {preferredType} 类型为 {obj.GetType().Name}");
                }
            }
        }

        public override void InitializeParameter(string parameter)
        {
            try
            {
                this.preferredType = int.Parse(parameter);
            }
            catch
            {
            }
        }
    }
}
