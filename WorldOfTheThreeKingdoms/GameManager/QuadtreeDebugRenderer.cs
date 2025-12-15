using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameManager;

namespace WorldOfTheThreeKingdoms.GameManager
{
    /// <summary>
    /// Debug visualization for Quadtree spatial partitioning
    /// </summary>
    public static class QuadtreeDebugRenderer
    {
        private static Texture2D _pixelTexture;
        
        /// <summary>
        /// Initialize the debug renderer
        /// </summary>
        /// <param name="graphicsDevice">Graphics device for creating textures</param>
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            // Create a 1x1 white pixel texture for drawing lines
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Draw Quadtree bounds for debugging
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="quadtree">Quadtree to visualize</param>
        /// <param name="camera">Camera transformation matrix</param>
        public static void DrawQuadtreeBounds(SpriteBatch spriteBatch, Quadtree quadtree, Matrix camera)
        {
            if (!QuadtreeConfig.EnableDebugVisualization || _pixelTexture == null || quadtree == null)
                return;

            // This would require exposing internal quadtree structure
            // For now, we'll draw the visible area bounds
            DrawVisibleAreaBounds(spriteBatch, camera);
        }

        /// <summary>
        /// Draw the visible area bounds
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="camera">Camera transformation matrix</param>
        public static void DrawVisibleAreaBounds(SpriteBatch spriteBatch, Matrix camera)
        {
            if (!QuadtreeConfig.EnableDebugVisualization || _pixelTexture == null)
                return;

            try
            {
                // Get visible area
                var mainGameScreen = Session.MainGame.mainGameScreen;
                if (mainGameScreen == null) return;

                Rectangle visibleArea = mainGameScreen.GetVisibleArea(camera);
                
                // Draw rectangle outline
                Color debugColor = Color.Yellow * 0.5f; // Semi-transparent yellow
                int lineWidth = 2;
                
                // Top line
                spriteBatch.Draw(_pixelTexture, new Rectangle(visibleArea.Left, visibleArea.Top, visibleArea.Width, lineWidth), debugColor);
                // Bottom line
                spriteBatch.Draw(_pixelTexture, new Rectangle(visibleArea.Left, visibleArea.Bottom - lineWidth, visibleArea.Width, lineWidth), debugColor);
                // Left line
                spriteBatch.Draw(_pixelTexture, new Rectangle(visibleArea.Left, visibleArea.Top, lineWidth, visibleArea.Height), debugColor);
                // Right line
                spriteBatch.Draw(_pixelTexture, new Rectangle(visibleArea.Right - lineWidth, visibleArea.Top, lineWidth, visibleArea.Height), debugColor);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QuadtreeDebugRenderer] Error drawing debug bounds: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw performance information on screen
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch for drawing</param>
        /// <param name="font">Font for text rendering</param>
        /// <param name="position">Position to draw the text</param>
        public static void DrawPerformanceInfo(SpriteBatch spriteBatch, SpriteFont font, Vector2 position)
        {
            if (!QuadtreeConfig.EnableDebugVisualization || font == null)
                return;

            try
            {
                // TODO: Fix PerformanceMonitor reference
                string performanceText = "Performance monitoring temporarily disabled";
                spriteBatch.DrawString(font, performanceText, position + Vector2.One, Color.Black); // Shadow
                spriteBatch.DrawString(font, performanceText, position, Color.White);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QuadtreeDebugRenderer] Error drawing performance info: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public static void Dispose()
        {
            _pixelTexture?.Dispose();
            _pixelTexture = null;
        }
    }
}