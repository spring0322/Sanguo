using System;
using System.IO;
using GameObjects;

namespace WorldOfTheThreeKingdomsEditor.Core;

/// <summary>
/// CommonData 加载器 - 编辑器专用包装类
/// 
/// 设计说明：
/// - 编辑器通过项目引用使用主程序的 WorldOfTheThreeKingdoms.Serialization.CommonDataLoader
/// - 本类提供简化的接口，符合编辑器使用场景
/// - 使用 System.Text.Json + GameJsonContext 实现 AOT 兼容
/// 
/// 日期：2026-04-01
/// </summary>
public static class CommonDataLoader
{
    /// <summary>
    /// 加载 CommonData.json 文件
    /// </summary>
    /// <param name="filePath">CommonData.json 文件路径</param>
    /// <returns>加载的 CommonData 对象</returns>
    /// <exception cref="FileNotFoundException">文件不存在</exception>
    /// <exception cref="InvalidDataException">数据格式错误或反序列化失败</exception>
    /// <remarks>
    /// 错误处理策略：Fail-Fast
    /// - 文件不存在：抛出 FileNotFoundException
    /// - 数据损坏：抛出 InvalidDataException
    /// - 编辑器无法在 CommonData 加载失败时继续运行
    /// </remarks>
    public static CommonData LoadCommonData(string filePath)
    {
        // 🔥 Fail-Fast：文件不存在时立即失败
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"CommonData 文件不存在，编辑器无法启动。\n文件路径: {filePath}",
                filePath);
        }

        try
        {
            // 🔥 关键：使用主程序的 CommonDataLoader（AOT 兼容）
            // 主程序的 CommonDataLoader 使用 System.Text.Json + GameJsonContext
            return WorldOfTheThreeKingdoms.Serialization.CommonDataLoader.LoadFromFile(filePath);
        }
        catch (InvalidDataException)
        {
            // 重新抛出，保留原始异常信息
            throw;
        }
        catch (Exception ex)
        {
            // 🔥 Fail-Fast：任何其他错误都包装为 InvalidDataException
            throw new InvalidDataException(
                $"CommonData 加载失败，编辑器无法启动。\n文件路径: {filePath}\n错误: {ex.Message}",
                ex);
        }
    }
}
