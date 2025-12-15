using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 双击左键处理器
    /// </summary>
    public class DoubleClickHandler
    {
        private static DoubleClickHandler _instance;
        public static DoubleClickHandler Instance => _instance ??= new DoubleClickHandler();

        // 双击时间阈值（毫秒）
        private const int DOUBLE_CLICK_INTERVAL = 400;
        
        // 双击位置容差（像素）
        private const int DOUBLE_CLICK_TOLERANCE = 10;
        
        // 记录上一次点击的时间和位置
        private Stopwatch _clickTimer;
        private Point? _lastClickPosition;
        private MouseState _previousMouseState;
        
        // 事件
        public event Action<Point> OnDoubleClick;
        public event Action<Point> OnLeftClick;

        private DoubleClickHandler()
        {
            _clickTimer = new Stopwatch();
            _previousMouseState = Mouse.GetState();
        }

        /// <summary>
        /// 更新输入状态
        /// </summary>
        public void Update()
        {
            var currentMouseState = Mouse.GetState();
            
            // 检测左键按下
            if (IsLeftButtonClicked(_previousMouseState, currentMouseState))
            {
                HandleLeftClick(currentMouseState);
            }
            
            _previousMouseState = currentMouseState;
        }

        /// <summary>
        /// 检测左键是否刚被点击
        /// </summary>
        private bool IsLeftButtonClicked(MouseState previous, MouseState current)
        {
            return previous.LeftButton == ButtonState.Released && current.LeftButton == ButtonState.Pressed;
        }

        /// <summary>
        /// 处理左键点击
        /// </summary>
        private void HandleLeftClick(MouseState mouseState)
        {
            var clickPosition = new Point(mouseState.X, mouseState.Y);
            
            // 触发单击事件
            OnLeftClick?.Invoke(clickPosition);
            
            // 检查是否是双击
            if (_lastClickPosition.HasValue && _clickTimer.IsRunning)
            {
                var timeElapsed = _clickTimer.ElapsedMilliseconds;
                var positionDelta = Vector2.Distance(
                    _lastClickPosition.Value.ToVector2(),
                    clickPosition.ToVector2()
                );
                
                // 判断是否为双击：时间间隔小且位置相近
                if (timeElapsed < DOUBLE_CLICK_INTERVAL && positionDelta < DOUBLE_CLICK_TOLERANCE)
                {
                    // 触发双击事件
                    OnDoubleClick?.Invoke(clickPosition);
                    
                    // 重置双击检测
                    ResetDoubleClickDetection();
                    return;
                }
            }
            
            // 记录这次点击
            _lastClickPosition = clickPosition;
            _clickTimer.Restart();
        }

        /// <summary>
        /// 重置双击检测
        /// </summary>
        private void ResetDoubleClickDetection()
        {
            _lastClickPosition = null;
            _clickTimer.Reset();
        }

        /// <summary>
        /// 设置双击阈值
        /// </summary>
        public void SetDoubleClickInterval(int milliseconds)
        {
            // 可以动态调整双击间隔
        }
    }
}