#nullable disable

using GameObjects.MapDetail;
using GameObjects.ArchitectureDetail;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using GameManager;
using System.Text.Json.Serialization;

namespace GameObjects
{
    [DataContract]
    public class GameArea
    {
        // 🔥 AOT 修复：添加 JsonInclude 以支持 System.Text.Json 序列化字段
        // 日期：2026-03-20
        // 原因：DataContract/DataMember 是 DataContractSerializer 的标记
        //       System.Text.Json 不识别这些标记，导致读档后字段值为默认值
        
        [DataMember]
        [JsonInclude]
        public List<Point> Area = [];  // 🔥 C# 12：集合表达式
        
        [DataMember]
        [JsonInclude]
        public Point Centre;
        
        [JsonIgnore]
        public Point? topleft { get; set; } = null;
        
        [JsonIgnore]
        public Point? topright { get; set; } = null;
        
        [JsonIgnore]
        public Point? bottomleft { get; set; } = null;
        
        [JsonIgnore]
        public Point? bottomright { get; set; } = null;

        // 🔥 新增：对象身份追踪（用于诊断随机序列化问题）
        private static int _nextInstanceId = 1;
        private static readonly object _idLock = new object();
        
        [JsonIgnore]
        public int InstanceId { get; private set; }
        
        [JsonIgnore]
        public string CreationStackTrace { get; private set; }

        public GameArea() 
        { 
            lock (_idLock)
            {
                InstanceId = _nextInstanceId++;
            }
            // 🔥 Checklist Fix: Priority 2 - Field Initialization
            this.Area = new List<Point>();
            
            // 🔥 默认不输出调试日志，避免刷屏
            #if DEBUG
            // 🔥 只在启用详细日志时才记录堆栈（确实有性能开销，但用于诊断时很有价值）
            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameAreaCreationLog)
            {
                CreationStackTrace = Environment.StackTrace;
                System.Diagnostics.Debug.WriteLine($"[GameArea创建] ID:{InstanceId} 线程:{System.Threading.Thread.CurrentThread.ManagedThreadId}");
            }
            #endif
        }

        public GameArea(GameArea old)
        {
            lock (_idLock)
            {
                InstanceId = _nextInstanceId++;
            }
            
            this.Area = new List<Point>(old.Area);
            this.Centre = new Point(old.Centre.X, old.Centre.Y);
            if (!old.topleft.HasValue)
            {
                this.topleft = null;
            } else {
                this.topleft = new Point(old.topleft.Value.X, old.topleft.Value.Y);
            }
            // Note: other cached bounds are lazily regenerated, no strict need to copy if null
            
            #if DEBUG
            // 🔥 只在启用详细日志时才记录堆栈（确实有性能开销，但用于诊断时很有价值）
            if (WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableGameAreaCreationLog)
            {
                CreationStackTrace = Environment.StackTrace;
                System.Diagnostics.Debug.WriteLine($"[GameArea拷贝] 新ID:{InstanceId} 来源ID:{old.InstanceId}");
            }
            #endif
        }

        public void AddPoint(Point point)
        {
            this.Area.Add(point);
            this.topleft = null;
            this.topright = null;
            this.bottomleft = null;
            this.bottomright = null;
        }

        private static void CheckPoint(GameArea Area, List<Point> BlackAngles, Point point, Faction faction)
        {
            TerrainDetail terrainDetailByPosition = Session.Current.Scenario.GetTerrainDetailByPosition(point);
            if (terrainDetailByPosition != null)
            {
                if (terrainDetailByPosition.ViewThrough)
                {
                    if (faction != null)
                    {
                        Architecture architectureByPosition = Session.Current.Scenario.GetArchitectureByPosition(point);
                        if (!(architectureByPosition == null || architectureByPosition.Endurance <= 0 || faction.IsFriendlyWithoutTruce(architectureByPosition.BelongedFaction)))
                        {
                            BlackAngles.Add(point);
                            return;
                        }
                    }
                    if (!IsInBlackAngle(Area.Centre, BlackAngles, point))
                    {
                        Area.AddPoint(point);
                    }
                }
                else
                {
                    BlackAngles.Add(point);
                }
            }
        }

        public void CombineArea(GameArea AreaToCombine, Dictionary<Point, object> ClosedList)
        {
            foreach (Point point in AreaToCombine.Area)
            {
                if (!ClosedList.ContainsKey(point))
                {
                    ClosedList.Add(point, null);
                }
            }
        }

        public bool Contact(Point p, bool oblique)
        {
            return this.GetContactArea(oblique).HasPoint(p);
        }

        public override bool Equals(object obj)
        {
            // 🔥 根本修复：基于引用相等而不是内容相等
            // 这样ReferenceHandler.Preserve就不会错误地认为不同的GameArea对象是同一个
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            // 🔥 与Equals()保持一致，使用引用的HashCode
            return RuntimeHelpers.GetHashCode(this);
        }

        private static double GetAngle(Point centre, Point point)
        {
            int num = point.X - centre.X;
            int num2 = point.Y - centre.Y;
            return Math.Asin(((double) num2) / Math.Sqrt((double) ((num * num) + (num2 * num2))));
        }

        public static GameArea GetArea(Point Centre, int Radius, bool Oblique)
        {
            GameArea area = new GameArea();
            List<float> list = new List<float>();
            area.Centre = Centre;
            if (Oblique)
            {
                for (int i = Centre.X - Radius; i <= Centre.X + Radius; i++)
                {
                    for (int j = Centre.Y - Radius; j <= Centre.Y + Radius; j++)
                    {
                        area.AddPoint(new Point(i, j));
                    }
                }
            }
            else
            {
                for (int i = -Radius; i <= Radius; i++)
                {
                    if (i <= 0)
                    {
                        for (int j = -Radius - i; j <= i + Radius; j++)
                        {
                            area.AddPoint(new Point(Centre.X + i, Centre.Y + j));
                        }
                    }
                    else
                    {
                        for (int j = i - Radius; j <= Radius - i; j++)
                        {
                            area.AddPoint(new Point(Centre.X + i, Centre.Y + j));
                        }
                    }
                }
            }
            return area;
        }

        public static GameArea GetAreaFromArea(GameArea area, int radius, bool oblique, Faction faction)
        {
            // 🔥 添加 null 检查
            if (area == null)
            {
                System.Diagnostics.Debug.WriteLine($"[GameArea.GetAreaFromArea] ❌ 错误：area 参数为 null！");
                throw new ArgumentNullException(nameof(area), "GameArea.GetAreaFromArea: area 参数不能为 null");
            }
            
            if (area.Area == null)
            {
                System.Diagnostics.Debug.WriteLine($"[GameArea.GetAreaFromArea] ❌ 错误：area.Area 为 null！");
                throw new InvalidOperationException("GameArea.GetAreaFromArea: area.Area 不能为 null");
            }
            
            /*int longRadius;
            if (area.Count <= 1)
                longRadius = radius;
            else if (area.Count <= 5)
                longRadius = radius + 1;
            else
                longRadius = radius + 2;
            GameArea candidateArea = GetArea(area.Centre, longRadius, oblique);
            if (longRadius >= (radius + 1))
            {
                candidateArea.Area.Remove(new Point(area.Centre.X - longRadius, area.Centre.Y - longRadius));
                candidateArea.Area.Remove(new Point(area.Centre.X - longRadius, area.Centre.Y + longRadius));
                candidateArea.Area.Remove(new Point(area.Centre.X + longRadius, area.Centre.Y - longRadius));
                candidateArea.Area.Remove(new Point(area.Centre.X + longRadius, area.Centre.Y + longRadius));
            }
            if (longRadius >= (radius + 2))
            {
                candidateArea.Area.Remove(new Point(area.Centre.X - longRadius + 1, area.Centre.Y - longRadius));
                candidateArea.Area.Remove(new Point(area.Centre.X - longRadius, area.Centre.Y - longRadius + 1));
                candidateArea.Area.Remove(new Point(area.Centre.X - longRadius + 1, area.Centre.Y + longRadius));
                candidateArea.Area.Remove(new Point(area.Centre.X - longRadius, area.Centre.Y + longRadius - 1));
                candidateArea.Area.Remove(new Point(area.Centre.X + longRadius - 1, area.Centre.Y - longRadius));
                candidateArea.Area.Remove(new Point(area.Centre.X + longRadius, area.Centre.Y - longRadius + 1));
                candidateArea.Area.Remove(new Point(area.Centre.X + longRadius - 1, area.Centre.Y + longRadius));
                candidateArea.Area.Remove(new Point(area.Centre.X + longRadius, area.Centre.Y + longRadius - 1));
            }
            return candidateArea;*/
            Dictionary<Point, object> closedList = new Dictionary<Point, object>();
            GameArea area2 = new GameArea();
            foreach (Point point in area.Area)
            {
                area2.CombineArea(GetViewArea(point, radius, oblique, faction), closedList);
            }
            foreach (Point point in closedList.Keys)
            {
                area2.Area.Add(point);
            }
            return area2;
        }

        public GameArea GetContactArea(bool oblique)
        {
            return GetContactArea(oblique, false, false);
        }

        public GameArea GetContactArea(bool oblique, bool waterOnly, bool landOnly)
        {
            GameArea area = new GameArea();
            Dictionary<Point, object> dictionary = new Dictionary<Point, object>();
            foreach (Point point2 in this.Area)
            {
                bool ok = true;
                if (waterOnly && Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID != 6)
                {
                    ok = false;
                }
                if (landOnly && Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID == 6)
                {
                    ok = false;
                }
                if (ok)
                {
                    dictionary.Add(point2, null);
                }
            }
            foreach (Point point2 in this.Area)
            {
                Point key = new Point(point2.X - 1, point2.Y);
                if (!dictionary.ContainsKey(key) && (!waterOnly || Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID == 6))
                {
                    dictionary.Add(key, null);
                    area.AddPoint(key);
                }
                key = new Point(point2.X + 1, point2.Y);
                if (!dictionary.ContainsKey(key) && (!waterOnly || Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID == 6))
                {
                    dictionary.Add(key, null);
                    area.AddPoint(key);
                }
                key = new Point(point2.X, point2.Y - 1);
                if (!dictionary.ContainsKey(key) && (!waterOnly || Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID == 6))
                {
                    dictionary.Add(key, null);
                    area.AddPoint(key);
                }
                key = new Point(point2.X, point2.Y + 1);
                if (!dictionary.ContainsKey(key) && (!waterOnly || Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID == 6))
                {
                    dictionary.Add(key, null);
                    area.AddPoint(key);
                }
                if (oblique)
                {
                    key = new Point(point2.X - 1, point2.Y - 1);
                    if (!dictionary.ContainsKey(key) && (!waterOnly || Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID == 6))
                    {
                        dictionary.Add(key, null);
                        area.AddPoint(key);
                    }
                    key = new Point(point2.X + 1, point2.Y - 1);
                    if (!dictionary.ContainsKey(key) && (!waterOnly || Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID == 6))
                    {
                        dictionary.Add(key, null);
                        area.AddPoint(key);
                    }
                    key = new Point(point2.X - 1, point2.Y + 1);
                    if (!dictionary.ContainsKey(key) && (!waterOnly || Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID == 6))
                    {
                        dictionary.Add(key, null);
                        area.AddPoint(key);
                    }
                    key = new Point(point2.X + 1, point2.Y + 1);
                    if (!dictionary.ContainsKey(key) && (!waterOnly || Session.Current.Scenario.GetTerrainDetailByPositionNoCheck(point2).ID == 6))
                    {
                        dictionary.Add(key, null);
                        area.AddPoint(key);
                    }
                }
            }
            return area;
        }

        public int GetPointIndex(Point point)
        {
            for (int i = 0; i < this.Area.Count; i++)
            {
                if (this.Area[i] == point)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 计算视野区域（考虑地形遮挡和敌城阻挡）
        /// 🧊 Cold Path：初始化时调用，不在 Update 循环中
        /// 
        /// 🔥 最终修复：分离视野和范围语义
        /// 日期：2026-03-22
        /// 原因：GetViewArea 被用于两种不同的场景，Radius 语义冲突
        /// - 视野系统：Radius = "距离"，Radius = 3 应覆盖距离 0, 1, 2（3 层）
        /// - 攻击/计略系统：Radius = "半径"，Radius = 1 应覆盖距离 0, 1（2 层）
        /// 解决：创建 GetRangeArea 用于攻击/计略，GetViewArea 用于视野
        /// 
        /// 🔥 重构：现代 C# 12 风格
        /// - 使用集合表达式 []
        /// - 使用有意义的变量名
        /// - 使用 for 循环而不是 while 循环
        /// - 添加详细注释
        /// </summary>
        /// <param name="Centre">视野中心点</param>
        /// <param name="Radius">视野距离（不包含边界）</param>
        /// <param name="Oblique">是否包含斜向视野</param>
        /// <param name="faction">势力（用于判断敌城阻挡）</param>
        /// <returns>视野区域</returns>
        public static GameArea GetViewArea(Point Centre, int Radius, bool Oblique, Faction faction)
        {
            // 🔥 C# 12：使用集合表达式
            GameArea area = new();
            List<Point> blackAngles = [];
            area.Centre = Centre;
            
            // 边界情况：无效半径
            if (Radius < 0) return area;
            
            // 边界情况：半径为 0，只包含中心点
            if (Radius == 0)
            {
                area.AddPoint(Centre);
                return area;
            }
            
            // 🔥 视野语义：使用 < Radius（距离语义）
            // 日期：2026-03-22
            // 原因：修复视野多 1 格问题，使视野范围与能量传播范围一致
            // Radius = 3 → 覆盖距离 0, 1, 2（共 3 层）
            // 注意：攻击/计略系统已使用独立的 GetRangeArea，不受影响
            for (int distance = 0; distance < Radius; distance++)
            {
                if (distance == 0)
                {
                    // 中心点：沿 Y 轴扩展
                    for (int offsetY = 0; offsetY < Radius; offsetY++)
                    {
                        if (offsetY != 0)
                        {
                            // 上方和下方
                            CheckPoint(area, blackAngles, new Point(Centre.X, Centre.Y + offsetY), faction);
                            CheckPoint(area, blackAngles, new Point(Centre.X, Centre.Y - offsetY), faction);
                        }
                        else
                        {
                            // 中心点
                            area.AddPoint(Centre);
                        }
                    }
                }
                else
                {
                    // 非中心点：沿 X 轴扩展
                    // 🔥 关键：斜向视野的范围计算
                    // - Oblique = true（斜向）：offsetLimit = Radius（正方形）
                    // - Oblique = false（直向）：offsetLimit = Radius - distance（菱形）
                    int offsetLimit = Oblique ? Radius : (Radius - distance);
                    
                    for (int offsetY = 0; offsetY < offsetLimit; offsetY++)
                    {
                        // 右侧（X + distance）
                        CheckPoint(area, blackAngles, new Point(Centre.X + distance, Centre.Y + offsetY), faction);
                        if (offsetY != 0)
                        {
                            CheckPoint(area, blackAngles, new Point(Centre.X + distance, Centre.Y - offsetY), faction);
                        }
                    }
                    
                    // 左侧（X - distance）
                    for (int offsetY = 0; offsetY < offsetLimit; offsetY++)
                    {
                        CheckPoint(area, blackAngles, new Point(Centre.X - distance, Centre.Y + offsetY), faction);
                        if (offsetY != 0)
                        {
                            CheckPoint(area, blackAngles, new Point(Centre.X - distance, Centre.Y - offsetY), faction);
                        }
                    }
                }
            }
            
            return area;
        }

        /// <summary>
        /// 计算范围区域（用于攻击范围、计略范围、补给范围）
        /// 🧊 Cold Path：初始化时调用，不在 Update 循环中
        /// 
        /// 🔥 新方法：分离范围语义
        /// 日期：2026-03-22
        /// 原因：攻击/计略/补给系统使用"半径"语义，与视野系统的"距离"语义不同
        /// - Radius = 1（近战）→ 覆盖距离 0, 1（中心点 + 周围 8 格）
        /// - Radius = 2（远程）→ 覆盖距离 0, 1, 2（中心点 + 两层）
        /// 
        /// 🔥 与 GetViewArea 的区别：
        /// - 不考虑战争迷雾（无 faction 参数）
        /// - 不考虑地形遮挡（不使用 CheckPoint）
        /// - 使用 <= Radius（半径语义）
        /// </summary>
        /// <param name="Centre">范围中心点</param>
        /// <param name="Radius">范围半径（包含边界）</param>
        /// <param name="Oblique">是否包含斜向范围</param>
        /// <returns>范围区域</returns>
        public static GameArea GetRangeArea(Point Centre, int Radius, bool Oblique)
        {
            // 🔥 C# 12：使用集合表达式
            GameArea area = new();
            area.Centre = Centre;
            
            // 边界情况：无效半径
            if (Radius < 0) return area;
            
            // 边界情况：半径为 0，只包含中心点
            if (Radius == 0)
            {
                area.AddPoint(Centre);
                return area;
            }
            
            // 🔥 范围语义：使用 <= Radius（半径语义）
            // Radius = 1 → 覆盖距离 0, 1（共 2 层，近战）
            // Radius = 2 → 覆盖距离 0, 1, 2（共 3 层，远程）
            for (int distance = 0; distance <= Radius; distance++)
            {
                if (distance == 0)
                {
                    // 中心点：沿 Y 轴扩展
                    for (int offsetY = 0; offsetY <= Radius; offsetY++)
                    {
                        if (offsetY != 0)
                        {
                            // 上方和下方（不使用 CheckPoint，直接添加）
                            area.AddPoint(new Point(Centre.X, Centre.Y + offsetY));
                            area.AddPoint(new Point(Centre.X, Centre.Y - offsetY));
                        }
                        else
                        {
                            // 中心点
                            area.AddPoint(Centre);
                        }
                    }
                }
                else
                {
                    // 非中心点：沿 X 轴扩展
                    // 🔥 关键：斜向范围的计算
                    // - Oblique = true（斜向）：offsetLimit = Radius（正方形）
                    // - Oblique = false（直向）：offsetLimit = Radius - distance（菱形）
                    int offsetLimit = Oblique ? Radius : (Radius - distance);
                    
                    for (int offsetY = 0; offsetY <= offsetLimit; offsetY++)
                    {
                        // 右侧（X + distance）
                        area.AddPoint(new Point(Centre.X + distance, Centre.Y + offsetY));
                        if (offsetY != 0)
                        {
                            area.AddPoint(new Point(Centre.X + distance, Centre.Y - offsetY));
                        }
                    }
                    
                    // 左侧（X - distance）
                    for (int offsetY = 0; offsetY <= offsetLimit; offsetY++)
                    {
                        area.AddPoint(new Point(Centre.X - distance, Centre.Y + offsetY));
                        if (offsetY != 0)
                        {
                            area.AddPoint(new Point(Centre.X - distance, Centre.Y - offsetY));
                        }
                    }
                }
            }
            
            return area;
        }

        public bool HasPoint(Point point)
        {
            foreach (Point point2 in this.Area)
            {
                if (point2 == point)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsInBlackAngle(Point centre, List<Point> BlackAngles, Point anglePoint)
        {
            foreach (Point point in BlackAngles)
            {
                int num = point.X - centre.X;
                int num2 = point.Y - centre.Y;
                double num3 = 3.1415926535897931 / (5.0 * Math.Sqrt((double) ((num * num) + (num2 * num2))));
                double angle = GetAngle(centre, point);
                double num5 = GetAngle(centre, anglePoint);
                if (IsInSameRegion(centre, point, anglePoint) && (Math.Round(Math.Abs((double) (angle - num5)), 5) <= Math.Round(num3, 5)))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsInSameRegion(Point centre, Point point1, Point point2)
        {
            return ((((point1.X - centre.X) * (point2.X - centre.X)) >= 0) && (((point1.Y - centre.Y) * (point2.Y - centre.Y)) >= 0));
        }

        public void MoveArea(int XOffset, int YOffset)
        {
            if ((XOffset != 0) || (YOffset != 0))
            {
                this.Centre.X += XOffset;
                this.Centre.Y += YOffset;
                for (int i = 0; i < this.Count; i++)
                {
                    this.Area[i] = new Point(this.Area[i].X + XOffset, this.Area[i].Y + YOffset);
                }
                this.topleft = null;
            }
        }

        public void RemoveArea(GameArea area)
        {
            foreach (Point point in area.Area)
            {
                this.Area.Remove(point);
            }
        }

        public void RemovePoints(List<Point> points)
        {
            foreach (Point point in points)
            {
                this.Area.Remove(point);
            }
        }

        private void ResetTopLeft()
        {
            if (this.Area.Count == 0)
            {
                this.topleft = null;
            }
            else if (this.Area.Count == 1)
            {
                this.topleft = new Point?(this.Area[0]);
            }
            else
            {
                int num = this.Area[0].X * this.Area[0].Y;
                int num2 = 0;
                int num3 = 0;
                for (int i = 1; i < this.Area.Count; i++)
                {
                    num3 = this.Area[i].X * this.Area[i].Y;
                    if (num3 < num)
                    {
                        num2 = i;
                        num = num3;
                    }
                }
                this.topleft = new Point?(this.Area[num2]);
            }
        }

        public int Count
        {
            get
            {
                return this.Area.Count;
            }
        }

        public Point this[int index]
        {
            get
            {
                return this.Area[index];
            }
            set
            {
                this.Area[index] = value;
            }
        }

        [JsonPropertyName("TopLeft")]
        public Point TopLeft
        {
            get
            {
                if (!this.topleft.HasValue)
                {
                    this.ResetTopLeft();
                }
                // AOT修复：如果仍然没有值，返回默认坐标而不是异常
                if (!this.topleft.HasValue && this.Area != null && this.Area.Count > 0)
                {
                    // 手动计算TopLeft作为备用方案
                    int minX = int.MaxValue;
                    int minY = int.MaxValue;
                    foreach (Point point in this.Area)
                    {
                        if (point.X < minX) minX = point.X;
                        if (point.Y < minY) minY = point.Y;
                    }
                    if (minX != int.MaxValue && minY != int.MaxValue)
                    {
                        return new Point(minX, minY);
                    }
                }
                return this.topleft.GetValueOrDefault(new Point(0, 0));
            }
        }


        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(this.Centre.X);
            sb.Append(",");
            sb.Append(this.Centre.Y);
            sb.Append("|");
            foreach (Point p in this.Area)
            {
                sb.Append(p.X);
                sb.Append(",");
                sb.Append(p.Y);
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static GameArea FromString(string s)
        {
            GameArea area = new GameArea();
            if (string.IsNullOrEmpty(s)) return area;

            string[] parts = s.Split('|');
            if (parts.Length > 0)
            {
                string[] centreParts = parts[0].Split(',');
                if (centreParts.Length == 2)
                {
                    area.Centre = new Point(int.Parse(centreParts[0]), int.Parse(centreParts[1]));
                }
            }
            if (parts.Length > 1)
            {
                string[] points = parts[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string point in points)
                {
                    string[] xy = point.Split(',');
                    if (xy.Length == 2)
                    {
                        area.AddPoint(new Point(int.Parse(xy[0]), int.Parse(xy[1])));
                    }
                }
            }
            return area;
        }
        
        // ============================================================
        // 🆕 势力范围系统扩展（2026-03-11）
        // ============================================================
        
        /// <summary>
        /// 🆕 能量强度映射（用于势力范围系统）
        /// Key: 坐标点, Value: 剩余能量值
        /// </summary>
        public Dictionary<Point, int>? InfluenceMap { get; set; }
        
        /// <summary>
        /// 🔥 四方向数组（避免重复分配）
        /// </summary>
        private static readonly Point[] Directions = [
            new Point(0, -1),  // 上
            new Point(0, 1),   // 下
            new Point(-1, 0),  // 左
            new Point(1, 0)    // 右
        ];
        
        /// <summary>
        /// 🆕 带权重的泛滥填充算法（Dijkstra）
        /// 用于计算势力范围，考虑地形阻力
        /// 🧊 Cold Path：初始化时调用，不在 Update 循环中
        /// </summary>
        /// <param name="startPoints">起始区域（建筑占地）</param>
        /// <param name="maxEnergy">最大能量值（决定扩散范围）</param>
        /// <param name="getTerrainCost">地形阻力查询函数</param>
        /// <returns>包含能量梯度的区域</returns>
        public static GameArea GetWeightedFloodFill(
            GameArea startPoints, 
            int maxEnergy,
            Func<Point, int> getTerrainCost)
        {
            // 🔥 数据验证：追溯根源而非掩盖
            if (startPoints?.Area == null)
            {
                throw new ArgumentNullException(nameof(startPoints), 
                    "GetWeightedFloodFill: 起始区域不能为 null，请检查 Architecture.ArchitectureArea 初始化");
            }
            
            if (maxEnergy <= 0)
            {
                throw new ArgumentException(
                    $"GetWeightedFloodFill: maxEnergy={maxEnergy} 无效，请检查 CalculateInfluenceEnergy() 逻辑",
                    nameof(maxEnergy));
            }
            
            var result = new GameArea { Centre = startPoints.Centre };
            result.InfluenceMap = new Dictionary<Point, int>(capacity: maxEnergy * 4);
            
            var frontier = new PriorityQueue<Point, int>();
            var costSoFar = new Dictionary<Point, int>(capacity: maxEnergy * 4);
            
            // 初始化起点
            foreach (Point p in startPoints.Area)
            {
                frontier.Enqueue(p, 0);
                costSoFar[p] = 0;
                result.AddPoint(p);
                result.InfluenceMap[p] = maxEnergy;
            }
            
            // Dijkstra 主循环
            while (frontier.TryDequeue(out Point current, out int currentCost))
            {
                if (currentCost >= maxEnergy) continue;
                
                // 🔥 使用静态数组避免分配
                for (int i = 0; i < Directions.Length; i++)
                {
                    Point dir = Directions[i];
                    Point next = new Point(current.X + dir.X, current.Y + dir.Y);
                    
                    // 🔥 修复：必须先检查边界，再查询地形代价
                    // 日期：2026-03-13
                    // 原因：getTerrainCost 内部访问数组，越界坐标会导致 IndexOutOfRangeException
                    if (Session.Current.Scenario.PositionOutOfRange(next)) continue;
                    
                    int terrainCost = getTerrainCost(next);
                    
                    // 峻岭阻断
                    if (terrainCost >= 99999) continue;
                    
                    int newCost = currentCost + terrainCost;
                    
                    if (newCost <= maxEnergy)
                    {
                        // 发现更短路径或新路径
                        if (!costSoFar.TryGetValue(next, out int oldCost) || newCost < oldCost)
                        {
                            costSoFar[next] = newCost;
                            frontier.Enqueue(next, newCost);
                            
                            if (!result.HasPoint(next)) result.AddPoint(next);
                            
                            // 记录剩余能量（用于 Buff 计算和渲染）
                            result.InfluenceMap[next] = maxEnergy - newCost;
                        }
                    }
                }
            }
            
            return result;
        }
    }
}

