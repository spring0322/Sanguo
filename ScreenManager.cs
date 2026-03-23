using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameManager
{
    public static class ScreenManager
    {
        // 设定你游戏的原始设计分辨率（Zhsan原版通常是 1024x768 或 1280x720，请根据实际情况改！）
        public static readonly int VirtualWidth = 1280;
        public static readonly int VirtualHeight = 720;

        public static float ScaleX { get; private set; }
        public static float ScaleY { get; private set; }
        public static Matrix ScaleMatrix { get; private set; } = Matrix.Identity;

        // 在 Initialize 时调用，或者窗口大小改变时调用
        public static void UpdateResolution(int screenWidth, int screenHeight)
        {
            ScaleX = (float)screenWidth / VirtualWidth;
            ScaleY = (float)screenHeight / VirtualHeight;
            
            // 创建缩放矩阵
            ScaleMatrix = Matrix.CreateScale(ScaleX, ScaleY, 1.0f);
        }

        // 把鼠标在屏幕上的真实坐标，转换回游戏里的虚拟坐标
        // 解决“鼠标点不准”的问题
        public static Vector2 InputToWorld(Vector2 mousePosition)
        {
            return Vector2.Transform(mousePosition, Matrix.Invert(ScaleMatrix));
        }
    }
}
