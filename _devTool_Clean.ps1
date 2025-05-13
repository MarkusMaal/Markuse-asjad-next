# Deletes unneeded files from debugging the apps to free up disk space
clear
& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_Logo.ps1"
Get-ChildItem . -Directory -Exclude ".vscode",".vs",".idea",".git","out","UniplatformTest" | Foreach-Object {
	# this stores just the directory name, not the full path
	$pn = $_.Name
	Write-Output "- Cleaning $pn ..."
	Remove-Item -Path "$pn/bin" -Recurse -Force -ErrorAction SilentlyContinue -ProgressAction SilentlyContinue > $null
	Remove-Item -Path "$pn/obj" -Recurse -Force -ErrorAction SilentlyContinue -ProgressAction SilentlyContinue > $null
}
$CleanOut = Read-Host "Also delete out/ directory? (Y/N)"
if ($CleanOut -eq "Y" -or $CleanOut -eq "y") {
	Remove-Item -Path "out/" -Recurse -Force -ErrorAction SilentlyContinue -ProgressAction SilentlyContinue > $null
}
