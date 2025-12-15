using System;
using System.Runtime.InteropServices;
using System.Threading;
using Platforms;
using Tools;

namespace WorldOfTheThreeKingdoms.Desktop
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();

        static bool isDebug = false; // 设置为 false 禁用调试控制台
        static bool autoCloseConsole = true; // 设置为 true 自动关闭控制台

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
                // 设置异常处理器
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(ExceptionHandler);
                
                if (isDebug)
                {
                    // 只在调试模式下分配控制台
                    AllocConsole();
                    Console.WriteLine("Starting WorldOfTheThreeKingdoms.Desktop...");
                }

                try
                {
                    if (isDebug) Console.WriteLine("Creating MainGame instance...");
                    
                    MainGame game = null;
                    try
                    {
                        game = new MainGame();
                        if (isDebug) Console.WriteLine("MainGame created successfully.");
                    }
                    catch (Exception ex)
                    {
                        if (isDebug)
                        {
                            Console.WriteLine($"Error creating MainGame: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                            if (ex.InnerException != null)
                            {
                                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                            }
                            Console.WriteLine("Press any key to exit...");
                            Console.ReadKey();
                        }
                        return;
                    }

                    using (game)
                    {
                        if (isDebug) Console.WriteLine("Running game...");
                        
                        // 如果启用自动关闭控制台，在游戏启动后关闭调试窗口
                        if (isDebug && autoCloseConsole)
                        {
                            Console.WriteLine("Game started successfully. Debug console will close in 2 seconds...");
                            System.Threading.Thread.Sleep(2000);
                            FreeConsole();
                        }
                        
                        game.Run();
                    }
                    
                    if (isDebug && !autoCloseConsole)
                    {
                        Console.WriteLine("Game finished normally.");
                    }
                }
                catch (Exception ex)
                {
                    if (isDebug)
                    {
                        Console.WriteLine($"Fatal error: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadKey();
                    }
                }
        }

        static void UIExceptionHandler(object sender, ThreadExceptionEventArgs args)
        {
            Exception e = (Exception)args.Exception;
            //Season.Current.OpenLink(WebTools.WebSite2 + "/service.aspx?mes=" + e.ToString() + "&platform=" + Season.PlatForm.ToString() + "&ver=" + Season.GameVersion.ToString());
            WebTools.TakeWarnMsg("RuntimeTerminating: " + args, "", e);
        }

        static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            //Season.Current.OpenLink(WebTools.WebSite2 + "/service.aspx?mes=" + e.ToString() + "&platform=" + Season.PlatForm.ToString() + "&ver=" + Season.GameVersion.ToString());
            WebTools.TakeWarnMsg("RuntimeTerminating: " + args.IsTerminating, "", e);
        }
    }
}
