@echo off
chcp 65001 >nul
echo ===================================
echo 三国志世界 - 完整设置和启动工具
echo ===================================
echo.

echo [1/5] 检查游戏文件...
echo 当前目录: %CD%
if not exist "WorldOfTheThreeKingdoms\bin\Win\WorldOfTheThreeKingdoms.exe" (
    echo ✗ 游戏可执行文件不存在
    echo 尝试从当前目录查找...
    if exist "WorldOfTheThreeKingdoms.exe" (
        echo ✓ 在当前目录找到游戏文件
        set "GAME_DIR=%CD%"
        goto :found_game
    )
    pause
    exit /b 1
)
echo ✓ 游戏可执行文件存在
set "GAME_DIR=%CD%\WorldOfTheThreeKingdoms\bin\Win"
:found_game

echo.
echo [2/5] 检查 OpenAL 音频库...
if not exist "WorldOfTheThreeKingdoms\bin\Win\soft_oal.dll" (
    echo ✗ OpenAL 库缺失
    echo   请先运行 OpenAL 修复工具
    pause
    exit /b 1
)
echo ✓ OpenAL 库存在

echo.
echo [3/5] 设置内容文件...
if not exist "WorldOfTheThreeKingdoms\bin\Win\Content" (
    echo 正在复制 Content 目录...
    xcopy "Content" "WorldOfTheThreeKingdoms\bin\Win\Content" /E /I /Y >nul
    if errorlevel 1 (
        echo ✗ Content 目录复制失败
        pause
        exit /b 1
    )
    echo ✓ Content 目录已复制
) else (
    echo ✓ Content 目录已存在
)

echo.
echo [4/5] 设置 MODs 目录...
if not exist "WorldOfTheThreeKingdoms\bin\Win\MODs" (
    if exist "MODs" (
        echo 正在复制 MODs 目录...
        xcopy "MODs" "WorldOfTheThreeKingdoms\bin\Win\MODs" /E /I /Y >nul
        if errorlevel 1 (
            echo ✗ MODs 目录复制失败
            pause
            exit /b 1
        )
        echo ✓ MODs 目录已复制
    ) else (
        echo 正在创建空的 MODs 目录...
        mkdir "WorldOfTheThreeKingdoms\bin\Win\MODs" >nul 2>&1
        echo ✓ MODs 目录已创建
    )
) else (
    echo ✓ MODs 目录已存在
)

echo.
echo [5/5] 启动游戏...
echo 正在启动《三国志世界》...
echo.

cd "WorldOfTheThreeKingdoms\bin\Win"
start "" "WorldOfTheThreeKingdoms.exe"

echo 游戏已启动！
echo 等待游戏初始化...
timeout /t 3 /nobreak >nul

echo.
echo 检查游戏窗口可见性...
cd "%~dp0"
powershell -ExecutionPolicy Bypass -Command "& {Add-Type -AssemblyName System.Windows.Forms; $processes = Get-Process -Name 'WorldOfTheThreeKingdoms' -ErrorAction SilentlyContinue; if ($processes) { Write-Host '✓ 游戏进程运行中 (PID: ' $processes[0].Id ')' -ForegroundColor Green; if ([string]::IsNullOrEmpty($processes[0].MainWindowTitle)) { Write-Host '⚠ 游戏窗口可能不可见，正在尝试显示...' -ForegroundColor Yellow; } else { Write-Host '✓ 游戏窗口: ' $processes[0].MainWindowTitle -ForegroundColor Green; } } else { Write-Host '✗ 游戏进程未运行' -ForegroundColor Red; } }"

echo.
echo 🎮 游戏启动完成！
echo.
echo 如果看不到游戏窗口，请：
echo 1. 按 Alt+Tab 切换到游戏窗口
echo 2. 运行 show_game_window.bat 强制显示窗口
echo 3. 检查任务栏是否有游戏图标
echo 4. 检查其他显示器（如果有多个屏幕）
echo.
echo 如果游戏无法正常运行，请检查：
echo - 显卡驱动是否最新
echo - .NET Framework 4.8 是否已安装
echo - Visual C++ 运行时库是否已安装
echo.
echo 按任意键退出...
pause >nul