# 窗口可见性测试脚本
Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    
    public class WindowHelper {
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
        
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }
"@

Write-Host "=== 窗口可见性测试 ===" -ForegroundColor Green
Write-Host ""

# 启动游戏
Write-Host "启动游戏..." -ForegroundColor Yellow
$gameProcess = Start-Process -FilePath "WorldOfTheThreeKingdoms\bin\Win\WorldOfTheThreeKingdoms.exe" -PassThru -WorkingDirectory "WorldOfTheThreeKingdoms\bin\Win"

if ($gameProcess) {
    Write-Host "游戏进程已启动，PID: $($gameProcess.Id)" -ForegroundColor Green
    
    # 等待游戏初始化
    Start-Sleep -Seconds 3
    
    # 检查进程是否还在运行
    $currentProcess = Get-Process -Id $gameProcess.Id -ErrorAction SilentlyContinue
    if ($currentProcess) {
        Write-Host "游戏进程仍在运行" -ForegroundColor Green
        Write-Host "主窗口句柄: $($currentProcess.MainWindowHandle)" -ForegroundColor Cyan
        Write-Host "主窗口标题: $($currentProcess.MainWindowTitle)" -ForegroundColor Cyan
        
        # 查找所有属于游戏进程的窗口
        $gameWindows = @()
        $enumProc = {
            param($hWnd, $lParam)
            
            $processId = 0
            [WindowHelper]::GetWindowThreadProcessId($hWnd, [ref]$processId)
            
            if ($processId -eq $gameProcess.Id) {
                $length = [WindowHelper]::GetWindowTextLength($hWnd)
                if ($length -gt 0) {
                    $sb = New-Object System.Text.StringBuilder($length + 1)
                    [WindowHelper]::GetWindowText($hWnd, $sb, $sb.Capacity)
                    $title = $sb.ToString()
                    
                    $isVisible = [WindowHelper]::IsWindowVisible($hWnd)
                    
                    $gameWindows += [PSCustomObject]@{
                        Handle = $hWnd
                        Title = $title
                        Visible = $isVisible
                    }
                }
            }
            return $true
        }
        
        [WindowHelper]::EnumWindows($enumProc, [IntPtr]::Zero)
        
        Write-Host ""
        Write-Host "找到的游戏窗口:" -ForegroundColor Yellow
        foreach ($window in $gameWindows) {
            Write-Host "  句柄: $($window.Handle), 标题: '$($window.Title)', 可见: $($window.Visible)" -ForegroundColor Cyan
            
            if ($window.Visible -eq $false -and $window.Title -like "*三国志*") {
                Write-Host "  尝试显示隐藏的窗口..." -ForegroundColor Yellow
                [WindowHelper]::ShowWindow($window.Handle, 9) # SW_RESTORE
                [WindowHelper]::SetForegroundWindow($window.Handle)
            }
        }
        
        # 等待用户确认
        Write-Host ""
        Write-Host "请检查游戏窗口是否现在可见。按任意键继续..." -ForegroundColor Green
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        
        # 结束游戏进程
        Write-Host "结束游戏进程..." -ForegroundColor Yellow
        $gameProcess.Kill()
        
    } else {
        Write-Host "游戏进程已退出" -ForegroundColor Red
        
        # 检查退出代码
        if ($gameProcess.HasExited) {
            Write-Host "退出代码: $($gameProcess.ExitCode)" -ForegroundColor Red
        }
    }
} else {
    Write-Host "无法启动游戏进程" -ForegroundColor Red
}

Write-Host ""
Write-Host "测试完成" -ForegroundColor Green