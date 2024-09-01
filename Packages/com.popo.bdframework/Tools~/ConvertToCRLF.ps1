##############################################
#该脚本是将当前所有文件转换为 crlf结尾
##############################################
# 获取当前目录的上一级目录
$parentDir = (Get-Item -Path ..).FullName

# 获取上一级目录及其所有子目录中的 .cs 和 .meta 文件
$files = Get-ChildItem -Path $parentDir -Recurse -File -Include *.cs, *.meta

foreach ($file in $files) {
    # 读取文件内容
    $content = [System.IO.File]::ReadAllText($file.FullName)

    # 将所有的 LF (\n) 换行符替换为 CRLF (\r\n)，但保持已经是 CRLF 的部分不变
    $newContent = $content -replace "(?<!\r)\n", "`r`n"

    # 只有在内容发生变化时才写回文件
    if ($newContent -ne $content) {
        [System.IO.File]::WriteAllText($file.FullName, $newContent)
        Write-Host "Processed $($file.FullName)"
    } else {
        Write-Host "No change needed for $($file.FullName)"
    }
}

Write-Host "所有 .cs 和 .meta 文件的换行符已转换为 CRLF"


