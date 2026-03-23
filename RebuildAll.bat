@echo off
echo ========================================
echo 清理并重新编译项目
echo ========================================

echo.
echo [1/4] 清理 Source Generator...
dotnet clean WorldOfTheThreeKingdoms.SourceGenerators/WorldOfTheThreeKingdoms.SourceGenerators.csproj

echo.
echo [2/4] 清理主项目...
dotnet clean WorldOfTheThreeKingdoms/WorldOfTheThreeKingdoms.csproj

echo.
echo [3/4] 编译 Source Generator...
dotnet build WorldOfTheThreeKingdoms.SourceGenerators/WorldOfTheThreeKingdoms.SourceGenerators.csproj

echo.
echo [4/4] 编译主项目...
dotnet build WorldOfTheThreeKingdoms/WorldOfTheThreeKingdoms.csproj

echo.
echo ========================================
echo 编译完成！请重新启动游戏。
echo ========================================
pause
