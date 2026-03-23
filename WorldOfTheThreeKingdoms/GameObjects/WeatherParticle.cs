using Microsoft.Xna.Framework;

namespace WorldOfTheThreeKingdoms.GameObjects;

/// <summary>
/// 极度轻量的天气粒子结构体（仅占用几十字节内存）
/// 日期：2026-03-10
/// </summary>
public struct WeatherParticle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color Color;
    public float Scale;
    public bool IsActive;
}
