@echo off
echo 启动《三国志世界》(原版)...

REM 检查游戏文件是否存在
set "GAME_EXE=WorldOfTheThreeKingdoms\bin\Win\WorldOfTheThreeKingdoms.exe"

if not exist "%GAME_EXE%" (
    echo 错误：游戏文件不存在！
    echo 路径：%GAME_EXE%
    pause
    exit /b 1
)

REM 检查 OpenAL 库是否存在
set "OPENAL_DLL=WorldOfTheThreeKingdoms\bin\Win\soft_oal.dll"

if not exist "%OPENAL_DLL%" (
    echo 警告：OpenAL 音频库缺失！
    echo 正在自动设置 OpenAL 库...
    
    REM 从Desktop版本复制OpenAL库
    if exist "WorldOfTheThreeKingdoms.Desktop\bin\DesktopGL\AnyCPU\Release\soft_oal.dll" (
        copy "WorldOfTheThreeKingdoms.Desktop\bin\DesktopGL\AnyCPU\Release\soft_oal.dll" "%OPENAL_DLL%"
        echo OpenAL 库设置完成！
    ) else (
        echo 错误：找不到 OpenAL 库文件！
        echo 请先运行 setup_openal.bat 来设置 OpenAL 库。
        pause
        exit /b 1
    )
)

echo 启动游戏...
start "" "%GAME_EXE%"

echo 游戏已启动！控制台窗口将在 3 秒后自动关闭...
timeout /t 3 /nobreak >nul

REM 自动关闭控制台窗口
exit