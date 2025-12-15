# 强制显示游戏窗口脚本
# Force Game Window to Front Script

Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    
    public class WindowAPI {
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_SHOWMINNOACTIVE = 7;
        public const int SW_SHOWNA = 8;
        public const int SW_RESTORE = 9;
        
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOREDRAW = 0x0008;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;
        
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
"@

Write-Host "=== 游戏窗口强制显示工具 ===" -ForegroundColor Green
Write-Host ""

# 查找游戏进程
$gameProcesses = Get-Process -Name "WorldOfTheThreeKingdoms" -ErrorAction SilentlyContinue

if (-not $gameProcesses) {
    Write-Host "❌ 未找到游戏进程，正在启动游戏..." -ForegroundColor Yellow
    
    # 启动游戏
    if (Test-Path "WorldOfTheThreeKingdoms\bin\Win\WorldOfTheThreeKingdoms.exe") {
        $gameProcess = Start-Process -FilePath "WorldOfTheThreeKingdoms\bin\Win\WorldOfTheThreeKingdoms.exe" -PassThru -WorkingDirectory "WorldOfTheThreeKingdoms\bin\Win"
        Write-Host "✅ 游戏已启动，PID: $($gameProcess.Id)" -ForegroundColor Green
        Start-Sleep -Seconds 3
        $gameProcesses = @($gameProcess)
    } else {
        Write-Host "❌ 找不到游戏文件" -ForegroundColor Red
        exit 1
    }
}

foreach ($process in $gameProcesses) {
    Write-Host "🎮 找到游戏进程 PID: $($process.Id)" -ForegroundColor Cyan
    Write-Host "   主窗口句柄: $($process.MainWindowHandle)" -ForegroundColor Gray
    Write-Host "   主窗口标题: '$($process.MainWindowTitle)'" -ForegroundColor Gray
    
    # 查找所有属于游戏进程的窗口
    $gameWindows = @()
    $enumProc = {
        param($hWnd, $lParam)
        
        $processId = 0
        [WindowAPI]::GetWindowThreadProcessId($hWnd, [ref]$processId)
        
        if ($processId -eq $process.Id) {
            $length = [WindowAPI]::GetWindowTextLength($hWnd)
            $title = ""
            if ($length -gt 0) {
                $sb = New-Object System.Text.StringBuilder($length + 1)
                [WindowAPI]::GetWindowText($hWnd, $sb, $sb.Capacity)
                $title = $sb.ToString()
            }
            
            $isVisible = [WindowAPI]::IsWindowVisible($hWnd)
            
            # 获取窗口位置
            $rect = New-Object RECT
            [WindowAPI]::GetWindowRect($hWnd, [ref]$rect)
            
            $script:gameWindows += [PSCustomObject]@{
                Handle = $hWnd
                Title = $title
                Visible = $isVisible
                Left = $rect.Left
                Top = $rect.Top
                Right = $rect.Right
                Bottom = $rect.Bottom
                Width = $rect.Right - $rect.Left
                Height = $rect.Bottom - $rect.Top
            }
        }
        return $true
    }
    
    [WindowAPI]::EnumWindows($enumProc, [IntPtr]::Zero)
    
    Write-Host ""
    Write-Host "🔍 找到的游戏窗口:" -ForegroundColor Yellow
    
    $mainGameWindow = $null
    foreach ($window in $gameWindows) {
        Write-Host "   句柄: $($window.Handle)" -ForegroundColor Cyan
        Write-Host "   标题: '$($window.Title)'" -ForegroundColor Cyan
        Write-Host "   可见: $($window.Visible)" -ForegroundColor Cyan
        Write-Host "   位置: ($($window.Left), $($window.Top)) 大小: $($window.Width)x$($window.Height)" -ForegroundColor Cyan
        Write-Host ""
        
        # 找到主游戏窗口（通常是有标题且最大的窗口）
        if ($window.Title -like "*三国志*" -or $window.Title -like "*WorldOfTheThreeKingdoms*" -or 
            ($window.Width -gt 400 -and $window.Height -gt 300)) {
            $mainGameWindow = $window
        }
    }
    
    if ($mainGameWindow) {
        Write-Host "🎯 找到主游戏窗口: '$($mainGameWindow.Title)'" -ForegroundColor Green
        Write-Host ""
        
        # 多种方法尝试显示窗口
        Write-Host "🔧 尝试显示游戏窗口..." -ForegroundColor Yellow
        
        # 方法1: 恢复窗口
        Write-Host "   1. 恢复窗口..." -ForegroundColor Gray
        [WindowAPI]::ShowWindow($mainGameWindow.Handle, [WindowAPI]::SW_RESTORE) | Out-Null
        Start-Sleep -Milliseconds 500
        
        # 方法2: 显示窗口
        Write-Host "   2. 显示窗口..." -ForegroundColor Gray
        [WindowAPI]::ShowWindow($mainGameWindow.Handle, [WindowAPI]::SW_SHOW) | Out-Null
        Start-Sleep -Milliseconds 500
        
        # 方法3: 移动到屏幕中央
        Write-Host "   3. 移动到屏幕中央..." -ForegroundColor Gray
        $screenWidth = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Width
        $screenHeight = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Height
        $newX = [Math]::Max(0, ($screenWidth - $mainGameWindow.Width) / 2)
        $newY = [Math]::Max(0, ($screenHeight - $mainGameWindow.Height) / 2)
        [WindowAPI]::MoveWindow($mainGameWindow.Handle, $newX, $newY, $mainGameWindow.Width, $mainGameWindow.Height, $true) | Out-Null
        Start-Sleep -Milliseconds 500
        
        # 方法4: 置顶显示
        Write-Host "   4. 置顶显示..." -ForegroundColor Gray
        [WindowAPI]::SetWindowPos($mainGameWindow.Handle, [WindowAPI]::HWND_TOPMOST, 0, 0, 0, 0, 
            [WindowAPI]::SWP_NOMOVE -bor [WindowAPI]::SWP_NOSIZE -bor [WindowAPI]::SWP_SHOWWINDOW) | Out-Null
        Start-Sleep -Milliseconds 500
        
        # 方法5: 取消置顶但保持在前台
        Write-Host "   5. 设置前台窗口..." -ForegroundColor Gray
        [WindowAPI]::SetWindowPos($mainGameWindow.Handle, [WindowAPI]::HWND_NOTOPMOST, 0, 0, 0, 0, 
            [WindowAPI]::SWP_NOMOVE -bor [WindowAPI]::SWP_NOSIZE -bor [WindowAPI]::SWP_SHOWWINDOW) | Out-Null
        [WindowAPI]::SetForegroundWindow($mainGameWindow.Handle) | Out-Null
        [WindowAPI]::BringWindowToTop($mainGameWindow.Handle) | Out-Null
        
        Write-Host ""
        Write-Host "✅ 窗口显示操作完成！" -ForegroundColor Green
        Write-Host ""
        Write-Host "🎮 游戏窗口现在应该可见了！" -ForegroundColor Green
        Write-Host "   如果仍然看不到，请尝试：" -ForegroundColor Yellow
        Write-Host "   • 按 Alt+Tab 切换窗口" -ForegroundColor Yellow
        Write-Host "   • 检查任务栏是否有游戏图标" -ForegroundColor Yellow
        Write-Host "   • 检查其他显示器（如果有多个屏幕）" -ForegroundColor Yellow
        
    } else {
        Write-Host "⚠️  未找到主游戏窗口，尝试显示所有游戏相关窗口..." -ForegroundColor Yellow
        
        foreach ($window in $gameWindows) {
            if ($window.Width -gt 100 -and $window.Height -gt 100) {
                Write-Host "   显示窗口: '$($window.Title)'" -ForegroundColor Gray
                [WindowAPI]::ShowWindow($window.Handle, [WindowAPI]::SW_RESTORE) | Out-Null
                [WindowAPI]::SetForegroundWindow($window.Handle) | Out-Null
                Start-Sleep -Milliseconds 300
            }
        }
    }
}

Write-Host ""
Write-Host "🔍 当前游戏进程状态:" -ForegroundColor Cyan
Get-Process -Name "WorldOfTheThreeKingdoms" -ErrorAction SilentlyContinue | 
    Select-Object Id, ProcessName, MainWindowTitle, @{Name='Memory(MB)';Expression={[Math]::Round($_.WorkingSet64/1MB,2)}} |
    Format-Table -AutoSize

Write-Host ""
Write-Host "✨ 脚本执行完成！" -ForegroundColor Green
Write-Host "   按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")