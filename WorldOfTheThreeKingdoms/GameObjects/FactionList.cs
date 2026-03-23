using GameManager;
using GameObjects.FactionDetail;
using System;
using System.Runtime.Serialization;

namespace GameObjects
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract]，添加 [JsonConverter]
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class FactionList : GameObjectList
    {
        public void AddFactionWithEvent(Faction faction, bool add = true)
        {
            if (add)
            {
                base.GameObjects.Add(faction);
            }
            //if (Session.MainGame.mainGameScreen != null)
            //{
                faction.OnGetControl += new Faction.GetControl(this.faction_OnGetControl);
                faction.OnFactionDestroy += new Faction.FactionDestroy(this.faction_OnFactionDestroy);
                faction.OnAfterCatchLeader += new Faction.AfterCatchLeader(this.faction_OnAfterCatchLeader);
                faction.OnUpgradeTechnique += new Faction.FactionUpgradeTechnique(this.faction_OnUpgradeTechnique);
                faction.OnInitiativeChangeCapital += new Faction.InitiativeChangeCapital(this.faction_OnInitiativeChangeCapital);
                faction.OnForcedChangeCapital += new Faction.ForcedChangeCapital(this.faction_OnForcedChangeCapital);
                faction.OnTechniqueFinished += new Faction.TechniqueFinished(this.faction_OnTechniqueFinished);
                faction.OnAppointAdvisor += new Faction.AppointAdvisorDelegate(this.faction_OnAppointAdvisor);
            //}
        }

        public void ApplyInfluences()
        {
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (Faction in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                Faction faction = base.GameObjects[i] as Faction;
                if (faction == null)
                {
                    throw new InvalidOperationException($"FactionList 中存在非 Faction 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                faction.ApplyTechniques();
            }
        }

        private void faction_OnAfterCatchLeader(Person leader, Faction faction)
        {
            if (Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.FactionAfterCatchLeader(leader, faction);
            }
        }

        private void faction_OnFactionDestroy(Faction faction)
        {
            if (Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.FactionDestroy(faction);
            }
        }

        private void faction_OnForcedChangeCapital(Faction faction, Architecture oldCapital, Architecture newCapital)
        {
            if (Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.FactionForcedChangeCapital(faction, oldCapital, newCapital);
            }
        }

        private void faction_OnGetControl(Faction faction)
        {
            if (Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.FactionGetControl(faction);
            }
        }

        private void faction_OnInitiativeChangeCapital(Faction faction, Architecture oldCapital, Architecture newCapital)
        {
            if (Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.FactionInitialtiveChangeCapital(faction, oldCapital, newCapital);
            }
        }

        private void faction_OnTechniqueFinished(Faction faction, Technique technique)
        {
            if (Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.FactionTechniqueFinished(faction, technique);
            }
        }

        private void faction_OnUpgradeTechnique(Faction faction, Technique technique, Architecture architecture)
        {
            if (Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.FactionUpgradeTechnique(faction, technique, architecture);
            }
        }

        private void faction_OnAppointAdvisor(Person leader, Person advisor)
        {
            if (Session.MainGame.mainGameScreen != null)
            {
                Session.MainGame.mainGameScreen.AppointAdvisor(leader, advisor);
            }
        }

        public void RemoveFaction(Faction faction)
        {
            if (base.HasGameObject(faction))
            {
                foreach (Architecture architecture in faction.Architectures.GetList())
                {
                    faction.RemoveArchitecture(architecture);
                }
                foreach (Troop troop in faction.Troops.GetList())
                {
                    faction.RemoveTroop(troop);
                }
                foreach (Legion legion in faction.Legions.GetList())
                {
                    faction.RemoveLegion(legion);
                }
                foreach (Section section in faction.Sections.GetList())
                {
                    faction.RemoveSection(section);
                }
                base.GameObjects.Remove(faction);
            }
        }
    }
}

