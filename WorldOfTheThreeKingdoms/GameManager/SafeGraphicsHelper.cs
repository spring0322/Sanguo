using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameManager
{
    /// <summary>
    /// 安全的图形操作辅助类 - 防止GPU设备移除异常
    /// </summary>
    public static class SafeGraphicsHelper
    {
        /// <summary>
        /// 安全创建纹理
        /// </summary>
        /// <param name="graphicsDevice">图形设备</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="mipMap">是否使用mipmap</param>
        /// <param name="format">表面格式</param>
        /// <returns>创建的纹理，如果失败返回null</returns>
        public static Texture2D SafeCreateTexture2D(GraphicsDevice graphicsDevice, int width, int height, 
            bool mipMap = false, SurfaceFormat format = SurfaceFormat.Color)
        {
            try
            {
                // 检查图形设备状态
                if (graphicsDevice == null || graphicsDevice.IsDisposed)
                {
                    System.Diagnostics.Debug.WriteLine("[SafeGraphicsHelper] 图形设备不可用，无法创建纹理");
                    return null;
                }

                // 检查参数有效性
                if (width <= 0 || height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SafeGraphicsHelper] 无效的纹理尺寸: {width}x{height}");
                    return null;
                }

                // 检查尺寸是否过大（防止内存不足）
                if (width > 4096 || height > 4096)
                {
                    System.Diagnostics.Debug.WriteLine($"[SafeGraphicsHelper] 纹理尺寸过大: {width}x{height}，可能导致内存问题");
                }

                // 尝试创建纹理
                var texture = new Texture2D(graphicsDevice, width, height, mipMap, format);
                
                return texture;
            }
            catch (SharpDX.SharpDXException dxEx) when (dxEx.ResultCode.Code == unchecked((int)0x887A0005))
            {
                System.Diagnostics.Debug.WriteLine($"[SafeGraphicsHelper] GPU设备移除，无法创建纹理: {dxEx.Message}");
                return null;
            }
            catch (OutOfMemoryException memEx)
            {
                System.Diagnostics.Debug.WriteLine($"[SafeGraphicsHelper] 内存不足，无法创建纹理: {memEx.Message}");
                
                // 尝试垃圾回收后再试一次
                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    
                    System.Diagnostics.Debug.WriteLine("[SafeGraphicsHelper] 执行垃圾回收后重试创建纹理");
                    return new Texture2D(graphicsDevice, width, height, mipMap, format);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("[SafeGraphicsHelper] 垃圾回收后仍无法创建纹理");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SafeGraphicsHelper] 创建纹理时发生异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 安全设置纹理数据
        /// </summary>
        /// <param name="texture">目标纹理</param>
        /// <param name="data">数据数组</param>
        /// <returns>是否成功</returns>
        public static bool SafeSetTextureData<T>(Texture2D texture, T[] data) where T : struct
        {
            try
            {
                if (texture == null || texture.IsDisposed)
                {
                    System.Diagnostics.Debug.WriteLine("[SafeGraphicsHelper] 纹理不可用，无法设置数据");
                    return false;
                }

                if (data == null || data.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[SafeGraphicsHelper] 数据无效，无法设置纹理数据");
                    return false;
                }

                texture.SetData(data);
                return true;
            }
            catch (SharpDX.SharpDXException dxEx) when (dxEx.ResultCode.Code == unchecked((int)0x887A0005))
            {
                System.Diagnostics.Debug.WriteLine($"[SafeGraphicsHelper] GPU设备移除，无法设置纹理数据: {dxEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SafeGraphicsHelper] 设置纹理数据时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建单色纹理的安全方法
        /// </summary>
        /// <param name="graphicsDevice">图形设备</param>
        /// <param name="color">颜色</param>
        /// <param name="width">宽度（默认1）</param>
        /// <param name="height">高度（默认1）</param>
        /// <returns>创建的纹理，如果失败返回null</returns>
        public static Texture2D CreateSolidColorTexture(GraphicsDevice graphicsDevice, Color color, int width = 1, int height = 1)
        {
            try
            {
                var texture = SafeCreateTexture2D(graphicsDevice, width, height);
                if (texture == null)
                    return null;

                var colorData = new Color[width * height];
                for (int i = 0; i < colorData.Length; i++)
                {
                    colorData[i] = color;
                }

                if (SafeSetTextureData(texture, colorData))
                {
                    return texture;
                }
                else
                {
                    texture?.Dispose();
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SafeGraphicsHelper] 创建单色纹理时发生异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查图形设备是否可用
        /// </summary>
        /// <param name="graphicsDevice">图形设备</param>
        /// <returns>是否可用</returns>
        public static bool IsGraphicsDeviceAvailable(GraphicsDevice graphicsDevice)
        {
            try
            {
                return graphicsDevice != null && !graphicsDevice.IsDisposed;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 安全释放纹理资源
        /// </summary>
        /// <param name="texture">要释放的纹理</param>
        public static void SafeDisposeTexture(ref Texture2D texture)
        {
            try
            {
                if (texture != null && !texture.IsDisposed)
                {
                    texture.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SafeGraphicsHelper] 释放纹理时发生异常: {ex.Message}");
            }
            finally
            {
                texture = null;
            }
        }
    }
}