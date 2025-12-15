using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameManager
{
    /// <summary>
    /// 音频系统扩展方法 - 为游戏对象提供便捷的音效播放接口
    /// </summary>
    public static class AudioExtensions
    {
        /// <summary>
        /// 为部队播放音效
        /// </summary>
        public static void PlaySound(this Troop troop, string soundName, float volume = 1.0f, WorldOfTheThreeKingdoms.GameManager.AudioPriority priority = WorldOfTheThreeKingdoms.GameManager.AudioPriority.Normal)
        {
            if (AudioManager.Instance != null)
            {
                Vector2 position = new Vector2(troop.Position.X, troop.Position.Y);
                AudioManager.Instance.PlaySound(soundName, position, volume, priority);
            }
        }

        /// <summary>
        /// 为建筑播放音效
        /// </summary>
        public static void PlaySound(this Architecture architecture, string soundName, float volume = 1.0f, WorldOfTheThreeKingdoms.GameManager.AudioPriority priority = WorldOfTheThreeKingdoms.GameManager.AudioPriority.Normal)
        {
            if (AudioManager.Instance != null)
            {
                Vector2 position = new Vector2(architecture.ArchitectureArea.Centre.X, architecture.ArchitectureArea.Centre.Y);
                AudioManager.Instance.PlaySound(soundName, position, volume, priority);
            }
        }

        /// <summary>
        /// 在指定位置播放音效
        /// </summary>
        public static void PlaySoundAt(Vector2 position, string soundName, float volume = 1.0f, WorldOfTheThreeKingdoms.GameManager.AudioPriority priority = WorldOfTheThreeKingdoms.GameManager.AudioPriority.Normal)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(soundName, position, volume, priority);
            }
        }

        /// <summary>
        /// 播放战斗音效
        /// </summary>
        public static void PlayCombatSound(this Troop attacker, Troop defender, string soundName)
        {
            if (AudioManager.Instance != null)
            {
                // 在攻击者和防御者中间播放声音
                Vector2 attackerPos = new Vector2(attacker.Position.X, attacker.Position.Y);
                Vector2 defenderPos = new Vector2(defender.Position.X, defender.Position.Y);
                Vector2 midPoint = (attackerPos + defenderPos) / 2f;
                
                AudioManager.Instance.PlaySound(soundName, midPoint, 1.0f, WorldOfTheThreeKingdoms.GameManager.AudioPriority.High);
            }
        }

        /// <summary>
        /// 播放UI音效（不受距离影响）
        /// </summary>
        public static void PlayUISound(string soundName, float volume = 1.0f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUISound(soundName, volume);
            }
        }

        /// <summary>
        /// 根据部队类型播放行军音效
        /// </summary>
        public static void PlayMarchSound(this Troop troop)
        {
            if (AudioManager.Instance != null && troop.Army != null)
            {
                string soundName = "March"; // 默认行军声
                
                // 根据部队类型选择不同音效
                if (troop.Army.Kind != null)
                {
                    switch (troop.Army.Kind.Type)
                    {
                        case global::GameObjects.TroopDetail.MilitaryType.步兵:
                            soundName = "March_Infantry";
                            break;
                        case global::GameObjects.TroopDetail.MilitaryType.弩兵:
                            soundName = "March_Archer";
                            break;
                        case global::GameObjects.TroopDetail.MilitaryType.骑兵:
                            soundName = "March_Cavalry";
                            break;
                        case global::GameObjects.TroopDetail.MilitaryType.器械:
                            soundName = "March_Siege";
                            break;
                        case global::GameObjects.TroopDetail.MilitaryType.水军:
                            soundName = "March_Navy";
                            break;
                        default:
                            soundName = "March";
                            break;
                    }
                }
                
                troop.PlaySound(soundName, 0.7f, WorldOfTheThreeKingdoms.GameManager.AudioPriority.Low);
            }
        }

        /// <summary>
        /// 播放战斗结果音效
        /// </summary>
        public static void PlayBattleResultSound(this Troop winner, bool isVictory)
        {
            if (AudioManager.Instance != null)
            {
                string soundName = isVictory ? "Victory" : "Defeat";
                winner.PlaySound(soundName, 1.0f, WorldOfTheThreeKingdoms.GameManager.AudioPriority.High);
            }
        }

        /// <summary>
        /// 根据武器类型播放攻击音效
        /// </summary>
        public static void PlayAttackSound(this Troop attacker, Troop defender)
        {
            if (AudioManager.Instance != null)
            {
                string soundName = "Sword"; // 默认攻击声
                
                // 根据攻击类型选择音效
                if (attacker.ArrowOffence)
                {
                    soundName = "Arrow";
                }
                else if (attacker.Army?.Kind != null)
                {
                    switch (attacker.Army.Kind.Type)
                    {
                        case global::GameObjects.TroopDetail.MilitaryType.骑兵:
                            soundName = "Cavalry_Charge";
                            break;
                        case global::GameObjects.TroopDetail.MilitaryType.器械:
                            soundName = "Siege_Attack";
                            break;
                        case global::GameObjects.TroopDetail.MilitaryType.水军:
                            soundName = "Naval_Attack";
                            break;
                        default:
                            soundName = "Sword";
                            break;
                    }
                }
                
                attacker.PlayCombatSound(defender, soundName);
            }
        }
    }
}