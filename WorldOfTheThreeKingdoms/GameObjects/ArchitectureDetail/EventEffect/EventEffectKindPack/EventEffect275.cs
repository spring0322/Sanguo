using GameManager;
using GameObjects;
using GameObjects.PersonDetail;
using System;


using System.Runtime.Serialization;namespace GameObjects.ArchitectureDetail.EventEffect
{


    [DataContract]public class EventEffect275 : EventEffectKind
    {
        
        public override void ApplyEffectKind(Person person, Event e)
        {
            if (person.BelongedFaction != null && person.LocationArchitecture != null && person.BelongedCaptive == null)
            {
                // 🔥 修复：防御性类型检查，避免索引访问时的 InvalidCastException
                var allTypes = Session.Current.Scenario.GameCommonData.AllPersonGeneratorTypes;
                
                if (allTypes.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[EventEffect275] ❌ AllPersonGeneratorTypes 为空");
                    return;
                }

                int randomIndex = GameObject.Random(allTypes.Count);
                GameObject obj = allTypes[randomIndex];
                
                if (obj is PersonGeneratorType type)
                {
                    person.LocationArchitecture.GenerateOfficer(type, true);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[EventEffect275] ⚠️ 数据污染：索引 {randomIndex} 类型为 {obj.GetType().Name}");
                }
            }
        }

    }
}
