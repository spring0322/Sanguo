using System;
using System.Collections.Generic;

namespace GameManager
{
    /// <summary>
    /// 确定性随机数生成器 - 确保相同输入产生相同结果，避免SL大法
    /// </summary>
    public class DeterministicRNG
    {
        /// <summary>
        /// 获取确定性随机值 (0.0 - 1.0)
        /// </summary>
        /// <param name="turn">回合数</param>
        /// <param name="unitId">单位ID</param>
        /// <param name="actionType">行动类型</param>
        /// <returns>0.0-1.0之间的浮点数</returns>
        public static float GetValue(int turn, int unitId, string actionType)
        {
            // 只要回合数没变，单位没变，无论玩家读档多少次，这个Hash值永远一样
            int seed = (turn * 1000) + unitId + actionType.GetHashCode();
            System.Random rnd = new System.Random(seed);
            return (float)rnd.NextDouble();
        }

        /// <summary>
        /// 获取确定性随机整数
        /// </summary>
        /// <param name="turn">回合数</param>
        /// <param name="unitId">单位ID</param>
        /// <param name="actionType">行动类型</param>
        /// <param name="min">最小值（包含）</param>
        /// <param name="max">最大值（不包含）</param>
        /// <returns>随机整数</returns>
        public static int GetInt(int turn, int unitId, string actionType, int min, int max)
        {
            float value = GetValue(turn, unitId, actionType);
            return min + (int)(value * (max - min));
        }

        /// <summary>
        /// 获取确定性随机布尔值
        /// </summary>
        /// <param name="turn">回合数</param>
        /// <param name="unitId">单位ID</param>
        /// <param name="actionType">行动类型</param>
        /// <param name="probability">成功概率 (0.0-1.0)</param>
        /// <returns>是否成功</returns>
        public static bool GetBool(int turn, int unitId, string actionType, float probability)
        {
            return GetValue(turn, unitId, actionType) < probability;
        }

        /// <summary>
        /// 从列表中确定性选择一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="turn">回合数</param>
        /// <param name="unitId">单位ID</param>
        /// <param name="actionType">行动类型</param>
        /// <param name="items">候选列表</param>
        /// <returns>选中的元素</returns>
        public static T Choose<T>(int turn, int unitId, string actionType, IList<T> items)
        {
            if (items == null || items.Count == 0)
                return default(T);

            int index = GetInt(turn, unitId, actionType, 0, items.Count);
            return items[index];
        }

        /// <summary>
        /// 获取当前回合数（用于确定性计算）
        /// </summary>
        /// <returns>当前回合数</returns>
        public static int GetCurrentTurn()
        {
            if (Session.Current?.Scenario?.Date != null)
            {
                return Session.Current.Scenario.Date.Year * 12 + Session.Current.Scenario.Date.Month;
            }
            return 0;
        }

        /// <summary>
        /// 生成确定性的随机种子（用于复杂计算）
        /// </summary>
        /// <param name="turn">回合数</param>
        /// <param name="unitId">单位ID</param>
        /// <param name="actionType">行动类型</param>
        /// <param name="extraSalt">额外的盐值</param>
        /// <returns>随机种子</returns>
        public static int GetSeed(int turn, int unitId, string actionType, int extraSalt = 0)
        {
            return (turn * 1000) + unitId + actionType.GetHashCode() + extraSalt;
        }
    }

    /// <summary>
    /// 战斗结果类型
    /// </summary>
    public enum BattleResultType
    {
        Crush,      // 碾压
        Victory,    // 常规胜利
        CloseWin,   // 险胜
        Defeat,     // 失败
        Stalemate   // 僵持
    }

    /// <summary>
    /// 战斗日志生成器 - 生成生动的战斗描述
    /// </summary>
    public static class BattleLogGenerator
    {
        /// <summary>
        /// 生成战斗日志
        /// </summary>
        /// <param name="winner">胜利方</param>
        /// <param name="loser">失败方</param>
        /// <param name="powerDiff">实力差距</param>
        /// <param name="turn">回合数（用于确定性）</param>
        /// <returns>战斗描述</returns>
        public static string GenerateBattleLog(GameObjects.Faction winner, GameObjects.Faction loser, float powerDiff, int turn = -1)
        {
            if (turn == -1)
                turn = DeterministicRNG.GetCurrentTurn();

            BattleResultType resultType = GetBattleResultType(powerDiff);
            
            return resultType switch
            {
                BattleResultType.Crush => GenerateCrushLog(winner, loser, turn),
                BattleResultType.CloseWin => GenerateCloseWinLog(winner, loser, turn),
                BattleResultType.Victory => GenerateVictoryLog(winner, loser, turn),
                _ => GenerateVictoryLog(winner, loser, turn)
            };
        }

        /// <summary>
        /// 确定战斗结果类型
        /// </summary>
        private static BattleResultType GetBattleResultType(float powerDiff)
        {
            if (powerDiff > 3.0f)
                return BattleResultType.Crush;
            else if (powerDiff < 1.1f)
                return BattleResultType.CloseWin;
            else
                return BattleResultType.Victory;
        }

        /// <summary>
        /// 生成碾压战斗日志
        /// </summary>
        private static string GenerateCrushLog(GameObjects.Faction winner, GameObjects.Faction loser, int turn)
        {
            var crushTemplates = new[]
            {
                "{0} 亲率大军压境，{1} 无力抵抗，开城投降。{2} 兵不血刃占领了目标。",
                "{0} 的铁骑如潮水般涌来，{1} 望风而逃，{2} 轻松夺取了城池。",
                "{0} 大军兵临城下，{1} 见大势已去，献城而降。{2} 不费一兵一卒。",
                "{0} 威名远播，{1} 闻风丧胆，未战先降。{2} 声威大震。",
                "{0} 挥师而至，{1} 军心涣散，望旗而降。{2} 势如破竹。"
            };

            string template = DeterministicRNG.Choose(turn, winner.ID, "crush_battle", crushTemplates);
            return string.Format(template, winner.Leader?.Name ?? winner.Name, 
                                         loser.Leader?.Name ?? loser.Name, 
                                         winner.Name);
        }

        /// <summary>
        /// 生成险胜战斗日志
        /// </summary>
        private static string GenerateCloseWinLog(GameObjects.Faction winner, GameObjects.Faction loser, int turn)
        {
            var closeWinTemplates = new[]
            {
                "{0} 与 {1} 在城下血战十日，尸横遍野。最终 {0} 依靠微弱优势惨胜，损兵折将严重。",
                "{0} 与 {1} 激战三日三夜，双方伤亡惨重。{0} 险胜一筹，但代价沉重。",
                "{0} 苦战不下，{1} 顽强抵抗。经过殊死搏斗，{0} 终于攻破城池，但元气大伤。",
                "{0} 与 {1} 鏖战至深夜，刀光剑影中 {0} 略胜一筹，但胜利来之不易。",
                "{0} 强攻不下，{1} 死守不退。最终 {0} 付出巨大代价才勉强获胜。"
            };

            string template = DeterministicRNG.Choose(turn, winner.ID, "close_battle", closeWinTemplates);
            return string.Format(template, winner.Leader?.Name ?? winner.Name, 
                                         loser.Leader?.Name ?? loser.Name);
        }

        /// <summary>
        /// 生成常规胜利战斗日志
        /// </summary>
        private static string GenerateVictoryLog(GameObjects.Faction winner, GameObjects.Faction loser, int turn)
        {
            var victoryTemplates = new[]
            {
                "{0} 攻破了 {1} 的防线。",
                "{0} 率军猛攻，{1} 败退，{2} 成功夺取了城池。",
                "{0} 指挥有方，{1} 抵挡不住，{2} 占领了目标。",
                "{0} 发动总攻，{1} 力战不支，{2} 取得了胜利。",
                "{0} 破城而入，{1} 败走，{2} 控制了局面。",
                "{0} 奋勇当先，{1} 节节败退，{2} 大获全胜。"
            };

            string template = DeterministicRNG.Choose(turn, winner.ID, "victory_battle", victoryTemplates);
            return string.Format(template, winner.Leader?.Name ?? winner.Name, 
                                         loser.Leader?.Name ?? loser.Name, 
                                         winner.Name);
        }

        /// <summary>
        /// 生成攻城战斗日志
        /// </summary>
        public static string GenerateSiegeLog(GameObjects.Faction attacker, GameObjects.Architecture target, bool success, int turn = -1)
        {
            if (turn == -1)
                turn = DeterministicRNG.GetCurrentTurn();

            if (success)
            {
                var siegeSuccessTemplates = new[]
                {
                    "{0} 围攻 {1}，经过激烈攻防，终于破城而入。",
                    "{0} 大军围困 {1} 数日，城中粮尽援绝，守军投降。",
                    "{0} 用云梯攻城，{1} 城高墙厚，但最终还是被攻破。",
                    "{0} 昼夜不停攻城，{1} 守军疲惫不堪，城池失守。",
                    "{0} 挖掘地道，{1} 城墙轰然倒塌，守军溃散。"
                };

                string template = DeterministicRNG.Choose(turn, attacker.ID, "siege_success", siegeSuccessTemplates);
                return string.Format(template, attacker.Leader?.Name ?? attacker.Name, target.Name);
            }
            else
            {
                var siegeFailTemplates = new[]
                {
                    "{0} 强攻 {1} 不下，损兵折将，被迫撤军。",
                    "{0} 围攻 {1}，但城防坚固，久攻不克。",
                    "{0} 猛攻 {1}，守军顽强抵抗，攻城失败。",
                    "{0} 试图攻取 {1}，但遭到守军顽强反击，铩羽而归。",
                    "{0} 攻城不利，{1} 守备森严，只得暂时退兵。"
                };

                string template = DeterministicRNG.Choose(turn, attacker.ID, "siege_fail", siegeFailTemplates);
                return string.Format(template, attacker.Leader?.Name ?? attacker.Name, target.Name);
            }
        }

        /// <summary>
        /// 生成野战日志
        /// </summary>
        public static string GenerateFieldBattleLog(GameObjects.Faction winner, GameObjects.Faction loser, string location, int turn = -1)
        {
            if (turn == -1)
                turn = DeterministicRNG.GetCurrentTurn();

            var fieldBattleTemplates = new[]
            {
                "{0} 与 {1} 在 {2} 遭遇，经过激战，{0} 获得胜利。",
                "{0} 在 {2} 设伏，{1} 中计，大败而逃。",
                "{0} 与 {1} 在 {2} 展开决战，{0} 技高一筹。",
                "{0} 率军在 {2} 迎击 {1}，经过苦战终获胜利。",
                "{0} 与 {1} 在 {2} 相遇，双方摆开阵势，{0} 略胜一筹。"
            };

            string template = DeterministicRNG.Choose(turn, winner.ID, "field_battle", fieldBattleTemplates);
            return string.Format(template, winner.Leader?.Name ?? winner.Name, 
                                         loser.Leader?.Name ?? loser.Name, 
                                         location);
        }

        /// <summary>
        /// 生成外交事件日志
        /// </summary>
        public static string GenerateDiplomacyLog(GameObjects.Faction initiator, GameObjects.Faction target, string actionType, bool success, int turn = -1)
        {
            if (turn == -1)
                turn = DeterministicRNG.GetCurrentTurn();

            var diplomacyTemplates = new Dictionary<string, string[]>
            {
                ["alliance_success"] = new[]
                {
                    "{0} 派遣使者与 {1} 结盟，双方一拍即合。",
                    "{0} 与 {1} 歃血为盟，共同对抗强敌。",
                    "{0} 诚意满满，{1} 欣然同意结盟。"
                },
                ["alliance_fail"] = new[]
                {
                    "{0} 提议结盟，但 {1} 婉言谢绝。",
                    "{0} 派遣使者求盟，{1} 态度冷淡，结盟失败。",
                    "{0} 的结盟提议被 {1} 拒绝。"
                },
                ["trade_success"] = new[]
                {
                    "{0} 与 {1} 达成贸易协定，互通有无。",
                    "{0} 商队与 {1} 建立贸易关系，双方获利。",
                    "{0} 与 {1} 签署通商条约。"
                },
                ["peace_success"] = new[]
                {
                    "{0} 与 {1} 握手言和，战争结束。",
                    "{0} 派遣使者求和，{1} 同意停战。",
                    "{0} 与 {1} 签署和平协议。"
                }
            };

            string key = $"{actionType}_{(success ? "success" : "fail")}";
            if (diplomacyTemplates.ContainsKey(key))
            {
                string template = DeterministicRNG.Choose(turn, initiator.ID, key, diplomacyTemplates[key]);
                return string.Format(template, initiator.Name, target.Name);
            }

            return $"{initiator.Name} 与 {target.Name} 进行了外交接触。";
        }

        /// <summary>
        /// 生成招募事件日志
        /// </summary>
        public static string GenerateRecruitmentLog(GameObjects.Faction faction, GameObjects.Person person, bool success, int turn = -1)
        {
            if (turn == -1)
                turn = DeterministicRNG.GetCurrentTurn();

            if (success)
            {
                var recruitSuccessTemplates = new[]
                {
                    "{0} 三顾茅庐，终于请得 {1} 出山相助。",
                    "{0} 诚意感动了 {1}，{1} 决定投靠 {2}。",
                    "{0} 慧眼识珠，成功招揽了贤才 {1}。",
                    "{0} 亲自登门拜访，{1} 被其诚意打动，加入 {2}。",
                    "{0} 求贤若渴，{1} 欣然应允，共图大业。"
                };

                string template = DeterministicRNG.Choose(turn, faction.ID, "recruit_success", recruitSuccessTemplates);
                return string.Format(template, faction.Leader?.Name ?? faction.Name, person.Name, faction.Name);
            }
            else
            {
                var recruitFailTemplates = new[]
                {
                    "{0} 多次拜访 {1}，但 {1} 志不在此，婉言谢绝。",
                    "{0} 欲招揽 {1}，但 {1} 心有所属，拒绝了邀请。",
                    "{0} 求贤心切，但 {1} 无意出仕，招募失败。",
                    "{0} 派人游说 {1}，但 {1} 意志坚定，不为所动。",
                    "{0} 礼贤下士，但 {1} 另有打算，未能成功。"
                };

                string template = DeterministicRNG.Choose(turn, faction.ID, "recruit_fail", recruitFailTemplates);
                return string.Format(template, faction.Leader?.Name ?? faction.Name, person.Name);
            }
        }
    }
}