using System;

namespace GameManager
{
    /// <summary>
    /// 表示范围的结构体
    /// </summary>
    public struct Bounds
    {
        /// <summary>
        /// 左上角X坐标
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// 左上角Y坐标
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// 右下角X坐标
        /// </summary>
        public float X2 { get; set; }

        /// <summary>
        /// 右下角Y坐标
        /// </summary>
        public float Y2 { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public float Width => X2 - X;

        /// <summary>
        /// 高度
        /// </summary>
        public float Height => Y2 - Y;

        public Bounds(float x, float y, float x2, float y2)
        {
            X = x;
            Y = y;
            X2 = x2;
            Y2 = y2;
        }
    }
}