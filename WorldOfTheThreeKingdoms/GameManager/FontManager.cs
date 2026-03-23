using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Platforms;

namespace GameManager
{
    /// <summary>
    /// Font rendering configuration for FontStashSharp compatibility with GDI+ style text
    /// </summary>
    public static class FontRenderConfig
    {
        /// <summary>
        /// Font size scale multiplier to match GDI+ visual size (GDI+ Points -> FontStashSharp Pixels)
        /// </summary>
        public static float FontScaleMultiplier { get; set; } = 1.2f;
        
        /// <summary>
        /// Global Y-offset to fix vertical alignment (FontStashSharp renders tighter than System.Drawing)
        /// Positive values shift text DOWN
        /// </summary>
        public static float GlobalYOffset { get; set; } = 2.0f;
        
        /// <summary>
        /// Shadow offset in pixels (creates drop shadow effect for readability)
        /// </summary>
        public static Vector2 ShadowOffset { get; set; } = new Vector2(1, 1);
        
        /// <summary>
        /// Shadow color (black with high opacity for GDI+ style drop shadow)
        /// </summary>
        public static Color ShadowColor { get; set; } = new Color(0, 0, 0, 204); // 0.8f opacity
        
        /// <summary>
        /// Enable/disable drop shadow globally
        /// </summary>
        public static bool EnableShadow { get; set; } = true;
        
        /// <summary>
        /// Global font scale (defaults to 1.0f to prevent invisible text)
        /// </summary>
        public static float GlobalFontScale { get; set; } = 1.0f;
    }

    public class FontManager
    {
        private static FontManager _instance;
        public static FontManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FontManager();
                }
                return _instance;
            }
        }

        private FontSystem _fontSystem;
        private DynamicSpriteFont _defaultFont;
        private string _fontPath;
        private bool _hasFontSource = false; // Track if we have added any fonts

        private FontManager()
        {
            _fontSystem = new FontSystem();
        }

        public void LoadFont(string path, int size)
        {
            try
            {
                _fontPath = path;
                var bytes = Platform.Current.LoadFile(path);
                
                // Fallback: try direct file read if Platform load fails
                if (bytes == null || bytes.Length == 0)
                {
                    string absolutePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                    if (File.Exists(absolutePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[FontManager] Platform load failed, trying direct file read: {absolutePath}");
                        bytes = File.ReadAllBytes(absolutePath);
                    }
                    else if (File.Exists(path))
                    {
                         System.Diagnostics.Debug.WriteLine($"[FontManager] Platform load failed, trying direct file read (relative): {path}");
                         bytes = File.ReadAllBytes(path);
                    }
                }

                if (bytes != null && bytes.Length > 0)
                {
                    _fontSystem.AddFont(bytes);
                    _hasFontSource = true; // Mark as having font source
                    // Apply font scale multiplier when getting the font
                    int scaledSize = (int)(size * FontRenderConfig.FontScaleMultiplier);
                    _defaultFont = _fontSystem.GetFont(scaledSize);
                    System.Diagnostics.Debug.WriteLine($"[FontManager] Loaded font: {path}, Original Size: {size}, Scaled: {scaledSize}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[FontManager] Failed to load font: {path}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FontManager] Error loading font: {ex.Message}");
            }
        }

        public void LoadFont(byte[] data, int size)
        {
            try
            {
                if (data != null && data.Length > 0)
                {
                    _fontSystem.AddFont(data);
                    _hasFontSource = true; // Mark as having font source
                    int scaledSize = (int)(size * FontRenderConfig.FontScaleMultiplier);
                    _defaultFont = _fontSystem.GetFont(scaledSize);
                    System.Diagnostics.Debug.WriteLine($"[FontManager] Loaded font from byte array, Original Size: {size}, Scaled: {scaledSize}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[FontManager] Failed to load font: byte array is null or empty");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FontManager] Error loading font from bytes: {ex.Message}");
            }
        }

        public DynamicSpriteFont GetFont(int size)
        {
            if (_fontSystem == null || !_hasFontSource) return null; // Check _hasFontSource to prevent exception
            try
            {
                // 🔧 Scale font size by FontScaleMultiplier to convert from GDI+ Points to FontStashSharp Pixels
                int scaledSize = (int)(size * FontRenderConfig.FontScaleMultiplier);
                return _fontSystem.GetFont(scaledSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FontManager] GetFont failed (probably no font sources): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Draw string with configurable drop shadow for readability
        /// </summary>
        public void DrawString(SpriteBatch batch, string text, Vector2 pos, Color color, float scale = 1f, float depth = 0f, bool drawShadow = true)
        {
            if (_defaultFont == null || batch == null || string.IsNullOrEmpty(text)) return;

            // Apply global font scale
            float finalScale = scale * FontRenderConfig.GlobalFontScale;
            if (finalScale <= 0) finalScale = scale > 0 ? scale : 1.0f;

            // Apply global Y-offset for vertical alignment fix
            var adjustedPos = pos + new Vector2(0, FontRenderConfig.GlobalYOffset);
            
            // Draw shadow first (behind main text) if enabled
            if (drawShadow && FontRenderConfig.EnableShadow)
            {
                var shadowPos = adjustedPos + FontRenderConfig.ShadowOffset;
                _defaultFont.DrawText(batch, text, shadowPos, FontRenderConfig.ShadowColor, 0f, Vector2.Zero, new Vector2(finalScale), depth);
            }
            
            // Draw main text on top
            _defaultFont.DrawText(batch, text, adjustedPos, color, 0f, Vector2.Zero, new Vector2(finalScale), depth);
        }
        
        /// <summary>
        /// Draw string with rotation and origin support, plus configurable drop shadow
        /// </summary>
        public void DrawString(SpriteBatch batch, string text, Vector2 pos, Color color, float rotation, Vector2 origin, float scale, float depth, bool drawShadow = true)
        {
            if (_defaultFont == null || batch == null || string.IsNullOrEmpty(text)) return;

            // Apply global font scale
            float finalScale = scale * FontRenderConfig.GlobalFontScale;
            if (finalScale <= 0) finalScale = scale > 0 ? scale : 1.0f;

            // Apply global Y-offset for vertical alignment fix
            var adjustedPos = pos + new Vector2(0, FontRenderConfig.GlobalYOffset);
            
            // Draw shadow first (behind main text) if enabled
            if (drawShadow && FontRenderConfig.EnableShadow)
            {
                var shadowPos = adjustedPos + FontRenderConfig.ShadowOffset;
                _defaultFont.DrawText(batch, text, shadowPos, FontRenderConfig.ShadowColor, rotation, origin, new Vector2(finalScale), depth);
            }
            
            // Draw main text on top
            _defaultFont.DrawText(batch, text, adjustedPos, color, rotation, origin, new Vector2(finalScale), depth);
        }

        /// <summary>
        /// Measure string dimensions (used for layout calculations)
        /// </summary>
        public Vector2 MeasureString(string text, float scale = 1f)
        {
            if (_defaultFont == null || string.IsNullOrEmpty(text)) return Vector2.Zero;
            return _defaultFont.MeasureString(text) * scale;
        }

        /// <summary>
        /// Reset font system (used for GPU device recovery)
        /// </summary>
        public void Reset()
        {
            _fontSystem?.Reset();
            _hasFontSource = false; // Reset the flag
            // Re-load if we have path
             if (!string.IsNullOrEmpty(_fontPath) && _defaultFont != null)
            {
                 var size = _defaultFont.FontSize;
                 // Reverse the scaling to get original size for reload
                 int originalSize = (int)(size / FontRenderConfig.FontScaleMultiplier);
                 LoadFont(_fontPath, originalSize);
            }
        }
    }
}
