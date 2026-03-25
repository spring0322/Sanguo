using GameObjects;
using System;
using System.Runtime.Serialization;

namespace GameObjects.TroopDetail.EventEffect
{
    [DataContract]
    public class EventEffect : GameObject
    {
        [DataMember]
        public EventEffectKind Kind;
        private string parameter;

        public EventEffect Clone()
        {
            return this.MemberwiseClone() as EventEffect;
        }

        public void ApplyEffect(Person person)
        {
            if (this.Kind == null)
            {
                throw new InvalidOperationException($"TroopEventEffect {this.ID} ({this.Name}) 的 Kind 为空，无法施加效果。");
            }

            if (this.Kind.GetType() == typeof(EventEffectKind))
            {
                EventEffectKind concreteKind = EventEffectKindFactory.CreateEventEffectKindByID(this.Kind.ID);
                if (concreteKind == null)
                {
                    throw new InvalidOperationException(
                        $"TroopEventEffect {this.ID} ({this.Name}) 的 Kind.ID={this.Kind.ID} 未注册具体实现，无法施加效果。");
                }

                concreteKind.ID = this.Kind.ID;
                concreteKind.Name = this.Kind.Name;
                this.Kind = concreteKind;
            }

            this.Kind.InitializeParameter(this.Parameter);
            this.Kind.ApplyEffectKind(person);
        }

        [DataMember]
        public string Parameter
        {
            get
            {
                return this.parameter;
            }
            set
            {
                this.parameter = value;
            }
        }
    }
}

