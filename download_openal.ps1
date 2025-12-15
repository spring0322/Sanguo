# OpenAL 自动下载和安装脚本
param(
    [string]$TargetDir = "WorldOfTheThreeKingdoms\bin\Win"
)

Write-Host "====================================" -ForegroundColor Green
Write-Host "OpenAL 音频库自动安装工具" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""

# 检查目标目录
if (-not (Test-Path $TargetDir)) {
    Write-Host "错误: 目标目录不存在: $TargetDir" -ForegroundColor Red
    exit 1
}

Write-Host "目标目录: $TargetDir" -ForegroundColor Yellow
Write-Host ""

# OpenAL 下载 URL
$openalUrl = "https://openal-soft.org/openal-binaries/openal-soft-1.23.1-bin.zip"
$tempZip = "$env:TEMP\openal-soft.zip"
$tempExtract = "$env:TEMP\openal-soft"

try {
    Write-Host "正在下载 OpenAL Soft 库..." -ForegroundColor Yellow
    
    # 下载文件
    Invoke-WebRequest -Uri $openalUrl -OutFile $tempZip -UseBasicParsing
    Write-Host "✓ 下载完成" -ForegroundColor Green
    
    Write-Host "正在解压文件..." -ForegroundColor Yellow
    
    # 解压文件
    if (Test-Path $tempExtract) {
        Remove-Item $tempExtract -Recurse -Force
    }
    Expand-Archive -Path $tempZip -DestinationPath $tempExtract -Force
    Write-Host "✓ 解压完成" -ForegroundColor Green
    
    # 查找 soft_oal.dll 文件
    $dllPath = Get-ChildItem -Path $tempExtract -Name "soft_oal.dll" -Recurse | Select-Object -First 1
    
    if ($dllPath) {
        $sourceDll = Join-Path $tempExtract $dllPath
        $targetDll = Join-Path $TargetDir "soft_oal.dll"
        
        Write-Host "正在复制 soft_oal.dll 到游戏目录..." -ForegroundColor Yellow
        Copy-Item $sourceDll $targetDll -Force
        Write-Host "✓ 复制完成" -ForegroundColor Green
        
        # 验证文件
        if (Test-Path $targetDll) {
            Write-Host ""
            Write-Host "✓ OpenAL 库安装成功!" -ForegroundColor Green
            Write-Host "文件位置: $targetDll" -ForegroundColor Gray
            
            # 获取文件信息
            $fileInfo = Get-Item $targetDll
            Write-Host "文件大小: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
            Write-Host "修改时间: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
        } else {
            Write-Host "✗ 文件复制失败" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "✗ 在下载的文件中找不到 soft_oal.dll" -ForegroundColor Red
        exit 1
    }
    
} catch {
    Write-Host "✗ 安装失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # 清理临时文件
    if (Test-Path $tempZip) {
        Remove-Item $tempZip -Force
    }
    if (Test-Path $tempExtract) {
        Remove-Item $tempExtract -Recurse -Force
    }
}

Write-Host ""
Write-Host "现在可以尝试启动游戏了:" -ForegroundColor Yellow
Write-Host "  cd $TargetDir" -ForegroundColor Gray
Write-Host "  .\WorldOfTheThreeKingdoms.exe" -ForegroundColor Gray
Write-Host ""