#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using GameManager;

namespace WorldOfTheThreeKingdoms.Tools;

/// <summary>
/// 崩溃报告系统 - 集成日志、存档备份、性能监控、线程堆栈转储
/// </summary>
public static class CrashReporter
{
    private static readonly string CrashLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashLogs");
    private static readonly string CrashSaveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashSaves");

    /// <summary>
    /// 记录崩溃信息并强制终止进程（带线程堆栈转储）
    /// </summary>
    /// <param name="e">异常对象（可为 null，用于超时/死锁场景）</param>
    /// <param name="context">崩溃上下文（如 "游戏初始化"、"游戏循环"、"存档加载"、"主线程超时"）</param>
    public static void ReportCrashAndTerminate(Exception? e, string context = "未知")
    {
        try
        {
            // 确保目录存在
            Directory.CreateDirectory(CrashLogDir);
            Directory.CreateDirectory(CrashSaveDir);

            string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HHh_mmm_sss");
            string logFileName = $"CrashLog_{timestamp}.log";
            string logPath = Path.Combine(CrashLogDir, logFileName);

            // C# 12: 使用 using 声明简化层级
            using var sw = new StreamWriter(logPath, false, Encoding.UTF8);

            WriteCrashHeader(sw, timestamp, context);
            
            // 🔥 如果有异常对象，记录异常详情
            if (e != null)
            {
                WriteExceptionDetails(sw, e);
            }
            else
            {
                sw.WriteLine("==================== 异常消息 ====================");
                sw.WriteLine("⚠️ 无异常对象（可能是超时/死锁/未响应导致的强制终止）");
                sw.WriteLine();
            }
            
            // 🔥 2026-03-15：条件启用线程堆栈转储
            // Debug 模式：使用分层防御策略获取详细信息
            // Release 模式：仅记录基本信息，避免 ExecutionEngineException
            #if DEBUG
            WriteSafeThreadStacks(sw);
            #else
            WriteBasicProcessInfo(sw);
            #endif
            
            WriteSystemInfo(sw, timestamp);
            WritePerformanceMetrics(sw);

            sw.Flush(); // 确保在 FailFast 之前写入磁盘

            // 🔥 尝试备份当前存档
            TryBackupCurrentSave(timestamp);

            Console.WriteLine($"==================== 致命错误 ====================");
            Console.WriteLine($"崩溃日志: {logPath}");
            Console.WriteLine($"请将 CrashLogs 文件夹提交给开发者");
            Console.WriteLine("程序将在 3 秒后强制终止...");
            
            System.Threading.Thread.Sleep(3000); // 给用户时间看到消息
        }
        catch (Exception logEx)
        {
            // 日志写入失败的后备方案
            Console.WriteLine("==================== 崩溃信息（日志写入失败）====================");
            Console.WriteLine($"原始异常: {e.Message}");
            Console.WriteLine($"堆栈: {e.StackTrace}");
            Console.WriteLine($"日志系统异常: {logEx.Message}");
        }
        finally
        {
            // 🔥 强制终止进程，跳过所有 Finalizers
            // 防止 MonoGame 渲染器或内存堆栈损坏导致次生崩溃/死锁
            Environment.FailFast($"[{context}] 发生致命异常并已记录，强制终止以保护系统状态。", e);
        }
    }

    /// <summary>
    /// 记录非致命异常（不终止进程）
    /// </summary>
    public static void ReportNonFatalException(Exception e, string context = "未知")
    {
        try
        {
            Directory.CreateDirectory(CrashLogDir);

            string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HHh_mmm_sss");
            string logFileName = $"Error_{timestamp}.log";
            string logPath = Path.Combine(CrashLogDir, logFileName);

            using var sw = new StreamWriter(logPath, false, Encoding.UTF8);

            sw.WriteLine("==================== 非致命错误 ====================");
            sw.WriteLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sw.WriteLine($"上下文: {context}");
            sw.WriteLine();

            WriteExceptionDetails(sw, e);
            WriteSystemInfo(sw, DateTime.Now.ToString("yyyy_MM_dd_HHh_mmm_sss"));

            sw.Flush();

            DebugLogger.Error(DebugLogger.LogCategory.General, $"非致命错误已记录: {logPath}");
        }
        catch (Exception logEx)
        {
            // 静默失败，但输出到调试器
            System.Diagnostics.Debug.WriteLine($"⚠️ 非致命错误日志记录失败: {logEx.Message}");
        }
    }

    private static void WriteCrashHeader(StreamWriter sw, string timestamp, string context)
    {
        sw.WriteLine("==================== 崩溃信息 (AOT 优化版) ====================");
        sw.WriteLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sw.WriteLine($"崩溃上下文: {context}");
        
        // 🔥 AOT 优化：使用 FileVersionInfo 替代反射
        string processPath = Environment.ProcessPath ?? string.Empty;
        string version = "未知版本";
        if (!string.IsNullOrEmpty(processPath))
        {
            try
            {
                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(processPath);
                version = fileInfo.ProductVersion ?? fileInfo.FileVersion ?? "未知版本";
            }
            catch
            {
                version = "版本信息获取失败";
            }
        }
        sw.WriteLine($"游戏版本: 中华三国志 {version}");
        sw.WriteLine();
    }

    private static void WriteExceptionDetails(StreamWriter sw, Exception e)
    {
        sw.WriteLine("==================== 异常消息 ====================");
        sw.WriteLine(e.Message);
        sw.WriteLine();

        sw.WriteLine("==================== 堆栈跟踪 ====================");
        sw.WriteLine("⚠️ Native AOT 环境下需要 .pdb 文件才能解析行号和文件名");
        sw.WriteLine(e.StackTrace ?? "无堆栈信息");
        sw.WriteLine();

        // 递归记录所有内部异常
        Exception? inner = e.InnerException;
        int depth = 1;
        while (inner != null)
        {
            sw.WriteLine($"==================== 内部异常 (深度 {depth}) ====================");
            sw.WriteLine($"类型: {inner.GetType().FullName}");
            sw.WriteLine($"消息: {inner.Message}");
            sw.WriteLine("堆栈:");
            sw.WriteLine(inner.StackTrace ?? "无堆栈信息");
            sw.WriteLine();

            inner = inner.InnerException;
            depth++;

            if (depth > 10) // 防止无限循环
            {
                sw.WriteLine("⚠️ 内部异常链过长，已截断");
                break;
            }
        }
    }

    /// <summary>
    /// 安全地记录线程信息（分层防御策略）
    /// 🔥 2026-03-15：使用多层 try-catch 保护，避免 ExecutionEngineException
    /// </summary>
    private static void WriteSafeThreadStacks(StreamWriter sw)
    {
        sw.WriteLine("==================== 线程信息（安全模式）====================");
        sw.WriteLine("⚠️ 使用分层防御策略，避免 ExecutionEngineException");
        sw.WriteLine();

        try
        {
            Process currentProcess = Process.GetCurrentProcess();
            
            // 第 1 层：基本进程信息（最安全）
            try
            {
                sw.WriteLine($"进程 ID: {currentProcess.Id}");
                sw.WriteLine($"线程总数: {currentProcess.Threads.Count}");
                sw.WriteLine();
            }
            catch (Exception ex)
            {
                sw.WriteLine($"⚠️ 基本进程信息获取失败: {ex.GetType().Name}");
                return;  // 如果连基本信息都获取不了，直接返回
            }

            // 第 2 层：当前线程堆栈（中等风险）
            sw.WriteLine("--- 当前线程（崩溃报告线程）---");
            try
            {
                // 🔥 关键：使用 skipFrames 跳过崩溃报告自身的帧
                // fNeedFileInfo: false 避免访问文件信息（AOT 环境下可能失败）
                StackTrace currentStackTrace = new(skipFrames: 2, fNeedFileInfo: false);
                sw.WriteLine(currentStackTrace.ToString());
            }
            catch (Exception ex)
            {
                sw.WriteLine($"⚠️ 当前线程堆栈获取失败: {ex.GetType().Name} - {ex.Message}");
            }
            sw.WriteLine();

            // 第 3 层：所有线程基本信息（逐个保护）
            sw.WriteLine("--- 所有线程基本信息 ---");
            int successCount = 0;
            int failCount = 0;

            foreach (ProcessThread thread in currentProcess.Threads)
            {
                try
                {
                    sw.WriteLine($"线程 ID: {thread.Id}");
                    
                    // 🔥 安全访问：每个属性都单独保护
                    TryWriteThreadProperty(sw, "状态", () => thread.ThreadState.ToString());
                    TryWriteThreadProperty(sw, "优先级", () => thread.PriorityLevel.ToString());
                    TryWriteThreadProperty(sw, "CPU 时间", () => thread.TotalProcessorTime.ToString());
                    
                    // 🔥 高风险属性：StartTime 可能触发异常
                    try
                    {
                        sw.WriteLine($"  启动时间: {thread.StartTime:yyyy-MM-dd HH:mm:ss}");
                    }
                    catch (InvalidOperationException)
                    {
                        sw.WriteLine("  启动时间: (线程已终止)");
                    }
                    catch
                    {
                        sw.WriteLine("  启动时间: (无法访问)");
                    }
                    
                    sw.WriteLine();
                    successCount++;
                }
                catch (Exception threadEx)
                {
                    sw.WriteLine($"⚠️ 线程信息获取失败: {threadEx.GetType().Name}");
                    sw.WriteLine();
                    failCount++;
                }
            }

            sw.WriteLine($"线程信息统计: 成功 {successCount}, 失败 {failCount}");
            sw.WriteLine();
            
            sw.WriteLine("💡 提示：如需完整堆栈跟踪，请使用外部工具：");
            sw.WriteLine("  - WinDbg: !threads, ~*k");
            sw.WriteLine("  - PerfView: 捕获 ETW 事件");
            sw.WriteLine("  - Visual Studio: 调试 > 窗口 > 线程");
        }
        catch (Exception ex)
        {
            sw.WriteLine($"⚠️ 线程信息转储失败: {ex.GetType().Name} - {ex.Message}");
        }

        sw.WriteLine();
    }

    /// <summary>
    /// 安全地写入线程属性
    /// </summary>
    private static void TryWriteThreadProperty(StreamWriter sw, string propertyName, Func<string> getValue)
    {
        try
        {
            sw.WriteLine($"  {propertyName}: {getValue()}");
        }
        catch
        {
            sw.WriteLine($"  {propertyName}: (无法访问)");
        }
    }

    /// <summary>
    /// 记录基本进程信息（安全版本，避免 ExecutionEngineException）
    /// </summary>
    private static void WriteBasicProcessInfo(StreamWriter sw)
    {
        sw.WriteLine("==================== 基本进程信息 ====================");
        sw.WriteLine("⚠️ 线程堆栈转储已禁用，避免 ExecutionEngineException");
        sw.WriteLine();

        try
        {
            Process currentProcess = Process.GetCurrentProcess();
            
            sw.WriteLine($"进程 ID: {currentProcess.Id}");
            
            try
            {
                sw.WriteLine($"线程总数: {currentProcess.Threads.Count}");
            }
            catch (Exception ex)
            {
                sw.WriteLine($"线程总数: (无法获取 - {ex.GetType().Name})");
            }
            
            sw.WriteLine();
            sw.WriteLine("💡 提示：如需详细线程堆栈信息，请使用以下工具：");
            sw.WriteLine("  - WinDbg: 附加到进程并使用 !threads 命令");
            sw.WriteLine("  - PerfView: 捕获 ETW 事件和堆栈跟踪");
            sw.WriteLine("  - Visual Studio: 调试 > 窗口 > 线程");
        }
        catch (Exception ex)
        {
            sw.WriteLine($"⚠️ 进程信息获取失败: {ex.GetType().Name} - {ex.Message}");
        }

        sw.WriteLine();
    }

    /// <summary>
    /// 记录所有线程的堆栈跟踪（用于诊断死锁/无限循环/未响应）
    /// ⚠️ 已禁用：可能触发 ExecutionEngineException
    /// </summary>
    private static void WriteAllThreadStacks(StreamWriter sw)
    {
        sw.WriteLine("==================== 所有线程堆栈跟踪 ====================");
        sw.WriteLine("⚠️ 用于诊断死锁、无限循环、主线程未响应等问题");
        sw.WriteLine();

        try
        {
            Process currentProcess = Process.GetCurrentProcess();
            
            sw.WriteLine($"进程 ID: {currentProcess.Id}");
            sw.WriteLine($"线程总数: {currentProcess.Threads.Count}");
            sw.WriteLine();

            // 🔥 记录当前线程（崩溃报告线程）的堆栈
            sw.WriteLine("--- 当前线程（崩溃报告线程）堆栈 ---");
            try
            {
                StackTrace currentStackTrace = new(true);
                sw.WriteLine(currentStackTrace.ToString());
            }
            catch (Exception ex)
            {
                sw.WriteLine($"⚠️ 无法获取当前线程堆栈: {ex.Message}");
            }
            sw.WriteLine();

            // 遍历所有进程线程（记录基本信息）
            sw.WriteLine("--- 所有进程线程信息 ---");
            foreach (ProcessThread thread in currentProcess.Threads)
            {
                try
                {
                    sw.WriteLine($"线程 ID: {thread.Id}");
                    sw.WriteLine($"  状态: {thread.ThreadState}");
                    sw.WriteLine($"  优先级: {thread.PriorityLevel}");
                    sw.WriteLine($"  CPU 时间: {thread.TotalProcessorTime}");
                    
                    // 🔥 修复：StartTime 访问可能触发 ExecutionEngineException
                    // 原因：系统线程或已终止线程的 StartTime 不可访问
                    // 日期：2026-03-13
                    try
                    {
                        sw.WriteLine($"  启动时间: {thread.StartTime:yyyy-MM-dd HH:mm:ss}");
                    }
                    catch (InvalidOperationException)
                    {
                        sw.WriteLine("  启动时间: (线程已终止或无权限访问)");
                    }
                    catch (Exception ex)
                    {
                        sw.WriteLine($"  启动时间: (获取失败: {ex.GetType().Name})");
                    }
                    
                    sw.WriteLine();
                }
                catch (Exception threadEx)
                {
                    // 🔥 防止单个线程信息获取失败导致整个转储中断
                    sw.WriteLine($"⚠️ 线程 {thread.Id} 信息获取失败: {threadEx.GetType().Name} - {threadEx.Message}");
                    sw.WriteLine();
                }
            }
            
            sw.WriteLine("⚠️ 注意：AOT 环境下无法通过反射获取托管线程的详细堆栈跟踪");
            sw.WriteLine("⚠️ 如需完整堆栈信息，请使用调试器或性能分析工具");
        }
        catch (Exception ex)
        {
            sw.WriteLine($"⚠️ 线程堆栈转储失败: {ex.Message}");
            sw.WriteLine(ex.StackTrace ?? "无堆栈信息");
        }

        sw.WriteLine();
    }

    private static void WriteSystemInfo(StreamWriter sw, string timestamp)
    {
        sw.WriteLine("==================== 系统信息 ====================");
        
        // 🔥 .NET 8 现代 API：替代老旧的 Environment.OSVersion
        sw.WriteLine($"操作系统: {RuntimeInformation.OSDescription}");
        sw.WriteLine($"运行时框架: {RuntimeInformation.FrameworkDescription}");
        sw.WriteLine($"运行架构: {RuntimeInformation.RuntimeIdentifier}");
        sw.WriteLine($"进程架构: {RuntimeInformation.ProcessArchitecture}");
        sw.WriteLine($"系统架构: {RuntimeInformation.OSArchitecture}");
        sw.WriteLine($"逻辑处理器数: {Environment.ProcessorCount}");
        sw.WriteLine($"系统页面大小: {Environment.SystemPageSize} bytes");
        sw.WriteLine($"工作目录: {Environment.CurrentDirectory}");
        sw.WriteLine();
        
        // 🔥 救援存档功能
        WriteRescueSave(sw, timestamp);
    }

    private static void WritePerformanceMetrics(StreamWriter sw)
    {
        sw.WriteLine("==================== 性能指标 ====================");

        try
        {
            Process currentProcess = Process.GetCurrentProcess();

            // 内存使用
            long workingSetMB = currentProcess.WorkingSet64 / 1024 / 1024;
            long privateMemoryMB = currentProcess.PrivateMemorySize64 / 1024 / 1024;
            long virtualMemoryMB = currentProcess.VirtualMemorySize64 / 1024 / 1024;

            sw.WriteLine($"工作集内存 (Working Set): {workingSetMB} MB");
            sw.WriteLine($"私有内存 (Private Memory): {privateMemoryMB} MB");
            sw.WriteLine($"虚拟内存 (Virtual Memory): {virtualMemoryMB} MB");

            // CPU 时间
            sw.WriteLine($"总 CPU 时间: {currentProcess.TotalProcessorTime}");
            sw.WriteLine($"用户 CPU 时间: {currentProcess.UserProcessorTime}");

            // 线程和句柄
            sw.WriteLine($"线程数: {currentProcess.Threads.Count}");
            sw.WriteLine($"句柄数: {currentProcess.HandleCount}");

            // GC 信息
            sw.WriteLine($"GC Gen0 回收次数: {GC.CollectionCount(0)}");
            sw.WriteLine($"GC Gen1 回收次数: {GC.CollectionCount(1)}");
            sw.WriteLine($"GC Gen2 回收次数: {GC.CollectionCount(2)}");
            sw.WriteLine($"托管堆总内存: {GC.GetTotalMemory(false) / 1024 / 1024} MB");

            // 运行时间
            sw.WriteLine($"进程运行时间: {DateTime.Now - currentProcess.StartTime}");
        }
        catch (Exception ex)
        {
            sw.WriteLine($"⚠️ 性能指标获取失败: {ex.Message}");
        }

        sw.WriteLine();
    }

    /// <summary>
    /// 尝试生成救援存档（崩溃时的最后一次保存）
    /// </summary>
    private static void WriteRescueSave(StreamWriter sw, string timestamp)
    {
        sw.WriteLine("==================== 救援存档 ====================");
        
        try
        {
            // 🔥 1. 状态拦截：必须前置判断 Session 和核心对象是否存活
            // 防止引发 NullReferenceException 二次崩溃
            if (Session.MainGame != null)
            {
                string savePath = Path.Combine(Environment.CurrentDirectory, $"CrashSave_{timestamp}.sav");
                sw.WriteLine($"正在尝试生成救援存档: {savePath}");
                
                // 🔥 2. 同步执行：在崩溃上下文中，绝对不要使用 Task.Run 或 async/await
                // 必须阻塞当前线程直接写入
                // 注：SaveGameWhenCrash 内部的序列化方案已兼容 AOT（使用 System.Text.Json 源生成器或 BinaryWriter）
                Session.MainGame.SaveGameWhenCrash(savePath);
                
                sw.WriteLine("✅ 救援存档生成成功。");
            }
            else
            {
                sw.WriteLine("⚠️ 当前未实例化主游戏会话，跳过救援存档。");
            }
        }
        catch (Exception saveEx)
        {
            // 🔥 3. 异常吞噬：内存状态如果已经严重损坏（如字典正在扩容时崩溃），序列化必然失败
            // 此时只需记录原因，绝不能向上抛出，必须让程序顺利进入 FailFast
            sw.WriteLine($"❌ 救援存档生成失败 (内存状态可能已严重损坏): {saveEx.Message}");
            sw.WriteLine(saveEx.StackTrace ?? "无堆栈信息");
        }
        
        sw.WriteLine();
    }

    private static void TryBackupCurrentSave(string timestamp)
    {
        try
        {
            // 🔥 尝试查找最近的存档文件
            string saveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Save");
            if (!Directory.Exists(saveDir))
                return;

            string[] saveFiles = Directory.GetFiles(saveDir, "*.sav");
            if (saveFiles.Length == 0)
                return;

            // 找到最新的存档
            string? latestSave = null;
            DateTime latestTime = DateTime.MinValue;

            foreach (string saveFile in saveFiles)
            {
                FileInfo fi = new(saveFile);
                if (fi.LastWriteTime > latestTime)
                {
                    latestTime = fi.LastWriteTime;
                    latestSave = saveFile;
                }
            }

            if (latestSave != null)
            {
                string backupName = $"CrashSave_{timestamp}_{Path.GetFileName(latestSave)}";
                string backupPath = Path.Combine(CrashSaveDir, backupName);
                File.Copy(latestSave, backupPath, true);

                Console.WriteLine($"已备份崩溃存档: {backupPath}");
            }
        }
        catch (Exception backupEx)
        {
            // 记录备份失败，但不影响崩溃报告主流程
            Console.WriteLine($"⚠️ 存档备份失败: {backupEx.Message}");
        }
    }
}
