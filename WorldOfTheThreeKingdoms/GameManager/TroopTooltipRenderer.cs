using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using GameObjects;
using GameObjects.TroopDetail;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// 部队信息悬停显示渲染器
    /// </summary>
    public class TroopTooltipRenderer
    {
        // --------------------------------------------------------
        // 配置参数 (可以在这里调整UI的大小和颜色)
        // --------------------------------------------------------
        private const int AVATAR_SIZE = 120;       // 左侧头像大小
        private const int PADDING = 15;            // 内边距
        private const int LINE_HEIGHT = 24;        // 文字行高
        private const int TEXT_AREA_WIDTH = 140;   // 右侧文字区域宽度

        // 颜色配置
        private readonly Color _bgColor = new Color(30, 40, 50, 230); // 深青灰色半透明背景
        private readonly Color _borderColor = new Color(180, 190, 200); // 银灰色边框
        private readonly Color _labelColor = new Color(220, 220, 220);  // 标签颜色 (白色偏灰)
        private readonly Color _valueColor = Color.White;               // 数值颜色
        private readonly Color _titleColor = new Color(255, 165, 0);    // 称号颜色 (橙金色)
        private readonly Color _nameColor = new Color(100, 200, 255);   // 顶部部队名颜色 (天蓝色)

        // 缓存纹理
        private static Texture2D _pixelTexture;

        /// <summary>
        /// 获取1x1白点纹理 (用于画矩形)
        /// </summary>
        private Texture2D GetPixel(GraphicsDevice device)
        {
            try
            {
                if (_pixelTexture == null || _pixelTexture.IsDisposed)
                {
                    _pixelTexture = new Texture2D(device, 1, 1);
                    _pixelTexture.SetData(new[] { Color.White });
                }
                return _pixelTexture;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TroopTooltipRenderer] 创建像素纹理失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 主绘制方法：绘制宽屏信息面板
        /// </summary>
        public void DrawWideTooltip(SpriteBatch sb, Point mousePos, Troop troop, Texture2D bigPortrait, SpriteFont font)
        {
            if (troop?.Leader == null || sb == null || font == null) return;

            try
            {
                // 1. 准备数据
                string name = troop.DisplayName; // 使用老系统的DisplayName属性
                string type = troop.KindString; // 使用老系统的KindString属性
                string faction = troop.FactionString; // 使用老系统的FactionString属性
                Color factionColor = troop.BelongedFaction?.FactionColor ?? Color.Gray;
                string title = troop.CombatTitleString; // 使用老系统的CombatTitleString属性
                string count = troop.Quantity.ToString();
                string morale = troop.Morale.ToString(); // 直接使用Morale属性
                string combat = troop.Combativity.ToString(); // 直接使用Combativity属性
                string status = troop.DisplayStatus; // 使用老系统的DisplayStatus属性

                // 2. 计算尺寸
                // 宽度 = 左边距 + 头像 + 间距 + 文字区 + 右边距
                int totalWidth = PADDING + AVATAR_SIZE + PADDING + TEXT_AREA_WIDTH + PADDING;
                // 高度 = 标题行 + 7行数据 (兵种,势力,称号,人数,士气,战意,状态)
                int contentHeight = 30 + (7 * LINE_HEIGHT); // 确保高度至少能包住头像
                int totalHeight = Math.Max(contentHeight + PADDING * 2, AVATAR_SIZE + PADDING * 2);

                // 3. 确定面板位置 (在鼠标右下方)
                Rectangle panelRect = new Rectangle(mousePos.X + 24, mousePos.Y + 24, totalWidth, totalHeight);

                // 屏幕边界检测 (防止画出屏幕)
                var viewport = sb.GraphicsDevice.Viewport;
                if (panelRect.Right > viewport.Width)
                    panelRect.X = mousePos.X - totalWidth - 10;
                if (panelRect.Bottom > viewport.Height)
                    panelRect.Y = viewport.Height - totalHeight - 10;

                Texture2D pixel = GetPixel(sb.GraphicsDevice);
                if (pixel == null) return; // 如果无法创建像素纹理，跳过绘制

                // =========================================================
                // 开始绘制
                // =========================================================
                // [背景] 深色底板
                sb.Draw(pixel, panelRect, _bgColor);

                // [边框] 绘制古典双层边框
                // 外框 (细线)
                DrawHollowRect(sb, panelRect, pixel, _borderColor, 1);
                // 内框 (装饰线，向内缩3像素)
                Rectangle innerBorder = new Rectangle(panelRect.X + 3, panelRect.Y + 3, panelRect.Width - 6, panelRect.Height - 6);
                DrawHollowRect(sb, innerBorder, pixel, _borderColor * 0.5f, 1);

                // [装饰] 在标题下方画一条横线
                int headerY = panelRect.Y + PADDING + 30;
                sb.Draw(pixel, new Rectangle(panelRect.X + 5, headerY, panelRect.Width - 10, 1), _borderColor * 0.3f);

                // ---------------------------------------------------------
                // 左侧：头像区域
                // ---------------------------------------------------------
                Rectangle portraitRect = new Rectangle(panelRect.X + PADDING,
                    panelRect.Y + PADDING + 35, // 下移一点，避开顶部标题栏
                    AVATAR_SIZE,
                    AVATAR_SIZE);

                // 头像底色 (半透明黑)
                sb.Draw(pixel, portraitRect, new Color(0, 0, 0, 100));

                // 绘制头像
                if (bigPortrait != null && !bigPortrait.IsDisposed)
                {
                    sb.Draw(bigPortrait, portraitRect, Color.White);
                }

                // 头像边框
                DrawHollowRect(sb, portraitRect, pixel, Color.Gray, 1);

                // ---------------------------------------------------------
                // 右侧：文字列表
                // ---------------------------------------------------------
                int textX = panelRect.X + PADDING + AVATAR_SIZE + PADDING; // 文字起始X
                int startY = panelRect.Y + PADDING;

                // 1. 顶部大标题 (部队名)
                // 居中显示在右侧文字区上方
                Vector2 titleSize = font.MeasureString(name);
                Vector2 titlePos = new Vector2(textX + (TEXT_AREA_WIDTH - titleSize.X) / 2, startY);
                sb.DrawString(font, name, titlePos, _nameColor);

                // 2. 绘制属性行
                // 从分割线下方开始
                int currentY = headerY + 10;

                // 辅助函数：绘制一行 "标签 + 值"
                void DrawRow(string label, string val, Color valColor, bool drawSquare = false, Color squareColor = default)
                {
                    try
                    {
                        // 绘制标签 (左对齐)
                        sb.DrawString(font, label, new Vector2(textX, currentY), _labelColor);

                        // 绘制值 (固定偏移)
                        int valueOffsetX = 60;
                        Vector2 valPos = new Vector2(textX + valueOffsetX, currentY);

                        // 特殊处理：如果是势力，先画个小色块
                        if (drawSquare)
                        {
                            Rectangle colorBox = new Rectangle((int)valPos.X, currentY + 4, 12, 12);
                            sb.Draw(pixel, colorBox, squareColor);
                            sb.DrawString(font, val, valPos + new Vector2(16, 0), valColor); // 文字右移
                        }
                        else
                        {
                            sb.DrawString(font, val, valPos, valColor);
                        }

                        currentY += LINE_HEIGHT;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TroopTooltipRenderer] 绘制行时出错: {ex.Message}");
                        currentY += LINE_HEIGHT; // 确保继续绘制下一行
                    }
                }

                // --- 数据列表 ---
                DrawRow("兵种", type, _valueColor);
                DrawRow("势力", faction, _valueColor, true, factionColor); // 对应截图中的蓝色小方块
                DrawRow("称号", title, _titleColor); // 称号用橙色
                DrawRow("人数", count, _valueColor); // 可追加 "↑" 箭头
                DrawRow("士气", morale, Color.Yellow); // 士气高通常用黄色或绿色
                DrawRow("战意", combat, Color.Cyan);
                DrawRow("状态", status, Color.White);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TroopTooltipRenderer] 绘制部队信息时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 辅助方法：绘制空心矩形 (边框)
        /// </summary>
        private void DrawHollowRect(SpriteBatch sb, Rectangle rect, Texture2D pixel, Color color, int thickness)
        {
            try
            {
                if (pixel == null || pixel.IsDisposed) return;

                // 上
                sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
                // 下
                sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
                // 左
                sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
                // 右
                sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TroopTooltipRenderer] 绘制边框时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Dispose()
        {
            try
            {
                if (_pixelTexture != null && !_pixelTexture.IsDisposed)
                {
                    _pixelTexture.Dispose();
                    _pixelTexture = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TroopTooltipRenderer] 清理资源时出错: {ex.Message}");
            }
        }
    }
}