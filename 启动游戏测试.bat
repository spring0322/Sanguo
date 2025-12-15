@echo off
echo ========================================
echo 中华三国志 - 底部黑屏问题修复测试
echo ========================================
echo.
echo 已完成的修复：
echo ✓ 清理了所有导致窗口闪烁的调试代码
echo ✓ 修复了MainMenuScreen的viewportSize错误
echo ✓ 使用Platform.GraphicsDevice.Viewport获取实际窗口尺寸
echo ✓ 背景图片现在应该铺满整个窗口（包括底部）
echo ✓ 游戏编译成功，无错误
echo.
echo 正在启动游戏...
echo.

cd "WorldOfTheThreeKingdoms\bin\Win"
start "" "WorldOfTheThreeKingdoms.exe"

echo.
echo 游戏已启动，请观察：
echo 1. 底部三分之一是否还有黑屏？
echo 2. 背景图片是否铺满整个窗口？
echo 3. 窗口是否可以正常关闭？
echo 4. 按钮是否可以正常点击？
echo.
echo 修复说明：
echo - 现在使用GraphicsDevice.Viewport获取实际窗口尺寸
echo - 而不是Session.ResolutionX/Y（可能不匹配实际窗口）
echo - 背景图片应该完全覆盖窗口，不再有底部黑屏
echo.
pause