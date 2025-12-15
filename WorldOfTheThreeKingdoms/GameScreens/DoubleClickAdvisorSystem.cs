using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameManager;
using GameObjects;
using WorldOfTheThreeKingdoms.GameScreens;

namespace WorldOfTheThreeKingdoms.GameScreens
{
    /// <summary>
    /// 点击对象类型枚举
    /// </summary>
    public enum ClickedObjectType
    {
        EmptyTerrain,
        Architecture,
        Troop,
        Routeway,
        UIElement,
        None
    }

    /// <summary>
    /// 双击军师系统 - 优化手感版
    /// </summary>
    public class DoubleClickAdvisorSystem
    {
        private static DoubleClickAdvisorSystem _instance;
        public static DoubleClickAdvisorSystem Instance
        {
            get
            {
                if (_instance == null) _instance = new DoubleClickAdvisorSystem();
                return _instance;
            }
        }

        // [调整] 收紧判定条件
        private const int DOUBLE_CLICK_INTERVAL = 300; // 从 400 改为 300，避免单击误判
        private const int CLICK_TOLERANCE = 5;         // 从 10 改为 5，避免拖动误判
        
        private Stopwatch _clickTimer;
        private Point? _lastClickPosition;
        private MouseState _previousMouseState;

        // 事件定义
        public delegate void DoubleClickDetectedHandler(ClickedObjectType type, object target, Point position);
        public event DoubleClickDetectedHandler OnDoubleClickDetected;

        private DoubleClickAdvisorSystem()
        {
            _clickTimer = new Stopwatch();
            _previousMouseState = Mouse.GetState();
        }

        public void Update()
        {
            try
            {
                var currentMouseState = Mouse.GetState();

                // 检测左键 "Released -> Pressed" 的瞬间
                if (_previousMouseState.LeftButton == ButtonState.Released && 
                    currentMouseState.LeftButton == ButtonState.Pressed)
                {
                    HandleClick(currentMouseState);
                }

                _previousMouseState = currentMouseState;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[双击系统] Update 异常: {ex.Message}");
            }
        }

        private void HandleClick(MouseState mouseState)
        {
            var clickPosition = new Point(mouseState.X, mouseState.Y);

            // 检查是否满足双击条件
            if (_lastClickPosition.HasValue && _clickTimer.IsRunning && 
                _clickTimer.ElapsedMilliseconds < DOUBLE_CLICK_INTERVAL)
            {
                var distance = Vector2.Distance(_lastClickPosition.Value.ToVector2(), clickPosition.ToVector2());
                
                // 只有位置非常接近才算双击
                if (distance < CLICK_TOLERANCE)
                {
                    // 确认双击！
                    TriggerDoubleClick(clickPosition);
                    // [重要] 触发后立即重置，防止三连击触发两次
                    Reset();
                    return;
                }
            }

            // 如果不是双击，记录这次点击作为 "第一次点击"
            _lastClickPosition = clickPosition;
            _clickTimer.Restart();
        }

        private void TriggerDoubleClick(Point position)
        {
            try
            {
                // 判定点击了什么
                var objectType = DetectClickedObjectType(position);
                var targetObject = GetClickedObject(position, objectType);

                Console.WriteLine($"[双击军师系统] 检测到双击: {objectType}");

                // 触发事件
                OnDoubleClickDetected?.Invoke(objectType, targetObject, position);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[双击系统] TriggerDoubleClick 异常: {ex.Message}");
            }
        }

        private void Reset()
        {
            _lastClickPosition = null;
            _clickTimer.Reset();
        }

        private ClickedObjectType DetectClickedObjectType(Point clickPosition)
        {
            try
            {
                // [安全检查] 确保 Session 和相关对象存在
                if (Session.MainGame == null)
                {
                    Console.WriteLine("[双击系统] Session.MainGame 为 null");
                    return ClickedObjectType.EmptyTerrain;
                }

                var mainScreen = Session.MainGame.mainGameScreen as MainGameScreen;
                if (mainScreen == null)
                {
                    Console.WriteLine("[双击系统] mainGameScreen 为 null");
                    return ClickedObjectType.EmptyTerrain;
                }

                // 优先判定建筑
                if (mainScreen.CurrentArchitecture != null)
                    return ClickedObjectType.Architecture;

                // 其次判定部队
                if (mainScreen.CurrentTroop != null)
                    return ClickedObjectType.Troop;

                // 默认空地 (保证一定能触发菜单)
                return ClickedObjectType.EmptyTerrain;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[双击系统] DetectClickedObjectType 异常: {ex.Message}");
                return ClickedObjectType.EmptyTerrain;
            }
        }

        private object GetClickedObject(Point clickPosition, ClickedObjectType objectType)
        {
            try
            {
                // [安全检查] 确保 Session 和相关对象存在
                if (Session.MainGame == null)
                {
                    Console.WriteLine("[双击系统] Session.MainGame 为 null");
                    return null;
                }

                var mainScreen = Session.MainGame.mainGameScreen as MainGameScreen;
                if (mainScreen == null)
                {
                    Console.WriteLine("[双击系统] mainGameScreen 为 null");
                    return null;
                }

                switch (objectType)
                {
                    case ClickedObjectType.Architecture: 
                        return mainScreen.CurrentArchitecture;
                    case ClickedObjectType.Troop: 
                        return mainScreen.CurrentTroop;
                    default: 
                        return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[双击系统] GetClickedObject 异常: {ex.Message}");
                return null; 
            }
        }

        // 辅助方法
        public bool IsCurrentPlayerOwned()
        {
            try 
            {
                var f = Session.Current.Scenario.CurrentFaction;
                return f != null && Session.Current.Scenario.IsPlayer(f);
            } 
            catch 
            { 
                return false; 
            }
        }

        public bool CanAppointAdvisor()
        {
            try 
            { 
                return Session.Current.Scenario.CurrentFaction.AppointAdvisorAvail(); 
            } 
            catch 
            { 
                return false; 
            }
        }

        public bool CanSendAdvisor()
        {
            // 简化逻辑，仅作演示
            return true; 
        }
    }
}