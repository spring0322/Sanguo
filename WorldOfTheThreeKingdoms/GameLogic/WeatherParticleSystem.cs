using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms.GameObjects;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameLogic;

/// <summary>
/// 天气粒子系统（支持风向风力影响）
/// 日期：2026-03-10
/// </summary>
public class WeatherParticleSystem
{
    private const int MAX_PARTICLES = 600;  // 🔥 由 3000 降低至五分之一
    private readonly WeatherParticle[] _particles = new WeatherParticle[MAX_PARTICLES];
    
    // 预加载的一张极小的纯白贴图（1x1），用于拉伸成雨滴或雪花
    private readonly Texture2D _pixelTexture;
    private readonly Random _rng = new();

    // 平滑过渡变量
    private WeatherType _currentScreenWeather = WeatherType.Sunny;

    // 🔥 HOT PATH 优化：预缓存风向向量，避免每帧分配
    private static readonly Vector2 WindNorth = new(0, 1);
    private static readonly Vector2 WindSouth = new(0, -1);
    private static readonly Vector2 WindEast = new(-1, 0);
    private static readonly Vector2 WindWest = new(1, 0);
    private static readonly Vector2 WindSouthEast = new(-1, -1);
    private static readonly Vector2 WindSouthWest = new(1, -1);
    private static readonly Vector2 WindNorthEast = new(-1, 1);
    private static readonly Vector2 WindNorthWest = new(1, 1);

    // 🔥 HOT PATH 优化：预缓存绘制缩放向量，避免每次绘制分配
    private static readonly Vector2 RainScale = new(1.5f, 20f);
    private static readonly Vector2 SnowScaleBase = new(3f, 3f);

    public WeatherParticleSystem(Texture2D pixelTexture)
    {
        _pixelTexture = pixelTexture ?? throw new ArgumentNullException(nameof(pixelTexture));
        
        // 预先用 AOT 友好的方式清空池子
        Array.Clear(_particles, 0, _particles.Length);
    }

    /// <summary>
    /// 每帧更新逻辑
    /// 🔥 HOT PATH：每帧调用，必须高性能
    /// </summary>
    public void Update(GameTime gameTime, Point focalMapPosition, int screenWidth, int screenHeight)
    {
        // 1. 获取当前焦点所在区域的真实天气和风况
        var targetWeather = Session.Current.Scenario.WeatherManager.GetWeatherAt(focalMapPosition);
        var wind = Session.Current.Scenario.WeatherManager.GetWindAt(focalMapPosition);

        // 2. 状态机过渡逻辑
        if (_currentScreenWeather != targetWeather)
        {
            // 切换天气，重置过渡状态
            _currentScreenWeather = targetWeather;
        }

        // 3. 动态发射新粒子（仅当需要时）
        EmitParticles(_currentScreenWeather, screenWidth);

        // 4. 🔥 将风向转换为 2D 向量（C# 12 极速查表，使用预缓存向量避免分配）
        Vector2 baseWindVector = wind.Direction switch
        {
            WindDirection.North => WindNorth,       // 北风往南吹（Y 增加）
            WindDirection.South => WindSouth,       // 南风往北吹（Y 减少）
            WindDirection.East => WindEast,         // 东风往西吹（X 减少）
            WindDirection.West => WindWest,         // 西风往东吹（X 增加）
            WindDirection.SouthEast => WindSouthEast, // 东南风往西北吹
            WindDirection.SouthWest => WindSouthWest, // 西南风往西南吹
            WindDirection.NorthEast => WindNorthEast, // 东北风往西南吹
            WindDirection.NorthWest => WindNorthWest, // 西北风往东南吹
            _ => Vector2.Zero                         // 无风
        };

        // 5. 结合风力等级计算最终横向偏移力
        // 假设 1 级风横向偏移 100 像素/秒，3 级风偏移 400 像素/秒
        float windPower = wind.Force switch
        {
            WindForce.Breeze => 100f,  // 微风：100 像素/秒
            WindForce.Strong => 250f,  // 大风：250 像素/秒
            WindForce.Gale => 400f,    // 狂风：400 像素/秒
            _ => 0f                    // 无风
        };

        Vector2 appliedWindForce = baseWindVector * windPower;

        // 6. 🔥 更新现有所有粒子的物理状态（极致性能的热路径）
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            // 🔥 使用 ref 直接操作数组内存，避免 struct 的值拷贝
            ref var p = ref _particles[i];
            if (!p.IsActive) continue;

            // 🔥 核心物理：自身下落速度 + 环境风力干预
            Vector2 finalVelocity = p.Velocity + appliedWindForce;
            p.Position += finalVelocity * dt;

            // 简单的销毁逻辑：掉出屏幕底部或左右边界则回收
            if (p.Position.Y > screenHeight || p.Position.X < -50 || p.Position.X > screenWidth + 50)
            {
                p.IsActive = false;
            }
        }
    }

    /// <summary>
    /// 根据当前天气决定发射哪种粒子
    /// </summary>
    private void EmitParticles(WeatherType weather, int screenWidth)
    {
        // 晴天/阴天/雾天不需要下落粒子（雾天用 Shader 滤镜实现）
        if (weather == WeatherType.Sunny || weather == WeatherType.Cloudy || weather == WeatherType.Fog)
            return;

        // 决定每帧发射的数量（修改为原先的五分之一）
        int emitCount = weather == WeatherType.Rain ? 3 : 1;

        for (int i = 0; i < MAX_PARTICLES && emitCount > 0; i++)
        {
            ref var p = ref _particles[i];
            if (!p.IsActive)
            {
                // 激活并初始化一个空闲粒子
                p.IsActive = true;
                p.Position = new Vector2(_rng.Next(0, screenWidth), -10); // 从屏幕顶部随机位置生成

                if (weather == WeatherType.Rain)
                {
                    // 雨滴：速度极快，略带倾斜，半透明蓝色
                    p.Velocity = new Vector2(_rng.Next(-50, 50), _rng.Next(600, 900));
                    p.Color = new Color(150, 200, 255, 180);
                    p.Scale = 1.0f; // 细长型由后续 Draw 时的缩放矩阵决定
                }
                else if (weather == WeatherType.Snow)
                {
                    // 雪花：速度慢，随机飘动，纯白色
                    p.Velocity = new Vector2(_rng.Next(-100, 100), _rng.Next(100, 250));
                    p.Color = Color.White;
                    p.Scale = (float)(_rng.NextDouble() * 1.5 + 0.5);
                }

                emitCount--;
            }
        }
    }

    /// <summary>
    /// 渲染粒子
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            ref var p = ref _particles[i];
            if (!p.IsActive) continue;

            if (_currentScreenWeather == WeatherType.Rain)
            {
                // 雨滴被拉长（X 宽 1.5，Y 高 20）
                spriteBatch.Draw(
                    _pixelTexture,
                    p.Position,
                    null,
                    p.Color,
                    0f,
                    Vector2.Zero,
                    RainScale,  // 🔥 使用预缓存向量，避免分配
                    SpriteEffects.None,
                    0f);
            }
            else if (_currentScreenWeather == WeatherType.Snow)
            {
                // 雪花稍微呈现方块或使用你提供的雪花贴图
                // 🔥 注意：这里仍需要计算，但可以优化为直接乘法
                Vector2 snowScale = SnowScaleBase * p.Scale;
                spriteBatch.Draw(
                    _pixelTexture,
                    p.Position,
                    null,
                    p.Color,
                    0f,
                    Vector2.Zero,
                    snowScale,
                    SpriteEffects.None,
                    0f);
            }
        }
    }
}
