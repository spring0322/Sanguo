using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using WorldOfTheThreeKingdoms.GameGlobal;

namespace GameObjects
{
    public class SmartTroop : Troop
    {
        public SmartTroop() : base() { }

        public virtual void ResetAIState() { }
        public virtual void ExecuteAITurn() { }
        public virtual void RunUnifiedAI() { }
        
        public int CurrentHP { get; set; }
        public bool IsHero { get; set; }
        public float HpRatio { get; set; }
        public int PersonId { get; set; }
        public int Attack { get; set; }
        public int Intelligence { get; set; }
        public List<PersonDetail.Skill> AvailableSkills { get; set; } = new List<PersonDetail.Skill>();
        public int CurrentPrestige { get; set; }
        public int Endurance { get; set; }
        
        public bool IsEnemy(Troop other) => false;
        public bool IsFriend(Troop other) => true;
    }
}
