$b = [char]::ConvertFromUtf32(0x25cf)
# display logo
$showlogo = $true
if (Test-Path "DevTool.json" -PathType Leaf) {
	$settings = Get-Content -Path "DevTool.json" | ConvertFrom-Json
	$showlogo = $settings.ShowLogo
}
Write-Output ""
if ($showlogo -eq $true) {
    Write-Host -ForegroundColor Red "	 $b "
    Write-Host -ForegroundColor Yellow -NoNewLine "	$b "
    Write-Host -ForegroundColor Green -NoNewLine "$b   "
    Write-Host "markuse arvuti asjad"
    Write-Host -ForegroundColor Blue "	 $b "
}
Write-Output ""
