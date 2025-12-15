@echo off
title 《三国志世界》启动器
color 0A

:menu
cls
echo.
echo ========================================
echo     《三国志世界》启动器
echo ========================================
echo.
echo 请选择要执行的操作：
echo.
echo 1. 启动游戏（无控制台窗口）
echo 2. 启动游戏（调试模式）
echo 3. 启动修改器
echo 4. 设置游戏文件
echo 5. 设置修改器
echo 6. 查看使用说明
echo 0. 退出
echo.
set /p choice=请输入选项 (0-6): 

if "%choice%"=="1" goto start_game
if "%choice%"=="2" goto start_game_debug
if "%choice%"=="3" goto start_editor
if "%choice%"=="4" goto setup_game
if "%choice%"=="5" goto setup_editor
if "%choice%"=="6" goto show_help
if "%choice%"=="0" goto exit
goto menu

:start_game
echo.
echo 启动游戏（无控制台窗口）...
call run_game_clean.bat
goto menu

:start_game_debug
echo.
echo 启动游戏（调试模式）...
call run_game_debug.bat
goto menu

:start_editor
echo.
echo 启动修改器...
call run_editor.bat
goto menu

:setup_game
echo.
echo 设置游戏文件...
call setup_game_files.bat
pause
goto menu

:setup_editor
echo.
echo 设置修改器...
call setup_editor.bat
goto menu

:show_help
cls
echo.
echo ========================================
echo           使用说明
echo ========================================
echo.
echo 游戏相关：
echo - 首次运行请先执行"设置游戏文件"
echo - 游戏需要 OpenAL 音频库支持
echo - 如遇到 DLL 缺失错误，运行设置脚本会自动修复
echo.
echo 修改器相关：
echo - 修改器用于编辑剧本和游戏数据
echo - 需要先打开剧本文件才能进行编辑
echo - 支持导出到 Excel 和批量操作
echo.
echo 文件说明：
echo - run_game_clean.bat     : 简洁启动游戏
echo - run_game_debug.bat     : 调试模式启动
echo - run_editor.bat         : 启动修改器
echo - setup_game_files.bat   : 设置游戏文件
echo - Editor_Usage_Guide.md  : 详细使用指南
echo.
echo 按任意键返回主菜单...
pause >nul
goto menu

:exit
echo.
echo 感谢使用《三国志世界》启动器！
timeout /t 2 /nobreak >nul
exit

:error
echo.
echo 发生错误，请检查文件是否存在。
pause
goto menu