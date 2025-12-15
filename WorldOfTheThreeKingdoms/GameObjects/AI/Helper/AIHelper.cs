using GameGlobal;
using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace GameObjects.AI.Helper
{
    public static class AIHelper
    {
        // 1. 基础算法
        public static int GetManhattanDistance(Point p1, Point p2)
        {
            return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
        }

        public static void Log(string message)
        {
            // 【修复】只接收 1 个字符串，防止重载报错
            // 之前的报错是因为调用了 Log(str, arg) 这种格式
            System.Diagnostics.Debug.WriteLine("[AI] " + message);
        }

        // 2. 战斗判定
        public static bool CanAttackTarget(Troop attacker, Troop target)
        {
            if (attacker == null || target == null) return false;
            return GetManhattanDistance(attacker.Position, target.Position) <= 1;
        }

        public static bool CanCastStratagem(Troop caster, Troop target)
        {
            return false;
        }

        // 3. 场景获取
        public static GameScenario GetScenario()
        {
            if (Session.Current != null)
            {
                return Session.Current.Scenario;
            }
            return null;
        }

        // 4. 暴力拆解 GameObjectList (彻底解决类型转换报错)
        // 既然无法隐式转换，我们就手动把里面的东西拿出来放进新 List
        public static List<Troop> GetTroopsFromList(object listObj)
        {
            List<Troop> result = new List<Troop>();

            // 检查对象是否有效
            if (listObj != null)
            {
                // 尝试转换为 GameObjectList
                GameObjectList gList = listObj as GameObjectList;
                if (gList != null && gList.GameObjects != null)
                {
                    foreach (GameObject obj in gList.GameObjects)
                    {
                        if (obj is Troop) result.Add(obj as Troop);
                    }
                }
            }
            return result;
        }

        public static List<Architecture> GetArchitecturesFromList(object listObj)
        {
            List<Architecture> result = new List<Architecture>();

            if (listObj != null)
            {
                GameObjectList gList = listObj as GameObjectList;
                if (gList != null && gList.GameObjects != null)
                {
                    foreach (GameObject obj in gList.GameObjects)
                    {
                        if (obj is Architecture) result.Add(obj as Architecture);
                    }
                }
            }
            return result;
        }
    }
}