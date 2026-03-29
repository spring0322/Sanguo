using GameManager;
using System;
using System.Runtime.Serialization;

namespace GameObjects
{
    // 🔥 2026-02-12 根本修复：移除 [DataContract]，添加 [JsonConverter]
    [System.Text.Json.Serialization.JsonConverter(typeof(WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter))]
    public class ArchitectureList : GameObjectList
    {
        public void AddArchitectureWithEvent(Architecture architecture, bool add = true)
        {
            if (add)
            {
                base.Add(architecture);
            }
            //if (Session.MainGame.mainGameScreen != null)
            //{
                architecture.OnHirePerson += new Architecture.HirePerson(this.architecture_OnHirePerson);
                architecture.OnRewardPersons += new Architecture.RewardPersons(this.architecture_OnRewardPersons);
                architecture.OnFacilityCompleted += new Architecture.FacilityCompleted(this.architecture_OnFacilityCompleted);
                architecture.Onfashengzainan  += new Architecture.fashengzainan (this.architecture_Onfashengzainan);
                architecture.OnReleaseCaptiveAfterOccupied += new Architecture.ReleaseCaptiveAfterOccupied(this.architecture_OnReleaseCaptiveAfterOccupied);
                architecture.OnBeginRecentlyAttacked += new Architecture.BeginRecentlyAttacked(this.architecture_OnBeginRecentlyAttacked);
                architecture.OnPopulationEnter += new Architecture.PopulationEnter(this.architecture_OnPopulationEnter);
                architecture.OnPopulationEscape += new Architecture.PopulationEscape(this.architecture_OnPopulationEscape);
                architecture.OnSelectprince += new Architecture.Selectprince(this.architecture_OnSelectprince);
                architecture.OnAppointmayor += new Architecture.Appointmayor(this.architecture_OnAppointmayor);
                architecture.OnZhaoxian += new Architecture.Zhaoxian(this.architecture_OnZhaoxian);
            //}
        }

        public void architecture_OnZhaoxian(Person person, Person leader)
        {
            Session.MainGame.mainGameScreen.Zhaoxian(person, leader);
        }

        public void architecture_OnSelectprince(Person person, Person leader)//立储
        {
            Session.MainGame.mainGameScreen.Selectprince(person, leader);
        }

        public void architecture_OnAppointmayor(Person person, Person leader) //太守
        {
            Session.MainGame.mainGameScreen.Appointmayor(person, leader);
        }

        public void ApplyInfluences()
        {
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (Architecture in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                Architecture architecture = base.GameObjects[i] as Architecture;
                if (architecture == null)
                {
                    throw new InvalidOperationException($"ArchitectureList 中存在非 Architecture 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                architecture.ApplyInfluences();
            }
        }
        
        /// <summary>
        /// 🆕 批量应用势力范围增益到所有城池
        /// 🧊 Cold Path：势力范围更新或读档后调用
        /// 日期：2026-03-16
        /// </summary>
        public void ApplyInfluenceBuff()
        {
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (Architecture in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                Architecture architecture = base.GameObjects[i] as Architecture;
                if (architecture == null)
                {
                    throw new InvalidOperationException($"ArchitectureList 中存在非 Architecture 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                try
                {
                    architecture.ApplyInfluenceBuff();
                }
                catch (Exception ex)
                {
                    string factionName = architecture.BelongedFaction == null ? "null" : architecture.BelongedFaction.Name;
                    System.Diagnostics.Debug.WriteLine(
                        $"[ArchitectureList.ApplyInfluenceBuff] architecture={architecture.Name}(ID:{architecture.ID}), faction={factionName}: {ex}");
                    throw;
                }
            }
        }

        private void architecture_OnBeginRecentlyAttacked(Architecture architecture)
        {
            Session.MainGame.mainGameScreen.ArchitectureBeginRecentlyAttacked(architecture);
        }

        private void architecture_OnFacilityCompleted(Architecture architecture, Facility facility)
        {
            Session.MainGame.mainGameScreen.ArchitectureFacilityCompleted(architecture, facility);
        }

        private void architecture_Onfashengzainan(Architecture architecture, int zainanID)
        {
            Session.MainGame.mainGameScreen.Architecturefashengzainan(architecture, zainanID);
        }

        private void architecture_OnHirePerson(PersonList personList)
        {
            if (personList.Count > 0)
            {
                Session.MainGame.mainGameScreen.ArchitectureHirePerson(personList);
            }
        }

        private void architecture_OnPopulationEnter(Architecture a, int quantity)
        {
            Session.MainGame.mainGameScreen.ArchitecturePopulationEnter(a, quantity);
        }

        private void architecture_OnPopulationEscape(Architecture a, int quantity)
        {
            Session.MainGame.mainGameScreen.ArchitecturePopulationEscape(a, quantity);
        }

        private void architecture_OnReleaseCaptiveAfterOccupied(Architecture architecture, PersonList persons)
        {
            Session.MainGame.mainGameScreen.ArchitectureReleaseCaptiveAfterOccupied(architecture, persons);
        }

        private void architecture_OnRewardPersons(Architecture architecture, GameObjectList personlist)
        {
            Session.MainGame.mainGameScreen.ArchitectureRewardPersons(architecture, personlist);
        }

        public Architecture GetMaxEnduranceArchitecture(Architecture target)
        {
            int endurance = -1;
            Architecture architecture = null;
            
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (Architecture in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                Architecture architecture2 = base.GameObjects[i] as Architecture;
                if (architecture2 == null)
                {
                    throw new InvalidOperationException($"ArchitectureList 中存在非 Architecture 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                
                if (architecture2 == target)
                {
                    return architecture2;
                }
                if (architecture2.Endurance > endurance)
                {
                    endurance = architecture2.Endurance;
                    architecture = architecture2;
                }
            }
            return architecture;
        }

        public Architecture GetMinEnduranceArchitecture(Architecture target)
        {
            int endurance = 0x7fffffff;
            Architecture architecture = null;
            
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (Architecture in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                Architecture architecture2 = base.GameObjects[i] as Architecture;
                if (architecture2 == null)
                {
                    throw new InvalidOperationException($"ArchitectureList 中存在非 Architecture 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                
                if (architecture2 == target)
                {
                    return architecture2;
                }
                if (architecture2.Endurance < endurance)
                {
                    endurance = architecture2.Endurance;
                    architecture = architecture2;
                }
            }
            return architecture;
        }

        public void NoFactionDevelop()
        {
            // 🔥 AOT 修复：显式类型转换，避免隐式转换失败
            // 日期：2026-03-21
            // 原因：AOT 环境下 foreach (Architecture in List<GameObject>) 隐式转换失败
            // 解决：使用 for 循环 + as 类型转换 + Fail Fast
            for (int i = 0; i < base.GameObjects.Count; i++)
            {
                Architecture architecture = base.GameObjects[i] as Architecture;
                if (architecture == null)
                {
                    throw new InvalidOperationException($"ArchitectureList 中存在非 Architecture 类型的对象：{base.GameObjects[i]?.GetType().Name ?? "null"}");
                }
                
                if (architecture.BelongedFaction == null)
                {
                    architecture.DevelopDayNoFaction();
                }
            }
        }
    }
}

