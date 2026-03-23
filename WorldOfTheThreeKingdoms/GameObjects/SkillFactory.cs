using System;
using System.Collections.Generic;
using GameObjects.PersonDetail;

namespace GameObjects
{
    public static class SkillFactory
    {
        public static Skill CreateFireAttack()
        {
            return new Skill() { ID = 1001, Name = "火计", Cost = 30 };
        }

        public static Skill CreateConfusion()
        {
            return new Skill() { ID = 1002, Name = "混乱", Cost = 40 };
        }
    }
}
