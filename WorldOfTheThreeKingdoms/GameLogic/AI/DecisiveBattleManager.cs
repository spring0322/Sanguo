using System;
using Zhsan.GameLogic.Config;
using WorldOfTheThreeKingdoms.GameGlobal;
using GameManager; // Session is in GameManager namespace

namespace Zhsan.GameLogic.AI
{
    public enum WarState
    {
        Normal,
        Aggressive,
        AllOutWar
    }

    public class DecisiveBattleManager
    {
        private int _peacefulTurnCounter = 0;

        public DecisiveBattleManager()
        {
        }

        public void OnTurnEnd(bool hasBattleOccurred)
        {
            if (hasBattleOccurred) _peacefulTurnCounter = 0;
            else _peacefulTurnCounter++;
        }

        public WarState DetermineWarState(int currentYear, float resourceRatio)
        {
            // 获取最新配置
            var cfg = ConfigManager.AI;

            // Starup Protection: Check if game is in protection period
            // Session.Current.Scenario.DaySince is the number of days since scenario start
            if (Session.Current != null && Session.Current.Scenario != null)
            {
                if (Session.Current.Scenario.DaySince < cfg.StartupProtectionYears * 365)
                {
                    return WarState.Normal;
                }
            }

            if (currentYear >= cfg.GlobalTotalWarYear)
                return WarState.AllOutWar;

            if (resourceRatio < cfg.DesperationResourceThreshold)
                return WarState.AllOutWar;

            if (_peacefulTurnCounter >= cfg.MaxStalemateTurns)
                return WarState.Aggressive;

            return WarState.Normal;
        }

        public float GetAdjustedAttackThreshold(float standardThreshold, WarState state)
        {
            var cfg = ConfigManager.AI; // 获取最新配置

            switch (state)
            {
                case WarState.Aggressive:
                    return Math.Max(cfg.AggressiveAttackThreshold, standardThreshold * 0.7f);

                case WarState.AllOutWar:
                    return cfg.AllOutWarAttackThreshold;

                default:
                    return standardThreshold;
            }
        }
    }
}
