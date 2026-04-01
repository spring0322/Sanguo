using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace WorldOfTheThreeKingdoms.Helpers
{
    public static class DDSLoader
    {
        private const int DDS_SIGNATURE = 0x20534444;

        // FourCC Codes
        private const int FOURCC_DXT1 = 0x31545844;
        private const int FOURCC_DXT3 = 0x33545844;
        private const int FOURCC_DXT5 = 0x35545844;
        private const int FOURCC_DX10 = 0x30315844; // "DX10"

        // Pixel Format Flags
        private const int DDPF_ALPHAPIXELS = 0x00000001;
        private const int DDPF_ALPHA       = 0x00000002;
        private const int DDPF_FOURCC      = 0x00000004;
        private const int DDPF_RGB         = 0x00000040;
        private const int DDPF_YUV         = 0x00000200;
        private const int DDPF_LUMINANCE   = 0x00020000;

        public static Texture2D Load(GraphicsDevice graphicsDevice, string filePath)
        {
            // 🔥 Fail-Fast：GraphicsDevice 必须已初始化
            // 日期：2026-03-31
            // 原因：在 GraphicsDeviceManager 初始化之前调用会导致崩溃
            if (graphicsDevice == null)
            {
                throw new InvalidOperationException(
                    $"[DDSLoader] GraphicsDevice 未初始化，无法加载 DDS 文件: {filePath}");
            }

            try
            {
                if (!File.Exists(filePath)) return null;

                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // 1. Magic Number
                    if (reader.ReadInt32() != DDS_SIGNATURE) return null;

                    // 2. DDS_HEADER (124 bytes)
                    int size = reader.ReadInt32(); // must be 124
                    int flags = reader.ReadInt32();
                    int height = reader.ReadInt32();
                    int width = reader.ReadInt32();
                    int pitchOrLinearSize = reader.ReadInt32(); // 压缩图是大小，未压缩是行距
                    int depth = reader.ReadInt32();
                    int mipMapCount = reader.ReadInt32();
                    reader.ReadBytes(44); // reserved1[11]

                    // 3. DDS_PIXELFORMAT (32 bytes)
                    int pfSize = reader.ReadInt32(); // must be 32
                    int pfFlags = reader.ReadInt32();
                    int fourCC = reader.ReadInt32();
                    int rgbBitCount = reader.ReadInt32();
                    uint rBitMask = reader.ReadUInt32();
                    uint gBitMask = reader.ReadUInt32();
                    uint bBitMask = reader.ReadUInt32();
                    uint aBitMask = reader.ReadUInt32();

                    // 4. Header 剩余部分
                    reader.ReadBytes(16); // caps
                    reader.ReadBytes(4);  // reserved2

                    // --- 解析格式 ---
                    SurfaceFormat format = SurfaceFormat.Color;
                    bool isCompressed = false;
                    int blockSize = 0;

                    // A. 处理 FourCC (压缩格式 或 DX10扩展)
                    if ((pfFlags & DDPF_FOURCC) != 0)
                    {
                        if (fourCC == FOURCC_DX10)
                        {
                            // === 解析 DX10 扩展头 (20 bytes) ===
                            int dxgiFormat = reader.ReadInt32();
                            int resourceDimension = reader.ReadInt32();
                            int miscFlag = reader.ReadInt32();
                            int arraySize = reader.ReadInt32();
                            int miscFlags2 = reader.ReadInt32();

                            // 映射 DXGI Format 到 MonoGame SurfaceFormat
                            // 71=BC1(DXT1), 74=BC2(DXT3), 77=BC3(DXT5)
                            // 28=R8G8B8A8_UNORM
                            switch (dxgiFormat)
                            {
                                case 70: // BC1_TYPELESS
                                case 71: // BC1_UNORM (DXT1)
                                case 72: // BC1_UNORM_SRGB
                                    format = SurfaceFormat.Dxt1;
                                    isCompressed = true;
                                    blockSize = 8;
                                    break;
                                case 73: // BC2_TYPELESS
                                case 74: // BC2_UNORM (DXT3)
                                case 75: // BC2_UNORM_SRGB
                                    format = SurfaceFormat.Dxt3;
                                    isCompressed = true;
                                    blockSize = 16;
                                    break;
                                case 76: // BC3_TYPELESS
                                case 77: // BC3_UNORM (DXT5)
                                case 78: // BC3_UNORM_SRGB
                                    format = SurfaceFormat.Dxt5;
                                    isCompressed = true;
                                    blockSize = 16;
                                    break;
                                case 28: // R8G8B8A8_UNORM
                                    format = SurfaceFormat.Color;
                                    isCompressed = false;
                                    break;
                                case 87: // B8G8R8A8_UNORM (常见)
                                    // 注意：MonoGame可能不支持ColorBgraEXT，如果编译错误就用Color
                                    format = SurfaceFormat.Color; // 如果颜色反了再考虑其他方案
                                    isCompressed = false;
                                    break;
                                default:
                                    return null; // 不支持的其他 DX10 格式
                            }
                        }
                        else
                        {
                            // === 标准 FourCC ===
                            isCompressed = true;
                            switch (fourCC)
                            {
                                case FOURCC_DXT1:
                                    format = SurfaceFormat.Dxt1;
                                    blockSize = 8;
                                    break;
                                case FOURCC_DXT3:
                                    format = SurfaceFormat.Dxt3;
                                    blockSize = 16;
                                    break;
                                case FOURCC_DXT5:
                                    format = SurfaceFormat.Dxt5;
                                    blockSize = 16;
                                    break;
                                case 111: // R16F (Single channel float)
                                    format = SurfaceFormat.Single;
                                    isCompressed = false;
                                    break;
                                case 114: // R32F
                                    format = SurfaceFormat.Single; // Close enough?
                                    isCompressed = false;
                                    break;
                                default:
                                    return null; // 未知 FourCC
                            }
                        }
                    }
                    // B. 处理未压缩 RGB/RGBA (旧标准)
                    else if ((pfFlags & DDPF_RGB) != 0)
                    {
                        if (rgbBitCount == 32)
                        {
                            // 判断是 RGBA 还是 BGRA
                            // 红色掩码: 0x00FF0000 -> BGR (SurfaceFormat.Color 默认是 RGBA 还是 BGRA 取决于平台，MonoGame 通常是 RGBA)
                            // 红色掩码: 0x000000FF -> RGB
                            // 这里做一个简单假设，大部分未压缩 DDS 是 BGRA (Windows 标准)
                            if (rBitMask == 0x00FF0000) 
                            {
                                // 尝试使用 BGRA 格式，如果 MonoGame 版本不支持，可能需要手动 Swizzle
                                // 注意：SurfaceFormat.Color 在 XNA/MonoGame 中通常定义为 R8G8B8A8
                                // 但显卡硬件通常是 BGRA。这部分比较 tricky。
                                // 为了稳妥，我们假设它是 Color，如果颜色反了（红蓝互换），说明需要 ColorBgraEXT
                                format = SurfaceFormat.Color; 
                            }
                            else
                            {
                                format = SurfaceFormat.Color;
                            }
                        }
                        else if (rgbBitCount == 24)
                        {
                            // MonoGame 不直接支持 24位纹理，需要填充 alpha，这里简单返回 null 让 PNG 处理
                            return null; 
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null; // 其他怪异格式 (YUV, Luminance)
                    }

                    // 5. 创建 Texture
                    // 修正尺寸：DXT 格式要求尺寸至少为 4x4，或者 blockSize
                    int texWidth = Math.Max(1, width);
                    int texHeight = Math.Max(1, height);
                    Texture2D texture = new Texture2D(graphicsDevice, texWidth, texHeight, false, format);

                    // 6. 计算数据长度
                    int dataSize;
                    if (isCompressed)
                    {
                        int blockCountX = (texWidth + 3) / 4;
                        int blockCountY = (texHeight + 3) / 4;
                        dataSize = blockCountX * blockCountY * blockSize;
                    }
                    else
                    {
                        // 未压缩：RowPitch * Height
                        // DDS Header 中的 pitchOrLinearSize 可能是行距
                        // 但为了保险，我们手动计算：Width * BytesPerPixel
                        int bytesPerPixel = rgbBitCount / 8;
                        dataSize = texWidth * texHeight * bytesPerPixel;
                    }

                    // 7. 安全读取数据
                    // 确保不会读过头 (Stream 可能包含 mipmaps)
                    // 只读我们需要的主表面数据，剩下的忽略（mipmap数据）
                    long remainingBytes = stream.Length - stream.Position;
                    if (remainingBytes < dataSize)
                    {
                        // 如果剩余数据不足，调整读取大小为实际可用数据
                        dataSize = (int)remainingBytes;
                    }

                    byte[] data = reader.ReadBytes(dataSize);

                    // 8. 填充
                    // 如果是 BGRA 但我们用了 RGBA 格式，可能需要在这里手动交换 R/B 字节
                    // 但考虑到性能，如果不严重影响观感（红脸关羽变成蓝脸），先不处理
                    texture.SetData(data);

                    return texture;
                }
            }
            catch (Exception ex)
            {
                // 记录DDS加载失败的详细信息
                System.Diagnostics.Debug.WriteLine($"[DDSLoader] DDS加载失败: {filePath}");
                System.Diagnostics.Debug.WriteLine($"[DDSLoader] 异常信息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DDSLoader] 异常堆栈: {ex.StackTrace}");
                
                // 🔥 检测GPU设备丢失错误
                bool isGpuError = ex.GetType().FullName.Contains("SharpDX") ||
                                  ex.Message.Contains("DEVICE_REMOVED") ||
                                  ex.Message.Contains("0x887A") ||
                                  ex.Message.Contains("D3D11") ||
                                  ex.Message.Contains("DXGI_ERROR");
                
                if (isGpuError)
                {
                    System.Diagnostics.Debug.WriteLine($"[DDSLoader] 检测到GPU设备错误，标记设备丢失");
                    global::GameManager.CacheManager.MarkDeviceLost(ex);
                }
                
                return null;
            }
        }

        /// <summary>
        /// 尝试从流加载DDS纹理
        /// </summary>
        /// <param name="stream">DDS文件流</param>
        /// <param name="graphicsDevice">图形设备</param>
        /// <returns>加载的纹理，如果失败返回null</returns>
        public static Texture2D LoadDDSFromStream(Stream stream, GraphicsDevice graphicsDevice)
        {
            try
            {
                // 确保流从开始位置读取
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }
                
                using (BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
                {
                    // 1. Magic Number
                    if (reader.ReadInt32() != DDS_SIGNATURE) return null;

                    // 2. DDS_HEADER (124 bytes)
                    int size = reader.ReadInt32(); // must be 124
                    int flags = reader.ReadInt32();
                    int height = reader.ReadInt32();
                    int width = reader.ReadInt32();
                    int pitchOrLinearSize = reader.ReadInt32(); // 压缩图是大小，未压缩是行距
                    int depth = reader.ReadInt32();
                    int mipMapCount = reader.ReadInt32();
                    reader.ReadBytes(44); // reserved1[11]

                    // 3. DDS_PIXELFORMAT (32 bytes)
                    int pfSize = reader.ReadInt32(); // must be 32
                    int pfFlags = reader.ReadInt32();
                    int fourCC = reader.ReadInt32();
                    int rgbBitCount = reader.ReadInt32();
                    uint rBitMask = reader.ReadUInt32();
                    uint gBitMask = reader.ReadUInt32();
                    uint bBitMask = reader.ReadUInt32();
                    uint aBitMask = reader.ReadUInt32();

                    // 4. Header 剩余部分
                    reader.ReadBytes(16); // caps
                    reader.ReadBytes(4);  // reserved2

                    // --- 解析格式 ---
                    SurfaceFormat format = SurfaceFormat.Color;
                    bool isCompressed = false;
                    int blockSize = 0;

                    // A. 处理 FourCC (压缩格式 或 DX10扩展)
                    if ((pfFlags & DDPF_FOURCC) != 0)
                    {
                        if (fourCC == FOURCC_DX10)
                        {
                            // === 解析 DX10 扩展头 (20 bytes) ===
                            int dxgiFormat = reader.ReadInt32();
                            int resourceDimension = reader.ReadInt32();
                            int miscFlag = reader.ReadInt32();
                            int arraySize = reader.ReadInt32();
                            int miscFlags2 = reader.ReadInt32();

                            // 映射 DXGI Format 到 MonoGame SurfaceFormat
                            switch (dxgiFormat)
                            {
                                case 70: // BC1_TYPELESS
                                case 71: // BC1_UNORM (DXT1)
                                case 72: // BC1_UNORM_SRGB
                                    format = SurfaceFormat.Dxt1;
                                    isCompressed = true;
                                    blockSize = 8;
                                    break;
                                case 73: // BC2_TYPELESS
                                case 74: // BC2_UNORM (DXT3)
                                case 75: // BC2_UNORM_SRGB
                                    format = SurfaceFormat.Dxt3;
                                    isCompressed = true;
                                    blockSize = 16;
                                    break;
                                case 76: // BC3_TYPELESS
                                case 77: // BC3_UNORM (DXT5)
                                case 78: // BC3_UNORM_SRGB
                                    format = SurfaceFormat.Dxt5;
                                    isCompressed = true;
                                    blockSize = 16;
                                    break;
                                case 28: // R8G8B8A8_UNORM
                                    format = SurfaceFormat.Color;
                                    isCompressed = false;
                                    break;
                                case 87: // B8G8R8A8_UNORM (常见)
                                    format = SurfaceFormat.Color; // 如果颜色反了再考虑其他方案
                                    isCompressed = false;
                                    break;
                                default:
                                    return null; // 不支持的其他 DX10 格式
                            }
                        }
                        else
                        {
                            // === 标准 FourCC ===
                            isCompressed = true;
                            switch (fourCC)
                            {
                                case FOURCC_DXT1:
                                    format = SurfaceFormat.Dxt1;
                                    blockSize = 8;
                                    break;
                                case FOURCC_DXT3:
                                    format = SurfaceFormat.Dxt3;
                                    blockSize = 16;
                                    break;
                                case FOURCC_DXT5:
                                    format = SurfaceFormat.Dxt5;
                                    blockSize = 16;
                                    break;
                                case 111: // R16F (Single channel float)
                                    format = SurfaceFormat.Single;
                                    isCompressed = false;
                                    break;
                                case 114: // R32F
                                    format = SurfaceFormat.Single; // Close enough?
                                    isCompressed = false;
                                    break;
                                default:
                                    return null; // 未知 FourCC
                            }
                        }
                    }
                    // B. 处理未压缩 RGB/RGBA (旧标准)
                    else if ((pfFlags & DDPF_RGB) != 0)
                    {
                        if (rgbBitCount == 32)
                        {
                            if (rBitMask == 0x00FF0000) 
                            {
                                format = SurfaceFormat.Color; 
                            }
                            else
                            {
                                format = SurfaceFormat.Color;
                            }
                        }
                        else if (rgbBitCount == 24)
                        {
                            return null; 
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null; // 其他怪异格式 (YUV, Luminance)
                    }

                    // 5. 创建 Texture
                    int texWidth = Math.Max(1, width);
                    int texHeight = Math.Max(1, height);
                    Texture2D texture = new Texture2D(graphicsDevice, texWidth, texHeight, false, format);

                    // 6. 计算数据长度
                    int dataSize;
                    if (isCompressed)
                    {
                        int blockCountX = (texWidth + 3) / 4;
                        int blockCountY = (texHeight + 3) / 4;
                        dataSize = blockCountX * blockCountY * blockSize;
                    }
                    else
                    {
                        int bytesPerPixel = rgbBitCount / 8;
                        dataSize = texWidth * texHeight * bytesPerPixel;
                    }

                    // 7. 安全读取数据
                    // 只读我们需要的主表面数据，剩下的忽略（mipmap数据）
                    long remainingBytes = stream.Length - stream.Position;
                    if (remainingBytes < dataSize)
                    {
                        // 如果剩余数据不足，调整读取大小为实际可用数据
                        dataSize = (int)remainingBytes;
                    }

                    byte[] data = reader.ReadBytes(dataSize);
                    texture.SetData(data);

                    return texture;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        /// <summary>
        /// 检查文件是否为DDS格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>如果是DDS格式返回true</returns>
        public static bool IsDDSFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;
                    
                using (var stream = File.OpenRead(filePath))
                {
                    if (stream.Length < 4)
                        return false;
                        
                    using (var reader = new BinaryReader(stream))
                    {
                        return reader.ReadInt32() == DDS_SIGNATURE;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}