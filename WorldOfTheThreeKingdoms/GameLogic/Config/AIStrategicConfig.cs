namespace Zhsan.GameLogic.Config
{
    public class AIStrategicConfig
    {
        // 触发全面战争的年份 (例如 220)
        public int GlobalTotalWarYear { get; set; } = 220;

        // 绝望状态下的资源阈值 (0.0 - 1.0)
        public float DesperationResourceThreshold { get; set; } = 0.3f;

        // 僵持状态最大回合数
        public int MaxStalemateTurns { get; set; } = 12;

        // 激进状态下的攻击阈值
        public float AggressiveAttackThreshold { get; set; } = 0.8f;

        // 全面战争状态下的攻击阈值
        public float AllOutWarAttackThreshold { get; set; } = 0.5f;

        // 开局保护年数 (游戏开始多少年内不触发此模式)
        public int StartupProtectionYears { get; set; } = 1;
    }
}
