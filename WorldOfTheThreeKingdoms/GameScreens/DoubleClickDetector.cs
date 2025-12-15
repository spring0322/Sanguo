using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameManager;
using GameObjects;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 双击检测器 - 专门负责检测双击事件
    /// </summary>
    public class DoubleClickDetector
    {
        private static DoubleClickDetector _instance;
        public static DoubleClickDetector Instance => _instance ??= new DoubleClickDetector();

        // 双击检测参数
        private const int DOUBLE_CLICK_INTERVAL = 400; // 双击时间间隔（毫秒）
        private const int CLICK_TOLERANCE = 15;        // 点击位置容差（像素）

        private Stopwatch _clickTimer;
        private Point? _lastClickPosition;
        private MouseState _previousMouseState;
        private bool _isDoubleClickDetected = false;

        // 当前点击的对象信息
        private object _clickedObject;
        private Point _clickedPosition;

        // 双击事件
        public event Action<object, Point> OnDoubleClickDetected;

        private DoubleClickDetector()
        {
            _clickTimer = new Stopwatch();
            _previousMouseState = Mouse.GetState();
        }

        /// <summary>
        /// 更新检测双击
        /// </summary>
        public void Update()
        {
            var currentMouseState = Mouse.GetState();

            // 检测左键按下
            if (IsLeftButtonClicked(_previousMouseState, currentMouseState))
            {
                HandleLeftClick(currentMouseState);
            }

            // 如果双击已经检测到并处理完毕，重置标志
            if (_isDoubleClickDetected)
            {
                _isDoubleClickDetected = false;
            }

            _previousMouseState = currentMouseState;
        }

        private bool IsLeftButtonClicked(MouseState previous, MouseState current)
        {
            return previous.LeftButton == ButtonState.Released &&
                   current.LeftButton == ButtonState.Pressed;
        }

        private void HandleLeftClick(MouseState mouseState)
        {
            var clickPosition = new Point(mouseState.X, mouseState.Y);
            var clickedObject = GetClickedObject(clickPosition);

            // 检查是否是双击
            if (_lastClickPosition.HasValue &&
                _clickTimer.IsRunning &&
                _clickTimer.ElapsedMilliseconds < DOUBLE_CLICK_INTERVAL)
            {
                var positionDelta = Vector2.Distance(_lastClickPosition.Value.ToVector2(),
                                                   clickPosition.ToVector2());

                // 判定双击：位置相近 且 点击的是同一类对象
                if (positionDelta < CLICK_TOLERANCE &&
                    IsSameObjectType(clickedObject, _clickedObject))
                {
                    _isDoubleClickDetected = true;
                    
                    // 触发双击事件
                    OnDoubleClickDetected?.Invoke(clickedObject, clickPosition);
                    
                    ResetDoubleClickDetection();
                    return;
                }
            }

            // 记录这次点击（作为第一次点击）
            _lastClickPosition = clickPosition;
            _clickedObject = clickedObject;
            _clickedPosition = clickPosition;
            _clickTimer.Restart();
        }

        /// <summary>
        /// 获取点击的对象
        /// </summary>
        private object GetClickedObject(Point clickPosition)
        {
            var selectedObject = GetSelectedObject();
            if (selectedObject == null)
            {
                // 屏幕区域简易判定
                var screenWidth = Platform.GraphicsDevice.PresentationParameters.BackBufferWidth;
                var screenHeight = Platform.GraphicsDevice.PresentationParameters.BackBufferHeight;

                // 简单的边距检测，排除UI点击
                if (clickPosition.X > screenWidth * 0.1 &&
                    clickPosition.X < screenWidth * 0.9 &&
                    clickPosition.Y > screenHeight * 0.1 &&
                    clickPosition.Y < screenHeight * 0.9)
                {
                    return "Map"; // 地图/空地
                }
            }

            return selectedObject;
        }

        /// <summary>
        /// 反射获取当前选中对象
        /// </summary>
        private object GetSelectedObject()
        {
            try
            {
                var gameScreen = Session.MainGame.mainGameScreen;
                var type = gameScreen.GetType();

                var pProp = type.GetProperty("SelectedPerson");
                var val = pProp?.GetValue(gameScreen);
                if (val != null) return val;

                var aProp = type.GetProperty("SelectedArchitecture");
                val = aProp?.GetValue(gameScreen);
                if (val != null) return val;

                var tProp = type.GetProperty("SelectedTroop");
                val = tProp?.GetValue(gameScreen);
                if (val != null) return val;

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 判断是否是同一类对象
        /// </summary>
        private bool IsSameObjectType(object obj1, object obj2)
        {
            if (obj1 == null || obj2 == null) return false;

            // 优先检查字符串特殊标记 "Map"
            bool isMap1 = (obj1 is string s1 && s1 == "Map");
            bool isMap2 = (obj2 is string s2 && s2 == "Map");

            if (isMap1 && isMap2) return true;
            if (isMap1 || isMap2) return false; // 一个是Map一个不是

            // 检查实际类型
            return obj1.GetType() == obj2.GetType();
        }

        private void ResetDoubleClickDetection()
        {
            _lastClickPosition = null;
            _clickedObject = null;
            _clickTimer.Reset();
        }

        /// <summary>
        /// 检查是否检测到双击（用于调试）
        /// </summary>
        public bool IsDoubleClickDetected()
        {
            return _isDoubleClickDetected;
        }

        /// <summary>
        /// 获取双击的对象（用于调试）
        /// </summary>
        public object GetDoubleClickedObject()
        {
            return _clickedObject;
        }
    }
}