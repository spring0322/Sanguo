using Microsoft.Xna.Framework;
using GameObjects;
using WorldOfTheThreeKingdoms.GameGlobal;

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
        public static void PlaySound(this Troop troop, string soundName, float volume = 1.0f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(soundName);
            }
        }

        /// <summary>
        /// 为建筑播放音效
        /// </summary>
        public static void PlaySound(this Architecture architecture, string soundName, float volume = 1.0f)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(soundName);
            }
        }

        /// <summary>
        /// 播放战斗音效
        /// </summary>
        public static void PlayCombatSound(this Troop attacker, Troop defender, string soundName)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(soundName);
            }
        }

        /// <summary>
        /// 根据部队类型播放行军音效
        /// </summary>
        public static void PlayMarchSound(this Troop troop)
        {
            if (AudioManager.Instance != null && troop.Army != null)
            {
                string soundName = "March";
                AudioManager.Instance.PlaySound(soundName);
            }
        }

        /// <summary>
        /// 为势力播放对应的战斗状态音乐
        /// </summary>
        public static void PlayBattleStateMusic(this Faction faction)
        {
            if (AudioManager.Instance != null && faction.BattleState != null)
            {
                AudioManager.Instance.PlayBattleMusic(faction.BattleState);
            }
        }

        /// <summary>
        /// 播放当前季节音乐
        /// </summary>
        public static void PlayCurrentSeasonMusic(this GameDate date)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySeasonMusic(date.Season);
            }
        }

        /// <summary>
        /// 为场景播放对应音乐
        /// </summary>
        public static void PlaySceneMusic(this object context, AudioManager.MusicScene scene)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySceneMusic(scene);
            }
        }

        /// <summary>
        /// 智能播放音乐：根据当前游戏状态自动选择合适的音乐
        /// </summary>
        public static void PlayContextualMusic()
        {
            if (AudioManager.Instance == null || Session.Current?.Scenario == null) 
                return;

            var currentPlayer = Session.Current.Scenario.CurrentPlayer;
            if (currentPlayer != null && currentPlayer.BattleState != ZhandouZhuangtai.和平)
            {
                // 如果处于战斗状态，播放战斗音乐
                AudioManager.Instance.PlayBattleMusic(currentPlayer.BattleState);
            }
            else if (Session.Current.Scenario.Date != null)
            {
                // 和平状态下播放季节音乐
                AudioManager.Instance.PlaySeasonMusic(Session.Current.Scenario.Date.Season);
            }
        }

        /// <summary>
        /// 播放UI音效的便捷方法
        /// </summary>
        public static void PlayUISound(string soundName)
        {
            AudioManager.Instance?.PlaySound($"UI/{soundName}");
        }

        /// <summary>
        /// 播放战斗音效的便捷方法
        /// </summary>
        public static void PlayBattleSound(string soundName)
        {
            AudioManager.Instance?.PlaySound($"Battle/{soundName}");
        }

        /// <summary>
        /// 播放环境音效的便捷方法
        /// </summary>
        public static void PlayAmbientSound(string soundName)
        {
            AudioManager.Instance?.PlaySound($"Ambient/{soundName}");
        }
    }
}