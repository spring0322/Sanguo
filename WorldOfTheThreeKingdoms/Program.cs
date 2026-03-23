using GameManager;
using Platforms;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WorldOfTheThreeKingdoms.Tools;

namespace WorldOfTheThreeKingdoms
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Allow .NET 8 to read GB2312/GBK encoded legacy text files
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // 🔥 2026-03-13 修复：捕获异步任务的未处理异常
            // 注意：.NET 8 中 UnobservedTaskException 不会导致进程终止（与旧版 .NET Framework 不同）
            // 此处捕获是为了防止隐藏的异步错误导致游戏逻辑中断或状态不一致
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // 🔥 启用调试：部队销毁时打印堆栈跟踪
            // 用于诊断 ID=0 部队被销毁的问题
            WorldOfTheThreeKingdoms.Diagnostics.SerializationDebugConfig.EnableTroopDestroyStackTrace = true;
            System.Diagnostics.Debug.WriteLine("[调试] 已启用部队销毁堆栈跟踪");

            try
            {
                // Ensure the game looks for files in the .exe directory, not the system directory
                System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                if (Platform.PlatFormType == PlatFormType.Win || Platform.PlatFormType == PlatFormType.Desktop)
                {
                    // 🔥 2026-03-13 修复：启用全局异常处理器，确保发布版崩溃时生成日志
                    AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;

                    try
                    {
                        using MainGame game = new();
                        
                        if (System.Diagnostics.Debugger.IsAttached)
                        {
                            game.Run();
                        }
                        else
                        {
                            try
                            {
                                game.Run();
                            }
                            catch (Exception ex)
                            {
                                CrashReporter.ReportCrashAndTerminate(ex, "游戏循环");
                            }
                        }
                    }
                    catch (Exception initEx)
                    {
                        // 捕获游戏初始化阶段的错误
                        Console.WriteLine($"游戏初始化失败，可能是显卡驱动或DirectX问题：\n\n{initEx.Message}\n\n建议：\n1. 更新显卡驱动\n2. 安装最新的DirectX\n3. 重启计算机后重试");
                        CrashReporter.ReportCrashAndTerminate(initEx, "游戏初始化");
                    }
                }
                else if (Platform.PlatFormType == PlatFormType.UWP)
                {
                    Platform.Current.OpenFactory();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序启动时发生未捕获的异常：\n\n{ex.Message}\n\n堆栈跟踪：\n{ex.StackTrace}");
                CrashReporter.ReportCrashAndTerminate(ex, "程序启动");
            }
        }

        static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            CrashReporter.ReportCrashAndTerminate(e, "全局未处理异常");
        }

        static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            // 🔥 标记为已观察，防止异常在 GC 时被重新抛出
            args.SetObserved();
            
            // 记录异常但不终止进程（异步任务错误通常不是致命的）
            CrashReporter.ReportNonFatalException(args.Exception, "未观察的异步任务异常");
        }
    }
}
