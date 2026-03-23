@echo off
chcp 65001 >nul
color 0A
echo =======================================================
echo          .NET 项目深度清理工具 (缓存 + 编译输出)
echo =======================================================
echo.

echo [1/2] 正在强行清理全局 NuGet 缓存 (HTTP、Temp、全局包)...
dotnet nuget locals all --clear
echo NuGet 全局缓存清理完毕！
echo.

echo [2/2] 正在扫描并删除当前及所有子目录下的 bin 和 obj 文件夹...
FOR /d /r . %%d in (bin,obj) do (
    if exist "%%d" (
        echo 删除目录: %%d
        rd /s /q "%%d"
    )
)
echo 本地旧编译残留清理完毕！
echo.

echo =======================================================
echo   全部清理工作已完成！你的项目现在处于绝对干净的状态。
echo   请重新执行 dotnet restore 和编译操作。
echo =======================================================
pause