@echo off
echo ===================================================================
echo 智能破坏系统测试脚本
echo ===================================================================
echo.
echo 测试内容：
echo 1. 智能破坏系统入口调用
echo 2. 军师分析和对话显示
echo 3. 执行人员推荐功能
echo 4. 破坏成功率计算
echo.
echo 测试步骤：
echo 1. 启动游戏
echo 2. 选择势力并进入游戏
echo 3. 右键点击敌方建筑
echo 4. 选择"破坏"选项
echo 5. 观察智能破坏系统的工作流程
echo.
echo 预期结果：
echo - 显示可破坏的目标建筑列表
echo - 选择目标后显示军师分析对话
echo - 根据成功率显示支持或劝阻对话
echo - 显示执行人员选择界面（可能预选推荐人员）
echo - 正常执行破坏任务
echo.
echo 开始启动游戏进行测试...
echo.

cd /d "%~dp0"
start "" "WorldOfTheThreeKingdoms\bin\Win\WorldOfTheThreeKingdoms.exe"

echo 游戏已启动，请按照上述步骤进行测试
echo 查看调试输出以确认智能破坏系统是否正常工作
echo.
pause