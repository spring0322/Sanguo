using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WorldOfTheThreeKingdoms.GameGlobal;
using WorldOfTheThreeKingdoms.GameManager;

namespace GameManager
{
    /// <summary>
    /// AI调试渲染器 - 可视化影响力地图和路径
    /// </summary>
    public class AIDebugRenderer
    {
        private static Texture2D _pixel;
        private static SpriteFont _debugFont;
        private static bool _isInitialized = false;

        // 默认参数
        private const float MaxThreatValue = 100f;
        private const float MinInfluenceToShow = 0.1f;
        private const float TileOpacity = 0.3f;

        /// <summary>
        /// 初始化调试渲染器
        /// </summary>
        public static void Initialize(GraphicsDevice graphicsDevice, SpriteFont font = null)
        {
            if (graphicsDevice == null) return;

            try
            {
                _pixel = new Texture2D(graphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
                _debugFont = font;
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDebugRenderer] 初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制影响力地图和路径
        /// </summary>
        public static void Draw(SpriteBatch spriteBatch, InfluenceMap map, List<Point> currentPath, Point mapScrollOffset, int tileSize)
        {
            if (!_isInitialized || _pixel == null || spriteBatch == null) return;

            try
            {
                // 1. 绘制影响力地图 (热力图)
                DrawInfluenceMap(spriteBatch, map, mapScrollOffset, tileSize);

                // 2. 绘制当前路径
                DrawPath(spriteBatch, currentPath, mapScrollOffset, tileSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDebugRenderer] Draw 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制影响力地图热力图
        /// </summary>
        private static void DrawInfluenceMap(SpriteBatch spriteBatch, InfluenceMap map, Point mapScrollOffset, int tileSize)
        {
            if (map == null) return;

            int mapWidth = map.Width;
            int mapHeight = map.Height;

            // 优化：只绘制屏幕范围内的格子
            int startX = Math.Max(0, mapScrollOffset.X / tileSize - 1);
            int startY = Math.Max(0, mapScrollOffset.Y / tileSize - 1);
            int endX = Math.Min(mapWidth, startX + 40); // 假设屏幕最多显示40格
            int endY = Math.Min(mapHeight, startY + 30);

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    float influence = map.GetInfluence(x, y);
                    
                    // 忽略低影响区域
                    if (Math.Abs(influence) < MinInfluenceToShow) continue;

                    // 颜色映射：绿色(友军/安全) -> 红色(敌军/危险)
                    Color color;
                    if (influence > 0)
                    {
                        // 正值 = 友军区域 = 绿色
                        float ratio = MathHelper.Clamp(influence / MaxThreatValue, 0f, 1f);
                        color = Color.Lerp(Color.DarkGreen, Color.LightGreen, ratio) * TileOpacity;
                    }
                    else
                    {
                        // 负值 = 敌军区域 = 红色
                        float ratio = MathHelper.Clamp(-influence / MaxThreatValue, 0f, 1f);
                        color = Color.Lerp(Color.DarkRed, Color.Red, ratio) * TileOpacity;
                    }

                    Rectangle rect = new Rectangle(
                        x * tileSize - mapScrollOffset.X,
                        y * tileSize - mapScrollOffset.Y,
                        tileSize,
                        tileSize);

                    spriteBatch.Draw(_pixel, rect, color);
                }
            }
        }

        /// <summary>
        /// 绘制路径
        /// </summary>
        private static void DrawPath(SpriteBatch spriteBatch, List<Point> path, Point mapScrollOffset, int tileSize)
        {
            if (path == null || path.Count < 2) return;

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 start = new Vector2(
                    path[i].X * tileSize + tileSize / 2 - mapScrollOffset.X,
                    path[i].Y * tileSize + tileSize / 2 - mapScrollOffset.Y);

                Vector2 end = new Vector2(
                    path[i + 1].X * tileSize + tileSize / 2 - mapScrollOffset.X,
                    path[i + 1].Y * tileSize + tileSize / 2 - mapScrollOffset.Y);

                DrawLine(spriteBatch, start, end, Color.Yellow, 2);
            }

            // 绘制起点和终点标记
            if (path.Count > 0)
            {
                // 起点 - 蓝色圆点
                DrawPoint(spriteBatch, path[0], mapScrollOffset, tileSize, Color.Blue);
                
                // 终点 - 红色圆点
                DrawPoint(spriteBatch, path[path.Count - 1], mapScrollOffset, tileSize, Color.Red);
            }
        }

        /// <summary>
        /// 绘制点标记
        /// </summary>
        private static void DrawPoint(SpriteBatch spriteBatch, Point point, Point mapScrollOffset, int tileSize, Color color)
        {
            int markerSize = tileSize / 3;
            Rectangle rect = new Rectangle(
                point.X * tileSize + tileSize / 2 - markerSize / 2 - mapScrollOffset.X,
                point.Y * tileSize + tileSize / 2 - markerSize / 2 - mapScrollOffset.Y,
                markerSize,
                markerSize);

            spriteBatch.Draw(_pixel, rect, color);
        }

        /// <summary>
        /// 绘制线段
        /// </summary>
        private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            
            spriteBatch.Draw(_pixel,
                new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness),
                null,
                color,
                angle,
                new Vector2(0, 0.5f),
                SpriteEffects.None,
                0);
        }

        /// <summary>
        /// 绘制调试文本
        /// </summary>
        public static void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color)
        {
            if (_debugFont == null || spriteBatch == null) return;

            try
            {
                spriteBatch.DrawString(_debugFont, text, position, color);
            }
            catch
            {
                // 忽略字体绘制错误
            }
        }

        /// <summary>
        /// 检查是否已初始化
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// 绘制调试日志
        /// </summary>
        public static void DrawLogs(SpriteBatch spriteBatch, Vector2 position, Color color)
        {
            if (spriteBatch == null) return;

            try
            {
                var logs = AIDebugger.GetLogs();
                if (logs == null || logs.Count == 0) return;
                
                Vector2 currentPos = position;
                float lineHeight = 20f; // 默认行高
                
                // 尝试使用Session.Current.Font计算行高
                if (Session.Current?.Font != null)
                {
                    try
                    {
                        lineHeight = Session.Current.Font.MeasureString("A").Y;
                    }
                    catch { lineHeight = 20f; }
                }

                foreach (var log in logs)
                {
                    // 使用CacheManager绘制文本（与游戏其他部分一致）
                    CacheManager.DrawString(Session.Current.Font, log, currentPos, color, 0f, Vector2.Zero, 0.6f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0.01f);
                    currentPos.Y += lineHeight * 0.6f;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIDebugRenderer] DrawLogs 错误: {ex.Message}");
            }
        }
    }
}
