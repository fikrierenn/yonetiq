$files = Get-ChildItem -Path d:\Dev\yonet -File -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs|node_modules)' }
Write-Output "Total Files: $($files.Count)"
$files | Group-Object Extension -NoElement | Sort-Object Count -Descending | Format-Table -AutoSize
$files | Select-Object -ExpandProperty FullName | Out-File -FilePath d:\Dev\yonet\inventory.txt -Encoding utf8
