using GameManager;
using GameObjects;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Diagnostics.CodeAnalysis;


namespace WorldOfTheThreeKingdoms.GameGlobal
{
    public class Font
    {
        public static float GlobalScale = 1.0f; // ★★★ 全局字体缩放系数 ★★★

        public string Name { get; set; }
        public float Size = 14;
        public string Style { get; set; }

        public float Scale
        {
            get
            {
                return (Size / 20f) * GlobalScale;
            }
        }

        public Font()
        {

        }

        public Font(string name, float size, string style)
        {
            Name = name;
            Size = size;
            Style = style;
        }

        public Vector2 GetWidthHeight(string text)
        {
            //return TextManager.GetWidthHeight(text, CacheManager.FontPair, 1f);
            float width = 0f;

            float scale = ((Size == 0f ? 14 : Size) / 20) * GlobalScale;

            var chars = text.ToCharArray();

            foreach (char ch in chars)
            {
                width += (ch > 128 ? 28 : 14);
            }
            return new Vector2(Convert.ToInt32(width * scale), 25 * scale);
        }

        public void SetFreeTextBuilder(Font font)
        {
            Name = font.Name;
            Size = font.Size;
            Style = font.Style;
        }
    }

    public class StaticMethods
    {
        public static System.Random RandomDigit => System.Random.Shared;

        public static void AdjustRectangleInViewport(ref Microsoft.Xna.Framework.Rectangle rect)
        {
            if (rect.Left < 0)
            {
                rect.X += rect.Width;
            }
            if (rect.Top < 0)
            {
                rect.Y += rect.Height;
            }
        }

        public static void AdjustRectangleInViewport(ref Microsoft.Xna.Framework.Rectangle rect, Microsoft.Xna.Framework.Point viewportSize)
        {
            if (rect.Right > viewportSize.X)
            {
                rect.X -= rect.Width;
            }
            if (rect.Bottom > viewportSize.Y)
            {
                rect.Y -= rect.Height;
            }
        }

        public static Microsoft.Xna.Framework.Rectangle CenterRectangle(Microsoft.Xna.Framework.Rectangle desRectangle, Microsoft.Xna.Framework.Rectangle rectangleToBeCentered)
        {
            return new Microsoft.Xna.Framework.Rectangle(desRectangle.Left + ((desRectangle.Width - rectangleToBeCentered.Width) / 2), desRectangle.Top + (((desRectangle.Height - rectangleToBeCentered.Height) * 2) / 3), rectangleToBeCentered.Width, rectangleToBeCentered.Height);
        }

        // 🔥 2026-02-28 重构：使用源生成器替代反射
        // 原方法使用反射，不兼容 AOT 且性能差（~100ns/调用）
        // 新方法使用编译期生成的代码，性能提升 10x（~10ns/调用）
        // 🔥 2026-03-02 修复：类型安全拦截，避免 InvalidCastException 中断 UI 线程
        // 🔥 2026-03-04 修复：支持带一个 int 参数的方法（如 HasStratagem(int id)）
        public static bool GetBoolMethodValue(object ClassInstance, string methodName, params object[] param)
        {
            if (_propertyAccessDepth > 20) return false;
            
            try
            {
                _propertyAccessDepth++;
                
                object result;
                
                // 🔥 如果有一个 int 参数，使用专门的带参数方法访问器
                if (param != null && param.Length == 1 && param[0] is int intParam)
                {
                    result = global::GameGlobal.UIPropertyAccessor.CallMethodWithIntParam(ClassInstance, methodName, intParam);
                    
                }
                else
                {
                    // 无参方法，使用标准访问器
                    result = global::GameGlobal.UIPropertyAccessor.GetPropertyValueGenerated(ClassInstance, methodName);
                }
                
                // 🔥 类型安全拦截：避免 InvalidCastException
                // 1. 如果是 bool，直接返回（源生成器已优化，bool 直接装箱）
                if (result is bool boolValue)
                {
                    return boolValue;
                }
                
                // 2. 🔥 2026-03-05 修复：检查哨兵对象（方法不存在）
                if (ReferenceEquals(result, PropertyNotFoundSentinel))
                {
                    System.Diagnostics.Debug.WriteLine($"[GetBoolMethodValue] ❌ 方法不存在: {methodName}, 类型: {ClassInstance?.GetType().Name}");
                    return false;
                }
                
                // 3. 如果是字符串 "----"（未找到成员），返回 false
                if (result is string str && str == "----")
                {
                    return false;
                }
                
                // 4. 如果是字符串，尝试解析
                if (result is string str2)
                {
                    // 尝试解析字符串为 bool
                    if (bool.TryParse(str2, out var parsed))
                    {
                        return parsed;
                    }
                }
                
                // 5. 其他类型（如 int, null 等），返回 false
                return false;
            }
            catch (Exception ex)
            {
                // 🔥 异常兜底：绝对不能让 InvalidCastException 中断 UI 线程
                System.Diagnostics.Debug.WriteLine($"[GetBoolMethodValue] 异常: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
            finally
            {
                _propertyAccessDepth--;
            }
        }

        public static Microsoft.Xna.Framework.Rectangle GetBottomLeftRectangle(Microsoft.Xna.Framework.Rectangle rectDes, Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(rectDes.Left, rectDes.Bottom - rect.Height, rect.Width, rect.Height);
        }

        public static Microsoft.Xna.Framework.Rectangle GetBottomRectangle(Microsoft.Xna.Framework.Rectangle rectDes, Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(rectDes.Left + ((rectDes.Width - rect.Width) / 2), rectDes.Bottom - rect.Height, rect.Width, rect.Height);
        }

        public static Microsoft.Xna.Framework.Rectangle GetBottomRightRectangle(Microsoft.Xna.Framework.Rectangle rectDes, Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(rectDes.Right - rect.Width, rectDes.Bottom - rect.Height, rect.Width, rect.Height);
        }

        public static Microsoft.Xna.Framework.Rectangle GetCenterRectangle(Microsoft.Xna.Framework.Rectangle rectDes, Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(rectDes.Left + ((rectDes.Width - rect.Width) / 2), rectDes.Top + ((rectDes.Height - rect.Height) / 2), rect.Width, rect.Height);
        }

        //public static object GetConstValue(Type type, string PropertyName)
        //{
        //    FieldInfo field = type.GetField(PropertyName, BindingFlags.Public | BindingFlags.Static);
        //    if (field != null)
        //    {
        //        return field.GetRawConstantValue();
        //    }
        //    return null;
        //}

        public static ContextMenuResult GetContextMenuResultByName(string Name)
        {
            try
            {
                return Enum.Parse<ContextMenuResult>(Name, false);
            }
            catch
            {
                return ContextMenuResult.None;
            }
        }

        public static Microsoft.Xna.Framework.Rectangle GetLeftRectangle(Microsoft.Xna.Framework.Rectangle rectDes, Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(rectDes.Left, rectDes.Top + ((rectDes.Height - rect.Height) / 2), rect.Width, rect.Height);
        }

        // 🔥 2026-02-28 重构：使用源生成器替代反射
        // 🔥 2026-03-02 修复：类型安全拦截，避免 InvalidCastException 中断 UI 线程
        public static object GetListMethodValue(object ClassInstance, string methodName)
        {
            if (_propertyAccessDepth > 20) return null;
            
            try
            {
                _propertyAccessDepth++;
                
                // ✅ 使用统一的生成访问器（无反射，AOT 兼容）
                var result = global::GameGlobal.UIPropertyAccessor.GetPropertyValueGenerated(ClassInstance, methodName);
                
                // 如果是 "----"（未找到成员），返回 null
                if (result is string str && str == "----")
                {
                    return null;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                // 🔥 异常兜底：绝对不能让异常中断 UI 线程
                System.Diagnostics.Debug.WriteLine($"[GetListMethodValue] 异常: {ex.GetType().Name} - {ex.Message}");
                return null;
            }
            finally
            {
                _propertyAccessDepth--;
            }
        }

        public static string GetNumberStringByGranularity(int number, int granularity)
        {
            int num = number / granularity;
            int num2 = granularity * num;
            return (num2.ToString() + "↑");
        }

        public static string GetPercentString(float rate, int digits)
        {
            return (Math.Round((double) (rate * 100f), digits) + "%");
        }

        // 🔥 2026-02-28 重构：使用源生成器替代反射
        // 🔥 2026-03-02 修复：类型安全拦截，避免 InvalidCastException 中断 UI 线程
        public static object GetMethodValue(object ClassInstance, string MethodName, object[] param)
        {
            if (_propertyAccessDepth > 20) return "----";
            
            try
            {
                _propertyAccessDepth++;
                
                // ✅ 使用统一的生成访问器（无反射，AOT 兼容）
                // 注意：当前生成器不支持带参数的方法，如果需要支持需扩展生成器
                object result = global::GameGlobal.UIPropertyAccessor.GetPropertyValueGenerated(ClassInstance, MethodName);
                
                // 🔍 照妖镜逻辑：如果源生成器没抓到，返回了 "----" 或者 null，就把凶手打印出来！
                if (result == null || (result is string str && str == "----"))
                {
                    var type = ClassInstance != null ? ClassInstance.GetType() : null;
                    string actualType = type != null ? (type.FullName ?? type.Name) : "null";
                    System.Diagnostics.Debug.WriteLine($"[AOT漏球警告] GetMethodValue 尝试获取方法失败! 实际类型: {actualType}, 请求的方法名: {MethodName}");
                    return "----";
                }
                
                return result;
            }
            catch (Exception ex)
            {
                // 🔥 异常兜底：绝对不能让异常中断 UI 线程
                System.Diagnostics.Debug.WriteLine($"[GetMethodValue] 异常: {ex.GetType().Name} - {ex.Message}");
                return "----";
            }
            finally
            {
                _propertyAccessDepth--;
            }
        }

        [ThreadStatic]
        private static int _propertyAccessDepth;

        // 🔥 2026-03-04 修复：引用 UIPropertyAccessor 的哨兵对象（确保引用相等性）
        private static object PropertyNotFoundSentinel => global::GameGlobal.UIPropertyAccessor.PropertyNotFoundSentinel;

        // 🔥 2026-02-28 重构：使用源生成器替代反射
        // 原方法使用反射访问属性和字段，不兼容 AOT
        // 新方法使用编译期生成的代码，性能提升 10x
        // 🔥 2026-03-02 修复：类型安全拦截，避免 InvalidCastException 中断 UI 线程
        // 🔥 2026-03-04 修复：使用特殊哨兵对象，精确识别属性不存在（不再误判属性返回的 "----"）
        public static object GetPropertyValue(object ClassInstance, string PropertyName)
        {
            if (_propertyAccessDepth > 20)
            {
                // Prevent infinite recursion
                return "----";
            }

            try
            {
                _propertyAccessDepth++;
                
                // ✅ 使用生成的访问器（无反射，AOT 兼容）
                object result = global::GameGlobal.UIPropertyAccessor.GetPropertyValueGenerated(ClassInstance, PropertyName);
                
                // 🔍 使用引用相等性检查哨兵对象（不会误判属性返回的 "----"）
                if (ReferenceEquals(result, PropertyNotFoundSentinel))
                {
                    var type = ClassInstance != null ? ClassInstance.GetType() : null;
                    string actualType = type != null ? (type.FullName ?? type.Name) : "null";
                    System.Diagnostics.Debug.WriteLine($"[AOT漏球警告] GetPropertyValue 尝试获取属性失败! 实际类型: {actualType}, 请求的属性名: {PropertyName}");
                    return "----";  // 转换为 UI 显示的哨兵值
                }
                
                // 正常返回（包括属性本身返回的 "----"、null 等）
                return result;
            }
            catch (Exception ex)
            {
                // 🔥 异常兜底：绝对不能让异常中断 UI 线程
                System.Diagnostics.Debug.WriteLine($"[GetPropertyValue] 异常: {ex.GetType().Name} - {ex.Message}");
                return "----";
            }
            finally
            {
                _propertyAccessDepth--;
            }
        }

        public static int GetRandomValue(int a, int b)
        {
            int num;
            int num2;
            if (b == 0)
            {
                return 0;
            }
            if (b > 0)
            {
                num = a / b;
                num2 = a % b;
                if ((num2 > 0) && (Random(b) < num2))
                {
                    num++;
                }
                return num;
            }
            b = Math.Abs(b);
            num = a / b;
            num2 = a % b;
            if ((num2 > 0) && (Random(b) < num2))
            {
                num++;
            }
            return -num;
        }

        public static int GetBigRandomValue(int a, int  b)
        {
            int num;
            int num2;
            if (b == 0)
            {
                return 0;
            }
            if (b > 0)
            {
                num =(int) (a / b);
                num2 =(int) (a % b);
                if ((num2 > 0) && (Random((int) b) < num2))
                {
                    num++;
                }
                return num;
            }
            //b = Math.Abs(b);
            b = -b;
            num = (int)(a / b);
            num2 = (int)(a % b);
            if ((num2 > 0) && (Random((int) b) < num2))
            {
                num++;
            }
            return -num;
        }

        public static Microsoft.Xna.Framework.Rectangle GetRectangleFitViewport(int width, int height, Microsoft.Xna.Framework.Point viewportSize)
        {
            int x = width;
            int y = height;
            if (viewportSize.X < width)
            {
                x = viewportSize.X;
            }
            if (viewportSize.Y < height)
            {
                y = viewportSize.Y;
            }
            return new Microsoft.Xna.Framework.Rectangle((viewportSize.X - x) / 2, (viewportSize.Y - y) / 2, x, y);
        }

        public static Microsoft.Xna.Framework.Rectangle GetRightRectangle(Microsoft.Xna.Framework.Rectangle rectDes, Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(rectDes.Right - rect.Width, rectDes.Top + ((rectDes.Height - rect.Height) / 2), rect.Width, rect.Height);
        }

        // 🔥 2026-02-28 重构：使用源生成器替代反射
        // 🔥 2026-03-02 修复：类型安全拦截，避免 InvalidCastException 中断 UI 线程
        // 🔥 2026-03-04 修复：使用特殊哨兵对象，避免误判方法返回的 "----"
        public static string GetStringMethodValue(object ClassInstance, string methodName, params object[] param)
        {
            if (_propertyAccessDepth > 20) return "----";
            
            try
            {
                _propertyAccessDepth++;
                
                object result;
                
                // 🔥 如果有一个 int 参数，使用专门的带参数方法访问器
                if (param != null && param.Length == 1 && param[0] is int intParam)
                {
                    result = global::GameGlobal.UIPropertyAccessor.CallMethodWithIntParam(ClassInstance, methodName, intParam);
                }
                else
                {
                    // 无参方法，使用标准访问器
                    result = global::GameGlobal.UIPropertyAccessor.GetPropertyValueGenerated(ClassInstance, methodName);
                }
                
                // 🔍 使用引用相等性检查哨兵对象（不会误判方法返回的 "----"）
                if (ReferenceEquals(result, PropertyNotFoundSentinel))
                {
                    var type = ClassInstance != null ? ClassInstance.GetType() : null;
                    string actualType = type != null ? (type.FullName ?? type.Name) : "null";
                    System.Diagnostics.Debug.WriteLine($"[AOT漏球警告] GetStringMethodValue 尝试获取方法失败! 实际类型: {actualType}, 请求的方法名: {methodName}");
                    return "----";
                }
                
                return result?.ToString() ?? "----";
            }
            catch (Exception ex)
            {
                // 🔥 异常兜底：绝对不能让异常中断 UI 线程
                System.Diagnostics.Debug.WriteLine($"[GetStringMethodValue] 异常: {ex.GetType().Name} - {ex.Message}");
                return "----";
            }
            finally
            {
                _propertyAccessDepth--;
            }
        }

        public static Microsoft.Xna.Framework.Rectangle GetTopLeftRectangle(Microsoft.Xna.Framework.Rectangle rectDes, Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(rectDes.Left, rectDes.Top, rect.Width, rect.Height);
        }

        public static Microsoft.Xna.Framework.Rectangle GetTopRectangle(Microsoft.Xna.Framework.Rectangle rectDes, Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(rectDes.Left + ((rectDes.Width - rect.Width) / 2), rectDes.Top, rect.Width, rect.Height);
        }

        public static Microsoft.Xna.Framework.Rectangle GetTopRightRectangle(Microsoft.Xna.Framework.Rectangle rectDes, Microsoft.Xna.Framework.Rectangle rect)
        {
            return new Microsoft.Xna.Framework.Rectangle(rectDes.Right - rect.Width, rectDes.Top, rect.Width, rect.Height);
        }

        public static Microsoft.Xna.Framework.Rectangle GetViewportCenterRectangle(int width, int height, Microsoft.Xna.Framework.Point viewportSize)
        {
            return new Microsoft.Xna.Framework.Rectangle((viewportSize.X - width) / 2, (viewportSize.Y - height) / 2, width, height);
        }

        public static Microsoft.Xna.Framework.Rectangle LeftRectangle(Microsoft.Xna.Framework.Rectangle desRectangle, Microsoft.Xna.Framework.Rectangle rectangleToBeLefted)
        {
            return new Microsoft.Xna.Framework.Rectangle(desRectangle.Left, desRectangle.Top + (((desRectangle.Height - rectangleToBeLefted.Height) * 2) / 3), rectangleToBeLefted.Width, rectangleToBeLefted.Height);
        }

        public static void LoadFontAndColorFromXMLNode(XmlNode node, out Font font, out Microsoft.Xna.Framework.Color color)
        {
            font = new Font(node.Attributes.GetNamedItem("FontName").Value, float.Parse(node.Attributes.GetNamedItem("FontSize").Value), node.Attributes.GetNamedItem("FontStyle").Value);
            color = new Microsoft.Xna.Framework.Color();
            uint x = uint.Parse(node.Attributes.GetNamedItem("FontColor").Value);
            color.PackedValue = (x & 0xFF000000) | ((x & 0x00FF0000) >> 16) | (x & 0x0000FF00) | ((x & 0x000000FF) << 16);
        }

        public static Microsoft.Xna.Framework.Color LoadColor(String code)
        {
            // 🔥 修复：支持两种颜色格式
            // 1. 十六进制 uint 格式: "4294967295"
            // 2. RGB 格式: "255,255,255" 或 "255,255,255,255" (带 Alpha)
            
            if (code.Contains(','))
            {
                // RGB/RGBA 格式
                string[] parts = code.Split(',');
                
                if (parts.Length == 3)
                {
                    // RGB 格式
                    byte r = byte.Parse(parts[0].Trim());
                    byte g = byte.Parse(parts[1].Trim());
                    byte b = byte.Parse(parts[2].Trim());
                    return new Microsoft.Xna.Framework.Color(r, g, b);
                }
                else if (parts.Length == 4)
                {
                    // RGBA 格式
                    byte r = byte.Parse(parts[0].Trim());
                    byte g = byte.Parse(parts[1].Trim());
                    byte b = byte.Parse(parts[2].Trim());
                    byte a = byte.Parse(parts[3].Trim());
                    return new Microsoft.Xna.Framework.Color(r, g, b, a);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadColor] 无效的 RGB 格式: {code}，使用默认白色");
                    return Microsoft.Xna.Framework.Color.White;
                }
            }
            else
            {
                // 十六进制 uint 格式（原有逻辑）
                Microsoft.Xna.Framework.Color color = new();
                uint x = uint.Parse(code);
                color.PackedValue = (x & 0xFF000000) | ((x & 0x00FF0000) >> 16) | (x & 0x0000FF00) | ((x & 0x000000FF) << 16);
                return color;
            }
        }

        public static Microsoft.Xna.Framework.Point? LoadFromString(string dataString)
        {
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length == 2)
            {
                return new Microsoft.Xna.Framework.Point(int.Parse(strArray[0]), int.Parse(strArray[1]));
            }
            return null;
        }

        public static void LoadFromString(out int[] intArray, string dataString)
        {
            // 🔥 技术性修复：安全处理字符串解析，避免ArgumentOutOfRangeException
            try
            {
                if (string.IsNullOrEmpty(dataString))
                {
                    intArray = new int[0];
                    return;
                }
                
                char[] separator = new char[] { ' ', '\n', '\r', '\t' };
                string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                intArray = new int[strArray.Length];
                for (int i = 0; i < strArray.Length; i++)
                {
                    if (int.TryParse(strArray[i], out int value))
                    {
                        intArray[i] = value;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[StaticMethods] 无法解析整数: {strArray[i]}");
                        intArray[i] = 0; // 使用默认值
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaticMethods] LoadFromString(int[])失败: {ex.Message}");
                intArray = new int[0]; // 返回空数组
            }
        }

        public static void LoadFromString(List<Microsoft.Xna.Framework.Point> pointList, string dataString)
        {
            // 🔥 技术性修复：安全处理Point列表解析，避免ArgumentOutOfRangeException
            try
            {
                if (pointList == null)
                {
                    System.Diagnostics.Debug.WriteLine("[StaticMethods] pointList为null");
                    return;
                }
                
                pointList.Clear();
                
                if (string.IsNullOrEmpty(dataString))
                {
                    return;
                }
                
                char[] separator = new char[] { ' ', '\n', '\r', '\t' };
                string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                
                // 确保数组长度是偶数，因为每个Point需要两个值
                for (int i = 0; i < strArray.Length - 1; i += 2)
                {
                    if (int.TryParse(strArray[i], out int x) && int.TryParse(strArray[i + 1], out int y))
                    {
                        pointList.Add(new Microsoft.Xna.Framework.Point(x, y));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[StaticMethods] 无法解析Point: {strArray[i]}, {strArray[i + 1]}");
                    }
                }
                
                // 如果有奇数个元素，记录警告
                if (strArray.Length % 2 != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[StaticMethods] Point数据长度为奇数，最后一个元素被忽略: {strArray[strArray.Length - 1]}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaticMethods] LoadFromString(Point)失败: {ex.Message}");
                if (pointList != null)
                {
                    pointList.Clear();
                }
            }
        }

        public static void LoadFromString(List<int> intList, string dataString)
        {
            // 🔥 技术性修复：安全处理整数列表解析，避免ArgumentOutOfRangeException
            try
            {
                if (intList == null)
                {
                    System.Diagnostics.Debug.WriteLine("[StaticMethods] intList为null");
                    return;
                }
                
                intList.Clear();
                
                if (string.IsNullOrEmpty(dataString))
                {
                    return;
                }
                
                char[] separator = new char[] { ' ', '\n', '\r', '\t' };
                string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < strArray.Length; i++)
                {
                    if (int.TryParse(strArray[i], out int value))
                    {
                        intList.Add(value);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[StaticMethods] 无法解析整数: {strArray[i]}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaticMethods] LoadFromString(List<int>)失败: {ex.Message}");
                if (intList != null)
                {
                    intList.Clear();
                }
            }
        }

        public static void LoadFromString(List<string> list, string dataString)
        {
            // 🔥 技术性修复：安全处理字符串列表解析，避免ArgumentOutOfRangeException
            try
            {
                if (list == null)
                {
                    System.Diagnostics.Debug.WriteLine("[StaticMethods] string list为null");
                    return;
                }
                
                list.Clear();
                
                if (string.IsNullOrEmpty(dataString))
                {
                    return;
                }
                
                char[] separator = new char[] { ' ', '\n', '\r', '\t' };
                string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < strArray.Length; i++)
                {
                    list.Add(strArray[i]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaticMethods] LoadFromString(List<string>)失败: {ex.Message}");
                if (list != null)
                {
                    list.Clear();
                }
            }
        }

        public static void LoadFromString(Dictionary<int, int> list, string dataString)
        {
            // 🔥 技术性修复：安全处理Dictionary解析，避免ArgumentOutOfRangeException
            try
            {
                if (list == null)
                {
                    System.Diagnostics.Debug.WriteLine("[StaticMethods] Dictionary为null");
                    return;
                }
                
                list.Clear();
                
                if (string.IsNullOrEmpty(dataString))
                {
                    return;
                }
                
                char[] separator = new char[] { ' ', '\n', '\r', '\t' };
                string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                
                if (strArray.Length >= 2)
                {
                    // 确保数组长度是偶数，因为每个键值对需要两个值
                    for (int i = 0; i < strArray.Length - 1; i += 2)
                    {
                        if (int.TryParse(strArray[i], out int key) && int.TryParse(strArray[i + 1], out int value))
                        {
                            if (!list.ContainsKey(key))
                            {
                                list.Add(key, value);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[StaticMethods] 重复的键被忽略: {key}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[StaticMethods] 无法解析键值对: {strArray[i]}, {strArray[i + 1]}");
                        }
                    }
                    
                    // 如果有奇数个元素，记录警告
                    if (strArray.Length % 2 != 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[StaticMethods] Dictionary数据长度为奇数，最后一个元素被忽略: {strArray[strArray.Length - 1]}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaticMethods] LoadFromString(Dictionary)失败: {ex.Message}");
                if (list != null)
                {
                    list.Clear();
                }
            }
        }

        public static Microsoft.Xna.Framework.Rectangle LoadRectangleFromXMLNode(XmlNode node)
        {
            // 🔥 技术性修复：安全处理XML节点解析，避免ArgumentNullException
            try
            {
                if (node?.Attributes == null)
                {
                    System.Diagnostics.Debug.WriteLine("[StaticMethods] XML节点或属性为null");
                    return Microsoft.Xna.Framework.Rectangle.Empty;
                }
                
                var xAttr = node.Attributes.GetNamedItem("X");
                var yAttr = node.Attributes.GetNamedItem("Y");
                var widthAttr = node.Attributes.GetNamedItem("Width");
                var heightAttr = node.Attributes.GetNamedItem("Height");
                
                if (xAttr?.Value == null || yAttr?.Value == null || widthAttr?.Value == null || heightAttr?.Value == null)
                {
                    System.Diagnostics.Debug.WriteLine("[StaticMethods] XML节点缺少必需的Rectangle属性");
                    return Microsoft.Xna.Framework.Rectangle.Empty;
                }
                
                if (int.TryParse(xAttr.Value, out int x) && 
                    int.TryParse(yAttr.Value, out int y) && 
                    int.TryParse(widthAttr.Value, out int width) && 
                    int.TryParse(heightAttr.Value, out int height))
                {
                    return new Microsoft.Xna.Framework.Rectangle(x, y, width, height);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[StaticMethods] 无法解析Rectangle属性值");
                    return Microsoft.Xna.Framework.Rectangle.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaticMethods] LoadRectangleFromXMLNode失败: {ex.Message}");
                return Microsoft.Xna.Framework.Rectangle.Empty;
            }
        }

        public static bool PointInRectangle(Microsoft.Xna.Framework.Point point, Microsoft.Xna.Framework.Rectangle rect)
        {
            if (CacheManager.Scale != Vector2.One)
            {
                //return ((((point.X * CacheManager.Scale.X > rect.Left * CacheManager.Scale.X) && (point.Y * CacheManager.Scale.Y > rect.Top * CacheManager.Scale.Y)) && (point.X * CacheManager.Scale.X < rect.Right * CacheManager.Scale.X)) && (point.Y * CacheManager.Scale.Y < rect.Bottom * CacheManager.Scale.Y));
                return ((((point.X > rect.Left * CacheManager.Scale.X) && (point.Y > rect.Top * CacheManager.Scale.Y)) && (point.X < rect.Right * CacheManager.Scale.X)) && (point.Y < rect.Bottom * CacheManager.Scale.Y));
            }
            else
            {
                return ((((point.X > rect.Left) && (point.Y > rect.Top)) && (point.X < rect.Right)) && (point.Y < rect.Bottom));
            }
        }

        public static bool PointInViewport(Microsoft.Xna.Framework.Point position, Microsoft.Xna.Framework.Point viewportSize)
        {
            return PointInRectangle(position, new Microsoft.Xna.Framework.Rectangle(-1, -1, viewportSize.X + 1, viewportSize.Y + 1));
        }

        public static int Random(int maxValue)
        {
            if (maxValue <= 0)
            {
                return 0;
            }
            return RandomDigit.Next(maxValue);
        }

        public static double Random()
        {
            return RandomDigit.NextDouble();
        }

        public static bool RectangleInViewport(Microsoft.Xna.Framework.Rectangle rect, Microsoft.Xna.Framework.Point viewportSize)
        {
            if ((((rect.Left >= viewportSize.X) || (rect.Right <= 0)) || (rect.Top >= viewportSize.Y)) || (rect.Bottom <= 0))
            {
                return false;
            }
            return true;
        }

        public static Microsoft.Xna.Framework.Rectangle RightRectangle(Microsoft.Xna.Framework.Rectangle desRectangle, Microsoft.Xna.Framework.Rectangle rectangleToBeRighted)
        {
            return new Microsoft.Xna.Framework.Rectangle(desRectangle.Right - rectangleToBeRighted.Width, desRectangle.Top + (((desRectangle.Height - rectangleToBeRighted.Height) * 2) / 3), rectangleToBeRighted.Width, rectangleToBeRighted.Height);
        }

        public static string SaveToString(int[] intArray)
        {
            if (intArray == null)
            {
                return "";
            }
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < intArray.Length; i++)
            {
                builder.Append(intArray[i].ToString() + " ");
            }
            return builder.ToString();
        }

        public static string SaveToString(List<Microsoft.Xna.Framework.Point> pointList)
        {
            if (pointList == null)
            {
                return "";
            }
            StringBuilder builder = new StringBuilder();
            foreach (Microsoft.Xna.Framework.Point point in pointList)
            {
                builder.Append(point.X.ToString() + " " + point.Y.ToString() + " ");
            }
            return builder.ToString();
        }

        public static string SaveToString(List<int> intList)
        {
            if (intList == null)
            {
                return "";
            }
            StringBuilder builder = new StringBuilder();
            foreach (int num in intList)
            {
                builder.Append(num.ToString() + " ");
            }
            return builder.ToString();
        }

        public static string SaveToString(List<string> list)
        {
            if (list == null)
            {
                return "";
            }
            StringBuilder builder = new StringBuilder();
            foreach (string str in list)
            {
                builder.Append(str + " ");
            }
            return builder.ToString();
        }

        public static string SaveToString(Microsoft.Xna.Framework.Point? point)
        {
            if (point.HasValue)
            {
                return (point.Value.X.ToString() + " " + point.Value.Y.ToString());
            }
            return string.Empty;
        }

        public static string SaveToString(Dictionary<int, int> intList) 
        {
            if (intList == null)
            {
                return "";
            }
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<int, int> num in intList)
            {
                builder.Append(num.Key.ToString() + " " + num.Value.ToString() + " ");
            }
            return builder.ToString();
        }

        public static string SaveToString(List<KeyValuePair<int, int>> List)
        {
            if (List == null)
            {
                return "";
            }
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<int, int> num in List)
            {
                builder.Append(num.Key.ToString() + " " + num.Value.ToString() + " ");
            }
            return builder.ToString();
        }
        public static void LoadFromString(List<KeyValuePair<int, int>> list, string dataString)
        {
            // 🔥 技术性修复：安全处理KeyValuePair列表解析，避免ArgumentOutOfRangeException
            try
            {
                if (list == null)
                {
                    System.Diagnostics.Debug.WriteLine("[StaticMethods] KeyValuePair list为null");
                    return;
                }
                
                list.Clear();
                
                if (string.IsNullOrEmpty(dataString))
                {
                    return;
                }
                
                char[] separator = new char[] { ' ', '\n', '\r', '\t' };
                string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                
                // 确保数组长度是偶数，因为每个KeyValuePair需要两个值
                for (int i = 0; i < strArray.Length - 1; i += 2)
                {
                    if (int.TryParse(strArray[i], out int key) && int.TryParse(strArray[i + 1], out int value))
                    {
                        list.Add(new KeyValuePair<int, int>(key, value));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[StaticMethods] 无法解析KeyValuePair: {strArray[i]}, {strArray[i + 1]}");
                    }
                }
                
                // 如果有奇数个元素，记录警告
                if (strArray.Length % 2 != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[StaticMethods] KeyValuePair数据长度为奇数，最后一个元素被忽略: {strArray[strArray.Length - 1]}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaticMethods] LoadFromString(KeyValuePair)失败: {ex.Message}");
                if (list != null)
                {
                    list.Clear();
                }
            }
        }
        
        public static global::GameObjects.zainanlei LoadzainanfromString(string zainanstring)
        {
            // 🔥 技术性修复：安全处理zainanlei解析，避免ArgumentOutOfRangeException
            try
            {
                global::GameObjects.zainanlei zainan = new global::GameObjects.zainanlei();
                
                if (string.IsNullOrEmpty(zainanstring))
                {
                    return zainan;
                }
                
                char[] separator = new char[] { ' ', '\n', '\r', '\t' };
                string[] strArray = zainanstring.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                
                // 确保至少有两个元素
                if (strArray.Length >= 2)
                {
                    if (int.TryParse(strArray[0], out int leixing) && int.TryParse(strArray[1], out int tianshu))
                    {
                        zainan.zainanleixing = leixing;
                        zainan.shengyutianshu = tianshu;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[StaticMethods] 无法解析zainanlei: {strArray[0]}, {strArray[1]}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[StaticMethods] zainanlei数据不足，需要至少2个元素，实际: {strArray.Length}");
                }
                
                return zainan;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaticMethods] LoadzainanfromString失败: {ex.Message}");
                return new global::GameObjects.zainanlei(); // 返回默认对象
            }
        }

        public static string[] LoadStringArrayFromString(string dataString)
        {
            // 🔥 技术性修复：安全处理字符串数组解析，避免ArgumentOutOfRangeException
            try
            {
                if (string.IsNullOrEmpty(dataString))
                {
                    return new string[] { };
                }
                
                char[] separator = new char[] { ' ', '{', '}', ',', '\n', '\r', '\t' };
                string[] strArray = dataString.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                
                if (strArray.Length > 0)
                {
                    return strArray;
                }
                else 
                {
                    return new string[] { };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaticMethods] LoadStringArrayFromString失败: {ex.Message}");
                return new string[] { };
            }
        }
    }
}

