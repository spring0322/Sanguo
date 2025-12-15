# OpenAL 音频库设置指南

## 问题描述

如果你在启动游戏时遇到以下错误：
```
System.DllNotFoundException: 无法加载 DLL "soft_oal.dll": 找不到指定的模块。
```

这是因为缺少 OpenAL Soft 音频库，这是 MonoGame 在 Windows 上处理音频所需的库。

## 解决方案

### 方法一：自动设置（推荐）

1. 运行 `setup_game_files.bat` - 这会自动下载并设置所有必需的文件，包括 OpenAL 库
2. 或者单独运行 `setup_openal.bat` 来只设置 OpenAL 库

### 方法二：手动设置

1. 下载 OpenAL Soft 库：
   - 访问：https://openal-soft.org/openal-binaries/
   - 下载 `openal-soft-1.23.1-bin.zip`

2. 解压文件并复制 DLL：
   - 从 `bin/Win32/soft_oal.dll` 复制到游戏目录
   - 从 `bin/Win32/soft_oal.dll` 复制到游戏目录的 `x86/` 文件夹
   - 从 `bin/Win64/soft_oal.dll` 复制到游戏目录的 `x64/` 文件夹

3. 游戏目录路径：
   ```
   WorldOfTheThreeKingdoms.Desktop/bin/DesktopGL/AnyCPU/Debug/
   ```

## 文件结构

设置完成后，你的游戏目录应该包含：
```
WorldOfTheThreeKingdoms.Desktop/bin/DesktopGL/AnyCPU/Debug/
├── soft_oal.dll          (主 OpenAL 库)
├── x86/
│   └── soft_oal.dll      (32位版本)
├── x64/
│   └── soft_oal.dll      (64位版本)
└── WorldOfTheThreeKingdoms.Desktop.exe
```

## 启动游戏

设置完成后，你可以：
- 直接运行 `WorldOfTheThreeKingdoms.Desktop.exe`
- 使用 `run_game_clean.bat` - 简洁启动（推荐）
- 使用 `run_game.bat` - 带提示信息的启动
- 使用 `run_game_debug.bat` - 调试模式启动

**注意：** 游戏现在默认不显示调试控制台窗口，直接进入游戏界面。如果需要调试信息，请使用调试模式启动脚本。

## 注意事项

- OpenAL Soft 是开源的音频库，完全免费使用
- 这个库提供了跨平台的音频支持
- 如果你仍然遇到问题，请确保你的 Windows 系统是最新的，并安装了最新的 Visual C++ 运行时库

## 故障排除

如果游戏仍然无法启动：

1. 确保所有 DLL 文件都在正确的位置
2. 尝试以管理员身份运行游戏
3. 检查 Windows 防火墙或杀毒软件是否阻止了文件
4. 确保你的系统安装了 .NET Framework 4.8 或更高版本

## 启动脚本说明

- `run_game_clean.bat` - 最简洁的启动方式，自动检查依赖项后直接启动游戏
- `run_game.bat` - 带有状态提示的启动方式，会显示启动过程
- `run_game_debug.bat` - 调试模式启动，适合开发者使用
- `run_game_silent.bat` - 静默启动，控制台窗口立即关闭

## 开发者选项

如果你是开发者并需要查看调试信息：
1. 打开 `WorldOfTheThreeKingdoms.Desktop/Program.cs`
2. 将 `static bool isDebug = false;` 改为 `static bool isDebug = true;`
3. 重新编译项目
4. 使用 `run_game_debug.bat` 启动游戏