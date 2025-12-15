// UI修复补丁 - 解决UI元素被压缩到左上角的问题
// 这个补丁强制设置所有UI元素的DrawScale为1，确保正确显示

using System;
using Microsoft.Xna.Framework;
using GamePanels;

namespace WorldOfTheThreeKingdoms.Fixes
{
    /// <summary>
    /// UI显示修复工具类
    /// 解决UI元素被压缩显示在左上角的问题
    /// </summary>
    public static class UIDisplayFix
    {
        /// <summary>
        /// 强制修复ButtonTexture的DrawScale
        /// </summary>
        /// <param name="button">要修复的按钮</param>
        public static void FixButtonDrawScale(ButtonTexture button)
        {
            if (button != null)
            {
                button.DrawScale = 1f;
            }
        }

        /// <summary>
        /// 强制修复CheckBox的DrawScale
        /// </summary>
        /// <param name="checkBox">要修复的复选框</param>
        public static void FixCheckBoxDrawScale(CheckBox checkBox)
        {
            if (checkBox != null)
            {
                checkBox.DrawScale = 1f;
            }
        }

        /// <summary>
        /// 批量修复ButtonTexture列表的DrawScale
        /// </summary>
        /// <param name="buttons">按钮列表</param>
        public static void FixButtonListDrawScale(System.Collections.Generic.List<ButtonTexture> buttons)
        {
            buttons?.ForEach(bt => FixButtonDrawScale(bt));
        }

        /// <summary>
        /// 批量修复CheckBox列表的DrawScale
        /// </summary>
        /// <param name="checkBoxes">复选框列表</param>
        public static void FixCheckBoxListDrawScale(System.Collections.Generic.List<CheckBox> checkBoxes)
        {
            checkBoxes?.ForEach(cb => FixCheckBoxDrawScale(cb));
        }

        /// <summary>
        /// 输出UI调试信息
        /// </summary>
        public static void LogUIDebugInfo()
        {
            try
            {
                var viewport = Platforms.Platform.GraphicsDevice.Viewport;
                System.Diagnostics.Debug.WriteLine($"[UIDisplayFix] Viewport: {viewport}");
                System.Diagnostics.Debug.WriteLine($"[UIDisplayFix] CacheManager.Scale: {GameManager.CacheManager.Scale}");
                System.Diagnostics.Debug.WriteLine($"[UIDisplayFix] InputManager.Scale1: {GameManager.InputManager.Scale1}");
                System.Diagnostics.Debug.WriteLine($"[UIDisplayFix] InputManager.ScaleDraw: {GameManager.InputManager.ScaleDraw}");
                
                if (GameManager.Session.MainGame != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[UIDisplayFix] SpriteScale1: {GameManager.Session.MainGame.SpriteScale1}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIDisplayFix] 调试信息输出失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用完整的UI修复
        /// 在任何UI界面的Draw方法开头调用此方法
        /// </summary>
        public static void ApplyUIFix()
        {
            try
            {
                // 确保CacheManager使用正确的缩放
                GameManager.CacheManager.Scale = Vector2.One;
                
                // 确保InputManager使用正确的缩放
                GameManager.InputManager.ScaleDraw = Vector2.One;
                
                // 输出调试信息
                LogUIDebugInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UIDisplayFix] UI修复应用失败: {ex.Message}");
            }
        }
    }
}

// 使用示例：
// 在MainMenuScreen.cs的Draw方法开头添加：
// UIDisplayFix.ApplyUIFix();
// UIDisplayFix.FixButtonListDrawScale(btList);
// UIDisplayFix.FixCheckBoxListDrawScale(btScenarioSelectList);