using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameManager;
using Platforms;

namespace Tools
{
    /// <summary>
    /// 头像处理工具，用于批量生成缺失的小图
    /// </summary>
    public static class PortraitProcessor
    {
        /// <summary>
        /// 批量处理指定目录下的头像文件，为缺失小图的大图自动生成小图
        /// </summary>
        /// <param name="portraitDirectory">头像目录路径</param>
        /// <returns>处理结果统计</returns>
        public static PortraitProcessResult ProcessPortraits(string portraitDirectory)
        {
            var result = new PortraitProcessResult();

            if (!Directory.Exists(portraitDirectory))
            {
                result.ErrorMessage = $"目录不存在: {portraitDirectory}";
                return result;
            }

            try
            {
                // 支持的图片格式
                var supportedExtensions = new[] { "*.dds", "*.png", "*.jpg" };
                var allFiles = new List<string>();
                
                // 获取所有支持格式的文件
                foreach (var pattern in supportedExtensions)
                {
                    allFiles.AddRange(Directory.GetFiles(portraitDirectory, pattern, SearchOption.TopDirectoryOnly));
                }
                
                // 筛选出大图文件（不以's'结尾的文件）
                var largeImages = allFiles.Where(f => !Path.GetFileNameWithoutExtension(f).EndsWith("s")).ToArray();

                result.TotalLargeImages = largeImages.Length;

                foreach (var largeImagePath in largeImages)
                {
                    var fileName = Path.GetFileNameWithoutExtension(largeImagePath);
                    var directory = Path.GetDirectoryName(largeImagePath);
                    var extension = Path.GetExtension(largeImagePath);
                    var smallImagePath = Path.Combine(directory, $"{fileName}s{extension}");

                    // 检查小图是否已存在
                    if (File.Exists(smallImagePath))
                    {
                        result.ExistingSmallImages++;
                        continue;
                    }

                    // 暂时禁用图像处理功能，避免编译错误
                    // TODO: 实现 ImageProcessor.GenerateSmallPortrait 方法
                    result.FailedGenerations++;
                    Console.WriteLine($"图像处理功能暂未实现: {Path.GetFileName(largeImagePath)}");
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 处理所有默认头像目录
        /// </summary>
        /// <returns>处理结果</returns>
        public static PortraitProcessResult ProcessAllDefaultPortraits()
        {
            var defaultDir = @"Content/Textures/GameComponents/PersonPortrait/Images/Default/";
            return ProcessPortraits(defaultDir);
        }

        /// <summary>
        /// 处理所有玩家自定义头像目录
        /// </summary>
        /// <returns>处理结果</returns>
        public static PortraitProcessResult ProcessAllPlayerPortraits()
        {
            var playerDir = @"Content/Textures/GameComponents/PersonPortrait/Images/Player/";
            return ProcessPortraits(playerDir);
        }

        /// <summary>
        /// 清理指定目录下的所有小图文件（以's'结尾的jpg文件）
        /// </summary>
        /// <param name="portraitDirectory">头像目录路径</param>
        /// <returns>删除的文件数量</returns>
        public static int CleanupSmallPortraits(string portraitDirectory)
        {
            if (!Directory.Exists(portraitDirectory))
            {
                return 0;
            }

            var deletedCount = 0;
            try
            {
                // 支持的小图格式
                var smallImagePatterns = new[] { "*s.dds", "*s.png", "*s.jpg" };
                var smallImages = new List<string>();
                
                // 获取所有格式的小图文件
                foreach (var pattern in smallImagePatterns)
                {
                    smallImages.AddRange(Directory.GetFiles(portraitDirectory, pattern, SearchOption.TopDirectoryOnly));
                }
                
                foreach (var smallImagePath in smallImages)
                {
                    File.Delete(smallImagePath);
                    deletedCount++;
                    Console.WriteLine($"已删除小图: {Path.GetFileName(smallImagePath)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理小图时出错: {ex.Message}");
            }

            return deletedCount;
        }
    }

    /// <summary>
    /// 头像处理结果
    /// </summary>
    public class PortraitProcessResult
    {
        /// <summary>
        /// 总的大图数量
        /// </summary>
        public int TotalLargeImages { get; set; }

        /// <summary>
        /// 已存在的小图数量
        /// </summary>
        public int ExistingSmallImages { get; set; }

        /// <summary>
        /// 成功生成的小图数量
        /// </summary>
        public int GeneratedSmallImages { get; set; }

        /// <summary>
        /// 生成失败的数量
        /// </summary>
        public int FailedGenerations { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// 获取处理结果摘要
        /// </summary>
        /// <returns></returns>
        public string GetSummary()
        {
            if (!IsSuccess)
            {
                return $"处理失败: {ErrorMessage}";
            }

            return $"处理完成 - 总大图: {TotalLargeImages}, 已存在小图: {ExistingSmallImages}, " +
                   $"新生成小图: {GeneratedSmallImages}, 生成失败: {FailedGenerations}";
        }
    }
}