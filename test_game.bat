@echo off
echo 正在启动游戏进行测试...
echo 已清理所有调试代码，恢复原始Draw方法
echo 如果主菜单仍然黑屏，问题可能在于：
echo 1. 背景图片文件缺失或损坏
echo 2. 纹理加载问题
echo 3. SpriteBatch状态问题
echo.
echo 启动游戏...
start "" "WorldOfTheThreeKingdoms.exe"
echo.
echo 请观察：
echo - 主菜单是否显示背景图片
echo - 是否有按钮显示
echo - 窗口是否正常响应
echo.
pause