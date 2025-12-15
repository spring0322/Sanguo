# 批量修复 GameManager 命名空间引用
$files = Get-ChildItem -Path "WorldOfTheThreeKingdoms" -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notlike "*\修改记录\*" }

$count = 0
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content -and $content.Contains("using GameManager;")) {
        $newContent = $content -replace "using GameManager;", "using WorldOfTheThreeKingdoms.GameManager;"
        if ($content -ne $newContent) {
            try {
                Set-Content $file.FullName -Value $newContent -NoNewline
                $count++
                Write-Host "修复: $($file.FullName)"
            } catch {
                Write-Host "跳过: $($file.FullName) - $($_.Exception.Message)"
            }
        }
    }
}

Write-Host "总共修复了 $count 个文件"