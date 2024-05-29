& "$(Split-Path $MyInvocation.MyCommand.Path)/_devTool_Logo.ps1"
Write-Output "Available projects:"
Get-ChildItem . -Directory -Exclude ".vscode" | Foreach-Object {
	# this stores just the directory name, not the full path
	$pn = $_.Name
	Write-Output "- $pn"
}