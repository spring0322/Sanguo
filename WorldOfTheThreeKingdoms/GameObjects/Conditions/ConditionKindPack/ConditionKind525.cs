using GameManager;
using GameObjects;
using GameObjects.Conditions;
using System;


using System.Runtime.Serialization;namespace GameObjects.Conditions.ConditionKindPack
{

    [DataContract]public class ConditionKind525 : ConditionKind
    {
        private int personID;

        public override bool CheckConditionKind(Person person)
        {
            Person p1 = (Session.Current.Scenario.Persons.GetGameObject(personID) is Person ? (Person)Session.Current.Scenario.Persons.GetGameObject(personID) : null);
            return !( person.LocationArchitecture != null && p1.LocationArchitecture  != null && person.LocationArchitecture == p1.LocationArchitecture );
        }

        public override void InitializeParameter(string parameter)
        {
            try
            {
                this.personID = int.Parse(parameter);
            }
            catch
            {
            }
        }
    }
}

